using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Editor
{
    public static class NpcBlockoutPreview
    {
        public static void CaptureFromCli()
        {
            if (!Application.isBatchMode || Application.isPlaying) throw new InvalidOperationException("Static candidate-only background capture.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampLoopNpcAuthoring.PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Missing verified NPC prefab.");
            var actor = UnityEngine.Object.Instantiate(prefab); actor.transform.position = Vector3.zero;
            var camera = new GameObject("Blockout preview camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(2.8f, 2.8f, 3.8f); camera.transform.LookAt(Vector3.up * .55f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f, .055f, .045f);
            camera.orthographic = true; camera.orthographicSize = 1.35f; camera.nearClipPlane = .1f; camera.farClipPlane = 20;
            camera.GetUniversalAdditionalCameraData().SetRenderer(0);
            var light = new GameObject("Static key").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.4f, .35f, .3f);
            var target = RenderTexture.GetTemporary(900, 700, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(900, 700, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 900, 700), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/lager-blockout.png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
            Debug.Log("NPC_STATIC_PREVIEW_PASS: no Play Mode, scene save or foreground activation.");
        }
    }
}
