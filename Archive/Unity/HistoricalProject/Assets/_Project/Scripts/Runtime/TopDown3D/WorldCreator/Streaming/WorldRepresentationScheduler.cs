using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public sealed class WorldRepresentationScheduler : IDisposable
    {
        private readonly object gate = new object();
        private readonly IWorldRepresentationCompiler compiler;
        private readonly WorldRepresentationCache cache;
        private readonly ConcurrentQueue<IntegrationCandidate> integrationQueue =
            new ConcurrentQueue<IntegrationCandidate>();
        private readonly Dictionary<WorldRepresentationKey, long> latestTokens =
            new Dictionary<WorldRepresentationKey, long>();
        private long nextToken;
        private long cacheHits;
        private long cacheMisses;
        private long buildsStarted;
        private long buildsCompleted;
        private long buildsCancelled;
        private long failedBuilds;
        private long integrated;
        private long staleRejected;
        private long cacheRejected;
        private long evicted;
        private int peakQueued;
        private long lastIntegrationTicks;
        private bool disposed;

        public WorldRepresentationScheduler(
            IWorldRepresentationCompiler compiler,
            WorldRepresentationCache cache,
            LocalOriginFrame initialFrame)
        {
            this.compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            CurrentFrame = initialFrame;
        }

        public LocalOriginFrame CurrentFrame { get; private set; }
        public int QueuedIntegrationCount => integrationQueue.Count;

        public bool TryGetIntegrated(
            WorldRepresentationKey key,
            out WorldRepresentationBuildResult result)
        {
            ThrowIfDisposed();
            return cache.TryGet(key, out result);
        }

        public async Task<WorldRepresentationRequestOutcome> RequestAsync(
            WorldRepresentationKey key,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            lock (gate)
            {
                if (cache.TryGet(key, out _))
                {
                    cacheHits++;
                    return new WorldRepresentationRequestOutcome(key, 0L, WorldRepresentationRequestState.CacheHit);
                }

                cacheMisses++;
            }

            var token = Interlocked.Increment(ref nextToken);
            lock (gate)
            {
                latestTokens[key] = token;
                buildsStarted++;
            }

            try
            {
                var result = await compiler.BuildAsync(key, cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Dispose();
                    lock (gate) buildsCancelled++;
                    return new WorldRepresentationRequestOutcome(key, token, WorldRepresentationRequestState.Cancelled);
                }

                integrationQueue.Enqueue(new IntegrationCandidate(token, result));
                lock (gate)
                {
                    buildsCompleted++;
                    peakQueued = Math.Max(peakQueued, integrationQueue.Count);
                }

                return new WorldRepresentationRequestOutcome(
                    key,
                    token,
                    WorldRepresentationRequestState.QueuedForIntegration);
            }
            catch (OperationCanceledException)
            {
                lock (gate) buildsCancelled++;
                return new WorldRepresentationRequestOutcome(key, token, WorldRepresentationRequestState.Cancelled);
            }
            catch (Exception exception)
            {
                lock (gate) failedBuilds++;
                return new WorldRepresentationRequestOutcome(
                    key,
                    token,
                    WorldRepresentationRequestState.Failed,
                    exception.Message);
            }
        }

        public int DrainIntegrationQueue(int maximumItems, TimeSpan maximumDuration)
        {
            ThrowIfDisposed();
            if (maximumItems < 1) throw new ArgumentOutOfRangeException(nameof(maximumItems));
            if (maximumDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumDuration));
            var stopwatch = Stopwatch.StartNew();
            var processed = 0;
            while (processed < maximumItems
                && stopwatch.Elapsed < maximumDuration
                && integrationQueue.TryDequeue(out var candidate))
            {
                processed++;
                var isLatest = false;
                lock (gate)
                {
                    isLatest = latestTokens.TryGetValue(candidate.Result.Key, out var latest)
                        && latest == candidate.Token;
                }

                if (!isLatest)
                {
                    candidate.Result.Dispose();
                    lock (gate) staleRejected++;
                    continue;
                }

                if (!candidate.Result.TryRebase(CurrentFrame))
                {
                    candidate.Result.Dispose();
                    lock (gate) staleRejected++;
                    continue;
                }

                if (!cache.Store(candidate.Result, out var evictionCount))
                {
                    lock (gate) cacheRejected++;
                    continue;
                }

                lock (gate)
                {
                    integrated++;
                    evicted += evictionCount;
                }
            }

            stopwatch.Stop();
            Interlocked.Exchange(ref lastIntegrationTicks, stopwatch.ElapsedTicks);
            return processed;
        }

        public bool TryRebase(LocalOriginFrame nextFrame)
        {
            ThrowIfDisposed();
            var representations = cache.Snapshot();
            var retained = new List<WorldRepresentationBuildResult>(representations.Count);
            var removed = 0;
            for (var i = 0; i < representations.Count; i++)
            {
                if (representations[i].CanRebase(nextFrame))
                {
                    retained.Add(representations[i]);
                }
                else if (cache.Remove(representations[i].Key))
                {
                    removed++;
                }
            }

            for (var i = 0; i < retained.Count; i++)
            {
                if (!retained[i].TryRebase(nextFrame))
                    throw new InvalidOperationException("Validated representation rebase failed.");
            }

            if (removed > 0)
            {
                lock (gate) evicted += removed;
            }

            CurrentFrame = nextFrame;
            return true;
        }

        public void Invalidate(WorldRepresentationKey key)
        {
            ThrowIfDisposed();
            lock (gate) latestTokens[key] = Interlocked.Increment(ref nextToken);
            cache.Remove(key);
        }

        public WorldRepresentationMetricsSnapshot CaptureMetrics()
        {
            lock (gate)
            {
                return new WorldRepresentationMetricsSnapshot(
                    cacheHits,
                    cacheMisses,
                    buildsStarted,
                    buildsCompleted,
                    buildsCancelled,
                    failedBuilds,
                    integrated,
                    staleRejected,
                    cacheRejected,
                    evicted,
                    integrationQueue.Count,
                    peakQueued,
                    Interlocked.Read(ref lastIntegrationTicks));
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            while (integrationQueue.TryDequeue(out var candidate)) candidate.Result.Dispose();
            cache.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(WorldRepresentationScheduler));
        }

        private readonly struct IntegrationCandidate
        {
            public IntegrationCandidate(long token, WorldRepresentationBuildResult result)
            {
                Token = token;
                Result = result;
            }

            public long Token { get; }
            public WorldRepresentationBuildResult Result { get; }
        }
    }
}
