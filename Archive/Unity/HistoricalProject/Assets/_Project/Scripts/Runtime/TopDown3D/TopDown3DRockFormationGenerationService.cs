using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Shared, deterministic entry point for consumers that need to inspect or realize
    /// geological rock formations without owning a second placement algorithm.
    /// </summary>
    public static class TopDown3DRockFormationGenerationService
    {
        private const int MaximumSearchRadiusInChunks = 16;

        /// <summary>
        /// Builds the formations owned by one streaming chunk, including the supplied
        /// runtime spawn-exclusion rule.
        /// </summary>
        public static IReadOnlyList<TopDown3DRockFormationPlan> BuildChunk(
            TopDown3DWorldSettings settings,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter)
        {
            ValidateSettings(settings);
            var generator = new TopDown3DWorldGenerator(settings);
            return TopDown3DGeologicalRockAdapter.BuildPhysicalFormations(
                settings,
                generator,
                settings.NaturalObjectCatalog,
                chunkCoordinate,
                spawnExclusionCenter).AsReadOnly();
        }

        /// <summary>
        /// Collects the formation plans around an absolute prototype-world position.
        /// This is an authoring query, so it intentionally does not apply the runtime
        /// player-spawn exclusion that would otherwise hide nearby formations.
        /// </summary>
        public static IReadOnlyList<TopDown3DRockFormationPlan> BuildAround(
            TopDown3DWorldSettings settings,
            Vector2 worldPosition,
            int searchRadiusInChunks)
        {
            ValidateSettings(settings);
            if (searchRadiusInChunks < 0 || searchRadiusInChunks > MaximumSearchRadiusInChunks)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(searchRadiusInChunks),
                    $"Search radius must be between zero and {MaximumSearchRadiusInChunks} chunks.");
            }

            var generator = new TopDown3DWorldGenerator(settings);
            var center = generator.WorldToChunk(
                settings,
                new Vector3(worldPosition.x, 0f, worldPosition.y));
            var formations = new List<TopDown3DRockFormationPlan>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            var disabledSpawnExclusion = new Vector2(10000000f, 10000000f);
            for (var z = -searchRadiusInChunks; z <= searchRadiusInChunks; z++)
            {
                for (var x = -searchRadiusInChunks; x <= searchRadiusInChunks; x++)
                {
                    var chunk = new Vector2Int(center.x + x, center.y + z);
                    var plans = TopDown3DGeologicalRockAdapter.BuildPhysicalFormations(
                        settings,
                        generator,
                        settings.NaturalObjectCatalog,
                        chunk,
                        disabledSpawnExclusion);
                    for (var planIndex = 0; planIndex < plans.Count; planIndex++)
                    {
                        var plan = plans[planIndex];
                        if (plan != null && seenIds.Add(plan.StableId))
                        {
                            formations.Add(plan);
                        }
                    }
                }
            }

            formations.Sort((left, right) => string.CompareOrdinal(left.StableId, right.StableId));
            return formations.AsReadOnly();
        }

        private static void ValidateSettings(TopDown3DWorldSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.NaturalObjectCatalog == null)
            {
                throw new InvalidOperationException(
                    "Rock formation generation requires a natural-object catalog on the world settings.");
            }
        }
    }
}
