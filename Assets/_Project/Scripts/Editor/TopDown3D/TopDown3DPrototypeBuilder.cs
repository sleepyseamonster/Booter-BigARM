using System;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class TopDown3DPrototypeBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity";
        public const string WorldSettingsPath = "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        public const string MaterialFolder = "Assets/_Project/Materials/TopDown3D";
        public const string TerrainMaterialPath = MaterialFolder + "/Greybox_Terrain.mat";
        public const string TerrainAlbedoPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldSandDirtAlbedo.png";
        public const float TerrainTextureTiling = 6f;
        public const float TerrainSmoothness = 0.18f;

        public const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";

        [MenuItem("Booter & BigARM/Top Down 3D/Build Perspective Prototype")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            if (exists && !EditorUtility.DisplayDialog(
                    "Rebuild Perspective Top-Down Prototype",
                    "This replaces only the generated TopDown3DPrototype scene. The protected 2D scenes, isometric lab, Build Settings, and renderer default remain unchanged.",
                    "Rebuild Generated Prototype",
                    "Cancel"))
            {
                return;
            }

            Build(exists);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"Built perspective top-down 3D prototype at {ScenePath}.");
        }

        [MenuItem("Booter & BigARM/Top Down 3D/Open Perspective Prototype")]
        public static void OpenFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog(
                    "Perspective Prototype Missing",
                    $"Build the generated prototype first. Expected scene: {ScenePath}",
                    "OK");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        public static void BuildFromCli()
        {
            Build(false);
        }

        public static void RebuildFromCli()
        {
            Build(true);
        }

        public static void Build(bool allowOverwrite)
        {
            var baselineErrors = ConversionBaselineValidator.CollectErrors();
            if (baselineErrors.Count > 0)
            {
                throw new BuildFailedException(
                    "Refusing to build the perspective prototype because the protected baseline is invalid:\n- "
                    + string.Join("\n- ", baselineErrors));
            }

            if (!allowOverwrite && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                throw new InvalidOperationException($"{ScenePath} already exists and overwrite was not authorized.");
            }

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                throw new InvalidOperationException($"Missing input actions at {InputActionsPath}.");
            }

            EnsureFolder("Assets/_Project/Scenes/TopDown3D");
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/_Project/Settings/World");

            var settings = EnsureWorldSettings();
            var terrainMaterial = EnsureTerrainMaterial();
            var rockMaterial = EnsureRockMaterial();
            var playerMaterial = EnsureMaterial("Greybox_Booter", new Color(0.87f, 0.31f, 0.12f));
            var bigArmMaterial = EnsureMaterial("Greybox_BigARM", new Color(0.08f, 0.74f, 0.76f));
            var rendererIndex = ResolveConversionRendererIndex();
            CreateScene(
                rendererIndex,
                inputActions,
                settings,
                terrainMaterial,
                rockMaterial,
                playerMaterial,
                bigArmMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static TopDown3DWorldSettings EnsureWorldSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            if (settings != null)
            {
                // Re-save existing assets so newly introduced serialized tuning fields become durable.
                EditorUtility.SetDirty(settings);
                return settings;
            }

            settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            settings.name = "TopDown3DWorldSettings";
            AssetDatabase.CreateAsset(settings, WorldSettingsPath);
            return settings;
        }

        private static Material EnsureTerrainMaterial()
        {
            var material = EnsureMaterial("Greybox_Terrain", Color.white);
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainAlbedoPath);
            if (albedo == null)
            {
                throw new InvalidOperationException($"Missing terrain albedo texture at {TerrainAlbedoPath}.");
            }

            var changed = false;
            var tiling = Vector2.one * TerrainTextureTiling;
            if (material.GetTexture("_BaseMap") != albedo)
            {
                material.SetTexture("_BaseMap", albedo);
                changed = true;
            }

            if (material.GetTextureScale("_BaseMap") != tiling)
            {
                material.SetTextureScale("_BaseMap", tiling);
                changed = true;
            }

            if (!Mathf.Approximately(material.GetFloat("_Smoothness"), TerrainSmoothness))
            {
                material.SetFloat("_Smoothness", TerrainSmoothness);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static Material EnsureRockMaterial()
        {
            var material = EnsureMaterial("Greybox_Rock", new Color(0.31f, 0.145f, 0.085f));
            if (!Mathf.Approximately(material.GetFloat("_Smoothness"), 0.18f))
            {
                material.SetFloat("_Smoothness", 0.18f);
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static Material EnsureMaterial(string name, Color color)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("The URP Lit shader could not be resolved.");
                }

                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            var changed = false;
            if (material.GetColor("_BaseColor") != color)
            {
                material.SetColor("_BaseColor", color);
                changed = true;
            }

            if (!material.enableInstancing)
            {
                material.enableInstancing = true;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static int ResolveConversionRendererIndex()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                ConversionBaselineValidator.PipelineAssetPath);
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            if (pipeline == null || renderer == null)
            {
                throw new InvalidOperationException("The protected conversion renderer topology is incomplete.");
            }

            var serialized = new SerializedObject(pipeline);
            var renderers = serialized.FindProperty("m_RendererDataList");
            var defaultIndex = serialized.FindProperty("m_DefaultRendererIndex");
            if (renderers == null || !renderers.isArray || defaultIndex == null || defaultIndex.intValue != 0)
            {
                throw new InvalidOperationException("The URP renderer list or protected default could not be verified.");
            }

            for (var i = 1; i < renderers.arraySize; i++)
            {
                if (renderers.GetArrayElementAtIndex(i).objectReferenceValue == renderer)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("The 3D conversion renderer is not registered at a non-default index.");
        }

        private static void CreateScene(
            int rendererIndex,
            InputActionAsset inputActions,
            TopDown3DWorldSettings settings,
            Material terrainMaterial,
            Material rockMaterial,
            Material playerMaterial,
            Material bigArmMaterial)
        {
            var previousActiveScene = SceneManager.GetActiveScene();
            var loadedTargetScene = SceneManager.GetSceneByPath(ScenePath);
            if (loadedTargetScene.IsValid() && loadedTargetScene.isLoaded && loadedTargetScene.isDirty)
            {
                throw new InvalidOperationException(
                    $"Refusing to replace dirty loaded scene {ScenePath}; save or discard its changes first.");
            }

            var creationMode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, creationMode);
            SceneManager.SetActiveScene(scene);

            try
            {
                if (!Application.isBatchMode && loadedTargetScene.IsValid() && loadedTargetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(loadedTargetScene, true);
                }

                scene.name = "TopDown3DPrototype";
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = new Color(0.30f, 0.38f, 0.46f);
                RenderSettings.fogStartDistance = 55f;
                RenderSettings.fogEndDistance = 135f;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.36f, 0.39f, 0.43f);

                var input = new GameObject("Top Down 3D Input").AddComponent<TopDown3DInputRouter>();
                input.Configure(inputActions);

                var player = CreatePlayer(settings, playerMaterial);
                var cameraRig = CreateCamera(player.transform, input, rendererIndex);
                var motor = player.GetComponent<TopDown3DPlayerMotor>();
                motor.Configure(input, cameraRig.transform);

                var world = new GameObject("Deterministic Streamed 3D World").AddComponent<TopDown3DProceduralWorld>();
                world.Configure(settings, player.transform, terrainMaterial, rockMaterial);

                var bigArm = CreateBigArm(settings, bigArmMaterial);
                var follower = bigArm.GetComponent<TopDown3DBigArmFollower>();
                follower.Configure(player.transform, cameraRig.transform, input);

                CreateLighting();
                new GameObject("Top Down 3D Debug Overlay")
                    .AddComponent<TopDown3DDebugOverlay>()
                    .Configure(input, motor, world, follower, cameraRig);

                if (!EditorSceneManager.SaveScene(scene, ScenePath, false))
                {
                    throw new InvalidOperationException($"Unity did not save {ScenePath}.");
                }
            }
            finally
            {
                // In batch mode this is the only loaded scene and Unity is about to quit.
                // Closing the last scene produces an avoidable editor warning.
                if (!Application.isBatchMode && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static GameObject CreatePlayer(TopDown3DWorldSettings settings, Material material)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Booter Perspective 3D Controller";
            player.transform.position = new Vector3(
                0f,
                TopDown3DHeightSampler.SampleHeight(settings, 0f, 0f) + 1.05f,
                0f);
            player.transform.localScale = new Vector3(0.82f, 1f, 0.82f);
            player.GetComponent<Renderer>().sharedMaterial = material;

            var body = player.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            player.AddComponent<TopDown3DPlayerMotor>();

            var facing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            facing.name = "Facing Marker";
            facing.transform.SetParent(player.transform, false);
            facing.transform.localPosition = new Vector3(0f, 0.3f, 0.55f);
            facing.transform.localScale = new Vector3(0.55f, 0.16f, 0.14f);
            UnityEngine.Object.DestroyImmediate(facing.GetComponent<Collider>());
            facing.GetComponent<Renderer>().sharedMaterial = material;
            return player;
        }

        private static TopDown3DCameraRig CreateCamera(
            Transform target,
            TopDown3DInputRouter input,
            int rendererIndex)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 48f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.AddComponent<AudioListener>();
            var additionalData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            additionalData.SetRenderer(rendererIndex);
            additionalData.renderPostProcessing = false;
            var rig = cameraObject.AddComponent<TopDown3DCameraRig>();
            rig.Configure(target, input);
            return rig;
        }

        private static GameObject CreateBigArm(TopDown3DWorldSettings settings, Material material)
        {
            const float startX = -4.2f;
            const float startZ = -2.5f;
            var bigArm = new GameObject("BigARM Simple Follow AI");
            bigArm.transform.position = new Vector3(
                startX,
                TopDown3DHeightSampler.SampleHeight(settings, startX, startZ) + 0.82f,
                startZ);
            var collider = bigArm.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.3f, 0f);
            collider.size = new Vector3(1.1f, 2.2f, 1.3f);
            var body = bigArm.AddComponent<Rigidbody>();
            body.mass = 8f;
            body.isKinematic = true;
            bigArm.AddComponent<TopDown3DBigArmFollower>();

            var bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyVisual.name = "BigARM Rectangular Prism";
            bodyVisual.transform.SetParent(bigArm.transform, false);
            bodyVisual.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            bodyVisual.transform.localScale = new Vector3(1.05f, 2.2f, 1.25f);
            UnityEngine.Object.DestroyImmediate(bodyVisual.GetComponent<Collider>());
            bodyVisual.GetComponent<Renderer>().sharedMaterial = material;
            return bigArm;
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Directional Key Light");
            var light = lightObject.AddComponent<Light>();
            lightObject.AddComponent<PerpetualTwilightSun>().Configure(light);
        }

        private static void EnsureFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
