using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Editor
{
    public static class TopDown3DWaypointCampPreview
    {
        // Static layout/art preview only. No actor simulation, Play Mode, scene save, or foreground activation.
        public static void CaptureFromCli()
        {
            if (!Application.dataPath.Replace('\\', '/').Contains("/BooterBigArmValidation/")) throw new InvalidOperationException("Use an isolated candidate");
            // A camera-space UI capture must omit world atmosphere/distortion features. Runtime UI remains overlay-based.
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var serialized = new SerializedObject(pipeline);
            var rendererSlot = serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0);
            var renderer = (ScriptableRendererData)rendererSlot.objectReferenceValue;
            var enabledFeatures = renderer.rendererFeatures.Select(feature => feature.isActive).ToArray();
            foreach (var feature in renderer.rendererFeatures) feature.SetActive(false);
            try
            {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Preview camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .075f, .045f); camera.transform.position = new Vector3(6, 5, -7); camera.transform.LookAt(Vector3.up * .7f);
            var sun = new GameObject("Preview sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f; sun.transform.rotation = Quaternion.Euler(15, 40, 0);
            RenderSettings.ambientLight = new Color(.45f, .4f, .35f); RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            var shade = UnityEngine.Object.Instantiate(TopDown3DCampSettings.Load().deployedPrefab);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube); marker.transform.position = new Vector3(2, 1.6f, -.4f); marker.transform.localScale = new Vector3(.12f, 3.2f, .12f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.position = new Vector3(0, -.15f, 0); ground.transform.localScale = new Vector3(20, .3f, 20);
            var material = new Material(TopDown3DCampSettings.Load().previewMaterial); material.SetColor("_BaseColor", new Color(.3f, .2f, .12f));
            ground.GetComponent<Renderer>().sharedMaterial = material;
            var ui = new GameObject("Camps preview", typeof(RectTransform)); var view = ui.AddComponent<TopDown3DCampCanvas>();
            view.Configure(() => {}, () => {}, () => {}); view.SetLabels("Return", "Regroup", "Back");
            var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            view.Show("Starting Camp · known · 32 m straight-line · bearing 175°\n~19 world seconds at 4x · shared return\nFood -0.48 · Hydration -0.76 · Reserve -0.19\nShared return route ready · session progress is not saved", true);
            view.SetAvailability(true, true, true);
            Render(camera, "starting-camp-and-camps-panel.png"); view.ApplyTextScale(true); Render(camera, "camps-panel-large-text.png");
            UnityEngine.Object.DestroyImmediate(ui);
            var system = new GameObject("Preview EventSystem").AddComponent<EventSystem>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DWaypointCampAuthoring.MenuPath);
            var menu = UnityEngine.Object.Instantiate(prefab); var menuView = menu.GetComponent<TopDown3DMenuView>(); menuView.Initialize(system);
            menuView.ApplySettings(new TopDown3DMenuSettings { largeText = true, reducedMotion = true }); menuView.Show("Root", "Camps");
            menu.SetActive(true);
            var menuCanvas = menu.GetComponent<Canvas>(); menuCanvas.renderMode = RenderMode.ScreenSpaceCamera; menuCanvas.worldCamera = camera; menuCanvas.planeDistance = 1;
            Render(camera, "system-menu-camps-large-text.png");
            UnityEngine.Object.DestroyImmediate(material);
            Debug.Log("WAYPOINT STATIC PREVIEWS COMPLETE; no gameplay smoke test or scene saved");
            }
            finally
            {
                for (var i = 0; i < enabledFeatures.Length; i++) renderer.rendererFeatures[i].SetActive(enabledFeatures[i]);
            }
        }
        private static void Render(Camera camera, string name)
        {
            var target = RenderTexture.GetTemporary(1200, 800, 24, RenderTextureFormat.ARGB32); var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                foreach (var text in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
                { text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle); text.SetAllDirty(); }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(1200, 800, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs/WaypointPreview"); File.WriteAllBytes("Logs/WaypointPreview/" + name, image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }
    }
}
