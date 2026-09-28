using System;
using System.Collections.Generic;
using System.Globalization;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// Bounded calibration values for candidate detection. These are technical-study values,
    /// not accepted production cliff thresholds or a new terrain-height authority.
    /// </summary>
    public readonly struct WorldCliffStudyProfile
    {
        public WorldCliffStudyProfile(
            double cellSpan,
            double traceStep,
            int maximumTraceSteps,
            float minimumSlopeDegrees,
            double minimumDrop,
            int minimumSteepSamples,
            double debrisRunout,
            double debrisHalfWidth)
        {
            CellSpan = Positive(cellSpan, nameof(cellSpan));
            TraceStep = Positive(traceStep, nameof(traceStep));
            if (maximumTraceSteps < 2 || maximumTraceSteps > 32)
                throw new ArgumentOutOfRangeException(nameof(maximumTraceSteps));
            MaximumTraceSteps = maximumTraceSteps;
            if (float.IsNaN(minimumSlopeDegrees) || float.IsInfinity(minimumSlopeDegrees)
                || minimumSlopeDegrees <= 0f || minimumSlopeDegrees >= 89f)
                throw new ArgumentOutOfRangeException(nameof(minimumSlopeDegrees));
            MinimumSlopeDegrees = minimumSlopeDegrees;
            MinimumDrop = Positive(minimumDrop, nameof(minimumDrop));
            if (minimumSteepSamples < 1 || minimumSteepSamples > maximumTraceSteps * 2 + 1)
                throw new ArgumentOutOfRangeException(nameof(minimumSteepSamples));
            MinimumSteepSamples = minimumSteepSamples;
            DebrisRunout = Positive(debrisRunout, nameof(debrisRunout));
            DebrisHalfWidth = Positive(debrisHalfWidth, nameof(debrisHalfWidth));
        }

        public double CellSpan { get; }
        public double TraceStep { get; }
        public int MaximumTraceSteps { get; }
        public float MinimumSlopeDegrees { get; }
        public double MinimumDrop { get; }
        public int MinimumSteepSamples { get; }
        public double DebrisRunout { get; }
        public double DebrisHalfWidth { get; }

        public static WorldCliffStudyProfile CreateTechnicalStudy()
            => new WorldCliffStudyProfile(1.5d, 0.75d, 8, 35f, 1.25d, 2, 5d, 3d);

        private static double Positive(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public readonly struct WorldCliffSectionCandidate
    {
        public WorldCliffSectionCandidate(
            WorldFeatureId id,
            WorldFeatureId parentFeatureId,
            long ownerCellA,
            long ownerCellB,
            AbsoluteWorldPosition center,
            AbsoluteWorldPosition rim,
            AbsoluteWorldPosition toe,
            double outwardA,
            double outwardB,
            float centerSlopeDegrees,
            string strataFamilyId,
            double debrisRunout,
            double debrisHalfWidth)
        {
            Id = id;
            ParentFeatureId = parentFeatureId;
            OwnerCellA = ownerCellA;
            OwnerCellB = ownerCellB;
            Center = center;
            Rim = rim;
            Toe = toe;
            OutwardA = outwardA;
            OutwardB = outwardB;
            CenterSlopeDegrees = centerSlopeDegrees;
            StrataFamilyId = strataFamilyId ?? string.Empty;
            DebrisRunout = debrisRunout;
            DebrisHalfWidth = debrisHalfWidth;
        }

        public WorldFeatureId Id { get; }
        // Empty on broad ground: its DominantFeatureId is a per-position context ID,
        // not the identity of a continuous landform or cliff.
        public WorldFeatureId ParentFeatureId { get; }
        public long OwnerCellA { get; }
        public long OwnerCellB { get; }
        public AbsoluteWorldPosition Center { get; }
        public AbsoluteWorldPosition Rim { get; }
        public AbsoluteWorldPosition Toe { get; }
        public double OutwardA { get; }
        public double OutwardB { get; }
        public float CenterSlopeDegrees { get; }
        public string StrataFamilyId { get; }
        public double DebrisRunout { get; }
        public double DebrisHalfWidth { get; }
        public double VerticalDrop => Rim.Vertical - Toe.Vertical;

        /// <summary>
        /// A semantic deposit influence below this source, not a rock-placement decision.
        /// The downstream decorator must still apply route, formation, and collision constraints.
        /// </summary>
        public float SampleDebrisInfluence(AbsoluteWorldPosition position)
        {
            var deltaA = position.HorizontalA - Toe.HorizontalA;
            var deltaB = position.HorizontalB - Toe.HorizontalB;
            var forward = deltaA * OutwardA + deltaB * OutwardB;
            if (forward < 0d || forward >= DebrisRunout) return 0f;
            var sideways = Math.Abs(deltaA * OutwardB - deltaB * OutwardA);
            if (sideways >= DebrisHalfWidth) return 0f;
            var along = 1d - forward / DebrisRunout;
            var across = 1d - sideways / DebrisHalfWidth;
            return (float)(along * along * across * across);
        }
    }

    /// <summary>
    /// Samples the canonical world query at absolute grid centers. A section's owner and ID
    /// do not depend on the requesting terrain chunk, local origin, or build order.
    /// This study deliberately makes no Unity meshes, colliders, or persistent entities.
    /// </summary>
    public sealed class WorldCliffSectionStudy
    {
        private static readonly WorldSeedNamespace SectionNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.cliff-section-study");
        private readonly WorldIdentity world;
        private readonly IWorldCoordinateModel coordinateModel;
        private readonly IWorldQueryService query;
        private readonly WorldCliffStudyProfile profile;

        public WorldCliffSectionStudy(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            IWorldQueryService query,
            WorldCliffStudyProfile profile)
        {
            this.world = world;
            this.coordinateModel = coordinateModel ?? throw new ArgumentNullException(nameof(coordinateModel));
            this.query = query ?? throw new ArgumentNullException(nameof(query));
            this.profile = profile;
            if (profile.CellSpan <= 0d) throw new ArgumentException("A cliff study profile is required.", nameof(profile));
        }

        public bool TryBuild(long cellA, long cellB, out WorldCliffSectionCandidate candidate, out string error)
        {
            candidate = default;
            var centerA = (cellA + 0.5d) * profile.CellSpan;
            var centerB = (cellB + 0.5d) * profile.CellSpan;
            var centerPoint = new AbsoluteWorldPosition(centerA, 0d, centerB);
            if (!query.TrySampleSurface(centerPoint, out var center, out error)) return false;
            if (SlopeDegrees(center.NormalVertical) < profile.MinimumSlopeDegrees
                || (center.Semantic & WorldSurfaceSemantic.SiteReservation) != 0)
            {
                error = null;
                return false;
            }

            var horizontalLength = Math.Sqrt(
                center.NormalA * center.NormalA + center.NormalB * center.NormalB);
            if (horizontalLength <= 1e-6d)
            {
                error = null;
                return false;
            }

            // A heightfield's horizontal upward normal points toward decreasing height.
            var outwardA = center.NormalA / horizontalLength;
            var outwardB = center.NormalB / horizontalLength;
            var rim = center;
            var toe = center;
            var steepSamples = 1;
            var releaseSlope = profile.MinimumSlopeDegrees * 0.6f;
            for (var direction = -1; direction <= 1; direction += 2)
            {
                for (var step = 1; step <= profile.MaximumTraceSteps; step++)
                {
                    var distance = direction * step * profile.TraceStep;
                    var point = new AbsoluteWorldPosition(
                        centerA + outwardA * distance,
                        0d,
                        centerB + outwardB * distance);
                    if (!query.TrySampleSurface(point, out var surface, out error)) return false;
                    if (direction < 0) rim = surface;
                    else toe = surface;
                    var slope = SlopeDegrees(surface.NormalVertical);
                    if (slope >= profile.MinimumSlopeDegrees) steepSamples++;
                    if (step >= 2 && slope < releaseSlope) break;
                }
            }

            if (steepSamples < profile.MinimumSteepSamples
                || rim.Position.Vertical - toe.Position.Vertical < profile.MinimumDrop)
            {
                error = null;
                return false;
            }

            if ((rim.Semantic & WorldSurfaceSemantic.SiteReservation) != 0
                || (toe.Semantic & WorldSurfaceSemantic.SiteReservation) != 0)
            {
                error = null;
                return false;
            }

            // This is only the canonical route screen. Spawn and authored formation
            // reservations are required before a candidate becomes production geometry.
            if (!TryRouteClear(center.Position, out var centerClear, out error)
                || !TryRouteClear(rim.Position, out var rimClear, out error)
                || !TryRouteClear(toe.Position, out var toeClear, out error)) return false;
            if (!centerClear || !rimClear || !toeClear)
            {
                error = null;
                return false;
            }

            var address = coordinateModel.Encode(centerPoint);
            var parentFeatureId = (center.Semantic & (WorldSurfaceSemantic.CanyonFloor
                | WorldSurfaceSemantic.CanyonShelf | WorldSurfaceSemantic.CanyonWall
                | WorldSurfaceSemantic.BoundedLandform)) != 0
                ? center.DominantFeatureId
                : WorldFeatureId.Empty;
            var key = cellA.ToString(CultureInfo.InvariantCulture) + ":"
                + cellB.ToString(CultureInfo.InvariantCulture) + ":"
                + parentFeatureId;
            var id = WorldFeatureId.Create(world, SectionNamespace, address, key);
            candidate = new WorldCliffSectionCandidate(
                id, parentFeatureId, cellA, cellB,
                center.Position, rim.Position, toe.Position,
                outwardA, outwardB, SlopeDegrees(center.NormalVertical), center.StrataFamilyId,
                profile.DebrisRunout, profile.DebrisHalfWidth);
            error = null;
            return true;
        }

        public bool TryBuildWindow(
            long minimumCellA,
            long maximumCellA,
            long minimumCellB,
            long maximumCellB,
            out IReadOnlyList<WorldCliffSectionCandidate> candidates,
            out string error)
        {
            if (minimumCellA > maximumCellA || minimumCellB > maximumCellB
                || ((double)maximumCellA - minimumCellA + 1d)
                    * ((double)maximumCellB - minimumCellB + 1d) > 4096d)
                throw new ArgumentOutOfRangeException(nameof(maximumCellA), "Cliff study windows must contain at most 4096 cells.");
            var results = new List<WorldCliffSectionCandidate>();
            for (var b = minimumCellB; ; b++)
            {
                for (var a = minimumCellA; ; a++)
                {
                    if (!TryBuild(a, b, out var candidate, out error))
                    {
                        if (error != null)
                        {
                            candidates = null;
                            return false;
                        }
                        if (a == maximumCellA) break;
                        continue;
                    }
                    results.Add(candidate);
                    if (a == maximumCellA) break;
                }
                if (b == maximumCellB) break;
            }
            results.Sort((left, right) => left.Id.CompareTo(right.Id));
            candidates = results.AsReadOnly();
            error = null;
            return true;
        }

        private bool TryRouteClear(AbsoluteWorldPosition position, out bool clear, out string error)
        {
            if (!query.TrySampleAffordance(position, WorldAgentProfile.BooterProof, out var booter, out error)
                || !query.TrySampleAffordance(position, WorldAgentProfile.BigArmProof, out var bigArm, out error))
            {
                clear = false;
                return false;
            }
            clear = !booter.ReservedRoute && !bigArm.ReservedRoute;
            return true;
        }

        private static float SlopeDegrees(float normalVertical)
        {
            return (float)(Math.Acos(Math.Max(-1d, Math.Min(1d, normalVertical))) * 180d / Math.PI);
        }
    }
}
