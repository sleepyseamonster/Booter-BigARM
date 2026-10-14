using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundLowMotionProfileTests
    {
        private AnimationPlaygroundLowMotionProfile profile;
        [SetUp] public void Setup()
        {
            var source=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundLowMotionProfile>(AnimationPlaygroundLowMotionAuthoring.ProfilePath);
            Assert.That(source,Is.Not.Null,"Restore the private low-motion draft before this qualification gate.");
            profile=Object.Instantiate(source);
        }
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(profile);
        [Test] public void SixDistinctSegmentedPhasesRemainUnavailableUntilPhysicalQualification()
        {
            Assert.That(profile.Validate(out var error,false),Is.True,error);
            Assert.That(profile.qualified,Is.False);Assert.That(profile.Validate(out error),Is.False);
            Assert.That(error,Does.Contain("qualification is pending"));
            var clips=new System.Collections.Generic.HashSet<AnimationClip>();
            foreach(var entry in profile.entries)Assert.That(clips.Add(entry.clip),Is.True);
        }
        [Test] public void QualifiedFlagCannotBypassMissingRolesOrProjectionPairing()
        {
            profile.qualified=true;Assert.That(profile.Validate(out var error),Is.True,error);
            profile.entries=new[]{profile.entries[0]};
            Assert.That(profile.Validate(out _),Is.False);
        }
        [Test] public void WrongModelAndMissingBodyProjectionCannotPassTheImportContract()
        {
            profile.targetModel=new GameObject("Unpaired model");
            try{Assert.That(profile.Validate(out _,false),Is.False);}
            finally{Object.DestroyImmediate(profile.targetModel);}
            profile.bodyProjection=null;Assert.That(profile.Validate(out _,false),Is.False);
        }
        [Test] public void InvalidShapeValuesCannotReachTheMotor()
        {
            var entry=profile.entries[0];entry.capsuleHeight=AnimationCurve.Linear(0,1.82f,1,float.NaN);
            Assert.That(profile.Validate(out _,false),Is.False);
        }
        [Test] public void ACurveWithoutTheFullPhaseRangeCannotQualify()
        {
            profile.entries[0].capsuleHeight=AnimationCurve.Linear(.1f,1.8f,.9f,1);
            Assert.That(profile.Validate(out _,false),Is.False);
        }
        [Test] public void OversizedLongitudinalShapeAndUnboundedSpeedRemainRejected()
        {
            profile.lowLength=4;Assert.That(profile.Validate(out _,false),Is.False);
            profile.lowLength=2.2f;profile.rollSpeed=100;Assert.That(profile.Validate(out _,false),Is.False);
        }
    }
}
