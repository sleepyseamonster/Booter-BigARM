using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class GroundTextureToolVerification
    {
        public const string DemoScene = "Assets/_Project/Scenes/Reference/TextureAuthoringDemo.unity";
        public const string DemoAssets = "Assets/_Project/Art/Environment/Ground/Studies/TextureAuthoringDemo";
        [Serializable] private sealed class Proof
        {
            public string graphicsAPI, scene = DemoScene;
            public int shaderVariants, paintedTiles = 2, stickers = 2;
            public bool heightsPreserved;
            public string limit = "Isolated synthetic authoring/render fixture. No gameplay smoke test, production painting, Player profiling or hands-on acceptance.";
        }
        public static void BuildAndCapture()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run verification in an isolated background batchmode copy.");
            if (File.Exists(DemoScene) || Directory.Exists(DemoAssets)) throw new InvalidOperationException("Preserve the existing demo; this builder does not overwrite it.");
            GroundTextureTool.InstallDefaults();
            var shader = Shader.Find("BooterBigArm/Ground Texture Sticker");
            var variants = new ShaderVariantCollection();
            foreach (var keywords in new[] { Array.Empty<string>(), new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT" },
                new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT", "_ADDITIONAL_LIGHTS", "_ADDITIONAL_LIGHT_SHADOWS", "FOG_LINEAR" } })
                variants.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.ScriptableRenderPipeline, keywords));
            variants.WarmUp();
            var proof = new Proof { graphicsAPI = SystemInfo.graphicsDeviceType.ToString(), shaderVariants = variants.variantCount, heightsPreserved = true };
            // Batch-only isolated copy: use a saved reference as the neutral host, avoiding untitled-scene modals.
            var previous = EditorSceneManager.OpenScene(FixedTerrainDressingStudyBuilder.ScenePath, OpenSceneMode.Single);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            RenderTexture target = null; Texture2D pixels = null; var oldActive = RenderTexture.active;
            try
            {
                EnsureFolder(DemoAssets);
                Texture2D Texture(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Project/Art/Environment/Ground/SandDirt/{file}.png");
                var sand = GroundTextureTool.CreateLayer(Texture("BrokenWorldSweptSandAlbedo"));
                var gravel = GroundTextureTool.CreateLayer(Texture("BrokenWorldGravelAlbedo"));
                var rocky = GroundTextureTool.CreateLayer(Texture("BrokenWorldMixedRockyAlbedo"));
                for (int tile = 0; tile < 2; tile++)
                {
                    var data = new TerrainData { name = $"TextureDemo_{tile}", heightmapResolution = 65, alphamapResolution = 64,
                        size = new Vector3(16, 4, 16), terrainLayers = new[] { sand } };
                    var heights = new float[65, 65];
                    for (int z = 0; z < 65; z++) for (int x = 0; x < 65; x++)
                    {
                        float wx = (tile - 1) * 16 + x / 4f;
                        heights[z, x] = .14f + .04f * Mathf.Sin(wx * .25f) + .025f * Mathf.Cos(z / 4f * .3f);
                    }
                    data.SetHeights(0, 0, heights);
                    var weights = new float[64, 64, 1]; for (int z = 0; z < 64; z++) for (int x = 0; x < 64; x++) weights[z, x, 0] = 1;
                    data.SetAlphamaps(0, 0, weights);
                    AssetDatabase.CreateAsset(data, $"{DemoAssets}/TextureDemo_{tile}.asset");
                    var terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
                    SceneManager.MoveGameObjectToScene(terrain.gameObject, scene); terrain.transform.position = new Vector3((tile - 1) * 16, 0, 0);
                    Physics.SyncTransforms(); var before = data.GetHeights(0, 0, 65, 65);
                    GroundTextureAuthoring.PaintTerrain(terrain, gravel, new Vector3(0, 0, 8), 5, .9f, .7f);
                    GroundTextureAuthoring.PaintTerrain(terrain, rocky, new Vector3(7, 0, 8), 4, .95f, .6f);
                    var after = data.GetHeights(0, 0, 65, 65);
                    for (int z = 0; z < 65; z++) for (int x = 0; x < 65; x++) proof.heightsPreserved &= before[z, x] == after[z, x];
                    AssetDatabase.SaveAssetIfDirty(data);
                }
                GroundTextureAuthoring.Create(scene, new Vector3(-9, 0, 8), Texture("BrokenWorldRockyMidTransitionAlbedo"), new Vector2(7, 6), 18);
                GroundTextureAuthoring.Create(scene, new Vector3(-7, 0, 5), Texture("BrokenWorldGravelAlbedo"), new Vector2(5, 4), -25);
                var sunObject = new GameObject("Authoring preview sunlight", typeof(Light)); SceneManager.MoveGameObjectToScene(sunObject, scene);
                sunObject.transform.rotation = Quaternion.Euler(38, -35, 0);
                var sun = sunObject.GetComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.5f; sun.shadows = LightShadows.Soft;
                var cameraObject = new GameObject("Authoring preview camera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.10f,.08f,.065f);
                var rotation = Quaternion.Euler(60, 0, 0); camera.transform.SetPositionAndRotation(new Vector3(0, 0, 8) - rotation * Vector3.forward * 28, rotation);
                camera.fieldOfView = 48; camera.farClipPlane = 100;
                if (!EditorSceneManager.SaveScene(scene, DemoScene)) throw new IOException("Could not save texture-authoring demo.");
                // Additive ordinary scenes share the default culling mask. Remove the isolated host
                // before rendering so its geometry and sunlight cannot contaminate this fixture.
                EditorSceneManager.CloseScene(previous, true);
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
                camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                string folder = "Docs/Evidence/WorldCreator/GroundTextureTool"; Directory.CreateDirectory(folder);
                File.WriteAllBytes(folder + "/authoring-demo.png", pixels.EncodeToPNG());
                var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
                if (errors.Length > 0 || !proof.heightsPreserved) throw new InvalidOperationException("Shader or height-preservation gate failed.");
                File.WriteAllText(folder + "/render-proof-2026-10-09.json", JsonUtility.ToJson(proof, true) + "\n");
            }
            finally
            {
                RenderTexture.active = oldActive;
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(variants);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        public static void CaptureSavedDemo()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use an isolated background batchmode copy.");
            var scene = EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            var previous = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                target.Create(); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Docs/Evidence/WorldCreator/GroundTextureTool");
                File.WriteAllBytes("Docs/Evidence/WorldCreator/GroundTextureTool/authoring-demo.png", pixels.EncodeToPNG());
                var errors = ShaderUtil.GetShaderMessages(Shader.Find("BooterBigArm/Ground Texture Sticker"))
                    .Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
                if (errors.Length > 0) throw new InvalidOperationException("Sticker shader failed during rendering.");
            }
            finally
            { camera.targetTexture = null; RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels); }
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
