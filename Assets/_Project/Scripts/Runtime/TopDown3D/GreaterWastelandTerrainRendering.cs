using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Runtime-only visual residency for the fixed production terrain. Never streams terrain or collision.</summary>
    [DefaultExecutionOrder(2000), DisallowMultipleComponent]
    public sealed class GreaterWastelandTerrainRendering : MonoBehaviour
    {
        public const string ScenePath = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        public const string AllTerrainArgument = "-greaterWastelandAllTerrainRendering";
        private readonly List<Bounds> coverage = new();
        private readonly List<Light> shadowLights = new();
        private readonly Vector3[] corners = new Vector3[4];
        private Camera[] cameras = Array.Empty<Camera>();
        private FixedTerrainRenderWindow window;
        public int ResidentTerrainCount => window?.Count ?? 0;
        public int EnabledTerrainCount => window?.EnabledCount ?? 0;

        private void OnEnable()
        {
            if (!Application.isPlaying || gameObject.scene.path != ScenePath) return;
            var sources = new List<Terrain>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                sources.AddRange(root.GetComponentsInChildren<Terrain>(true));
            }
            for (int i = 0; i < SceneManager.sceneCount; i++) RegisterSceneLights(SceneManager.GetSceneAt(i));
            try { window = new FixedTerrainRenderWindow(sources); }
            catch (ArgumentException exception) { Debug.LogWarning("Terrain rendering remains unchanged: " + exception.Message, this); return; }
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
            Refresh(null);
        }

        private void LateUpdate() => Refresh(null);
        private void BeforeCamera(ScriptableRenderContext context, Camera camera)
        { if (camera != null && ContributesCoverage(camera.cameraType)) Refresh(camera); }
        /// <summary>Explicit render requests that bypass URP camera callbacks must call this before culling.</summary>
        public void PrepareCamera(Camera camera) => Refresh(camera);
        /// <summary>Procedural/additive light owners register shadow contributors before their first render.</summary>
        public void RegisterShadowLight(Light light)
        {
            if (light != null && !shadowLights.Contains(light)) shadowLights.Add(light);
        }
        private void RegisterSceneLights(Scene scene)
        {
            if (!scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var light in root.GetComponentsInChildren<Light>(true)) RegisterShadowLight(light);
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            window?.Dispose(); window = null;
            shadowLights.Clear();
        }

        private void Refresh(Camera current)
        {
            if (window == null) return;
            coverage.Clear();
            int count = Camera.allCamerasCount;
            if (cameras.Length < count) cameras = new Camera[count];
            count = Camera.GetAllCameras(cameras);
            RegisterShadowLight(RenderSettings.sun);
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float shadowDistance = pipeline != null ? pipeline.shadowDistance : QualitySettings.shadowDistance;
            for (int i = 0; i < count; i++)
            {
                var camera = cameras[i];
                if (camera != null && camera.isActiveAndEnabled && ContributesCoverage(camera.cameraType))
                    AddCoverage(camera, shadowDistance);
            }
            // Editor Scene views preview the runtime set; their distant debug frusta must not re-enable the whole world.
            if (current != null && ContributesCoverage(current.cameraType)) AddCoverage(current, shadowDistance);
            // Multiple render callbacks in one frame may enable arrivals, but only LateUpdate spends the exit budget.
            window.Update(coverage, 256, current == null ? 16 : 0);
        }

        internal static bool ContributesCoverage(CameraType type) => type != CameraType.SceneView && type != CameraType.Preview;

        private void AddCoverage(Camera camera, float shadowDistance)
        {
            // Bounds from the actual view include orbit, orthographic cameras, jumps and multiple cameras.
            var view = ViewBounds(camera, camera.farClipPlane);
            view.Expand(new Vector3(256, 0, 256));
            coverage.Add(view);
            if (shadowDistance <= 0) return;
            var receivers = ViewBounds(camera, Mathf.Min(camera.farClipPlane, shadowDistance));
            for (int i = shadowLights.Count - 1; i >= 0; i--)
            {
                var light = shadowLights[i];
                if (light == null) { shadowLights.RemoveAt(i); continue; }
                if (!light.isActiveAndEnabled || light.shadows == LightShadows.None) continue;
                if (light.type == LightType.Directional)
                    coverage.Add(ShadowCasterBounds(receivers, window.LandscapeBounds, light.transform.forward));
                else
                {
                    var lightBounds = new Bounds(light.transform.position, Vector3.one * (light.range * 2));
                    if (FixedTerrainRenderWindow.IntersectsXZ(receivers, lightBounds)) coverage.Add(lightBounds);
                }
            }
        }

        private Bounds ViewBounds(Camera camera, float distance)
        {
            var result = new Bounds(camera.transform.position, Vector3.zero);
            // Stereo eyes are conservatively unioned; Mono is used for ordinary cameras.
            if (camera.stereoEnabled)
            {
                AddCorners(camera, distance, Camera.MonoOrStereoscopicEye.Left, ref result);
                AddCorners(camera, distance, Camera.MonoOrStereoscopicEye.Right, ref result);
            }
            else AddCorners(camera, distance, Camera.MonoOrStereoscopicEye.Mono, ref result);
            return result;
        }

        private void AddCorners(Camera camera, float distance, Camera.MonoOrStereoscopicEye eye, ref Bounds bounds)
        {
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, eye, corners);
            for (int i = 0; i < 4; i++) bounds.Encapsulate(camera.transform.TransformPoint(corners[i]));
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), Mathf.Max(distance, camera.nearClipPlane), eye, corners);
            for (int i = 0; i < 4; i++) bounds.Encapsulate(camera.transform.TransformPoint(corners[i]));
        }

        public static Bounds ShadowCasterBounds(Bounds receivers, Bounds landscape, Vector3 lightTravelDirection)
        {
            if (Mathf.Abs(lightTravelDirection.y) < .001f) return landscape; // Horizon lighting fails open.
            float heightRange = Mathf.Max(0, landscape.max.y - landscape.min.y);
            var towardSource = -lightTravelDirection * (heightRange / Mathf.Abs(lightTravelDirection.y));
            var result = receivers;
            result.Encapsulate(receivers.min + towardSource);
            result.Encapsulate(receivers.max + towardSource);
            result.Expand(new Vector3(256, 0, 256));
            return result;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallLoaded()
        {
            SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) Install(SceneManager.GetSceneAt(i));
        }
        private static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (!Application.isPlaying) return;
            Install(scene);
            for (int i = 0; i < SceneManager.sceneCount; i++)
                foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    var controller = root.GetComponentInChildren<GreaterWastelandTerrainRendering>(true);
                    if (controller != null) controller.RegisterSceneLights(scene);
                }
        }
        private static void Install(Scene scene)
        {
            if (!Application.isPlaying || scene.path != ScenePath || Array.Exists(Environment.GetCommandLineArgs(),
                value => string.Equals(value, AllTerrainArgument, StringComparison.OrdinalIgnoreCase))) return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<GreaterWastelandTerrainRendering>(true) != null) return;
            var owner = new GameObject("Greater Wasteland Terrain Rendering") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(owner, scene);
            owner.AddComponent<GreaterWastelandTerrainRendering>();
        }
    }
}
