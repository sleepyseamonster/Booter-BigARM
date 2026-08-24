using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BooterBigArm.Editor.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class UnboundedHybridWorldQueryTests
    {
        [Test]
        public void RuntimeProofCoordinateAdapterRoundTripsFarAddressesWithoutDefiningGlobeRules()
        {
            var model = new NonCanonTechnicalCoordinateModel();
            var position = new AbsoluteWorldPosition(9_007_199_254_740_991d, -731.25d, -8_765_432_109_876d);
            var address = model.Encode(position);
            Assert.That(model.ModelId, Does.StartWith("proof.non-canon."));
            Assert.That(model.TryResolve(address, out var restored), Is.True);
            Assert.That(restored, Is.EqualTo(position));
            Assert.That(address.CanonicalValue, Does.Not.Contain("latitude"));
            Assert.That(address.CanonicalValue, Does.Not.Contain("longitude"));
        }

        [Test]
        public void QueryResultsAreIndependentOfWindowRequestOrderAndEviction()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var model = new NonCanonTechnicalCoordinateModel();
            var first = CreateService(fixture, model, 12, 2);
            var second = CreateService(fixture, model, 12, 2);
            var positions = new[]
            {
                new AbsoluteWorldPosition(-1728.25d, 0d, 91.5d),
                new AbsoluteWorldPosition(-0.25d, 0d, 576d),
                new AbsoluteWorldPosition(576d, 0d, -0.25d),
                new AbsoluteWorldPosition(2304.75d, 0d, -1152.5d),
                new AbsoluteWorldPosition(1_000_000.125d, 0d, -1_000_000.875d)
            };
            var expected = new WorldSurfaceSample[positions.Length];
            for (var i = 0; i < positions.Length; i++)
            {
                Assert.That(first.TrySampleSurface(positions[i], out expected[i], out var error), Is.True, error);
            }

            for (var i = positions.Length - 1; i >= 0; i--)
            {
                Assert.That(second.TrySampleSurface(positions[i], out var actual, out var error), Is.True, error);
                Assert.That(actual, Is.EqualTo(expected[i]));
            }

            first.ClearCaches();
            for (var i = 0; i < positions.Length; i++)
            {
                Assert.That(first.TrySampleSurface(positions[i], out var rebuilt, out var error), Is.True, error);
                Assert.That(rebuilt, Is.EqualTo(expected[i]));
            }
        }

        [Test]
        public void CanonicalBoundarySamplesAgreeExactlyAcrossIndependentQueryWindows()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var model = new NonCanonTechnicalCoordinateModel();
            var leftFirst = CreateService(fixture, model, 18, 3);
            var rightFirst = CreateService(fixture, model, 18, 3);
            for (var along = -540d; along <= 540d; along += 36d)
            {
                Assert.That(leftFirst.TrySampleSurface(new AbsoluteWorldPosition(575.9d, 0d, along), out _, out var leftError), Is.True, leftError);
                Assert.That(leftFirst.TrySampleSurface(new AbsoluteWorldPosition(576d, 0d, along), out var first, out var firstError), Is.True, firstError);
                Assert.That(rightFirst.TrySampleSurface(new AbsoluteWorldPosition(576.1d, 0d, along), out _, out var rightError), Is.True, rightError);
                Assert.That(rightFirst.TrySampleSurface(new AbsoluteWorldPosition(576d, 0d, along), out var second, out var secondError), Is.True, secondError);
                Assert.That(second, Is.EqualTo(first));
            }
        }

        [Test]
        public void OnDemandPlanAndTerrainCachesRemainBoundedAcrossLongTravel()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var service = CreateService(fixture, new NonCanonTechnicalCoordinateModel(), 12, 2);
            for (var cell = -40; cell <= 40; cell++)
            {
                var position = new AbsoluteWorldPosition((cell + 0.5d) * 576d, 0d, (cell % 5 + 0.5d) * 576d);
                Assert.That(service.TrySampleSurface(position, out _, out var error), Is.True, error);
            }

            var snapshot = service.CaptureCacheSnapshot();
            Assert.That(snapshot.CanyonPlanCount, Is.LessThanOrEqualTo(snapshot.MaximumCanyonPlans));
            Assert.That(snapshot.TerrainWindowCount, Is.LessThanOrEqualTo(snapshot.MaximumTerrainWindows));
            Assert.That(snapshot.CanyonEvictions, Is.GreaterThan(0));
            Assert.That(snapshot.TerrainEvictions, Is.GreaterThan(0));
            Assert.That(snapshot.CanyonPlanBuilds, Is.GreaterThan(81));
        }

        [Test]
        public async Task RepresentationCompilerCanBuildFarAbsoluteTilesFromUnboundedService()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var model = new NonCanonTechnicalCoordinateModel();
            var service = CreateService(fixture, model, 24, 4);
            var pool = new WorldRepresentationBufferPool();
            var sourceIdentity = new WorldFeatureId(0xabcUL, 0xdefUL);
            var compiler = new WorldRepresentationCompiler(
                service,
                pool,
                WorldRepresentationBuildProfile.CreateNonCanonTechnicalProofProfile(),
                sourceIdentity);
            var key = new WorldRepresentationKey(
                fixture.World,
                model,
                WorldRepresentationTier.Near,
                250_000L,
                -250_000L,
                144d);
            var result = await compiler.BuildAsync(key, CancellationToken.None);
            try
            {
                Assert.That(result.VertexCount, Is.EqualTo(17 * 17));
                Assert.That(result.SourceFingerprint, Is.EqualTo(sourceIdentity));
                Assert.That(result.GetAbsolutePosition(0).HorizontalA, Is.EqualTo(36_000_000d));
                Assert.That(result.GetAbsolutePosition(0).HorizontalB, Is.EqualTo(-36_000_000d));
                Assert.That(result.GetFeatureId(result.VertexCount / 2).IsEmpty, Is.False);
            }
            finally
            {
                result.Dispose();
            }

            Assert.That(pool.OutstandingLeases, Is.Zero);
        }

        private static UnboundedHybridWorldQueryService CreateService(
            HybridTerrainComparisonPanelExporter.ProofFixture fixture,
            IWorldCoordinateModel model,
            int maximumCanyonPlans,
            int maximumTerrainWindows)
        {
            return new UnboundedHybridWorldQueryService(
                fixture.World,
                model,
                fixture.Context,
                CanyonPlannerProfile.CreateNonCanonTechnicalProofProfile(),
                maximumCanyonPlans,
                maximumTerrainWindows);
        }
    }
}
