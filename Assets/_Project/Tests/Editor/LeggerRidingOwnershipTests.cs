using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class LeggerRidingOwnershipTests
    {
        private GameObject playerObject, ownerObject, floor;
        private TopDown3DPlayerMotor motor;
        private TopDown3DLeggerRidingController owner;
        private Vector3 position;
        [SetUp] public void SetUp()
        {
            position = new Vector3(24000, .935f, 24000);
            floor = new GameObject("Standing support"); floor.transform.position = position + Vector3.down * 1.035f;
            floor.AddComponent<BoxCollider>().size = new Vector3(10, .2f, 10); floor.AddComponent<TopDown3DGroundSurface>();
            playerObject = new GameObject("Booter ownership fixture"); playerObject.SetActive(false); playerObject.transform.position = position;
            var capsule = playerObject.AddComponent<CapsuleCollider>(); capsule.height = 1.82f; capsule.radius = .2f;
            playerObject.AddComponent<Rigidbody>().useGravity = true;
            motor = playerObject.AddComponent<TopDown3DPlayerMotor>();
            typeof(TopDown3DPlayerMotor).GetField("<IsGrounded>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(motor, true);
            ownerObject = new GameObject("Riding owner fixture"); ownerObject.SetActive(false);
            owner = ownerObject.AddComponent<TopDown3DLeggerRidingController>(); Physics.SyncTransforms();
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(ownerObject); Object.DestroyImmediate(playerObject); Object.DestroyImmediate(floor);
        }
        [Test] public void ExclusiveLeaseSuppressesGravityAndRestoresCapturedBodyState()
        {
            Assert.That(motor.TryAcquireRider(owner), Is.True);
            Assert.That(motor.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(motor.GetComponent<Rigidbody>().useGravity, Is.False);
            Assert.That(motor.GetComponent<CapsuleCollider>().enabled, Is.False);
            Assert.That(motor.TryBeginActionConstraint(new object()), Is.False);
            Assert.That(motor.TryAcquireRider(owner), Is.False);
            Assert.That(motor.TryReleaseRider(owner, position, Quaternion.identity), Is.True);
            Assert.That(motor.GetComponent<Rigidbody>().isKinematic, Is.False);
            Assert.That(motor.GetComponent<Rigidbody>().useGravity, Is.True);
            Assert.That(motor.GetComponent<CapsuleCollider>().enabled, Is.True);
            Assert.That(motor.IsActionConstrained, Is.False);
        }
        [Test] public void ActualMotorCallbackYieldsBeforePopulationAndOtherMotionWriters()
        {
            Assert.That(motor.TryAcquireRider(owner), Is.True);
            motor.PopulationPreparation = _ => throw new System.InvalidOperationException("Mounted motor must yield before population.");
            motor.SetRiderPose(owner, position + Vector3.up * 3, Quaternion.Euler(0, 30, 0));
            var expected = motor.Position;
            typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(motor, null);
            Assert.That(motor.Position, Is.EqualTo(expected)); Assert.That(motor.IsGrounded, Is.False);
            Assert.That(motor.TryReleaseRider(owner, position, Quaternion.identity), Is.True);
        }
        [Test] public void UnsupportedOrBlockedReleaseLeavesRiderPassive()
        {
            motor.TryAcquireRider(owner);
            Assert.That(motor.TryReleaseRider(owner, position + Vector3.up * 5, Quaternion.identity), Is.False);
            var blocker = new GameObject("Exit obstruction"); blocker.transform.position = position;
            blocker.AddComponent<BoxCollider>().size = Vector3.one * 2; Physics.SyncTransforms();
            try { Assert.That(motor.TryReleaseRider(owner, position, Quaternion.identity), Is.False); Assert.That(motor.IsMounted, Is.True); }
            finally { Object.DestroyImmediate(blocker); }
        }
        [Test] public void ResetWithoutVerifiedLabOwnerAndEpochCannotReleaseRider()
        {
            motor.TryAcquireRider(owner);
            Assert.That(motor.TryResetRider(owner, new object(), owner.State.Epoch, position), Is.False);
            Assert.That(motor.IsMounted, Is.True);
        }
        [Test] public void DestroyedControllerCannotReenableNormalMotorWrites()
        {
            motor.TryAcquireRider(owner);
            motor.PopulationPreparation = _ => throw new System.InvalidOperationException("A lost owner must remain passive pending safe reset.");
            Object.DestroyImmediate(ownerObject);
            Assert.That(motor.IsMounted, Is.True);
            typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(motor, null);
            Assert.That(motor.GetComponent<Rigidbody>().isKinematic, Is.True);
        }
    }
}
