using System;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundLowMotionControllerTests
    {
        private GameObject actor,floor,roof,visual;
        private Rigidbody body;
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundLowMotionController low;
        private AnimationPlaygroundCrouchController crouch;
        private AnimationPlaygroundLowMotionProfile profile;
        private AnimationPlaygroundCrouchProfile crouchProfile;
        private AnimationPlaygroundFootProjection projection;
        private AnimationPlaygroundResetRegistry registry;
        private PlayableGraph graph;
        private bool paused,busy;
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private static object Call(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
        [SetUp] public void Setup()
        {
            paused=false;busy=false;
            var source=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundLowMotionProfile>(AnimationPlaygroundLowMotionAuthoring.ProfilePath);
            Assert.That(source,Is.Not.Null);profile=Object.Instantiate(source);profile.qualified=true; // Controller fixture does not qualify source art.
            actor=new GameObject("Low-motion lease fixture");actor.SetActive(false);actor.transform.position=new Vector3(78000,.91f,78000);
            motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();body=actor.GetComponent<Rigidbody>();
            var shape=actor.GetComponent<CapsuleCollider>();shape.height=1.82f;shape.radius=.2f;shape.center=Vector3.zero;
            visual=new GameObject("Owned low visual");visual.transform.SetParent(actor.transform,false);var animator=visual.AddComponent<Animator>();
            projection=ScriptableObject.CreateInstance<AnimationPlaygroundFootProjection>();projection.model=profile.targetModel;
            projection.bonePaths=new[]{""};projection.vertices=new[]{new AnimationPlaygroundFootVertex{points=new[]{
                new AnimationPlaygroundSkinPoint{bone=0,position=Vector3.down*.91f,weight=1}}}};profile.bodyProjection=projection;
            graph=PlayableGraph.Create("Low-motion single output");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var mixer=AnimationMixerPlayable.Create(graph,22);var idle=AnimationClipPlayable.Create(graph,profile.entries[0].clip);idle.SetSpeed(0);
            graph.Connect(idle,0,mixer,0);mixer.SetInputWeight(0,1);var output=AnimationPlayableOutput.Create(graph,"Only output",animator);output.SetSourcePlayable(mixer);graph.Play();
            Set(driver,"motor",motor);Set(driver,"visualInstance",visual);Set(driver,"visualLocalPosition",Vector3.zero);
            Set(driver,"playableGraph",graph);Set(driver,"mixer",mixer);Set(driver,"laboratoryFootProjection",projection);
            Set(driver,"footProjectionSampler",projection.Bind(visual.transform));Set(driver,"laboratoryFeetPlane",-.91f);
            registry=new AnimationPlaygroundResetRegistry();coordinator=actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,registry,new[]{actor.transform.position},()=>paused);
            low=actor.AddComponent<AnimationPlaygroundLowMotionController>();low.Configure(motor,driver,coordinator,profile,registry,()=>paused,()=>busy,
                owner=>crouch!=null && crouch.TryReceiveLowRecovery(owner));
            actor.SetActive(true);Set(motor,"<IsGrounded>k__BackingField",true);
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(78000,-.1f,78000);floor.transform.localScale=new Vector3(8,.2f,8);
            floor.AddComponent<TopDown3DGroundSurface>();Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(roof);Physics.SyncTransforms();
            low?.DisposeOwnedServices();crouch?.DisposeOwnedServices();coordinator?.DisposeOwnedServices();
            var recovery=actor!=null ? actor.GetComponent<AnimationPlaygroundLowRecovery>() : null;recovery?.DisposeOwnedServices();
            if(graph.IsValid())graph.Destroy();Object.DestroyImmediate(actor);Object.DestroyImmediate(floor);
            Object.DestroyImmediate(profile);Object.DestroyImmediate(crouchProfile);Object.DestroyImmediate(projection);Physics.SyncTransforms();
        }
        private void Tick(int count=1)
        {
            for(var i=0;i<count;i++)
            {
                Call(motor,"FixedUpdate");
                if(driver.AnimationPresentationOwned)Call(driver,"PresentAnimationAction");
                graph.Evaluate(0);
            }
        }
        private void Slide()
        {
            body.linearVelocity=Vector3.forward*3.8f;Set(motor,"<SprintActive>k__BackingField",true);
            Assert.That(low.TrySlide(),Is.True,low.LastFailure);
        }
        private void Active()
        {
            for(var i=0;i<100 && low.State!=AnimationPlaygroundLowMotionState.Active;i++)Tick();
            Assert.That(low.State,Is.EqualTo(AnimationPlaygroundLowMotionState.Active),low.LastFailure);
        }
        private void Roof(float underside)
        {
            roof=GameObject.CreatePrimitive(PrimitiveType.Cube);roof.transform.position=new Vector3(78000,underside+.05f,78000);
            roof.transform.localScale=new Vector3(5,.1f,5);Physics.SyncTransforms();
        }
        private void Crouch()
        {
            crouchProfile=Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCrouchProfile>(AnimationPlaygroundMobilityAuthoring.CrouchProfilePath));
            crouch=actor.AddComponent<AnimationPlaygroundCrouchController>();crouch.Configure(motor,driver,coordinator,crouchProfile,registry,()=>paused,()=>busy);
        }
        [Test] public void SlideRunsAllPhasesWithOneLeaseAndRestoresStanding()
        {
            Slide();Assert.That(coordinator.IsSequenceOwner(low),Is.True);Tick(150);
            Assert.That(low.Active,Is.False);Assert.That(coordinator.IsBusy,Is.False);Assert.That(motor.IsActionConstrained,Is.False);
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));Assert.That(actor.GetComponent<CapsuleCollider>().direction,Is.EqualTo(1));
            Assert.That(graph.GetOutputCount(),Is.EqualTo(1));
        }
        [Test] public void SlideRequiresSprintSpeedSupportAndQualification()
        {
            Assert.That(low.TrySlide(),Is.False);profile.qualified=false;SlideFailure();
            profile.qualified=true;Set(motor,"<IsGrounded>k__BackingField",false);SlideFailure();
        }
        [Test] public void RollEmitsItsLandingContactOnceAfterTheBlendAndPause()
        {
            var foot=new GameObject("Left contact fixture");foot.transform.SetParent(visual.transform,false);foot.transform.localPosition=Vector3.down*.9f;
            Set(driver,"leftFoot",foot.transform);
            var locomotion=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath).locomotion;
            var entries=(TopDown3DLocomotionClipEntry[])driver.GetType().GetField("locomotionEntries",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver);
            entries[(int)TopDown3DLocomotionClipRole.Sprint]=locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Sprint);
            var contacts=0;driver.FootContact+=contact=>contacts++;
            Assert.That(low.TryBeginLandingRoll(8),Is.True);Tick(2);Assert.That(contacts,Is.Zero);
            paused=true;Tick(20);Assert.That(contacts,Is.Zero);
            paused=false;Tick(100);Assert.That(contacts,Is.EqualTo(1));
        }
        private void SlideFailure(){body.linearVelocity=Vector3.forward*3.8f;Set(motor,"<SprintActive>k__BackingField",true);Assert.That(low.TrySlide(),Is.False);Assert.That(coordinator.IsBusy,Is.False);}
        [Test] public void PauseFreezesPoseAndMotorAndCancellationFinishesTheRollPhase()
        {
            Assert.That(low.TryBeginLandingRoll(8),Is.True);Active();Tick(3);
            var at=low.PoseSeconds;paused=true;Tick(20);Assert.That(low.PoseSeconds,Is.EqualTo(at));Assert.That(body.linearVelocity,Is.EqualTo(Vector3.zero));
            paused=false;Assert.That(low.TryCancel(),Is.True);Tick();
            Assert.That(low.CurrentRole,Is.EqualTo(AnimationPlaygroundLowMotionRole.RollActive));Assert.That(low.PoseSeconds,Is.GreaterThan(at));
            Tick(150);Assert.That(low.Active,Is.False);Assert.That(body.useGravity,Is.True);
        }
        [Test] public void RecoveryWaitsForRealSupportRatherThanTheSourceClock()
        {
            Assert.That(low.TryBeginLandingRoll(8),Is.True);Active();
            body.position+=Vector3.up*2;body.linearVelocity=Vector3.down*3;Physics.SyncTransforms();Tick(80);
            Assert.That(low.Active,Is.True);Assert.That(low.CurrentRole,Is.EqualTo(AnimationPlaygroundLowMotionRole.RollActive));
            Assert.That(motor.AnimationSnapshot.Supported,Is.False);Assert.That(body.useGravity,Is.True);
            body.position=new Vector3(78000,.91f,78000);Physics.SyncTransforms();Tick(150);Assert.That(low.Active,Is.False);
        }
        [Test] public void BlockedStandingFreezesTheLastSafeRecoveryPoseThenResumes()
        {
            Slide();Active();Roof(1.78f);Tick(100);Assert.That(low.State,Is.EqualTo(AnimationPlaygroundLowMotionState.BlockedRecovery));
            var at=low.PoseSeconds;Tick(20);Assert.That(low.PoseSeconds,Is.EqualTo(at));Assert.That(motor.IsActionConstrained,Is.True);
            Object.DestroyImmediate(roof);Physics.SyncTransforms();Tick(150);Assert.That(low.Active,Is.False);
        }
        [Test] public void BlockedRecoveryHandsOffToTheExistingCrouchController()
        {
            Crouch();Slide();Active();Roof(1.78f);Tick(150);
            Assert.That(low.Active,Is.False);Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Crouched));
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Crouched));Assert.That(driver.CrouchPresentationOwned,Is.True);
            Assert.That(motor.IsActionConstrained,Is.False);
        }
        [Test] public void ResetInvalidatesTheSequenceAndRestoresTheAuthoredReturn()
        {
            Slide();Active();Roof(1.78f);Tick(100);Object.DestroyImmediate(roof);Physics.SyncTransforms();
            Assert.That(registry.TryReset(),Is.True,registry.LastFailure);Assert.That(low.Active,Is.False);
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));Assert.That(coordinator.IsBusy,Is.False);
        }
        [Test] public void ExternalCancellationCannotLeaveAProneActorWithoutItsLease()
        {
            Slide();Active();coordinator.Cancel();Tick();Assert.That(coordinator.IsSequenceOwner(low),Is.True);
            Tick(150);Assert.That(low.Active,Is.False);Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));
        }
        [Test] public void AFailedResetCanBeRetriedWithoutLosingTheRetainedPose()
        {
            Slide();Active();Roof(1.78f);Tick(100);
            Assert.That(registry.TryReset(),Is.False);Assert.That(driver.IsAnimationPresentationOwner(low),Is.True);
            Assert.That(registry.TryReset(),Is.False);Assert.That(registry.LastFailure,Does.Not.Contain("retain the low pose"));
            Object.DestroyImmediate(roof);Physics.SyncTransforms();
            Assert.That(registry.TryReset(),Is.True,registry.LastFailure);Assert.That(low.Active,Is.False);
        }
        [Test] public void DisposalFromAFoldedRollFinishesItsSourceRangeBeforeStanding()
        {
            Assert.That(low.TryBeginLandingRoll(8),Is.True);Active();Tick(8);Roof(1.2f);
            low.DisposeOwnedServices();var recovery=actor.GetComponent<AnimationPlaygroundLowRecovery>();
            Assert.That(recovery,Is.Not.Null);Assert.That(recovery.CurrentRole,Is.EqualTo(AnimationPlaygroundLowMotionRole.RollActive));
            Object.DestroyImmediate(roof);Physics.SyncTransforms();Tick();
            Assert.That(recovery.CurrentRole,Is.EqualTo(AnimationPlaygroundLowMotionRole.RollActive));
            Tick(150);Assert.That(actor.GetComponent<AnimationPlaygroundLowRecovery>(),Is.Null);
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));
        }
        [Test] public void LandingHandoffChangesTheSequenceOwnerWithoutReleasingTheMotor()
        {
            var previous=new object();var clip=profile.Get(AnimationPlaygroundLowMotionRole.RollContact).clip;
            Assert.That(coordinator.TryBeginPhysicalSequence(previous,new AnimationPlaygroundMotionBinding{clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified},
                AnimationPlaygroundMotionMode.Fall,null,time=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null),Is.True);
            Assert.That(low.TryTakeLandingSequence(new object(),8),Is.False);Assert.That(coordinator.IsSequenceOwner(previous),Is.True);
            Assert.That(low.TryTakeLandingSequence(previous,8),Is.True);Assert.That(coordinator.IsSequenceOwner(low),Is.True);Assert.That(motor.IsAnimationMotionOwner(coordinator),Is.True);
            Tick(150);Assert.That(low.Active,Is.False);
        }
        [Test] public void DisableFinishesSafelyAndBlockedDisposalRetainsPhysicsUntilClear()
        {
            Slide();Active();Roof(1.78f);Tick(100);low.enabled=false;Tick(10);Assert.That(low.Active,Is.True);
            var position=body.position;low.DisposeOwnedServices();
            var recovery=actor.GetComponent<AnimationPlaygroundLowRecovery>();Assert.That(recovery,Is.Not.Null);
            Assert.That(motor.IsAnimationMotionOwner(recovery),Is.True);Assert.That(body.position,Is.EqualTo(position));Assert.That(body.useGravity,Is.True);
            Object.DestroyImmediate(roof);Physics.SyncTransforms();Tick(30);
            Assert.That(actor.GetComponent<AnimationPlaygroundLowRecovery>(),Is.Null);Assert.That(motor.IsActionConstrained,Is.False);
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));
        }
    }
}
