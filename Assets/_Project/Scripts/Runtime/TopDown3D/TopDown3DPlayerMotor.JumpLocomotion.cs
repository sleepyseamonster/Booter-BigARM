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
        private float jumpGravityScale=1;
        private double jumpGravityAppliedTime=double.NegativeInfinity;
        private Collider jumpSupportCollider;
        private Vector3 jumpSupportNormal,jumpSupportPoint;
        private float jumpContactDownwardSpeed;
        private ulong jumpTouchdownId;
        private ContactPoint[] jumpContacts=new ContactPoint[16];
        private readonly Vector3[] jumpBlockingNormals=new Vector3[16];
        private int jumpBlockingCount;
        public bool JumpLocomotionActive=>animationMotionOwner!=null && animationMotionPolicy==AnimationPlaygroundMotionPolicy.JumpLocomotion;
        public bool JumpUsesGroundedLocomotion=>JumpLocomotionActive && jumpGroundedRequested;
        public Vector3 JumpControlVelocity=>animationWasPaused ? animationPausedVelocity : Velocity;
        public float JumpGravityMagnitude=>Mathf.Max(.01f,-Physics.gravity.y*(JumpLocomotionActive && jumpLaunched ? jumpGravityScale : 1));
        public float JumpCapsuleRadius=>capsule!=null ? capsule.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z)) : 0;
        public Vector3 JumpLaunchFacing=>standingStrafeLocked ? standingStrafeFacing
            : body!=null ? Vector3.ProjectOnPlane(body.rotation*Vector3.forward,Vector3.up).normalized : FacingDirection;
        public AnimationPlaygroundJumpObservation JumpObservation {get;private set;}

        // Presentation forecast only. This query never declares support, changes velocity,
        // or commits landing. Use the actual rounded capsule rather than a centre-foot ray.
        public bool TryPredictJumpSupport(object owner,float distance,out RaycastHit support)
        {
            support=default;
            if(!IsAnimationMotionOwner(owner) || !JumpLocomotionActive || !float.IsFinite(distance) || distance<=0 || distance>1)return false;
            GetWorldCapsule(body.position,1,out var top,out var bottom,out var radius);
            int count;
            while(true)
            {
                count=Physics.CapsuleCastNonAlloc(top,bottom,radius,Vector3.down,animationReturnHits,distance,groundMask,QueryTriggerInteraction.Ignore);
                if(count<animationReturnHits.Length)break;
                if(animationReturnHits.Length>=4096)return false;
                Array.Resize(ref animationReturnHits,animationReturnHits.Length*2);
            }
            var nearest=float.PositiveInfinity;
            for(var i=0;i<count;i++)
            {
                var hit=animationReturnHits[i];
                if(hit.collider==null || hit.collider.transform.IsChildOf(transform) || hit.distance>=nearest
                    || hit.collider.GetComponentInParent<TopDown3DGroundSurface>()==null)continue;
                // An initial overlap can report the cast's inverse direction as its normal.
                if(hit.distance<.0001f)
                {
                    if(!TryReadTrueSurfaceNormal(AnimationCurrentFootPoint()+Vector3.up*.2f,.5f,hit.collider,out var normal,out var point))continue;
                    hit.normal=normal;hit.point=point;
                }
                else if(TryReadTrueSurfaceNormal(AnimationCurrentFootPoint()+Vector3.up*.25f,distance+.35f,hit.collider,out var trueNormal,out var truePoint))
                {hit.normal=trueNormal;hit.point=truePoint;}
                if(Vector3.Angle(hit.normal,Vector3.up)>maxWalkableSlope)continue;
                nearest=hit.distance;support=hit;
            }
            if(support.collider!=null)return true;
            // Capsule casts omit colliders already overlapping their starting shape.
            // A true-normal centre probe supplies a presentation-only forecast there;
            // rounded-capsule contact can sit above that ray's floor on a slope.
            while(true)
            {
                count=Physics.RaycastNonAlloc(AnimationCurrentFootPoint()+Vector3.up*.25f,Vector3.down,
                    animationReturnHits,distance+.35f,groundMask,QueryTriggerInteraction.Ignore);
                if(count<animationReturnHits.Length)break;
                if(animationReturnHits.Length>=4096)return false;
                Array.Resize(ref animationReturnHits,animationReturnHits.Length*2);
            }
            for(var i=0;i<count;i++)
            {
                var hit=animationReturnHits[i];
                if(hit.collider==null || hit.collider.transform.IsChildOf(transform)
                    || hit.collider.GetComponentInParent<TopDown3DGroundSurface>()==null
                    || Vector3.Angle(hit.normal,Vector3.up)>maxWalkableSlope)continue;
                var gap=Mathf.Max(0,hit.distance-.25f-radius*(1/Mathf.Max(.01f,hit.normal.y)-1));
                if(gap>distance || gap>=nearest)continue;
                nearest=gap;hit.distance=gap;support=hit;
            }
            return support.collider!=null;
        }

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
            jumpGravityScale=1;jumpGravityAppliedTime=double.NegativeInfinity;
            JumpObservation=default;
            return true;
        }

        public bool SetJumpGroundedLocomotion(object owner,bool grounded)
        {
            if(!IsAnimationMotionOwner(owner) || !JumpLocomotionActive)return false;
            jumpGroundedRequested=grounded;return true;
        }
        public bool SetJumpGravityScale(object owner,float scale)
        {
            if(!IsAnimationMotionOwner(owner) || !JumpLocomotionActive || !float.IsFinite(scale) || scale<1 || scale>3)return false;
            jumpGravityScale=scale;return true;
        }
        private void ApplyJumpGravity()
        {
            if(!JumpLocomotionActive || !jumpLaunched || !animationGravity || animationIntent.Paused || jumpGravityScale==1
                || jumpGravityAppliedTime==Time.fixedTimeAsDouble)return;
            // World gravity supplies the first 1x. Add only this jump's excess
            // acceleration, once per simulation step, through the same body owner.
            body.AddForce(Physics.gravity*(jumpGravityScale-1),ForceMode.Acceleration);
            jumpGravityAppliedTime=Time.fixedTimeAsDouble;
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
            GuardStandingStrafeOwner();
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
                if(!supported)ApplyJumpGravity();
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
