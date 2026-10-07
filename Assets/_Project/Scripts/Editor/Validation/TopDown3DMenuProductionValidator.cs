using System;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Editor
{
    public static class TopDown3DMenuProductionValidator
    {
        public static void ValidateAndCaptureFromCli() { ValidateFromCli(); CaptureFromCli(); }
        public static void UpdateOwnedPrefabAndCaptureFromCli() { TopDown3DMenuPrefabAuthoring.UpdateOwnedFromCli(); ValidateAndCaptureFromCli(); }
        public static void ValidateFromCli()
        {
            BadwaterPlayableSceneBuilder.ValidateFromCli();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DMenuPrefabAuthoring.PrefabPath);
            if (prefab == null || prefab.GetComponent<TopDown3DMenuView>() == null)
                throw new InvalidOperationException("Canonical menu view prefab is missing.");
            if (prefab.GetComponentsInChildren<EventSystem>(true).Length != 0 || prefab.GetComponent<Canvas>().sortingOrder != 300)
                throw new InvalidOperationException("Menu prefab must use order 300 and the scene EventSystem.");
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            if (actions.FindAction("System/ToggleMenu", true).bindings.Count != 2)
                throw new InvalidOperationException("ToggleMenu requires controller and keyboard bindings.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var directory = Path.Combine(Path.GetTempPath(), "BooterMenuValidation", Guid.NewGuid().ToString("N"));
            var kit = TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene, directory);
            var menu = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(scene);
            if (kit == null || menu == null || !menu.IsConfigured || menu.IsOpen)
                throw new InvalidOperationException("Production menu did not install closed and configured.");
            if (TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene, directory) != kit
                || TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(scene) != menu)
                throw new InvalidOperationException("Repeated menu installation changed ownership.");
            var count = 0;
            foreach (var root in scene.GetRootGameObjects()) count += root.GetComponentsInChildren<EventSystem>(true).Length;
            if (count != 1) throw new InvalidOperationException("Production requires one EventSystem.");
            Debug.Log("MENU PRODUCTION VALIDATION PASSED: production routing, installed closed menu, idempotent installation, one EventSystem, canonical prefab and input action.");
        }

        // Disposable GUI-only scene and render targets; never opens/saves gameplay content.
        public static void CaptureFromCli()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var output = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "MenuCaptures"); Directory.CreateDirectory(output);
            var oldQuality = QualitySettings.renderPipeline; var oldDefault = GraphicsSettings.defaultRenderPipeline;
            var renderer = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Project/Settings/Rendering/URP/Renderer3D.asset"));
            renderer.rendererFeatures.Clear();
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            try
            {
                QualitySettings.renderPipeline = pipeline; GraphicsSettings.defaultRenderPipeline = pipeline;
                foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(1024,768) })
                foreach (var panel in new[] { "Root", "Settings", "Setup", "Dialog" })
                foreach (var large in new[] { false, true }) Capture(output, size, panel, large);
            }
            finally { QualitySettings.renderPipeline = oldQuality; GraphicsSettings.defaultRenderPipeline = oldDefault; UnityEngine.Object.DestroyImmediate(pipeline); UnityEngine.Object.DestroyImmediate(renderer); }
        }

        private static void Capture(string output, Vector2Int size, string panel, bool large)
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DMenuPrefabAuthoring.PrefabPath));
            var eventRoot = new GameObject("Capture EventSystem", typeof(EventSystem));
            var cameraRoot = new GameObject("Menu Capture Camera", typeof(Camera));
            var camera = cameraRoot.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = size.y * 0.5f;
            camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(80, 57, 39, 255);
            var target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
            root.GetComponent<CanvasScaler>().enabled = false; canvas.scaleFactor = Mathf.Min(size.x / 1920f, size.y / 1080f);
            var view = root.GetComponent<TopDown3DMenuView>(); view.Initialize(eventRoot.GetComponent<EventSystem>());
            view.ApplySettings(new TopDown3DMenuSettings { largeText = large, reducedMotion = true });
            view.Show(panel, panel == "Root" ? "Resume" : panel == "Setup" ? "Slot0" : panel == "Dialog" ? "DialogCancel" : "TextSize");
            if (panel == "Setup")
            {
                var page = new TopDown3DRadialPage { name = "A full eight direction page", slots = new TopDown3DRadialCommand[8] };
                for (var i = 0; i < 8; i++) page.slots[i] = (TopDown3DRadialCommand)(i % 5);
                view.DrawSlots(page, 0); view.SetPageName("A full eight direction"); view.SetText("PageNumber", "A full eight direction\n4 / 4");
                view.SetText("SelectedDirection", "Selected: No action"); view.SetActive("Advanced", false);
            }
            if (panel == "Dialog") { view.SetText("DialogTitle", "Exit game?"); view.SetText("DialogMessage", "Current session progress will not be saved."); view.SetText("DialogCancel", "Keep Playing"); view.SetText("DialogAccept", "Exit Game"); }
            root.SetActive(true); root.GetComponent<CanvasGroup>().alpha = 1;
            view.SetText("Hints", "Select     Back");
            var focus = panel == "Root" ? "Resume" : panel == "Setup" ? "Slot0" : panel == "Dialog" ? "DialogCancel" : "TextSize";
            eventRoot.GetComponent<EventSystem>().SetSelectedGameObject(null);
            foreach (var button in root.GetComponentsInChildren<Button>()) if (button.name == focus) eventRoot.GetComponent<EventSystem>().SetSelectedGameObject(button.gameObject);
            Canvas.ForceUpdateCanvases(); view.ReflowForViewport(size.x, size.y); Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(output, $"{panel}-{size.x}x{size.y}-{(large ? "large" : "normal")}.png"), texture.EncodeToPNG());
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture); camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(eventRoot); UnityEngine.Object.DestroyImmediate(cameraRoot);
        }
    }
}
