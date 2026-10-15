using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        private readonly AnimationPlaygroundGroundedMotionTracker groundedMotionTracker = new();
        private Collider laboratoryGroundSupport;
        private Collider observedGroundSupport;
        private Matrix4x4 observedSupportMatrix;
        private Plane laboratoryGroundPlane, completedLaboratoryGroundPlane;
        private bool hasCompletedLaboratoryGroundPlane;
        private bool laboratoryUphillGaitEnabled;
        public AnimationPlaygroundGroundedMotion LaboratoryGroundedMotion { get; private set; }
        public event Action<AnimationPlaygroundGroundedMotion> LaboratoryGroundedMotionCompleted;
        public Collider LaboratorySupportCollider => TryGetLaboratorySupportPlane(out _, true) ? observedGroundSupport : null;

        // Author-approved Camp Loop policy. No persisted state, new motor or
        // animation clock: only the existing motor's requested speed changes.
        private float ApplyLaboratoryUphillGait(float requestedSpeed, Vector3 direction, Vector3 normal)
        {
            if (!laboratoryUphillGaitEnabled || animationPostureOwner == null || !IsGrounded
                || laboratoryPostureSuspended || AnimationPosture != AnimationPlaygroundPosture.Standing
                || laboratoryGroundSupport == null || laboratoryGroundSupport.attachedRigidbody != null)
                return requestedSpeed;
            var limit = AnimationPlaygroundUphillGait.SpeedLimit(requestedSpeed, animationWalkSpeed,
                direction, normal, maxWalkableSlope);
            if (limit <= animationWalkSpeed || direction.sqrMagnitude < .000001f) return limit;

            // One bounded nonallocating ray, parallel to the current support.
            // Unlike a downward probe at a distant point, this detects the front
            // of a short rising ramp. Its distance does not retreat while braking.
            GetWorldCapsule(body.position, 1, out _, out var bottom, out var radius);
            var topSpeed = Mathf.Max(sprintSpeed, laboratoryGamepadSprintSpeed);
            var distance = Mathf.Min(2, radius + Mathf.Max(0, topSpeed * topSpeed - animationWalkSpeed * animationWalkSpeed)
                / (2 * Mathf.Max(.1f, deceleration)) + topSpeed * Time.fixedDeltaTime * 2 + .1f);
            var origin = laboratoryGroundPlane.ClosestPointOnPlane(bottom) + normal.normalized * .06f;
            var tangent = TopDown3DSlopeMath.ProjectDirectionOnSlope(direction, normal, maxWalkableSlope);
            var count = Physics.RaycastNonAlloc(origin, tangent.normalized, groundHits, distance,
                groundMask, QueryTriggerInteraction.Ignore);
            var nearest = float.PositiveInfinity;
            var ahead = normal;
            for (var i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)
                    || hit.collider.attachedRigidbody != null || hit.distance >= nearest
                    || hit.collider.GetComponentInParent<TopDown3DGroundSurface>() == null) continue;
                nearest = hit.distance; ahead = hit.normal;
            }
            return Mathf.Min(limit, AnimationPlaygroundUphillGait.SpeedLimit(requestedSpeed,
                animationWalkSpeed, direction, ahead, maxWalkableSlope));
        }

        // The existing normal ray supplies this plane. Reading it adds no query
        // and cannot return a support from another completed tick or owner.
        public bool TryGetLaboratorySupportPlane(out Plane plane, bool allowRebasedPose = false)
        {
            plane = completedLaboratoryGroundPlane;
            return hasCompletedLaboratoryGroundPlane && (LaboratoryGroundedMotion.Available
                || allowRebasedPose && LaboratoryGroundedMotion.Rebased)
                && IsGrounded && observedGroundSupport != null && observedGroundSupport == laboratoryGroundSupport
                && observedGroundSupport.enabled && observedGroundSupport.gameObject.activeInHierarchy
                && observedGroundSupport.attachedRigidbody == null
                && observedGroundSupport.transform.localToWorldMatrix == observedSupportMatrix;
        }

        private void ResetStandingMotionObservation()
        {
            groundedMotionTracker.Reset(); LaboratoryGroundedMotion = default;
            observedGroundSupport = null; laboratoryGroundSupport = null;
            hasCompletedLaboratoryGroundPlane = false;
        }

        private void ObserveCompletedLaboratoryMotion()
            => ObserveCompletedLaboratoryMotionAt(Time.fixedTimeAsDouble);

        private void ObserveCompletedLaboratoryMotionAt(double completedTime)
        {
            hasCompletedLaboratoryGroundPlane = false;
            if (animationPostureOwner == null) return;
            var support = laboratoryGroundSupport;
            var eligible = body != null && IsGrounded && !IsMounted && !IsActionConstrained
                && !WaitingForPopulation && !laboratoryPostureSuspended && animationMotionOwner == null
                && AnimationPosture == AnimationPlaygroundPosture.Standing
                && ActiveTraversal == TopDown3DTraversalMove.None && climbMode < TopDown3DClimbMode.Scramble
                && support != null && support.attachedRigidbody == null;
            if (support != null)
            {
                var matrix = support.transform.localToWorldMatrix;
                if (observedGroundSupport == support && matrix != observedSupportMatrix) eligible = false;
                observedGroundSupport = support; observedSupportMatrix = matrix;
            }
            // This executes after motor commands, before the next physics step.
            // Rigidbody.position is still the completed prior step; Transform may be interpolated.
            LaboratoryGroundedMotion = groundedMotionTracker.Observe(Position, completedTime,
                eligible, support != null ? support.GetInstanceID() : 0,
                hasStableGroundNormal ? stableGroundNormal : Vector3.up);
            if (eligible && (LaboratoryGroundedMotion.Available || LaboratoryGroundedMotion.Rebased))
            {
                completedLaboratoryGroundPlane = laboratoryGroundPlane;
                hasCompletedLaboratoryGroundPlane = true;
            }
            LaboratoryGroundedMotionCompleted?.Invoke(LaboratoryGroundedMotion);
        }
    }

    public static class AnimationPlaygroundUphillGait
    {
        public const float BeginGrade = 32, WalkingGrade = 40;

        public static float SpeedLimit(float requestedSpeed, float walkingSpeed, Vector3 direction,
            Vector3 normal, float maximumSlope)
        {
            if (requestedSpeed <= walkingSpeed || !TopDown3DSlopeMath.IsWalkable(normal, maximumSlope)) return requestedSpeed;
            var tangent = TopDown3DSlopeMath.ProjectDirectionOnSlope(direction, normal, maximumSlope);
            var grade = Mathf.Atan2(tangent.y, new Vector2(tangent.x, tangent.z).magnitude) * Mathf.Rad2Deg;
            var weight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(BeginGrade, WalkingGrade, grade));
            return Mathf.Lerp(requestedSpeed, walkingSpeed, weight);
        }
    }
}
