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
                var source = new TopDown3DNaturalObjectPlacement(
                    "taper-test-rock",
                    TopDown3DNaturalObjectLayer.Obstacle,
                    TopDown3DNaturalObjectShape.Boulder,
                    TopDown3DRockSurface.Regular,
                    0,
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one * 1.4f,
                    1.4f,
                    19731,
                    1);
                var sources = new List<TopDown3DNaturalObjectPlacement> { source };
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
