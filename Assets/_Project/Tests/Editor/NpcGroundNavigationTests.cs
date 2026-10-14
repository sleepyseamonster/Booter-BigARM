using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class NpcGroundNavigationTests
    {
        private Scene scene;
        private NpcGroundMotor motor;
        private NpcTuning tuning;
        private GameObject target;
        [SetUp] public void Setup()
        {
            // Always a disposable candidate: other fixtures may leave an untitled scene.
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = Box("Ground", new Vector3(0, -.5f, 0), new Vector3(100, 1, 100)); floor.AddComponent<TopDown3DGroundSurface>();
            var root = new GameObject("NPC"); SceneManager.MoveGameObjectToScene(root, scene);
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var capsule = root.AddComponent<CapsuleCollider>(); capsule.radius = .67f; capsule.height = 1.4f; capsule.center = Vector3.up * .7f;
            motor = root.AddComponent<NpcGroundMotor>(); root.transform.position = Vector3.up * NpcGroundMotor.Skin; body.position = root.transform.position;
            target = new GameObject("Threat"); SceneManager.MoveGameObjectToScene(target, scene); target.transform.position = new Vector3(0, 0, -8);
            var targetShape = target.AddComponent<CapsuleCollider>(); targetShape.height = 1.8f; targetShape.radius = .2f; targetShape.center = Vector3.up * .9f;
            tuning = new(); Physics.SyncTransforms();
        }
        private GameObject Box(string name, Vector3 p, Vector3 size)
        { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); go.transform.position = p; var c = go.AddComponent<BoxCollider>(); c.size = size; return go; }
        [TearDown] public void Cleanup() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        [Test] public void FlatSupportAndThinWallSweepAreDifferentQueries()
        {
            Assert.That(motor.TryGround(motor.Position, tuning, false, out _), Is.True);
            Box("Thin wall", new Vector3(1.5f, 1, 0), new Vector3(.05f, 2, 4)); Physics.SyncTransforms();
            Assert.That(motor.Clear(new Vector3(3, .03f, 0)), Is.True);
            Assert.That(motor.SegmentClear(motor.Position, new Vector3(3, .03f, 0)), Is.False);
        }
        [Test] public void PartialSupportAtEdgeAndDistantLowerGroundAreRejected()
        {
            Assert.That(motor.TryGround(new Vector3(49.8f, .03f, 0), tuning, false, out _), Is.False);
            Assert.That(motor.TryGround(new Vector3(0, 3, 0), tuning, false, out _), Is.False);
        }
        [Test] public void CapsuleYawCannotChangeCollisionEnvelope()
        { var before = motor.Clear(motor.Position); motor.transform.rotation = Quaternion.Euler(0, 90, 0); Physics.SyncTransforms(); Assert.That(motor.Clear(motor.Position), Is.EqualTo(before)); Assert.That(motor.ValidateShape(out _), Is.True); }
        [Test] public void SightBehindFleeingBodyRefreshesButWallKeepsLastKnownPosition()
        {
            var sensor = new NpcThreatPerception();
            Assert.That(sensor.Sample(motor.transform, target.transform, tuning, 1), Is.True);
            Box("Occluder", new Vector3(0, 1, -4), new Vector3(5, 3, .2f)); Physics.SyncTransforms(); target.transform.position += Vector3.back;
            Assert.That(sensor.Sample(motor.transform, target.transform, tuning, 2), Is.False);
            Assert.That(sensor.Observation.ObservedAt, Is.EqualTo(1)); Assert.That(sensor.Observation.Position.z, Is.EqualTo(-8));
            Assert.That(sensor.Observation.IsFresh(3, tuning.memorySeconds), Is.False);
        }
        [Test] public void VisiblePursuerUpdatesForLongerThanMemory()
        { var sensor = new NpcThreatPerception(); for (var n = 0; n < 30; n++) Assert.That(sensor.Sample(motor.transform, target.transform, tuning, n * .2), Is.True); Assert.That(sensor.Observation.ObservedAt, Is.GreaterThan(5)); }
        [Test] public void EscapePlanningIsBoundedAndImprovesActualEndpoint()
        {
            var nav = new NpcLocalGroundNavigation(); var threat = new Vector3(-5, .03f, 0);
            nav.Plan(motor, new NpcIntent(threat, 7.5f), tuning, motor.Position, 35, 0);
            Assert.That(nav.HasRoute, Is.True); Assert.That(NpcMath.Distance(nav.Endpoint, threat), Is.GreaterThanOrEqualTo(6));
            Assert.That(nav.Searches, Is.LessThanOrEqualTo(8)); Assert.That(nav.ProbeCalls, Is.LessThanOrEqualTo(NpcLocalGroundNavigation.MaxProbeCalls));
            nav.Plan(motor, new NpcIntent(threat, 7.5f), tuning, motor.Position, 35, .1);
            Assert.That(nav.PlanCount, Is.EqualTo(1));
        }
        [Test] public void EnclosureAndAuthoredReservationsCannotBecomeEscapeSuccess()
        {
            var nav = new NpcLocalGroundNavigation();
            nav.Plan(motor, new NpcIntent(Vector3.left * 5, 7), tuning, motor.Position, 35, 0,
                new[] { new NpcReservation(motor.Position, 10) });
            Assert.That(nav.HasRoute, Is.False); Assert.That(nav.Status, Is.EqualTo(NpcMovementStatus.Blocked));
        }
        [Test] public void StackedUnreachableSurfaceDoesNotRelocateFixture()
        {
            var shelf = Box("Upper surface", new Vector3(0, 3, 0), new Vector3(3, .1f, 3)); shelf.AddComponent<TopDown3DGroundSurface>(); Physics.SyncTransforms();
            var original = motor.Position; Assert.That(motor.Restore(new Vector3(0, 2, 0), Quaternion.identity, tuning, out _), Is.False);
            Assert.That(motor.Position, Is.EqualTo(original));
        }
        [Test] public void SupportedEndpointsCannotCrossUnsupportedGap()
        {
            foreach (var collider in scene.GetRootGameObjects())
                if (collider.name == "Ground") Object.DestroyImmediate(collider);
            Box("Left support", new Vector3(-2, -.5f, 0), new Vector3(4, 1, 10)).AddComponent<TopDown3DGroundSurface>();
            Box("Right support", new Vector3(2.5f, -.5f, 0), new Vector3(4, 1, 10)).AddComponent<TopDown3DGroundSurface>();
            Physics.SyncTransforms();
            Assert.That(motor.Clear(new Vector3(-1, .03f, 0)), Is.True);
            Assert.That(motor.Clear(new Vector3(1.5f, .03f, 0)), Is.True);
            Assert.That(motor.SegmentClear(new Vector3(-1, .03f, 0), new Vector3(1.5f, .03f, 0)), Is.False);
        }
        [Test] public void PartialPathCanEscapeAnObstructedDestination()
        {
            // Block all requested ring endpoints, leaving safe intermediate progress.
            for (var n = 0; n < 8; n++)
            {
                var d = Quaternion.Euler(0, n * 45, 0) * Vector3.forward;
                var p = new Vector3(Mathf.RoundToInt(d.x * 6) * 1.25f, 1, Mathf.RoundToInt(d.z * 6) * 1.25f);
                Box("Blocked goal", p, new Vector3(.5f, 2, .5f));
            }
            Physics.SyncTransforms(); var nav = new NpcLocalGroundNavigation();
            nav.Plan(motor, new NpcIntent(Vector3.back * 5, 7), tuning, motor.Position, 35, 0);
            Assert.That(nav.HasRoute, Is.True); Assert.That(nav.Status, Is.EqualTo(NpcMovementStatus.Partial));
        }
    }
}
