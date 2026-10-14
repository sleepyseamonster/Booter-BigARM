using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundMotionReadiness { SourceRecorded, ImportQualified, PreviewQualified, PhysicalQualified, AuthorAccepted }
    [Serializable]
    public sealed class AnimationPlaygroundMotionBinding
    {
        public string sourceId;
        public AnimationClip clip;
        public AnimationPlaygroundMotionReadiness readiness;
        public bool restrictedReference;
        public bool loop;
        public float contactTime = -1f, releaseTime = -1f;
        public bool CanPreview => clip != null && readiness >= AnimationPlaygroundMotionReadiness.ImportQualified && !restrictedReference;
        public bool CanPerform => clip != null && readiness >= AnimationPlaygroundMotionReadiness.PhysicalQualified && !restrictedReference;
    }

    public sealed class TopDown3DAnimationPlaygroundBindings : ScriptableObject
    {
        public int schemaVersion = 1;
        public GameObject targetModel;
        public Avatar sourceAvatar, targetAvatar;
        public TopDown3DLocomotionClipProfile locomotion;
        public float visualScale = 1f;
        public Vector3 visualOffset;
        public AnimationPlaygroundMotionBinding[] motions = Array.Empty<AnimationPlaygroundMotionBinding>();
        public bool ValidateImport(out string reason)
        {
            reason = null;
            if (schemaVersion != 1) reason = "Unsupported private animation binding schema.";
            else if (targetModel == null || sourceAvatar == null || targetAvatar == null) reason = "Character model/source/target Avatar is missing.";
            else if (!sourceAvatar.isValid || !sourceAvatar.isHuman || !targetAvatar.isValid || !targetAvatar.isHuman) reason = "Source and target Humanoid Avatars must qualify separately.";
            else if (!float.IsFinite(visualScale) || visualScale <= 0 || !float.IsFinite(visualOffset.x) || !float.IsFinite(visualOffset.y) || !float.IsFinite(visualOffset.z)) reason = "Character calibration is invalid.";
            else
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var motion in motions ?? Array.Empty<AnimationPlaygroundMotionBinding>())
                    if (motion == null || string.IsNullOrWhiteSpace(motion.sourceId) || !ids.Add(motion.sourceId) || motion.clip == null)
                    { reason = "Animation bindings have missing clips or duplicate source IDs."; break; }
            }
            return reason == null;
        }
        public AnimationPlaygroundMotionBinding Find(string id)
        {
            foreach (var motion in motions ?? Array.Empty<AnimationPlaygroundMotionBinding>())
                if (motion != null && motion.sourceId == id) return motion;
            return null;
        }
    }
}
