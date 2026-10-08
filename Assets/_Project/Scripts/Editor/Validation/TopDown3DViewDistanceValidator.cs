using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    /// <summary>Read-only source inspection and disposable offscreen captures; never enters Play Mode or saves scenes.</summary>
    public static class TopDown3DViewDistanceValidator
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        private static string Output => Path.Combine(Directory.GetCurrentDirectory(), "Logs", "ViewDistanceProduction");

        [Serializable] private sealed class Report
        {
            public bool passed;
            public string error;
            public string editor;
            public string graphics;
            public int terrains;
            public string sceneHash;
            public Capture[] captures;
        }
        [Serializable] private sealed class Capture
        {
            public string preset;
            public string viewpoint;
            public Vector3 cameraPosition;
            public float pitch;
            public float exposure;
            public float distance;
            public bool distanceHaze;
            public double renderAndReadbackMilliseconds;
            public string image;
        }

        public static void ValidateFeatureFromCli()
        {
            TopDown3DMenuProductionValidator.ValidateFromCli();
            ValidateAndCaptureFromCli();
            var report = JsonUtility.FromJson<Report>(File.ReadAllText(Path.Combine(Output, "validation.json")));
            if (!report.passed) throw new InvalidDataException(report.error);
        }

        // Can be scheduled with EditorApplication.delayCall so a long terrain import does not outlive a CLI eval timeout.
        public static void ValidateAndCaptureFromCli()
        {
            Directory.CreateDirectory(Output);
            var report = new Report { editor = Application.unityVersion, graphics = SystemInfo.graphicsDeviceType.ToString() };
            var scene = default(UnityEngine.SceneManagement.Scene);
            var previousSky = RenderSettings.skybox;
            Material sky = null;
            RenderTexture target = null;
            try
            {
                byte[] before = File.ReadAllBytes(ScenePath);
                using (var hash = System.Security.Cryptography.SHA256.Create())
                    report.sceneHash = BitConverter.ToString(hash.ComputeHash(before)).Replace("-", "").ToLowerInvariant();
                scene = EditorSceneManager.OpenPreviewScene(ScenePath);
                var roots = scene.GetRootGameObjects();
                var terrains = roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).ToArray();
                report.terrains = terrains.Length;
                if (terrains.Length == 0 || terrains.Any(t => t.terrainData == null || !t.drawInstanced
                    || t.GetComponent<TerrainCollider>()?.terrainData != t.terrainData))
                    throw new InvalidDataException("Terrain instancing/collider reference contract changed.");
                var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true))
                    .Single(c => c.GetComponent<BadwaterCameraRange>() != null);
                var range = camera.GetComponent<BadwaterCameraRange>();
                // Only the camera-range owner runs in this disposable preview; gameplay remains parked.
                range.enabled = false; range.runInEditMode = true; range.enabled = true;
                if (range.FarClipPlane != BadwaterCameraRange.MaximumDistance)
                    throw new InvalidDataException("Production must retain its serialized Maximum baseline.");
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Project/Settings/Rendering/URP/Renderer3D.asset");
                if (renderer.rendererFeatures.OfType<TopDown3DVolumetricDustFeature>().Count(f => f.isActive && f.VolumetricShader != null) != 1)
                    throw new InvalidDataException("Production requires exactly one atmosphere rendering owner.");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DMenuPrefabAuthoring.PrefabPath);
                if (prefab.GetComponentsInChildren<Button>(true).Count(b => b.name == "ViewDistance") != 1)
                    throw new InvalidDataException("Canonical menu distance row is missing or duplicated.");
                camera.enabled = false; camera.scene = scene; camera.cameraType = CameraType.Game;
                camera.GetUniversalAdditionalCameraData().requiresDepthTexture = true;
                var targetTransform = new SerializedObject(camera.GetComponent<TopDown3DCameraRig>()).FindProperty("target").objectReferenceValue as Transform;
                if (targetTransform == null) throw new InvalidDataException("Production camera target is missing.");
                sky = new Material(Resources.Load<Material>("TopDown3D/MartianPanoramaSky"));
                RenderSettings.skybox = sky;
                target = new RenderTexture(1280, 720, 24); camera.targetTexture = target;
                var captures = new System.Collections.Generic.List<Capture>();
                var ridge = terrains.Select(t => t.transform.position + new Vector3(
                    t.terrainData.size.x * 0.5f, t.terrainData.GetInterpolatedHeight(0.5f, 0.5f), t.terrainData.size.z * 0.5f))
                    .OrderByDescending(p => p.y).First();
                foreach (string viewpoint in new[] { "spawn", "ridge" })
                foreach (float exposure in new[] { 0.65f, 1.05f })
                foreach (float pitch in new[] { 26f, 50f })
                foreach (string preset in new[] { "Maximum", "High", "Medium", "Low" })
                {
                    range.SetPreset(preset);
                    if (preset != "Maximum" && !range.DistanceHazeEnabled)
                        throw new InvalidOperationException("Preview camera-range owner did not activate for distance-haze proof.");
                    sky.SetFloat("_Exposure", exposure);
                    camera.transform.rotation = Quaternion.Euler(pitch, 40f, 0f);
                    var focus = viewpoint == "spawn" ? targetTransform.position : ridge + Vector3.up * 15f;
                    camera.transform.position = focus + Vector3.up * 1.15f - camera.transform.forward * 25f;
                    camera.cameraType = CameraType.Game;
                    camera.Render(); // warm the shader/terrain path before timing the capture
                    camera.cameraType = CameraType.Game;
                    var timer = Stopwatch.StartNew(); camera.Render();
                    var texture = Readback(target); timer.Stop();
                    string image = $"{viewpoint}-{preset}-pitch{pitch}-exposure{exposure}.png";
                    try { File.WriteAllBytes(Path.Combine(Output, image), texture.EncodeToPNG()); }
                    finally { Object.DestroyImmediate(texture); }
                    captures.Add(new Capture { preset = preset, viewpoint = viewpoint, cameraPosition = camera.transform.position,
                        pitch = pitch, exposure = exposure, distance = camera.farClipPlane,
                        distanceHaze = range.DistanceHazeEnabled,
                        renderAndReadbackMilliseconds = timer.Elapsed.TotalMilliseconds, image = image });
                }
                report.captures = captures.ToArray();
                var shader = Shader.Find("BooterBigArm/TopDown3D/Broken World Volumetric Dust");
                if (ShaderUtil.GetShaderMessages(shader).Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
                    throw new InvalidDataException("Atmosphere shader compiler reported an error.");
                if (!before.SequenceEqual(File.ReadAllBytes(ScenePath)))
                    throw new IOException("Production scene changed during validation; revalidate the new candidate.");
                report.passed = true;
                Debug.Log("VIEW_DISTANCE_VALIDATED: saved production wiring, instanced terrain/collider references, shader diagnostics, and 32 offscreen captures. Readback timing is Editor evidence, not Player FPS; Editor draw counters are unavailable for this path.");
            }
            catch (Exception e) { report.error = e.ToString(); Debug.LogError(report.error); }
            finally
            {
                RenderSettings.skybox = previousSky;
                if (target != null) { target.Release(); Object.DestroyImmediate(target); }
                if (sky != null) Object.DestroyImmediate(sky);
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                File.WriteAllText(Path.Combine(Output, "validation.json"), JsonUtility.ToJson(report, true));
            }
        }

        private static Texture2D Readback(RenderTexture target)
        {
            var previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
                return texture;
            }
            catch { Object.DestroyImmediate(texture); throw; }
            finally { RenderTexture.active = previous; }
        }
    }
}
