using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// Pure query adapter over one immutable compiled terrain window. It owns no scene or
    /// streaming authority; callers decide whether the window is dormant proof or production data.
    /// </summary>
    public class HybridTerrainWindowQueryService : IWorldQueryService
    {
        private readonly HybridTerrainPlan plan;
        private readonly IWorldCoordinateModel coordinateModel;
        private readonly IWorldCoordinateContextProvider contextProvider;
        private readonly bool includeCanyonExcavation;
        private readonly HashSet<WorldFeatureId> canyonSourceIds;

        public HybridTerrainWindowQueryService(
            HybridTerrainPlan plan,
            IWorldCoordinateModel coordinateModel,
            IWorldCoordinateContextProvider contextProvider,
            bool includeCanyonExcavation = true)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            this.coordinateModel = coordinateModel ?? throw new ArgumentNullException(nameof(coordinateModel));
            this.contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            this.includeCanyonExcavation = includeCanyonExcavation;
            if (!includeCanyonExcavation)
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
            if (!TryRawHeight(position.HorizontalA, position.HorizontalB, out var height, out var semantic, out var featureId, out var context, out error))
            {
                sample = default;
                return false;
            }

            const double epsilon = 0.5d;
            if (!TryRawHeight(position.HorizontalA + epsilon, position.HorizontalB, out var right, out _, out _, out _, out error)
                || !TryRawHeight(position.HorizontalA - epsilon, position.HorizontalB, out var left, out _, out _, out _, out error)
                || !TryRawHeight(position.HorizontalA, position.HorizontalB + epsilon, out var forward, out _, out _, out _, out error)
                || !TryRawHeight(position.HorizontalA, position.HorizontalB - epsilon, out var back, out _, out _, out _, out error))
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
            for (var i = 0; i < plan.LandformRequests.Count; i++)
            {
                var request = plan.LandformRequests[i];
                if (!includeCanyonExcavation && IsCanyonDerived(request))
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

            var slope = (float)(Math.Acos(Math.Max(-1d, Math.Min(1d, surface.NormalVertical))) * 180d / Math.PI);
            var reserved = false;
            var routeId = WorldFeatureId.Empty;
            for (var i = 0; includeCanyonExcavation && i < plan.CanyonSegments.Count; i++)
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
            out string error)
        {
            var queryPosition = new AbsoluteWorldPosition(horizontalA, 0d, horizontalB);
            var address = coordinateModel.Encode(queryPosition);
            if (!contextProvider.TrySample(plan.World, coordinateModel, address, out context, out error))
            {
                height = default;
                semantic = default;
                featureId = default;
                return false;
            }

            var landscape = context.Landscape;
            var seedPhase = (plan.World.Seed & 0xffffL) * 0.00013d;
            var directionalA = Math.Cos(landscape.StructuralDirection * Math.PI * 2d);
            var directionalB = Math.Sin(landscape.StructuralDirection * Math.PI * 2d);
            var projected = horizontalA * directionalA + horizontalB * directionalB;
            var cross = -horizontalA * directionalB + horizontalB * directionalA;
            height = landscape.BaseElevationBias * 46d
                + Math.Sin(projected * 0.006d + seedPhase) * (8d + landscape.Relief * 28d)
                + Math.Sin(cross * 0.0027d - seedPhase * 0.7d) * (4d + landscape.Relief * 13d)
                + Math.Sin((horizontalA + horizontalB) * 0.0011d + seedPhase * 1.7d) * 7d;
            if (!includeCanyonExcavation)
            {
                height += SampleInitialPlayableAreaRelief(
                    horizontalA,
                    horizontalB,
                    landscape.Relief,
                    plan.World.Seed);
            }

            semantic = WorldSurfaceSemantic.BroadGround;
            featureId = context.ContextId;

            var nearestCanyonDistance = double.MaxValue;
            var strongestCanyonCut = 0d;
            for (var i = 0; includeCanyonExcavation && i < plan.CanyonSegments.Count; i++)
            {
                var segment = plan.CanyonSegments[i];
                var distance = DistanceToSegment(queryPosition, segment.From.Position, segment.To.Position, out var amount);
                var width = Lerp(segment.From.CrossSection.Width, segment.To.CrossSection.Width, amount);
                var depth = Lerp(segment.From.CrossSection.Depth, segment.To.CrossSection.Depth, amount);
                var normalized = distance / Math.Max(1d, width * 0.5d);
                if (normalized >= 1d) continue;
                var smooth = 1d - normalized * normalized * (3d - 2d * normalized);
                strongestCanyonCut = Math.Max(strongestCanyonCut, depth * smooth);
                if (distance < nearestCanyonDistance)
                {
                    nearestCanyonDistance = distance;
                    featureId = segment.Source.Id;
                    semantic |= normalized < 0.34d
                        ? WorldSurfaceSemantic.CanyonFloor
                        : normalized < 0.72d
                            ? WorldSurfaceSemantic.CanyonShelf
                            : WorldSurfaceSemantic.CanyonWall;
                }
            }

            // Confluences merge into one excavated landform. Summing every overlapping segment
            // would make graph density, rather than geology, author unbounded depth.
            height -= strongestCanyonCut;

            for (var i = 0; i < plan.LandformRequests.Count; i++)
            {
                var request = plan.LandformRequests[i];
                if (!includeCanyonExcavation && IsCanyonDerived(request))
                {
                    continue;
                }

                var distance = WorldHistoryPlan.HorizontalDistance(queryPosition, request.Center);
                if (distance >= request.Radius) continue;
                var amount = 1d - distance / request.Radius;
                height += request.Height * amount * amount;
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

        private static double SampleInitialPlayableAreaRelief(
            double horizontalA,
            double horizontalB,
            float relief,
            long worldSeed)
        {
            var seed = unchecked((ulong)worldSeed);
            var warpA = SampleValueNoise(horizontalA, horizontalB, 180d, seed ^ 0x9e3779b97f4a7c15UL) * 24d;
            var warpB = SampleValueNoise(horizontalA, horizontalB, 180d, seed ^ 0xd1b54a32d192ed03UL) * 24d;
            var rollingRelief = SampleValueNoise(
                horizontalA + warpA,
                horizontalB + warpB,
                72d,
                seed ^ 0x94d049bb133111ebUL) * (3d + relief * 7d);
            var pitSignal = SampleValueNoise(
                horizontalA - warpB * 0.35d,
                horizontalB + warpA * 0.35d,
                46d,
                seed ^ 0xbf58476d1ce4e5b9UL);
            var pitAmount = Smooth01((-pitSignal - 0.25d) / 0.55d);
            var pitDepth = pitAmount * pitAmount * (1.5d + relief * 5d);
            return rollingRelief - pitDepth;
        }

        private static double SampleValueNoise(
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
