using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundCrouchState { Standing, Entering, Crouched, Exiting, SettlingCrouch, SettlingStanding }

    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundCrouchController : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundCrouchProfile profile;
        private AnimationPlaygroundResetRegistry registry;
        private Func<bool> paused,otherActivity;
        private readonly AnimationPlaygroundCrouchClock clock=new();
        private Vector3 facing;
        private bool exitRequested,disposed,resetPending,suspended;
        private float contactSpeed;
        private const float BlendSeconds=.18f;
        private float settleElapsed;
        public AnimationPlaygroundCrouchState State {get;private set;}
        public bool Active=>State!=AnimationPlaygroundCrouchState.Standing;
        private bool TimedTransition=>State==AnimationPlaygroundCrouchState.Entering || State==AnimationPlaygroundCrouchState.Exiting;
        private bool Settling=>State==AnimationPlaygroundCrouchState.SettlingCrouch || State==AnimationPlaygroundCrouchState.SettlingStanding;
        public bool Transitioning=>TimedTransition || Settling;
        public string LastFailure {get;private set;}
        public string ResetId=>"CampLoop.Booter.Crouch";
        public AnimationPlaygroundResetStage ResetStage=>AnimationPlaygroundResetStage.Actor;

        public void Configure(TopDown3DPlayerMotor value,TopDown3DPlayerAnimationDriver presentation,
            AnimationPlaygroundActionCoordinator actions,AnimationPlaygroundCrouchProfile calibration,
            AnimationPlaygroundResetRegistry resets,Func<bool> shouldPause,Func<bool> activityBusy)
        {
            if(motor!=null || value==null || presentation==null || actions==null || calibration==null || resets==null
                || !calibration.Validate(out _,false))throw new ArgumentException("Configure one calibrated crouch family on the existing player.");
            motor=value;driver=presentation;coordinator=actions;profile=calibration;registry=resets;paused=shouldPause;otherActivity=activityBusy;
            registry.Register(this);coordinator.ActionCompleted+=CompleteTransition;motor.AnimationLocomotionAdvanced+=AdvanceLocomotion;clock.Contact+=Contact;
        }

        public bool TryToggle()
        {
            LastFailure=null;
            if(disposed || suspended || resetPending || !isActiveAndEnabled || motor==null || paused?.Invoke()==true)return Fail("Crouch input is unavailable.");
            if(!profile.Validate(out var qualification))return Fail(qualification);
            if(Transitioning)return Fail("Finish the posture transition first.");
            if(coordinator.IsBusy || otherActivity?.Invoke()==true)return Fail("Finish the current activity first.");
            if(State==AnimationPlaygroundCrouchState.Crouched)return TryExit();
            if(!motor.IsGrounded)return Fail("Crouching requires ground support.");
            if(!motor.IsAnimationPostureShapeClear(profile.standingHeight,profile.crouchRadius))
                return Fail("The crouch pose needs more space around the character.");
            facing=motor.FacingDirection;exitRequested=false;
            return BeginTransition(AnimationPlaygroundCrouchRole.Enter);
        }

        public bool TryCancel()
        {
            if(disposed || suspended || !isActiveAndEnabled)return Fail("Crouch input is unavailable.");
            if(resetPending)return Fail("Finish the safe actor reset first.");
            if(State==AnimationPlaygroundCrouchState.Entering || State==AnimationPlaygroundCrouchState.SettlingCrouch){exitRequested=true;return true;}
            if(State==AnimationPlaygroundCrouchState.Exiting || State==AnimationPlaygroundCrouchState.SettlingStanding)return true;
            return State==AnimationPlaygroundCrouchState.Crouched && TryExit();
        }
        public bool TryReceiveLowRecovery(object previousSequenceOwner)
        {
            if(disposed || suspended || resetPending || !isActiveAndEnabled || Active || paused?.Invoke()==true
                || !profile.Validate(out _) || !coordinator.IsSequenceOwner(previousSequenceOwner)
                || !motor.IsGrounded || !motor.IsAnimationPostureShapeClear(profile.crouchHeight,profile.crouchRadius))return false;
            if(!driver.TransferPoseBlendOwner(previousSequenceOwner,this))return false;
            if(!driver.TryCapturePoseBlend(this,BlendSeconds))
            {driver.TransferPoseBlendOwner(this,previousSequenceOwner);return false;}
            if(!motor.TrySetAnimationPosture(coordinator,AnimationPlaygroundPosture.Crouched,profile.crouchHeight,profile.crouchRadius))
            {driver.TransferPoseBlendOwner(this,previousSequenceOwner);return false;}
            facing=motor.FacingDirection;exitRequested=false;
            coordinator.CancelPhysicalSequence(previousSequenceOwner);HoldCrouch();
            State=AnimationPlaygroundCrouchState.SettlingCrouch;BeginSettle();return true;
        }

        private bool TryExit()
        {
            if(!motor.IsStandingAnimationShapeClear())return Fail("Move out from under the obstruction before standing.");
            return BeginTransition(AnimationPlaygroundCrouchRole.Exit);
        }

        // Reserve the outgoing pose before releasing crouch. The standing capsule
        // is conservative throughout the visual rise and must fit before any travel.
        public bool TryReleaseForSprint(object blendOwner, float blendSeconds)
        {
            if (disposed || suspended || resetPending || !isActiveAndEnabled || State != AnimationPlaygroundCrouchState.Crouched
                || coordinator.IsBusy || paused?.Invoke() == true || !motor.IsGrounded || !motor.IsStandingAnimationShapeClear()
                || !driver.TryCapturePoseBlend(blendOwner, blendSeconds)) return false;
            if (!motor.TryRestoreAnimationPosture(coordinator)) { driver.EndPoseBlend(blendOwner); return false; }
            driver.EndCrouchPresentation(this); motor.SetLaboratoryCrouchFacing(coordinator, facing, false);
            State = AnimationPlaygroundCrouchState.Standing; clock.Reset(); exitRequested = false;
            return true;
        }

        public bool TryResumeAfterSprintFailure()
        {
            if (disposed || suspended || resetPending || !isActiveAndEnabled || Active || coordinator.IsBusy
                || !motor.IsGrounded || !motor.TrySetAnimationPosture(coordinator, AnimationPlaygroundPosture.Crouched, profile.crouchHeight, profile.crouchRadius)) return false;
            HoldCrouch(); return true;
        }

        private bool BeginTransition(AnimationPlaygroundCrouchRole role)
        {
            var entry=profile.Get(role);
            var binding=new AnimationPlaygroundMotionBinding{sourceId="QualifiedCrouch."+role,clip=entry.clip,
                readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
            if(!driver.TryCapturePoseBlend(this,BlendSeconds))return Fail("The current pose could not be reserved for blending.");
            if(!coordinator.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.AlignedAction,null,
                seconds=>TransitionIntent(entry,(float)(seconds/entry.clip.length)),null))
            {driver.EndPoseBlend(this);return Fail("The player pose or motor already has an owner.");}
            driver.EndCrouchPresentation(this);
            State=role==AnimationPlaygroundCrouchRole.Enter ? AnimationPlaygroundCrouchState.Entering : AnimationPlaygroundCrouchState.Exiting;
            motor.SetLaboratoryCrouchFacing(coordinator,facing,true);
            try{TransitionIntent(entry,0);}
            catch(Exception error){coordinator.Cancel(error.Message);HoldCrouch();return Fail(error.Message);}
            return true;
        }

        private AnimationPlaygroundMotorIntent TransitionIntent(AnimationPlaygroundCrouchEntry entry,float phase)
        {
            phase=Mathf.Clamp01(phase);
            if(!motor.TrySetAnimationPosture(coordinator,AnimationPlaygroundPosture.Crouched,entry.capsuleHeight.Evaluate(phase),profile.crouchRadius))
                throw new InvalidOperationException("The posture transition was obstructed.");
            if(!driver.SetAnimationHeightCorrection(coordinator,entry.Correction(phase)))
                throw new InvalidOperationException("The posture presentation lost its owner.");
            if(!driver.SetAnimationFootProjection(coordinator,true))
                throw new InvalidOperationException("The posture foot projection lost its owner.");
            driver.SetPoseBlendElapsed(this,phase*entry.clip.length);
            return new AnimationPlaygroundMotorIntent(Vector3.zero,facing,phase:phase);
        }

        private void CompleteTransition()
        {
            if(!TimedTransition)return;
            var entering=State==AnimationPlaygroundCrouchState.Entering;
            if(!driver.TryCapturePoseBlend(this,BlendSeconds))throw new InvalidOperationException("The completed transition could not preserve its outgoing pose.");
            coordinator.Cancel();
            if(!entering && motor.TryRestoreAnimationPosture(coordinator))
            {
                State=AnimationPlaygroundCrouchState.SettlingStanding;motor.SetLaboratoryCrouchFacing(coordinator,facing,false);clock.Reset();BeginSettle();return;
            }
            HoldCrouch();
            State=AnimationPlaygroundCrouchState.SettlingCrouch;BeginSettle();
        }
        private void BeginSettle()
        {
            settleElapsed=0;
            if(!motor.TryBeginActionConstraint(this))throw new InvalidOperationException("The settling pose could not preserve its motor constraint.");
        }

        private void HoldCrouch()
        {
            if(!motor.TrySetAnimationPosture(coordinator,AnimationPlaygroundPosture.Crouched,profile.crouchHeight,profile.crouchRadius))
                throw new InvalidOperationException("The calibrated crouch shape could not be retained.");
            if(!driver.CrouchPresentationOwned && !driver.TryBeginCrouchPresentation(this,profile))
                throw new InvalidOperationException("The sustained crouch pose could not acquire its branch.");
            State=AnimationPlaygroundCrouchState.Crouched;
            motor.SetLaboratoryCrouchFacing(coordinator,facing,true);
            clock.Reset();driver.SetCrouchPose(this,clock.Pose);
        }

        private void AdvanceLocomotion()
        {
            if(resetPending || suspended || !isActiveAndEnabled)return;
            if(Settling)
            {
                if(paused?.Invoke()!=true)settleElapsed+=Time.fixedDeltaTime;
                driver.SetPoseBlendElapsed(this,settleElapsed);
                if(settleElapsed>=BlendSeconds)
                {
                    var crouched=State==AnimationPlaygroundCrouchState.SettlingCrouch;
                    driver.EndPoseBlend(this);motor.EndActionConstraint(this);
                    State=crouched ? AnimationPlaygroundCrouchState.Crouched : AnimationPlaygroundCrouchState.Standing;
                    if(crouched && exitRequested){exitRequested=false;TryExit();}
                }
                return;
            }
            if(TimedTransition && !coordinator.IsBusy){LastFailure=coordinator.LastFailure;driver.EndPoseBlend(this);HoldCrouch();}
            if(State!=AnimationPlaygroundCrouchState.Crouched || coordinator.IsBusy)return;
            var velocity=motor.LaboratoryMeasuredVelocity;contactSpeed=Vector3.ProjectOnPlane(velocity,Vector3.up).magnitude;
            var local=new Vector2(Vector3.Dot(velocity,Vector3.Cross(Vector3.up,facing)),Vector3.Dot(velocity,facing));
            var sum=Mathf.Abs(local.x)+Mathf.Abs(local.y);
            var seconds=0f;var speed=0f;
            for(var i=2;i<=5;i++)
            {
                var weight=i==2 ? Mathf.Max(0,local.y) : i==3 ? Mathf.Max(0,-local.y) : i==4 ? Mathf.Max(0,-local.x) : Mathf.Max(0,local.x);
                var entry=profile.Get((AnimationPlaygroundCrouchRole)i);
                weight=sum>.0001f ? weight/sum : i==2 ? 1 : 0;
                seconds+=entry.clip.length*weight;speed+=entry.nominalSpeed*weight;
            }
            clock.Advance(Time.fixedDeltaTime,velocity,facing,speed,seconds,motor.IsGrounded,paused?.Invoke()==true);
            driver.SetCrouchPose(this,clock.Pose);
        }

        private void Contact(TopDown3DFootSide side)
        {driver.SetCrouchPose(this,clock.Pose);driver.TryEmitCrouchFootContact(this,side,contactSpeed);}
        private bool Fail(string reason){LastFailure=reason;return false;}
        public void InvalidateForReset(uint epoch)
        {
            if(motor==null)return;
            if(TimedTransition)coordinator.Cancel();
            driver.EndPoseBlend(this);motor.EndActionConstraint(this);
            if(motor.AnimationPosture==AnimationPlaygroundPosture.Crouched)HoldCrouch();
            else {driver.EndCrouchPresentation(this);State=AnimationPlaygroundCrouchState.Standing;}
            clock.Reset();resetPending=true;exitRequested=false;
        }
        public bool TryRestoreForReset(out string reason)
        {
            if(motor.AnimationPosture!=AnimationPlaygroundPosture.Standing)
            {reason="Restore the actor at a clear standing return before releasing the crouch pose.";return false;}
            driver.EndCrouchPresentation(this);motor.SetLaboratoryCrouchFacing(coordinator,facing,false);
            State=AnimationPlaygroundCrouchState.Standing;resetPending=false;reason=null;return true;
        }
        public void DisposeOwnedServices()
        {
            if(disposed)return;disposed=true;
            if(motor!=null)
            {
                if(TimedTransition)coordinator.Cancel();
                driver.EndPoseBlend(this);motor.EndActionConstraint(this);
                if(Active && !motor.TryRestoreAnimationPosture(coordinator))
                {
                    HoldCrouch();
                    if(!driver.TryAdoptCrouchRecovery(this,coordinator))
                        throw new InvalidOperationException("The interrupted crouch pose could not transfer its safe recovery ownership.");
                }
                else {driver.EndCrouchPresentation(this);motor.SetLaboratoryCrouchFacing(coordinator,facing,false);motor.SetLaboratoryPostureSuspended(coordinator,false);}
                clock.Reset();State=AnimationPlaygroundCrouchState.Standing;
            }
            if(motor!=null){motor.AnimationLocomotionAdvanced-=AdvanceLocomotion;motor.TryRestoreAnimationPosture(coordinator);}
            if(coordinator!=null)coordinator.ActionCompleted-=CompleteTransition;
            clock.Contact-=Contact;registry?.Unregister(this);motor=null;driver=null;coordinator=null;
        }
        private void OnDisable()
        {
            if(motor==null || disposed || !Active)return;
            if(TimedTransition)coordinator.Cancel();
            driver.EndPoseBlend(this);motor.EndActionConstraint(this);
            if(motor.TryRestoreAnimationPosture(coordinator))
            {driver.EndCrouchPresentation(this);State=AnimationPlaygroundCrouchState.Standing;motor.SetLaboratoryCrouchFacing(coordinator,facing,false);}
            else HoldCrouch();
            suspended=true;motor.SetLaboratoryPostureSuspended(coordinator,Active);clock.Reset();
        }
        private void OnEnable()
        {
            if(motor==null || disposed || !suspended)return;
            suspended=false;motor.SetLaboratoryPostureSuspended(coordinator,false);clock.Reset();
        }
        private void OnDestroy()=>DisposeOwnedServices();
    }
}
