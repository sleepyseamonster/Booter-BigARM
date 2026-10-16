using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundDodgeMotorTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject actor, ground, wall;
        TopDown3DPlayerMotor motor;
        Rigidbody body;
        CapsuleCollider capsule;
        readonly object owner = new();
        void Grounded(bool value) => typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor, value);
        void Tick() => typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate", Private).Invoke(motor, null);
        bool Begin(object value = null) => motor.TryBeginAnimationMotion(value ?? owner, AnimationPlaygroundMotionMode.Dodge,
            policy: AnimationPlaygroundMotionPolicy.DodgeLocomotion);
        [SetUp] public void Setup()
        {
            actor = new GameObject("Dodge command fixture"); actor.SetActive(false); actor.transform.position = new Vector3(73000, .91f, 73000);
            motor = actor.AddComponent<TopDown3DPlayerMotor>(); body = actor.GetComponent<Rigidbody>(); capsule = actor.GetComponent<CapsuleCollider>();
            capsule.height = 1.82f; capsule.radius = .2f; capsule.center = Vector3.zero;
            motor.BindAnimationPostureOwner(owner); actor.SetActive(true);
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.position = new Vector3(73000, -.1f, 73000);
            ground.transform.localScale = new Vector3(20, .2f, 20); ground.AddComponent<TopDown3DGroundSurface>();
            Physics.SyncTransforms(); Grounded(true);
        }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(wall); Object.DestroyImmediate(ground); Object.DestroyImmediate(actor); Physics.SyncTransforms(); }

        [Test] public void AcquisitionAndReleaseRetainMomentumCapsuleGravityAndPosition()
        {
            var velocity = new Vector3(4.9f, -.2f, 0); body.linearVelocity = velocity; var position = body.position;
            Assert.That(Begin(), Is.True); Assert.That(body.linearVelocity, Is.EqualTo(velocity));
            Assert.That(motor.DodgeLocomotionActive, Is.True); Assert.That(motor.JumpLocomotionActive, Is.False);
            Assert.That(Begin(new object()), Is.False);
            Assert.That(motor.SetAnimationMotorIntent(new object(), new AnimationPlaygroundMotorIntent(Vector3.one, Vector3.forward)), Is.False);
            Assert.That(motor.ReleaseAnimationMotion(owner), Is.True); Assert.That(body.linearVelocity, Is.EqualTo(velocity));
            Assert.That(body.position, Is.EqualTo(position)); Assert.That(body.useGravity, Is.True);
            Assert.That(capsule.enabled, Is.True); Assert.That(capsule.direction, Is.EqualTo(1));
            Assert.That(capsule.height, Is.EqualTo(1.82f)); Assert.That(capsule.radius, Is.EqualTo(.2f));
            Assert.That(motor.IsActionConstrained, Is.False);
        }
        [Test] public void AirborneAndCompetingOwnersAreRejectedWithoutMutatingVelocity()
        {
            body.linearVelocity = Vector3.forward * 4.9f; Grounded(false); Assert.That(Begin(), Is.False);
            Assert.That(body.linearVelocity.z, Is.EqualTo(4.9f)); Grounded(true);
            var busy = new object(); motor.TryBeginActionConstraint(busy); var held = body.linearVelocity;
            Assert.That(Begin(), Is.False); Assert.That(body.linearVelocity, Is.EqualTo(held)); motor.EndActionConstraint(busy);
            Assert.That(Begin(new object()), Is.False);
        }
        [Test] public void FlatGroundDoesNotEraseDodgeTravelAndCommandsNeverTeleport()
        {
            var position = body.position; Assert.That(Begin(), Is.True);
            motor.SetAnimationMotorIntent(owner, new AnimationPlaygroundMotorIntent(Vector3.right * 5, Vector3.right)); Tick();
            Assert.That(body.linearVelocity.x, Is.EqualTo(5).Within(.001)); Assert.That(motor.AnimationSnapshot.Blocked, Is.False);
            Assert.That(motor.IsGrounded, Is.True); Assert.That(body.position, Is.EqualTo(position));
        }
        [Test] public void WallSweepConstrainsTheExistingCapsuleAndRetainsItsCollision()
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = actor.transform.position + Vector3.right * .31f;
            wall.transform.localScale = new Vector3(.1f, 4, 4); Physics.SyncTransforms(); Assert.That(Begin(), Is.True);
            motor.SetAnimationMotorIntent(owner, new AnimationPlaygroundMotorIntent(Vector3.right * 5, Vector3.right)); Tick();
            Assert.That(motor.AnimationSnapshot.Blocked, Is.True); Assert.That(body.linearVelocity.x, Is.InRange(0, 3));
            Assert.That(capsule.enabled, Is.True); Assert.That(body.detectCollisions, Is.True);
        }
        [Test] public void WalkableSlopeReceivesATangentAndPauseRetainsThePhysicalOwner()
        {
            ground.transform.rotation = Quaternion.Euler(0, 0, 20); Physics.SyncTransforms(); Assert.That(Begin(), Is.True);
            motor.SetAnimationMotorIntent(owner, new AnimationPlaygroundMotorIntent(Vector3.right * 4, Vector3.right)); Tick();
            Assert.That(body.linearVelocity.x, Is.GreaterThan(0)); Assert.That(body.linearVelocity.y, Is.GreaterThan(0));
            Assert.That(Vector3.Dot(body.linearVelocity, ground.transform.up), Is.EqualTo(0).Within(.002));
            motor.SetAnimationMotorIntent(owner, new AnimationPlaygroundMotorIntent(Vector3.right * 4, Vector3.right, paused: true)); Tick();
            Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero)); Assert.That(body.useGravity, Is.False);
            Assert.That(motor.IsAnimationMotionOwner(owner), Is.True);
            motor.SetAnimationMotorIntent(owner, new AnimationPlaygroundMotorIntent(Vector3.right * 4, Vector3.right)); Tick();
            Assert.That(body.useGravity, Is.True); Assert.That(body.linearVelocity.x, Is.GreaterThan(0));
        }
        [Test] public void SupportLossPreservesVerticalVelocityAndOrdinaryGravity()
        {
            Object.DestroyImmediate(ground); Physics.SyncTransforms(); Grounded(true); Assert.That(Begin(), Is.True);
            body.linearVelocity = Vector3.down * 3;
            motor.SetAnimationMotorIntent(owner, new AnimationPlaygroundMotorIntent(Vector3.forward * 4, Vector3.forward)); Tick();
            Assert.That(motor.IsGrounded, Is.False); Assert.That(body.linearVelocity.y, Is.EqualTo(-3));
            Assert.That(body.useGravity, Is.True); Assert.That(motor.JumpGravityMagnitude, Is.EqualTo(Mathf.Abs(Physics.gravity.y)));
        }
    }
}
