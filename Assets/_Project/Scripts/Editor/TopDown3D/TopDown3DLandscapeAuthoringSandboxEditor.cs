using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DLandscapeAuthoringSandbox))]
    public sealed class TopDown3DLandscapeAuthoringSandboxEditor : UnityEditor.Editor
    {
        private const string TerrainPreviewRootName = "__Generated Terrain Context";

        [MenuItem("Booter & BigARM/Top Down 3D/Open Landscape Authoring Sandbox")]
        public static void OpenOrCreateSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(LandscapeAuthoringSandboxBuilder.ScenePath);
            if (existing != null)
            {
                EditorSceneManager.OpenScene(LandscapeAuthoringSandboxBuilder.ScenePath, OpenSceneMode.Single);
                var loadedSandbox = UnityEngine.Object.FindAnyObjectByType<TopDown3DLandscapeAuthoringSandbox>();
                if (loadedSandbox != null) BuildTerrainContext(loadedSandbox);
                return;
            }

            LandscapeAuthoringSandboxBuilder.CreateScene();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            var sandbox = (TopDown3DLandscapeAuthoringSandbox)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Terrain Context is a temporary editor view built from the production terrain generator. "
                + "It provides scale and lighting for authoring without becoming a second terrain asset.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(sandbox.WorldSettings == null || sandbox.TerrainMaterial == null))
            {
                if (GUILayout.Button("Build Terrain Context"))
                {
                    BuildTerrainContext(sandbox);
                }
            }

            if (GUILayout.Button("Clear Terrain Context"))
            {
                ClearTerrainContext(sandbox);
            }
        }

        internal static void BuildTerrainContext(TopDown3DLandscapeAuthoringSandbox sandbox)
        {
            if (sandbox == null) throw new ArgumentNullException(nameof(sandbox));
            if (sandbox.WorldSettings == null)
                throw new InvalidOperationException("Landscape authoring needs a World Settings asset.");
            if (sandbox.TerrainMaterial == null)
                throw new InvalidOperationException("Landscape authoring needs the production terrain material.");

            ClearTerrainContext(sandbox);
            var contextRoot = new GameObject(TerrainPreviewRootName)
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };
            contextRoot.transform.SetParent(sandbox.transform, false);

            try
            {
                var settings = sandbox.WorldSettings;
                var generator = new TopDown3DWorldGenerator(settings);
                var radius = sandbox.TerrainRadiusInChunks;
                var center = sandbox.CenterChunk;
                for (var z = -radius; z <= radius; z++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        var coordinate = new Vector2Int(center.x + x, center.y + z);
                        var chunk = new GameObject($"Terrain {coordinate.x}, {coordinate.y}")
                        {
                            hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
                        };
                        chunk.transform.SetParent(contextRoot.transform, false);
                        chunk.transform.localPosition = new Vector3(
                            coordinate.x * settings.ChunkSize,
                            0f,
                            coordinate.y * settings.ChunkSize);

                        var mesh = TopDown3DChunkMeshBuilder.BuildMesh(settings, generator, coordinate);
                        mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
                        chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                        chunk.AddComponent<MeshRenderer>().sharedMaterial = sandbox.TerrainMaterial;
                    }
                }
            }
            catch
            {
                ClearTerrainContext(sandbox);
                throw;
            }
        }

        internal static void ClearTerrainContext(TopDown3DLandscapeAuthoringSandbox sandbox)
        {
            if (sandbox == null) return;
            var existing = sandbox.transform.Find(TerrainPreviewRootName);
            if (existing == null) return;

            var filters = existing.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh != null && (mesh.hideFlags & HideFlags.DontSaveInEditor) != 0)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }

    internal static class LandscapeAuthoringSandboxBuilder
    {
        internal const string ScenePath = "Assets/_Project/Scenes/TopDown3D/LandscapeAuthoringSandbox.unity";

        internal static void CreateScene()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.TerrainMaterialPath);
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.RockMaterialPath);
            if (settings == null || terrainMaterial == null || rockMaterial == null)
            {
                throw new InvalidOperationException(
                    "Landscape Authoring Sandbox requires the production world settings, terrain material, and rock material.");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LandscapeAuthoringSandbox";
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientLight = new Color(0.36f, 0.39f, 0.43f);

            var sandboxObject = new GameObject("Landscape Authoring Sandbox");
            var sandbox = sandboxObject.AddComponent<TopDown3DLandscapeAuthoringSandbox>();
            sandbox.Configure(settings, terrainMaterial, rockMaterial, Vector2Int.zero);

            CreateLighting();
            CreateCamera();

            var formationObject = new GameObject("Rock Formation Authoring");
            formationObject.transform.SetParent(sandboxObject.transform, false);
            var formation = formationObject.AddComponent<TopDown3DRockFormationAuthoring>();
            formation.Configure(settings.NaturalObjectCatalog, rockMaterial);

            if (!EditorSceneManager.SaveScene(scene, ScenePath, false))
            {
                throw new InvalidOperationException($"Unity did not save {ScenePath}.");
            }

            Selection.activeGameObject = formationObject;
            EditorApplication.delayCall += BuildInitialTerrainContext;
        }

        private static void BuildInitialTerrainContext()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath) return;

            var sandbox = UnityEngine.Object.FindAnyObjectByType<TopDown3DLandscapeAuthoringSandbox>();
            if (sandbox == null) return;

            try
            {
                TopDown3DLandscapeAuthoringSandboxEditor.BuildTerrainContext(sandbox);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, sandbox);
            }
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Authoring Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.82f, 0.64f);
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Authoring Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 50f;
            cameraObject.transform.position = new Vector3(24f, 26f, -24f);
            cameraObject.transform.LookAt(new Vector3(0f, 0f, 4f));
        }
    }
}
