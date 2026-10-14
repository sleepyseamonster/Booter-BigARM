using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundCrouchClockTests
    {
        [Test] public void SustainedPostureHasNoFiniteActionQuotaAndUsesLocalDirections()
        {
            var clock=new AnimationPlaygroundCrouchClock();
            for(var i=0;i<5000;i++)clock.Advance(.02f,Vector3.right,Vector3.forward,1,1,true,false);
            Assert.That(clock.Pose.Direction,Is.EqualTo(Vector2.right));Assert.That(clock.Pose.MovingWeight,Is.EqualTo(1));
            clock.Advance(.02f,Vector3.back,Vector3.forward,1,1,true,false);
            Assert.That(clock.Pose.Direction,Is.EqualTo(Vector2.down));
            clock.Advance(.02f,Vector3.forward,Vector3.right,1,1,true,false);
            Assert.That(clock.Pose.Direction,Is.EqualTo(Vector2.left));
        }
        [Test] public void SkippedContactBoundariesEmitAlternatingMarkersAtTheirActualPosePhase()
        {
            var clock=new AnimationPlaygroundCrouchClock();var sides=new List<TopDown3DFootSide>();var phases=new List<float>();
            clock.Contact+=side=>{sides.Add(side);phases.Add(clock.Pose.Phase);};
            clock.Advance(.8f,Vector3.forward,Vector3.forward,1,1,true,false);
            Assert.That(sides,Is.EqualTo(new[]{TopDown3DFootSide.Left,TopDown3DFootSide.Right,TopDown3DFootSide.Left}));
            Assert.That(phases,Is.EqualTo(new[]{.25f,.5f,.75f}));
            Assert.That(clock.Pose.Phase,Is.EqualTo(.8f).Within(.00001f));
        }
        [Test] public void PauseAndUnsupportedMotionCannotConsumeContacts()
        {
            var clock=new AnimationPlaygroundCrouchClock();var contacts=0;clock.Contact+=_=>contacts++;
            clock.Advance(.2f,Vector3.forward,Vector3.forward,1,1,true,false);var before=clock.Pose;
            clock.Advance(1,Vector3.right,Vector3.forward,1,1,true,true);
            Assert.That(clock.Pose.Phase,Is.EqualTo(before.Phase));Assert.That(clock.Pose.Direction,Is.EqualTo(before.Direction));
            clock.Advance(1,Vector3.forward,Vector3.forward,1,1,false,false);Assert.That(contacts,Is.Zero);
            clock.Advance(.1f,Vector3.forward,Vector3.forward,1,1,true,false);Assert.That(contacts,Is.EqualTo(1));
            clock.Reset();Assert.That(clock.Pose.MovingWeight,Is.Zero);Assert.That(clock.Pose.Phase,Is.Zero);
        }
        [Test] public void ClockRejectsInvalidOrUnboundedFixedSteps()
        {
            var clock=new AnimationPlaygroundCrouchClock();
            Assert.Throws<ArgumentException>(()=>clock.Advance(float.NaN,Vector3.zero,Vector3.forward,1,1,true,false));
            Assert.Throws<ArgumentException>(()=>clock.Advance(10,Vector3.zero,Vector3.forward,1,1,true,false));
            Assert.Throws<ArgumentException>(()=>clock.Advance(.02f,Vector3.zero,Vector3.zero,1,1,true,false));
            Assert.Throws<ArgumentException>(()=>clock.Advance(.02f,Vector3.zero,Vector3.forward,1,.001f,true,false));
        }
        [Test] public void SlowActualMotionDoesNotForceHalfSpeedFootsteps()
        {
            var clock=new AnimationPlaygroundCrouchClock();var contacts=0;clock.Contact+=_=>contacts++;
            clock.Advance(.5f,Vector3.forward*.1f,Vector3.forward,1,1,true,false);
            Assert.That(clock.Pose.Phase,Is.EqualTo(.05f).Within(.00001f));Assert.That(contacts,Is.Zero);
        }
    }
}
