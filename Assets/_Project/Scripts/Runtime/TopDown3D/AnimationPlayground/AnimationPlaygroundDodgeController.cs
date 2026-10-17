using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundDodgeController : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundDodgeProfile profile;
        private AnimationPlaygroundResetRegistry registry;
        private Func<bool> paused, otherBusy;
        // A running roll may shed momentum, but must not inherit the standing
        // source's instantaneous braking. Ordinary gaits keep their own tuning.
        private const float MovingBraking = 8f;
        private Vector3 direction, entryDirection, entryFacing;
        private float entrySpeed, previousSpeed, previousTime, blendTime, cooldown;
        private bool rolling, blending, disposed;
        public bool Active => rolling || blending;
        public bool Ready => profile != null && profile.Validate(profile.targetModel, out _);
        public string LastFailure { get; private set; }
        public string ResetId => "CampLoop.Booter.Dodge";
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.Actor;

        public void Configure(TopDown3DPlayerMotor value, TopDown3DPlayerAnimationDriver presentation,
            AnimationPlaygroundActionCoordinator actions, AnimationPlaygroundDodgeProfile calibration,
            AnimationPlaygroundResetRegistry resets, Func<bool> shouldPause, Func<bool> activityBusy)
        {
            if (motor != null || value == null || presentation == null || actions == null || resets == null
                || calibration == null || !calibration.Validate(calibration.targetModel, out _))
                throw new ArgumentException("Configure one qualified dodge family and its existing motor owner.");
            motor = value; driver = presentation; coordinator = actions; profile = calibration; registry = resets;
            paused = shouldPause; otherBusy = activityBusy; registry.Register(this);
            coordinator.ActionCompleted += StageCompleted; motor.AnimationLocomotionAdvanced += ObserveLocomotion;
            motor.AnimationMotionPreparing += GuardActiveMotion;
        }

        public bool TryDodge()
        {
            if (disposed || !isActiveAndEnabled || !Ready || Active || cooldown > .000001f
                || paused?.Invoke() == true || otherBusy?.Invoke() == true || coordinator.IsBusy
                || !motor.CanBeginDodgeMotion(coordinator)) return Fail("Dodge requires clear grounded movement.");
            var vitals = motor.GetComponent<TopDown3DSurvivalVitals>();
            if (vitals != null && vitals.Health <= 0) return Fail("Dodge is unavailable.");
            direction = Vector3.ProjectOnPlane(motor.LaboratoryMoveDirection, Vector3.up);
            if (direction.sqrMagnitude < .01f) direction = Vector3.ProjectOnPlane(motor.FacingDirection, Vector3.up);
            if (direction.sqrMagnitude < .000001f) return Fail("A dodge direction is unavailable.");
            direction.Normalize(); entryFacing = motor.FacingDirection;
            entryDirection = Vector3.ProjectOnPlane(motor.Velocity, Vector3.up);
            entryDirection = entryDirection.sqrMagnitude > .0001f ? entryDirection.normalized : direction;
            var normal = motor.TryGetLaboratorySupportPlane(out var support, true) ? support.normal : Vector3.up;
            entrySpeed = previousSpeed = Mathf.Min(profile.maximumSpeed, Vector3.ProjectOnPlane(motor.Velocity, normal).magnitude);
            previousTime = 0;
            if (!driver.TryCapturePoseBlend(this, profile.entryBlend)) return Fail("The dodge entry pose is unavailable.");
            var binding = new AnimationPlaygroundMotionBinding { sourceId = "QualifiedDodge.Roll", clip = profile.roll,
                readiness = AnimationPlaygroundMotionReadiness.PhysicalQualified };
            if (!coordinator.TryBeginPhysicalSequence(this, binding, AnimationPlaygroundMotionMode.Dodge,
                null, Intent, null, policy: AnimationPlaygroundMotionPolicy.DodgeLocomotion))
            { driver.EndPoseBlend(this); return Fail("The dodge could not acquire its movement and presentation."); }
            rolling = true; LastFailure = null;
            if (!driver.SetAnimationBodyProjection(coordinator, profile.bodyProjection)
                || !driver.SetAnimationDodgeProjection(coordinator, direction)
                || !driver.SetAnimationGarmentFloor(coordinator, true, profile.maximumGarmentProjection, profile.garmentClearance))
            { Cancel(); return Fail("The paired dodge projection is unavailable."); }
            return true;
        }

        private AnimationPlaygroundMotorIntent Intent(double seconds)
        {
            var time = Mathf.Min((float)seconds, profile.roll.length);
            var entry = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / profile.entryBlend));
            var speed = Mathf.Lerp(entrySpeed, profile.Speed(previousTime, time), entry);
            var heading = BlendHeading(entryDirection, direction, entry);
            var phase = time / profile.roll.length;
            var runOut = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1 - profile.runOutFraction, 1, phase));
            var target = motor.DodgeRunOutVelocity(coordinator);
            speed = Mathf.Lerp(speed, target.magnitude, runOut);
            if (target.sqrMagnitude > .0001f) heading = BlendHeading(heading, target, runOut);
            if (entrySpeed > .18f) speed = Mathf.Max(speed, previousSpeed - MovingBraking * Mathf.Max(0, time - previousTime));
            speed = Mathf.Clamp(speed, 0, profile.maximumSpeed);
            if (paused?.Invoke() != true) { previousTime = time; previousSpeed = speed; }
            driver.SetPoseBlendElapsed(this, time);
            return new AnimationPlaygroundMotorIntent(heading * speed, entryFacing, phase: phase, paused: paused?.Invoke() == true);
        }

        private static Vector3 BlendHeading(Vector3 from, Vector3 to, float blend)
        {
            from = Vector3.ProjectOnPlane(from, Vector3.up); to = Vector3.ProjectOnPlane(to, Vector3.up);
            return Quaternion.Slerp(Quaternion.LookRotation(from.normalized, Vector3.up),
                Quaternion.LookRotation(to.normalized, Vector3.up), blend) * Vector3.forward;
        }

        private void StageCompleted()
        { if (rolling && coordinator.IsSequenceOwner(this)) Finish(); }
        private void Finish()
        {
            blending = driver.TryCapturePoseBlend(this, profile.exitBlend); blendTime = -Time.fixedDeltaTime;
            motor.RestoreDodgeFacing(coordinator); coordinator.CancelPhysicalSequence(this);
            rolling = false; cooldown = profile.cooldown;
        }
        private void ObserveLocomotion()
        {
            if (disposed) return;
            var vitals = motor.GetComponent<TopDown3DSurvivalVitals>();
            if (rolling && vitals != null && vitals.Health <= 0) { Cancel(); return; }
            if (paused?.Invoke() == true) return;
            if (cooldown > 0) cooldown = Mathf.Max(0, cooldown - Time.fixedDeltaTime);
            if (rolling && !coordinator.IsSequenceOwner(this)) { rolling = false; driver.EndPoseBlend(this); cooldown = profile.cooldown; }
            if (!blending) return;
            blendTime += Time.fixedDeltaTime; driver.SetPoseBlendElapsed(this, blendTime);
            if (blendTime >= profile.exitBlend) { driver.EndPoseBlend(this); blending = false; }
        }
        private void GuardActiveMotion()
        {
            var vitals = motor != null ? motor.GetComponent<TopDown3DSurvivalVitals>() : null;
            if (rolling && vitals != null && vitals.Health <= 0) Cancel();
        }
        public void Cancel()
        {
            if (coordinator != null) { if (motor != null) motor.RestoreDodgeFacing(coordinator); coordinator.CancelPhysicalSequence(this); }
            driver?.EndPoseBlend(this); rolling = blending = false; cooldown = profile != null ? profile.cooldown : 0;
        }
        private bool Fail(string reason) { LastFailure = reason; return false; }
        public void InvalidateForReset(uint epoch) => Cancel();
        public bool TryRestoreForReset(out string reason) { Cancel(); cooldown = 0; reason = null; return true; }
        private void OnDisable() => Cancel();
        public void DisposeOwnedServices()
        {
            if (disposed) return; Cancel(); disposed = true;
            if (coordinator != null) coordinator.ActionCompleted -= StageCompleted;
            if (motor != null) motor.AnimationLocomotionAdvanced -= ObserveLocomotion;
            if (motor != null) motor.AnimationMotionPreparing -= GuardActiveMotion;
            registry?.Unregister(this);
        }
        private void OnDestroy() => DisposeOwnedServices();
    }
}
