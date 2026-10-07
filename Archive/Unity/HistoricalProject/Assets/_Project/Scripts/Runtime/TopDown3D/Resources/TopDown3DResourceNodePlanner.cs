using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DResourceNodePlacement : IEquatable<TopDown3DResourceNodePlacement>
    {
        public TopDown3DResourceNodePlacement(
            string stableId,
            string resourceId,
            Vector2Int ownerChunk,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            int cellX,
            int cellZ)
        {
            StableId = stableId;
            ResourceId = resourceId;
            OwnerChunk = ownerChunk;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            CellX = cellX;
            CellZ = cellZ;
        }

        public string StableId { get; }
        public string ResourceId { get; }
        public Vector2Int OwnerChunk { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }
        public int CellX { get; }
        public int CellZ { get; }

        public bool Equals(TopDown3DResourceNodePlacement other)
        {
            return StableId == other.StableId && ResourceId == other.ResourceId
                && OwnerChunk == other.OwnerChunk && Position == other.Position
                && Rotation == other.Rotation && Scale == other.Scale
                && CellX == other.CellX && CellZ == other.CellZ;
        }

        public override bool Equals(object obj) => obj is TopDown3DResourceNodePlacement other && Equals(other);
        public override int GetHashCode() => StableId != null ? StableId.GetHashCode() : 0;
    }

    public static class TopDown3DResourceNodePlanner
    {
        public static List<TopDown3DResourceNodePlacement> BuildChunkPlacements(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DResourceCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> physicalFormations)
        {
            var output = new List<TopDown3DResourceNodePlacement>();
            if (settings == null || generator == null || catalog == null)
            {
                return output;
            }

            for (var definitionIndex = 0; definitionIndex < catalog.Definitions.Count; definitionIndex++)
            {
                BuildDefinition(
                    settings,
                    generator,
                    catalog.Definitions[definitionIndex],
                    chunkCoordinate,
                    spawnExclusionCenter,
                    physicalFormations,
                    output);
            }

            output.Sort((left, right) => string.CompareOrdinal(left.StableId, right.StableId));
            return output;
        }

        private static void BuildDefinition(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DResourceDefinition definition,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> physicalFormations,
            ICollection<TopDown3DResourceNodePlacement> output)
        {
            if (definition == null)
            {
                return;
            }

            var chunkSize = settings.ChunkSize;
            var cellSize = definition.GlobalCellSize;
            var originX = chunkCoordinate.x * chunkSize;
            var originZ = chunkCoordinate.y * chunkSize;
            var minCellX = Mathf.FloorToInt(originX / cellSize);
            var maxCellX = Mathf.FloorToInt((originX + chunkSize) / cellSize);
            var minCellZ = Mathf.FloorToInt(originZ / cellSize);
            var maxCellZ = Mathf.FloorToInt((originZ + chunkSize) / cellSize);
            var cellsPerChunk = (chunkSize * chunkSize) / (cellSize * cellSize);
            var admission = Mathf.Clamp01(definition.TargetPerChunk / Mathf.Max(0.0001f, cellsPerChunk));
            var seed = StableHash(settings.WorldSeed, settings.ResourceGenerationVersion, StableStringHash(definition.ResourceId));
            var accepted = new List<TopDown3DResourceNodePlacement>();

            for (var cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
            {
                for (var cellX = minCellX; cellX <= maxCellX; cellX++)
                {
                    var candidate = BuildCandidate(seed, cellX, cellZ, cellSize);
                    if (!BelongsToChunk(candidate.Position, chunkCoordinate, chunkSize)
                        || candidate.Admission > admission
                        || !PassesAuthoredConstraints(
                            generator,
                            definition,
                            candidate.Position,
                            spawnExclusionCenter,
                            settings.ClearSpawnRadius,
                            physicalFormations)
                        || LosesNeighborCompetition(
                            settings,
                            generator,
                            definition,
                            seed,
                            candidate,
                            admission,
                            spawnExclusionCenter,
                            physicalFormations))
                    {
                        continue;
                    }

                    var surface = generator.Sample(candidate.Position.x, candidate.Position.y);
                    var scaleValue = Mathf.Lerp(
                        definition.UniformScaleRange.x,
                        definition.UniformScaleRange.y,
                        candidate.Scale);
                    var scale = Vector3.one * scaleValue;
                    var tilt = Quaternion.FromToRotation(Vector3.up, surface.Normal);
                    var rotation = tilt * Quaternion.AngleAxis(candidate.Yaw * 360f, Vector3.up);
                    var position = new Vector3(
                        candidate.Position.x,
                        surface.Height - definition.SinkDepth * scaleValue,
                        candidate.Position.y);
                    var stableId = $"resource:{settings.WorldSeed}:{settings.ResourceGenerationVersion}:{definition.ResourceId}:{cellX}:{cellZ}:0";
                    accepted.Add(new TopDown3DResourceNodePlacement(
                        stableId,
                        definition.ResourceId,
                        chunkCoordinate,
                        position,
                        rotation,
                        scale,
                        cellX,
                        cellZ));
                }
            }

            accepted.Sort((left, right) => string.CompareOrdinal(left.StableId, right.StableId));
            var densityCap = Mathf.Max(1, Mathf.CeilToInt(definition.TargetPerChunk));
            for (var i = 0; i < Mathf.Min(densityCap, accepted.Count); i++)
            {
                output.Add(accepted[i]);
            }
        }

        private static bool LosesNeighborCompetition(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DResourceDefinition definition,
            int seed,
            Candidate candidate,
            float admission,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> physicalFormations)
        {
            var range = Mathf.Max(1, Mathf.CeilToInt(definition.MinimumSpacing / definition.GlobalCellSize));
            for (var z = -range; z <= range; z++)
            {
                for (var x = -range; x <= range; x++)
                {
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }

                    var neighbor = BuildCandidate(
                        seed,
                        candidate.CellX + x,
                        candidate.CellZ + z,
                        definition.GlobalCellSize);
                    if (neighbor.Admission > admission
                        || Vector2.Distance(candidate.Position, neighbor.Position) >= definition.MinimumSpacing
                        || !PassesAuthoredConstraints(
                            generator,
                            definition,
                            neighbor.Position,
                            spawnExclusionCenter,
                            settings.ClearSpawnRadius,
                            physicalFormations))
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

        private static bool PassesAuthoredConstraints(
            TopDown3DWorldGenerator generator,
            TopDown3DResourceDefinition definition,
            Vector2 position,
            Vector2 spawnExclusionCenter,
            float clearSpawnRadius,
            IReadOnlyList<TopDown3DRockFormationPlan> physicalFormations)
        {
            var surface = generator.Sample(position.x, position.y);
            if (Vector3.Angle(surface.Normal, Vector3.up) > definition.MaximumSlope
                || surface.BedrockWeight < definition.MinimumBedrockWeight
                || surface.DepositWeight < definition.MinimumDepositWeight
                || surface.Lithology < definition.MinimumLithology
                || surface.TraversalCorridor > definition.MaximumTraversalCorridor
                || Vector2.Distance(position, spawnExclusionCenter)
                    < clearSpawnRadius + definition.FootprintRadius)
            {
                return false;
            }

            if (physicalFormations != null)
            {
                for (var i = 0; i < physicalFormations.Count; i++)
                {
                    var formation = physicalFormations[i];
                    if (Vector2.Distance(position, formation.EnvelopeCenter)
                        < definition.FootprintRadius + formation.EnvelopeRadius)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool BelongsToChunk(Vector2 position, Vector2Int coordinate, float chunkSize)
        {
            return Mathf.FloorToInt(position.x / chunkSize) == coordinate.x
                && Mathf.FloorToInt(position.y / chunkSize) == coordinate.y;
        }

        private static Candidate BuildCandidate(int seed, int cellX, int cellZ, float cellSize)
        {
            var hash = StableHash(seed, cellX, cellZ);
            return new Candidate(
                cellX,
                cellZ,
                new Vector2(
                    (cellX + Mathf.Lerp(0.08f, 0.92f, Hash01(hash ^ 0x18A31D2B))) * cellSize,
                    (cellZ + Mathf.Lerp(0.08f, 0.92f, Hash01(hash ^ 0x52D16C47))) * cellSize),
                Hash01(hash ^ 0x2473A913),
                Hash01(hash ^ 0x7319BC25),
                Hash01(hash ^ 0x4A1D3E67),
                Hash01(hash ^ 0x19C74B81));
        }

        private static int StableCellOrder(int leftX, int leftZ, int rightX, int rightZ)
        {
            var z = leftZ.CompareTo(rightZ);
            return z != 0 ? z : leftX.CompareTo(rightX);
        }

        private static int StableStringHash(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
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

        private readonly struct Candidate
        {
            public Candidate(int cellX, int cellZ, Vector2 position, float priority, float admission, float scale, float yaw)
            {
                CellX = cellX;
                CellZ = cellZ;
                Position = position;
                Priority = priority;
                Admission = admission;
                Scale = scale;
                Yaw = yaw;
            }

            public int CellX { get; }
            public int CellZ { get; }
            public Vector2 Position { get; }
            public float Priority { get; }
            public float Admission { get; }
            public float Scale { get; }
            public float Yaw { get; }
        }
    }
}
