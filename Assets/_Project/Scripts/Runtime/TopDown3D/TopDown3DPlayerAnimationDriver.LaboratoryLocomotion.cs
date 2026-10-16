using System;
using UnityEngine;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        // Session-only diagnostic opt-in. It does not change the serialized twelve-role
        // profile or authorize the experimental family for the author's player.
        public bool LaboratorySustainedDiagnostic { get; private set; }

        public void SetLaboratorySustainedDiagnostic(bool enabled)
        {
            if (enabled && laboratoryAvatar == null)
                throw new InvalidOperationException("Sustained diagnostics require a configured laboratory character.");
            if (LaboratorySustainedDiagnostic == enabled) return;
            LaboratorySustainedDiagnostic = enabled;
            ResetTransitionState(TopDown3DLocomotionState.Idle);
            lastActionIndex = -1;
        }

        private AnimationPlaygroundForwardLocomotionProfile laboratoryForwardProfile;
        private AnimationPlaygroundGaitClock forwardClock;
        private bool forwardBound;
        private AnimationPlaygroundGroundedMotion forwardMotion;
        private float forwardPreviousSpeed, forwardSpeed;
        private struct PendingPlant { public long epoch, ordinal; public double time; public float speed; }
        private readonly PendingPlant[] forwardPlants = new PendingPlant[8];
        private int forwardPlantStart, forwardPlantCount;
        private double forwardIdleSeconds;
        private readonly AnimationPlaygroundForwardPoseEnvelope forwardPoseEnvelope = new();
        private bool forwardPoseRenderedThisFrame;
        private bool forwardFootEffectsProcessed;
        private AnimationPlaygroundTerrainPose forwardTerrainPose;
        private bool forwardTerrainApplied;
        private Plane forwardTerrainPlane;
        private long forwardTerrainTick, forwardTerrainEpoch;
        private bool ForwardTerrainPoseCurrent => forwardTerrainApplied && motor != null && (ContinuationActionTerrainCurrent
            || !AnimationPresentationOwned && !CrouchPresentationOwned && !PoseBlendOwned && !motor.IsMounted
            && motor.LaboratoryGroundedMotion.Tick == forwardTerrainTick
            && motor.LaboratoryGroundedMotion.Epoch == forwardTerrainEpoch
            && motor.TryGetLaboratorySupportPlane(out _, true));
        public float LaboratoryTerrainBodyLowering => forwardTerrainApplied ? forwardTerrainPose.Sample.lowering : 0;
        public int LaboratoryTerrainEvaluations => forwardTerrainPose?.Sample.evaluations ?? 0;
        public bool LaboratoryForwardReady { get; private set; }
        public string LaboratoryForwardFailure { get; private set; }
        public float LaboratoryForwardSpeed => forwardSpeed;
        public float LaboratoryForwardPoseSpeed => forwardPoseEnvelope.Speed;
        public double LaboratoryForwardCycles => forwardClock?.Cycles ?? 0;
        public long LaboratoryForwardContacts => forwardClock?.ContactCount ?? 0;
        public long LaboratoryDiscardedFootEffects { get; private set; }
        public double LaboratoryRenderedCycles { get; private set; }
        public float LaboratoryForwardLift { get; private set; }
        public long LaboratoryLastFootEffectOrdinal { get; private set; }
        public long LaboratoryLastFootEffectEpoch { get; private set; }

        private void InitializeForwardLocomotion()
        {
            if (laboratoryForwardProfile == null || !laboratoryForwardProfile.qualified && !LaboratoryForwardReviewConfigured) return;
            if (!laboratoryForwardProfile.Validate(humanoidPrefab, LaboratoryForwardReviewConfigured ? laboratoryForwardProfile.locomotion : locomotionProfile,
                out var reason, !LaboratoryForwardReviewConfigured))
            { LaboratoryForwardFailure = reason; return; }
            InitializeForwardReviewInputs();
            forwardClock = new AnimationPlaygroundGaitClock(laboratoryForwardProfile.enterSpeed, laboratoryForwardProfile.exitSpeed);
            if (laboratoryForwardProfile.terrainQualified || LaboratoryContinuationConfigured)
                forwardTerrainPose = new AnimationPlaygroundTerrainPose(humanoidAnimator, visualInstance.transform,
                    laboratoryForwardProfile.footProjection, playableGraph, laboratoryForwardProfile.maximumTerrainBodyLowering);
            LaboratoryForwardReady = true; BindForwardMotion();
        }
        private void OnEnable() => BindForwardMotion();
        private void BindForwardMotion()
        {
            if (!LaboratoryForwardReady || motor == null || forwardBound) return;
            motor.LaboratoryGroundedMotionCompleted += AdvanceForwardMotion; forwardBound = true;
            forwardClock.Reset(); forwardSpeed = forwardPreviousSpeed = 0;
            forwardPlantStart = forwardPlantCount = 0; forwardMotion = default;
        }
        private void UnbindForwardMotion()
        {
            ResetLaboratoryContinuationReview();
            RestoreStandingStrafeBaseline();
            ResetStandingStrafePresentation();
            RestoreForwardReviewBaseline();
            if (forwardBound && motor != null) motor.LaboratoryGroundedMotionCompleted -= AdvanceForwardMotion;
            forwardBound = false; forwardClock?.Reset(); forwardSpeed = forwardPreviousSpeed = 0;
            forwardPoseRenderedThisFrame = false;
            forwardTerrainApplied = false; forwardTerrainPose?.Disable();
            forwardPoseEnvelope.Reset();
            forwardFootEffectsProcessed = false;
            forwardPlantStart = forwardPlantCount = 0;
            if (playableGraph.IsValid()) playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        }
        private void AdvanceForwardMotion(AnimationPlaygroundGroundedMotion sample)
        {
            if (!LaboratoryForwardReady || !isActiveAndEnabled) return;
            if(JumpPresentationOwned && !motor.JumpUsesGroundedLocomotion)
            {LaboratoryDiscardedFootEffects+=forwardPlantCount;forwardPlantCount=forwardPlantStart=0;return;}
            if (!ForwardReviewSupportAvailable())
            {
                if (LaboratoryStandingStrafeConfigured) ResetStandingStrafePresentation();
                forwardClock.Reset(); forwardSpeed = forwardPreviousSpeed = 0;
                LaboratoryDiscardedFootEffects += forwardPlantCount; forwardPlantCount = forwardPlantStart = 0;
                forwardPoseEnvelope.Reset(); return;
            }
            forwardMotion = sample;
            if (AdvanceStandingStrafeMotion(sample))
            {
                forwardClock.Reset(); forwardSpeed = forwardPreviousSpeed = 0;
                forwardPlantStart = forwardPlantCount = 0; forwardPoseEnvelope.Reset(); return;
            }
            forwardPreviousSpeed = forwardSpeed;
            var directional = ObserveDirectionalMotion(sample);
            var contacts = directional ? forwardClock.Advance(sample, DirectionalCycleDistance(sample.Speed))
                : forwardClock.Advance(sample, laboratoryForwardProfile);
            forwardSpeed = forwardClock.Travelling && sample.Available ? sample.Speed : 0;
            if (sample.Rebased || !sample.Available)
            { LaboratoryDiscardedFootEffects += forwardPlantCount; forwardPlantCount = 0; forwardPlantStart = 0; return; }
            var keep = (int)Math.Min(contacts.Count, forwardPlants.Length);
            LaboratoryDiscardedFootEffects += contacts.Count - keep;
            for (var i = 0; i < keep; i++)
            {
                if (forwardPlantCount == forwardPlants.Length)
                {
                    forwardPlantStart = (forwardPlantStart + 1) % forwardPlants.Length;
                    forwardPlantCount--; LaboratoryDiscardedFootEffects++;
                }
                var index = (forwardPlantStart + forwardPlantCount++) % forwardPlants.Length;
                forwardPlants[index] = new PendingPlant { epoch = contacts.Epoch,
                    ordinal = contacts.FirstOrdinal + contacts.Count - keep + i, time = sample.CompletedTime, speed = sample.Speed };
            }
        }

        private void PrepareForwardGraphUpdate()
        {
            if (!JumpPresentationOwned && (AnimationPresentationOwned || CrouchPresentationOwned || PoseBlendOwned || motor != null && motor.IsActionConstrained))
                ResetDirectionalPresentation();
            continuationActionTerrainApplied=false;
            RestoreStandingStrafeBaseline();
            RestoreForwardReviewBaseline();
            forwardTerrainApplied = false; forwardTerrainPose?.Disable();
            if (!forwardPoseRenderedThisFrame) { forwardIdleSeconds = 0; forwardPoseEnvelope.Reset(); }
            forwardPoseRenderedThisFrame = false;
            forwardFootEffectsProcessed = false;
            // Other presentation owners keep their established automatic-time path.
            // The standing branch switches to Manual before setting/evaluating its pose.
            if (LaboratoryForwardReady && playableGraph.IsValid())
            {
                playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                if (clipPlayables[IdleIndex].IsValid()) clipPlayables[IdleIndex].SetSpeed(1);
            }
        }

        private bool TryPresentForwardLocomotion()
        {
            if (TryPresentStandingStrafe()) return true;
            if (!LaboratoryForwardReady || !ForwardReviewSupportAvailable() || !playableGraph.IsValid() || motor == null || visualInstance == null
                || motor.IsActionConstrained || motor.AnimationPosture != AnimationPlaygroundPosture.Standing
                || motor.ActiveTraversal != TopDown3DTraversalMove.None || motor.ClimbMode >= TopDown3DClimbMode.Scramble
                || IsGathering || campIndex >= 0) return false;
            if (motor.LaboratoryGroundedMotion.Epoch != forwardMotion.Epoch)
            {
                forwardSpeed = forwardPreviousSpeed = 0; forwardPlantCount = 0;
                forwardClock.Reset(); forwardPoseEnvelope.Reset();
            }
            var alpha = Mathf.Clamp01((float)((Time.timeAsDouble - Time.fixedTimeAsDouble) / Math.Max(.0001, Time.fixedDeltaTime)));
            var speed = Mathf.Lerp(forwardPreviousSpeed, forwardSpeed, alpha);
            if (!motor.IsGrounded || Time.fixedTimeAsDouble - forwardMotion.CompletedTime > .1) speed = 0;
            var cycles = forwardClock.RenderCycles(alpha, forwardSpeed, laboratoryForwardProfile);
            try { PresentForwardFrame(speed, cycles, Time.deltaTime); }
            catch(InvalidOperationException error) when(continuationReview!=null)
            {RejectContinuationPose(error.Message);return false;}
            CurrentLocomotionState = forwardPoseEnvelope.Settling ? TopDown3DLocomotionState.Stopping
                : forwardPoseEnvelope.Resuming ? TopDown3DLocomotionState.Starting
                : speed > 0 ? TopDown3DLocomotionState.Sustained : TopDown3DLocomotionState.Idle;
            return true;
        }

        private void PresentForwardFrame(float speed, double cycles, float deltaTime)
        {
            forwardIdleSeconds += deltaTime;
            var weights = forwardPoseEnvelope.SampleGaitWeights(laboratoryForwardProfile.PoseWeights(speed), speed, deltaTime);
            PresentForwardGaitPose(weights, cycles, forwardIdleSeconds);
            forwardPoseRenderedThisFrame = true;
        }

        private void EmitForwardFootEffectsForRenderedPose()
        {
            if (forwardFootEffectsProcessed || !forwardPoseRenderedThisFrame || !LaboratoryForwardReady || !ForwardReviewSupportAvailable() || motor == null
                || AnimationPresentationOwned || CrouchPresentationOwned || PoseBlendOwned || motor.IsMounted
                || motor.IsActionConstrained) return;
            // Effects sample the displayed pose after frame interpolation, rather
            // than reading last frame's foot transforms during simulation.
            forwardFootEffectsProcessed = true;
            var current = motor.LaboratoryGroundedMotion;
            if (!current.Available || current.Epoch != forwardMotion.Epoch || current.Tick != forwardMotion.Tick)
            {
                LaboratoryDiscardedFootEffects += forwardPlantCount;
                forwardPlantStart = forwardPlantCount = 0;
                return;
            }
            EmitForwardRenderedPlants(LaboratoryRenderedCycles, Time.fixedTimeAsDouble);
        }

        // Same pose path is used by guarded offline qualification, without running a motor.
        private void PresentForwardLocomotionPose(float speed, double cycles, double idleSeconds)
            => PresentForwardGaitPose(laboratoryForwardProfile.PoseWeights(speed), cycles, idleSeconds);

        private void PresentForwardWeightedPose(Vector4 weights, double cycles, double idleSeconds)
            => PresentForwardGaitPose(new AnimationPlaygroundGaitWeights(weights), cycles, idleSeconds);

        private void PresentForwardGaitPose(AnimationPlaygroundGaitWeights weights, double cycles, double idleSeconds)
        {
            var policy = laboratoryForwardProfile;
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            SetForwardPoseWeights(weights);
            for (var i = 0; i < clipPlayables.Length; i++) if (clipPlayables[i].IsValid()) clipPlayables[i].SetSpeed(0);
            var idleEntry = LaboratoryForwardReviewConfigured ? policy.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle) : locomotionEntries[IdleIndex];
            ForwardClip(IdleIndex).SetTime(idleSeconds);
            var correction = idleEntry.HeightCorrection(
                Mathf.Repeat((float)(idleSeconds / idleEntry.Clip.length), 1)) * weights[0];
            for (var i = 0; i < policy.MovingGaitCount; i++)
            {
                ForwardClip(i + 1).SetTime(policy.gaits[i].SourceSeconds(cycles));
                correction += policy.gaits[i].Height(cycles) * weights[i + 1];
            }
            visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition + Vector3.up * correction,
                Quaternion.Euler(visualLocalEulerAngles));
            if(jumpComposingUnderlay){LaboratoryForwardLift=correction;LaboratoryRenderedCycles=cycles;return;}
            var plane = transform.TransformPoint(Vector3.up * laboratoryFeetPlane).y;
            if (TryEvaluateNativeCampGround(correction, out var nativeCorrection))
            {
                visualInstance.transform.localPosition = visualLocalPosition + Vector3.up * nativeCorrection;
                LaboratoryForwardLift = nativeCorrection; LaboratoryRenderedCycles = cycles;
                lastActionIndex = activeTransitionIndex = activeHardTurnIndex = -1;
                return;
            }
            forwardTerrainApplied = forwardTerrainPose != null && motor != null
                && motor.TryGetLaboratorySupportPlane(out forwardTerrainPlane, true);
            if (forwardTerrainApplied)
            {
                forwardTerrainTick = motor.LaboratoryGroundedMotion.Tick;
                forwardTerrainEpoch = motor.LaboratoryGroundedMotion.Epoch;
                forwardTerrainPose.Configure(forwardTerrainPlane, plane);
            }
            else forwardTerrainPose?.Disable();
            // Evaluate exactly once at the supplied timestamp; moving the visual host
            // afterward updates the same cached bone transforms without advancing time.
            playableGraph.Evaluate(0);
            if (forwardTerrainApplied)
            {
                var sample = forwardTerrainPose.Sample;
                if (!float.IsFinite(sample.requiredLowering) || sample.requiredLowering > policy.maximumTerrainBodyLowering + .00001f
                    || sample.incompatibleReach > .001f || correction + sample.flatLift > policy.maximumLift + .00001f)
                    throw new InvalidOperationException("The terrain pose exceeds its qualified reach/body envelope.");
                correction += Mathf.Max(0, -footProjectionSampler.MinimumDistance(forwardTerrainPlane)) / forwardTerrainPlane.normal.y;
            }
            else correction += Mathf.Max(0, plane - footProjectionSampler.MinimumWorldY());
            if (!float.IsFinite(correction) || correction < -policy.maximumLowering - .00001f || correction > policy.maximumLift + .00001f)
                throw new InvalidOperationException("The forward pose exceeds its qualified visual grounding envelope.");
            visualInstance.transform.localPosition = visualLocalPosition + Vector3.up * correction;
            LaboratoryForwardLift = correction; LaboratoryRenderedCycles = cycles;
            lastActionIndex = -1; activeTransitionIndex = -1; activeHardTurnIndex = -1;
        }
        private void EmitForwardRenderedPlants(double cycles, double completedNow)
        {
            var emitted = 0;
            while (forwardPlantCount > 0)
            {
                var plant = forwardPlants[forwardPlantStart];
                var contactCycle = plant.ordinal * .25;
                if (cycles < contactCycle) break; // This contact has not reached the displayed pose yet.
                forwardPlantStart = (forwardPlantStart + 1) % forwardPlants.Length; forwardPlantCount--;
                if (cycles - contactCycle > .03 || completedNow - plant.time > .12 || emitted >= 2
                    || !motor.IsGrounded || forwardSpeed < TopDown3DFootstepDust.MinimumMovementSpeed)
                { LaboratoryDiscardedFootEffects++; continue; }
                var side = AnimationPlaygroundGaitContacts.Side(plant.ordinal);
                if (EmitFootContact(side, side == TopDown3DFootSide.Left ? leftFoot : rightFoot, plant.speed))
                {
                    LaboratoryLastFootEffectEpoch = plant.epoch; LaboratoryLastFootEffectOrdinal = plant.ordinal;
                    emitted++;
                }
            }
        }
    }
}
