using System;
using BooterBigArm.TopDown3D;
using NUnit.Framework;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundClockTests
    {
        [Test]
        public void LongFrameCrossesEveryApprovedCycleWithoutDuplicatingEffects()
        {
            var clock=new AnimationPlaygroundActionClock(2,7,1,3,new[]{new AnimationPlaygroundMarker(0,.25),new AnimationPlaygroundMarker(1,.75)});
            clock.Advance(2.8);Assert.That(clock.PendingCount,Is.EqualTo(6));
            var count=0;while(clock.TryTake(out var key)){Assert.That(clock.WasCommitted(key),Is.False);Assert.That(clock.Commit(key),Is.True);Assert.That(clock.Commit(key),Is.False);count++;}
            Assert.That(count,Is.EqualTo(6));clock.Advance(5);Assert.That(clock.Complete,Is.True);Assert.That(clock.TryTake(out _),Is.False);
            Assert.That(clock.ClipTime,Is.EqualTo(1));
        }
        [Test]
        public void PauseCancelAndEpochPreventUncommittedOrStaleEffects()
        {
            var clock=new AnimationPlaygroundActionClock(3,9,1,1,new[]{new AnimationPlaygroundMarker(0,.5)});
            clock.Paused=true;clock.Advance(1);Assert.That(clock.Elapsed,Is.EqualTo(0));
            clock.Paused=false;clock.Advance(.6);Assert.That(clock.TryTake(out var key),Is.True);
            Assert.That(clock.Commit(new AnimationPlaygroundEffectKey(3,8,0,0)),Is.False);
            clock.Cancel();Assert.That(clock.Commit(key),Is.False);Assert.That(clock.WasCommitted(key),Is.False);Assert.That(clock.TryTake(out _),Is.False);
            var committed=new AnimationPlaygroundActionClock(4,10,1,1,new[]{new AnimationPlaygroundMarker(0,.5)});
            committed.Advance(.6);committed.TryTake(out var applied);committed.Commit(applied);committed.Cancel();
            Assert.That(committed.WasCommitted(applied),Is.True,"Cancellation cannot undo a committed effect.");
        }
        [Test]
        public void BoundaryMarkersAndInvalidClockInputsAreExplicit()
        {
            var clock=new AnimationPlaygroundActionClock(4,5,1,2,new[]{new AnimationPlaygroundMarker(0,0),new AnimationPlaygroundMarker(1,1)});
            clock.Advance(1);Assert.That(clock.PendingCount,Is.EqualTo(3));
            clock.Advance(1);Assert.That(clock.PendingCount,Is.EqualTo(4));
            Assert.Throws<ArgumentOutOfRangeException>(()=>clock.Advance(-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationPlaygroundActionClock(0,0,double.NaN,1,null));
            Assert.Throws<ArgumentException>(()=>new AnimationPlaygroundActionClock(0,0,1,1,new[]{new AnimationPlaygroundMarker(0,.3),new AnimationPlaygroundMarker(0,.6)}));
        }
    }
}
