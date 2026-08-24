using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct WorldIdentity : IEquatable<WorldIdentity>
    {
        public WorldIdentity(long seed, WorldVersionManifest versions)
        {
            Seed = seed;
            Versions = versions;
        }

        public long Seed { get; }
        public WorldVersionManifest Versions { get; }

        public bool Equals(WorldIdentity other)
        {
            return Seed == other.Seed && Versions.Equals(other.Versions);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new WorldStableHashBuilder("world-identity-v1");
            AppendTo(ref hash);
            return hash.FinishHashCode();
        }

        public override string ToString()
        {
            return $"seed={Seed};{Versions}";
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append(Seed);
            Versions.AppendTo(ref hash);
        }
    }
}
