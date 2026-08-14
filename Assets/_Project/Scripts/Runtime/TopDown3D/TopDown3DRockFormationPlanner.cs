using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DRockRootKey : IEquatable<TopDown3DRockRootKey>
    {
        public TopDown3DRockRootKey(
            TopDown3DRockSizeTier tier,
            int cellX,
            int cellZ,
            int generationVersion)
        {
            Tier = tier;
            CellX = cellX;
            CellZ = cellZ;
            GenerationVersion = generationVersion;
        }

        public TopDown3DRockSizeTier Tier { get; }
        public int CellX { get; }
        public int CellZ { get; }
        public int GenerationVersion { get; }

        public bool Equals(TopDown3DRockRootKey other)
        {
            return Tier == other.Tier
                && CellX == other.CellX
                && CellZ == other.CellZ
                && GenerationVersion == other.GenerationVersion;
        }

        public override bool Equals(object obj)
        {
            return obj is TopDown3DRockRootKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Tier;
                hash = hash * 397 ^ CellX;
                hash = hash * 397 ^ CellZ;
                hash = hash * 397 ^ GenerationVersion;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{GenerationVersion}:{Tier}:{CellX}:{CellZ}";
        }
    }

    public readonly struct TopDown3DRockFormationMember : IEquatable<TopDown3DRockFormationMember>
    {
        public TopDown3DRockFormationMember(
            string stableId,
            string definitionStableId,
            TopDown3DRockSizeTier tier,
            TopDown3DNaturalObjectShape shape,
            int variant,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            int memberIndex,
            int parentIndex,
            float supportRadius,
            Bounds worldBounds)
        {
            StableId = stableId;
            DefinitionStableId = definitionStableId;
            Tier = tier;
            Shape = shape;
            Variant = variant;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            MemberIndex = memberIndex;
            ParentIndex = parentIndex;
            SupportRadius = supportRadius;
            WorldBounds = worldBounds;
        }

        public string StableId { get; }
        public string DefinitionStableId { get; }
        public TopDown3DRockSizeTier Tier { get; }
        public TopDown3DNaturalObjectShape Shape { get; }
        public int Variant { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }
        public int MemberIndex { get; }
        public int ParentIndex { get; }
        public float SupportRadius { get; }
        public Bounds WorldBounds { get; }

        public bool Equals(TopDown3DRockFormationMember other)
        {
            return StableId == other.StableId
                && DefinitionStableId == other.DefinitionStableId
                && Tier == other.Tier
                && Shape == other.Shape
                && Variant == other.Variant
                && Position == other.Position
                && Rotation == other.Rotation
                && Scale == other.Scale
                && MemberIndex == other.MemberIndex
                && ParentIndex == other.ParentIndex
                && SupportRadius.Equals(other.SupportRadius)
                && WorldBounds.Equals(other.WorldBounds);
        }

        public override bool Equals(object obj)
        {
            return obj is TopDown3DRockFormationMember other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StableId != null ? StableId.GetHashCode() : 0;
                hash = hash * 397 ^ MemberIndex;
                hash = hash * 397 ^ ParentIndex;
                hash = hash * 397 ^ Position.GetHashCode();
                return hash;
            }
        }
    }

    public sealed class TopDown3DRockFormationPlan : IEquatable<TopDown3DRockFormationPlan>
    {
        private readonly TopDown3DRockFormationMember[] members;

        public TopDown3DRockFormationPlan(
            TopDown3DRockRootKey rootKey,
            string stableId,
            int seed,
            TopDown3DNaturalObjectLayer layer,
            TopDown3DRockSurface surface,
            TopDown3DRockFormationMember[] members,
            Vector2 envelopeCenter,
            float envelopeRadius,
            float height)
        {
            RootKey = rootKey;
            StableId = stableId;
            Seed = seed;
            Layer = layer;
            Surface = surface;
            this.members = members;
            EnvelopeCenter = envelopeCenter;
            EnvelopeRadius = envelopeRadius;
            Height = height;
        }

        public TopDown3DRockRootKey RootKey { get; }
        public string StableId { get; }
        public int Seed { get; }
        public TopDown3DNaturalObjectLayer Layer { get; }
        public TopDown3DRockSurface Surface { get; }
        public IReadOnlyList<TopDown3DRockFormationMember> Members => members;
        public Vector2 EnvelopeCenter { get; }
        public float EnvelopeRadius { get; }
        public float Height { get; }

        public bool Equals(TopDown3DRockFormationPlan other)
        {
            if (other == null
                || !RootKey.Equals(other.RootKey)
                || StableId != other.StableId
                || Seed != other.Seed
                || Layer != other.Layer
                || Surface != other.Surface
                || EnvelopeCenter != other.EnvelopeCenter
                || !EnvelopeRadius.Equals(other.EnvelopeRadius)
                || !Height.Equals(other.Height)
                || members.Length != other.members.Length)
            {
                return false;
            }

            for (var i = 0; i < members.Length; i++)
            {
                if (!members[i].Equals(other.members[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is TopDown3DRockFormationPlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            return RootKey.GetHashCode();
        }
    }

    public static class TopDown3DRockFormationPlanner
    {
        public const int DirectionAttempts = 12;
        private const float GoldenAngleDegrees = 137.507764f;
        private const float MaximumNonParentOverlap = 0.12f;

        public static List<TopDown3DRockFormationPlan> BuildPhysicalFormations(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter)
        {
            var formations = new List<TopDown3DRockFormationPlan>();
            if (settings == null || catalog == null)
            {
                return formations;
            }

            BuildTier(
                settings,
                catalog,
                chunkCoordinate,
                spawnExclusionCenter,
                TopDown3DRockSizeTier.Towering,
                formations);
            BuildTier(
                settings,
                catalog,
                chunkCoordinate,
                spawnExclusionCenter,
                TopDown3DRockSizeTier.Massive,
                formations);
            BuildTier(
                settings,
                catalog,
                chunkCoordinate,
                spawnExclusionCenter,
                TopDown3DRockSizeTier.Large,
                formations);
            return formations;
        }

        private static void BuildTier(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter,
            TopDown3DRockSizeTier tier,
            ICollection<TopDown3DRockFormationPlan> output)
        {
            var definitions = GetDefinitions(catalog, tier);
            var config = GetConfig(settings, tier);
            if (definitions.Count == 0 || config.TargetCount <= 0f)
            {
                return;
            }

            var cellSize = GetCellSize(settings.ChunkSize, config.TargetCount);
            var cellsPerChunk = (settings.ChunkSize / cellSize) * (settings.ChunkSize / cellSize);
            var baseAdmission = Mathf.Clamp01(config.TargetCount / Mathf.Max(1f, cellsPerChunk));
            var layerSeed = GetTierSeed(settings, tier);
            var originX = chunkCoordinate.x * settings.ChunkSize;
            var originZ = chunkCoordinate.y * settings.ChunkSize;
            var minCellX = Mathf.FloorToInt(originX / cellSize);
            var maxCellX = Mathf.FloorToInt((originX + settings.ChunkSize) / cellSize);
            var minCellZ = Mathf.FloorToInt(originZ / cellSize);
            var maxCellZ = Mathf.FloorToInt((originZ + settings.ChunkSize) / cellSize);

            for (var cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
            {
                for (var cellX = minCellX; cellX <= maxCellX; cellX++)
                {
                    var candidate = BuildCandidate(layerSeed, cellX, cellZ, cellSize);
                    if (!BelongsToChunk(candidate.Position, chunkCoordinate, settings.ChunkSize)
                        || !TryBuildRootMember(
                            settings,
                            definitions,
                            config,
                            tier,
                            candidate,
                            baseAdmission,
                            spawnExclusionCenter,
                            out var root))
                    {
                        continue;
                    }

                    var formation = BuildFormation(
                        settings,
                        catalog,
                        tier,
                        candidate,
                        root,
                        spawnExclusionCenter);
                    if (LosesCompetition(
                            settings,
                            catalog,
                            tier,
                            config,
                            candidate,
                            formation,
                            spawnExclusionCenter))
                    {
                        continue;
                    }

                    output.Add(formation);
                }
            }
        }

        private static bool TryBuildRootMember(
            TopDown3DWorldSettings settings,
            IReadOnlyList<TopDown3DNaturalObjectDefinition> definitions,
            TierConfig config,
            TopDown3DRockSizeTier tier,
            Candidate candidate,
            float baseAdmission,
            Vector2 spawnExclusionCenter,
            out TopDown3DRockFormationMember root)
        {
            root = default;
            var abundance = TopDown3DNaturalObjectPlanner.SampleRockAbundance(
                settings,
                candidate.Position);
            if (candidate.Admission > Mathf.Clamp01(baseAdmission * abundance))
            {
                return false;
            }

            var definition = SelectDefinition(definitions, candidate.Selection);
            var scale = GetScale(definition, candidate.Scale);
            var variant = GetVariant(candidate.Variant);
            var normal = TopDown3DHeightSampler.SampleNormal(
                settings,
                candidate.Position.x,
                candidate.Position.y);
            var slope = Vector3.Angle(normal, Vector3.up);
            if (slope > config.MaximumSlope)
            {
                return false;
            }

            var rotation = GetGroundedRotation(definition, normal, candidate.Yaw * 360f);
            var support = GetProjectedSupportRadius(definition.Shape, variant, rotation, scale);
            if (Vector2.Distance(candidate.Position, spawnExclusionCenter)
                < settings.ClearSpawnRadius + support)
            {
                return false;
            }

            var height = TopDown3DHeightSampler.SampleHeight(
                settings,
                candidate.Position.x,
                candidate.Position.y);
            var position = new Vector3(
                candidate.Position.x,
                height - definition.SinkDepth * scale.y,
                candidate.Position.y);
            var key = new TopDown3DRockRootKey(
                tier,
                candidate.CellX,
                candidate.CellZ,
                settings.PhysicalRockGenerationVersion);
            var stableId = $"rock:{settings.WorldSeed}:{key}:0";
            root = CreateMember(
                stableId,
                definition,
                tier,
                variant,
                position,
                rotation,
                scale,
                0,
                -1);
            return true;
        }

        private static bool LosesCompetition(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DRockSizeTier tier,
            TierConfig config,
            Candidate candidate,
            TopDown3DRockFormationPlan formation,
            Vector2 spawnExclusionCenter)
        {
            for (var otherTierValue = (int)tier; otherTierValue <= (int)TopDown3DRockSizeTier.Towering; otherTierValue++)
            {
                var otherTier = (TopDown3DRockSizeTier)otherTierValue;
                var otherDefinitions = GetDefinitions(catalog, otherTier);
                var otherConfig = GetConfig(settings, otherTier);
                if (otherDefinitions.Count == 0 || otherConfig.TargetCount <= 0f)
                {
                    continue;
                }

                var otherCellSize = GetCellSize(settings.ChunkSize, otherConfig.TargetCount);
                var otherCellsPerChunk = (settings.ChunkSize / otherCellSize)
                    * (settings.ChunkSize / otherCellSize);
                var otherAdmission = Mathf.Clamp01(
                    otherConfig.TargetCount / Mathf.Max(1f, otherCellsPerChunk));
                var searchDistance = formation.EnvelopeRadius
                    + GetMaximumFormationRadius(settings, otherTier, otherDefinitions)
                    + Mathf.Max(config.Spacing, otherConfig.Spacing);
                var range = Mathf.Max(1, Mathf.CeilToInt(searchDistance / otherCellSize) + 1);
                var centerCellX = Mathf.FloorToInt(candidate.Position.x / otherCellSize);
                var centerCellZ = Mathf.FloorToInt(candidate.Position.y / otherCellSize);
                var otherSeed = GetTierSeed(settings, otherTier);
                for (var z = -range; z <= range; z++)
                {
                    for (var x = -range; x <= range; x++)
                    {
                        var other = BuildCandidate(
                            otherSeed,
                            centerCellX + x,
                            centerCellZ + z,
                            otherCellSize);
                        if (otherTier == tier
                            && other.CellX == candidate.CellX
                            && other.CellZ == candidate.CellZ)
                        {
                            continue;
                        }

                        if (!TryBuildRootMember(
                                settings,
                                otherDefinitions,
                                otherConfig,
                                otherTier,
                                other,
                                otherAdmission,
                                spawnExclusionCenter,
                                out var otherRoot))
                        {
                            continue;
                        }

                        var otherFormation = BuildFormation(
                            settings,
                            catalog,
                            otherTier,
                            other,
                            otherRoot,
                            spawnExclusionCenter);
                        var minimumDistance = formation.EnvelopeRadius
                            + otherFormation.EnvelopeRadius
                            + Mathf.Max(config.Spacing, otherConfig.Spacing);
                        if ((otherFormation.EnvelopeCenter - formation.EnvelopeCenter).sqrMagnitude
                            >= minimumDistance * minimumDistance)
                        {
                            continue;
                        }

                        if (otherTier > tier
                            || other.Priority > candidate.Priority
                            || (Mathf.Approximately(other.Priority, candidate.Priority)
                                && StableCellOrder(
                                    other.CellX,
                                    other.CellZ,
                                    candidate.CellX,
                                    candidate.CellZ) < 0))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static TopDown3DRockFormationPlan BuildFormation(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DRockSizeTier rootTier,
            Candidate candidate,
            TopDown3DRockFormationMember root,
            Vector2 spawnExclusionCenter)
        {
            var key = new TopDown3DRockRootKey(
                rootTier,
                candidate.CellX,
                candidate.CellZ,
                settings.PhysicalRockGenerationVersion);
            var formationStableId = $"rock:{settings.WorldSeed}:{key}";
            var members = new List<TopDown3DRockFormationMember> { root };
            var parentIndex = 0;
            var depth = 0;
            var consecutiveLargeDepth = rootTier == TopDown3DRockSizeTier.Large ? 0 : -1;
            while (members.Count < settings.PhysicalFormationMaximumMembers
                && depth < settings.PhysicalFormationMaximumDepth)
            {
                var parent = members[parentIndex];
                var childTier = GetChildTier(parent.Tier);
                if (childTier == TopDown3DRockSizeTier.None)
                {
                    break;
                }

                var branchSeed = StableHash(candidate.FormationSeed, members.Count, depth + 0x4139);
                var chance = GetChildChance(settings, parent.Tier, consecutiveLargeDepth);
                if (Hash01(branchSeed ^ 0x5D27A1E3) > chance
                    || !TryCreateChild(
                        settings,
                        catalog,
                        formationStableId,
                        branchSeed,
                        childTier,
                        parentIndex,
                        members,
                        spawnExclusionCenter,
                        out var child))
                {
                    break;
                }

                members.Add(child);
                parentIndex = child.MemberIndex;
                depth++;
                consecutiveLargeDepth = childTier == TopDown3DRockSizeTier.Large
                    ? Mathf.Max(0, consecutiveLargeDepth + 1)
                    : -1;
            }

            GetEnvelope(members, out var center, out var radius, out var height);
            return new TopDown3DRockFormationPlan(
                key,
                formationStableId,
                candidate.FormationSeed,
                rootTier == TopDown3DRockSizeTier.Towering
                    ? TopDown3DNaturalObjectLayer.Landmark
                    : TopDown3DNaturalObjectLayer.Obstacle,
                TopDown3DNaturalObjectPlanner.SampleRockSurface(settings, candidate.Position),
                members.ToArray(),
                center,
                radius,
                height);
        }

        private static bool TryCreateChild(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog,
            string formationStableId,
            int seed,
            TopDown3DRockSizeTier childTier,
            int parentIndex,
            IReadOnlyList<TopDown3DRockFormationMember> members,
            Vector2 spawnExclusionCenter,
            out TopDown3DRockFormationMember child)
        {
            child = default;
            var definitions = GetDefinitions(catalog, childTier);
            if (definitions.Count == 0)
            {
                return false;
            }

            var definition = SelectDefinition(definitions, Hash01(seed ^ 0x719D3A11));
            var scale = GetScale(definition, Hash01(seed ^ 0x41B92C57));
            var variant = GetVariant(Hash01(seed ^ 0x2C7158E9));
            var yaw = Hash01(seed ^ 0x6A91E3D5) * 360f;
            var parent = members[parentIndex];
            var baseAngle = Hash01(seed ^ 0x173BC8A1) * 360f;
            var axis = new Vector2(
                Mathf.Cos(baseAngle * Mathf.Deg2Rad),
                Mathf.Sin(baseAngle * Mathf.Deg2Rad));
            var bestScore = float.NegativeInfinity;
            var found = false;
            for (var attempt = 0; attempt < DirectionAttempts; attempt++)
            {
                var angle = (baseAngle + attempt * GoldenAngleDegrees) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var provisionalNormal = TopDown3DHeightSampler.SampleNormal(
                    settings,
                    parent.Position.x,
                    parent.Position.z);
                var provisionalRotation = GetGroundedRotation(definition, provisionalNormal, yaw);
                var support = GetProjectedSupportRadius(
                    definition.Shape,
                    variant,
                    provisionalRotation,
                    scale);
                var distance = (parent.SupportRadius + support)
                    * (1f - settings.FormationContactInset);
                var worldXZ = new Vector2(parent.Position.x, parent.Position.z) + direction * distance;
                var normal = TopDown3DHeightSampler.SampleNormal(settings, worldXZ.x, worldXZ.y);
                var slope = Vector3.Angle(normal, Vector3.up);
                if (slope > GetConfig(settings, childTier).MaximumSlope)
                {
                    continue;
                }

                var rotation = GetGroundedRotation(definition, normal, yaw);
                support = GetProjectedSupportRadius(definition.Shape, variant, rotation, scale);
                if (Vector2.Distance(worldXZ, spawnExclusionCenter)
                    < settings.ClearSpawnRadius + support)
                {
                    continue;
                }

                var surfaceHeight = TopDown3DHeightSampler.SampleHeight(settings, worldXZ.x, worldXZ.y);
                var position = new Vector3(
                    worldXZ.x,
                    surfaceHeight - definition.SinkDepth * scale.y,
                    worldXZ.y);
                var candidate = CreateMember(
                    $"{formationStableId}:{members.Count}",
                    definition,
                    childTier,
                    variant,
                    position,
                    rotation,
                    scale,
                    members.Count,
                    parentIndex);
                if (!HasVerticalOverlap(parent.WorldBounds, candidate.WorldBounds)
                    || OverlapsNonParent(candidate, members, parentIndex))
                {
                    continue;
                }

                var rootPosition = new Vector2(members[0].Position.x, members[0].Position.z);
                var envelopeGrowth = Vector2.Distance(rootPosition, worldXZ) + candidate.SupportRadius;
                var crowding = GetCrowdingScore(candidate, members, parentIndex);
                var score = Vector2.Dot(direction, axis) * 0.45f
                    - envelopeGrowth * 0.035f
                    + crowding * 0.2f
                    - attempt * 0.0001f;
                if (!found || score > bestScore)
                {
                    child = candidate;
                    bestScore = score;
                    found = true;
                }
            }

            return found;
        }

        private static TopDown3DRockFormationMember CreateMember(
            string stableId,
            TopDown3DNaturalObjectDefinition definition,
            TopDown3DRockSizeTier tier,
            int variant,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            int memberIndex,
            int parentIndex)
        {
            var support = GetProjectedSupportRadius(definition.Shape, variant, rotation, scale);
            var bounds = GetWorldBounds(definition.Shape, variant, position, rotation, scale);
            return new TopDown3DRockFormationMember(
                stableId,
                definition.StableId,
                tier,
                definition.Shape,
                variant,
                position,
                rotation,
                scale,
                memberIndex,
                parentIndex,
                support,
                bounds);
        }

        private static float GetProjectedSupportRadius(
            TopDown3DNaturalObjectShape shape,
            int variant,
            Quaternion rotation,
            Vector3 scale)
        {
            var vertices = TopDown3DNaturalMeshLibrary.GetData(shape, variant).Vertices;
            var maximum = 0f;
            for (var i = 0; i < vertices.Length; i++)
            {
                var point = rotation * Vector3.Scale(vertices[i], scale);
                maximum = Mathf.Max(maximum, new Vector2(point.x, point.z).magnitude);
            }

            return Mathf.Max(0.05f, maximum);
        }

        private static Bounds GetWorldBounds(
            TopDown3DNaturalObjectShape shape,
            int variant,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            var vertices = TopDown3DNaturalMeshLibrary.GetData(shape, variant).Vertices;
            var matrix = Matrix4x4.TRS(position, rotation, scale);
            var bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            for (var i = 1; i < vertices.Length; i++)
            {
                bounds.Encapsulate(matrix.MultiplyPoint3x4(vertices[i]));
            }

            return bounds;
        }

        private static bool HasVerticalOverlap(Bounds parent, Bounds child)
        {
            return Mathf.Min(parent.max.y, child.max.y) > Mathf.Max(parent.min.y, child.min.y);
        }

        private static bool OverlapsNonParent(
            TopDown3DRockFormationMember candidate,
            IReadOnlyList<TopDown3DRockFormationMember> members,
            int parentIndex)
        {
            var candidatePosition = new Vector2(candidate.Position.x, candidate.Position.z);
            for (var i = 0; i < members.Count; i++)
            {
                if (i == parentIndex)
                {
                    continue;
                }

                var other = members[i];
                var minimumDistance = (candidate.SupportRadius + other.SupportRadius)
                    * (1f - MaximumNonParentOverlap);
                var otherPosition = new Vector2(other.Position.x, other.Position.z);
                if ((candidatePosition - otherPosition).sqrMagnitude
                    < minimumDistance * minimumDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetCrowdingScore(
            TopDown3DRockFormationMember candidate,
            IReadOnlyList<TopDown3DRockFormationMember> members,
            int parentIndex)
        {
            var minimumClearance = 1f;
            var candidatePosition = new Vector2(candidate.Position.x, candidate.Position.z);
            for (var i = 0; i < members.Count; i++)
            {
                if (i == parentIndex)
                {
                    continue;
                }

                var other = members[i];
                var otherPosition = new Vector2(other.Position.x, other.Position.z);
                var clearance = Vector2.Distance(candidatePosition, otherPosition)
                    / Mathf.Max(0.01f, candidate.SupportRadius + other.SupportRadius);
                minimumClearance = Mathf.Min(minimumClearance, Mathf.Clamp01(clearance));
            }

            return minimumClearance;
        }

        private static void GetEnvelope(
            IReadOnlyList<TopDown3DRockFormationMember> members,
            out Vector2 center,
            out float radius,
            out float height)
        {
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minZ = float.PositiveInfinity;
            var maxZ = float.NegativeInfinity;
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            for (var i = 0; i < members.Count; i++)
            {
                var bounds = members[i].WorldBounds;
                minX = Mathf.Min(minX, bounds.min.x);
                maxX = Mathf.Max(maxX, bounds.max.x);
                minZ = Mathf.Min(minZ, bounds.min.z);
                maxZ = Mathf.Max(maxZ, bounds.max.z);
                minY = Mathf.Min(minY, bounds.min.y);
                maxY = Mathf.Max(maxY, bounds.max.y);
            }

            center = new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);
            radius = 0f;
            for (var i = 0; i < members.Count; i++)
            {
                var position = new Vector2(members[i].Position.x, members[i].Position.z);
                radius = Mathf.Max(
                    radius,
                    Vector2.Distance(center, position) + members[i].SupportRadius);
            }

            height = Mathf.Max(0f, maxY - minY);
        }

        private static float GetChildChance(
            TopDown3DWorldSettings settings,
            TopDown3DRockSizeTier parentTier,
            int consecutiveLargeDepth)
        {
            switch (parentTier)
            {
                case TopDown3DRockSizeTier.Towering:
                    return settings.ToweringToMassiveChance;
                case TopDown3DRockSizeTier.Massive:
                    return settings.MassiveToLargeChance;
                case TopDown3DRockSizeTier.Large:
                    return settings.LargeToLargeChance
                        * Mathf.Pow(settings.LargeContinuationDecay, Mathf.Max(0, consecutiveLargeDepth));
                default:
                    return 0f;
            }
        }

        private static TopDown3DRockSizeTier GetChildTier(TopDown3DRockSizeTier parentTier)
        {
            switch (parentTier)
            {
                case TopDown3DRockSizeTier.Towering:
                    return TopDown3DRockSizeTier.Massive;
                case TopDown3DRockSizeTier.Massive:
                case TopDown3DRockSizeTier.Large:
                    return TopDown3DRockSizeTier.Large;
                default:
                    return TopDown3DRockSizeTier.None;
            }
        }

        private static Quaternion GetGroundedRotation(
            TopDown3DNaturalObjectDefinition definition,
            Vector3 normal,
            float yawDegrees)
        {
            var slope = Vector3.Angle(normal, Vector3.up);
            var tiltRatio = slope > 0.001f
                ? Mathf.Min(1f, definition.MaximumTilt / slope)
                : 0f;
            var tilt = Quaternion.Slerp(
                Quaternion.identity,
                Quaternion.FromToRotation(Vector3.up, normal),
                tiltRatio);
            return tilt * Quaternion.AngleAxis(yawDegrees, Vector3.up);
        }

        private static Vector3 GetScale(TopDown3DNaturalObjectDefinition definition, float sample)
        {
            var range = definition.UniformScaleRange;
            return definition.Proportions * Mathf.Lerp(range.x, range.y, sample);
        }

        private static int GetVariant(float sample)
        {
            return Mathf.FloorToInt(sample * TopDown3DNaturalMeshLibrary.VariantsPerShape)
                % TopDown3DNaturalMeshLibrary.VariantsPerShape;
        }

        private static List<TopDown3DNaturalObjectDefinition> GetDefinitions(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DRockSizeTier tier)
        {
            var matches = new List<TopDown3DNaturalObjectDefinition>();
            var definitions = catalog.Definitions;
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].RockSizeTier == tier)
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

        private static float GetMaximumRootSupport(
            IReadOnlyList<TopDown3DNaturalObjectDefinition> definitions)
        {
            var maximum = 0f;
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var scale = definition.Proportions * definition.UniformScaleRange.y;
                maximum = Mathf.Max(
                    maximum,
                    GetProjectedSupportRadius(
                        definition.Shape,
                        0,
                        Quaternion.identity,
                        scale) * 1.25f);
            }

            return maximum;
        }

        private static float GetMaximumFormationRadius(
            TopDown3DWorldSettings settings,
            TopDown3DRockSizeTier rootTier,
            IReadOnlyList<TopDown3DNaturalObjectDefinition> rootDefinitions)
        {
            var radius = GetMaximumRootSupport(rootDefinitions);
            var childTier = GetChildTier(rootTier);
            var catalog = settings.NaturalObjectCatalog;
            var remainingMembers = Mathf.Min(
                settings.PhysicalFormationMaximumMembers - 1,
                settings.PhysicalFormationMaximumDepth);
            while (childTier != TopDown3DRockSizeTier.None
                && catalog != null
                && remainingMembers-- > 0)
            {
                radius += GetMaximumRootSupport(GetDefinitions(catalog, childTier)) * 2f;
                if (childTier == TopDown3DRockSizeTier.Large)
                {
                    continue;
                }

                childTier = GetChildTier(childTier);
            }

            return radius;
        }

        private static TierConfig GetConfig(
            TopDown3DWorldSettings settings,
            TopDown3DRockSizeTier tier)
        {
            switch (tier)
            {
                case TopDown3DRockSizeTier.Large:
                    return new TierConfig(
                        settings.PropsPerChunk,
                        settings.PropSpacing,
                        settings.MaximumPropSlope);
                case TopDown3DRockSizeTier.Massive:
                    return new TierConfig(
                        settings.MassiveRocksPerChunk,
                        settings.MassiveRockSpacing,
                        settings.MaximumMassiveRockSlope);
                case TopDown3DRockSizeTier.Towering:
                    return new TierConfig(
                        settings.LandmarksPerChunk,
                        settings.LandmarkSpacing,
                        settings.MaximumLandmarkSlope);
                default:
                    return default;
            }
        }

        private static float GetCellSize(float chunkSize, float targetCount)
        {
            return Mathf.Max(
                0.18f,
                Mathf.Sqrt((chunkSize * chunkSize) / Mathf.Max(1f, targetCount * 2.25f)));
        }

        private static int GetTierSeed(TopDown3DWorldSettings settings, TopDown3DRockSizeTier tier)
        {
            return StableHash(
                settings.WorldSeed,
                settings.PhysicalRockGenerationVersion,
                (int)tier * 104729 + 0x5231);
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
                Hash01(hash ^ 0x631F8D29),
                StableHash(hash, cellX ^ 0x416D2E3B, cellZ ^ 0x2D1F7A65));
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

        private readonly struct TierConfig
        {
            public TierConfig(float targetCount, float spacing, float maximumSlope)
            {
                TargetCount = targetCount;
                Spacing = spacing;
                MaximumSlope = maximumSlope;
            }

            public float TargetCount { get; }
            public float Spacing { get; }
            public float MaximumSlope { get; }
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
                float variant,
                int formationSeed)
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
                FormationSeed = formationSeed;
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
            public int FormationSeed { get; }
        }
    }
}
