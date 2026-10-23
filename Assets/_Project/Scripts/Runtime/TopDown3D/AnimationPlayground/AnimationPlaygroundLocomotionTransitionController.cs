using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundLocomotionTransitionController : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundLocomotionTransitionProfile profile;
        private AnimationPlaygroundResetRegistry registry;
        private Func<bool> paused, otherBusy;
        private AnimationPlaygroundLocomotionTake take;
        private Quaternion basis;
        private Vector3 demand;
        private float previousTime, sourceRate, blendTime, targetYaw;
        private bool performing, blending, disposed, rearm;
        public bool Active => performing || blending;
        public AnimationPlaygroundLocomotionTransition Kind { get; private set; }
        public string ResetId => "CampLoop.Booter.LocomotionTransition";
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.Actor;
        public void Configure(TopDown3DPlayerMotor value, TopDown3DPlayerAnimationDriver presentation,
            AnimationPlaygroundActionCoordinator actions, AnimationPlaygroundLocomotionTransitionProfile calibration,
            AnimationPlaygroundResetRegistry resets, Func<bool> shouldPause, Func<bool> busy)
        {
            if (motor != null || value == null || presentation == null || actions == null || resets == null
                || calibration == null || !calibration.Validate(calibration.targetModel,out _))
                throw new ArgumentException("Configure one qualified locomotion transition family.");
            motor=value; driver=presentation; coordinator=actions; profile=calibration; registry=resets; paused=shouldPause; otherBusy=busy;
            registry.Register(this); motor.AnimationLocomotionPreparing+=ObserveRequest;
            motor.AnimationMotionPreparing+=Guard; motor.AnimationLocomotionAdvanced+=AdvanceBlend;
            motor.AnimationMotionAdvanced+=AfterMotion;
        }
        // Called inside the motor tick after its fresh support query, before ordinary acceleration/facing.
        private void ObserveRequest()
        {
            if (disposed || !isActiveAndEnabled || motor==null) return;
            var input=motor.InputRouter;
            var request=motor.LaboratoryMoveDirection;
            if (request.sqrMagnitude>.01f) rearm=false;
            if (Active || rearm || input==null || input.Mode!=TopDown3DInputMode.Gameplay || input.LaboratoryResetBlocked
                || paused?.Invoke()==true || otherBusy?.Invoke()==true || coordinator.IsBusy || !motor.IsGrounded || motor.IsMounted
                || motor.AnimationPosture!=AnimationPlaygroundPosture.Standing || input.AnimationSlowWalkHeld
                || input.LaboratoryStandingFacingHeld || motor.LaboratoryStandingFacingLocked
                || !driver.StandingStrafeReviewSupportAvailable()) return;
            var vitals=motor.GetComponent<TopDown3DSurvivalVitals>();
            if (vitals!=null && vitals.Health<=0) return;
            var velocity=Vector3.ProjectOnPlane(motor.Velocity,Vector3.up);
            var kind=profile.Select(velocity,motor.FacingDirection,request,false,motor.SprintActive);
            if (kind==AnimationPlaygroundLocomotionTransition.None) return;
            // Ordinary stopping keeps the responsive gait-to-idle envelope.
            // The long authored brake/pivot family is reserved for sprint intent.
            if(kind==AnimationPlaygroundLocomotionTransition.RunStop
                || kind>=AnimationPlaygroundLocomotionTransition.PivotLeft && !input.SprintHeld)return;
            take=profile.Take(kind); basis=Quaternion.LookRotation(velocity.normalized,Vector3.up);
            demand=request.normalized; previousTime=0;
            targetYaw=kind>=AnimationPlaygroundLocomotionTransition.PivotLeft ? Vector3.SignedAngle(velocity,request,Vector3.up) : 0;
            sourceRate=Mathf.Min(profile.maximumSpeed/take.maximumSourceSpeed,Mathf.Clamp(velocity.magnitude/take.entrySpeed,.4f,2.2f));
            if (kind>=AnimationPlaygroundLocomotionTransition.PivotLeft)
            {
                var requestedSpeed=motor.GroundedLocomotionRequestVelocity(coordinator).magnitude;
                if(requestedSpeed<profile.minimumSpeed || velocity.magnitude>requestedSpeed+.5f)return;
                sourceRate=Mathf.Min(sourceRate,requestedSpeed/take.maximumSourceSpeed);
            }
            if (!driver.TryCapturePoseBlend(this,profile.entryBlend,true)) return;
            var binding=new AnimationPlaygroundMotionBinding {sourceId="QualifiedLocomotion."+kind,clip=take.clip,
                readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
            if (!coordinator.TryBeginPhysicalSequence(this,binding,AnimationPlaygroundMotionMode.AlignedAction,null,Intent,null,
                seconds=>Math.Min(seconds*sourceRate,take.clip.length),looping:true,policy:AnimationPlaygroundMotionPolicy.DodgeLocomotion)) { driver.EndPoseBlend(this); return; }
            performing=true; Kind=kind;
            if (!driver.SetAnimationBodyProjection(coordinator,profile.bodyProjection) || !driver.SetAnimationDodgeProjection(coordinator)
                || !driver.SetAnimationGarmentFloor(coordinator,true,.6f,.01f)) Cancel();
        }
        private AnimationPlaygroundMotorIntent Intent(double seconds)
        {
            var time=Mathf.Min((float)seconds*sourceRate,take.clip.length);
            var velocity=basis*(take.Position(time)-take.Position(previousTime))/Time.fixedDeltaTime;
            var yaw=take.yaw.Evaluate(time);
            if (Pivot)
            {
                var nativeEnd=take.yaw.Evaluate(take.clip.length);
                yaw*=targetYaw/nativeEnd;
                velocity=Quaternion.Euler(0,(targetYaw-nativeEnd)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1,time/take.clip.length)),0)*velocity;
            }
            if (Pivot && time>=take.clip.length)
            {
                // Complete the unused part of this motor tick with the requested
                // run-out, rather than dividing a partial source interval by a full tick.
                var remaining=Mathf.Clamp01(1-(time-previousTime)/(sourceRate*Time.fixedDeltaTime));
                velocity+=Vector3.ProjectOnPlane(motor.DodgeRunOutVelocity(coordinator),Vector3.up)*remaining;
            }
            velocity=Vector3.ClampMagnitude(velocity,profile.maximumSpeed);
            var facing=basis*Quaternion.Euler(0,yaw,0)*Vector3.forward;
            previousTime=time; driver.SetPoseBlendElapsed(this,seconds);
            return new AnimationPlaygroundMotorIntent(velocity,facing,phase:time/take.clip.length,paused:paused?.Invoke()==true);
        }
        private bool Pivot => Kind>=AnimationPlaygroundLocomotionTransition.PivotLeft;
        private void Guard()
        {
            if (!performing) return;
            var input=motor.InputRouter; var request=motor.LaboratoryMoveDirection;
            var vitals=motor.GetComponent<TopDown3DSurvivalVitals>();
            if (paused?.Invoke()==true || input==null || input.Mode!=TopDown3DInputMode.Gameplay || input.LaboratoryResetBlocked
                || input.LaboratoryStandingFacingHeld || input.AnimationSlowWalkHeld || !motor.IsGrounded || motor.IsMounted
                || motor.AnimationSnapshot.Blocked || vitals!=null && vitals.Health<=0
                || !motor.TryAnimationGroundDistance(coordinator,.2f,out _)
                || Pivot && motor.DodgeRunOutVelocity(coordinator).magnitude+.05f<take.maximumSourceSpeed*sourceRate
                || (Pivot ? request.sqrMagnitude<.64f || Vector3.Dot(request.normalized,demand)<.9f : request.sqrMagnitude>=.01f))
                Finish(false);
        }
        private void AfterMotion() { if (performing && previousTime>=take.clip.length && coordinator.IsSequenceOwner(this)) Finish(true); }
        private void Finish(bool complete)
        {
            blending=driver.TryCapturePoseBlend(this,profile.exitBlend); blendTime=0;
            var runOutSpeed=motor.DodgeRunOutVelocity(coordinator).magnitude;
            coordinator.CancelPhysicalSequence(this); performing=false;
            if (complete && Pivot) driver.PrepareGroundedEntryRunOut(take.RunOutPhase(runOutSpeed,profile.sprintThreshold));
            rearm=complete && !Pivot;
        }
        private void AdvanceBlend()
        {
            if (disposed) return;
            if (performing && !coordinator.IsSequenceOwner(this)) { performing=false; driver.EndPoseBlend(this); }
            if (!blending || paused?.Invoke()==true) return;
            blendTime+=Time.fixedDeltaTime; driver.SetPoseBlendElapsed(this,blendTime);
            if (blendTime>=profile.exitBlend) { driver.EndPoseBlend(this); blending=false; Kind=AnimationPlaygroundLocomotionTransition.None; }
        }
        public bool TryYieldTo(object successor)
        {
            if(!Active || disposed || !driver.TryReservePoseBlendHandoff(this,successor,profile.exitBlend))return false;
            coordinator.CancelPhysicalSequence(this);driver.SelectReservedPoseBlend(successor);
            performing=blending=false;rearm=true;Kind=AnimationPlaygroundLocomotionTransition.None;
            return true;
        }
        public void Cancel()
        { coordinator?.CancelPhysicalSequence(this); driver?.EndPoseBlend(this); performing=blending=false; rearm=true; Kind=AnimationPlaygroundLocomotionTransition.None; }
        public void InvalidateForReset(uint epoch)=>Cancel();
        public bool TryRestoreForReset(out string reason) {Cancel();reason=null;return true;}
        private void OnDisable()=>Cancel();
        public void DisposeOwnedServices()
        {
            if (disposed) return; Cancel(); disposed=true;
            if (motor!=null) {motor.AnimationLocomotionPreparing-=ObserveRequest;motor.AnimationMotionPreparing-=Guard;motor.AnimationLocomotionAdvanced-=AdvanceBlend;}
            if (motor!=null) motor.AnimationMotionAdvanced-=AfterMotion;
            registry?.Unregister(this);
        }
        private void OnDestroy()=>DisposeOwnedServices();
    }
}
