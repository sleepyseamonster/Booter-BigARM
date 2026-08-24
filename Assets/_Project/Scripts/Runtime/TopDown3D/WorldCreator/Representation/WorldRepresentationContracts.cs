using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public enum WorldRepresentationTier : byte
    {
        Near = 1,
        Mid = 2,
        Far = 3
    }

    public readonly struct WorldRepresentationKey : IEquatable<WorldRepresentationKey>, IComparable<WorldRepresentationKey>
    {
        private static readonly WorldSeedNamespace RepresentationNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Landform, "representation.world-tile");

        public WorldRepresentationKey(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            WorldRepresentationTier tier,
            long tileA,
            long tileB,
            double tileSpan)
        {
            if (coordinateModel == null) throw new ArgumentNullException(nameof(coordinateModel));
            if (double.IsNaN(tileSpan) || double.IsInfinity(tileSpan) || tileSpan <= 0d)
                throw new ArgumentOutOfRangeException(nameof(tileSpan));
            Tier = tier;
            TileA = tileA;
            TileB = tileB;
            TileSpan = tileSpan;
            Minimum = new AbsoluteWorldPosition(checked(tileA * tileSpan), 0d, checked(tileB * tileSpan));
            OwnerAddress = coordinateModel.Encode(new AbsoluteWorldPosition(
                Minimum.HorizontalA + tileSpan * 0.5d,
                0d,
                Minimum.HorizontalB + tileSpan * 0.5d));
            StableId = WorldFeatureId.Create(
                world,
                RepresentationNamespace,
                OwnerAddress,
                $"{(byte)tier}:{tileA}:{tileB}:{tileSpan:R}");
        }

        public WorldRepresentationTier Tier { get; }
        public long TileA { get; }
        public long TileB { get; }
        public double TileSpan { get; }
        public AbsoluteWorldPosition Minimum { get; }
        public WorldCoordinateAddress OwnerAddress { get; }
        public WorldFeatureId StableId { get; }
        public bool IncludesCollision => Tier == WorldRepresentationTier.Near;

        public int CompareTo(WorldRepresentationKey other) => StableId.CompareTo(other.StableId);
        public bool Equals(WorldRepresentationKey other)
        {
            return Tier == other.Tier
                && TileA == other.TileA
                && TileB == other.TileB
                && TileSpan.Equals(other.TileSpan)
                && OwnerAddress.Equals(other.OwnerAddress)
                && StableId.Equals(other.StableId);
        }

        public override bool Equals(object obj) => obj is WorldRepresentationKey other && Equals(other);
        public override int GetHashCode() => StableId.GetHashCode();
        public override string ToString() => $"{Tier}:{TileA},{TileB}:{TileSpan:R}:{StableId}";
    }

    public readonly struct WorldRepresentationBuildProfile
    {
        public WorldRepresentationBuildProfile(int nearResolution, int midResolution, int farResolution)
        {
            NearResolution = RequireResolution(nearResolution, nameof(nearResolution));
            MidResolution = RequireResolution(midResolution, nameof(midResolution));
            FarResolution = RequireResolution(farResolution, nameof(farResolution));
            if (!(NearResolution > MidResolution && MidResolution > FarResolution))
                throw new ArgumentException("Representation resolutions must strictly decrease from near to far.");
        }

        public int NearResolution { get; }
        public int MidResolution { get; }
        public int FarResolution { get; }

        public int ResolutionFor(WorldRepresentationTier tier)
        {
            return tier switch
            {
                WorldRepresentationTier.Near => NearResolution,
                WorldRepresentationTier.Mid => MidResolution,
                WorldRepresentationTier.Far => FarResolution,
                _ => throw new ArgumentOutOfRangeException(nameof(tier))
            };
        }

        public static WorldRepresentationBuildProfile CreateNonCanonTechnicalProofProfile()
            => new WorldRepresentationBuildProfile(17, 9, 5);

        private static int RequireResolution(int value, string parameterName)
        {
            if (value < 3 || value > 257 || (value & 1) == 0)
                throw new ArgumentOutOfRangeException(parameterName, "Grid resolution must be an odd number inside [3, 257].");
            return value;
        }
    }

    public enum WorldRepresentationRequestState : byte
    {
        CacheHit = 1,
        QueuedForIntegration = 2,
        Cancelled = 3,
        Failed = 4
    }

    public readonly struct WorldRepresentationRequestOutcome
    {
        public WorldRepresentationRequestOutcome(
            WorldRepresentationKey key,
            long requestToken,
            WorldRepresentationRequestState state,
            string error = null)
        {
            Key = key;
            RequestToken = requestToken;
            State = state;
            Error = error ?? string.Empty;
        }

        public WorldRepresentationKey Key { get; }
        public long RequestToken { get; }
        public WorldRepresentationRequestState State { get; }
        public string Error { get; }
    }

    public readonly struct WorldRepresentationMetricsSnapshot
    {
        public WorldRepresentationMetricsSnapshot(
            long cacheHits,
            long cacheMisses,
            long buildsStarted,
            long buildsCompleted,
            long buildsCancelled,
            long failedBuilds,
            long integrated,
            long staleRejected,
            long cacheRejected,
            long evicted,
            int queued,
            int peakQueued,
            long lastIntegrationTicks)
        {
            CacheHits = cacheHits;
            CacheMisses = cacheMisses;
            BuildsStarted = buildsStarted;
            BuildsCompleted = buildsCompleted;
            BuildsCancelled = buildsCancelled;
            FailedBuilds = failedBuilds;
            Integrated = integrated;
            StaleRejected = staleRejected;
            CacheRejected = cacheRejected;
            Evicted = evicted;
            Queued = queued;
            PeakQueued = peakQueued;
            LastIntegrationTicks = lastIntegrationTicks;
        }

        public long CacheHits { get; }
        public long CacheMisses { get; }
        public long BuildsStarted { get; }
        public long BuildsCompleted { get; }
        public long BuildsCancelled { get; }
        public long FailedBuilds { get; }
        public long Integrated { get; }
        public long StaleRejected { get; }
        public long CacheRejected { get; }
        public long Evicted { get; }
        public int Queued { get; }
        public int PeakQueued { get; }
        public long LastIntegrationTicks { get; }
    }
}
