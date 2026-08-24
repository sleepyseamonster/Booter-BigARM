using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct WorldSurfaceMaterialSample : IEquatable<WorldSurfaceMaterialSample>
    {
        public WorldSurfaceMaterialSample(
            AbsoluteWorldPosition position,
            float strataExposure,
            float erosion,
            float windExposure,
            float shelter,
            float deposit,
            float weathering,
            float sediment,
            float structuralDirection,
            float prevailingWindDirection,
            float slopeDegrees)
        {
            Position = position;
            StrataExposure = RequireNormalized(strataExposure, nameof(strataExposure));
            Erosion = RequireNormalized(erosion, nameof(erosion));
            WindExposure = RequireNormalized(windExposure, nameof(windExposure));
            Shelter = RequireNormalized(shelter, nameof(shelter));
            Deposit = RequireNormalized(deposit, nameof(deposit));
            Weathering = RequireNormalized(weathering, nameof(weathering));
            Sediment = RequireNormalized(sediment, nameof(sediment));
            StructuralDirection = RequireNormalized(structuralDirection, nameof(structuralDirection));
            PrevailingWindDirection = RequireNormalized(
                prevailingWindDirection,
                nameof(prevailingWindDirection));
            if (float.IsNaN(slopeDegrees) || float.IsInfinity(slopeDegrees)
                || slopeDegrees < 0f || slopeDegrees > 90f)
                throw new ArgumentOutOfRangeException(nameof(slopeDegrees));
            SlopeDegrees = slopeDegrees;
        }

        public AbsoluteWorldPosition Position { get; }
        public float StrataExposure { get; }
        public float Erosion { get; }
        public float WindExposure { get; }
        public float Shelter { get; }
        public float Deposit { get; }
        public float Weathering { get; }
        public float Sediment { get; }
        public float StructuralDirection { get; }
        public float PrevailingWindDirection { get; }
        public float SlopeDegrees { get; }

        public bool Equals(WorldSurfaceMaterialSample other)
        {
            return Position.Equals(other.Position)
                && StrataExposure.Equals(other.StrataExposure)
                && Erosion.Equals(other.Erosion)
                && WindExposure.Equals(other.WindExposure)
                && Shelter.Equals(other.Shelter)
                && Deposit.Equals(other.Deposit)
                && Weathering.Equals(other.Weathering)
                && Sediment.Equals(other.Sediment)
                && StructuralDirection.Equals(other.StructuralDirection)
                && PrevailingWindDirection.Equals(other.PrevailingWindDirection)
                && SlopeDegrees.Equals(other.SlopeDegrees);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldSurfaceMaterialSample other && Equals(other);
        }

        public override int GetHashCode() => Position.GetHashCode();

        private static float RequireNormalized(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
                throw new ArgumentOutOfRangeException(parameterName);
            return value == 0f ? 0f : value;
        }
    }

    public interface IWorldSurfaceMaterialService
    {
        bool TrySample(
            AbsoluteWorldPosition position,
            out WorldSurfaceMaterialSample sample,
            out string error);
    }

    /// <summary>
    /// Resolves renderer-independent dry-world surface response from coordinate context and
    /// canonical surface semantics. It contains no texture, shader-channel, or Unity-mesh contract.
    /// </summary>
    public sealed class WorldSurfaceMaterialService : IWorldSurfaceMaterialService
    {
        private const int MaximumCachedSamples = 65536;
        private readonly object gate = new object();
        private readonly WorldIdentity world;
        private readonly IWorldCoordinateModel coordinateModel;
        private readonly IWorldCoordinateContextProvider contextProvider;
        private readonly IWorldQueryService query;
        private readonly Dictionary<CacheKey, CacheEntry> cache =
            new Dictionary<CacheKey, CacheEntry>();
        private readonly LinkedList<CacheKey> recency = new LinkedList<CacheKey>();

        public WorldSurfaceMaterialService(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            IWorldCoordinateContextProvider contextProvider,
            IWorldQueryService query)
        {
            this.world = world;
            this.coordinateModel = coordinateModel ?? throw new ArgumentNullException(nameof(coordinateModel));
            this.contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            this.query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public bool TrySample(
            AbsoluteWorldPosition position,
            out WorldSurfaceMaterialSample sample,
            out string error)
        {
            var key = new CacheKey(position.HorizontalA, position.HorizontalB);
            lock (gate)
            {
                if (cache.TryGetValue(key, out var cached))
                {
                    recency.Remove(cached.Node);
                    recency.AddLast(cached.Node);
                    sample = cached.Sample;
                    error = null;
                    return true;
                }
            }

            var address = coordinateModel.Encode(position);
            if (!contextProvider.TrySample(world, coordinateModel, address, out var context, out error)
                || !query.TrySampleSurface(position, out var surface, out error))
            {
                sample = default;
                return false;
            }

            sample = Resolve(surface, context);
            lock (gate)
            {
                if (!cache.ContainsKey(key))
                {
                    var node = recency.AddLast(key);
                    cache.Add(key, new CacheEntry(sample, node));
                    while (cache.Count > MaximumCachedSamples)
                    {
                        var oldest = recency.First;
                        if (oldest == null) break;
                        cache.Remove(oldest.Value);
                        recency.RemoveFirst();
                    }
                }
            }
            error = null;
            return true;
        }

        private static WorldSurfaceMaterialSample Resolve(
            WorldSurfaceSample surface,
            WorldCoordinateContext context)
        {
            var normalVertical = Clamp01(surface.NormalVertical);
            var slope = (float)(Math.Acos(Math.Max(-1d, Math.Min(1d, normalVertical))) * 180d / Math.PI);
            var slope01 = Clamp01(slope / 58f);
            var canyonFloor = Has(surface.Semantic, WorldSurfaceSemantic.CanyonFloor);
            var canyonShelf = Has(surface.Semantic, WorldSurfaceSemantic.CanyonShelf);
            var canyonWall = Has(surface.Semantic, WorldSurfaceSemantic.CanyonWall);
            var buried = Has(surface.Semantic, WorldSurfaceSemantic.Buried);
            var disturbed = Has(surface.Semantic, WorldSurfaceSemantic.Disturbed);
            var semanticWeathered = Has(surface.Semantic, WorldSurfaceSemantic.Weathered);

            var windRadians = context.Landscape.PrevailingWindDirection * Math.PI * 2d;
            var windA = (float)Math.Cos(windRadians);
            var windB = (float)Math.Sin(windRadians);
            var windFacing = Math.Abs(surface.NormalA * windA + surface.NormalB * windB);
            var canyonShelter = canyonFloor ? 0.42f : canyonShelf ? 0.16f : 0f;
            var windExposure = Clamp01(
                0.16f
                + context.Landscape.WindStrength * 0.46f
                + slope01 * 0.24f
                + windFacing * 0.20f
                - canyonShelter
                - (buried ? 0.28f : 0f));
            var shelter = Clamp01(
                (1f - windExposure) * 0.62f
                + canyonShelter
                + (buried ? 0.28f : 0f));
            var weathering = Clamp01(
                context.Landscape.Weathering * 0.58f
                + context.StrataFracture * 0.18f
                + windExposure * 0.16f
                + (semanticWeathered ? 0.24f : 0f));
            var erosion = Clamp01(
                weathering * 0.48f
                + windExposure * 0.24f
                + context.StrataFracture * 0.20f
                + (disturbed ? 0.20f : 0f));
            var strataExposure = Clamp01(
                context.StrataHardness * 0.24f
                + context.StrataFracture * 0.18f
                + slope01 * 0.42f
                + (canyonWall ? 0.54f : canyonShelf ? 0.24f : 0f)
                - (buried ? 0.42f : 0f));
            var deposit = Clamp01(
                context.Landscape.Sediment * 0.40f
                + shelter * 0.34f
                + (canyonFloor ? 0.34f : 0f)
                + (buried ? 0.34f : 0f)
                + (disturbed ? 0.12f : 0f)
                - slope01 * 0.42f
                - windExposure * 0.12f);

            return new WorldSurfaceMaterialSample(
                surface.Position,
                strataExposure,
                erosion,
                windExposure,
                shelter,
                deposit,
                weathering,
                context.Landscape.Sediment,
                context.Landscape.StructuralDirection,
                context.Landscape.PrevailingWindDirection,
                slope);
        }

        private static bool Has(WorldSurfaceSemantic value, WorldSurfaceSemantic flag)
        {
            return (value & flag) != 0;
        }

        private static float Clamp01(float value)
        {
            return value <= 0f ? 0f : value >= 1f ? 1f : value;
        }

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            public CacheKey(double horizontalA, double horizontalB)
            {
                HorizontalA = BitConverter.DoubleToInt64Bits(horizontalA == 0d ? 0d : horizontalA);
                HorizontalB = BitConverter.DoubleToInt64Bits(horizontalB == 0d ? 0d : horizontalB);
            }

            private long HorizontalA { get; }
            private long HorizontalB { get; }
            public bool Equals(CacheKey other) => HorizontalA == other.HorizontalA && HorizontalB == other.HorizontalB;
            public override bool Equals(object obj) => obj is CacheKey other && Equals(other);
            public override int GetHashCode() => unchecked(HorizontalA.GetHashCode() * 397 ^ HorizontalB.GetHashCode());
        }

        private sealed class CacheEntry
        {
            public CacheEntry(WorldSurfaceMaterialSample sample, LinkedListNode<CacheKey> node)
            {
                Sample = sample;
                Node = node;
            }
            public WorldSurfaceMaterialSample Sample { get; }
            public LinkedListNode<CacheKey> Node { get; }
        }
    }

    internal sealed class QueryDerivedSurfaceMaterialService : IWorldSurfaceMaterialService
    {
        private readonly IWorldQueryService query;

        public QueryDerivedSurfaceMaterialService(IWorldQueryService query)
        {
            this.query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public bool TrySample(
            AbsoluteWorldPosition position,
            out WorldSurfaceMaterialSample sample,
            out string error)
        {
            if (!query.TrySampleSurface(position, out var surface, out error))
            {
                sample = default;
                return false;
            }
            var wall = (surface.Semantic & WorldSurfaceSemantic.CanyonWall) != 0;
            var floor = (surface.Semantic & WorldSurfaceSemantic.CanyonFloor) != 0;
            var shelf = (surface.Semantic & WorldSurfaceSemantic.CanyonShelf) != 0;
            var weathered = (surface.Semantic & WorldSurfaceSemantic.Weathered) != 0;
            var normalVertical = Math.Max(-1f, Math.Min(1f, surface.NormalVertical));
            var slope = (float)(Math.Acos(normalVertical) * 180d / Math.PI);
            sample = new WorldSurfaceMaterialSample(
                surface.Position,
                wall ? 0.9f : shelf ? 0.58f : 0.22f,
                weathered ? 0.84f : 0.42f,
                floor ? 0.18f : 0.58f,
                floor ? 0.76f : 0.30f,
                floor ? 0.72f : 0.18f,
                weathered ? 0.88f : 0.5f,
                floor ? 0.68f : 0.28f,
                0f,
                0f,
                slope);
            error = null;
            return true;
        }
    }
}
