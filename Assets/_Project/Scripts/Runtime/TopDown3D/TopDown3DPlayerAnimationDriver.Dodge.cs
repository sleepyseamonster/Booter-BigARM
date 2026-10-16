using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private bool animationDodgeProjection, poseBlendDodgeProjection;
        private Collider dodgeGroundSupport;
        private Matrix4x4 dodgeGroundMatrix;
        private int dodgeGroundFrame = -1;
        private bool DodgeProjectionActive => animationDodgeProjection || poseBlendDodgeProjection;
        private bool DodgeGroundSupportCurrent => DodgeProjectionActive && dodgeGroundFrame == Time.frameCount
            && dodgeGroundSupport != null && dodgeGroundSupport.enabled && dodgeGroundSupport.gameObject.activeInHierarchy
            && dodgeGroundSupport.attachedRigidbody == null && dodgeGroundSupport.transform.localToWorldMatrix == dodgeGroundMatrix;

        public bool SetAnimationDodgeProjection(object owner)
        {
            if (!IsAnimationPresentationOwner(owner) || animationPreview || !animationBodyProjection) return false;
            animationDodgeProjection = true; return true;
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
            var right = transform.right * .35f; var forward = transform.forward * .65f;
            var leftY = nativeGarmentHeight(centre - right); var rightY = nativeGarmentHeight(centre + right);
            var backY = nativeGarmentHeight(centre - forward); var frontY = nativeGarmentHeight(centre + forward);
            if (leftY.HasValue && rightY.HasValue && backY.HasValue && frontY.HasValue)
            {
                var normal = (Vector3.up - transform.right * ((rightY.Value - leftY.Value) / .7f)
                    - transform.forward * ((frontY.Value - backY.Value) / 1.3f)).normalized;
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
            visualInstance.transform.localRotation = tilt * Quaternion.Euler(visualLocalEulerAngles);
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
        {
            if (!poseBlendDodgeProjection || !LaboratoryForwardReady || forwardClock == null) return false;
            var speed = Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).magnitude;
            var cycles = forwardClock.RenderCycles(1, speed, laboratoryForwardProfile);
            jumpComposingUnderlay = true;
            try
            {
                if (LaboratoryDirectionalConfigured && motor.LaboratoryStandingFacingLocked)
                    PresentDirectionalFrame(speed, cycles, Time.deltaTime);
                else PresentForwardFrame(speed, cycles, Time.deltaTime);
            }
            finally { jumpComposingUnderlay = false; }
            ApplyPoseBlend(); CurrentLocomotionState = TopDown3DLocomotionState.ConstrainedAction;
            return true;
        }
    }
}
