using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundAirborneState { Standing,Takeoff,Rising,Falling,Approach,Landing,Settling }
    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundAirborneController : MonoBehaviour,IAnimationPlaygroundResetParticipant
    {
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundAirborneProfile profile;
        private AnimationPlaygroundResetRegistry registry;
        private Func<bool> paused,otherActivity;
        private Func<double> gameplayTime;
        private AnimationPlaygroundJumpVariant variant;
        private Vector3 facing,planar;
        private bool launched,disposed,genericFall,hardLanding,pendingJump;
        private float launchSpeed,airLimit,flightPhase,impactSpeed,flightGravityScale=1;
        private double launchStageTime,fallStageTime,landingStageTime,lastGroundedTime=double.NegativeInfinity,pendingUntil;
        private ulong lastTouchdown;
        public AnimationPlaygroundAirborneState State {get;private set;}
        public bool Active=>State!=AnimationPlaygroundAirborneState.Standing;
        public bool Ready=>profile!=null && profile.schema==2 && profile.Validate(out _);
        public bool CanYieldToGroundedAction=>!disposed && isActiveAndEnabled && Ready && paused?.Invoke()!=true
            && State==AnimationPlaygroundAirborneState.Landing && !hardLanding
            && motor!=null && motor.isActiveAndEnabled && motor.IsGrounded
            && coordinator!=null && coordinator.isActiveAndEnabled && coordinator.IsSequenceOwner(this)
            && driver!=null && driver.isActiveAndEnabled && driver.IsAnimationPresentationOwner(coordinator);
        public string LastFailure {get;private set;}
        public event Action<float> Landed;
        public Func<float,bool> LandingRecovery {get;set;}
        private double Now=>gameplayTime?.Invoke() ?? Time.timeAsDouble;
        public string ResetId=>"CampLoop.Booter.Jump";
        public AnimationPlaygroundResetStage ResetStage=>AnimationPlaygroundResetStage.Actor;

        public void ConfigureContinuationReview(AnimationPlaygroundContinuationProfile review)
        {
            if(motor==null || Active || review==null || !float.IsFinite(review.maximumAirSpeed) || review.maximumAirSpeed<6 || review.maximumAirSpeed>6.15f)
                throw new InvalidOperationException("Configure airborne continuation before acquiring a jump.");
            airLimit=review.maximumAirSpeed;
        }
        public void Configure(TopDown3DPlayerMotor value,TopDown3DPlayerAnimationDriver presentation,
            AnimationPlaygroundActionCoordinator actions,AnimationPlaygroundAirborneProfile calibration,
            AnimationPlaygroundResetRegistry resets,Func<bool> shouldPause,Func<bool> activityBusy,Func<double> clock=null)
        {
            if(motor!=null || value==null || presentation==null || actions==null || calibration==null || resets==null
                || !calibration.Validate(out _,false))throw new ArgumentException("Configure one paired airborne family.");
            motor=value;driver=presentation;coordinator=actions;profile=calibration;registry=resets;paused=shouldPause;otherActivity=activityBusy;gameplayTime=clock;
            airLimit=profile.maximumPlanarSpeed;
            registry.Register(this);coordinator.ActionCompleted+=StageCompleted;
            motor.AnimationMotionAdvanced+=ObserveMotion;motor.AnimationLocomotionAdvanced+=ObserveLocomotion;
        }
        private void Update()
        {
            // A disabled motor no longer sends fixed-step callbacks. Release our
            // sequence here as well so its presentation cannot retain the actor.
            if(Active && (motor==null || !motor.isActiveAndEnabled || driver==null || !driver.isActiveAndEnabled
                || coordinator==null || !coordinator.isActiveAndEnabled || !coordinator.IsSequenceOwner(this)
                || !motor.IsAnimationMotionOwner(coordinator) || !driver.IsAnimationPresentationOwner(coordinator)))
            {Finish();DisarmJumpRequest();return;}
            if(paused?.Invoke()==true)DisarmJumpRequest();
            else if(pendingJump && Now>pendingUntil)pendingJump=false;
        }
        public void DisarmJumpRequest()
        {pendingJump=false;lastGroundedTime=double.NegativeInfinity;}
        public bool QueueJumpRequest()
        {
            if(disposed || !isActiveAndEnabled || paused?.Invoke()==true || !Ready)return false;
            pendingJump=true;pendingUntil=Now+AnimationPlaygroundJumpTiming.InputGraceSeconds;
            TryConsumeJumpRequest();return true;
        }
        private void TryConsumeJumpRequest()
        {
            if(!pendingJump)return;
            if(Now>pendingUntil){pendingJump=false;return;}
            if(CanStartJump())TryJump();
        }
        private bool CanStartJump()
        {
            var passiveFall=Active && genericFall && !launched && Now-lastGroundedTime<=AnimationPlaygroundJumpTiming.InputGraceSeconds;
            var softLanding=State==AnimationPlaygroundAirborneState.Landing && !hardLanding;
            return motor!=null && motor.isActiveAndEnabled && driver!=null && driver.isActiveAndEnabled
                && coordinator!=null && coordinator.isActiveAndEnabled && Ready && paused?.Invoke()!=true && otherActivity?.Invoke()!=true
                && motor.AnimationPosture==AnimationPlaygroundPosture.Standing
                && (!Active && !coordinator.IsBusy && !motor.IsActionConstrained || (passiveFall || softLanding) && coordinator.IsSequenceOwner(this))
                && (motor.IsGrounded || Now-lastGroundedTime<=AnimationPlaygroundJumpTiming.InputGraceSeconds);
        }
        public bool TryJump(bool useUnarmed=false)
        {
            LastFailure=null;
            if(disposed || !isActiveAndEnabled || !CanStartJump())return Fail("Jumping requires ready standing support or a fresh coyote window.");
            if(Active)
            {
                if(!driver.TryCapturePoseBlend(this,.08f,true))return Fail("The buffered jump could not preserve its outgoing pose.");
                Finish(true);
            }
            planar=Vector3.ProjectOnPlane(motor.Velocity,Vector3.up);launchSpeed=planar.magnitude;
            facing=motor.JumpLaunchFacing;
            variant=useUnarmed ? profile.unarmed : launchSpeed>=profile.runningThreshold ? profile.running : profile.standing;
            if(!useUnarmed && planar.sqrMagnitude>.0001f && Vector3.Dot(planar.normalized,facing)<.5f)variant=profile.standing;
            launched=false;genericFall=false;hardLanding=false;flightPhase=0;impactSpeed=0;
            flightGravityScale=profile.gravityScale;
            var markers=new[]{new AnimationPlaygroundMarker(1,AnimationPlaygroundJumpTiming.ImpulseMarkerSeconds(Time.fixedDeltaTime))};
            if(!Begin(variant.continuous,AnimationPlaygroundMotionMode.Jump,AnimationPlaygroundAirborneState.Takeoff,markers,key=>{
                if(launched || !motor.TryAnimationTakeoff(coordinator,variant.upwardSpeed,acceptedCoyote:true))return false;
                launched=true;facing=motor.JumpLaunchFacing;launchStageTime=coordinator.PhysicalStageSeconds;
                State=AnimationPlaygroundAirborneState.Rising;return true;
            }))return Fail("The jump could not acquire the player.");
            pendingJump=false;return true;
        }
        private AnimationPlaygroundMotionBinding Binding(AnimationClip clip)=>new(){sourceId="ContinuousAirborne."+clip.name,
            clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
        private bool Begin(AnimationClip clip,AnimationPlaygroundMotionMode mode,AnimationPlaygroundAirborneState next,
            AnimationPlaygroundMarker[] markers=null,Func<AnimationPlaygroundEffectKey,bool> effect=null,bool transition=false)
        {
            if(transition && !driver.TryCapturePoseBlend(this,.08f,true))return false;
            var success=coordinator.IsSequenceOwner(this)
                ? coordinator.TrySwitchPhysicalSequence(this,Binding(clip),mode,markers,Intent,effect,PoseTime,clip.isLooping)
                : coordinator.TryBeginPhysicalSequence(this,Binding(clip),mode,markers,Intent,effect,PoseTime,clip.isLooping,
                    policy:AnimationPlaygroundMotionPolicy.JumpLocomotion);
            if(!success){driver.EndPoseBlend(this);return false;}
            if(!motor.SetJumpGravityScale(coordinator,flightGravityScale))
            {coordinator.CancelPhysicalSequence(this);driver.EndPoseBlend(this);return false;}
            if(!driver.ConfigureJumpPresentation(coordinator,next==AnimationPlaygroundAirborneState.Takeoff ? 0 : 1,
                next==AnimationPlaygroundAirborneState.Takeoff ? variant.liftTime : -1))
            {coordinator.CancelPhysicalSequence(this);driver.EndPoseBlend(this);return false;}
            State=next;
            motor.SetJumpGroundedLocomotion(coordinator,next==AnimationPlaygroundAirborneState.Takeoff && motor.IsGrounded);
            driver.SetJumpPresentationState(coordinator,next==AnimationPlaygroundAirborneState.Takeoff ? 0 : 1,
                next==AnimationPlaygroundAirborneState.Takeoff && motor.IsGrounded,false);
            return true;
        }
        private AnimationPlaygroundMotorIntent Intent(double seconds)
        {
            var isPaused=paused?.Invoke()==true;
            driver.SetPoseBlendElapsed(this,coordinator.PhysicalStageSeconds);
            if(!isPaused)
            {
                planar=Vector3.ProjectOnPlane(motor.JumpControlVelocity,Vector3.up);
                if(State!=AnimationPlaygroundAirborneState.Takeoff && State!=AnimationPlaygroundAirborneState.Landing)
                    planar=AnimationPlaygroundAirControl.Advance(planar,motor.LaboratoryMoveDirection,launchSpeed,
                        airLimit,profile.airControlAcceleration,Time.fixedDeltaTime);
                AdvanceFlightPhase();
            }
            var grounded=State==AnimationPlaygroundAirborneState.Takeoff && motor.IsGrounded
                || State==AnimationPlaygroundAirborneState.Landing;
            motor.SetJumpGroundedLocomotion(coordinator,grounded && !hardLanding);
            var weight=State==AnimationPlaygroundAirborneState.Landing && !hardLanding
                ? 1-Mathf.SmoothStep(0,1,(float)(coordinator.PhysicalStageSeconds-landingStageTime)/AnimationPlaygroundJumpTiming.SoftLandingSeconds)
                : !genericFall ? Mathf.SmoothStep(0,1,(float)coordinator.PhysicalStageSeconds/AnimationPlaygroundJumpTiming.EntryBlendSeconds) : 1;
            driver.SetJumpPresentationState(coordinator,weight,grounded,isPaused);
            if(!isPaused)
            {
                if(State==AnimationPlaygroundAirborneState.Falling && motor.TryPredictJumpSupport(coordinator,.65f,out var predicted))
                {
                    var closing=-Vector3.Dot(motor.Velocity,predicted.normal)/Mathf.Max(.01f,predicted.normal.y);
                    var remaining=AnimationPlaygroundJumpTiming.TimeToContact(predicted.distance,Mathf.Max(0,closing),motor.JumpGravityMagnitude);
                    var supportWeight=1-Mathf.SmoothStep(0,1,remaining/.08f);
                    driver.SetJumpSupportPrediction(coordinator,predicted.collider,new Plane(predicted.normal,predicted.point),supportWeight);
                }
                else driver.SetJumpSupportPrediction(coordinator,null,default,0);
            }
            return new AnimationPlaygroundMotorIntent(hardLanding ? Vector3.zero : planar,facing,paused:isPaused);
        }
        private void AdvanceFlightPhase()
        {
            if(!launched || variant==null || State==AnimationPlaygroundAirborneState.Landing || genericFall)return;
            var half=variant.upwardSpeed/motor.JumpGravityMagnitude;
            var elapsed=Mathf.Max(0,(float)(coordinator.PhysicalStageSeconds-launchStageTime));
            var progress=.5f*Mathf.Clamp01(1-motor.Velocity.y/variant.upwardSpeed);
            if(motor.Velocity.y<=0)
            {
                if(State!=AnimationPlaygroundAirborneState.Falling)fallStageTime=coordinator.PhysicalStageSeconds;
                State=AnimationPlaygroundAirborneState.Falling;
                var falling=Mathf.Max(0,(float)(coordinator.PhysicalStageSeconds-fallStageTime));
                if(motor.TryAnimationGroundDistance(coordinator,3,out var gap))
                {
                    var remaining=AnimationPlaygroundJumpTiming.TimeToContact(gap,Mathf.Max(0,-motor.Velocity.y),motor.JumpGravityMagnitude);
                    progress=.5f+.5f*falling/Mathf.Max(.0001f,falling+remaining);
                }
                else progress=Mathf.Min(.5f+falling/(2*half),.94f);
            }
            flightPhase=Mathf.MoveTowards(flightPhase,Mathf.Max(flightPhase,Mathf.Clamp01(progress)),3*Time.fixedDeltaTime);
        }
        private double PoseTime(double seconds)
        {
            if(genericFall)return hardLanding ? Math.Min(seconds,profile.hardLanding.length) : seconds%profile.falling.length;
            if(State==AnimationPlaygroundAirborneState.Takeoff)
                return Mathf.Clamp01((float)seconds/AnimationPlaygroundJumpTiming.AnticipationSeconds)*variant.liftTime;
            if(State==AnimationPlaygroundAirborneState.Landing)
                return Math.Min(variant.continuous.length,variant.contactTime+Math.Max(0,seconds-landingStageTime));
            return AnimationPlaygroundJumpTiming.FlightSourceSeconds(variant,flightPhase,variant.upwardSpeed/motor.JumpGravityMagnitude);
        }
        private void ObserveMotion()
        {
            if(disposed || !isActiveAndEnabled || !Active)return;
            if(paused?.Invoke()==true){DisarmJumpRequest();return;}
            if(!coordinator.IsSequenceOwner(this)){State=AnimationPlaygroundAirborneState.Standing;driver.EndPoseBlend(this);return;}
            var sample=motor.JumpObservation;
            // A short manifold gap over the same nearby floor is still the
            // current landing. Physics retains gravity and its real contacts;
            // only the recovery presentation/event is kept continuous.
            var continuingLanding=State==AnimationPlaygroundAirborneState.Landing
                && (Now-lastGroundedTime<=.06 && sample.DownwardImpactSpeed<1
                    || motor.IsGrounded && sample.DownwardImpactSpeed<profile.hardLandingSpeed);
            if(sample.Supported)lastGroundedTime=Now;
            if(State==AnimationPlaygroundAirborneState.Takeoff)return;
            // The impulse commits after this tick's completed contact observation.
            if(launched && !genericFall && coordinator.PhysicalStageSeconds<=launchStageTime+.000001)return;
            if(sample.Supported && sample.TouchdownId!=lastTouchdown)
            {
                if(continuingLanding){lastTouchdown=sample.TouchdownId;TryConsumeJumpRequest();return;}
                lastTouchdown=sample.TouchdownId;impactSpeed=sample.DownwardImpactSpeed;
                pendingJump &= Now<=pendingUntil;
                if(LandingRecovery?.Invoke(impactSpeed)==true)
                {State=AnimationPlaygroundAirborneState.Standing;driver.EndPoseBlend(this);Landed?.Invoke(impactSpeed);return;}
                hardLanding=impactSpeed>=profile.hardLandingSpeed;
                if(hardLanding)
                {
                    genericFall=true;
                    if(!Begin(profile.hardLanding,AnimationPlaygroundMotionMode.Recovery,AnimationPlaygroundAirborneState.Landing,transition:true))
                    {Finish();return;}
                    hardLanding=true;landingStageTime=0;
                }
                else
                {
                    if(variant==null)
                    {
                        planar=Vector3.ProjectOnPlane(motor.Velocity,Vector3.up);
                        variant=planar.magnitude>=profile.runningThreshold && (planar.sqrMagnitude<.0001f || Vector3.Dot(planar.normalized,facing)>=.5f)
                            ? profile.running : profile.standing;
                        genericFall=false;
                        if(!Begin(variant.continuous,AnimationPlaygroundMotionMode.Recovery,AnimationPlaygroundAirborneState.Landing,transition:true))
                        {Finish();return;}
                    }
                    State=AnimationPlaygroundAirborneState.Landing;landingStageTime=coordinator.PhysicalStageSeconds;
                    motor.SetAnimationMotionMode(coordinator,AnimationPlaygroundMotionMode.Recovery);
                    motor.SetJumpGroundedLocomotion(coordinator,true);
                    driver.SetAnimationPresentationTime(coordinator,variant.contactTime);
                }
                driver.SetJumpPresentationState(coordinator,1,true,false);
                driver.TryEmitJumpLandingContact(coordinator,impactSpeed);
                Landed?.Invoke(impactSpeed);TryConsumeJumpRequest();return;
            }
            if(State==AnimationPlaygroundAirborneState.Landing)
            {
                if(!sample.Supported && !motor.IsGrounded && motor.Velocity.y<-.1f
                    && !(Now-lastGroundedTime<=.06 && motor.TryPredictJumpSupport(coordinator,.04f,out _)))
                {BeginPassiveFall(true);return;}
                if(!hardLanding && coordinator.PhysicalStageSeconds-landingStageTime>=AnimationPlaygroundJumpTiming.SoftLandingSeconds+Time.fixedDeltaTime)
                {Finish();TryConsumeJumpRequest();}
                return;
            }
            if(!genericFall && launched && coordinator.PhysicalStageSeconds-launchStageTime>1.4
                && !motor.TryAnimationGroundDistance(coordinator,3,out _))BeginPassiveFall(false);
            TryConsumeJumpRequest();
        }
        private void StageCompleted()
        {
            if(coordinator.IsSequenceOwner(this) && State==AnimationPlaygroundAirborneState.Landing && hardLanding)
            {Finish();TryConsumeJumpRequest();}
        }
        private void ObserveLocomotion()
        {
            if(disposed || !isActiveAndEnabled)return;
            if(paused?.Invoke()==true){DisarmJumpRequest();return;}
            if(motor.IsGrounded)lastGroundedTime=Now;
            if(Active && !coordinator.IsSequenceOwner(this))
            {State=AnimationPlaygroundAirborneState.Standing;driver.EndPoseBlend(this);}
            if(Active)return;
            TryConsumeJumpRequest();
            if(Active || !Ready || coordinator.IsBusy || motor.IsActionConstrained || otherActivity?.Invoke()==true
                || motor.AnimationPosture!=AnimationPlaygroundPosture.Standing || motor.IsGrounded || motor.Velocity.y>-.1f)return;
            BeginPassiveFall(false);
        }
        private void BeginPassiveFall(bool newDeparture)
        {
            planar=Vector3.ProjectOnPlane(motor.Velocity,Vector3.up);launchSpeed=planar.magnitude;facing=motor.JumpLaunchFacing;
            variant=null;genericFall=true;hardLanding=false;impactSpeed=0;
            if(newDeparture || !Active || !launched){launched=false;flightGravityScale=1;}
            Begin(profile.falling,AnimationPlaygroundMotionMode.Fall,AnimationPlaygroundAirborneState.Falling,
                transition:true);
        }
        private void Finish(bool retainBlend=false)
        {
            if(coordinator!=null)coordinator.CancelPhysicalSequence(this);
            if(!retainBlend && driver!=null)driver.EndPoseBlend(this);
            State=AnimationPlaygroundAirborneState.Standing;hardLanding=false;genericFall=false;
        }
        public bool TryYieldToGroundedAction(object successor)
        {
            if(successor==null || !CanYieldToGroundedAction || !driver.TryReservePoseBlendHandoff(this,successor,.08f))return false;
            Finish(true);pendingJump=false;driver.SelectReservedPoseBlend(successor);return true;
        }
        public bool TryCancel()
        {
            pendingJump=false;
            if(!Active)return false;
            if(State==AnimationPlaygroundAirborneState.Takeoff && !launched)Finish();
            return true; // Fixed-height flight continues; cancel cannot manufacture an airborne standing release.
        }
        private bool Fail(string reason){LastFailure=reason;return false;}
        public void InvalidateForReset(uint epoch)
        {Finish();DisarmJumpRequest();launched=false;lastTouchdown=motor!=null ? motor.JumpObservation.TouchdownId : 0;}
        public bool TryRestoreForReset(out string reason){reason=null;return true;}
        private void OnDisable(){if(motor!=null)InvalidateForReset(0);}
        public void DisposeOwnedServices()
        {
            if(disposed)return;disposed=true;InvalidateForReset(0);
            if(coordinator!=null)coordinator.ActionCompleted-=StageCompleted;
            if(motor!=null){motor.AnimationMotionAdvanced-=ObserveMotion;motor.AnimationLocomotionAdvanced-=ObserveLocomotion;}
            registry?.Unregister(this);motor=null;driver=null;coordinator=null;LandingRecovery=null;
        }
        private void OnDestroy()=>DisposeOwnedServices();
    }
}
