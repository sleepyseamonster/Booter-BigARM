using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct WorldCoordinateAddress : IEquatable<WorldCoordinateAddress>, IComparable<WorldCoordinateAddress>
    {
        public WorldCoordinateAddress(string modelId, int modelVersion, string canonicalValue)
        {
            ModelId = WorldStableText.Require(modelId, nameof(modelId), 128);
            if (modelVersion < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(modelVersion), modelVersion, "Coordinate model versions cannot be negative.");
            }

            ModelVersion = modelVersion;
            CanonicalValue = WorldStableText.Require(canonicalValue, nameof(canonicalValue), 4096);
        }

        public string ModelId { get; }
        public int ModelVersion { get; }
        public string CanonicalValue { get; }

        public bool IsCompatibleWith(IWorldCoordinateModel model)
        {
            if (model == null)
            {
                return false;
            }

            return string.Equals(ModelId, model.ModelId, StringComparison.Ordinal)
                && ModelVersion == model.ModelVersion;
        }

        public int CompareTo(WorldCoordinateAddress other)
        {
            var modelComparison = string.CompareOrdinal(ModelId, other.ModelId);
            if (modelComparison != 0)
            {
                return modelComparison;
            }

            var versionComparison = ModelVersion.CompareTo(other.ModelVersion);
            return versionComparison != 0
                ? versionComparison
                : string.CompareOrdinal(CanonicalValue, other.CanonicalValue);
        }

        public bool Equals(WorldCoordinateAddress other)
        {
            return ModelVersion == other.ModelVersion
                && string.Equals(ModelId, other.ModelId, StringComparison.Ordinal)
                && string.Equals(CanonicalValue, other.CanonicalValue, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldCoordinateAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new WorldStableHashBuilder("world-coordinate-address-v1");
            AppendTo(ref hash);
            return hash.FinishHashCode();
        }

        public override string ToString()
        {
            return $"{ModelId}@{ModelVersion}:{CanonicalValue}";
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append(ModelId);
            hash.Append(ModelVersion);
            hash.Append(CanonicalValue);
        }
    }
}
