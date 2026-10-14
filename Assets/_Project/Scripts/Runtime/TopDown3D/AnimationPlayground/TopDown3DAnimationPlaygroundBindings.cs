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
        public AnimationPlaygroundCrouchProfile crouch;
        public AnimationPlaygroundFootProjection footProjection;
        public AnimationPlaygroundAirborneProfile airborne;
        public float visualScale = 1f;
        public Vector3 visualOffset;
        public Material bodyMaterial;
        public float bodyHeight = 1.8f, capsuleHeight = 1.82f, capsuleRadius = .2f;
        public float equipmentModelHeight = 1.77220833f;
        public float slowWalkSpeed = 1.4f, runSpeed = 2.7f, sprintSpeed = 3.8f;
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
        public bool ValidatePlayer(out string reason)
        {
            if (!ValidateImport(out reason)) return false;
            if (locomotion == null || !locomotion.TryValidate(out reason))
            { reason = "A valid private locomotion profile is required: " + reason; return false; }
            reason = null;
            if (bodyMaterial == null) reason = "The calibrated character requires a body material.";
            else if (!float.IsFinite(bodyHeight) || bodyHeight <= 0 || !float.IsFinite(capsuleHeight)
                || !float.IsFinite(capsuleRadius) || capsuleRadius <= 0 || capsuleHeight < bodyHeight
                || capsuleHeight < capsuleRadius * 2 || !float.IsFinite(equipmentModelHeight) || equipmentModelHeight <= 0)
                reason = "Body, collider or equipment dimensions are invalid.";
            else if (!float.IsFinite(slowWalkSpeed) || !float.IsFinite(runSpeed) || !float.IsFinite(sprintSpeed)
                || slowWalkSpeed <= 0 || runSpeed < slowWalkSpeed || sprintSpeed < runSpeed)
                reason = "Laboratory locomotion speeds must be finite and ordered.";
            else if(footProjection!=null && !footProjection.Validate(targetModel,out reason))return false;
            else if(crouch!=null && (crouch.contactsQualified || crouch.groundingQualified) && footProjection==null)
                reason="A qualified crouch family requires its paired transition foot projection.";
            return reason == null;
        }
    }
}
