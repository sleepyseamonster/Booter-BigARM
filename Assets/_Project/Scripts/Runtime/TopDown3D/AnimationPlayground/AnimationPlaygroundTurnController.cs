using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundTurnController : MonoBehaviour,IAnimationPlaygroundResetParticipant
    {
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundContinuationProfile profile;
        private AnimationPlaygroundResetRegistry registry;
        private Func<bool> unavailable;
        private AnimationPlaygroundAdaptedTurn turn;
        private Vector3 origin, expected;
        private Quaternion basis;
        private Collider support;
        private Matrix4x4 supportMatrix;
        private bool settling,disposed,blockedUntilNeutral;
        private float settleSeconds;
        private const float EntryBlendSeconds=.05f, TurnSeconds=.38f, ExitBlendSeconds=.08f;
        private float SourceRate=>turn==null?1:turn.clip.length/TurnSeconds;
        public bool Active => turn != null || settling;
        public string LastFailure {get;private set;}
        public string ResetId => "CampLoop.Booter.AdaptedTurn";
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.Actor;

        public void Configure(TopDown3DPlayerMotor value,TopDown3DPlayerAnimationDriver presentation,
            AnimationPlaygroundActionCoordinator actions,AnimationPlaygroundContinuationProfile review,
            AnimationPlaygroundResetRegistry resets,Func<bool> busy)
        {
            if(motor!=null || value==null || presentation==null || actions==null || review==null || resets==null
                || !review.Validate(review.targetModel,review.footProjection,out _))throw new ArgumentException("Require the paired turn review.");
            motor=value;driver=presentation;coordinator=actions;profile=review;registry=resets;unavailable=busy;
            registry.Register(this);motor.AnimationLocomotionAdvanced+=ObserveLocomotion;motor.AnimationMotionAdvanced+=ObserveTurn;
            motor.ConfigureLaboratoryTurnReview(coordinator,true,driver.StandingStrafeReviewSupportAvailable);
        }
        private void ObserveLocomotion()
        {
            if(disposed || !isActiveAndEnabled || unavailable?.Invoke()==true)return;
            if(turn!=null && !coordinator.IsSequenceOwner(this))Cancel("Turn interrupted; release the stick to rearm.");
            if(motor.LaboratoryMoveDirection.sqrMagnitude<.0001f)blockedUntilNeutral=false;
            if(settling)
            {
                settleSeconds+=Time.fixedDeltaTime;driver.SetPoseBlendElapsed(this,settleSeconds);
                if(settleSeconds>=ExitBlendSeconds){driver.EndPoseBlend(this);motor.EndActionConstraint(this);settling=false;}
                return;
            }
            if(motor.LaboratoryTurnBraking)TryBeginTurn(motor.LaboratoryMoveDirection);
        }
        public bool TryBeginTurn(Vector3 direction)
        {
            if(disposed || !isActiveAndEnabled || motor==null || turn!=null || settling || blockedUntilNeutral || unavailable?.Invoke()==true
                || coordinator.IsBusy || motor.IsActionConstrained || motor.LaboratoryStandingFacingLocked
                || Vector3.ProjectOnPlane(motor.Velocity,Vector3.up).sqrMagnitude>.01f
                || !driver.StandingStrafeReviewSupportAvailable() || !motor.TryGetLaboratorySupportPlane(out var plane,true)
                || Vector3.Angle(plane.normal,Vector3.up)>.25f)return false;
            // Transform rotation can still be interpolating the preceding turn.
            // The curve, foot constraint and motor must share completed facing.
            var facing=Vector3.ProjectOnPlane(motor.FacingDirection,Vector3.up).normalized;
            var angle=Vector3.SignedAngle(facing,direction,Vector3.up);
            if(!float.IsFinite(direction.sqrMagnitude) || direction.sqrMagnitude<.01f || Mathf.Abs(angle)<150)return false;
            origin=motor.Position;basis=Quaternion.LookRotation(facing);expected=origin;
            support=motor.LaboratorySupportCollider;if(support==null)return false;supportMatrix=support.transform.localToWorldMatrix;
            turn=angle<0 ? profile.left : profile.right;
            if(!driver.TryCapturePoseBlend(this,EntryBlendSeconds)){turn=null;return false;}
            var binding=new AnimationPlaygroundMotionBinding{sourceId="CampReview.AdaptedTurn."+turn.clip.name,
                clip=turn.clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
            if(!coordinator.TryBeginPhysicalSequence(this,binding,AnimationPlaygroundMotionMode.Turn,null,Intent,null,
                seconds=>Math.Min(turn.clip.length,Math.Max(0,seconds-EntryBlendSeconds)*SourceRate),true))
            {driver.EndPoseBlend(this);turn=null;return false;}
            var anchor=origin+basis*new Vector3(turn.soleAnchor.x,0,turn.soleAnchor.y);
            try
            {
                if(!driver.TryBeginAdaptedTurnPose(coordinator,angle<0,anchor)) {Cancel("Turn foot constraint unavailable.");return false;}
            }
            catch(Exception error){Cancel("Turn foot constraint unavailable: "+error.Message);return false;}
            driver.SetAnimationFootProjection(coordinator,true);LastFailure=null;
            return true;
        }
        private AnimationPlaygroundMotorIntent Intent(double seconds)
        {
            driver.SetPoseBlendElapsed(this,Math.Min(EntryBlendSeconds,seconds));
            var current=Mathf.Min(turn.clip.length,Mathf.Max(0,(float)seconds-EntryBlendSeconds-Time.fixedDeltaTime)*SourceRate);
            turn.Sample(current,out var point,out _);expected=origin+basis*point;
            turn.Sample(Mathf.Min(Mathf.Max(0,(float)seconds-EntryBlendSeconds)*SourceRate,turn.clip.length),out var next,out var nextYaw);
            // Both position and rotation describe the completed physics step.
            // The render graph interpolates that same pair of source times.
            return new AnimationPlaygroundMotorIntent(basis*(next-point)/Time.fixedDeltaTime,basis*nextYaw*Vector3.forward);
        }
        private void ObserveTurn()
        {
            if(turn==null)return;
            if(!coordinator.IsSequenceOwner(this)){Cancel("Turn owner interrupted.");return;}
            if(unavailable?.Invoke()==true)return;
            if(support==null || !support.enabled || !support.gameObject.activeInHierarchy || support.attachedRigidbody!=null
                || support.transform.localToWorldMatrix!=supportMatrix || !motor.AnimationSnapshot.Supported
                || motor.AnimationSnapshot.Blocked || Vector3.ProjectOnPlane(motor.Position-expected,Vector3.up).magnitude>.015f)
            {Cancel("Turn travel or support was blocked; steer to resume.");return;}
            if(driver.LaboratoryTurnContactUnavailable)
            {Cancel("Turn contact left its bounded envelope; release the stick to rearm.");return;}
            if(coordinator.PhysicalStageSeconds<TurnSeconds+EntryBlendSeconds+Time.fixedDeltaTime*.5f)return;
            if(driver.TryCapturePoseBlend(this,ExitBlendSeconds))
            {
                coordinator.CancelPhysicalSequence(this);turn=null;
                if(motor.TryBeginActionConstraint(this)){settling=true;settleSeconds=0;return;}
                driver.EndPoseBlend(this);
            }
            Cancel(null);
        }
        // Capture before releasing the turn's physical lease, including its
        // recovery constraint. The successor receives the currently displayed pose.
        public bool TryYieldTo(object successor)
        {
            if(disposed || !Active || !driver.TryReservePoseBlendHandoff(this,successor,ExitBlendSeconds))return false;
            coordinator.CancelPhysicalSequence(this);motor.EndActionConstraint(this);
            driver.SelectReservedPoseBlend(successor);
            turn=null;settling=false;support=null;LastFailure=null;blockedUntilNeutral=true;
            return true;
        }
        public void Cancel(string reason)
        {
            coordinator?.CancelPhysicalSequence(this);driver?.EndPoseBlend(this);motor?.EndActionConstraint(this);
            turn=null;settling=false;support=null;LastFailure=reason;
            blockedUntilNeutral=reason!=null;
        }
        public void InvalidateForReset(uint epoch){Cancel(null);driver?.ResetLaboratoryContinuationReview();blockedUntilNeutral=true;}
        public bool TryRestoreForReset(out string reason){reason=null;return true;}
        private void OnDisable()=>Cancel(null);
        public void DisposeOwnedServices()
        {
            if(disposed)return;disposed=true;Cancel(null);
            if(motor!=null)
            {motor.AnimationLocomotionAdvanced-=ObserveLocomotion;motor.AnimationMotionAdvanced-=ObserveTurn;
                if(coordinator!=null && motor.IsAnimationPostureOwner(coordinator))motor.ConfigureLaboratoryTurnReview(coordinator,false);}
            registry?.Unregister(this);motor=null;driver=null;coordinator=null;
        }
        private void OnDestroy()=>DisposeOwnedServices();
    }
}
