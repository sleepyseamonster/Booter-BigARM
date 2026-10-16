using System;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundGaitTests
    {
        [Test] public void LandingPhaseRebaseSelectsTheContactSideWithoutEmittingDistanceContacts()
        {
            var clock=new AnimationPlaygroundGaitClock();var count=clock.ContactCount;
            clock.RebaseContact(TopDown3DFootSide.Left);
            Assert.That(clock.Cycles-Math.Floor(clock.Cycles),Is.EqualTo(.25).Within(.000001));
            clock.RebaseContact(TopDown3DFootSide.Right);
            Assert.That(clock.Cycles-Math.Floor(clock.Cycles),Is.EqualTo(0).Within(.000001));
            Assert.That(clock.ContactCount,Is.EqualTo(count));Assert.That(clock.Travelling,Is.False);
        }
        [Test] public void HeightCorrectionPreservesFlightAndRejectsBrokenRetargeting()
        {
            var measurement=new AnimationPlaygroundMotionSampler.MotionMeasurement{finite=true,seconds=1,frames=new[]{
                new AnimationPlaygroundMotionSampler.PoseFrame{seconds=0,leftSole=new Vector3(0,-.92f,0),rightSole=new Vector3(0,-.85f,0)},
                new AnimationPlaygroundMotionSampler.PoseFrame{seconds=.5f,leftSole=new Vector3(0,-.3f,0),rightSole=new Vector3(0,-.2f,0)},
                new AnimationPlaygroundMotionSampler.PoseFrame{seconds=1,leftSole=new Vector3(0,-.92f,0),rightSole=new Vector3(0,-.85f,0)}}};
            var curve=AnimationPlaygroundClipDerivatives.GroundCorrection(measurement,-.9f);
            Assert.That(curve.Evaluate(0),Is.EqualTo(.02f).Within(.00001));Assert.That(curve.Evaluate(.5f),Is.Zero);
            Assert.That(curve.Evaluate(1),Is.EqualTo(curve.Evaluate(0)));
            measurement.frames[0].leftSole=Vector3.down*1.3f;
            Assert.Throws<System.IO.InvalidDataException>(()=>AnimationPlaygroundClipDerivatives.GroundCorrection(measurement,-.9f));
        }
        [Test] public void LegacyEntryHasNoCorrectionAndInvalidLoopSeamsAreRejected()
        {
            var entry=new TopDown3DLocomotionClipEntry();Assert.That(entry.HeightCorrection(.5f),Is.Zero);Assert.That(entry.HasValidHeightCorrection(),Is.True);
            typeof(TopDown3DLocomotionClipEntry).GetField("looping",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(entry,true);
            var field=typeof(TopDown3DLocomotionClipEntry).GetField("visualHeightCorrection",BindingFlags.Instance|BindingFlags.NonPublic);
            field.SetValue(entry,AnimationCurve.Linear(0,0,1,.04f));Assert.That(entry.HasValidHeightCorrection(),Is.False);
            field.SetValue(entry,new AnimationCurve(new Keyframe(0,.01f),new Keyframe(.5f,.09f),new Keyframe(1,.01f)));
            Assert.That(entry.HasValidHeightCorrection(),Is.True);
        }
        [Test,Category("PrivateAnimationQualification")] public void HydratedProfileHasTwelveRolesAndActualTwoCycleSources()
        {
            var profile=AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(AnimationPlaygroundLocomotionAuthoring.ProfilePath);
            if(profile==null)Assert.Ignore("Restore the private qualification profile before running this gate.");
            Assert.That(profile.TryValidate(out var error),Is.True,error);
            foreach(TopDown3DLocomotionClipRole role in Enum.GetValues(typeof(TopDown3DLocomotionClipRole)))
            {var entry=profile.GetRequiredEntry(role);Assert.That(entry.Clip.isHumanMotion,Is.True);Assert.That(entry.HasVisualHeightCorrection,Is.True);}
            foreach(var role in new[]{TopDown3DLocomotionClipRole.Walk,TopDown3DLocomotionClipRole.Run,TopDown3DLocomotionClipRole.Sprint,
                TopDown3DLocomotionClipRole.DirectionalLeft,TopDown3DLocomotionClipRole.DirectionalRight})
            {
                var entry=profile.GetRequiredEntry(role);Assert.That(entry.Clip.isLooping,Is.True);
                Assert.That(entry.RightContactPhases[0],Is.LessThan(entry.LeftContactPhases[0]));
                Assert.That(entry.LeftContactPhases[0],Is.LessThan(entry.RightContactPhases[1]));
                Assert.That(entry.RightContactPhases[1],Is.LessThan(entry.LeftContactPhases[1]));
            }
        }
    }
}
