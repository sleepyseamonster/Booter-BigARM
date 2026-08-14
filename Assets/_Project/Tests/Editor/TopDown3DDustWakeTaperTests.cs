using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DDustWakeTaperTests
    {
        [Test]
        public void WakeTip_NarrowsAndSettlesIntoTheGround()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            try
            {
                MakeTerrainFlatAndRemoveBaseDeposition(settings);
                var wind = TopDown3DDustDepositionPlanner.GetPrevailingWindDirection(settings);
                var crossWind = new Vector2(-wind.y, wind.x);
                var bounds = new Bounds(Vector3.up, new Vector3(2.8f, 2f, 2.8f));
                var member = new TopDown3DRockFormationMember(
                    "taper-test-rock:0",
                    "taper-test-rock",
                    TopDown3DRockSizeTier.Large,
                    TopDown3DNaturalObjectShape.Boulder,
                    0,
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    0,
                    -1,
                    1.4f,
                    bounds);
                var source = new TopDown3DRockFormationPlan(
                    new TopDown3DRockRootKey(TopDown3DRockSizeTier.Large, 0, 0, 1),
                    "taper-test-rock",
                    19731,
                    TopDown3DNaturalObjectLayer.Obstacle,
                    TopDown3DRockSurface.Regular,
                    new[] { member },
                    Vector2.zero,
                    1.4f,
                    2f);
                var sources = new List<TopDown3DRockFormationPlan> { source };
                var middlePosition = wind * 1.5f;
                var tipPosition = wind * 2.9f;

                var middleCenter = TopDown3DDustDepositionPlanner.SampleShelterWeight(
                    settings,
                    middlePosition,
                    sources);
                var middleSide = TopDown3DDustDepositionPlanner.SampleShelterWeight(
                    settings,
                    middlePosition + crossWind * 0.45f,
                    sources);
                var tipCenter = TopDown3DDustDepositionPlanner.SampleShelterWeight(
                    settings,
                    tipPosition,
                    sources);
                var tipSide = TopDown3DDustDepositionPlanner.SampleShelterWeight(
                    settings,
                    tipPosition + crossWind * 0.45f,
                    sources);
                var middleHeight = TopDown3DDustDepositionPlanner.SampleAt(
                    settings,
                    middlePosition,
                    sources).Height;
                var tipHeight = TopDown3DDustDepositionPlanner.SampleAt(
                    settings,
                    tipPosition,
                    sources).Height;

                Assert.That(middleCenter, Is.GreaterThan(0.5f));
                Assert.That(middleSide, Is.GreaterThan(0.2f));
                Assert.That(tipCenter, Is.GreaterThan(0.01f));
                Assert.That(tipSide, Is.LessThan(tipCenter * 0.1f));
                Assert.That(tipHeight, Is.GreaterThan(0f));
                Assert.That(tipHeight, Is.LessThan(middleHeight * 0.1f));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        private static void MakeTerrainFlatAndRemoveBaseDeposition(
            TopDown3DWorldSettings settings)
        {
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("heightAmplitude").floatValue = 0f;
            serializedSettings.FindProperty("escarpmentRegionChance").floatValue = 0f;
            serializedSettings.FindProperty("dustMaximumBaseHeight").floatValue = 0f;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
