using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DCameraClearanceTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private readonly Vector3 origin = new Vector3(12345f, 12345f, 12345f);
        private TopDown3DCameraRig rig;
        private Transform target;

        [SetUp]
        public void SetUp()
        {
            target = CreateObject("Clearance target", origin - Vector3.up * 1.15f).transform;
            var cameraObject = CreateObject("Clearance camera", origin);
            cameraObject.AddComponent<Camera>();
            rig = cameraObject.AddComponent<TopDown3DCameraRig>();
            var serialized = new SerializedObject(rig);
            serialized.FindProperty("obstructionMask").intValue = 1 << 31;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            rig.Configure(target);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            Physics.SyncTransforms();
        }

        [Test]
        public void ClearBoom_PreservesAuthoredDistance()
        {
            Assert.That(rig.TryResolveDistance(origin, Vector3.forward, out var distance), Is.True);
            Assert.That(distance, Is.EqualTo(25f).Within(0.001f));
        }

        [Test]
        public void CloseBlocker_OverridesMinimumDistanceWithoutPenetration()
        {
            var blocker = Box(origin + Vector3.forward * 2f, Vector3.one);
            Physics.SyncTransforms();
            Assert.That(rig.TryResolveDistance(origin, Vector3.forward, out var distance), Is.True);
            Assert.That(distance, Is.LessThan(4f));
            Assert.That(distance, Is.GreaterThanOrEqualTo(0f));
            AssertClear(origin + Vector3.forward * distance, blocker);
        }

        [Test]
        public void ConfigureAndViewReset_ResolveClearanceImmediately()
        {
            var backward = -(Quaternion.Euler(rig.PitchDegrees, rig.YawDegrees, 0f) * Vector3.forward);
            var blocker = Box(origin + backward * 7f, Vector3.one * 2f);
            Physics.SyncTransforms();
            rig.Configure(target);
            Assert.That(rig.HasUnresolvedClearance, Is.False);
            Assert.That(Vector3.Distance(origin, rig.transform.position), Is.LessThan(7f));
            AssertClear(rig.transform.position, blocker);
            rig.SetViewAngles(rig.YawDegrees, rig.PitchDegrees);
            AssertClear(rig.transform.position, blocker);
        }

        [Test]
        public void FollowUpdate_UsesSameClearanceAsSnap()
        {
            var backward = -(Quaternion.Euler(rig.PitchDegrees, rig.YawDegrees, 0f) * Vector3.forward);
            var blocker = Box(origin + backward * 7f, Vector3.one * 2f);
            Physics.SyncTransforms();
            var followUpdate = typeof(TopDown3DCameraRig).GetMethod("LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(followUpdate, Is.Not.Null);
            followUpdate.Invoke(rig, null);
            Assert.That(rig.HasUnresolvedClearance, Is.False);
            Assert.That(Vector3.Distance(origin, rig.transform.position), Is.LessThan(7f));
            AssertClear(rig.transform.position, blocker);
        }

        [Test]
        public void OriginInsideBlocker_RecoversToClearPointOnAuthoredBoom()
        {
            var blocker = Box(origin, Vector3.one * 2f);
            Box(origin + Vector3.forward * 5f, Vector3.one);
            Physics.SyncTransforms();
            Assert.That(rig.TryResolveDistance(origin, Vector3.forward, out var distance), Is.True);
            Assert.That(distance, Is.InRange(1.35f, 4.5f));
            AssertClear(origin + Vector3.forward * distance, blocker);
        }

        [Test]
        public void SaturatedCast_FindsNearestBlocker()
        {
            BoxCollider nearest = null;
            for (var i = 0; i < 22; i++)
            {
                var blocker = Box(origin + Vector3.forward * (2f + i), new Vector3(2f, 2f, 0.1f));
                if (i == 0) nearest = blocker;
            }
            Physics.SyncTransforms();
            Assert.That(rig.TryResolveDistance(origin, Vector3.forward, out var distance), Is.True);
            Assert.That(distance, Is.LessThan(2f));
            AssertClear(origin + Vector3.forward * distance, nearest);
        }

        [Test]
        public void SaturatedOriginOverlap_StillExcludesTargetAndFindsExternalBlocker()
        {
            for (var i = 0; i < 20; i++)
                Box(origin, Vector3.one).transform.SetParent(target, true);
            var blocker = Box(origin, Vector3.one * 2f);
            Physics.SyncTransforms();
            Assert.That(rig.TryResolveDistance(origin, Vector3.forward, out var distance), Is.True);
            AssertClear(origin + Vector3.forward * distance, blocker);
        }

        [Test]
        public void TargetHierarchyAndTriggers_AreIgnoredInBothQueries()
        {
            Box(origin, Vector3.one * 4f).transform.SetParent(target, true);
            Box(origin + Vector3.forward * 8f, Vector3.one).transform.SetParent(target, true);
            Box(origin, Vector3.one * 60f).isTrigger = true;
            Physics.SyncTransforms();
            Assert.That(rig.TryResolveDistance(origin, Vector3.forward, out var distance), Is.True);
            Assert.That(distance, Is.EqualTo(25f).Within(0.001f));
        }

        [Test]
        public void FullyEnclosedBoom_ReportsFailureAndDoesNotAcceptUnsafePose()
        {
            var previous = rig.transform.position;
            Box(origin, Vector3.one * 100f);
            Physics.SyncTransforms();
            Assert.That(rig.TryResolveDistance(origin, Vector3.forward, out _), Is.False);
            rig.Configure(target);
            Assert.That(rig.HasUnresolvedClearance, Is.True);
            Assert.That(rig.transform.position, Is.EqualTo(previous),
                "An invalid prior pose must not be reported as newly verified clearance.");
        }

        [Test]
        public void FullyEnclosedNewTarget_RetainsRecheckedSafePreviousPosition()
        {
            var previous = rig.transform.position;
            target.position += Vector3.right * 200f;
            Box(target.position + Vector3.up * 1.15f, Vector3.one * 100f);
            Physics.SyncTransforms();
            rig.Configure(target);
            Assert.That(rig.HasUnresolvedClearance, Is.True);
            Assert.That(rig.transform.position, Is.EqualTo(previous));
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            var result = new GameObject(name) { layer = 31 };
            result.transform.position = position;
            objects.Add(result);
            return result;
        }

        private BoxCollider Box(Vector3 position, Vector3 size)
        {
            var collider = CreateObject("Clearance blocker", position).AddComponent<BoxCollider>();
            collider.size = size;
            return collider;
        }

        private static void AssertClear(Vector3 position, Collider blocker)
        {
            Assert.That(Vector3.Distance(position, blocker.ClosestPoint(position)),
                Is.GreaterThanOrEqualTo(0.349f));
        }
    }
}
