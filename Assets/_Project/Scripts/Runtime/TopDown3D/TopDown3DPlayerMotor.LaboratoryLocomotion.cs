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
        public AnimationPlaygroundGroundedMotion LaboratoryGroundedMotion { get; private set; }
        public event Action<AnimationPlaygroundGroundedMotion> LaboratoryGroundedMotionCompleted;

        private void ResetStandingMotionObservation()
        {
            groundedMotionTracker.Reset(); LaboratoryGroundedMotion = default;
            observedGroundSupport = null; laboratoryGroundSupport = null;
        }

        private void ObserveCompletedLaboratoryMotion()
        {
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
            LaboratoryGroundedMotion = groundedMotionTracker.Observe(Position, Time.fixedTimeAsDouble,
                eligible, support != null ? support.GetInstanceID() : 0,
                hasStableGroundNormal ? stableGroundNormal : Vector3.up);
            LaboratoryGroundedMotionCompleted?.Invoke(LaboratoryGroundedMotion);
        }
    }
}
