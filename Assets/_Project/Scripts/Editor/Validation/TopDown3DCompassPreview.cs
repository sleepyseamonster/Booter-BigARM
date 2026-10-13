using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace BooterBigArm.Editor
{
    public static class TopDown3DCompassPreview
    {
        // Static UI geometry only: no production scene, Play Mode, actor simulation, or scene save.
        public static void CaptureFromCli()
        {
            if (!Application.dataPath.Replace('\\', '/').Contains("/BooterBigArmValidation/")) throw new InvalidOperationException("Use an isolated candidate");
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var renderer = new UnityEditor.SerializedObject(pipeline).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
            var features = renderer.rendererFeatures.Select(feature => feature.isActive).ToArray();
            foreach (var feature in renderer.rendererFeatures) feature.SetActive(false);
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Static UI camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .10f, .075f);
                var player = new GameObject("Inactive UI sources"); player.SetActive(false);
                player.AddComponent<TopDown3DPlayerMotor>();
                var input = player.AddComponent<TopDown3DInputRouter>();
                var vitals = player.AddComponent<TopDown3DSurvivalVitals>();
                var state = player.AddComponent<TopDown3DAlgaeCanisterState>(); state.Configure(TopDown3DAlgaeCanisterSettings.Load());
                var consumption = player.AddComponent<TopDown3DAlgaeConsumptionController>(); consumption.Configure(input, vitals, state);
                var pad = TopDown3DActionDpadHud.TryInstallForScene(scene);
                TopDown3DAlgaeCanisterHud.TryInstallForScene(scene, consumption);
                var compass = TopDown3DCompassHud.Install(pad);
                var owner = pad.GetComponentInParent<TopDown3DGameHudCanvas>();
                owner.Scaler.enabled = false;
                owner.Canvas.renderMode = RenderMode.ScreenSpaceCamera; owner.Canvas.worldCamera = camera; owner.Canvas.planeDistance = 1;
                compass.Graphic.SetState(0, true, 90, true, false);
                Capture(camera, owner, pad, 1920, 1080, "north-facing.png");
                compass.Graphic.SetState(90, true, 0, true, false);
                Capture(camera, owner, pad, 1280, 720, "east-facing.png");
                compass.Graphic.SetState(180, true, 0, true, true);
                Capture(camera, owner, pad, 1024, 768, "at-camp.png");
                compass.Graphic.SetState(0, true, 0, true, false);
                Capture(camera, owner, pad, 960, 540, "coincident-north.png");
                Debug.Log("COMPASS STATIC UI PREVIEWS COMPLETE; no gameplay smoke test or scene save");
            }
            finally { for (var i = 0; i < features.Length; i++) renderer.rendererFeatures[i].SetActive(features[i]); }
        }

        private static void Capture(Camera camera, TopDown3DGameHudCanvas owner, TopDown3DActionDpadHud pad, int width, int height, string name)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                owner.Canvas.scaleFactor = Mathf.Sqrt((width / 1920f) * (height / 1080f));
                ((RectTransform)pad.transform).anchoredPosition = Vector2.one * pad.SafeAreaMargin + TopDown3DCompassLayout.ExtraInset(pad.Size);
                foreach (var text in owner.GetComponentsInChildren<Text>())
                { text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle); text.SetAllDirty(); }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs/CompassPreview"); File.WriteAllBytes("Logs/CompassPreview/" + name, image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
                var scale = owner.Canvas.scaleFactor;
                var croppedWidth = Mathf.Min(width, Mathf.CeilToInt((pad.SafeAreaMargin + TopDown3DCompassLayout.ExtraInset(pad.Size).x + pad.DetailsLeft + 247) * scale));
                var croppedHeight = Mathf.Min(height, Mathf.CeilToInt((pad.SafeAreaMargin + TopDown3DCompassLayout.ExtraInset(pad.Size).y + pad.Size * .5f + TopDown3DCompassLayout.OuterRadius(pad.Size) + 12) * scale));
                var hudImage = new Texture2D(croppedWidth, croppedHeight, TextureFormat.RGB24, false);
                hudImage.ReadPixels(new Rect(0, 0, croppedWidth, croppedHeight), 0, 0); hudImage.Apply();
                File.WriteAllBytes("Logs/CompassPreview/" + Path.GetFileNameWithoutExtension(name) + "-hud.png", hudImage.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(hudImage);
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }
    }
}
