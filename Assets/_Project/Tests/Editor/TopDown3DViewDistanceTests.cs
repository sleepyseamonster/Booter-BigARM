using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DViewDistanceTests
    {
        [TestCase("Low", 500f)]
        [TestCase("Medium", 1000f)]
        [TestCase("High", 2000f)]
        [TestCase("Maximum", 8000f)]
        public void CameraRigCannotOverwriteAcceptedDistance(string preset, float expected)
        {
            var root = new GameObject("View distance owner test", typeof(Camera));
            try
            {
                var range = root.AddComponent<BadwaterCameraRange>();
                var rig = root.AddComponent<TopDown3DCameraRig>();
                range.SetPreset(preset);
                typeof(TopDown3DCameraRig).GetMethod("ApplyLens", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(rig, null);
                Assert.That(root.GetComponent<Camera>().farClipPlane, Is.EqualTo(expected));
                range.enabled = false;
                range.enabled = true;
                Assert.That(root.GetComponent<Camera>().farClipPlane, Is.EqualTo(expected));
                Assert.That(range.FarClipPlane, Is.EqualTo(8000f), "Local preference must not change serialized baseline");
                Assert.That(range.DistanceHazeEnabled, Is.EqualTo(preset != "Maximum"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void GeneratedReferenceWithoutAuthoredRangeKeepsItsLens()
        {
            var root = new GameObject("Reference camera test", typeof(Camera));
            try
            {
                var rig = root.AddComponent<TopDown3DCameraRig>();
                typeof(TopDown3DCameraRig).GetMethod("ApplyLens", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(rig, null);
                Assert.That(root.GetComponent<Camera>().farClipPlane, Is.EqualTo(1300f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void FadeDoesNotAffectNearGroundAndReachesSkyBeforeClipping()
        {
            Assert.That(BadwaterCameraRange.EvaluateFade(50f, 500f), Is.Zero);
            Assert.That(BadwaterCameraRange.EvaluateFade(375f, 500f), Is.Zero);
            Assert.That(BadwaterCameraRange.EvaluateFade(437.5f, 500f), Is.EqualTo(0.5f));
            Assert.That(BadwaterCameraRange.EvaluateFade(500f, 500f), Is.EqualTo(1f));
            Assert.That(BadwaterCameraRange.EvaluateFade(510f, 500f), Is.EqualTo(1f));
        }

        [Test]
        public void ReducedDistanceDoesNotRequireAnAtmosphereOrDensityMap()
        {
            var root = new GameObject("Independent horizon test", typeof(Camera));
            try
            {
                var range = root.AddComponent<BadwaterCameraRange>();
                range.SetPreset("Low");
                Assert.That(range.DistanceHazeEnabled, Is.True);
                range.SetPreset("unexpected");
                Assert.That(range.Preset, Is.EqualTo("Maximum"));
                Assert.That(range.DistanceHazeEnabled, Is.False);
                Assert.That(root.GetComponent<Camera>().farClipPlane, Is.EqualTo(8000f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
