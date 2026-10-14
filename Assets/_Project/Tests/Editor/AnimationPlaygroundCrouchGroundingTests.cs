using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundCrouchGroundingTests
    {
        [Test] public void GroundingInterpolatesTheBlendWeightsAndWrapsAcrossForward()
        {
            var profile=ScriptableObject.CreateInstance<AnimationPlaygroundCrouchProfile>();
            try
            {
                profile.grounding=AnimationPlaygroundCrouchPoseQualification.ZeroGrounding();
                for(var i=1;i<profile.grounding.Length;i++)
                {
                    var sample=profile.grounding[i];var value=sample.directionIndex*.001f+sample.weightIndex*.01f;
                    sample.correction=AnimationCurve.Linear(0,value,1,value);
                    Assert.That(profile.GroundCorrection(AnimationPlaygroundCrouchProfile.GroundDirection(sample.directionIndex),sample.weightIndex/(float)AnimationPlaygroundCrouchProfile.GroundWeightCount,.3f),
                        Is.EqualTo(value).Within(.000001f));
                }
                Assert.That(profile.GroundCorrection(new Vector2(.0625f,.9375f),.1875f,.3f),Is.EqualTo(.0155f).Within(.000001f));
                Assert.That(profile.GroundCorrection(new Vector2(-.0625f,.9375f),.25f,.3f),Is.EqualTo(.0355f).Within(.000001f));
                Assert.That(profile.GroundCorrection(Vector2.zero,0,.3f),Is.Zero);
            }
            finally{Object.DestroyImmediate(profile);}
        }
        [Test] public void PhysicalProfileRequiresACompleteReviewedGroundingGrid()
        {
            var source=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCrouchProfile>(AnimationPlaygroundMobilityAuthoring.CrouchProfilePath);
            Assert.That(source,Is.Not.Null);var profile=Object.Instantiate(source);
            try
            {
                profile.contactsQualified=true;profile.groundingQualified=true;
                foreach(var entry in profile.entries){entry.rightContacts=new[]{0f,.5f};entry.leftContacts=new[]{.25f,.75f};}
                profile.grounding=AnimationPlaygroundCrouchPoseQualification.ZeroGrounding();
                Assert.That(profile.Validate(out var error),Is.True,error);
                profile.groundingQualified=false;Assert.That(profile.Validate(out _),Is.False);
                profile.groundingQualified=true;profile.grounding[2].directionIndex=5;Assert.That(profile.Validate(out _),Is.False);
                profile.grounding=AnimationPlaygroundCrouchPoseQualification.ZeroGrounding();
                profile.grounding[1].correction=AnimationCurve.Linear(0,.15f,1,.15f);Assert.That(profile.Validate(out _),Is.False);
            }
            finally{Object.DestroyImmediate(profile);}
        }
    }
}
