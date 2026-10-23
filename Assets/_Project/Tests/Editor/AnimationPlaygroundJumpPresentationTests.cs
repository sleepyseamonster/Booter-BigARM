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
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void PredictedUnarmedLandingConvergesAtRecordedModerateSlopeSeam(SkinWeights quality)
        {
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            var driver=f.Inner.Driver;var motor=f.Inner.Motor;
            var coordinator=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,new AnimationPlaygroundResetRegistry(),new[]{motor.Position});
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset");
            var continuation=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(AnimationPlaygroundDirectionalTests.NativePath);
            var body=continuation.nativeBodyProjection.Bind(f.Inner.Forward.Get<GameObject>("visualInstance").transform);
            var height=(Func<Vector3,float?>)typeof(TopDown3DPlayerAnimationDriver).GetField("nativeGarmentHeight",Private).GetValue(driver);
            var mesh=new Mesh();
            try
            {
                // Recorded PlayerLoop near-contact pose plus a small spatial/time
                // neighborhood. Six body/foot passes discarded an otherwise
                // bounded correction and left the displayed body 99 mm below terrain.
                foreach(var dx in new[]{-.02f,0,.02f})foreach(var dy in new[]{-.02f,0,.02f})
                foreach(var source in new[]{.6987f,.7070324f,.7133f})
                {
                    f.Place(new Vector2(-1607.68762f+dx,-1660.36047f),40);f.Inner.Lock(true);
                    f.Inner.Frame(4.07015228f,.25,new Vector2(-.7071068f,-.7071068f),1);
                    var position=new Vector3(-1607.68762f+dx,-47.52311f+dy,-1660.36047f);
                    f.Inner.Forward.Actor.transform.position=position;f.Inner.Forward.Player.position=position;Physics.SyncTransforms();
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="Recorded unarmed slope seam",clip=profile.unarmed.continuous,
                        readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(coordinator.TryBeginPhysicalSequence(this,binding,AnimationPlaygroundMotionMode.Jump,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null,
                        policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.True);
                    Assert.That(driver.ConfigureJumpPresentation(coordinator,1),Is.True);
                    typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,false);motor.SetJumpGroundedLocomotion(coordinator,false);
                    driver.SetJumpPresentationState(coordinator,1,false,false);driver.SetAnimationPresentationTime(coordinator,source);
                    Assert.That(motor.TryPredictJumpSupport(coordinator,.65f,out var predicted),Is.True);
                    driver.SetJumpSupportPrediction(coordinator,predicted.collider,new Plane(predicted.normal,predicted.point),.9344983f);
                    f.Inner.Forward.Call("PrepareForwardGraphUpdate");Render(driver,1,0);f.Inner.Forward.Call("LateUpdate");
                    Assert.That(driver.LaboratoryNativeGroundFailure,Is.Null,$"{quality}/{dx}/{dy}/{source}");
                    Assert.That(body.MinimumTerrainDistance(height,out _),Is.GreaterThanOrEqualTo(-.002f));
                    Assert.That(f.MeshMinimum(mesh),Is.GreaterThanOrEqualTo(-.002f),f.MinimumSkin);
                    Assert.That(driver.LaboratoryNativeBodyLift,Is.InRange(0f,.18f));
                    Assert.That(motor.IsGrounded,Is.False);Assert.That(motor.Position,Is.EqualTo(position));
                    Assert.That(motor.JumpObservation.TouchdownId,Is.Zero);
                    Assert.That(f.Inner.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));
                    coordinator.Cancel();
                }
            }
            finally{coordinator.DisposeOwnedServices();Object.DestroyImmediate(coordinator);Object.DestroyImmediate(mesh);}
        }

        private static void Render(TopDown3DPlayerAnimationDriver driver,float alpha,float seconds)
        {
            try{typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentJumpActionAt",Private).Invoke(driver,new object[]{alpha,seconds});}
            catch(TargetInvocationException error){throw new InvalidOperationException(error.InnerException?.Message,error.InnerException);}
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void LandingHandoffPreservesFadedDirectionalPoseOnFlatSupport(SkinWeights quality)
        {
            using var f=new AnimationPlaygroundDirectionalTests.Fixture(quality);f.Flat();f.Lock(true);
            var driver=f.Driver;var motor=f.Motor;
            var coordinator=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,new AnimationPlaygroundResetRegistry(),new[]{motor.Position});
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset");
            var bones=Joints.Select(f.Forward.Get<Animator>("humanoidAnimator").GetBoneTransform).ToArray();
            try
            {
                foreach(var angle in new[]{135f,225f})for(var phase=0;phase<16;phase++)
                {
                    f.Flat(); // Reinstall the completed support cleared by the previous sequence's release.
                    var direction=new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad));
                    var clock=f.Forward.Get<AnimationPlaygroundGaitClock>("forwardClock");clock.RebasePhase(phase/16f);
                    f.Frame(4.5f,clock.Cycles,direction,1);
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="Faded directional landing",clip=profile.standing.continuous,
                        readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(coordinator.TryBeginPhysicalSequence(this,binding,AnimationPlaygroundMotionMode.Recovery,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.forward*4.5f,Vector3.forward),null,
                        policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.True);
                    driver.ConfigureJumpPresentation(coordinator,0);driver.SetJumpPresentationState(coordinator,0,true,false);
                    driver.SetAnimationPresentationTime(coordinator,profile.standing.contactTime+.24f);
                    typeof(TopDown3DPlayerAnimationDriver).GetField("forwardSpeed",Private).SetValue(driver,4.5f);
                    typeof(TopDown3DPlayerAnimationDriver).GetField("forwardPreviousSpeed",Private).SetValue(driver,4.5f);
                    f.Forward.Call("PrepareForwardGraphUpdate");Render(driver,1,0);f.Forward.Call("LateUpdate");
                    var before=bones.Select(b=>b.position).ToArray();coordinator.Cancel();
                    f.Frame(4.5f,clock.Cycles,direction,0);
                    var gap=bones.Select((b,i)=>Vector3.Distance(b.position,before[i])).Max();
                    Assert.That(gap,Is.LessThanOrEqualTo(.002f),$"{angle}/{phase}/{quality}: releasing a fully faded action moved a joint {gap:R}m.");
                }
            }
            finally{coordinator.DisposeOwnedServices();Object.DestroyImmediate(coordinator);}
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void LandingHandoffPredictionKeepsEquippedClearanceAtSolverBoundary(SkinWeights quality)
        {
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            var driver=f.Inner.Driver;var motor=f.Inner.Motor;
            var coordinator=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,new AnimationPlaygroundResetRegistry(),new[]{motor.Position});
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset");
            var continuation=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(AnimationPlaygroundDirectionalTests.NativePath);
            var body=continuation.nativeBodyProjection.Bind(f.Inner.Forward.Get<GameObject>("visualInstance").transform);
            var height=(Func<Vector3,float?>)typeof(TopDown3DPlayerAnimationDriver).GetField("nativeGarmentHeight",Private).GetValue(driver);
            try
            {
                foreach(var takeoff in new[]{false,true})
                {
                    f.Place(new Vector2(-1636,-1516),175);f.Inner.Frame(takeoff?1.4f:0,.25,Vector2.up,1);
                    motor.TryGetLaboratorySupportPlane(out var plane,true);var support=motor.LaboratorySupportCollider;
                    // The real PlayerLoop checkpoint includes one interpolated
                    // frame below rest while the solver resolves touchdown.
                    var position=motor.Position+Vector3.down*(takeoff?0:.05f);
                    f.Inner.Forward.Actor.transform.position=position;f.Inner.Forward.Player.position=position;
                    var variant=takeoff?profile.running:profile.standing;
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="Predicted clearance seam",clip=variant.continuous,
                        readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(coordinator.TryBeginPhysicalSequence(this,binding,AnimationPlaygroundMotionMode.Jump,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null,
                        policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.True);
                    typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,false);
                    driver.ConfigureJumpPresentation(coordinator,takeoff?.74f:1);
                    driver.SetJumpPresentationState(coordinator,takeoff?.74f:1,false,false);
                    driver.SetAnimationPresentationTime(coordinator,takeoff?variant.liftTime:variant.contactTime-.01f);
                    driver.SetJumpSupportPrediction(coordinator,support,plane,takeoff?1:.94f);
                    f.Inner.Forward.Get<AnimationPlaygroundGaitClock>("forwardClock").RebasePhase(.25f);
                    typeof(TopDown3DPlayerAnimationDriver).GetField("forwardSpeed",Private).SetValue(driver,takeoff?1.4f:0);
                    typeof(TopDown3DPlayerAnimationDriver).GetField("forwardPreviousSpeed",Private).SetValue(driver,takeoff?1.4f:0);
                    if(takeoff)
                    {
                        typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,true);
                        driver.ConfigureJumpPresentation(coordinator,.74f,variant.liftTime);
                        driver.SetJumpPresentationState(coordinator,.74f,true,false);
                        driver.SetAnimationPresentationTime(coordinator,variant.liftTime-.02f);
                        f.Inner.Forward.Call("PrepareForwardGraphUpdate");Render(driver,1,.06f);f.Inner.Forward.Call("LateUpdate");
                        Assert.That(motor.TryAnimationTakeoff(coordinator,variant.upwardSpeed),Is.True);
                        f.Inner.Forward.Actor.transform.position+=Vector3.down*.014f;
                        driver.SetAnimationPresentationTime(coordinator,variant.liftTime);
                        typeof(TopDown3DPlayerAnimationDriver).GetField("jumpPreviousSourceTime",Private).SetValue(driver,(double)(variant.liftTime-.02f));
                        typeof(TopDown3DPlayerAnimationDriver).GetField("jumpPreviousWeight",Private).SetValue(driver,.5f);
                        f.Inner.Forward.Call("PrepareForwardGraphUpdate");Render(driver,1f/6,1f/60);f.Inner.Forward.Call("LateUpdate");
                    }
                    else{f.Inner.Forward.Call("PrepareForwardGraphUpdate");Render(driver,1,0);f.Inner.Forward.Call("LateUpdate");}
                    Assert.That(driver.LaboratoryNativeGroundFailure,Is.Null,$"takeoff={takeoff}/{quality}");
                    Assert.That(body.MinimumTerrainDistance(height,out _),Is.GreaterThanOrEqualTo(-.002f),$"takeoff={takeoff}/{quality}");
                    Assert.That(driver.LaboratoryNativeBodyLift,Is.LessThanOrEqualTo(.18f));
                    coordinator.Cancel();
                }
            }
            finally{coordinator.DisposeOwnedServices();Object.DestroyImmediate(coordinator);}
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
            var impulse=profile.standing.upwardSpeed;var gravity=-Physics.gravity.y*profile.gravityScale;
            var half=impulse/gravity;var contact=.08f+2*half;var end=contact+.24f;var step=Time.fixedDeltaTime;
            var visual=f.Inner.Forward.Get<GameObject>("visualInstance");
            void Set(string name,object value)=>typeof(TopDown3DPlayerAnimationDriver).GetField(name,Private).SetValue(driver,value);
            float Height(double t){var flight=Math.Max(0,t-.08);return t<contact ? Mathf.Max(0,(float)(impulse*flight-.5*gravity*flight*flight)) : 0;}
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
