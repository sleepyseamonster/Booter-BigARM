using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed class AnimationPlaygroundDodgeProfile : ScriptableObject
    {
        public int schema = 1;
        public bool qualified;
        public GameObject targetModel;
        public AnimationClip roll;
        public AnimationPlaygroundFootProjection bodyProjection;
        public AnimationCurve travel = AnimationCurve.Linear(0, 0, 1, 1);
        public float distance = 2.4f, entryBlend = .08f, exitBlend = .16f, cooldown = .2f;
        public float runOutFraction = .2f, maximumSpeed = 9f;
        public float maximumGarmentProjection = .6f, garmentClearance = .01f;

        public bool Validate(GameObject model, out string reason, bool requireQualified = true)
        {
            reason = null;
            if (schema != 1 || targetModel == null || targetModel != model || roll == null
                || !roll.isHumanMotion || roll.isLooping || roll.length < .35f || roll.length > 1.5f
                || bodyProjection == null || !bodyProjection.Validate(model, out reason))
            { reason ??= "Restore the paired complete dodge roll and body projection."; return false; }
            if (!float.IsFinite(distance + entryBlend + exitBlend + cooldown + runOutFraction + maximumSpeed
                + maximumGarmentProjection + garmentClearance) || distance < .5f || distance > 3
                || entryBlend < .02f || entryBlend > .15f || exitBlend < .05f || exitBlend > .25f
                || cooldown < .1f || cooldown > 1 || runOutFraction < .1f || runOutFraction > .3f
                || maximumSpeed < 1 || maximumSpeed > 10 || maximumGarmentProjection <= 0
                || maximumGarmentProjection > 1 || garmentClearance < 0 || garmentClearance > .05f)
            { reason = "Dodge timing, distance or fitting bounds are invalid."; return false; }
            if (travel == null || travel.length < 2 || travel[0].time != 0 || travel[0].value != 0
                || travel[travel.length - 1].time != 1 || travel[travel.length - 1].value != 1)
            { reason = "Dodge travel must cover one complete normalized displacement."; return false; }
            var previous = travel.Evaluate(0);
            for (var i = 1; i <= 256; i++)
            {
                var value = travel.Evaluate(i / 256f);
                if (!float.IsFinite(value) || value < previous - .000001f || value < 0 || value > 1
                    || (value - previous) * 256 * distance / roll.length > maximumSpeed + .01f)
                { reason = "Dodge displacement must be monotone and remain inside its speed limit."; return false; }
                previous = value;
            }
            if (requireQualified && !qualified) { reason = "Dodge pose and collision qualification is pending."; return false; }
            return true;
        }

        public float Speed(float fromSeconds, float toSeconds)
        {
            if (toSeconds <= fromSeconds) return 0;
            return Mathf.Clamp((travel.Evaluate(Mathf.Clamp01(toSeconds / roll.length))
                - travel.Evaluate(Mathf.Clamp01(fromSeconds / roll.length))) * distance / (toSeconds - fromSeconds), 0, maximumSpeed);
        }
    }
}
