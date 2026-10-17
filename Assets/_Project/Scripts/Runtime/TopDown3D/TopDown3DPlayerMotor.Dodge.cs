using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        private bool dodgePreserveGroundObservationOnRelease;

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
            if (!IsAnimationMotionOwner(owner) || !DodgeLocomotionActive || input == null
                || input.Mode != TopDown3DInputMode.Gameplay || input.LaboratoryResetBlocked) return Vector3.zero;
            var direction = LaboratoryMoveDirection;
            if (direction.sqrMagnitude < .000001f) return Vector3.zero;
            if (TryGetSurfaceNormalAhead(direction, out var ahead))
                direction = TopDown3DSlopeMath.RemoveSteepUphillComponent(direction, ahead, maxWalkableSlope);
            var speed = AnimationLocomotionSpeed(LaboratoryRequestedSpeed(input.SprintHeld));
            if (standingStrafeLocked && standingStrafeProfile != null && standingStrafeProfile.AllDirections)
                speed = Mathf.Min(speed, AnimationPlaygroundDirectionalMath.SpeedLimit(standingStrafeProfile,
                    AnimationPlaygroundDirectionalMath.Local(direction, standingStrafeFacing)));
            if (TryGetGroundNormal(out var normal))
            {
                var magnitude = direction.magnitude;
                direction = TopDown3DSlopeMath.ProjectDirectionOnSlope(direction, normal, maxWalkableSlope) * magnitude;
                speed = ApplyLaboratoryUphillGait(speed, direction, normal);
            }
            return direction * speed;
        }

        public void RestoreDodgeFacing(object owner)
        {
            if (IsAnimationMotionOwner(owner) && DodgeLocomotionActive && standingStrafeLocked
                && input != null && input.LaboratoryStandingFacingHeld) FacePlanarDirection(standingStrafeFacing);
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
