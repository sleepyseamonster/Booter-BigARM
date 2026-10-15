using System;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundLocomotionRepairTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(2.05f)]
        [TestCase(3.25f)]
        public void MeasuredStrideClockHasNoDominantRoleDiscontinuity(float speed)
        {
            var nominal = new Vector3(1.4f, 2.7f, 3.8f);
            var distance = new Vector3(3.17f, 4.37f, 5.37f);
            double Cadence(float s) => s / AnimationPlaygroundGaitClock.BlendedCycleDistance(s, nominal, distance);
            Assert.That(Math.Abs(Cadence(speed + .00001f) - Cadence(speed - .00001f)), Is.LessThan(.00002));
            Assert.That(Cadence(speed), Is.GreaterThan(0));
        }

        [Test] public void ClockCountsMultipleWrappedContactsAndKeepsCosmeticDroppingSeparate()
        {
            var clock = new AnimationPlaygroundGaitClock();
            clock.Advance(Sample(1, 1, 0), 1);
            var batch = clock.Advance(Sample(2, 1, 2.8f, 1), 1);
            Assert.That(clock.Cycles, Is.EqualTo(2.8).Within(.00001));
            Assert.That(batch.Count, Is.EqualTo(11));
            Assert.That(clock.ContactCount, Is.EqualTo(11));
            Assert.That(batch.FirstOrdinal, Is.EqualTo(1));
            Assert.That(batch.FirstRecentOrdinal(2), Is.EqualTo(10));
            Assert.That(AnimationPlaygroundGaitContacts.Side(10), Is.EqualTo(TopDown3DFootSide.Right));
            Assert.That(AnimationPlaygroundGaitContacts.Side(11), Is.EqualTo(TopDown3DFootSide.Left));
            Assert.That(clock.Advance(Sample(2, 1, 2.8f, 1), 1).Count, Is.Zero);
            Assert.That(clock.ContactCount, Is.EqualTo(11));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void RenderSubdivisionsCannotAdvanceSimulationOrDuplicateContacts(int renderRate)
        {
            var clock = new AnimationPlaygroundGaitClock();
            clock.Advance(Sample(1, 1, 0), 1);
            for (var tick = 2; tick <= 129; tick++)
            {
                clock.Advance(Sample(tick, 1, .03125f), 1);
                var cycles = clock.Cycles; var contacts = clock.ContactCount;
                for (var frame = 0; frame <= renderRate; frame++)
                {
                    var rendered = clock.RenderCycles((float)frame / renderRate);
                    Assert.That(rendered, Is.InRange(cycles - .03126, cycles + .00001));
                }
                Assert.That(clock.Cycles, Is.EqualTo(cycles));
                Assert.That(clock.ContactCount, Is.EqualTo(contacts));
            }
            Assert.That(clock.Cycles, Is.EqualTo(4).Within(.00001));
            Assert.That(clock.ContactCount, Is.EqualTo(16));
        }

        [Test] public void ClockSettlesWhenBlockedAndRebasesOwnedMotionBeforeResuming()
        {
            var clock = new AnimationPlaygroundGaitClock();
            clock.Advance(Sample(1, 1, 0), 1);
            clock.Advance(Sample(2, 1, .04f), 1);
            var cycles = clock.Cycles;
            clock.Advance(Sample(3, 1, 0), 1);
            Assert.That(clock.Travelling, Is.False); Assert.That(clock.Cycles, Is.EqualTo(cycles));
            clock.Advance(new AnimationPlaygroundGroundedMotion(4, 2, .08, .02, Vector3.forward * 2, Vector3.up, false, true), 1);
            Assert.That(clock.Cycles, Is.EqualTo(cycles));
            clock.Advance(Sample(5, 2, .004f), 1);
            Assert.That(clock.Travelling, Is.True);
            clock.Advance(Sample(6, 2, .0024f), 1);
            Assert.That(clock.Travelling, Is.True); // inside hysteresis after entering
            clock.Advance(Sample(7, 2, .001f), 1);
            Assert.That(clock.Travelling, Is.False);
        }

        private static AnimationPlaygroundGroundedMotion Sample(long tick, long epoch, float distance, double duration = .02) =>
            new(tick, epoch, tick * .02, duration, Vector3.forward * distance, Vector3.up, true, false);

        [Test] public void CompletedMotionUsesTravelAlongSupportAndIgnoresSettlement()
        {
            var tracker = new AnimationPlaygroundGroundedMotionTracker();
            var normal = new Vector3(0, 1, -1).normalized;
            var tangent = new Vector3(0, 1, 1).normalized * .08f;
            Assert.That(tracker.Observe(Vector3.zero, 0, true, 1, normal).Rebased, Is.True);
            var sample = tracker.Observe(tangent + normal * .03f, .02, true, 1, normal);
            Assert.That(sample.Available, Is.True);
            Assert.That(sample.Distance, Is.EqualTo(.08).Within(.00001));
            Assert.That(sample.Speed, Is.EqualTo(4).Within(.0001));
            Assert.That(Vector3.Distance(sample.TangentDisplacement, tangent), Is.LessThan(.00001));
            var stationary = tracker.Observe(tangent + normal * .06f, .04, true, 1, normal);
            Assert.That(stationary.Distance, Is.LessThan(.00001));
        }

        [Test] public void CompletedMotionRebasesSupportOwnerAndTeleportWithoutDuplicateTicks()
        {
            var tracker = new AnimationPlaygroundGroundedMotionTracker();
            tracker.Observe(Vector3.zero, 0, true, 1, Vector3.up);
            var moving = tracker.Observe(Vector3.forward * .05f, .02, true, 1, Vector3.up);
            var duplicate = tracker.Observe(Vector3.forward * .15f, .02, true, 1, Vector3.up);
            Assert.That(duplicate.Tick, Is.EqualTo(moving.Tick));
            Assert.That(duplicate.Distance, Is.Zero);
            var blocked = tracker.Observe(Vector3.forward * .05f, .04, true, 1, Vector3.up);
            Assert.That(blocked.Available, Is.True); Assert.That(blocked.Speed, Is.Zero);
            var changed = tracker.Observe(Vector3.forward * .1f, .06, true, 2, Vector3.up);
            Assert.That(changed.Rebased, Is.True); Assert.That(changed.Distance, Is.Zero);
            Assert.That(changed.Epoch, Is.GreaterThan(blocked.Epoch));
            var owned = tracker.Observe(Vector3.forward * .2f, .08, false, 2, Vector3.up);
            Assert.That(owned.Available, Is.False);
            Assert.That(tracker.Observe(Vector3.forward * .3f, .1, true, 2, Vector3.up).Distance, Is.Zero);
            Assert.That(tracker.Observe(Vector3.forward * 10, .12, true, 2, Vector3.up).Rebased, Is.True);
            tracker.Reset();
            Assert.That(tracker.Observe(Vector3.forward * 10.1f, .14, true, 2, Vector3.up).Distance, Is.Zero);
        }

        [Test] public void CompletedMotionRejectsInvalidObservations()
        {
            var tracker = new AnimationPlaygroundGroundedMotionTracker();
            Assert.Throws<ArgumentException>(() => tracker.Observe(new Vector3(float.NaN, 0, 0), 0, true, 1, Vector3.up));
            Assert.Throws<ArgumentException>(() => tracker.Observe(Vector3.zero, double.PositiveInfinity, true, 1, Vector3.up));
            Assert.That(tracker.Observe(Vector3.zero, 0, true, 0, Vector3.up).Available, Is.False);
        }

        [Test] public void MotorConstraintBoundariesRebaseEvenWhenActionCompletesBetweenTicks()
        {
            var actor = new GameObject("motion ownership rebase"); actor.SetActive(false);
            try
            {
                var motor = actor.AddComponent<TopDown3DPlayerMotor>();
                var tracker = (AnimationPlaygroundGroundedMotionTracker)typeof(TopDown3DPlayerMotor)
                    .GetField("groundedMotionTracker", Private).GetValue(motor);
                tracker.Observe(Vector3.zero, 0, true, 1, Vector3.up);
                Assert.That(tracker.Observe(Vector3.forward * .05f, .02, true, 1, Vector3.up).Available, Is.True);
                var owner = new object();
                Assert.That(motor.TryBeginActionConstraint(owner), Is.True);
                motor.EndActionConstraint(owner);
                var returned = tracker.Observe(Vector3.forward * .1f, .04, true, 1, Vector3.up);
                Assert.That(returned.Rebased, Is.True); Assert.That(returned.Distance, Is.Zero);
                motor.BindAnimationPostureOwner(owner);
                tracker.Observe(Vector3.forward * .1f, .06, true, 1, Vector3.up);
                Assert.That(tracker.Observe(Vector3.forward * .15f, .08, true, 1, Vector3.up).Available, Is.True);
                Assert.That(motor.TransferAnimationPostureOwner(owner, new object()), Is.True);
                Assert.That(tracker.Observe(Vector3.forward * .2f, .1, true, 1, Vector3.up).Rebased, Is.True);
            }
            finally { Object.DestroyImmediate(actor); }
        }

        [TestCase(0, 3.8f)]
        [TestCase(30, 3.8f)]
        [TestCase(32, 3.8f)]
        [TestCase(36, 2.6f)]
        [TestCase(40, 1.4f)]
        [TestCase(48, 1.4f)]
        public void UphillPolicyRetainsFlatSpeedAndBlendsToWalking(float grade, float expected)
        {
            var normal = Quaternion.AngleAxis(-grade, Vector3.right) * Vector3.up;
            Assert.That(AnimationPlaygroundUphillGait.SpeedLimit(3.8f, 1.4f, Vector3.forward, normal, 48),
                Is.EqualTo(expected).Within(.0001f));
        }

        [TestCase(90)]
        [TestCase(180)]
        [TestCase(270)]
        public void UphillPolicyDoesNotSlowLevelOrDownhillTravel(float heading)
        {
            var normal = Quaternion.AngleAxis(-48, Vector3.right) * Vector3.up;
            var direction = Quaternion.AngleAxis(heading, Vector3.up) * Vector3.forward;
            Assert.That(AnimationPlaygroundUphillGait.SpeedLimit(3.8f, 1.4f, direction, normal, 48), Is.EqualTo(3.8f));
        }

        [Test] public void UphillPolicyIsContinuousAndNeverIncreasesASlowerRequest()
        {
            var previous = 3.8f;
            for (var i = 0; i <= 1000; i++)
            {
                var normal = Quaternion.AngleAxis(-(30 + i * .012f), Vector3.right) * Vector3.up;
                var value = AnimationPlaygroundUphillGait.SpeedLimit(3.8f, 1.4f, Vector3.forward, normal, 48);
                Assert.That(value, Is.InRange(1.39999f, previous + .00001f));
                Assert.That(previous - value, Is.LessThan(.006f)); previous = value;
                Assert.That(AnimationPlaygroundUphillGait.SpeedLimit(.7f, 1.4f, Vector3.forward, normal, 48), Is.EqualTo(.7f));
            }
        }

        [TestCase(1f, false)]
        [TestCase(.5f, false)]
        [TestCase(1f, true)]
        [TestCase(.5f, true)]
        public void UpcomingInclineBrakesThroughTheExistingMotorAndKeepsInputMagnitude(float magnitude, bool slowWalk)
        {
            using var fixture = new UphillFixture();
            fixture.Input(magnitude, slowWalk);
            fixture.Body.linearVelocity = Vector3.forward * 3.8f;
            fixture.Tick();
            Assert.That(fixture.Motor.IsGrounded, Is.True);
            Assert.That(fixture.Motor.LocomotionSnapshot.DesiredPlanarVelocity.z, Is.EqualTo(1.4f * magnitude).Within(.0001f));
            Assert.That(fixture.Body.linearVelocity.z, Is.EqualTo(3.8f - 12.5f * Time.fixedDeltaTime).Within(.0001f));
            Assert.That(fixture.Motor.SprintActive, Is.False);
            Assert.That(fixture.Body.position, Is.EqualTo(fixture.StartPosition), "Velocity planning does not move the actor in this Edit Mode fixture.");
            fixture.Ramp.SetActive(false); Physics.SyncTransforms(); fixture.Tick();
            var flatSpeed = (slowWalk ? 1.4f : 3.8f) * magnitude;
            Assert.That(fixture.Motor.LocomotionSnapshot.DesiredPlanarVelocity.z, Is.EqualTo(flatSpeed).Within(.0001f));
        }

        [Test] public void UpcomingInclineIsDetectedBeforeTheReviewedBrakingDistanceIsConsumed()
        {
            using var fixture = new UphillFixture();
            fixture.Input(1, false); fixture.Body.position = fixture.StartPosition + Vector3.back * .25f;
            fixture.Body.linearVelocity = Vector3.forward * 3.8f; Physics.SyncTransforms(); fixture.Tick();
            Assert.That(fixture.Motor.LocomotionSnapshot.DesiredPlanarVelocity.z, Is.EqualTo(1.4f).Within(.0001f));
            var distanceToLeadingContact = .8f - .2f;
            var stoppingDistance = (3.8f * 3.8f - 1.4f * 1.4f) / (2 * 12.5f);
            Assert.That(stoppingDistance + 3.8f * Time.fixedDeltaTime, Is.LessThan(distanceToLeadingContact));
        }

        [TestCase(1f)]
        [TestCase(.5f)]
        public void SupportedSteepUphillTravelUsesWalkingMetresPerSecondAlongTheSurface(float magnitude)
        {
            using var fixture = new UphillFixture(); fixture.Input(magnitude, false);
            var height = Mathf.Tan(48 * Mathf.Deg2Rad) * .95f + .71f + .2f / Mathf.Cos(48 * Mathf.Deg2Rad);
            var position = new Vector3(87000, height, 87001.5f);
            fixture.Body.position = position; Physics.SyncTransforms();
            for (var tick = 0; tick < 16; tick++) fixture.Tick();
            Assert.That(fixture.Motor.IsGrounded, Is.True);
            Assert.That(fixture.Body.linearVelocity.magnitude, Is.EqualTo(1.4f * magnitude).Within(.001f));
            Assert.That(fixture.Motor.SprintActive, Is.False);
            Assert.That(fixture.Body.position, Is.EqualTo(position));
        }

        [Test] public void UphillPolicyRequiresConfiguredLabOwnershipAndIgnoresDynamicOrTriggerRamps()
        {
            using var fixture = new UphillFixture();
            fixture.Input(1, false);
            fixture.Ramp.GetComponent<BoxCollider>().isTrigger = true; Physics.SyncTransforms(); fixture.Tick();
            Assert.That(fixture.Motor.LocomotionSnapshot.DesiredPlanarVelocity.z, Is.EqualTo(3.8f).Within(.0001f));
            fixture.Ramp.GetComponent<BoxCollider>().isTrigger = false;
            fixture.Ramp.AddComponent<Rigidbody>().isKinematic = true; Physics.SyncTransforms(); fixture.Tick();
            Assert.That(fixture.Motor.LocomotionSnapshot.DesiredPlanarVelocity.z, Is.EqualTo(3.8f).Within(.0001f));
            Object.DestroyImmediate(fixture.Ramp.GetComponent<Rigidbody>());
            Assert.That(fixture.Motor.ReleaseAnimationPostureOwner(fixture.Owner), Is.True);
            Physics.SyncTransforms(); fixture.Tick();
            Assert.That(fixture.Motor.LocomotionSnapshot.DesiredPlanarVelocity.z, Is.EqualTo(3.8f).Within(.0001f));
        }

        // Explicit static query/velocity fixtures. No Play Mode or physics simulation.
        private sealed class UphillFixture : IDisposable
        {
            public readonly object Owner = new();
            public readonly Vector3 StartPosition = new(87000, .91f, 87000);
            public readonly TopDown3DPlayerMotor Motor;
            public readonly Rigidbody Body;
            public readonly GameObject Ramp;
            private readonly GameObject actor, floor, basis;
            private readonly TopDown3DInputRouter input;
            private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);

            public UphillFixture()
            {
                actor = new GameObject("Uphill velocity planning"); actor.SetActive(false); actor.transform.position = StartPosition;
                Motor = actor.AddComponent<TopDown3DPlayerMotor>(); Body = actor.GetComponent<Rigidbody>();
                var capsule = actor.GetComponent<CapsuleCollider>(); capsule.height = 1.82f; capsule.radius = .2f;
                input = actor.AddComponent<TopDown3DInputRouter>(); basis = new GameObject("Uphill camera basis");
                Set(Motor, "input", input); Set(Motor, "cameraBasis", basis.transform);
                Motor.BindAnimationPostureOwner(Owner); Motor.ConfigureLaboratorySpeeds(Owner, 1.4f, 2.7f, 3.8f);
                Motor.BlockUnqualifiedLaboratoryTraversal(Owner);
                floor = new GameObject("Uphill support"); floor.transform.position = new Vector3(87000, -.05f, 87000);
                floor.AddComponent<BoxCollider>().size = new Vector3(10, .1f, 10); floor.AddComponent<TopDown3DGroundSurface>();
                Ramp = new GameObject("Upcoming 48 degree ramp"); var rotation = Quaternion.AngleAxis(-48, Vector3.right);
                Ramp.transform.SetPositionAndRotation(new Vector3(87000, 0, 87000.55f) + rotation * new Vector3(0, -.05f, 2), rotation);
                Ramp.AddComponent<BoxCollider>().size = new Vector3(4, .1f, 4); Ramp.AddComponent<TopDown3DGroundSurface>();
                actor.SetActive(true); Physics.SyncTransforms();
            }
            public void Input(float magnitude, bool slowWalk)
            {
                Set(input, "<MoveValue>k__BackingField", Vector2.up * magnitude);
                Set(input, "<SprintHeld>k__BackingField", true);
                Set(input, "<AnimationSlowWalkHeld>k__BackingField", slowWalk);
            }
            public void Tick() => typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate", Private).Invoke(Motor, null);
            public void Dispose()
            { Object.DestroyImmediate(actor); Object.DestroyImmediate(floor); Object.DestroyImmediate(Ramp); Object.DestroyImmediate(basis); Physics.SyncTransforms(); }
        }

        [Test] public void SustainedDiagnosticCannotActivateOnProductionConfiguration()
        {
            var actor = new GameObject("diagnostic isolation"); actor.SetActive(false);
            try
            {
                var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                Assert.That(driver.LaboratorySustainedDiagnostic, Is.False);
                Assert.Throws<InvalidOperationException>(() => driver.SetLaboratorySustainedDiagnostic(true));
                driver.SetLaboratorySustainedDiagnostic(false);
            }
            finally { Object.DestroyImmediate(actor); }
        }

        [Test, Category("PrivateAnimationQualification")]
        public void SustainedDiagnosticGatesLegacyDispatchAndPreservesConstrainedOwner()
        {
            var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
            if (bindings == null) Assert.Ignore("Restore private character bindings.");
            var actor = new GameObject("diagnostic dispatch"); actor.SetActive(false);
            try
            {
                var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                driver.ConfigureLaboratory(bindings);
                var motor = actor.GetComponent<TopDown3DPlayerMotor>();
                typeof(TopDown3DPlayerAnimationDriver).GetField("motor", Private).SetValue(driver, motor);
                driver.SetLaboratorySustainedDiagnostic(true);
                var snapshot = new TopDown3DLocomotionSnapshot(Vector3.forward * 2.7f, Vector3.back * 2.7f,
                    Vector3.zero, Vector3.forward, 240, Vector3.up, true, false, false, -1, 180);
                var resolve = typeof(TopDown3DPlayerAnimationDriver).GetMethod("ResolveActionIndex", Private);
                Assert.That(resolve.Invoke(driver, new object[] { snapshot, 2.7f }), Is.EqualTo(-1));
                Assert.That(driver.CurrentLocomotionState, Is.EqualTo(TopDown3DLocomotionState.Sustained));
                typeof(TopDown3DPlayerAnimationDriver).GetField("campIndex", Private).SetValue(driver, 17);
                Assert.That(resolve.Invoke(driver, new object[] { snapshot, 2.7f }), Is.EqualTo(17));
                Assert.That(driver.CurrentLocomotionState, Is.EqualTo(TopDown3DLocomotionState.ConstrainedAction));
                driver.SetLaboratorySustainedDiagnostic(false);
                Assert.That(bindings.locomotion.TryValidate(out var reason), Is.True, reason);
            }
            finally { Object.DestroyImmediate(actor); }
        }
    }
}
