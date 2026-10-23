using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private bool animationDodgeProjection, poseBlendDodgeProjection;
        private bool animationDodgeFixedHeading;
        private double dodgePreviousSourceTime, dodgeSourceSampleTime = double.NegativeInfinity;

        private void ObserveDodgeSourceTime()
        {
            if (animationDodgeProjection && dodgeSourceSampleTime != Time.fixedTimeAsDouble)
            {
                dodgePreviousSourceTime = animationPresentationTime;
                dodgeSourceSampleTime = Time.fixedTimeAsDouble;
            }
        }

        private double SampleDodgeSourceTime(float alpha)
            => dodgePreviousSourceTime + (animationPresentationTime - dodgePreviousSourceTime) * Mathf.Clamp01(alpha);

        // The Rigidbody is interpolated between completed physics steps. Keep
        // the roll on that same interval, as entry/recovery already are.
        private double DodgePresentationTime => animationDodgeProjection && Application.isPlaying
            ? SampleDodgeSourceTime(JumpRenderAlpha()) : AdaptedTurnPresentationTime;
        private Quaternion animationDodgeHeading, animationDodgeEntryHeading, poseBlendDodgeHeading;
        private Collider dodgeGroundSupport;
        private Matrix4x4 dodgeGroundMatrix;
        private int dodgeGroundFrame = -1;
        private sealed class DodgeRigidPart
        { public SkinnedMeshRenderer skin; public Transform bone; public Vector3[] points; }
        private List<DodgeRigidPart> dodgeRigidParts;

        // Rigid equipment is skinned at runtime. Its bind pose can differ from
        // the offline body cache's startup pose, so retain its actual bone-local
        // vertices once and query those too. No per-frame mesh baking/allocation.
        private float MinimumDodgeEquipmentDistance(bool terrain, Plane plane)
        {
            if (!cloakBound || wornCloak == null) return float.PositiveInfinity;
            if (dodgeRigidParts == null)
            {
                dodgeRigidParts = new List<DodgeRigidPart>();
                foreach (var skin in wornCloak.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (skin.name != "Rigid backpack skin" || skin.sharedMesh == null) continue;
                    var mesh = skin.sharedMesh; var vertices = mesh.vertices; var weights = mesh.boneWeights;
                    var bind = mesh.bindposes; var bones = skin.bones;
                    var groups = new Dictionary<Transform, HashSet<Vector3>>();
                    for (var i = 0; i < vertices.Length; i++)
                    {
                        var w = weights[i];
                        if (w.weight0 != 1 || w.weight1 != 0 || w.weight2 != 0 || w.weight3 != 0) continue;
                        var bone = bones[w.boneIndex0]; if (bone == null) continue;
                        if (!groups.TryGetValue(bone, out var points)) groups[bone] = points = new HashSet<Vector3>();
                        points.Add(bind[w.boneIndex0].MultiplyPoint3x4(vertices[i]));
                    }
                    foreach (var group in groups)
                    {
                        var points = new Vector3[group.Value.Count]; group.Value.CopyTo(points);
                        dodgeRigidParts.Add(new DodgeRigidPart {skin=skin,bone=group.Key,points=points});
                    }
                }
            }
            var minimum = float.PositiveInfinity;
            foreach (var part in dodgeRigidParts)
            {
                if (part.skin == null || part.bone == null || !part.skin.enabled || !part.skin.gameObject.activeInHierarchy) continue;
                var matrix = part.bone.localToWorldMatrix;
                foreach (var point in part.points)
                {
                    var world = matrix.MultiplyPoint3x4(point);
                    if (!terrain) { minimum = Mathf.Min(minimum,plane.GetDistanceToPoint(world)); continue; }
                    // NativeTerrainHeight deliberately omits vertices safely
                    // above the local contact band; that is not lost support.
                    if (world.y > transform.TransformPoint(Vector3.up * laboratoryFeetPlane).y + .75f) continue;
                    var ground = nativeGarmentHeight(world);
                    if (!ground.HasValue || !float.IsFinite(ground.Value)) return float.NaN;
                    minimum = Mathf.Min(minimum,world.y-ground.Value);
                }
            }
            return minimum;
        }
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
            if (!animationDodgeProjection)
            {
                dodgePreviousSourceTime = animationPresentationTime;
                dodgeSourceSampleTime = double.NegativeInfinity;
            }
            animationDodgeFixedHeading = fixedHeading;
            animationDodgeHeading = Quaternion.LookRotation(worldHeading.normalized, Vector3.up);
            animationDodgeEntryHeading = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
            animationDodgeProjection = true; return true;
        }

        // The outgoing roll pose, heading and terrain tilt share one render
        // clock. Keep other action families on their existing blend policy.
        private double DodgeBlendPresentationElapsed => Application.isPlaying && poseBlendOwner is AnimationPlaygroundDodgeController
            ? Math.Max(0, poseBlendPreviousElapsed + (poseBlendElapsed - poseBlendPreviousElapsed) * JumpRenderAlpha())
            : poseBlendElapsed;

        private Quaternion CurrentDodgeWorldHeading()
        {
            var ordinary = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
            var amount = PoseBlendOwned ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((float)(DodgeBlendPresentationElapsed / poseBlendDuration))) : 1;
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
            var fraction = PoseBlendOwned ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((float)(DodgeBlendPresentationElapsed / poseBlendDuration))) : 1;
            var tiltWeight = animationDodgeProjection ? poseBlendDodgeProjection ? 1 : fraction : 1 - fraction;
            var localNormal = transform.InverseTransformDirection(plane.normal).normalized;
            var tilt = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, localNormal), tiltWeight);
            var anchor = Vector3.up * laboratoryFeetPlane;
            var offset = visualInstance.transform.localPosition - visualLocalPosition;
            visualInstance.transform.localPosition = anchor + tilt * (visualLocalPosition - anchor) + offset;
            visualInstance.transform.localRotation = tilt * Quaternion.Inverse(transform.rotation) * worldHeading
                * Quaternion.Euler(visualLocalEulerAngles);
            // The outgoing standing clips do not contain their runtime foot IK.
            // Retain that support while they fade into the folded roll, rather
            // than lifting the whole unadapted stride above a curved slope.
            if(animationDodgeProjection && PoseBlendOwned && !poseBlendDodgeProjection && fraction<1
                && forwardTerrainPose!=null)
            {
                playableGraph.Evaluate(0);
                if(TrySampleNativeFoot(leftFoot,out var leftPlane) && TrySampleNativeFoot(rightFoot,out var rightPlane))
                {
                    forwardTerrainPose.ConfigureFeet(plane,centre.y,leftPlane,rightPlane);
                    forwardTerrainPose.SetSupportBlend(1-fraction);
                }
            }
            var correction = 0f;
            for (var pass = 0; pass < 6; pass++)
            {
                playableGraph.Evaluate(0);
                var minimum = Mathf.Min(bodyProjectionSampler.MinimumTerrainDistance(nativeGarmentHeight, out _),
                    MinimumDodgeEquipmentDistance(true,default));
                if (!float.IsFinite(minimum)) return false;
                // Preserve the three-pass margin behavior. Additional passes
                // are only for a residual outside the equipped-mesh tolerance.
                if (minimum >= .001f || pass >= 3 && minimum >= -.002f) break;
                var lift = Mathf.Min(-minimum + .005f, .18f - correction);
                if (lift <= 0 && minimum >= -.002f) break;
                if (lift <= 0)
                    throw new InvalidOperationException($"Dodge body clearance exceeds the qualified 180 mm action envelope: minimum {minimum:R}, prior lift {correction:R}, time {animationPresentationTime:R}, position {transform.position}, normal {plane.normal}, visual {visualInstance.transform.localPosition}.");
                correction += lift; visualInstance.transform.localPosition += Vector3.up * lift;
            }
            playableGraph.Evaluate(0);
            var finalMinimum = Mathf.Min(bodyProjectionSampler.MinimumTerrainDistance(nativeGarmentHeight, out _),
                MinimumDodgeEquipmentDistance(true,default));
            if (!float.IsFinite(finalMinimum)) return false;
            if (finalMinimum < -.002f)
                throw new InvalidOperationException($"Dodge body clearance remains outside the qualified 180 mm action envelope: minimum {finalMinimum:R}, lift {correction:R}, time {animationPresentationTime:R}.");
            LaboratoryNativeGroundAdapted = forwardTerrainApplied = true; LaboratoryNativeGroundFailure = null;
            LaboratoryNativeBodyLift = LastPoseBlendGroundCorrection = correction; forwardTerrainPlane = plane;
            dodgeGroundSupport = support; dodgeGroundMatrix = support.transform.localToWorldMatrix; dodgeGroundFrame = Time.frameCount;
            return true;
        }

        private bool PresentDodgeRunOutBlend()
            => PresentDodgeRunOutBlendAt(Mathf.Clamp01((float)((Time.timeAsDouble - Time.fixedTimeAsDouble)
                / Math.Max(.0001, Time.fixedDeltaTime))), Time.deltaTime);

        public bool PrepareGroundedEntryRunOut(float phase)
        {
            if (!LaboratoryForwardReady || forwardClock == null || !float.IsFinite(phase) || phase < 0 || phase >= 1) return false;
            forwardClock.RebasePhase(phase); forwardPlantStart = forwardPlantCount = 0;
            forwardSpeed = forwardPreviousSpeed = Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).magnitude;
            directionalInitialized = false;
            return true;
        }

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
