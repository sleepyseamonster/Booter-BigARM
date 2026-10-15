using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        private AnimationPlaygroundStandingStrafeProfile standingStrafeProfile;
        private Func<bool> standingStrafeSupport;
        private Vector3 standingStrafeFacing;
        private bool standingStrafeLocked, standingStrafeAwaitRelease;
        public AnimationPlaygroundStandingTravel LaboratoryStandingTravel { get; private set; }
        public bool LaboratoryStandingFacingLocked => standingStrafeLocked;
        public bool LaboratoryStandingLateral => standingStrafeLocked
            && (LaboratoryStandingTravel == AnimationPlaygroundStandingTravel.Left || LaboratoryStandingTravel == AnimationPlaygroundStandingTravel.Right);
        public bool LaboratoryStandingDirectionUnsupported { get; private set; }

        public void ConfigureLaboratoryStandingStrafe(object owner, AnimationPlaygroundStandingStrafeProfile profile,
            Func<bool> supportAvailable)
        {
            if (!IsAnimationPostureOwner(owner) || profile == null || supportAvailable == null
                || !profile.Validate(profile.targetModel, profile.footProjection, out _))
                throw new ArgumentException("Configure standing strafe through the laboratory posture owner and paired review.");
            standingStrafeProfile = profile; standingStrafeSupport = supportAvailable;
            ResetStandingStrafe(true);
        }

        private void ResetStandingStrafe(bool awaitRelease)
        {
            standingStrafeLocked = false; standingStrafeAwaitRelease = awaitRelease;
            LaboratoryStandingTravel = AnimationPlaygroundStandingTravel.Idle;
            LaboratoryStandingDirectionUnsupported = false;
        }
        private void ReleaseStandingStrafeReview()
        {
            ResetStandingStrafe(true); standingStrafeProfile = null; standingStrafeSupport = null;
        }

        // Early action/mount returns must also drop this transient facing lease.
        private void GuardStandingStrafeOwner()
        {
            if (standingStrafeProfile == null) return;
            if (animationPostureOwner == null || IsMounted || IsActionConstrained || laboratoryPostureSuspended
                || AnimationPosture != AnimationPlaygroundPosture.Standing || input == null
                || input.Mode != TopDown3DInputMode.Gameplay || input.LaboratoryResetBlocked
                || input.LaboratoryStandingFacingDisarmed
                || Application.isPlaying && !Application.isFocused)
                ResetStandingStrafe(true);
        }

        private void ApplyStandingStrafeIntent(ref Vector3 direction)
        {
            if (standingStrafeProfile == null) return;
            var held = input != null && input.LaboratoryStandingFacingHeld;
            if (!held) standingStrafeAwaitRelease = false;
            if (animationPostureOwner == null || IsActionConstrained || laboratoryPostureSuspended
                || AnimationPosture != AnimationPlaygroundPosture.Standing || !IsGrounded
                || standingStrafeSupport?.Invoke() != true)
            { ResetStandingStrafe(held); return; }
            var speed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
            if (!standingStrafeLocked)
            {
                if (!held || standingStrafeAwaitRelease) return;
                // Capture the completed physics orientation only after braking.
                if (speed > AnimationPlaygroundStandingStrafeMath.StoppedSpeed) { direction = Vector3.zero; return; }
                standingStrafeFacing = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, Vector3.up).normalized;
                standingStrafeLocked = true; ResetStandingMotionObservation();
            }
            if (!held && !LaboratoryStandingLateral)
            { ResetStandingStrafe(false); return; }
            var requested = held ? AnimationPlaygroundStandingStrafeMath.Classify(direction, standingStrafeFacing, out _) : AnimationPlaygroundStandingTravel.Idle;
            LaboratoryStandingDirectionUnsupported = requested == AnimationPlaygroundStandingTravel.Unsupported;
            if (!held && speed <= AnimationPlaygroundStandingStrafeMath.StoppedSpeed)
            { ResetStandingStrafe(false); ResetStandingMotionObservation(); return; }
            if (requested == AnimationPlaygroundStandingTravel.Unsupported || requested == AnimationPlaygroundStandingTravel.Idle
                || requested != LaboratoryStandingTravel && speed > AnimationPlaygroundStandingStrafeMath.StoppedSpeed)
            { direction = Vector3.zero; return; }
            if (requested != LaboratoryStandingTravel)
            { LaboratoryStandingTravel = requested; ResetStandingMotionObservation(); }
            AnimationPlaygroundStandingStrafeMath.Classify(direction, standingStrafeFacing, out direction);
        }

        private float StandingStrafeRequestedSpeed(float ordinary) => LaboratoryStandingLateral
            ? Mathf.Min(ordinary, standingStrafeProfile.maximumSpeed) : ordinary;
    }
}
