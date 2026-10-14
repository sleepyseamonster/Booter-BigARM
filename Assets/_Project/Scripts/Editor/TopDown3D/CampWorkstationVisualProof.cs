using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    public static class CampWorkstationVisualProof
    {
        public static void CaptureFromCli()
        {
            if (!Application.isBatchMode || Application.isPlaying) throw new InvalidOperationException("Background Edit Mode capture only.");
            var scene = EditorSceneManager.NewPreviewScene();
            var prior = GraphicsSettings.defaultRenderPipeline; var priorQuality = QualitySettings.renderPipeline;
            try
            {
                GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null;
                var cameraObject = new GameObject("Workstation UI proof camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 540;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene); camera.transform.position = new Vector3(0, 0, -10);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .09f, .06f); camera.farClipPlane = 100;
                var owner = new GameObject("Workstation canvas proof", typeof(RectTransform)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, scene);
                var view = owner.AddComponent<TopDown3DCampWorkstationCanvas>(); view.Initialize(_ => { }, _ => { });
                var canvas = owner.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                foreach (var type in new[] { "workbench", "refinery", "chest" })
                {
                    var labels = Enumerable.Range(0, 24).Select(i => i == 0 ? "Ironstone Ore x4" : i == 1 && type != "workbench" ? "Canister 13/100" : "Empty").ToArray();
                    view.Present(type == "workbench" ? "CAMP WORKBENCH" : type == "refinery" ? "DUST REFINERY" : "CAMP CHEST",
                        type == "workbench" ? "Empty Arc Battery\nIronstone Ore: 4/4\nOutput: Booter supply pocket" : type == "refinery"
                            ? "Canister: 13/100 dust\nBattery: empty battery\nCost: 10 dust per battery" : "Local camp storage\nResources stay with Legger.\nEquipment and batteries may transfer to legal destinations.",
                        "Choose a source and an item. Contents stay in their physical owner until an action succeeds.\n[B] Back", labels, 1, true);
                    foreach (var id in new[] { "Craft", "Insert", "Refine", "Battery", "Canister", "Deploy", "Transfer", "Reset", "Destination", "Close" })
                        view.SetCommand(id, true, id == "Close" || id == "Reset" || id == "Destination" || type == "workbench" && id == "Craft"
                            || type == "refinery" && (id == "Insert" || id == "Refine" || id == "Battery" || id == "Canister")
                            || type == "chest" && (id == "Deploy" || id == "Transfer"));
                    view.SetCommand("Source", true, true, "SOURCE: CAMP CHEST"); view.SetVisible(true); view.SetInputReady(true);
                    view.SetLargeText(false); Capture(camera, canvas, type + "-1920.png", 1920, 1080);
                    view.SetLargeText(true); Capture(camera, canvas, type + "-1280-large.png", 1280, 720);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); GraphicsSettings.defaultRenderPipeline = prior; QualitySettings.renderPipeline = priorQuality; }
        }
        private static void Capture(Camera camera, Canvas canvas, string name, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 24); var prior = RenderTexture.active;
            try
            {
                camera.targetTexture = target; canvas.scaleFactor = 1; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32(); if (!pixels.Any(p => Math.Abs(p.r-pixels[0].r)+Math.Abs(p.g-pixels[0].g)+Math.Abs(p.b-pixels[0].b) > 100))
                    throw new InvalidOperationException("Blank workstation capture.");
                Directory.CreateDirectory("Logs/WorkstationCaptures"); File.WriteAllBytes("Logs/WorkstationCaptures/" + name, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            }
            finally { camera.targetTexture = null; RenderTexture.active = prior; RenderTexture.ReleaseTemporary(target); }
        }
    }
}
