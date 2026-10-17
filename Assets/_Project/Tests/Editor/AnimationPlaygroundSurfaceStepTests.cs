using System;
using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    // Held collision-query/controller contracts, without gameplay or physics stepping.
    public sealed class AnimationPlaygroundSurfaceStepTests
    {
        private readonly List<GameObject> objects = new();
        private readonly object owner = new();
        private readonly Vector3 origin = new(22000, 0, 22000);
        private GameObject actor;
        private TopDown3DPlayerMotor motor;
        private Rigidbody body;
        private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void Setup()
        {
            actor = new GameObject("Held step actor"); objects.Add(actor); actor.SetActive(false);
            motor = actor.AddComponent<TopDown3DPlayerMotor>(); body = actor.GetComponent<Rigidbody>();
            var capsule = actor.GetComponent<CapsuleCollider>(); capsule.height = 1.82f; capsule.radius = .4f; capsule.center = Vector3.zero;
            typeof(TopDown3DPlayerMotor).GetMethod("EnsureBody", Hidden).Invoke(motor, null);
            motor.BindAnimationPostureOwner(owner); motor.ConfigureLaboratorySpeeds(owner, 1.4f, 3, 4.9f);
            actor.SetActive(true); Place(new Vector3(0, .91f, .37f));
            Box("Floor", new Vector3(0, -.1f, 0), new Vector3(20, .2f, 20));
            Assert.That(Probe(), Is.True);
        }
        [TearDown] public void Cleanup()
        {
            for (var i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear(); Physics.SyncTransforms();
        }
        private GameObject Box(string name, Vector3 position, Vector3 size, bool ground = true)
        {
            var value = new GameObject(name); objects.Add(value); value.transform.position = origin + position;
            value.AddComponent<BoxCollider>().size = size;
            if (ground) value.AddComponent<TopDown3DGroundSurface>(); Physics.SyncTransforms(); return value;
        }
        private void Place(Vector3 relative)
        { body.position = origin + relative; actor.transform.position = body.position; Physics.SyncTransforms(); }
        private bool Probe()
        {
            var args = new object[] { Vector3.zero };
            var supported = (bool)typeof(TopDown3DPlayerMotor).GetMethod("TryGetGroundNormal", Hidden).Invoke(motor, args);
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor, supported);
            return supported;
        }
        private bool Step(Vector3 velocity)
            => (bool)typeof(TopDown3DPlayerMotor).GetMethod("TryApplyLaboratoryGroundStep", Hidden).Invoke(motor, new object[] { velocity });
        private GameObject Tread(float height = .25f) => Box("Low tread", new Vector3(0, height * .5f, 1), new Vector3(3, height, .5f));

        [TestCase(1.4f)] [TestCase(3f)] [TestCase(4f)] [TestCase(4.9f)] [TestCase(6.15f)]
        public void QuarterMetreRiserClearsWithoutChangingPlanarVelocityOrFacing(float speed)
        {
            Tread(); var velocity = Vector3.forward * speed; body.linearVelocity = velocity;
            var before = body.position; var facing = body.rotation;
            Assert.That(Step(velocity), Is.True);
            Assert.That(body.position.y - before.y, Is.EqualTo(.26f).Within(.004));
            Assert.That(body.position.x, Is.EqualTo(before.x)); Assert.That(body.position.z, Is.EqualTo(before.z));
            Assert.That(body.linearVelocity, Is.EqualTo(velocity)); Assert.That(body.rotation, Is.EqualTo(facing));
            Assert.That(Probe(), Is.True, "The bounded step stays within real support-probe reach.");
        }
        [Test] public void ALeadingTreadDoesNotImmediatelySnapBackToTheTrailingFloor()
        {
            Tread(); Assert.That(Step(Vector3.forward * 1.4f), Is.True); Physics.SyncTransforms();
            Assert.That(Probe(), Is.True); var height = body.position.y;
            Step(Vector3.forward * 1.4f); Assert.That(body.position.y, Is.EqualTo(height).Within(.004));
        }
        [Test] public void DescendingANearbyTreadClosesTheGroundGapWithoutChangingPlanarVelocity()
        {
            Place(new Vector3(0, 1.17f, 0)); Assert.That(Probe(), Is.True);
            body.linearVelocity = Vector3.forward * 3; var before = body.position;
            Assert.That(Step(Vector3.forward * 3), Is.True);
            Assert.That(body.position.y, Is.EqualTo(.92f).Within(.004));
            Assert.That(body.position.x, Is.EqualTo(before.x)); Assert.That(body.position.z, Is.EqualTo(before.z));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector3.forward * 3));
        }
        [Test] public void HalfMetreObstacleStillRequiresAJump()
        {
            Tread(.5f); var before = body.position; Assert.That(Step(Vector3.forward * 3), Is.False);
            Assert.That(body.position, Is.EqualTo(before));
        }
        [Test] public void LowCeilingRejectsTheStepWithoutMovingTheActor()
        {
            Tread(); Box("Low ceiling", new Vector3(0, 1.93f, .6f), new Vector3(3, .15f, 2), false);
            var before = body.position; Assert.That(Step(Vector3.forward * 3), Is.False);
            Assert.That(body.position, Is.EqualTo(before));
        }
        [Test] public void ABlockedRaisedPathCannotPassThroughMarkedGroundGeometry()
        {
            Tread(); Box("Raised wall", new Vector3(0, 1.2f, .9f), new Vector3(3, 2, .1f));
            var before = body.position; Assert.That(Step(Vector3.forward * 6.15f), Is.False);
            Assert.That(body.position, Is.EqualTo(before));
        }
        [Test] public void MovingPropCannotSupplyAStaticStepTop()
        {
            var tread = Tread(); tread.AddComponent<Rigidbody>().isKinematic = true; Physics.SyncTransforms();
            var before = body.position; Assert.That(Step(Vector3.forward * 3), Is.False);
            Assert.That(body.position, Is.EqualTo(before));
        }
        [Test] public void TriggerCannotSupplyAStepTop()
        {
            Tread().GetComponent<Collider>().isTrigger = true; Physics.SyncTransforms();
            var before = body.position; Assert.That(Step(Vector3.forward * 3), Is.False);
            Assert.That(body.position, Is.EqualTo(before));
        }
        [Test] public void AirborneOrForeignActionOwnershipCannotApplyGroundAdhesion()
        {
            Place(new Vector3(0, 1.17f, 0)); Assert.That(Probe(), Is.True); var before = body.position;
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor, false);
            Assert.That(Step(Vector3.forward * 3), Is.False);
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor, true);
            var foreign = new object(); Assert.That(motor.TryBeginActionConstraint(foreign), Is.True);
            Assert.That(Step(Vector3.forward * 3), Is.False); Assert.That(body.position, Is.EqualTo(before));
            motor.EndActionConstraint(foreign);
        }
        [Test] public void ARealDropOutsideTheProbeBoundIsNotSnappedToGround()
        {
            Place(new Vector3(0, 1.5f, 0)); Probe(); var before = body.position;
            Assert.That(Step(Vector3.forward * 3), Is.False); Assert.That(body.position, Is.EqualTo(before));
        }
        [TestCase(7f)] [TestCase(14f)] [TestCase(30f)] [TestCase(40f)] [TestCase(48f)]
        public void SupportedRampCapsuleIsNotPulledIntoItsInclinedSurface(float angle)
        {
            var ramp = Box("Ramp", Vector3.zero, new Vector3(4, .2f, 6));
            ramp.transform.rotation = Quaternion.Euler(-angle, 0, 0);
            var normal = ramp.transform.up;
            Place(new Vector3(0, .51f + .5f / normal.y, 0)); Assert.That(Probe(), Is.True);
            var before = body.position;
            Assert.That(Step(Vector3.forward * 1.4f), Is.False); Assert.That(body.position, Is.EqualTo(before));
            Assert.That(Step(Vector3.back * 1.4f), Is.False); Assert.That(body.position, Is.EqualTo(before));
        }
        [Test] public void OverlappingGroundCandidatesGrowTheBufferAndRetainTheNearestValidFloor()
        {
            for (var i = 0; i < 20; i++) Box("Stacked support " + i, new Vector3(0, -.1f - i * .004f, 0), new Vector3(4, .2f, 4));
            Assert.That(Probe(), Is.True);
            var buffer = (RaycastHit[])typeof(TopDown3DPlayerMotor).GetField("groundProbeHits", Hidden).GetValue(motor);
            Assert.That(buffer.Length, Is.GreaterThan(8));
            var plane = (Plane)typeof(TopDown3DPlayerMotor).GetField("laboratoryGroundPlane", Hidden).GetValue(motor);
            Assert.That(Mathf.Abs(plane.GetDistanceToPoint(origin)), Is.LessThan(.004));
        }
        [Test] public void CompletedPositionCaptureExcludesStepCommandsOnASlopedObservation()
        {
            Tread(); Place(new Vector3(0, .91f, .342f)); Assert.That(Probe(), Is.True);
            var normal = Quaternion.Euler(-30, 0, 0) * Vector3.up;
            typeof(TopDown3DPlayerMotor).GetField("stableGroundNormal", Hidden).SetValue(motor, normal);
            typeof(TopDown3DPlayerMotor).GetField("hasStableGroundNormal", Hidden).SetValue(motor, true);
            var observe = typeof(TopDown3DPlayerMotor).GetMethod("ObserveCompletedLaboratoryMotionAtPosition", Hidden);
            var previous = body.position; observe.Invoke(motor, new object[] { 1d, previous });
            Place(new Vector3(0, .91f, .37f)); Assert.That(Probe(), Is.True);
            var completed = body.position; var expected = Vector3.ProjectOnPlane(completed - previous, normal);
            Assert.That(Step(Vector3.forward * 1.4f), Is.True);
            Assert.That(body.position.y - completed.y, Is.GreaterThan(.2f));
            observe.Invoke(motor, new object[] { 1.02d, completed });
            Assert.That(motor.LaboratoryGroundedMotion.Available, Is.True);
            Assert.That(Vector3.Distance(motor.LaboratoryGroundedMotion.TangentDisplacement, expected), Is.LessThan(.0001f));
            Assert.That(motor.LaboratoryGroundedMotion.Distance, Is.EqualTo(expected.magnitude).Within(.0001));
        }
        [Test] public void NearbySteepEdgeDoesNotHideTheWalkableFloor()
        {
            var edge = Box("Steep adjacent edge", new Vector3(.52f, .5f, .37f), new Vector3(1, .1f, 3));
            edge.transform.rotation = Quaternion.Euler(0, 0, 60); Physics.SyncTransforms();
            Assert.That(Probe(), Is.True);
            var plane = (Plane)typeof(TopDown3DPlayerMotor).GetField("laboratoryGroundPlane", Hidden).GetValue(motor);
            Assert.That(Vector3.Angle(plane.normal, Vector3.up), Is.LessThan(.01f));
        }
        [Test] public void AnUnboundMotorCannotAcquireTheCampLoopStepPolicy()
        {
            Tread(); Assert.That(motor.ReleaseAnimationPostureOwner(owner), Is.True); var before = body.position;
            Assert.That(Step(Vector3.forward * 3), Is.False); Assert.That(body.position, Is.EqualTo(before));
        }
        [Test] public void InvalidOrZeroMovementCannotMoveAnOtherwiseGroundedActor()
        {
            var before = body.position;
            Assert.That(Step(Vector3.zero), Is.False); Assert.That(Step(new Vector3(float.NaN, 0, 1)), Is.False);
            Assert.That(body.position, Is.EqualTo(before));
        }
    }
}
