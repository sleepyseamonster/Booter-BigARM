using System;
using System.Collections.Generic;
using BooterBigArm.Runtime;
using Unity.Cinemachine;
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
    /// <summary>
    /// Creates the protected CP-03 through CP-05 conversion lab without opening, saving,
    /// replacing, or adding the preserved 2D scenes to Build Settings.
    /// </summary>
    public static class IsometricConversionLabBuilder
    {
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        private const string ItemDatabasePath = "Assets/_Project/Legacy2D/Settings/Items/PrototypeItemDatabase.asset";
        private const string MaterialFolder = "Assets/_Project/Materials/IsometricSpike";
        private const string VolumeProfilePath = "Assets/_Project/Settings/Rendering/URP/IsometricLabVolumeProfile.asset";
        private const string DefaultPostProcessDataPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

        [MenuItem("Booter & BigARM/Conversion/Build Protected Isometric Lab")]
        public static void BuildFromMenu()
        {
            try
            {
                BuildConversionLab();
                Debug.Log($"Built protected isometric conversion lab at {ConversionBaselineValidator.ConversionScenePath}.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem("Booter & BigARM/Conversion/Open Isometric Lab")]
        public static void OpenLabFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ConversionBaselineValidator.ConversionScenePath) == null)
            {
                Debug.LogError($"The conversion lab does not exist at {ConversionBaselineValidator.ConversionScenePath}.");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ConversionBaselineValidator.ConversionScenePath, OpenSceneMode.Single);
            }
        }

        [MenuItem("Booter & BigARM/Conversion/Rebuild Generated Isometric Lab")]
        public static void RebuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Generated Isometric Lab",
                    "This replaces only the generated IsometricConversionLab scene. The protected 2D scenes, Build Settings, renderer default, and source assets remain untouched.",
                    "Rebuild Generated Lab",
                    "Cancel"))
            {
                return;
            }

            if (string.Equals(
                    SceneManager.GetActiveScene().path,
                    ConversionBaselineValidator.ConversionScenePath,
                    StringComparison.Ordinal))
            {
                EditorSceneManager.OpenScene(ConversionBaselineValidator.PrototypeScenePath, OpenSceneMode.Single);
            }

            BuildConversionLab(true);
            EditorSceneManager.OpenScene(ConversionBaselineValidator.ConversionScenePath, OpenSceneMode.Single);
            Debug.Log("Rebuilt the generated isometric conversion lab from its protected builder.");
        }

        public static void BuildFromCli()
        {
            BuildConversionLab(false);
        }

        public static void BuildConversionLab()
        {
            BuildConversionLab(false);
        }

        private static void BuildConversionLab(bool allowSceneOverwrite)
        {
            var baselineErrors = ConversionBaselineValidator.CollectErrors();
            if (baselineErrors.Count > 0)
            {
                throw new BuildFailedException("Refusing to build the conversion lab because the protected baseline is invalid:\n- "
                                               + string.Join("\n- ", baselineErrors));
            }

            if (!allowSceneOverwrite
                && AssetDatabase.LoadAssetAtPath<SceneAsset>(ConversionBaselineValidator.ConversionScenePath) != null)
            {
                throw new InvalidOperationException(
                    $"{ConversionBaselineValidator.ConversionScenePath} already exists. The protected builder will not overwrite possible manual work.");
            }

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            var itemDatabase = AssetDatabase.LoadAssetAtPath<PrototypeItemDatabase>(ItemDatabasePath);
            if (inputActions == null || itemDatabase == null)
            {
                throw new InvalidOperationException("The conversion lab requires the existing input-actions asset and prototype item database.");
            }

            EnsureAssetFolder("Assets/_Project/Scenes/Isometric");
            EnsureAssetFolder(MaterialFolder);
            EnsureAssetFolder("Assets/_Project/Settings/Rendering/URP");

            var rendererIndex = EnsureParallelRenderer();
            var palette = CreatePalette();
            var volumeProfile = EnsureVolumeProfile();
            CreateScene(rendererIndex, inputActions, itemDatabase, palette, volumeProfile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var finalErrors = ConversionBaselineValidator.CollectErrors();
            if (finalErrors.Count > 0)
            {
                throw new BuildFailedException("The conversion lab was created, but preservation validation failed:\n- "
                                               + string.Join("\n- ", finalErrors));
            }
        }

        private static int EnsureParallelRenderer()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                ConversionBaselineValidator.PipelineAssetPath);
            var legacyRenderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.LegacyRendererPath);
            if (pipeline == null || legacyRenderer == null)
            {
                throw new InvalidOperationException("The existing URP asset or Renderer2D asset is missing.");
            }

            var conversionRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            if (conversionRenderer == null)
            {
                conversionRenderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                conversionRenderer.name = "IsometricRenderer";
                AssetDatabase.CreateAsset(conversionRenderer, ConversionBaselineValidator.ConversionRendererPath);
            }

            var defaultPostProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessDataPath);
            if (defaultPostProcessData == null)
            {
                throw new InvalidOperationException("The URP default post-process resources could not be loaded.");
            }

            if (conversionRenderer.postProcessData != defaultPostProcessData)
            {
                conversionRenderer.postProcessData = defaultPostProcessData;
                EditorUtility.SetDirty(conversionRenderer);
            }

            var serializedPipeline = new SerializedObject(pipeline);
            var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            var defaultRendererIndex = serializedPipeline.FindProperty("m_DefaultRendererIndex");
            if (rendererList == null || !rendererList.isArray || defaultRendererIndex == null)
            {
                throw new InvalidOperationException("Unity's serialized URP renderer-list fields were not found.");
            }

            if (defaultRendererIndex.intValue != 0
                || rendererList.arraySize == 0
                || rendererList.GetArrayElementAtIndex(0).objectReferenceValue != legacyRenderer)
            {
                throw new InvalidOperationException("Renderer2D is no longer the protected default at index 0.");
            }

            for (var i = 1; i < rendererList.arraySize; i++)
            {
                if (rendererList.GetArrayElementAtIndex(i).objectReferenceValue == conversionRenderer)
                {
                    return i;
                }
            }

            var newIndex = rendererList.arraySize;
            rendererList.InsertArrayElementAtIndex(newIndex);
            rendererList.GetArrayElementAtIndex(newIndex).objectReferenceValue = conversionRenderer;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            return newIndex;
        }

        private static void CreateScene(
            int rendererIndex,
            InputActionAsset inputActions,
            PrototypeItemDatabase itemDatabase,
            IReadOnlyDictionary<string, Material> palette,
            VolumeProfile volumeProfile)
        {
            var previousActiveScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            try
            {
                scene.name = "IsometricConversionLab";
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = new Color(0.25f, 0.31f, 0.4f);
                RenderSettings.fogStartDistance = 20f;
                RenderSettings.fogEndDistance = 70f;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.32f, 0.35f, 0.4f);
                RenderSettings.reflectionIntensity = 0.55f;

                var environmentRoot = new GameObject("Greybox Environment");
                CreateGround(environmentRoot.transform, palette);
                CreateRamp(environmentRoot.transform, palette);
                CreateOcclusionCourse(environmentRoot.transform, palette);

                var player = CreatePlayer(inputActions, itemDatabase, palette["Booter"]);
                var cameraRig = CreateCameraRig(player.transform, rendererIndex, volumeProfile);
                var bigArm = CreateBigArm(
                    player.transform,
                    cameraRig.Camera.transform,
                    inputActions,
                    palette["BigARM"]);
                var harvestNode = CreateHarvestNode(palette["Resource"], palette["Pickup"]);
                var pickup = CreatePickup(palette["Pickup"]);
                var interactor = player.GetComponent<IsometricHarvestInteractor3D>();
                var inventory = player.GetComponent<PrototypeInventory>();

                var projectionToggle = cameraRig.Camera.gameObject.AddComponent<IsometricCameraProjectionToggle>();
                projectionToggle.Configure(cameraRig.Camera, cameraRig.VirtualCamera);

                var occlusion = cameraRig.Camera.gameObject.AddComponent<IsometricOcclusionController>();
                occlusion.Configure(cameraRig.Camera, player.transform);

                var overlay = new GameObject("Spike Evidence Overlay").AddComponent<IsometricSpikeOverlay>();
                overlay.Configure(projectionToggle, interactor, inventory);

                var lightObject = new GameObject("Directional Key Light");
                lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.89f, 0.72f);
                light.intensity = 1.25f;
                light.shadows = LightShadows.Soft;

                var volumeObject = new GameObject("Isometric Lab Global Volume");
                var volume = volumeObject.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 1f;
                volume.sharedProfile = volumeProfile;

                // Keep explicit references alive and readable in the hierarchy during inspection.
                harvestNode.name = "Harvest Proof — Ironstone";
                pickup.name = "Pickup Proof — Scrap Metal";
                bigArm.name = "BigARM Scale And Recall Proof";

                if (!EditorSceneManager.SaveScene(scene, ConversionBaselineValidator.ConversionScenePath, false))
                {
                    throw new InvalidOperationException("Unity did not save the conversion lab scene.");
                }
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static GameObject CreatePlayer(
            InputActionAsset inputActions,
            PrototypeItemDatabase itemDatabase,
            Material material)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Booter 3D Movement Proof";
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.transform.localScale = new Vector3(0.85f, 1f, 0.85f);
            SetMaterial(player, material);

            var body = player.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var input = player.AddComponent<PlayerInputAdapter>();
            input.SetInputActions(inputActions);

            var inventory = player.AddComponent<PrototypeInventory>();
            SetObjectReference(inventory, "itemDatabase", itemDatabase);

            var motor = player.AddComponent<IsometricPlayerMotor3D>();
            var interactor = player.AddComponent<IsometricHarvestInteractor3D>();
            interactor.Configure(inputActions, motor, inventory);

            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Facing Marker";
            visor.transform.SetParent(player.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.3f, 0.55f);
            visor.transform.localScale = new Vector3(0.65f, 0.18f, 0.15f);
            UnityEngine.Object.DestroyImmediate(visor.GetComponent<Collider>());
            SetMaterial(visor, material);

            // Camera configuration follows so the output camera can become the movement basis.
            return player;
        }

        private static CameraRig CreateCameraRig(Transform player, int rendererIndex, VolumeProfile volumeProfile)
        {
            var target = new GameObject("Camera Follow Target").transform;
            target.SetParent(player, false);
            target.localPosition = new Vector3(0f, 0.8f, 0f);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(-10.4f, 11.2f, -10.4f);
            cameraObject.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
            var outputCamera = cameraObject.AddComponent<Camera>();
            outputCamera.orthographic = true;
            outputCamera.orthographicSize = 8f;
            outputCamera.nearClipPlane = 0.1f;
            outputCamera.farClipPlane = 120f;
            outputCamera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CinemachineBrain>();

            var additionalData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            additionalData.SetRenderer(rendererIndex);
            additionalData.renderPostProcessing = volumeProfile != null;

            var virtualCameraObject = new GameObject("Isometric Cinemachine Camera");
            virtualCameraObject.transform.position = cameraObject.transform.position;
            virtualCameraObject.transform.rotation = cameraObject.transform.rotation;
            var virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
            virtualCamera.Follow = target;
            var lens = virtualCamera.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = 8f;
            lens.FieldOfView = 48f;
            lens.NearClipPlane = 0.1f;
            lens.FarClipPlane = 120f;
            virtualCamera.Lens = lens;

            var positionComposer = virtualCameraObject.AddComponent<CinemachinePositionComposer>();
            positionComposer.CameraDistance = 18f;
            positionComposer.Damping = new Vector3(0.35f, 0.35f, 0.2f);
            positionComposer.TargetOffset = Vector3.zero;

            var motor = player.GetComponent<IsometricPlayerMotor3D>();
            var input = player.GetComponent<PlayerInputAdapter>();
            motor.Configure(input, cameraObject.transform);

            return new CameraRig(outputCamera, virtualCamera);
        }

        private static GameObject CreateBigArm(
            Transform player,
            Transform cameraBasis,
            InputActionAsset inputActions,
            Material material)
        {
            var bigArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bigArm.transform.position = new Vector3(-5f, 1.6f, 1f);
            bigArm.transform.localScale = new Vector3(3.2f, 3.2f, 4.2f);
            SetMaterial(bigArm, material);

            bigArm.AddComponent<Rigidbody>();
            var follower = bigArm.AddComponent<IsometricBigArmFollower3D>();
            follower.Configure(player, cameraBasis, inputActions);

            var shoulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shoulder.name = "BigARM Shoulder Silhouette";
            shoulder.transform.SetParent(bigArm.transform, false);
            shoulder.transform.localPosition = new Vector3(0.55f, 0.25f, 0f);
            shoulder.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            UnityEngine.Object.DestroyImmediate(shoulder.GetComponent<Collider>());
            SetMaterial(shoulder, material);
            return bigArm;
        }

        private static GameObject CreateHarvestNode(Material material, Material markerMaterial)
        {
            var node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            node.transform.position = new Vector3(1.5f, 1.2f, 4f);
            node.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            SetMaterial(node, material);

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "World-Space Interaction Marker";
            marker.transform.SetParent(node.transform, false);
            marker.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            marker.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
            marker.transform.localScale = Vector3.one * 0.25f;
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
            SetMaterial(marker, markerMaterial);

            var harvestNode = node.AddComponent<IsometricHarvestNode3D>();
            harvestNode.Configure(
                "conversion.ironstone",
                "Ironstone Outcrop",
                "ironstone",
                2,
                0.75f,
                6f,
                node.GetComponent<Renderer>(),
                marker.GetComponent<Renderer>());
            return node;
        }

        private static GameObject CreatePickup(Material material)
        {
            var pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.transform.position = new Vector3(3f, 0.65f, 1f);
            pickup.transform.localScale = Vector3.one * 0.65f;
            SetMaterial(pickup, material);
            pickup.GetComponent<Collider>().isTrigger = true;
            pickup.AddComponent<IsometricWorldItemPickup3D>().Configure("scrap_metal", 1, 0.35f);
            return pickup;
        }

        private static void CreateGround(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            CreateBlock(
                "Walkable Ground",
                new Vector3(0f, -0.25f, 0f),
                new Vector3(24f, 0.5f, 24f),
                Quaternion.identity,
                palette["Ground"],
                root);

            CreateBlock(
                "Raised Readability Pad",
                new Vector3(7.5f, 0.35f, -5.5f),
                new Vector3(5f, 0.7f, 5f),
                Quaternion.identity,
                palette["Raised"],
                root);
        }

        private static void CreateRamp(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            CreateBlock(
                "Modest Ramp Proof",
                new Vector3(5f, 0.2f, -3.2f),
                new Vector3(5f, 0.3f, 3f),
                Quaternion.Euler(0f, 0f, 8f),
                palette["Ramp"],
                root);
        }

        private static void CreateOcclusionCourse(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            var cameraSideBlocker = CreateBlock(
                "Camera-Side Occluder Proof",
                new Vector3(-3.2f, 2.1f, -3.2f),
                new Vector3(2.2f, 4.2f, 2.2f),
                Quaternion.identity,
                palette["Occluder"],
                root);
            var marker = cameraSideBlocker.AddComponent<IsometricOccluder>();
            marker.Configure(cameraSideBlocker.GetComponentsInChildren<Renderer>());

            CreateBlock(
                "Navigation Wall North",
                new Vector3(3.5f, 1.25f, 5.5f),
                new Vector3(7f, 2.5f, 0.7f),
                Quaternion.identity,
                palette["Wall"],
                root);
            CreateBlock(
                "Navigation Wall East",
                new Vector3(8f, 1.25f, 1.5f),
                new Vector3(0.7f, 2.5f, 6f),
                Quaternion.identity,
                palette["Wall"],
                root);
        }

        private static GameObject CreateBlock(
            string name,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material,
            Transform parent)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, true);
            block.transform.SetPositionAndRotation(position, rotation);
            block.transform.localScale = scale;
            SetMaterial(block, material);
            return block;
        }

        private static IReadOnlyDictionary<string, Material> CreatePalette()
        {
            return new Dictionary<string, Material>
            {
                ["Ground"] = EnsureMaterial("Greybox_Ground", new Color(0.46f, 0.35f, 0.23f)),
                ["Raised"] = EnsureMaterial("Greybox_Raised", new Color(0.58f, 0.43f, 0.26f)),
                ["Ramp"] = EnsureMaterial("Greybox_Ramp", new Color(0.64f, 0.49f, 0.3f)),
                ["Wall"] = EnsureMaterial("Greybox_Wall", new Color(0.28f, 0.22f, 0.2f)),
                ["Occluder"] = EnsureMaterial("Greybox_Occluder", new Color(0.4f, 0.25f, 0.18f)),
                ["Booter"] = EnsureMaterial("Greybox_Booter", new Color(0.12f, 0.76f, 0.82f)),
                ["BigARM"] = EnsureMaterial("Greybox_BigARM", new Color(0.9f, 0.34f, 0.13f)),
                ["Resource"] = EnsureMaterial("Greybox_Resource", new Color(0.44f, 0.58f, 0.68f)),
                ["Pickup"] = EnsureMaterial("Greybox_Pickup", new Color(1f, 0.82f, 0.12f))
            };
        }

        private static Material EnsureMaterial(string name, Color color)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("The URP Lit shader was not found.");
            }

            material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static VolumeProfile EnsureVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "IsometricLabVolumeProfile";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            var color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(5f);
            color.saturation.Override(-5f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void SetMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedTarget = new SerializedObject(target);
            var property = serializedTarget.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
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

        private readonly struct CameraRig
        {
            public CameraRig(Camera camera, CinemachineCamera virtualCamera)
            {
                Camera = camera;
                VirtualCamera = virtualCamera;
            }

            public Camera Camera { get; }
            public CinemachineCamera VirtualCamera { get; }
        }
    }
}
