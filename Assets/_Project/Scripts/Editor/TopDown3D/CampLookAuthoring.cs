using System;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

namespace BooterBigArm.Editor
{
    public static class CampLookAuthoring
    {
        [Serializable] private sealed class PreviewProof
        {
            public bool authoringCameraRestored, sourceSkyUnchanged, frozenCycle, skyVisible, opticsVisible;
            public float skyMean, opticsMeanDifference;
        }
        public static void CaptureFromCli()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Reference/CampLoopTesting.unity");
            var look = UnityEngine.Object.FindFirstObjectByType<TopDown3DCampLook>();
            if (look == null) throw new InvalidOperationException("Install the Camp Look rig first.");
            var view = ScriptableObject.CreateInstance<SceneView>();
            var camera = view.camera;
            var target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGBHalf);
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false, true);
            var previous = RenderTexture.active;
            TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(look.gameCamera, out var profile);
            var sourceMaterial = look.sun.EnvironmentModules != null ? look.sun.EnvironmentModules.sky?.profile?.material : profile != null ? profile.skyMaterial : null;
            if (sourceMaterial == null) throw new InvalidOperationException("The selected look has no valid source sky.");
            string source = EditorJsonUtility.ToJson(sourceMaterial);
            float cycle = look.sun.Cycle01;
            var proof = new PreviewProof();
            const string output = "Logs/CampLookPreview";
            Directory.CreateDirectory(output);
            try
            {
                CampLookEditorPreview.Refresh();
                camera.enabled = false; camera.targetTexture = target; camera.aspect = 16f / 9f;
                camera.fieldOfView = look.gameCamera.fieldOfView;
                camera.nearClipPlane = .1f; camera.farClipPlane = look.gameCamera.farClipPlane;
                camera.clearFlags = CameraClearFlags.Skybox; camera.allowHDR = true;
                camera.transform.SetPositionAndRotation(look.gameCamera.transform.position, Quaternion.LookRotation(look.sun.DirectionToSun));
                Color[] Capture(string name, bool effects)
                {
                    look.previewSceneViewEffects = effects;
                    CampLookEditorPreview.Refresh();
                    try { camera.Render(); } finally { CampLookEditorPreview.EndCamera(default, camera); }
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
                    var pixels = image.GetPixels(); var display = new Color[pixels.Length];
                    for (int i = 0; i < pixels.Length; i++) display[i] = pixels[i].gamma;
                    image.SetPixels(display); image.Apply();
                    File.WriteAllBytes(output + "/" + name + ".png", image.EncodeToPNG());
                    return pixels;
                }
                var bare = Capture("Scene-sky-and-sun", false);
                var optics = Capture("Scene-sky-and-sun-optics", true);
                double mean = 0, difference = 0;
                for (int i = 0; i < bare.Length; i++)
                {
                    mean += bare[i].r + bare[i].g + bare[i].b;
                    difference += Mathf.Abs(bare[i].r - optics[i].r) + Mathf.Abs(bare[i].g - optics[i].g) + Mathf.Abs(bare[i].b - optics[i].b);
                }
                proof.skyMean = (float)(mean / (bare.Length * 3));
                proof.opticsMeanDifference = (float)(difference / (bare.Length * 3));
                proof.skyVisible = proof.skyMean > .01f;
                proof.opticsVisible = proof.opticsMeanDifference > .0001f;
                proof.authoringCameraRestored = !TopDown3DSceneRenderingOverride.IsAuthoringCamera(camera);
                proof.sourceSkyUnchanged = source == EditorJsonUtility.ToJson(sourceMaterial);
                proof.frozenCycle = cycle == look.sun.Cycle01;
            }
            finally
            {
                look.previewSceneViewEffects = true;
                CampLookEditorPreview.Release(); camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(view); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
            File.WriteAllText(output + "/proof.json", JsonUtility.ToJson(proof, true));
            if (!proof.skyVisible || !proof.opticsVisible || !proof.frozenCycle || !proof.sourceSkyUnchanged || !proof.authoringCameraRestored)
                throw new InvalidOperationException("Scene camera preview proof failed.");
            Debug.Log("CAMP_LOOK_PREVIEW_OK: actual Scene View camera renders the panorama/sun and selected optics in Edit Mode; restored state.");
        }

        public static void InstallFromCli()
        {
            if (!Application.dataPath.Replace('\\', '/').Contains("BooterBigArmValidation/CampOptics"))
                throw new InvalidOperationException("Camp look authoring requires the isolated optics candidate.");
            CampLookEditorPreview.Release();
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Reference/CampLoopTesting.unity");
            var camera = TopDown3DSceneRenderingOverride.AtmosphereCamera(scene);
            if (camera == null || !TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out var profile))
                throw new InvalidOperationException("Requires the existing unique camp rendering camera.");
            PerpetualTwilightSun sun = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<PerpetualTwilightSun>(true))
                { if (sun != null) throw new InvalidOperationException("Duplicate camp suns."); sun = candidate; }
            if (sun == null) throw new InvalidOperationException("Missing existing camp sun.");
            var look = camera.GetComponentInParent<TopDown3DCampLook>();
            if (look == null)
            {
                var root = new GameObject("Camp Look");
                look = root.AddComponent<TopDown3DCampLook>();
                var position = camera.transform.position; var rotation = camera.transform.rotation;
                camera.transform.SetParent(root.transform, true);
                sun.transform.SetParent(root.transform, true);
                if (Vector3.Distance(position, camera.transform.position) > .0001f || Quaternion.Angle(rotation, camera.transform.rotation) > .001f)
                    throw new InvalidOperationException("Camera world pose changed during organization.");
            }
            look.gameCamera = camera; look.sun = sun;
            look.previewInEditor = true; look.previewSceneViewEffects = true;
            sun.RefreshEditorPresentation();
            var sky = RenderSettings.ambientSkyColor; var equator = RenderSettings.ambientEquatorColor;
            var ground = RenderSettings.ambientGroundColor; var shadow = RenderSettings.subtractiveShadowColor;
            var light = sun.GetComponent<Light>(); var color = light.color; float intensity = light.intensity;
            var sunRotation = sun.transform.rotation; float reflection = RenderSettings.reflectionIntensity;
            sun.EndStudyPreview();
            // Persist authored source assets and the frozen authored lighting state, never
            // owned preview materials. Camera-local transient state restores before saving.
            RenderSettings.skybox = profile.skyMaterial;
            RenderSettings.sun = light; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground; RenderSettings.subtractiveShadowColor = shadow;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = profile.sunlessReflection;
            RenderSettings.reflectionIntensity = reflection;
            light.color = color; light.intensity = intensity; sun.transform.rotation = sunRotation;
            EditorSceneManager.SaveScene(scene);
            Debug.Log("CAMP_LOOK_AUTHORING_OK: existing camera and sun grouped; persistent source sky/reflection; frozen editor preview references.");
        }
    }
}
