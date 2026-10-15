using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundTurnDirection { Left = -1, Right = 1 }

    [Serializable] public struct AnimationPlaygroundTurnSample
    {
        public float seconds, degrees;
        public AnimationPlaygroundTurnSample(float seconds, float degrees)
        { this.seconds = seconds; this.degrees = degrees; }
    }

    [Serializable] public sealed class AnimationPlaygroundTurnSource
    {
        public AnimationPlaygroundTurnDirection direction;
        public AnimationClip clip;
        public string sampledClipName;
        public float duration, maximumPlanarRootExcursion, maximumVerticalRootExcursion;
        public AnimationPlaygroundTurnSample[] samples = Array.Empty<AnimationPlaygroundTurnSample>();

        // Piecewise linear data comes from evaluated target-root motion. Raw
        // imported RootQ curves do not contain the evaluated mirror contract.
        public float DegreesAtSeconds(float seconds)
        {
            RequireSamples();
            if (!float.IsFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            var low = 0; var high = samples.Length - 1;
            seconds = Mathf.Clamp(seconds, samples[low].seconds, samples[high].seconds);
            while (high - low > 1)
            { var middle = (low + high) / 2; if (samples[middle].seconds <= seconds) low = middle; else high = middle; }
            return Mathf.Lerp(samples[low].degrees, samples[high].degrees,
                (seconds - samples[low].seconds) / (samples[high].seconds - samples[low].seconds));
        }

        public float SecondsAtDegrees(float degrees)
        {
            RequireSamples();
            var sign = (int)direction;
            if (!float.IsFinite(degrees) || degrees * sign < 0) throw new ArgumentOutOfRangeException(nameof(degrees));
            var low = 0; var high = samples.Length - 1;
            var angle = Mathf.Clamp(degrees * sign, 0, samples[high].degrees * sign);
            if (angle <= 0) return samples[0].seconds;
            while (high - low > 1)
            { var middle = (low + high) / 2; if (samples[middle].degrees * sign < angle) low = middle; else high = middle; }
            var span = (samples[high].degrees - samples[low].degrees) * sign;
            // Duplicate yaw values represent a pause in the source. Return the
            // earliest bracketing time, without inventing angular progression.
            return span <= .000001f ? samples[low].seconds : Mathf.Lerp(samples[low].seconds, samples[high].seconds,
                (angle - samples[low].degrees * sign) / span);
        }

        public bool Validate(AnimationPlaygroundTurnDirection expected, out string reason)
        {
            reason = null;
            if (direction != expected || clip == null || !clip.isHumanMotion || string.IsNullOrWhiteSpace(sampledClipName)
                || clip.name != sampledClipName || !float.IsFinite(duration)
                || duration <= 0 || Mathf.Abs(duration - clip.length) > .0001f)
                reason = "The named Humanoid turn, direction and native duration must agree.";
            else if (!float.IsFinite(maximumPlanarRootExcursion) || maximumPlanarRootExcursion < 0 || maximumPlanarRootExcursion > .001f
                || !float.IsFinite(maximumVerticalRootExcursion) || maximumVerticalRootExcursion < 0 || maximumVerticalRootExcursion > .001f)
                reason = "This stationary turn contract cannot discard a source root trajectory.";
            else if (samples == null || samples.Length < 2 || Mathf.Abs(samples[0].seconds) > .000001f
                || Mathf.Abs(samples[0].degrees) > .0001f || Mathf.Abs(samples[^1].seconds - duration) > .0001f
                || Mathf.Abs(samples[^1].degrees * (int)direction - 180) > .001f)
                reason = "Turn samples must span the complete signed native 180-degree source.";
            else
            {
                for (var i = 0; i < samples.Length; i++)
                {
                    var sample = samples[i];
                    if (!float.IsFinite(sample.seconds) || !float.IsFinite(sample.degrees)
                        || sample.seconds < 0 || sample.seconds > duration + .0001f || sample.degrees * (int)direction < -.0001f
                        || i > 0 && (sample.seconds <= samples[i - 1].seconds
                            || (sample.degrees - samples[i - 1].degrees) * (int)direction < 0))
                    { reason = "Turn samples must retain finite increasing time and monotone signed yaw."; break; }
                }
            }
            return reason == null;
        }

        private void RequireSamples()
        {
            if (samples == null || samples.Length < 2 || direction != AnimationPlaygroundTurnDirection.Left && direction != AnimationPlaygroundTurnDirection.Right)
                throw new InvalidOperationException("Validate the complete turn source before sampling it.");
        }
    }

    // Source-data contract only. No motor, graph, input or world owner is added.
    // An unqualified asset is usable by offline probes through the explicit route.
    public sealed class AnimationPlaygroundTurnProfile : ScriptableObject
    {
        public int schema = 1;
        public bool qualified;
        public GameObject targetModel;
        public AnimationPlaygroundFootProjection footProjection;
        public float visualScale;
        public Vector3 visualOffset;
        public string sourceReceiptSha256, contactReceiptSha256;
        public AnimationPlaygroundTurnSource left = new(), right = new();

        public bool Validate(GameObject expectedModel, out string reason, bool requireQualified = true)
        {
            reason = null;
            if (schema != 1 || targetModel == null || targetModel != expectedModel || footProjection == null
                || !footProjection.Validate(targetModel, out reason))
            { reason ??= "Turn source data must pair with the selected model and sole cache."; return false; }
            if (requireQualified && !qualified) reason = "Turn contact, timing and handoffs are awaiting qualification.";
            else if (!float.IsFinite(visualScale) || visualScale <= 0 || !float.IsFinite(visualOffset.sqrMagnitude))
                reason = "The turn source calibration must be finite and positive.";
            else if (!IsSha256(sourceReceiptSha256) || !IsSha256(contactReceiptSha256))
                reason = "Source and contact observations must have exact SHA-256 receipts.";
            else if (left == null || right == null) reason = "Both signed turn sources are required.";
            else if (!left.Validate(AnimationPlaygroundTurnDirection.Left, out reason)
                || !right.Validate(AnimationPlaygroundTurnDirection.Right, out reason)) return false;
            else if (left.clip == right.clip) reason = "Left and right turns require distinct named clips.";
            return reason == null;
        }

        private static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
                if (character < '0' || character > '9')
                    if (character < 'a' || character > 'f') return false;
            return true;
        }
    }
}
