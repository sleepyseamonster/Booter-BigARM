using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private const int ForwardReviewInputIndex = 24;
        private TopDown3DAnimationPlaygroundStation forwardReviewStation;
        private TopDown3DAnimationPlaygroundSession forwardReviewSession;
        private bool forwardReviewConfigured;
        private AnimationMixerPlayable forwardReviewMixer;
        private readonly AnimationClipPlayable[] forwardReviewClips = new AnimationClipPlayable[6];
        private readonly float[] forwardReviewBaselineWeights = new float[ClipCount];
        private bool forwardReviewOutputApplied;
        public bool LaboratoryForwardReviewConfigured => forwardReviewConfigured;
        public bool LaboratoryForwardReviewActive => LaboratoryForwardReviewConfigured && forwardPoseRenderedThisFrame
            && forwardReviewOutputApplied && ForwardReviewSupportAvailable() && !AnimationPresentationOwned && !CrouchPresentationOwned
            && !PoseBlendOwned && motor != null && !motor.IsMounted && !motor.IsActionConstrained;
        public string LaboratoryForwardReviewStatus => (!LaboratoryForwardReviewConfigured ? null
            : LaboratoryForwardFailure != null ? "Forward gait review unavailable: " + LaboratoryForwardFailure
            : LaboratoryForwardReviewActive ? "Experimental forward gaits — AP02 flat deck"
            : "Experimental forward gaits available on AP02's flat deck. Baseline active elsewhere.")
            + (LaboratoryStandingStrafeConfigured ? "\n" + LaboratoryStandingStrafeStatus : null);

        public void ConfigureLaboratoryForwardReview(AnimationPlaygroundForwardLocomotionProfile policy,
            TopDown3DAnimationPlaygroundStation station)
        {
            if (playableGraph.IsValid() || visualInstance != null || laboratoryAvatar == null)
                throw new InvalidOperationException("Configure forward review after lab bindings and before visual initialization.");
            var session = TopDown3DAnimationPlaygroundSession.Find(gameObject.scene);
            if (session == null || session.Player == null || session.Player.gameObject != gameObject
                || station == null || station.gameObject.scene != gameObject.scene || station.StationId != "AP02"
                || !station.transform.IsChildOf(session.transform) || Array.IndexOf(session.Stations, station) < 0)
                throw new ArgumentException("Forward review requires the authored Camp Loop player and AP02 station.");
            if (policy == null || policy.qualified || policy.terrainQualified
                || !policy.Validate(humanoidPrefab, policy.locomotion, out var reason, false)
                || policy.footProjection != laboratoryFootProjection)
                throw new ArgumentException("Forward review requires the paired unqualified flat-only policy.");
            laboratoryForwardProfile = policy; forwardReviewStation = station; forwardReviewSession = session;
            forwardReviewConfigured = true;
        }

        private bool ForwardReviewSupportAvailable()
        {
            if (!LaboratoryForwardReviewConfigured) return true;
            if (forwardReviewStation == null || forwardReviewSession == null || !forwardReviewSession.isActiveAndEnabled
                || forwardReviewSession.Readiness == AnimationPlaygroundReadiness.Failed
                || !forwardReviewStation.isActiveAndEnabled || motor == null || !motor.TryGetLaboratorySupportPlane(out var plane, true)
                || Vector3.Angle(plane.normal, Vector3.up) > .25f) return false;
            var support = motor.LaboratorySupportCollider;
            if (support == null || !support.transform.IsChildOf(forwardReviewStation.transform)) return false;
            var point = forwardReviewStation.transform.InverseTransformPoint(motor.Position);
            var size = forwardReviewStation.Footprint;
            var margin = GetComponent<CapsuleCollider>().radius;
            return Mathf.Abs(point.x) < size.x * .5f - margin && Mathf.Abs(point.z) < size.y * .5f - margin;
        }

        private void InitializeForwardReviewInputs()
        {
            if (!LaboratoryForwardReviewConfigured) return;
            if (mixer.GetInputCount() <= ForwardReviewInputIndex) mixer.SetInputCount(ForwardReviewInputIndex + 1);
            var inputs = laboratoryForwardProfile.MovingGaitCount + 1;
            forwardReviewMixer = AnimationMixerPlayable.Create(playableGraph, inputs);
            for (var i = 0; i < inputs; i++)
            {
                var clip = i == 0 ? laboratoryForwardProfile.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip
                    : laboratoryForwardProfile.gaits[i - 1].sourceClip;
                var playable = AnimationClipPlayable.Create(playableGraph, clip);
                playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); playable.SetSpeed(0);
                playableGraph.Connect(playable, 0, forwardReviewMixer, i); forwardReviewClips[i] = playable;
            }
            playableGraph.Connect(forwardReviewMixer, 0, mixer, ForwardReviewInputIndex);
            mixer.SetInputWeight(ForwardReviewInputIndex, 0);
            InitializeStandingStrafeReviewInputs();
        }

        private AnimationClipPlayable ForwardClip(int role) => LaboratoryForwardReviewConfigured ? forwardReviewClips[role] : clipPlayables[role];
        private void SetForwardPoseWeights(AnimationPlaygroundGaitWeights weights)
        {
            if (LaboratoryForwardReviewConfigured)
            {
                for (var i = 0; i < ClipCount; i++) forwardReviewBaselineWeights[i] = mixer.GetInputWeight(i);
                for (var i = 0; i < mixer.GetInputCount(); i++) mixer.SetInputWeight(i, i == ForwardReviewInputIndex ? 1 : 0);
                for (var i = 0; i < forwardReviewMixer.GetInputCount(); i++) forwardReviewMixer.SetInputWeight(i, weights[i]);
                forwardReviewOutputApplied = true;
            }
            else for (var i = 0; i < mixer.GetInputCount(); i++) mixer.SetInputWeight(i, i < 4 ? weights[i] : 0);
        }

        private void RestoreForwardReviewBaseline()
        {
            if (!forwardReviewOutputApplied || !mixer.IsValid()) return;
            mixer.SetInputWeight(ForwardReviewInputIndex, 0);
            for (var i = 0; i < ClipCount; i++) mixer.SetInputWeight(i, forwardReviewBaselineWeights[i]);
            forwardReviewOutputApplied = false;
        }
    }
}
