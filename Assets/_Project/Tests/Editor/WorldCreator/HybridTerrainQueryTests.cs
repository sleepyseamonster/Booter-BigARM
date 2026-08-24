using System;
using System.Collections.Generic;
using BooterBigArm.Editor.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class HybridTerrainQueryTests
    {
        [Test]
        public void CompilationAndQueriesAreDeterministicAcrossPlanBuildOrder()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(true);
            var reversed = new List<CanyonSystemPlan>(fixture.CanyonPlans);
            reversed.Reverse();
            var rebuiltPlan = HybridTerrainCompiler.Compile(fixture.World, reversed, fixture.Plan.Histories);
            var rebuiltQuery = new DormantHybridWorldQueryService(rebuiltPlan, fixture.Model, fixture.Context);

            Assert.That(rebuiltPlan.Fingerprint, Is.EqualTo(fixture.Plan.Fingerprint));
            Assert.That(rebuiltPlan.CanyonSegments.Count, Is.EqualTo(fixture.Plan.CanyonSegments.Count));
            Assert.That(rebuiltPlan.LandformRequests, Is.EqualTo(fixture.Plan.LandformRequests));
            var positions = new[]
            {
                new AbsoluteWorldPosition(-576d, 0d, 0d),
                new AbsoluteWorldPosition(0d, 0d, 0d),
                new AbsoluteWorldPosition(576d, 0d, 127.25d),
                new AbsoluteWorldPosition(72d, 0d, 54d)
            };
            for (var i = 0; i < positions.Length; i++)
            {
                Assert.That(fixture.Query.TrySampleSurface(positions[i], out var first, out var firstError), Is.True, firstError);
                Assert.That(rebuiltQuery.TrySampleSurface(positions[i], out var second, out var secondError), Is.True, secondError);
                Assert.That(second, Is.EqualTo(first));
            }
        }

        [Test]
        public void AbsoluteBoundarySamplesAreExactlyEqualAcrossEquivalentCompilations()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var first = HybridTerrainCompiler.Compile(fixture.World, fixture.CanyonPlans);
            var secondPlans = new List<CanyonSystemPlan>(fixture.CanyonPlans);
            secondPlans.Sort((left, right) => right.Cell.CompareTo(left.Cell));
            var second = HybridTerrainCompiler.Compile(fixture.World, secondPlans);
            var firstQuery = new DormantHybridWorldQueryService(first, fixture.Model, fixture.Context);
            var secondQuery = new DormantHybridWorldQueryService(second, fixture.Model, fixture.Context);

            for (var coordinate = -540; coordinate <= 540; coordinate += 36)
            {
                var boundary = new AbsoluteWorldPosition(576d, 0d, coordinate);
                Assert.That(firstQuery.TrySampleSurface(boundary, out var a, out var aError), Is.True, aError);
                Assert.That(secondQuery.TrySampleSurface(boundary, out var b, out var bError), Is.True, bError);
                Assert.That(b, Is.EqualTo(a));
            }
        }

        [Test]
        public void SurfaceVolumeAndAffordanceSamplesRemainFiniteAndBounded()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(true);
            for (var a = -864; a <= 864; a += 72)
            {
                for (var b = -864; b <= 864; b += 72)
                {
                    var position = new AbsoluteWorldPosition(a, 0d, b);
                    Assert.That(fixture.Query.TrySampleSurface(position, out var surface, out var surfaceError), Is.True, surfaceError);
                    Assert.That(surface.Position.Vertical, Is.InRange(-240d, 240d));
                    Assert.That(surface.NormalVertical, Is.InRange(-1f, 1f));
                    Assert.That(fixture.Query.TrySampleVolume(position, out var volume, out var volumeError), Is.True, volumeError);
                    Assert.That(float.IsNaN(volume.SignedDistanceToSolid), Is.False);
                    Assert.That(float.IsInfinity(volume.SignedDistanceToSolid), Is.False);
                    Assert.That(fixture.Query.TrySampleAffordance(position, WorldAgentProfile.BooterProof, out var affordance, out var affordanceError), Is.True, affordanceError);
                    Assert.That(affordance.SlopeDegrees, Is.InRange(0f, 180f));
                }
            }
        }

        [Test]
        public void CompiledMacroRoutesPreserveBooterAndBigArmClearanceIntent()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var graph = new CanyonMacroRouteGraph(fixture.CanyonPlans);
            var first = FindPlan(fixture.CanyonPlans, -1, -1);
            var destination = FindPlan(fixture.CanyonPlans, 1, 1);
            var start = FindBoundary(first, CanyonBoundarySide.West).PortNodeId;
            Assert.That(graph.TryFindRoute(start, destination.RegroupAnchorNodeId, CanyonTraversalAccess.BooterAndBigArm, out var route), Is.True);
            Assert.That(route.SegmentIds.Count, Is.GreaterThan(2));

            var checkedBigArm = 0;
            var checkedBooterOnly = 0;
            for (var i = 0; i < fixture.Plan.CanyonSegments.Count; i++)
            {
                var segment = fixture.Plan.CanyonSegments[i];
                var midpoint = Midpoint(segment.From.Position, segment.To.Position);
                if (segment.Source.Traversal.SupportsBigArm && checkedBigArm < 6)
                {
                    Assert.That(fixture.Query.TrySampleAffordance(midpoint, WorldAgentProfile.BigArmProof, out var bigArm, out var error), Is.True, error);
                    Assert.That(bigArm.ReservedRoute, Is.True);
                    checkedBigArm++;
                }
                else if (!segment.Source.Traversal.SupportsBigArm && checkedBooterOnly < 3)
                {
                    Assert.That(fixture.Query.TrySampleAffordance(midpoint, WorldAgentProfile.BooterProof, out var booter, out var booterError), Is.True, booterError);
                    Assert.That(booter.ReservedRoute, Is.True);
                    Assert.That(fixture.Query.TrySampleAffordance(midpoint, WorldAgentProfile.BigArmProof, out var bigArm, out var bigArmError), Is.True, bigArmError);
                    Assert.That(bigArm.ReservedRoute, Is.False);
                    checkedBooterOnly++;
                }
            }

            Assert.That(checkedBigArm, Is.GreaterThan(0));
            Assert.That(checkedBooterOnly, Is.GreaterThan(0));
        }

        [Test]
        public void BoundedFeatureAndHistoryIdentitiesSurviveRebuild()
        {
            var first = HybridTerrainComparisonPanelExporter.BuildProofFixture(true);
            var second = HybridTerrainComparisonPanelExporter.BuildProofFixture(true);
            Assert.That(second.Plan.Fingerprint, Is.EqualTo(first.Plan.Fingerprint));
            Assert.That(second.Plan.LandformRequests, Is.EqualTo(first.Plan.LandformRequests));
            Assert.That(second.Plan.Histories[0].Id, Is.EqualTo(first.Plan.Histories[0].Id));
            Assert.That(second.Plan.Histories[0].Reservation.Id, Is.EqualTo(first.Plan.Histories[0].Reservation.Id));
            Assert.That(second.Plan.Histories[0].Operations, Is.EqualTo(first.Plan.Histories[0].Operations));
        }

        [Test]
        public void SyntheticHistoryChangesOnlyItsReservedNeighborhood()
        {
            var baseline = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var history = HybridTerrainComparisonPanelExporter.BuildProofFixture(true);
            var center = history.Plan.Histories[0].Reservation.Center;
            var far = new AbsoluteWorldPosition(center.HorizontalA + 200d, 0d, center.HorizontalB + 200d);
            Assert.That(baseline.Query.TrySampleSurface(center, out var baselineCenter, out var errorA), Is.True, errorA);
            Assert.That(history.Query.TrySampleSurface(center, out var historyCenter, out var errorB), Is.True, errorB);
            Assert.That(historyCenter.Position.Vertical, Is.Not.EqualTo(baselineCenter.Position.Vertical));
            Assert.That((historyCenter.Semantic & WorldSurfaceSemantic.SiteReservation) != 0, Is.True);
            Assert.That(baseline.Query.TrySampleSurface(far, out var baselineFar, out var errorC), Is.True, errorC);
            Assert.That(history.Query.TrySampleSurface(far, out var historyFar, out var errorD), Is.True, errorD);
            Assert.That(historyFar.Position.Vertical, Is.EqualTo(baselineFar.Position.Vertical));
            Assert.That((historyFar.Semantic & WorldSurfaceSemantic.SiteReservation) != 0, Is.False);
        }

        private static AbsoluteWorldPosition Midpoint(AbsoluteWorldPosition from, AbsoluteWorldPosition to)
        {
            return new AbsoluteWorldPosition(
                (from.HorizontalA + to.HorizontalA) * 0.5d,
                0d,
                (from.HorizontalB + to.HorizontalB) * 0.5d);
        }

        private static CanyonSystemPlan FindPlan(IReadOnlyList<CanyonSystemPlan> plans, long a, long b)
        {
            for (var i = 0; i < plans.Count; i++)
                if (plans[i].Cell.HorizontalA == a && plans[i].Cell.HorizontalB == b) return plans[i];
            throw new AssertionException("Expected proof plan was absent.");
        }

        private static CanyonBoundaryPortReference FindBoundary(CanyonSystemPlan plan, CanyonBoundarySide side)
        {
            for (var i = 0; i < plan.BoundaryPorts.Count; i++)
                if (plan.BoundaryPorts[i].Side == side) return plan.BoundaryPorts[i];
            throw new AssertionException("Expected boundary port was absent.");
        }
    }
}
