using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundCrouchState { Standing, Entering, Crouched, Exiting }

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
        public AnimationPlaygroundCrouchState State {get;private set;}
        public bool Active=>State!=AnimationPlaygroundCrouchState.Standing;
        public bool Transitioning=>State==AnimationPlaygroundCrouchState.Entering || State==AnimationPlaygroundCrouchState.Exiting;
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
            if(State==AnimationPlaygroundCrouchState.Entering){exitRequested=true;return true;}
            if(State==AnimationPlaygroundCrouchState.Exiting)return true;
            return State==AnimationPlaygroundCrouchState.Crouched && TryExit();
        }

        private bool TryExit()
        {
            if(!motor.IsStandingAnimationShapeClear())return Fail("Move out from under the obstruction before standing.");
            return BeginTransition(AnimationPlaygroundCrouchRole.Exit);
        }

        private bool BeginTransition(AnimationPlaygroundCrouchRole role)
        {
            var entry=profile.Get(role);
            var binding=new AnimationPlaygroundMotionBinding{sourceId="QualifiedCrouch."+role,clip=entry.clip,
                readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
            if(!coordinator.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.AlignedAction,null,
                seconds=>TransitionIntent(entry,(float)(seconds/entry.clip.length)),null))
                return Fail("The player pose or motor already has an owner.");
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
            return new AnimationPlaygroundMotorIntent(Vector3.zero,facing,phase:phase);
        }

        private void CompleteTransition()
        {
            if(!Transitioning)return;
            var entering=State==AnimationPlaygroundCrouchState.Entering;
            coordinator.Cancel();
            if(!entering && motor.TryRestoreAnimationPosture(coordinator))
            {
                State=AnimationPlaygroundCrouchState.Standing;motor.SetLaboratoryCrouchFacing(coordinator,facing,false);clock.Reset();return;
            }
            HoldCrouch();
            if(entering && exitRequested){exitRequested=false;TryExit();}
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
            if(Transitioning && !coordinator.IsBusy){LastFailure=coordinator.LastFailure;HoldCrouch();}
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
            if(Transitioning)coordinator.Cancel();
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
                if(Transitioning)coordinator.Cancel();
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
            if(Transitioning)coordinator.Cancel();
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
