using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DResourceNodeTests
    {
        private const string WorldSettingsPath = "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        private static readonly Vector2 DistantExclusion = new Vector2(10000f, 10000f);
        private readonly List<Object> ownedObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void ResourcePlanner_IsDeterministicStableAndOwnerChunkBound()
        {
            var settings = CreateSettings(out var definition);
            var coordinate = FindChunkWithResources(settings);
            var first = BuildResources(settings, coordinate);
            var second = BuildResources(settings, coordinate);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.Not.Empty);
            Assert.That(first.All(placement =>
                placement.OwnerChunk == coordinate
                && Mathf.FloorToInt(placement.Position.x / settings.ChunkSize) == coordinate.x
                && Mathf.FloorToInt(placement.Position.z / settings.ChunkSize) == coordinate.y
                && placement.StableId.StartsWith(
                    $"resource:{settings.WorldSeed}:{settings.ResourceGenerationVersion}:{definition.ResourceId}:")),
                Is.True);
        }

        [Test]
        public void ResourceGenerationVersion_DoesNotReshuffleCosmeticsOrPhysicalRocks()
        {
            var settings = CreateSettings(out _);
            var changed = Object.Instantiate(settings);
            ownedObjects.Add(changed);
            changed.ConfigureResourceAssets(settings.ResourceCatalog, settings.ResourceGenerationVersion + 1);
            var coordinate = FindChunkWithResources(settings);
            var baseline = Build(settings, coordinate);
            var versioned = Build(changed, coordinate);

            Assert.That(versioned.CosmeticPlacements, Is.EqualTo(baseline.CosmeticPlacements));
            Assert.That(versioned.PhysicalFormations, Is.EqualTo(baseline.PhysicalFormations));
            Assert.That(
                BuildResources(changed, coordinate).Select(item => item.StableId).ToArray(),
                Is.Not.EqualTo(BuildResources(settings, coordinate).Select(item => item.StableId).ToArray()));
        }

        [Test]
        public void ResourcesRespectSurfaceSpawnAndPhysicalFormationExclusions()
        {
            var settings = CreateSettings(out var definition);
            var generator = new TopDown3DWorldGenerator(settings);
            var placements = new List<TopDown3DResourceNodePlacement>();
            for (var z = -4; z <= 4; z++)
            {
                for (var x = -4; x <= 4; x++)
                {
                    var chunkPlacements = BuildResources(settings, new Vector2Int(x, z));
                    placements.AddRange(chunkPlacements);
                    foreach (var placement in chunkPlacements)
                    {
                        var surface = generator.Sample(placement.Position.x, placement.Position.z);
                        Assert.That(Vector3.Angle(surface.Normal, Vector3.up),
                            Is.LessThanOrEqualTo(definition.MaximumSlope + 0.001f));
                        Assert.That(surface.BedrockWeight,
                            Is.GreaterThanOrEqualTo(definition.MinimumBedrockWeight));
                        Assert.That(surface.DepositWeight,
                            Is.GreaterThanOrEqualTo(definition.MinimumDepositWeight));
                        Assert.That(surface.Lithology,
                            Is.GreaterThanOrEqualTo(definition.MinimumLithology));
                        Assert.That(surface.TraversalCorridor,
                            Is.LessThanOrEqualTo(definition.MaximumTraversalCorridor));
                    }
                }
            }

            Assert.That(placements, Is.Not.Empty);
            Assert.That(placements.Select(item => item.StableId).Distinct().Count(),
                Is.EqualTo(placements.Count));
            Assert.That(placements
                .GroupBy(item => item.OwnerChunk)
                .All(group => group.Count() <= Mathf.CeilToInt(definition.TargetPerChunk)),
                Is.True);
            for (var left = 0; left < placements.Count; left++)
            {
                for (var right = left + 1; right < placements.Count; right++)
                {
                    Assert.That(
                        Vector2.Distance(
                            new Vector2(placements[left].Position.x, placements[left].Position.z),
                            new Vector2(placements[right].Position.x, placements[right].Position.z)),
                        Is.GreaterThanOrEqualTo(definition.MinimumSpacing - 0.001f));
                }
            }
        }

        [Test]
        public void ResourcePlanner_RejectsPhysicalFormationEnvelopeOverlap()
        {
            var settings = CreateSettings(out var definition);
            var coordinate = FindChunkWithResources(settings);
            var candidate = BuildResources(settings, coordinate).First();
            var exclusion = new TopDown3DRockFormationPlan(
                new TopDown3DRockRootKey(TopDown3DRockSizeTier.Small, 0, 0, 1),
                "test-exclusion",
                1,
                TopDown3DNaturalObjectLayer.Obstacle,
                TopDown3DRockSurface.Regular,
                new TopDown3DRockFormationMember[0],
                new Vector2(candidate.Position.x, candidate.Position.z),
                definition.FootprintRadius,
                1f);

            var filtered = BuildResources(settings, coordinate, new[] { exclusion });

            Assert.That(filtered.Any(item => item.StableId == candidate.StableId), Is.False);
        }

        [Test]
        public void WorldState_ConsumesOnceAndRoundTripsDeterministically()
        {
            var sourceObject = new GameObject("Resource State Source");
            var targetObject = new GameObject("Resource State Target");
            ownedObjects.Add(sourceObject);
            ownedObjects.Add(targetObject);
            var source = sourceObject.AddComponent<TopDown3DResourceWorldState>();
            source.Configure(4);

            Assert.That(source.GetRemainingUses("resource:a", 1), Is.EqualTo(1));
            Assert.That(source.TryConsume("resource:a", 1, out var remaining), Is.True);
            Assert.That(remaining, Is.Zero);
            Assert.That(source.TryConsume("resource:a", 1, out remaining), Is.False);
            var snapshot = source.CaptureSnapshot();
            var target = targetObject.AddComponent<TopDown3DResourceWorldState>();
            target.Configure(4);

            Assert.That(target.ApplySnapshot(snapshot), Is.True);
            Assert.That(target.GetRemainingUses("resource:a", 1), Is.Zero);
            Assert.That(JsonUtility.ToJson(target.CaptureSnapshot()),
                Is.EqualTo(JsonUtility.ToJson(snapshot)));
        }

        [Test]
        public void WorldState_RejectsWrongVersionAndDuplicateRecordsAtomically()
        {
            var stateObject = new GameObject("Resource State");
            ownedObjects.Add(stateObject);
            var state = stateObject.AddComponent<TopDown3DResourceWorldState>();
            state.Configure(2);
            Assert.That(state.TryConsume("resource:kept", 1, out _), Is.True);
            var before = JsonUtility.ToJson(state.CaptureSnapshot());

            var wrongVersion = JsonUtility.FromJson<TopDown3DResourceWorldSnapshot>(
                before.Replace("\"resourceGenerationVersion\":2", "\"resourceGenerationVersion\":3"));
            Assert.That(state.ApplySnapshot(wrongVersion), Is.False);
            var duplicate = before.Replace("]}", ",{\"stableId\":\"resource:kept\",\"remainingUses\":0}]}");
            Assert.That(state.ApplySnapshot(
                JsonUtility.FromJson<TopDown3DResourceWorldSnapshot>(duplicate)), Is.False);
            Assert.That(JsonUtility.ToJson(state.CaptureSnapshot()), Is.EqualTo(before));
        }

        [Test]
        public void Decorator_RecreatesDepletedNodeFromWorldOwnedState()
        {
            var settings = CreateSettings(out var definition);
            var coordinate = FindChunkWithResources(settings);
            var placements = BuildResources(settings, coordinate).ToList();
            var plan = new TopDown3DNaturalObjectChunkPlan(
                new List<TopDown3DNaturalObjectPlacement>(),
                new List<TopDown3DRockFormationPlan>(),
                placements);
            var worldObject = new GameObject("Resource World State");
            var firstChunkObject = new GameObject("First Resource Chunk");
            ownedObjects.Add(worldObject);
            ownedObjects.Add(firstChunkObject);
            var state = worldObject.AddComponent<TopDown3DResourceWorldState>();
            state.Configure(settings.ResourceGenerationVersion);
            var firstChunk = firstChunkObject.AddComponent<TopDown3DGeneratedChunk>();
            firstChunk.Initialize(coordinate, null);

            TopDown3DResourceNodeDecorator.Decorate(firstChunk, settings, plan, state);

            var firstNode = firstChunk.GetComponentsInChildren<TopDown3DIronstoneNode>()
                .Single(node => node.StableId == placements[0].StableId);
            Assert.That(firstNode.IsDepleted, Is.False);
            Assert.That(firstNode.TryConsume(out var remaining), Is.True);
            Assert.That(remaining, Is.Zero);
            Assert.That(firstNode.IsDepleted, Is.True);
            Assert.That(firstNode.GetComponent<Collider>().enabled, Is.False);
            Object.DestroyImmediate(firstChunkObject);

            var secondChunkObject = new GameObject("Reloaded Resource Chunk");
            ownedObjects.Add(secondChunkObject);
            var secondChunk = secondChunkObject.AddComponent<TopDown3DGeneratedChunk>();
            secondChunk.Initialize(coordinate, null);
            TopDown3DResourceNodeDecorator.Decorate(secondChunk, settings, plan, state);
            var reloaded = secondChunk.GetComponentsInChildren<TopDown3DIronstoneNode>()
                .Single(node => node.StableId == placements[0].StableId);

            Assert.That(reloaded.IsDepleted, Is.True);
            Assert.That(reloaded.GetComponent<Collider>().enabled, Is.False);
            Assert.That(reloaded.TryConsume(out remaining), Is.False);
            Assert.That(reloaded.GetComponentInChildren<MeshRenderer>().sharedMaterial,
                Is.SameAs(definition.DepletedMaterial));
        }

        [Test]
        public void ProductionSettings_GenerateSparseValidIronstonePlacements()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.ResourceCatalog, Is.Not.Null);
            Assert.That(settings.ResourceCatalog.TryValidate(settings.NaturalObjectCatalog, out var error),
                Is.True, error);
            var definition = settings.ResourceCatalog.Definitions.Single();
            var placements = new List<TopDown3DResourceNodePlacement>();
            var generator = new TopDown3DWorldGenerator(settings);
            var maximumBedrock = 0f;
            var maximumDeposit = 0f;
            var maximumLithology = 0f;
            var minimumCorridor = 1f;
            for (var z = -12; z <= 12; z++)
            {
                for (var x = -12; x <= 12; x++)
                {
                    var sample = generator.Sample(
                        (x + 0.5f) * settings.ChunkSize,
                        (z + 0.5f) * settings.ChunkSize);
                    maximumBedrock = Mathf.Max(maximumBedrock, sample.BedrockWeight);
                    maximumDeposit = Mathf.Max(maximumDeposit, sample.DepositWeight);
                    maximumLithology = Mathf.Max(maximumLithology, sample.Lithology);
                    minimumCorridor = Mathf.Min(minimumCorridor, sample.TraversalCorridor);
                    var chunkPlacements = BuildResources(settings, new Vector2Int(x, z));
                    Assert.That(chunkPlacements.Count,
                        Is.LessThanOrEqualTo(Mathf.CeilToInt(definition.TargetPerChunk)));
                    placements.AddRange(chunkPlacements);
                }
            }

            Assert.That(placements, Is.Not.Empty,
                $"Sampled maxima bedrock={maximumBedrock:F3}, deposit={maximumDeposit:F3}, "
                + $"lithology={maximumLithology:F3}, minimum corridor={minimumCorridor:F3}.");
            Assert.That(placements.Select(placement => placement.StableId).Distinct().Count(),
                Is.EqualTo(placements.Count));
        }

        private TopDown3DWorldSettings CreateSettings(out TopDown3DResourceDefinition definition)
        {
            var source = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(source, Is.Not.Null);
            var settings = Object.Instantiate(source);
            ownedObjects.Add(settings);
            var texture = new Texture2D(2, 2);
            ownedObjects.Add(texture);
            var icon = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            ownedObjects.Add(icon);
            var item = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            ownedObjects.Add(item);
            item.Configure("resource.ironstone_ore", "Ironstone Ore", "Test", "Resource", icon, 99, 1.25f);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            var active = new Material(shader);
            var depleted = new Material(shader);
            ownedObjects.Add(active);
            ownedObjects.Add(depleted);
            definition = ScriptableObject.CreateInstance<TopDown3DResourceDefinition>();
            ownedObjects.Add(definition);
            definition.Configure(
                "resource.ironstone_node",
                "broken-world-outcrop-00",
                TopDown3DNaturalObjectShape.Outcrop,
                0,
                active,
                depleted,
                item,
                1,
                1,
                2.2f,
                1.1f,
                "Gather Ironstone",
                1.5f,
                12f,
                5f);
            definition.ConfigurePlacementConstraints(60f, 0f, 0f, 0f, 1f);
            var catalog = ScriptableObject.CreateInstance<TopDown3DResourceCatalog>();
            ownedObjects.Add(catalog);
            catalog.Configure(new[] { definition });
            Assert.That(catalog.TryValidate(settings.NaturalObjectCatalog, out var error), Is.True, error);
            settings.ConfigureResourceAssets(catalog, 1);
            return settings;
        }

        private static TopDown3DNaturalObjectChunkPlan Build(
            TopDown3DWorldSettings settings,
            Vector2Int coordinate)
        {
            return TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                settings,
                new TopDown3DWorldGenerator(settings),
                settings.NaturalObjectCatalog,
                coordinate,
                DistantExclusion);
        }

        private static IReadOnlyList<TopDown3DResourceNodePlacement> BuildResources(
            TopDown3DWorldSettings settings,
            Vector2Int coordinate,
            IReadOnlyList<TopDown3DRockFormationPlan> physicalFormations = null)
        {
            return TopDown3DResourceNodePlanner.BuildChunkPlacements(
                settings,
                new TopDown3DWorldGenerator(settings),
                settings.ResourceCatalog,
                coordinate,
                DistantExclusion,
                physicalFormations ?? new TopDown3DRockFormationPlan[0]);
        }

        private static Vector2Int FindChunkWithResources(TopDown3DWorldSettings settings)
        {
            for (var radius = 0; radius <= 12; radius++)
            {
                for (var z = -radius; z <= radius; z++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        var coordinate = new Vector2Int(x, z);
                        if (BuildResources(settings, coordinate).Count > 0)
                        {
                            return coordinate;
                        }
                    }
                }
            }

            Assert.Fail("No deterministic resource placement was found in the audited search region.");
            return default;
        }
    }
}
