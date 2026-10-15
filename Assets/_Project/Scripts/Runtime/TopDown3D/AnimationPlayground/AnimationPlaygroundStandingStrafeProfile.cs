using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Private, unqualified AP02 review. Native unequal contact intervals are
    // retained; the forward family's equal-quarter phase map is not reused.
    public sealed class AnimationPlaygroundStandingStrafeProfile : ScriptableObject
    {
        public int schema = 1;
        public GameObject targetModel;
        public AnimationPlaygroundFootProjection footProjection;
        public AnimationPlaygroundForwardGait left, right;
        public float maximumSpeed = .5f;
        public bool Validate(GameObject model, AnimationPlaygroundFootProjection projection, out string reason)
        {
            reason = null;
            if (schema != 1 || targetModel == null || targetModel != model || footProjection == null || footProjection != projection
                || !float.IsFinite(maximumSpeed) || maximumSpeed <= 0 || maximumSpeed > .6f)
                reason = "Standing strafe requires its paired model, projection and bounded slow-review speed.";
            else foreach (var gait in new[] { left, right })
            {
                if (gait == null || gait.sourceClip == null || !gait.sourceClip.isHumanMotion || !gait.sourceClip.isLooping
                    || !float.IsFinite(gait.metresPerCycle) || gait.metresPerCycle <= 0
                    || maximumSpeed * gait.sourceClip.length / gait.metresPerCycle > 1.35f
                    || gait.nativeContacts == null || gait.nativeContacts.Length != 4
                    || gait.signedHeight == null || gait.signedHeight.length < 2)
                { reason = "Standing strafe requires complete looping sources and measured lateral strides/contacts."; break; }
                for (var i = 0; i < 4; i++)
                    if (!float.IsFinite(gait.nativeContacts[i]) || gait.nativeContacts[i] < 0 || gait.nativeContacts[i] >= 1
                        || i > 0 && gait.nativeContacts[i] <= gait.nativeContacts[i - 1])
                        reason = "Native strafe contacts must retain their ordered R-L-R-L source timing.";
                var keys = gait.signedHeight.keys;
                if (keys[0].time != 0 || keys[^1].time != 1 || Mathf.Abs(keys[0].value - keys[^1].value) > .001f)
                    reason = "Standing strafe height fitting must close the complete loop.";
                for (var i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    if (!float.IsFinite(key.time) || !float.IsFinite(key.value) || key.time < 0 || key.time > 1
                        || key.value < -.03f || key.value > .08f || i > 0 && key.time <= keys[i - 1].time)
                        reason = "Standing strafe height fitting exceeds the existing visual bounds.";
                    if (i == 0) continue;
                    var previous = keys[i - 1]; var slope = (key.value - previous.value) / (key.time - previous.time);
                    if (!float.IsFinite(previous.outTangent) || !float.IsFinite(key.inTangent)
                        || Mathf.Abs(previous.outTangent - slope) > .001f || Mathf.Abs(key.inTangent - slope) > .001f
                        || previous.weightedMode != WeightedMode.None || key.weightedMode != WeightedMode.None)
                        reason = "Standing strafe height segments must retain bounded linear interpolation.";
                }
            }
            return reason == null;
        }
    }

    public enum AnimationPlaygroundStandingTravel { Idle, Forward, Left, Right, Unsupported }

    public static class AnimationPlaygroundStandingStrafeMath
    {
        public const float AxisConeDegrees = 20, StoppedSpeed = .1f;
        public static AnimationPlaygroundStandingTravel Classify(Vector3 direction, Vector3 facing, out Vector3 projected)
        {
            projected = Vector3.zero;
            if (!Finite(direction) || !Finite(facing) || facing.sqrMagnitude < .0001f)
                return AnimationPlaygroundStandingTravel.Unsupported;
            var forward = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
            if (forward.sqrMagnitude < .5f) return AnimationPlaygroundStandingTravel.Unsupported;
            var right = Vector3.Cross(Vector3.up, forward);
            var x = Vector3.Dot(direction, right); var z = Vector3.Dot(direction, forward);
            if (x * x + z * z < .0001f) return AnimationPlaygroundStandingTravel.Idle;
            var cone = Mathf.Tan(AxisConeDegrees * Mathf.Deg2Rad);
            if (z > 0 && Mathf.Abs(x) <= z * cone)
            { projected = forward * z; return AnimationPlaygroundStandingTravel.Forward; }
            if (Mathf.Abs(z) <= Mathf.Abs(x) * cone)
            { projected = right * x; return x < 0 ? AnimationPlaygroundStandingTravel.Left : AnimationPlaygroundStandingTravel.Right; }
            return AnimationPlaygroundStandingTravel.Unsupported;
        }
        private static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
    }
}
