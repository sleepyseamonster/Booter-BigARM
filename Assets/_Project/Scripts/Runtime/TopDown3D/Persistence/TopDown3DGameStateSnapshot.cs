using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DWorldManifestSnapshot
    {
        [SerializeField] private int schemaVersion;
        [SerializeField] private long worldSeed;
        [SerializeField] private int topologyVersion;
        [SerializeField] private int coordinateVersion;
        [SerializeField] private int landformVersion;
        [SerializeField] private int materialVersion;
        [SerializeField] private int decorationVersion;
        [SerializeField] private int resourceVersion;
        [SerializeField] private int siteVersion;
        [SerializeField] private string coordinateModelId;
        [SerializeField] private int coordinateModelVersion;

        public long WorldSeed => worldSeed;
        public int TopologyVersion => topologyVersion;

        internal static TopDown3DWorldManifestSnapshot Create(WorldPersistenceManifest manifest)
        {
            return new TopDown3DWorldManifestSnapshot
            {
                schemaVersion = manifest.SchemaVersion,
                worldSeed = manifest.World.Seed,
                topologyVersion = manifest.World.Versions.Topology,
                coordinateVersion = manifest.World.Versions.Coordinate,
                landformVersion = manifest.World.Versions.Landform,
                materialVersion = manifest.World.Versions.Material,
                decorationVersion = manifest.World.Versions.Decoration,
                resourceVersion = manifest.World.Versions.Resource,
                siteVersion = manifest.World.Versions.Site,
                coordinateModelId = manifest.CoordinateModelId,
                coordinateModelVersion = manifest.CoordinateModelVersion
            };
        }

        internal bool TryToManifest(out WorldPersistenceManifest manifest, out string error)
        {
            manifest = default;
            try
            {
                manifest = new WorldPersistenceManifest(
                    schemaVersion,
                    new WorldIdentity(
                        worldSeed,
                        new WorldVersionManifest(
                            topologyVersion,
                            coordinateVersion,
                            landformVersion,
                            materialVersion,
                            decorationVersion,
                            resourceVersion,
                            siteVersion)),
                    coordinateModelId,
                    coordinateModelVersion);
                error = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is ArgumentOutOfRangeException)
            {
                error = exception.Message;
                return false;
            }
        }
    }

    [Serializable]
    public sealed class TopDown3DAbsolutePositionSnapshot
    {
        [SerializeField] private double horizontalA;
        [SerializeField] private double vertical;
        [SerializeField] private double horizontalB;
        [SerializeField] private string coordinateModelId;
        [SerializeField] private int coordinateModelVersion;
        [SerializeField] private string canonicalAddress;

        public double HorizontalA => horizontalA;
        public double Vertical => vertical;
        public double HorizontalB => horizontalB;
        public string CanonicalAddress => canonicalAddress;

        internal static TopDown3DAbsolutePositionSnapshot Create(
            AbsoluteWorldPosition position,
            IWorldCoordinateModel model)
        {
            var address = model.Encode(position);
            return new TopDown3DAbsolutePositionSnapshot
            {
                horizontalA = position.HorizontalA,
                vertical = position.Vertical,
                horizontalB = position.HorizontalB,
                coordinateModelId = address.ModelId,
                coordinateModelVersion = address.ModelVersion,
                canonicalAddress = address.CanonicalValue
            };
        }

        internal bool TryResolve(
            IWorldCoordinateModel model,
            out AbsoluteWorldPosition position,
            out string error)
        {
            position = default;
            if (model == null)
            {
                error = "A coordinate model is required.";
                return false;
            }
            try
            {
                var encoded = new WorldCoordinateAddress(
                    coordinateModelId,
                    coordinateModelVersion,
                    canonicalAddress);
                if (!encoded.IsCompatibleWith(model) || !model.TryResolve(encoded, out var resolved))
                {
                    error = "The saved absolute position cannot be resolved by the current coordinate model.";
                    return false;
                }
                var declared = new AbsoluteWorldPosition(horizontalA, vertical, horizontalB);
                if (!resolved.Equals(declared))
                {
                    error = "The saved absolute position disagrees with its canonical coordinate address.";
                    return false;
                }
                position = declared;
                error = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is ArgumentOutOfRangeException)
            {
                error = exception.Message;
                return false;
            }
        }
    }

    [Serializable]
    public sealed class TopDown3DWorldDeltaSnapshot
    {
        [SerializeField] private string featureId;
        [SerializeField] private int versionDomain;
        [SerializeField] private string operationId;
        [SerializeField] private int revision;
        [SerializeField] private string payload;

        public string FeatureId => featureId;
        public WorldVersionDomain VersionDomain => (WorldVersionDomain)versionDomain;
        public string OperationId => operationId;
        public int Revision => revision;
        public string Payload => payload;

        internal static TopDown3DWorldDeltaSnapshot Create(
            WorldFeatureId id,
            WorldVersionDomain domain,
            string operation,
            int deltaRevision,
            string deltaPayload)
        {
            Validate(id, domain, operation, deltaRevision, deltaPayload);
            return new TopDown3DWorldDeltaSnapshot
            {
                featureId = id.ToString(),
                versionDomain = (int)domain,
                operationId = operation,
                revision = deltaRevision,
                payload = deltaPayload ?? string.Empty
            };
        }

        internal bool TryValidate(out WorldFeatureId id, out string error)
        {
            id = WorldFeatureId.Empty;
            try
            {
                if (!WorldFeatureId.TryParse(featureId, out id))
                    throw new ArgumentException("Runtime delta has an invalid feature ID.");
                Validate(id, (WorldVersionDomain)versionDomain, operationId, revision, payload);
                error = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is ArgumentOutOfRangeException)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void Validate(
            WorldFeatureId id,
            WorldVersionDomain domain,
            string operation,
            int deltaRevision,
            string deltaPayload)
        {
            if (id.IsEmpty) throw new ArgumentException("Runtime deltas require feature identity.", nameof(id));
            _ = new WorldSeedNamespace(domain, "runtime-delta-validation");
            _ = WorldStableText.Require(operation, nameof(operation), 128);
            if (deltaRevision < 1) throw new ArgumentOutOfRangeException(nameof(deltaRevision));
            if (deltaPayload != null && deltaPayload.Length > 4096)
                throw new ArgumentOutOfRangeException(nameof(deltaPayload));
        }
    }

    [Serializable]
    public sealed class TopDown3DGameStateSnapshot
    {
        public const int CurrentVersion = 3;
        public const int MaximumSavedPlaces = 512;
        public const int MaximumWorldDeltas = 8192;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private TopDown3DWorldManifestSnapshot worldManifest;
        [SerializeField] private TopDown3DAbsolutePositionSnapshot booterAbsolutePosition;
        [SerializeField] private TopDown3DAbsolutePositionSnapshot bigArmAbsolutePosition;
        [SerializeField] private TopDown3DInventorySnapshot booterInventory;
        [SerializeField] private bool hasBigArm;
        [SerializeField] private bool hasBigArmCargo;
        [SerializeField] private TopDown3DBigArmCompanionSnapshot bigArm;
        [SerializeField] private bool hasResources;
        [SerializeField] private TopDown3DResourceWorldSnapshot resources;
        [SerializeField] private List<string> savedPlacePayloads = new List<string>();
        [SerializeField] private List<TopDown3DWorldDeltaSnapshot> worldDeltas =
            new List<TopDown3DWorldDeltaSnapshot>();
        [SerializeField] private long nextSavedPlaceSequence;

        // Retained only so v1/v2 JSON can be recognized and explicitly rejected.
        [SerializeField] private int worldSeed;
        [SerializeField] private int worldTopologyVersion;
        [SerializeField] private Vector3 booterPosition;

        public int Version => version;
        public TopDown3DWorldManifestSnapshot WorldManifest => worldManifest;
        public TopDown3DAbsolutePositionSnapshot BooterAbsolutePosition => booterAbsolutePosition;
        public TopDown3DAbsolutePositionSnapshot BigArmAbsolutePosition => bigArmAbsolutePosition;
        public TopDown3DInventorySnapshot BooterInventory => booterInventory;
        public bool HasBigArm => hasBigArm;
        public bool HasBigArmCargo => hasBigArmCargo;
        public TopDown3DBigArmCompanionSnapshot BigArm => bigArm;
        public bool HasResources => hasResources;
        public TopDown3DResourceWorldSnapshot Resources => resources;
        public IReadOnlyList<string> SavedPlacePayloads => savedPlacePayloads;
        public IReadOnlyList<TopDown3DWorldDeltaSnapshot> WorldDeltas => worldDeltas;
        public long NextSavedPlaceSequence => nextSavedPlaceSequence;
        public int WorldSeed => worldManifest != null ? checked((int)worldManifest.WorldSeed) : worldSeed;
        public int WorldTopologyVersion => worldManifest != null ? worldManifest.TopologyVersion : worldTopologyVersion;
        public Vector3 BooterPosition => booterPosition;

        internal bool IsCompatible(int expectedWorldSeed, int expectedTopologyVersion)
        {
            if (version != CurrentVersion || worldManifest == null
                || !worldManifest.TryToManifest(out var manifest, out _))
                return false;
            return manifest.World.Seed == expectedWorldSeed
                && manifest.World.Versions.Topology == expectedTopologyVersion;
        }

        internal bool TryCheckCompatibility(
            WorldIdentity world,
            IWorldCoordinateModel model,
            out WorldCompatibilityResult result,
            out string error)
        {
            error = null;
            if (version != CurrentVersion)
            {
                result = default;
                error = version < CurrentVersion
                    ? $"Prototype save schema {version} lacks precision-safe World Creator identity and cannot be migrated safely."
                    : $"Prototype save schema {version} is newer than supported schema {CurrentVersion}.";
                return false;
            }
            if (worldManifest == null || !worldManifest.TryToManifest(out var manifest, out error))
            {
                result = default;
                return false;
            }
            result = manifest.CheckCompatibility(world, model);
            error = result.IsCompatible ? null : result.Message;
            return result.IsCompatible;
        }

        internal static TopDown3DGameStateSnapshot Create(
            int seed,
            int topologyVersion,
            Vector3 position,
            TopDown3DInventorySnapshot inventory,
            TopDown3DBigArmCompanionSnapshot companion)
        {
            var authority = WorldCreatorProductionRuntime.GetSharedAuthority(seed);
            var versions = authority.Identity.Versions;
            var compatibilityWorld = new WorldIdentity(
                seed,
                new WorldVersionManifest(
                    topologyVersion,
                    versions.Coordinate,
                    versions.Landform,
                    versions.Material,
                    versions.Decoration,
                    versions.Resource,
                    versions.Site));
            var manifest = WorldPersistenceManifest.CreateCurrent(
                compatibilityWorld,
                authority.CoordinateModel);
            var absolute = new AbsoluteWorldPosition(position.x, position.y, position.z);
            return Create(
                manifest,
                authority.CoordinateModel,
                absolute,
                inventory,
                companion,
                companion != null
                    ? new AbsoluteWorldPosition(
                        companion.AuthoritativePosition.x,
                        companion.AuthoritativePosition.y,
                        companion.AuthoritativePosition.z)
                    : (AbsoluteWorldPosition?)null,
                null,
                Array.Empty<SavedPlaceRecord>(),
                Array.Empty<TopDown3DWorldDeltaSnapshot>(),
                0L);
        }

        internal static TopDown3DGameStateSnapshot Create(
            WorldPersistenceManifest manifest,
            IWorldCoordinateModel model,
            AbsoluteWorldPosition booterPosition,
            TopDown3DInventorySnapshot inventory,
            TopDown3DBigArmCompanionSnapshot companion,
            AbsoluteWorldPosition? bigArmPosition,
            TopDown3DResourceWorldSnapshot resourceSnapshot,
            IEnumerable<SavedPlaceRecord> savedPlaces,
            IEnumerable<TopDown3DWorldDeltaSnapshot> deltas,
            long nextSequence)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (savedPlaces == null) throw new ArgumentNullException(nameof(savedPlaces));
            if (deltas == null) throw new ArgumentNullException(nameof(deltas));
            if (nextSequence < 0) throw new ArgumentOutOfRangeException(nameof(nextSequence));

            var placePayloads = new List<string>();
            foreach (var place in savedPlaces)
            {
                if (!place.Manifest.Equals(manifest))
                    throw new ArgumentException("Saved-place manifest differs from the game-state manifest.", nameof(savedPlaces));
                placePayloads.Add(Convert.ToBase64String(SavedPlaceRecordCodec.Encode(place)));
            }
            placePayloads.Sort(StringComparer.Ordinal);
            if (placePayloads.Count > MaximumSavedPlaces)
                throw new ArgumentOutOfRangeException(nameof(savedPlaces));

            var orderedDeltas = new List<TopDown3DWorldDeltaSnapshot>(deltas);
            orderedDeltas.Sort((left, right) => string.CompareOrdinal(left.FeatureId, right.FeatureId));
            if (orderedDeltas.Count > MaximumWorldDeltas)
                throw new ArgumentOutOfRangeException(nameof(deltas));

            return new TopDown3DGameStateSnapshot
            {
                worldManifest = TopDown3DWorldManifestSnapshot.Create(manifest),
                booterAbsolutePosition = TopDown3DAbsolutePositionSnapshot.Create(booterPosition, model),
                bigArmAbsolutePosition = bigArmPosition.HasValue
                    ? TopDown3DAbsolutePositionSnapshot.Create(bigArmPosition.Value, model)
                    : null,
                booterInventory = inventory,
                hasBigArm = companion != null,
                hasBigArmCargo = companion?.Cargo != null,
                bigArm = companion,
                hasResources = resourceSnapshot != null,
                resources = resourceSnapshot,
                savedPlacePayloads = placePayloads,
                worldDeltas = orderedDeltas,
                nextSavedPlaceSequence = nextSequence
            };
        }
    }
}
