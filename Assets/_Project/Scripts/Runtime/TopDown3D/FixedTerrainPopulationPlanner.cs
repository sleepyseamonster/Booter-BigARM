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
            public bool Complete => ordinal >= FixedTerrainPopulationIdentity.CandidatesPerCell;
            public bool Cancelled { get; private set; }
            public int Operations { get; private set; }
            public int SurfaceQueries { get; private set; }
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

            /// <summary>Each operation is one candidate lookup, one neighbour, one mask or one surface query.</summary>
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
                if (phase == 0)
                {
                    gx = (long)cell.x * Grid + ordinal % Grid; gz = (long)cell.y * Grid + ordinal / Grid;
                    current = GetCandidate(gx, gz);
                    if (!current.Eligible) { Next(); return; }
                    neighbour = 0; phase = 1; return;
                }
                if (phase == 1)
                {
                    if (neighbour >= side * side) { mask = 0; phase = 2; return; }
                    long x = gx + neighbour % side - halo, z = gz + neighbour / side - halo; neighbour++;
                    if (x == gx && z == gz) return;
                    var other = GetCandidate(x, z);
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
                placements.Add(new FixedTerrainPopulationPlacement(cell, ordinal, owner.identity.CandidateId(cell, ordinal),
                    center.Position, center.Normal.normalized, (float)owner.identity.Sample01(cell, ordinal, 4) * 360,
                    .85f + (float)owner.identity.Sample01(cell, ordinal, 6) * .3f));
                Next();
            }
            private bool ValidSurface(Vector2 point, Surface sample)
            {
                var p = sample.Position; var n = sample.Normal;
                return float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z) && float.IsFinite(n.x) && float.IsFinite(n.y)
                    && float.IsFinite(n.z) && n.sqrMagnitude > .000001f && n.y > 0
                    && (new Vector2(p.x, p.z) - point).sqrMagnitude < .0001f && Vector3.Angle(n, Vector3.up) <= owner.slope;
            }
            private Candidate GetCandidate(long x, long z)
            {
                if (cache.TryGetValue((x, z), out var value)) return value;
                long cx = FloorDiv(x), cz = FloorDiv(z);
                var coordinate = new Vector2Int(checked((int)cx), checked((int)cz));
                int slot = (int)(z - cz * Grid) * Grid + (int)(x - cx * Grid);
                value = new Candidate(owner.identity.CandidatePosition(coordinate, slot), owner.identity.Sample01(coordinate, slot, 3),
                    owner.identity.Sample01(coordinate, slot, 5) < owner.density);
                cache.Add((x, z), value); return value;
            }
            private static long FloorDiv(long value) => value >= 0 ? value / Grid : (value + 1) / Grid - 1;
            private void Next() { ordinal++; phase = 0; }
        }
    }
}
