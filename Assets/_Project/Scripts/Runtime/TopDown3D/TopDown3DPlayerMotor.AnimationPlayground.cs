using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundMotionMode { None, AlignedAction, Slide, Dodge, Ladder, Cover, Jump, Fall, Recovery, Dead }
    public enum AnimationPlaygroundPosture { Standing, Crouched, Prone }
    public readonly struct AnimationPlaygroundMotorIntent
    {
        public readonly Vector3 Velocity, Facing;
        public readonly bool OwnsVertical, Paused;
        public readonly float Phase;
        public AnimationPlaygroundMotorIntent(Vector3 velocity,Vector3 facing,bool ownsVertical=false,float phase=0,bool paused=false)
        {Velocity=velocity;Facing=facing;OwnsVertical=ownsVertical;Phase=phase;Paused=paused;}
    }
    public readonly struct AnimationPlaygroundMotorSnapshot
    {
        public readonly AnimationPlaygroundMotionMode Mode;
        public readonly AnimationPlaygroundPosture Posture;
        public readonly bool Supported, Blocked;
        public readonly Vector3 Velocity, Displacement, Facing;
        public readonly float Phase;
        public AnimationPlaygroundMotorSnapshot(AnimationPlaygroundMotionMode mode,AnimationPlaygroundPosture posture,bool support,bool blocked,
            Vector3 velocity,Vector3 displacement,Vector3 facing,float phase)
        {Mode=mode;Posture=posture;Supported=support;Blocked=blocked;Velocity=velocity;Displacement=displacement;Facing=facing;Phase=phase;}
    }
    public sealed partial class TopDown3DPlayerMotor
    {
        private object animationMotionOwner,animationPostureOwner;
        private AnimationPlaygroundMotorIntent animationIntent;
        private bool animationGravity,animationColliderEnabled,animationOriginalGravity;
        private bool animationWasPaused;
        private Vector3 animationPausedVelocity;
        private float animationOriginalHeight,animationOriginalRadius;
        private Vector3 animationOriginalCenter,animationPreviousPosition;
        private bool animationShapeCaptured;
        private Collider[] animationShapeHits=new Collider[16];
        private RaycastHit[] animationReturnHits=new RaycastHit[16];
        private int animationSupportReleaseTicks;
        private float animationWalkSpeed=1.5f,animationCrouchSpeed=1.2f;
        private bool laboratoryBlocksLegacyTraversal;
        public AnimationPlaygroundMotionMode AnimationMotionMode {get;private set;}
        public AnimationPlaygroundPosture AnimationPosture {get;private set;}
        public AnimationPlaygroundMotorSnapshot AnimationSnapshot {get;private set;}
        public event Action AnimationMotionPreparing,AnimationMotionAdvanced;

        public void BindAnimationPostureOwner(object owner)
        {
            if(owner == null || animationPostureOwner != null && !ReferenceEquals(animationPostureOwner,owner))
                throw new InvalidOperationException("Bind one animation posture owner.");
            EnsureAnimationShape();animationPostureOwner=owner;
        }
        public void ConfigureAnimationLocomotionSpeeds(object owner,float slowWalk,float crouch)
        {
            if(!ReferenceEquals(owner,animationPostureOwner) || !float.IsFinite(slowWalk) || !float.IsFinite(crouch)
                || slowWalk<=0 || crouch<=0 || slowWalk>walkSpeed || crouch>walkSpeed)throw new ArgumentException("Qualified lab speeds and the posture owner are required.");
            animationWalkSpeed=slowWalk;animationCrouchSpeed=crouch;
        }
        public void ConfigureLaboratorySpeeds(object owner,float slowWalk,float run,float sprint)
        {
            if(!ReferenceEquals(owner,animationPostureOwner) || !float.IsFinite(run) || !float.IsFinite(sprint)
                || !float.IsFinite(slowWalk) || slowWalk<=0 || run<slowWalk || sprint<run)
                throw new ArgumentException("The laboratory posture owner and finite ordered speeds are required.");
            walkSpeed=run;sprintSpeed=sprint;animationWalkSpeed=slowWalk;
        }
        public void BlockUnqualifiedLaboratoryTraversal(object owner)
        {
            if(!ReferenceEquals(owner,animationPostureOwner))throw new ArgumentException("Only the laboratory posture owner configures traversal qualification.");
            laboratoryBlocksLegacyTraversal=true;
        }
        public bool ReleaseAnimationPostureOwner(object owner)
        {
            if(owner==null || !ReferenceEquals(owner,animationPostureOwner))return false;
            TryRestoreAnimationPosture(owner);animationPostureOwner=null;laboratoryBlocksLegacyTraversal=false;return true;
        }
        private float AnimationLocomotionSpeed(float normalSpeed)
        {
            if(AnimationPosture==AnimationPlaygroundPosture.Prone)return 0;
            if(AnimationPosture==AnimationPlaygroundPosture.Crouched)return animationCrouchSpeed;
            if(animationPostureOwner==null)return normalSpeed;
            return input!=null && input.AnimationSlowWalkHeld ? animationWalkSpeed : normalSpeed;
        }
        public bool TryBeginAnimationMotion(object owner,AnimationPlaygroundMotionMode mode,bool disableGravity=false)
        {
            if(owner == null || mode == AnimationPlaygroundMotionMode.None || !Enum.IsDefined(typeof(AnimationPlaygroundMotionMode),mode)
                || animationMotionOwner != null || !TryBeginActionConstraint(owner))return false;
            EnsureAnimationShape();animationMotionOwner=owner;AnimationMotionMode=mode;
            animationOriginalGravity=body.useGravity;animationGravity=!disableGravity;
            animationColliderEnabled=capsule.enabled;animationPreviousPosition=Position;
            animationIntent=new AnimationPlaygroundMotorIntent(Vector3.zero,FacingDirection);
            body.useGravity=animationGravity;
            return true;
        }
        public bool SetAnimationMotorIntent(object owner,AnimationPlaygroundMotorIntent intent)
        {
            if(owner==null || !ReferenceEquals(owner,animationMotionOwner) || !Finite(intent.Velocity) || !Finite(intent.Facing) || !float.IsFinite(intent.Phase))return false;
            animationIntent=intent;return true;
        }
        public bool SetAnimationMotionMode(object owner,AnimationPlaygroundMotionMode mode,bool disableGravity=false)
        {
            if(owner==null || !ReferenceEquals(owner,animationMotionOwner) || mode==AnimationPlaygroundMotionMode.None
                || !Enum.IsDefined(typeof(AnimationPlaygroundMotionMode),mode))return false;
            AnimationMotionMode=mode;animationGravity=!disableGravity;return true;
        }
        public bool ReleaseAnimationMotion(object owner)
        {
            if(owner==null || !ReferenceEquals(owner,animationMotionOwner))return false;
            ReleaseAnimationMotionCore();EndActionConstraint(owner);return true;
        }
        public bool TrySetAnimationPosture(object owner,AnimationPlaygroundPosture posture,float height)
        {
            if(owner==null || !ReferenceEquals(owner,animationPostureOwner) && !ReferenceEquals(owner,animationMotionOwner))return false;
            EnsureAnimationShape();
            if(!Enum.IsDefined(typeof(AnimationPlaygroundPosture),posture))return false;
            if(posture==AnimationPlaygroundPosture.Standing)
                return Mathf.Approximately(height,animationOriginalHeight) && TryRestoreAnimationPosture(owner);
            var radius=posture==AnimationPlaygroundPosture.Prone ? Mathf.Min(animationOriginalRadius,height*.5f) : animationOriginalRadius;
            if(!float.IsFinite(height) || height<=.1f || height<2*radius || height>animationOriginalHeight)return false;
            var center=capsule.center+Vector3.up*(height-capsule.height)*.5f;
            if((height>capsule.height || radius>capsule.radius) && !AnimationShapeClear(height,center,Position,radius))return false;
            capsule.height=height;capsule.radius=radius;capsule.center=center;AnimationPosture=posture;return true;
        }
        public bool TryRestoreAnimationPosture(object owner)
        {
            if(owner==null || !ReferenceEquals(owner,animationPostureOwner) && !ReferenceEquals(owner,animationMotionOwner))return false;
            EnsureAnimationShape();
            if(!AnimationShapeClear(animationOriginalHeight,animationOriginalCenter,Position,animationOriginalRadius))return false;
            capsule.height=animationOriginalHeight;capsule.radius=animationOriginalRadius;capsule.center=animationOriginalCenter;AnimationPosture=AnimationPlaygroundPosture.Standing;
            return true;
        }
        public bool IsAnimationReturnClear(Vector3 position)
        {EnsureAnimationShape();return Finite(position) && AnimationShapeClear(animationOriginalHeight,animationOriginalCenter,position,animationOriginalRadius);}
        public bool IsAnimationReturnSupported(Vector3 position)
        {
            EnsureAnimationShape();
            if(!Finite(position))return false;
            var foot=position+Vector3.up*(animationOriginalCenter.y-animationOriginalHeight*.5f);
            int count;
            while(true)
            {
                count=Physics.RaycastNonAlloc(foot+Vector3.up*.1f,Vector3.down,animationReturnHits,.22f,groundMask,QueryTriggerInteraction.Ignore);
                if(count<animationReturnHits.Length)break;
                if(animationReturnHits.Length>=4096)return false;
                Array.Resize(ref animationReturnHits,animationReturnHits.Length*2);
            }
            for(var i=0;i<count;i++)
            {
                var hit=animationReturnHits[i];
                if(hit.collider!=null && !hit.collider.transform.IsChildOf(transform) && Vector3.Angle(hit.normal,Vector3.up)<=maxWalkableSlope)
                    return true;
            }
            return false;
        }
        public bool TryResetAnimationActor(object owner,Vector3 position,Vector3 facing)
        {
            if(owner==null || !ReferenceEquals(owner,animationPostureOwner) || !Finite(facing)
                || actionConstraintOwner!=null && !ReferenceEquals(actionConstraintOwner,owner)
                || !IsAnimationReturnClear(position))return false;
            ReleaseAnimationMotion(owner);
            EndActionConstraint(owner);
            Teleport(position);
            capsule.height=animationOriginalHeight;capsule.radius=animationOriginalRadius;capsule.center=animationOriginalCenter;
            AnimationPosture=AnimationPlaygroundPosture.Standing;
            FacePlanarDirection(facing);
            return true;
        }
        public bool TryAnimationTakeoff(object owner,float upwardSpeed)
        {
            if(owner==null || !ReferenceEquals(owner,animationMotionOwner) || !IsGrounded || !float.IsFinite(upwardSpeed) || upwardSpeed<=0)return false;
            body.useGravity=true;animationGravity=true;body.linearVelocity=new Vector3(body.linearVelocity.x,upwardSpeed,body.linearVelocity.z);
            animationSupportReleaseTicks=2;IsGrounded=false;return true;
        }
        private void EnsureAnimationShape()
        {
            EnsureBody();if(capsule==null)capsule=GetComponent<CapsuleCollider>();
            if(capsule.direction!=1)throw new InvalidOperationException("Animation posture requires a vertical capsule.");
            if(animationShapeCaptured)return;
            animationOriginalHeight=capsule.height;animationOriginalRadius=capsule.radius;animationOriginalCenter=capsule.center;animationShapeCaptured=true;
        }
        private bool AnimationShapeClear(float height,Vector3 center,Vector3 position,float shapeRadius)
        {
            var scale=transform.lossyScale;
            var radius=Mathf.Max(.01f,shapeRadius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z))-.02f);
            var half=Mathf.Max(radius,height*Mathf.Abs(scale.y)*.5f-.02f);
            var middle=position+transform.rotation*Vector3.Scale(center,scale);
            var a=middle+Vector3.up*(half-radius);var b=middle-Vector3.up*(half-radius);
            int count;
            while((count=Physics.OverlapCapsuleNonAlloc(a,b,radius,animationShapeHits,~0,QueryTriggerInteraction.Ignore))==animationShapeHits.Length)
                Array.Resize(ref animationShapeHits,checked(animationShapeHits.Length*2));
            for(var i=0;i<count;i++)
                if(animationShapeHits[i]!=null && !animationShapeHits[i].transform.IsChildOf(transform))return false;
            return true;
        }
        private bool AdvanceAnimationMotion()
        {
            if(animationMotionOwner==null)return false;
            if(animationMotionOwner is UnityEngine.Object owner && owner==null){ReleaseAnimationMotionCore();actionConstraintOwner=null;return false;}
            if(!ReferenceEquals(actionConstraintOwner,animationMotionOwner))throw new InvalidOperationException("Animation motion lost its motor lease.");
            AnimationMotionPreparing?.Invoke();
            if(animationMotionOwner==null)return false;
            var desired=animationIntent.Velocity;
            body.useGravity=animationGravity && !animationIntent.Paused;
            if(animationIntent.Paused)
            {
                if(!animationWasPaused)animationPausedVelocity=body.linearVelocity;
                desired=Vector3.zero;animationWasPaused=true;
            }
            else
            {
                if(!animationIntent.OwnsVertical)desired.y=animationWasPaused ? animationPausedVelocity.y : body.linearVelocity.y;
                animationWasPaused=false;
            }
            var motion=animationIntent.OwnsVertical ? desired : Vector3.ProjectOnPlane(desired,Vector3.up);
            var blocked=false;
            if(motion.sqrMagnitude>.0001f && body.SweepTest(motion.normalized,out var hit,motion.magnitude*Time.fixedDeltaTime+.025f,QueryTriggerInteraction.Ignore))
            {
                var permitted=Mathf.Max(0,hit.distance-.025f)/Mathf.Max(.0001f,Time.fixedDeltaTime);
                var constrained=motion.normalized*Mathf.Min(motion.magnitude,permitted);
                if(animationIntent.OwnsVertical)desired=constrained;else{desired.x=constrained.x;desired.z=constrained.z;}
                blocked=true;
            }
            if(animationIntent.Facing.sqrMagnitude>.0001f)FacePlanarDirection(animationIntent.Facing);
            body.linearVelocity=desired;body.angularVelocity=Vector3.zero;SprintActive=false;PlanarAcceleration=Vector3.zero;
            var supported=TryGetGroundNormal(out var normal);
            if(animationSupportReleaseTicks>0){if(!animationIntent.Paused)animationSupportReleaseTicks--;supported=false;}
            IsGrounded=supported && body.linearVelocity.y<=.1f;
            if(IsGrounded){stableGroundNormal=normal;hasStableGroundNormal=true;}
            var displacement=Position-animationPreviousPosition;animationPreviousPosition=Position;
            AnimationSnapshot=new AnimationPlaygroundMotorSnapshot(AnimationMotionMode,AnimationPosture,IsGrounded,blocked,body.linearVelocity,displacement,FacingDirection,Mathf.Clamp01(animationIntent.Phase));
            var planar=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up);PublishPresentationSnapshot(planar,Vector3.ProjectOnPlane(animationIntent.Velocity,Vector3.up),true);
            AnimationMotionAdvanced?.Invoke();
            return true;
        }
        private void ReleaseAnimationMotionCore()
        {
            if(animationMotionOwner==null)return;
            if(body!=null){body.useGravity=animationOriginalGravity;body.linearVelocity=Vector3.up*(animationWasPaused ? animationPausedVelocity.y : body.linearVelocity.y);body.angularVelocity=Vector3.zero;}
            if(capsule!=null)capsule.enabled=animationColliderEnabled;
            animationMotionOwner=null;AnimationMotionMode=AnimationPlaygroundMotionMode.None;animationIntent=default;animationSupportReleaseTicks=0;animationWasPaused=false;animationPausedVelocity=default;AnimationSnapshot=default;
        }
        private void DisableAnimationCapability()
        {
            ReleaseAnimationMotionCore();
            if(animationShapeCaptured && capsule!=null && AnimationShapeClear(animationOriginalHeight,animationOriginalCenter,Position,animationOriginalRadius))
            {capsule.height=animationOriginalHeight;capsule.radius=animationOriginalRadius;capsule.center=animationOriginalCenter;AnimationPosture=AnimationPlaygroundPosture.Standing;}
            animationPostureOwner=null;
        }
        private static bool Finite(Vector3 value)=>float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
