using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// Pure, dormant Batch 4 adapter. Production terrain remains authoritative until the atomic cutover batch.
    /// </summary>
    public sealed class DormantHybridWorldQueryService : IWorldQueryService
    {
        private readonly HybridTerrainPlan plan;
        private readonly IWorldCoordinateModel coordinateModel;
        private readonly IWorldCoordinateContextProvider contextProvider;

        public DormantHybridWorldQueryService(
            HybridTerrainPlan plan,
            IWorldCoordinateModel coordinateModel,
            IWorldCoordinateContextProvider contextProvider)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            this.coordinateModel = coordinateModel ?? throw new ArgumentNullException(nameof(coordinateModel));
            this.contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
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
            for (var i = 0; i < plan.CanyonSegments.Count; i++)
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
            semantic = WorldSurfaceSemantic.BroadGround;
            featureId = context.ContextId;

            var nearestCanyonDistance = double.MaxValue;
            var strongestCanyonCut = 0d;
            for (var i = 0; i < plan.CanyonSegments.Count; i++)
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

        private static double Lerp(float from, float to, double amount) => from + (to - from) * amount;

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
}
