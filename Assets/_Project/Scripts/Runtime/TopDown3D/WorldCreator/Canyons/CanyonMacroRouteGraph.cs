using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// A pure-data route graph that remains available while terrain representations are unloaded.
    /// Traversal is bidirectional; canyon flow direction remains encoded by each segment.
    /// </summary>
    public sealed class CanyonMacroRouteGraph
    {
        private readonly Dictionary<WorldFeatureId, CanyonNodePlan> nodes =
            new Dictionary<WorldFeatureId, CanyonNodePlan>();
        private readonly Dictionary<WorldFeatureId, CanyonSegmentPlan> segments =
            new Dictionary<WorldFeatureId, CanyonSegmentPlan>();
        private readonly Dictionary<WorldFeatureId, List<Adjacency>> adjacency =
            new Dictionary<WorldFeatureId, List<Adjacency>>();

        public CanyonMacroRouteGraph(IEnumerable<CanyonSystemPlan> plans)
        {
            if (plans == null)
            {
                throw new ArgumentNullException(nameof(plans));
            }

            foreach (var plan in plans)
            {
                if (plan == null)
                {
                    throw new ArgumentException("Macro route graphs cannot contain null plans.", nameof(plans));
                }

                AddPlan(plan);
            }

            foreach (var pair in adjacency)
            {
                pair.Value.Sort((left, right) =>
                {
                    var nodeComparison = left.NeighborId.CompareTo(right.NeighborId);
                    return nodeComparison != 0
                        ? nodeComparison
                        : left.Segment.Id.CompareTo(right.Segment.Id);
                });
            }
        }

        public int NodeCount => nodes.Count;
        public int SegmentCount => segments.Count;

        public bool TryFindRoute(
            WorldFeatureId startNodeId,
            WorldFeatureId destinationNodeId,
            CanyonTraversalAccess requiredAccess,
            out CanyonRoutePath path)
        {
            path = null;
            if (!nodes.ContainsKey(startNodeId) || !nodes.ContainsKey(destinationNodeId))
            {
                return false;
            }

            if (startNodeId.Equals(destinationNodeId))
            {
                path = new CanyonRoutePath(new[] { startNodeId }, Array.Empty<WorldFeatureId>(), 0f);
                return true;
            }

            var frontier = new List<FrontierEntry>
            {
                new FrontierEntry(startNodeId, 0f)
            };
            var bestCost = new Dictionary<WorldFeatureId, float>
            {
                [startNodeId] = 0f
            };
            var previous = new Dictionary<WorldFeatureId, PreviousStep>();
            while (frontier.Count > 0)
            {
                frontier.Sort(FrontierEntry.Compare);
                var current = frontier[0];
                frontier.RemoveAt(0);
                if (bestCost.TryGetValue(current.NodeId, out var recordedCost)
                    && current.Cost > recordedCost)
                {
                    continue;
                }

                if (current.NodeId.Equals(destinationNodeId))
                {
                    path = Reconstruct(startNodeId, destinationNodeId, previous, current.Cost);
                    return true;
                }

                var neighbors = adjacency[current.NodeId];
                for (var i = 0; i < neighbors.Count; i++)
                {
                    var candidate = neighbors[i];
                    if (!Supports(candidate.Segment.Traversal.Access, requiredAccess))
                    {
                        continue;
                    }

                    var candidateCost = current.Cost + candidate.Segment.Traversal.RouteCost;
                    if (bestCost.TryGetValue(candidate.NeighborId, out var oldCost)
                        && candidateCost >= oldCost)
                    {
                        continue;
                    }

                    bestCost[candidate.NeighborId] = candidateCost;
                    previous[candidate.NeighborId] = new PreviousStep(current.NodeId, candidate.Segment.Id);
                    frontier.Add(new FrontierEntry(candidate.NeighborId, candidateCost));
                }
            }

            return false;
        }

        private void AddPlan(CanyonSystemPlan plan)
        {
            for (var i = 0; i < plan.Nodes.Count; i++)
            {
                var node = plan.Nodes[i];
                if (nodes.TryGetValue(node.Id, out var existing) && !existing.Equals(node))
                {
                    throw new InvalidOperationException(
                        $"Canyon plans disagree about shared node '{node.Id}'.");
                }

                nodes[node.Id] = node;
                if (!adjacency.ContainsKey(node.Id))
                {
                    adjacency.Add(node.Id, new List<Adjacency>());
                }
            }

            for (var i = 0; i < plan.Segments.Count; i++)
            {
                var segment = plan.Segments[i];
                if (segments.TryGetValue(segment.Id, out var existing) && !existing.Equals(segment))
                {
                    throw new InvalidOperationException(
                        $"Canyon plans disagree about shared segment '{segment.Id}'.");
                }

                if (segments.ContainsKey(segment.Id))
                {
                    continue;
                }

                if (!nodes.ContainsKey(segment.FromNodeId) || !nodes.ContainsKey(segment.ToNodeId))
                {
                    throw new InvalidOperationException(
                        $"Canyon segment '{segment.Id}' references a node absent from its supplied plans.");
                }

                segments.Add(segment.Id, segment);
                adjacency[segment.FromNodeId].Add(new Adjacency(segment.ToNodeId, segment));
                adjacency[segment.ToNodeId].Add(new Adjacency(segment.FromNodeId, segment));
            }
        }

        private static bool Supports(CanyonTraversalAccess available, CanyonTraversalAccess required)
        {
            return required == CanyonTraversalAccess.BooterOnly
                || available == CanyonTraversalAccess.BooterAndBigArm;
        }

        private static CanyonRoutePath Reconstruct(
            WorldFeatureId start,
            WorldFeatureId destination,
            IReadOnlyDictionary<WorldFeatureId, PreviousStep> previous,
            float totalCost)
        {
            var nodeIds = new List<WorldFeatureId> { destination };
            var segmentIds = new List<WorldFeatureId>();
            var cursor = destination;
            while (!cursor.Equals(start))
            {
                var step = previous[cursor];
                segmentIds.Add(step.SegmentId);
                cursor = step.PreviousNodeId;
                nodeIds.Add(cursor);
            }

            nodeIds.Reverse();
            segmentIds.Reverse();
            return new CanyonRoutePath(nodeIds, segmentIds, totalCost);
        }

        private readonly struct Adjacency
        {
            public Adjacency(WorldFeatureId neighborId, CanyonSegmentPlan segment)
            {
                NeighborId = neighborId;
                Segment = segment;
            }

            public WorldFeatureId NeighborId { get; }
            public CanyonSegmentPlan Segment { get; }
        }

        private readonly struct PreviousStep
        {
            public PreviousStep(WorldFeatureId previousNodeId, WorldFeatureId segmentId)
            {
                PreviousNodeId = previousNodeId;
                SegmentId = segmentId;
            }

            public WorldFeatureId PreviousNodeId { get; }
            public WorldFeatureId SegmentId { get; }
        }

        private readonly struct FrontierEntry
        {
            public FrontierEntry(WorldFeatureId nodeId, float cost)
            {
                NodeId = nodeId;
                Cost = cost;
            }

            public WorldFeatureId NodeId { get; }
            public float Cost { get; }

            public static int Compare(FrontierEntry left, FrontierEntry right)
            {
                var costComparison = left.Cost.CompareTo(right.Cost);
                return costComparison != 0 ? costComparison : left.NodeId.CompareTo(right.NodeId);
            }
        }
    }
}
