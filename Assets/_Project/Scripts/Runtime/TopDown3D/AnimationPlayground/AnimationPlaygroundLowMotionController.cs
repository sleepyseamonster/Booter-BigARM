using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundLowMotionState { Standing,Entering,Active,Recovering,BlockedRecovery,Settling }
    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundLowMotionController : MonoBehaviour,IAnimationPlaygroundResetParticipant
    {
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundLowMotionProfile profile;
        private AnimationPlaygroundResetRegistry registry;
        private Func<bool> paused,otherActivity;
        private Func<object,bool> crouchRecovery;
        private AnimationPlaygroundLowMotionEntry entry;
        private AnimationPlaygroundLowMotionRole? nextRole;
        private Vector3 facing;
        private float poseTime,totalTime,initialSpeed,settleTime;
        private bool roll,cancelRequested,shapeBlocked,disposed,resetPending;
        private bool landingContactPending;
        private float landingImpact;
        private const float BlendSeconds=.12f;
        public AnimationPlaygroundLowMotionState State {get;private set;}
        public bool Active=>State!=AnimationPlaygroundLowMotionState.Standing;
        public string LastFailure {get;private set;}
        public float PoseSeconds=>poseTime;
        public AnimationPlaygroundLowMotionRole? CurrentRole=>Active && entry!=null ? entry.role : null;
        public string ResetId=>"CampLoop.Booter.Low";
        public AnimationPlaygroundResetStage ResetStage=>AnimationPlaygroundResetStage.Actor;
        public void Configure(TopDown3DPlayerMotor actor,TopDown3DPlayerAnimationDriver presentation,
            AnimationPlaygroundActionCoordinator actions,AnimationPlaygroundLowMotionProfile calibration,
            AnimationPlaygroundResetRegistry resets,Func<bool> shouldPause,Func<bool> activityBusy,Func<object,bool> recoverToCrouch=null)
        {
            if(motor!=null || actor==null || presentation==null || actions==null || calibration==null || resets==null
                || !calibration.Validate(out _,false))throw new ArgumentException("Configure one paired low-motion family.");
            motor=actor;driver=presentation;coordinator=actions;profile=calibration;registry=resets;paused=shouldPause;otherActivity=activityBusy;crouchRecovery=recoverToCrouch;
            registry.Register(this);motor.AnimationMotionAdvanced+=Observe;motor.AnimationLocomotionAdvanced+=ObserveLocomotion;
        }
        private bool Eligible(bool allowCurrentSequence=false)
        {
            LastFailure=null;
            if(disposed || resetPending || !isActiveAndEnabled || motor==null || paused?.Invoke()==true)return Fail("Low-motion input is unavailable.");
            if(!profile.Validate(out var reason))return Fail(reason);
            if(Active || otherActivity?.Invoke()==true || !allowCurrentSequence && (coordinator.IsBusy || motor.IsActionConstrained))
                return Fail("Finish the current activity first.");
            if(!motor.IsGrounded || motor.AnimationPosture!=AnimationPlaygroundPosture.Standing)return Fail("Low movement requires standing ground support.");
            return true;
        }
        public bool TrySlide()
        {
            if(!Eligible())return false;
            var velocity=Vector3.ProjectOnPlane(motor.Velocity,Vector3.up);
            if(!motor.SprintActive || velocity.magnitude<profile.minimumSlideSpeed
                || Vector3.Dot(velocity.normalized,motor.FacingDirection)<.7f)return Fail("Sprint forward before sliding.");
            roll=false;facing=velocity.normalized;initialSpeed=velocity.magnitude;totalTime=0;cancelRequested=false;landingContactPending=false;
            if(Begin(AnimationPlaygroundLowMotionRole.SlideEnter))return true;
            motor.TryRestoreAnimationPosture(coordinator);return false;
        }
        public bool TryBeginLandingRoll(float impactSpeed)
        {
            if(!float.IsFinite(impactSpeed) || impactSpeed<0 || !Eligible())return false;
            roll=true;facing=motor.FacingDirection;initialSpeed=profile.rollSpeed;totalTime=0;cancelRequested=false;
            if(Begin(AnimationPlaygroundLowMotionRole.RollContact)){landingImpact=impactSpeed;landingContactPending=true;return true;}
            motor.TryRestoreAnimationPosture(coordinator);return false;
        }
        public bool TryTakeLandingSequence(object previousOwner,float impactSpeed)
        {
            if(previousOwner==null || !float.IsFinite(impactSpeed) || impactSpeed<0 || !Eligible(true)
                || !coordinator.IsSequenceOwner(previousOwner))return false;
            if(!driver.TransferPoseBlendOwner(previousOwner,this))return false;
            if(!coordinator.TransferPhysicalSequenceOwner(previousOwner,this))
            {driver.TransferPoseBlendOwner(this,previousOwner);return false;}
            roll=true;facing=motor.FacingDirection;initialSpeed=profile.rollSpeed;totalTime=0;cancelRequested=false;
            if(Begin(AnimationPlaygroundLowMotionRole.RollContact)){landingImpact=impactSpeed;landingContactPending=true;return true;}
            coordinator.TransferPhysicalSequenceOwner(this,previousOwner);driver.TransferPoseBlendOwner(this,previousOwner);
            motor.TryRestoreAnimationPosture(coordinator);return false;
        }
        private AnimationPlaygroundMotionBinding Binding(AnimationClip clip)=>new(){
            sourceId="QualifiedLow."+clip.name,clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
        private bool Shape(AnimationPlaygroundLowMotionEntry value,float time)=>value.horizontal
            ? motor.TrySetAnimationLowShape(coordinator,profile.lowLength,profile.lowRadius,profile.lowForwardOffset)
            : motor.TrySetAnimationPosture(coordinator,AnimationPlaygroundPosture.Crouched,
                value.capsuleHeight.Evaluate(Mathf.Clamp01(time/value.clip.length)),profile.verticalRadius);
        private bool Begin(AnimationPlaygroundLowMotionRole role)
        {
            var value=profile.Get(role);
            if(!Shape(value,0))return Fail("The next low-motion shape is obstructed.");
            if(!driver.TryCapturePoseBlend(this,BlendSeconds))return Fail("The low-motion snapshot is unavailable.");
            var success=coordinator.IsSequenceOwner(this)
                ? coordinator.TrySwitchPhysicalSequence(this,Binding(value.clip),roll ? AnimationPlaygroundMotionMode.Recovery : AnimationPlaygroundMotionMode.Slide,
                    null,Intent,null,time=>poseTime,true)
                : coordinator.TryBeginPhysicalSequence(this,Binding(value.clip),roll ? AnimationPlaygroundMotionMode.Recovery : AnimationPlaygroundMotionMode.Slide,
                    null,Intent,null,time=>poseTime,true);
            if(!success){driver.EndPoseBlend(this);return Fail("The low-motion stage lost its player lease.");}
            entry=value;poseTime=0;shapeBlocked=false;nextRole=null;
            State=value.horizontal ? AnimationPlaygroundLowMotionState.Active
                : role==AnimationPlaygroundLowMotionRole.SlideRecover || role==AnimationPlaygroundLowMotionRole.RollRecover
                    ? AnimationPlaygroundLowMotionState.Recovering : AnimationPlaygroundLowMotionState.Entering;
            if(!driver.SetAnimationBodyProjection(coordinator,profile.bodyProjection))
                throw new InvalidOperationException("The low-motion family lost its paired body projection.");
            driver.SetAnimationGarmentFloor(coordinator,profile.garmentFloorQualified,profile.maximumGarmentProjection,profile.garmentClearance);
            return true;
        }
        private AnimationPlaygroundMotorIntent Intent(double seconds)
        {
            var pause=paused?.Invoke()==true || resetPending;
            if(!pause && nextRole==null)
            {
                var next=Mathf.Min(entry.clip.length,poseTime+Time.fixedDeltaTime);
                shapeBlocked=!Shape(entry,next);
                if(!shapeBlocked){poseTime=next;totalTime+=Time.fixedDeltaTime;}
                else {State=AnimationPlaygroundLowMotionState.BlockedRecovery;LastFailure="Move the obstruction or reset to a clear return.";}
            }
            driver.SetPoseBlendElapsed(this,poseTime);
            var speed=roll ? profile.rollSpeed : Mathf.Max(0,initialSpeed-profile.slideDeceleration*totalTime);
            if(cancelRequested)speed=0;
            if(entry.role==AnimationPlaygroundLowMotionRole.SlideRecover || entry.role==AnimationPlaygroundLowMotionRole.RollRecover || shapeBlocked || nextRole.HasValue)speed=0;
            return new AnimationPlaygroundMotorIntent(facing*speed,facing,phase:poseTime/entry.clip.length,paused:pause);
        }
        private void Observe()
        {
            if(disposed || resetPending || !Active || State==AnimationPlaygroundLowMotionState.Settling || paused?.Invoke()==true
                || !coordinator.IsSequenceOwner(this))return;
            if(landingContactPending && entry.role==AnimationPlaygroundLowMotionRole.RollContact && !driver.PoseBlendOwned
                && driver.TryEmitAnimationFootContact(coordinator,TopDown3DFootSide.Left,landingImpact))landingContactPending=false;
            if(shapeBlocked)
            {
                if(IsRecoveryRole && crouchRecovery?.Invoke(this)==true){State=AnimationPlaygroundLowMotionState.Standing;return;}
                return;
            }
            if(poseTime<entry.clip.length-.000001f)return;
            nextRole=entry.role switch{
                AnimationPlaygroundLowMotionRole.SlideEnter=>AnimationPlaygroundLowMotionRole.SlideActive,
                AnimationPlaygroundLowMotionRole.RollContact=>AnimationPlaygroundLowMotionRole.RollActive,
                AnimationPlaygroundLowMotionRole.SlideActive=>AnimationPlaygroundLowMotionRole.SlideRecover,
                AnimationPlaygroundLowMotionRole.RollActive=>AnimationPlaygroundLowMotionRole.RollRecover,
                _=>null};
            if(!motor.AnimationSnapshot.Supported)return; // Gravity/contact remains physical while a low pose waits in flight.
            if(nextRole.HasValue)
            {
                var next=nextRole.Value;
                if(!Begin(next))State=AnimationPlaygroundLowMotionState.BlockedRecovery;
            }
            else Settle();
        }
        private void Settle()
        {
            if(!motor.IsStandingAnimationShapeClear()){State=AnimationPlaygroundLowMotionState.BlockedRecovery;shapeBlocked=true;return;}
            if(!driver.TryCapturePoseBlend(this,BlendSeconds) || !motor.TryRestoreAnimationPosture(coordinator))return;
            coordinator.CancelPhysicalSequence(this);
            if(!motor.TryBeginActionConstraint(this))throw new InvalidOperationException("Low-motion settling lost its motor constraint.");
            State=AnimationPlaygroundLowMotionState.Settling;settleTime=0;
        }
        private void ObserveLocomotion()
        {
            if(disposed || resetPending || !Active)return;
            if(State==AnimationPlaygroundLowMotionState.Settling)
            {
                if(paused?.Invoke()!=true)settleTime+=Time.fixedDeltaTime;
                driver.SetPoseBlendElapsed(this,settleTime);
                if(settleTime>=BlendSeconds){driver.EndPoseBlend(this);motor.EndActionConstraint(this);State=AnimationPlaygroundLowMotionState.Standing;}
                return;
            }
            if(coordinator.IsSequenceOwner(this) || coordinator.IsBusy || motor.IsActionConstrained)return;
            cancelRequested=true;initialSpeed=0;
            if(coordinator.TryBeginPhysicalSequence(this,Binding(entry.clip),roll ? AnimationPlaygroundMotionMode.Recovery : AnimationPlaygroundMotionMode.Slide,
                null,Intent,null,time=>poseTime,true))
            {driver.SetAnimationPresentationTime(coordinator,poseTime);driver.SetAnimationBodyProjection(coordinator,profile.bodyProjection);
                driver.SetAnimationGarmentFloor(coordinator,profile.garmentFloorQualified,profile.maximumGarmentProjection,profile.garmentClearance);}
        }
        public bool TryCancel()
        {
            if(!Active || disposed)return false;
            cancelRequested=true; // Finish the active source range before recovering from a folded roll.
            if(State==AnimationPlaygroundLowMotionState.BlockedRecovery && IsRecoveryRole && crouchRecovery?.Invoke(this)==true)
                State=AnimationPlaygroundLowMotionState.Standing;
            return true;
        }
        private bool Fail(string reason){LastFailure=reason;return false;}
        private bool IsRecoveryRole=>entry!=null && (entry.role==AnimationPlaygroundLowMotionRole.SlideRecover || entry.role==AnimationPlaygroundLowMotionRole.RollRecover);
        public void InvalidateForReset(uint epoch)
        {
            resetPending=true;landingContactPending=false;coordinator.CancelPhysicalSequence(this);driver.EndPoseBlend(this);motor.EndActionConstraint(this);
            if(Active && motor.AnimationPosture!=AnimationPlaygroundPosture.Standing)
            {
                if(!driver.IsAnimationPresentationOwner(this) && !driver.TryBeginAnimationPresentation(this,entry.clip))
                    throw new InvalidOperationException("Reset could not retain the low pose.");
                driver.SetAnimationPresentationTime(this,poseTime);driver.SetAnimationBodyProjection(this,profile.bodyProjection);
                driver.SetAnimationGarmentFloor(this,profile.garmentFloorQualified,profile.maximumGarmentProjection,profile.garmentClearance);
                motor.SetLaboratoryPostureSuspended(coordinator,true);
            }
        }
        public bool TryRestoreForReset(out string reason)
        {
            reason=null;
            if(motor.AnimationPosture!=AnimationPlaygroundPosture.Standing){reason="Restore the actor at a clear standing return.";return false;}
            driver.EndAnimationPresentation(this);motor.SetLaboratoryPostureSuspended(coordinator,false);State=AnimationPlaygroundLowMotionState.Standing;resetPending=false;return true;
        }
        private void OnDisable(){if(Active)cancelRequested=true;}
        public void DisposeOwnedServices()
        {
            if(disposed)return;disposed=true;
            if(motor!=null)
            {
                coordinator.CancelPhysicalSequence(this);motor.EndActionConstraint(this);
                driver.EndAnimationPresentation(this);
                if(Active && !motor.TryRestoreAnimationPosture(coordinator))
                {
                    var retained=motor.gameObject.AddComponent<AnimationPlaygroundLowRecovery>();
                    if(!retained.TryAdopt(motor,driver,coordinator,this,profile,entry.role,poseTime,facing))
                        throw new InvalidOperationException("The blocked low pose could not retain its recovery lease.");
                }
                else driver.EndPoseBlend(this);
                motor.AnimationMotionAdvanced-=Observe;motor.AnimationLocomotionAdvanced-=ObserveLocomotion;
            }
            registry?.Unregister(this);State=AnimationPlaygroundLowMotionState.Standing;motor=null;driver=null;coordinator=null;
        }
        private void OnDestroy()=>DisposeOwnedServices();
    }
}
