using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct WorldSeedNamespace : IEquatable<WorldSeedNamespace>, IComparable<WorldSeedNamespace>
    {
        public WorldSeedNamespace(WorldVersionDomain versionDomain, string name)
        {
            VersionDomain = versionDomain;
            Name = WorldStableText.Require(name, nameof(name), 128);
            _ = GetDomainSortOrder(versionDomain);
        }

        public WorldVersionDomain VersionDomain { get; }
        public string Name { get; }

        public ulong DeriveSeed(WorldIdentity world, WorldCoordinateAddress address, ulong streamIndex = 0UL)
        {
            var hash = new WorldStableHashBuilder("world-seed-stream-v1");
            hash.Append(world.Seed);
            AppendTo(ref hash);
            AppendIdentityVersions(world.Versions, ref hash);
            address.AppendTo(ref hash);
            hash.Append(streamIndex);
            return hash.Finish64();
        }

        public int CompareTo(WorldSeedNamespace other)
        {
            var domainComparison = GetDomainSortOrder(VersionDomain).CompareTo(GetDomainSortOrder(other.VersionDomain));
            return domainComparison != 0 ? domainComparison : string.CompareOrdinal(Name, other.Name);
        }

        public bool Equals(WorldSeedNamespace other)
        {
            return VersionDomain == other.VersionDomain
                && string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldSeedNamespace other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new WorldStableHashBuilder("world-seed-namespace-v1");
            AppendTo(ref hash);
            return hash.FinishHashCode();
        }

        public override string ToString()
        {
            return $"{VersionDomain}:{Name}";
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append((byte)VersionDomain);
            hash.Append(Name);
        }

        internal void AppendIdentityVersions(WorldVersionManifest versions, ref WorldStableHashBuilder hash)
        {
            // Every spatial identity depends on the coordinate and topology foundations.
            // Its own domain remains isolated from unrelated material/content version changes.
            hash.Append(versions.Coordinate);
            hash.Append(versions.Topology);
            if (VersionDomain != WorldVersionDomain.Coordinate
                && VersionDomain != WorldVersionDomain.Topology)
            {
                hash.Append(versions.GetVersion(VersionDomain));
            }
        }

        private static int GetDomainSortOrder(WorldVersionDomain domain)
        {
            return domain switch
            {
                WorldVersionDomain.Topology => 1,
                WorldVersionDomain.Coordinate => 2,
                WorldVersionDomain.Landform => 3,
                WorldVersionDomain.Material => 4,
                WorldVersionDomain.Decoration => 5,
                WorldVersionDomain.Resource => 6,
                WorldVersionDomain.Site => 7,
                _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown world version domain.")
            };
        }
    }
}
