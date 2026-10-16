using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    // Offline equipped pose qualification on native terrain. No motor ticks,
    // actor input, Play Mode or physics trajectory is executed.
    public sealed class AnimationPlaygroundJumpPresentationTests
    {
        private static readonly System.Collections.Generic.Dictionary<string,string> renderMetrics=new();
        public static string RenderMeasurementReport=>string.Join("\n",renderMetrics.Values);
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static readonly HumanBodyBones[] Joints={HumanBodyBones.Hips,HumanBodyBones.Chest,
            HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,
            HumanBodyBones.LeftHand,HumanBodyBones.RightHand};
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void LiftoffPreservesTheEquippedPoseAtTheSameSourceTimestamp(SkinWeights quality)
        {
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            var driver=f.Inner.Driver;var motor=f.Inner.Motor;var registry=new AnimationPlaygroundResetRegistry();
            var coordinator=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,registry,new[]{motor.Position});
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset");
            var animator=f.Inner.Forward.Get<Animator>("humanoidAnimator");
            var bones=Joints.Select(animator.GetBoneTransform).ToArray();var maximum=0f;
            try
            {
                foreach(var site in new[]{AnimationPlaygroundNativeGroundTests.Sites[0],AnimationPlaygroundNativeGroundTests.Sites[3]})
                foreach(var variant in new[]{profile.standing,profile.running,profile.unarmed})
                {
                    f.Place(site,0);f.Inner.Lock(true);f.Inner.Frame(3,.25,Vector2.up,1);
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="Offline continuous jump pose",clip=variant.continuous,
                        readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(coordinator.TryBeginPhysicalSequence(this,binding,AnimationPlaygroundMotionMode.Jump,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.forward*3,Vector3.forward),null,
                        policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.True);
                    driver.ConfigureJumpPresentation(coordinator,1);driver.SetAnimationPresentationTime(coordinator,variant.liftTime);
                    driver.SetJumpPresentationState(coordinator,1,true,false);
                    f.Inner.Forward.Call("PrepareForwardGraphUpdate");Render(driver,1,0);f.Inner.Forward.Call("LateUpdate");
                    var before=bones.Select(b=>b.position).ToArray();
                    typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,false);
                    motor.SetJumpGroundedLocomotion(coordinator,false);driver.SetJumpPresentationState(coordinator,1,false,false);
                    f.Inner.Forward.Call("PrepareForwardGraphUpdate");Render(driver,1,0);f.Inner.Forward.Call("LateUpdate");
                    var gap=bones.Select((b,i)=>Vector3.Distance(b.position,before[i])).Max();maximum=Mathf.Max(maximum,gap);
                    Assert.That(gap,Is.LessThanOrEqualTo(.002f),$"{variant.sourceId}/{site}/{quality}: support switch moved a joint {gap:R}m at identical pose/time.");
                    Assert.That(f.Inner.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));
                    coordinator.Cancel();
                }
                TestContext.Out.WriteLine($"Equipped same-time liftoff maximum {maximum:R}m.");
            }
            finally{coordinator.DisposeOwnedServices();Object.DestroyImmediate(coordinator);}
        }
        private static void Render(TopDown3DPlayerAnimationDriver driver,float alpha,float seconds)
        {
            try{typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentJumpActionAt",Private).Invoke(driver,new object[]{alpha,seconds});}
            catch(TargetInvocationException error){throw new InvalidOperationException(error.InnerException?.Message,error.InnerException);}
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void PredictedSupportDoesNotBecomeAPoseSnapOrGameplayContactAtTouchdown(SkinWeights quality)
        {
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            var driver=f.Inner.Driver;var motor=f.Inner.Motor;
            var coordinator=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,new AnimationPlaygroundResetRegistry(),new[]{motor.Position});
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset");
            var bones=Joints.Select(f.Inner.Forward.Get<Animator>("humanoidAnimator").GetBoneTransform).ToArray();
            try
            {
                foreach(var site in new[]{AnimationPlaygroundNativeGroundTests.Sites[0],AnimationPlaygroundNativeGroundTests.Sites[3]})
                foreach(var variant in new[]{profile.standing,profile.running,profile.unarmed})
                {
                    f.Place(site,0);f.Inner.Frame(3,.25,Vector2.up,1);
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="Offline predicted landing",clip=variant.continuous,
                        readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(coordinator.TryBeginPhysicalSequence(this,binding,AnimationPlaygroundMotionMode.Jump,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.forward*3,Vector3.forward),null,
                        policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.True);
                    typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,false);motor.SetJumpGroundedLocomotion(coordinator,false);
                    driver.ConfigureJumpPresentation(coordinator,1);driver.SetAnimationPresentationTime(coordinator,variant.contactTime);
                    driver.SetJumpPresentationState(coordinator,1,false,false);
                    Assert.That(motor.TryPredictJumpSupport(coordinator,.65f,out var predicted),Is.True);
                    driver.SetJumpSupportPrediction(coordinator,predicted.collider,new Plane(predicted.normal,predicted.point),1);
                    f.Inner.Forward.Call("PrepareForwardGraphUpdate");Render(driver,1,0);f.Inner.Forward.Call("LateUpdate");
                    var before=bones.Select(b=>b.position).ToArray();
                    Assert.That(motor.JumpObservation.Supported,Is.False);Assert.That(motor.JumpObservation.TouchdownId,Is.Zero);
                    typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,true);motor.SetJumpGroundedLocomotion(coordinator,true);
                    driver.SetJumpPresentationState(coordinator,1,true,false);
                    f.Inner.Forward.Call("PrepareForwardGraphUpdate");Render(driver,1,0);f.Inner.Forward.Call("LateUpdate");
                    var gap=bones.Select((b,i)=>Vector3.Distance(b.position,before[i])).Max();
                    Assert.That(gap,Is.LessThanOrEqualTo(.002f),$"{variant.sourceId}/{site}/{quality}: predicted/actual support seam {gap:R}m.");
                    coordinator.Cancel();
                }
            }
            finally{coordinator.DisposeOwnedServices();Object.DestroyImmediate(coordinator);}
        }
        [TestCase(SkinWeights.OneBone,30)] [TestCase(SkinWeights.OneBone,60)] [TestCase(SkinWeights.OneBone,120)]
        [TestCase(SkinWeights.TwoBones,30)] [TestCase(SkinWeights.TwoBones,60)] [TestCase(SkinWeights.TwoBones,120)]
        [TestCase(SkinWeights.FourBones,30)] [TestCase(SkinWeights.FourBones,60)] [TestCase(SkinWeights.FourBones,120)]
        public void EquippedRenderedJumpModelKeepsOneOutputAndNativeTerrainClearance(SkinWeights quality,int fps)
        {
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            var driver=f.Inner.Driver;var motor=f.Inner.Motor;
            var coordinator=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,new AnimationPlaygroundResetRegistry(),new[]{motor.Position});
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset");
            var mesh=new Mesh();var frames=0;var minimum=float.PositiveInfinity;var maximumJointSpeed=0f;var peak="";
            var animator=f.Inner.Forward.Get<Animator>("humanoidAnimator");var bones=Joints.Select(animator.GetBoneTransform).ToArray();
            var half=4.6f/9.81f;var contact=.08f+2*half;var end=contact+.24f;var step=Time.fixedDeltaTime;
            var visual=f.Inner.Forward.Get<GameObject>("visualInstance");
            void Set(string name,object value)=>typeof(TopDown3DPlayerAnimationDriver).GetField(name,Private).SetValue(driver,value);
            float Height(double t){var flight=Math.Max(0,t-.08);return t<contact ? Mathf.Max(0,(float)(4.6*flight-4.905*flight*flight)) : 0;}
            float Source(AnimationPlaygroundJumpVariant variant,double t)=>t<.08 ? variant.liftTime*(float)(t/.08)
                : t<contact ? AnimationPlaygroundJumpTiming.FlightSourceSeconds(variant,(float)((t-.08)/(2*half)),half)
                : Mathf.Min(variant.continuous.length,variant.contactTime+(float)(t-contact));
            float Weight(double t)=>t<contact ? Mathf.SmoothStep(0,1,(float)t/AnimationPlaygroundJumpTiming.EntryBlendSeconds)
                : 1-Mathf.SmoothStep(0,1,(float)(t-contact)/AnimationPlaygroundJumpTiming.SoftLandingSeconds);
            try
            {
                foreach(var site in new[]{AnimationPlaygroundNativeGroundTests.Sites[0],AnimationPlaygroundNativeGroundTests.Sites[3]})
                for(var scenario=0;scenario<5;scenario++)
                {
                    var variant=scenario==1 ? profile.running : scenario==4 ? profile.unarmed : profile.standing;
                    var speed=scenario==1 ? 6 : scenario==2 ? 5 : scenario==3 ? 4 : 0;
                    var direction=scenario==2 ? Vector2.right : scenario==3 ? Vector2.down : Vector2.up;
                    f.Place(site,0);f.Inner.Lock(scenario==2 || scenario==3);f.Inner.Frame(speed,.25,direction,1);
                    var origin=f.Inner.Forward.Player.position;
                    var support=motor.LaboratorySupportCollider;motor.TryGetLaboratorySupportPlane(out var plane,true);
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="Offline rendered jump model",clip=variant.continuous,
                        readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(coordinator.TryBeginPhysicalSequence(this,binding,AnimationPlaygroundMotionMode.Jump,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.forward*speed,Vector3.forward),null,
                        policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.True);
                    driver.ConfigureJumpPresentation(coordinator,0,variant.liftTime);Vector3[] previous=null;var previousHeight=0f;
                    for(var frame=0;frame<=Mathf.CeilToInt(end*fps);frame++)
                    {
                        var now=frame/(double)fps;var current=Math.Floor(now/step)*step;var before=Math.Max(0,current-step);
                        var alpha=Mathf.Clamp01((float)((now-current)/step));
                        var grounded=current<.08 || current>=contact;
                        var height=Mathf.Lerp(Height(before),Height(current),alpha);
                        f.Inner.Forward.Player.position=origin+Vector3.up*height;
                        f.Inner.Forward.Actor.transform.position=origin+Vector3.up*height;
                        typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,grounded);motor.SetJumpGroundedLocomotion(coordinator,grounded);
                        Set("jumpPoseSampleTime",double.NegativeInfinity);Set("jumpWeightSampleTime",double.NegativeInfinity);
                        driver.SetJumpPresentationState(coordinator,Weight(current),grounded,false);
                        driver.SetAnimationPresentationTime(coordinator,Source(variant,current));
                        Set("jumpPreviousSourceTime",(double)Source(variant,before));Set("jumpPreviousWeight",Weight(before));
                        if(!grounded && current>=.08+half)
                        {
                            Set("jumpPredictionSampleTime",double.NegativeInfinity);
                            driver.SetJumpSupportPrediction(coordinator,support,plane,1-Mathf.SmoothStep(0,1,(float)(contact-current)/.08f));
                            Set("jumpPredictionPreviousBlend",1-Mathf.SmoothStep(0,1,(float)(contact-before)/.08f));
                        }
                        else driver.SetJumpSupportPrediction(coordinator,null,default,0);
                        var clock=f.Inner.Forward.Get<AnimationPlaygroundGaitClock>("forwardClock");
                        var distance=speed*(Math.Min(now,.08)+Math.Max(0,now-contact));
                        var cycles=f.Inner.Forward.Policy.AdvanceCycles(speed,.25,distance);
                        typeof(AnimationPlaygroundGaitClock).GetProperty("Cycles").SetValue(clock,cycles);
                        typeof(AnimationPlaygroundGaitClock).GetField("previousCycles",Private).SetValue(clock,cycles);
                        Set("forwardSpeed",(float)speed);Set("forwardPreviousSpeed",(float)speed);Set("directionalLocal",direction);
                        f.Inner.Forward.Call("PrepareForwardGraphUpdate");Render(driver,alpha,1f/fps);f.Inner.Forward.Call("LateUpdate");frames++;
                        Assert.That(f.Inner.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));
                        Assert.That(float.IsFinite(visual.transform.position.y),Is.True);
                        var pose=bones.Select(b=>b.position).ToArray();
                        if(previous!=null)
                            for(var i=0;i<pose.Length;i++)
                            {
                                var velocity=(pose[i]-previous[i]-Vector3.up*(height-previousHeight)).magnitude*fps;
                                if(velocity>maximumJointSpeed){maximumJointSpeed=velocity;peak=$"{variant.sourceId}/{direction}/{site}/{Joints[i]} at {now:R}s";}
                            }
                        previous=pose;previousHeight=height;
                        if(frame%8==0 || Math.Abs(now-.08)<1f/fps || Math.Abs(now-contact)<1f/fps)
                        {
                            var gap=f.MeshMinimum(mesh);minimum=Mathf.Min(minimum,gap);
                            Assert.That(gap,Is.GreaterThanOrEqualTo(-.002f),$"{quality}/{fps}/{variant.sourceId}/{direction}/{site}/t={now:R}: {gap:R}m at {f.MinimumSkin}/{f.MinimumPoint}.");
                        }
                    }
                    coordinator.Cancel();
                }
                var report=$"{quality}/{fps}: {frames} equipped poses, minimum collision gap {minimum:R}m; maximum source-relative joint speed {maximumJointSpeed:R}m/s ({peak}). Analytic root and supplied pose history, no physics trajectory.";
                renderMetrics[$"{quality}/{fps}"]=report;
                TestContext.Out.WriteLine(report);
            }
            finally{coordinator.DisposeOwnedServices();Object.DestroyImmediate(coordinator);Object.DestroyImmediate(mesh);}
        }
    }
}
