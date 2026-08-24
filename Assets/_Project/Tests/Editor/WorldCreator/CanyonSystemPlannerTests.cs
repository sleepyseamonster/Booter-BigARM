using System;
using System.Collections.Generic;
using System.Globalization;
using BooterBigArm.Editor.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEditor;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class CanyonSystemPlannerTests
    {
        [Test]
        public void SameCellBuildIsDeterministicAndValid()
        {
            var planner = CreatePlanner(out var world, out var model);
            var cell = new CanyonSystemCellIndex(7, -4);

            var first = Build(planner, world, model, cell);
            var second = Build(planner, world, model, cell);

            Assert.That(second.Header, Is.EqualTo(first.Header));
            Assert.That(second.Nodes, Is.EqualTo(first.Nodes));
            Assert.That(second.Segments, Is.EqualTo(first.Segments));
            Assert.That(second.BoundaryPorts, Is.EqualTo(first.BoundaryPorts));
            Assert.That(CanyonGraphValidator.TryValidate(second, out var error), Is.True, error);
        }

        [Test]
        public void AdjacentCellsAgreeExactlyOnSharedPortsInEveryDirection()
        {
            var planner = CreatePlanner(out var world, out var model);
            var center = Build(planner, world, model, new CanyonSystemCellIndex(0, 0));
            var neighbors = new[]
            {
                Build(planner, world, model, new CanyonSystemCellIndex(-1, 0)),
                Build(planner, world, model, new CanyonSystemCellIndex(1, 0)),
                Build(planner, world, model, new CanyonSystemCellIndex(0, -1)),
                Build(planner, world, model, new CanyonSystemCellIndex(0, 1))
            };

            for (var i = 0; i < neighbors.Length; i++)
            {
                Assert.That(
                    CanyonGraphValidator.TryValidateBoundaryAgreement(center, neighbors[i], out var error),
                    Is.True,
                    error);
            }
        }

        [Test]
        public void BuildOrderDoesNotChangePlansOrBoundaryOwnership()
        {
            var planner = CreatePlanner(out var world, out var model);
            var cells = new[]
            {
                new CanyonSystemCellIndex(-1, 0),
                new CanyonSystemCellIndex(0, 0),
                new CanyonSystemCellIndex(1, 0),
                new CanyonSystemCellIndex(1, 1)
            };
            var firstOrder = BuildMap(planner, world, model, cells);
            Array.Reverse(cells);
            var secondOrder = BuildMap(planner, world, model, cells);

            foreach (var pair in firstOrder)
            {
                var rebuilt = secondOrder[pair.Key];
                Assert.That(rebuilt.Header, Is.EqualTo(pair.Value.Header));
                Assert.That(rebuilt.Nodes, Is.EqualTo(pair.Value.Nodes));
                Assert.That(rebuilt.Segments, Is.EqualTo(pair.Value.Segments));
                for (var portIndex = 0; portIndex < rebuilt.BoundaryPorts.Count; portIndex++)
                {
                    var port = rebuilt.BoundaryPorts[portIndex];
                    Assert.That(port.CanonicalOwner.CompareTo(port.Boundary.Neighbor), Is.LessThan(0));
                }
            }
        }

        [Test]
        public void ThreeByThreeMacroGraphKeepsBigArmRegroupRouteConnected()
        {
            var plans = CanyonPlanOverlayExporter.BuildProofPlans(1);
            var graph = new CanyonMacroRouteGraph(plans);
            var first = FindPlan(plans, -1, -1);
            var destination = FindPlan(plans, 1, 1);
            var start = GetBoundary(first, CanyonBoundarySide.West).PortNodeId;

            Assert.That(
                graph.TryFindRoute(
                    start,
                    destination.RegroupAnchorNodeId,
                    CanyonTraversalAccess.BooterAndBigArm,
                    out var route),
                Is.True);
            Assert.That(route.NodeIds.Count, Is.GreaterThan(3));
            Assert.That(route.TotalCost, Is.GreaterThan(0f));
        }

        [Test]
        public void BooterOpportunityIsIntentionallyUnavailableToBigArm()
        {
            var planner = CreatePlanner(out var world, out var model);
            var plan = Build(planner, world, model, new CanyonSystemCellIndex(0, 0));
            var graph = new CanyonMacroRouteGraph(new[] { plan });
            var opportunity = FindNodeWithRole(plan, CanyonNodeRole.LocalOpportunity);

            Assert.That(
                graph.TryFindRoute(
                    plan.RegroupAnchorNodeId,
                    opportunity.Id,
                    CanyonTraversalAccess.BooterOnly,
                    out _),
                Is.True);
            Assert.That(
                graph.TryFindRoute(
                    plan.RegroupAnchorNodeId,
                    opportunity.Id,
                    CanyonTraversalAccess.BooterAndBigArm,
                    out _),
                Is.False);
        }

        [Test]
        public void GeneratedHierarchyProfilesAndClearancesAreBounded()
        {
            var planner = CreatePlanner(out var world, out var model);
            var seenHierarchy = new HashSet<CanyonHierarchyTier>();
            var seenRoles = CanyonNodeRole.None;
            for (var horizontalA = -2; horizontalA <= 2; horizontalA++)
            {
                for (var horizontalB = -2; horizontalB <= 2; horizontalB++)
                {
                    var plan = Build(
                        planner,
                        world,
                        model,
                        new CanyonSystemCellIndex(horizontalA, horizontalB));
                    Assert.That(CanyonGraphValidator.TryValidate(plan, out var error), Is.True, error);
                    Assert.That(plan.PlanningCost.ContextSamples, Is.EqualTo(5));
                    Assert.That(plan.PlanningCost.BoundaryEvaluations, Is.EqualTo(4));
                    Assert.That(plan.PlanningCost.NodeCount, Is.LessThanOrEqualTo(plan.Profile.MaximumNodes));
                    Assert.That(plan.PlanningCost.SegmentCount, Is.LessThanOrEqualTo(plan.Profile.MaximumSegments));
                    for (var nodeIndex = 0; nodeIndex < plan.Nodes.Count; nodeIndex++)
                    {
                        var section = plan.Nodes[nodeIndex].CrossSection;
                        Assert.That(section.Width, Is.GreaterThan(0f));
                        Assert.That(section.Depth, Is.GreaterThan(0f));
                        Assert.That(section.Shelves.Lower, Is.LessThan(section.Shelves.Middle));
                        Assert.That(section.Shelves.Middle, Is.LessThan(section.Shelves.Upper));
                        seenRoles |= plan.Nodes[nodeIndex].Role;
                    }

                    for (var segmentIndex = 0; segmentIndex < plan.Segments.Count; segmentIndex++)
                    {
                        seenHierarchy.Add(plan.Segments[segmentIndex].Hierarchy);
                    }
                }
            }

            Assert.That(seenHierarchy, Does.Contain(CanyonHierarchyTier.MainSpine));
            Assert.That(seenHierarchy, Does.Contain(CanyonHierarchyTier.Tributary));
            Assert.That(seenHierarchy, Does.Contain(CanyonHierarchyTier.MinorBranch));
            Assert.That((seenRoles & CanyonNodeRole.Source) != 0, Is.True);
            Assert.That((seenRoles & CanyonNodeRole.Termination) != 0, Is.True);
            Assert.That((seenRoles & CanyonNodeRole.Branch) != 0, Is.True);
            Assert.That((seenRoles & CanyonNodeRole.Confluence) != 0, Is.True);
            Assert.That((seenRoles & CanyonNodeRole.Spine) != 0, Is.True);
        }

        [Test]
        public void LandscapeContextChangesBranchDensityWithoutChangingPlanningBounds()
        {
            var planner = CreatePlanner(out var world, out var model);
            var lowerInfluence = Build(planner, world, model, new CanyonSystemCellIndex(-1, -1));
            var higherInfluence = Build(planner, world, model, new CanyonSystemCellIndex(1, 1));

            Assert.That(higherInfluence.Nodes.Count, Is.GreaterThan(lowerInfluence.Nodes.Count));
            Assert.That(higherInfluence.Segments.Count, Is.GreaterThan(lowerInfluence.Segments.Count));
            Assert.That(higherInfluence.Nodes.Count, Is.LessThanOrEqualTo(higherInfluence.Profile.MaximumNodes));
            Assert.That(higherInfluence.Segments.Count, Is.LessThanOrEqualTo(higherInfluence.Profile.MaximumSegments));
        }

        [Test]
        public void UndeclaredDirectedCycleIsRejectedAndDeclaredCycleCanBeIsolated()
        {
            var planner = CreatePlanner(out var world, out var model);
            var fixture = Build(planner, world, model, new CanyonSystemCellIndex(0, 0));
            var nodes = new[] { fixture.Nodes[0], fixture.Nodes[1], fixture.Nodes[2] };
            var traversal = fixture.Segments[0].Traversal;
            var owner = fixture.Header.Key;
            var first = CreateTestSegment(world, owner, nodes[0].Id, nodes[1].Id, traversal, 1, null);
            var second = CreateTestSegment(world, owner, nodes[1].Id, nodes[2].Id, traversal, 2, null);
            var closing = CreateTestSegment(world, owner, nodes[2].Id, nodes[0].Id, traversal, 3, null);

            Assert.That(
                CanyonGraphValidator.TryValidateTopology(nodes, new[] { first, second, closing }, false, out var error),
                Is.False);
            Assert.That(error, Does.Contain("undeclared directed cycle"));

            var declaredClosing = CreateTestSegment(
                world,
                owner,
                nodes[2].Id,
                nodes[0].Id,
                traversal,
                4,
                "proof.non-canon.cycle.fixture");
            Assert.That(
                CanyonGraphValidator.TryValidateTopology(
                    nodes,
                    new[] { first, second, declaredClosing },
                    true,
                    out var declaredError),
                Is.True,
                declaredError);
        }

        [Test]
        public void PlannerAcceptsAReplacementCoordinateAdapterWithoutOwningItsSchema()
        {
            var planner = CreatePlanner(out var world, out var firstModel);
            var secondModel = new AlternateNonCanonCoordinateModel();
            var cell = new CanyonSystemCellIndex(2, -3);
            var first = Build(planner, world, firstModel, cell);
            var second = Build(planner, world, secondModel, cell);

            Assert.That(second.Nodes.Count, Is.EqualTo(first.Nodes.Count));
            Assert.That(second.Segments.Count, Is.EqualTo(first.Segments.Count));
            Assert.That(second.BoundaryPorts.Count, Is.EqualTo(first.BoundaryPorts.Count));
            Assert.That(second.Header.Key.OwnerAddress.ModelId, Is.EqualTo(secondModel.ModelId));
            Assert.That(second.Header.ContentFingerprint, Is.Not.EqualTo(first.Header.ContentFingerprint));
            Assert.That(CanyonGraphValidator.TryValidate(second, out var error), Is.True, error);
        }

        [Test]
        public void PlannerFailsClosedWhenCoordinateAdapterCollapsesAdjacentCells()
        {
            var planner = CreatePlanner(out var world, out _);
            var collapsed = new CollapsedNonCanonCoordinateModel();

            Assert.That(
                planner.TryBuild(
                    world,
                    collapsed,
                    new CanyonSystemCellIndex(0, 0),
                    out _,
                    out var error),
                Is.False);
            Assert.That(error, Does.Contain("cannot distinguish adjacent technical planning cells"));
        }

        private static CanyonSystemPlanner CreatePlanner(
            out WorldIdentity world,
            out IWorldCoordinateModel model)
        {
            var provinces = AssetDatabase.LoadAssetAtPath<GeologicProvinceCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.ProvinceCatalogPath);
            var strata = AssetDatabase.LoadAssetAtPath<StrataFamilyCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.StrataCatalogPath);
            var influence = AssetDatabase.LoadAssetAtPath<NonCanonProofInfluenceProfile>(
                WorldCreatorBatch2ProofAssetBuilder.InfluenceProfilePath);
            Assert.That(provinces, Is.Not.Null);
            Assert.That(strata, Is.Not.Null);
            Assert.That(influence, Is.Not.Null);
            world = WorldCreatorTestFactory.CreateWorld();
            model = new NonCanonProofCoordinateModel();
            return new CanyonSystemPlanner(
                new WorldCoordinateContextSampler(provinces, strata, influence),
                CanyonPlannerProfile.CreateNonCanonTechnicalProofProfile());
        }

        private static CanyonSystemPlan Build(
            CanyonSystemPlanner planner,
            WorldIdentity world,
            IWorldCoordinateModel model,
            CanyonSystemCellIndex cell)
        {
            Assert.That(planner.TryBuild(world, model, cell, out var plan, out var error), Is.True, error);
            return plan;
        }

        private static Dictionary<CanyonSystemCellIndex, CanyonSystemPlan> BuildMap(
            CanyonSystemPlanner planner,
            WorldIdentity world,
            IWorldCoordinateModel model,
            IEnumerable<CanyonSystemCellIndex> cells)
        {
            var result = new Dictionary<CanyonSystemCellIndex, CanyonSystemPlan>();
            foreach (var cell in cells)
            {
                result.Add(cell, Build(planner, world, model, cell));
            }

            return result;
        }

        private static CanyonSystemPlan FindPlan(
            IReadOnlyList<CanyonSystemPlan> plans,
            long horizontalA,
            long horizontalB)
        {
            var cell = new CanyonSystemCellIndex(horizontalA, horizontalB);
            for (var i = 0; i < plans.Count; i++)
            {
                if (plans[i].Cell.Equals(cell))
                {
                    return plans[i];
                }
            }

            Assert.Fail($"Missing proof plan for {cell}.");
            return null;
        }

        private static CanyonBoundaryPortReference GetBoundary(
            CanyonSystemPlan plan,
            CanyonBoundarySide side)
        {
            for (var i = 0; i < plan.BoundaryPorts.Count; i++)
            {
                if (plan.BoundaryPorts[i].Side == side)
                {
                    return plan.BoundaryPorts[i];
                }
            }

            Assert.Fail($"Missing boundary side {side}.");
            return default;
        }

        private static CanyonNodePlan FindNodeWithRole(CanyonSystemPlan plan, CanyonNodeRole role)
        {
            for (var i = 0; i < plan.Nodes.Count; i++)
            {
                if ((plan.Nodes[i].Role & role) != 0)
                {
                    return plan.Nodes[i];
                }
            }

            Assert.Fail($"Missing node role {role}.");
            return default;
        }

        private static CanyonSegmentPlan CreateTestSegment(
            WorldIdentity world,
            WorldPlanKey owner,
            WorldFeatureId from,
            WorldFeatureId to,
            CanyonTraversalReservation traversal,
            int ordinal,
            string declaredCycleId)
        {
            var id = WorldFeatureId.Create(
                world,
                new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.canyon-cycle-test"),
                owner.OwnerAddress,
                "segment:" + ordinal);
            return new CanyonSegmentPlan(
                id,
                owner,
                from,
                to,
                CanyonHierarchyTier.Tributary,
                0.5f,
                traversal,
                declaredCycleId);
        }

        private sealed class AlternateNonCanonCoordinateModel : IWorldCoordinateModel
        {
            public string ModelId => "proof.non-canon.coordinate.alternate-canyon-test";
            public int ModelVersion => 1;

            public WorldCoordinateAddress Encode(AbsoluteWorldPosition absolutePosition)
            {
                var canonical = absolutePosition.HorizontalB.ToString("R", CultureInfo.InvariantCulture)
                    + ";" + absolutePosition.Vertical.ToString("R", CultureInfo.InvariantCulture)
                    + ";" + absolutePosition.HorizontalA.ToString("R", CultureInfo.InvariantCulture);
                return new WorldCoordinateAddress(ModelId, ModelVersion, canonical);
            }

            public bool TryResolve(WorldCoordinateAddress address, out AbsoluteWorldPosition absolutePosition)
            {
                absolutePosition = default;
                if (!address.IsCompatibleWith(this))
                {
                    return false;
                }

                var parts = address.CanonicalValue.Split(';');
                if (parts.Length != 3
                    || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var horizontalB)
                    || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var vertical)
                    || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var horizontalA))
                {
                    return false;
                }

                absolutePosition = new AbsoluteWorldPosition(horizontalA, vertical, horizontalB);
                return true;
            }
        }

        private sealed class CollapsedNonCanonCoordinateModel : IWorldCoordinateModel
        {
            public string ModelId => "proof.non-canon.coordinate.collapsed-canyon-test";
            public int ModelVersion => 1;

            public WorldCoordinateAddress Encode(AbsoluteWorldPosition absolutePosition)
            {
                return new WorldCoordinateAddress(ModelId, ModelVersion, "collapsed");
            }

            public bool TryResolve(WorldCoordinateAddress address, out AbsoluteWorldPosition absolutePosition)
            {
                absolutePosition = default;
                return address.IsCompatibleWith(this);
            }
        }
    }
}
