using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct WorldPlanKey : IEquatable<WorldPlanKey>, IComparable<WorldPlanKey>
    {
        public WorldPlanKey(
            WorldIdentity world,
            WorldSeedNamespace seedNamespace,
            WorldCoordinateAddress ownerAddress,
            string planKind,
            ulong variant = 0UL)
        {
            WorldSeed = world.Seed;
            SeedNamespace = seedNamespace;
            CoordinateVersion = world.Versions.Coordinate;
            TopologyVersion = world.Versions.Topology;
            DomainVersion = world.Versions.GetVersion(seedNamespace.VersionDomain);
            OwnerAddress = ownerAddress;
            PlanKind = WorldStableText.Require(planKind, nameof(planKind), 128);
            Variant = variant;
            StableId = default;

            var hash = new WorldStableHashBuilder("world-plan-key-v1");
            AppendTo(ref hash);
            hash.Finish128(out var high, out var low);
            StableId = new WorldFeatureId(high, low);
        }

        public long WorldSeed { get; }
        public WorldSeedNamespace SeedNamespace { get; }
        public int CoordinateVersion { get; }
        public int TopologyVersion { get; }
        public int DomainVersion { get; }
        public WorldCoordinateAddress OwnerAddress { get; }
        public string PlanKind { get; }
        public ulong Variant { get; }
        public WorldFeatureId StableId { get; }

        public int CompareTo(WorldPlanKey other)
        {
            var seedComparison = WorldSeed.CompareTo(other.WorldSeed);
            if (seedComparison != 0)
            {
                return seedComparison;
            }

            var namespaceComparison = SeedNamespace.CompareTo(other.SeedNamespace);
            if (namespaceComparison != 0)
            {
                return namespaceComparison;
            }

            var versionComparison = CoordinateVersion.CompareTo(other.CoordinateVersion);
            if (versionComparison != 0)
            {
                return versionComparison;
            }

            versionComparison = TopologyVersion.CompareTo(other.TopologyVersion);
            if (versionComparison != 0)
            {
                return versionComparison;
            }

            versionComparison = DomainVersion.CompareTo(other.DomainVersion);
            if (versionComparison != 0)
            {
                return versionComparison;
            }

            var addressComparison = OwnerAddress.CompareTo(other.OwnerAddress);
            if (addressComparison != 0)
            {
                return addressComparison;
            }

            var kindComparison = string.CompareOrdinal(PlanKind, other.PlanKind);
            return kindComparison != 0 ? kindComparison : Variant.CompareTo(other.Variant);
        }

        public bool Equals(WorldPlanKey other)
        {
            return WorldSeed == other.WorldSeed
                && CoordinateVersion == other.CoordinateVersion
                && TopologyVersion == other.TopologyVersion
                && DomainVersion == other.DomainVersion
                && Variant == other.Variant
                && SeedNamespace.Equals(other.SeedNamespace)
                && OwnerAddress.Equals(other.OwnerAddress)
                && string.Equals(PlanKind, other.PlanKind, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPlanKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StableId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{StableId}:{SeedNamespace}:{PlanKind}:{Variant}:{OwnerAddress}";
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append(WorldSeed);
            SeedNamespace.AppendTo(ref hash);
            hash.Append(CoordinateVersion);
            hash.Append(TopologyVersion);
            hash.Append(DomainVersion);
            OwnerAddress.AppendTo(ref hash);
            hash.Append(PlanKind);
            hash.Append(Variant);
        }
    }

    public readonly struct WorldPlanBoundaryKey : IEquatable<WorldPlanBoundaryKey>
    {
        public WorldPlanBoundaryKey(WorldPlanKey first, WorldPlanKey second)
        {
            if (first.Equals(second))
            {
                throw new ArgumentException("A plan cannot form a boundary with itself.", nameof(second));
            }

            if (first.WorldSeed != second.WorldSeed
                || first.CoordinateVersion != second.CoordinateVersion
                || first.TopologyVersion != second.TopologyVersion
                || first.DomainVersion != second.DomainVersion
                || first.Variant != second.Variant
                || !first.SeedNamespace.Equals(second.SeedNamespace)
                || !string.Equals(first.PlanKind, second.PlanKind, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A boundary requires two distinct owners from the same world, namespace, version, plan kind, and variant.",
                    nameof(second));
            }

            if (first.CompareTo(second) < 0)
            {
                Owner = first;
                Neighbor = second;
            }
            else
            {
                Owner = second;
                Neighbor = first;
            }

            var hash = new WorldStableHashBuilder("world-plan-boundary-key-v1");
            Owner.AppendTo(ref hash);
            Neighbor.AppendTo(ref hash);
            hash.Finish128(out var high, out var low);
            StableId = new WorldFeatureId(high, low);
        }

        public WorldPlanKey Owner { get; }
        public WorldPlanKey Neighbor { get; }
        public WorldFeatureId StableId { get; }

        public bool Equals(WorldPlanBoundaryKey other)
        {
            return Owner.Equals(other.Owner) && Neighbor.Equals(other.Neighbor);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPlanBoundaryKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StableId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{StableId}:{Owner.StableId}<->{Neighbor.StableId}";
        }
    }
}
