using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRouteRetryTests
    {
        [Test]
        public void FailedRoutesRespectDeadlineEvenForMovingTargetsAndCallOverCanInvalidate()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var follower = Create(scene);
                var before = follower.GetComponent<Rigidbody>().position;
                Assert.That(follower.TryGetRouteDirection(Vector3.right * 5, out _), Is.False);
                for (int i = 0; i < 20; i++)
                    Assert.That(follower.TryGetRouteDirection(Vector3.right * (5 + i), out _), Is.False);
                Assert.That(follower.RouteBuildAttempts, Is.EqualTo(1));
                Set(follower, "routeReplanAt", -1f);
                Assert.That(follower.TryGetRouteDirection(Vector3.right * 5, out _), Is.False);
                Assert.That(follower.RouteBuildAttempts, Is.EqualTo(2));
                follower.RequestCallOver();
                Assert.That(follower.TryGetRouteDirection(Vector3.right * 5, out _), Is.False);
                Assert.That(follower.RouteBuildAttempts, Is.EqualTo(3));
                Assert.That(follower.GetComponent<Rigidbody>().position, Is.EqualTo(before));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void AlreadyConsumedRouteDoesNotBuildTwiceInOneUpdate()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var follower = Create(scene);
                Assert.That(follower.TryGetRouteDirection(Vector3.zero, out _), Is.False);
                Assert.That(follower.RouteBuildAttempts, Is.EqualTo(1));
                Assert.That(follower.TryGetRouteDirection(Vector3.zero, out _), Is.False);
                Assert.That(follower.RouteBuildAttempts, Is.EqualTo(1));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static TopDown3DBigArmFollower Create(Scene scene)
        {
            var owner = new GameObject("Route retry fixture"); SceneManager.MoveGameObjectToScene(owner, scene);
            var follower = owner.AddComponent<TopDown3DBigArmFollower>();
            Set(follower, "body", owner.GetComponent<Rigidbody>());
            Set(follower, "bodyCollider", owner.GetComponent<BoxCollider>());
            Set(follower, "movementMask", (LayerMask)0); // No scene collider can satisfy these synthetic probes.
            return follower;
        }
        private static void Set(object owner, string name, object value) => owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    }
}
