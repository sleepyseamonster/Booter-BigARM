using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct OptionalThematicCoordinatePayload : IEquatable<OptionalThematicCoordinatePayload>
    {
        public OptionalThematicCoordinatePayload(
            bool hasValue,
            string authorityId,
            int version,
            string payload)
        {
            if (!hasValue)
            {
                HasValue = false;
                AuthorityId = string.Empty;
                Version = 0;
                Payload = string.Empty;
                return;
            }
            HasValue = true;
            AuthorityId = WorldStableText.Require(authorityId, nameof(authorityId), 128);
            if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
            Version = version;
            Payload = WorldStableText.Require(payload, nameof(payload), 4096);
        }

        public bool HasValue { get; }
        public string AuthorityId { get; }
        public int Version { get; }
        public string Payload { get; }
        public static OptionalThematicCoordinatePayload None => default;
        public bool Equals(OptionalThematicCoordinatePayload other)
            => HasValue == other.HasValue
                && Version == other.Version
                && string.Equals(AuthorityId, other.AuthorityId, StringComparison.Ordinal)
                && string.Equals(Payload, other.Payload, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is OptionalThematicCoordinatePayload other && Equals(other);
        public override int GetHashCode() => unchecked(HasValue.GetHashCode() * 397 ^ Version);
    }

    public readonly struct WorldFeatureReference : IEquatable<WorldFeatureReference>
    {
        public WorldFeatureReference(string role, WorldFeatureId featureId)
        {
            Role = WorldStableText.Require(role, nameof(role), 64);
            if (featureId.IsEmpty) throw new ArgumentException("Feature references require identity.", nameof(featureId));
            FeatureId = featureId;
        }

        public string Role { get; }
        public WorldFeatureId FeatureId { get; }
        public bool Equals(WorldFeatureReference other)
            => string.Equals(Role, other.Role, StringComparison.Ordinal) && FeatureId.Equals(other.FeatureId);
        public override bool Equals(object obj) => obj is WorldFeatureReference other && Equals(other);
        public override int GetHashCode() => unchecked(StringComparer.Ordinal.GetHashCode(Role ?? string.Empty) * 397 ^ FeatureId.GetHashCode());
    }

    public readonly struct SavedPlaceRecord : IEquatable<SavedPlaceRecord>
    {
        public SavedPlaceRecord(
            WorldFeatureId savedPlaceId,
            WorldPersistenceManifest manifest,
            WorldCoordinateAddress address,
            bool hasAnchorFeature,
            WorldFeatureId anchorFeatureId)
            : this(
                savedPlaceId,
                manifest,
                "Saved place",
                address,
                default,
                OptionalThematicCoordinatePayload.None,
                BuildLegacyReferences(hasAnchorFeature, anchorFeatureId))
        {
        }

        public SavedPlaceRecord(
            WorldFeatureId savedPlaceId,
            WorldPersistenceManifest manifest,
            string playerName,
            WorldCoordinateAddress address,
            AbsoluteWorldPosition absolutePosition,
            OptionalThematicCoordinatePayload thematicCoordinate,
            IEnumerable<WorldFeatureReference> featureReferences)
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

            PlayerName = RequirePlayerName(playerName);
            if (featureReferences == null)
            {
                throw new ArgumentNullException(nameof(featureReferences));
            }
            var references = new List<WorldFeatureReference>(featureReferences);
            if (references.Count > 64)
                throw new ArgumentOutOfRangeException(nameof(featureReferences));
            references.Sort((left, right) =>
            {
                var role = string.CompareOrdinal(left.Role, right.Role);
                return role != 0 ? role : left.FeatureId.CompareTo(right.FeatureId);
            });
            var unique = new HashSet<string>(StringComparer.Ordinal);
            var anchorCount = 0;
            for (var i = 0; i < references.Count; i++)
            {
                var key = $"{references[i].Role}:{references[i].FeatureId}";
                if (!unique.Add(key)) throw new ArgumentException("Saved places cannot repeat feature references.", nameof(featureReferences));
                if (string.Equals(references[i].Role, "anchor", StringComparison.Ordinal)
                    && ++anchorCount > 1)
                    throw new ArgumentException("Saved places may have at most one anchor feature.", nameof(featureReferences));
            }

            SavedPlaceId = savedPlaceId;
            Manifest = manifest;
            Address = address;
            AbsolutePosition = absolutePosition;
            ThematicCoordinate = thematicCoordinate;
            FeatureReferences = references.AsReadOnly();
            HasAnchorFeature = TryFindAnchor(references, out var anchor);
            AnchorFeatureId = anchor;
        }

        public WorldFeatureId SavedPlaceId { get; }
        public WorldPersistenceManifest Manifest { get; }
        public string PlayerName { get; }
        public WorldCoordinateAddress Address { get; }
        public AbsoluteWorldPosition AbsolutePosition { get; }
        public OptionalThematicCoordinatePayload ThematicCoordinate { get; }
        public IReadOnlyList<WorldFeatureReference> FeatureReferences { get; }
        public bool HasAnchorFeature { get; }
        public WorldFeatureId AnchorFeatureId { get; }

        public bool Equals(SavedPlaceRecord other)
        {
            return SavedPlaceId.Equals(other.SavedPlaceId)
                && Manifest.Equals(other.Manifest)
                && string.Equals(PlayerName, other.PlayerName, StringComparison.Ordinal)
                && Address.Equals(other.Address)
                && AbsolutePosition.Equals(other.AbsolutePosition)
                && ThematicCoordinate.Equals(other.ThematicCoordinate)
                && HasAnchorFeature == other.HasAnchorFeature
                && AnchorFeatureId.Equals(other.AnchorFeatureId)
                && ReferencesEqual(FeatureReferences, other.FeatureReferences);
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
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(PlayerName ?? string.Empty);
                hash = hash * 397 ^ Address.GetHashCode();
                hash = hash * 397 ^ HasAnchorFeature.GetHashCode();
                hash = hash * 397 ^ AnchorFeatureId.GetHashCode();
                return hash;
            }
        }

        private static string RequirePlayerName(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            value = value.Trim();
            if (value.Length < 1 || value.Length > 64) throw new ArgumentOutOfRangeException(nameof(value));
            return value;
        }

        private static WorldFeatureReference[] BuildLegacyReferences(
            bool hasAnchorFeature,
            WorldFeatureId anchorFeatureId)
        {
            if (hasAnchorFeature == anchorFeatureId.IsEmpty)
                throw new ArgumentException("Anchor presence and anchor identity must agree.", nameof(anchorFeatureId));
            return hasAnchorFeature
                ? new[] { new WorldFeatureReference("anchor", anchorFeatureId) }
                : Array.Empty<WorldFeatureReference>();
        }

        private static bool TryFindAnchor(IReadOnlyList<WorldFeatureReference> references, out WorldFeatureId anchor)
        {
            for (var i = 0; i < references.Count; i++)
            {
                if (string.Equals(references[i].Role, "anchor", StringComparison.Ordinal))
                {
                    anchor = references[i].FeatureId;
                    return true;
                }
            }
            anchor = WorldFeatureId.Empty;
            return false;
        }

        private static bool ReferencesEqual(
            IReadOnlyList<WorldFeatureReference> left,
            IReadOnlyList<WorldFeatureReference> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Count != right.Count) return false;
            for (var i = 0; i < left.Count; i++) if (!left[i].Equals(right[i])) return false;
            return true;
        }
    }
}
