using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class AnimationPlaygroundDirectionalMath
    {
        // Cardinal weights are forward, left, right, backward. Angular interpolation
        // preserves the requested vector and never gives diagonals a speed bonus.
        public static Vector4 Weights(Vector2 direction)
        {
            if (!float.IsFinite(direction.sqrMagnitude)) throw new ArgumentException("Finite direction required.");
            if (direction.sqrMagnitude < .000001f) return new Vector4(1, 0, 0, 0);
            var angle = Mathf.Atan2(Mathf.Abs(direction.x), direction.y) * Mathf.Rad2Deg;
            var side = 1 - Mathf.Abs(angle - 90) / 90;
            return new Vector4(Mathf.Max(0, 1 - angle / 90), direction.x < 0 ? side : 0,
                direction.x >= 0 ? side : 0, Mathf.Max(0, (angle - 90) / 90));
        }

        public static Vector2 Local(Vector3 direction, Vector3 facing)
        {
            if (!float.IsFinite(direction.sqrMagnitude + facing.sqrMagnitude)) throw new ArgumentException("Finite facing and intent required.");
            var forward = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
            if (forward.sqrMagnitude < .5f) throw new ArgumentException("A standing facing is required.");
            return new Vector2(Vector3.Dot(direction, Vector3.Cross(Vector3.up, forward)), Vector3.Dot(direction, forward));
        }

        public static AnimationPlaygroundStandingTravel Classify(Vector2 direction)
        {
            if (!float.IsFinite(direction.sqrMagnitude)) return AnimationPlaygroundStandingTravel.Unsupported;
            if (direction.sqrMagnitude < .0001f) return AnimationPlaygroundStandingTravel.Idle;
            var sector = (Mathf.RoundToInt(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg / 45) + 8) % 8;
            return sector switch { 0 => AnimationPlaygroundStandingTravel.Forward, 1 => AnimationPlaygroundStandingTravel.ForwardRight,
                2 => AnimationPlaygroundStandingTravel.Right, 3 => AnimationPlaygroundStandingTravel.BackwardRight,
                4 => AnimationPlaygroundStandingTravel.Backward, 5 => AnimationPlaygroundStandingTravel.BackwardLeft,
                6 => AnimationPlaygroundStandingTravel.Left, _ => AnimationPlaygroundStandingTravel.ForwardLeft };
        }

        public static float SpeedLimit(AnimationPlaygroundStandingStrafeProfile profile, Vector2 direction)
        {
            var weights = Weights(direction);
            return weights.x * profile.maximumSpeed + (weights.y + weights.z) * profile.maximumLateralSpeed + weights.w * profile.maximumBackwardSpeed;
        }

        // Idle and five forward channels, then lateral pairs (schema 2) or
        // walk/jog/run triples (schema 3), followed by the unchanged backward pair.
        public static void FillPoseWeights(AnimationPlaygroundStandingStrafeProfile profile,
            AnimationPlaygroundForwardLocomotionProfile forward, float speed, Vector2 direction, float[] result)
        {
            if (profile == null || result == null || result.Length != profile.DirectionalChannelCount || !float.IsFinite(speed) || speed < 0)
                throw new ArgumentException("Use the complete paired directional channel array and a finite speed.");
            Array.Clear(result, 0, result.Length); var angles = Weights(direction); var f = forward.PoseWeights(speed);
            for (var i = 0; i < 6; i++) result[i] = f[i] * angles.x;
            for (var side = 0; side < 3; side++)
            {
                var first = profile.HasJog ? side < 2 ? side * 3 : 6 : side * 2;
                var angular = angles[side+1]; var walk = profile.directionalGaits[first];
                if (profile.HasJog && side < 2)
                {
                    var jog = profile.directionalGaits[first+1]; var fast = profile.directionalGaits[first+2];
                    var moving = Mathf.Clamp01(speed / walk.nominalSpeed);
                    var jogging = Mathf.InverseLerp(walk.nominalSpeed, jog.nominalSpeed, speed);
                    var runningFast = Mathf.InverseLerp(jog.nominalSpeed, fast.nominalSpeed, speed);
                    result[0] += angular * (1-moving);
                    result[6+first] = angular * moving * (1-jogging);
                    result[7+first] = angular * moving * jogging * (1-runningFast);
                    result[8+first] = angular * moving * runningFast;
                    continue;
                }
                var run = profile.directionalGaits[first+1];
                var travel = Mathf.Clamp01(speed / walk.nominalSpeed); var running = Mathf.InverseLerp(walk.nominalSpeed, run.nominalSpeed, speed);
                result[0] += angular * (1 - travel); result[6+first] = angular * travel * (1-running); result[7+first] = angular * travel * running;
            }
        }

        public static bool Validate(AnimationPlaygroundStandingStrafeProfile p, GameObject model,
            AnimationPlaygroundFootProjection projection, out string reason)
        {
            reason = null;
            if (!p.AllDirections || model == null || p.targetModel != model || projection == null || p.footProjection != projection
                || !float.IsFinite(p.maximumSpeed) || p.maximumSpeed < 6 || p.maximumSpeed > 6.15f || p.maximumLateralSpeed != 5 || p.maximumBackwardSpeed != 4
                || p.directionalGaits == null || p.directionalGaits.Length != (p.HasJog ? 8 : 6))
            { reason = "Restore the complete paired directional sources and their calibrated speed limits."; return false; }
            for (var i = 0; i < p.directionalGaits.Length; i++)
            {
                var lateral = i < (p.HasJog ? 6 : 4);
                var stage = p.HasJog && lateral ? i % 3 : i % 2;
                var nominal = lateral ? stage == 0 ? 1.4f : stage == 1 ? 3f : 5f : stage == 0 ? 1.1f : 1.8f;
                var gait = p.directionalGaits[i]; var maximum = lateral ? p.maximumLateralSpeed : p.maximumBackwardSpeed;
                if (gait?.sourceClip == null || !gait.sourceClip.isHumanMotion || !gait.sourceClip.isLooping
                    || gait.nominalSpeed != nominal || !float.IsFinite(gait.metresPerCycle) || gait.metresPerCycle <= 0
                    || gait.nativeContacts == null || gait.nativeContacts.Length != 4 || gait.phaseMap == null || gait.phaseMap.Length < 5
                    || gait.signedHeight == null || gait.signedHeight.length < 2)
                { reason = "Directional gait channels require complete humanoid loops and measured stride, phase and height data."; return false; }
                for (var k = 0; k < gait.phaseMap.Length; k++)
                {
                    var point = gait.phaseMap[k];
                    if (!float.IsFinite(point.semantic + point.native) || k > 0 && (point.semantic <= gait.phaseMap[k-1].semantic || point.native <= gait.phaseMap[k-1].native))
                    { reason = "Directional source time must be finite and strictly increasing."; return false; }
                    if (k == 0) continue;
                    var previous = gait.phaseMap[k-1];
                    var derivative = (point.native - previous.native) / (point.semantic - previous.semantic);
                    var speed = stage == 0 || p.HasJog && lateral && stage == 1 ? gait.nominalSpeed : maximum;
                    var rate = derivative * gait.sourceClip.length * speed / gait.metresPerCycle;
                    if (rate < .25f || rate > 1.65f)
                    { reason = "Directional native timing exceeds its measured playback budget."; return false; }
                }
                if (gait.phaseMap[0].semantic != 0 || gait.phaseMap[^1].semantic != 1
                    || Mathf.Abs(gait.phaseMap[^1].native - gait.phaseMap[0].native - 1) > .00001f)
                { reason = "Directional time mapping must retain the whole native take."; return false; }
                for (var k = 0; k < 4; k++)
                    if (!float.IsFinite(gait.nativeContacts[k]) || gait.nativeContacts[k] < 0 || gait.nativeContacts[k] >= 1
                        || k > 0 && gait.nativeContacts[k] <= gait.nativeContacts[k-1]
                        || Mathf.Abs(gait.NativePhase(k*.25f,out _) - gait.nativeContacts[k]) > .001f)
                    { reason = "Directional R-L-R-L contacts must agree with their source map."; return false; }
                var keys = gait.signedHeight.keys;
                if (keys[0].time != 0 || keys[^1].time != 1 || Mathf.Abs(keys[0].value - keys[^1].value) > .001f)
                { reason = "Directional height fitting must close the full loop."; return false; }
                for (var k = 0; k < keys.Length; k++)
                {
                    if (!float.IsFinite(keys[k].time + keys[k].value) || keys[k].time < 0 || keys[k].time > 1 || keys[k].value < -.03f || keys[k].value > .08f
                        || k > 0 && keys[k].time <= keys[k-1].time)
                    { reason = "Directional height fitting exceeds the existing visual bounds."; return false; }
                    if (k == 0) continue;
                    var a = keys[k-1]; var b = keys[k]; var slope = (b.value-a.value)/(b.time-a.time);
                    if (!float.IsFinite(a.outTangent + b.inTangent) || Mathf.Abs(a.outTangent-slope) > .001f || Mathf.Abs(b.inTangent-slope) > .001f
                        || a.weightedMode != WeightedMode.None || b.weightedMode != WeightedMode.None)
                    { reason = "Directional height segments must retain bounded linear interpolation."; return false; }
                }
            }
            return true;
        }
    }
}
