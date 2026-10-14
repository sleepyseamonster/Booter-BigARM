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
        private AnimationPlaygroundJumpVariant variant;
        private Vector3 facing,planar;
        private bool launched,leftGround,disposed,landingContactPending;
        private float impactSpeed,settleTime;
        private const float BlendSeconds=.12f;
        public AnimationPlaygroundAirborneState State {get;private set;}
        public bool Active=>State!=AnimationPlaygroundAirborneState.Standing;
        public string LastFailure {get;private set;}
        public event Action<float> Landed;
        public string ResetId=>"CampLoop.Booter.Jump";
        public AnimationPlaygroundResetStage ResetStage=>AnimationPlaygroundResetStage.Actor;
        public void Configure(TopDown3DPlayerMotor value,TopDown3DPlayerAnimationDriver presentation,
            AnimationPlaygroundActionCoordinator actions,AnimationPlaygroundAirborneProfile calibration,
            AnimationPlaygroundResetRegistry resets,Func<bool> shouldPause,Func<bool> activityBusy)
        {
            if(motor!=null || value==null || presentation==null || actions==null || calibration==null || resets==null
                || !calibration.Validate(out _,false))throw new ArgumentException("Configure one paired airborne family.");
            motor=value;driver=presentation;coordinator=actions;profile=calibration;registry=resets;paused=shouldPause;otherActivity=activityBusy;
            registry.Register(this);coordinator.ActionCompleted+=StageCompleted;
            motor.AnimationMotionAdvanced+=ObserveMotion;motor.AnimationLocomotionAdvanced+=ObserveLocomotion;
        }
        public bool TryJump(bool useUnarmed=false)
        {
            LastFailure=null;
            if(disposed || !isActiveAndEnabled || motor==null || paused?.Invoke()==true)return Fail("Jump input is unavailable.");
            if(!profile.Validate(out var reason))return Fail(reason);
            if(Active || coordinator.IsBusy || motor.IsActionConstrained || otherActivity?.Invoke()==true)return Fail("Finish the current activity first.");
            if(!motor.IsGrounded || motor.AnimationPosture!=AnimationPlaygroundPosture.Standing)return Fail("Jumping requires standing ground support.");
            planar=Vector3.ProjectOnPlane(motor.Velocity,Vector3.up);facing=motor.FacingDirection;
            variant=useUnarmed ? profile.unarmed : planar.magnitude>=profile.runningThreshold ? profile.running : profile.standing;
            launched=false;leftGround=false;impactSpeed=0;
            if(!Begin(variant.takeoff,AnimationPlaygroundMotionMode.Jump,AnimationPlaygroundAirborneState.Takeoff,false,
                new[]{new AnimationPlaygroundMarker(1,variant.takeoff.length)},key=>{
                    if(launched || !motor.TryAnimationTakeoff(coordinator,variant.upwardSpeed))return false;
                    launched=true;return true;
                }))return Fail("The jump could not acquire the player.");
            return true;
        }
        private AnimationPlaygroundMotionBinding Binding(AnimationClip clip)=>new(){
            sourceId="QualifiedAirborne."+clip.name,clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
        private bool Begin(AnimationClip clip,AnimationPlaygroundMotionMode mode,AnimationPlaygroundAirborneState next,
            bool loop,AnimationPlaygroundMarker[] markers=null,Func<AnimationPlaygroundEffectKey,bool> effect=null,Func<double,double> pose=null)
        {
            if(!driver.TryCapturePoseBlend(this,BlendSeconds)){AbandonSequence("The airborne snapshot is unavailable.");return false;}
            var success=coordinator.IsSequenceOwner(this)
                ? coordinator.TrySwitchPhysicalSequence(this,Binding(clip),mode,markers,Intent,effect,pose,loop)
                : coordinator.TryBeginPhysicalSequence(this,Binding(clip),mode,markers,Intent,effect,pose,loop);
            if(!success){driver.EndPoseBlend(this);AbandonSequence("The airborne stage lost its owner.");return false;}
            State=next;driver.SetAnimationFootProjection(coordinator,next==AnimationPlaygroundAirborneState.Takeoff
                || next==AnimationPlaygroundAirborneState.Approach || next==AnimationPlaygroundAirborneState.Landing);
            return true;
        }
        private void AbandonSequence(string reason)
        {
            coordinator.CancelPhysicalSequence(this);driver.EndPoseBlend(this);
            State=AnimationPlaygroundAirborneState.Standing;landingContactPending=false;LastFailure=reason;
        }
        private AnimationPlaygroundMotorIntent Intent(double seconds)
        {
            driver.SetPoseBlendElapsed(this,coordinator.PhysicalStageSeconds);
            if(paused?.Invoke()!=true && State!=AnimationPlaygroundAirborneState.Takeoff && State!=AnimationPlaygroundAirborneState.Landing)
                planar=Vector3.MoveTowards(planar,motor.LaboratoryMoveDirection*profile.maximumPlanarSpeed,profile.airControlAcceleration*Time.fixedDeltaTime);
            var velocity=State==AnimationPlaygroundAirborneState.Landing ? Vector3.zero : planar;
            return new AnimationPlaygroundMotorIntent(velocity,facing,phase:0);
        }
        private void StageCompleted()
        {
            if(!coordinator.IsSequenceOwner(this))return;
            if(State==AnimationPlaygroundAirborneState.Takeoff && launched)
            {
                Begin(variant.rising,AnimationPlaygroundMotionMode.Jump,AnimationPlaygroundAirborneState.Rising,true,
                    pose:time=>Mathf.Clamp01(1-motor.Velocity.y/variant.upwardSpeed)*variant.rising.length);
            }
            else if(State==AnimationPlaygroundAirborneState.Landing)Settle();
        }
        private void ObserveMotion()
        {
            if(disposed || !isActiveAndEnabled || paused?.Invoke()==true || !Active || State==AnimationPlaygroundAirborneState.Settling)return;
            if(!coordinator.IsSequenceOwner(this)){State=AnimationPlaygroundAirborneState.Standing;driver.EndPoseBlend(this);ObserveLocomotion();return;}
            var sample=motor.AnimationSnapshot;
            impactSpeed=Mathf.Max(impactSpeed,Mathf.Max(0,-sample.Velocity.y));
            if(!sample.Supported)leftGround=true;
            if(State==AnimationPlaygroundAirborneState.Takeoff)return;
            if(State==AnimationPlaygroundAirborneState.Rising && !leftGround)return;
            if(State==AnimationPlaygroundAirborneState.Landing)
            {
                if(landingContactPending && !driver.PoseBlendOwned)
                {landingContactPending=false;driver.TryEmitAnimationFootContact(coordinator,TopDown3DFootSide.Left,impactSpeed);}
                return;
            }
            if(leftGround && sample.Supported && motor.TryAnimationGroundDistance(coordinator,profile.anticipationDistance,out var contact)
                && contact<=profile.contactDistance)
            {
                var clip=variant==null || impactSpeed>=profile.hardLandingSpeed ? profile.hardLanding : variant.landing;
                if(Begin(clip,AnimationPlaygroundMotionMode.Recovery,AnimationPlaygroundAirborneState.Landing,false))
                {landingContactPending=true;Landed?.Invoke(impactSpeed);}
                return;
            }
            if(State==AnimationPlaygroundAirborneState.Rising && sample.Velocity.y<=.05f)
                Begin(profile.falling,AnimationPlaygroundMotionMode.Fall,AnimationPlaygroundAirborneState.Falling,true);
            if(State==AnimationPlaygroundAirborneState.Falling && motor.TryAnimationGroundDistance(coordinator,profile.anticipationDistance,out var distance)
                && distance<profile.anticipationDistance)
                Begin(profile.approach,AnimationPlaygroundMotionMode.Fall,AnimationPlaygroundAirborneState.Approach,true,
                    pose:time=>motor.TryAnimationGroundDistance(coordinator,profile.anticipationDistance,out var gap)
                        ? Mathf.Clamp01(1-gap/profile.anticipationDistance)*profile.approach.length : 0);
            else if(State==AnimationPlaygroundAirborneState.Approach
                && !motor.TryAnimationGroundDistance(coordinator,profile.anticipationDistance,out _))
                Begin(profile.falling,AnimationPlaygroundMotionMode.Fall,AnimationPlaygroundAirborneState.Falling,true);
        }
        private void ObserveLocomotion()
        {
            if(disposed || !isActiveAndEnabled || paused?.Invoke()==true)return;
            if(Active && State!=AnimationPlaygroundAirborneState.Settling && !coordinator.IsSequenceOwner(this))
            {State=AnimationPlaygroundAirborneState.Standing;driver.EndPoseBlend(this);}
            if(State==AnimationPlaygroundAirborneState.Settling)
            {
                settleTime+=Time.fixedDeltaTime;driver.SetPoseBlendElapsed(this,settleTime);
                if(settleTime>=BlendSeconds){driver.EndPoseBlend(this);motor.EndActionConstraint(this);State=AnimationPlaygroundAirborneState.Standing;}
                return;
            }
            if(Active || coordinator.IsBusy || motor.IsActionConstrained || otherActivity?.Invoke()==true
                || motor.AnimationPosture!=AnimationPlaygroundPosture.Standing || motor.IsGrounded || motor.Velocity.y>-.5f
                || !profile.Validate(out _))return;
            variant=null;planar=Vector3.ProjectOnPlane(motor.Velocity,Vector3.up);facing=motor.FacingDirection;
            impactSpeed=-motor.Velocity.y;leftGround=true;
            Begin(profile.falling,AnimationPlaygroundMotionMode.Fall,AnimationPlaygroundAirborneState.Falling,true);
        }
        private void Settle()
        {
            if(!driver.TryCapturePoseBlend(this,BlendSeconds)){coordinator.CancelPhysicalSequence(this);State=AnimationPlaygroundAirborneState.Standing;return;}
            coordinator.CancelPhysicalSequence(this);
            if(!motor.TryBeginActionConstraint(this)){driver.EndPoseBlend(this);State=AnimationPlaygroundAirborneState.Standing;return;}
            State=AnimationPlaygroundAirborneState.Settling;settleTime=0;
        }
        public bool TryCancel()
        {
            if(!Active)return false;
            if(State==AnimationPlaygroundAirborneState.Takeoff && !launched){Settle();return true;}
            if(State==AnimationPlaygroundAirborneState.Rising)
                return Begin(profile.falling,AnimationPlaygroundMotionMode.Fall,AnimationPlaygroundAirborneState.Falling,true);
            return true; // Falling continues through physical contact; cancel cannot create a mid-air standing release.
        }
        private bool Fail(string reason){LastFailure=reason;return false;}
        public void InvalidateForReset(uint epoch)
        {coordinator?.CancelPhysicalSequence(this);driver?.EndPoseBlend(this);motor?.EndActionConstraint(this);State=AnimationPlaygroundAirborneState.Standing;landingContactPending=false;}
        public bool TryRestoreForReset(out string reason){reason=null;return true;}
        private void OnDisable(){if(motor!=null)InvalidateForReset(0);}
        public void DisposeOwnedServices()
        {
            if(disposed)return;disposed=true;InvalidateForReset(0);
            if(coordinator!=null)coordinator.ActionCompleted-=StageCompleted;
            if(motor!=null){motor.AnimationMotionAdvanced-=ObserveMotion;motor.AnimationLocomotionAdvanced-=ObserveLocomotion;}
            registry?.Unregister(this);motor=null;driver=null;coordinator=null;
        }
        private void OnDestroy()=>DisposeOwnedServices();
    }
}
