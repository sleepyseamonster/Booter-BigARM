using System;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    // Command/state contracts with explicitly injected completed contacts. No physics trajectory is simulated.
    public sealed class AnimationPlaygroundAirborneTests
    {
        private GameObject actor,floor;
        private Rigidbody body;
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundAirborneController airborne;
        private AnimationPlaygroundAirborneProfile profile;
        private AnimationPlaygroundResetRegistry registry;
        private PlayableGraph graph;
        private bool paused,recordedSupport;
        private float recordedImpact;
        private double clock;
        private static void Set(object value,string name,object field)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(value,field);
        private static object Call(object value,string method)=>value.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);
        [SetUp] public void Setup()
        {
            paused=false;recordedSupport=true;recordedImpact=0;clock=0;
            var source=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>(AnimationPlaygroundAirborneAuthoring.ProfilePath);
            Assert.That(source,Is.Not.Null);Assert.That(source.schema,Is.EqualTo(2));profile=Object.Instantiate(source);profile.qualified=true;
            actor=new GameObject("Continuous airborne contract fixture");actor.SetActive(false);actor.transform.position=new Vector3(68000,.91f,68000);
            motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();body=actor.GetComponent<Rigidbody>();
            var capsule=actor.GetComponent<CapsuleCollider>();capsule.height=1.82f;capsule.radius=.2f;capsule.center=Vector3.zero;
            var visual=new GameObject("Owned visual");visual.transform.SetParent(actor.transform,false);var animator=visual.AddComponent<Animator>();
            graph=PlayableGraph.Create("Continuous jump state fixture");var mixer=AnimationMixerPlayable.Create(graph,22);
            var idle=AnimationClipPlayable.Create(graph,profile.standing.takeoff);idle.SetSpeed(0);graph.Connect(idle,0,mixer,0);mixer.SetInputWeight(0,1);
            AnimationPlayableOutput.Create(graph,"One output",animator).SetSourcePlayable(mixer);graph.Play();
            Set(driver,"motor",motor);Set(driver,"visualInstance",visual);Set(driver,"visualLocalPosition",Vector3.zero);Set(driver,"playableGraph",graph);Set(driver,"mixer",mixer);
            registry=new AnimationPlaygroundResetRegistry();coordinator=actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,registry,new[]{actor.transform.position},()=>paused);
            airborne=actor.AddComponent<AnimationPlaygroundAirborneController>();airborne.Configure(motor,driver,coordinator,profile,registry,()=>paused,()=>false,()=>clock);
            airborne.ConfigureContinuationReview(AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/CampMotionContinuationReview.asset"));
            actor.SetActive(true);Set(motor,"<IsGrounded>k__BackingField",true);
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(68000,-.1f,68000);floor.transform.localScale=new Vector3(10,.2f,10);
            floor.AddComponent<TopDown3DGroundSurface>();Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup()
        {
            airborne?.DisposeOwnedServices();coordinator?.DisposeOwnedServices();if(graph.IsValid())graph.Destroy();
            Object.DestroyImmediate(actor);Object.DestroyImmediate(floor);Object.DestroyImmediate(profile);Physics.SyncTransforms();
        }
        private void Tick(int count=1)
        {
            for(var i=0;i<count;i++)
            {
                clock+=Time.fixedDeltaTime;
                Set(motor,"jumpContactTime",Time.fixedTimeAsDouble);
                Set(motor,"jumpSupportCollider",recordedSupport ? floor.GetComponent<Collider>() : null);
                Set(motor,"jumpSupportNormal",Vector3.up);Set(motor,"jumpContactDownwardSpeed",recordedImpact);
                // Synthetic FixedUpdate calls do not advance Unity's physics timestamp. This adapter
                // declares that a supplied landing record belongs after the recorded launch.
                if(recordedSupport && airborne.State!=AnimationPlaygroundAirborneState.Takeoff)Set(motor,"jumpLaunchTime",double.NegativeInfinity);
                Call(motor,"FixedUpdate");
            }
        }
        private void Launch(float speed=0,float heading=0)
        {
            motor.FacePlanarDirection(Vector3.forward);body.linearVelocity=Quaternion.Euler(0,heading,0)*Vector3.forward*speed;
            Assert.That(airborne.TryJump(),Is.True,airborne.LastFailure);
            FinishAnticipation();
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Rising));Assert.That(body.linearVelocity.y,Is.EqualTo(4.6f));
            recordedSupport=false;body.position+=Vector3.up;body.linearVelocity=new Vector3(body.linearVelocity.x,4,body.linearVelocity.z);Physics.SyncTransforms();Tick();
        }
        private void Land(float impact=3)
        {
            recordedSupport=true;recordedImpact=impact;body.position=new Vector3(68000,.91f,68000);
            body.linearVelocity=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up);Physics.SyncTransforms();Tick();
        }
        private void FinishAnticipation()
        {
            var before=clock;var ticks=0;
            while(airborne.State==AnimationPlaygroundAirborneState.Takeoff && ticks++<10)Tick();
            Assert.That(clock-before,Is.LessThanOrEqualTo(.10001));
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Rising));
        }
        [TestCase(0f,0f)] [TestCase(1.4f,0f)] [TestCase(3f,0f)] [TestCase(4f,0f)] [TestCase(5f,0f)] [TestCase(6f,0f)]
        [TestCase(5f,45f)] [TestCase(5f,90f)] [TestCase(4f,135f)] [TestCase(4f,180f)]
        public void EveryAcceptedEntryRetainsMomentumThroughPromptTakeoffAndSoftRunOut(float speed,float heading)
        {
            var expected=Quaternion.Euler(0,heading,0)*Vector3.forward*speed;var landings=0;airborne.Landed+=value=>landings++;
            Launch(speed,heading);Assert.That(Vector3.Distance(Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up),expected),Is.LessThan(.0001f));
            Land();Assert.That(landings,Is.EqualTo(1));Assert.That(motor.JumpUsesGroundedLocomotion,Is.True);
            Tick(20);Assert.That(airborne.Active,Is.False);Assert.That(motor.IsActionConstrained,Is.False);
            Assert.That(Vector3.Distance(Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up),expected),Is.LessThan(.0001f));
            Assert.That(graph.GetOutputCount(),Is.EqualTo(1));Assert.That(landings,Is.EqualTo(1));
        }
        [Test] public void AscentAndDescentKeepTheSameContinuousClipAndNeverManufactureContact()
        {
            var landings=0;airborne.Landed+=value=>landings++;Launch(3);
            var clip=((AnimationClipPlayable)driver.GetType().GetField("animationActionPlayable",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver)).GetAnimationClip();
            body.linearVelocity=Vector3.forward*3+Vector3.down*2;Tick(30);
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Falling));Assert.That(landings,Is.Zero);
            var current=((AnimationClipPlayable)driver.GetType().GetField("animationActionPlayable",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver)).GetAnimationClip();
            Assert.That(current,Is.SameAs(clip));Assert.That(current,Is.SameAs(profile.running.continuous));
        }
        [Test] public void ImpactSeverityUsesTheContactRecordInsteadOfPeakFallSpeed()
        {
            var impact=-1f;airborne.Landed+=value=>impact=value;Launch(5);
            body.linearVelocity=Vector3.down*9;Tick();body.linearVelocity=Vector3.down*2;Tick();Land(2);
            Assert.That(impact,Is.EqualTo(2));Assert.That(profile.LandingDamage(impact),Is.Zero);Assert.That(motor.JumpUsesGroundedLocomotion,Is.True);
        }
        [Test] public void SupportLossAfterLandingAllowsTheNextDistinctImpactExactlyOnce()
        {
            var landings=0;airborne.Landed+=value=>landings++;Launch();Land(3);Tick();Assert.That(landings,Is.EqualTo(1));
            recordedSupport=false;body.position+=Vector3.up;body.linearVelocity=Vector3.down*2;Tick();
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Falling));Land(2);Tick(20);
            Assert.That(landings,Is.EqualTo(2));Assert.That(airborne.Active,Is.False);
        }
        [Test] public void BufferedPressStartsTheNextJumpAtSoftTouchdown()
        {
            var landings=0;airborne.Landed+=value=>landings++;Launch(3);body.linearVelocity=Vector3.down*2;Tick();
            Assert.That(airborne.QueueJumpRequest(),Is.True);Tick(2);Land();
            Assert.That(landings,Is.EqualTo(1));Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Takeoff));
            FinishAnticipation();Assert.That(body.linearVelocity.y,Is.EqualTo(4.6f));
        }
        [Test] public void ExpiredOrDisarmedBufferedPressCannotCreateALaterJump()
        {
            Launch();Assert.That(airborne.QueueJumpRequest(),Is.True);clock+=.2;Land();Tick(20);
            Assert.That(airborne.Active,Is.False);
            Launch();Assert.That(airborne.QueueJumpRequest(),Is.True);airborne.DisarmJumpRequest();Land();Tick(20);
            Assert.That(airborne.Active,Is.False);
        }
        [Test] public void FreshCoyoteRequestCanReplacePassiveFallWithoutAGroundedImpulseCheck()
        {
            Tick();recordedSupport=false;Set(motor,"<IsGrounded>k__BackingField",false);body.linearVelocity=Vector3.down*.3f;Tick();
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Falling));
            Assert.That(airborne.QueueJumpRequest(),Is.True);Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Takeoff));
            FinishAnticipation();Assert.That(body.linearVelocity.y,Is.EqualTo(4.6f));
        }
        [Test] public void ExpiredCoyoteWindowCannotCreateADoubleJump()
        {
            Tick();recordedSupport=false;Set(motor,"<IsGrounded>k__BackingField",false);body.linearVelocity=Vector3.down*.3f;Tick();clock+=.2;
            Assert.That(airborne.TryJump(),Is.False);Assert.That(body.linearVelocity.y,Is.LessThan(0));
        }
        [Test] public void AcceptedGroundedRequestStillLaunchesAfterAnticipationLeavesTheEdge()
        {
            var requested=clock;Assert.That(airborne.TryJump(),Is.True);Tick();recordedSupport=false;
            Set(motor,"<IsGrounded>k__BackingField",false);body.linearVelocity=Vector3.down*.3f;FinishAnticipation();
            Assert.That(clock-requested,Is.LessThanOrEqualTo(.10001));
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Rising));Assert.That(body.linearVelocity.y,Is.EqualTo(4.6f));
        }
        [Test] public void PauseAndCancelKeepFixedHeightFlightAndRetainedMomentum()
        {
            Launch(6);var velocity=body.linearVelocity;var time=coordinator.PhysicalStageSeconds;paused=true;Tick(5);
            Assert.That(body.linearVelocity,Is.EqualTo(Vector3.zero));Assert.That(body.useGravity,Is.False);Assert.That(coordinator.PhysicalStageSeconds,Is.EqualTo(time));
            paused=false;Tick();Assert.That(body.linearVelocity,Is.EqualTo(velocity));Assert.That(body.useGravity,Is.True);
            Assert.That(airborne.TryCancel(),Is.True);Assert.That(body.linearVelocity,Is.EqualTo(velocity));Assert.That(coordinator.IsSequenceOwner(airborne),Is.True);
        }
        [Test] public void CancelBeforeImpulseReleasesWithoutBraking()
        {
            body.linearVelocity=Vector3.forward*5;Assert.That(airborne.TryJump(),Is.True);Tick();Assert.That(airborne.TryCancel(),Is.True);
            Assert.That(airborne.Active,Is.False);Assert.That(body.linearVelocity,Is.EqualTo(Vector3.forward*5));
        }
        [Test] public void HardContactHasItsOwnRecoveryAndDamageOccursOnce()
        {
            var damage=0f;var landings=0;airborne.Landed+=value=>{damage+=profile.LandingDamage(value);landings++;};Launch(5);Land(8);Tick();
            Assert.That(motor.JumpUsesGroundedLocomotion,Is.False);Assert.That(damage,Is.EqualTo(15));Tick(100);
            Assert.That(landings,Is.EqualTo(1));Assert.That(airborne.Active,Is.False);
        }
        [Test] public void ResetReleasesTheOwnerAndClearsPendingInput()
        {
            Launch(3);airborne.QueueJumpRequest();airborne.InvalidateForReset(1);
            Assert.That(coordinator.IsBusy,Is.False);Assert.That(motor.IsActionConstrained,Is.False);Assert.That(driver.PoseBlendOwned,Is.False);
            Assert.That(airborne.Active,Is.False);Assert.That(body.useGravity,Is.True);
        }
        [Test] public void ExplicitRollHandoffRestoresTheActionPolicyWithoutRepeatingTheImpact()
        {
            var lowProfile=Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationPlaygroundLowMotionProfile>(AnimationPlaygroundLowMotionAuthoring.ProfilePath));
            var projection=ScriptableObject.CreateInstance<AnimationPlaygroundFootProjection>();var low=actor.AddComponent<AnimationPlaygroundLowMotionController>();
            try
            {
                projection.model=lowProfile.targetModel;projection.bonePaths=new[]{""};projection.vertices=new[]{new AnimationPlaygroundFootVertex{points=new[]{
                    new AnimationPlaygroundSkinPoint{bone=0,position=Vector3.zero,weight=1}}}};
                lowProfile.bodyProjection=projection;lowProfile.qualified=true;Set(driver,"laboratoryFootProjection",projection);
                low.Configure(motor,driver,coordinator,lowProfile,registry,()=>paused,()=>false);
                var landings=0;airborne.Landed+=value=>landings++;airborne.LandingRecovery=value=>low.TryTakeLandingSequence(airborne,value);
                Launch(3);Land(3);
                Assert.That(low.Active,Is.True);Assert.That(airborne.Active,Is.False);Assert.That(coordinator.IsSequenceOwner(low),Is.True);
                Assert.That(motor.JumpLocomotionActive,Is.False);Assert.That(driver.JumpPresentationOwned,Is.False);Assert.That(landings,Is.EqualTo(1));
                Tick(180);Assert.That(low.Active,Is.False);Assert.That(landings,Is.EqualTo(1));Assert.That(graph.GetOutputCount(),Is.EqualTo(1));
            }
            finally{airborne.LandingRecovery=null;low.DisposeOwnedServices();Object.DestroyImmediate(low);Object.DestroyImmediate(lowProfile);Object.DestroyImmediate(projection);}
        }
        [Test] public void UnqualifiedOrCrouchedEntryCannotLaunch()
        {
            profile.qualified=false;Assert.That(airborne.TryJump(),Is.False);profile.qualified=true;
            Assert.That(motor.TrySetAnimationPosture(coordinator,AnimationPlaygroundPosture.Crouched,1.57f,.4f),Is.True);
            Assert.That(airborne.TryJump(),Is.False);Assert.That(coordinator.IsBusy,Is.False);
        }
        [TestCase(float.NaN)] [TestCase(.9f)] [TestCase(3.1f)]
        public void InvalidJumpGravityCannotQualify(float scale)
        {
            profile.gravityScale=scale;Assert.That(profile.Validate(out _),Is.False);Assert.That(airborne.TryJump(),Is.False);
        }
        [Test] public void ControllerUsesBoostedGravityForJumpAndRestoresWorldGravityForANewLedgeFall()
        {
            profile.gravityScale=1.6f;Launch(3);
            Assert.That(motor.JumpGravityMagnitude,Is.EqualTo(-Physics.gravity.y*1.6f).Within(.00001f));
            Land(3);recordedSupport=false;body.position+=Vector3.up*2;body.linearVelocity=Vector3.down;Tick();
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Falling));
            Assert.That(motor.JumpGravityMagnitude,Is.EqualTo(-Physics.gravity.y).Within(.00001f));
        }
        [Test] public void PresentationLossReleasesOnlyTheJumpAndPreservesGravity()
        {
            Launch(3);Call(driver,"OnDestroy");Tick();
            Assert.That(coordinator.IsBusy,Is.False);Assert.That(motor.IsActionConstrained,Is.False);Assert.That(body.useGravity,Is.True);
        }
        [Test] public void DisabledMotorReleasesTheJumpWithoutAnotherMotorTick()
        {
            Launch(3);motor.enabled=false;Call(airborne,"Update");
            Assert.That(airborne.Active,Is.False);Assert.That(coordinator.IsBusy,Is.False);
            Assert.That(driver.AnimationPresentationOwned,Is.False);Assert.That(body.useGravity,Is.True);
        }
        [Test] public void DisabledMotorCannotAcquireANewJump()
        {
            motor.enabled=false;Assert.That(airborne.TryJump(),Is.False);
            Assert.That(coordinator.IsBusy,Is.False);Assert.That(driver.AnimationPresentationOwned,Is.False);
        }
        [Test] public void JumpPresentationRejectionLeavesRunningVelocityAndMotorOwnershipIntact()
        {
            var foreign=new object();Assert.That(driver.TryBeginAnimationPresentation(foreign,profile.standing.takeoff),Is.True);body.linearVelocity=Vector3.forward*5;
            var binding=new AnimationPlaygroundMotionBinding{sourceId="Jump rollback contract",clip=profile.standing.continuous,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
            Assert.That(coordinator.TryBeginPhysicalSequence(airborne,binding,AnimationPlaygroundMotionMode.Jump,null,
                time=>new AnimationPlaygroundMotorIntent(Vector3.forward*5,Vector3.forward),null,policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.False);
            Assert.That(body.linearVelocity,Is.EqualTo(Vector3.forward*5));Assert.That(motor.IsActionConstrained,Is.False);
            Assert.That(driver.IsAnimationPresentationOwner(foreign),Is.True);driver.EndAnimationPresentation(foreign);
        }
        [Test] public void JumpMotorRejectionDoesNotAcquireOrResetPresentation()
        {
            var foreign=new object();motor.TryBeginActionConstraint(foreign);body.linearVelocity=Vector3.right*4;
            var binding=new AnimationPlaygroundMotionBinding{sourceId="Busy motor contract",clip=profile.standing.continuous,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
            Assert.That(coordinator.TryBeginPhysicalSequence(airborne,binding,AnimationPlaygroundMotionMode.Jump,null,
                time=>new AnimationPlaygroundMotorIntent(Vector3.right*4,Vector3.forward),null,policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.False);
            Assert.That(driver.AnimationPresentationOwned,Is.False);Assert.That(body.linearVelocity,Is.EqualTo(Vector3.right*4));motor.EndActionConstraint(foreign);
        }
        [Test] public void ContinuousSourceKeepsBodyMotionAndGroundedSolesWithoutAnAirborneFloor()
        {
            var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
            foreach(var variant in new[]{profile.standing,profile.running,profile.unarmed})
            {
                Assert.That(variant.HasContinuousSource,Is.True);
                var measured=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),AssetDatabase.GetAssetPath(variant.continuous),bindings.visualScale,bindings.visualOffset,60);
                var min=float.PositiveInfinity;var max=float.NegativeInfinity;
                foreach(var frame in measured.frames)
                {
                    min=Mathf.Min(min,frame.hips.y);max=Mathf.Max(max,frame.hips.y);
                    if(frame.seconds<=variant.liftTime || frame.seconds>=variant.contactTime)
                        Assert.That(Mathf.Min(frame.leftSole.y,frame.rightSole.y),Is.GreaterThan(-.913f));
                }
                Assert.That(measured.finite,Is.True);Assert.That(max-min,Is.GreaterThan(.02f));
            }
        }
        [Test] public void SoftRunOutUsesOrdinaryHeldMovementAndNeutralDeceleration()
        {
            var inputObject=new GameObject("Recorded input contract");inputObject.SetActive(false);
            try
            {
                var input=inputObject.AddComponent<TopDown3DInputRouter>();input.enabled=false;
                Set(input,"<MoveValue>k__BackingField",Vector2.up);motor.Configure(input,actor.transform);
                motor.ConfigureLaboratorySpeeds(coordinator,1.4f,3,6);Launch(3);Land();Tick();
                Assert.That(body.linearVelocity.z,Is.EqualTo(3).Within(.0001f));
                Set(input,"<MoveValue>k__BackingField",Vector2.zero);Tick();
                Assert.That(body.linearVelocity.z,Is.GreaterThan(0).And.LessThan(3));Tick(30);
                Assert.That(body.linearVelocity.z,Is.EqualTo(0).Within(.0001f));Assert.That(airborne.Active,Is.False);
            }
            finally{Object.DestroyImmediate(inputObject);}
        }
        [Test] public void HeldLTFacingAndLookAheadSurviveTakeoffFlightAndSoftRelease()
        {
            using var f=new AnimationPlaygroundStandingStrafeTests.InputFixture();
            motor.Configure(f.Router,actor.transform);motor.ConfigureLaboratorySpeeds(coordinator,1.4f,3,6);
            motor.ConfigureLaboratoryStandingStrafe(coordinator,AnimationPlaygroundDirectionalTests.Profile(),()=>true);
            void Apply(Vector3 direction)
            {
                var args=new object[]{direction};
                motor.GetType().GetMethod("ApplyStandingStrafeIntent",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motor,args);
            }
            Apply(Vector3.zero);f.PadState(new GamepadState{leftTrigger=1});
            Set(f.Router,"<MoveValue>k__BackingField",Vector2.right);Apply(Vector3.right);
            Assert.That(motor.LaboratoryStandingFacingLocked,Is.True);var facing=motor.JumpLaunchFacing;
            Launch(3,90);Assert.That(motor.LaboratoryStandingFacingLocked,Is.True);
            Assert.That(Vector3.Angle(motor.JumpLaunchFacing,facing),Is.LessThan(.001f));
            Assert.That(f.Router.CameraLookAheadHeld,Is.True);Land();Tick(20);
            Assert.That(airborne.Active,Is.False);Assert.That(motor.LaboratoryStandingFacingLocked,Is.True);
            Assert.That(f.Router.LaboratoryStandingFacingHeld,Is.True);Assert.That(f.Router.CameraLookAheadHeld,Is.True);
            f.Router.AnimationCommandsDisarmed+=airborne.DisarmJumpRequest;
            try
            {
                recordedSupport=false;Set(motor,"<IsGrounded>k__BackingField",false);
                Assert.That(airborne.QueueJumpRequest(),Is.True);f.Router.EnterSystemMenuMode();
                Assert.That((bool)airborne.GetType().GetField("pendingJump",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(airborne),Is.False);
                Call(motor,"GuardStandingStrafeOwner");Assert.That(motor.LaboratoryStandingFacingLocked,Is.False);
            }
            finally{f.Router.AnimationCommandsDisarmed-=airborne.DisarmJumpRequest;}
        }
    }
}
