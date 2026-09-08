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
        private static readonly TopDown3DRockSizeTier[] PhysicalTiersAscending =
        {
            TopDown3DRockSizeTier.Small,
            TopDown3DRockSizeTier.Medium,
            TopDown3DRockSizeTier.Large,
            TopDown3DRockSizeTier.ExtraLarge,
            TopDown3DRockSizeTier.Massive,
            TopDown3DRockSizeTier.Towering
        };

        [Test]
        public void ChunkPlan_IsDeterministicAndRootChunkOwned()
        {
            var settings = LoadSettings();
            var coordinate = new Vector2Int(3, -2);
            var first = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                settings,
                new TopDown3DWorldGenerator(settings),
                settings.NaturalObjectCatalog,
                coordinate,
                DistantExclusion);
            var second = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                settings,
                new TopDown3DWorldGenerator(settings),
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
                    var spacing = leftRoot.Tier == rightRoot.Tier
                        ? Mathf.Max(GetSpacing(settings, leftRoot.Tier), GetSpacing(settings, rightRoot.Tier))
                        : Mathf.Min(GetSpacing(settings, leftRoot.Tier), GetSpacing(settings, rightRoot.Tier));
                    var minimum = left.EnvelopeRadius
                        + right.EnvelopeRadius
                        + spacing;
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
                new TopDown3DWorldGenerator(settings),
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

            foreach (var tier in PhysicalTiersAscending)
            {
                Assert.That(catalog.Definitions.Any(definition => definition.RockSizeTier == tier), Is.True);
            }

            Assert.That(
                catalog.Definitions.Where(definition => definition.Layer == TopDown3DNaturalObjectLayer.Obstacle)
                    .All(definition => definition.RockSizeTier != TopDown3DRockSizeTier.None
                        && definition.RockSizeTier != TopDown3DRockSizeTier.Towering),
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
        public void EveryPhysicalTier_GeneratesIndependentRootsAtDescendingFrequency()
        {
            var settings = LoadSettings();
            var formations = CollectFormations(settings, -8, 8);
            var previousCount = int.MaxValue;
            foreach (var tier in PhysicalTiersAscending)
            {
                var count = formations.Count(formation => formation.RootKey.Tier == tier);
                Assert.That(count, Is.GreaterThan(0), $"Expected independent {tier} roots.");
                Assert.That(count, Is.LessThan(previousCount), $"{tier} roots must be rarer than the prior tier.");
                previousCount = count;
            }
        }

        [Test]
        public void RareTierAdmission_MatchesConfiguredLinearDensity()
        {
            const float chunkSize = 18f;
            Assert.That(
                TopDown3DRockFormationPlanner.GetBaseAdmission(chunkSize, 0.7f),
                Is.EqualTo(1f / 2.25f).Within(0.0001f));
            Assert.That(
                TopDown3DRockFormationPlanner.GetBaseAdmission(chunkSize, 0.36f),
                Is.EqualTo(0.36f).Within(0.0001f));
            Assert.That(
                TopDown3DRockFormationPlanner.GetBaseAdmission(chunkSize, 0.16f),
                Is.EqualTo(0.16f).Within(0.0001f));
        }

        [Test]
        public void StartingLandscape_PresentsReadableFormationHierarchy()
        {
            var settings = LoadSettings();
            var formations = CollectFormations(settings, -3, 3);
            var mediumOrLarger = formations.Count(formation =>
                formation.RootKey.Tier != TopDown3DRockSizeTier.Small);
            var extraLargeOrLarger = formations.Count(formation =>
                formation.RootKey.Tier == TopDown3DRockSizeTier.ExtraLarge
                || formation.RootKey.Tier == TopDown3DRockSizeTier.Massive
                || formation.RootKey.Tier == TopDown3DRockSizeTier.Towering);
            var towering = formations.Count(formation =>
                formation.RootKey.Tier == TopDown3DRockSizeTier.Towering);

            Assert.That(formations.Count, Is.InRange(49, 225));
            Assert.That(mediumOrLarger, Is.GreaterThanOrEqualTo(18));
            Assert.That(extraLargeOrLarger, Is.GreaterThanOrEqualTo(5));
            Assert.That(towering, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void FormationTopology_UsesOnlyApprovedEdgesAndHonorsCaps()
        {
            var settings = LoadSettings();
            var formations = CollectFormations(settings, -8, 8);
            Assert.That(formations.Any(formation => formation.Members.Count > 1), Is.True);
            var foundBranch = false;
            foreach (var formation in formations)
            {
                Assert.That(formation.Members.Count, Is.InRange(1, settings.PhysicalFormationMaximumMembers));
                Assert.That(formation.Members[0].ParentIndex, Is.EqualTo(-1));
                var childCounts = new int[formation.Members.Count];
                for (var i = 1; i < formation.Members.Count; i++)
                {
                    var member = formation.Members[i];
                    Assert.That(member.MemberIndex, Is.EqualTo(i));
                    Assert.That(member.ParentIndex, Is.InRange(0, i - 1));
                    childCounts[member.ParentIndex]++;
                    Assert.That(IsApprovedEdge(
                        formation.Members[member.ParentIndex].Tier,
                        member.Tier), Is.True);
                    Assert.That(
                        GetMemberDepth(formation.Members, i),
                        Is.LessThanOrEqualTo(settings.PhysicalFormationMaximumDepth));
                }

                Assert.That(childCounts.All(count => count <= settings.FormationMaximumChildrenPerParent), Is.True);
                foundBranch |= childCounts.Any(count => count > 1);
            }

            Assert.That(foundBranch, Is.True, "Expected at least one parent with multiple touching children.");
        }

        [Test]
        public void FormationMembers_BlendAcrossConfiguredRangeAndTouchImmediateParents()
        {
            var settings = LoadSettings();
            var formations = CollectFormations(settings, -8, 8)
                .Where(formation => formation.Members.Count > 1)
                .ToArray();
            Assert.That(formations, Is.Not.Empty);
            var minimumObservedRatio = float.PositiveInfinity;
            var maximumObservedRatio = float.NegativeInfinity;
            foreach (var formation in formations)
            {
                for (var i = 1; i < formation.Members.Count; i++)
                {
                    var child = formation.Members[i];
                    var parent = formation.Members[child.ParentIndex];
                    var distance = Vector2.Distance(
                        new Vector2(child.Position.x, child.Position.z),
                        new Vector2(parent.Position.x, parent.Position.z));
                    var direction = new Vector2(
                        child.Position.x - parent.Position.x,
                        child.Position.z - parent.Position.z).normalized;
                    var contactDistance = GetDirectionalSupport(
                            settings.NaturalObjectCatalog,
                            parent,
                            direction)
                        + GetDirectionalSupport(
                            settings.NaturalObjectCatalog,
                            child,
                            -direction);
                    var parentDistanceRatio = distance / contactDistance;
                    minimumObservedRatio = Mathf.Min(minimumObservedRatio, parentDistanceRatio);
                    maximumObservedRatio = Mathf.Max(maximumObservedRatio, parentDistanceRatio);
                    Assert.That(
                        parentDistanceRatio,
                        Is.InRange(
                            settings.FormationMinimumParentDistanceRatio - 0.04f,
                            settings.FormationMaximumParentDistanceRatio + 0.04f),
                        $"{child.StableId} must touch its immediate parent {parent.StableId}.");
                    Assert.That(
                        Mathf.Min(child.WorldBounds.max.y, parent.WorldBounds.max.y),
                        Is.GreaterThan(Mathf.Max(child.WorldBounds.min.y, parent.WorldBounds.min.y)));
                    Assert.That(
                        TopDown3DRockVolumeOverlap.HasPositiveVolumeOverlap(
                            TopDown3DRockVolumeOverlap.CreateWorldSolid(
                                settings.NaturalObjectCatalog,
                                parent),
                            TopDown3DRockVolumeOverlap.CreateWorldSolid(
                                settings.NaturalObjectCatalog,
                                child)),
                        Is.True,
                        $"{child.StableId} must have a cleared interior witness in " +
                        $"its immediate parent {parent.StableId}.");

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
                                child.SupportRadius + other.SupportRadius - 0.001f));
                    }
                }
            }

            Assert.That(
                minimumObservedRatio,
                Is.LessThan(settings.FormationMinimumParentDistanceRatio + 0.12f),
                "Expected deeply fused child rocks in the sampled formations.");
            Assert.That(
                maximumObservedRatio,
                Is.GreaterThan(settings.FormationMaximumParentDistanceRatio - 0.12f),
                "Expected looser edge-contact child rocks in the sampled formations.");
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
                    new TopDown3DWorldGenerator(settings),
                    settings.NaturalObjectCatalog,
                    coordinate,
                    DistantExclusion);
                var changed = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                    changedPhysicalVersion,
                    new TopDown3DWorldGenerator(changedPhysicalVersion),
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
        public void Decorator_ConsolidatesRenderingPerFormationAndKeepsColliderPerMember()
        {
            var settings = LoadSettings();
            var coordinate = FindChunkWithMultiMemberFormation(settings);
            var plan = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                settings,
                new TopDown3DWorldGenerator(settings),
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
                    plan);

                var obstacles = chunk.GetComponentsInChildren<TopDown3DTraversalObstacle>();
                Assert.That(obstacles.Length, Is.EqualTo(plan.PhysicalFormations.Count));
                var orderedObstacles = obstacles
                    .OrderBy(obstacle => obstacle.transform.GetSiblingIndex())
                    .ToArray();
                for (var formationIndex = 0;
                     formationIndex < plan.PhysicalFormations.Count;
                     formationIndex++)
                {
                    Assert.That(
                        orderedObstacles[formationIndex].name,
                        Does.Contain(plan.PhysicalFormations[formationIndex].StableId));
                }

                for (var i = 0; i < obstacles.Length; i++)
                {
                    var matchingPlan = plan.PhysicalFormations.Single(formation =>
                        obstacles[i].name.Contains(formation.StableId));
                    Assert.That(obstacles[i].GetComponent<Collider>(), Is.Null);
                    Assert.That(
                        obstacles[i].GetComponentsInChildren<MeshRenderer>().Length,
                        Is.EqualTo(3));
                    Assert.That(
                        obstacles[i].GetComponentsInChildren<LODGroup>().Length,
                        Is.EqualTo(1));
                    Assert.That(
                        obstacles[i].GetComponentsInChildren<BoxCollider>().Length,
                        Is.EqualTo(matchingPlan.Members.Count));
                    var lods = obstacles[i].GetComponent<LODGroup>().GetLODs();
                    Assert.That(lods.Length, Is.EqualTo(3));
                    for (var lodIndex = 0; lodIndex < lods.Length; lodIndex++)
                    {
                        Assert.That(lods[lodIndex].renderers.Length, Is.EqualTo(1));
                        var combinedMesh = lods[lodIndex].renderers[0]
                            .GetComponent<MeshFilter>()
                            .sharedMesh;
                        var expectedVertexCount = matchingPlan.Members.Sum(member =>
                            settings.NaturalObjectCatalog
                                .GetRequiredMeshFamily(member.Shape, member.Variant)
                                .GetLod(lodIndex)
                                .vertexCount);
                        Assert.That(combinedMesh.vertexCount, Is.EqualTo(expectedVertexCount));
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(chunkObject);
            }
        }

        [Test]
        public void BakedRockFamily_ProvidesFiniteReadableLodVariants()
        {
            var catalog = LoadSettings().NaturalObjectCatalog;
            foreach (TopDown3DNaturalObjectShape shape in Enum.GetValues(typeof(TopDown3DNaturalObjectShape)))
            {
                var distinctBounds = new HashSet<Vector3>();
                for (var variant = 0; variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape; variant++)
                {
                    var family = catalog.GetRequiredMeshFamily(shape, variant);
                    var usesApprovedRecipe = BooterBigArm.Editor.TopDown3DProductionRockBaker
                        .UsesApprovedRestingPose(shape);
                    var previousTriangleCount = int.MaxValue;
                    for (var lodIndex = 0; lodIndex < 3; lodIndex++)
                    {
                        var mesh = family.GetLod(lodIndex);
                        Assert.That(mesh, Is.Not.Null);
                        Assert.That(mesh.isReadable, Is.True);
                        Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                        // The 600-vertex cap belongs to the original procedural primitives,
                        // not the accepted workbench-baked Boulder/Slab/Nodule recipes.
                        if (!usesApprovedRecipe)
                            Assert.That(mesh.vertexCount, Is.LessThan(600));
                        Assert.That(mesh.triangles.Length % 3, Is.Zero);
                        Assert.That(mesh.triangles.Length, Is.GreaterThan(0).And.LessThan(previousTriangleCount));
                        previousTriangleCount = mesh.triangles.Length;
                        Assert.That(mesh.bounds.size.sqrMagnitude, Is.GreaterThan(0f));
                        if (usesApprovedRecipe)
                        {
                            Assert.That(mesh.bounds.min.y, Is.LessThanOrEqualTo(0.001f));
                            Assert.That(
                                mesh.bounds.min.y,
                                Is.GreaterThanOrEqualTo(-mesh.bounds.size.y * 0.2f));
                        }
                        else
                        {
                            Assert.That(mesh.bounds.min.y, Is.GreaterThanOrEqualTo(-0.0001f));
                        }
                        Assert.That(mesh.normals.Length, Is.EqualTo(mesh.vertexCount));
                        AssertFiniteUnitNormals(mesh);
                    }

                    distinctBounds.Add(family.Lod0.bounds.size);
                }

                Assert.That(distinctBounds.Count, Is.EqualTo(TopDown3DNaturalObjectCatalog.MeshVariantsPerShape));
            }
        }

        private static void AssertFiniteUnitNormals(Mesh mesh)
        {
            var normals = mesh.normals;
            for (var vertex = 0; vertex < normals.Length; vertex++)
            {
                Assert.That(float.IsNaN(normals[vertex].x), Is.False);
                Assert.That(float.IsNaN(normals[vertex].y), Is.False);
                Assert.That(float.IsNaN(normals[vertex].z), Is.False);
                Assert.That(normals[vertex].magnitude, Is.EqualTo(1f).Within(0.0001f));
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
            var generator = new TopDown3DWorldGenerator(settings);
            var placements = new List<TopDown3DNaturalObjectPlacement>();
            for (var z = -3; z <= 3; z++)
            {
                for (var x = -3; x <= 3; x++)
                {
                    placements.AddRange(
                        TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                                settings,
                                generator,
                                settings.NaturalObjectCatalog,
                                new Vector2Int(x, z),
                                DistantExclusion)
                            .CosmeticPlacements
                            .Where(placement =>
                                placement.Layer == TopDown3DNaturalObjectLayer.FineGrayCluster));
                }
            }
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
                        new TopDown3DWorldGenerator(settings),
                        settings.NaturalObjectCatalog,
                        new Vector2Int(x, z),
                        DistantExclusion));
                }
            }

            return formations;
        }

        private static Vector2Int FindChunkWithMultiMemberFormation(TopDown3DWorldSettings settings)
        {
            for (var z = -4; z <= 4; z++)
            {
                for (var x = -4; x <= 4; x++)
                {
                    var coordinate = new Vector2Int(x, z);
                    var formations = TopDown3DRockFormationPlanner.BuildPhysicalFormations(
                        settings,
                        new TopDown3DWorldGenerator(settings),
                        settings.NaturalObjectCatalog,
                        coordinate,
                        DistantExclusion);
                    if (formations.Any(formation => formation.Members.Count > 1))
                    {
                        return coordinate;
                    }
                }
            }

            Assert.Fail("Expected a multi-member physical-rock formation in the sampled area.");
            return default;
        }

        private static float GetSpacing(TopDown3DWorldSettings settings, TopDown3DRockSizeTier tier)
        {
            switch (tier)
            {
                case TopDown3DRockSizeTier.Small:
                    return settings.SmallRockSpacing;
                case TopDown3DRockSizeTier.Medium:
                    return settings.MediumRockSpacing;
                case TopDown3DRockSizeTier.Large:
                    return settings.PropSpacing;
                case TopDown3DRockSizeTier.ExtraLarge:
                    return settings.ExtraLargeRockSpacing;
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
                || parent == TopDown3DRockSizeTier.Massive && child == TopDown3DRockSizeTier.ExtraLarge
                || parent == TopDown3DRockSizeTier.ExtraLarge && child == TopDown3DRockSizeTier.Large
                || parent == TopDown3DRockSizeTier.Large && child == TopDown3DRockSizeTier.Medium
                || parent == TopDown3DRockSizeTier.Medium && child == TopDown3DRockSizeTier.Small;
        }

        private static int GetMemberDepth(
            IReadOnlyList<TopDown3DRockFormationMember> members,
            int memberIndex)
        {
            var depth = 0;
            while (memberIndex > 0)
            {
                memberIndex = members[memberIndex].ParentIndex;
                depth++;
            }

            return depth;
        }

        private static float GetDirectionalSupport(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DRockFormationMember member,
            Vector2 direction)
        {
            var vertices = catalog.GetRequiredMeshFamily(member.Shape, member.Variant).Lod0.vertices;
            var maximum = 0f;
            for (var i = 0; i < vertices.Length; i++)
            {
                var point = member.Rotation * Vector3.Scale(vertices[i], member.Scale);
                maximum = Mathf.Max(maximum, point.x * direction.x + point.z * direction.y);
            }

            return maximum;
        }

        private static TopDown3DWorldSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            return settings;
        }
    }
}
