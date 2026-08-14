using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DNaturalObjectTests
    {
        private const string WorldSettingsPath = "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        private static readonly Vector2 DistantExclusion = new Vector2(10000f, 10000f);

        [Test]
        public void ChunkPlan_IsDeterministicAndRootChunkOwned()
        {
            var settings = LoadSettings();
            var coordinate = new Vector2Int(3, -2);
            var first = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                settings,
                settings.NaturalObjectCatalog,
                coordinate,
                DistantExclusion);
            var second = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                settings,
                settings.NaturalObjectCatalog,
                coordinate,
                DistantExclusion);

            Assert.That(first.CosmeticPlacements, Is.EqualTo(second.CosmeticPlacements));
            Assert.That(first.PhysicalFormations, Is.EqualTo(second.PhysicalFormations));
            Assert.That(first.CosmeticPlacements, Is.Not.Empty);
            Assert.That(first.CosmeticPlacements.All(placement =>
                Mathf.FloorToInt(placement.Position.x / settings.ChunkSize) == coordinate.x
                && Mathf.FloorToInt(placement.Position.z / settings.ChunkSize) == coordinate.y), Is.True);
            Assert.That(first.PhysicalFormations.All(formation =>
                Mathf.FloorToInt(formation.Members[0].Position.x / settings.ChunkSize) == coordinate.x
                && Mathf.FloorToInt(formation.Members[0].Position.z / settings.ChunkSize) == coordinate.y), Is.True);
        }

        [Test]
        public void PhysicalRoots_RespectTierPrecedenceAndSpacingAcrossChunkBorders()
        {
            var settings = LoadSettings();
            var formations = CollectFormations(settings, -2, 2);
            for (var i = 0; i < formations.Count; i++)
            {
                var left = formations[i];
                var leftRoot = left.Members[0];
                for (var j = i + 1; j < formations.Count; j++)
                {
                    var right = formations[j];
                    var rightRoot = right.Members[0];
                    var distance = Vector2.Distance(left.EnvelopeCenter, right.EnvelopeCenter);
                    var minimum = left.EnvelopeRadius
                        + right.EnvelopeRadius
                        + Mathf.Max(GetSpacing(settings, leftRoot.Tier), GetSpacing(settings, rightRoot.Tier));
                    Assert.That(
                        distance,
                        Is.GreaterThanOrEqualTo(minimum - 0.001f),
                        $"Root spacing failed for {left.RootKey} and {right.RootKey}.");
                }
            }
        }

        [Test]
        public void SpawnExclusion_CoversEveryCosmeticAndEveryFormationMember()
        {
            var settings = LoadSettings();
            var center = new Vector2(settings.ChunkSize * 0.5f, settings.ChunkSize * 0.5f);
            var plan = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                settings,
                settings.NaturalObjectCatalog,
                Vector2Int.zero,
                center);

            Assert.That(plan.CosmeticPlacements, Is.Not.Empty);
            Assert.That(plan.CosmeticPlacements.All(placement =>
                Vector2.Distance(
                    new Vector2(placement.Position.x, placement.Position.z),
                    center) >= settings.ClearSpawnRadius + placement.FootprintRadius), Is.True);
            Assert.That(plan.PhysicalFormations.SelectMany(formation => formation.Members).All(member =>
                Vector2.Distance(
                    new Vector2(member.Position.x, member.Position.z),
                    center) >= settings.ClearSpawnRadius + member.SupportRadius), Is.True);
        }

        [Test]
        public void Catalog_ContainsAllCostLayersAndAllPhysicalTiersWithUniqueStableIds()
        {
            var settings = LoadSettings();
            var catalog = settings.NaturalObjectCatalog;
            Assert.That(catalog, Is.Not.Null);
            foreach (TopDown3DNaturalObjectLayer layer in Enum.GetValues(typeof(TopDown3DNaturalObjectLayer)))
            {
                Assert.That(catalog.HasLayer(layer), Is.True, $"Missing cost layer {layer}.");
            }

            foreach (var tier in new[]
                     {
                         TopDown3DRockSizeTier.Large,
                         TopDown3DRockSizeTier.Massive,
                         TopDown3DRockSizeTier.Towering
                     })
            {
                Assert.That(catalog.Definitions.Any(definition => definition.RockSizeTier == tier), Is.True);
            }

            Assert.That(
                catalog.Definitions.Where(definition => definition.Layer == TopDown3DNaturalObjectLayer.Obstacle)
                    .All(definition => definition.RockSizeTier == TopDown3DRockSizeTier.Large
                        || definition.RockSizeTier == TopDown3DRockSizeTier.Massive),
                Is.True);
            Assert.That(
                catalog.Definitions.Where(definition => definition.Layer == TopDown3DNaturalObjectLayer.Landmark)
                    .All(definition => definition.RockSizeTier == TopDown3DRockSizeTier.Towering),
                Is.True);
            Assert.That(
                catalog.Definitions.Select(definition => definition.StableId).Distinct().Count(),
                Is.EqualTo(catalog.Definitions.Count));
        }

        [Test]
        public void MassiveRootFrequency_SitsBetweenLargeAndTowering()
        {
            var settings = LoadSettings();
            var formations = CollectFormations(settings, -8, 8);
            var large = formations.Count(formation => formation.RootKey.Tier == TopDown3DRockSizeTier.Large);
            var massive = formations.Count(formation => formation.RootKey.Tier == TopDown3DRockSizeTier.Massive);
            var towering = formations.Count(formation => formation.RootKey.Tier == TopDown3DRockSizeTier.Towering);

            Assert.That(towering, Is.GreaterThan(0));
            Assert.That(massive, Is.GreaterThan(towering));
            Assert.That(large, Is.GreaterThan(massive));
        }

        [Test]
        public void FormationTopology_UsesOnlyApprovedEdgesAndHonorsCaps()
        {
            var settings = LoadSettings();
            var formations = CollectFormations(settings, -8, 8);
            Assert.That(formations.Any(formation => formation.Members.Count > 1), Is.True);
            foreach (var formation in formations)
            {
                Assert.That(formation.Members.Count, Is.InRange(1, settings.PhysicalFormationMaximumMembers));
                Assert.That(formation.Members[0].ParentIndex, Is.EqualTo(-1));
                for (var i = 1; i < formation.Members.Count; i++)
                {
                    var member = formation.Members[i];
                    Assert.That(member.MemberIndex, Is.EqualTo(i));
                    Assert.That(member.ParentIndex, Is.EqualTo(i - 1));
                    Assert.That(IsApprovedEdge(
                        formation.Members[member.ParentIndex].Tier,
                        member.Tier), Is.True);
                    Assert.That(i, Is.LessThanOrEqualTo(settings.PhysicalFormationMaximumDepth));
                }
            }
        }

        [Test]
        public void FormationMembers_AreGroundedTouchParentsAndAvoidNonParentInterpenetration()
        {
            var settings = LoadSettings();
            var formations = CollectFormations(settings, -8, 8)
                .Where(formation => formation.Members.Count > 1)
                .ToArray();
            Assert.That(formations, Is.Not.Empty);
            foreach (var formation in formations)
            {
                for (var i = 1; i < formation.Members.Count; i++)
                {
                    var child = formation.Members[i];
                    var parent = formation.Members[child.ParentIndex];
                    var distance = Vector2.Distance(
                        new Vector2(child.Position.x, child.Position.z),
                        new Vector2(parent.Position.x, parent.Position.z));
                    var supportSum = child.SupportRadius + parent.SupportRadius;
                    Assert.That(distance, Is.InRange(supportSum * 0.78f, supportSum * 1.08f));
                    Assert.That(
                        Mathf.Min(child.WorldBounds.max.y, parent.WorldBounds.max.y),
                        Is.GreaterThan(Mathf.Max(child.WorldBounds.min.y, parent.WorldBounds.min.y)));

                    for (var otherIndex = 0; otherIndex < i; otherIndex++)
                    {
                        if (otherIndex == child.ParentIndex)
                        {
                            continue;
                        }

                        var other = formation.Members[otherIndex];
                        var nonParentDistance = Vector2.Distance(
                            new Vector2(child.Position.x, child.Position.z),
                            new Vector2(other.Position.x, other.Position.z));
                        Assert.That(
                            nonParentDistance,
                            Is.GreaterThanOrEqualTo(
                                (child.SupportRadius + other.SupportRadius) * 0.88f - 0.001f));
                    }
                }
            }
        }

        [Test]
        public void PhysicalGenerationVersion_DoesNotReshuffleCosmetics()
        {
            var settings = LoadSettings();
            var changedPhysicalVersion = UnityEngine.Object.Instantiate(settings);
            try
            {
                var serialized = new SerializedObject(changedPhysicalVersion);
                serialized.FindProperty("physicalRockGenerationVersion").intValue++;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var coordinate = new Vector2Int(4, 3);
                var baseline = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                    settings,
                    settings.NaturalObjectCatalog,
                    coordinate,
                    DistantExclusion);
                var changed = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                    changedPhysicalVersion,
                    changedPhysicalVersion.NaturalObjectCatalog,
                    coordinate,
                    DistantExclusion);

                Assert.That(changed.CosmeticPlacements, Is.EqualTo(baseline.CosmeticPlacements));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(changedPhysicalVersion);
            }
        }

        [Test]
        public void Decorator_CreatesOneRendererAndOneColliderPerFormationMember()
        {
            var settings = LoadSettings();
            var coordinate = FindChunkWithFormation(settings);
            var plan = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                settings,
                settings.NaturalObjectCatalog,
                coordinate,
                DistantExclusion);
            var chunkObject = new GameObject("Rock Formation Decorator Test");
            try
            {
                var chunk = chunkObject.AddComponent<TopDown3DGeneratedChunk>();
                chunk.Initialize(coordinate, null);
                TopDown3DNaturalObjectDecorator.Decorate(
                    chunk,
                    settings,
                    settings.DarkRockMaterial,
                    DistantExclusion);

                var obstacles = chunk.GetComponentsInChildren<TopDown3DTraversalObstacle>();
                Assert.That(obstacles.Length, Is.EqualTo(plan.PhysicalFormations.Count));
                for (var i = 0; i < obstacles.Length; i++)
                {
                    var matchingPlan = plan.PhysicalFormations.Single(formation =>
                        obstacles[i].name.Contains(formation.StableId));
                    Assert.That(obstacles[i].GetComponents<MeshRenderer>().Length, Is.EqualTo(1));
                    Assert.That(
                        obstacles[i].GetComponentsInChildren<BoxCollider>().Length,
                        Is.EqualTo(matchingPlan.Members.Count));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(chunkObject);
            }
        }

        [Test]
        public void ProceduralRockFamily_ProducesFiniteFacetedMeshes()
        {
            foreach (TopDown3DNaturalObjectShape shape in Enum.GetValues(typeof(TopDown3DNaturalObjectShape)))
            {
                var distinctBounds = new HashSet<Vector3>();
                for (var variant = 0; variant < TopDown3DNaturalMeshLibrary.VariantsPerShape; variant++)
                {
                    var mesh = TopDown3DNaturalMeshLibrary.GetMesh(shape, variant);
                    Assert.That(mesh, Is.Not.Null);
                    Assert.That(mesh.vertexCount, Is.GreaterThan(0).And.LessThan(600));
                    Assert.That(mesh.triangles.Length % 3, Is.Zero);
                    Assert.That(mesh.bounds.size.sqrMagnitude, Is.GreaterThan(0f));
                    Assert.That(mesh.bounds.min.y, Is.GreaterThanOrEqualTo(-0.0001f));
                    distinctBounds.Add(mesh.bounds.size);
                }

                Assert.That(distinctBounds.Count, Is.GreaterThanOrEqualTo(8));
            }
        }

        [Test]
        public void RockSurfaces_UseAllThreeFamiliesInSpatialClusters()
        {
            var settings = LoadSettings();
            var seen = new bool[3];
            var matchingNeighbors = 0;
            var neighborComparisons = 0;
            for (var z = -60; z <= 60; z += 2)
            {
                for (var x = -60; x <= 60; x += 2)
                {
                    var surface = TopDown3DNaturalObjectPlanner.SampleRockSurface(settings, new Vector2(x, z));
                    seen[(int)surface] = true;
                    if (x < 60)
                    {
                        var neighbor = TopDown3DNaturalObjectPlanner.SampleRockSurface(
                            settings,
                            new Vector2(x + 2, z));
                        matchingNeighbors += surface == neighbor ? 1 : 0;
                        neighborComparisons++;
                    }
                }
            }

            Assert.That(seen.All(value => value), Is.True);
            Assert.That((float)matchingNeighbors / neighborComparisons, Is.GreaterThan(0.8f));
        }

        [Test]
        public void FineGrayCluster_RemainsDenseStrongAndCosmetic()
        {
            var settings = LoadSettings();
            Assert.That(settings.FineGrayClutterPerChunk, Is.GreaterThan(settings.GroundDetailsPerChunk));
            Assert.That(settings.FineGrayClusterStrength, Is.GreaterThan(settings.ClutterClusterStrength));
            Assert.That(settings.FineGrayClusterFrequency, Is.GreaterThan(settings.ClutterClusterFrequency));
            var placements = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                    settings,
                    settings.NaturalObjectCatalog,
                    new Vector2Int(2, 2),
                    DistantExclusion)
                .CosmeticPlacements
                .Where(placement => placement.Layer == TopDown3DNaturalObjectLayer.FineGrayCluster)
                .ToArray();
            Assert.That(placements, Is.Not.Empty);
            Assert.That(placements.All(placement => Mathf.Max(placement.Scale.x, placement.Scale.z) <= 0.2f), Is.True);
        }

        [Test]
        public void RockAbundance_CreatesBroadSparseAndDenseRegions()
        {
            var settings = LoadSettings();
            var minimum = float.MaxValue;
            var maximum = float.MinValue;
            var neighborDifference = 0f;
            var comparisons = 0;
            for (var z = -180; z <= 180; z += 12)
            {
                for (var x = -180; x <= 180; x += 12)
                {
                    var position = new Vector2(x, z);
                    var abundance = TopDown3DNaturalObjectPlanner.SampleRockAbundance(settings, position);
                    minimum = Mathf.Min(minimum, abundance);
                    maximum = Mathf.Max(maximum, abundance);
                    neighborDifference += Mathf.Abs(
                        abundance
                        - TopDown3DNaturalObjectPlanner.SampleRockAbundance(
                            settings,
                            position + Vector2.right * 4f));
                    comparisons++;
                }
            }

            Assert.That(minimum, Is.LessThan(0.25f));
            Assert.That(maximum, Is.GreaterThan(1.55f));
            Assert.That(neighborDifference / comparisons, Is.LessThan(0.2f));
        }

        private static List<TopDown3DRockFormationPlan> CollectFormations(
            TopDown3DWorldSettings settings,
            int minimumChunk,
            int maximumChunk)
        {
            var formations = new List<TopDown3DRockFormationPlan>();
            for (var z = minimumChunk; z <= maximumChunk; z++)
            {
                for (var x = minimumChunk; x <= maximumChunk; x++)
                {
                    formations.AddRange(TopDown3DRockFormationPlanner.BuildPhysicalFormations(
                        settings,
                        settings.NaturalObjectCatalog,
                        new Vector2Int(x, z),
                        DistantExclusion));
                }
            }

            return formations;
        }

        private static Vector2Int FindChunkWithFormation(TopDown3DWorldSettings settings)
        {
            for (var z = -4; z <= 4; z++)
            {
                for (var x = -4; x <= 4; x++)
                {
                    var coordinate = new Vector2Int(x, z);
                    if (TopDown3DRockFormationPlanner.BuildPhysicalFormations(
                            settings,
                            settings.NaturalObjectCatalog,
                            coordinate,
                            DistantExclusion).Count > 0)
                    {
                        return coordinate;
                    }
                }
            }

            Assert.Fail("Expected a generated physical-rock formation in the sampled area.");
            return default;
        }

        private static float GetSpacing(TopDown3DWorldSettings settings, TopDown3DRockSizeTier tier)
        {
            switch (tier)
            {
                case TopDown3DRockSizeTier.Large:
                    return settings.PropSpacing;
                case TopDown3DRockSizeTier.Massive:
                    return settings.MassiveRockSpacing;
                case TopDown3DRockSizeTier.Towering:
                    return settings.LandmarkSpacing;
                default:
                    return 0f;
            }
        }

        private static bool IsApprovedEdge(TopDown3DRockSizeTier parent, TopDown3DRockSizeTier child)
        {
            return parent == TopDown3DRockSizeTier.Towering && child == TopDown3DRockSizeTier.Massive
                || parent == TopDown3DRockSizeTier.Massive && child == TopDown3DRockSizeTier.Large
                || parent == TopDown3DRockSizeTier.Large && child == TopDown3DRockSizeTier.Large;
        }

        private static TopDown3DWorldSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            return settings;
        }
    }
}
