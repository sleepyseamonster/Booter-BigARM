using System;
using System.Threading;
using System.Threading.Tasks;
using BooterBigArm.Editor.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldRepresentationCompilerTests
    {
        [Test]
        public async Task NearMidAndFarRetainCanonicalIdentityWithTierSpecificPolicy()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(true);
            var pool = new WorldRepresentationBufferPool();
            var compiler = CreateCompiler(fixture, pool);
            var near = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Near, 0), CancellationToken.None);
            var mid = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Mid, 0), CancellationToken.None);
            var far = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Far, 0), CancellationToken.None);
            try
            {
                Assert.That(near.SourceFingerprint, Is.EqualTo(fixture.Plan.Fingerprint));
                Assert.That(mid.SourceFingerprint, Is.EqualTo(near.SourceFingerprint));
                Assert.That(far.SourceFingerprint, Is.EqualTo(near.SourceFingerprint));
                Assert.That(near.Resolution, Is.EqualTo(17));
                Assert.That(mid.Resolution, Is.EqualTo(9));
                Assert.That(far.Resolution, Is.EqualTo(5));
                Assert.That(near.HasCollision, Is.True);
                Assert.That(mid.HasCollision, Is.False);
                Assert.That(far.HasCollision, Is.False);
                Assert.That(mid.GetFeatureId(mid.VertexCount / 2), Is.EqualTo(near.GetFeatureId(near.VertexCount / 2)));
                Assert.That(far.GetFeatureId(far.VertexCount / 2), Is.EqualTo(near.GetFeatureId(near.VertexCount / 2)));
                Assert.That(mid.GetHeight(mid.VertexCount / 2), Is.EqualTo(near.GetHeight(near.VertexCount / 2)));
                Assert.That(far.GetHeight(far.VertexCount / 2), Is.EqualTo(near.GetHeight(near.VertexCount / 2)));
            }
            finally
            {
                near.Dispose();
                mid.Dispose();
                far.Dispose();
            }

            Assert.That(pool.OutstandingLeases, Is.Zero);
        }

        [Test]
        public async Task NearDetailNoiseOnlyChangesNearRepresentationHeightAndMaterialSlope()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var query = new HybridTerrainWindowQueryService(fixture.Plan, fixture.Model, fixture.Context, false);
            var position = new AbsoluteWorldPosition(72d, 0d, 72d);
            Assert.That(query.TrySampleSurface(position, out var detailed, out var detailedError), Is.True, detailedError);
            Assert.That(query.TrySampleCoarseSurface(position, out var coarse, out var coarseError), Is.True, coarseError);
            Assert.That(detailed.Semantic, Is.EqualTo(coarse.Semantic));
            Assert.That(detailed.DominantFeatureId, Is.EqualTo(coarse.DominantFeatureId));
            // Current Little Noise is bounded by 0.10 m and Fine Noise by 0.075 m.
            const double maximumNearDetailOffset = 0.175d;
            Assert.That(Math.Abs(detailed.Position.Vertical - coarse.Position.Vertical), Is.LessThanOrEqualTo(maximumNearDetailOffset));
            var largestNearDetailOffset = 0d;
            for (var b = 72d; b <= 84d; b += 3d)
            {
                for (var a = 72d; a <= 84d; a += 3d)
                {
                    var detailPosition = new AbsoluteWorldPosition(a, 0d, b);
                    Assert.That(query.TrySampleSurface(detailPosition, out var detailedBump, out detailedError), Is.True, detailedError);
                    Assert.That(query.TrySampleCoarseSurface(detailPosition, out var coarseBump, out coarseError), Is.True, coarseError);
                    largestNearDetailOffset = Math.Max(
                        largestNearDetailOffset,
                        Math.Abs(detailedBump.Position.Vertical - coarseBump.Position.Vertical));
                    Assert.That(Math.Abs(detailedBump.Position.Vertical - coarseBump.Position.Vertical),
                        Is.LessThanOrEqualTo(maximumNearDetailOffset));
                }
            }
            Assert.That(largestNearDetailOffset, Is.GreaterThan(0.1d));
            var layeredPosition = new AbsoluteWorldPosition(96d, 0d, 33d);
            Assert.That(query.TrySampleSurface(layeredPosition, out var layered, out detailedError), Is.True, detailedError);
            Assert.That(query.TrySampleCoarseSurface(layeredPosition, out var layeredCoarse, out coarseError), Is.True, coarseError);
            Assert.That(Math.Abs(layered.Position.Vertical - layeredCoarse.Position.Vertical), Is.GreaterThan(0.16d));

            var pool = new WorldRepresentationBufferPool();
            var compiler = new WorldRepresentationCompiler(
                query,
                pool,
                WorldRepresentationBuildProfile.CreateNonCanonTechnicalProofProfile(),
                fixture.Plan.Fingerprint);
            var near = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Near, 0), CancellationToken.None);
            var mid = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Mid, 0), CancellationToken.None);
            var far = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Far, 0), CancellationToken.None);
            try
            {
                var nearIndex = near.VertexCount / 2;
                var midIndex = mid.VertexCount / 2;
                var farIndex = far.VertexCount / 2;
                Assert.That(near.GetHeight(nearIndex), Is.EqualTo((float)detailed.Position.Vertical));
                Assert.That(mid.GetHeight(midIndex), Is.EqualTo((float)coarse.Position.Vertical));
                Assert.That(far.GetHeight(farIndex), Is.EqualTo((float)coarse.Position.Vertical));
                var detailedSlope = (float)(Math.Acos(Math.Max(-1f, Math.Min(1f, detailed.NormalVertical))) * 180d / Math.PI);
                var coarseSlope = (float)(Math.Acos(Math.Max(-1f, Math.Min(1f, coarse.NormalVertical))) * 180d / Math.PI);
                Assert.That(near.GetMaterial(nearIndex).SlopeDegrees, Is.EqualTo(detailedSlope));
                Assert.That(mid.GetMaterial(midIndex).SlopeDegrees, Is.EqualTo(coarseSlope));
                Assert.That(far.GetMaterial(farIndex).SlopeDegrees, Is.EqualTo(coarseSlope));
            }
            finally
            {
                near.Dispose();
                mid.Dispose();
                far.Dispose();
            }

            Assert.That(pool.OutstandingLeases, Is.Zero);
        }

        [Test]
        public async Task RebaseChangesOnlyReusableLocalBuffers()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var pool = new WorldRepresentationBufferPool();
            var result = await CreateCompiler(fixture, pool).BuildAsync(
                CreateKey(fixture, WorldRepresentationTier.Near, 1200),
                CancellationToken.None);
            try
            {
                var absolute = result.GetAbsolutePosition(result.VertexCount / 2);
                var feature = result.GetFeatureId(result.VertexCount / 2);
                var firstOrigin = new AbsoluteWorldPosition(172800d, 0d, 0d);
                var firstFrame = new LocalOriginFrame(fixture.Model.Encode(firstOrigin), firstOrigin, 10000d);
                var secondOrigin = new AbsoluteWorldPosition(173376d, 0d, -288d);
                var secondFrame = new LocalOriginFrame(fixture.Model.Encode(secondOrigin), secondOrigin, 10000d);
                Assert.That(result.TryRebase(firstFrame), Is.True);
                var firstLocal = result.GetLocalPosition(result.VertexCount / 2);
                Assert.That(result.TryRebase(secondFrame), Is.True);
                var secondLocal = result.GetLocalPosition(result.VertexCount / 2);
                Assert.That(secondLocal, Is.Not.EqualTo(firstLocal));
                Assert.That(result.GetAbsolutePosition(result.VertexCount / 2), Is.EqualTo(absolute));
                Assert.That(result.GetFeatureId(result.VertexCount / 2), Is.EqualTo(feature));
                Assert.That(result.Key.StableId.IsEmpty, Is.False);
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public async Task CacheEnforcesEntryAndByteBudgetsAndReturnsEvictedBuffers()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var pool = new WorldRepresentationBufferPool();
            var compiler = CreateCompiler(fixture, pool);
            var first = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Near, 0), CancellationToken.None);
            var second = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Near, 1), CancellationToken.None);
            var third = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Near, 2), CancellationToken.None);
            var singleBytes = first.EstimatedBytes;
            using (var cache = new WorldRepresentationCache(2, singleBytes * 2L))
            {
                Assert.That(cache.Store(first, out var evictedA), Is.True);
                Assert.That(cache.Store(second, out var evictedB), Is.True);
                Assert.That(cache.Store(third, out var evictedC), Is.True);
                Assert.That(evictedA + evictedB + evictedC, Is.EqualTo(1));
                Assert.That(cache.Count, Is.EqualTo(2));
                Assert.That(cache.CurrentBytes, Is.LessThanOrEqualTo(cache.MaximumBytes));
                Assert.That(first.IsDisposed, Is.True);
                Assert.That(cache.TryGet(second.Key, out _), Is.True);
                Assert.That(cache.TryGet(first.Key, out _), Is.False);
            }

            Assert.That(pool.OutstandingLeases, Is.Zero);
        }

        [Test]
        public async Task PoolAllocationsRemainBoundedAfterWarmup()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var pool = new WorldRepresentationBufferPool();
            var compiler = CreateCompiler(fixture, pool);
            for (var i = 0; i < 12; i++)
            {
                var result = await compiler.BuildAsync(CreateKey(fixture, WorldRepresentationTier.Near, i), CancellationToken.None);
                result.Dispose();
            }

            Assert.That(pool.TotalSetAllocations, Is.EqualTo(1));
            Assert.That(pool.TotalSetReuses, Is.EqualTo(11));
            Assert.That(pool.OutstandingLeases, Is.Zero);
        }

        private static WorldRepresentationCompiler CreateCompiler(
            HybridTerrainComparisonPanelExporter.ProofFixture fixture,
            WorldRepresentationBufferPool pool)
        {
            return new WorldRepresentationCompiler(
                fixture.Query,
                pool,
                WorldRepresentationBuildProfile.CreateNonCanonTechnicalProofProfile(),
                fixture.Plan.Fingerprint);
        }

        internal static WorldRepresentationKey CreateKey(
            HybridTerrainComparisonPanelExporter.ProofFixture fixture,
            WorldRepresentationTier tier,
            long tileA)
        {
            return new WorldRepresentationKey(fixture.World, fixture.Model, tier, tileA, 0L, 144d);
        }
    }
}
