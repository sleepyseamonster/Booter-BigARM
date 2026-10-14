using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    // Called from renderer setup after URP's volume update, including EditMode cameras.
    // Private stacks are reused; shared stack selection is restored when the camera ends.
    public static class TopDown3DCampOpticsStack
    {
        private sealed class State
        {
            public Camera camera;
            public VolumeStack owned;
            public VolumeStack previous;
            public Transform focusTarget;
            public TopDown3DCampOpticsProfile profile;
            public Vector3 lastPosition;
            public Quaternion lastRotation;
            public float focusDepth;
            public bool focusInitialized;
            public bool focusSearched;
            public Vector3 lastTargetPosition;
            public long lastManagedAllocation = -1;
        }
        private static readonly Dictionary<int, State> states = new Dictionary<int, State>();

        public static bool TryApply(Camera camera)
        {
            if (camera == null || camera.cameraType != CameraType.Game
                || !TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out var study)
                || study.nativeOptics == null || !study.nativeOptics.IsValid)
            {
                Release(camera);
                return false;
            }
            var manager = VolumeManager.instance;
            if (!manager.isInitialized) return false;
            long allocationBefore = study.nativeOptics.collectDiagnostics ? System.GC.GetAllocatedBytesForCurrentThread() : 0;
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null || data.renderType != CameraRenderType.Base || !data.renderPostProcessing) return false;
            if (states.TryGetValue(camera.GetInstanceID(), out var oldState) && !oldState.owned.isValid)
            {
                states.Remove(camera.GetInstanceID());
                if (manager.stack == oldState.owned) manager.ResetMainStack();
            }
            if (!states.TryGetValue(camera.GetInstanceID(), out var state))
            {
                state = new State { camera = camera, owned = manager.CreateStack() };
                states.Add(camera.GetInstanceID(), state);
                RenderPipelineManager.endCameraRendering -= EndCamera;
                RenderPipelineManager.endCameraRendering += EndCamera;
            }
            if (manager.stack != state.owned) state.previous = manager.stack;
            manager.Update(state.owned, data.volumeTrigger != null ? data.volumeTrigger : camera.transform, data.volumeLayerMask);
            Apply(state.owned, study.nativeOptics);
            UpdateFocus(state, study.nativeOptics);
            manager.stack = state.owned;
            state.lastManagedAllocation = study.nativeOptics.collectDiagnostics ? System.GC.GetAllocatedBytesForCurrentThread() - allocationBefore : -1;
            return true;
        }

        public static long LastApplyAllocatedBytes(Camera camera)
            => camera != null && states.TryGetValue(camera.GetInstanceID(), out var state) ? state.lastManagedAllocation : -1;

        public static float EyeDepth(Camera camera, Vector3 point)
            => Vector3.Dot(point - camera.transform.position, camera.transform.forward);

        private static void UpdateFocus(State state, TopDown3DCampOpticsProfile profile)
        {
            if (!profile.followPlayerFocus) { state.focusInitialized = false; return; }
            bool profileChanged = state.profile != profile;
            if (profileChanged) { state.focusSearched = false; state.focusTarget = null; }
            var binding = state.camera.GetComponent<TopDown3DSceneRenderingOverride>();
            if (binding != null && binding.OpticsFocusTarget != null && binding.OpticsFocusTarget != state.focusTarget)
            {
                state.focusTarget = binding.OpticsFocusTarget; state.focusSearched = true; state.focusInitialized = false;
            }
            if (state.focusTarget == null && !state.focusSearched)
            {
                state.focusSearched = true;
                foreach (var motor in Object.FindObjectsByType<TopDown3DPlayerMotor>(FindObjectsSortMode.None))
                    if (motor.gameObject.scene == state.camera.gameObject.scene) { state.focusTarget = motor.transform; break; }
            }
            if (state.focusTarget == null) { state.profile = profile; state.focusInitialized = false; return; }
            var position = state.camera.transform.position;
            var rotation = state.camera.transform.rotation;
            var targetPosition = state.focusTarget.position + (binding != null ? binding.OpticsFocusOffset : Vector3.up * 1.15f);
            float target = Mathf.Max(.1f, EyeDepth(state.camera, targetPosition));
            if (!float.IsFinite(target)) { state.focusInitialized = false; return; }
            bool cut = !state.focusInitialized || profileChanged
                || (position - state.lastPosition).sqrMagnitude > 25f
                || (targetPosition - state.lastTargetPosition).sqrMagnitude > 25f
                || Quaternion.Angle(rotation, state.lastRotation) > 35f;
            float weight = 1f - Mathf.Exp(-Mathf.Max(0f, Time.unscaledDeltaTime) / Mathf.Max(.01f, profile.focusSmoothingSeconds));
            state.focusDepth = cut ? target : Mathf.Lerp(state.focusDepth, target, weight);
            state.focusInitialized = true; state.profile = profile;
            state.lastPosition = position; state.lastRotation = rotation;
            state.lastTargetPosition = targetPosition;
            var dof = state.owned.GetComponent<DepthOfField>();
            dof.focusDistance.Override(state.focusDepth);
            dof.gaussianStart.Override(state.focusDepth + Mathf.Max(0f, profile.readableFarPadding));
            dof.gaussianEnd.Override(state.focusDepth + Mathf.Max(0f, profile.readableFarPadding) + Mathf.Max(.1f, profile.farBlurTransition));
        }

        public static void Apply(VolumeStack stack, TopDown3DCampOpticsProfile profile)
        {
            var bloom = stack.GetComponent<Bloom>();
            bloom.intensity.Override(profile.opticsEnabled && profile.bloomEnabled ? Mathf.Clamp(profile.bloomIntensity, 0f, 2f) : 0f);
            bloom.threshold.Override(Mathf.Clamp(profile.bloomThreshold, 0f, 5f));
            bloom.scatter.Override(Mathf.Clamp01(profile.bloomScatter));
            bloom.tint.Override(Color.white);
            bloom.highQualityFiltering.Override(profile.highQualityBloom);
            bloom.dirtTexture.Override(profile.dirtTexture);
            bloom.dirtIntensity.Override(profile.opticsEnabled ? Mathf.Clamp(profile.dirtIntensity, 0f, 5f) : 0f);
            var dof = stack.GetComponent<DepthOfField>();
            dof.mode.Override(profile.opticsEnabled ? profile.depthMode : DepthOfFieldMode.Off);
            dof.focusDistance.Override(Mathf.Max(.1f, profile.focusDistance));
            dof.gaussianStart.Override(Mathf.Max(.1f, profile.farStart));
            dof.gaussianEnd.Override(Mathf.Max(profile.farStart + .1f, profile.farEnd));
            dof.gaussianMaxRadius.Override(Mathf.Clamp(profile.gaussianRadius, 0f, 2f));
            dof.aperture.Override(Mathf.Clamp(profile.aperture, 1f, 32f));
            dof.focalLength.Override(Mathf.Clamp(profile.focalLength, 1f, 150f));
            stack.GetComponent<ColorAdjustments>().postExposure.Override(Mathf.Clamp(profile.postExposure, -4f, 4f));
            stack.GetComponent<Tonemapping>().mode.Override(profile.tonemapping);
        }

        private static void EndCamera(ScriptableRenderContext context, Camera camera)
            => Restore(camera);

        // Rendering harnesses without camera callbacks must call this in finally.
        public static void Restore(Camera camera)
        {
            if (camera != null && states.TryGetValue(camera.GetInstanceID(), out var state)
                && VolumeManager.instance.stack == state.owned)
                RestorePrevious(state);
        }
        public static void Release(Camera camera)
        {
            if (camera == null || !states.TryGetValue(camera.GetInstanceID(), out var state)) return;
            var manager = VolumeManager.instance;
            if (manager.stack == state.owned) RestorePrevious(state);
            manager.DestroyStack(state.owned);
            states.Remove(camera.GetInstanceID());
            if (states.Count == 0) RenderPipelineManager.endCameraRendering -= EndCamera;
        }

        private static void RestorePrevious(State state)
        {
            var manager = VolumeManager.instance;
            if (state.previous != null && state.previous.isValid) manager.stack = state.previous;
            else manager.ResetMainStack();
        }
    }
}
