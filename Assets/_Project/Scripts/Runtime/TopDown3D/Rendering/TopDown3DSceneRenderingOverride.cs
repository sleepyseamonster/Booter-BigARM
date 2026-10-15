using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-600)]
    public sealed class TopDown3DSceneRenderingOverride : MonoBehaviour
    {
        [SerializeField] private bool disableTiltShift = true;
        [SerializeField] private TopDown3DAtmosphereStudyProfile atmosphereProfile;
        [SerializeField] private TopDown3DCameraOpticsProfile cameraOpticsProfile;
        public bool ModularOpticsConfigured => cameraOpticsProfile != null;
        public void ConfigureOptics(TopDown3DCameraOpticsProfile profile)
        {
            TopDown3DCampOpticsStack.Release(GetComponent<Camera>());
            cameraOpticsProfile = profile;
        }
        public static TopDown3DCameraOpticsProfile CameraOpticsFor(Camera camera)
        {
            var binding = BindingForCamera(camera);
            if (binding == null || !binding.isActiveAndEnabled || binding.cameraOpticsProfile == null
                || !binding.cameraOpticsProfile.IsValid || !AuthoringEffectsEnabled(camera)
                || (camera.cameraType == CameraType.SceneView && !IsAuthoringCamera(camera))) return null;
            int count = 0;
            foreach (var item in active)
                if (item != null && item.isActiveAndEnabled && item.gameObject.scene == binding.gameObject.scene
                    && (item.cameraOpticsProfile != null || item.atmosphereProfile != null)) count++;
            return count == 1 && binding.GetComponent<Camera>() != null ? binding.cameraOpticsProfile : null;
        }
        public static bool TryGetOpticsProfile(Camera camera, out TopDown3DCampOpticsProfile optics)
        {
            optics = null;
            var binding = BindingForCamera(camera);
            if (binding != null && binding.ModularOpticsConfigured)
            { var selected = CameraOpticsFor(camera); optics = selected != null ? selected.nativeOptics : null; return optics != null; }
            if (!TryGetAtmosphereProfile(camera, out var legacy)) return false;
            optics = legacy.nativeOptics;
            return optics != null && optics.IsValid;
        }
        public static bool TryGetOpticsProfile(Scene scene, out TopDown3DCampOpticsProfile optics)
        {
            optics = null;
            foreach (var item in active)
                if (item != null && item.gameObject.scene == scene && item.ModularOpticsConfigured)
                    return TryGetOpticsProfile(item.GetComponent<Camera>(), out optics);
            return TryGetAtmosphereProfile(scene, out var legacy) && (optics = legacy.nativeOptics) != null && optics.IsValid;
        }
        public static float SunAngularDiameterFor(Camera camera, float fallback)
        {
            var rig = TopDown3DEnvironmentRig.ForScene(RenderingScene(camera));
            return rig != null && rig.sky != null && rig.sky.profile != null ? rig.sky.profile.sunAngularDiameterDegrees : fallback;
        }
        public static Color SunTintFor(Camera camera, Color fallback)
        {
            var rig = TopDown3DEnvironmentRig.ForScene(RenderingScene(camera));
            return rig != null && rig.sunLight != null && rig.sunLight.profile != null
                ? rig.sunLight.profile.Tint(rig.State.Brightness01) : fallback;
        }
        [SerializeField] private Transform opticsFocusTarget;
        [SerializeField] private Vector3 opticsFocusOffset = new Vector3(0f, 1.15f, 0f);
        public Transform OpticsFocusTarget => opticsFocusTarget;
        public Vector3 OpticsFocusOffset => opticsFocusOffset;
        public void ConfigureFocusTarget(Transform target, Vector3 offset)
        {
            opticsFocusTarget = target; opticsFocusOffset = offset;
            TopDown3DCampOpticsStack.Release(GetComponent<Camera>());
        }
        private static readonly HashSet<TopDown3DSceneRenderingOverride> active = new HashSet<TopDown3DSceneRenderingOverride>();
        private static readonly Dictionary<int, TopDown3DSceneRenderingOverride> authoringCameras = new Dictionary<int, TopDown3DSceneRenderingOverride>();
        private static readonly HashSet<int> authoringEffects = new HashSet<int>();
        public static void RegisterAuthoringCamera(Camera camera, TopDown3DSceneRenderingOverride binding, bool effects)
        {
            if (camera == null || camera.cameraType != CameraType.SceneView || binding == null) return;
            authoringCameras[camera.GetInstanceID()] = binding;
            if (effects) authoringEffects.Add(camera.GetInstanceID()); else authoringEffects.Remove(camera.GetInstanceID());
        }
        public static void UnregisterAuthoringCamera(Camera camera)
        {
            if (ReferenceEquals(camera, null)) return;
            TopDown3DCampOpticsStack.Release(camera);
            authoringCameras.Remove(camera.GetInstanceID()); authoringEffects.Remove(camera.GetInstanceID());
        }
        public static bool IsAuthoringCamera(Camera camera)
            => camera != null && authoringCameras.ContainsKey(camera.GetInstanceID());
        public static bool AuthoringEffectsEnabled(Camera camera)
            => !IsAuthoringCamera(camera) || authoringEffects.Contains(camera.GetInstanceID());
        public static TopDown3DSceneRenderingOverride BindingForCamera(Camera camera)
            => camera == null ? null : authoringCameras.TryGetValue(camera.GetInstanceID(), out var binding)
                ? binding : camera.GetComponent<TopDown3DSceneRenderingOverride>();
        private Camera ownerCamera;
        private bool cameraWasEnabled;
        private void OnEnable()
        {
            active.Add(this);
            ownerCamera = GetComponent<Camera>();
            cameraWasEnabled = ownerCamera != null && ownerCamera.enabled;
        }
        private void Update()
        {
            var cameraEnabled = ownerCamera != null && ownerCamera.enabled;
            if (cameraWasEnabled && !cameraEnabled) TopDown3DCampOpticsStack.Release(ownerCamera);
            cameraWasEnabled = cameraEnabled;
        }
        private void OnDisable()
        {
            active.Remove(this);
            TopDown3DCampOpticsStack.Release(GetComponent<Camera>());
        }
        public static bool TiltShiftDisabledIn(Scene scene)
        {
            if (!scene.IsValid()) return false;
            if (TryGetAtmosphereProfile(scene, out var profile)) return !profile.tiltShiftEnabled;
            foreach (var item in active)
                if (item != null && item.isActiveAndEnabled && item.disableTiltShift && item.gameObject.scene == scene) return true;
            return false;
        }

        public void ConfigureAtmosphere(TopDown3DAtmosphereStudyProfile profile)
        {
            if (atmosphereProfile != profile) TopDown3DCampOpticsStack.Release(GetComponent<Camera>());
            atmosphereProfile = profile;
        }

        // Fail closed for ambiguous bindings. The validator reports duplicates, never first-wins selection.
        public static bool TryGetAtmosphereProfile(Scene scene, out TopDown3DAtmosphereStudyProfile profile)
        {
            profile = null;
            if (!scene.IsValid()) return false;
            int count = 0;
            Camera ownerCamera = null;
            foreach (var item in active)
            {
                if (item == null || !item.isActiveAndEnabled || item.gameObject.scene != scene || item.atmosphereProfile == null) continue;
                count++;
                profile = item.atmosphereProfile;
                ownerCamera = item.GetComponent<Camera>();
            }
            if (count != 1 || ownerCamera == null || !profile.IsValid) { profile = null; return false; }
            return true;
        }

        public static int AtmosphereBindingCount(Scene scene)
        {
            int count = 0;
            foreach (var item in active)
                if (item != null && item.isActiveAndEnabled && item.gameObject.scene == scene && item.atmosphereProfile != null) count++;
            return count;
        }

        public static Camera AtmosphereCamera(Scene scene)
        {
            if (!TryGetAtmosphereProfile(scene, out _)) return null;
            foreach (var item in active)
                if (item != null && item.isActiveAndEnabled && item.gameObject.scene == scene && item.atmosphereProfile != null)
                    return item.GetComponent<Camera>();
            return null;
        }

        public static bool TryGetAtmosphereProfile(Camera camera, out TopDown3DAtmosphereStudyProfile profile)
        {
            if (IsAuthoringCamera(camera))
            {
                var binding = BindingForCamera(camera);
                if (binding != null && binding.isActiveAndEnabled
                    && AtmosphereCamera(binding.gameObject.scene) == binding.GetComponent<Camera>())
                    return TryGetAtmosphereProfile(binding.gameObject.scene, out profile);
                profile = null; return false;
            }
            var scene = RenderingScene(camera);
            // Camera-local sky/optics are selected together. Utility and unrelated cameras retain defaults.
            if (camera == null || camera.cameraType == CameraType.SceneView || AtmosphereCamera(scene) != camera)
            { profile = null; return false; }
            return TryGetAtmosphereProfile(scene, out profile);
        }

        public static Scene RenderingScene(Camera camera)
            => camera == null ? default : IsAuthoringCamera(camera) && BindingForCamera(camera) != null
                ? BindingForCamera(camera).gameObject.scene : camera.scene.IsValid() ? camera.scene : camera.gameObject.scene;
    }
}
