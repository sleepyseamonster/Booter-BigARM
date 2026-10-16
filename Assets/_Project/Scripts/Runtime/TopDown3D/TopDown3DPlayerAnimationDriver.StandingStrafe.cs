using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private const int StandingStrafeInputIndex = 25;
        private AnimationPlaygroundStandingStrafeProfile standingStrafeReview;
        private AnimationMixerPlayable standingStrafeMixer;
        private AnimationClipPlayable[] standingStrafeClips = new AnimationClipPlayable[3];
        private readonly float[] standingStrafeBaseline = new float[ClipCount];
        private AnimationPlaygroundGaitClock standingStrafeClock;
        private AnimationPlaygroundGroundedMotion standingStrafeMotion;
        private AnimationPlaygroundStandingTravel standingStrafeSide;
        private float standingStrafeSpeed, standingStrafePreviousSpeed, standingStrafeWeight;
        private bool standingStrafeOutputApplied, standingStrafeRendered;
        private bool standingStrafeMotionUnavailable;
        private double standingStrafeRenderedCycles;
        private double standingStrafeIdleSeconds;
        private struct StrafePlant { public double cycle, time; public TopDown3DFootSide side; public float speed; }
        private readonly StrafePlant[] standingStrafePlants = new StrafePlant[8];
        private int standingStrafePlantStart, standingStrafePlantCount;
        public bool LaboratoryStandingStrafeConfigured => standingStrafeReview != null;
        public bool LaboratoryStandingStrafeActive => standingStrafeRendered && standingStrafeOutputApplied;
        public string LaboratoryStandingStrafeStatus => !LaboratoryStandingStrafeConfigured ? null
            : standingStrafeReview.AllDirections ? "LT: lock facing and move in every direction. Side run 5 m/s / backward run 4 m/s. Look-ahead retained."
            : standingStrafeMotionUnavailable && motor != null && motor.LaboratoryStandingLateral ? "LT review: completed travel left the lateral envelope; pose paused."
            : motor != null && motor.LaboratoryStandingDirectionUnsupported ? "LT review: diagonal/backward travel unavailable; release LT to steer normally."
            : motor != null && motor.LaboratoryStandingFacingLocked ? "LT facing locked: forward or slow left/right (0.5 m/s). Release LT to steer normally."
            : (LaboratoryContinuationConfigured ? "Camp Loop" : "AP02") + ": hold LT for facing lock and slow left/right strafe. Look-ahead remains active.";

        public void ConfigureLaboratoryStandingStrafeReview(AnimationPlaygroundStandingStrafeProfile profile)
        {
            if (!LaboratoryForwardReviewConfigured || playableGraph.IsValid() || visualInstance != null || profile == null
                || profile.AllDirections && !laboratoryForwardProfile.ExpandedReview
                || !profile.Validate(humanoidPrefab, laboratoryFootProjection, out _))
                throw new ArgumentException("Configure the paired standing strafe review after AP02 forward review and before visual startup.");
            standingStrafeReview = profile;
        }

        public bool StandingStrafeReviewSupportAvailable() => LaboratoryStandingStrafeConfigured && LaboratoryForwardReady
            && ForwardReviewSupportAvailable() && (JumpPresentationOwned || !AnimationPresentationOwned && !PoseBlendOwned) && !CrouchPresentationOwned
            && motor != null && !motor.IsMounted && (!motor.IsActionConstrained || motor.JumpUsesGroundedLocomotion);

        private void InitializeStandingStrafeReviewInputs()
        {
            if (!LaboratoryStandingStrafeConfigured) return;
            if (standingStrafeReview.AllDirections) { InitializeDirectionalInputs(); return; }
            if (mixer.GetInputCount() <= StandingStrafeInputIndex) mixer.SetInputCount(StandingStrafeInputIndex + 1);
            standingStrafeMixer = AnimationMixerPlayable.Create(playableGraph, 3);
            var clips = new[] { laboratoryForwardProfile.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip,
                standingStrafeReview.left.sourceClip, standingStrafeReview.right.sourceClip };
            for (var i = 0; i < 3; i++)
            {
                var clip = AnimationClipPlayable.Create(playableGraph, clips[i]);
                clip.SetApplyFootIK(false); clip.SetApplyPlayableIK(false); clip.SetSpeed(0);
                playableGraph.Connect(clip, 0, standingStrafeMixer, i); standingStrafeClips[i] = clip;
            }
            playableGraph.Connect(standingStrafeMixer, 0, mixer, StandingStrafeInputIndex);
            mixer.SetInputWeight(StandingStrafeInputIndex, 0);
            ResetStandingStrafePresentation();
        }

        private void ResetStandingStrafePresentation()
        {
            standingStrafeClock = new AnimationPlaygroundGaitClock(); standingStrafeSide = AnimationPlaygroundStandingTravel.Idle;
            standingStrafeMotion = default; standingStrafePreviousSpeed = standingStrafeSpeed = standingStrafeWeight = 0;
            standingStrafePlantStart = standingStrafePlantCount = 0; standingStrafeRendered = false;
            standingStrafeMotionUnavailable = false;
            standingStrafeIdleSeconds = 0;
            ResetDirectionalPresentation();
        }

        private bool AdvanceStandingStrafeMotion(AnimationPlaygroundGroundedMotion sample)
        {
            if (!LaboratoryStandingStrafeConfigured || standingStrafeReview.AllDirections || !motor.LaboratoryStandingLateral) return false;
            var side = motor.LaboratoryStandingTravel;
            if (standingStrafeSide != side) { ResetStandingStrafePresentation(); standingStrafeSide = side; }
            var gait = side == AnimationPlaygroundStandingTravel.Left ? standingStrafeReview.left : standingStrafeReview.right;
            var lateral = Vector3.Cross(Vector3.up, motor.FacingDirection).normalized
                * (side == AnimationPlaygroundStandingTravel.Left ? -1 : 1);
            standingStrafeMotionUnavailable = sample.Available
                && (sample.Speed > Mathf.Min(standingStrafeReview.maximumSpeed + .02f, 1.35f * gait.metresPerCycle / gait.sourceClip.length)
                    || sample.Distance > .00001 && (Vector3.Dot(sample.TangentDisplacement, lateral) <= 0
                    || Mathf.Abs(Vector3.Dot(sample.TangentDisplacement, motor.FacingDirection))
                    > Vector3.Dot(sample.TangentDisplacement, lateral) * Mathf.Tan(AnimationPlaygroundStandingStrafeMath.AxisConeDegrees * Mathf.Deg2Rad)));
            if (standingStrafeMotionUnavailable)
                sample = new AnimationPlaygroundGroundedMotion(sample.Tick, sample.Epoch, sample.CompletedTime, sample.Duration,
                    Vector3.zero, sample.SupportNormal, false, false);
            var before = standingStrafeClock.Cycles;
            standingStrafePreviousSpeed = standingStrafeSpeed; standingStrafeMotion = sample;
            standingStrafeClock.Advance(sample, gait.metresPerCycle);
            standingStrafeSpeed = standingStrafeClock.Travelling && sample.Available ? sample.Speed : 0;
            if (sample.Rebased || !sample.Available)
            { standingStrafePlantStart = standingStrafePlantCount = 0; return true; }
            // Unequal source intervals remain unequal. Never emit the forward
            // clock's equal-quarter contacts for these lateral source takes.
            for (var loop = (long)Math.Floor(before); loop <= (long)Math.Floor(standingStrafeClock.Cycles); loop++)
            for (var contact = 0; contact < 4; contact++)
            {
                var cycle = loop + gait.nativeContacts[contact];
                if (cycle <= before || cycle > standingStrafeClock.Cycles) continue;
                if (standingStrafePlantCount == standingStrafePlants.Length)
                { standingStrafePlantStart = (standingStrafePlantStart + 1) % standingStrafePlants.Length; standingStrafePlantCount--; }
                var index = (standingStrafePlantStart + standingStrafePlantCount++) % standingStrafePlants.Length;
                standingStrafePlants[index] = new StrafePlant { cycle = cycle, time = sample.CompletedTime,
                    speed = sample.Speed, side = contact % 2 == 0 ? TopDown3DFootSide.Right : TopDown3DFootSide.Left };
            }
            return true;
        }

        private bool TryPresentStandingStrafe()
        {
            if (standingStrafeReview != null && standingStrafeReview.AllDirections) return TryPresentDirectional();
            if (!StandingStrafeReviewSupportAvailable() || !motor.LaboratoryStandingLateral || !playableGraph.IsValid()
                || motor.AnimationPosture != AnimationPlaygroundPosture.Standing || IsGathering || campIndex >= 0) return false;
            if (standingStrafeMotion.Epoch != motor.LaboratoryGroundedMotion.Epoch)
            { ResetStandingStrafePresentation(); standingStrafeSide = motor.LaboratoryStandingTravel; }
            var alpha = Mathf.Clamp01((float)((Time.timeAsDouble - Time.fixedTimeAsDouble) / Math.Max(.0001, Time.fixedDeltaTime)));
            var speed = Mathf.Lerp(standingStrafePreviousSpeed, standingStrafeSpeed, alpha);
            if (!motor.IsGrounded || Time.fixedTimeAsDouble - standingStrafeMotion.CompletedTime > .1) speed = 0;
            try { PresentStandingStrafeFrame(speed, standingStrafeClock.RenderCycles(alpha), Time.deltaTime); }
            catch(InvalidOperationException error) when(continuationReview!=null)
            {RejectContinuationPose(error.Message);return false;}
            return true;
        }

        private void PresentStandingStrafeFrame(float speed, double cycles, float deltaTime)
        {
            standingStrafeIdleSeconds += Math.Max(0, deltaTime);
            var gait = standingStrafeSide == AnimationPlaygroundStandingTravel.Left ? standingStrafeReview.left : standingStrafeReview.right;
            standingStrafeWeight = Mathf.MoveTowards(standingStrafeWeight, Mathf.Clamp01(speed / .3f), Mathf.Max(0, deltaTime) / .15f);
            for (var i = 0; i < ClipCount; i++) standingStrafeBaseline[i] = mixer.GetInputWeight(i);
            for (var i = 0; i < mixer.GetInputCount(); i++) mixer.SetInputWeight(i, i == StandingStrafeInputIndex ? 1 : 0);
            var phase = (float)(cycles - Math.Floor(cycles));
            for (var i = 0; i < 3; i++)
            {
                standingStrafeMixer.SetInputWeight(i, i == 0 ? 1 - standingStrafeWeight
                    : (i == 1) == (standingStrafeSide == AnimationPlaygroundStandingTravel.Left) ? standingStrafeWeight : 0);
                standingStrafeClips[i].SetTime(i == 0 ? standingStrafeIdleSeconds : phase * standingStrafeClips[i].GetAnimationClip().length);
            }
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            for (var i = 0; i < clipPlayables.Length; i++) if (clipPlayables[i].IsValid()) clipPlayables[i].SetSpeed(0);
            var idle = laboratoryForwardProfile.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
            var correction = gait.signedHeight.Evaluate(phase) * standingStrafeWeight
                + idle.HeightCorrection(Mathf.Repeat((float)(standingStrafeIdleSeconds / idle.Clip.length), 1)) * (1 - standingStrafeWeight);
            visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition + Vector3.up * correction, Quaternion.Euler(visualLocalEulerAngles));
            forwardTerrainApplied = continuationReview != null && forwardTerrainPose != null && motor != null
                && motor.TryGetLaboratorySupportPlane(out forwardTerrainPlane,true);
            if(forwardTerrainApplied)
            {
                forwardTerrainTick=motor.LaboratoryGroundedMotion.Tick;forwardTerrainEpoch=motor.LaboratoryGroundedMotion.Epoch;
                forwardTerrainPose.Configure(forwardTerrainPlane,transform.TransformPoint(Vector3.up*laboratoryFeetPlane).y);
            }
            playableGraph.Evaluate(0);
            if(forwardTerrainApplied)
            {
                var terrain=forwardTerrainPose.Sample;
                if(!float.IsFinite(terrain.requiredLowering) || terrain.requiredLowering>laboratoryForwardProfile.maximumTerrainBodyLowering+.00001f
                    || terrain.incompatibleReach>.001f)throw new InvalidOperationException("Standing strafe exceeded the terrain reach envelope.");
                correction+=Mathf.Max(0,-footProjectionSampler.MinimumDistance(forwardTerrainPlane))/forwardTerrainPlane.normal.y;
            }
            else correction += Mathf.Max(0, transform.TransformPoint(Vector3.up * laboratoryFeetPlane).y - footProjectionSampler.MinimumWorldY());
            if (!float.IsFinite(correction) || correction < -.03001f || correction > .08001f)
                throw new InvalidOperationException("Standing strafe exceeded the existing visual grounding envelope.");
            visualInstance.transform.localPosition = visualLocalPosition + Vector3.up * correction;
            standingStrafeRenderedCycles = cycles; standingStrafeOutputApplied = standingStrafeRendered = true;
            lastActionIndex = activeTransitionIndex = activeHardTurnIndex = -1;
            CurrentLocomotionState = speed > .1f ? TopDown3DLocomotionState.Sustained : TopDown3DLocomotionState.Stopping;
        }

        private void RestoreStandingStrafeBaseline()
        {
            standingStrafeRendered = false;
            if (!standingStrafeOutputApplied || !mixer.IsValid()) return;
            mixer.SetInputWeight(StandingStrafeInputIndex, 0);
            for (var i = 0; i < ClipCount; i++) mixer.SetInputWeight(i, standingStrafeBaseline[i]);
            standingStrafeOutputApplied = false;
        }

        private void EmitStandingStrafeFootEffects()
        {
            if (standingStrafeReview != null && standingStrafeReview.AllDirections) return;
            if (!standingStrafeRendered || !StandingStrafeReviewSupportAvailable() || motor == null) return;
            var sample = motor.LaboratoryGroundedMotion;
            if (!sample.Available || sample.Tick != standingStrafeMotion.Tick || sample.Epoch != standingStrafeMotion.Epoch)
            { standingStrafePlantStart = standingStrafePlantCount = 0; return; }
            var emitted = 0;
            while (standingStrafePlantCount > 0)
            {
                var plant = standingStrafePlants[standingStrafePlantStart];
                if (plant.cycle > standingStrafeRenderedCycles) break;
                standingStrafePlantStart = (standingStrafePlantStart + 1) % standingStrafePlants.Length; standingStrafePlantCount--;
                if (standingStrafeRenderedCycles - plant.cycle > .03 || Time.fixedTimeAsDouble - plant.time > .12
                    || emitted >= 2 || standingStrafeWeight < .5f || plant.speed < TopDown3DFootstepDust.MinimumMovementSpeed) continue;
                if (EmitFootContact(plant.side, plant.side == TopDown3DFootSide.Left ? leftFoot : rightFoot, plant.speed)) emitted++;
            }
        }
    }
}
