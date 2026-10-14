using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class LeggerRidingLifecycleTests
    {
        private readonly List<GameObject> objects = new();
        private TopDown3DLeggerRideProfile profile;
        private TopDown3DPlayerMotor player;
        private TopDown3DBigArmFollower follower;
        private TopDown3DLeggerRidingController owner;
        private TopDown3DInputRouter input;
        private TopDown3DAnimationPlaygroundSession lab;
        private Vector3 returnPosition;
        private GameObject New(string name)
        { var go = new GameObject(name); go.SetActive(false); objects.Add(go); return go; }
        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        [SetUp] public void SetUp()
        {
            returnPosition = new Vector3(23000, .935f, 23000);
            var floor = New("Reset support"); floor.transform.position = new Vector3(23000, -.1f, 23000);
            floor.AddComponent<BoxCollider>().size = new Vector3(20, .2f, 20); floor.AddComponent<TopDown3DGroundSurface>(); floor.SetActive(true);
            var booter = New("Passive Booter"); booter.transform.position = returnPosition;
            booter.AddComponent<Rigidbody>(); var capsule = booter.AddComponent<CapsuleCollider>(); capsule.height = 1.82f; capsule.radius = .2f;
            player = booter.AddComponent<TopDown3DPlayerMotor>(); var presentation = booter.AddComponent<TopDown3DPlayerAnimationDriver>();
            input = New("Reset input").AddComponent<TopDown3DInputRouter>();
            var camera = New("Reset camera").AddComponent<TopDown3DCameraRig>(); player.Configure(input, camera.transform);
            var legger = New("Legger reset position"); legger.transform.position = returnPosition + new Vector3(3, -.11f, 0);
            var box = legger.AddComponent<BoxCollider>(); box.size = new Vector3(1.1f, 2.2f, 1.3f); box.center = Vector3.up * .3f;
            follower = legger.AddComponent<TopDown3DBigArmFollower>();
            lab = New("Lab reset owner").AddComponent<TopDown3DAnimationPlaygroundSession>();
            var coordinator = New("Existing posture owner").AddComponent<AnimationPlaygroundActionCoordinator>(); Set(lab, "coordinator", coordinator);
            player.BindAnimationPostureOwner(coordinator);
            owner = New("Riding lifetime owner").AddComponent<TopDown3DLeggerRidingController>();
            profile = ScriptableObject.CreateInstance<TopDown3DLeggerRideProfile>();
            Set(owner, "player", player); Set(owner, "follower", follower); Set(owner, "input", input); Set(owner, "cameraRig", camera);
            Set(owner, "laboratory", lab); Set(owner, "profile", profile); Set(owner, "presentation", presentation);
            Set(owner, "ground", new LeggerGroundMotion(profile, follower.transform, player.transform, box, capsule, Vector3.up * 2.39f));
            var anchor = New("Supported actor return").transform; anchor.position = returnPosition; Set(owner, "resetReturns", new[] { anchor });
            follower.BindRiding(owner);
            Set(player, "<IsGrounded>k__BackingField", true); Assert.That(player.TryAcquireRider(owner), Is.True);
            owner.State.BeginMount(); owner.State.CommitMount(owner.State.Epoch);
            player.SetRiderPose(owner, follower.transform.position + Vector3.up * 2.39f, Quaternion.identity);
            lab.ResetRegistry.Register(owner.ResetParticipant); Physics.SyncTransforms();
        }
        [TearDown] public void TearDown()
        {
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear(); Object.DestroyImmediate(profile);
        }
        [Test] public void VerifiedResetReturnsBooterBeforeRestoringGravityAndDoesNotMoveLegger()
        {
            var position = follower.transform.position; input.SetLaboratoryResetBlocked(true);
            Assert.That(lab.ResetRegistry.TryReset(), Is.True, lab.ResetRegistry.LastFailure);
            Assert.That(player.Position, Is.EqualTo(returnPosition)); Assert.That(player.IsMounted, Is.False);
            Assert.That(player.GetComponent<Rigidbody>().isKinematic, Is.False);
            Assert.That(player.GetComponent<Rigidbody>().useGravity, Is.True);
            Assert.That(follower.transform.position, Is.EqualTo(position));
        }
        [Test] public void BlockedResetKeepsElevatedRiderPassiveAndInputBlocked()
        {
            var obstacle = New("Blocked return"); obstacle.transform.position = returnPosition; obstacle.AddComponent<BoxCollider>().size = Vector3.one * 2;
            obstacle.SetActive(true); Physics.SyncTransforms(); var elevated = player.Position;
            input.SetLaboratoryResetBlocked(true);
            Assert.That(lab.ResetRegistry.TryReset(), Is.False);
            Assert.That(player.IsMounted, Is.True); Assert.That(player.Position, Is.EqualTo(elevated));
            Assert.That(player.GetComponent<Rigidbody>().useGravity, Is.False); Assert.That(input.LaboratoryResetBlocked, Is.True);
        }
        [Test] public void ManagedResetLeaseCanRecoverAfterNativeControllerRemoval()
        {
            var position = follower.transform.position;
            Object.DestroyImmediate(owner.gameObject);
            Assert.That(player.IsMounted, Is.True);
            input.SetLaboratoryResetBlocked(true);
            Assert.That(lab.ResetRegistry.TryReset(), Is.True, lab.ResetRegistry.LastFailure);
            Assert.That(player.Position, Is.EqualTo(returnPosition)); Assert.That(player.IsMounted, Is.False);
            Assert.That(follower.transform.position, Is.EqualTo(position));
        }
        [Test] public void VerifiedResetCanRecoverPostureLeaseClearedByMotorDisable()
        {
            typeof(TopDown3DPlayerMotor).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
            Assert.That(player.IsMounted, Is.True);
            input.SetLaboratoryResetBlocked(true);
            Assert.That(lab.ResetRegistry.TryReset(), Is.True, lab.ResetRegistry.LastFailure);
            Assert.That(player.IsMounted, Is.False); Assert.That(player.IsAnimationPostureOwner(lab.Coordinator), Is.True);
        }
    }
}
