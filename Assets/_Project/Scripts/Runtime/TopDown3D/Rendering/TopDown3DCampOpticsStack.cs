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
            public double nextFocusSearchTime, lastFocusTime;
            public Transform explicitTarget;
            public Vector2Int lastResolution;
            public readonly List<GameObject> searchRoots = new List<GameObject>();
            public readonly List<TopDown3DPlayerMotor> searchMotors = new List<TopDown3DPlayerMotor>();
            public Vector3 lastTargetPosition;
            public long lastManagedAllocation = -1;
        }
        private static readonly Dictionary<int, State> states = new Dictionary<int, State>();

        public static bool TryApply(Camera camera)
        {
            if (camera == null || (camera.cameraType != CameraType.Game && !TopDown3DSceneRenderingOverride.IsAuthoringCamera(camera))
                || !TopDown3DSceneRenderingOverride.AuthoringEffectsEnabled(camera)
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
            if (data == null || data.renderType != CameraRenderType.Base || !data.renderPostProcessing)
            {
                Release(camera);
                return false;
            }
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
            double now = Time.realtimeSinceStartupAsDouble;
            bool profileChanged = state.profile != profile;
            if (profileChanged) { state.nextFocusSearchTime = 0; state.focusTarget = null; state.explicitTarget = null; }
            var binding = TopDown3DSceneRenderingOverride.BindingForCamera(state.camera);
            var explicitTarget = binding != null ? binding.OpticsFocusTarget : null;
            if (state.explicitTarget != explicitTarget)
            {
                state.explicitTarget = explicitTarget; state.focusTarget = explicitTarget;
                state.nextFocusSearchTime = 0; state.focusInitialized = false;
            }
            if ((!ReferenceEquals(state.focusTarget, null) && state.focusTarget == null)
                || (state.focusTarget != null && !state.focusTarget.gameObject.activeInHierarchy))
            {
                state.focusTarget = null; state.focusInitialized = false; state.nextFocusSearchTime = 0;
            }
            if (explicitTarget != null && explicitTarget.gameObject.activeInHierarchy && state.focusTarget != explicitTarget)
            { state.focusTarget = explicitTarget; state.focusInitialized = false; }
            if (state.focusTarget == null && now >= state.nextFocusSearchTime)
            {
                // A delayed spawn or replacement is found within half a second. Reused lists
                // avoid allocating a global search array and also support preview scenes.
                state.nextFocusSearchTime = now + .5;
                var scene = TopDown3DSceneRenderingOverride.RenderingScene(state.camera);
                if (scene.IsValid() && scene.isLoaded)
                {
                    scene.GetRootGameObjects(state.searchRoots);
                    foreach (var root in state.searchRoots)
                    {
                        state.searchMotors.Clear();
                        root.GetComponentsInChildren(false, state.searchMotors);
                        if (state.searchMotors.Count == 0) continue;
                        state.focusTarget = state.searchMotors[0].transform;
                        break;
                    }
                    state.searchRoots.Clear(); state.searchMotors.Clear();
                }
            }
            if (state.focusTarget == null) { state.profile = profile; state.focusInitialized = false; return; }
            var position = state.camera.transform.position;
            var rotation = state.camera.transform.rotation;
            var targetPosition = state.focusTarget.position + (binding != null ? binding.OpticsFocusOffset : Vector3.up * 1.15f);
            float target = EyeDepth(state.camera, targetPosition);
            // Preserve the configured fallback values already applied this render. A source
            // behind the near plane must not focus the entire world at ten centimetres.
            if (!float.IsFinite(target) || target <= Mathf.Max(.1f, state.camera.nearClipPlane))
            { state.profile = profile; state.focusInitialized = false; return; }
            var resolution = new Vector2Int(state.camera.pixelWidth, state.camera.pixelHeight);
            bool cut = !state.focusInitialized || profileChanged
                || state.lastResolution != resolution
                || (position - state.lastPosition).sqrMagnitude > 25f
                || (targetPosition - state.lastTargetPosition).sqrMagnitude > 25f
                || Quaternion.Angle(rotation, state.lastRotation) > 35f;
            float elapsed = (float)System.Math.Max(0, now - state.lastFocusTime);
            float weight = 1f - Mathf.Exp(-elapsed / Mathf.Max(.01f, profile.focusSmoothingSeconds));
            state.focusDepth = cut ? target : Mathf.Lerp(state.focusDepth, target, weight);
            state.focusInitialized = true; state.profile = profile;
            state.lastPosition = position; state.lastRotation = rotation;
            state.lastTargetPosition = targetPosition;
            state.lastFocusTime = now; state.lastResolution = resolution;
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
            bloom.filter.Override(profile.bloomFilter);
            bloom.downscale.Override(profile.bloomDownscale);
            bloom.maxIterations.Override(Mathf.Clamp(profile.bloomMaxIterations, 2, 8));
            bloom.dirtTexture.Override(profile.dirtTexture);
            bloom.dirtIntensity.Override(profile.opticsEnabled ? Mathf.Clamp(profile.dirtIntensity, 0f, 5f) : 0f);
            var dof = stack.GetComponent<DepthOfField>();
            dof.mode.Override(profile.opticsEnabled && !profile.gaussianBeforeLens ? profile.depthMode : DepthOfFieldMode.Off);
            dof.focusDistance.Override(Mathf.Max(.1f, profile.focusDistance));
            dof.gaussianStart.Override(Mathf.Max(.1f, profile.farStart));
            dof.gaussianEnd.Override(Mathf.Max(profile.farStart + .1f, profile.farEnd));
            dof.gaussianMaxRadius.Override(Mathf.Clamp(profile.gaussianRadius, 0f, 2f));
            dof.highQualitySampling.Override(profile.gaussianHighQualitySampling);
            dof.aperture.Override(Mathf.Clamp(profile.aperture, 1f, 32f));
            dof.focalLength.Override(Mathf.Clamp(profile.focalLength, 1f, 150f));
            stack.GetComponent<ColorAdjustments>().postExposure.Override(Mathf.Clamp(profile.postExposure, -4f, 4f));
            stack.GetComponent<Tonemapping>().mode.Override(profile.tonemapping);
        }

        private static void EndCamera(ScriptableRenderContext context, Camera camera)
        {
            // A disabled camera can still render manually, but does not retain idle ownership
            // after that render. The binding handles an enabled-to-disabled transition as well.
            if (camera != null && !camera.enabled) Release(camera);
            else Restore(camera);
        }

        // Rendering harnesses without camera callbacks must call this in finally.
        public static void Restore(Camera camera)
        {
            if (camera != null && states.TryGetValue(camera.GetInstanceID(), out var state)
                && VolumeManager.instance.stack == state.owned)
                RestorePrevious(state);
        }
        public static void Release(Camera camera)
        {
            // Unity's destroyed-object equality reports null, but its managed wrapper still
            // carries the instance ID needed to release the binding's cached owner.
            if (ReferenceEquals(camera, null) || !states.TryGetValue(camera.GetInstanceID(), out var state)) return;
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
