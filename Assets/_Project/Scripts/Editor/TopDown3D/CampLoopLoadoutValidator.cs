using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BooterBigArm.Editor
{
    public static class CampLoopLoadoutValidator
    {
        public const string ProfilePath = "Assets/_Project/Settings/Items/CampLoopLoadout/Resources/CampLoopLoadoutProfile.asset";
        public static void ValidateFromCli()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DCampLoopLoadoutProfile>(ProfilePath);
            if (profile == null || profile.sourceCatalog == null || !AssetDatabase.GetAssetPath(profile.sourceCatalog).StartsWith("Assets/_Project/Settings/Studies/", StringComparison.Ordinal))
                throw new InvalidOperationException("Loadout must target the private study catalog, never production.");
            var catalog = UnityEngine.Object.Instantiate(profile.sourceCatalog);
            try
            {
                catalog.ApplyLoadoutProfile(profile);
                if (!catalog.TryValidate(out var reason)) throw new InvalidOperationException(reason);
                var equipment = catalog.Definitions.Where(item => item.IsEquipment).ToArray();
                if (equipment.Length != 6 || equipment.Select(item => item.EquipmentSlot).Distinct().Count() != 6)
                    throw new InvalidOperationException("Exactly six unique equipment definitions required.");
                var state = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 10, 0, true));
                foreach (var item in equipment)
                    if (!state.TryAdd(new TopDown3DItemAmount(item.ItemId, 1)).Succeeded) throw new InvalidOperationException("Equipment fixture did not fit.");
                if (!state.TryAdd(new TopDown3DItemAmount("tool.popup_camp", 1)).Succeeded || state.Slots[6].ItemId != "tool.popup_camp")
                    throw new InvalidOperationException("Portable camp must occupy a supply pocket.");
                var production = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
                if (production.HasEquipmentDefinitions || new TopDown3DInventoryState(production, 16).CaptureSnapshot().Version != 1)
                    throw new InvalidOperationException("Production catalog or snapshot contract changed.");
                Debug.Log("CAMP_LOADOUT_VALIDATION_PASS: six strict equipment slots, four supply pockets, private profile, production v1 preserved.");
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        public static void CaptureFromCli()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Capture only in an isolated background candidate.");
            ValidateFromCli();
            var priorPipeline = GraphicsSettings.defaultRenderPipeline; var priorQuality = QualitySettings.renderPipeline;
            var scene = EditorSceneManager.NewPreviewScene();
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DCampLoopLoadoutProfile>(ProfilePath);
            var catalog = UnityEngine.Object.Instantiate(profile.sourceCatalog); catalog.ApplyLoadoutProfile(profile);
            try
            {
                // In-memory capture pipeline only; no serialized settings or scene writes.
                GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null;
                var cameraObject = new GameObject("UI capture camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 540;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                camera.transform.position = new Vector3(0, 0, -10);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .08f, .06f);
                camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                var canvasObject = new GameObject("Loadout capture", typeof(RectTransform)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var view = canvasObject.AddComponent<TopDown3DInventoryCanvas>();
                view.InitializeDual(10, 12, _ => { }, _ => { }, _ => { }, _ => { }, () => { }); view.ConfigureLoadout(true);
                var kit = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 10, 0, true));
                foreach (var item in catalog.Definitions.Where(item => item.IsEquipment)) kit.TryAdd(new TopDown3DItemAmount(item.ItemId, 1));
                kit.TryAdd(new TopDown3DItemAmount("tool.popup_camp", 1));
                for (var i = 0; i < 10; i++)
                {
                    var slot = kit.Slots[i]; view.SlotViews[i].Refresh(slot, slot.IsEmpty ? null : catalog.GetRequiredDefinition(slot.ItemId), false);
                    if (i < 6) view.SlotViews[i].SetLoadoutCaption(TopDown3DLoadoutLayout.Label(i) + "\n" + (i == 1 ? "72.4 / 100 algae" : "Equipped"));
                }
                var cargo = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 12, 92));
                cargo.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 12));
                cargo.TryAdd(new TopDown3DItemAmount("resource.airborne_dust", 36));
                for (var i = 0; i < 12; i++) view.CargoSlotViews[i].Refresh(cargo.Slots[i], cargo.Slots[i].IsEmpty ? null : catalog.GetRequiredDefinition(cargo.Slots[i].ItemId), false);
                view.SetDetails("Algae canister", "Attached personal reserve\nEat · Drink · Treat\nDoes not restore Reserve", "6 / 6 EQUIPPED · 1 / 4 SUPPLIES");
                view.SetCargoSummary("WITHIN REACH · 2 / 12 MOUNTS\n16.8 / 92 MASS · COMFORTABLE");
                view.ConfigureRadialCustomization(() => { });
                view.ConfigureTrialControls(() => { }, true, false, () => { }, false, "ATTACHED EQUIPMENT", () => { }, true);
                view.SetControlHints("A: inspect / confirm    X: transfer / fit    LB / RB: switch owner\nB: cancel / back    Y: close"); view.SetVisible(true);
                var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
                var canvasRect = canvasObject.GetComponent<RectTransform>(); canvasRect.sizeDelta = new Vector2(1920, 1080);
                canvasRect.localScale = Vector3.one; canvasRect.position = Vector3.zero;
                canvasObject.GetComponent<CanvasScaler>().enabled = false; canvas.scaleFactor = 1;
                Capture(camera, canvas, "loadout-1920.png", 1920, 1080);
                Capture(camera, canvas, "loadout-1280.png", 1280, 720);
                Debug.Log("CAMP_LOADOUT_CAPTURE_PASS");
            }
            finally
            { UnityEngine.Object.DestroyImmediate(catalog); EditorSceneManager.ClosePreviewScene(scene); GraphicsSettings.defaultRenderPipeline = priorPipeline; QualitySettings.renderPipeline = priorQuality; }
        }
        private static void Capture(Camera camera, Canvas canvas, string name, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 24);
            var prior = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                canvas.scaleFactor = 1;
                foreach (var graphic in canvas.GetComponentsInChildren<TopDown3DEquipmentSilhouette>(true))
                    graphic.Rebuild(CanvasUpdate.PreRender);
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32(); var first = pixels[0];
                if (!pixels.Any(pixel => Math.Abs(pixel.r - first.r) + Math.Abs(pixel.g - first.g) + Math.Abs(pixel.b - first.b) > 100))
                    throw new InvalidOperationException("Blank UI capture cannot establish layout proof.");
                Directory.CreateDirectory("Logs/LoadoutCaptures"); File.WriteAllBytes("Logs/LoadoutCaptures/" + name, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally { camera.targetTexture = null; RenderTexture.active = prior; RenderTexture.ReleaseTemporary(target); }
        }
    }
}
