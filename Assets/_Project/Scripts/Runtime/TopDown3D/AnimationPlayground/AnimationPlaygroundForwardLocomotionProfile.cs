using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable] public struct AnimationPlaygroundNativePhase
    {
        public float semantic, native;
        public AnimationPlaygroundNativePhase(float semantic, float native)
        { this.semantic = semantic; this.native = native; }
    }

    [Serializable] public sealed class AnimationPlaygroundForwardGait
    {
        public TopDown3DLocomotionClipRole role;
        public AnimationClip sourceClip;
        public float nominalSpeed, metresPerCycle;
        public float pureMinimumRate, pureMaximumRate, blendMinimumRate, blendMaximumRate;
        public float[] nativeContacts = Array.Empty<float>();
        public AnimationPlaygroundNativePhase[] phaseMap = Array.Empty<AnimationPlaygroundNativePhase>();
        public AnimationCurve signedHeight;

        public float NativePhase(float semantic, out float derivative)
        {
            semantic = Mathf.Clamp01(semantic);
            var low = 0; var high = phaseMap.Length - 1;
            while (high - low > 1)
            {
                var middle = (low + high) / 2;
                if (phaseMap[middle].semantic <= semantic) low = middle; else high = middle;
            }
            var a = phaseMap[low]; var b = phaseMap[high];
            derivative = (b.native - a.native) / (b.semantic - a.semantic);
            return a.native + (semantic - a.semantic) * derivative;
        }

        public float SourceSeconds(double cycles)
        {
            var phase = (float)(cycles - Math.Floor(cycles));
            return Mathf.Repeat(NativePhase(phase, out _), 1) * sourceClip.length;
        }

        public float Height(double cycles)
        {
            var phase = (float)(cycles - Math.Floor(cycles));
            return signedHeight.Evaluate(Mathf.Repeat(NativePhase(phase, out _), 1));
        }
    }

    // Separate lab extension. The twelve-role production profile and its 0.9-1.1
    // validation contract are unchanged; original imported clips remain the source.
    public sealed class AnimationPlaygroundForwardLocomotionProfile : ScriptableObject
    {
        public int schema = 1;
        public bool qualified;
        // Independently sealed optional terrain extension. Existing assets and
        // production/null profiles retain their original presentation path.
        public int terrainSchema = 1;
        public bool terrainQualified;
        public float maximumTerrainBodyLowering = .45f;
        public float maximumTerrainGarmentDisplacement = .35f;
        public GameObject targetModel;
        public TopDown3DLocomotionClipProfile locomotion;
        public AnimationPlaygroundFootProjection footProjection;
        public AnimationPlaygroundForwardGait[] gaits = Array.Empty<AnimationPlaygroundForwardGait>();
        public float significantWeight = .1f;
        public float maximumLift = .08f, maximumLowering = .03f;
        public float enterSpeed = .18f, exitSpeed = .1f;
        // Optional full-jog plateau: finish blending out walking before reaching
        // the requested jogging travel speed. Zero preserves existing profiles.
        public float walkToJogBlendEndSpeed;
        // Measured mixed-pose distance derivative around the second right stance.
        // x = actual speed, y = adjustment in metres per semantic cycle.
        public Vector2[] secondRightDistanceAdjustment = Array.Empty<Vector2>();

        public bool Validate(GameObject expectedModel, TopDown3DLocomotionClipProfile expectedProfile,
            out string reason, bool requireQualified = true)
        {
            reason = null;
            if (schema != 1 || targetModel == null || targetModel != expectedModel || locomotion == null || locomotion != expectedProfile)
                reason = "The forward gait policy must pair with the selected model and twelve-role profile.";
            else if (requireQualified && !qualified) reason = "Forward locomotion is awaiting pose/contact qualification.";
            else if (!locomotion.TryValidate(out reason)) return false;
            else if (footProjection == null || !footProjection.Validate(targetModel, out reason))
            { reason = reason ?? "Forward locomotion requires its paired foot projection."; return false; }
            else if (gaits == null || gaits.Length != 3 || significantWeight != .1f
                || !float.IsFinite(maximumLift) || maximumLift <= 0 || maximumLift > .08f
                || !float.IsFinite(maximumLowering) || maximumLowering < 0 || maximumLowering > .03f
                || !float.IsFinite(enterSpeed) || !float.IsFinite(exitSpeed) || exitSpeed < 0 || enterSpeed <= exitSpeed)
                reason = "The forward gait policy has invalid reviewed bounds.";
            if (reason != null) return false;
            if (terrainQualified && (terrainSchema != 1 || !float.IsFinite(maximumTerrainBodyLowering)
                || maximumTerrainBodyLowering <= 0 || maximumTerrainBodyLowering > .45f
                || !float.IsFinite(maximumTerrainGarmentDisplacement) || maximumTerrainGarmentDisplacement <= 0
                || maximumTerrainGarmentDisplacement > .35f))
            { reason = "The terrain extension requires its reviewed version and bounded body/garment policy."; return false; }
            if (secondRightDistanceAdjustment == null || secondRightDistanceAdjustment.Length > 32)
            { reason = "Mixed stance distance calibration is incomplete."; return false; }
            for (var k = 0; k < secondRightDistanceAdjustment.Length; k++)
            {
                var point = secondRightDistanceAdjustment[k];
                if (!float.IsFinite(point.x) || !float.IsFinite(point.y) || point.x < 0 || Math.Abs(point.y) > .8
                    || k > 0 && point.x <= secondRightDistanceAdjustment[k - 1].x)
                { reason = "Mixed stance distance calibration is invalid."; return false; }
            }
            var previousSpeed = 0f;
            for (var i = 0; i < 3; i++)
            {
                var gait = gaits[i];
                if (gait == null || gait.role != (TopDown3DLocomotionClipRole)(i + 1)
                    || gait.sourceClip == null || gait.sourceClip != locomotion.GetRequiredEntry(gait.role).Clip
                    || !gait.sourceClip.isHumanMotion || !gait.sourceClip.isLooping
                    || !float.IsFinite(gait.nominalSpeed) || gait.nominalSpeed <= previousSpeed
                    || !float.IsFinite(gait.metresPerCycle) || gait.metresPerCycle <= 0
                    || !ValidRates(gait.pureMinimumRate, gait.pureMaximumRate, .65f, 1.35f)
                    || !ValidRates(gait.blendMinimumRate, gait.blendMaximumRate, .45f, 1.65f))
                { reason = "Forward roles require their original looping sources, measured strides and reviewed rates."; return false; }
                previousSpeed = gait.nominalSpeed;
                if (gait.nativeContacts == null || gait.nativeContacts.Length != 4 || gait.phaseMap == null
                    || gait.phaseMap.Length < 2 || gait.phaseMap.Length > 256 || gait.signedHeight == null || gait.signedHeight.length < 2)
                { reason = "Forward gait contact/time/height calibration is incomplete."; return false; }
                for (var k = 0; k < 4; k++)
                    if (!float.IsFinite(gait.nativeContacts[k]) || gait.nativeContacts[k] < 0 || gait.nativeContacts[k] >= 1
                        || k > 0 && gait.nativeContacts[k] <= gait.nativeContacts[k - 1])
                    { reason = "Native R0-L0-R1-L1 contacts must be ordered."; return false; }
                for (var k = 0; k < gait.phaseMap.Length; k++)
                {
                    var point = gait.phaseMap[k];
                    if (!float.IsFinite(point.semantic) || !float.IsFinite(point.native)
                        || k > 0 && (point.semantic <= gait.phaseMap[k - 1].semantic || point.native <= gait.phaseMap[k - 1].native))
                    { reason = "Forward source time must remain strictly increasing."; return false; }
                    if (k > 0)
                    {
                        var previous = gait.phaseMap[k - 1];
                        var rate = (point.native - previous.native) / (point.semantic - previous.semantic)
                            * gait.sourceClip.length * gait.nominalSpeed / gait.metresPerCycle;
                        if (rate < gait.pureMinimumRate - .001f || rate > gait.pureMaximumRate + .001f)
                        { reason = "A native source segment exceeds its pure-gait retiming budget."; return false; }
                    }
                }
                if (gait.phaseMap[0].semantic != 0 || gait.phaseMap[^1].semantic != 1
                    || Mathf.Abs(gait.phaseMap[0].native - gait.nativeContacts[0]) > .0001f
                    || Mathf.Abs(gait.phaseMap[^1].native - gait.nativeContacts[0] - 1) > .0001f)
                { reason = "Forward source maps must retain one complete native loop."; return false; }
                for (var k = 0; k < 4; k++)
                    if (Mathf.Abs(gait.NativePhase(k * .25f, out _) - gait.nativeContacts[k]) > .001f)
                    { reason = "Semantic plants disagree with measured native contacts."; return false; }
                foreach (var key in gait.signedHeight.keys)
                    if (!float.IsFinite(key.time) || !float.IsFinite(key.value) || key.time < 0 || key.time > 1
                        || key.value < -maximumLowering || key.value > maximumLift)
                    { reason = "Signed stance fitting exceeds its reviewed height envelope."; return false; }
                var keys = gait.signedHeight.keys;
                if (keys[0].time != 0 || keys[^1].time != 1 || Mathf.Abs(keys[0].value - keys[^1].value) > .001f)
                { reason = "Forward stance fitting must close its loop."; return false; }
                for (var k = 1; k < keys.Length; k++)
                {
                    var a = keys[k - 1]; var b = keys[k];
                    var slope = (b.value - a.value) / (b.time - a.time);
                    if (b.time <= a.time || !float.IsFinite(a.outTangent) || !float.IsFinite(b.inTangent)
                        || Math.Abs(a.outTangent - slope) > .001 || Math.Abs(b.inTangent - slope) > .001
                        || a.weightedMode != WeightedMode.None || b.weightedMode != WeightedMode.None)
                    { reason = "Signed height segments must retain bounded linear interpolation."; return false; }
                }
            }
            var walk = gaits[0].nominalSpeed; var run = gaits[1].nominalSpeed; var sprint = gaits[2].nominalSpeed;
            var blendEnd = walkToJogBlendEndSpeed > 0 ? walkToJogBlendEndSpeed : run;
            if (!float.IsFinite(walkToJogBlendEndSpeed) || walkToJogBlendEndSpeed < 0 || blendEnd <= walk || blendEnd > run)
            { reason = "The walk/jog blend must end between walking and nominal jogging speed."; return false; }
            foreach (var point in secondRightDistanceAdjustment)
                if (point.y != 0 && (point.x <= walk || point.x >= blendEnd))
                { reason = "Mixed stance distance calibration is limited to the measured walk/run interval."; return false; }
            if (DistanceAdjustment(0) != 0 || DistanceAdjustment(walk) != 0 || DistanceAdjustment(blendEnd) != 0 || DistanceAdjustment(run) != 0 || DistanceAdjustment(sprint) != 0)
            { reason = "Mixed stance calibration must preserve the measured pure gaits."; return false; }
            var rateSpeeds = new System.Collections.Generic.List<float> { .18f, walk, run, sprint,
                Mathf.Lerp(walk, run, .1f), Mathf.Lerp(walk, run, .9f), Mathf.Lerp(run, sprint, .1f), Mathf.Lerp(run, sprint, .9f) };
            rateSpeeds.Add(blendEnd); rateSpeeds.Add(Mathf.Lerp(walk, blendEnd, .1f)); rateSpeeds.Add(Mathf.Lerp(walk, blendEnd, .9f));
            foreach (var point in secondRightDistanceAdjustment) rateSpeeds.Add(point.x);
            foreach (var speed in rateSpeeds)
            {
                if (speed <= 0) continue;
                var weights = Weights(speed);
                for (var i = 0; i < 3; i++)
                {
                    if (weights[i + 1] < significantWeight - .00001f) continue;
                    var gait = gaits[i];
                    for (var k = 1; k < gait.phaseMap.Length; k++)
                    {
                        var a = gait.phaseMap[k - 1]; var b = gait.phaseMap[k];
                        foreach (var phase in new[] { (double)a.semantic, Math.Max(a.semantic, b.semantic - .00000001),
                            Math.Clamp(.4, a.semantic, b.semantic), Math.Clamp(.45, a.semantic, b.semantic),
                            Math.Clamp(.55, a.semantic, b.semantic), Math.Clamp(.6, a.semantic, b.semantic) })
                        {
                            var derivative = DistanceDerivative(speed, phase);
                            var rate = (b.native - a.native) / (b.semantic - a.semantic) * gait.sourceClip.length * speed / derivative;
                            if (derivative <= 0 || !double.IsFinite(rate) || rate < gait.blendMinimumRate - .001 || rate > gait.blendMaximumRate + .001)
                            { reason = "A significant blend contributor exceeds its measured native-source rate budget."; return false; }
                        }
                    }
                }
            }
            return true;
        }

        private double DistanceAdjustment(float speed)
        {
            var points = secondRightDistanceAdjustment;
            if (points.Length == 0 || speed <= points[0].x) return points.Length == 0 ? 0 : points[0].y;
            for (var i = 1; i < points.Length; i++)
                if (speed <= points[i].x)
                    return Mathf.Lerp(points[i - 1].y, points[i].y, Mathf.InverseLerp(points[i - 1].x, points[i].x, speed));
            return points[^1].y;
        }

        // Trapezoid: blend in before the measured plant, constant over its full
        // window, then blend out. Its integral makes inversion exact and monotone.
        private static double StanceIntegral(double phase)
        {
            if (phase <= .4) return 0;
            if (phase < .45) return (phase - .4) * (phase - .4) / .1;
            if (phase <= .55) return .025 + phase - .45;
            if (phase < .6) return .15 - (.6 - phase) * (.6 - phase) / .1;
            return .15;
        }

        public double DistanceDerivative(float speed, double phase)
        {
            phase -= Math.Floor(phase);
            var influence = phase < .4 || phase > .6 ? 0 : phase < .45 ? (phase - .4) / .05
                : phase <= .55 ? 1 : (.6 - phase) / .05;
            return CycleDistance(speed) + DistanceAdjustment(speed) * influence;
        }

        public double DistanceAtCycles(float speed, double cycles)
        {
            var whole = Math.Floor(cycles); var phase = cycles - whole;
            return CycleDistance(speed) * cycles + DistanceAdjustment(speed) * (whole * .15 + StanceIntegral(phase));
        }

        public double AdvanceCycles(float speed, double cycles, double distance)
        {
            if (distance == 0) return cycles;
            var target = DistanceAtCycles(speed, cycles) + distance;
            var cycleDistance = DistanceAtCycles(speed, 1);
            var whole = Math.Floor(target / cycleDistance);
            var remainder = target - whole * cycleDistance;
            var low = 0d; var high = 1d;
            for (var i = 0; i < 30; i++)
            {
                var middle = (low + high) * .5;
                if (DistanceAtCycles(speed, middle) < remainder) low = middle; else high = middle;
            }
            return whole + (low + high) * .5;
        }

        public Vector4 Weights(float speed)
        {
            var blendEnd = walkToJogBlendEndSpeed > 0 ? walkToJogBlendEndSpeed : gaits[1].nominalSpeed;
            var lowerBand = speed <= gaits[1].nominalSpeed;
            TopDown3DAnimationMath.CalculateGaitWeights(lowerBand ? Mathf.Min(speed, blendEnd) : speed,
                gaits[0].nominalSpeed, lowerBand ? blendEnd : gaits[1].nominalSpeed, gaits[2].nominalSpeed,
                out var walk, out var run, out var sprint);
            var travel = Mathf.Clamp01(speed / gaits[0].nominalSpeed);
            return new Vector4(1 - travel, walk * travel, run * travel, sprint * travel);
        }

        public double CycleDistance(float speed)
        {
            var w = Weights(speed);
            // Small analog movement shortens the stride by blending idle, retaining
            // native cadence rather than holding a raised foot for several seconds.
            return Math.Max(.01, w.y * gaits[0].metresPerCycle + w.z * gaits[1].metresPerCycle + w.w * gaits[2].metresPerCycle);
        }

        private static bool ValidRates(float minimum, float maximum, float low, float high) =>
            float.IsFinite(minimum) && float.IsFinite(maximum) && minimum >= low && maximum <= high && maximum >= minimum;
    }
}
