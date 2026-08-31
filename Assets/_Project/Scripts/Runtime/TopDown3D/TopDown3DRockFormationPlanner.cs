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
        private static readonly TopDown3DRockSizeTier[] PhysicalTiersDescending =
        {
            TopDown3DRockSizeTier.Towering,
            TopDown3DRockSizeTier.Massive,
            TopDown3DRockSizeTier.ExtraLarge,
            TopDown3DRockSizeTier.Large,
            TopDown3DRockSizeTier.Medium,
            TopDown3DRockSizeTier.Small
        };

        public static List<TopDown3DRockFormationPlan> BuildPhysicalFormations(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter)
        {
            var formations = new List<TopDown3DRockFormationPlan>();
            if (settings == null || catalog == null)
            {
                return formations;
            }

            var tierPlanningData = BuildTierPlanningData(settings, catalog);
            var formationCache = new Dictionary<TopDown3DRockRootKey, TopDown3DRockFormationPlan>();
            for (var tierIndex = 0; tierIndex < tierPlanningData.Length; tierIndex++)
            {
                BuildTier(
                    settings,
                    generator,
                    catalog,
                    chunkCoordinate,
                    spawnExclusionCenter,
                    tierPlanningData[tierIndex],
                    tierPlanningData,
                    formationCache,
                    formations);
            }

            return formations;
        }

        private static void BuildTier(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter,
            TierPlanningData tierData,
            IReadOnlyList<TierPlanningData> allTierData,
            IDictionary<TopDown3DRockRootKey, TopDown3DRockFormationPlan> formationCache,
            ICollection<TopDown3DRockFormationPlan> output)
        {
            var tier = tierData.Tier;
            var definitions = tierData.Definitions;
            var config = tierData.Config;
            if (definitions.Count == 0 || config.TargetCount <= 0f)
            {
                return;
            }

            var cellSize = tierData.CellSize;
            var layerSeed = tierData.LayerSeed;
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
                        || !TryBuildFormationPlan(
                            settings,
                            generator,
                            catalog,
                            tierData,
                            candidate,
                            spawnExclusionCenter,
                            formationCache,
                            out var formation))
                    {
                        continue;
                    }

                    if (LosesCompetition(
                            settings,
                            generator,
                            catalog,
                            tierData,
                            allTierData,
                            candidate,
                            formation,
                            spawnExclusionCenter,
                            formationCache))
                    {
                        continue;
                    }

                    output.Add(formation);
                }
            }
        }

        private static bool TryBuildRootMember(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            IReadOnlyList<TopDown3DNaturalObjectDefinition> definitions,
            TierConfig config,
            TopDown3DRockSizeTier tier,
            Candidate candidate,
            float baseAdmission,
            Vector2 spawnExclusionCenter,
            out TopDown3DRockFormationMember root)
        {
            root = default;
            var sharedAbundance = TopDown3DNaturalObjectPlanner.SampleRockAbundance(
                settings,
                candidate.Position);
            var formationAbundance = Mathf.Lerp(
                0.78f,
                1.35f,
                Mathf.Clamp01(sharedAbundance / 2.2f));
            var surface = generator.Sample(candidate.Position.x, candidate.Position.y);
            // Large formations can rise through red dirt. Visible bedrock remains a useful
            // geological signal, but it must not be the admission gate for the formation
            // hierarchy or sparse terrain texturing will erase the world's silhouettes.
            var geologySuitability = 0.72f
                + surface.BedrockWeight * 0.35f
                + surface.GravelWeight * 0.20f
                + surface.Talus * 0.28f
                + surface.Lithology * 0.12f
                + Mathf.Abs(surface.Curvature) * 0.14f;
            var corridorProtection = GetTierRank(tier) >= GetTierRank(TopDown3DRockSizeTier.Large)
                ? Mathf.Lerp(1f, 0.04f, surface.TraversalCorridor)
                : Mathf.Lerp(1f, 0.35f, surface.TraversalCorridor);
            if (candidate.Admission > Mathf.Clamp01(
                    baseAdmission
                    * formationAbundance
                    * geologySuitability
                    * corridorProtection))
            {
                return false;
            }

            var definition = SelectDefinition(definitions, candidate.Selection);
            var scale = GetScale(definition, candidate.Scale);
            var variant = GetVariant(candidate.Variant);
            var normal = generator.SampleNormal(candidate.Position.x, candidate.Position.y);
            var slope = Vector3.Angle(normal, Vector3.up);
            if (slope > config.MaximumSlope)
            {
                return false;
            }

            var rotation = GetGroundedRotation(definition, normal, candidate.Yaw * 360f);
            var support = GetProjectedSupportRadius(catalog, definition.Shape, variant, rotation, scale);
            if (Vector2.Distance(candidate.Position, spawnExclusionCenter)
                < settings.ClearSpawnRadius + support)
            {
                return false;
            }

            var height = generator.SampleHeight(candidate.Position.x, candidate.Position.y);
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
                catalog,
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
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            TierPlanningData tierData,
            IReadOnlyList<TierPlanningData> allTierData,
            Candidate candidate,
            TopDown3DRockFormationPlan formation,
            Vector2 spawnExclusionCenter,
            IDictionary<TopDown3DRockRootKey, TopDown3DRockFormationPlan> formationCache)
        {
            var tier = tierData.Tier;
            var config = tierData.Config;
            var tierRank = GetTierRank(tier);
            for (var tierIndex = 0; tierIndex < allTierData.Count; tierIndex++)
            {
                var otherData = allTierData[tierIndex];
                var otherTier = otherData.Tier;
                if (GetTierRank(otherTier) < tierRank)
                {
                    continue;
                }

                var otherDefinitions = otherData.Definitions;
                var otherConfig = otherData.Config;
                if (otherDefinitions.Count == 0 || otherConfig.TargetCount <= 0f)
                {
                    continue;
                }

                var otherCellSize = otherData.CellSize;
                var competitionSpacing = GetCompetitionSpacing(
                    tier,
                    config,
                    otherTier,
                    otherConfig);
                var searchDistance = formation.EnvelopeRadius
                    + otherData.MaximumFormationRadius
                    + competitionSpacing;
                var range = Mathf.Max(1, Mathf.CeilToInt(searchDistance / otherCellSize) + 1);
                var centerCellX = Mathf.FloorToInt(candidate.Position.x / otherCellSize);
                var centerCellZ = Mathf.FloorToInt(candidate.Position.y / otherCellSize);
                var otherSeed = otherData.LayerSeed;
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

                        if (!TryBuildFormationPlan(
                                settings,
                                generator,
                                catalog,
                                otherData,
                                other,
                                spawnExclusionCenter,
                                formationCache,
                                out var otherFormation))
                        {
                            continue;
                        }

                        var minimumDistance = formation.EnvelopeRadius
                            + otherFormation.EnvelopeRadius
                            + competitionSpacing;
                        if ((otherFormation.EnvelopeCenter - formation.EnvelopeCenter).sqrMagnitude
                            >= minimumDistance * minimumDistance)
                        {
                            continue;
                        }

                        if (GetTierRank(otherTier) > tierRank
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

        private static bool TryBuildFormationPlan(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            TierPlanningData tierData,
            Candidate candidate,
            Vector2 spawnExclusionCenter,
            IDictionary<TopDown3DRockRootKey, TopDown3DRockFormationPlan> formationCache,
            out TopDown3DRockFormationPlan formation)
        {
            var key = new TopDown3DRockRootKey(
                tierData.Tier,
                candidate.CellX,
                candidate.CellZ,
                settings.PhysicalRockGenerationVersion);
            if (formationCache.TryGetValue(key, out formation))
            {
                return formation != null;
            }

            if (!TryBuildRootMember(
                    settings,
                    generator,
                    catalog,
                    tierData.Definitions,
                    tierData.Config,
                    tierData.Tier,
                    candidate,
                    tierData.BaseAdmission,
                    spawnExclusionCenter,
                    out var root))
            {
                formationCache.Add(key, null);
                formation = null;
                return false;
            }

            formation = BuildFormation(
                settings,
                generator,
                catalog,
                tierData.Tier,
                candidate,
                root,
                spawnExclusionCenter);
            formationCache.Add(key, formation);
            return true;
        }

        private static TopDown3DRockFormationPlan BuildFormation(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
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
            var memberDepths = new List<int> { 0 };
            for (var parentIndex = 0;
                 parentIndex < members.Count
                 && members.Count < settings.PhysicalFormationMaximumMembers;
                 parentIndex++)
            {
                var depth = memberDepths[parentIndex];
                if (depth >= settings.PhysicalFormationMaximumDepth)
                {
                    continue;
                }

                var parent = members[parentIndex];
                var childTier = GetChildTier(parent.Tier);
                if (childTier == TopDown3DRockSizeTier.None)
                {
                    continue;
                }

                var baseChance = GetChildChance(settings, parent.Tier);
                for (var childSlot = 0;
                     childSlot < settings.FormationMaximumChildrenPerParent
                     && members.Count < settings.PhysicalFormationMaximumMembers;
                     childSlot++)
                {
                    var branchSeed = StableHash(
                        candidate.FormationSeed,
                        parentIndex * 4099 + childSlot * 131,
                        depth + 0x4139);
                    var chance = baseChance * Mathf.Pow(
                        settings.AdditionalChildChanceMultiplier,
                        childSlot);
                    if (Hash01(branchSeed ^ 0x5D27A1E3) > chance
                        || !TryCreateChild(
                            settings,
                            generator,
                            catalog,
                            formationStableId,
                            branchSeed,
                            childTier,
                            parentIndex,
                            members,
                            spawnExclusionCenter,
                            out var child))
                    {
                        continue;
                    }

                    members.Add(child);
                    memberDepths.Add(depth + 1);
                }
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
            TopDown3DWorldGenerator generator,
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
            var parentDistanceRatio = Mathf.Lerp(
                settings.FormationMinimumParentDistanceRatio,
                settings.FormationMaximumParentDistanceRatio,
                Mathf.Pow(Hash01(seed ^ 0x34A71C9D), 1.6f));
            var parent = members[parentIndex];
            var parentSolid = TopDown3DRockVolumeOverlap.CreateWorldSolid(catalog, parent);
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
                var provisionalNormal = generator.SampleNormal(parent.Position.x, parent.Position.z);
                var provisionalRotation = GetGroundedRotation(definition, provisionalNormal, yaw);
                var parentContactRadius = GetDirectionalSupportRadius(
                    catalog,
                    parent.Shape,
                    parent.Variant,
                    parent.Rotation,
                    parent.Scale,
                    direction);
                var childContactRadius = GetDirectionalSupportRadius(
                    catalog,
                    definition.Shape,
                    variant,
                    provisionalRotation,
                    scale,
                    -direction);
                var distance = (parentContactRadius + childContactRadius) * parentDistanceRatio;
                var worldXZ = new Vector2(parent.Position.x, parent.Position.z) + direction * distance;
                var normal = generator.SampleNormal(worldXZ.x, worldXZ.y);
                var slope = Vector3.Angle(normal, Vector3.up);
                if (slope > GetConfig(settings, childTier).MaximumSlope)
                {
                    continue;
                }

                var rotation = GetGroundedRotation(definition, normal, yaw);
                childContactRadius = GetDirectionalSupportRadius(
                    catalog,
                    definition.Shape,
                    variant,
                    rotation,
                    scale,
                    -direction);
                distance = (parentContactRadius + childContactRadius) * parentDistanceRatio;
                worldXZ = new Vector2(parent.Position.x, parent.Position.z) + direction * distance;
                normal = generator.SampleNormal(worldXZ.x, worldXZ.y);
                slope = Vector3.Angle(normal, Vector3.up);
                if (slope > GetConfig(settings, childTier).MaximumSlope)
                {
                    continue;
                }

                rotation = GetGroundedRotation(definition, normal, yaw);
                var support = GetProjectedSupportRadius(
                    catalog,
                    definition.Shape,
                    variant,
                    rotation,
                    scale);
                if (Vector2.Distance(worldXZ, spawnExclusionCenter)
                    < settings.ClearSpawnRadius + support)
                {
                    continue;
                }

                var surfaceHeight = generator.SampleHeight(worldXZ.x, worldXZ.y);
                var position = new Vector3(
                    worldXZ.x,
                    surfaceHeight - definition.SinkDepth * scale.y,
                    worldXZ.y);
                var candidate = CreateMember(
                    catalog,
                    $"{formationStableId}:{members.Count}",
                    definition,
                    childTier,
                    variant,
                    position,
                    rotation,
                    scale,
                    members.Count,
                    parentIndex);
                var candidateSolid = TopDown3DRockVolumeOverlap.CreateWorldSolid(catalog, candidate);
                if (!HasVerticalOverlap(parent.WorldBounds, candidate.WorldBounds)
                    || !TopDown3DRockVolumeOverlap.HasPositiveVolumeOverlap(
                        parentSolid,
                        candidateSolid)
                    || OverlapsNonParent(
                        candidate,
                        members,
                        parentIndex))
                {
                    continue;
                }

                var rootPosition = new Vector2(members[0].Position.x, members[0].Position.z);
                var envelopeGrowth = Vector2.Distance(rootPosition, worldXZ) + candidate.SupportRadius;
                var crowding = GetCrowdingScore(candidate, members, parentIndex);
                var score = Vector2.Dot(direction, axis) * 0.25f
                    - envelopeGrowth * 0.08f
                    + crowding * 0.05f
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
            TopDown3DNaturalObjectCatalog catalog,
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
            var support = GetProjectedSupportRadius(
                catalog,
                definition.Shape,
                variant,
                rotation,
                scale);
            var bounds = GetWorldBounds(
                catalog,
                definition.Shape,
                variant,
                position,
                rotation,
                scale);
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
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DNaturalObjectShape shape,
            int variant,
            Quaternion rotation,
            Vector3 scale)
        {
            var vertices = catalog.GetRequiredLod0Data(shape, variant).Vertices;
            var maximum = 0f;
            for (var i = 0; i < vertices.Length; i++)
            {
                var point = rotation * Vector3.Scale(vertices[i], scale);
                maximum = Mathf.Max(maximum, new Vector2(point.x, point.z).magnitude);
            }

            return Mathf.Max(0.05f, maximum);
        }

        private static float GetDirectionalSupportRadius(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DNaturalObjectShape shape,
            int variant,
            Quaternion rotation,
            Vector3 scale,
            Vector2 direction)
        {
            direction.Normalize();
            var vertices = catalog.GetRequiredLod0Data(shape, variant).Vertices;
            var maximum = 0f;
            for (var i = 0; i < vertices.Length; i++)
            {
                var point = rotation * Vector3.Scale(vertices[i], scale);
                maximum = Mathf.Max(maximum, point.x * direction.x + point.z * direction.y);
            }

            return Mathf.Max(0.025f, maximum);
        }

        private static Bounds GetWorldBounds(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DNaturalObjectShape shape,
            int variant,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            var vertices = catalog.GetRequiredLod0Data(shape, variant).Vertices;
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
                // Projected support radii conservatively contain every horizontal vertex.
                // Keeping these circles disjoint prevents non-parent volume intersections.
                var minimumDistance = candidate.SupportRadius + other.SupportRadius;
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
            TopDown3DRockSizeTier parentTier)
        {
            switch (parentTier)
            {
                case TopDown3DRockSizeTier.Towering:
                    return settings.ToweringToMassiveChance;
                case TopDown3DRockSizeTier.Massive:
                    return settings.MassiveToExtraLargeChance;
                case TopDown3DRockSizeTier.ExtraLarge:
                    return settings.ExtraLargeToLargeChance;
                case TopDown3DRockSizeTier.Large:
                    return settings.LargeToMediumChance;
                case TopDown3DRockSizeTier.Medium:
                    return settings.MediumToSmallChance;
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
                    return TopDown3DRockSizeTier.ExtraLarge;
                case TopDown3DRockSizeTier.ExtraLarge:
                    return TopDown3DRockSizeTier.Large;
                case TopDown3DRockSizeTier.Large:
                    return TopDown3DRockSizeTier.Medium;
                case TopDown3DRockSizeTier.Medium:
                    return TopDown3DRockSizeTier.Small;
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
            return Mathf.FloorToInt(sample * TopDown3DNaturalObjectCatalog.MeshVariantsPerShape)
                % TopDown3DNaturalObjectCatalog.MeshVariantsPerShape;
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
            TopDown3DNaturalObjectCatalog catalog,
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
                        catalog,
                        definition.Shape,
                        0,
                        Quaternion.identity,
                        scale) * 1.25f);
            }

            return maximum;
        }

        private static TierPlanningData[] BuildTierPlanningData(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog)
        {
            var result = new TierPlanningData[PhysicalTiersDescending.Length];
            for (var i = 0; i < PhysicalTiersDescending.Length; i++)
            {
                var tier = PhysicalTiersDescending[i];
                var definitions = GetDefinitions(catalog, tier);
                var config = GetConfig(settings, tier);
                result[i] = new TierPlanningData(
                    tier,
                    definitions,
                    config,
                    GetCellSize(settings.ChunkSize, config.TargetCount),
                    GetBaseAdmission(settings.ChunkSize, config.TargetCount),
                    GetTierSeed(settings, tier),
                    GetMaximumRootSupport(catalog, definitions));
            }

            for (var i = 0; i < result.Length; i++)
            {
                var radius = result[i].MaximumRootSupport;
                var childTier = GetChildTier(result[i].Tier);
                var remainingMembers = Mathf.Min(
                    settings.PhysicalFormationMaximumMembers - 1,
                    settings.PhysicalFormationMaximumDepth);
                while (childTier != TopDown3DRockSizeTier.None && remainingMembers-- > 0)
                {
                    radius += GetTierPlanningData(result, childTier).MaximumRootSupport * 2f;
                    childTier = GetChildTier(childTier);
                }

                result[i].MaximumFormationRadius = radius;
            }

            return result;
        }

        private static TierPlanningData GetTierPlanningData(
            IReadOnlyList<TierPlanningData> tiers,
            TopDown3DRockSizeTier tier)
        {
            for (var i = 0; i < tiers.Count; i++)
            {
                if (tiers[i].Tier == tier)
                {
                    return tiers[i];
                }
            }

            throw new InvalidOperationException($"Missing planning data for physical rock tier {tier}.");
        }

        private static TierConfig GetConfig(
            TopDown3DWorldSettings settings,
            TopDown3DRockSizeTier tier)
        {
            switch (tier)
            {
                case TopDown3DRockSizeTier.Small:
                    return new TierConfig(
                        settings.SmallRocksPerChunk,
                        settings.SmallRockSpacing,
                        settings.MaximumSmallRockSlope);
                case TopDown3DRockSizeTier.Medium:
                    return new TierConfig(
                        settings.MediumRocksPerChunk,
                        settings.MediumRockSpacing,
                        settings.MaximumMediumRockSlope);
                case TopDown3DRockSizeTier.Large:
                    return new TierConfig(
                        settings.PropsPerChunk,
                        settings.PropSpacing,
                        settings.MaximumPropSlope);
                case TopDown3DRockSizeTier.ExtraLarge:
                    return new TierConfig(
                        settings.ExtraLargeRocksPerChunk,
                        settings.ExtraLargeRockSpacing,
                        settings.MaximumExtraLargeRockSlope);
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

        private static float GetCompetitionSpacing(
            TopDown3DRockSizeTier tier,
            TierConfig config,
            TopDown3DRockSizeTier otherTier,
            TierConfig otherConfig)
        {
            // Peer formations retain their full authored spacing. Across tiers, the smaller
            // spacing wins so secondary outcrops can gather around a landmark without any
            // formation envelopes intersecting. This creates geological clusters instead
            // of a large empty halo around every massive or towering root.
            return tier == otherTier
                ? Mathf.Max(config.Spacing, otherConfig.Spacing)
                : Mathf.Min(config.Spacing, otherConfig.Spacing);
        }

        private static int GetTierRank(TopDown3DRockSizeTier tier)
        {
            switch (tier)
            {
                case TopDown3DRockSizeTier.Small:
                    return 0;
                case TopDown3DRockSizeTier.Medium:
                    return 1;
                case TopDown3DRockSizeTier.Large:
                    return 2;
                case TopDown3DRockSizeTier.ExtraLarge:
                    return 3;
                case TopDown3DRockSizeTier.Massive:
                    return 4;
                case TopDown3DRockSizeTier.Towering:
                    return 5;
                default:
                    return -1;
            }
        }

        private static float GetCellSize(float chunkSize, float targetCount)
        {
            return Mathf.Max(
                0.18f,
                Mathf.Sqrt((chunkSize * chunkSize) / Mathf.Max(1f, targetCount * 2.25f)));
        }

        internal static float GetBaseAdmission(float chunkSize, float targetCount)
        {
            if (targetCount <= 0f)
            {
                return 0f;
            }

            var cellSize = GetCellSize(chunkSize, targetCount);
            var cellsPerChunk = (chunkSize / cellSize) * (chunkSize / cellSize);
            return Mathf.Clamp01(targetCount / Mathf.Max(0.0001f, cellsPerChunk));
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

        private sealed class TierPlanningData
        {
            public TierPlanningData(
                TopDown3DRockSizeTier tier,
                IReadOnlyList<TopDown3DNaturalObjectDefinition> definitions,
                TierConfig config,
                float cellSize,
                float baseAdmission,
                int layerSeed,
                float maximumRootSupport)
            {
                Tier = tier;
                Definitions = definitions;
                Config = config;
                CellSize = cellSize;
                BaseAdmission = baseAdmission;
                LayerSeed = layerSeed;
                MaximumRootSupport = maximumRootSupport;
            }

            public TopDown3DRockSizeTier Tier { get; }
            public IReadOnlyList<TopDown3DNaturalObjectDefinition> Definitions { get; }
            public TierConfig Config { get; }
            public float CellSize { get; }
            public float BaseAdmission { get; }
            public int LayerSeed { get; }
            public float MaximumRootSupport { get; }
            public float MaximumFormationRadius { get; set; }
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
