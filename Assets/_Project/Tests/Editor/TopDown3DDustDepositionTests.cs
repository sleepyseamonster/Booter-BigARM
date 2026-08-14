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

        [Test]
        public void Settings_ProvideAuthoredDepositedDustMaterialAndBoundedTuning()
        {
            var settings = LoadSettings();
            Assert.That(settings.DepositedDustMaterial, Is.Not.Null);
            Assert.That(
                settings.DepositedDustMaterial.shader.name,
                Is.EqualTo("BooterBigArm/TopDown3D/Broken World Deposited Dust"));
            Assert.That(settings.DustOverlayQuadsPerAxis, Is.InRange(8, 40));
            Assert.That(settings.DustMaximumBaseHeight, Is.GreaterThan(0f));
            Assert.That(settings.DustMaximumWakeHeight, Is.GreaterThan(settings.DustMaximumBaseHeight));
            Assert.That(settings.DustWakeLength, Is.LessThan(settings.ChunkSize));
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
                wind * 3f,
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

        private static TopDown3DWorldSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            return settings;
        }
    }
}
