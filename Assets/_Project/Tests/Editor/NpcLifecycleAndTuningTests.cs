using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class NpcLifecycleAndTuningTests
    {
        [Test] public void RearmRequiresFullResetSuccessAndCurrentEpoch()
        {
            Assert.That(NpcMath.CanRearm(true, false, null, 2, 2), Is.True);
            Assert.That(NpcMath.CanRearm(true, true, null, 2, 2), Is.False);
            Assert.That(NpcMath.CanRearm(true, false, "Later participant failed", 2, 2), Is.False);
            Assert.That(NpcMath.CanRearm(true, false, null, 3, 2), Is.False);
            Assert.That(NpcMath.CanRearm(false, false, null, 2, 2), Is.False);
        }
        [Test] public void FailedTuningIsAtomicAndCannotRearmActor()
        {
            var go = new GameObject("Tuning fixture");
            try
            {
                var actor = go.AddComponent<NpcActor>(); Assert.That(actor.ApplyTuning(new NpcTuning(), out _), Is.True);
                var revision = actor.IntentRevision;
                Assert.That(actor.ApplyTuning(new NpcTuning { fleeSpeed = float.NaN }, out _), Is.False);
                Assert.That(actor.Tuning.fleeSpeed, Is.EqualTo(7.5f)); Assert.That(actor.IntentRevision, Is.EqualTo(revision));
                actor.Invalidate(); Assert.That(actor.ApplyTuning(new NpcTuning { panic = 4 }, out _), Is.True);
                Assert.That(actor.IsArmed, Is.False); Assert.That(actor.Epoch, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void ObservationFutureTimestampsAndExpiredKnowledgeAreInvalid()
        { var observation = new NpcThreatObservation(Vector3.zero, 10); Assert.That(observation.IsFresh(9, 2), Is.False); Assert.That(observation.IsFresh(12, 2), Is.False); }
        [Test] public void ReservationAndExposureChecksIncludeSegmentInterior()
        {
            var from = new Vector3(-2, 0, 2); var to = new Vector3(2, 0, 2);
            Assert.That(NpcMath.ReservationsClear(from, to, .67f, new[] { new NpcReservation(new Vector3(0, 0, 2), 1) }), Is.False);
            NpcLocalGroundNavigation.Score(from, new Vector3(0, 0, 1), new[] { to }, Vector3.right, out _, out var exposure);
            Assert.That(exposure, Is.GreaterThan(1));
        }
    }
}
