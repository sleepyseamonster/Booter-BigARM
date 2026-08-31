using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DLocomotionClipRole
    {
        Idle,
        Walk,
        Run,
        Sprint,
        DirectionalLeft,
        DirectionalRight,
        StartLeft,
        StartRight,
        StopLeft,
        StopRight,
        PivotLeft,
        PivotRight,
    }

    [Serializable]
    public sealed class TopDown3DLocomotionClipEntry
    {
        [SerializeField] private TopDown3DLocomotionClipRole role;
        [SerializeField] private AnimationClip clip;
        [SerializeField] private float nominalForwardSpeed;
        [SerializeField] private float nominalLateralSpeed;
        [SerializeField] private bool looping;
        [SerializeField] private float[] leftContactPhases = Array.Empty<float>();
        [SerializeField] private float[] rightContactPhases = Array.Empty<float>();
        [SerializeField] private TopDown3DFootSide plantedFoot;
        [SerializeField, Range(0f, 1f)] private float plantTime;
        [SerializeField, Range(0f, 1f)] private float releaseTime = 1f;
        [SerializeField, Range(0.5f, 1.5f)] private float minimumPlaybackRate = 0.9f;
        [SerializeField, Range(0.5f, 1.5f)] private float maximumPlaybackRate = 1.1f;
        [SerializeField] private TopDown3DLocomotionClipRole[] preferredEntryRoles =
            Array.Empty<TopDown3DLocomotionClipRole>();
        [SerializeField] private TopDown3DLocomotionClipRole[] preferredExitRoles =
            Array.Empty<TopDown3DLocomotionClipRole>();
        [SerializeField] private bool intentionalMirror;

        public TopDown3DLocomotionClipRole Role => role;
        public AnimationClip Clip => clip;
        public float NominalForwardSpeed => nominalForwardSpeed;
        public float NominalLateralSpeed => nominalLateralSpeed;
        public bool Looping => looping;
        public float[] LeftContactPhases => leftContactPhases ?? Array.Empty<float>();
        public float[] RightContactPhases => rightContactPhases ?? Array.Empty<float>();
        public TopDown3DFootSide PlantedFoot => plantedFoot;
        public float PlantTime => Mathf.Clamp01(plantTime);
        public float ReleaseTime => Mathf.Clamp01(releaseTime);
        public float MinimumPlaybackRate => Mathf.Max(0.01f, minimumPlaybackRate);
        public float MaximumPlaybackRate => Mathf.Max(MinimumPlaybackRate, maximumPlaybackRate);
        public TopDown3DLocomotionClipRole[] PreferredEntryRoles =>
            preferredEntryRoles ?? Array.Empty<TopDown3DLocomotionClipRole>();
        public TopDown3DLocomotionClipRole[] PreferredExitRoles =>
            preferredExitRoles ?? Array.Empty<TopDown3DLocomotionClipRole>();
        public bool IntentionalMirror => intentionalMirror;

        public bool HasOrderedContacts()
        {
            if (!looping || role == TopDown3DLocomotionClipRole.Idle)
            {
                return true;
            }

            return AreOrderedNormalizedPhases(LeftContactPhases)
                && AreOrderedNormalizedPhases(RightContactPhases)
                && LeftContactPhases.Length == 2
                && RightContactPhases.Length == 2;
        }

        private static bool AreOrderedNormalizedPhases(float[] phases)
        {
            var previous = -1f;
            for (var i = 0; i < phases.Length; i++)
            {
                if (phases[i] < 0f || phases[i] >= 1f || phases[i] <= previous)
                {
                    return false;
                }

                previous = phases[i];
            }

            return true;
        }
    }

    [CreateAssetMenu(
        fileName = "TopDown3DLocomotionClipProfile",
        menuName = "Booter & BigARM/Top Down 3D/Locomotion Clip Profile")]
    public sealed class TopDown3DLocomotionClipProfile : ScriptableObject
    {
        [SerializeField] private TopDown3DLocomotionClipEntry[] clips =
            Array.Empty<TopDown3DLocomotionClipEntry>();

        [Header("State Thresholds")]
        [SerializeField, Min(0f)] private float idleExitSpeed = 0.35f;
        [SerializeField, Min(0f)] private float startCompletionSpeed = 1.2f;
        [SerializeField, Min(0f)] private float stopIntentSpeed = 0.15f;
        [SerializeField, Min(0f)] private float stopEntrySpeed = 0.75f;
        [SerializeField, Min(0f)] private float idleEntrySpeed = 0.15f;
        [SerializeField, Min(0f)] private float pivotEntrySpeed = 2.4f;
        [SerializeField, Range(0f, 180f)] private float pivotEntryAngle = 125f;
        [SerializeField, Range(0f, 180f)] private float pivotReleaseAngle = 72f;
        [SerializeField, Range(0.01f, 0.35f)] private float sustainedBlendDuration = 0.18f;
        [SerializeField, Range(0.01f, 0.35f)] private float oneShotBlendDuration = 0.14f;

        public float IdleExitSpeed => idleExitSpeed;
        public float StartCompletionSpeed => startCompletionSpeed;
        public float StopIntentSpeed => stopIntentSpeed;
        public float StopEntrySpeed => stopEntrySpeed;
        public float IdleEntrySpeed => idleEntrySpeed;
        public float PivotEntrySpeed => pivotEntrySpeed;
        public float PivotEntryAngle => pivotEntryAngle;
        public float PivotReleaseAngle => pivotReleaseAngle;
        public float SustainedBlendDuration => sustainedBlendDuration;
        public float OneShotBlendDuration => oneShotBlendDuration;

        public bool TryGetEntry(
            TopDown3DLocomotionClipRole role,
            out TopDown3DLocomotionClipEntry entry)
        {
            entry = null;
            for (var i = 0; i < clips.Length; i++)
            {
                var candidate = clips[i];
                if (candidate != null && candidate.Role == role)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        public TopDown3DLocomotionClipEntry GetRequiredEntry(TopDown3DLocomotionClipRole role)
        {
            if (TryGetEntry(role, out var entry) && entry.Clip != null)
            {
                return entry;
            }

            throw new InvalidOperationException($"Locomotion profile is missing required role '{role}'.");
        }

        public bool TryValidate(out string error)
        {
            var roleCount = Enum.GetValues(typeof(TopDown3DLocomotionClipRole)).Length;
            if (clips == null || clips.Length != roleCount)
            {
                error = $"Profile must contain exactly {roleCount} clip roles.";
                return false;
            }

            for (var roleIndex = 0; roleIndex < roleCount; roleIndex++)
            {
                var role = (TopDown3DLocomotionClipRole)roleIndex;
                var matches = 0;
                TopDown3DLocomotionClipEntry entry = null;
                for (var clipIndex = 0; clipIndex < clips.Length; clipIndex++)
                {
                    if (clips[clipIndex] != null && clips[clipIndex].Role == role)
                    {
                        matches++;
                        entry = clips[clipIndex];
                    }
                }

                if (matches != 1 || entry == null || entry.Clip == null)
                {
                    error = $"Role '{role}' must have one non-null clip authority.";
                    return false;
                }

                if (entry.Looping != entry.Clip.isLooping)
                {
                    error = $"Role '{role}' loop metadata disagrees with its imported clip.";
                    return false;
                }

                if (!entry.HasOrderedContacts())
                {
                    error = $"Role '{role}' has invalid contact phases.";
                    return false;
                }

                if (!entry.Looping && entry.ReleaseTime < entry.PlantTime)
                {
                    error = $"Role '{role}' releases before its plant.";
                    return false;
                }

                if (entry.MinimumPlaybackRate < 0.9f || entry.MaximumPlaybackRate > 1.1f)
                {
                    error = $"Role '{role}' exceeds the approved 0.90-1.10 playback range.";
                    return false;
                }

                if (entry.PreferredEntryRoles.Length == 0 || entry.PreferredExitRoles.Length == 0)
                {
                    error = $"Role '{role}' must record preferred entry and exit roles.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            idleExitSpeed = Mathf.Max(0f, idleExitSpeed);
            startCompletionSpeed = Mathf.Max(idleExitSpeed, startCompletionSpeed);
            stopIntentSpeed = Mathf.Max(0f, stopIntentSpeed);
            stopEntrySpeed = Mathf.Max(stopIntentSpeed, stopEntrySpeed);
            idleEntrySpeed = Mathf.Max(0f, idleEntrySpeed);
            pivotEntrySpeed = Mathf.Max(0f, pivotEntrySpeed);
            pivotEntryAngle = Mathf.Clamp(pivotEntryAngle, 0f, 180f);
            pivotReleaseAngle = Mathf.Clamp(pivotReleaseAngle, 0f, pivotEntryAngle);
        }
    }
}
