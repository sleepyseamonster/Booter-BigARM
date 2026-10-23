using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundLocomotionTransition { None, RunStop, SprintStop, PivotLeft, PivotRight }
    [Serializable]
    public sealed class AnimationPlaygroundLocomotionTake
    {
        public AnimationClip clip;
        public AnimationCurve x, z, yaw;
        public float entrySpeed, exitPhase, fastRunExitPhase, maximumSourceSpeed;
        // x = requested metres/second, y = measured semantic gait phase.
        // Select a measured pose directly: phase interpolation across a wrap or
        // between opposite-foot minima can create an unrelated airborne pose.
        public Vector2[] runOutPhases;
        public float RunOutPhase(float speed,float sprintThreshold)
        {
            if(runOutPhases==null || runOutPhases.Length==0)return speed>=sprintThreshold ? exitPhase : fastRunExitPhase;
            var nearest=0;
            for(var i=1;i<runOutPhases.Length;i++)
                if(Mathf.Abs(runOutPhases[i].x-speed)<Mathf.Abs(runOutPhases[nearest].x-speed))nearest=i;
            return runOutPhases[nearest].y;
        }
        public Vector3 Position(float seconds) => new Vector3(x.Evaluate(seconds), 0, z.Evaluate(seconds));
        public Vector3 Velocity(float from, float to) => to <= from ? Vector3.zero : (Position(to)-Position(from))/(to-from);
    }
    public sealed class AnimationPlaygroundLocomotionTransitionProfile : ScriptableObject
    {
        public int schema = 1;
        public bool qualified;
        public GameObject targetModel;
        public AnimationPlaygroundFootProjection bodyProjection;
        public AnimationPlaygroundLocomotionTake runStop, sprintStop, pivotLeft, pivotRight;
        public float minimumSpeed = 2.2f, sprintThreshold = 5.2f, reversalAngle = 150;
        public float entryBlend = .08f, exitBlend = .14f, maximumSpeed = 6.15f;
        public AnimationPlaygroundLocomotionTake Take(AnimationPlaygroundLocomotionTransition kind) => kind switch
        {
            AnimationPlaygroundLocomotionTransition.RunStop => runStop,
            AnimationPlaygroundLocomotionTransition.SprintStop => sprintStop,
            AnimationPlaygroundLocomotionTransition.PivotLeft => pivotLeft,
            AnimationPlaygroundLocomotionTransition.PivotRight => pivotRight,
            _ => null
        };
        public AnimationPlaygroundLocomotionTransition Select(Vector3 velocity, Vector3 facing, Vector3 request, bool facingLocked, bool wasSprinting = false)
        {
            var speed = velocity.magnitude;
            if (facingLocked || !float.IsFinite(speed) || speed < minimumSpeed || facing.sqrMagnitude < .5f
                || Vector3.Dot(velocity.normalized, facing.normalized) < .9f) return AnimationPlaygroundLocomotionTransition.None;
            if (request.sqrMagnitude < .01f) return speed >= sprintThreshold || wasSprinting && speed >= sprintThreshold*.8f
                ? AnimationPlaygroundLocomotionTransition.SprintStop : AnimationPlaygroundLocomotionTransition.RunStop;
            if (request.sqrMagnitude < .64f || Vector3.Angle(velocity, request) < reversalAngle) return AnimationPlaygroundLocomotionTransition.None;
            return Vector3.SignedAngle(velocity, request, Vector3.up) < 0
                ? AnimationPlaygroundLocomotionTransition.PivotLeft : AnimationPlaygroundLocomotionTransition.PivotRight;
        }
        public bool Validate(GameObject model, out string reason, bool requireQualified = true)
        {
            reason = null;
            if ((schema != 1 && schema != 2) || model == null || targetModel != model || bodyProjection == null || !bodyProjection.Validate(model,out reason))
            { reason ??= "Restore the paired locomotion transition model and projection."; return false; }
            if (!float.IsFinite(minimumSpeed+sprintThreshold+reversalAngle+entryBlend+exitBlend+maximumSpeed)
                || minimumSpeed < 2 || sprintThreshold <= minimumSpeed || sprintThreshold > maximumSpeed
                || reversalAngle < 150 || reversalAngle > 175 || entryBlend < .04f || entryBlend > .15f
                || exitBlend < .08f || exitBlend > .25f || maximumSpeed <= 0 || maximumSpeed > 6.15f)
            { reason = "Locomotion transition thresholds or envelopes are invalid."; return false; }
            foreach (var kind in new[]{AnimationPlaygroundLocomotionTransition.RunStop, AnimationPlaygroundLocomotionTransition.SprintStop,
                AnimationPlaygroundLocomotionTransition.PivotLeft, AnimationPlaygroundLocomotionTransition.PivotRight})
            {
                var take = Take(kind); var pivot = kind >= AnimationPlaygroundLocomotionTransition.PivotLeft;
                if (take?.clip == null || !take.clip.isHumanMotion || take.clip.isLooping || take.clip.length < .3f || take.clip.length > 2
                    || !float.IsFinite(take.entrySpeed+take.exitPhase+take.fastRunExitPhase+take.maximumSourceSpeed) || take.entrySpeed < .5f || take.entrySpeed > maximumSpeed || take.maximumSourceSpeed < take.entrySpeed || take.maximumSourceSpeed > maximumSpeed
                    || take.exitPhase < 0 || take.exitPhase >= 1 || take.fastRunExitPhase < 0 || take.fastRunExitPhase >= 1)
                { reason = "Transitions need complete Humanoid takes and measured entry speed/exit phase."; return false; }
                if(pivot && (schema>=2 || take.runOutPhases!=null && take.runOutPhases.Length>0))
                {
                    var phases=take.runOutPhases;
                    if(phases==null || phases.Length<2 || phases.Length>160
                        || Mathf.Abs(phases[0].x-minimumSpeed)>.001f || Mathf.Abs(phases[phases.Length-1].x-maximumSpeed)>.001f)
                    {reason="Moving pivots need measured exit phases across the admitted speed range.";return false;}
                    for(var i=0;i<phases.Length;i++)
                        if(!float.IsFinite(phases[i].x) || !float.IsFinite(phases[i].y) || phases[i].y<0 || phases[i].y>=1
                            || i>0 && (phases[i].x<=phases[i-1].x || phases[i].x-phases[i-1].x>.101f))
                        {reason="Moving pivot exit phases must be finite, ordered, and sampled at most 0.1 m/s apart.";return false;}
                }
                foreach (var curve in new[]{take.x,take.z,take.yaw})
                {
                    if (curve == null || curve.length < 2 || curve[0].time != 0 || Mathf.Abs(curve[0].value) > .0001f
                        || Mathf.Abs(curve[curve.length-1].time-take.clip.length) > .00001f)
                    { reason = "Transition curves must cover the complete take from a zero origin."; return false; }
                    for (var i=0;i<curve.length;i++)
                    {
                        var k=curve[i];
                        if (!float.IsFinite(k.time+k.value+k.inTangent+k.outTangent) || k.weightedMode != WeightedMode.None
                            || i>0 && k.time<=curve[i-1].time || Mathf.Abs(k.value) > (curve==take.yaw ? 185 : 4))
                        { reason = "Transition curves are nonfinite, unordered or out of bounds."; return false; }
                        if (i>0)
                        {
                            var a=curve[i-1]; var slope=(k.value-a.value)/(k.time-a.time);
                            if (Mathf.Abs(a.outTangent-slope)>.002f || Mathf.Abs(k.inTangent-slope)>.002f)
                            { reason="Transition curves must use bounded linear interpolation."; return false; }
                        }
                    }
                }
                var endYaw=take.yaw.Evaluate(take.clip.length);
                if (pivot ? Mathf.Abs(endYaw-(kind==AnimationPlaygroundLocomotionTransition.PivotLeft ? -180 : 180)) > .1f : Mathf.Abs(endYaw) > .001f)
                { reason="Moving pivots must complete their signed 180-degree turn."; return false; }
                for (var i=1;i<=240;i++)
                    if (take.Velocity(take.clip.length*(i-1)/240f,take.clip.length*i/240f).magnitude > take.maximumSourceSpeed+.01f)
                    { reason="Source travel exceeds the existing sprint limit."; return false; }
            }
            if (requireQualified && !qualified) { reason="Locomotion transition qualification is pending."; return false; }
            return true;
        }
    }
}
