using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class LeggerRideStateTests
    {
        [Test] public void RelationshipRejectsDuplicateAndStaleTransitions()
        {
            var state = new LeggerRideState();
            Assert.That(state.BeginMount(), Is.True); var epoch = state.Epoch;
            Assert.That(state.BeginMount(), Is.False);
            Assert.That(state.CommitMount(epoch - 1), Is.False);
            Assert.That(state.CommitMount(epoch), Is.True);
            Assert.That(state.CommitMount(epoch), Is.False);
            state.Clear(); Assert.That(state.CommitMount(epoch), Is.False);
        }
        [Test] public void HeldSteeringCannotImmediatelyCancelDismount()
        {
            var state = new LeggerRideState(); state.BeginMount(); state.CommitMount(state.Epoch); state.RequestExit();
            Assert.That(state.ObserveExitInput(Vector2.up), Is.False);
            Assert.That(state.Phase, Is.EqualTo(LeggerRidePhase.Dismounting));
            state.ObserveExitInput(Vector2.zero);
            Assert.That(state.ObserveExitInput(Vector2.up), Is.True);
            Assert.That(state.Phase, Is.EqualTo(LeggerRidePhase.Mounted));
        }
        [Test] public void EntryTimeoutReleasesReservationAndInvalidatesOldCommit()
        {
            var state = new LeggerRideState(); state.BeginMount(); var epoch = state.Epoch;
            Assert.That(state.AdvanceEntry(1, 2), Is.False);
            Assert.That(state.AdvanceEntry(1, 2), Is.True);
            Assert.That(state.OwnsMount, Is.False); Assert.That(state.CommitMount(epoch), Is.False);
        }
        [Test] public void ProfileRejectsNonFiniteAndUnsafeWorkCaps()
        {
            var p = ScriptableObject.CreateInstance<TopDown3DLeggerRideProfile>();
            try
            {
                Assert.That(p.Validate(out _), Is.True);
                p.fastSpeed = 24.1f; Assert.That(p.Validate(out _), Is.False);
                p.fastSpeed = float.NaN; Assert.That(p.Validate(out _), Is.False);
                p.fastSpeed = 10; p.queryBudget = 257; Assert.That(p.Validate(out _), Is.False);
                p.queryBudget = 256; p.slopeDegrees = 89; Assert.That(p.Validate(out _), Is.False);
                p.slopeDegrees = 75; Assert.That(p.Validate(out _), Is.True);
                p.steepSlopeSpeedMultiplier = 1.1f; Assert.That(p.Validate(out _), Is.False);
                p.steepSlopeSpeedMultiplier = float.NaN; Assert.That(p.Validate(out _), Is.False);
            }
            finally { Object.DestroyImmediate(p); }
        }

        [TestCase(0f, 1f)]
        [TestCase(37f, 1f)]
        [TestCase(45f, 1f)]
        [TestCase(60f, .675f)]
        [TestCase(75f, .35f)]
        public void SlopeSpeedIsFullThrough45ThenReducesProgressively(float degrees, float expected)
        {
            var p = ScriptableObject.CreateInstance<TopDown3DLeggerRideProfile>();
            try
            {
                var normal = Quaternion.Euler(-degrees, 0, 0) * Vector3.up;
                Assert.That(p.SpeedMultiplierForSlope(normal), Is.EqualTo(expected).Within(.0001f));
                Assert.That(p.normalSpeed * p.SpeedMultiplierForSlope(normal), Is.EqualTo(10 * expected).Within(.001f));
                Assert.That(p.fastSpeed * p.SpeedMultiplierForSlope(normal), Is.EqualTo(24 * expected).Within(.001f));
            }
            finally { Object.DestroyImmediate(p); }
        }
    }
}
