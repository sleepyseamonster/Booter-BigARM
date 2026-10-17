using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private AnimationPlaygroundMovingTurnProfile movingTurnProfile;
        private readonly AnimationPlaygroundMovingTurnDemand movingTurnDemand = new();
        private readonly float[] movingTurnWeights = new float[5];
        private Transform[] movingTurnBones;
        private Quaternion[] movingTurnLastDeltas, movingTurnSnapshotDeltas;
        private double movingTurnPoseCycles;
        private float movingTurnPoseCurvature, movingTurnRenderDelta;
        private bool movingTurnRight, movingTurnPoseReady, movingTurnLastHasDelta;
        private long movingTurnRenderSequence, movingTurnAppliedSequence = -1;
        public bool LaboratoryMovingTurnsConfigured => movingTurnProfile != null;
        public float LaboratoryMovingTurnCurvature => movingTurnDemand.Curvature;
        public float LaboratoryMovingTurnWeight { get; private set; }

        private bool MovingTurnStandingContext => movingTurnProfile != null && motor != null && motor.IsGrounded
            && !AnimationPresentationOwned && !CrouchPresentationOwned
            && !PoseBlendOwned && !JumpPresentationOwned && !motor.IsMounted && !motor.IsActionConstrained
            && motor.AnimationPosture == AnimationPlaygroundPosture.Standing
            && motor.ActiveTraversal == TopDown3DTraversalMove.None && motor.ClimbMode < TopDown3DClimbMode.Scramble
            && !IsGathering && campIndex < 0;
        private bool MovingTurnEligible => MovingTurnStandingContext && !motor.LaboratoryStandingFacingLocked;

        public void ConfigureLaboratoryMovingTurns(AnimationPlaygroundMovingTurnProfile profile)
        {
            if (playableGraph.IsValid() || visualInstance != null || !LaboratoryForwardReviewConfigured)
                throw new InvalidOperationException("Configure moving turns after the forward review and before visual initialization.");
            string reason = null;
            if (profile == null || !profile.Validate(laboratoryForwardProfile, out reason))
                throw new ArgumentException("Moving-turn calibration is unavailable or invalid: " + reason);
            movingTurnProfile = profile; ResetMovingTurns();
        }

        private void InitializeMovingTurnInputs()
        {
            if (movingTurnProfile == null) return;
            movingTurnBones = new Transform[movingTurnProfile.upperBodyBones.Length];
            movingTurnLastDeltas = new Quaternion[movingTurnBones.Length];
            for (var i = 0; i < movingTurnBones.Length; i++)
            {
                movingTurnBones[i] = humanoidAnimator.GetBoneTransform(movingTurnProfile.upperBodyBones[i]);
                if (movingTurnBones[i] == null) throw new InvalidOperationException("The paired moving-turn torso bone is unavailable.");
                movingTurnLastDeltas[i] = Quaternion.identity;
            }
        }

        private void ResetMovingTurns()
        {
            movingTurnDemand.Reset(); movingTurnPoseCurvature = LaboratoryMovingTurnWeight = 0;
            movingTurnPoseReady = false; Array.Clear(movingTurnWeights, 0, movingTurnWeights.Length);
            if (movingTurnProfile == null)
            { movingTurnBones = null; movingTurnLastDeltas = movingTurnSnapshotDeltas = null; movingTurnLastHasDelta = false; }
        }

        private void ClearMovingTurnDeltas()
        {
            movingTurnSnapshotDeltas = null; movingTurnLastHasDelta = false;
            if (movingTurnLastDeltas != null)
                for (var i = 0; i < movingTurnLastDeltas.Length; i++) movingTurnLastDeltas[i] = Quaternion.identity;
        }

        private void PrepareMovingTurnFrame()
        {
            movingTurnRenderSequence++; movingTurnPoseReady = false; movingTurnRenderDelta = Time.deltaTime;
            if (!MovingTurnEligible) ResetMovingTurns();
            if (!MovingTurnStandingContext && !PoseBlendOwned) ClearMovingTurnDeltas();
        }

        private float ApplyMovingTurnPose(AnimationPlaygroundGaitWeights weights, double cycles, float correction)
        {
            LaboratoryMovingTurnWeight = 0; Array.Clear(movingTurnWeights, 0, movingTurnWeights.Length);
            if (movingTurnProfile == null || movingTurnBones == null) return correction;
            var curvature = MovingTurnEligible && !jumpComposingUnderlay ? movingTurnPoseCurvature : 0;
            movingTurnPoseCycles = cycles; movingTurnRight = curvature > 0; movingTurnPoseReady = true;
            for (var i = 0; i < movingTurnProfile.bands.Length; i++)
            {
                var band = movingTurnProfile.bands[i];
                if (Mathf.Abs(curvature) <= .5f || weights[i + 1] <= 0) continue;
                var amount = band.maximumWeight * Mathf.SmoothStep(0, 1,
                    Mathf.InverseLerp(.5f, band.sourceCurvatureDegreesPerMetre, Mathf.Abs(curvature)));
                amount *= movingTurnProfile.RateWeight(i, cycles, forwardPoseEnvelope.Speed, movingTurnRight);
                movingTurnWeights[i] = weights[i + 1] * amount;
                LaboratoryMovingTurnWeight += movingTurnWeights[i];
            }
            // Root, hips, legs, height correction and the single-output graph
            // remain the accepted family. Only allowed local torso/arm rotations
            // are applied after the final supported pose has been evaluated.
            return correction;
        }

        private Quaternion[] CaptureMovingTurnSnapshot()
            => movingTurnLastHasDelta && movingTurnLastDeltas != null ? (Quaternion[])movingTurnLastDeltas.Clone() : null;
        private void RestoreMovingTurnSnapshot(Quaternion[] snapshot) => movingTurnSnapshotDeltas = snapshot;
        private void ReleaseMovingTurnSnapshot() => movingTurnSnapshotDeltas = null;

        private void ApplyMovingTurnBones()
        {
            if (movingTurnBones == null || movingTurnAppliedSequence == movingTurnRenderSequence) return;
            movingTurnAppliedSequence = movingTurnRenderSequence;
            var snapshotWeight = 0f;
            if (PoseBlendOwned && movingTurnSnapshotDeltas != null)
            {
                var elapsed = poseBlendAdvanceSources ? Math.Max(0, poseBlendPreviousElapsed
                    + (poseBlendElapsed - poseBlendPreviousElapsed) * JumpRenderAlpha()) : poseBlendElapsed;
                snapshotWeight = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((float)(elapsed / poseBlendDuration)));
            }
            var live = MovingTurnEligible && movingTurnPoseReady && LaboratoryMovingTurnWeight > .000001f;
            var release = MovingTurnStandingContext && ForwardReviewSupportAvailable()
                && movingTurnLastHasDelta && !live;
            var retain = Mathf.Exp(-Mathf.Max(0, movingTurnRenderDelta) / .08f);
            var any = false; var phase = (float)(movingTurnPoseCycles - Math.Floor(movingTurnPoseCycles));
            for (var bone = 0; bone < movingTurnBones.Length; bone++)
            {
                var basis = movingTurnBones[bone].localRotation; var result = basis;
                if (snapshotWeight > 0)
                    result = Quaternion.Slerp(Quaternion.identity, movingTurnSnapshotDeltas[bone], snapshotWeight) * basis;
                else if (live)
                {
                    var sum = new Vector4(basis.x, basis.y, basis.z, basis.w) * (1 - LaboratoryMovingTurnWeight);
                    for (var band = 0; band < movingTurnWeights.Length; band++)
                    {
                        var weight = movingTurnWeights[band]; if (weight <= 0) continue;
                        var data = movingTurnRight ? movingTurnProfile.bands[band].rightPose : movingTurnProfile.bands[band].leftPose;
                        var q = data.Sample(phase, bone, movingTurnBones.Length);
                        if (Quaternion.Dot(basis, q) < 0) weight = -weight;
                        sum += new Vector4(q.x, q.y, q.z, q.w) * weight;
                    }
                    result = new Quaternion(sum.x, sum.y, sum.z, sum.w).normalized;
                }
                else if (release)
                    result = Quaternion.Slerp(Quaternion.identity, movingTurnLastDeltas[bone], retain) * basis;
                var delta = result * Quaternion.Inverse(basis);
                if (Quaternion.Angle(delta, Quaternion.identity) > .01f) any = true;
                else if (!live && snapshotWeight <= 0) { delta = Quaternion.identity; result = basis; }
                movingTurnLastDeltas[bone] = delta; movingTurnBones[bone].localRotation = result;
            }
            movingTurnLastHasDelta = any;
        }
    }
}
