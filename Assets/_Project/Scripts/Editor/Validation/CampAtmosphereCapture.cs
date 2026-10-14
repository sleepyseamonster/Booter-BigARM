using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    /// <summary>Static native-height context references. Never enters Play Mode or saves terrain/scenes.</summary>
    public static class CampAtmosphereCapture
    {
        [Serializable] private sealed class Tile { public string path; public float x, y, z; }
        [Serializable] private sealed class Context { public Tile[] tiles; }
        [Serializable] private sealed class Report
        {
            public string proof = "Static coarse native-height reconstruction, not gameplay or GPU profiling";
            public string graphics; public int tiles; public int resolution = 17;
            public string[] images; public Vector3 referencePosition;
            public float southernTerrainBoundary = -2048;
        }
        public static void CaptureContextFromCli()
        {
            RequireCandidate();
            var context = JsonUtility.FromJson<Context>("{\"tiles\":" + File.ReadAllText("Logs/terrain-context.json") + "}");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var meshes = new List<Mesh>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Project/Settings/Rendering/URP/Renderer3D.asset");
            var features = renderer.rendererFeatures.Select(f => f.isActive).ToArray();
            RenderTexture target = null;
            try
            {
                foreach (var feature in renderer.rendererFeatures) feature.SetActive(false);
                material.SetColor("_BaseColor", new Color(.36f, .19f, .095f));
                material.SetFloat("_Smoothness", 0);
                const int resolution = 17;
                foreach (var tile in context.tiles)
                {
                    var data = AssetDatabase.LoadAssetAtPath<TerrainData>(tile.path);
                    if (data == null) throw new InvalidDataException("Missing context terrain: " + tile.path);
                    var vertices = new Vector3[resolution * resolution];
                    var triangles = new int[(resolution - 1) * (resolution - 1) * 6];
                    for (int z = 0; z < resolution; z++) for (int x = 0; x < resolution; x++)
                    {
                        float u = x / (float)(resolution - 1), v = z / (float)(resolution - 1);
                        vertices[z * resolution + x] = new Vector3(u * data.size.x,
                            data.GetInterpolatedHeight(u, v), v * data.size.z);
                    }
                    var index = 0;
                    for (int z = 0; z < resolution - 1; z++) for (int x = 0; x < resolution - 1; x++)
                    {
                        int a = z * resolution + x, b = a + resolution;
                        triangles[index++] = a; triangles[index++] = b; triangles[index++] = a + 1;
                        triangles[index++] = a + 1; triangles[index++] = b; triangles[index++] = b + 1;
                    }
                    var mesh = new Mesh { name = "Diagnostic native-height context" };
                    mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    meshes.Add(mesh);
                    var go = new GameObject("Context " + tile.x + " " + tile.z, typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.position = new Vector3(tile.x, tile.y, tile.z);
                    go.GetComponent<MeshFilter>().sharedMesh = mesh;
                    go.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
                var light = new GameObject("Diagnostic light").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.1f; light.color = new Color(1f, .72f, .32f);
                light.transform.rotation = Quaternion.Euler(19, 18, 0); light.shadows = LightShadows.None;
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.32f, .20f, .12f);
                RenderSettings.fog = false;
                var camera = new GameObject("Diagnostic context camera").AddComponent<Camera>();
                camera.enabled = false; camera.cameraType = CameraType.Game; camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.55f, .28f, .11f, 1);
                camera.nearClipPlane = .1f; camera.farClipPlane = 8000; camera.fieldOfView = 75;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                camera.transform.position = new Vector3(-1848, -27.08493f, -1498);
                target = new RenderTexture(1536, 864, 24); camera.targetTexture = target;
                Directory.CreateDirectory("Logs/CampAtmosphere");
                var images = new List<string>();
                foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
                {
                    camera.transform.rotation = Quaternion.Euler(0, yaw, 0);
                    camera.Render(); camera.Render();
                    var path = "Logs/CampAtmosphere/context-yaw" + yaw + ".png";
                    SavePng(target, path); images.Add(path);
                }
                File.WriteAllText("Logs/CampAtmosphere/context-report.json", JsonUtility.ToJson(new Report {
                    graphics = SystemInfo.graphicsDeviceType.ToString(), tiles = context.tiles.Length,
                    images = images.ToArray(), referencePosition = camera.transform.position }, true));
                Debug.Log("CAMP_ATMOSPHERE_CONTEXT_PASS: " + context.tiles.Length + " native-height sources; four static views.");
            }
            finally
            {
                for (int i = 0; i < features.Length; i++) renderer.rendererFeatures[i].SetActive(features[i]);
                if (target != null) { target.Release(); Object.DestroyImmediate(target); }
                foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(material);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
        internal static void RequireCandidate()
        {
            if (!Application.isBatchMode || Application.isPlaying || !File.Exists("snapshot-receipt.json"))
                throw new InvalidOperationException("Use a frozen isolated background candidate; never the author checkout.");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("Static rendering requires a graphics device.");
        }
        internal static void SavePng(RenderTexture target, string path)
        {
            var old = RenderTexture.active; var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try { RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
            finally { RenderTexture.active = old; Object.DestroyImmediate(texture); }
        }
    }
}
