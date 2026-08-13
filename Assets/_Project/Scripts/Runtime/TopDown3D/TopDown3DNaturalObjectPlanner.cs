using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DNaturalObjectPlacement : IEquatable<TopDown3DNaturalObjectPlacement>
    {
        public TopDown3DNaturalObjectPlacement(
            string stableId,
            TopDown3DNaturalObjectLayer layer,
            TopDown3DNaturalObjectShape shape,
            TopDown3DRockSurface surface,
            int variant,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            float footprintRadius)
        {
            StableId = stableId;
            Layer = layer;
            Shape = shape;
            Surface = surface;
            Variant = variant;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            FootprintRadius = footprintRadius;
        }

        public string StableId { get; }
        public TopDown3DNaturalObjectLayer Layer { get; }
        public TopDown3DNaturalObjectShape Shape { get; }
        public TopDown3DRockSurface Surface { get; }
        public int Variant { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }
        public float FootprintRadius { get; }

        public bool Equals(TopDown3DNaturalObjectPlacement other)
        {
            return StableId == other.StableId
                && Layer == other.Layer
                && Shape == other.Shape
                && Surface == other.Surface
                && Variant == other.Variant
                && Position == other.Position
                && Rotation == other.Rotation
                && Scale == other.Scale
                && FootprintRadius.Equals(other.FootprintRadius);
        }

        public override bool Equals(object obj)
        {
            return obj is TopDown3DNaturalObjectPlacement other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StableId != null ? StableId.GetHashCode() : 0;
                hash = hash * 397 ^ (int)Layer;
                hash = hash * 397 ^ (int)Shape;
                hash = hash * 397 ^ (int)Surface;
                hash = hash * 397 ^ Variant;
                hash = hash * 397 ^ Position.GetHashCode();
                return hash;
            }
        }
    }

    public static class TopDown3DNaturalObjectPlanner
    {
        public static List<TopDown3DNaturalObjectPlacement> BuildPlacements(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter)
        {
            var placements = new List<TopDown3DNaturalObjectPlacement>();
            if (settings == null || catalog == null)
            {
                return placements;
            }

            BuildLayer(
                settings,
                catalog,
                chunkCoordinate,
                spawnExclusionCenter,
                TopDown3DNaturalObjectLayer.Obstacle,
                settings.PropsPerChunk,
                settings.PropSpacing,
                settings.MaximumPropSlope,
                settings.ClutterClusterFrequency,
                settings.ClutterClusterStrength,
                0.2f,
                1.8f,
                placements);
            BuildLayer(
                settings,
                catalog,
                chunkCoordinate,
                spawnExclusionCenter,
                TopDown3DNaturalObjectLayer.Scatter,
                settings.ScatterObjectsPerChunk,
                settings.ScatterSpacing,
                settings.MaximumClutterSlope,
                settings.ClutterClusterFrequency,
                settings.ClutterClusterStrength,
                0.2f,
                1.8f,
                placements);
            BuildLayer(
                settings,
                catalog,
                chunkCoordinate,
                spawnExclusionCenter,
                TopDown3DNaturalObjectLayer.GroundDetail,
                settings.GroundDetailsPerChunk,
                settings.GroundDetailSpacing,
                settings.MaximumClutterSlope,
                settings.ClutterClusterFrequency,
                settings.ClutterClusterStrength,
                0.2f,
                1.8f,
                placements);
            BuildLayer(
                settings,
                catalog,
                chunkCoordinate,
                spawnExclusionCenter,
                TopDown3DNaturalObjectLayer.FineGrayCluster,
                settings.FineGrayClutterPerChunk,
                settings.FineGrayClutterSpacing,
                settings.MaximumClutterSlope,
                settings.FineGrayClusterFrequency,
                settings.FineGrayClusterStrength,
                // A negative low-density factor creates truly empty ground between dense gray pockets.
                -2.4f,
                3.2f,
                placements);
            return placements;
        }

        private static void BuildLayer(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter,
            TopDown3DNaturalObjectLayer layer,
            int targetCount,
            float extraSpacing,
            float maximumSlope,
            float clusterFrequency,
            float clusterStrength,
            float clusterMinimumFactor,
            float clusterMaximumFactor,
            ICollection<TopDown3DNaturalObjectPlacement> output)
        {
            if (targetCount <= 0 || !catalog.HasLayer(layer))
            {
                return;
            }

            var definitions = GetDefinitions(catalog, layer);
            var maximumFootprint = 0f;
            for (var i = 0; i < definitions.Count; i++)
            {
                maximumFootprint = Mathf.Max(
                    maximumFootprint,
                    definitions[i].FootprintRadius
                    * definitions[i].UniformScaleRange.y
                    * Mathf.Max(definitions[i].Proportions.x, definitions[i].Proportions.z));
            }

            var chunkSize = settings.ChunkSize;
            var cellSize = Mathf.Max(
                0.18f,
                Mathf.Sqrt((chunkSize * chunkSize) / Mathf.Max(1f, targetCount * 2.25f)));
            var minimumDistance = maximumFootprint * 2f + extraSpacing;
            cellSize = Mathf.Max(cellSize, minimumDistance * 0.62f);
            var layerSeed = StableHash(
                settings.WorldSeed,
                settings.NaturalObjectGenerationVersion,
                (int)layer * 104729 + 8191);

            var originX = chunkCoordinate.x * chunkSize;
            var originZ = chunkCoordinate.y * chunkSize;
            var minCellX = Mathf.FloorToInt((originX - minimumDistance) / cellSize);
            var maxCellX = Mathf.FloorToInt((originX + chunkSize + minimumDistance) / cellSize);
            var minCellZ = Mathf.FloorToInt((originZ - minimumDistance) / cellSize);
            var maxCellZ = Mathf.FloorToInt((originZ + chunkSize + minimumDistance) / cellSize);
            var cellsPerChunk = (chunkSize / cellSize) * (chunkSize / cellSize);
            var baseAdmission = Mathf.Clamp01(targetCount / Mathf.Max(1f, cellsPerChunk));

            for (var cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
            {
                for (var cellX = minCellX; cellX <= maxCellX; cellX++)
                {
                    var candidate = BuildCandidate(layerSeed, cellX, cellZ, cellSize);
                    if (!BelongsToChunk(candidate.Position, chunkCoordinate, chunkSize)
                        || !PassesDensity(
                            layerSeed,
                            candidate,
                            baseAdmission,
                            clusterFrequency,
                            clusterStrength,
                            clusterMinimumFactor,
                            clusterMaximumFactor)
                        || LosesNeighborCompetition(
                            layerSeed,
                            candidate,
                            cellSize,
                            minimumDistance,
                            baseAdmission,
                            clusterFrequency,
                            clusterStrength,
                            clusterMinimumFactor,
                            clusterMaximumFactor))
                    {
                        continue;
                    }

                    var worldPosition = candidate.Position;
                    var definition = SelectDefinition(definitions, candidate.Selection);
                    var uniformScale = Mathf.Lerp(
                        definition.UniformScaleRange.x,
                        definition.UniformScaleRange.y,
                        candidate.Scale);
                    var scale = definition.Proportions * uniformScale;
                    var footprint = definition.FootprintRadius * Mathf.Max(scale.x, scale.z);
                    if (Vector2.Distance(worldPosition, spawnExclusionCenter)
                        < settings.ClearSpawnRadius + footprint)
                    {
                        continue;
                    }

                    var normal = TopDown3DHeightSampler.SampleNormal(settings, worldPosition.x, worldPosition.y);
                    var slope = Vector3.Angle(normal, Vector3.up);
                    if (slope > maximumSlope)
                    {
                        continue;
                    }

                    var tiltRatio = slope > 0.001f
                        ? Mathf.Min(1f, definition.MaximumTilt / slope)
                        : 0f;
                    var tilt = Quaternion.Slerp(
                        Quaternion.identity,
                        Quaternion.FromToRotation(Vector3.up, normal),
                        tiltRatio);
                    var yaw = Quaternion.AngleAxis(candidate.Yaw * 360f, Vector3.up);
                    var surfaceHeight = TopDown3DHeightSampler.SampleHeight(
                        settings,
                        worldPosition.x,
                        worldPosition.y);
                    var position = new Vector3(
                        worldPosition.x,
                        surfaceHeight - definition.SinkDepth * scale.y,
                        worldPosition.y);
                    output.Add(new TopDown3DNaturalObjectPlacement(
                        definition.StableId,
                        layer,
                        definition.Shape,
                        layer == TopDown3DNaturalObjectLayer.FineGrayCluster
                            ? TopDown3DRockSurface.Regular
                            : SampleRockSurface(settings, worldPosition),
                        Mathf.FloorToInt(candidate.Variant * TopDown3DNaturalMeshLibrary.VariantsPerShape)
                            % TopDown3DNaturalMeshLibrary.VariantsPerShape,
                        position,
                        tilt * yaw,
                        scale,
                        footprint));
                }
            }
        }

        private static bool LosesNeighborCompetition(
            int layerSeed,
            Candidate candidate,
            float cellSize,
            float minimumDistance,
            float baseAdmission,
            float clusterFrequency,
            float clusterStrength,
            float clusterMinimumFactor,
            float clusterMaximumFactor)
        {
            var range = Mathf.Max(1, Mathf.CeilToInt(minimumDistance / cellSize));
            var minimumDistanceSquared = minimumDistance * minimumDistance;
            for (var z = -range; z <= range; z++)
            {
                for (var x = -range; x <= range; x++)
                {
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }

                    var neighbor = BuildCandidate(
                        layerSeed,
                        candidate.CellX + x,
                        candidate.CellZ + z,
                        cellSize);
                    if (!PassesDensity(
                            layerSeed,
                            neighbor,
                            baseAdmission,
                            clusterFrequency,
                            clusterStrength,
                            clusterMinimumFactor,
                            clusterMaximumFactor)
                        || (neighbor.Position - candidate.Position).sqrMagnitude >= minimumDistanceSquared)
                    {
                        continue;
                    }

                    if (neighbor.Priority > candidate.Priority
                        || (Mathf.Approximately(neighbor.Priority, candidate.Priority)
                            && StableCellOrder(neighbor.CellX, neighbor.CellZ, candidate.CellX, candidate.CellZ) < 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool PassesDensity(
            int layerSeed,
            Candidate candidate,
            float baseAdmission,
            float clusterFrequency,
            float clusterStrength,
            float clusterMinimumFactor,
            float clusterMaximumFactor)
        {
            var cluster = ValueNoise(
                layerSeed ^ 0x2C9277B5,
                candidate.Position.x * clusterFrequency,
                candidate.Position.y * clusterFrequency);
            var clusterFactor = Mathf.Lerp(
                1f,
                Mathf.Lerp(clusterMinimumFactor, clusterMaximumFactor, cluster),
                clusterStrength);
            return candidate.Admission <= Mathf.Clamp01(baseAdmission * clusterFactor);
        }

        public static TopDown3DRockSurface SampleRockSurface(
            TopDown3DWorldSettings settings,
            Vector2 worldPosition)
        {
            var surfaceSeed = StableHash(
                settings.WorldSeed,
                settings.NaturalObjectGenerationVersion,
                0x25A17D3E);
            var cluster = ValueNoise(
                surfaceSeed,
                worldPosition.x * settings.RockSurfaceClusterFrequency,
                worldPosition.y * settings.RockSurfaceClusterFrequency);
            if (cluster < settings.DarkRockSurfaceThreshold)
            {
                return TopDown3DRockSurface.Dark;
            }

            return cluster > settings.TealRockSurfaceThreshold
                ? TopDown3DRockSurface.Teal
                : TopDown3DRockSurface.Regular;
        }

        private static List<TopDown3DNaturalObjectDefinition> GetDefinitions(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DNaturalObjectLayer layer)
        {
            var matches = new List<TopDown3DNaturalObjectDefinition>();
            var definitions = catalog.Definitions;
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].Layer == layer)
                {
                    matches.Add(definitions[i]);
                }
            }

            return matches;
        }

        private static TopDown3DNaturalObjectDefinition SelectDefinition(
            IReadOnlyList<TopDown3DNaturalObjectDefinition> definitions,
            float selection)
        {
            var totalWeight = 0f;
            for (var i = 0; i < definitions.Count; i++)
            {
                totalWeight += definitions[i].Weight;
            }

            var target = selection * totalWeight;
            for (var i = 0; i < definitions.Count; i++)
            {
                target -= definitions[i].Weight;
                if (target <= 0f)
                {
                    return definitions[i];
                }
            }

            return definitions[definitions.Count - 1];
        }

        private static bool BelongsToChunk(Vector2 position, Vector2Int coordinate, float chunkSize)
        {
            return Mathf.FloorToInt(position.x / chunkSize) == coordinate.x
                && Mathf.FloorToInt(position.y / chunkSize) == coordinate.y;
        }

        private static Candidate BuildCandidate(int seed, int cellX, int cellZ, float cellSize)
        {
            var hash = StableHash(seed, cellX, cellZ);
            var jitterX = Mathf.Lerp(0.08f, 0.92f, Hash01(hash ^ 0x68E31DA4));
            var jitterZ = Mathf.Lerp(0.08f, 0.92f, Hash01(hash ^ 0x1B56C4E9));
            return new Candidate(
                cellX,
                cellZ,
                new Vector2((cellX + jitterX) * cellSize, (cellZ + jitterZ) * cellSize),
                Hash01(hash ^ 0x5A17D3E1),
                Hash01(hash ^ 0x74D0A55B),
                Hash01(hash ^ 0x37C8E4D7),
                Hash01(hash ^ 0x19F34AC1),
                Hash01(hash ^ 0x4E2B81F3),
                Hash01(hash ^ 0x631F8D29));
        }

        private static int StableCellOrder(int leftX, int leftZ, int rightX, int rightZ)
        {
            var z = leftZ.CompareTo(rightZ);
            return z != 0 ? z : leftX.CompareTo(rightX);
        }

        private static int StableHash(int a, int b, int c)
        {
            unchecked
            {
                var hash = (uint)a;
                hash ^= (uint)b * 0x9E3779B9u;
                hash = (hash << 13) | (hash >> 19);
                hash ^= (uint)c * 0x85EBCA6Bu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return (int)hash;
            }
        }

        private static float Hash01(int hash)
        {
            unchecked
            {
                var value = (uint)hash;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                return (value & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static float ValueNoise(int seed, float x, float z)
        {
            var x0 = Mathf.FloorToInt(x);
            var z0 = Mathf.FloorToInt(z);
            var tx = Smooth(x - x0);
            var tz = Smooth(z - z0);
            var a = Hash01(StableHash(seed, x0, z0));
            var b = Hash01(StableHash(seed, x0 + 1, z0));
            var c = Hash01(StableHash(seed, x0, z0 + 1));
            var d = Hash01(StableHash(seed, x0 + 1, z0 + 1));
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private readonly struct Candidate
        {
            public Candidate(
                int cellX,
                int cellZ,
                Vector2 position,
                float priority,
                float admission,
                float selection,
                float scale,
                float yaw,
                float variant)
            {
                CellX = cellX;
                CellZ = cellZ;
                Position = position;
                Priority = priority;
                Admission = admission;
                Selection = selection;
                Scale = scale;
                Yaw = yaw;
                Variant = variant;
            }

            public int CellX { get; }
            public int CellZ { get; }
            public Vector2 Position { get; }
            public float Priority { get; }
            public float Admission { get; }
            public float Selection { get; }
            public float Scale { get; }
            public float Yaw { get; }
            public float Variant { get; }
        }
    }
}
