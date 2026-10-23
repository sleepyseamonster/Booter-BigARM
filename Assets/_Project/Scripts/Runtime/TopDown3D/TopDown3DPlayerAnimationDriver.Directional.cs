using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private int DirectionalChannels => standingStrafeReview != null ? standingStrafeReview.DirectionalChannelCount : 12;
        private float[] directionalWeights = new float[12], directionalTarget = new float[12];
        private Vector2 directionalLocal = Vector2.up;
        private bool directionalInitialized;
        public bool LaboratoryDirectionalConfigured => standingStrafeReview != null && standingStrafeReview.AllDirections;

        private void InitializeDirectionalInputs()
        {
            if (directionalWeights.Length != DirectionalChannels)
            { directionalWeights = new float[DirectionalChannels]; directionalTarget = new float[DirectionalChannels]; }
            if (mixer.GetInputCount() <= StandingStrafeInputIndex) mixer.SetInputCount(StandingStrafeInputIndex + 1);
            standingStrafeMixer = AnimationMixerPlayable.Create(playableGraph, DirectionalChannels);
            standingStrafeClips = new AnimationClipPlayable[DirectionalChannels];
            for (var i = 0; i < DirectionalChannels; i++)
            {
                var clip = i == 0 ? laboratoryForwardProfile.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip
                    : i <= 5 ? laboratoryForwardProfile.gaits[i-1].sourceClip : standingStrafeReview.directionalGaits[i-6].sourceClip;
                var playable = AnimationClipPlayable.Create(playableGraph, clip);
                playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); playable.SetSpeed(0);
                playableGraph.Connect(playable, 0, standingStrafeMixer, i); standingStrafeClips[i] = playable;
            }
            playableGraph.Connect(standingStrafeMixer, 0, mixer, StandingStrafeInputIndex); mixer.SetInputWeight(StandingStrafeInputIndex, 0);
            ResetStandingStrafePresentation();
        }

        private void ResetDirectionalPresentation()
        {
            Array.Clear(directionalWeights, 0, directionalWeights.Length); directionalWeights[0] = 1;
            directionalLocal = Vector2.up; directionalInitialized = false;
        }

        private bool ObserveDirectionalMotion(AnimationPlaygroundGroundedMotion sample)
        {
            if (!LaboratoryDirectionalConfigured) return false;
            if (!motor.LaboratoryStandingFacingLocked) { directionalLocal = Vector2.up; return false; }
            if (sample.Available && sample.TangentDisplacement.sqrMagnitude > .00000001f)
                directionalLocal = AnimationPlaygroundDirectionalMath.Local(sample.TangentDisplacement, motor.FacingDirection).normalized;
            return AnimationPlaygroundDirectionalMath.Weights(directionalLocal).x < .999999f;
        }

        private double DirectionalCycleDistance(float speed)
        {
            AnimationPlaygroundDirectionalMath.FillPoseWeights(standingStrafeReview, laboratoryForwardProfile, speed, directionalLocal, directionalTarget);
            var distance = 0d;
            for (var i = 1; i < DirectionalChannels; i++) distance += directionalTarget[i] * (i <= 5
                ? laboratoryForwardProfile.gaits[i-1].metresPerCycle : standingStrafeReview.directionalGaits[i-6].metresPerCycle);
            return Math.Max(.01, distance);
        }

        private bool TryPresentDirectional()
        {
            if (!StandingStrafeReviewSupportAvailable() || !playableGraph.IsValid() || visualInstance == null
                || motor.AnimationPosture != AnimationPlaygroundPosture.Standing || IsGathering || campIndex >= 0) return false;
            var remaining = 0f; for (var i = 6; i < DirectionalChannels; i++) remaining += directionalWeights[i];
            if (remaining < .0001f && AnimationPlaygroundDirectionalMath.Weights(directionalLocal).x > .999999f)
            { directionalInitialized = false; return false; }
            if (!motor.LaboratoryStandingFacingLocked && (!directionalInitialized || remaining < .0001f))
            { directionalInitialized = false; return false; }
            if (motor.LaboratoryGroundedMotion.Epoch != forwardMotion.Epoch)
            { forwardClock.Reset(); forwardSpeed = forwardPreviousSpeed = 0; forwardPlantCount = forwardPlantStart = 0; }
            var alpha = Mathf.Clamp01((float)((Time.timeAsDouble - Time.fixedTimeAsDouble) / Math.Max(.0001, Time.fixedDeltaTime)));
            var speed = Mathf.Lerp(forwardPreviousSpeed, forwardSpeed, alpha);
            if (!motor.IsGrounded || Time.fixedTimeAsDouble - forwardMotion.CompletedTime > .1) speed = 0;
            var cycles = forwardClock.RenderCycles(alpha);
            // Pure forward retains its accepted distance map, including the jog plateau.
            if (AnimationPlaygroundDirectionalMath.Weights(directionalLocal).x > .999999f)
                cycles = forwardClock.RenderCycles(alpha, forwardSpeed, laboratoryForwardProfile);
            try { PresentDirectionalFrame(speed, cycles, Time.deltaTime); }
            catch (InvalidOperationException error) when (continuationReview != null)
            { RejectContinuationPose(error.Message); return false; }
            return true;
        }

        private void PresentDirectionalFrame(float speed, double cycles, float deltaTime)
        {
            forwardIdleSeconds += Math.Max(0, deltaTime);
            forwardPoseEnvelope.SampleGaitWeights(laboratoryForwardProfile.PoseWeights(speed), speed, deltaTime);
            if (!directionalInitialized)
            {
                var baseline = laboratoryForwardProfile.PoseWeights(speed);
                for (var i = 0; i < DirectionalChannels; i++) directionalWeights[i] = i < 6 ? baseline[i] : 0;
                directionalInitialized = true;
            }
            AnimationPlaygroundDirectionalMath.FillPoseWeights(standingStrafeReview, laboratoryForwardProfile,
                speed, motor.LaboratoryStandingFacingLocked ? directionalLocal : Vector2.up, directionalTarget);
            var blend = 1 - Mathf.Exp(-Mathf.Max(0, deltaTime) / .06f);
            var total = 0f;
            for (var i = 0; i < DirectionalChannels; i++)
            { directionalWeights[i] = Mathf.Lerp(directionalWeights[i], directionalTarget[i], blend); total += directionalWeights[i]; }
            for (var i = 0; i < DirectionalChannels; i++) directionalWeights[i] /= total;
            for (var i = 0; i < ClipCount; i++) standingStrafeBaseline[i] = mixer.GetInputWeight(i);
            for (var i = 0; i < mixer.GetInputCount(); i++) mixer.SetInputWeight(i, i == StandingStrafeInputIndex ? 1 : 0);
            var idle = laboratoryForwardProfile.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
            var correction = 0f;
            for (var i = 0; i < DirectionalChannels; i++)
            {
                var gait = i == 0 ? null : i <= 5 ? laboratoryForwardProfile.gaits[i-1] : standingStrafeReview.directionalGaits[i-6];
                standingStrafeMixer.SetInputWeight(i, directionalWeights[i]);
                standingStrafeClips[i].SetTime(i == 0 ? forwardIdleSeconds : gait.SourceSeconds(cycles));
                correction += directionalWeights[i] * (i == 0 ? idle.HeightCorrection(Mathf.Repeat((float)(forwardIdleSeconds / idle.Clip.length), 1)) : gait.Height(cycles));
            }
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            for (var i = 0; i < clipPlayables.Length; i++) if (clipPlayables[i].IsValid()) clipPlayables[i].SetSpeed(0);
            visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition + Vector3.up * correction, Quaternion.Euler(visualLocalEulerAngles));
            if(jumpComposingUnderlay)
            {LaboratoryForwardLift=correction;LaboratoryRenderedCycles=cycles;standingStrafeRendered=standingStrafeOutputApplied=forwardPoseRenderedThisFrame=true;return;}
            if (!TryEvaluateNativeCampGround(correction, out correction, true))
            {
                forwardTerrainApplied = forwardTerrainPose != null && motor.TryGetLaboratorySupportPlane(out forwardTerrainPlane, true);
                if (forwardTerrainApplied)
                {
                    forwardTerrainTick = motor.LaboratoryGroundedMotion.Tick; forwardTerrainEpoch = motor.LaboratoryGroundedMotion.Epoch;
                    forwardTerrainPose.Configure(forwardTerrainPlane, transform.TransformPoint(Vector3.up * laboratoryFeetPlane).y);
                }
                playableGraph.Evaluate(0);
                if (forwardTerrainApplied)
                {
                    var terrain = forwardTerrainPose.Sample;
                    if (!float.IsFinite(terrain.requiredLowering) || terrain.requiredLowering > laboratoryForwardProfile.maximumTerrainBodyLowering + .00001f
                        || terrain.incompatibleReach > .001f) throw new InvalidOperationException("Directional pose exceeded the terrain reach envelope.");
                    correction += Mathf.Max(0, -footProjectionSampler.MinimumDistance(forwardTerrainPlane)) / forwardTerrainPlane.normal.y;
                }
                else correction += Mathf.Max(0, transform.TransformPoint(Vector3.up * laboratoryFeetPlane).y - footProjectionSampler.MinimumWorldY());
            }
            if (!float.IsFinite(correction) || correction < -.03001f || correction > .08001f)
                throw new InvalidOperationException($"Directional pose exceeded the visual grounding envelope ({correction:R}m, speed {speed:R}, cycle {cycles:R}, local {directionalLocal}).");
            visualInstance.transform.localPosition = visualLocalPosition + Vector3.up * correction;
            LaboratoryForwardLift = correction; LaboratoryRenderedCycles = cycles;
            standingStrafeRendered = standingStrafeOutputApplied = forwardPoseRenderedThisFrame = true;
            lastActionIndex = activeTransitionIndex = activeHardTurnIndex = -1;
            CurrentLocomotionState = speed > .1f ? TopDown3DLocomotionState.Sustained : TopDown3DLocomotionState.Stopping;
        }
    }
}
