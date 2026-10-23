using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundMovementExpansionTests
    {
        private const string Private = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/";
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Load<T>(string name) where T : Object
        { var asset = AssetDatabase.LoadAssetAtPath<T>(Private+name+".asset"); Assert.That(asset, Is.Not.Null, "Restore the private movement expansion bundle."); return asset; }
        private static AnimationPlaygroundStandingStrafeProfile Directional => Load<AnimationPlaygroundStandingStrafeProfile>("StandingWalkJogRunReviewV3");
        private static AnimationPlaygroundSprintEntryProfile Entries => Load<AnimationPlaygroundSprintEntryProfile>("SprintEntryProfile");

        [Test] public void ThreeLateralGaitsHaveDistinctAnchorsAndContinuousNormalizedAngularWeights()
        {
            var p = Directional; var f = Load<AnimationPlaygroundForwardLocomotionProfile>("ExpandedForwardGaitReview");
            Assert.That(p.Validate(p.targetModel,p.footProjection,out var reason),Is.True,reason);
            Assert.That(p.DirectionalChannelCount,Is.EqualTo(14));
            Assert.That(p.directionalGaits[0].sourceClip,Is.Not.EqualTo(p.directionalGaits[1].sourceClip));
            Assert.That(p.directionalGaits[1].sourceClip,Is.Not.EqualTo(p.directionalGaits[2].sourceClip));
            var weights = new float[14]; var previous = new float[14];
            foreach (var speed in new[] {0f,.5f,1.4f,2.2f,3f,4f,5f,6.15f})
            for (var angle = -180; angle <= 180; angle++)
            {
                var local = new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad));
                AnimationPlaygroundDirectionalMath.FillPoseWeights(p,f,speed,local,weights);
                Assert.That(weights.All(w=>float.IsFinite(w)&&w>=0&&w<=1),Is.True);
                Assert.That(weights.Sum(),Is.EqualTo(1).Within(.00001));
                if(angle>-180) Assert.That(weights.Zip(previous,(a,b)=>Mathf.Abs(a-b)).Max(),Is.LessThan(.012f));
                Array.Copy(weights,previous,14);
            }
            foreach (var side in new[] {-1,1})
            foreach (var stage in new[] {0,1,2})
            {
                AnimationPlaygroundDirectionalMath.FillPoseWeights(p,f,new[]{1.4f,3f,5f}[stage],new Vector2(side,0),weights);
                Assert.That(weights[6+(side<0?0:3)+stage],Is.EqualTo(1));
            }
            AnimationPlaygroundDirectionalMath.FillPoseWeights(p,f,4,Vector2.down,weights);
            Assert.That(weights[13],Is.EqualTo(1));
            AnimationPlaygroundDirectionalMath.FillPoseWeights(p,f,6.15f,Vector2.up,weights);
            Assert.That(weights[5],Is.EqualTo(1));
        }

        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void ActualFourteenChannelGraphKeepsOneOutputAndEquippedClearance(SkinWeights quality)
        {
            using var f = new AnimationPlaygroundDirectionalTests.Fixture(quality,Directional); var mesh = new Mesh();
            try
            {
                f.Flat(); f.Lock(true);
                foreach(var angle in new[]{-135f,-90f,-45f,45f,90f,135f})
                foreach(var speed in new[]{1.4f,3f,5f})
                for(var phase=0;phase<4;phase++)
                {
                    var direction=new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad));
                    f.Frame(Mathf.Min(speed,AnimationPlaygroundDirectionalMath.SpeedLimit(Directional,direction)),phase/4d,direction,.3f);
                    Assert.That(f.Minimum(mesh,out var height),Is.GreaterThanOrEqualTo(-.002f)); Assert.That(float.IsFinite(height),Is.True);
                }
                Assert.That(f.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));
                Assert.That(f.Forward.Get<AnimationMixerPlayable>("standingStrafeMixer").GetInputCount(),Is.EqualTo(14));
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test] public void CompleteEntrySourcesKeepMotorRootOwnershipAndRejectUnboundedTravel()
        {
            var p=Entries; Assert.That(p.Validate(p.targetModel,out var reason),Is.True,reason);
            Assert.That(p.idle.clip.length,Is.EqualTo(.8f).Within(.00001)); Assert.That(p.crouched.clip.length,Is.EqualTo(.5f).Within(.00001));
            foreach(var entry in new[]{p.idle,p.crouched})
            {
                foreach(var binding in AnimationUtility.GetCurveBindings(entry.clip).Where(b=>b.propertyName=="RootT.x"||b.propertyName=="RootT.z"))
                    Assert.That(AnimationUtility.GetEditorCurve(entry.clip,binding).keys.All(k=>k.value==0),Is.True);
                var travelled=0f;
                for(var i=0;i<256;i++) travelled+=entry.Speed(entry.clip.length*i/256f,entry.clip.length*(i+1)/256f)*entry.clip.length/256f;
                Assert.That(travelled,Is.EqualTo(entry.distance).Within(.0001));
            }
            var clone=Object.Instantiate(p);
            try { clone.idle.distance=10; Assert.That(clone.Validate(p.targetModel,out _),Is.False); }
            finally { Object.DestroyImmediate(clone); }
        }

        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void JogBlendsRemainSelectedAndGroundedOnTheNativeCampTerrain(SkinWeights quality)
        {
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality,Directional); var mesh=new Mesh();
            try
            {
                foreach(var site in new[]{AnimationPlaygroundNativeGroundTests.Sites[0],AnimationPlaygroundNativeGroundTests.Sites[3]})
                foreach(var yaw in new[]{0f,90f})
                foreach(var angle in new[]{-135f,-90f,-45f,45f,90f,135f})
                foreach(var speed in new[]{2.2f,3f,4f})
                for(var phase=0;phase<4;phase++)
                {
                    f.Place(site,yaw); f.Inner.Lock(true);
                    var direction=new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad));
                    f.Inner.Frame(speed,phase/4d,direction,.3f);
                    Assert.That(f.Inner.Driver.LaboratoryStandingStrafeConfigured,Is.True);
                    Assert.That(f.Inner.Driver.LaboratoryNativeGroundAdapted,Is.True,f.Inner.Driver.LaboratoryNativeGroundFailure);
                    Assert.That(f.SoleMinimum(),Is.GreaterThanOrEqualTo(-.002f));
                    if(phase%2==0)Assert.That(f.MeshMinimum(mesh),Is.GreaterThanOrEqualTo(-.002f),$"{quality}/{site}/{yaw}/{angle}/{speed}/{phase}/{f.MinimumSkin}");
                }
            }
            finally {Object.DestroyImmediate(mesh);}
        }

        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void CompleteNewActionPosesFitTheEquippedNativeGroundEnvelope(SkinWeights quality)
        {
            var p=Entries; var roll=Load<AnimationPlaygroundDodgeProfile>("MovingRollProfile");
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            var actions=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            actions.Configure(f.Inner.Motor,f.Inner.Driver,new AnimationPlaygroundResetRegistry(),new[]{f.Inner.Motor.Position});
            var mesh=new Mesh();
            try
            {
                foreach(var clip in new[]{p.idle.clip,p.crouched.clip,roll.roll})
                {
                    f.Place(AnimationPlaygroundNativeGroundTests.Sites[0],0);
                    var binding=new AnimationPlaygroundMotionBinding {sourceId="OfflineMovementExpansion",clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(actions.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.AlignedAction,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null,policy:AnimationPlaygroundMotionPolicy.DodgeLocomotion),Is.True);
                    f.Inner.Driver.SetAnimationBodyProjection(actions,clip==roll.roll?roll.bodyProjection:p.bodyProjection); f.Inner.Driver.SetAnimationDodgeProjection(actions);
                    f.Inner.Driver.SetAnimationGarmentFloor(actions,true,.6f,.01f);
                    foreach(var site in new[]{AnimationPlaygroundNativeGroundTests.Sites[0],AnimationPlaygroundNativeGroundTests.Sites[3]})
                    foreach(var yaw in new[]{0f,90f,180f,270f})
                    for(var frame=0;frame<=32;frame++)
                    {
                        f.Place(site,yaw); f.Inner.Forward.Call("PrepareForwardGraphUpdate");
                        f.Inner.Driver.SetAnimationPresentationTime(actions,clip.length*frame/32f);
                        f.Inner.Forward.Call("PresentAnimationAction"); f.Inner.Forward.Call("LateUpdate");
                        Assert.That(f.Inner.Driver.LaboratoryNativeGroundAdapted,Is.True,f.Inner.Driver.LaboratoryNativeGroundFailure);
                        Assert.That(f.Inner.Driver.GarmentFloorExceeded,Is.False,$"{quality}/{clip.name}/{site}/{yaw}/{frame}");
                        if(frame%4==0)Assert.That(f.MeshMinimum(mesh),Is.GreaterThanOrEqualTo(-.002f),$"{quality}/{clip.name}/{site}/{yaw}/{frame}/{f.MinimumSkin}");
                    }
                    actions.Cancel();
                }
            }
            finally { actions.DisposeOwnedServices(); Object.DestroyImmediate(mesh); }
        }

        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void EquippedActionsRemainClearWhenGearWasBoundDuringMovementOnTheFlatDeck(SkinWeights quality)
        {
            using var f=new AnimationPlaygroundDirectionalTests.Fixture(quality,Directional);
            f.Frame(3,.21,Vector2.up,.3f); // Deliberately differs from the offline cache's neutral bind pose.
            var actions=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            actions.Configure(f.Motor,f.Driver,new AnimationPlaygroundResetRegistry(),new[]{f.Motor.Position}); var mesh=new Mesh();
            var p=Entries; var roll=Load<AnimationPlaygroundDodgeProfile>("MovingRollProfile");
            try
            {
                foreach(var clip in new[]{p.idle.clip,p.crouched.clip,roll.roll})
                {
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="OfflineFlatEntry",clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(actions.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.AlignedAction,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null,policy:AnimationPlaygroundMotionPolicy.DodgeLocomotion),Is.True);
                    f.Driver.SetAnimationBodyProjection(actions,clip==roll.roll?roll.bodyProjection:p.bodyProjection);
                    f.Driver.SetAnimationDodgeProjection(actions); f.Driver.SetAnimationGarmentFloor(actions,true,.6f,.01f);
                    for(var frame=0;frame<=32;frame++)
                    {
                        f.Forward.Call("PrepareForwardGraphUpdate"); f.Driver.SetAnimationPresentationTime(actions,clip.length*frame/32f);
                        f.Forward.Call("PresentAnimationAction"); f.Forward.Call("LateUpdate");
                        Assert.That(f.Minimum(mesh,out _),Is.GreaterThanOrEqualTo(-.002f),$"{quality}/{clip.name}/{frame}");
                    }
                    actions.Cancel();
                }
            }
            finally {actions.DisposeOwnedServices();Object.DestroyImmediate(mesh);}
        }

        [Test] public void RunningRollIsChosenOnceFromEntrySpeedAndStandingRollStillWorks()
        {
            var p=Load<AnimationPlaygroundDodgeProfile>("DodgeWithRunningRoll");
            Assert.That(p.Validate(p.targetModel,out var reason),Is.True,reason);
            Assert.That(p.Select(0),Is.SameAs(p)); Assert.That(p.Select(4.9f),Is.SameAs(p.movingRoll));
            using var f=new AnimationPlaygroundDodgeTests.Fixture(p); f.Body.linearVelocity=Vector3.forward*4.9f;
            Assert.That(f.Dodge.TryDodge(),Is.True,f.Dodge.LastFailure);
            f.Body.linearVelocity=Vector3.zero; f.Tick(3);
            Assert.That(typeof(AnimationPlaygroundDodgeController).GetField("profile",Hidden).GetValue(f.Dodge),Is.SameAs(p.movingRoll));
            f.Dodge.Cancel(); Assert.That(f.Motor.IsActionConstrained,Is.False);
        }

        [TestCase(90f)] [TestCase(180f)]
        public void SprintHeldFacingCrouchRejectsLateralStartsWithoutChangingPosture(float angle)
        {
            using var f=new EntryFixture();f.Demand(false,facing:true);
            Assert.That(f.Crouch.TryToggle(),Is.True,f.Crouch.LastFailure);f.Base.Tick(130);
            Assert.That(f.Crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Crouched));
            Assert.That(f.Base.Motor.LaboratoryStandingFacingLocked,Is.False);
            var direction=new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad));
            var facing=f.Base.Motor.FacingDirection;f.Demand(true,direction,facing:true);
            Assert.That(f.Entry.TrySprintEntry(),Is.False,"Held-facing crouch must retain its directional gait.");
            Assert.That(f.Crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Crouched));
            Assert.That(f.Base.Actions.IsBusy || f.Base.Motor.IsActionConstrained,Is.False);
            Assert.That(Vector3.Angle(facing,f.Base.Motor.FacingDirection),Is.LessThan(.01f));
        }

        [Test] public void SprintHeldFacingSurvivesCrouchStartupAndOrdinaryRunOut()
        {
            using var f=new EntryFixture();f.Base.Motor.ConfigureLaboratoryStandingStrafe(f.Base.Actions,Directional,()=>true);
            f.Demand(false);f.Base.Tick(1);f.Demand(false,facing:true);f.Base.Tick(2);
            Assert.That(f.Base.Motor.LaboratoryStandingFacingLocked,Is.True);
            Assert.That(f.Crouch.TryToggle(),Is.True,f.Crouch.LastFailure);f.Base.Tick(130);
            Assert.That(f.Crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Crouched));
            Assert.That(f.Base.Motor.LaboratoryStandingFacingLocked,Is.False);
            var facing=f.Base.Motor.FacingDirection;f.Demand(true,facing:true);
            Assert.That(f.Entry.TrySprintEntry(),Is.True,f.Entry.LastFailure);
            f.Base.Tick(Mathf.CeilToInt(Entries.crouched.Duration/Time.fixedDeltaTime)+12);
            Assert.That(f.Entry.Active || f.Base.Actions.IsBusy,Is.False);
            Assert.That(f.Base.Motor.LaboratoryStandingFacingLocked,Is.True,"The held lock must transfer back to ordinary standing locomotion.");
            Assert.That(Vector3.Angle(facing,f.Base.Motor.FacingDirection),Is.LessThan(.01f));
        }
        [TestCase("foreign")] [TestCase("menu")] [TestCase("focus")] [TestCase("airborne")] [TestCase("released")]
        public void SprintHeldFacingRejectsForeignAndStaleRearming(string condition)
        {
            using var f=new EntryFixture();f.Base.Motor.ConfigureLaboratoryStandingStrafe(f.Base.Actions,Directional,()=>true);
            f.Demand(true,facing:true);Assert.That(f.Entry.TrySprintEntry(),Is.True,f.Entry.LastFailure);f.Base.Tick(2);
            Assert.That(f.Base.Motor.LaboratoryStandingFacingLocked,Is.False);
            if(condition=="menu")f.Input.Router.EnterSystemMenuMode();
            if(condition=="focus"){var method=typeof(TopDown3DInputRouter).GetMethod("OnApplicationFocus",Hidden);method.Invoke(f.Input.Router,new object[]{false});method.Invoke(f.Input.Router,new object[]{true});}
            if(condition=="airborne")typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(f.Base.Motor,false);
            if(condition=="released")f.Demand(true,facing:false);
            f.Base.Motor.RestoreDodgeFacing(condition=="foreign"?new object():f.Base.Actions);
            Assert.That(f.Base.Motor.LaboratoryStandingFacingLocked,Is.False);Assert.That(f.Base.Actions.IsSequenceOwner(f.Entry),Is.True);
        }

        [TestCase(0f,30)] [TestCase(0f,50)] [TestCase(0f,60)] [TestCase(0f,120)]
        [TestCase(.6f,30)] [TestCase(.6f,50)] [TestCase(.6f,60)] [TestCase(.6f,120)]
        public void SharedSprintClockPreservesCompleteSourceTravelWithoutRunOutBoost(float duration,int hz)
        {
            var p=Object.Instantiate(Entries);
            try
            {
                p.idle.responseDuration=duration;Assert.That(p.Validate(p.targetModel,out var reason),Is.True,reason);
                var take=p.idle;var travelled=0f;var previous=0d;var previousSource=0f;
                for(var frame=1;previous<take.Duration;frame++)
                {
                    var now=Math.Min(take.Duration,frame/(double)hz);var source=take.SourceSeconds(now);
                    var speed=take.ResponseSpeed(previous,now);travelled+=speed*(float)(now-previous);
                    Assert.That(source,Is.InRange(previousSource,take.clip.length));Assert.That(speed,Is.InRange(0f,p.maximumSpeed+.001f));
                    previous=now;previousSource=source;
                }
                Assert.That(previousSource,Is.EqualTo(take.clip.length));Assert.That(travelled,Is.EqualTo(take.distance).Within(.0001f));
                Assert.That(take.SourceSeconds(take.Duration+1),Is.EqualTo(take.clip.length));
            }
            finally{Object.DestroyImmediate(p);}
        }
        [TestCase(-.1f)] [TestCase(.2f)] [TestCase(.5f)] [TestCase(2f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void SprintClockRejectsInvalidOrOverspeedPhysicalDurations(float duration)
        {
            var p=Object.Instantiate(Entries);
            try{p.idle.responseDuration=duration;Assert.That(p.Validate(p.targetModel,out _),Is.False);}
            finally{Object.DestroyImmediate(p);}
        }
        [TestCase(.6f)] [TestCase(.8f)]
        public void SprintClockCommandsExactTravelAndHandsOffAtThePhysicalEndpoint(float duration)
        {
            var p=Object.Instantiate(Entries);
            try
            {
                p.idle.responseDuration=duration;using var f=new EntryFixture(p);f.Demand(true);
                Assert.That(f.Entry.TrySprintEntry(),Is.True,f.Entry.LastFailure);var origin=f.Base.Body.position;var travelled=0f;
                var ticks=Mathf.RoundToInt(duration/Time.fixedDeltaTime);
                for(var i=0;i<ticks;i++){f.Base.Tick(1);travelled+=f.Base.Body.linearVelocity.z*Time.fixedDeltaTime;}
                Assert.That(travelled,Is.EqualTo(p.idle.distance).Within(.001f),"The fixed-step commands must not add travel to force the target gait.");
                Assert.That(f.Base.Actions.IsBusy || f.Base.Motor.IsActionConstrained,Is.False);
                Assert.That(f.Base.Body.linearVelocity.z,Is.GreaterThan(4),"No extra zero-speed catch-up tick at the complete-source boundary.");
                Assert.That(f.Base.Body.position,Is.EqualTo(origin),"This command fixture does not step physics.");
                Assert.That(f.Base.Driver.PoseBlendOwned,Is.True);f.Base.Tick(12);f.Poll();
                Assert.That(f.Entry.Active || f.Base.Driver.PoseBlendOwned || f.Base.Actions.IsBusy,Is.False,"Held sprint cannot queue another startup.");
            }
            finally{Object.DestroyImmediate(p);}
        }

        [Test] public void IdleEntryCompletesOnceKeepsMomentumAndBlendsIntoTheMeasuredGaitPhase()
        {
            using var f=new EntryFixture(); f.Demand(true);
            Assert.That(f.Entry.TrySprintEntry(),Is.True,f.Entry.LastFailure); Assert.That(f.Entry.TrySprintEntry(),Is.False);
            var position=f.Base.Body.position; f.Base.Tick(70);
            Assert.That(f.Entry.Active,Is.False); Assert.That(f.Base.Actions.IsBusy,Is.False); Assert.That(f.Base.Driver.PoseBlendOwned,Is.False);
            Assert.That(f.Base.Body.linearVelocity.z,Is.GreaterThan(4)); Assert.That(f.Base.Body.position,Is.EqualTo(position),"Command/state proof does not step physics.");
            f.Poll(); Assert.That(f.Entry.Active,Is.False,"Held demand cannot repeat the take.");
        }

        [TestCase("release")] [TestCase("reverse")] [TestCase("support")] [TestCase("menu")] [TestCase("death")] [TestCase("disable")] [TestCase("reset")]
        public void EntryInterruptionsReleaseOwnersWithoutQueuedRestart(string interruption)
        {
            using var f=new EntryFixture(); f.Demand(true); Assert.That(f.Entry.TrySprintEntry(),Is.True,f.Entry.LastFailure); f.Base.Tick(3);
            switch(interruption)
            {
                case "release": f.Demand(false); break;
                case "reverse": f.Demand(true,Vector2.down); break;
                case "support": typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(f.Base.Motor,false); break;
                case "menu": f.Base.Paused=true; break;
                case "death": typeof(TopDown3DSurvivalVitals).GetField("health",Hidden).SetValue(f.Base.Motor.GetComponent<TopDown3DSurvivalVitals>(),0f); break;
                case "disable": f.Entry.enabled=false; typeof(AnimationPlaygroundSprintEntryController).GetMethod("OnDisable",Hidden).Invoke(f.Entry,null); break;
                case "reset": f.Entry.InvalidateForReset(2); break;
            }
            f.Poll(); f.Base.Tick(20); if(interruption=="menu") { f.Base.Paused=false; f.Base.Tick(20); }
            Assert.That(f.Entry.Active,Is.False); Assert.That(f.Base.Actions.IsBusy,Is.False); Assert.That(f.Base.Driver.PoseBlendOwned,Is.False);
            Assert.That(f.Base.Motor.IsActionConstrained,Is.False);
        }

        [Test] public void CrouchSprintRequiresStandingClearanceAndRestoresStandingThroughTheCompleteTake()
        {
            using var f=new EntryFixture(); Assert.That(f.Crouch.TryToggle(),Is.True,f.Crouch.LastFailure); f.Base.Tick(130);
            Assert.That(f.Crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Crouched));
            var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(ceiling,f.Base.Forward.Scene);
            try
            {
                ceiling.transform.position=f.Base.Body.position+Vector3.up*.5f; ceiling.transform.localScale=new Vector3(2,.1f,2); Physics.SyncTransforms();
                f.Demand(true); Assert.That(f.Entry.TrySprintEntry(),Is.False); Assert.That(f.Crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Crouched));
                Assert.That(f.Base.Driver.PoseBlendOwned,Is.False); ceiling.SetActive(false); Physics.SyncTransforms();
                Assert.That(f.Entry.TrySprintEntry(),Is.True,f.Entry.LastFailure); Assert.That(f.Base.Motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));
                f.Base.Tick(60); Assert.That(f.Entry.Active,Is.False); Assert.That(f.Crouch.Active,Is.False); Assert.That(f.Base.Motor.IsActionConstrained,Is.False);
            }
            finally { Object.DestroyImmediate(ceiling); }
        }

        [Test] public void SessionCancelKeepsSprintEntryAvailableAndServiceDetachRemovesIt()
        {
            using var f=new EntryFixture();var session=f.Base.Forward.Session;
            typeof(TopDown3DAnimationPlaygroundSession).GetField("sprintEntries",Hidden).SetValue(session,f.Entry);
            typeof(TopDown3DAnimationPlaygroundSession).GetField("coordinator",Hidden).SetValue(session,f.Base.Actions);
            typeof(TopDown3DAnimationPlaygroundSession).GetMethod("CancelOwnedAction",Hidden).Invoke(session,null);
            Assert.That(f.Entry!=null && f.Entry.Ready,Is.True,"Cancelling ordinary locomotion must retain the entry service.");
            f.Demand(true);Assert.That(f.Entry.TrySprintEntry(),Is.True,f.Entry.LastFailure);
            typeof(TopDown3DAnimationPlaygroundSession).GetMethod("DetachPlayerServices",Hidden).Invoke(session,null);
            Assert.That(typeof(TopDown3DAnimationPlaygroundSession).GetField("sprintEntries",Hidden).GetValue(session),Is.Null);
            Assert.That(f.Base.Motor.IsActionConstrained,Is.False);Assert.That(f.Base.Driver.PoseBlendOwned,Is.False);
        }

        [Test] public void SustainedRunningDoesNotRetriggerAnIdleEntry()
        { using var f=new EntryFixture(); f.Demand(true); f.Base.Body.linearVelocity=Vector3.forward*4.9f; Assert.That(f.Entry.TrySprintEntry(),Is.False); }

        [TestCase(3)] [TestCase(18)] [TestCase(36)]
        public void SprintEntryCanYieldItsPoseAndMovementWithoutRestartingWhileSprintRemainsHeld(int ticks)
        {
            using var f=new EntryFixture();f.Demand(true);Assert.That(f.Entry.TrySprintEntry(),Is.True,f.Entry.LastFailure);f.Base.Tick(ticks);
            Assert.That(f.Entry.Active,Is.True);
            Assert.That(f.Entry.TryYieldTo(f.Base.Dodge),Is.True);
            Assert.That(f.Entry.Active,Is.False);Assert.That(f.Base.Actions.IsBusy,Is.False);Assert.That(f.Base.Motor.IsActionConstrained,Is.False);
            Assert.That(f.Base.Dodge.TryDodge(),Is.True,f.Base.Dodge.LastFailure);
            f.Base.Tick(90);f.Poll();
            Assert.That(f.Entry.Active,Is.False);Assert.That(f.Base.Dodge.Active,Is.False);Assert.That(f.Base.Driver.PoseBlendOwned,Is.False);
        }

        [TestCase(false)] [TestCase(true)] public void SlowWalkModifierKeepsItsMovementAuthority(bool duringEntry)
        {
            using var f=new EntryFixture(); f.Demand(true);
            if(duringEntry)Assert.That(f.Entry.TrySprintEntry(),Is.True,f.Entry.LastFailure);
            typeof(TopDown3DInputRouter).GetProperty("AnimationSlowWalkHeld").SetValue(f.Input.Router,true);
            if(duringEntry) { f.Base.Tick(20); Assert.That(f.Entry.Active,Is.False); Assert.That(f.Base.Motor.IsActionConstrained,Is.False); }
            else Assert.That(f.Entry.TrySprintEntry(),Is.False);
        }

        private sealed class EntryFixture : IDisposable
        {
            public readonly AnimationPlaygroundStandingStrafeTests.InputFixture Input=new();
            public readonly AnimationPlaygroundDodgeTests.Fixture Base=new();
            public readonly AnimationPlaygroundSprintEntryController Entry;
            public readonly AnimationPlaygroundCrouchController Crouch;
            private readonly GameObject basis=new("Sprint entry camera basis");
            public EntryFixture():this(null) {}
            public EntryFixture(AnimationPlaygroundSprintEntryProfile selected)
            {
                typeof(TopDown3DPlayerMotor).GetField("input",Hidden).SetValue(Base.Motor,Input.Router);
                typeof(TopDown3DPlayerMotor).GetField("cameraBasis",Hidden).SetValue(Base.Motor,basis.transform);
                Base.Motor.ConfigureLaboratorySpeeds(Base.Actions,1.4f,3,4.9f); Base.Motor.ConfigureLaboratoryGamepadGaits(Base.Actions,4.9f,6.15f);
                Crouch=Base.Forward.Actor.AddComponent<AnimationPlaygroundCrouchController>();
                Crouch.Configure(Base.Motor,Base.Driver,Base.Actions,AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCrouchProfile>(AnimationPlaygroundMobilityAuthoring.CrouchProfilePath),Base.Registry,()=>Base.Paused,()=>Base.Busy);
                Entry=Base.Forward.Actor.AddComponent<AnimationPlaygroundSprintEntryController>();
                Entry.Configure(Base.Motor,Base.Driver,Base.Actions,Crouch,selected ?? Entries,Base.Registry,()=>Base.Paused,()=>Base.Busy);
                Demand(false); Poll();
            }
            public void Demand(bool held,Vector2? move=null,bool facing=false)
            { var state=new GamepadState {leftStick=held?move??Vector2.up:Vector2.zero,leftTrigger=facing?1:0}; if(held)state=state.WithButton(GamepadButton.RightShoulder); Input.PadState(state); }
            public void Poll()=>typeof(AnimationPlaygroundSprintEntryController).GetMethod("Update",Hidden).Invoke(Entry,null);
            public void Dispose() { Entry.DisposeOwnedServices(); Crouch.DisposeOwnedServices(); Base.Dispose(); Input.Dispose(); Object.DestroyImmediate(basis); }
        }
    }
}
