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
