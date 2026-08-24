using System;
using System.Threading;
using System.Threading.Tasks;
using BooterBigArm.Editor.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldRepresentationSchedulerTests
    {
        [Test]
        public async Task NewerRequestRejectsStaleResultAndThenServesCacheHit()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var pool = new WorldRepresentationBufferPool();
            var compiler = CreateCompiler(fixture, pool);
            using var cache = new WorldRepresentationCache(8, 512 * 1024L);
            using var scheduler = new WorldRepresentationScheduler(compiler, cache, CreateFrame(fixture, 0d));
            var key = WorldRepresentationCompilerTests.CreateKey(fixture, WorldRepresentationTier.Near, 0);
            var outcomes = await Task.WhenAll(
                scheduler.RequestAsync(key, CancellationToken.None),
                scheduler.RequestAsync(key, CancellationToken.None));
            Assert.That(outcomes[0].State, Is.EqualTo(WorldRepresentationRequestState.QueuedForIntegration));
            Assert.That(outcomes[1].State, Is.EqualTo(WorldRepresentationRequestState.QueuedForIntegration));
            Assert.That(scheduler.DrainIntegrationQueue(4, TimeSpan.FromMilliseconds(20d)), Is.EqualTo(2));
            var metrics = scheduler.CaptureMetrics();
            Assert.That(metrics.StaleRejected, Is.EqualTo(1));
            Assert.That(metrics.Integrated, Is.EqualTo(1));
            Assert.That(cache.TryGet(key, out var result), Is.True);
            Assert.That(result.HasLocalFrame, Is.True);
            var cacheOutcome = await scheduler.RequestAsync(key, CancellationToken.None);
            Assert.That(cacheOutcome.State, Is.EqualTo(WorldRepresentationRequestState.CacheHit));
        }

        [Test]
        public async Task CancellationReturnsWithoutQueueingOrLeakingBuffers()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var delayed = new CancellationOnlyCompiler();
            using var cache = new WorldRepresentationCache(4, 256 * 1024L);
            using var scheduler = new WorldRepresentationScheduler(delayed, cache, CreateFrame(fixture, 0d));
            using var cancellation = new CancellationTokenSource();
            var task = scheduler.RequestAsync(
                WorldRepresentationCompilerTests.CreateKey(fixture, WorldRepresentationTier.Near, 0),
                cancellation.Token);
            await delayed.Started.Task;
            cancellation.Cancel();
            var outcome = await task;
            Assert.That(outcome.State, Is.EqualTo(WorldRepresentationRequestState.Cancelled));
            Assert.That(scheduler.QueuedIntegrationCount, Is.Zero);
            Assert.That(scheduler.CaptureMetrics().BuildsCancelled, Is.EqualTo(1));
        }

        [Test]
        public async Task InFlightAbsoluteKeyIntegratesAgainstLatestLocalFrame()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var pool = new WorldRepresentationBufferPool();
            var gated = new GatedCompiler(CreateCompiler(fixture, pool));
            using var cache = new WorldRepresentationCache(4, 256 * 1024L);
            using var scheduler = new WorldRepresentationScheduler(gated, cache, CreateFrame(fixture, 0d));
            var key = WorldRepresentationCompilerTests.CreateKey(fixture, WorldRepresentationTier.Near, 2);
            var request = scheduler.RequestAsync(key, CancellationToken.None);
            await gated.Started.Task;
            var shifted = CreateFrame(fixture, 288d);
            Assert.That(scheduler.TryRebase(shifted), Is.True);
            gated.Release.TrySetResult(true);
            var outcome = await request;
            Assert.That(outcome.Key, Is.EqualTo(key));
            scheduler.DrainIntegrationQueue(1, TimeSpan.FromMilliseconds(20d));
            Assert.That(cache.TryGet(key, out var result), Is.True);
            Assert.That(result.Key, Is.EqualTo(key));
            Assert.That(result.LocalFrame, Is.EqualTo(shifted));
        }

        [Test]
        public async Task MainThreadIntegrationHonorsItemBudget()
        {
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(false);
            var pool = new WorldRepresentationBufferPool();
            using var cache = new WorldRepresentationCache(8, 512 * 1024L);
            using var scheduler = new WorldRepresentationScheduler(CreateCompiler(fixture, pool), cache, CreateFrame(fixture, 0d));
            await Task.WhenAll(
                scheduler.RequestAsync(WorldRepresentationCompilerTests.CreateKey(fixture, WorldRepresentationTier.Near, 0)),
                scheduler.RequestAsync(WorldRepresentationCompilerTests.CreateKey(fixture, WorldRepresentationTier.Mid, 0)),
                scheduler.RequestAsync(WorldRepresentationCompilerTests.CreateKey(fixture, WorldRepresentationTier.Far, 0)));
            Assert.That(scheduler.QueuedIntegrationCount, Is.EqualTo(3));
            Assert.That(scheduler.DrainIntegrationQueue(1, TimeSpan.FromMilliseconds(20d)), Is.EqualTo(1));
            Assert.That(scheduler.QueuedIntegrationCount, Is.EqualTo(2));
            Assert.That(cache.Count, Is.EqualTo(1));
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

        private static LocalOriginFrame CreateFrame(
            HybridTerrainComparisonPanelExporter.ProofFixture fixture,
            double horizontalA)
        {
            var position = new AbsoluteWorldPosition(horizontalA, 0d, 0d);
            return new LocalOriginFrame(fixture.Model.Encode(position), position, 10000d);
        }

        private sealed class CancellationOnlyCompiler : IWorldRepresentationCompiler
        {
            public TaskCompletionSource<bool> Started { get; } =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public async Task<WorldRepresentationBuildResult> BuildAsync(
                WorldRepresentationKey key,
                CancellationToken cancellationToken)
            {
                Started.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken);
                throw new InvalidOperationException("Unreachable cancellation test continuation.");
            }
        }

        private sealed class GatedCompiler : IWorldRepresentationCompiler
        {
            private readonly IWorldRepresentationCompiler inner;

            public GatedCompiler(IWorldRepresentationCompiler inner) => this.inner = inner;
            public TaskCompletionSource<bool> Started { get; } =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Release { get; } =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public async Task<WorldRepresentationBuildResult> BuildAsync(
                WorldRepresentationKey key,
                CancellationToken cancellationToken)
            {
                Started.TrySetResult(true);
                await Release.Task;
                return await inner.BuildAsync(key, cancellationToken);
            }
        }
    }
}
