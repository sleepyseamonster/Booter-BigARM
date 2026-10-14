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
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class CampAtmosphereAuthoring
    {
        public const string Settings = "Assets/_Project/Settings/Studies/CampAtmosphere20261014";
        public const string ContextPrefab = "Assets/_Project/Prefabs/TopDown3D/Studies/CampAtmosphere20261014/TerrainContext.prefab";
        public const string ContextArt = "Assets/_Project/Art/Environment/Sky/CampAtmosphere20261014/Context";
        [Serializable] private sealed class Tile { public string path; public float x, y, z; }
        [Serializable] private sealed class Context { public Tile[] tiles; }

        public static void AuthorAssetsFromCli()
        {
            CampAtmosphereCapture.RequireCandidate();
            if (Directory.Exists(Settings) || File.Exists(ContextPrefab)) throw new InvalidOperationException("Preserve existing study assets; author once.");
            Directory.CreateDirectory(Settings); Directory.CreateDirectory(ContextArt);
            Directory.CreateDirectory(Path.GetDirectoryName(ContextPrefab)); AssetDatabase.Refresh();
            var shader = Shader.Find("BooterBigArm/TopDown3D/Camp Ochre Spherical Sky");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Study sky shader unsupported.");
            foreach (var variant in new[] { "Moderate", "Clear", "Deep" })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environment/Sky/CampAtmosphere20261014/OchreSky" + variant + ".png");
                if (texture == null || texture.width != texture.height * 2) throw new InvalidDataException("2:1 artwork required.");
                var material = new Material(shader) { name = "CampOchreSky" + variant, mainTexture = texture };
                material.SetColor("_SunColor", new Color(1.3f, .65f, .06f));
                material.SetColor("_GroundHazeColor", new Color(.36f, .16f, .055f));
                var initial = PerpetualTwilightSun.EvaluateCycle(.12f, 19, 7, 18, 14);
                material.SetVector("_SunDirection", -(Quaternion.Euler(initial.ElevationDegrees, initial.AzimuthDegrees, 0) * Vector3.forward));
                material.SetFloat("_TwilightBrightness", initial.Brightness01);
                material.SetFloat("_Exposure", Mathf.Lerp(.65f, 1.05f, initial.Brightness01));
                AssetDatabase.CreateAsset(material, Settings + "/Sky" + variant + ".mat");
                var profile = ScriptableObject.CreateInstance<TopDown3DAtmosphereStudyProfile>();
                profile.name = "CampAtmosphere" + variant; profile.skyMaterial = material;
                AssetDatabase.CreateAsset(profile, Settings + "/" + variant + ".asset");
            }
            BuildContext(); AssetDatabase.SaveAssets();
            Debug.Log("CAMP_ATMOSPHERE_ASSETS_PASS: three sibling skies/profiles, render-only native-height context.");
        }

        private static void BuildContext()
        {
            var context = JsonUtility.FromJson<Context>("{\"tiles\":" + File.ReadAllText("Logs/terrain-context.json") + "}");
            var root = new GameObject("Camp Atmosphere Terrain Context");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(.36f, .19f, .095f)); material.SetFloat("_Smoothness", 0);
            AssetDatabase.CreateAsset(material, Settings + "/ContextTerrain.mat");
            var sources = context.tiles.Where(t => !(t.x >= -2304 && t.x < -1536 && t.z >= -1792 && t.z < -1024));
            int tileCount = 0;
            try
            {
                foreach (var group in sources.GroupBy(t => new Vector2Int(Mathf.FloorToInt(t.x / 1024), Mathf.FloorToInt(t.z / 1024))))
                {
                    var vertices = new List<Vector3>(); var indices = new List<int>();
                    var origin = new Vector3(group.Key.x * 1024, -100, group.Key.y * 1024);
                    foreach (var tile in group)
                    {
                        var data = AssetDatabase.LoadAssetAtPath<TerrainData>(tile.path);
                        if (data == null) throw new InvalidDataException("Missing native data: " + tile.path);
                        const int n = 9; int start = vertices.Count;
                        for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
                        {
                            float u = x / (float)(n - 1), v = z / (float)(n - 1);
                            vertices.Add(new Vector3(tile.x + u * data.size.x, tile.y + data.GetInterpolatedHeight(u, v), tile.z + v * data.size.z) - origin);
                        }
                        for (int z = 0; z < n - 1; z++) for (int x = 0; x < n - 1; x++)
                        {
                            int a = start + z * n + x, b = a + n;
                            indices.Add(a); indices.Add(b); indices.Add(a + 1);
                            indices.Add(a + 1); indices.Add(b); indices.Add(b + 1);
                        }
                        tileCount++;
                    }
                    var mesh = new Mesh { name = "Context " + group.Key.x + " " + group.Key.y };
                    mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    var meshPath = ContextArt + "/Sector" + group.Key.x + "_" + group.Key.y + ".asset";
                    AssetDatabase.CreateAsset(mesh, meshPath);
                    var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root.transform);
                    go.transform.position = origin; go.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                }
                PrefabUtility.SaveAsPrefabAsset(root, ContextPrefab);
                File.WriteAllText("Logs/CampAtmosphere/context-asset-report.json", "{\"nativeSourceTiles\":" + tileCount + ",\"samplesPerSide\":9,\"colliders\":0,\"sectors\":" + root.transform.childCount + "}");
            }
            finally { Object.DestroyImmediate(root); }
        }

        public static void InstallFromCli()
        {
            CampAtmosphereCapture.RequireCandidate();
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-campSkyVariant");
            if (index < 0 || index + 1 >= args.Length || !new[] { "Moderate", "Clear", "Deep" }.Contains(args[index + 1]))
                throw new InvalidOperationException("Explicit accepted -campSkyVariant required.");
            var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene, OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single();
            if (TopDown3DSceneRenderingOverride.AtmosphereBindingCount(scene) > 0) throw new InvalidOperationException("Study already installed; preserve its binding.");
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(Settings + "/" + args[index + 1] + ".asset");
            if (profile == null || !profile.IsValid) throw new InvalidDataException("Missing accepted profile.");
            var binding = camera.gameObject.AddComponent<TopDown3DSceneRenderingOverride>(); binding.ConfigureAtmosphere(profile);
            var skybox = camera.gameObject.AddComponent<Skybox>(); skybox.material = profile.skyMaterial;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ContextPrefab);
            if (prefab == null) throw new InvalidDataException("Missing render-only context.");
            ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).name = "Camp Atmosphere Terrain Context";
            var sun = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PerpetualTwilightSun>(true)).Single();
            var preview = new Material(profile.skyMaterial);
            try { sun.ApplyStudyPreview(sun.Cycle01, preview); }
            finally { sun.EndStudyPreview(); Object.DestroyImmediate(preview); }
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("CAMP_ATMOSPHERE_INSTALL_PASS: " + args[index + 1]);
        }

        public static void AuthorReflectionFromCli()
        {
            CampAtmosphereCapture.RequireCandidate();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Project/Settings/Rendering/URP/Renderer3D.asset");
            var enabled = renderer.rendererFeatures.Select(f => f.isActive).ToArray();
            try
            {
                foreach (var feature in renderer.rendererFeatures) feature.SetActive(false);
                var camera = new GameObject("Sunless reflection capture").AddComponent<Camera>();
                camera.enabled = false; camera.scene = scene; camera.clearFlags = CameraClearFlags.Skybox; camera.cullingMask = 0;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                var skybox = camera.gameObject.AddComponent<Skybox>();
                foreach (var variant in new[] { "Moderate", "Clear", "Deep" })
                {
                    var path = Settings + "/Reflection" + variant + ".asset";
                    if (File.Exists(path)) throw new InvalidOperationException("Preserve existing reflection asset.");
                    var profile = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(Settings + "/" + variant + ".asset");
                    var sky = new Material(profile.skyMaterial); sky.SetColor("_SunColor", Color.clear); sky.SetFloat("_Exposure", 1);
                    skybox.material = sky;
                    var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBHalf) {
                        dimension = TextureDimension.Cube, useMipMap = false };
                    var pixels = new Texture2D(128, 128, TextureFormat.RGBAHalf, false, true);
                    var cube = new Cubemap(128, TextureFormat.RGBAHalf, true) { name = "Sunless " + variant + " reflection", filterMode = FilterMode.Trilinear };
                    var old = RenderTexture.active;
                    try
                    {
                        if (!camera.RenderToCubemap(target)) throw new InvalidOperationException("Reflection capture unsupported.");
                        for (int face = 0; face < 6; face++)
                        {
                            Graphics.SetRenderTarget(target, 0, (CubemapFace)face);
                            pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply();
                            cube.SetPixels(pixels.GetPixels(), (CubemapFace)face);
                        }
                        cube.Apply(true, false); AssetDatabase.CreateAsset(cube, path);
                        profile.sunlessReflection = cube; EditorUtility.SetDirty(profile);
                    }
                    finally
                    {
                        RenderTexture.active = old; target.Release(); Object.DestroyImmediate(target);
                        Object.DestroyImmediate(pixels); Object.DestroyImmediate(sky);
                    }
                }
                AssetDatabase.SaveAssets(); Debug.Log("CAMP_ATMOSPHERE_REFLECTION_PASS: three 128px sunless art-derived linear reflection cubemaps; not calibrated HDRI.");
            }
            finally
            {
                for (int i = 0; i < enabled.Length; i++) renderer.rendererFeatures[i].SetActive(enabled[i]);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        public static void ValidateInstalledFromCli()
        {
            CampAtmosphereCapture.RequireCandidate();
            var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single();
            if (!TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out var profile)
                || AssetDatabase.GetAssetPath(profile) != Settings + "/Clear.asset")
                throw new InvalidDataException("Installed camera must select accepted Clear profile exactly once.");
            if (camera.GetComponent<Skybox>()?.material != profile.skyMaterial || profile.sunlessReflection == null)
                throw new InvalidDataException("Installed camera sky/reflection is missing.");
            var context = roots.Single(r => r.name.StartsWith("Camp Atmosphere Terrain Context"));
            if (context.GetComponentsInChildren<Collider>(true).Length != 0
                || context.GetComponentsInChildren<Terrain>(true).Length != 0
                || context.GetComponentsInChildren<MonoBehaviour>(true).Length != 0
                || context.GetComponentsInChildren<MeshRenderer>(true).Length != 95)
                throw new InvalidDataException("Context must retain 95 render-only sectors.");
            if (roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).Count() != 9)
                throw new InvalidDataException("Native camp footprint changed.");
            if (ShaderUtil.GetShaderMessages(profile.skyMaterial.shader).Any(m =>
                m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
                throw new InvalidDataException("Study shader compilation failed.");
            Debug.Log("CAMP_ATMOSPHERE_INSTALLED_PASS: Clear camera binding, sunless reflection, 95 render-only sectors, nine native terrains, shader diagnostics.");
        }
    }
}
