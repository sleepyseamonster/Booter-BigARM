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
        public AnimationPlaygroundGroundedMotion LaboratoryGroundedMotion { get; private set; }
        public event Action<AnimationPlaygroundGroundedMotion> LaboratoryGroundedMotionCompleted;

        // The existing normal ray supplies this plane. Reading it adds no query
        // and cannot return a support from another completed tick or owner.
        public bool TryGetLaboratorySupportPlane(out Plane plane)
        {
            plane = completedLaboratoryGroundPlane;
            return hasCompletedLaboratoryGroundPlane && LaboratoryGroundedMotion.Available
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
            if (eligible && LaboratoryGroundedMotion.Available)
            {
                completedLaboratoryGroundPlane = laboratoryGroundPlane;
                hasCompletedLaboratoryGroundPlane = true;
            }
            LaboratoryGroundedMotionCompleted?.Invoke(LaboratoryGroundedMotion);
        }
    }
}
