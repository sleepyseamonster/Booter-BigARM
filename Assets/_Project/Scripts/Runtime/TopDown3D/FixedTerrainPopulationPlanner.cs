using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Immutable exclusion footprint. Corridors reserve traversal; disks reserve sites/spawn clearance.</summary>
    public readonly struct FixedTerrainPopulationExclusion
    {
        public readonly Vector2 Start, End;
        public readonly float Radius;
        public FixedTerrainPopulationExclusion(Vector2 start, Vector2 end, float radius)
        {
            if (!Finite(start) || !Finite(end) || !float.IsFinite(radius) || radius < 0) throw new ArgumentException("Invalid protected footprint.");
            Start = start; End = end; Radius = radius;
        }
        public bool Intersects(Vector2 point, float footprintRadius)
        {
            var segment = End - Start;
            float t = segment.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector2.Dot(point - Start, segment) / segment.sqrMagnitude) : 0;
            float distance = Radius + footprintRadius;
            return (point - (Start + segment * t)).sqrMagnitude <= distance * distance;
        }
        private static bool Finite(Vector2 v) => float.IsFinite(v.x) && float.IsFinite(v.y);
    }

    public sealed class FixedTerrainPopulationPlacement
    {
        public readonly Vector2Int Cell;
        public readonly int Ordinal;
        public readonly string StableId;
        public readonly Vector3 Position, Normal;
        public readonly float Yaw, Scale;
        internal FixedTerrainPopulationPlacement(Vector2Int cell, int ordinal, string id, Vector3 position, Vector3 normal, float yaw, float scale)
        { Cell = cell; Ordinal = ordinal; StableId = id; Position = position; Normal = normal; Yaw = yaw; Scale = scale; }
        public FixedTerrainPopulationCandidateAddress Address(string recipeId) => new() { x = Cell.x, z = Cell.y, ordinal = Ordinal, recipeId = recipeId };
    }

    /// <summary>Bounded placement on a fixed surface. No Unity objects, native terrain writes or mutable random stream.</summary>
    public sealed class FixedTerrainPopulationPlanner
    {
        public readonly struct Surface
        {
            public readonly Vector3 Position, Normal;
            public Surface(Vector3 position, Vector3 normal) { Position = position; Normal = normal; }
        }
        public delegate bool SurfaceQuery(Vector2 position, out Surface surface);
        private readonly FixedTerrainPopulationIdentity identity;
        private readonly SurfaceQuery surface;
        private readonly FixedTerrainPopulationExclusion[] exclusions;
        private readonly float spacing, footprintRadius, slope, relief;
        private readonly double density;
        private const int Grid = FixedTerrainPopulationIdentity.CandidateGridSize;
        private const float SlotSize = (float)FixedTerrainPopulationIdentity.CellSize / Grid;

        public FixedTerrainPopulationPlanner(FixedTerrainPopulationIdentity identity, SurfaceQuery surface,
            IEnumerable<FixedTerrainPopulationExclusion> exclusions, float minimumSpacing, float footprintRadius,
            float maximumSlopeDegrees, float maximumFootprintRelief, double density)
        {
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.surface = surface ?? throw new ArgumentNullException(nameof(surface));
            if (!float.IsFinite(minimumSpacing) || minimumSpacing < 1 || minimumSpacing > 128
                || !float.IsFinite(footprintRadius) || footprintRadius <= 0 || footprintRadius * 2 > minimumSpacing
                || !float.IsFinite(maximumSlopeDegrees) || maximumSlopeDegrees < 0 || maximumSlopeDegrees >= 90
                || !float.IsFinite(maximumFootprintRelief) || maximumFootprintRelief < 0
                || double.IsNaN(density) || density < 0 || density > 1) throw new ArgumentException("Invalid bounded placement rules.");
            this.exclusions = exclusions == null ? Array.Empty<FixedTerrainPopulationExclusion>() : new List<FixedTerrainPopulationExclusion>(exclusions).ToArray();
            spacing = minimumSpacing; this.footprintRadius = footprintRadius; // Maximum world radius, including the recipe's scale range.
            slope = maximumSlopeDegrees; relief = maximumFootprintRelief; this.density = density;
        }

        // Native collision queries remain main-thread work. A worker must use an independently validated immutable surface cache.
        public static SurfaceQuery NativeSurface(FixedTerrainSurfaceIndex index)
        {
            if (index == null) throw new ArgumentNullException(nameof(index));
            return (Vector2 point, out Surface result) =>
            {
                bool found = index.TrySample(point, out var sample);
                result = found ? new Surface(sample.Position, sample.Normal) : default;
                return found;
            };
        }
        public CellBuild Begin(Vector2Int cell) => new(this, cell);

        public sealed class CellBuild
        {
            private readonly struct Candidate
            {
                public readonly Vector2 Position;
                public readonly double Priority;
                public readonly bool Eligible;
                public Candidate(Vector2 point, double priority, bool eligible) { Position = point; Priority = priority; Eligible = eligible; }
            }
            private readonly FixedTerrainPopulationPlanner owner;
            private readonly Vector2Int cell;
            private readonly Dictionary<(long, long), Candidate> cache = new();
            private readonly List<FixedTerrainPopulationPlacement> placements = new();
            private readonly IReadOnlyList<FixedTerrainPopulationPlacement> view;
            private readonly Stopwatch clock = new();
            private readonly int halo, side;
            private int ordinal, phase, neighbour, mask, probe;
            private long gx, gz;
            private Candidate current;
            private Surface center;
            private float lowest, highest;
            private bool generatingCandidate;
            private long pendingX, pendingZ;
            private Vector2Int pendingCell;
            private int pendingSlot, pendingStream;
            private double jitterX, jitterZ, pendingPriority;
            private string placementId;
            private float placementYaw, placementScale;
            public bool Complete => ordinal >= FixedTerrainPopulationIdentity.CandidatesPerCell;
            public bool Cancelled { get; private set; }
            public int Operations { get; private set; }
            public int SurfaceQueries { get; private set; }
            public int IdentityHashes { get; private set; }
            public int CachedCandidates => cache.Count;
            public Vector2Int Cell => cell;
            public string RecipeId => owner.identity.RecipeId;
            public IReadOnlyList<FixedTerrainPopulationPlacement> Placements => view;
            internal CellBuild(FixedTerrainPopulationPlanner owner, Vector2Int cell)
            {
                this.owner = owner; this.cell = cell;
                view = placements.AsReadOnly();
                // Jitter stays within [.1,.9] of a slot. Beyond ceil(spacing/slot), even the
                // nearest possible competitor is at least spacing + .2 slots away.
                // Keep the former extra ring outside +/-1,048,576 m, where float rounding
                // can erode that margin. No random stream or candidate address changes.
                bool preciseLattice = Math.Abs((long)cell.x) <= 4095 && Math.Abs((long)cell.y) <= 4095;
                halo = Mathf.CeilToInt(owner.spacing / SlotSize) + (preciseLattice ? 0 : 1); side = halo * 2 + 1;
            }
            public void Cancel() { Cancelled = true; cache.Clear(); placements.Clear(); }
            internal bool MatchesState(FixedTerrainPopulationState state) => state != null
                && state.MatchesRecipe(owner.identity, FixedTerrainPopulationIdentity.CandidatesPerCell);
            public bool TryCommitBaseline(FixedTerrainPopulationState state)
            {
                if (!Complete || Cancelled || !MatchesState(state)) return false;
                return state.RegisterPlannedBaseline(this);
            }

            /// <summary>Each operation performs at most one identity hash, neighbour/mask check, surface query or placement publication.</summary>
            public int Advance(int maximumOperations, double milliseconds)
            {
                if (maximumOperations < 1 || double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds <= 0)
                    throw new ArgumentException("A finite positive work budget is required.");
                int count = 0; clock.Restart();
                while (!Complete && !Cancelled && count < maximumOperations && clock.Elapsed.TotalMilliseconds < milliseconds)
                { Step(); count++; Operations++; }
                return count;
            }

            private void Step()
            {
                if (generatingCandidate) { GenerateCandidateStep(); return; }
                if (phase == 0)
                {
                    gx = (long)cell.x * Grid + ordinal % Grid; gz = (long)cell.y * Grid + ordinal / Grid;
                    if (!TryGetCandidate(gx, gz, out current)) return;
                    if (!current.Eligible) { Next(); return; }
                    neighbour = 0; phase = 1; return;
                }
                if (phase == 1)
                {
                    if (neighbour >= side * side) { mask = 0; phase = 2; return; }
                    long x = gx + neighbour % side - halo, z = gz + neighbour / side - halo;
                    if (x == gx && z == gz) { neighbour++; return; }
                    if (!TryGetCandidate(x, z, out var other)) return;
                    neighbour++;
                    if (other.Eligible && (other.Position - current.Position).sqrMagnitude < owner.spacing * owner.spacing
                        && (other.Priority < current.Priority || other.Priority == current.Priority && (x < gx || x == gx && z < gz))) Next();
                    return;
                }
                if (phase == 2)
                {
                    if (mask < owner.exclusions.Length)
                    { if (owner.exclusions[mask++].Intersects(current.Position, owner.footprintRadius)) Next(); return; }
                    probe = 0; phase = 3; return;
                }
                if (phase == 4) { IdentityHashes++; placementId = owner.identity.CandidateId(cell, ordinal); phase = 5; return; }
                if (phase == 5) { placementYaw = (float)Sample(cell, ordinal, 4) * 360; phase = 6; return; }
                if (phase == 6) { placementScale = .85f + (float)Sample(cell, ordinal, 6) * .3f; phase = 7; return; }
                if (phase == 7)
                {
                    placements.Add(new FixedTerrainPopulationPlacement(cell, ordinal, placementId,
                        center.Position, center.Normal.normalized, placementYaw, placementScale));
                    Next(); return;
                }
                Vector2 point = current.Position;
                if (probe > 0)
                {
                    float angle = (probe - 1) * Mathf.PI / 4;
                    point += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * owner.footprintRadius;
                }
                SurfaceQueries++;
                if (!owner.surface(point, out var sample) || !ValidSurface(point, sample)) { Next(); return; }
                if (probe == 0) { center = sample; lowest = highest = sample.Position.y; }
                else { lowest = Mathf.Min(lowest, sample.Position.y); highest = Mathf.Max(highest, sample.Position.y); }
                if (highest - lowest > owner.relief) { Next(); return; }
                probe++;
                if (probe < 9) return;
                phase = 4;
            }
            private bool ValidSurface(Vector2 point, Surface sample)
            {
                var p = sample.Position; var n = sample.Normal;
                return float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z) && float.IsFinite(n.x) && float.IsFinite(n.y)
                    && float.IsFinite(n.z) && n.sqrMagnitude > .000001f && n.y > 0
                    && (new Vector2(p.x, p.z) - point).sqrMagnitude < .0001f && Vector3.Angle(n, Vector3.up) <= owner.slope;
            }
            private bool TryGetCandidate(long x, long z, out Candidate value)
            {
                if (cache.TryGetValue((x, z), out value)) return true;
                long cx = FloorDiv(x), cz = FloorDiv(z);
                pendingCell = new Vector2Int(checked((int)cx), checked((int)cz));
                pendingSlot = (int)(z - cz * Grid) * Grid + (int)(x - cx * Grid);
                pendingX = x; pendingZ = z; pendingStream = 0; generatingCandidate = true;
                return false;
            }
            private double Sample(Vector2Int coordinate, int slot, int stream)
            { IdentityHashes++; return owner.identity.Sample01(coordinate, slot, stream); }
            private void GenerateCandidateStep()
            {
                if (pendingStream == 0) jitterX = Sample(pendingCell, pendingSlot, 1);
                else if (pendingStream == 1) jitterZ = Sample(pendingCell, pendingSlot, 2);
                else if (pendingStream == 2) pendingPriority = Sample(pendingCell, pendingSlot, 3);
                else
                {
                    bool eligible = Sample(pendingCell, pendingSlot, 5) < owner.density;
                    double x = (double)pendingCell.x * FixedTerrainPopulationIdentity.CellSize + (pendingSlot % Grid + .1 + .8 * jitterX) * SlotSize;
                    double z = (double)pendingCell.y * FixedTerrainPopulationIdentity.CellSize + (pendingSlot / Grid + .1 + .8 * jitterZ) * SlotSize;
                    cache.Add((pendingX, pendingZ), new Candidate(new Vector2((float)x, (float)z), pendingPriority, eligible));
                    generatingCandidate = false;
                }
                pendingStream++;
            }
            private static long FloorDiv(long value) => value >= 0 ? value / Grid : (value + 1) / Grid - 1;
            private void Next() { ordinal++; phase = 0; placementId = null; }
        }
    }
}
