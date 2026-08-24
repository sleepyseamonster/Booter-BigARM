using System;
using System.Globalization;
using System.Text;
using System.Threading;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator
{
    public static class WorldRepresentationStressTransect
    {
        public const string DefaultReportPath = "/tmp/booter-worldcreator-batch5-streaming-report.txt";

        [MenuItem("Booter & BigARM/World Creator/Export Batch 5 Non-Canon Streaming Transect")]
        public static void ExportFromMenu() => ExportFromCli();

        public static void ExportFromCli()
        {
            var report = RunProof();
            System.IO.File.WriteAllText(DefaultReportPath, report.Text, new UTF8Encoding(false));
            Debug.Log($"Wrote non-canon Batch 5 representation/streaming evidence to {DefaultReportPath}.\n{report.Text}");
        }

        public static StressProofResult RunProof(int tileCount = 23)
        {
            if (tileCount < 9 || tileCount > 65) throw new ArgumentOutOfRangeException(nameof(tileCount));
            var fixture = HybridTerrainComparisonPanelExporter.BuildProofFixture(true);
            var pool = new WorldRepresentationBufferPool(4);
            var profile = WorldRepresentationBuildProfile.CreateNonCanonTechnicalProofProfile();
            var compiler = new WorldRepresentationCompiler(fixture.Query, pool, profile, fixture.Plan.Fingerprint);
            var cache = new WorldRepresentationCache(12, 512 * 1024L);
            var origin = new AbsoluteWorldPosition(0d, 0d, 0d);
            var frame = new LocalOriginFrame(fixture.Model.Encode(origin), origin, 10000d);
            WorldRepresentationMetricsSnapshot metrics;
            int allocationsAfterFirstPass;
            int allocationsAfterSecondPass;
            long peakCacheBytes;
            using (var scheduler = new WorldRepresentationScheduler(compiler, cache, frame))
            {
                peakCacheBytes = 0L;
                RunPass(scheduler, cache, fixture, tileCount, false, ref peakCacheBytes);
                allocationsAfterFirstPass = pool.TotalSetAllocations;
                RunPass(scheduler, cache, fixture, tileCount, true, ref peakCacheBytes);
                allocationsAfterSecondPass = pool.TotalSetAllocations;

                var shiftedOrigin = new AbsoluteWorldPosition(1152d, 0d, -576d);
                var shiftedFrame = new LocalOriginFrame(
                    fixture.Model.Encode(shiftedOrigin),
                    shiftedOrigin,
                    10000d);
                if (!scheduler.TryRebase(shiftedFrame))
                    throw new InvalidOperationException("The stress-transect cache could not rebase inside its declared local range.");
                metrics = scheduler.CaptureMetrics();
            }

            var builder = new StringBuilder();
            builder.AppendLine("NON-CANON WORLD CREATOR BATCH 5 REPRESENTATION AND STREAMING REPORT");
            builder.AppendLine("Dormant technical evidence only; production scene, terrain, coordinate regions, and lore remain unchanged.");
            builder.Append("transectMetres=").Append(F((tileCount - 1) * 144d))
                .Append(", tiers=near/mid/far, requests=").Append(tileCount * 3 * 2).AppendLine();
            builder.Append("buildsStarted=").Append(metrics.BuildsStarted)
                .Append(", buildsCompleted=").Append(metrics.BuildsCompleted)
                .Append(", integrated=").Append(metrics.Integrated)
                .Append(", cacheHits=").Append(metrics.CacheHits)
                .Append(", evicted=").Append(metrics.Evicted)
                .Append(", peakQueued=").Append(metrics.PeakQueued).AppendLine();
            builder.Append("poolAllocationsFirstPass=").Append(allocationsAfterFirstPass)
                .Append(", poolAllocationsSecondPass=").Append(allocationsAfterSecondPass)
                .Append(", poolReuses=").Append(pool.TotalSetReuses)
                .Append(", leasesAfterDispose=").Append(pool.OutstandingLeases).AppendLine();
            builder.Append("peakCacheBytes=").Append(peakCacheBytes)
                .Append(", cacheBudgetBytes=").Append(512 * 1024L)
                .Append(", lastIntegrationTicks=").Append(metrics.LastIntegrationTicks).AppendLine();
            builder.AppendLine("collision policy: near only; mid/far representations are non-colliding");
            builder.AppendLine("identity: every tier retains the same canonical source fingerprint and absolute feature samples");
            builder.AppendLine("rebase: cached local buffers changed frame without changing absolute keys, source identity, or in-flight authority");
            builder.AppendLine("persisted deltas: not applicable in Batch 5; representations are disposable views of canonical immutable plans");
            builder.AppendLine("performance: technical counters only; hardware-specific frame and memory acceptance remains an explicit production gate");
            return new StressProofResult(
                builder.ToString(),
                allocationsAfterFirstPass,
                allocationsAfterSecondPass,
                pool.TotalSetReuses,
                pool.OutstandingLeases,
                peakCacheBytes,
                metrics);
        }

        private static void RunPass(
            WorldRepresentationScheduler scheduler,
            WorldRepresentationCache cache,
            HybridTerrainComparisonPanelExporter.ProofFixture fixture,
            int tileCount,
            bool reverse,
            ref long peakCacheBytes)
        {
            for (var offset = 0; offset < tileCount; offset++)
            {
                var index = reverse ? tileCount - offset - 1 : offset;
                var tileA = index - tileCount / 2;
                foreach (WorldRepresentationTier tier in Enum.GetValues(typeof(WorldRepresentationTier)))
                {
                    var key = new WorldRepresentationKey(fixture.World, fixture.Model, tier, tileA, 0L, 144d);
                    var outcome = scheduler.RequestAsync(key, CancellationToken.None).GetAwaiter().GetResult();
                    if (outcome.State == WorldRepresentationRequestState.Failed)
                        throw new InvalidOperationException(outcome.Error);
                    scheduler.DrainIntegrationQueue(1, TimeSpan.FromMilliseconds(2d));
                    peakCacheBytes = Math.Max(peakCacheBytes, cache.CurrentBytes);
                }
            }

            while (scheduler.QueuedIntegrationCount > 0)
            {
                scheduler.DrainIntegrationQueue(2, TimeSpan.FromMilliseconds(2d));
                peakCacheBytes = Math.Max(peakCacheBytes, cache.CurrentBytes);
            }
        }

        private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        public readonly struct StressProofResult
        {
            public StressProofResult(
                string text,
                int allocationsAfterFirstPass,
                int allocationsAfterSecondPass,
                int reuses,
                int outstandingAfterDispose,
                long peakCacheBytes,
                WorldRepresentationMetricsSnapshot metrics)
            {
                Text = text;
                AllocationsAfterFirstPass = allocationsAfterFirstPass;
                AllocationsAfterSecondPass = allocationsAfterSecondPass;
                Reuses = reuses;
                OutstandingAfterDispose = outstandingAfterDispose;
                PeakCacheBytes = peakCacheBytes;
                Metrics = metrics;
            }

            public string Text { get; }
            public int AllocationsAfterFirstPass { get; }
            public int AllocationsAfterSecondPass { get; }
            public int Reuses { get; }
            public int OutstandingAfterDispose { get; }
            public long PeakCacheBytes { get; }
            public WorldRepresentationMetricsSnapshot Metrics { get; }
        }
    }
}
