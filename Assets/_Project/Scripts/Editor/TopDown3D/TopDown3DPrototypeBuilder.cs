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
        public const string NaturalObjectCatalogPath =
            "Assets/_Project/Settings/World/TopDown3DNaturalObjectCatalog.asset";
        public const string FineGrayClutterMaterialPath = MaterialFolder + "/FineGray_Clutter.mat";
        public const string TerrainMaterialPath = MaterialFolder + "/Greybox_Terrain.mat";
        public const string RockMaterialPath = MaterialFolder + "/Greybox_Rock.mat";
        public const string DarkRockMaterialPath = MaterialFolder + "/BrokenWorld_Rock_Dark.mat";
        public const string TealRockMaterialPath = MaterialFolder + "/BrokenWorld_Rock_Teal.mat";
        public const string TerrainShaderPath =
            "Assets/_Project/Shaders/TopDown3D/BrokenWorldTerrainBlend.shader";
        public const string RockShaderPath =
            "Assets/_Project/Shaders/TopDown3D/BrokenWorldRockTriplanar.shader";
        public const string RockAlbedoPath =
            "Assets/_Project/Art/Environment/Rocks/BrokenWorldRockSurfaceAlbedo.png";
        public const string DarkRockAlbedoPath =
            "Assets/_Project/Art/Environment/Rocks/BrokenWorldRockSurfaceDarkAlbedo.png";
        public const string TealRockAlbedoPath =
            "Assets/_Project/Art/Environment/Rocks/BrokenWorldRockSurfaceTealAlbedo.png";
        public const string TerrainAlbedoPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldSandDirtAlbedo.png";
        public const string TerrainSweptSandAlbedoPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldSweptSandAlbedo.png";
        public const string TerrainSweptSandTransitionAlbedoPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldSweptSandTransitionAlbedo.png";
        public const string TerrainGravelAlbedoPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldGravelAlbedo.png";
        public const string TerrainGravelTransitionAlbedoPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldGravelTransitionAlbedo.png";
        public const string TerrainRockyAlbedoPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldMixedRockyAlbedo.png";
        public const string TerrainRockyHeightPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldMixedRockyHeight.png";
        public const string TerrainRockyNormalPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldMixedRockyNormal.png";
        public const string TerrainRockyTransitionAlbedoPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldRockyTransitionAlbedo.png";
        public const string TerrainRockyTransitionHeightPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldRockyTransitionHeight.png";
        public const string TerrainRockyTransitionNormalPath =
            "Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldRockyTransitionNormal.png";
        public const float TerrainBaseMetersPerTile = 3f;
        public const float TerrainSweptSandMetersPerTile = 4f;
        public const float TerrainGravelMetersPerTile = 2.25f;
        public const float TerrainRockyMetersPerTile = 3f;
        public const float TerrainPatchFrequency = 0.035f;
        public const float TerrainSweptSandThreshold = 0.64f;
        public const float TerrainGravelThreshold = 0.66f;
        public const float TerrainRockyThreshold = 0.62f;
        public const float TerrainRockyStrength = 0.96f;
        public const float TerrainRockyHeightScale = 0.055f;
        public const float TerrainRockyNormalStrength = 0.9f;
        public const float TerrainRockyReliefOcclusion = 0.16f;
        public const float TerrainPatchBlendWidth = 0.11f;
        public const float TerrainTransitionWidth = 0.13f;
        public const float TerrainSmoothness = 0.18f;
        public const float RockMetersPerTile = 0.85f;
        public const float RockSmoothness = 0.14f;
        public const float PlayerColliderHeight = 2.48f;
        public const float PlayerColliderRadius = 0.52f;
        public const float PlayerGroundClearance = 0.06f;

        public const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        public const string PrototypeHumanoidFolder =
            "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid";
        public const string PrototypeHumanoidModelPath = PrototypeHumanoidFolder + "/defaultmale_rig.fbx";
        public const string PrototypeHumanoidAnimationsFolder = PrototypeHumanoidFolder + "/Animations";
        public const string PrototypeHumanoidIdlePath = PrototypeHumanoidAnimationsFolder + "/m@Idle.fbx";
        public const string PrototypeHumanoidWalkPath = PrototypeHumanoidAnimationsFolder + "/m@WalkForwards.fbx";
        public const string PrototypeHumanoidRunPath = PrototypeHumanoidAnimationsFolder + "/m@RunForwards.fbx";
        public const string PrototypeHumanoidSprintPath = PrototypeHumanoidAnimationsFolder + "/m@SprintForwards.fbx";
        public const string PrototypeHumanoidSpinPath = PrototypeHumanoidAnimationsFolder + "/m@RapidTurningLeft.fbx";
        public const string PrototypeHumanoidVaultPath =
            PrototypeHumanoidAnimationsFolder + "/m@SprintForwardsJump_Frame01.fbx";

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
            var darkRockMaterial = EnsureRockMaterial(
                "BrokenWorld_Rock_Dark",
                DarkRockAlbedoPath);
            var tealRockMaterial = EnsureRockMaterial(
                "BrokenWorld_Rock_Teal",
                TealRockAlbedoPath);
            var fineGrayClutterMaterial = EnsureFineGrayClutterMaterial();
            var naturalObjectCatalog = AssetDatabase.LoadAssetAtPath<TopDown3DNaturalObjectCatalog>(
                NaturalObjectCatalogPath);
            if (naturalObjectCatalog == null)
            {
                throw new InvalidOperationException(
                    $"Missing natural-object catalog at {NaturalObjectCatalogPath}.");
            }

            settings.ConfigureNaturalObjectAssets(
                naturalObjectCatalog,
                fineGrayClutterMaterial,
                darkRockMaterial,
                tealRockMaterial);
            EditorUtility.SetDirty(settings);
            var playerMaterial = EnsureMaterial("Greybox_Booter", new Color(0.58f, 0.49f, 0.37f));
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
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(TerrainShaderPath);
            var baseAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainAlbedoPath);
            var sweptSandAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainSweptSandAlbedoPath);
            var sweptSandTransitionAlbedo =
                AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainSweptSandTransitionAlbedoPath);
            var gravelAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainGravelAlbedoPath);
            var gravelTransitionAlbedo =
                AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainGravelTransitionAlbedoPath);
            var rockyAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainRockyAlbedoPath);
            var rockyHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainRockyHeightPath);
            var rockyNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainRockyNormalPath);
            var rockyTransitionAlbedo =
                AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainRockyTransitionAlbedoPath);
            var rockyTransitionHeight =
                AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainRockyTransitionHeightPath);
            var rockyTransitionNormal =
                AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainRockyTransitionNormalPath);
            if (shader == null || baseAlbedo == null || sweptSandAlbedo == null || gravelAlbedo == null
                || rockyAlbedo == null || sweptSandTransitionAlbedo == null || gravelTransitionAlbedo == null
                || rockyTransitionAlbedo == null || rockyHeight == null || rockyNormal == null
                || rockyTransitionHeight == null || rockyTransitionNormal == null)
            {
                throw new InvalidOperationException("The layered terrain shader or one of its albedo textures is missing.");
            }

            var changed = false;
            if (material.shader != shader)
            {
                material.shader = shader;
                changed = true;
            }

            changed |= SetTextureIfNeeded(material, "_BaseMap", baseAlbedo);
            changed |= SetTextureIfNeeded(material, "_SweptSandMap", sweptSandAlbedo);
            changed |= SetTextureIfNeeded(material, "_SweptSandTransitionMap", sweptSandTransitionAlbedo);
            changed |= SetTextureIfNeeded(material, "_GravelMap", gravelAlbedo);
            changed |= SetTextureIfNeeded(material, "_GravelTransitionMap", gravelTransitionAlbedo);
            changed |= SetTextureIfNeeded(material, "_RockyMap", rockyAlbedo);
            changed |= SetTextureIfNeeded(material, "_RockyTransitionMap", rockyTransitionAlbedo);
            changed |= SetTextureIfNeeded(material, "_RockyHeightMap", rockyHeight);
            changed |= SetTextureIfNeeded(material, "_RockyNormalMap", rockyNormal);
            changed |= SetTextureIfNeeded(material, "_RockyTransitionHeightMap", rockyTransitionHeight);
            changed |= SetTextureIfNeeded(material, "_RockyTransitionNormalMap", rockyTransitionNormal);
            changed |= SetFloatIfNeeded(material, "_BaseMetersPerTile", TerrainBaseMetersPerTile);
            changed |= SetFloatIfNeeded(material, "_SweptSandMetersPerTile", TerrainSweptSandMetersPerTile);
            changed |= SetFloatIfNeeded(material, "_GravelMetersPerTile", TerrainGravelMetersPerTile);
            changed |= SetFloatIfNeeded(material, "_RockyMetersPerTile", TerrainRockyMetersPerTile);
            changed |= SetFloatIfNeeded(material, "_PatchFrequency", TerrainPatchFrequency);
            changed |= SetFloatIfNeeded(material, "_SweptSandThreshold", TerrainSweptSandThreshold);
            changed |= SetFloatIfNeeded(material, "_GravelThreshold", TerrainGravelThreshold);
            changed |= SetFloatIfNeeded(material, "_RockyThreshold", TerrainRockyThreshold);
            changed |= SetFloatIfNeeded(material, "_RockyStrength", TerrainRockyStrength);
            changed |= SetFloatIfNeeded(material, "_RockyHeightScale", TerrainRockyHeightScale);
            changed |= SetFloatIfNeeded(material, "_RockyNormalStrength", TerrainRockyNormalStrength);
            changed |= SetFloatIfNeeded(material, "_RockyReliefOcclusion", TerrainRockyReliefOcclusion);
            changed |= SetFloatIfNeeded(material, "_BlendWidth", TerrainPatchBlendWidth);
            changed |= SetFloatIfNeeded(material, "_TransitionWidth", TerrainTransitionWidth);
            changed |= SetFloatIfNeeded(material, "_Smoothness", TerrainSmoothness);

            if (changed)
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static bool SetTextureIfNeeded(Material material, string propertyName, Texture texture)
        {
            if (material.GetTexture(propertyName) == texture)
            {
                return false;
            }

            material.SetTexture(propertyName, texture);
            return true;
        }

        private static bool SetFloatIfNeeded(Material material, string propertyName, float value)
        {
            if (Mathf.Approximately(material.GetFloat(propertyName), value))
            {
                return false;
            }

            material.SetFloat(propertyName, value);
            return true;
        }

        private static Material EnsureRockMaterial()
        {
            return EnsureRockMaterial("Greybox_Rock", RockAlbedoPath);
        }

        private static Material EnsureRockMaterial(string materialName, string albedoPath)
        {
            var material = EnsureMaterial(materialName, Color.white);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(RockShaderPath);
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            if (shader == null || albedo == null)
            {
                throw new InvalidOperationException(
                    $"The triplanar rock shader or albedo texture is missing for {materialName}.");
            }

            var changed = false;
            if (material.shader != shader)
            {
                material.shader = shader;
                changed = true;
            }

            changed |= SetTextureIfNeeded(material, "_BaseMap", albedo);
            changed |= SetFloatIfNeeded(material, "_RockMetersPerTile", RockMetersPerTile);
            changed |= SetFloatIfNeeded(material, "_TriplanarSharpness", 4f);
            changed |= SetFloatIfNeeded(material, "_Smoothness", RockSmoothness);
            if (changed)
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static Material EnsureFineGrayClutterMaterial()
        {
            var material = EnsureMaterial("FineGray_Clutter", new Color(0.36f, 0.36f, 0.36f));
            if (!Mathf.Approximately(material.GetFloat("_Smoothness"), 0.12f))
            {
                material.SetFloat("_Smoothness", 0.12f);
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
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.42f, 0.19f, 0.10f);
                RenderSettings.fogDensity = TopDown3DDustAtmosphere.DefaultFogDensityAtIntensityOne;
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
                CreateDustAtmosphere(player.transform, cameraRig.GetComponent<Camera>(), settings.WorldSeed);
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
                TopDown3DHeightSampler.SampleHeight(settings, 0f, 0f)
                    + PlayerColliderHeight * 0.5f
                    + PlayerGroundClearance,
                0f);
            player.transform.localScale = Vector3.one;
            player.GetComponent<Renderer>().sharedMaterial = material;
            player.GetComponent<Renderer>().enabled = false;
            var capsule = player.GetComponent<CapsuleCollider>();
            capsule.height = PlayerColliderHeight;
            capsule.radius = PlayerColliderRadius;

            var body = player.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            player.AddComponent<TopDown3DPlayerMotor>();
            player.AddComponent<TopDown3DPlayerAnimationDriver>().Configure(
                LoadRequiredAsset<GameObject>(PrototypeHumanoidModelPath),
                LoadRequiredAnimationClip(PrototypeHumanoidIdlePath, "Idle"),
                LoadRequiredAnimationClip(PrototypeHumanoidWalkPath, "WalkForwards"),
                LoadRequiredAnimationClip(PrototypeHumanoidRunPath, "RunForwards"),
                LoadRequiredAnimationClip(PrototypeHumanoidSprintPath, "SprintForwards"),
                LoadRequiredAnimationClip(PrototypeHumanoidSpinPath, "RapidTurningLeft"),
                LoadRequiredAnimationClip(
                    PrototypeHumanoidVaultPath,
                    "SprintForwardsJump_Frame01"));

            var facing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            facing.name = "Facing Marker";
            facing.transform.SetParent(player.transform, false);
            facing.transform.localPosition = new Vector3(0f, 0.3f, 0.55f);
            facing.transform.localScale = new Vector3(0.55f, 0.16f, 0.14f);
            UnityEngine.Object.DestroyImmediate(facing.GetComponent<Collider>());
            facing.GetComponent<Renderer>().sharedMaterial = material;
            facing.SetActive(false);
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
            additionalData.renderPostProcessing = true;
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

        private static void CreateDustAtmosphere(Transform subject, Camera camera, int worldSeed)
        {
            new GameObject("Dust Atmosphere")
                .AddComponent<TopDown3DDustAtmosphere>()
                .Configure(subject, camera, worldSeed);

            var regions = new GameObject("Dust Regions");
            CreateDustZone(
                regions.transform,
                "Dense Dust Basin",
                new Vector3(38f, 0f, 16f),
                15f,
                16f,
                1.82f,
                new Color(0.72f, 0.32f, 0.12f));
            CreateDustZone(
                regions.transform,
                "Sheltered Thin-Dust Pocket",
                new Vector3(-34f, 0f, -22f),
                11f,
                14f,
                0.90f,
                new Color(0.40f, 0.25f, 0.18f));
        }

        private static void CreateDustZone(
            Transform parent,
            string zoneName,
            Vector3 position,
            float innerRadius,
            float blendDistance,
            float intensity,
            Color tint)
        {
            var zoneObject = new GameObject(zoneName);
            zoneObject.transform.SetParent(parent, false);
            zoneObject.transform.position = position;
            zoneObject.AddComponent<TopDown3DDustZone>().Configure(
                innerRadius,
                blendDistance,
                intensity,
                tint);
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

        private static T LoadRequiredAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Missing required prototype character asset: {assetPath}.");
            }

            return asset;
        }

        private static AnimationClip LoadRequiredAnimationClip(string assetPath, string clipName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && clip.name == clipName)
                {
                    return clip;
                }
            }

            throw new InvalidOperationException(
                $"Missing required animation clip '{clipName}' at {assetPath}.");
        }
    }
}
