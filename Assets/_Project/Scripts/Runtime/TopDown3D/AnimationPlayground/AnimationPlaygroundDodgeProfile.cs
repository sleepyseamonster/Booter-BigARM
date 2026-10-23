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
        public float playbackRate = 1f;
        public float responseDuration;
        public AnimationCurve sourceProgress;
        public float maximumGarmentProjection = .6f, garmentClearance = .01f;
        public AnimationPlaygroundDodgeProfile movingRoll;
        public float movingMinimumSpeed = 2f;

        public float Duration => responseDuration > 0 ? responseDuration : roll.length / playbackRate;
        public float SourceSeconds(double seconds)
        {
            if (responseDuration > 0)
            {
                // Float fixed steps can accumulate just below the nominal end.
                // Reach the complete source and release motion on that tick.
                if (seconds >= responseDuration - .000001) return roll.length;
                return roll.length * sourceProgress.Evaluate(Mathf.Clamp01((float)seconds / responseDuration));
            }
            return Mathf.Min((float)seconds * playbackRate, roll.length);
        }
        public float CommandSpeed(double from, double to)
        {
            if(to<=from)return 0;
            var a=SourceSeconds(from)/roll.length;var b=SourceSeconds(to)/roll.length;
            return Mathf.Clamp((travel.Evaluate(b)-travel.Evaluate(a))*distance/(float)(to-from),0,maximumSpeed);
        }

        // Shorten the lead-in and stand-up independently of the actual tumble.
        // A single monotone clock drives both pose and displacement; source
        // coverage and the complete forward rotation are retained.
        public void ConfigureResponsiveTiming(bool moving)
        {
            responseDuration=moving?.6f:.5f;
            runOutFraction=.3f;
            var times=new[]{0f,moving?.18f:.10f,moving?.52f:.42f,responseDuration};
            var sources=new[]{0f,moving?.397f:.267f,moving?.927f:.667f,roll.length};
            var keys=new Keyframe[4];var slopes=new float[3];
            for(var i=0;i<3;i++)slopes[i]=(sources[i+1]-sources[i])/roll.length/((times[i+1]-times[i])/responseDuration);
            for(var i=0;i<4;i++)
            {
                var tangent=i==0?slopes[0]:i==3?slopes[2]:2*slopes[i-1]*slopes[i]/(slopes[i-1]+slopes[i]);
                keys[i]=new Keyframe(times[i]/responseDuration,sources[i]/roll.length,tangent,tangent);
            }
            sourceProgress=new AnimationCurve(keys);
        }

        public AnimationPlaygroundDodgeProfile Select(float completedSpeed)
            => movingRoll != null && float.IsFinite(completedSpeed) && completedSpeed >= movingMinimumSpeed ? movingRoll : this;

        public bool Validate(GameObject model, out string reason, bool requireQualified = true)
        {
            reason = null;
            if (movingRoll != null && (movingRoll == this || movingRoll.movingRoll != null
                || !float.IsFinite(movingMinimumSpeed) || movingMinimumSpeed < .2f || movingMinimumSpeed > 5
                || !movingRoll.Validate(model, out reason, requireQualified)))
            { reason ??= "The moving roll needs a separate complete paired calibration and a bounded entry threshold."; return false; }
            if ((schema != 1 && schema != 2) || targetModel == null || targetModel != model || roll == null
                || !roll.isHumanMotion || roll.isLooping || roll.length < .35f || roll.length > 1.5f
                || bodyProjection == null || !bodyProjection.Validate(model, out reason))
            { reason ??= "Restore the paired complete dodge roll and body projection."; return false; }
            if (!float.IsFinite(distance + entryBlend + exitBlend + cooldown + runOutFraction + maximumSpeed + playbackRate
                + maximumGarmentProjection + garmentClearance) || distance < .5f || distance > (schema == 2 ? 6 : 3)
                || entryBlend < .02f || entryBlend > .15f || exitBlend < .05f || exitBlend > .25f
                || cooldown < .1f || cooldown > 1 || runOutFraction < .1f || runOutFraction > .3f
                || maximumSpeed < 1 || maximumSpeed > 10 || playbackRate < 1 || playbackRate > 3 || maximumGarmentProjection <= 0
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
                    || (value - previous) * 256 * distance * playbackRate / roll.length > maximumSpeed + .01f)
                { reason = "Dodge displacement must be monotone and remain inside its speed limit."; return false; }
                previous = value;
            }
            if (requireQualified && !qualified) { reason = "Dodge pose and collision qualification is pending."; return false; }
            if(!float.IsFinite(responseDuration) || responseDuration<0 || responseDuration>0
                && (responseDuration<.3f || responseDuration>.8f || playbackRate!=1 || sourceProgress==null || sourceProgress.length<2
                    || sourceProgress[0].time!=0 || sourceProgress[0].value!=0
                    || sourceProgress[sourceProgress.length-1].time!=1 || sourceProgress[sourceProgress.length-1].value!=1))
            {reason="Responsive dodge timing needs a bounded complete monotone source clock.";return false;}
            if(responseDuration>0)
            {
                var source=0f;var displacement=0f;
                for(var i=1;i<=512;i++)
                {
                    var next=sourceProgress.Evaluate(i/512f);var travelled=travel.Evaluate(next);
                    if(!float.IsFinite(next+travelled) || next<source-.000001f || next<0 || next>1
                        || (travelled-displacement)*512*distance/responseDuration>maximumSpeed+.01f)
                    {reason="Responsive dodge source/travel timing exceeds its monotone speed envelope.";return false;}
                    source=next;displacement=travelled;
                }
            }
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
