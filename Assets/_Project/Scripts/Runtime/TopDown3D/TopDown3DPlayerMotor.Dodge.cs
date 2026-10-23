using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        private bool dodgePreserveGroundObservationOnRelease;

        private float dodgeRunOutRemaining;
        public bool BeginDodgeRunOut(object owner)
        {
            if(!CanBeginDodgeMotion(owner))return false;
            dodgeRunOutRemaining=.2f;return true;
        }
        public void EndDodgeRunOut(object owner)
        {if(IsAnimationPostureOwner(owner))ClearDodgeRunOut();}
        private void ClearDodgeRunOut()=>dodgeRunOutRemaining=0;
        private float SelectDodgeRunOutAcceleration(float ordinaryRate,Vector3 target)
        {
            if(dodgeRunOutRemaining<=.000001f || animationPostureOwner==null || !IsGrounded || IsActionConstrained)
            {ClearDodgeRunOut();return ordinaryRate;}
            dodgeRunOutRemaining=Mathf.Max(0,dodgeRunOutRemaining-Time.fixedDeltaTime);
            // A short grounded handoff accepts late steering after the feet
            // recover. The normal motor still writes and caps all velocity.
            return target.sqrMagnitude>.0001f ? Mathf.Max(ordinaryRate,40f) : ordinaryRate;
        }

        public bool DodgeLocomotionActive => animationMotionOwner != null
            && animationMotionPolicy == AnimationPlaygroundMotionPolicy.DodgeLocomotion;

        public bool CanBeginDodgeMotion(object owner) => isActiveAndEnabled && CanBeginJumpMotion(owner)
            && IsGrounded && AnimationPosture == AnimationPlaygroundPosture.Standing;

        private bool TryBeginDodgeConstraint(object owner)
        {
            if (!CanBeginDodgeMotion(owner)) return false;
            EnsureBody(); EnsureAnimationShape(); actionConstraintOwner = owner;
            return true;
        }

        public Vector3 DodgeRunOutVelocity(object owner)
        {
            if (!IsAnimationMotionOwner(owner) || !DodgeLocomotionActive) return Vector3.zero;
            return CalculateGroundedLocomotionRequestVelocity(false);
        }

        // Read the same bounded request before taking a physical presentation lease.
        // Rejected transitions must leave the outgoing graph and motor untouched.
        public Vector3 GroundedLocomotionRequestVelocity(object owner)
        {
            if (!IsAnimationPostureOwner(owner)) return Vector3.zero;
            return CalculateGroundedLocomotionRequestVelocity(true);
        }

        private Vector3 CalculateGroundedLocomotionRequestVelocity(bool beforeAcquire)
        {
            if (input == null
                || input.Mode != TopDown3DInputMode.Gameplay || input.LaboratoryResetBlocked) return Vector3.zero;
            var direction = LaboratoryMoveDirection;
            if (direction.sqrMagnitude < .000001f) return Vector3.zero;
            if (TryGetSurfaceNormalAhead(direction, out var ahead))
                direction = TopDown3DSlopeMath.RemoveSteepUphillComponent(direction, ahead, maxWalkableSlope);
            var speed = AnimationLocomotionSpeed(LaboratoryRequestedSpeed(input.SprintHeld));
            if (standingStrafeLocked && standingStrafeProfile != null && standingStrafeProfile.AllDirections)
                speed = Mathf.Min(speed, AnimationPlaygroundDirectionalMath.SpeedLimit(standingStrafeProfile,
                    AnimationPlaygroundDirectionalMath.Local(direction, standingStrafeFacing)));
            Vector3 normal;bool grounded;
            if(beforeAcquire)
            {
                // The completed support already gates transition admission.
                // A fresh physical query here would overwrite that observation.
                grounded=TryGetLaboratorySupportPlane(out var plane,true);normal=plane.normal;
                if(!grounded)return Vector3.zero;
            }
            else grounded=TryGetGroundNormal(out normal);
            if (grounded)
            {
                var magnitude = direction.magnitude;
                direction = TopDown3DSlopeMath.ProjectDirectionOnSlope(direction, normal, maxWalkableSlope) * magnitude;
                speed = ApplyLaboratoryUphillGait(speed, direction, normal);
            }
            return direction * speed;
        }

        public void RestoreDodgeFacing(object owner)
        {
            if(!IsAnimationMotionOwner(owner) || !DodgeLocomotionActive || input==null || !input.LaboratoryStandingFacingHeld)return;
            // Crouch clears the upright facing lease. A supported action that
            // stands back up must return a still-armed held lock to locomotion.
            if(!standingStrafeLocked && standingStrafeProfile!=null && standingStrafeProfile.AllDirections
                && IsGrounded && AnimationPosture==AnimationPlaygroundPosture.Standing && !laboratoryPostureSuspended
                && input.Mode==TopDown3DInputMode.Gameplay && !input.LaboratoryResetBlocked && !input.LaboratoryStandingFacingDisarmed)
            {
                var planar=Vector3.ProjectOnPlane(facingDirection,Vector3.up);
                if(planar.sqrMagnitude>.000001f)
                {standingStrafeFacing=planar.normalized;standingStrafeLocked=true;standingStrafeAwaitRelease=false;}
            }
            if(standingStrafeLocked)FacePlanarDirection(standingStrafeFacing);
        }

        // The ordinary upright capsule remains authoritative. Walkable ground is
        // a support, rather than an obstacle reported by a horizontal sweep.
        private Vector3 ConstrainDodgeVelocity(Vector3 desired, out bool blocked)
        {
            blocked = false;
            if (animationIntent.Paused) return Vector3.zero;
            var planar = Vector3.ProjectOnPlane(desired, Vector3.up);
            if (planar.sqrMagnitude > .000001f && TryGetSurfaceNormalAhead(planar, out var ahead))
                planar = TopDown3DSlopeMath.RemoveSteepUphillComponent(planar, ahead, maxWalkableSlope);
            if (TryGetGroundNormal(out var normal))
            {
                var tangent = TopDown3DSlopeMath.ProjectDirectionOnSlope(planar, normal, maxWalkableSlope) * planar.magnitude;
                desired = tangent;
                body.AddForce(-Vector3.ProjectOnPlane(Physics.gravity, normal), ForceMode.Acceleration);
            }
            else { desired.x = planar.x; desired.z = planar.z; }
            if (desired.sqrMagnitude <= .000001f) return desired;
            var speed = desired.magnitude;
            var permitted = speed;
            foreach (var hit in body.SweepTestAll(desired / speed, speed * Time.fixedDeltaTime + .025f, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)
                    || Vector3.Dot(hit.normal, desired) >= -.0001f
                    || hit.collider.GetComponentInParent<TopDown3DGroundSurface>() != null
                        && TopDown3DSlopeMath.IsWalkable(hit.normal, maxWalkableSlope)) continue;
                permitted = Mathf.Min(permitted, Mathf.Max(0, hit.distance - .025f) / Time.fixedDeltaTime);
                blocked = true;
            }
            return desired * (permitted / speed);
        }
    }
}
