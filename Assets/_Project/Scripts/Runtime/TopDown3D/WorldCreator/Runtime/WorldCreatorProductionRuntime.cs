using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// Owns the topology-v2 production query and representation path. The coordinate and
    /// landscape profile behind this runtime are explicitly disposable non-canon adapters.
    /// </summary>
    public sealed class WorldCreatorProductionRuntime : IDisposable
    {
        private readonly object requestGate = new object();
        private readonly Dictionary<WorldRepresentationKey, Task<WorldRepresentationRequestOutcome>> inFlight =
            new Dictionary<WorldRepresentationKey, Task<WorldRepresentationRequestOutcome>>();
        private readonly CancellationTokenSource lifetimeCancellation = new CancellationTokenSource();
        private readonly WorldRepresentationScheduler scheduler;
        private bool disposed;

        private WorldCreatorProductionRuntime(
            WorldCreatorProductionAuthority authority,
            LocalOriginFrame initialFrame)
        {
            Authority = authority ?? throw new ArgumentNullException(nameof(authority));
            var pool = new WorldRepresentationBufferPool(
                authority.Profile.MaximumRetainedBuffersPerResolution);
            var compiler = new WorldRepresentationCompiler(
                authority.Query,
                pool,
                authority.Profile.CreateRepresentationProfile(),
                authority.SourceFingerprint);
            var cache = new WorldRepresentationCache(
                authority.Profile.MaximumRepresentationEntries,
                authority.Profile.MaximumRepresentationBytes);
            scheduler = new WorldRepresentationScheduler(compiler, cache, initialFrame);
        }

        internal WorldCreatorProductionAuthority Authority { get; }
        public WorldCreatorProductionProfile Profile => Authority.Profile;
        public WorldIdentity Identity => Authority.Identity;
        public IWorldCoordinateModel CoordinateModel => Authority.CoordinateModel;
        public IWorldQueryService Query => Authority.Query;
        public LocalOriginFrame CurrentFrame => scheduler.CurrentFrame;
        public WorldFeatureId SourceFingerprint => Authority.SourceFingerprint;

        public static WorldCreatorProductionRuntime Create(long worldSeed)
        {
            var authority = WorldCreatorProductionAuthorityRegistry.GetOrCreate(worldSeed);
            var origin = new AbsoluteWorldPosition(0d, 0d, 0d);
            var frame = new LocalOriginFrame(
                authority.CoordinateModel.Encode(origin),
                origin,
                authority.Profile.LocalFrameRadius);
            return new WorldCreatorProductionRuntime(authority, frame);
        }

        internal static WorldCreatorProductionAuthority GetSharedAuthority(long worldSeed)
        {
            return WorldCreatorProductionAuthorityRegistry.GetOrCreate(worldSeed);
        }

        public AbsoluteWorldPosition ToAbsolute(float localX, float localY, float localZ)
        {
            ThrowIfDisposed();
            return CurrentFrame.ToAbsolute(new LocalWorldPosition(localX, localY, localZ));
        }

        public bool TryToLocal(AbsoluteWorldPosition absolutePosition, out LocalWorldPosition localPosition)
        {
            ThrowIfDisposed();
            return CurrentFrame.TryToLocal(absolutePosition, out localPosition);
        }

        public WorldRepresentationKey CreateRepresentationKey(
            WorldRepresentationTier tier,
            long tileA,
            long tileB,
            double tileSpan)
        {
            ThrowIfDisposed();
            return new WorldRepresentationKey(
                Identity,
                CoordinateModel,
                tier,
                tileA,
                tileB,
                tileSpan);
        }

        public Task<WorldRepresentationRequestOutcome> RequestAsync(
            WorldRepresentationKey key,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            lock (requestGate)
            {
                if (inFlight.TryGetValue(key, out var existing))
                {
                    return existing;
                }

                var request = scheduler.RequestAsync(
                    key,
                    cancellationToken.CanBeCanceled
                        ? cancellationToken
                        : lifetimeCancellation.Token);
                inFlight.Add(key, request);
                _ = RemoveCompletedRequestAsync(key, request);
                return request;
            }
        }

        public int DrainIntegrationQueue(int maximumItems, TimeSpan maximumDuration)
        {
            ThrowIfDisposed();
            return scheduler.DrainIntegrationQueue(maximumItems, maximumDuration);
        }

        public bool TryGetIntegrated(
            WorldRepresentationKey key,
            out WorldRepresentationBuildResult result)
        {
            ThrowIfDisposed();
            return scheduler.TryGetIntegrated(key, out result);
        }

        public bool TryRebase(AbsoluteWorldPosition nextOrigin)
        {
            ThrowIfDisposed();
            var frame = new LocalOriginFrame(
                CoordinateModel.Encode(nextOrigin),
                nextOrigin,
                Profile.LocalFrameRadius);
            return scheduler.TryRebase(frame);
        }

        public WorldRepresentationMetricsSnapshot CaptureMetrics()
        {
            ThrowIfDisposed();
            return scheduler.CaptureMetrics();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            lifetimeCancellation.Cancel();
            lock (requestGate)
            {
                inFlight.Clear();
            }

            scheduler.Dispose();
            lifetimeCancellation.Dispose();
        }

        private async Task RemoveCompletedRequestAsync(
            WorldRepresentationKey key,
            Task<WorldRepresentationRequestOutcome> request)
        {
            try
            {
                await request.ConfigureAwait(false);
            }
            finally
            {
                lock (requestGate)
                {
                    if (inFlight.TryGetValue(key, out var current) && ReferenceEquals(current, request))
                    {
                        inFlight.Remove(key);
                    }
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldCreatorProductionRuntime));
            }
        }
    }

    internal sealed class WorldCreatorProductionAuthority
    {
        private const int MaximumSurfaceSamples = 65536;
        private static readonly WorldSeedNamespace RuntimeNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Landform, "production-runtime-source");
        private readonly object surfaceGate = new object();
        private readonly Dictionary<SurfaceCacheKey, SurfaceCacheEntry> surfaceSamples =
            new Dictionary<SurfaceCacheKey, SurfaceCacheEntry>();
        private readonly LinkedList<SurfaceCacheKey> surfaceRecency =
            new LinkedList<SurfaceCacheKey>();

        public WorldCreatorProductionAuthority(long worldSeed)
        {
            Profile = WorldCreatorProductionProfile.LoadRequired();
            Identity = new WorldIdentity(worldSeed, Profile.CreateVersionManifest());
            CoordinateModel = new NonCanonTechnicalCoordinateModel();
            var contextProvider = new WorldCoordinateContextSampler(
                Profile.ProvinceCatalog,
                Profile.StrataCatalog,
                Profile.InfluenceProfile);
            Query = new UnboundedHybridWorldQueryService(
                Identity,
                CoordinateModel,
                contextProvider,
                CanyonPlannerProfile.CreateNonCanonTechnicalProofProfile(),
                Profile.MaximumCanyonPlans,
                Profile.MaximumTerrainWindows);
            var originAddress = CoordinateModel.Encode(new AbsoluteWorldPosition(0d, 0d, 0d));
            SourceFingerprint = WorldFeatureId.Create(
                Identity,
                RuntimeNamespace,
                originAddress,
                Profile.InfluenceProfile.StableId);
        }

        public WorldCreatorProductionProfile Profile { get; }
        public WorldIdentity Identity { get; }
        public IWorldCoordinateModel CoordinateModel { get; }
        public UnboundedHybridWorldQueryService Query { get; }
        public WorldFeatureId SourceFingerprint { get; }

        public bool TrySampleSurface(
            AbsoluteWorldPosition position,
            out WorldSurfaceSample sample,
            out string error)
        {
            var key = new SurfaceCacheKey(position.HorizontalA, position.HorizontalB);
            lock (surfaceGate)
            {
                if (surfaceSamples.TryGetValue(key, out var cached))
                {
                    surfaceRecency.Remove(cached.Node);
                    surfaceRecency.AddLast(cached.Node);
                    sample = cached.Sample;
                    error = null;
                    return true;
                }
            }

            if (!Query.TrySampleSurface(position, out sample, out error))
            {
                return false;
            }

            lock (surfaceGate)
            {
                if (!surfaceSamples.ContainsKey(key))
                {
                    var node = surfaceRecency.AddLast(key);
                    surfaceSamples.Add(key, new SurfaceCacheEntry(sample, node));
                    while (surfaceSamples.Count > MaximumSurfaceSamples)
                    {
                        var oldest = surfaceRecency.First;
                        if (oldest == null)
                        {
                            break;
                        }

                        surfaceSamples.Remove(oldest.Value);
                        surfaceRecency.RemoveFirst();
                    }
                }
            }

            return true;
        }

        private readonly struct SurfaceCacheKey : IEquatable<SurfaceCacheKey>
        {
            public SurfaceCacheKey(double horizontalA, double horizontalB)
            {
                HorizontalA = BitConverter.DoubleToInt64Bits(horizontalA == 0d ? 0d : horizontalA);
                HorizontalB = BitConverter.DoubleToInt64Bits(horizontalB == 0d ? 0d : horizontalB);
            }

            private long HorizontalA { get; }
            private long HorizontalB { get; }

            public bool Equals(SurfaceCacheKey other)
            {
                return HorizontalA == other.HorizontalA && HorizontalB == other.HorizontalB;
            }

            public override bool Equals(object obj)
            {
                return obj is SurfaceCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return unchecked(HorizontalA.GetHashCode() * 397 ^ HorizontalB.GetHashCode());
            }
        }

        private sealed class SurfaceCacheEntry
        {
            public SurfaceCacheEntry(
                WorldSurfaceSample sample,
                LinkedListNode<SurfaceCacheKey> node)
            {
                Sample = sample;
                Node = node;
            }

            public WorldSurfaceSample Sample { get; }
            public LinkedListNode<SurfaceCacheKey> Node { get; }
        }
    }

    internal static class WorldCreatorProductionAuthorityRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<long, WorldCreatorProductionAuthority> Authorities =
            new Dictionary<long, WorldCreatorProductionAuthority>();

        public static WorldCreatorProductionAuthority GetOrCreate(long worldSeed)
        {
            lock (Gate)
            {
                if (!Authorities.TryGetValue(worldSeed, out var authority))
                {
                    authority = new WorldCreatorProductionAuthority(worldSeed);
                    Authorities.Add(worldSeed, authority);
                }

                return authority;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            lock (Gate)
            {
                Authorities.Clear();
            }
        }
    }
}
