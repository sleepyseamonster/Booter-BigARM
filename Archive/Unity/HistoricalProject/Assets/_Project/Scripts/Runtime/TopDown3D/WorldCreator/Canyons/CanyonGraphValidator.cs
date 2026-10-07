using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public static class CanyonGraphValidator
    {
        public static bool TryValidate(CanyonSystemPlan plan, out string error)
        {
            if (plan == null)
            {
                error = "A canyon plan is required.";
                return false;
            }

            if (plan.Nodes.Count == 0 || plan.Nodes.Count > plan.Profile.MaximumNodes)
            {
                error = "Canyon node count is empty or exceeds the bounded planning profile.";
                return false;
            }

            if (plan.Segments.Count == 0 || plan.Segments.Count > plan.Profile.MaximumSegments)
            {
                error = "Canyon segment count is empty or exceeds the bounded planning profile.";
                return false;
            }

            if (plan.BoundaryPorts.Count != 4)
            {
                error = "Each technical system cell requires exactly four cardinal boundary ports.";
                return false;
            }

            if (!plan.Profile.HaloPolicy.AcceptsParticipantCount(plan.PlanningCost.ContextSamples)
                || plan.PlanningCost.ContextSamples > plan.Profile.MaximumContextSamples
                || plan.PlanningCost.BoundaryEvaluations > plan.Profile.MaximumBoundaryEvaluations
                || plan.PlanningCost.NodeCount != plan.Nodes.Count
                || plan.PlanningCost.SegmentCount != plan.Segments.Count)
            {
                error = "Canyon planning cost exceeds its declared deterministic bound.";
                return false;
            }

            var nodeById = new Dictionary<WorldFeatureId, CanyonNodePlan>();
            for (var i = 0; i < plan.Nodes.Count; i++)
            {
                var node = plan.Nodes[i];
                if (!nodeById.TryAdd(node.Id, node))
                {
                    error = $"Canyon plan contains duplicate node '{node.Id}'.";
                    return false;
                }
            }

            if (!nodeById.TryGetValue(plan.RegroupAnchorNodeId, out var regroupAnchor)
                || (regroupAnchor.Role & CanyonNodeRole.RegroupAnchor) == 0)
            {
                error = "Canyon plan regroup anchor is absent or not declared as an anchor.";
                return false;
            }

            var segmentById = new Dictionary<WorldFeatureId, CanyonSegmentPlan>();
            var hasBooterOnly = false;
            var hasBigArm = false;
            for (var i = 0; i < plan.Segments.Count; i++)
            {
                var segment = plan.Segments[i];
                if (!segmentById.TryAdd(segment.Id, segment))
                {
                    error = $"Canyon plan contains duplicate segment '{segment.Id}'.";
                    return false;
                }

                if (!nodeById.ContainsKey(segment.FromNodeId) || !nodeById.ContainsKey(segment.ToNodeId))
                {
                    error = $"Canyon segment '{segment.Id}' references an unknown node.";
                    return false;
                }

                if (!segment.Owner.Equals(plan.Header.Key))
                {
                    error = $"Canyon segment '{segment.Id}' is not owned by its plan.";
                    return false;
                }

                if (segment.DeclaresCycle && !plan.Profile.AllowDeclaredCycles)
                {
                    error = $"Canyon segment '{segment.Id}' declares a cycle but the profile forbids cycles.";
                    return false;
                }

                if (!segment.DeclaresCycle && segment.FromNodeId.CompareTo(segment.ToNodeId) <= 0)
                {
                    error = $"Canyon segment '{segment.Id}' violates the canonical acyclic direction rule.";
                    return false;
                }

                if (segment.Traversal.Access == CanyonTraversalAccess.BooterAndBigArm)
                {
                    hasBigArm = true;
                    if (segment.Traversal.MinimumClearWidth < plan.Profile.BigArmClearWidth
                        || segment.Traversal.MinimumClearHeight < plan.Profile.BigArmClearHeight)
                    {
                        error = $"BigARM route segment '{segment.Id}' does not reserve BigARM clearance.";
                        return false;
                    }
                }
                else
                {
                    hasBooterOnly = true;
                    if (segment.Traversal.MinimumClearWidth < plan.Profile.BooterClearWidth
                        || segment.Traversal.MinimumClearHeight < plan.Profile.BooterClearHeight)
                    {
                        error = $"Booter route segment '{segment.Id}' does not reserve Booter clearance.";
                        return false;
                    }
                }
            }

            if (!hasBooterOnly || !hasBigArm)
            {
                error = "Canyon plan must preserve an intentional Booter-only access difference and a BigARM-compatible network.";
                return false;
            }

            if (!TryValidatePorts(plan, nodeById, out error)
                || !TryValidateTopology(plan.Nodes, plan.Segments, plan.Profile.AllowDeclaredCycles, out error)
                || !TryValidateMacroRoutes(plan, segmentById, out error)
                || !TryValidateRegroupConnectivity(plan, out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryValidateBoundaryAgreement(
            CanyonSystemPlan first,
            CanyonSystemPlan second,
            out string error)
        {
            if (first == null || second == null)
            {
                error = "Two canyon plans are required for boundary validation.";
                return false;
            }

            for (var firstIndex = 0; firstIndex < first.BoundaryPorts.Count; firstIndex++)
            {
                var firstPort = first.BoundaryPorts[firstIndex];
                for (var secondIndex = 0; secondIndex < second.BoundaryPorts.Count; secondIndex++)
                {
                    var secondPort = second.BoundaryPorts[secondIndex];
                    if (!firstPort.Boundary.Equals(secondPort.Boundary))
                    {
                        continue;
                    }

                    if (!firstPort.PortNodeId.Equals(secondPort.PortNodeId)
                        || !first.TryGetNode(firstPort.PortNodeId, out var firstNode)
                        || !second.TryGetNode(secondPort.PortNodeId, out var secondNode)
                        || !firstNode.Equals(secondNode))
                    {
                        error = $"Canyon plans disagree at shared boundary '{firstPort.Boundary.StableId}'.";
                        return false;
                    }

                    error = null;
                    return true;
                }
            }

            error = "The supplied canyon plans do not share a boundary.";
            return false;
        }

        public static bool TryValidateTopology(
            IReadOnlyList<CanyonNodePlan> nodes,
            IReadOnlyList<CanyonSegmentPlan> segments,
            bool allowDeclaredCycles,
            out string error)
        {
            if (nodes == null || segments == null || nodes.Count == 0)
            {
                error = "Topology validation requires nodes and segments.";
                return false;
            }

            var indegree = new Dictionary<WorldFeatureId, int>();
            var outgoing = new Dictionary<WorldFeatureId, List<WorldFeatureId>>();
            for (var i = 0; i < nodes.Count; i++)
            {
                indegree[nodes[i].Id] = 0;
                outgoing[nodes[i].Id] = new List<WorldFeatureId>();
            }

            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (!indegree.ContainsKey(segment.FromNodeId) || !indegree.ContainsKey(segment.ToNodeId))
                {
                    error = $"Topology segment '{segment.Id}' references an unknown node.";
                    return false;
                }

                if (segment.DeclaresCycle)
                {
                    if (!allowDeclaredCycles)
                    {
                        error = $"Topology contains undeclared permission for cycle '{segment.DeclaredCycleId}'.";
                        return false;
                    }

                    continue;
                }

                outgoing[segment.FromNodeId].Add(segment.ToNodeId);
                indegree[segment.ToNodeId]++;
            }

            var frontier = new List<WorldFeatureId>();
            foreach (var pair in indegree)
            {
                if (pair.Value == 0)
                {
                    frontier.Add(pair.Key);
                }
            }

            var visited = 0;
            while (frontier.Count > 0)
            {
                frontier.Sort();
                var current = frontier[0];
                frontier.RemoveAt(0);
                visited++;
                var destinations = outgoing[current];
                for (var i = 0; i < destinations.Count; i++)
                {
                    var destination = destinations[i];
                    indegree[destination]--;
                    if (indegree[destination] == 0)
                    {
                        frontier.Add(destination);
                    }
                }
            }

            if (visited != nodes.Count)
            {
                error = "Canyon topology contains an undeclared directed cycle.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidatePorts(
            CanyonSystemPlan plan,
            IReadOnlyDictionary<WorldFeatureId, CanyonNodePlan> nodeById,
            out string error)
        {
            var sides = new HashSet<CanyonBoundarySide>();
            for (var i = 0; i < plan.BoundaryPorts.Count; i++)
            {
                var port = plan.BoundaryPorts[i];
                if (!sides.Add(port.Side))
                {
                    error = $"Canyon plan contains duplicate boundary side '{port.Side}'.";
                    return false;
                }

                if (!nodeById.TryGetValue(port.PortNodeId, out var node)
                    || (node.Role & CanyonNodeRole.BoundaryPort) == 0)
                {
                    error = $"Canyon boundary '{port.Boundary.StableId}' has no boundary-port node.";
                    return false;
                }

                if (!node.Owner.Equals(port.CanonicalOwner))
                {
                    error = $"Canyon boundary port '{port.PortNodeId}' violates canonical ownership.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateMacroRoutes(
            CanyonSystemPlan plan,
            IReadOnlyDictionary<WorldFeatureId, CanyonSegmentPlan> segmentById,
            out string error)
        {
            var kinds = new HashSet<CanyonMacroRouteKind>();
            for (var routeIndex = 0; routeIndex < plan.MacroRoutes.Count; routeIndex++)
            {
                var route = plan.MacroRoutes[routeIndex];
                if (!kinds.Add(route.Kind))
                {
                    error = $"Canyon plan contains duplicate macro route kind '{route.Kind}'.";
                    return false;
                }

                for (var segmentIndex = 0; segmentIndex < route.SegmentIds.Count; segmentIndex++)
                {
                    if (!segmentById.TryGetValue(route.SegmentIds[segmentIndex], out var segment))
                    {
                        error = $"Macro route '{route.Id}' references an unknown segment.";
                        return false;
                    }

                    if (route.Access == CanyonTraversalAccess.BooterAndBigArm
                        && segment.Traversal.Access != CanyonTraversalAccess.BooterAndBigArm)
                    {
                        error = $"BigARM macro route '{route.Id}' includes a Booter-only segment.";
                        return false;
                    }
                }
            }

            if (!kinds.Contains(CanyonMacroRouteKind.PrimarySpine)
                || !kinds.Contains(CanyonMacroRouteKind.BigArmRegroupNetwork)
                || !kinds.Contains(CanyonMacroRouteKind.BooterOpportunity))
            {
                error = "Canyon plan is missing a required macro route abstraction.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateRegroupConnectivity(CanyonSystemPlan plan, out string error)
        {
            var graph = new CanyonMacroRouteGraph(new[] { plan });
            for (var i = 0; i < plan.BoundaryPorts.Count; i++)
            {
                if (!graph.TryFindRoute(
                        plan.BoundaryPorts[i].PortNodeId,
                        plan.RegroupAnchorNodeId,
                        CanyonTraversalAccess.BooterAndBigArm,
                        out _))
                {
                    error = $"Boundary port '{plan.BoundaryPorts[i].PortNodeId}' lacks a BigARM regroup route.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
