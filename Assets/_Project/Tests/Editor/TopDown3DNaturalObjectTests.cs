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

        [Test]
        public void PlacementPlan_IsDeterministicAndChunkOwned()
        {
            var settings = LoadSettings();
            var coordinate = new Vector2Int(3, -2);
            var first = TopDown3DNaturalObjectPlanner.BuildPlacements(
                settings,
                settings.NaturalObjectCatalog,
                coordinate,
                new Vector2(10000f, 10000f));
            var second = TopDown3DNaturalObjectPlanner.BuildPlacements(
                settings,
                settings.NaturalObjectCatalog,
                coordinate,
                new Vector2(10000f, 10000f));

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.Not.Empty);
            Assert.That(first.All(placement =>
                Mathf.FloorToInt(placement.Position.x / settings.ChunkSize) == coordinate.x
                && Mathf.FloorToInt(placement.Position.z / settings.ChunkSize) == coordinate.y), Is.True);
        }

        [Test]
        public void ObstacleSpacing_RemainsValidAcrossChunkBorder()
        {
            var settings = LoadSettings();
            var exclusion = new Vector2(10000f, 10000f);
            var left = TopDown3DNaturalObjectPlanner.BuildPlacements(
                    settings,
                    settings.NaturalObjectCatalog,
                    Vector2Int.zero,
                    exclusion)
                .Where(placement => placement.Layer == TopDown3DNaturalObjectLayer.Obstacle)
                .ToArray();
            var right = TopDown3DNaturalObjectPlanner.BuildPlacements(
                    settings,
                    settings.NaturalObjectCatalog,
                    Vector2Int.right,
                    exclusion)
                .Where(placement => placement.Layer == TopDown3DNaturalObjectLayer.Obstacle)
                .ToArray();

            for (var i = 0; i < left.Length; i++)
            {
                for (var j = 0; j < right.Length; j++)
                {
                    var distance = Vector2.Distance(
                        new Vector2(left[i].Position.x, left[i].Position.z),
                        new Vector2(right[j].Position.x, right[j].Position.z));
                    Assert.That(
                        distance,
                        Is.GreaterThanOrEqualTo(
                            left[i].FootprintRadius + right[j].FootprintRadius + settings.PropSpacing - 0.0001f));
                }
            }
        }

        [Test]
        public void SpawnExclusion_AppliesToEveryNaturalObjectLayer()
        {
            var settings = LoadSettings();
            var center = new Vector2(settings.ChunkSize * 0.5f, settings.ChunkSize * 0.5f);
            var placements = TopDown3DNaturalObjectPlanner.BuildPlacements(
                settings,
                settings.NaturalObjectCatalog,
                Vector2Int.zero,
                center);

            Assert.That(placements, Is.Not.Empty);
            Assert.That(placements.All(placement =>
                Vector2.Distance(
                    new Vector2(placement.Position.x, placement.Position.z),
                    center) >= settings.ClearSpawnRadius + placement.FootprintRadius), Is.True);
        }

        [Test]
        public void Catalog_ContainsAllFourCostLayersWithUniqueStableIds()
        {
            var catalog = LoadSettings().NaturalObjectCatalog;
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.HasLayer(TopDown3DNaturalObjectLayer.GroundDetail), Is.True);
            Assert.That(catalog.HasLayer(TopDown3DNaturalObjectLayer.Scatter), Is.True);
            Assert.That(catalog.HasLayer(TopDown3DNaturalObjectLayer.Obstacle), Is.True);
            Assert.That(catalog.HasLayer(TopDown3DNaturalObjectLayer.FineGrayCluster), Is.True);
            Assert.That(LoadSettings().FineGrayClutterMaterial, Is.Not.Null);
            Assert.That(
                catalog.Definitions.Select(definition => definition.StableId).Distinct().Count(),
                Is.EqualTo(catalog.Definitions.Count));
        }

        [Test]
        public void ProceduralRockFamily_ProducesFiniteFacetedMeshes()
        {
            foreach (TopDown3DNaturalObjectShape shape in System.Enum.GetValues(typeof(TopDown3DNaturalObjectShape)))
            {
                for (var variant = 0; variant < 6; variant++)
                {
                    var mesh = TopDown3DNaturalMeshLibrary.GetMesh(shape, variant);
                    Assert.That(mesh, Is.Not.Null);
                    Assert.That(mesh.vertexCount, Is.GreaterThan(0).And.LessThan(600));
                    Assert.That(mesh.triangles.Length % 3, Is.Zero);
                    Assert.That(mesh.bounds.size.sqrMagnitude, Is.GreaterThan(0f));
                }
            }
        }

        [Test]
        public void FineGrayCluster_IsDenseStronglyClusteredAndSmall()
        {
            var settings = LoadSettings();
            Assert.That(settings.FineGrayClutterPerChunk, Is.GreaterThan(settings.GroundDetailsPerChunk));
            Assert.That(settings.FineGrayClusterStrength, Is.GreaterThan(settings.ClutterClusterStrength));
            Assert.That(settings.FineGrayClusterFrequency, Is.GreaterThan(settings.ClutterClusterFrequency));

            var placements = TopDown3DNaturalObjectPlanner.BuildPlacements(
                    settings,
                    settings.NaturalObjectCatalog,
                    new Vector2Int(2, 2),
                    new Vector2(10000f, 10000f))
                .Where(placement => placement.Layer == TopDown3DNaturalObjectLayer.FineGrayCluster)
                .ToArray();
            Assert.That(placements, Is.Not.Empty);
            Assert.That(placements.All(placement =>
                Mathf.Max(placement.Scale.x, placement.Scale.z) <= 0.15f), Is.True);
        }

        private static TopDown3DWorldSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            return settings;
        }
    }
}
