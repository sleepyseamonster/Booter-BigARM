using System;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundCrouchQualificationTests
    {
        [Serializable] private sealed class Plan { public PlanItem[] assets; }
        [Serializable] private sealed class PlanItem { public string destination,source_sha256; }
        [Test] public void QualificationRequiresAllCanonicalMotionPayloadsOnDisk()
        {
            var plan=JsonUtility.FromJson<Plan>(System.IO.File.ReadAllText(AnimationPlaygroundAssetImporter.TransferPlanPath));
            var count=0;
            foreach(var item in plan.assets)
            {
                if(item.destination==null || !item.destination.StartsWith("Assets/_Project/Art/Animations/Mixamo/",StringComparison.Ordinal))continue;
                Assert.That(System.IO.File.Exists(item.destination),Is.True,"Missing payload: "+item.destination);
                Assert.That(System.IO.File.Exists(item.destination+".meta"),Is.True,"Missing paired metadata: "+item.destination);
                Assert.That(AnimationPlaygroundLayoutAuthoring.Hash(item.destination),Is.EqualTo(item.source_sha256),item.destination);count++;
            }
            Assert.That(count,Is.EqualTo(71));
        }
        [Test] public void CyclicCrouchContactsRejectDuplicatesNonFiniteAndWrongOrder()
        {
            Assert.That(AnimationPlaygroundCrouchProfile.OrderedContacts(new[]{0f,.5f},new[]{.25f,.75f}),Is.True);
            Assert.That(AnimationPlaygroundCrouchProfile.OrderedContacts(new[]{0f,.5f},new[]{.5f,.75f}),Is.False);
            Assert.That(AnimationPlaygroundCrouchProfile.OrderedContacts(new[]{0f,float.NaN},new[]{.25f,.75f}),Is.False);
            Assert.That(AnimationPlaygroundCrouchProfile.OrderedContacts(new[]{0f},new[]{.25f,.75f}),Is.False);
        }
        [Test,Category("PrivateAnimationQualification")] public void HydratedDraftHasSevenHumanoidRolesButCannotPerformPhysicalActions()
        {
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCrouchProfile>(AnimationPlaygroundMobilityAuthoring.CrouchProfilePath);
            Assert.That(profile,Is.Not.Null,"Restore all private pairs before running this gate.");
            Assert.That(profile.Validate(out var error,false),Is.True,error);
            Assert.That(profile.Validate(out _,true),Is.False);
            Assert.That(profile.contactsQualified,Is.False);
            foreach(AnimationPlaygroundCrouchRole role in Enum.GetValues(typeof(AnimationPlaygroundCrouchRole)))
            {
                var entry=profile.Get(role);Assert.That(entry,Is.Not.Null);Assert.That(entry.clip.isHumanMotion,Is.True);
                for(var i=0;i<=120;i++)
                {
                    var phase=i/120f;Assert.That(entry.Correction(phase),Is.InRange(0,.18001f));
                    Assert.That(entry.capsuleHeight.Evaluate(phase),Is.InRange(profile.crouchHeight-.0001f,profile.standingHeight+.0001f));
                }
            }
            Assert.That(profile.Get(AnimationPlaygroundCrouchRole.Enter).clip.length,Is.EqualTo(1.5f).Within(.001f));
            Assert.That(profile.Get(AnimationPlaygroundCrouchRole.Forward).clip.length,Is.EqualTo(2*1.133333f).Within(.001f));
            Assert.That(profile.Get(AnimationPlaygroundCrouchRole.Backward).clip.length,Is.EqualTo(2*1.233333f).Within(.001f));
            Assert.That(profile.Get(AnimationPlaygroundCrouchRole.Left).clip.length,Is.EqualTo(2*1.166667f).Within(.001f));
            Assert.That(profile.Get(AnimationPlaygroundCrouchRole.Right).clip.length,Is.EqualTo(2*1.266667f).Within(.001f));
        }
        [Test,Category("PrivateAnimationQualification")] public void BrokenShapeCurvesAndMissingRolesCannotPassTheDraftGate()
        {
            var source=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCrouchProfile>(AnimationPlaygroundMobilityAuthoring.CrouchProfilePath);
            Assert.That(source,Is.Not.Null);var clone=Object.Instantiate(source);
            try
            {
                clone.entries[0].capsuleHeight=AnimationCurve.Linear(0,1,1,2);
                Assert.That(clone.Validate(out _,false),Is.False);
                clone.entries=Array.Empty<AnimationPlaygroundCrouchEntry>();Assert.That(clone.Validate(out _,false),Is.False);
            }
            finally{Object.DestroyImmediate(clone);}
        }
        [Test,Category("PrivateAnimationQualification")] public void DirectionalDerivativePreservesRelativeLimbMotion()
        {
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCrouchProfile>(AnimationPlaygroundMobilityAuthoring.CrouchProfilePath);
            var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
            Assert.That(profile,Is.Not.Null);Assert.That(bindings,Is.Not.Null);
            var model=AssetDatabase.GetAssetPath(bindings.targetModel);
            foreach(var role in new[]{AnimationPlaygroundCrouchRole.Forward,AnimationPlaygroundCrouchRole.Backward,AnimationPlaygroundCrouchRole.Left,AnimationPlaygroundCrouchRole.Right})
            {
                var source=AnimationPlaygroundMobilityAuthoring.Paths[(int)role+1];
                var original=AnimationPlaygroundMotionSampler.Measure(model,source,bindings.visualScale,bindings.visualOffset,60);
                var derived=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(profile.Get(role).clip),bindings.visualScale,bindings.visualOffset,60);
                foreach(var frame in original.frames)
                {
                    var before=derived.frames[0];var after=derived.frames[^1];
                    for(var index=1;index<derived.frames.Length;index++)
                        if(derived.frames[index].seconds>=frame.seconds){before=derived.frames[index-1];after=derived.frames[index];break;}
                    var t=Mathf.InverseLerp(before.seconds,after.seconds,frame.seconds);
                    var hips=Vector3.Lerp(before.hips,after.hips,t);
                    Assert.That(Vector3.Distance(frame.leftFoot-frame.hips,Vector3.Lerp(before.leftFoot,after.leftFoot,t)-hips),Is.LessThan(.007f),role+" left foot");
                    Assert.That(Vector3.Distance(frame.rightFoot-frame.hips,Vector3.Lerp(before.rightFoot,after.rightFoot,t)-hips),Is.LessThan(.007f),role+" right foot");
                    Assert.That(Vector3.Distance(frame.head-frame.hips,Vector3.Lerp(before.head,after.head,t)-hips),Is.LessThan(.007f),role+" head");
                }
            }
        }
    }
}
