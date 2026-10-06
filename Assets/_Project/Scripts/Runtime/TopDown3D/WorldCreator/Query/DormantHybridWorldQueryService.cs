using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// Pure query adapter over one immutable compiled terrain window. It owns no scene or
    /// streaming authority; callers decide whether the window is dormant proof or production data.
    /// </summary>
    public class HybridTerrainWindowQueryService : IWorldQueryService, IWorldCoarseSurfaceQuery
    {
        private const double FlatRegionCellSpan = 256d;
        private const int MaximumCachedFlatHeights = 128;
        private readonly object flatHeightGate = new object();
        private readonly Dictionary<(long, long), double> flatHeights =
            new Dictionary<(long, long), double>();
        private readonly HybridTerrainPlan plan;
        private readonly IWorldCoordinateModel coordinateModel;
        private readonly IWorldCoordinateContextProvider contextProvider;
        private readonly bool includeCanyonExcavation;
        private readonly double gentleCenterA;
        private readonly double gentleCenterB;
        private readonly double gentleRadius;
        private readonly double gentleTransition;
        private readonly HashSet<WorldFeatureId> canyonSourceIds;

        public HybridTerrainWindowQueryService(
            HybridTerrainPlan plan,
            IWorldCoordinateModel coordinateModel,
            IWorldCoordinateContextProvider contextProvider,
            bool includeCanyonExcavation = true,
            double gentleCenterA = double.NaN,
            double gentleCenterB = double.NaN,
            double gentleRadius = 0d,
            double gentleTransition = 0d)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            this.coordinateModel = coordinateModel ?? throw new ArgumentNullException(nameof(coordinateModel));
            this.contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            this.includeCanyonExcavation = includeCanyonExcavation;
            this.gentleCenterA = gentleCenterA;
            this.gentleCenterB = gentleCenterB;
            this.gentleRadius = gentleRadius;
            this.gentleTransition = gentleTransition;
            if (!includeCanyonExcavation || gentleRadius > 0d)
            {
                canyonSourceIds = new HashSet<WorldFeatureId>();
                for (var i = 0; i < plan.CanyonSegments.Count; i++)
                {
                    canyonSourceIds.Add(plan.CanyonSegments[i].Source.Id);
                }
            }
        }

        public bool TrySampleSurface(AbsoluteWorldPosition position, out WorldSurfaceSample sample, out string error)
        {
            return TrySampleSurface(position, true, out sample, out error);
        }

        public bool TrySampleCoarseSurface(AbsoluteWorldPosition position, out WorldSurfaceSample sample, out string error)
        {
            return TrySampleSurface(position, false, out sample, out error);
        }

        private bool TrySampleSurface(
            AbsoluteWorldPosition position,
            bool includeNearDetail,
            out WorldSurfaceSample sample,
            out string error)
        {
            if (!TryRawHeight(position.HorizontalA, position.HorizontalB, out var height, out var semantic, out var featureId, out var context, out error, includeNearDetail))
            {
                sample = default;
                return false;
            }

            const double epsilon = 0.5d;
            if (!TryRawHeight(position.HorizontalA + epsilon, position.HorizontalB, out var right, out _, out _, out _, out error, includeNearDetail, false)
                || !TryRawHeight(position.HorizontalA - epsilon, position.HorizontalB, out var left, out _, out _, out _, out error, includeNearDetail, false)
                || !TryRawHeight(position.HorizontalA, position.HorizontalB + epsilon, out var forward, out _, out _, out _, out error, includeNearDetail, false)
                || !TryRawHeight(position.HorizontalA, position.HorizontalB - epsilon, out var back, out _, out _, out _, out error, includeNearDetail, false))
            {
                sample = default;
                return false;
            }

            var normalA = (float)(left - right);
            var normalVertical = (float)(2d * epsilon);
            var normalB = (float)(back - forward);
            Normalize(ref normalA, ref normalVertical, ref normalB);
            sample = new WorldSurfaceSample(
                new AbsoluteWorldPosition(position.HorizontalA, height, position.HorizontalB),
                normalA,
                normalVertical,
                normalB,
                semantic,
                featureId,
                context.DominantProvinceId,
                context.DominantStrataFamilyId);
            error = null;
            return true;
        }

        public bool TrySampleVolume(AbsoluteWorldPosition position, out WorldVolumeSample sample, out string error)
        {
            if (!TryRawHeight(position.HorizontalA, position.HorizontalB, out var height, out _, out var featureId, out _, out error))
            {
                sample = default;
                return false;
            }

            var signedDistance = (float)(position.Vertical - height);
            var canyonWeight = CanyonWeight(position.HorizontalA, position.HorizontalB);
            for (var i = 0; i < plan.LandformRequests.Count; i++)
            {
                var request = plan.LandformRequests[i];
                if (canyonWeight < 1d && IsCanyonDerived(request))
                {
                    continue;
                }

                var horizontalDistance = WorldHistoryPlan.HorizontalDistance(position, request.Center);
                var radial = horizontalDistance - request.Radius;
                var verticalCenter = request.Center.Vertical + request.Height * 0.5d;
                var vertical = Math.Abs(position.Vertical - verticalCenter) - request.Height * 0.5d;
                var boundedDistance = (float)Math.Max(radial, vertical);
                if (boundedDistance < signedDistance)
                {
                    signedDistance = boundedDistance;
                    featureId = request.Id;
                }
            }

            sample = new WorldVolumeSample(signedDistance, signedDistance <= 0f, featureId);
            error = null;
            return true;
        }

        public bool TrySampleAffordance(
            AbsoluteWorldPosition position,
            WorldAgentProfile agent,
            out WorldAffordanceSample sample,
            out string error)
        {
            if (!TrySampleSurface(position, out var surface, out error))
            {
                sample = default;
                return false;
            }

            return TrySampleAffordance(position, surface, agent, out sample, out error);
        }

        // Placement already samples the surface at each contact. Reuse that exact sample
        // so a formation does not repeat five terrain queries for every mesh point.
        public bool TrySampleAffordance(
            AbsoluteWorldPosition position,
            WorldSurfaceSample surface,
            WorldAgentProfile agent,
            out WorldAffordanceSample sample,
            out string error)
        {

            var slope = (float)(Math.Acos(Math.Max(-1d, Math.Min(1d, surface.NormalVertical))) * 180d / Math.PI);
            var reserved = false;
            var routeId = WorldFeatureId.Empty;
            var canyonWeight = CanyonWeight(position.HorizontalA, position.HorizontalB);
            for (var i = 0; canyonWeight > 0d && i < plan.CanyonSegments.Count; i++)
            {
                var segment = plan.CanyonSegments[i];
                var distance = DistanceToSegment(position, segment.From.Position, segment.To.Position, out _);
                var supportsAgent = agent.Kind == WorldAgentKind.Booter || segment.Source.Traversal.SupportsBigArm;
                var clearanceWidth = segment.Source.Traversal.MinimumClearWidth;
                var clearanceHeight = segment.Source.Traversal.MinimumClearHeight;
                if (supportsAgent && agent.Radius * 2f <= clearanceWidth && agent.Height <= clearanceHeight
                    && distance <= clearanceWidth * 0.5d)
                {
                    reserved = true;
                    routeId = segment.Source.Id;
                    break;
                }
            }

            sample = new WorldAffordanceSample(slope <= agent.MaximumSlopeDegrees || reserved, reserved, slope, routeId);
            error = null;
            return true;
        }

        private bool TryRawHeight(
            double horizontalA,
            double horizontalB,
            out double height,
            out WorldSurfaceSemantic semantic,
            out WorldFeatureId featureId,
            out WorldCoordinateContext context,
            out string error,
            bool includeNearDetail = true,
            bool needContextIdentity = true)
        {
            var queryPosition = new AbsoluteWorldPosition(horizontalA, 0d, horizontalB);
            WorldLandscapeParameters landscape;
            WorldFeatureId contextId;
            bool sampledContext;
            if (!needContextIdentity && contextProvider is WorldCoordinateContextSampler sampler
                && coordinateModel is NonCanonTechnicalCoordinateModel)
            {
                context = null;
                contextId = WorldFeatureId.Empty;
                sampledContext = sampler.TrySampleLandscape(queryPosition, out landscape, out error);
            }
            else
            {
                var address = coordinateModel.Encode(queryPosition);
                sampledContext = contextProvider.TrySample(plan.World, coordinateModel,
                    address, out context, out error);
                landscape = sampledContext ? context.Landscape : default;
                contextId = sampledContext ? context.ContextId : WorldFeatureId.Empty;
            }
            if (!sampledContext)
            {
                height = default;
                semantic = default;
                featureId = default;
                return false;
            }

            var canyonWeight = CanyonWeight(horizontalA, horizontalB);
            if (canyonWeight < 1d
                && TrySampleFlatRegion(horizontalA, horizontalB, plan.World.Seed,
                    out var flatCenterA, out var flatCenterB, out var flatWeight))
            {
                if (!TryGetFlatHeight(flatCenterA, flatCenterB, out var flatHeight, out error))
                {
                    height = default;
                    semantic = default;
                    featureId = default;
                    return false;
                }

                // One center height makes the core exactly level in both detailed and coarse queries.
                height = flatWeight >= 1d
                    ? flatHeight
                    : Lerp(SampleBaseHeight(horizontalA, horizontalB, landscape,
                        plan.World.Seed, false, includeNearDetail), flatHeight, flatWeight);
            }
            else
            {
                height = SampleBaseHeight(horizontalA, horizontalB, landscape,
                    plan.World.Seed, canyonWeight >= 1d, includeNearDetail);
            }
            if (canyonWeight > 0d && canyonWeight < 1d)
                height = Lerp(height, SampleBaseHeight(horizontalA, horizontalB,
                    landscape, plan.World.Seed, true, includeNearDetail), canyonWeight);

            semantic = WorldSurfaceSemantic.BroadGround;
            featureId = contextId;

            var nearestCanyonDistance = double.MaxValue;
            var strongestCanyonCut = 0d;
            var nearestCanyonSemantic = WorldSurfaceSemantic.None;
            for (var i = 0; canyonWeight > 0d && i < plan.CanyonSegments.Count; i++)
            {
                var segment = plan.CanyonSegments[i];
                var distance = DistanceToSegment(queryPosition, segment.From.Position, segment.To.Position, out var amount);
                var width = Lerp(segment.From.CrossSection.Width, segment.To.CrossSection.Width, amount);
                var depth = Lerp(segment.From.CrossSection.Depth, segment.To.CrossSection.Depth, amount);
                var normalized = distance / Math.Max(1d, width * 0.5d);
                if (normalized >= 1d) continue;
                var lower = Lerp(segment.From.CrossSection.Shelves.Lower,
                    segment.To.CrossSection.Shelves.Lower, amount);
                var middle = Lerp(segment.From.CrossSection.Shelves.Middle,
                    segment.To.CrossSection.Shelves.Middle, amount);
                if (width >= 150d)
                {
                    lower += 0.20d;
                    middle += 0.10d;
                }
                var upper = Lerp(segment.From.CrossSection.Shelves.Upper,
                    segment.To.CrossSection.Shelves.Upper, amount);
                var cut = depth * (
                    0.22d * (1d - SmoothBand(normalized, lower - 0.065d, lower + 0.065d))
                    + 0.34d * (1d - SmoothBand(normalized, middle - 0.075d, middle + 0.075d))
                    + 0.28d * (1d - SmoothBand(normalized, upper - 0.075d, upper + 0.075d))
                    + 0.16d * (1d - SmoothBand(normalized, 0.94d, 1d)));
                var segmentLength = WorldHistoryPlan.HorizontalDistance(
                    segment.From.Position, segment.To.Position);
                var along = amount * segmentLength
                    + segment.From.Position.HorizontalA * 0.713d
                    + segment.From.Position.HorizontalB * 0.419d;
                var across = distance * 0.08d
                    + segment.From.Position.HorizontalB * 0.071d;
                var gullyNoise = SampleSmoothValueNoise(along, across, 24d,
                    unchecked((ulong)plan.World.Seed) ^ 0x66d54ef6a81c41b3UL);
                var gully = SmoothBand(gullyNoise, 0.05d, 0.55d);
                var wallMask = SmoothBand(normalized, 0.32d, 0.48d)
                    * (1d - SmoothBand(normalized, 0.82d, 1d));
                var endpointFade = SmoothBand(amount, 0.05d, 0.15d)
                    * (1d - SmoothBand(amount, 0.85d, 0.95d));
                cut += depth * 0.23d * gully * wallMask * endpointFade;
                var talusApron = depth * 0.06d
                    * SmoothBand(normalized, 0.14d, 0.32d)
                    * (1d - SmoothBand(normalized, 0.34d, 0.48d))
                    * endpointFade;
                cut = Math.Max(0d, cut - talusApron);
                strongestCanyonCut = Math.Max(strongestCanyonCut, cut);
                if (distance < nearestCanyonDistance)
                {
                    nearestCanyonDistance = distance;
                    featureId = segment.Source.Id;
                    nearestCanyonSemantic = normalized < lower
                        ? WorldSurfaceSemantic.CanyonFloor
                        : normalized < upper
                            ? WorldSurfaceSemantic.CanyonShelf
                            : WorldSurfaceSemantic.CanyonWall;
                }
            }
            if (canyonWeight > 0d) semantic |= nearestCanyonSemantic;

            // Confluences merge into one excavated landform. Summing every overlapping segment
            // would make graph density, rather than geology, author unbounded depth.
            height -= strongestCanyonCut * canyonWeight;

            for (var i = 0; i < plan.LandformRequests.Count; i++)
            {
                var request = plan.LandformRequests[i];
                if (canyonWeight <= 0d && IsCanyonDerived(request))
                {
                    continue;
                }

                var distance = WorldHistoryPlan.HorizontalDistance(queryPosition, request.Center);
                if (distance >= request.Radius) continue;
                var amount = 1d - distance / request.Radius;
                height += request.Height * amount * amount
                    * (IsCanyonDerived(request) ? canyonWeight : 1d);
                semantic |= WorldSurfaceSemantic.BoundedLandform;
                featureId = request.Id;
            }

            for (var historyIndex = 0; historyIndex < plan.Histories.Count; historyIndex++)
            {
                var history = plan.Histories[historyIndex];
                var reservationDistance = WorldHistoryPlan.HorizontalDistance(queryPosition, history.Reservation.Center);
                if (reservationDistance <= history.Reservation.FootprintRadius)
                {
                    semantic |= WorldSurfaceSemantic.SiteReservation;
                    featureId = history.Id;
                }

                for (var operationIndex = 0; operationIndex < history.Operations.Count; operationIndex++)
                {
                    var operation = history.Operations[operationIndex];
                    var distance = WorldHistoryPlan.HorizontalDistance(queryPosition, operation.Center);
                    if (distance >= operation.Radius) continue;
                    var amount = 1d - distance / operation.Radius;
                    height += operation.SignedVerticalChange * amount * amount * (3d - 2d * amount);
                    semantic |= SemanticFor(operation.Kind);
                    featureId = operation.Id;
                }
            }

            if (double.IsNaN(height) || double.IsInfinity(height))
            {
                error = "The hybrid terrain query produced a non-finite height.";
                return false;
            }

            error = null;
            return true;
        }

        private bool IsCanyonDerived(LandformFeatureRequest request)
        {
            return canyonSourceIds != null && canyonSourceIds.Contains(request.ParentFeatureId);
        }

        private double CanyonWeight(double horizontalA, double horizontalB)
        {
            if (!includeCanyonExcavation) return 0d;
            if (gentleRadius <= 0d || gentleTransition <= 0d) return 1d;
            var deltaA = horizontalA - gentleCenterA;
            var deltaB = horizontalB - gentleCenterB;
            var distance = Math.Sqrt(deltaA * deltaA + deltaB * deltaB);
            return Smooth01((distance - gentleRadius) / gentleTransition);
        }

        private bool TryGetFlatHeight(double centerA, double centerB, out double height, out string error)
        {
            var key = ((long)Math.Floor(centerA / FlatRegionCellSpan),
                (long)Math.Floor(centerB / FlatRegionCellSpan));
            lock (flatHeightGate)
            {
                if (flatHeights.TryGetValue(key, out height))
                {
                    error = null;
                    return true;
                }
            }

            var center = new AbsoluteWorldPosition(centerA, 0d, centerB);
            if (!contextProvider.TrySample(plan.World, coordinateModel,
                coordinateModel.Encode(center), out var context, out error))
            {
                height = default;
                return false;
            }

            height = SampleBaseHeight(centerA, centerB, context.Landscape, plan.World.Seed, false, false);
            lock (flatHeightGate)
            {
                if (flatHeights.Count >= MaximumCachedFlatHeights) flatHeights.Clear();
                flatHeights[key] = height;
            }

            error = null;
            return true;
        }

        private static double SampleBaseHeight(
            double horizontalA,
            double horizontalB,
            WorldLandscapeParameters landscape,
            long worldSeed,
            bool includeCanyonExcavation,
            bool includeNearDetail)
        {
            var seedPhase = (worldSeed & 0xffffL) * 0.00013d;
            var directionalA = Math.Cos(landscape.StructuralDirection * Math.PI * 2d);
            var directionalB = Math.Sin(landscape.StructuralDirection * Math.PI * 2d);
            var projected = horizontalA * directionalA + horizontalB * directionalB;
            var cross = -horizontalA * directionalB + horizontalB * directionalA;
            var broadRelief =
                Math.Sin(projected * 0.006d + seedPhase) * (8d + landscape.Relief * 28d)
                + Math.Sin(cross * 0.0027d - seedPhase * 0.7d) * (4d + landscape.Relief * 13d)
                + Math.Sin((horizontalA + horizontalB) * 0.0011d + seedPhase * 1.7d) * 7d;
            // The first playable area keeps regional character without broad rolling hills.
            var height = landscape.BaseElevationBias * 46d
                + broadRelief * (includeCanyonExcavation ? 1d : 0.05d);
            if (!includeCanyonExcavation)
            {
                height += SampleInitialPlayableAreaRelief(
                    horizontalA, horizontalB, landscape.Relief, worldSeed, includeNearDetail);
            }

            return height;
        }

        private static bool TrySampleFlatRegion(
            double horizontalA,
            double horizontalB,
            long worldSeed,
            out double centerA,
            out double centerB,
            out double weight)
        {
            var cellA = (long)Math.Floor(horizontalA / FlatRegionCellSpan);
            var cellB = (long)Math.Floor(horizontalB / FlatRegionCellSpan);
            var seed = unchecked((ulong)worldSeed);
            weight = 0d;
            if (HashToSignedUnit(seed ^ 0x510e527fade682d1UL, cellA, cellB) < -0.3d)
            {
                centerA = default;
                centerB = default;
                return false;
            }

            centerA = (cellA + 0.5d) * FlatRegionCellSpan
                + HashToSignedUnit(seed ^ 0x3c6ef372fe94f82bUL, cellA, cellB) * 16d;
            centerB = (cellB + 0.5d) * FlatRegionCellSpan
                + HashToSignedUnit(seed ^ 0xa54ff53a5f1d36f1UL, cellA, cellB) * 16d;

            var deltaA = horizontalA - centerA;
            var deltaB = horizontalB - centerB;
            var distance = Math.Sqrt(deltaA * deltaA + deltaB * deltaB);
            // The 109 m maximum influence ends inside the owner cell even at maximum jitter.
            if (distance >= 109d) return false;

            var coreRadius = Lerp(45d, 75d,
                (HashToSignedUnit(seed ^ 0x9b05688c2b3e6c1fUL, cellA, cellB) + 1d) * 0.5d);
            var transition = Lerp(28d, 30d,
                (HashToSignedUnit(seed ^ 0x1f83d9abfb41bd6bUL, cellA, cellB) + 1d) * 0.5d);
            var unevenEdge = SampleSmoothValueNoise(horizontalA, horizontalB, 32d,
                seed ^ 0x5be0cd19137e2179UL) * 4d;
            weight = 1d - Smooth01((distance + unevenEdge - coreRadius) / transition);
            return weight > 0d;
        }

        private static double SampleInitialPlayableAreaRelief(
            double horizontalA,
            double horizontalB,
            float relief,
            long worldSeed,
            bool includeNearDetail)
        {
            var seed = unchecked((ulong)worldSeed);
            // Sample Big Noise over a wider horizontal footprint while keeping its height range.
            var bigA = horizontalA * 0.8d;
            var bigB = horizontalB * 0.8d;
            // Small faceted offsets break long shelf edges into irregular planar turns.
            var warpA = SampleFacetedNoise(bigA, bigB, 10d, seed ^ 0x9e3779b97f4a7c15UL) * 3d;
            var warpB = SampleFacetedNoise(bigA, bigB, 10d, seed ^ 0xd1b54a32d192ed03UL) * 3d;
            var hillSignal = SampleFacetedNoise(
                bigA + warpA,
                bigB + warpB,
                36d,
                seed ^ 0x94d049bb133111ebUL);
            // Big Noise clamps most of each rise to a shelf and spends the height change in a short ramp.
            // Smooth01 keeps the ramp continuous for normals and adjacent chunk samples.
            var plateauRelief = (Smooth01((hillSignal + 0.12d) / 0.24d) * 2d - 1d)
                * (2d + relief * 2.5d);
            var pitSignal = SampleFacetedNoise(
                bigA - warpB * 0.35d,
                bigB + warpA * 0.35d,
                24d,
                seed ^ 0xbf58476d1ce4e5b9UL);
            var pitAmount = Smooth01((-pitSignal - 0.02d) / 0.2d);
            var pitDepth = pitAmount * (1d + relief * 2.5d);
            // Keep vertical relief independently constrained as the horizontal scale changes.
            var bigNoiseRelief = (plateauRelief - pitDepth) * 0.25d;
            // Little Noise forms compact shelves and divots with softened ramps.
            // The 4.5 m cells remain resolvable by the 0.75 m near-terrain vertex spacing.
            var littleNoiseRelief = includeNearDetail
                ? SampleLittleNoise(horizontalA, horizontalB, seed)
                : 0d;
            // Fine Noise breaks up repeated small shelves without adding another sharp edge.
            var fineNoiseRelief = includeNearDetail
                ? SampleFineNoise(horizontalA, horizontalB, seed)
                : 0d;
            return bigNoiseRelief + littleNoiseRelief + fineNoiseRelief;
        }

        private static double SampleLittleNoise(double horizontalA, double horizontalB, ulong seed)
        {
            var signal = SampleFacetedNoise(
                horizontalA,
                horizontalB,
                4.5d,
                seed ^ 0x6a09e667f3bcc909UL);
            return (Smooth01((signal + 0.35d) / 0.7d) * 2d - 1d) * 0.10d;
        }

        private static double SampleFineNoise(double horizontalA, double horizontalB, ulong seed)
        {
            return SampleSmoothValueNoise(
                horizontalA,
                horizontalB,
                3d,
                seed ^ 0xbb67ae8584caa73bUL) * 0.075d;
        }

        private static double SampleSmoothValueNoise(
            double horizontalA,
            double horizontalB,
            double cellSpan,
            ulong seed)
        {
            var scaledA = horizontalA / cellSpan;
            var scaledB = horizontalB / cellSpan;
            var cellA = (long)Math.Floor(scaledA);
            var cellB = (long)Math.Floor(scaledB);
            var amountA = Smooth01(scaledA - cellA);
            var amountB = Smooth01(scaledB - cellB);
            var lower = Lerp(
                HashToSignedUnit(seed, cellA, cellB),
                HashToSignedUnit(seed, cellA + 1L, cellB),
                amountA);
            var upper = Lerp(
                HashToSignedUnit(seed, cellA, cellB + 1L),
                HashToSignedUnit(seed, cellA + 1L, cellB + 1L),
                amountA);
            return Lerp(lower, upper, amountB);
        }

        private static double SampleFacetedNoise(
            double horizontalA,
            double horizontalB,
            double cellSpan,
            ulong seed)
        {
            var scaledA = horizontalA / cellSpan;
            var scaledB = horizontalB / cellSpan;
            var cellA = (long)Math.Floor(scaledA);
            var cellB = (long)Math.Floor(scaledB);
            var amountA = scaledA - cellA;
            var amountB = scaledB - cellB;
            var lowerLeft = HashToSignedUnit(seed, cellA, cellB);
            var lowerRight = HashToSignedUnit(seed, cellA + 1L, cellB);
            var upperLeft = HashToSignedUnit(seed, cellA, cellB + 1L);
            if (amountA + amountB <= 1d)
            {
                return lowerLeft + (lowerRight - lowerLeft) * amountA
                    + (upperLeft - lowerLeft) * amountB;
            }

            var upperRight = HashToSignedUnit(seed, cellA + 1L, cellB + 1L);
            return upperRight + (upperLeft - upperRight) * (1d - amountA)
                + (lowerRight - upperRight) * (1d - amountB);
        }

        private static double HashToSignedUnit(ulong seed, long cellA, long cellB)
        {
            var hash = seed;
            hash ^= unchecked((ulong)cellA) + 0x9e3779b97f4a7c15UL + (hash << 6) + (hash >> 2);
            hash ^= unchecked((ulong)cellB) + 0xd1b54a32d192ed03UL + (hash << 6) + (hash >> 2);
            hash ^= hash >> 30;
            hash *= 0xbf58476d1ce4e5b9UL;
            hash ^= hash >> 27;
            hash *= 0x94d049bb133111ebUL;
            hash ^= hash >> 31;
            var normalized = (hash >> 11) * (1d / 9007199254740992d);
            return normalized * 2d - 1d;
        }

        private static double Smooth01(double value)
        {
            value = Math.Max(0d, Math.Min(1d, value));
            return value * value * (3d - 2d * value);
        }

        private static WorldSurfaceSemantic SemanticFor(BoundedTerrainOperationKind kind)
        {
            return kind switch
            {
                BoundedTerrainOperationKind.ReserveApproach => WorldSurfaceSemantic.Approach,
                BoundedTerrainOperationKind.BurialDeposit => WorldSurfaceSemantic.Buried,
                BoundedTerrainOperationKind.Weathering => WorldSurfaceSemantic.Weathered,
                _ => WorldSurfaceSemantic.Disturbed
            };
        }

        private static double DistanceToSegment(
            AbsoluteWorldPosition point,
            AbsoluteWorldPosition from,
            AbsoluteWorldPosition to,
            out double amount)
        {
            var segmentA = to.HorizontalA - from.HorizontalA;
            var segmentB = to.HorizontalB - from.HorizontalB;
            var lengthSquared = segmentA * segmentA + segmentB * segmentB;
            if (lengthSquared <= double.Epsilon)
            {
                amount = 0d;
                return WorldHistoryPlan.HorizontalDistance(point, from);
            }

            amount = ((point.HorizontalA - from.HorizontalA) * segmentA
                + (point.HorizontalB - from.HorizontalB) * segmentB) / lengthSquared;
            amount = Math.Max(0d, Math.Min(1d, amount));
            var closest = new AbsoluteWorldPosition(
                from.HorizontalA + segmentA * amount,
                0d,
                from.HorizontalB + segmentB * amount);
            return WorldHistoryPlan.HorizontalDistance(point, closest);
        }

        private static double Lerp(double from, double to, double amount) => from + (to - from) * amount;

        private static double SmoothBand(double value, double start, double end)
        {
            var t = Math.Max(0d, Math.Min(1d, (value - start) / (end - start)));
            return t * t * (3d - 2d * t);
        }

        private static void Normalize(ref float a, ref float vertical, ref float b)
        {
            var length = Math.Sqrt(a * a + vertical * vertical + b * b);
            if (length <= double.Epsilon)
            {
                a = 0f;
                vertical = 1f;
                b = 0f;
                return;
            }

            a = (float)(a / length);
            vertical = (float)(vertical / length);
            b = (float)(b / length);
        }
    }

    /// <summary>
    /// Compatibility name retained for Batch 4 proof tools while production authority remains gated.
    /// </summary>
    public sealed class DormantHybridWorldQueryService : HybridTerrainWindowQueryService
    {
        public DormantHybridWorldQueryService(
            HybridTerrainPlan plan,
            IWorldCoordinateModel coordinateModel,
            IWorldCoordinateContextProvider contextProvider)
            : base(plan, coordinateModel, contextProvider)
        {
        }
    }
}
