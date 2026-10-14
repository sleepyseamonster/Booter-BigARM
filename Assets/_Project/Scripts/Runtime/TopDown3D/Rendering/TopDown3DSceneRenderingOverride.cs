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
        private void OnEnable() => active.Add(this);
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

        public void ConfigureAtmosphere(TopDown3DAtmosphereStudyProfile profile) => atmosphereProfile = profile;

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
            var scene = RenderingScene(camera);
            // Camera-local sky/optics are selected together. Utility and unrelated cameras retain defaults.
            if (camera == null || camera.cameraType == CameraType.SceneView || AtmosphereCamera(scene) != camera)
            { profile = null; return false; }
            return TryGetAtmosphereProfile(scene, out profile);
        }

        public static Scene RenderingScene(Camera camera)
            => camera == null ? default : camera.scene.IsValid() ? camera.scene : camera.gameObject.scene;
    }
}
