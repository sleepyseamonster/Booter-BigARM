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
    public sealed class AnimationPlaygroundResponsivenessTests
    {
        [TestCase(false)] [TestCase(true)]
        public void RejectedRepeatDodgePreservesTheActiveEntryOrRecoveryPose(bool recovery)
        {
            using var f=new AnimationPlaygroundDodgeTests.Fixture(Family());
            const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
            var session=f.Forward.Session;
            void Bind(string name,object value)=>typeof(TopDown3DAnimationPlaygroundSession).GetField(name,hidden).SetValue(session,value);
            Bind("servicesBound",true);Bind("dodge",f.Dodge);Bind("presentation",f.Driver);Bind("coordinator",f.Actions);
            Bind("playerAction",f.Motor.GetComponent<TopDown3DPlayerActionController>());
            typeof(TopDown3DAnimationPlaygroundSession).GetMethod("SetReadiness",hidden).Invoke(session,new object[]{AnimationPlaygroundReadiness.VisualReady,null});
            Assert.That(f.Dodge.TryDodge(),Is.True,f.Dodge.LastFailure);
            if(recovery)
            {
                var ticks=0;while(f.Motor.DodgeLocomotionActive && ticks++<100)f.Tick(1);
                Assert.That(f.Motor.DodgeLocomotionActive,Is.False);
            }
            else f.Tick(1);
            Assert.That(f.Dodge.Active && f.Driver.PoseBlendOwned,Is.True);
            typeof(TopDown3DAnimationPlaygroundSession).GetMethod("RequestDodge",hidden).Invoke(session,null);
            Assert.That(f.Dodge.Active,Is.True);
            Assert.That(f.Driver.PoseBlendOwned,Is.True,"A rejected repeat must not erase the current roll's pose blend.");
            f.Tick(100);Assert.That(f.Dodge.Active || f.Actions.IsBusy || f.Driver.PoseBlendOwned,Is.False);
        }
        static AnimationPlaygroundDodgeProfile Family()=>AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset").dodge;
        [TestCase(-80f)] [TestCase(80f)]
        public void RapidTurnAdmissionUsesCompletedMotorFacingWhenRenderTransformStillTrails(float renderYaw)
        {
            using var f=new AnimationPlaygroundContinuationTests.Fixture(SkinWeights.FourBones);
            var resets=new AnimationPlaygroundResetRegistry();var actions=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            var turn=f.Forward.Actor.AddComponent<AnimationPlaygroundTurnController>();actions.Configure(f.Motor,f.Driver,resets,new[]{f.Motor.Position});
            turn.Configure(f.Motor,f.Driver,actions,AnimationPlaygroundContinuationTests.Review(),resets,()=>false);
            try
            {
                f.Forward.Player.linearVelocity=Vector3.zero;f.Support(0,0);Physics.SyncTransforms();f.Motor.FacePlanarDirection(Vector3.forward);
                f.Forward.Actor.transform.rotation=Quaternion.Euler(0,renderYaw,0);
                Assert.That(Vector3.Angle(f.Motor.FacingDirection,Vector3.forward),Is.LessThan(.01f));
                Assert.That(turn.TryBeginTurn(Vector3.back),Is.True,"Render interpolation must not reject a valid reversal.");
                const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
                var curveBasis=(Quaternion)typeof(AnimationPlaygroundTurnController).GetField("basis",hidden).GetValue(turn);
                var plantBasis=(Quaternion)typeof(TopDown3DPlayerAnimationDriver).GetField("turnPoseBasis",hidden).GetValue(f.Driver);
                Assert.That(Quaternion.Angle(curveBasis,plantBasis),Is.LessThan(.01f));
                Assert.That(Vector3.Angle(curveBasis*Vector3.forward,f.Motor.FacingDirection),Is.LessThan(.01f));
            }
            finally{turn.DisposeOwnedServices();actions.DisposeOwnedServices();}
        }
        [TestCase(false,false)] [TestCase(true,false)] [TestCase(false,true)] [TestCase(true,true)]
        public void TurnAndRecoveryYieldTheirPoseAndConstraintToAnImmediatelyUsableRoll(bool left,bool recovery)
        {
            using var f=new AnimationPlaygroundContinuationTests.Fixture(SkinWeights.FourBones);
            var resets=new AnimationPlaygroundResetRegistry();var actions=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            var turn=f.Forward.Actor.AddComponent<AnimationPlaygroundTurnController>();var dodge=f.Forward.Actor.AddComponent<AnimationPlaygroundDodgeController>();
            actions.Configure(f.Motor,f.Driver,resets,new[]{f.Motor.Position});
            turn.Configure(f.Motor,f.Driver,actions,AnimationPlaygroundContinuationTests.Review(),resets,()=>false);
            dodge.Configure(f.Motor,f.Driver,actions,Family(),resets,()=>false,()=>turn.Active);
            const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
            try
            {
                f.Forward.Player.linearVelocity=Vector3.zero;f.Motor.FacePlanarDirection(Vector3.forward);f.Support(0,0);Physics.SyncTransforms();
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(f.Motor,true);
                Assert.That(turn.TryBeginTurn(Quaternion.Euler(0,left?-175:175,0)*Vector3.forward),Is.True);
                if(recovery)
                {
                    Assert.That(f.Driver.TryCapturePoseBlend(turn,.08f),Is.True);actions.CancelPhysicalSequence(turn);
                    typeof(AnimationPlaygroundTurnController).GetField("turn",hidden).SetValue(turn,null);
                    Assert.That(f.Motor.TryBeginActionConstraint(turn),Is.True);
                    typeof(AnimationPlaygroundTurnController).GetField("settling",hidden).SetValue(turn,true);
                }
                Assert.That(dodge.CanAcceptHandoff,Is.True);Assert.That(dodge.TryDodge(),Is.False,"Outgoing owner still blocks an uncoordinated request.");
                Assert.That(turn.TryYieldTo(dodge),Is.True);Assert.That(turn.Active,Is.False);Assert.That(actions.IsBusy,Is.False);
                Assert.That(f.Motor.IsActionConstrained,Is.False);Assert.That(f.Driver.PoseBlendOwned,Is.True);
                Assert.That(dodge.TryDodge(),Is.True,dodge.LastFailure);Assert.That(actions.IsSequenceOwner(dodge),Is.True);
                turn.Cancel(null);Assert.That(actions.IsSequenceOwner(dodge),Is.True,"Stale outgoing cleanup cannot release the successor.");
                dodge.Cancel();Assert.That(actions.IsBusy || f.Motor.IsActionConstrained || f.Driver.PoseBlendOwned,Is.False);
            }
            finally{dodge.DisposeOwnedServices();turn.DisposeOwnedServices();actions.DisposeOwnedServices();}
        }
        [Test] public void InvalidSuccessorLeavesTheTurnAndItsPoseIntact()
        {
            using var f=new AnimationPlaygroundContinuationTests.Fixture(SkinWeights.FourBones);
            var resets=new AnimationPlaygroundResetRegistry();var actions=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            var turn=f.Forward.Actor.AddComponent<AnimationPlaygroundTurnController>();actions.Configure(f.Motor,f.Driver,resets,new[]{f.Motor.Position});
            turn.Configure(f.Motor,f.Driver,actions,AnimationPlaygroundContinuationTests.Review(),resets,()=>false);
            try
            {
                f.Forward.Player.linearVelocity=Vector3.zero;f.Support(0,0);Physics.SyncTransforms();
                Assert.That(turn.TryBeginTurn(Vector3.back),Is.True);Assert.That(turn.TryYieldTo(null),Is.False);
                Assert.That(turn.Active && actions.IsSequenceOwner(turn) && f.Motor.IsActionConstrained && f.Driver.PoseBlendOwned,Is.True);
            }
            finally{turn.DisposeOwnedServices();actions.DisposeOwnedServices();}
        }
        [Test] public void ActivePausedDeadAirborneAndCoolingRollsCannotAdmitAHandoff()
        {
            using var f=new AnimationPlaygroundDodgeTests.Fixture(Family());
            Assert.That(f.Dodge.CanAcceptHandoff,Is.True);f.Paused=true;Assert.That(f.Dodge.CanAcceptHandoff,Is.False);f.Paused=false;
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(f.Motor,false);Assert.That(f.Dodge.CanAcceptHandoff,Is.False);
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(f.Motor,true);
            var vitals=f.Motor.GetComponent<TopDown3DSurvivalVitals>();var health=typeof(TopDown3DSurvivalVitals).GetField("health",BindingFlags.Instance|BindingFlags.NonPublic);
            health.SetValue(vitals,0f);Assert.That(f.Dodge.CanAcceptHandoff,Is.False);health.SetValue(vitals,100f);
            Assert.That(f.Dodge.TryDodge(),Is.True);Assert.That(f.Dodge.CanAcceptHandoff,Is.False);
            f.Dodge.Cancel();Assert.That(f.Dodge.CanAcceptHandoff,Is.False);f.Tick(8);Assert.That(f.Dodge.CanAcceptHandoff,Is.True);
        }
        [TestCase(true,false,0,false)] [TestCase(true,false,2,true)]
        [TestCase(true,true,0,false)] [TestCase(false,false,2,false)]
        public void UnevaluatedTurnContactIsPendingWhileEvaluatedFailuresStillCancel(bool enabled,bool available,int reason,bool failed)
        {
            var sample=new AnimationPlaygroundTangentFootPose.ContactJob{enabled=enabled,available=available,unavailableReason=reason};
            var policy=typeof(TopDown3DPlayerAnimationDriver).GetMethod("TurnContactFailed",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That((bool)policy.Invoke(null,new object[]{sample}),Is.EqualTo(failed));
        }
        [TestCase(true)] [TestCase(false)]
        public void TurnKeepsItsPlantedFootWhenPhysicsYawLeadsTheRenderedSource(bool left)
        {
            using var f=new AnimationPlaygroundContinuationTests.Fixture(SkinWeights.FourBones);
            var review=AnimationPlaygroundContinuationTests.Review();var take=left?review.left:review.right;
            var registry=new AnimationPlaygroundResetRegistry();var actions=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            var controller=f.Forward.Actor.AddComponent<AnimationPlaygroundTurnController>();
            actions.Configure(f.Motor,f.Driver,registry,new[]{f.Motor.Position});controller.Configure(f.Motor,f.Driver,actions,review,registry,()=>false);
            try
            {
                f.Forward.Player.linearVelocity=Vector3.zero;f.Motor.FacePlanarDirection(Vector3.forward);
                f.Support(0,0);Physics.SyncTransforms();
                var origin=f.Motor.Position;
                Assert.That(controller.TryBeginTurn(Quaternion.Euler(0,left?-175:175,0)*Vector3.forward),Is.True);
                f.Driver.EndPoseBlend(controller);var anchor=Vector3.zero;
                for(var i=0;i<=60;i++)
                {
                    var time=take.clip.length*i/60;take.Sample(time,out var point,out _);
                    take.Sample(Mathf.Min(time+.08f,take.clip.length),out _,out var physicalYaw);
                    f.Forward.Actor.transform.SetPositionAndRotation(origin+point,physicalYaw);
                    f.Forward.Player.position=origin+point;f.Forward.Player.rotation=physicalYaw;
                    f.Driver.SetAnimationPresentationTime(actions,time);f.Forward.Call("PrepareForwardGraphUpdate");f.Forward.Call("PresentAnimationAction");
                    Assert.That(f.Driver.LaboratoryTurnContactUnavailable,Is.False,$"{left}/{time}");
                    var sole=f.SolePoint(take.soleVertex,4);if(i==0)anchor=sole;
                    Assert.That(Vector3.ProjectOnPlane(sole-anchor,Vector3.up).magnitude,Is.LessThan(.001f));
                }
            }
            finally{controller.DisposeOwnedServices();actions.DisposeOwnedServices();}
        }
        [TestCase(0f,.15)] [TestCase(.5f,.225)] [TestCase(1f,.3)]
        public void TurnRenderInterpolatesCompletedSourceTimesWithoutAdvancingItsOwner(float alpha,double expected)
        {
            using var f=new AnimationPlaygroundDodgeTests.Fixture(Family());
            const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
            var type=typeof(TopDown3DPlayerAnimationDriver);
            type.GetField("turnPreviousSourceTime",hidden).SetValue(f.Driver,.15);
            type.GetField("animationPresentationTime",hidden).SetValue(f.Driver,.3);
            Assert.That((double)type.GetMethod("SampleAdaptedTurnSourceTime",hidden).Invoke(f.Driver,new object[]{alpha}),Is.EqualTo(expected).Within(.000001));
            Assert.That(type.GetField("animationPresentationTime",hidden).GetValue(f.Driver),Is.EqualTo(.3));
        }
        [TestCase(false,false,false,false,true)]
        [TestCase(true,false,false,false,false)]
        [TestCase(true,true,false,false,true)]
        [TestCase(true,false,true,false,true)]
        [TestCase(true,false,false,true,true)]
        public void OwnedTurnCanCompleteThroughItsAggregateBusyFlagButStillRejectsForeignWork(bool active,bool work,bool placement,bool camp,bool blocked)
        {
            var activity=new TopDown3DActivitySnapshot(work,placement,true,camp,true);
            Assert.That(activity.IsBusy,Is.True,"Real turn ownership is represented in the aggregate busy flag.");
            var policy=typeof(TopDown3DAnimationPlaygroundSession).GetMethod("TurnActivityUnavailable",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That((bool)policy.Invoke(null,new object[]{activity,active}),Is.EqualTo(blocked));
        }
        [Test] public void SelectedRollsKeepTheirCompleteForwardRotationWithoutSidewaysRootSpin()
        {
            var family=Family();Assert.That(family.name,Is.EqualTo("ResponsiveDodge"));Assert.That(family.Validate(family.targetModel,out var reason),Is.True,reason);
            foreach(var p in new[]{family,family.movingRoll})
            {
                var bindings=AnimationUtility.GetCurveBindings(p.roll);
                var curves=new[]{"RootQ.x","RootQ.y","RootQ.z","RootQ.w"}.Select(n=>AnimationUtility.GetEditorCurve(p.roll,bindings.Single(k=>k.propertyName==n))).ToArray();
                foreach(var key in curves[0].keys)
                    Assert.That(key.value*key.value+Mathf.Pow(curves[3].Evaluate(key.time),2),Is.EqualTo(1).Within(.0001),"Authored quaternion keys must remain unit rotations.");
                var sum=0f;var previous=0f;
                for(var i=0;i<=256;i++)
                {
                    var time=p.roll.length*i/256;var x=curves[0].Evaluate(time);var w=curves[3].Evaluate(time);
                    Assert.That(curves[1].Evaluate(time),Is.Zero);Assert.That(curves[2].Evaluate(time),Is.Zero);
                    Assert.That(x*x+w*w,Is.GreaterThan(.98f),"Linear component interpolation must remain nondegenerate between unit keys.");
                    var angle=2*Mathf.Atan2(x,w)*Mathf.Rad2Deg;if(i>0)sum+=Mathf.DeltaAngle(previous,angle);previous=angle;
                }
                Assert.That(sum,Is.InRange(340,380),"Keep one forward somersault; do not freeze or reverse the roll.");
                Assert.That(p.roll.isLooping,Is.False);
                Assert.That(p.playbackRate,Is.EqualTo(1),"Phase timing owns the response; do not stack a global speed multiplier.");
                Assert.That(p.Duration,Is.InRange(.48f,.62f));
            }
        }
        [TestCase(0f)] [TestCase(3f)] [TestCase(6.15f)]
        public void SelectedRollReturnsControlPromptlyWithPoseAndTravelOnTheSameCompleteClock(float speed)
        {
            var p=Family().Select(speed);using var f=new AnimationPlaygroundDodgeTests.Fixture(Family());f.Body.linearVelocity=Vector3.forward*speed;
            Assert.That(f.Dodge.TryDodge(),Is.True,f.Dodge.LastFailure);var ticks=0;
            while(f.Actions.IsBusy && ticks<100)
            {
                f.Tick(1);ticks++;Assert.That(f.Body.linearVelocity.magnitude,Is.LessThanOrEqualTo(10.01f));
                if(f.Actions.IsBusy)
                {
                    f.Forward.Call("PresentAnimationAction");
                    var playable=f.Forward.Get<AnimationClipPlayable>("animationActionPlayable");
                    Assert.That(playable.GetTime(),Is.EqualTo(p.SourceSeconds(f.Actions.PhysicalStageSeconds)).Within(.0001));
                }
            }
            var duration=p.Duration;
            Assert.That(ticks*Time.fixedDeltaTime,Is.InRange(duration-.000001f,duration+Time.fixedDeltaTime+.000001f));
            f.Tick(6);Assert.That(f.Dodge.Active,Is.False);Assert.That(f.Motor.IsActionConstrained,Is.False);Assert.That(f.Driver.PoseBlendOwned,Is.False);
        }
        [Test] public void SelectedRollPausesItsSourceClockAndResetReleasesAllOwners()
        {
            using var f=new AnimationPlaygroundDodgeTests.Fixture(Family());f.Body.linearVelocity=Vector3.forward*6.15f;
            Assert.That(f.Dodge.TryDodge(),Is.True);f.Tick(5);var seconds=f.Actions.PhysicalStageSeconds;f.Paused=true;f.Tick(6);
            Assert.That(f.Actions.PhysicalStageSeconds,Is.EqualTo(seconds));f.Paused=false;f.Tick(2);Assert.That(f.Actions.IsBusy,Is.True);
            f.Dodge.InvalidateForReset(7);Assert.That(f.Actions.IsBusy,Is.False);Assert.That(f.Driver.PoseBlendOwned,Is.False);Assert.That(f.Motor.IsActionConstrained,Is.False);
        }
        [TestCase(false)] [TestCase(true)]
        public void ResponsiveClockKeepsTheCompleteTravelWithoutSkippingOrReversingThePose(bool moving)
        {
            var family=Family();var p=moving?family.movingRoll:family;
            var source=0f;var travelled=0f;const int steps=512;
            for(var i=1;i<=steps;i++)
            {
                var from=p.Duration*(i-1)/steps;var to=p.Duration*i/steps;
                var next=p.SourceSeconds(to);Assert.That(next,Is.GreaterThanOrEqualTo(source));source=next;
                travelled+=p.CommandSpeed(from,to)*(to-from);
            }
            Assert.That(p.SourceSeconds(0),Is.Zero);Assert.That(source,Is.EqualTo(p.roll.length).Within(.00001f));
            Assert.That(travelled,Is.EqualTo(p.distance).Within(.001f),"Pose retiming and displacement must share the same source interval.");
            Assert.That(p.Duration+p.exitBlend,Is.LessThanOrEqualTo(.7f),"A dodge must not become a one-second action sequence.");
        }
        [TestCase(-.1f)] [TestCase(.2f)] [TestCase(.9f)] [TestCase(float.NaN)]
        public void InvalidResponseDurationsCannotBeSelected(float duration)
        {
            var p=Object.Instantiate(Family());try{p.responseDuration=duration;Assert.That(p.Validate(p.targetModel,out _),Is.False);}finally{Object.DestroyImmediate(p);}
        }
        [TestCase(.5f)] [TestCase(3.1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidRollPlaybackCannotPassQualification(float rate)
        {
            var p=Object.Instantiate(Family());try{p.playbackRate=rate;Assert.That(p.Validate(p.targetModel,out _),Is.False);}finally{Object.DestroyImmediate(p);}
        }
        [TestCase(-1f,.2)] [TestCase(0f,.2)] [TestCase(.25f,.205)] [TestCase(.5f,.21)] [TestCase(1f,.22)] [TestCase(2f,.22)]
        public void RollRenderInterpolatesCompletedSourceTimesWithoutAdvancingTheAction(float alpha,double expected)
        {
            using var f=new AnimationPlaygroundDodgeTests.Fixture(Family());
            Assert.That(f.Dodge.TryDodge(),Is.True);
            const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
            var type=typeof(TopDown3DPlayerAnimationDriver);
            type.GetField("dodgePreviousSourceTime",hidden).SetValue(f.Driver,.2);
            type.GetField("animationPresentationTime",hidden).SetValue(f.Driver,.22);
            var before=f.Actions.PhysicalStageSeconds;var velocity=f.Body.linearVelocity;
            Assert.That((double)type.GetMethod("SampleDodgeSourceTime",hidden).Invoke(f.Driver,new object[]{alpha}),Is.EqualTo(expected).Within(.000001));
            Assert.That(type.GetField("animationPresentationTime",hidden).GetValue(f.Driver),Is.EqualTo(.22));
            Assert.That(f.Actions.PhysicalStageSeconds,Is.EqualTo(before));Assert.That(f.Body.linearVelocity,Is.EqualTo(velocity));
        }
        [Test] public void RollSourceObservationIsOncePerPhysicsStepAndStartsFreshForTheNextRoll()
        {
            using var f=new AnimationPlaygroundDodgeTests.Fixture(Family());
            const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
            var type=typeof(TopDown3DPlayerAnimationDriver);
            Assert.That(f.Dodge.TryDodge(),Is.True);
            f.Driver.SetAnimationPresentationTime(f.Actions,.02);f.Driver.SetAnimationPresentationTime(f.Actions,.03);
            Assert.That(type.GetField("dodgePreviousSourceTime",hidden).GetValue(f.Driver),Is.EqualTo(0d));
            type.GetField("dodgeSourceSampleTime",hidden).SetValue(f.Driver,double.NegativeInfinity);
            f.Driver.SetAnimationPresentationTime(f.Actions,.04);f.Driver.SetAnimationPresentationTime(f.Actions,.05);
            Assert.That(type.GetField("dodgePreviousSourceTime",hidden).GetValue(f.Driver),Is.EqualTo(.03));
            f.Dodge.Cancel();Assert.That(f.Dodge.TryRestoreForReset(out _),Is.True);Assert.That(f.Dodge.TryDodge(),Is.True);
            Assert.That(type.GetField("dodgePreviousSourceTime",hidden).GetValue(f.Driver),Is.EqualTo(0d));
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void SelectedStraightRollsClearNativeGroundWithActualEquipment(SkinWeights quality)
        {
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);var actions=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();var mesh=new Mesh();
            actions.Configure(f.Inner.Motor,f.Inner.Driver,new AnimationPlaygroundResetRegistry(),new[]{f.Inner.Motor.Position});
            try
            {
                foreach(var p in new[]{Family(),Family().movingRoll})
                {
                    f.Place(AnimationPlaygroundNativeGroundTests.Sites[0],0);
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="OfflineStraightRoll",clip=p.roll,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(actions.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.Dodge,null,_=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null,
                        policy:AnimationPlaygroundMotionPolicy.DodgeLocomotion),Is.True);
                    f.Inner.Driver.SetAnimationBodyProjection(actions,p.bodyProjection);f.Inner.Driver.SetAnimationDodgeProjection(actions,Vector3.forward);f.Inner.Driver.SetAnimationGarmentFloor(actions,true,p.maximumGarmentProjection,p.garmentClearance);
                    foreach(var site in new[]{AnimationPlaygroundNativeGroundTests.Sites[0],AnimationPlaygroundNativeGroundTests.Sites[3]})
                    foreach(var yaw in new[]{0f,90f,180f,270f})
                    for(var frame=0;frame<=32;frame++)
                    {
                        f.Place(site,yaw);f.Inner.Forward.Call("PrepareForwardGraphUpdate");f.Inner.Driver.SetAnimationPresentationTime(actions,p.roll.length*frame/32);
                        f.Inner.Forward.Call("PresentAnimationAction");f.Inner.Forward.Call("LateUpdate");
                        Assert.That(f.Inner.Driver.LaboratoryNativeGroundAdapted,Is.True,f.Inner.Driver.LaboratoryNativeGroundFailure);Assert.That(f.Inner.Driver.GarmentFloorExceeded,Is.False);
                        if(frame%4==0)Assert.That(f.MeshMinimum(mesh),Is.GreaterThanOrEqualTo(-.002f),$"{quality}/{p.name}/{site}/{yaw}/{frame}/{f.MinimumSkin}");
                    }
                    actions.Cancel();
                }
            }
            finally{actions.DisposeOwnedServices();Object.DestroyImmediate(mesh);}
        }
    }
}
