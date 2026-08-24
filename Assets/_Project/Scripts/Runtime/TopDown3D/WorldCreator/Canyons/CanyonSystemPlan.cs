using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public sealed class CanyonSystemPlan : IWorldPlanRecord
    {
        private readonly IReadOnlyList<CanyonNodePlan> nodes;
        private readonly IReadOnlyList<CanyonSegmentPlan> segments;
        private readonly IReadOnlyList<CanyonBoundaryPortReference> boundaryPorts;
        private readonly IReadOnlyList<CanyonMacroRoutePlan> macroRoutes;

        public CanyonSystemPlan(
            WorldPlanHeader header,
            CanyonSystemCellIndex cell,
            CanyonPlannerProfile profile,
            IEnumerable<CanyonNodePlan> nodes,
            IEnumerable<CanyonSegmentPlan> segments,
            IEnumerable<CanyonBoundaryPortReference> boundaryPorts,
            IEnumerable<CanyonMacroRoutePlan> macroRoutes,
            WorldFeatureId regroupAnchorNodeId,
            CanyonPlanningCost planningCost)
        {
            Header = header;
            Cell = cell;
            Profile = profile;
            this.nodes = SortUnique(nodes, item => item.Id, nameof(nodes));
            this.segments = SortUnique(segments, item => item.Id, nameof(segments));
            this.boundaryPorts = SortUnique(boundaryPorts, item => item.PortNodeId, nameof(boundaryPorts));
            this.macroRoutes = SortUnique(macroRoutes, item => item.Id, nameof(macroRoutes));
            if (regroupAnchorNodeId.IsEmpty)
            {
                throw new ArgumentException("A canyon plan requires a stable regroup anchor.", nameof(regroupAnchorNodeId));
            }

            RegroupAnchorNodeId = regroupAnchorNodeId;
            PlanningCost = planningCost;
        }

        public WorldPlanHeader Header { get; }
        public CanyonSystemCellIndex Cell { get; }
        public CanyonPlannerProfile Profile { get; }
        public IReadOnlyList<CanyonNodePlan> Nodes => nodes;
        public IReadOnlyList<CanyonSegmentPlan> Segments => segments;
        public IReadOnlyList<CanyonBoundaryPortReference> BoundaryPorts => boundaryPorts;
        public IReadOnlyList<CanyonMacroRoutePlan> MacroRoutes => macroRoutes;
        public WorldFeatureId RegroupAnchorNodeId { get; }
        public CanyonPlanningCost PlanningCost { get; }

        public bool TryGetNode(WorldFeatureId id, out CanyonNodePlan node)
        {
            var low = 0;
            var high = nodes.Count - 1;
            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                var comparison = nodes[middle].Id.CompareTo(id);
                if (comparison == 0)
                {
                    node = nodes[middle];
                    return true;
                }

                if (comparison < 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            node = default;
            return false;
        }

        public bool TryGetSegment(WorldFeatureId id, out CanyonSegmentPlan segment)
        {
            var low = 0;
            var high = segments.Count - 1;
            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                var comparison = segments[middle].Id.CompareTo(id);
                if (comparison == 0)
                {
                    segment = segments[middle];
                    return true;
                }

                if (comparison < 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            segment = default;
            return false;
        }

        private static IReadOnlyList<T> SortUnique<T>(
            IEnumerable<T> source,
            Func<T, WorldFeatureId> getId,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var list = new List<T>(source);
            list.Sort((left, right) => getId(left).CompareTo(getId(right)));
            for (var i = 1; i < list.Count; i++)
            {
                if (getId(list[i]).Equals(getId(list[i - 1])))
                {
                    throw new ArgumentException($"'{parameterName}' contains duplicate stable identities.", parameterName);
                }
            }

            return list.AsReadOnly();
        }
    }

    public sealed class CanyonRoutePath
    {
        private readonly IReadOnlyList<WorldFeatureId> nodeIds;
        private readonly IReadOnlyList<WorldFeatureId> segmentIds;

        public CanyonRoutePath(
            IEnumerable<WorldFeatureId> nodeIds,
            IEnumerable<WorldFeatureId> segmentIds,
            float totalCost)
        {
            if (nodeIds == null || segmentIds == null)
            {
                throw new ArgumentNullException(nodeIds == null ? nameof(nodeIds) : nameof(segmentIds));
            }

            var copiedNodes = new List<WorldFeatureId>(nodeIds);
            var copiedSegments = new List<WorldFeatureId>(segmentIds);
            if (copiedNodes.Count == 0 || copiedNodes.Count != copiedSegments.Count + 1)
            {
                throw new ArgumentException("A route path requires one more node than segment.");
            }

            if (float.IsNaN(totalCost) || float.IsInfinity(totalCost) || totalCost < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCost));
            }

            this.nodeIds = copiedNodes.AsReadOnly();
            this.segmentIds = copiedSegments.AsReadOnly();
            TotalCost = totalCost;
        }

        public IReadOnlyList<WorldFeatureId> NodeIds => nodeIds;
        public IReadOnlyList<WorldFeatureId> SegmentIds => segmentIds;
        public float TotalCost { get; }
    }
}
