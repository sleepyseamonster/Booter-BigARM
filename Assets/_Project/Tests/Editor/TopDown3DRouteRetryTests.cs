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

        [Test]
        public void PopulationBarrierFreezesTraversalWaypointCampAndClimbWithoutRelocatingBooter()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var owner = new GameObject("Preparation motor fixture"); SceneManager.MoveGameObjectToScene(owner, scene);
                var motor = owner.AddComponent<TopDown3DPlayerMotor>(); var body = owner.GetComponent<Rigidbody>();
                Set(motor, "body", body); Set(motor, "capsule", owner.GetComponent<CapsuleCollider>());
                body.useGravity = false; body.linearVelocity = new Vector3(3, 2, 4); var position = body.position;
                Fixed(motor); Assert.That(body.linearVelocity, Is.EqualTo(new Vector3(3, 2, 4)), "Unbound production behavior is retained.");
                Bounds asked = default; motor.PopulationPreparation = bounds => { asked = bounds; return false; };
                Fixed(motor); Assert.That(motor.WaitingForPopulation, Is.True); Assert.That(body.linearVelocity, Is.EqualTo(Vector3.up * 2));
                Assert.That(body.position, Is.EqualTo(position));
                motor.GetType().GetProperty("ActiveTraversal").SetValue(motor, TopDown3DTraversalMove.Vault);
                Set(motor, "traversalStart", position); Set(motor, "traversalEnd", Vector3.right * 40);
                Set(motor, "traversalStartRotation", Quaternion.identity); Set(motor, "traversalEndRotation", Quaternion.identity);
                Set(motor, "activeTraversalElapsed", .25f); Set(motor, "activeTraversalDuration", 1f);
                Fixed(motor); Assert.That((float)Get(motor, "activeTraversalElapsed"), Is.EqualTo(.25f));
                Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero)); Assert.That(asked.Contains(Vector3.right * 40), Is.True);
                motor.PopulationPreparation = _ => true; Fixed(motor);
                Assert.That((float)Get(motor, "activeTraversalElapsed"), Is.GreaterThan(.25f), "Ready traversal resumes rather than consuming waiting time.");
                motor.GetType().GetProperty("ActiveTraversal").SetValue(motor, TopDown3DTraversalMove.None);
                motor.PopulationPreparation = _ => false;
                Set(motor, "actionConstraintOwner", new object()); Set(motor, "waypointTravelActive", true);
                Set(motor, "waypointPath", new[] { Vector3.right * 60 }); Set(motor, "waypointIndex", 0);
                Fixed(motor); Assert.That((int)Get(motor, "waypointIndex"), Is.Zero);
                Set(motor, "waypointTravelActive", false); Set(motor, "campApproaching", true); Set(motor, "campApproachRemaining", 3f);
                Fixed(motor); Assert.That((float)Get(motor, "campApproachRemaining"), Is.EqualTo(3));
                Set(motor, "climbMode", TopDown3DClimbMode.Wall); body.linearVelocity = Vector3.up * 3;
                Fixed(motor); Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
                Assert.That(motor.CanWalkWaypointSegment(Vector3.zero, Vector3.right * 60), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void OffscreenBigArmPreparationPrecedesGroundingAndRouteProbesWithoutTeleport()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var follower = Create(scene); var body = follower.GetComponent<Rigidbody>(); body.isKinematic = true;
                var target = new GameObject("Independent target"); SceneManager.MoveGameObjectToScene(target, scene); target.transform.position = Vector3.right * 10;
                follower.Configure(target.transform, null, null); var position = body.position;
                Bounds region = default; follower.PopulationPreparation = bounds => { region = bounds; return false; };
                Fixed(follower); Assert.That(follower.State, Is.EqualTo(TopDown3DBigArmFollower.FollowState.WaitingForPopulation));
                Assert.That(follower.TryGetRouteDirection(target.transform.position, out _), Is.False);
                Assert.That(follower.RouteBuildAttempts, Is.Zero); Assert.That(body.position, Is.EqualTo(position));
                Assert.That(region.size.x, Is.GreaterThanOrEqualTo(15), "Local route probes are included without a camera.");
                follower.PopulationPreparation = _ => true; follower.TryGetRouteDirection(target.transform.position, out _);
                Assert.That(follower.RouteBuildAttempts, Is.EqualTo(1));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void Fixed(object owner) => owner.GetType().GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
        private static object Get(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
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
