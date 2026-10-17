using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Local rotations of the configured upper bones, sampled at equal semantic
    // phases. The final sample closes the loop; bones are sample-major.
    [Serializable]
    public sealed class AnimationPlaygroundMovingTurnBonePose
    {
        public AnimationClip sourceClip;
        public int samples;
        public Quaternion[] rotations = Array.Empty<Quaternion>();

        public Quaternion Sample(float semanticPhase, int bone, int boneCount)
        {
            if (!float.IsFinite(semanticPhase) || boneCount <= 0 || bone < 0 || bone >= boneCount
                || samples < 2 || samples > 2049 || rotations == null || rotations.LongLength != (long)samples * boneCount)
                return Quaternion.identity;
            var position = Mathf.Repeat(semanticPhase, 1) * (samples - 1);
            var first = Mathf.Min(Mathf.FloorToInt(position), samples - 1);
            var second = Mathf.Min(first + 1, samples - 1);
            var a = rotations[first * boneCount + bone];
            var b = rotations[second * boneCount + bone];
            if (!FiniteRotation(a) || !FiniteRotation(b)) return Quaternion.identity;
            return Quaternion.Slerp(a.normalized, b.normalized, position - first).normalized;
        }

        internal static bool FiniteRotation(Quaternion rotation)
        {
            var norm = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
            return float.IsFinite(rotation.x) && float.IsFinite(rotation.y) && float.IsFinite(rotation.z)
                && float.IsFinite(rotation.w) && float.IsFinite(norm) && norm > .00000001f;
        }
    }

    [Serializable]
    public sealed class AnimationPlaygroundMovingTurnBand
    {
        public AnimationPlaygroundForwardGait left, right;
        public AnimationPlaygroundMovingTurnBonePose leftPose, rightPose;
        public float sourceCurvatureDegreesPerMetre;
        public float maximumWeight = .5f;
        public string title, provenance;
    }

    // Optional pose contributors paired to the exact accepted forward policy.
    // Arc sources share its distance clock; this asset cannot redefine a gait.
    public sealed class AnimationPlaygroundMovingTurnProfile : ScriptableObject
    {
        public const float MaximumAllowedWeight = .65f;
        public const float SourceRateFadeWidth = .10f;
        public int schema = 1;
        public AnimationPlaygroundForwardLocomotionProfile forwardProfile;
        public HumanBodyBones[] upperBodyBones = Array.Empty<HumanBodyBones>();
        public AnimationPlaygroundMovingTurnBand[] bands = Array.Empty<AnimationPlaygroundMovingTurnBand>();
        public GameObject TargetModel => forwardProfile != null ? forwardProfile.targetModel : null;
        public AnimationPlaygroundFootProjection FootProjection => forwardProfile != null ? forwardProfile.footProjection : null;

        public bool Validate(AnimationPlaygroundForwardLocomotionProfile expected, out string reason)
        {
            reason = null;
            if (schema != 1 || expected == null || forwardProfile != expected || !expected.ExpandedReview
                || expected.gaits == null || expected.gaits.Length != 5 || expected.targetModel == null || expected.footProjection == null)
            { reason = "Moving turns require the exact paired five-band forward policy, model and foot projection."; return false; }
            if (!expected.Validate(expected.targetModel, expected.locomotion, out var forwardReason, false))
            { reason = "The paired forward policy is invalid: " + forwardReason; return false; }
            if (bands == null || bands.Length != 5)
            { reason = "Moving turns require five complete bands in accepted forward-gait order."; return false; }
            if (upperBodyBones == null || upperBodyBones.Length == 0 || upperBodyBones.Length > 13)
            { reason = "Moving turns require a nonempty upper-body bone subset."; return false; }
            var selectedBones = new HashSet<HumanBodyBones>();
            foreach (var bone in upperBodyBones)
                if (!AllowedUpperBone(bone) || !selectedBones.Add(bone))
                { reason = "Moving-turn bones must be unique allowed upper bones; root, hips and legs are excluded."; return false; }

            var clips = new HashSet<AnimationClip>();
            foreach (var gait in expected.gaits) clips.Add(gait.sourceClip);
            var speeds = RateSpeeds(expected);
            for (var i = 0; i < bands.Length; i++)
            {
                var band = bands[i];
                if (band == null || string.IsNullOrWhiteSpace(band.title) || string.IsNullOrWhiteSpace(band.provenance)
                    || !float.IsFinite(band.sourceCurvatureDegreesPerMetre) || band.sourceCurvatureDegreesPerMetre <= 0
                    || !float.IsFinite(band.maximumWeight) || band.maximumWeight <= 0 || band.maximumWeight > MaximumAllowedWeight)
                { reason = $"Moving-turn band {i} requires provenance, positive source curvature and a weight in (0, 0.65]."; return false; }
                if (!ValidateGait(band.left, expected.gaits[i], expected, i, false, clips, speeds, out var sourceReason))
                { reason = $"Moving-turn band {i} left: {sourceReason}"; return false; }
                if (!ValidateGait(band.right, expected.gaits[i], expected, i, true, clips, speeds, out sourceReason))
                { reason = $"Moving-turn band {i} right: {sourceReason}"; return false; }
                if (!ValidatePose(band.leftPose, band.left.sourceClip, out sourceReason))
                { reason = $"Moving-turn band {i} left pose: {sourceReason}"; return false; }
                if (!ValidatePose(band.rightPose, band.right.sourceClip, out sourceReason))
                { reason = $"Moving-turn band {i} right pose: {sourceReason}"; return false; }
            }
            return true;
        }

        private static bool AllowedUpperBone(HumanBodyBones bone) => bone switch
        {
            HumanBodyBones.Spine or HumanBodyBones.Chest or HumanBodyBones.UpperChest
                or HumanBodyBones.Neck or HumanBodyBones.Head or HumanBodyBones.LeftShoulder
                or HumanBodyBones.LeftUpperArm or HumanBodyBones.LeftLowerArm or HumanBodyBones.LeftHand
                or HumanBodyBones.RightShoulder or HumanBodyBones.RightUpperArm
                or HumanBodyBones.RightLowerArm or HumanBodyBones.RightHand => true,
            _ => false
        };

        private bool ValidatePose(AnimationPlaygroundMovingTurnBonePose pose, AnimationClip source, out string reason)
        {
            reason = null;
            if (pose == null || pose.sourceClip == null || pose.sourceClip != source || pose.samples < 2 || pose.samples > 2049
                || pose.rotations == null || pose.rotations.LongLength != (long)pose.samples * upperBodyBones.Length)
            { reason = "A source-paired upper-bone tape with 2-2049 complete samples is required."; return false; }
            foreach (var rotation in pose.rotations)
            {
                var norm = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
                if (!AnimationPlaygroundMovingTurnBonePose.FiniteRotation(rotation) || Mathf.Abs(norm - 1) > .002f)
                { reason = "Upper-bone tape rotations must be finite and normalized."; return false; }
            }
            for (var bone = 0; bone < upperBodyBones.Length; bone++)
                if (Quaternion.Angle(pose.rotations[bone].normalized,
                    pose.rotations[(pose.samples - 1) * upperBodyBones.Length + bone].normalized) > .1f)
                { reason = "Upper-bone tape loop seams must stay within 0.1 degrees."; return false; }
            return true;
        }

        // Preserve the accepted straight pose when an arc's native source rate
        // approaches a mixed-gait edge. This does not alter the clock or source
        // time: callers return the removed arc weight to its straight channel.
        public float RateWeight(int band, double cycles, float speed, bool right)
        {
            if (!TrySourceRate(band, cycles, speed, right, out var gait, out var rate)
                || rate <= gait.blendMinimumRate || rate >= gait.blendMaximumRate) return 0;
            var enter = Mathf.SmoothStep(0, 1, (float)((rate - gait.blendMinimumRate) / SourceRateFadeWidth));
            var leave = Mathf.SmoothStep(0, 1, (float)((gait.blendMaximumRate - rate) / SourceRateFadeWidth));
            return Mathf.Clamp01(enter * leave);
        }

        private bool TrySourceRate(int band, double cycles, float speed, bool right,
            out AnimationPlaygroundForwardGait gait, out double rate)
        {
            gait = null; rate = 0;
            if (schema != 1 || !double.IsFinite(cycles) || cycles < 0 || !float.IsFinite(speed) || speed <= 0
                || forwardProfile == null || !forwardProfile.ExpandedReview || forwardProfile.gaits == null
                || forwardProfile.gaits.Length != 5 || forwardProfile.secondRightDistanceAdjustment == null
                || !float.IsFinite(forwardProfile.walkToJogBlendEndSpeed) || forwardProfile.walkToJogBlendEndSpeed < 0
                || bands == null || bands.Length != 5 || band < 0 || band >= bands.Length || bands[band] == null) return false;
            var previousSpeed = 0f;
            foreach (var forward in forwardProfile.gaits)
            {
                if (forward == null || !float.IsFinite(forward.nominalSpeed) || forward.nominalSpeed <= previousSpeed
                    || !float.IsFinite(forward.metresPerCycle) || forward.metresPerCycle <= 0) return false;
                previousSpeed = forward.nominalSpeed;
            }
            var end = forwardProfile.walkToJogBlendEndSpeed > 0 ? forwardProfile.walkToJogBlendEndSpeed : forwardProfile.gaits[1].nominalSpeed;
            if (end <= forwardProfile.gaits[0].nominalSpeed || end > forwardProfile.gaits[1].nominalSpeed) return false;
            gait = right ? bands[band].right : bands[band].left;
            if (gait?.sourceClip == null || !float.IsFinite(gait.sourceClip.length) || gait.sourceClip.length <= 0
                || gait.phaseMap == null || gait.phaseMap.Length < 2 || gait.phaseMap.Length > 256
                || gait.phaseMap[0].semantic != 0 || gait.phaseMap[^1].semantic != 1
                || !ValidRates(gait.blendMinimumRate, gait.blendMaximumRate, .45f, 1.65f)
                || gait.blendMinimumRate >= gait.blendMaximumRate) return false;
            var phase = cycles - Math.Floor(cycles);
            var native = gait.NativePhase((float)phase, out var derivative);
            var distanceDerivative = forwardProfile.DistanceDerivative(speed, cycles);
            if (!float.IsFinite(native) || !float.IsFinite(derivative) || derivative <= 0
                || !double.IsFinite(distanceDerivative) || distanceDerivative <= 0) return false;
            rate = (double)derivative * gait.sourceClip.length * speed / distanceDerivative;
            return double.IsFinite(rate) && rate > 0;
        }

        private bool ValidateGait(AnimationPlaygroundForwardGait gait, AnimationPlaygroundForwardGait accepted,
            AnimationPlaygroundForwardLocomotionProfile forward, int band, bool right, HashSet<AnimationClip> clips,
            List<float> speeds, out string reason)
        {
            reason = null;
            if (gait == null || gait.role != accepted.role || gait.nominalSpeed != accepted.nominalSpeed
                || gait.metresPerCycle != accepted.metresPerCycle)
            { reason = "Role, nominal speed and metres per cycle must exactly match the accepted band."; return false; }
            if (gait.sourceClip == null || !gait.sourceClip.isHumanMotion || !gait.sourceClip.isLooping
                || !float.IsFinite(gait.sourceClip.length) || gait.sourceClip.length <= 0)
            { reason = "A complete looping humanoid source is required."; return false; }
            if (!clips.Add(gait.sourceClip))
            { reason = "Every band and side needs a unique arc source; accepted forward clips cannot be aliases."; return false; }
            if (!ValidRates(gait.pureMinimumRate, gait.pureMaximumRate, .65f, 1.35f)
                || !ValidRates(gait.blendMinimumRate, gait.blendMaximumRate, .45f, 1.65f))
            { reason = "Pure and mixed source-rate bounds exceed the reviewed forward budgets."; return false; }
            if (gait.nativeContacts == null || gait.nativeContacts.Length != 4 || gait.phaseMap == null
                || gait.phaseMap.Length < 2 || gait.phaseMap.Length > 256 || gait.signedHeight == null || gait.signedHeight.length < 2)
            { reason = "Four native contacts and complete phase/height calibration are required."; return false; }
            for (var k = 0; k < 4; k++)
                if (!float.IsFinite(gait.nativeContacts[k]) || gait.nativeContacts[k] < 0 || gait.nativeContacts[k] >= 1
                    || k > 0 && gait.nativeContacts[k] <= gait.nativeContacts[k - 1])
                { reason = "Native R0-L0-R1-L1 contacts must remain finite and ordered."; return false; }
            for (var k = 0; k < gait.phaseMap.Length; k++)
            {
                var point = gait.phaseMap[k];
                if (!float.IsFinite(point.semantic) || !float.IsFinite(point.native)
                    || k > 0 && (point.semantic <= gait.phaseMap[k - 1].semantic || point.native <= gait.phaseMap[k - 1].native))
                { reason = "Native phase mapping must be finite and strictly increasing."; return false; }
                if (k == 0) continue;
                var previous = gait.phaseMap[k - 1];
                var derivative = (double)(point.native - previous.native) / (point.semantic - previous.semantic);
                var pureRate = derivative * gait.sourceClip.length * gait.nominalSpeed / gait.metresPerCycle;
                if (!InRate(pureRate, gait.pureMinimumRate, gait.pureMaximumRate))
                { reason = "A native source segment exceeds its pure-gait playback budget."; return false; }
            }
            if (gait.phaseMap[0].semantic != 0 || gait.phaseMap[^1].semantic != 1
                || Mathf.Abs(gait.phaseMap[0].native - gait.nativeContacts[0]) > .0001f
                || Mathf.Abs(gait.phaseMap[^1].native - gait.nativeContacts[0] - 1) > .0001f)
            { reason = "Phase mapping must retain one complete native loop from the first right contact."; return false; }
            for (var k = 0; k < 4; k++)
                if (Mathf.Abs(gait.NativePhase(k * .25f, out _) - gait.nativeContacts[k]) > .001f)
                { reason = "Semantic plants disagree with the measured native contacts."; return false; }
            var keys = gait.signedHeight.keys;
            if (keys[0].time != 0 || keys[^1].time != 1 || Mathf.Abs(keys[0].value - keys[^1].value) > .001f)
            { reason = "Signed height calibration must close the full loop."; return false; }
            for (var k = 0; k < keys.Length; k++)
            {
                var key = keys[k];
                if (!float.IsFinite(key.time) || !float.IsFinite(key.value) || key.time < 0 || key.time > 1
                    || key.value < -forward.maximumLowering || key.value > forward.maximumLift
                    || !float.IsFinite(key.inTangent) || !float.IsFinite(key.outTangent) || key.weightedMode != WeightedMode.None
                    || k > 0 && key.time <= keys[k - 1].time)
                { reason = "Signed height calibration exceeds the accepted visual bounds."; return false; }
                if (k == 0) continue;
                var previous = keys[k - 1];
                var slope = (key.value - previous.value) / (key.time - previous.time);
                if (!float.IsFinite(slope) || Mathf.Abs(previous.outTangent - slope) > .001f || Mathf.Abs(key.inTangent - slope) > .001f)
                { reason = "Signed height segments must retain bounded linear interpolation."; return false; }
            }

            // Mixed sources may be attenuated at rate edges. Qualify the actual
            // maximum arc contribution, preserving the unchanged straight gait
            // wherever its paired arc is outside the stored playback budget.
            foreach (var speed in speeds)
            {
                if (speed <= 0) continue;
                var forwardWeight = forward.PoseWeights(speed)[band + 1];
                if (forwardWeight * bands[band].maximumWeight < forward.significantWeight - .00001f) continue;
                for (var k = 1; k < gait.phaseMap.Length; k++)
                {
                    var a = gait.phaseMap[k - 1]; var b = gait.phaseMap[k];
                    foreach (var phase in new[] { (double)a.semantic, Math.Max(a.semantic, b.semantic - .00000001),
                        Math.Clamp(.4, a.semantic, b.semantic), Math.Clamp(.45, a.semantic, b.semantic),
                        Math.Clamp(.55, a.semantic, b.semantic), Math.Clamp(.6, a.semantic, b.semantic) })
                    {
                        var contribution = RateWeight(band, phase, speed, right) * bands[band].maximumWeight * forwardWeight;
                        if (contribution < forward.significantWeight - .00001f) continue;
                        if (!TrySourceRate(band, phase, speed, right, out _, out var rate)
                            || !InRate(rate, gait.blendMinimumRate, gait.blendMaximumRate))
                        { reason = $"A significant contributor exceeds its mixed-gait source playback budget ({speed:R} m/s, source rate {rate:R})."; return false; }
                    }
                }
            }
            return true;
        }

        private static List<float> RateSpeeds(AnimationPlaygroundForwardLocomotionProfile forward)
        {
            var speeds = new List<float> { .18f,
                Mathf.Lerp(forward.gaits[0].nominalSpeed, forward.gaits[1].nominalSpeed, .1f),
                Mathf.Lerp(forward.gaits[0].nominalSpeed, forward.gaits[1].nominalSpeed, .9f),
                Mathf.Lerp(forward.gaits[1].nominalSpeed, forward.gaits[^1].nominalSpeed, .1f),
                Mathf.Lerp(forward.gaits[1].nominalSpeed, forward.gaits[^1].nominalSpeed, .9f) };
            for (var i = 0; i < forward.gaits.Length; i++)
            {
                speeds.Add(forward.gaits[i].nominalSpeed);
                if (i == 0) continue;
                speeds.Add(Mathf.Lerp(forward.gaits[i - 1].nominalSpeed, forward.gaits[i].nominalSpeed, .1f));
                speeds.Add(Mathf.Lerp(forward.gaits[i - 1].nominalSpeed, forward.gaits[i].nominalSpeed, .9f));
            }
            var end = forward.walkToJogBlendEndSpeed > 0 ? forward.walkToJogBlendEndSpeed : forward.gaits[1].nominalSpeed;
            speeds.Add(end);
            speeds.Add(Mathf.Lerp(forward.gaits[0].nominalSpeed, end, .1f));
            speeds.Add(Mathf.Lerp(forward.gaits[0].nominalSpeed, end, .9f));
            foreach (var point in forward.secondRightDistanceAdjustment) speeds.Add(point.x);
            return speeds;
        }

        private static bool ValidRates(float minimum, float maximum, float low, float high) =>
            float.IsFinite(minimum) && float.IsFinite(maximum) && minimum >= low && maximum <= high && minimum <= maximum;
        private static bool InRate(double rate, float minimum, float maximum) =>
            double.IsFinite(rate) && rate >= minimum - .001 && rate <= maximum + .001;
    }
}
