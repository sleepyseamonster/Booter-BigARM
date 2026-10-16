using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct AnimationPlaygroundJumpObservation
    {
        public readonly double CompletedTime;
        public readonly Vector3 Position, Velocity, SupportNormal;
        public readonly bool Supported, Touchdown;
        public readonly ulong TouchdownId;
        public readonly float DownwardImpactSpeed;
        public AnimationPlaygroundJumpObservation(double time,Vector3 position,Vector3 velocity,Vector3 normal,
            bool supported,bool touchdown,ulong id,float impact)
        {CompletedTime=time;Position=position;Velocity=velocity;SupportNormal=normal;Supported=supported;
            Touchdown=touchdown;TouchdownId=id;DownwardImpactSpeed=impact;}
    }

    public sealed partial class TopDown3DPlayerMotor
    {
        private bool jumpGroundedRequested,jumpLaunched,jumpDeparted,jumpPreserveGroundObservationOnRelease;
        private double jumpLaunchTime=double.NegativeInfinity,jumpContactTime=double.NegativeInfinity;
        private Collider jumpSupportCollider;
        private Vector3 jumpSupportNormal,jumpSupportPoint;
        private float jumpContactDownwardSpeed;
        private ulong jumpTouchdownId;
        private ContactPoint[] jumpContacts=new ContactPoint[16];
        private readonly Vector3[] jumpBlockingNormals=new Vector3[16];
        private int jumpBlockingCount;
        public bool JumpLocomotionActive=>animationMotionOwner!=null && animationMotionPolicy==AnimationPlaygroundMotionPolicy.JumpLocomotion;
        public bool JumpUsesGroundedLocomotion=>JumpLocomotionActive && jumpGroundedRequested;
        public AnimationPlaygroundJumpObservation JumpObservation {get;private set;}

        public bool CanBeginJumpMotion(object owner)
            =>owner!=null && ReferenceEquals(owner,animationPostureOwner) && animationMotionOwner==null && actionConstraintOwner==null
                && !IsMounted && ActiveTraversal==TopDown3DTraversalMove.None && climbMode==TopDown3DClimbMode.None
                && !laboratoryPostureSuspended && AnimationPosture==AnimationPlaygroundPosture.Standing;

        private bool TryBeginJumpConstraint(object owner)
        {
            if(!CanBeginJumpMotion(owner))return false;
            EnsureBody();EnsureAnimationShape();
            actionConstraintOwner=owner;
            jumpGroundedRequested=IsGrounded;jumpLaunched=false;jumpDeparted=!IsGrounded;
            jumpLaunchTime=double.NegativeInfinity;jumpPreserveGroundObservationOnRelease=false;
            JumpObservation=default;
            return true;
        }

        public bool SetJumpGroundedLocomotion(object owner,bool grounded)
        {
            if(!IsAnimationMotionOwner(owner) || !JumpLocomotionActive)return false;
            jumpGroundedRequested=grounded;return true;
        }

        // Physics callbacks collect completed contacts; they never write the body or commit gameplay effects.
        private void OnCollisionEnter(Collision collision)=>RecordJumpContacts(collision);
        private void OnCollisionStay(Collision collision)=>RecordJumpContacts(collision);
        private void RecordJumpContacts(Collision collision)
        {
            if(collision==null || collision.collider==null || (groundMask.value & (1<<collision.collider.gameObject.layer))==0)return;
            if(collision.contactCount>jumpContacts.Length)Array.Resize(ref jumpContacts,collision.contactCount);
            var count=collision.GetContacts(jumpContacts);
            var time=Time.fixedTimeAsDouble;
            if(time!=jumpContactTime)
            {
                jumpContactTime=time;jumpSupportCollider=null;jumpSupportNormal=Vector3.zero;
                jumpContactDownwardSpeed=0;jumpBlockingCount=0;
            }
            for(var i=0;i<count;i++)
            {
                var point=jumpContacts[i];var normal=point.normal;
                if(Vector3.Angle(normal,Vector3.up)<=maxWalkableSlope
                    && collision.collider.GetComponentInParent<TopDown3DGroundSurface>()!=null)
                {
                    if(jumpSupportCollider==null || normal.y>jumpSupportNormal.y)
                    {jumpSupportCollider=collision.collider;jumpSupportNormal=normal;jumpSupportPoint=point.point;}
                    // Fixed terrain: retain the existing vertical-speed damage metric. Abs removes the
                    // callback's relative-velocity ordering convention; support and departure gate impact.
                    jumpContactDownwardSpeed=Mathf.Max(jumpContactDownwardSpeed,Mathf.Abs(collision.relativeVelocity.y));
                }
                else if(jumpBlockingCount<jumpBlockingNormals.Length)jumpBlockingNormals[jumpBlockingCount++]=normal;
            }
        }

        private bool AdvanceJumpLocomotion()
        {
            if(!JumpLocomotionActive)return false;
            if(animationMotionOwner is UnityEngine.Object owner && owner==null)
            {ReleaseAnimationMotionCore();actionConstraintOwner=null;return false;}
            if(!ReferenceEquals(actionConstraintOwner,animationMotionOwner))throw new InvalidOperationException("Jump motion lost its motor lease.");
            var completedPosition=Position;var completedVelocity=body.linearVelocity;
            var fresh=jumpContactTime<=Time.fixedTimeAsDouble && Time.fixedTimeAsDouble-jumpContactTime<=Time.fixedDeltaTime*1.5
                && jumpContactTime>jumpLaunchTime;
            var supported=fresh && jumpSupportCollider!=null && jumpSupportCollider.enabled
                && jumpSupportCollider.gameObject.activeInHierarchy;
            var touchdown=supported && jumpDeparted;
            var previousObservation=JumpObservation;
            JumpObservation=new AnimationPlaygroundJumpObservation(Time.fixedTimeAsDouble,completedPosition,completedVelocity,
                supported ? jumpSupportNormal : Vector3.up,supported,touchdown,jumpTouchdownId+(touchdown ? 1UL : 0UL),
                touchdown ? jumpContactDownwardSpeed : 0);

            AnimationMotionPreparing?.Invoke();
            if(!JumpLocomotionActive)return false;
            var paused=animationIntent.Paused;
            if(paused)
            {
                JumpObservation=new AnimationPlaygroundJumpObservation(Time.fixedTimeAsDouble,completedPosition,completedVelocity,
                    previousObservation.SupportNormal,previousObservation.Supported,false,jumpTouchdownId,0);
                if(!animationWasPaused)animationPausedVelocity=completedVelocity;
                animationWasPaused=true;body.useGravity=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            }
            else
            {
                if(touchdown)jumpTouchdownId++;
                jumpDeparted=!supported;
                AdvanceUnpausedJump(supported,fresh,completedVelocity);
            }
            var displacement=completedPosition-animationPreviousPosition;animationPreviousPosition=completedPosition;
            AnimationSnapshot=new AnimationPlaygroundMotorSnapshot(AnimationMotionMode,AnimationPosture,JumpObservation.Supported,false,
                completedVelocity,displacement,FacingDirection,Mathf.Clamp01(animationIntent.Phase));
            AnimationMotionAdvanced?.Invoke();
            return true;
        }

        private void AdvanceUnpausedJump(bool supported,bool fresh,Vector3 completedVelocity)
        {
            if(jumpGroundedRequested && (supported || !jumpLaunched && IsGrounded))
            {
                if(animationWasPaused)body.linearVelocity=animationPausedVelocity;
                animationWasPaused=false;body.useGravity=animationGravity;
                AdvanceMotorFixedTick(true);
            }
            else
            {
                var desired=animationIntent.Velocity;
                desired.y=animationWasPaused ? animationPausedVelocity.y : completedVelocity.y;
                animationWasPaused=false;body.useGravity=animationGravity;
                if(fresh)
                    for(var i=0;i<jumpBlockingCount;i++)
                    {
                        var blocking=Vector3.ProjectOnPlane(jumpBlockingNormals[i],Vector3.up).normalized;
                        var into=Vector3.Dot(desired,blocking);
                        if(into<0)desired-=blocking*into;
                    }
                // The existing continuous Rigidbody collision solver owns flight. A horizontal sweep
                // cannot classify the walkable capsule support and must not erase the planar vector.
                body.linearVelocity=desired;body.angularVelocity=Vector3.zero;
                IsGrounded=supported;
                if(supported)
                {
                    stableGroundNormal=jumpSupportNormal;hasStableGroundNormal=true;
                    laboratoryGroundSupport=jumpSupportCollider;laboratoryGroundPlane=new Plane(jumpSupportNormal,jumpSupportPoint);
                }
                if(animationIntent.Facing.sqrMagnitude>.0001f)FacePlanarDirection(animationIntent.Facing);
                PublishPresentationSnapshot(Vector3.ProjectOnPlane(completedVelocity,Vector3.up),
                    Vector3.ProjectOnPlane(desired,Vector3.up),true);
            }
        }
    }
}
