using System;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        public bool LaboratoryNativeGroundAdapted { get; private set; }
        public string LaboratoryNativeGroundFailure { get; private set; }
        public float LaboratoryNativeBodyLift { get; private set; }
        private AnimationPlaygroundFootProjection.Sampler nativeBodySampler;
        private TerrainCollider[] nativeGroundColliders = Array.Empty<TerrainCollider>();
        private Bounds[] nativeGroundBounds = Array.Empty<Bounds>();
        private Func<Vector3,float?> nativeGarmentHeight;
        private void ConfigureNativeCampSupport()
        {
            nativeGroundColliders = continuationReview.NativeCampGround
                ? gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TerrainCollider>(true))
                    .Where(ContinuationSurface).ToArray() : Array.Empty<TerrainCollider>();
            nativeGroundBounds = nativeGroundColliders.Select(t=>t.bounds).ToArray();
            nativeGarmentHeight = continuationReview.NativeCampGround ? NativeTerrainHeight : null;
            nativeBodySampler = null;
        }
        private float? NativeTerrainHeight(Vector3 point)
        {
            var flat=transform.TransformPoint(Vector3.up*laboratoryFeetPlane).y;
            if(point.y>flat+.75f)return null;
            var ray=new Ray(new Vector3(point.x,flat+.85f,point.z),Vector3.down);
            for(var i=0;i<nativeGroundColliders.Length;i++)
            {
                var collider=nativeGroundColliders[i];var bounds=nativeGroundBounds[i];
                if(collider==null || !collider.enabled || !collider.gameObject.activeInHierarchy
                    || point.x<bounds.min.x || point.x>bounds.max.x || point.z<bounds.min.z || point.z>bounds.max.z)continue;
                if(collider.Raycast(ray,out var hit,1.7f))return hit.point.y;
            }
            return null;
        }

        // Native terrain is not a single plane over a stride. Read the exact
        // displayed source pose, sample each foot, then solve in the same output
        // at the same timestamp. Neither evaluation advances animation time.
        private bool TryEvaluateNativeCampGround(float sourceCorrection, out float correction, bool directionalPose = false)
        {
            correction = sourceCorrection; LaboratoryNativeGroundAdapted = false; LaboratoryNativeBodyLift = 0;
            if (continuationReview == null || !continuationReview.NativeCampGround || forwardTerrainPose == null || motor == null || !motor.IsGrounded && !JumpPresentationSupportCurrent) return false;
            var standing = motor.TryGetLaboratorySupportPlane(out var plane, true);
            var support = standing ? motor.LaboratorySupportCollider : continuationActionSupport;
            if (!standing && !ContinuationActionTerrainCurrent || !ContinuationSurface(support)) return false;
            if (!standing) plane = forwardTerrainPlane;
            if (!directionalPose && support is not TerrainCollider && Vector3.Angle(plane.normal, Vector3.up) <= .25f) return false;
            forwardTerrainPose.Disable(); forwardTerrainApplied = false; playableGraph.Evaluate(0);
            if (!TrySampleNativeFoot(leftFoot, out var left) || !TrySampleNativeFoot(rightFoot, out var right))
            { LaboratoryNativeGroundFailure = "Foot support unavailable; retaining the accepted source pose."; return true; }
            var flat = JumpFeetReferenceY(transform.TransformPoint(Vector3.up * laboratoryFeetPlane).y);
            forwardTerrainPose.ConfigureFeet(plane, flat, left, right);
            if(JumpPresentationOwned)forwardTerrainPose.SetSupportBlend(jumpSupportBlend);
            playableGraph.Evaluate(0);
            var sample = forwardTerrainPose.Sample;
            if (!float.IsFinite(sample.requiredLowering) || sample.requiredLowering > laboratoryForwardProfile.maximumTerrainBodyLowering + .00001f
                || sample.incompatibleReach > .001f || correction + sample.flatLift > (AnimationPresentationOwned ? .18001f : .08001f))
            {
                LaboratoryNativeGroundFailure = $"Foot reach unavailable (lowering {sample.requiredLowering:R}, reach {sample.incompatibleReach:R}); retaining the accepted source pose.";
                forwardTerrainPose.Disable(); playableGraph.Evaluate(0); return true;
            }
            if (AnimationPresentationOwned && (motor.AnimationMotionMode == AnimationPlaygroundMotionMode.Jump || motor.AnimationMotionMode == AnimationPlaygroundMotionMode.Recovery
                || JumpPredictionCurrent && motor.AnimationMotionMode==AnimationPlaygroundMotionMode.Fall))
            {
                nativeBodySampler ??= continuationReview.nativeBodyProjection.Bind(visualInstance.transform);
                for (var iteration = 0; iteration < 3; iteration++)
                {
                    var minimum = nativeBodySampler.MinimumTerrainDistance(nativeGarmentHeight,out _);
                    if (minimum >= -.00025f) break;
                    // Whole-body projection is a sampled surface cache. Keep
                    // 5 mm clearance, then verify the final equipped mesh.
                    var lift = -minimum + .005f;
                    if (!float.IsFinite(lift) || correction + lift > .18001f)
                    {
                        LaboratoryNativeGroundFailure = "Grounded body clearance exceeded the existing jump/landing visual limit.";
                        forwardTerrainPose.Disable(); visualInstance.transform.localPosition = visualLocalPosition + Vector3.up * sourceCorrection;
                        correction = sourceCorrection; LaboratoryNativeBodyLift = 0; playableGraph.Evaluate(0); return true;
                    }
                    correction += lift; LaboratoryNativeBodyLift += lift;
                    visualInstance.transform.localPosition = visualLocalPosition + Vector3.up * correction;
                    // Raising the visual host must not turn a planted foot into
                    // a flying foot. Retain its original source-relative gap.
                    forwardTerrainPose.ConfigureFeet(plane,flat + LaboratoryNativeBodyLift,left,right);
                    if(JumpPresentationOwned)forwardTerrainPose.SetSupportBlend(jumpSupportBlend);
                    playableGraph.Evaluate(0);
                }
                sample = forwardTerrainPose.Sample;
                if (sample.incompatibleReach > .001f || sample.requiredLowering > laboratoryForwardProfile.maximumTerrainBodyLowering + .00001f
                    || nativeBodySampler.MinimumTerrainDistance(nativeGarmentHeight,out _) < -.002f)
                {
                    LaboratoryNativeGroundFailure = "Grounded body/foot reach is unavailable in this pose.";
                    forwardTerrainPose.Disable(); visualInstance.transform.localPosition = visualLocalPosition + Vector3.up * sourceCorrection;
                    correction = sourceCorrection; LaboratoryNativeBodyLift = 0; playableGraph.Evaluate(0); return true;
                }
            }
            LaboratoryNativeGroundFailure = null; LaboratoryNativeGroundAdapted = forwardTerrainApplied = true;
            forwardTerrainPlane = plane; forwardTerrainTick = motor.LaboratoryGroundedMotion.Tick; forwardTerrainEpoch = motor.LaboratoryGroundedMotion.Epoch;
            return true;
        }

        private bool TrySampleNativeFoot(Transform foot, out Plane plane)
        {
            plane = default; if (foot == null || !NativeFootHit(foot.position, out var hit)) return false;
            plane = new Plane(hit.normal, hit.point); var raise = 0f;
            // Include toe/heel width without pretending a curved terrain patch
            // is planar. The conservative envelope is local to this foot only.
            // Eight perimeter probes enclose the measured 220 mm sole radius,
            // including diagonal toe corners when the foot rolls or turns out.
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * .25f;
                var offset = (transform.forward * Mathf.Cos(angle) + transform.right * Mathf.Sin(angle)) * .24f;
                if (!NativeFootHit(foot.position + offset, out var probe)) return false;
                raise = Mathf.Max(raise, plane.GetDistanceToPoint(probe.point));
            }
            plane.distance -= raise + .001f;
            return true;
        }

        private bool NativeFootHit(Vector3 point, out RaycastHit hit)
        {
            var flat = transform.TransformPoint(Vector3.up * laboratoryFeetPlane).y;
            return Physics.Raycast(new Vector3(point.x, flat + .65f, point.z), Vector3.down, out hit, 1.3f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && ContinuationSurface(hit.collider)
                && !hit.collider.transform.IsChildOf(transform) && Mathf.Abs(hit.point.y - flat) <= .35f
                && Vector3.Angle(hit.normal, Vector3.up) <= continuationReview.maximumSlope + .001f;
        }
    }
}
