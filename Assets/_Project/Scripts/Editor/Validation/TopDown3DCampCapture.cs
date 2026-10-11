using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Deterministic art capture in an isolated candidate; never opens the user's project in Play Mode.</summary>
    public static class TopDown3DCampCapture
    {
        public static void CaptureAssetsFromCli()
        {
            if (!Application.dataPath.Replace('\\', '/').Contains("/BooterBigArmValidation/"))
                throw new InvalidOperationException("Camp capture must run in the isolated validation checkout.");
            var settings = TopDown3DCampSettings.Load();
            var study = EditorSceneManager.OpenScene(TopDown3DCampAssetBuilder.StudyPath, OpenSceneMode.Single);
            var driver = study.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<TopDown3DPlayerAnimationDriver>(true)).Single();
            var model = (GameObject)new SerializedObject(driver).FindProperty("humanoidPrefab").objectReferenceValue;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(.45f, .4f, .35f); RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            var stage = GameObject.CreatePrimitive(PrimitiveType.Cube); stage.transform.position = new Vector3(0, -.1f, 0);
            stage.transform.localScale = new Vector3(15, .2f, 15);
            var earth = new Material(settings.previewMaterial); earth.SetColor("_BaseColor", new Color(.24f, .15f, .09f)); stage.GetComponent<Renderer>().sharedMaterial = earth;
            var shade = UnityEngine.Object.Instantiate(settings.deployedPrefab); shade.transform.position = new Vector3(0, 0, -1.2f);
            var sun = new GameObject("Review sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(12, -40, 0); sun.shadows = LightShadows.Soft;
            var camera = new GameObject("Review camera").AddComponent<Camera>(); camera.fieldOfView = 45;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.2f, .11f, .075f);
            camera.transform.position = new Vector3(4.5f, 4, -6); camera.transform.LookAt(new Vector3(0, .7f, .1f));
            var body = UnityEngine.Object.Instantiate(model); body.transform.localScale = Vector3.one * driver.VisualScale;
            body.GetComponentInChildren<Animator>().applyRootMotion = false;
            var cloak = UnityEngine.Object.Instantiate(settings.wornCloakPrefab, body.transform, false);
            var blanket = UnityEngine.Object.Instantiate(settings.blanketPrefab); blanket.SetActive(false);
            var packed = UnityEngine.Object.Instantiate(settings.packedPrefab); packed.transform.position = new Vector3(2.4f, 0, -1);
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(body, settings.wakeClip, .2f); AnimationMode.EndSampling();
                Render(camera, "camp-standing.png");
                cloak.SetActive(false); body.transform.position = new Vector3(-1.05f, .35f, .2f); body.transform.rotation = Quaternion.Euler(0, 0, -90);
                AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(body, settings.sleepClip, .5f); AnimationMode.EndSampling();
                blanket.SetActive(true); blanket.transform.position = new Vector3(0, .2f, .2f); blanket.transform.rotation = Quaternion.Euler(0, 90, 0);
                blanket.transform.localScale = Vector3.one * driver.VisualScale;
                Render(camera, "camp-sleeping.png");
                camera.transform.position = new Vector3(-4.5f, 4, 6); camera.transform.LookAt(new Vector3(0, .5f, 0));
                Render(camera, "camp-sleeping-rear.png");
            }
            finally { AnimationMode.StopAnimationMode(); }
            Debug.Log("CAMP ART CAPTURE COMPLETE: standing, sleeping, and rear views. No gameplay smoke test or scene save.");
        }

        internal static void Render(Camera camera, string filename)
        {
            var target = RenderTexture.GetTemporary(1200, 800, 24, RenderTextureFormat.ARGB32);
            var prior = RenderTexture.active; camera.targetTexture = target;
            try
            {
                camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(1200, 800, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs/CampCapture"); File.WriteAllBytes("Logs/CampCapture/" + filename, image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally { camera.targetTexture = null; RenderTexture.active = prior; RenderTexture.ReleaseTemporary(target); }
        }
    }
}
