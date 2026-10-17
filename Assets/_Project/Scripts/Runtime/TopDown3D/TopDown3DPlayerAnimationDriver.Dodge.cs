using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private bool animationDodgeProjection, poseBlendDodgeProjection;
        private bool animationDodgeFixedHeading;
        private Quaternion animationDodgeHeading, animationDodgeEntryHeading, poseBlendDodgeHeading;
        private Collider dodgeGroundSupport;
        private Matrix4x4 dodgeGroundMatrix;
        private int dodgeGroundFrame = -1;
        private bool DodgeProjectionActive => animationDodgeProjection || poseBlendDodgeProjection;
        private bool DodgeRecoveryOwnsPresentation => poseBlendDodgeProjection && PoseBlendOwned && !AnimationPresentationOwned;
        private bool DodgeGroundSupportCurrent => DodgeProjectionActive && dodgeGroundFrame == Time.frameCount
            && dodgeGroundSupport != null && dodgeGroundSupport.enabled && dodgeGroundSupport.gameObject.activeInHierarchy
            && dodgeGroundSupport.attachedRigidbody == null && dodgeGroundSupport.transform.localToWorldMatrix == dodgeGroundMatrix;

        public bool SetAnimationDodgeProjection(object owner) => SetDodgeProjection(owner, transform.forward, false);
        public bool SetAnimationDodgeProjection(object owner, Vector3 worldHeading) => SetDodgeProjection(owner, worldHeading, true);

        private bool SetDodgeProjection(object owner, Vector3 worldHeading, bool fixedHeading)
        {
            worldHeading = Vector3.ProjectOnPlane(worldHeading, Vector3.up);
            if (!IsAnimationPresentationOwner(owner) || animationPreview || !animationBodyProjection
                || !float.IsFinite(worldHeading.sqrMagnitude) || worldHeading.sqrMagnitude < .0001f) return false;
            animationDodgeFixedHeading = fixedHeading;
            animationDodgeHeading = Quaternion.LookRotation(worldHeading.normalized, Vector3.up);
            animationDodgeEntryHeading = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
            animationDodgeProjection = true; return true;
        }

        private Quaternion CurrentDodgeWorldHeading()
        {
            var ordinary = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
            var amount = PoseBlendOwned ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((float)(poseBlendElapsed / poseBlendDuration))) : 1;
            if (animationDodgeProjection)
                return animationDodgeFixedHeading ? Quaternion.Slerp(animationDodgeEntryHeading, animationDodgeHeading,
                    poseBlendDodgeProjection ? 1 : amount) : ordinary;
            return poseBlendDodgeProjection ? Quaternion.Slerp(poseBlendDodgeHeading, ordinary, amount) : ordinary;
        }

        private void ApplyDodgeVisualHeading()
        {
            if (visualInstance != null)
                visualInstance.transform.localRotation = Quaternion.Inverse(transform.rotation) * CurrentDodgeWorldHeading()
                    * Quaternion.Euler(visualLocalEulerAngles);
        }

        // A roll bears weight through its hands/back/shoulders. Fit the complete
        // body against native terrain without the standing foot-plant job.
        private bool TryProjectDodgeBody()
        {
            if (continuationReview == null || !continuationReview.NativeCampGround || bodyProjectionSampler == null
                || motor == null || !motor.IsGrounded || visualInstance == null) return false;
            var observed = motor.TryGetLaboratorySupportPlane(out var plane, true);
            var support = observed ? motor.LaboratorySupportCollider : null;
            if (!observed && !TryContinuationActionSupport(out plane, out support) || support is not TerrainCollider) return false;
            // A folded roll spans more ground than the capsule contact. Fit its
            // visual tangent to that footprint instead of one terrain triangle.
            var centre = transform.TransformPoint(Vector3.up * laboratoryFeetPlane);
            var worldHeading = CurrentDodgeWorldHeading();
            var rightAxis = worldHeading * Vector3.right; var forwardAxis = worldHeading * Vector3.forward;
            var right = rightAxis * .35f; var forward = forwardAxis * .65f;
            var leftY = nativeGarmentHeight(centre - right); var rightY = nativeGarmentHeight(centre + right);
            var backY = nativeGarmentHeight(centre - forward); var frontY = nativeGarmentHeight(centre + forward);
            if (leftY.HasValue && rightY.HasValue && backY.HasValue && frontY.HasValue)
            {
                var normal = (Vector3.up - rightAxis * ((rightY.Value - leftY.Value) / .7f)
                    - forwardAxis * ((frontY.Value - backY.Value) / 1.3f)).normalized;
                if (Vector3.Angle(normal, Vector3.up) <= continuationReview.maximumSlope)
                    plane = new Plane(normal, centre);
            }
            forwardTerrainPose?.Disable(); forwardTerrainApplied = false;
            var fraction = PoseBlendOwned ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((float)(poseBlendElapsed / poseBlendDuration))) : 1;
            var tiltWeight = animationDodgeProjection ? poseBlendDodgeProjection ? 1 : fraction : 1 - fraction;
            var localNormal = transform.InverseTransformDirection(plane.normal).normalized;
            var tilt = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, localNormal), tiltWeight);
            var anchor = Vector3.up * laboratoryFeetPlane;
            var offset = visualInstance.transform.localPosition - visualLocalPosition;
            visualInstance.transform.localPosition = anchor + tilt * (visualLocalPosition - anchor) + offset;
            visualInstance.transform.localRotation = tilt * Quaternion.Inverse(transform.rotation) * worldHeading
                * Quaternion.Euler(visualLocalEulerAngles);
            var correction = 0f;
            for (var pass = 0; pass < 3; pass++)
            {
                playableGraph.Evaluate(0);
                var minimum = bodyProjectionSampler.MinimumTerrainDistance(nativeGarmentHeight, out _);
                if (!float.IsFinite(minimum)) return false;
                if (minimum >= .001f) break;
                var lift = -minimum + .005f;
                if (correction + lift > .18f)
                    throw new InvalidOperationException($"Dodge body clearance exceeds the qualified 180 mm action envelope: minimum {minimum:R}, prior lift {correction:R}, time {animationPresentationTime:R}, position {transform.position}, normal {plane.normal}, visual {visualInstance.transform.localPosition}.");
                correction += lift; visualInstance.transform.localPosition += Vector3.up * lift;
            }
            playableGraph.Evaluate(0);
            LaboratoryNativeGroundAdapted = forwardTerrainApplied = true; LaboratoryNativeGroundFailure = null;
            LaboratoryNativeBodyLift = LastPoseBlendGroundCorrection = correction; forwardTerrainPlane = plane;
            dodgeGroundSupport = support; dodgeGroundMatrix = support.transform.localToWorldMatrix; dodgeGroundFrame = Time.frameCount;
            return true;
        }

        private bool PresentDodgeRunOutBlend()
            => PresentDodgeRunOutBlendAt(Mathf.Clamp01((float)((Time.timeAsDouble - Time.fixedTimeAsDouble)
                / Math.Max(.0001, Time.fixedDeltaTime))), Time.deltaTime);

        private bool PresentDodgeRunOutBlendAt(float interpolation, float deltaTime)
        {
            if (!DodgeRecoveryOwnsPresentation || !LaboratoryForwardReady || forwardClock == null) return false;
            var supported = motor.TryGetLaboratorySupportPlane(out var plane, true);
            var velocity = Vector3.ProjectOnPlane(motor.Velocity, supported ? plane.normal : Vector3.up);
            var current = forwardMotion.Available && !forwardMotion.Rebased
                && forwardMotion.Tick == motor.LaboratoryGroundedMotion.Tick && forwardMotion.Epoch == motor.LaboratoryGroundedMotion.Epoch
                && Time.fixedTimeAsDouble - forwardMotion.CompletedTime <= .1;
            var speed = current ? Mathf.Lerp(forwardPreviousSpeed, forwardSpeed, interpolation) : velocity.magnitude;
            var acceptedSpeed = current ? forwardSpeed : speed;
            if (current) velocity = forwardMotion.TangentDisplacement;
            if (motor.LaboratoryStandingFacingLocked && velocity.sqrMagnitude > .00000001f)
                directionalLocal = AnimationPlaygroundDirectionalMath.Local(velocity, motor.FacingDirection).normalized;
            var directional = LaboratoryDirectionalConfigured && motor.LaboratoryStandingFacingLocked;
            var cycles = directional && AnimationPlaygroundDirectionalMath.Weights(directionalLocal).x < .999999f
                ? forwardClock.RenderCycles(interpolation) : forwardClock.RenderCycles(interpolation, acceptedSpeed, laboratoryForwardProfile);
            jumpComposingUnderlay = true;
            try
            {
                if (directional) PresentDirectionalFrame(speed, cycles, deltaTime);
                else PresentForwardFrame(speed, cycles, deltaTime);
            }
            finally { jumpComposingUnderlay = false; }
            ApplyPoseBlend(); CurrentLocomotionState = TopDown3DLocomotionState.ConstrainedAction;
            return true;
        }
    }
}
