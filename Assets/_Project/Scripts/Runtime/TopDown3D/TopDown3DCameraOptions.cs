using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DCameraView { Elevated, ThirdPerson }

    /// <summary>Evaluates a private Cinemachine rig, then commits only a verified camera pose.
    /// The elevated component stays enabled because riding uses its availability and framing API.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(TopDown3DCameraRig)), DefaultExecutionOrder(1000)]
    public sealed class TopDown3DCameraOptions : MonoBehaviour
    {
        private TopDown3DCameraRig rig;
        private Camera output;
        private Transform pivot;
        private CinemachineCamera virtualCamera;
        private CinemachineThirdPersonFollow follow;
        private TopDown3DThirdPersonCameraProfile profile;
        private TopDown3DCameraSelection selection;
        private bool preferencesApplied;
        private TopDown3DMenuSettings preferences = new();
        private float yaw, pitch = 12, lookIdle, recoveredDistance = 5, travelHeading;
        private bool initialized, hasOrbit, ownsOutput, hasTravelHeading;
        public bool OwnsOutput => isActiveAndEnabled && ownsOutput;
        public TopDown3DCameraView PreferredView => preferences.cameraView;
        public float ThirdPersonFieldOfView => preferences.cameraFieldOfView;
        public TopDown3DCameraView EffectiveView => ownsOutput ? TopDown3DCameraView.ThirdPerson : TopDown3DCameraView.Elevated;
        public string Notice { get; private set; }
        public bool HasUnresolvedClearance { get; private set; }

        public void Configure(TopDown3DCameraRig owner, TopDown3DThirdPersonCameraProfile tuning = null)
        {
            if (rig != null && rig != owner) throw new System.InvalidOperationException("Camera options already have an owner.");
            rig = owner; output = GetComponent<Camera>();
            profile = tuning != null ? tuning : Resources.Load<TopDown3DThirdPersonCameraProfile>("ThirdPersonCameraProfile");
            if (rig == null || output == null) throw new System.InvalidOperationException("Camera options require the existing camera rig.");
            rig.RegisterCameraOptions(this);
            selection = rig.CameraSelection;
            if (selection != null && selection.IsConfigured && selection.OutputRig == rig)
            {
                rig.BindAuthoring(selection.elevated);
                profile = selection.thirdPerson.profile;
                if (!preferencesApplied)
                {
                    preferences.cameraView = selection.startingView;
                    preferences.cameraFieldOfView = selection.thirdPerson.fieldOfView;
                    TopDown3DMenuSettingsService.NormalizeCamera(preferences);
                }
            }
            else selection = null;
        }

        private void EnsureSolver()
        {
            if (virtualCamera != null) return;
            var flags = selection != null ? HideFlags.DontSave : HideFlags.HideAndDontSave;
            var anchor = new GameObject("Follow Pivot (runtime)") { hideFlags = flags };
            SceneManager.MoveGameObjectToScene(anchor, gameObject.scene); pivot = anchor.transform;
            var solver = new GameObject("Pose Solver (runtime)") { hideFlags = flags };
            SceneManager.MoveGameObjectToScene(solver, gameObject.scene);
            if (selection != null)
            {
                pivot.SetParent(selection.thirdPerson.transform, true);
                solver.transform.SetParent(selection.thirdPerson.transform, true);
            }
            virtualCamera = solver.AddComponent<CinemachineCamera>();
            // Never register this solver as a competing live camera. There is intentionally no Brain.
            virtualCamera.enabled = false;
            follow = solver.AddComponent<CinemachineThirdPersonFollow>();
            virtualCamera.Follow = pivot;
            follow.ShoulderOffset = Vector3.zero; follow.VerticalArmLength = 0;
            follow.CameraSide = .5f; follow.CameraDistance = profile.distance;
            follow.Damping = profile.positionDamping;
            // One clearance owner preserves actor/mount hierarchy exclusions and enclosed-boom recovery.
            // Cinemachine calculates follow geometry; the shared project solver resolves collisions.
            follow.AvoidObstacles.Enabled = false;
        }

        public void ApplyPreferences(TopDown3DMenuSettings settings)
        {
            preferences = settings.Clone();
            if (selection != null && !preferencesApplied)
            {
                preferences.cameraView = selection.startingView;
                preferences.cameraFieldOfView = selection.thirdPerson.fieldOfView;
            }
            TopDown3DMenuSettingsService.NormalizeCamera(preferences);
            preferencesApplied = true;
            lookIdle = 0;
            Notice = PreferredView == EffectiveView ? null : "Camera view will change when gameplay resumes and space is clear.";
        }

        public void RequestView(TopDown3DCameraView view)
        {
            if (view != TopDown3DCameraView.Elevated && view != TopDown3DCameraView.ThirdPerson) return;
            preferences.cameraView = view;
            Notice = PreferredView == EffectiveView ? null : "Camera view will change when gameplay resumes and space is clear.";
        }

        internal static float RecenterYaw(float current, float heading, Vector2 move, float idle, bool enabled, float dt)
        {
            if (!enabled || idle < 1.5f || move.y < .2f || Mathf.Abs(move.x) > .15f) return current;
            return Mathf.MoveTowardsAngle(current, heading, 90f * Mathf.Max(0, dt));
        }

        private void LateUpdate() => Tick(Mathf.Min(Time.unscaledDeltaTime, .1f));

        internal void Tick(float deltaTime)
        {
            if (rig == null) Configure(GetComponent<TopDown3DCameraRig>());
            if (rig.FollowTarget == null || rig.InputRouter == null || !rig.isActiveAndEnabled) return;
            if (ownsOutput && (profile == null || !profile.IsValid))
            { TrySetView(TopDown3DCameraView.Elevated); Notice = "Third person camera profile is unavailable."; return; }
            var router = rig.InputRouter;
            bool placement = router.Mode == TopDown3DInputMode.CampPlacement;
            bool gameplay = router.Mode == TopDown3DInputMode.Gameplay;
            // Freeze automatic motion in modal UI. World speed does not accelerate orbit or damping.
            if (!gameplay && !placement) { lookIdle = 0; hasTravelHeading = false; return; }
            var requested = placement ? TopDown3DCameraView.Elevated : PreferredView;
            if (requested != EffectiveView)
            {
                if (!TrySetView(requested)) return;
            }
            if (!ownsOutput) return;
            var dt = Mathf.Clamp(deltaTime, 0, .1f);
            var stick = router.CameraLookValue;
            var mouse = router.MouseLookDelta;
            bool manual = stick.sqrMagnitude > .0001f || mouse.sqrMagnitude > .0001f;
            lookIdle = manual ? 0 : lookIdle + dt;
            yaw = Mathf.Repeat(yaw + stick.x * 120 * preferences.cameraSensitivityX * dt
                + mouse.x * .3f * preferences.cameraSensitivityX, 360);
            float vertical = stick.y * 70 * dt + mouse.y * .3f;
            pitch = Mathf.Clamp(pitch - vertical * preferences.cameraSensitivityY * (preferences.cameraInvertY ? -1 : 1), profile.pitchLimits.x, profile.pitchLimits.y);
            var motor = rig.FollowTarget.GetComponent<TopDown3DPlayerMotor>();
            var body = rig.FollowTarget.GetComponent<Rigidbody>();
            var velocity = body != null ? Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up) : Vector3.zero;
            if (!manual && !rig.HasRidingFrame && velocity.sqrMagnitude > .25f && motor != null)
            {
                var measuredHeading = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
                travelHeading = hasTravelHeading ? Mathf.LerpAngle(travelHeading, measuredHeading, 1 - Mathf.Exp(-dt / .2f)) : measuredHeading;
                hasTravelHeading = true;
                yaw = RecenterYaw(yaw, travelHeading, router.MoveValue, lookIdle, preferences.cameraAutoRecenter, dt);
            }
            else hasTravelHeading = false;
            if (TryThirdPose(dt, false, out var position, out var rotation)) CommitThird(position, rotation);
            else { HasUnresolvedClearance = true; Notice = "Camera space is blocked. Move into clear space."; }
        }

        public bool TrySetView(TopDown3DCameraView view)
        {
            if (rig == null) Configure(GetComponent<TopDown3DCameraRig>());
            if (rig.FollowTarget == null || !rig.isActiveAndEnabled) return false;
            if (view == EffectiveView) return true;
            if (view == TopDown3DCameraView.Elevated)
            {
                if (!rig.TryGetElevatedPose(out var position, out var rotation)) return Rejected();
                ownsOutput = false;
                rig.RestoreElevatedPose(position, rotation);
                Notice = null; HasUnresolvedClearance = false; lookIdle = 0;
                return true;
            }
            if (view != TopDown3DCameraView.ThirdPerson) return false;
            if (profile == null || !profile.IsValid) { Notice = "Third person camera profile is unavailable."; return false; }
            if (!hasOrbit) { yaw = rig.FollowTarget.eulerAngles.y; pitch = profile.startingPitch; }
            if (!TryThirdPose(0, true, out var candidatePosition, out var candidateRotation)) return Rejected();
            ownsOutput = true; hasOrbit = true;
            CommitThird(candidatePosition, candidateRotation);
            lookIdle = 0; return true;
        }

        private bool Rejected()
        {
            Notice = "Selected camera is blocked. Your current view is retained; it will retry in clear space.";
            return false;
        }

        private bool TryThirdPose(float dt, bool snap, out Vector3 position, out Quaternion rotation)
        {
            EnsureSolver();
            follow.CameraDistance = profile.distance; follow.Damping = profile.positionDamping;
            // Riding requests and exclusion lifetimes remain with the canonical elevated rig.
            var origin = snap ? rig.FollowAnchorPosition + Vector3.up * profile.pivotHeight
                : rig.UpdateFollowPosition(dt) + Vector3.up * (profile.pivotHeight - rig.TargetHeightOffset);
            pivot.SetPositionAndRotation(origin, Quaternion.Euler(pitch, yaw, 0));
            var lens = virtualCamera.Lens; lens.FieldOfView = preferences.cameraFieldOfView;
            lens.NearClipPlane = .1f; lens.FarClipPlane = output.farClipPlane; virtualCamera.Lens = lens;
            virtualCamera.InternalUpdateCameraState(Vector3.up, snap || !initialized ? -1 : dt);
            initialized = true;
            var state = virtualCamera.State;
            rotation = state.GetFinalOrientation();
            var desired = state.GetFinalPosition();
            var offset = desired - origin; var length = offset.magnitude;
            position = output.transform.position;
            if (!Finite(desired) || !float.IsFinite(length) || length < .001f) return false;
            var direction = offset / length;
            // Enclose all near-plane corners, including wide-aspect and configurable-FOV views.
            var nearY = .1f * Mathf.Tan(preferences.cameraFieldOfView * .5f * Mathf.Deg2Rad);
            var radius = Mathf.Max(profile.clearanceRadius, Mathf.Sqrt(.01f + nearY * nearY * (1 + output.aspect * output.aspect)));
            if (!rig.TryResolveDistance(origin, direction, length, radius, out var clearDistance)) return false;
            recoveredDistance = snap ? clearDistance : Mathf.Min(clearDistance,
                Mathf.MoveTowards(recoveredDistance, clearDistance, profile.distance / profile.collisionReturnTime * dt));
            position = origin + direction * recoveredDistance;
            // Recovery from an obstructed origin can impose a lower clear bound as well as an upper bound.
            if (rig.HasBlockingOverlap(position, radius)) position = origin + direction * clearDistance;
            return !rig.HasBlockingOverlap(position, radius) && Finite(position);
        }

        internal void InvalidateFollow()
        { initialized = false; lookIdle = 0; hasTravelHeading = false; }

        private void CommitThird(Vector3 position, Quaternion rotation)
        {
            output.transform.SetPositionAndRotation(position, rotation);
            output.orthographic = false; output.fieldOfView = preferences.cameraFieldOfView; output.nearClipPlane = .1f;
            var range = GetComponent<BadwaterCameraRange>();
            if (range != null && range.isActiveAndEnabled) range.Apply();
            rig.RememberVerifiedPose(position);
            HasUnresolvedClearance = false; Notice = null;
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private void OnDisable()
        {
            ownsOutput = false; initialized = false; lookIdle = 0;
            if (rig != null && rig.FollowTarget != null && rig.TryGetElevatedPose(out var p, out var r)) rig.RestoreElevatedPose(p, r);
        }
        private void OnDestroy()
        {
            Release(virtualCamera != null ? virtualCamera.gameObject : null);
            Release(pivot != null ? pivot.gameObject : null);
        }
        private static void Release(GameObject item)
        { if (item == null) return; if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
    }
}
