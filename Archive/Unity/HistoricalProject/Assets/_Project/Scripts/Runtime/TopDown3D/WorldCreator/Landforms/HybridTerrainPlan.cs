using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public enum LandformFeatureKind : byte
    {
        Buttress = 1,
        OverhangRequest = 2,
        Scarp = 3
    }

    public readonly struct LandformFeatureRequest : IEquatable<LandformFeatureRequest>
    {
        public LandformFeatureRequest(
            WorldFeatureId id,
            WorldFeatureId parentFeatureId,
            LandformFeatureKind kind,
            AbsoluteWorldPosition center,
            float radius,
            float height)
        {
            if (id.IsEmpty || parentFeatureId.IsEmpty)
            {
                throw new ArgumentException("Landform requests require stable child and parent identities.");
            }

            Id = id;
            ParentFeatureId = parentFeatureId;
            Kind = kind;
            Center = center;
            Radius = SiteIntentReservation.RequirePositive(radius, nameof(radius));
            Height = SiteIntentReservation.RequirePositive(height, nameof(height));
        }

        public WorldFeatureId Id { get; }
        public WorldFeatureId ParentFeatureId { get; }
        public LandformFeatureKind Kind { get; }
        public AbsoluteWorldPosition Center { get; }
        public float Radius { get; }
        public float Height { get; }

        public bool Equals(LandformFeatureRequest other)
        {
            return Id.Equals(other.Id)
                && ParentFeatureId.Equals(other.ParentFeatureId)
                && Kind == other.Kind
                && Center.Equals(other.Center)
                && Radius.Equals(other.Radius)
                && Height.Equals(other.Height);
        }

        public override bool Equals(object obj) => obj is LandformFeatureRequest other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
    }

    public readonly struct CompiledCanyonSegment
    {
        public CompiledCanyonSegment(
            CanyonSegmentPlan source,
            CanyonNodePlan from,
            CanyonNodePlan to)
        {
            Source = source;
            From = from;
            To = to;
        }

        public CanyonSegmentPlan Source { get; }
        public CanyonNodePlan From { get; }
        public CanyonNodePlan To { get; }
    }

    /// <summary>
    /// Immutable canonical data for the dormant Batch 4 query adapter. It owns no scene objects,
    /// chunks, or runtime deltas and can be discarded/rebuilt from absolute plans in any order.
    /// </summary>
    public sealed class HybridTerrainPlan
    {
        private readonly IReadOnlyList<CompiledCanyonSegment> canyonSegments;
        private readonly IReadOnlyList<LandformFeatureRequest> landformRequests;
        private readonly IReadOnlyList<WorldHistoryPlan> histories;

        internal HybridTerrainPlan(
            WorldIdentity world,
            IEnumerable<CompiledCanyonSegment> canyonSegments,
            IEnumerable<LandformFeatureRequest> landformRequests,
            IEnumerable<WorldHistoryPlan> histories,
            WorldFeatureId fingerprint)
        {
            World = world;
            this.canyonSegments = SortUnique(canyonSegments, item => item.Source.Id, nameof(canyonSegments));
            this.landformRequests = SortUnique(landformRequests, item => item.Id, nameof(landformRequests));
            this.histories = SortUnique(histories, item => item.Id, nameof(histories));
            if (fingerprint.IsEmpty) throw new ArgumentException("Terrain plans require a fingerprint.", nameof(fingerprint));
            Fingerprint = fingerprint;
        }

        public WorldIdentity World { get; }
        public IReadOnlyList<CompiledCanyonSegment> CanyonSegments => canyonSegments;
        public IReadOnlyList<LandformFeatureRequest> LandformRequests => landformRequests;
        public IReadOnlyList<WorldHistoryPlan> Histories => histories;
        public WorldFeatureId Fingerprint { get; }

        private static IReadOnlyList<T> SortUnique<T>(IEnumerable<T> source, Func<T, WorldFeatureId> getId, string name)
        {
            if (source == null) throw new ArgumentNullException(name);
            var result = new List<T>(source);
            result.Sort((left, right) => getId(left).CompareTo(getId(right)));
            for (var i = 1; i < result.Count; i++)
            {
                if (getId(result[i]).Equals(getId(result[i - 1])))
                {
                    throw new ArgumentException($"'{name}' contains duplicate stable identities.", name);
                }
            }

            return result.AsReadOnly();
        }
    }

    public static class HybridTerrainCompiler
    {
        private static readonly WorldSeedNamespace LandformNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.hybrid-terrain");
        private static readonly WorldSeedNamespace SiteNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Site, "site.synthetic-history-proof");

        public static HybridTerrainPlan Compile(
            WorldIdentity world,
            IEnumerable<CanyonSystemPlan> canyonPlans,
            IEnumerable<WorldHistoryPlan> histories = null)
        {
            if (canyonPlans == null) throw new ArgumentNullException(nameof(canyonPlans));
            var planList = new List<CanyonSystemPlan>(canyonPlans);
            planList.Sort((left, right) => left.Header.Key.CompareTo(right.Header.Key));
            if (planList.Count == 0) throw new ArgumentException("At least one canyon plan is required.", nameof(canyonPlans));

            var nodeById = new Dictionary<WorldFeatureId, CanyonNodePlan>();
            for (var i = 0; i < planList.Count; i++)
            {
                for (var j = 0; j < planList[i].Nodes.Count; j++)
                {
                    var node = planList[i].Nodes[j];
                    if (nodeById.TryGetValue(node.Id, out var prior) && !prior.Equals(node))
                    {
                        throw new InvalidOperationException("Canyon plans disagree on a shared node.");
                    }

                    nodeById[node.Id] = node;
                }
            }

            var segments = new Dictionary<WorldFeatureId, CompiledCanyonSegment>();
            var features = new Dictionary<WorldFeatureId, LandformFeatureRequest>();
            for (var i = 0; i < planList.Count; i++)
            {
                var plan = planList[i];
                for (var j = 0; j < plan.Segments.Count; j++)
                {
                    var segment = plan.Segments[j];
                    if (!nodeById.TryGetValue(segment.FromNodeId, out var from)
                        || !nodeById.TryGetValue(segment.ToNodeId, out var to))
                    {
                        throw new InvalidOperationException("A canyon segment cannot resolve both endpoint nodes.");
                    }

                    segments[segment.Id] = new CompiledCanyonSegment(segment, from, to);
                    if (segment.Hierarchy == CanyonHierarchyTier.MinorBranch) continue;

                    var center = Lerp(from.Position, to.Position, 0.5d);
                    var requestId = WorldFeatureId.Create(
                        world,
                        LandformNamespace,
                        plan.Header.Key.OwnerAddress,
                        "bounded-request:" + segment.Id);
                    var kind = segment.Hierarchy == CanyonHierarchyTier.MainSpine
                        ? LandformFeatureKind.Buttress
                        : LandformFeatureKind.Scarp;
                    features[requestId] = new LandformFeatureRequest(
                        requestId,
                        segment.Id,
                        kind,
                        center,
                        Math.Max(4f, Math.Min(from.CrossSection.Width, to.CrossSection.Width) * 0.18f),
                        Math.Max(3f, Math.Min(from.CrossSection.Depth, to.CrossSection.Depth) * 0.24f));
                }
            }

            var historyList = histories == null ? new List<WorldHistoryPlan>() : new List<WorldHistoryPlan>(histories);
            var hash = new WorldStableHashBuilder("hybrid-terrain-plan-v1");
            hash.Append(world.Seed);
            hash.Append(world.Versions.Topology);
            hash.Append(world.Versions.Landform);
            var segmentIds = new List<WorldFeatureId>(segments.Keys);
            segmentIds.Sort();
            for (var i = 0; i < segmentIds.Count; i++) hash.Append(segmentIds[i].ToString());
            var featureIds = new List<WorldFeatureId>(features.Keys);
            featureIds.Sort();
            for (var i = 0; i < featureIds.Count; i++) hash.Append(featureIds[i].ToString());
            historyList.Sort((left, right) => left.Id.CompareTo(right.Id));
            for (var i = 0; i < historyList.Count; i++) hash.Append(historyList[i].Id.ToString());
            hash.Finish128(out var high, out var low);
            return new HybridTerrainPlan(world, segments.Values, features.Values, historyList, new WorldFeatureId(high, low));
        }

        public static WorldHistoryPlan CreateNonCanonSyntheticHistoryFixture(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            AbsoluteWorldPosition center)
        {
            if (coordinateModel == null) throw new ArgumentNullException(nameof(coordinateModel));
            var address = coordinateModel.Encode(center);
            var reservationId = WorldFeatureId.Create(world, SiteNamespace, address, "reservation");
            var reservation = new SiteIntentReservation(
                reservationId,
                center,
                54f,
                new AbsoluteWorldPosition(center.HorizontalA - 42d, center.Vertical, center.HorizontalB),
                8f,
                12f,
                true);
            var operations = new[]
            {
                CreateOperation(world, address, reservationId, 1, WorldHistoryPhase.ConstructionAndOccupation, BoundedTerrainOperationKind.ReserveApproach, new AbsoluteWorldPosition(center.HorizontalA - 20d, center.Vertical, center.HorizontalB), 12f, -1.5f),
                CreateOperation(world, address, reservationId, 2, WorldHistoryPhase.ConstructionAndOccupation, BoundedTerrainOperationKind.LevelFoundation, center, 20f, 2f),
                CreateOperation(world, address, reservationId, 3, WorldHistoryPhase.Destruction, BoundedTerrainOperationKind.DestructionCut, new AbsoluteWorldPosition(center.HorizontalA + 4d, center.Vertical, center.HorizontalB + 2d), 10f, -6f),
                CreateOperation(world, address, reservationId, 4, WorldHistoryPhase.BurialAndWeathering, BoundedTerrainOperationKind.BurialDeposit, new AbsoluteWorldPosition(center.HorizontalA - 3d, center.Vertical, center.HorizontalB - 2d), 14f, 3f),
                CreateOperation(world, address, reservationId, 5, WorldHistoryPhase.BurialAndWeathering, BoundedTerrainOperationKind.Weathering, center, 18f, -0.8f)
            };
            var historyId = WorldFeatureId.Create(world, SiteNamespace, address, "history");
            return new WorldHistoryPlan(
                historyId,
                reservation,
                new[]
                {
                    WorldHistoryPhase.GeologicFormation,
                    WorldHistoryPhase.ConstructionAndOccupation,
                    WorldHistoryPhase.Destruction,
                    WorldHistoryPhase.BurialAndWeathering,
                    WorldHistoryPhase.PresentState
                },
                operations);
        }

        private static BoundedTerrainOperation CreateOperation(
            WorldIdentity world,
            WorldCoordinateAddress address,
            WorldFeatureId reservationId,
            int index,
            WorldHistoryPhase phase,
            BoundedTerrainOperationKind kind,
            AbsoluteWorldPosition center,
            float radius,
            float change)
        {
            var id = WorldFeatureId.Create(world, SiteNamespace, address, "operation:" + index);
            return new BoundedTerrainOperation(id, reservationId, phase, kind, center, radius, change);
        }

        private static AbsoluteWorldPosition Lerp(AbsoluteWorldPosition from, AbsoluteWorldPosition to, double amount)
        {
            return new AbsoluteWorldPosition(
                from.HorizontalA + (to.HorizontalA - from.HorizontalA) * amount,
                from.Vertical + (to.Vertical - from.Vertical) * amount,
                from.HorizontalB + (to.HorizontalB - from.HorizontalB) * amount);
        }
    }
}
