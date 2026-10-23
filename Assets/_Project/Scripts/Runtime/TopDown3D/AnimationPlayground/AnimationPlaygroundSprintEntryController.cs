using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundSprintEntryController : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundCrouchController crouch;
        private AnimationPlaygroundSprintEntryProfile profile;
        private AnimationPlaygroundSprintEntry entry;
        private AnimationPlaygroundResetRegistry registry;
        private Func<bool> paused, otherBusy;
        private Action<string> reportRejection;
        private Vector3 heading, entryFacing;
        private float previousTime, blendTime;
        private double previousSeconds;
        private bool entering, blending, lastDemand, disarmed, disposed, requestedSpeedLimited;
        public bool Active => entering || blending;
        public bool Ready => profile != null && profile.Validate(profile.targetModel, out _);
        public string LastFailure { get; private set; }
        public string ResetId => "CampLoop.Booter.SprintEntry";
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.Actor;

        public void Configure(TopDown3DPlayerMotor value, TopDown3DPlayerAnimationDriver presentation,
            AnimationPlaygroundActionCoordinator actions, AnimationPlaygroundCrouchController posture,
            AnimationPlaygroundSprintEntryProfile calibration, AnimationPlaygroundResetRegistry resets,
            Func<bool> shouldPause, Func<bool> activityBusy, Action<string> rejectionFeedback = null)
        {
            if (motor != null || value == null || presentation == null || actions == null || resets == null
                || calibration == null || !calibration.Validate(calibration.targetModel, out _))
                throw new ArgumentException("Configure one qualified sprint entry family on the existing player.");
            motor = value; driver = presentation; coordinator = actions; crouch = posture;
            profile = calibration; registry = resets; paused = shouldPause; otherBusy = activityBusy; reportRejection = rejectionFeedback;
            registry.Register(this); coordinator.ActionCompleted += StageCompleted;
            motor.AnimationMotionPreparing += GuardMotion; motor.AnimationLocomotionAdvanced += ObserveLocomotion;
            motor.AnimationMotionAdvanced += AfterMotion;
            disarmed = motor.InputRouter != null && motor.InputRouter.SprintHeld;
        }

        private bool HasDemand => motor != null && motor.InputRouter != null && motor.InputRouter.SprintHeld
            && motor.InputRouter.Mode == TopDown3DInputMode.Gameplay && !motor.InputRouter.LaboratoryResetBlocked
            && !motor.InputRouter.AnimationSlowWalkHeld
            && motor.LaboratoryMoveDirection.sqrMagnitude >= .81f;

        private void Update()
        {
            if (disposed || motor == null) return;
            var input = motor.InputRouter;
            if (input == null || paused?.Invoke() == true)
            { if (entering) Finish(false); disarmed = true; lastDemand = true; return; }
            if (!input.SprintHeld) disarmed = false;
            var demand = HasDemand;
            var requested = demand && !lastDemand && !disarmed;
            lastDemand = demand;
            if (requested && !Active && !TrySprintEntry()) reportRejection?.Invoke(LastFailure);
        }

        public bool TrySprintEntry()
        {
            if (disposed || !isActiveAndEnabled || !Ready || Active || !HasDemand || paused?.Invoke() == true
                || otherBusy?.Invoke() == true || coordinator.IsBusy || !motor.IsGrounded || motor.IsMounted)
                return Fail("Sprint entry requires clear grounded movement.");
            var vitals = motor.GetComponent<TopDown3DSurvivalVitals>();
            if (vitals != null && vitals.Health <= 0) return Fail("Sprint entry is unavailable.");
            var fromCrouch = crouch != null && crouch.State == AnimationPlaygroundCrouchState.Crouched;
            if (crouch != null && crouch.Active && !fromCrouch || !fromCrouch && Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).magnitude > .18f)
                return Fail("A sustained gait or posture transition already owns this movement.");
            heading = Vector3.ProjectOnPlane(motor.LaboratoryMoveDirection, Vector3.up).normalized;
            var facingHeld=motor.LaboratoryStandingFacingLocked || fromCrouch && motor.InputRouter.LaboratoryStandingFacingHeld;
            if (facingHeld && Vector3.Dot(heading, motor.FacingDirection) < .98f)
                return Fail("Lateral travel retains its directional gait.");
            entryFacing = facingHeld ? motor.FacingDirection : heading;
            entry = fromCrouch ? profile.crouched : profile.idle;
            if (fromCrouch ? !crouch.TryReleaseForSprint(this, profile.entryBlend) : !driver.TryCapturePoseBlend(this, profile.entryBlend))
                return Fail("Sprint entry needs standing clearance and an available outgoing pose.");
            var binding = new AnimationPlaygroundMotionBinding { sourceId = fromCrouch ? "QualifiedSprintEntry.Crouched" : "QualifiedSprintEntry.Idle",
                clip = entry.clip, readiness = AnimationPlaygroundMotionReadiness.PhysicalQualified };
            // Reuse the existing grounded, swept, momentum-preserving action lease.
            // Its historical DodgeLocomotion name does not add a second movement writer.
            if (!coordinator.TryBeginPhysicalSequence(this, binding, AnimationPlaygroundMotionMode.AlignedAction,
                null, Intent, null, seconds=>entry.SourceSeconds(seconds),looping:true,
                policy: AnimationPlaygroundMotionPolicy.DodgeLocomotion))
            {
                driver.EndPoseBlend(this); if (fromCrouch) crouch.TryResumeAfterSprintFailure();
                return Fail("Sprint entry could not acquire its movement and pose.");
            }
            entering = true; lastDemand = true; requestedSpeedLimited = false; previousTime = 0; previousSeconds = 0; LastFailure = null;
            if (!driver.SetAnimationBodyProjection(coordinator, profile.bodyProjection)
                || !driver.SetAnimationDodgeProjection(coordinator, heading)
                || !driver.SetAnimationGarmentFloor(coordinator, true, .6f, .01f))
            { Cancel(); return Fail("Sprint entry projection is unavailable."); }
            return true;
        }

        private AnimationPlaygroundMotorIntent Intent(double seconds)
        {
            var time = entry.SourceSeconds(seconds);
            var sourceSpeed = entry.ResponseSpeed(previousSeconds, seconds);
            var target = motor.DodgeRunOutVelocity(coordinator);
            // Uphill, analog and walk modifiers remain the motor's authority.
            // If they cannot support the source's travel, return to ordinary gaits.
            requestedSpeedLimited = target.magnitude + .01f < sourceSpeed;
            var speed = Mathf.Min(profile.maximumSpeed, Mathf.Min(sourceSpeed, target.magnitude));
            // The complete take's measured travel drives both pose and motion.
            // Ordinary locomotion supplies any remaining sprint acceleration.
            previousTime = time; previousSeconds = seconds; driver.SetPoseBlendElapsed(this, (float)seconds);
            return new AnimationPlaygroundMotorIntent(heading * Mathf.Min(speed, profile.maximumSpeed), entryFacing,
                phase: time/entry.clip.length, paused: paused?.Invoke() == true);
        }

        private void GuardMotion()
        {
            if (!entering) return;
            var vitals = motor.GetComponent<TopDown3DSurvivalVitals>();
            if (vitals != null && vitals.Health <= 0 || paused?.Invoke() == true || !HasDemand || !motor.IsGrounded
                || Vector3.Dot(motor.LaboratoryMoveDirection.normalized, heading) < .75f || motor.AnimationSnapshot.Blocked || requestedSpeedLimited)
                Finish(false);
        }
        private void StageCompleted() { AfterMotion(); }
        private void AfterMotion()
        { if(entering && previousTime>=entry.clip.length && coordinator.IsSequenceOwner(this))Finish(true); }
        private void Finish(bool completed)
        {
            blending = driver.TryCapturePoseBlend(this, profile.exitBlend); blendTime = -Time.fixedDeltaTime;
            motor.RestoreDodgeFacing(coordinator); coordinator.CancelPhysicalSequence(this); entering = false;
            if (completed) driver.PrepareGroundedEntryRunOut(entry.exitPhase);
        }
        private void ObserveLocomotion()
        {
            if (disposed) return;
            if (entering && !coordinator.IsSequenceOwner(this)) { entering = false; driver.EndPoseBlend(this); }
            if (!blending || paused?.Invoke() == true) return;
            blendTime += Time.fixedDeltaTime; driver.SetPoseBlendElapsed(this, blendTime);
            if (blendTime >= profile.exitBlend) { driver.EndPoseBlend(this); blending = false; }
        }
        public void Cancel()
        {
            if (coordinator != null) { motor?.RestoreDodgeFacing(coordinator); coordinator.CancelPhysicalSequence(this); }
            driver?.EndPoseBlend(this); entering = blending = false; disarmed = lastDemand = true;
        }
        public bool TryYieldTo(object successor)
        {
            if(!Active || disposed || paused?.Invoke()==true || !motor.IsGrounded || successor==null
                || !driver.TryReservePoseBlendHandoff(this,successor,profile.exitBlend))return false;
            motor.RestoreDodgeFacing(coordinator);coordinator.CancelPhysicalSequence(this);
            driver.SelectReservedPoseBlend(successor);
            entering=blending=false;disarmed=lastDemand=true;
            return true;
        }
        private bool Fail(string reason) { LastFailure = reason; return false; }
        public void InvalidateForReset(uint epoch) => Cancel();
        public bool TryRestoreForReset(out string reason) { Cancel(); reason = null; return true; }
        private void OnDisable() => Cancel();
        public void DisposeOwnedServices()
        {
            if (disposed) return; Cancel(); disposed = true;
            if (coordinator != null) coordinator.ActionCompleted -= StageCompleted;
            if (motor != null) { motor.AnimationMotionPreparing -= GuardMotion; motor.AnimationLocomotionAdvanced -= ObserveLocomotion; }
            if (motor != null) motor.AnimationMotionAdvanced -= AfterMotion;
            registry?.Unregister(this);
            reportRejection = null;
        }
        private void OnDestroy() => DisposeOwnedServices();
    }
}
