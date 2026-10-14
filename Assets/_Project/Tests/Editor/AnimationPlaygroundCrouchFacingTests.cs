using System;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundCrouchFacingTests
    {
        [Test] public void ContactAtTheLoopBoundaryMergesItsTwoFragments()
        {
            var windows=new[]{
                new AnimationPlaygroundMotionSampler.ContactWindow{start=.01f,end=.18f,phase=.0475f},
                new AnimationPlaygroundMotionSampler.ContactWindow{start=.9f,end=1.2f,phase=.525f},
                new AnimationPlaygroundMotionSampler.ContactWindow{start=1.95f,end=1.98f,phase=.9825f}};
            var result=AnimationPlaygroundCrouchFacingAuthoring.CyclicContactPhases(windows,2,.02f);
            Assert.That(result.Length,Is.EqualTo(2));Assert.That(result[0],Is.EqualTo(.0325f).Within(.00001f));
            Assert.That(result[1],Is.EqualTo(.525f));
        }
        [Test] public void UnexplainedExtraContactsAndInvalidTimelineRequireReview()
        {
            var windows=new[]{
                new AnimationPlaygroundMotionSampler.ContactWindow{start=.1f,end=.2f,phase=.15f},
                new AnimationPlaygroundMotionSampler.ContactWindow{start=.4f,end=.5f,phase=.45f},
                new AnimationPlaygroundMotionSampler.ContactWindow{start=.7f,end=.8f,phase=.75f}};
            Assert.Throws<System.IO.InvalidDataException>(()=>AnimationPlaygroundCrouchFacingAuthoring.CyclicContactPhases(windows,1,.02f));
            Assert.Throws<ArgumentException>(()=>AnimationPlaygroundCrouchFacingAuthoring.CyclicContactPhases(windows,float.NaN,.02f));
        }
        [Test] public void SoleContactRequiresGroundHeightAndMotionOpposingTheTravelAxis()
        {
            var frames=new AnimationPlaygroundMotionSampler.PoseFrame[11];
            for(var i=0;i<frames.Length;i++)frames[i]=new AnimationPlaygroundMotionSampler.PoseFrame
                {seconds=i*.1f,leftFoot=new Vector3(0,0,-i*.1f),leftSole=new Vector3(0,-.91f,0)};
            Assert.That(AnimationPlaygroundCrouchFacingAuthoring.FindSoleContactWindows(frames,true,Vector3.forward,-.91f).Length,Is.EqualTo(1));
            Assert.That(AnimationPlaygroundCrouchFacingAuthoring.FindSoleContactWindows(frames,true,Vector3.back,-.91f),Is.Empty);
            foreach(var frame in frames)frame.leftSole.y=-.8f;
            Assert.That(AnimationPlaygroundCrouchFacingAuthoring.FindSoleContactWindows(frames,true,Vector3.forward,-.91f),Is.Empty);
            Assert.Throws<ArgumentException>(()=>AnimationPlaygroundCrouchFacingAuthoring.FindSoleContactWindows(frames,true,Vector3.up,-.91f));
        }
    }
}
