using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DDustDepositionTests
    {
        private const string WorldSettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        private const string TerrainMaterialPath =
            "Assets/_Project/Materials/TopDown3D/Greybox_Terrain.mat";

        [Test]
        public void Settings_ProvideAuthoredDepositedDustMaterialAndBoundedTuning()
        {
            var settings = LoadSettings();
            Assert.That(settings.DepositedDustMaterial, Is.Not.Null);
            Assert.That(
                settings.DepositedDustMaterial.shader.name,
                Is.EqualTo("BooterBigArm/TopDown3D/Broken World Deposited Dust"));
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            Assert.That(terrainMaterial, Is.Not.Null);
            Assert.That(
                settings.DepositedDustMaterial.GetTexture("_BaseMap"),
                Is.SameAs(terrainMaterial.GetTexture("_BaseMap")),
                "Deposited drifts should inherit the rust ground surface, not pale swept sand.");
            Assert.That(settings.DepositedDustMaterial.GetTag("RenderType", false), Is.EqualTo("Transparent"));
            Assert.That(settings.DepositedDustMaterial.GetFloat("_EdgeFeather"), Is.GreaterThan(0.1f));
            Assert.That(settings.DepositedDustMaterial.GetFloat("_Opacity"), Is.LessThan(1f));
            Assert.That(settings.DustOverlayQuadsPerAxis, Is.EqualTo(40));
            Assert.That(settings.DustMaximumBaseHeight, Is.GreaterThan(0f));
            Assert.That(settings.DustMaximumWakeHeight, Is.GreaterThan(settings.DustMaximumBaseHeight));
            Assert.That(settings.DustWakeLength, Is.LessThan(settings.ChunkSize * 0.3f));
            Assert.That(settings.MaximumDustDepositionSlope, Is.LessThan(settings.MaximumClutterSlope));
        }

        [Test]
        public void DepositionPlan_IsDeterministic()
        {
            var settings = LoadSettings();
            var coordinate = new Vector2Int(4, -3);
            var exclusion = new Vector2(10000f, 10000f);
            var first = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                settings.NaturalObjectCatalog,
                coordinate,
                exclusion);
            var second = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                settings.NaturalObjectCatalog,
                coordinate,
                exclusion);

            Assert.That(first.QuadsPerAxis, Is.EqualTo(second.QuadsPerAxis));
            Assert.That(first.Step, Is.EqualTo(second.Step));
            for (var z = 0; z < first.VerticesPerAxis; z++)
            {
                for (var x = 0; x < first.VerticesPerAxis; x++)
                {
                    Assert.That(first.GetSample(x, z), Is.EqualTo(second.GetSample(x, z)));
                }
            }
        }

        [Test]
        public void AdjacentChunkPlans_MatchExactlyAtTheirSharedBorder()
        {
            var settings = LoadSettings();
            var exclusion = new Vector2(10000f, 10000f);
            var left = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                settings.NaturalObjectCatalog,
                Vector2Int.zero,
                exclusion);
            var right = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                settings.NaturalObjectCatalog,
                Vector2Int.right,
                exclusion);

            Assert.That(left.VerticesPerAxis, Is.EqualTo(right.VerticesPerAxis));
            for (var z = 0; z < left.VerticesPerAxis; z++)
            {
                Assert.That(
                    left.GetSample(left.QuadsPerAxis, z),
                    Is.EqualTo(right.GetSample(0, z)));
            }
        }

        [Test]
        public void DepositMesh_ClipsBoundariesBetweenGridLines()
        {
            var settings = LoadSettings();
            var exclusion = new Vector2(10000f, 10000f);
            var foundDeposit = false;
            var foundContourVertex = false;
            for (var z = -8; z <= 8 && !foundContourVertex; z++)
            {
                for (var x = -8; x <= 8 && !foundContourVertex; x++)
                {
                    var coordinate = new Vector2Int(x, z);
                    var plan = TopDown3DDustDepositionPlanner.BuildPlan(
                        settings,
                        settings.NaturalObjectCatalog,
                        coordinate,
                        exclusion);
                    if (!plan.HasVisibleDeposits)
                    {
                        continue;
                    }

                    var mesh = TopDown3DDustDepositionDecorator.BuildMeshData(
                        settings,
                        coordinate,
                        plan);
                    foundDeposit |= mesh.Triangles.Length > 0;
                    Assert.That(mesh.Normals.Length, Is.EqualTo(mesh.Vertices.Length));
                    for (var index = 0; index < mesh.Vertices.Length; index++)
                    {
                        Assert.That(mesh.Normals[index].magnitude, Is.EqualTo(1f).Within(0.001f));
                        var normalizedX = mesh.Vertices[index].x / plan.Step;
                        var normalizedZ = mesh.Vertices[index].z / plan.Step;
                        if (Mathf.Abs(normalizedX - Mathf.Round(normalizedX)) > 0.001f
                            || Mathf.Abs(normalizedZ - Mathf.Round(normalizedZ)) > 0.001f)
                        {
                            foundContourVertex = true;
                            break;
                        }
                    }
                }
            }

            Assert.That(foundDeposit, Is.True);
            Assert.That(
                foundContourVertex,
                Is.True,
                "The visible boundary should be interpolated within cells, not emitted as full grid squares.");
        }

        [Test]
        public void BaseDeposits_FormLongerFeaturesAlongThePrevailingWind()
        {
            var settings = LoadSettings();
            var wind = TopDown3DDustDepositionPlanner.GetPrevailingWindDirection(settings);
            var crossWind = new Vector2(-wind.y, wind.x);
            var alongDifference = 0f;
            var crossDifference = 0f;
            var comparisons = 0;
            for (var z = -120; z <= 120; z += 12)
            {
                for (var x = -120; x <= 120; x += 12)
                {
                    var position = new Vector2(x, z);
                    var center = TopDown3DDustDepositionPlanner.SampleBaseWeight(settings, position);
                    alongDifference += Mathf.Abs(
                        center
                        - TopDown3DDustDepositionPlanner.SampleBaseWeight(
                            settings,
                            position + wind * 3f));
                    crossDifference += Mathf.Abs(
                        center
                        - TopDown3DDustDepositionPlanner.SampleBaseWeight(
                            settings,
                            position + crossWind * 3f));
                    comparisons++;
                }
            }

            Assert.That(comparisons, Is.GreaterThan(0));
            Assert.That(
                crossDifference,
                Is.GreaterThan(alongDifference * 1.08f),
                "Windrows should vary more across the wind than along it.");
        }

        [Test]
        public void BaseDeposits_LeaveMostOfTheWorldSurfaceExposed()
        {
            var settings = LoadSettings();
            var deposited = 0;
            var total = 0;
            for (var z = -180; z <= 180; z += 3)
            {
                for (var x = -180; x <= 180; x += 3)
                {
                    if (TopDown3DDustDepositionPlanner.SampleBaseWeight(
                            settings,
                            new Vector2(x, z)) >= 0.025f)
                    {
                        deposited++;
                    }

                    total++;
                }
            }

            var coverage = deposited / (float)total;
            Assert.That(coverage, Is.GreaterThan(0.01f));
            Assert.That(coverage, Is.LessThan(0.3f));
        }

        [Test]
        public void PhysicalRock_CreatesALeeSideWakeButNotAnUpwindPile()
        {
            var settings = LoadSettings();
            var wind = TopDown3DDustDepositionPlanner.GetPrevailingWindDirection(settings);
            var crossWind = new Vector2(-wind.y, wind.x);
            var source = new TopDown3DNaturalObjectPlacement(
                "test-rock",
                TopDown3DNaturalObjectLayer.Obstacle,
                TopDown3DNaturalObjectShape.Boulder,
                TopDown3DRockSurface.Regular,
                0,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one,
                1f,
                91273,
                3);
            var sources = new List<TopDown3DNaturalObjectPlacement> { source };
            var lee = TopDown3DDustDepositionPlanner.SampleShelterWeight(
                settings,
                wind * 1.4f,
                sources);
            var side = TopDown3DDustDepositionPlanner.SampleShelterWeight(
                settings,
                crossWind * 3f,
                sources);
            var upwind = TopDown3DDustDepositionPlanner.SampleShelterWeight(
                settings,
                -wind * 3f,
                sources);

            Assert.That(lee, Is.GreaterThan(0.4f));
            Assert.That(side, Is.LessThan(0.01f));
            Assert.That(upwind, Is.LessThan(0.01f));
        }

        [Test]
        public void LargerFormation_BuildsAHigherLeeSidePile()
        {
            var settings = LoadSettings();
            var wind = TopDown3DDustDepositionPlanner.GetPrevailingWindDirection(settings);
            var small = new TopDown3DNaturalObjectPlacement(
                "small-rock",
                TopDown3DNaturalObjectLayer.Obstacle,
                TopDown3DNaturalObjectShape.Boulder,
                TopDown3DRockSurface.Regular,
                0,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one,
                0.4f,
                7123,
                1);
            var large = new TopDown3DNaturalObjectPlacement(
                "large-formation",
                TopDown3DNaturalObjectLayer.Landmark,
                TopDown3DNaturalObjectShape.Boulder,
                TopDown3DRockSurface.Regular,
                0,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one * 3f,
                2.8f,
                7123,
                5);
            var samplePosition = wind * 1.5f;
            var smallPile = TopDown3DDustDepositionPlanner.SampleAt(
                settings,
                samplePosition,
                new List<TopDown3DNaturalObjectPlacement> { small });
            var largePile = TopDown3DDustDepositionPlanner.SampleAt(
                settings,
                samplePosition,
                new List<TopDown3DNaturalObjectPlacement> { large });

            Assert.That(largePile.Height, Is.GreaterThan(smallPile.Height * 1.45f));
            Assert.That(largePile.ShelterWeight, Is.GreaterThan(0.4f));
        }

        [Test]
        public void ObjectWake_FadesOutSoonAfterTheConfiguredShortLength()
        {
            var settings = LoadSettings();
            var wind = TopDown3DDustDepositionPlanner.GetPrevailingWindDirection(settings);
            var source = new TopDown3DNaturalObjectPlacement(
                "large-rock",
                TopDown3DNaturalObjectLayer.Obstacle,
                TopDown3DNaturalObjectShape.Boulder,
                TopDown3DRockSurface.Regular,
                0,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one * 2f,
                2f,
                3917,
                1);
            var sources = new List<TopDown3DNaturalObjectPlacement> { source };
            var near = TopDown3DDustDepositionPlanner.SampleShelterWeight(
                settings,
                wind * 1.25f,
                sources);
            var beyond = TopDown3DDustDepositionPlanner.SampleShelterWeight(
                settings,
                wind * (settings.DustWakeLength * 1.5f),
                sources);

            Assert.That(near, Is.GreaterThan(0.4f));
            Assert.That(beyond, Is.LessThan(0.01f));
        }

        private static TopDown3DWorldSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            return settings;
        }
    }
}
