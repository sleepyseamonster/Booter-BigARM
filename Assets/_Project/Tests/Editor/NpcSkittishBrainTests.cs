using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class NpcSkittishBrainTests
    {
        private NpcTuning tuning;
        private SkittishNpcBrain brain;
        [SetUp] public void Setup() { tuning = new(); brain = new(); brain.Reset(0); }
        private NpcIntent Tick(float distance, double now) => brain.Tick(new Vector3(distance, 0, 0), new NpcThreatObservation(Vector3.zero, now), now, tuning);
        [Test] public void ApproachWaitsForAlertThenRetreats()
        { Assert.That(Tick(8, 0).Move, Is.False); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Alert)); Assert.That(Tick(8, .35).Move, Is.True); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Retreat)); }
        [TestCase(5, NpcBehavior.Flee)] [TestCase(5.001f, NpcBehavior.Alert)]
        public void PanicEntryHasExactInclusiveBoundary(float distance, NpcBehavior expected)
        { Tick(distance, 0); Assert.That(brain.State, Is.EqualTo(expected)); }
        [Test] public void PanicReleaseRetainsThreatResponse()
        { Tick(4, 0); Tick(6.999f, 1); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Flee)); Tick(7, 2); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Retreat)); }
        [Test] public void SustainedVisiblePursuitDoesNotExpireMemory()
        { Tick(4, 0); for (var n = 1; n <= 30; n++) Tick(8, n * .2); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Retreat)); }
        [Test] public void RecoveryStopsMovementAndRequiresContinuousCalm()
        { Tick(4, 0); Assert.That(Tick(16, 1).Move, Is.False); Tick(16, 3.99); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Recover)); Tick(16, 4); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Idle)); }
        [Test] public void RenewedPressurePreemptsCalmHold()
        { Tick(4, 0); Tick(16, 1); Tick(9, 2); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Retreat)); Tick(16, 3); Tick(16, 5.99); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Recover)); }
        [Test] public void ExactMemoryExpiryStopsEscape()
        { Tick(4, 0); var intent = brain.Tick(new Vector3(4, 0, 0), new NpcThreatObservation(Vector3.zero, 0), 2, tuning); Assert.That(intent.Move, Is.False); Assert.That(brain.State, Is.EqualTo(NpcBehavior.Recover)); }
        [Test] public void InvalidProfileRejectsNanThresholdOrderAndUnboundedWork()
        { tuning.panic = float.NaN; Assert.That(tuning.Validate(out _), Is.False); tuning = new() { comfort = 5 }; Assert.That(tuning.Validate(out _), Is.False); tuning = new() { replanSeconds = .001f }; Assert.That(tuning.Validate(out _), Is.False); }
        [Test] public void SnapshotDoesNotMutateAuthoredProfile()
        { var profile = ScriptableObject.CreateInstance<SkittishNpcProfile>(); try { var copy = profile.Snapshot(); copy.panic = 1; Assert.That(profile.Snapshot().panic, Is.EqualTo(5)); } finally { Object.DestroyImmediate(profile); } }
    }
}
