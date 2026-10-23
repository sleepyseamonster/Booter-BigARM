using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class AnimationPlaygroundSprintEntry
    {
        public AnimationClip clip;
        public AnimationCurve travel;
        public float distance, exitPhase;
        public float responseDuration;
        public float Duration => responseDuration > 0 ? responseDuration : clip != null ? clip.length : 0;
        public float SourceSeconds(double seconds)
        {
            if(!double.IsFinite(seconds) || seconds<0 || Duration<=0)throw new ArgumentOutOfRangeException(nameof(seconds));
            return seconds>=Duration-.000001 ? clip.length : Mathf.Clamp((float)(seconds/Duration)*clip.length,0,clip.length);
        }
        public float ResponseSpeed(double from,double to) => to<=from ? 0 : Mathf.Max(0,
            (travel.Evaluate(SourceSeconds(to)/clip.length)-travel.Evaluate(SourceSeconds(from)/clip.length))*distance/(float)(to-from));
        public float Speed(float from, float to) => to <= from ? 0 : Mathf.Max(0,
            (travel.Evaluate(Mathf.Clamp01(to / clip.length)) - travel.Evaluate(Mathf.Clamp01(from / clip.length))) * distance / (to-from));
    }

    public sealed class AnimationPlaygroundSprintEntryProfile : ScriptableObject
    {
        public int schema = 1;
        public bool qualified;
        public GameObject targetModel;
        public AnimationPlaygroundFootProjection bodyProjection;
        public AnimationPlaygroundSprintEntry idle, crouched;
        public float entryBlend = .08f, exitBlend = .16f, runOutFraction = .2f, maximumSpeed = 6.15f;
        public bool Validate(GameObject model, out string reason, bool requireQualified = true)
        {
            reason = null;
            if (schema != 1 || model == null || targetModel != model || bodyProjection == null || !bodyProjection.Validate(model, out reason))
            { reason ??= "Restore the paired sprint entry sources and body projection."; return false; }
            if (!float.IsFinite(entryBlend + exitBlend + runOutFraction + maximumSpeed)
                || entryBlend < .02f || entryBlend > .15f || exitBlend < .05f || exitBlend > .25f
                || runOutFraction < .1f || runOutFraction > .3f || maximumSpeed <= 0 || maximumSpeed > 6.15f)
            { reason = "Sprint entry blending or speed bounds are invalid."; return false; }
            foreach (var entry in new[] { idle, crouched })
            {
                if (entry?.clip == null || !entry.clip.isHumanMotion || entry.clip.isLooping || entry.clip.length < .2f || entry.clip.length > 1.5f
                    || !float.IsFinite(entry.distance + entry.exitPhase + entry.responseDuration) || entry.responseDuration<0
                    || entry.Duration<.2f || entry.Duration>1.5f || entry.distance <= 0 || entry.distance > 4 || entry.exitPhase < 0 || entry.exitPhase >= 1
                    || entry.travel == null || entry.travel.length < 2 || entry.travel[0].time != 0 || entry.travel[0].value != 0
                    || entry.travel[entry.travel.length-1].time != 1 || entry.travel[entry.travel.length-1].value != 1)
                { reason = "Sprint entries require complete takes, measured travel and an outgoing gait phase."; return false; }
                for (var i = 1; i < entry.travel.length; i++)
                {
                    var a = entry.travel[i-1]; var b = entry.travel[i];
                    var slope = (b.value-a.value)/(b.time-a.time);
                    if (!float.IsFinite(a.time + a.value + b.time + b.value + slope) || a.time < 0 || b.time > 1
                        || b.time <= a.time || slope < 0 || slope * entry.distance / entry.Duration > maximumSpeed + .001f
                        || !float.IsFinite(a.outTangent + b.inTangent) || Mathf.Abs(a.outTangent-slope) > .001f || Mathf.Abs(b.inTangent-slope) > .001f
                        || a.weightedMode != WeightedMode.None || b.weightedMode != WeightedMode.None)
                    { reason = "Sprint entry travel must be monotone, linear and inside the existing sprint speed cap."; return false; }
                }
            }
            if (requireQualified && !qualified) { reason = "Sprint entry pose qualification is pending."; return false; }
            return true;
        }
    }
}
