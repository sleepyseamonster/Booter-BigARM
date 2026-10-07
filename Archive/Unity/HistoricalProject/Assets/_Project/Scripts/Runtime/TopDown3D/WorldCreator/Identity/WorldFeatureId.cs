using System;
using System.Globalization;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct WorldFeatureId : IEquatable<WorldFeatureId>, IComparable<WorldFeatureId>
    {
        public static readonly WorldFeatureId Empty = default;

        public WorldFeatureId(ulong high, ulong low)
        {
            High = high;
            Low = low;
        }

        public ulong High { get; }
        public ulong Low { get; }
        public bool IsEmpty => High == 0UL && Low == 0UL;

        public static WorldFeatureId Create(
            WorldIdentity world,
            WorldSeedNamespace seedNamespace,
            WorldCoordinateAddress address,
            string stableLocalKey)
        {
            var hash = new WorldStableHashBuilder("world-feature-id-v1");
            hash.Append(world.Seed);
            seedNamespace.AppendTo(ref hash);
            seedNamespace.AppendIdentityVersions(world.Versions, ref hash);
            address.AppendTo(ref hash);
            hash.Append(WorldStableText.Require(stableLocalKey, nameof(stableLocalKey)));
            hash.Finish128(out var high, out var low);
            return new WorldFeatureId(high, low);
        }

        public static bool TryParse(string value, out WorldFeatureId featureId)
        {
            featureId = Empty;
            if (value == null || value.Length != 32
                || !ulong.TryParse(value.Substring(0, 16), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var high)
                || !ulong.TryParse(value.Substring(16, 16), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var low))
            {
                return false;
            }

            featureId = new WorldFeatureId(high, low);
            return true;
        }

        public int CompareTo(WorldFeatureId other)
        {
            var highComparison = High.CompareTo(other.High);
            return highComparison != 0 ? highComparison : Low.CompareTo(other.Low);
        }

        public bool Equals(WorldFeatureId other)
        {
            return High == other.High && Low == other.Low;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldFeatureId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)(High ^ (High >> 32) ^ Low ^ (Low >> 32)));
        }

        public override string ToString()
        {
            return High.ToString("x16", CultureInfo.InvariantCulture)
                + Low.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
