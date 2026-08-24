using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct SavedPlaceRecord : IEquatable<SavedPlaceRecord>
    {
        public SavedPlaceRecord(
            WorldFeatureId savedPlaceId,
            WorldPersistenceManifest manifest,
            WorldCoordinateAddress address,
            bool hasAnchorFeature,
            WorldFeatureId anchorFeatureId)
        {
            if (savedPlaceId.IsEmpty)
            {
                throw new ArgumentException("Saved places require a stable, non-empty identity.", nameof(savedPlaceId));
            }

            if (!string.Equals(manifest.CoordinateModelId, address.ModelId, StringComparison.Ordinal)
                || manifest.CoordinateModelVersion != address.ModelVersion)
            {
                throw new ArgumentException("The saved address does not match the persistence coordinate model.", nameof(address));
            }

            if (hasAnchorFeature == anchorFeatureId.IsEmpty)
            {
                throw new ArgumentException(
                    "Anchor presence and anchor identity must agree.",
                    nameof(anchorFeatureId));
            }

            SavedPlaceId = savedPlaceId;
            Manifest = manifest;
            Address = address;
            HasAnchorFeature = hasAnchorFeature;
            AnchorFeatureId = anchorFeatureId;
        }

        public WorldFeatureId SavedPlaceId { get; }
        public WorldPersistenceManifest Manifest { get; }
        public WorldCoordinateAddress Address { get; }
        public bool HasAnchorFeature { get; }
        public WorldFeatureId AnchorFeatureId { get; }

        public bool Equals(SavedPlaceRecord other)
        {
            return SavedPlaceId.Equals(other.SavedPlaceId)
                && Manifest.Equals(other.Manifest)
                && Address.Equals(other.Address)
                && HasAnchorFeature == other.HasAnchorFeature
                && AnchorFeatureId.Equals(other.AnchorFeatureId);
        }

        public override bool Equals(object obj)
        {
            return obj is SavedPlaceRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = SavedPlaceId.GetHashCode();
                hash = hash * 397 ^ Manifest.GetHashCode();
                hash = hash * 397 ^ Address.GetHashCode();
                hash = hash * 397 ^ HasAnchorFeature.GetHashCode();
                hash = hash * 397 ^ AnchorFeatureId.GetHashCode();
                return hash;
            }
        }
    }
}
