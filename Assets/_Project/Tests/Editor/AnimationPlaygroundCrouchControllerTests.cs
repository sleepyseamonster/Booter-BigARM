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
    public sealed class AnimationPlaygroundCrouchControllerTests
    {
        private GameObject actor,roof,floor,visual;
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundCrouchController crouch;
        private AnimationPlaygroundCrouchProfile profile;
        private AnimationPlaygroundResetRegistry registry;
        private AnimationMixerPlayable mixer;
        private PlayableGraph graph;
        private bool paused,busy;
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private static object Call(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);

        [SetUp] public void Setup()
        {
            var source=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCrouchProfile>(AnimationPlaygroundMobilityAuthoring.CrouchProfilePath);
            Assert.That(source,Is.Not.Null,"Restore the private crouch dependency before running this gate.");
            profile=Object.Instantiate(source);
            // Synthetic contacts isolate ownership/clearance. They do not qualify the source motion.
            profile.contactsQualified=true;
            profile.groundingQualified=true;
            if(profile.grounding==null || profile.grounding.Length==0)profile.grounding=AnimationPlaygroundCrouchPoseQualification.ZeroGrounding();
            foreach(var entry in profile.entries)
            {entry.leftContacts=new[]{.25f,.75f};entry.rightContacts=new[]{0f,.5f};}
            actor=new GameObject("Crouch controller fixture");actor.SetActive(false);actor.transform.position=new Vector3(71000,.91f,71000);
            motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();
            var shape=actor.GetComponent<CapsuleCollider>();shape.height=1.82f;shape.radius=.2f;shape.center=Vector3.zero;
            visual=new GameObject("Owned crouch visual");visual.transform.SetParent(actor.transform,false);var animator=visual.AddComponent<Animator>();
            graph=PlayableGraph.Create("Crouch ownership fixture");mixer=AnimationMixerPlayable.Create(graph,22);
            var idlePlayable=AnimationClipPlayable.Create(graph,profile.Get(AnimationPlaygroundCrouchRole.Idle).clip);
            idlePlayable.SetSpeed(0);graph.Connect(idlePlayable,0,mixer,0);mixer.SetInputWeight(0,1);
            var output=AnimationPlayableOutput.Create(graph,"Only output",animator);output.SetSourcePlayable(mixer);graph.Play();
            Set(driver,"motor",motor);Set(driver,"visualInstance",visual);Set(driver,"visualLocalPosition",Vector3.zero);
            Set(driver,"playableGraph",graph);Set(driver,"mixer",mixer);
            registry=new AnimationPlaygroundResetRegistry();coordinator=actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,registry,new[]{actor.transform.position},()=>paused);
            crouch=actor.AddComponent<AnimationPlaygroundCrouchController>();
            crouch.Configure(motor,driver,coordinator,profile,registry,()=>paused,()=>busy);
            actor.SetActive(true);Set(motor,"<IsGrounded>k__BackingField",true);
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=actor.transform.position+Vector3.down*1.01f;
            floor.transform.localScale=new Vector3(6,.2f,6);floor.AddComponent<TopDown3DGroundSurface>();Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup()
        {
            crouch?.DisposeOwnedServices();coordinator?.DisposeOwnedServices();
            if(graph.IsValid())graph.Destroy();
            Object.DestroyImmediate(actor);Object.DestroyImmediate(roof);Object.DestroyImmediate(floor);Object.DestroyImmediate(profile);
            Physics.SyncTransforms();
        }
        private void FinishTransition()
        {
            for(var i=0;i<400 && (coordinator.IsBusy || crouch.Transitioning);i++)
            {
                if(coordinator.IsBusy){Call(coordinator,"PrepareMotion");Call(coordinator,"CommitEffects");}
                else Call(crouch,"AdvanceLocomotion");
            }
            Assert.That(coordinator.IsBusy,Is.False);
            Assert.That(crouch.Transitioning,Is.False);
        }
        private void Enter(){Assert.That(crouch.TryToggle(),Is.True,crouch.LastFailure);FinishTransition();}
        private void PutRoof()
        {
            var fixture=AssetDatabase.LoadAssetAtPath<GameObject>(AnimationPlaygroundStationAuthoring.StationPath(3));
            var authored=fixture.transform.Find("Tunnel ceiling");Assert.That(authored,Is.Not.Null);
            roof=GameObject.CreatePrimitive(PrimitiveType.Cube);roof.transform.position=new Vector3(71000,authored.localPosition.y,71000);
            roof.transform.localScale=authored.localScale;Physics.SyncTransforms();
            Assert.That(authored.localPosition.y-authored.localScale.y*.5f,Is.GreaterThan(profile.crouchHeight+.05f));
        }

        [Test] public void DraftPauseAirborneAndOtherActivitiesCannotStartPosture()
        {
            profile.contactsQualified=false;Assert.That(crouch.TryToggle(),Is.False);Assert.That(coordinator.IsBusy,Is.False);
            profile.contactsQualified=true;paused=true;Assert.That(crouch.TryToggle(),Is.False);paused=false;
            busy=true;Assert.That(crouch.TryToggle(),Is.False);busy=false;
            Set(motor,"<IsGrounded>k__BackingField",false);Assert.That(crouch.TryToggle(),Is.False);
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));
        }
        [Test] public void EnterSustainsOneOutputAndExitsThroughTheSharedActionOwner()
        {
            Enter();Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Crouched));
            Assert.That(motor.IsActionConstrained,Is.False);Assert.That(driver.CrouchPresentationOwned,Is.True);
            Assert.That(motor.LaboratoryStandingActionsAllowed,Is.False);
            Assert.That(actor.GetComponent<CapsuleCollider>().height,Is.EqualTo(profile.crouchHeight));
            Assert.That(actor.GetComponent<CapsuleCollider>().radius,Is.EqualTo(profile.crouchRadius));
            driver.SetCrouchPose(crouch,new AnimationPlaygroundCrouchPose(new Vector2(1,1).normalized,1,.3f,0,true));
            Assert.That(Call(driver,"PresentCrouchPose"),Is.EqualTo(true));Assert.That(graph.GetOutputCount(),Is.EqualTo(1));
            for(var i=0;i<22;i++)Assert.That(mixer.GetInputWeight(i),Is.Zero);
            Assert.That(mixer.GetInputWeight(22),Is.EqualTo(1));var nested=(AnimationMixerPlayable)mixer.GetInput(22);
            var sum=0f;for(var i=0;i<5;i++){sum+=nested.GetInputWeight(i);Assert.That(nested.GetInput(i).GetSpeed(),Is.Zero);}
            Assert.That(sum,Is.EqualTo(1).Within(.00001f));
            Assert.That(crouch.TryToggle(),Is.True,crouch.LastFailure);FinishTransition();
            Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Standing));
            Assert.That(driver.CrouchPresentationOwned,Is.False);Assert.That(motor.LaboratoryStandingActionsAllowed,Is.True);
            Assert.That(actor.GetComponent<CapsuleCollider>().height,Is.EqualTo(1.82f));
            Assert.That(actor.GetComponent<CapsuleCollider>().radius,Is.EqualTo(.2f));
        }
        [Test] public void CeilingBlocksExitWithoutGrowingTheCapsuleOrLosingThePose()
        {
            Enter();PutRoof();var bottom=actor.GetComponent<CapsuleCollider>().center.y-actor.GetComponent<CapsuleCollider>().height*.5f;
            Assert.That(crouch.TryToggle(),Is.False);Assert.That(crouch.LastFailure,Does.Contain("obstruction"));
            Assert.That(crouch.TryCancel(),Is.False);Assert.That(driver.CrouchPresentationOwned,Is.True);
            var shape=actor.GetComponent<CapsuleCollider>();Assert.That(shape.height,Is.EqualTo(profile.crouchHeight));
            Assert.That(shape.center.y-shape.height*.5f,Is.EqualTo(bottom).Within(.00001f));
            Object.DestroyImmediate(roof);Physics.SyncTransforms();Assert.That(crouch.TryCancel(),Is.True);FinishTransition();
        }
        [Test] public void CancelDuringEntryFinishesSafelyAndResetInvalidatesEveryPoseBranch()
        {
            Assert.That(crouch.TryToggle(),Is.True);Assert.That(crouch.TryCancel(),Is.True);FinishTransition();
            Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Standing));
            Enter();Assert.That(registry.TryReset(),Is.True,registry.LastFailure);
            Assert.That(crouch.Active,Is.False);Assert.That(driver.CrouchPresentationOwned,Is.False);
            Assert.That(driver.AnimationPresentationOwned,Is.False);Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));
            Assert.That(actor.transform.position,Is.EqualTo(new Vector3(71000,.91f,71000)));
        }
        [Test] public void ForeignOwnersAndInvalidPoseCannotStealTheSustainedBranch()
        {
            Enter();var stranger=new object();
            Assert.That(driver.TryBeginCrouchPresentation(stranger,profile),Is.False);
            Assert.That(driver.SetCrouchPose(stranger,default),Is.False);Assert.That(driver.EndCrouchPresentation(stranger),Is.False);
            Assert.That(driver.SetCrouchPose(crouch,new AnimationPlaygroundCrouchPose(Vector2.zero,1,float.NaN,0,true)),Is.False);
            Assert.That(motor.SetLaboratoryCrouchFacing(stranger,Vector3.right,true),Is.False);
        }
        [Test] public void DirectionalCrouchRetainsEntryFacingAndStillUsesTheExistingMotor()
        {
            var input=actor.AddComponent<TopDown3DInputRouter>();
            var camera=new GameObject("Movement basis");camera.transform.SetParent(actor.transform,false);
            motor.Configure(input,camera.transform);motor.BlockUnqualifiedLaboratoryTraversal(coordinator);
            Enter();Set(input,"<MoveValue>k__BackingField",Vector2.right);
            actor.GetComponent<Rigidbody>().position+=Vector3.right*.01f;Physics.SyncTransforms();
            Call(motor,"FixedUpdate");
            Assert.That(motor.Velocity.x,Is.GreaterThan(0));Assert.That(motor.FacingDirection,Is.EqualTo(Vector3.forward));
            Assert.That(motor.IsGrounded,Is.True);Assert.That(motor.IsActionConstrained,Is.False);
            Assert.That(Call(driver,"PresentCrouchPose"),Is.EqualTo(true));
            var nested=(AnimationMixerPlayable)mixer.GetInput(22);
            Assert.That(nested.GetInputWeight(4),Is.GreaterThan(0));Assert.That(nested.GetInputWeight(1),Is.Zero);
        }
        [Test] public void BlockedResetRetainsAClearCrouchPoseUntilASafeReturnBecomesAvailable()
        {
            Enter();PutRoof();Assert.That(registry.TryReset(),Is.False);
            Assert.That(crouch.Active,Is.True);Assert.That(driver.CrouchPresentationOwned,Is.True);
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Crouched));
            Assert.That(actor.GetComponent<CapsuleCollider>().height,Is.EqualTo(profile.crouchHeight));
            Assert.That(crouch.TryToggle(),Is.False);Assert.That(crouch.TryCancel(),Is.False);
            Object.DestroyImmediate(roof);Physics.SyncTransforms();
            Assert.That(registry.TryReset(),Is.True,registry.LastFailure);Assert.That(crouch.Active,Is.False);
            Assert.That(driver.CrouchPresentationOwned,Is.False);
        }
        [Test] public void ACloseWallPreventsExpandingIntoTheWiderCrouchEnvelope()
        {
            roof=GameObject.CreatePrimitive(PrimitiveType.Cube);roof.transform.position=actor.transform.position+Vector3.right*.4f;
            roof.transform.localScale=new Vector3(.1f,3,3);Physics.SyncTransforms();
            Assert.That(motor.IsStandingAnimationShapeClear(),Is.True);
            Assert.That(crouch.TryToggle(),Is.False);Assert.That(crouch.LastFailure,Does.Contain("more space"));
            Assert.That(coordinator.IsBusy,Is.False);Assert.That(driver.CrouchPresentationOwned,Is.False);
            Assert.That(actor.GetComponent<CapsuleCollider>().radius,Is.EqualTo(.2f));
        }
        [Test] public void CommandedVelocityWithoutDisplacementCannotAdvanceCrouchSteps()
        {
            var input=actor.AddComponent<TopDown3DInputRouter>();var camera=new GameObject("Blocked movement basis");
            camera.transform.SetParent(actor.transform,false);motor.Configure(input,camera.transform);motor.BlockUnqualifiedLaboratoryTraversal(coordinator);
            Enter();Set(input,"<MoveValue>k__BackingField",Vector2.up);
            for(var i=0;i<20;i++)Call(motor,"FixedUpdate");
            Assert.That(motor.Velocity.z,Is.GreaterThan(0));Assert.That(motor.LaboratoryMeasuredVelocity,Is.EqualTo(Vector3.zero));
            Assert.That(Call(driver,"PresentCrouchPose"),Is.EqualTo(true));var nested=(AnimationMixerPlayable)mixer.GetInput(22);
            Assert.That(nested.GetInputWeight(0),Is.EqualTo(1));Assert.That(nested.GetInputWeight(1),Is.Zero);
        }
        [Test] public void DisposalBelowACeilingTransfersTheSafePoseUntilStandingIsClear()
        {
            Enter();PutRoof();var position=actor.transform.position;
            crouch.DisposeOwnedServices();coordinator.DisposeOwnedServices();
            Assert.That(driver.CrouchRecoveryOwned,Is.True);Assert.That(driver.CrouchPresentationOwned,Is.True);
            Assert.That(motor.IsAnimationPostureOwner(driver),Is.True);Assert.That(motor.LaboratoryStandingActionsAllowed,Is.False);
            Assert.That(driver.TryBeginAnimationPresentation(new object(),profile.Get(AnimationPlaygroundCrouchRole.Exit).clip),Is.False);
            Call(driver,"AdvanceCrouchRecovery");Assert.That(driver.CrouchRecoveryOwned,Is.True);
            Assert.That(actor.transform.position,Is.EqualTo(position));
            Object.DestroyImmediate(roof);Physics.SyncTransforms();Call(driver,"AdvanceCrouchRecovery");
            Assert.That(driver.CrouchRecoveryOwned,Is.False);Assert.That(driver.CrouchPresentationOwned,Is.False);
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));
            Assert.That(motor.IsAnimationPostureOwner(driver),Is.False);Assert.That(actor.transform.position,Is.EqualTo(position));
        }
        [Test] public void DisableBelowACeilingSuspendsPostureUntilTheControllerResumes()
        {
            Enter();PutRoof();Call(crouch,"OnDisable");
            Assert.That(driver.CrouchPresentationOwned,Is.True);Assert.That(crouch.TryToggle(),Is.False);
            Call(crouch,"OnEnable");Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Crouched));
            Assert.That(crouch.TryToggle(),Is.False);Assert.That(crouch.LastFailure,Does.Contain("obstruction"));
            Object.DestroyImmediate(roof);Physics.SyncTransforms();Assert.That(crouch.TryToggle(),Is.True);FinishTransition();
            Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Standing));
        }
        [Test] public void DisableInClearSpaceRestoresStandingAndKeepsOrdinaryMovementAvailable()
        {
            var input=actor.AddComponent<TopDown3DInputRouter>();var camera=new GameObject("Clear movement basis");
            camera.transform.SetParent(actor.transform,false);motor.Configure(input,camera.transform);motor.BlockUnqualifiedLaboratoryTraversal(coordinator);
            Enter();Call(crouch,"OnDisable");Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Standing));
            Assert.That(driver.CrouchPresentationOwned,Is.False);Assert.That(crouch.TryCancel(),Is.False);
            Set(input,"<MoveValue>k__BackingField",Vector2.right);Call(motor,"FixedUpdate");Assert.That(motor.Velocity.x,Is.GreaterThan(0));
        }
        [Test] public void SettlingBlendPausesAndResetReleasesItsGraphAndMotorLease()
        {
            Assert.That(crouch.TryToggle(),Is.True);
            for(var i=0;i<100 && coordinator.IsBusy;i++){Call(coordinator,"PrepareMotion");Call(coordinator,"CommitEffects");}
            Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.SettlingCrouch));
            Assert.That(driver.PoseBlendOwned,Is.True);Assert.That(motor.IsActionConstrained,Is.True);
            paused=true;for(var i=0;i<20;i++)Call(crouch,"AdvanceLocomotion");
            Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.SettlingCrouch));
            Assert.That(registry.TryReset(),Is.True,registry.LastFailure);
            Assert.That(driver.PoseBlendOwned,Is.False);Assert.That(motor.IsActionConstrained,Is.False);
            Assert.That(crouch.State,Is.EqualTo(AnimationPlaygroundCrouchState.Standing));
        }
    }
}
