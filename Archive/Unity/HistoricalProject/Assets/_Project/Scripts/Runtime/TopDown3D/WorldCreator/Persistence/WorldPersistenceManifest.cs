using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public enum WorldCompatibilityIssue
    {
        None = 0,
        UnsupportedPersistenceSchema = 1,
        WorldSeedMismatch = 2,
        TopologyVersionMismatch = 3,
        CoordinateVersionMismatch = 4,
        LandformVersionMismatch = 5,
        MaterialVersionMismatch = 6,
        DecorationVersionMismatch = 7,
        ResourceVersionMismatch = 8,
        SiteVersionMismatch = 9,
        CoordinateModelMismatch = 10,
        CoordinateModelVersionMismatch = 11
    }

    public readonly struct WorldCompatibilityResult : IEquatable<WorldCompatibilityResult>
    {
        public WorldCompatibilityResult(WorldCompatibilityIssue issue, string message)
        {
            Issue = issue;
            Message = message ?? string.Empty;
        }

        public WorldCompatibilityIssue Issue { get; }
        public string Message { get; }
        public bool IsCompatible => Issue == WorldCompatibilityIssue.None;

        public bool Equals(WorldCompatibilityResult other)
        {
            return Issue == other.Issue && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldCompatibilityResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Issue * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty));
        }
    }

    public readonly struct WorldPersistenceManifest : IEquatable<WorldPersistenceManifest>
    {
        public const int CurrentSchemaVersion = 1;

        public WorldPersistenceManifest(
            int schemaVersion,
            WorldIdentity world,
            string coordinateModelId,
            int coordinateModelVersion)
        {
            if (schemaVersion < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            }

            if (coordinateModelVersion < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coordinateModelVersion));
            }

            SchemaVersion = schemaVersion;
            World = world;
            CoordinateModelId = WorldStableText.Require(coordinateModelId, nameof(coordinateModelId), 128);
            CoordinateModelVersion = coordinateModelVersion;
        }

        public int SchemaVersion { get; }
        public WorldIdentity World { get; }
        public string CoordinateModelId { get; }
        public int CoordinateModelVersion { get; }

        public static WorldPersistenceManifest CreateCurrent(WorldIdentity world, IWorldCoordinateModel coordinateModel)
        {
            if (coordinateModel == null)
            {
                throw new ArgumentNullException(nameof(coordinateModel));
            }

            return new WorldPersistenceManifest(
                CurrentSchemaVersion,
                world,
                coordinateModel.ModelId,
                coordinateModel.ModelVersion);
        }

        public WorldCompatibilityResult CheckCompatibility(
            WorldIdentity currentWorld,
            IWorldCoordinateModel currentCoordinateModel)
        {
            if (currentCoordinateModel == null)
            {
                throw new ArgumentNullException(nameof(currentCoordinateModel));
            }

            if (SchemaVersion != CurrentSchemaVersion)
            {
                return Mismatch(
                    WorldCompatibilityIssue.UnsupportedPersistenceSchema,
                    $"Persistence schema {SchemaVersion} is not supported; expected {CurrentSchemaVersion}.");
            }

            if (World.Seed != currentWorld.Seed)
            {
                return Mismatch(WorldCompatibilityIssue.WorldSeedMismatch, "The saved place belongs to a different world seed.");
            }

            var versionResult = CheckVersion(WorldVersionDomain.Topology, WorldCompatibilityIssue.TopologyVersionMismatch, currentWorld);
            if (!versionResult.IsCompatible)
            {
                return versionResult;
            }

            versionResult = CheckVersion(WorldVersionDomain.Coordinate, WorldCompatibilityIssue.CoordinateVersionMismatch, currentWorld);
            if (!versionResult.IsCompatible)
            {
                return versionResult;
            }

            versionResult = CheckVersion(WorldVersionDomain.Landform, WorldCompatibilityIssue.LandformVersionMismatch, currentWorld);
            if (!versionResult.IsCompatible)
            {
                return versionResult;
            }

            versionResult = CheckVersion(WorldVersionDomain.Material, WorldCompatibilityIssue.MaterialVersionMismatch, currentWorld);
            if (!versionResult.IsCompatible)
            {
                return versionResult;
            }

            versionResult = CheckVersion(WorldVersionDomain.Decoration, WorldCompatibilityIssue.DecorationVersionMismatch, currentWorld);
            if (!versionResult.IsCompatible)
            {
                return versionResult;
            }

            versionResult = CheckVersion(WorldVersionDomain.Resource, WorldCompatibilityIssue.ResourceVersionMismatch, currentWorld);
            if (!versionResult.IsCompatible)
            {
                return versionResult;
            }

            versionResult = CheckVersion(WorldVersionDomain.Site, WorldCompatibilityIssue.SiteVersionMismatch, currentWorld);
            if (!versionResult.IsCompatible)
            {
                return versionResult;
            }

            if (!string.Equals(CoordinateModelId, currentCoordinateModel.ModelId, StringComparison.Ordinal))
            {
                return Mismatch(
                    WorldCompatibilityIssue.CoordinateModelMismatch,
                    $"Coordinate model '{CoordinateModelId}' cannot be resolved by '{currentCoordinateModel.ModelId}'.");
            }

            if (CoordinateModelVersion != currentCoordinateModel.ModelVersion)
            {
                return Mismatch(
                    WorldCompatibilityIssue.CoordinateModelVersionMismatch,
                    $"Coordinate model version {CoordinateModelVersion} does not match {currentCoordinateModel.ModelVersion}.");
            }

            return new WorldCompatibilityResult(WorldCompatibilityIssue.None, string.Empty);
        }

        public bool Equals(WorldPersistenceManifest other)
        {
            return SchemaVersion == other.SchemaVersion
                && CoordinateModelVersion == other.CoordinateModelVersion
                && World.Equals(other.World)
                && string.Equals(CoordinateModelId, other.CoordinateModelId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPersistenceManifest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = SchemaVersion;
                hash = hash * 397 ^ World.GetHashCode();
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(CoordinateModelId ?? string.Empty);
                hash = hash * 397 ^ CoordinateModelVersion;
                return hash;
            }
        }

        private WorldCompatibilityResult CheckVersion(
            WorldVersionDomain domain,
            WorldCompatibilityIssue issue,
            WorldIdentity currentWorld)
        {
            var savedVersion = World.Versions.GetVersion(domain);
            var currentVersion = currentWorld.Versions.GetVersion(domain);
            return savedVersion == currentVersion
                ? new WorldCompatibilityResult(WorldCompatibilityIssue.None, string.Empty)
                : Mismatch(issue, $"{domain} version {savedVersion} does not match {currentVersion}.");
        }

        private static WorldCompatibilityResult Mismatch(WorldCompatibilityIssue issue, string message)
        {
            return new WorldCompatibilityResult(issue, message);
        }
    }
}
