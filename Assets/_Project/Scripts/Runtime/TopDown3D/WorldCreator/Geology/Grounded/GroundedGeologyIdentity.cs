using System;

namespace BooterBigArm.TopDown3D.WorldCreator.GroundedGeology
{
    public readonly struct GroundedGeologySeedStream : IEquatable<GroundedGeologySeedStream>
    {
        public GroundedGeologySeedStream(ulong featureSeed, string salt)
        {
            FeatureSeed = featureSeed;
            Salt = WorldStableText.Require(salt, nameof(salt), 128);
            var hash = new WorldStableHashBuilder("grounded-geology-seed-stream-v1");
            hash.Append(featureSeed);
            hash.Append(Salt);
            Seed = hash.Finish64();
        }

        public ulong FeatureSeed { get; }
        public string Salt { get; }
        public ulong Seed { get; }

        public ulong Sample(ulong stableIndex)
        {
            var hash = new WorldStableHashBuilder("grounded-geology-seed-sample-v1");
            hash.Append(Seed);
            hash.Append(stableIndex);
            return hash.Finish64();
        }

        public bool Equals(GroundedGeologySeedStream other)
        {
            return FeatureSeed == other.FeatureSeed
                && Seed == other.Seed
                && string.Equals(Salt, other.Salt, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologySeedStream other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)(Seed ^ (Seed >> 32)));
        }

        public override string ToString()
        {
            return $"{Salt}:{Seed:x16}";
        }
    }

    /// <summary>
    /// Grounded-geology identity derives through World Creator's world, coordinate, version,
    /// namespace, and feature-ID conventions. It never introduces a parallel world identity.
    /// </summary>
    public static class GroundedGeologyIdentity
    {
        public static readonly WorldSeedNamespace SeedNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Landform, "grounded-geology");

        public static WorldFeatureId CreateFeatureId(
            WorldIdentity world,
            WorldCoordinateAddress ownerAddress,
            GroundedGeologyRecipe recipe,
            int featureOrdinal)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (featureOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(featureOrdinal));
            return WorldFeatureId.Create(
                world,
                SeedNamespace,
                ownerAddress,
                $"recipe:{recipe.StableId}:schema:{recipe.SchemaVersion}:plan:{recipe.AlgorithmVersions.Plan}:feature:{featureOrdinal}");
        }

        public static ulong CreateFeatureSeed(
            WorldIdentity world,
            WorldCoordinateAddress ownerAddress,
            GroundedGeologyRecipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            var worldStream = SeedNamespace.DeriveSeed(world, ownerAddress, recipe.SeedSalt);
            var hash = new WorldStableHashBuilder("grounded-geology-feature-seed-v1");
            hash.Append(worldStream);
            hash.Append(recipe.StableId);
            hash.Append(recipe.SchemaVersion);
            hash.Append(recipe.AlgorithmVersions.Plan);
            hash.Append(recipe.SeedSalt);
            return hash.Finish64();
        }

        public static GroundedGeologySeedStream CreateStream(ulong featureSeed, string salt)
        {
            return new GroundedGeologySeedStream(featureSeed, salt);
        }

        public static WorldFeatureId CreateStableSubfeatureId(
            WorldFeatureId parentFeatureId,
            string role,
            string stableCandidateKey)
        {
            if (parentFeatureId.IsEmpty)
            {
                throw new ArgumentException("A stable subfeature requires a parent feature ID.", nameof(parentFeatureId));
            }

            role = WorldStableText.Require(role, nameof(role), 128);
            stableCandidateKey = WorldStableText.Require(stableCandidateKey, nameof(stableCandidateKey), 256);
            var hash = new WorldStableHashBuilder("grounded-geology-subfeature-id-v1");
            hash.Append(parentFeatureId.High);
            hash.Append(parentFeatureId.Low);
            hash.Append(role);
            hash.Append(stableCandidateKey);
            hash.Finish128(out var high, out var low);
            return new WorldFeatureId(high, low);
        }
    }
}
