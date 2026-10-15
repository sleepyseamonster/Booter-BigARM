using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    [InitializeOnLoad]
    public static class CampLookEditorPreview
    {
        private static TopDown3DCampLook current;
        private static double nextUpdate;
        private static bool saving;
        private sealed class ViewState
        {
            public SceneView view;
            public bool lighting, sky, effects, fx;
        }
        private sealed class CameraState
        {
            public Camera camera;
            public UniversalAdditionalCameraData data;
            public bool createdData, post;
            public LayerMask mask;
            public Material sky;
            public Skybox skybox;
            public bool createdSkybox, skyEnabled;
            public bool hdr;
            public VolumeStack borrowed;
        }
        private static readonly Dictionary<int, ViewState> views = new Dictionary<int, ViewState>();
        private static readonly Dictionary<int, CameraState> cameras = new Dictionary<int, CameraState>();
        static CampLookEditorPreview()
        {
            EditorApplication.update += Tick;
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.quitting += Release;
            EditorApplication.playModeStateChanged += _ => Release();
            EditorSceneManager.sceneSaving += (_, __) =>
            {
                saving = true; Release();
                EditorApplication.delayCall += () => { saving = false; if (!Application.isBatchMode) Refresh(); };
            };
            EditorSceneManager.sceneSaved += _ => { saving = false; if (!Application.isBatchMode) EditorApplication.delayCall += Refresh; };
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => Release();
        }

        private static TopDown3DCampLook FindLook()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || saving
                || PrefabStageUtility.GetCurrentPrefabStage() != null) return null;
            TopDown3DCampLook result = null;
            var scene = SceneManager.GetActiveScene();
            foreach (var look in Object.FindObjectsByType<TopDown3DCampLook>(FindObjectsSortMode.None))
            {
                if (!look.isActiveAndEnabled || !look.previewInEditor || look.gameObject.scene != scene) continue;
                if (result != null) return null;
                result = look;
            }
            if (result == null || result.gameCamera == null || result.sun == null
                || result.sun.gameObject.scene != scene || result.gameCamera.gameObject.scene != scene
                || !result.sun.isActiveAndEnabled || !TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(result.gameCamera, out _)) return null;
            return result;
        }

        private static void Tick()
        {
            if (Application.isBatchMode) return;
            if (EditorApplication.timeSinceStartup < nextUpdate) return;
            nextUpdate = EditorApplication.timeSinceStartup + .15;
            foreach (var state in new List<CameraState>(cameras.Values)) EndCamera(default, state.camera);
            Refresh();
        }

        public static void Refresh()
        {
            var look = FindLook();
            if (current != look) { Release(); current = look; }
            if (current == null) return;
            current.sun.RefreshEditorPresentation();
            foreach (SceneView view in SceneView.sceneViews)
            {
                if (!views.ContainsKey(view.GetInstanceID())) views.Add(view.GetInstanceID(), new ViewState
                { view = view, lighting = view.sceneLighting, sky = view.sceneViewState.showSkybox,
                    effects = view.sceneViewState.showImageEffects, fx = view.sceneViewState.fxEnabled });
                view.sceneLighting = true;
                view.sceneViewState.fxEnabled = true;
                view.sceneViewState.showSkybox = true;
                view.sceneViewState.showImageEffects = current.previewSceneViewEffects;
            }
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        // These hooks are public for isolated EditMode rendering proof; production registration
        // still comes only from the actual Scene View camera callback.
        public static void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null || camera.cameraType != CameraType.SceneView || cameras.ContainsKey(camera.GetInstanceID())) return;
            var look = current;
            if (look == null || look.sun.EditorPreviewSky == null) return;
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            bool created = data == null;
            if (created) { data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>(); data.hideFlags = HideFlags.HideAndDontSave; }
            var skybox = camera.GetComponent<Skybox>();
            bool createdSkybox = skybox == null;
            if (createdSkybox) { skybox = camera.gameObject.AddComponent<Skybox>(); skybox.hideFlags = HideFlags.HideAndDontSave; }
            cameras.Add(camera.GetInstanceID(), new CameraState { camera = camera, data = data, createdData = created,
                post = data.renderPostProcessing, mask = data.volumeLayerMask, sky = skybox.material,
                skybox = skybox, createdSkybox = createdSkybox, skyEnabled = skybox.enabled,
                hdr = camera.allowHDR, borrowed = VolumeManager.instance.stack });
            camera.allowHDR = true;
            data.renderPostProcessing = look.previewSceneViewEffects;
            data.volumeLayerMask = look.gameCamera.GetUniversalAdditionalCameraData().volumeLayerMask;
            TopDown3DSceneRenderingOverride.RegisterAuthoringCamera(camera,
                look.gameCamera.GetComponent<TopDown3DSceneRenderingOverride>(), look.previewSceneViewEffects);
            skybox.material = look.sun.EditorPreviewSky; skybox.enabled = true;
        }

        public static void EndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (ReferenceEquals(camera, null) || !cameras.TryGetValue(camera.GetInstanceID(), out var state)) return;
            TopDown3DSceneRenderingOverride.UnregisterAuthoringCamera(camera);
            if (camera != null) camera.allowHDR = state.hdr;
            if (state.skybox != null)
            {
                state.skybox.material = state.sky; state.skybox.enabled = state.skyEnabled;
                if (state.createdSkybox) Object.DestroyImmediate(state.skybox);
            }
            if (state.data != null)
            {
                state.data.renderPostProcessing = state.post; state.data.volumeLayerMask = state.mask;
                if (state.createdData) Object.DestroyImmediate(state.data);
            }
            var manager = VolumeManager.instance;
            if (manager.isInitialized && (manager.stack == null || !manager.stack.isValid))
            {
                if (state.borrowed != null && state.borrowed.isValid) manager.stack = state.borrowed;
                else manager.ResetMainStack();
            }
            cameras.Remove(camera.GetInstanceID());
        }

        public static void Release()
        {
            foreach (var state in new List<CameraState>(cameras.Values)) EndCamera(default, state.camera);
            cameras.Clear();
            foreach (var state in views.Values)
                if (state.view != null)
                {
                    state.view.sceneLighting = state.lighting; state.view.sceneViewState.showSkybox = state.sky;
                    state.view.sceneViewState.showImageEffects = state.effects;
                    state.view.sceneViewState.fxEnabled = state.fx;
                }
            views.Clear();
            if (current != null && current.sun != null) current.sun.EndStudyPreview();
            current = null;
        }

        public static void LookAtSun(TopDown3DCampLook look)
        {
            Refresh();
            var view = SceneView.lastActiveSceneView;
            if (view == null || look == null || look.sun == null) return;
            var position = look.gameCamera != null ? look.gameCamera.transform.position : view.camera.transform.position;
            var direction = look.sun.DirectionToSun;
            view.orthographic = false;
            view.LookAtDirect(position + direction * 20f, Quaternion.LookRotation(direction, Vector3.up), 20f);
            view.Repaint();
        }

        public static void MatchGameCamera(TopDown3DCampLook look)
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null || look == null || look.gameCamera == null) return;
            var camera = look.gameCamera;
            view.orthographic = false;
            view.cameraSettings.fieldOfView = camera.fieldOfView;
            view.cameraSettings.dynamicClip = false;
            view.cameraSettings.nearClip = camera.nearClipPlane;
            view.cameraSettings.farClip = camera.farClipPlane;
            view.AlignViewToObject(camera.transform);
            view.Repaint();
        }
    }
}
