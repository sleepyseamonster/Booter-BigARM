using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundLocomotionTransitionTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static AnimationPlaygroundLocomotionTransitionProfile Profile()
        {
            var p=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundLocomotionTransitionProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/LocomotionStopsTurns.asset");
            Assert.That(p,Is.Not.Null,"Restore the private qualified locomotion transition bundle."); return p;
        }
        [TestCase(1.4f,0f,false,AnimationPlaygroundLocomotionTransition.None)]
        [TestCase(3f,0f,false,AnimationPlaygroundLocomotionTransition.RunStop)]
        [TestCase(4.9f,0f,false,AnimationPlaygroundLocomotionTransition.RunStop)]
        [TestCase(6.15f,0f,false,AnimationPlaygroundLocomotionTransition.SprintStop)]
        [TestCase(6.15f,0f,true,AnimationPlaygroundLocomotionTransition.None)]
        [TestCase(4.9f,90f,false,AnimationPlaygroundLocomotionTransition.None)]
        [TestCase(4.9f,149f,false,AnimationPlaygroundLocomotionTransition.None)]
        [TestCase(4.9f,160f,false,AnimationPlaygroundLocomotionTransition.PivotRight)]
        [TestCase(4.9f,-160f,false,AnimationPlaygroundLocomotionTransition.PivotLeft)]
        public void SelectsOnlyForwardStopsAndDeliberateReversals(float speed,float angle,bool locked,AnimationPlaygroundLocomotionTransition expected)
        {
            var request=angle==0?Vector3.zero:Quaternion.Euler(0,angle,0)*Vector3.forward;
            Assert.That(Profile().Select(Vector3.forward*speed,Vector3.forward,request,locked),Is.EqualTo(expected));
            Assert.That(Profile().Select(Vector3.right*speed,Vector3.forward,request,false),Is.EqualTo(AnimationPlaygroundLocomotionTransition.None));
        }
        [Test] public void SourceTakesKeepTheirCompleteDurationsAndMotorOwnsTranslationAndYaw()
        {
            var p=Profile();Assert.That(p.Validate(p.targetModel,out var reason,false),Is.True,reason);
            var takes=new[]{p.runStop,p.sprintStop,p.pivotLeft,p.pivotRight};var lengths=new[]{1.8f,.9f,.65f,.65f};
            for(var i=0;i<takes.Length;i++)
            {
                var take=takes[i];Assert.That(take.clip.length,Is.EqualTo(lengths[i]).Within(.00001));
                foreach(var b in AnimationUtility.GetCurveBindings(take.clip).Where(b=>b.propertyName=="RootT.x"||b.propertyName=="RootT.z"))
                    Assert.That(AnimationUtility.GetEditorCurve(take.clip,b).keys.All(k=>k.value==0),Is.True);
                var integrated=Vector3.zero;
                for(var k=1;k<=500;k++)integrated+=take.Velocity(take.clip.length*(k-1)/500,take.clip.length*k/500)*take.clip.length/500;
                Assert.That(Vector3.Distance(integrated,take.Position(take.clip.length)),Is.LessThan(.00001f));
            }
            for(var i=0;i<=100;i++)Assert.That(p.pivotLeft.x.Evaluate(.65f*i/100)+p.pivotRight.x.Evaluate(.65f*i/100),Is.EqualTo(0).Within(.0001));
            Assert.That(p.pivotLeft.yaw.Evaluate(.65f),Is.EqualTo(-180).Within(.001));
            Assert.That(p.pivotRight.yaw.Evaluate(.65f),Is.EqualTo(180).Within(.001));
        }
        [Test] public void RejectsNonfiniteCurvesAndUnqualifiedOrMismatchedProfiles()
        {
            var clone=Object.Instantiate(Profile());
            try
            {
                clone.qualified=false;Assert.That(clone.Validate(clone.targetModel,out _),Is.False);
                Assert.That(clone.Validate(null,out _,false),Is.False);
                clone.maximumSpeed=float.NaN;Assert.That(clone.Validate(clone.targetModel,out _,false),Is.False);
                clone.maximumSpeed=6.15f;var keys=clone.runStop.z.keys;keys[1].value=float.NaN;
                clone.runStop.z=new AnimationCurve(keys);Assert.That(clone.Validate(clone.targetModel,out _,false),Is.False);
            }
            finally{Object.DestroyImmediate(clone);}
        }
        [TestCase(6.15f)]
        public void StopCompletesOnceWithoutTeleportingAndNewMovementResumes(float speed)
        {
            using var f=new Fixture(speed);f.Request(Vector2.zero);f.Begin();
            Assert.That(f.Transition.Active,Is.True);
            Assert.That(f.Transition.Kind,Is.EqualTo(speed>5.2f?AnimationPlaygroundLocomotionTransition.SprintStop:AnimationPlaygroundLocomotionTransition.RunStop));
            var origin=f.Base.Body.position;f.Base.Tick(130);
            Assert.That(f.Transition.Active,Is.False);Assert.That(f.Base.Actions.IsBusy,Is.False);Assert.That(f.Base.Motor.IsActionConstrained,Is.False);
            Assert.That(f.Base.Body.position,Is.EqualTo(origin),"EditMode command test does not simulate a trajectory.");
            f.Request(Vector2.up);f.Base.Tick(2);Assert.That(f.Base.Body.linearVelocity.z,Is.GreaterThan(0));
        }
        [TestCase(3f)] [TestCase(4.9f)]
        public void OrdinaryStopsUseImmediateLocomotionWithoutAFullBodyBrakeLease(float speed)
        {
            using var f=new Fixture(speed);f.Request(Vector2.zero);f.Base.Tick(15);
            Assert.That(f.Transition.Active,Is.False);Assert.That(f.Base.Actions.IsBusy,Is.False);
            Assert.That(f.Base.Motor.IsActionConstrained,Is.False);
            Assert.That(Vector3.ProjectOnPlane(f.Base.Body.linearVelocity,Vector3.up).magnitude,Is.LessThan(.1f));
        }
        [TestCase(-160f)] [TestCase(160f)]
        public void PivotCompletesSignedHalfTurnAndKeepsAForwardRunOut(float angle)
        {
            using var f=new Fixture(6.15f);f.Request(new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad)));f.Begin();
            Assert.That(f.Transition.Kind,Is.EqualTo(angle<0?AnimationPlaygroundLocomotionTransition.PivotLeft:AnimationPlaygroundLocomotionTransition.PivotRight));
            var sign=0f;var previous=Vector3.forward;var travelled=Vector3.zero;
            for(var i=0;i<40 && f.Base.Actions.IsBusy;i++)
            {
                f.Base.Tick(1);var facing=f.Base.Motor.FacingDirection;
                sign+=Vector3.SignedAngle(previous,facing,Vector3.up);previous=facing;travelled+=f.Base.Body.linearVelocity*Time.fixedDeltaTime;
                Assert.That(Vector3.ProjectOnPlane(f.Base.Body.linearVelocity,Vector3.up).magnitude,Is.LessThanOrEqualTo(6.151));
            }
            Assert.That(sign,Is.EqualTo(angle).Within(.1));
            Assert.That(f.Base.Body.linearVelocity.z,Is.LessThan(-1));
            Assert.That(travelled.magnitude,Is.LessThan(.4f),"Native pivot brakes, turns and runs back across its entry.");
        }
        [Test] public void ResumingMovementCancelsTheStopWithinTheSameMotorTick()
        {
            using var f=new Fixture(6.15f);f.Request(Vector2.zero);f.Begin();f.Base.Tick(3);
            var before=f.Base.Body.linearVelocity.z;f.Request(Vector2.up);f.Base.Tick(1);
            Assert.That(f.Base.Actions.IsBusy,Is.False);
            Assert.That(f.Base.Body.linearVelocity.z,Is.GreaterThanOrEqualTo(before-.01f),"Cancellation must not write a zero-velocity tick.");
        }
        [Test] public void RollCanTakeTheCapturedStopPoseWithoutADeadInputFrame()
        {
            using var f=new Fixture(6.15f);f.Request(Vector2.zero);f.Begin();f.Base.Tick(5);
            Assert.That(f.Transition.TryYieldTo(f.Base.Dodge),Is.True);
            Assert.That(f.Base.Forward.Get<UnityEngine.Animations.AnimationMixerPlayable>("mixer").GetInputWeight(0),Is.Zero,"Releasing the clip must not inject idle into the reserved handoff.");
            Assert.That(f.Base.Dodge.TryDodge(),Is.True,f.Base.Dodge.LastFailure);
            Assert.That(f.Transition.Active,Is.False);Assert.That(f.Base.Actions.IsBusy,Is.True);
            f.Base.Tick(5);Assert.That(f.Base.Body.linearVelocity.magnitude,Is.GreaterThan(.1f));
        }
        [Test] public void MotorDetectsAStopWithoutPollingTheControllerFromUpdate()
        {
            using var f=new Fixture(6.15f);f.Request(Vector2.zero);f.Base.Tick(2);
            Assert.That(f.Transition.Active,Is.True);Assert.That(f.Base.Actions.IsBusy,Is.True);
        }
        [TestCase(-179f)] [TestCase(179f)]
        public void PivotCancellationReturnsControlWhenTheStickChanges(float angle)
        {
            using var f=new Fixture(6.15f);f.Request(new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad)));f.Begin();f.Base.Tick(4);
            Assert.That(f.Base.Actions.IsBusy,Is.True);f.Request(Vector2.right);f.Base.Tick(1);
            Assert.That(f.Base.Actions.IsBusy,Is.False);Assert.That(f.Base.Motor.IsActionConstrained,Is.False);
        }
        [Test] public void JumpTakesTheStopPoseAndMotorLeaseImmediately()
        {
            using var f=new Fixture(6.15f);f.Request(Vector2.zero);f.Begin();f.Base.Tick(4);
            var air=f.Base.Forward.Actor.AddComponent<AnimationPlaygroundAirborneController>();
            try
            {
                air.Configure(f.Base.Motor,f.Base.Driver,f.Base.Actions,f.Base.Forward.Session.Bindings.airborne,f.Base.Registry,()=>false,()=>false);
                Assert.That(f.Transition.TryYieldTo(air),Is.True);Assert.That(air.QueueJumpRequest(),Is.True);
                Assert.That(air.Active,Is.True);Assert.That(f.Base.Actions.IsBusy,Is.True);f.Base.Tick(1);
                Assert.That(f.Transition.Active,Is.False);
            }
            finally{air.DisposeOwnedServices();}
        }
        [Test] public void CrouchCanTakeTheStopPoseBeforeItSettlesToIdle()
        {
            using var f=new Fixture(6.15f);f.Request(Vector2.zero);f.Begin();f.Base.Tick(4);
            var crouch=f.Base.Forward.Actor.AddComponent<AnimationPlaygroundCrouchController>();
            try
            {
                crouch.Configure(f.Base.Motor,f.Base.Driver,f.Base.Actions,f.Base.Forward.Session.Bindings.crouch,f.Base.Registry,()=>false,()=>false);
                Assert.That(f.Transition.TryYieldTo(crouch),Is.True);Assert.That(crouch.TryToggle(),Is.True,crouch.LastFailure);
                Assert.That(crouch.Active,Is.True);Assert.That(f.Base.Actions.IsBusy,Is.True);f.Base.Tick(2);
                Assert.That(f.Transition.Active,Is.False);
            }
            finally{crouch.DisposeOwnedServices();}
        }
        [TestCase(-179f)] [TestCase(179f)]
        public void FinalPartialPivotTickPreservesTheRequestedRunOut(float angle)
        {
            using var f=new Fixture(6.15f);f.Request(new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad)));f.Begin();
            var previous=6.15f;
            for(var i=0;i<60 && f.Base.Actions.IsBusy;i++)
            {
                f.Base.Tick(1);var speed=Vector3.ProjectOnPlane(f.Base.Body.linearVelocity,Vector3.up).magnitude;
                if(!f.Base.Actions.IsBusy)
                {
                    Assert.That(speed,Is.GreaterThanOrEqualTo(4.8f));
                    Assert.That(speed,Is.GreaterThanOrEqualTo(previous-.8f),"A partial final source interval cannot halve the motor velocity.");
                }
                previous=speed;
            }
            Assert.That(f.Base.Actions.IsBusy,Is.False);
        }
        [Test] public void KeyboardSprintStopUsesTheAbruptTakeWithoutRequiringGamepadTopSpeed()
        {
            var p=Profile();Assert.That(p.Select(Vector3.forward*4.9f,Vector3.forward,Vector3.zero,false,true),Is.EqualTo(AnimationPlaygroundLocomotionTransition.SprintStop));
            Assert.That(p.Select(Vector3.forward*3,Vector3.forward,Vector3.zero,false,true),Is.EqualTo(AnimationPlaygroundLocomotionTransition.RunStop));
        }
        [TestCase(false)] [TestCase(true)]
        public void KeyboardAndGamepadRunOutSelectTheirMatchingExitPhase(bool gamepad)
        {
            using var f=new Fixture(gamepad?6.15f:4.9f);f.Request(Vector2.down);
            typeof(TopDown3DInputRouter).GetProperty("MoveFromGamepad").SetValue(f.Input.Router,gamepad);
            f.Begin();for(var i=0;i<100 && f.Base.Actions.IsBusy;i++)f.Base.Tick(1);
            Assert.That(f.Base.Actions.IsBusy,Is.False);
            var clock=f.Base.Forward.Get<AnimationPlaygroundGaitClock>("forwardClock");
            var expected=Profile().pivotRight.RunOutPhase(gamepad?6.15f:4.9f,Profile().sprintThreshold);
            Assert.That(clock.Cycles-Math.Floor(clock.Cycles),Is.EqualTo(expected).Within(.0001));
        }
        [Test] public void PivotRejectsAnEntryThatExceedsTheRequestedGaitSpeed()
        {
            using var f=new Fixture(6.15f);f.Request(Vector2.down);
            typeof(TopDown3DInputRouter).GetProperty("SprintHeld").SetValue(f.Input.Router,false);
            typeof(AnimationPlaygroundLocomotionTransitionController).GetMethod("ObserveRequest",Hidden).Invoke(f.Transition,null);
            Assert.That(f.Base.Actions.IsBusy,Is.False);Assert.That(f.Transition.Active,Is.False);
            Assert.That(f.Base.Body.linearVelocity.z,Is.EqualTo(6.15f).Within(.001),"Rejected presentation must preserve incoming momentum.");
        }
        [Test] public void RejectedPivotLeavesTheEntireOutgoingGraphUntouched()
        {
            using var f=new Fixture(6.15f);f.Request(Vector2.down);
            typeof(TopDown3DInputRouter).GetProperty("SprintHeld").SetValue(f.Input.Router,false);
            f.Base.Forward.Frame(6.15f,.35);
            var mixer=f.Base.Forward.Get<UnityEngine.Animations.AnimationMixerPlayable>("mixer");
            var weights=Enumerable.Range(0,mixer.GetInputCount()).Select(i=>mixer.GetInputWeight(i)).ToArray();
            Assert.That(weights[0],Is.LessThan(.01f),"Begin with an actual moving pose.");
            var graph=f.Base.Forward.Get<PlayableGraph>("playableGraph");var count=graph.GetPlayableCount();
            for(var i=0;i<3;i++)typeof(AnimationPlaygroundLocomotionTransitionController).GetMethod("ObserveRequest",Hidden).Invoke(f.Transition,null);
            Assert.That(f.Transition.Active,Is.False);Assert.That(f.Base.Actions.IsBusy,Is.False);
            Assert.That(graph.GetPlayableCount(),Is.EqualTo(count));
            Assert.That(Enumerable.Range(0,mixer.GetInputCount()).Select(i=>mixer.GetInputWeight(i)).ToArray(),Is.EqualTo(weights),
                "Reject before acquiring presentation; cancellation must not inject idle for a rendered frame.");
        }
        [Test] public void LoweringTheRequestedGaitSpeedReleasesAnActivePivot()
        {
            using var f=new Fixture(6.15f);f.Request(Vector2.down);f.Begin();f.Base.Tick(3);
            typeof(TopDown3DInputRouter).GetProperty("SprintHeld").SetValue(f.Input.Router,false);f.Base.Tick(1);
            Assert.That(f.Base.Actions.IsBusy,Is.False);Assert.That(f.Base.Motor.IsActionConstrained,Is.False);
            Assert.That(f.Base.Body.linearVelocity.magnitude,Is.GreaterThan(.1f));
        }
        [TestCase(-179f)] [TestCase(179f)]
        public void OrdinaryJogReversalKeepsControlWithResponsiveLocomotion(float angle)
        {
            using var f=new Fixture(3);f.Request(new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad)));
            typeof(TopDown3DInputRouter).GetProperty("SprintHeld").SetValue(f.Input.Router,false);
            typeof(TopDown3DInputRouter).GetProperty("MoveFromGamepad").SetValue(f.Input.Router,false);
            typeof(AnimationPlaygroundLocomotionTransitionController).GetMethod("ObserveRequest",Hidden).Invoke(f.Transition,null);
            Assert.That(f.Base.Actions.IsBusy,Is.False);Assert.That(f.Transition.Active,Is.False);
            Assert.That(f.Base.Motor.IsActionConstrained,Is.False);Assert.That(f.Base.Driver.PoseBlendOwned,Is.False);
        }
        [Test] public void MeasuredRunOutTableCoversTheRangeAndRejectsMissingOrUnorderedData()
        {
            var p=Object.Instantiate(Profile());
            try
            {
                Assert.That(p.schema,Is.EqualTo(2));
                foreach(var take in new[]{p.pivotLeft,p.pivotRight})
                {
                    Assert.That(take.runOutPhases.Length,Is.EqualTo(41));
                    foreach(var sample in take.runOutPhases)Assert.That(take.RunOutPhase(sample.x,p.sprintThreshold),Is.EqualTo(sample.y));
                }
                p.pivotLeft.runOutPhases=null;Assert.That(p.Validate(p.targetModel,out _),Is.False);
                p.pivotLeft.runOutPhases=(Vector2[])Profile().pivotLeft.runOutPhases.Clone();
                p.pivotLeft.runOutPhases[1]=p.pivotLeft.runOutPhases[0];Assert.That(p.Validate(p.targetModel,out _),Is.False);
                p.pivotLeft.runOutPhases=null;p.pivotRight.runOutPhases=null;p.schema=1;
                Assert.That(p.Validate(p.targetModel,out _),Is.True,"Retain the previous saved profile during guarded import.");
                Assert.That(p.pivotRight.RunOutPhase(4.9f,p.sprintThreshold),Is.EqualTo(p.pivotRight.fastRunExitPhase));
            }
            finally{Object.DestroyImmediate(p);}
        }
        [Test] public void RequestedVelocityPreviewRequiresThePostureOwnerAndMatchesTheAcquiredRequest()
        {
            using var f=new Fixture(4.9f);f.Request(Vector2.down);
            Assert.That(f.Base.Motor.GroundedLocomotionRequestVelocity(new object()),Is.EqualTo(Vector3.zero));
            var request=f.Base.Motor.GroundedLocomotionRequestVelocity(f.Base.Actions);
            Assert.That(request.magnitude,Is.EqualTo(6.15f).Within(.001));
            Assert.That(f.Base.Driver.StandingStrafeReviewSupportAvailable(),Is.True,"A request preview must preserve completed support.");f.Begin();
            Assert.That(f.Base.Motor.DodgeRunOutVelocity(f.Base.Actions),Is.EqualTo(request));
        }
        [TestCase(.01f,0f)] [TestCase(.02f,0f)] [TestCase(.0333333f,0f)]
        [TestCase(.01f,-179f)] [TestCase(.02f,-179f)] [TestCase(.0333333f,-179f)]
        [TestCase(.01f,179f)] [TestCase(.02f,179f)] [TestCase(.0333333f,179f)]
        public void CompleteTakesAndBlendDurationsRemainBoundedAtDifferentMotorRates(float step,float angle)
        {
            var original=Time.fixedDeltaTime;Time.fixedDeltaTime=step;
            try
            {
                using var f=new Fixture(6.15f);f.Request(angle==0?Vector2.zero:new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad)));f.Begin();
                var take=Profile().Take(f.Transition.Kind);
                var rate=(float)typeof(AnimationPlaygroundLocomotionTransitionController).GetField("sourceRate",Hidden).GetValue(f.Transition);
                var ticks=0;while(f.Base.Actions.IsBusy && ticks<200){f.Base.Tick(1);ticks++;}
                Assert.That(ticks*step,Is.InRange(take.clip.length/rate-.0001f,take.clip.length/rate+step+.0001f));
                var blendTicks=0;while(f.Transition.Active && blendTicks<40){f.Base.Tick(1);blendTicks++;}
                Assert.That(f.Transition.Active,Is.False);Assert.That(f.Base.Driver.PoseBlendOwned,Is.False);
                Assert.That(blendTicks*step,Is.LessThanOrEqualTo(Profile().exitBlend+step+.0001f));
            }
            finally{Time.fixedDeltaTime=original;}
        }
        [TestCase("resume")] [TestCase("reverse")] [TestCase("menu")] [TestCase("death")]
        [TestCase("disable")] [TestCase("reset")] [TestCase("support")]
        public void InterruptedStopsReleaseBothOwners(string kind)
        {
            using var f=new Fixture(6.15f);f.Request(Vector2.zero);f.Begin();f.Base.Tick(3);
            switch(kind)
            {
                case "resume":f.Request(Vector2.up);break;
                case "reverse":f.Request(Vector2.down);break;
                case "menu":f.Base.Paused=true;break;
                case "death":typeof(TopDown3DSurvivalVitals).GetField("health",Hidden).SetValue(f.Base.Motor.GetComponent<TopDown3DSurvivalVitals>(),0f);break;
                case "disable":f.Transition.enabled=false;typeof(AnimationPlaygroundLocomotionTransitionController).GetMethod("OnDisable",Hidden).Invoke(f.Transition,null);break;
                case "reset":f.Transition.InvalidateForReset(2);break;
                case "support":typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(f.Base.Motor,false);break;
            }
            f.Base.Tick(20);f.Base.Paused=false;f.Base.Tick(15);
            Assert.That(f.Base.Actions.IsBusy,Is.False);Assert.That(f.Base.Motor.IsActionConstrained,Is.False);
            Assert.That(f.Base.Driver.AnimationPresentationOwned,Is.False);Assert.That(f.Base.Driver.PoseBlendOwned,Is.False);
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void EquippedCompletePosesFitNativeGround(SkinWeights quality)
        {
            var p=Profile();using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);var mesh=new Mesh();
            var actions=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            actions.Configure(f.Inner.Motor,f.Inner.Driver,new AnimationPlaygroundResetRegistry(),new[]{f.Inner.Motor.Position});
            try
            {
                foreach(var take in new[]{p.runStop,p.sprintStop,p.pivotLeft,p.pivotRight})
                {
                    f.Place(AnimationPlaygroundNativeGroundTests.Sites[0],0);
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="OfflineStopTurnPose",clip=take.clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(actions.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.AlignedAction,null,_=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null,policy:AnimationPlaygroundMotionPolicy.DodgeLocomotion),Is.True);
                    f.Inner.Driver.SetAnimationBodyProjection(actions,p.bodyProjection);f.Inner.Driver.SetAnimationDodgeProjection(actions);f.Inner.Driver.SetAnimationGarmentFloor(actions,true,.6f,.01f);
                    foreach(var site in new[]{AnimationPlaygroundNativeGroundTests.Sites[0],AnimationPlaygroundNativeGroundTests.Sites[3]})
                    foreach(var yaw in new[]{0f,90f,180f,270f})
                    for(var phase=0;phase<=32;phase++)
                    {
                        f.Place(site,yaw);f.Inner.Forward.Call("PrepareForwardGraphUpdate");f.Inner.Driver.SetAnimationPresentationTime(actions,take.clip.length*phase/32f);
                        f.Inner.Forward.Call("PresentAnimationAction");f.Inner.Forward.Call("LateUpdate");
                        Assert.That(f.Inner.Driver.LaboratoryNativeGroundAdapted,Is.True,f.Inner.Driver.LaboratoryNativeGroundFailure);
                        Assert.That(f.Inner.Driver.GarmentFloorExceeded,Is.False);
                        if(phase%4==0)Assert.That(f.MeshMinimum(mesh),Is.GreaterThanOrEqualTo(-.002f),$"{quality}/{take.clip.name}/{site}/{yaw}/{phase}/{f.MinimumSkin}");
                    }
                    actions.Cancel();
                }
            }
            finally{actions.DisposeOwnedServices();Object.DestroyImmediate(mesh);}
        }
        private sealed class Fixture:IDisposable
        {
            public readonly AnimationPlaygroundStandingStrafeTests.InputFixture Input=new();
            public readonly AnimationPlaygroundDodgeTests.Fixture Base=new();
            public readonly AnimationPlaygroundLocomotionTransitionController Transition;
            readonly GameObject basis=new("Stop turn camera basis");readonly AnimationPlaygroundLocomotionTransitionProfile profile;
            public Fixture(float speed)
            {
                typeof(TopDown3DPlayerMotor).GetField("input",Hidden).SetValue(Base.Motor,Input.Router);
                typeof(TopDown3DPlayerMotor).GetField("cameraBasis",Hidden).SetValue(Base.Motor,basis.transform);
                Base.Motor.ConfigureLaboratorySpeeds(Base.Actions,1.4f,3,4.9f);Base.Motor.ConfigureLaboratoryGamepadGaits(Base.Actions,4.9f,6.15f);
                profile=Object.Instantiate(Profile());profile.qualified=true;
                Transition=Base.Forward.Actor.AddComponent<AnimationPlaygroundLocomotionTransitionController>();
                Transition.Configure(Base.Motor,Base.Driver,Base.Actions,profile,Base.Registry,()=>Base.Paused,()=>Base.Busy);
                Base.Body.linearVelocity=Vector3.forward*speed;Base.Motor.FacePlanarDirection(Vector3.forward);
                ((AnimationPlaygroundDirectionalTests.Fixture)typeof(AnimationPlaygroundDodgeTests.Fixture).GetField("directional",Hidden).GetValue(Base)).Flat();
            }
            public void Request(Vector2 direction)=>Input.PadState(new GamepadState{leftStick=direction}.WithButton(GamepadButton.RightShoulder));
            public void Begin()
            {
                typeof(AnimationPlaygroundLocomotionTransitionController).GetMethod("ObserveRequest",Hidden).Invoke(Transition,null);
                Assert.That(Transition.Active,Is.True,"The interruption must start a real owned transition.");
            }
            public void Dispose(){Transition.DisposeOwnedServices();Base.Dispose();Input.Dispose();Object.DestroyImmediate(basis);Object.DestroyImmediate(profile);}
        }
    }
}
