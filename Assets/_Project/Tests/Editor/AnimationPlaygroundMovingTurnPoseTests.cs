using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    [Category("PrivateAnimationQualification")]
    public sealed class AnimationPlaygroundMovingTurnPoseTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        const string Forward="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset";
        static AnimationPlaygroundMovingTurnProfile Pair(AnimationPlaygroundForwardLocomotionProfile policy)
        {
            var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
            var source=bindings!=null?bindings.movingTurns:null;Assert.That(source,Is.Not.Null,"Restore the selected source-paired moving-turn profile.");
            var p=Object.Instantiate(source);p.forwardProfile=policy;Assert.That(p.Validate(policy,out var reason),Is.True,reason);return p;
        }
        static void Set(TopDown3DPlayerAnimationDriver d,string n,object value)=>typeof(TopDown3DPlayerAnimationDriver).GetField(n,Private).SetValue(d,value);
        static object Call(TopDown3DPlayerAnimationDriver d,string n,params object[] args)=>typeof(TopDown3DPlayerAnimationDriver).GetMethod(n,Private).Invoke(d,args);
        static void Frame(AnimationPlaygroundForwardReviewTests.Fixture f,float speed,double phase,float curvature)
        {
            f.Call("PrepareForwardGraphUpdate");Set(f.Driver,"movingTurnPoseCurvature",curvature);Set(f.Driver,"forwardSpeed",speed);
            Call(f.Driver,"PresentForwardFrame",speed,phase,.2f);f.Call("LateUpdate");
        }
        [Test] public void ExplicitConfigurationRetainsOneOutputAndTheSixAcceptedInputs()
        {
            using var f=new AnimationPlaygroundForwardReviewTests.Fixture(false,SkinWeights.FourBones,Forward);var p=Pair(f.Policy);
            try
            {
                f.Driver.ConfigureLaboratoryForwardReview(f.Policy,f.Station);f.Driver.ConfigureLaboratoryMovingTurns(p);f.Call("Awake");
                Assert.That(f.Driver.LaboratoryMovingTurnsConfigured,Is.True);var g=f.Get<PlayableGraph>("playableGraph");
                Assert.That(g.GetOutputCount(),Is.EqualTo(1));Assert.That(f.Get<AnimationMixerPlayable>("forwardReviewMixer").GetInputCount(),Is.EqualTo(6));
                Assert.Throws<InvalidOperationException>(()=>f.Driver.ConfigureLaboratoryMovingTurns(p));
            }
            finally{Object.DestroyImmediate(p);}
        }
        [TestCase(1.4f)] [TestCase(3f)] [TestCase(4f)] [TestCase(4.9f)] [TestCase(6.15f)]
        public void ZeroCurvaturePreservesEveryStraightTimestampAndWeightExactly(float speed)
        {
            using var f=new AnimationPlaygroundForwardReviewTests.Fixture(true,SkinWeights.FourBones,Forward);var p=Pair(f.Policy);
            try
            {
                Set(f.Driver,"movingTurnProfile",p);Call(f.Driver,"InitializeMovingTurnInputs");Frame(f,speed,.317,0);
                var mix=f.Get<AnimationMixerPlayable>("forwardReviewMixer");var expected=f.Policy.PoseWeights(speed);
                for(var i=0;i<6;i++)Assert.That(mix.GetInputWeight(i),Is.EqualTo(expected[i]).Within(.000001));
                for(var i=0;i<5;i++)Assert.That(((AnimationClipPlayable)mix.GetInput(i+1)).GetTime(),Is.EqualTo(f.Policy.gaits[i].SourceSeconds(.317)).Within(.000001));

                Assert.That(f.Driver.LaboratoryMovingTurnWeight,Is.Zero);
            }
            finally{Object.DestroyImmediate(p);}
        }
        [TestCase(-30f)] [TestCase(30f)]
        public void TurnSignSelectsTheMeasuredSideWithoutChangingContactsOrClock(float curvature)
        {
            using var f=new AnimationPlaygroundForwardReviewTests.Fixture(true,SkinWeights.FourBones,Forward);var p=Pair(f.Policy);
            try
            {
                Set(f.Driver,"movingTurnProfile",p);Call(f.Driver,"InitializeMovingTurnInputs");var before=f.Driver.LaboratoryForwardContacts;
                Frame(f,4,.32,curvature);var mix=f.Get<AnimationMixerPlayable>("forwardReviewMixer");var total=0f;
                for(var i=0;i<6;i++)total+=mix.GetInputWeight(i);
                Assert.That(total,Is.EqualTo(1).Within(.000001));Assert.That(f.Driver.LaboratoryMovingTurnWeight,Is.GreaterThan(.1f).And.LessThanOrEqualTo(p.bands[2].maximumWeight));
                Assert.That(f.Get<bool>("movingTurnRight"),Is.EqualTo(curvature>0));
                Assert.That(mix.GetInputWeight(3),Is.EqualTo(1).Within(.000001));
                Assert.That(f.Driver.LaboratoryForwardContacts,Is.EqualTo(before));Assert.That(f.Driver.LaboratoryRenderedCycles,Is.EqualTo(.32));
                typeof(TopDown3DPlayerMotor).GetField("standingStrafeLocked",Private).SetValue(f.Motor,true);Frame(f,4,.32,curvature);
                Assert.That(f.Driver.LaboratoryMovingTurnWeight,Is.Zero,"LT keeps directional ownership.");
            }
            finally{Object.DestroyImmediate(p);}
        }
        [Test] public void ArcActionSnapshotsKeepTheAcceptedDistanceClockSources()
        {
            using var f=new AnimationPlaygroundForwardReviewTests.Fixture(true,SkinWeights.FourBones,Forward);var p=Pair(f.Policy);var owner=new object();
            try
            {
                Set(f.Driver,"movingTurnProfile",p);Call(f.Driver,"InitializeMovingTurnInputs");Frame(f,4,.31,40);
                Assert.That(f.Driver.TryCapturePoseBlend(owner,.1f,true),Is.True);
                var clips=f.Get<System.Collections.Generic.List<AnimationClipPlayable>>("poseBlendClips");Assert.That(clips.Count,Is.GreaterThan(0));
                Assert.That(clips.All(c=>f.Policy.gaits.Any(g=>g.sourceClip==c.GetAnimationClip())),Is.True);
                Assert.That(f.Get<Quaternion[]>("movingTurnSnapshotDeltas"),Is.Not.Null);
                Assert.That(f.Driver.EndPoseBlend(owner),Is.True);
            }
            finally{Object.DestroyImmediate(p);}
        }
        [TestCase(1.4f)] [TestCase(3f)] [TestCase(4f)] [TestCase(4.9f)] [TestCase(6.15f)]
        public void TurnTorsoPreservesAcceptedHipsLegsAndFeetExactly(float speed)
        {
            using var f=new AnimationPlaygroundForwardReviewTests.Fixture(true,SkinWeights.FourBones,Forward);var p=Pair(f.Policy);
            try
            {
                Set(f.Driver,"movingTurnProfile",p);Call(f.Driver,"InitializeMovingTurnInputs");
                var animator=f.Driver.HumanoidAnimator;var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftToes,HumanBodyBones.RightToes}.Select(animator.GetBoneTransform).ToArray();
                for(var phase=0;phase<8;phase++)foreach(var curvature in new[]{-40f,40f})
                {
                    Frame(f,speed,phase/8d,0);var positions=bones.Select(b=>b.position).ToArray();var rotations=bones.Select(b=>b.rotation).ToArray();
                    Frame(f,speed,phase/8d,curvature);Assert.That(f.Driver.LaboratoryMovingTurnWeight,Is.GreaterThan(.1f));
                    for(var bone=0;bone<bones.Length;bone++){Assert.That(Vector3.Distance(positions[bone],bones[bone].position),Is.LessThan(.00001f));Assert.That(Quaternion.Angle(rotations[bone],bones[bone].rotation),Is.LessThan(.01f));}
                    var torso=f.Get<Transform[]>("movingTurnBones");var before=torso.Select(b=>b.localRotation).ToArray();f.Call("LateUpdate");
                    for(var bone=0;bone<torso.Length;bone++)Assert.That(Quaternion.Angle(before[bone],torso[bone].localRotation),Is.LessThan(.01f),"Repeated LateUpdate must not accumulate the overlay.");
                }
            }
            finally{Object.DestroyImmediate(p);}
        }
        [Test] public void ActionSnapshotCarriesAndReleasesTheDisplayedTurnDelta()
        {
            using var f=new AnimationPlaygroundForwardReviewTests.Fixture(true,SkinWeights.FourBones,Forward);var p=Pair(f.Policy);var owner=new object();
            try
            {
                Set(f.Driver,"movingTurnProfile",p);Call(f.Driver,"InitializeMovingTurnInputs");Frame(f,4,.31,40);
                var torso=f.Get<Transform[]>("movingTurnBones");var shown=torso.Select(b=>b.localRotation).ToArray();
                Assert.That(f.Driver.TryCapturePoseBlend(owner,.1f),Is.True);Assert.That(f.Get<Quaternion[]>("movingTurnSnapshotDeltas"),Is.Not.Null);
                Call(f.Driver,"AdvanceForwardMotion",new AnimationPlaygroundGroundedMotion(2,2,Time.fixedTimeAsDouble+.02,.02,Vector3.zero,Vector3.up,true,true));
                Assert.That(f.Get<Quaternion[]>("movingTurnSnapshotDeltas"),Is.Not.Null,"Action ownership rebases must retain their captured turn delta.");
                Frame(f,4,.31,0);
                for(var bone=0;bone<torso.Length;bone++)Assert.That(Quaternion.Angle(shown[bone],torso[bone].localRotation),Is.LessThan(.01f));
                Assert.That(f.Driver.SetPoseBlendElapsed(owner,.1),Is.True);Frame(f,4,.31,0);Assert.That(f.Get<bool>("movingTurnLastHasDelta"),Is.False);
                Assert.That(f.Driver.EndPoseBlend(owner),Is.True);Assert.That(f.Get<Quaternion[]>("movingTurnSnapshotDeltas"),Is.Null);
                Assert.That(f.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));
            }
            finally{Object.DestroyImmediate(p);}
        }
        [Test] public void DisableAndForeignCampPresentationClearTheResidualTurn()
        {
            using var f=new AnimationPlaygroundForwardReviewTests.Fixture(true,SkinWeights.FourBones,Forward);var p=Pair(f.Policy);
            try
            {
                Set(f.Driver,"movingTurnProfile",p);Call(f.Driver,"InitializeMovingTurnInputs");Frame(f,4,.31,40);Assert.That(f.Get<bool>("movingTurnLastHasDelta"),Is.True);
                Set(f.Driver,"campIndex",17);Frame(f,0,.31,0);Assert.That(f.Get<bool>("movingTurnLastHasDelta"),Is.False);
                Set(f.Driver,"campIndex",-1);Frame(f,4,.31,40);Assert.That(f.Get<bool>("movingTurnLastHasDelta"),Is.True);
                f.Call("UnbindForwardMotion");Assert.That(f.Get<bool>("movingTurnLastHasDelta"),Is.False);Assert.That(f.Get<Quaternion[]>("movingTurnSnapshotDeltas"),Is.Null);
            }
            finally{Object.DestroyImmediate(p);}
        }
        [TestCase(1.4f)] [TestCase(3f)] [TestCase(4f)] [TestCase(4.9f)] [TestCase(6.15f)]
        public void UpperBodyTurnsLeanIntoTheRequestedArc(float speed)
        {
            using var f=new AnimationPlaygroundForwardReviewTests.Fixture(true,SkinWeights.FourBones,Forward);var p=Pair(f.Policy);
            try
            {
                Set(f.Driver,"movingTurnProfile",p);Call(f.Driver,"InitializeMovingTurnInputs");
                var head=f.Driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.Head);var hips=f.Driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.Hips);
                foreach(var sign in new[]{-1f,1f})
                {
                    var total=0f;
                    for(var phase=0;phase<16;phase++)
                    {
                        Call(f.Driver,"ClearMovingTurnDeltas");Frame(f,speed,phase/16d,0);var baseline=f.Actor.transform.InverseTransformDirection(head.position-hips.position).x;
                        Frame(f,speed,phase/16d,sign*40);total+=(f.Actor.transform.InverseTransformDirection(head.position-hips.position).x-baseline)*sign;
                    }
                    Assert.That(total/16,Is.GreaterThan(.0005f),$"{speed}/{sign}: turn lean must follow the path.");
                }
            }
            finally{Object.DestroyImmediate(p);}
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void FinalArcBlendsRetainNativeSolesEquipmentAndOneOutput(SkinWeights quality)
        {
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);var driver=f.Inner.Driver;var inner=f.Inner.Forward;var p=Pair(inner.Policy);var mesh=new Mesh();
            try
            {
                // The existing native fixture constructs its graph first. Add the
                // same validated optional inputs for offline pose qualification.
                Set(driver,"movingTurnProfile",p);Call(driver,"InitializeMovingTurnInputs");f.Inner.Lock(false);
                var minSole=float.PositiveInfinity;var minMesh=float.PositiveInfinity;var count=0;
                foreach(var site in new[]{AnimationPlaygroundNativeGroundTests.Sites[0],AnimationPlaygroundNativeGroundTests.Sites[2],AnimationPlaygroundNativeGroundTests.Sites[3]})
                foreach(var facing in new[]{0f,90f})foreach(var speed in new[]{1.4f,2.3f,3,3.5f,4,4.45f,4.9f,5.5f,6.15f})
                foreach(var curvature in new[]{-40f,0,40f})for(var phase=0;phase<8;phase++)
                {
                    f.Place(site,facing);Frame(inner,speed,phase/8d,curvature);count++;
                    Assert.That(driver.LaboratoryNativeGroundAdapted,Is.True,$"{site}/{speed}/{curvature}/{phase}: {driver.LaboratoryNativeGroundFailure}");
                    Assert.That(driver.GarmentFloorExceeded,Is.False);Assert.That(driver.LaboratoryForwardLift,Is.InRange(-.03001f,.08001f));
                    Assert.That(inner.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));minSole=Mathf.Min(minSole,f.SoleMinimum());
                    if(phase==0 && curvature!=0)minMesh=Mathf.Min(minMesh,f.MeshMinimum(mesh));
                }
                TestContext.Out.WriteLine($"Arc native poses {count}; sole {minSole:R}; equipment {minMesh:R}.");
                Assert.That(minSole,Is.GreaterThanOrEqualTo(-.002f));Assert.That(minMesh,Is.GreaterThanOrEqualTo(-.002f));
            }
            finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(p);}
        }
    }
}
