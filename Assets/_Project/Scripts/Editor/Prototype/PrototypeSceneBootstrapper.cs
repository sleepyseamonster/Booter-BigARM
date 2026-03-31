using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.Runtime;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.InputSystem;

namespace BooterBigArm.Editor
{
    public static class PrototypeSceneBootstrapper
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/PrototypeScene.unity";
        private const string PrototypeArtFolder = "Assets/_Project/Art/Prototype";
        private const string PlayerSpritePath = "Assets/_Project/Art/Prototype/PrototypeSquare.png";
        private const string ShadowSpritePath = "Assets/_Project/Art/Prototype/Shadows/PrototypeShadow.png";
        private const string TallPropSpritePath = "Assets/_Project/Art/Prototype/Props/TallPropPlaceholder.png";
        private const string TallPropPrefabPath = "Assets/_Project/Prefabs/Prototype/TallPropPlaceholder.prefab";
        private const string RuleGroundFolder = "Assets/_Project/Art/Prototype/Ground/RuleGround";
        private const string SandPatchRuleTilePath = "Assets/_Project/Art/Prototype/Ground/RuleGround/Sand Patch.asset";
        private const string TallPropWidePrefabPath = "Assets/_Project/Prefabs/Prototype/TallProps/TallPropWide64x64.prefab";
        private const string TallPropTallPrefabPath = "Assets/_Project/Prefabs/Prototype/TallProps/TallPropTall64x96.prefab";
        private const string TallPropSquarePrefabPath = "Assets/_Project/Prefabs/Prototype/TallProps/TallPropSquare32x32.prefab";
        private const string TallPropTinyPrefabPath = "Assets/_Project/Prefabs/Prototype/TallProps/TallPropTiny16x16.prefab";
        private const string TallPropSlimPrefabPath = "Assets/_Project/Prefabs/Prototype/TallProps/TallPropSlim16x32.prefab";
        private const string BoulderPrefabFolder = "Assets/_Project/Prefabs";
        private const string PrototypePropCatalogPath = "Assets/_Project/Settings/World/PrototypeWorldPropCatalog.asset";
        private const string GreaterWastelandBiomeId = "Greater Wasteland";
        private const string ReefBiomeId = "The Reef";
        private const string SandOverlayGridName = "Sand Overlay Grid";
        private const string SandOverlayOffsetGridName = "Sand Overlay Offset Grid";
        private const string SandOverlayOffsetTilemapName = "Sand Overlay Offset Tilemap";
        private const string GroundSandFolder = "Assets/_Project/Art/Prototype/Ground/Sand";
        private const string GroundSandPsdPath = "Assets/_Project/Art/Prototype/Ground/Sand/tilemap_sand.psd";
        private const string GroundSandOverlayPsdPath = "Assets/_Project/Art/Prototype/Ground/Sand/tilemap_sand_overlay_128.psd";
        private const string GroundPebbleFolder = "Assets/_Project/Art/Prototype/Ground/Pebbles";
        private const string GroundRockFolder = "Assets/_Project/Art/Prototype/Ground/Rocks";
        private const string GroundSmoothFolder = "Assets/_Project/Art/Prototype/Ground/Smooth";
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        private const string VolumeProfilePath = "Assets/_Project/Settings/Profiles/DefaultVolumeProfile.asset";
        private const int RuleGroundTextureSize = 16;
        private const int ShadowSortingOrder = 6;
        private const int ActorSortingOrder = 7;

        [MenuItem("Booter & BigARM/Prototype/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                throw new System.InvalidOperationException($"Missing input actions asset at '{InputActionsPath}'.");
            }

            var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (volumeProfile == null)
            {
                throw new System.InvalidOperationException($"Missing volume profile at '{VolumeProfilePath}'.");
            }

            var playerSprite = EnsurePlayerSpriteAsset();
            var shadowSprite = EnsureShadowSpriteAsset();
            var tallPropSprite = EnsureTallPropSpriteAsset();
            var tallPropPrefabs = EnsureTallPropPrefabs(tallPropSprite, shadowSprite);
            var propCatalog = EnsurePrototypePropCatalog();
            var sandPatchRuleTile = LoadSandPatchRuleTileAsset();
            var sandSprites = EnsureSandSprites();
            var sandOverlaySprites = EnsureSandOverlaySprites();
            var pebbleSprites = EnsurePebbleSprites();
            var rockSprites = EnsureRockSprites();
            var smoothSprites = EnsureSmoothSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PrototypeScene";

            var homeAnchor = CreateHomeAnchor(playerSprite);
            var player = CreatePlayer(inputActions, playerSprite, shadowSprite, homeAnchor);
            var cameraTarget = CreateCameraTarget(player);
            var world = CreateWorld(
                player.transform,
                tallPropPrefabs,
                propCatalog,
                sandPatchRuleTile,
                sandSprites,
                sandOverlaySprites,
                pebbleSprites,
                rockSprites,
                smoothSprites);
            var saveLoadController = CreateSessionSystems(
                inputActions,
                player.GetComponent<PlayerMotor2D>(),
                world,
                player.GetComponent<PrototypeSurvivalState>());
            CreateCamera(cameraTarget);
            CreateLighting();
            CreateVolume(volumeProfile);
            CreateDebugOverlay(player.GetComponent<PlayerMotor2D>(), world, saveLoadController);
            CreateSurvivalHud(player.GetComponent<PrototypeSurvivalState>());

            EditorSceneManager.SaveScene(scene, PrototypeScenePath);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(PrototypeScenePath);
        }

        [MenuItem("Booter & BigARM/Prototype/Repair Prototype Prop Setup")]
        public static void RepairPrototypePropSetup()
        {
            var scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            var catalog = EnsurePrototypePropCatalog();
            var boulderPrefabs = LoadBoulderPrefabs();
            var sandOverlaySprites = EnsureSandOverlaySprites();

            var worldSettings = UnityEngine.Object.FindFirstObjectByType<PrototypeWorldSettings>();
            if (worldSettings == null)
            {
                throw new InvalidOperationException(
                    $"Unable to find {nameof(PrototypeWorldSettings)} in '{PrototypeScenePath}'.");
            }

            SetObjectReference(worldSettings, "propCatalog", catalog);

            var generator = UnityEngine.Object.FindFirstObjectByType<PrototypeWorldGenerator>();
            if (generator == null)
            {
                throw new InvalidOperationException(
                    $"Unable to find {nameof(PrototypeWorldGenerator)} in '{PrototypeScenePath}'.");
            }

            for (var i = 0; i < boulderPrefabs.Length; i++)
            {
                EnsurePrefabInObjectArray(generator, "propPrefabs", boulderPrefabs[i]);
            }

            var sandOverlayGrid = generator.transform.parent != null ? generator.transform.parent.parent?.Find(SandOverlayGridName) : null;
            if (sandOverlayGrid != null)
            {
                sandOverlayGrid.localPosition = new Vector3(0.5f, 0.5f, 0f);
                var sandOverlayGridComponent = sandOverlayGrid.GetComponent<Grid>();
                if (sandOverlayGridComponent != null)
                {
                    sandOverlayGridComponent.cellSize = new Vector3(4f, 4f, 1f);
                }
            }

            var sandOverlayOffsetTilemap = EnsureOffsetSandOverlayTilemap(generator);
            SetObjectReference(generator, "sandOverlayOffsetTilemap", sandOverlayOffsetTilemap);
            SetObjectArray(generator, "sandOverlayTileSprites", sandOverlaySprites);
            SetObjectArray(generator, "sandOverlayOffsetTileSprites", sandOverlaySprites);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreatePlayer(
            InputActionAsset inputActions,
            Sprite prototypeSprite,
            Sprite shadowSprite,
            Transform homeAnchor)
        {
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var spriteRenderer = player.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = prototypeSprite;
            spriteRenderer.color = new Color(0.95f, 0.66f, 0.28f);
            spriteRenderer.sortingOrder = ActorSortingOrder;
            SetSpriteSortPoint(spriteRenderer, SpriteSortPoint.Pivot);

            var rigidbody = player.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f);

            CreateShadowChild(player.transform, shadowSprite, new Vector2(0.24f, -0.08f), new Vector2(1.0f, 0.28f));

            var inputAdapter = player.AddComponent<PlayerInputAdapter>();
            SetObjectReference(inputAdapter, "inputActions", inputActions);

            var motor = player.AddComponent<PlayerMotor2D>();
            SetFloat(motor, "walkSpeed", 5.4f);
            SetFloat(motor, "sprintSpeed", 7.2f);
            SetFloat(motor, "acceleration", 28f);
            SetFloat(motor, "deceleration", 34f);

            var survivalState = player.AddComponent<PrototypeSurvivalState>();
            SetObjectReference(survivalState, "playerMotor", motor);
            SetObjectReference(survivalState, "homeAnchor", homeAnchor);
            SetFloat(survivalState, "maxAlgaeReserve", 100f);
            SetFloat(survivalState, "algaeReserve", 100f);
            SetFloat(survivalState, "travelDrainPerSecond", 0.9f);
            SetFloat(survivalState, "sprintDrainPerSecond", 1.4f);
            SetFloat(survivalState, "homeRegenPerSecond", 4f);
            SetFloat(survivalState, "idleRegenPerSecond", 0.4f);
            SetFloat(survivalState, "safeZoneRadius", 8f);
            SetFloat(survivalState, "lowReserveSpeedFloor", 0.72f);
            return player;
        }

        private static Transform CreateCameraTarget(GameObject player)
        {
            var cameraTargetObject = new GameObject("Camera Target");
            cameraTargetObject.transform.SetParent(player.transform, false);
            cameraTargetObject.transform.localPosition = Vector3.zero;

            var inputAdapter = player.GetComponent<PlayerInputAdapter>();
            var controller = cameraTargetObject.AddComponent<PrototypeCameraTargetController>();
            SetObjectReference(controller, "player", player.transform);
            SetObjectReference(controller, "inputAdapter", inputAdapter);

            return cameraTargetObject.transform;
        }

        private static PrototypeSaveLoadController CreateSessionSystems(
            InputActionAsset inputActions,
            PlayerMotor2D playerMotor,
            PrototypeWorldGenerator worldGenerator,
            PrototypeSurvivalState survivalState)
        {
            var controllerObject = new GameObject("Prototype Session");
            var controller = controllerObject.AddComponent<PrototypeSaveLoadController>();
            SetObjectReference(controller, "playerMotor", playerMotor);
            SetObjectReference(controller, "worldGenerator", worldGenerator);
            SetObjectReference(controller, "survivalState", survivalState);
            var systemInput = controllerObject.AddComponent<PrototypeSystemInputAdapter>();
            SetObjectReference(systemInput, "inputActions", inputActions);
            SetObjectReference(systemInput, "saveLoadController", controller);
            return controller;
        }

        private static PrototypeWorldGenerator CreateWorld(
            Transform player,
            GameObject[] tallPropPrefabs,
            PrototypeWorldPropCatalog propCatalog,
            RuleTile sandPatchRuleTile,
            Sprite[] sandSprites,
            Sprite[] sandOverlaySprites,
            Sprite[] pebbleSprites,
            Sprite[] rockSprites,
            Sprite[] smoothSprites)
        {
            var worldRoot = new GameObject("World");
            var worldSettings = worldRoot.AddComponent<PrototypeWorldSettings>();
            SetFloat(worldSettings, "propSpawnChance", 0.12f);
            SetObjectReference(worldSettings, "propCatalog", propCatalog);
            var propRoot = new GameObject("Tall Props");
            propRoot.transform.SetParent(worldRoot.transform, false);

            var sandPatchGrid = CreateGrid(worldRoot.transform, "Sand Patch Grid", new Vector3(0.5f, 0.5f, 1f));
            var groundGrid = CreateGrid(worldRoot.transform, "Ground Grid", Vector3.one);
            var sandGrid = CreateGrid(worldRoot.transform, "Sand Grid", new Vector3(2f, 2f, 1f));
            var sandOverlayGrid = CreateGrid(worldRoot.transform, SandOverlayGridName, new Vector3(4f, 4f, 1f), new Vector3(0.5f, 0.5f, 0f));
            var sandOverlayOffsetGrid = CreateGrid(worldRoot.transform, SandOverlayOffsetGridName, new Vector3(4f, 4f, 1f), new Vector3(1f, 1f, 0f));

            var sandPatchTilemap = CreateTilemapLayer(sandPatchGrid.transform, "Sand Patch Tilemap", 5);
            var sandTilemap = CreateTilemapLayer(sandGrid.transform, "Sand Tilemap", 0);
            var sandOverlayTilemap = CreateTilemapLayer(sandOverlayGrid.transform, "Sand Overlay Tilemap", 1);
            var sandOverlayOffsetTilemap = CreateTilemapLayer(sandOverlayOffsetGrid.transform, SandOverlayOffsetTilemapName, 1);
            var pebbleTilemap = CreateTilemapLayer(groundGrid.transform, "Pebble Tilemap", 2);
            var rockTilemap = CreateTilemapLayer(groundGrid.transform, "Rock Tilemap", 3);
            var smoothTilemap = CreateTilemapLayer(groundGrid.transform, "Smooth Tilemap", 4);

            var generator = sandTilemap.gameObject.AddComponent<PrototypeWorldGenerator>();
            SetObjectReference(generator, "target", player);
            SetObjectReference(generator, "worldSettings", worldSettings);
            SetObjectReference(generator, "ruleGroundTilemap", sandPatchTilemap);
            SetObjectReference(generator, "ruleGroundTile", sandPatchRuleTile);
            SetObjectArray(generator, "tileSprites", sandSprites);
            SetObjectReference(generator, "sandOverlayTilemap", sandOverlayTilemap);
            SetObjectArray(generator, "sandOverlayTileSprites", sandOverlaySprites);
            SetObjectReference(generator, "sandOverlayOffsetTilemap", sandOverlayOffsetTilemap);
            SetObjectArray(generator, "sandOverlayOffsetTileSprites", sandOverlaySprites);
            SetObjectArray(generator, "propPrefabs", tallPropPrefabs);
            SetObjectReference(generator, "propParent", propRoot.transform);
            SetFloat(generator, "propSpawnChance", 0.12f);
            SetObjectReference(generator, "pebbleTilemap", pebbleTilemap);
            SetObjectArray(generator, "pebbleTileSprites", pebbleSprites);
            SetObjectReference(generator, "rockTilemap", rockTilemap);
            SetObjectArray(generator, "rockTileSprites", rockSprites);
            SetObjectReference(generator, "smoothTilemap", smoothTilemap);
            SetObjectArray(generator, "smoothTileSprites", smoothSprites);
            SetInt(generator, "seed", 4829);
            SetInt(generator, "chunkSize", 16);
            SetInt(generator, "chunkRadius", 4);

            return generator;
        }

        private static Grid CreateGrid(Transform parent, string name, Vector3 cellSize, Vector3? localPosition = null)
        {
            var gridObject = new GameObject(name);
            gridObject.transform.SetParent(parent, false);
            gridObject.transform.localPosition = localPosition ?? Vector3.zero;

            var grid = gridObject.AddComponent<Grid>();
            grid.cellSize = cellSize;
            return grid;
        }

        private static Tilemap CreateTilemapLayer(Transform parent, string name, int sortingOrder)
        {
            var layerObject = new GameObject(name);
            layerObject.transform.SetParent(parent, false);

            layerObject.AddComponent<Tilemap>();
            var renderer = layerObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            EnableAutomaticChunkCullingBounds(renderer);
            return layerObject.GetComponent<Tilemap>();
        }

        private static Tilemap EnsureOffsetSandOverlayTilemap(PrototypeWorldGenerator generator)
        {
            if (generator == null)
            {
                return null;
            }

            var worldRoot = generator.transform.parent != null ? generator.transform.parent.parent : null;
            if (worldRoot == null)
            {
                return null;
            }

            var gridTransform = worldRoot.Find(SandOverlayOffsetGridName);
            Grid grid;
            if (gridTransform == null)
            {
                grid = CreateGrid(worldRoot, SandOverlayOffsetGridName, new Vector3(4f, 4f, 1f), new Vector3(1f, 1f, 0f));
            }
            else
            {
                grid = gridTransform.GetComponent<Grid>();
                if (grid == null)
                {
                    grid = gridTransform.gameObject.AddComponent<Grid>();
                }

                grid.cellSize = new Vector3(4f, 4f, 1f);
                gridTransform.localPosition = new Vector3(1f, 1f, 0f);
            }

            var tilemapTransform = grid.transform.Find(SandOverlayOffsetTilemapName);
            if (tilemapTransform != null)
            {
                var existingTilemap = tilemapTransform.GetComponent<Tilemap>();
                if (existingTilemap != null)
                {
                    var existingRenderer = tilemapTransform.GetComponent<TilemapRenderer>();
                    if (existingRenderer != null)
                    {
                        existingRenderer.sortingOrder = 1;
                        EnableAutomaticChunkCullingBounds(existingRenderer);
                    }

                    return existingTilemap;
                }
            }

            return CreateTilemapLayer(grid.transform, SandOverlayOffsetTilemapName, 1);
        }

        private static void EnableAutomaticChunkCullingBounds(TilemapRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Auto;
        }

        private static void CreateCamera(Transform target)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = new Vector3(0f, 1f, 0f);

            cameraObject.AddComponent<AudioListener>();
            var pixelPerfectCamera = cameraObject.AddComponent<UnityEngine.U2D.PixelPerfectCamera>();
            pixelPerfectCamera.assetsPPU = 100;
            pixelPerfectCamera.refResolutionX = 640;
            pixelPerfectCamera.refResolutionY = 360;
            pixelPerfectCamera.upscaleRT = false;
            pixelPerfectCamera.pixelSnapping = false;
            pixelPerfectCamera.cropFrameX = false;
            pixelPerfectCamera.cropFrameY = false;
            pixelPerfectCamera.stretchFill = false;

            var additionalData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            additionalData.renderShadows = true;
            additionalData.renderPostProcessing = true;

            cameraObject.AddComponent<CinemachineBrain>();

            var cinemachineCameraObject = new GameObject("Gameplay Camera");
            cinemachineCameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var cinemachineCamera = cinemachineCameraObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Follow = target;
            var positionComposer = cinemachineCameraObject.AddComponent<CinemachinePositionComposer>();
            positionComposer.CameraDistance = 10f;
            positionComposer.DeadZoneDepth = 0f;
            positionComposer.CenterOnActivate = true;
            positionComposer.TargetOffset = Vector3.zero;
            positionComposer.Damping = Vector3.zero;
            positionComposer.Lookahead = new LookaheadSettings
            {
                Enabled = true,
                Time = 0.08f,
                Smoothing = 8f,
                IgnoreY = false
            };
            cinemachineCameraObject.AddComponent<Unity.Cinemachine.CinemachinePixelPerfect>();
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Lighting");
            var light2D = lightObject.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.72f, 0.82f, 1f);
            light2D.intensity = 0.9f;
        }

        private static void CreateVolume(VolumeProfile profile)
        {
            var volumeObject = new GameObject("Post Processing");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
        }

        private static void CreateDebugOverlay(
            PlayerMotor2D motor,
            PrototypeWorldGenerator generator,
            PrototypeSaveLoadController saveLoadController)
        {
            var overlayObject = new GameObject("Debug Overlay");
            var overlay = overlayObject.AddComponent<PrototypeDebugOverlay>();
            SetObjectReference(overlay, "playerMotor", motor);
            SetObjectReference(overlay, "worldGenerator", generator);
            SetObjectReference(overlay, "saveLoadController", saveLoadController);
        }

        private static void CreateSurvivalHud(PrototypeSurvivalState survivalState)
        {
            var hudObject = new GameObject("Survival HUD");
            var hud = hudObject.AddComponent<PrototypeSurvivalHud>();
            SetObjectReference(hud, "survivalState", survivalState);
        }

        private static Transform CreateHomeAnchor(Sprite prototypeSprite)
        {
            var homeObject = new GameObject("Home Anchor");
            homeObject.transform.position = Vector3.zero;

            var spriteRenderer = homeObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = prototypeSprite;
            spriteRenderer.color = new Color(0.42f, 0.81f, 0.94f, 1f);
            spriteRenderer.sortingOrder = 4;
            homeObject.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            return homeObject.transform;
        }

        private static Sprite EnsurePlayerSpriteAsset()
        {
            var folderPath = Path.GetDirectoryName(PlayerSpritePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var texture = CreatePrototypeTexture();
            File.WriteAllBytes(PlayerSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(PlayerSpritePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(PlayerSpritePath);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        }

        private static Sprite EnsureShadowSpriteAsset()
        {
            var folderPath = Path.GetDirectoryName(ShadowSpritePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var texture = CreateShadowTexture();
            File.WriteAllBytes(ShadowSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(ShadowSpritePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(ShadowSpritePath);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSpritePath);
        }

        private static Sprite EnsureTallPropSpriteAsset()
        {
            var folderPath = Path.GetDirectoryName(TallPropSpritePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var texture = CreateTallPropTexture();
            File.WriteAllBytes(TallPropSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(TallPropSpritePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(TallPropSpritePath);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(TallPropSpritePath);
        }

        private static GameObject[] EnsureTallPropPrefabs(Sprite tallPropSprite, Sprite shadowSprite)
        {
            var definitions = new[]
            {
                new TallPropPrefabDefinition(
                    TallPropPrefabPath,
                    "Tall Prop Placeholder",
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0.32f, -0.07f),
                    new Vector2(1.25f, 0.28f)),
                new TallPropPrefabDefinition(
                    TallPropWidePrefabPath,
                    "Tall Prop Wide",
                    new Vector2(2f, 1f),
                    new Vector2(1f, 2f),
                    new Vector2(0f, 1f),
                    new Vector2(0.52f, -0.08f),
                    new Vector2(1.9f, 0.38f)),
                new TallPropPrefabDefinition(
                    TallPropTallPrefabPath,
                    "Tall Prop Tall",
                    new Vector2(2f, 3f),
                    new Vector2(2f, 2f),
                    new Vector2(0f, 1f),
                    new Vector2(0.64f, -0.10f),
                    new Vector2(2.45f, 0.56f)),
                new TallPropPrefabDefinition(
                    TallPropSquarePrefabPath,
                    "Tall Prop Square",
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0.24f, -0.06f),
                    new Vector2(0.92f, 0.26f)),
                new TallPropPrefabDefinition(
                    TallPropTinyPrefabPath,
                    "Tall Prop Tiny",
                    new Vector2(0.5f, 0.25f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0.25f),
                    new Vector2(0.16f, -0.04f),
                    new Vector2(0.62f, 0.16f)),
                new TallPropPrefabDefinition(
                    TallPropSlimPrefabPath,
                    "Tall Prop Slim",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0.20f, -0.05f),
                    new Vector2(0.72f, 0.22f))
            };

            var prefabs = new List<GameObject>(definitions.Length);
            foreach (var definition in definitions)
            {
                prefabs.Add(EnsureTallPropPrefab(definition, tallPropSprite, shadowSprite));
            }

            var boulderPrefabs = LoadBoulderPrefabs();
            for (var i = 0; i < boulderPrefabs.Length; i++)
            {
                if (boulderPrefabs[i] != null)
                {
                    prefabs.Add(boulderPrefabs[i]);
                }
            }

            return prefabs.ToArray();
        }

        private static PrototypeWorldPropCatalog EnsurePrototypePropCatalog()
        {
            var folderPath = Path.GetDirectoryName(PrototypePropCatalogPath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeWorldPropCatalog>(PrototypePropCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PrototypeWorldPropCatalog>();
                AssetDatabase.CreateAsset(catalog, PrototypePropCatalogPath);
            }

            var serializedObject = new SerializedObject(catalog);
            var entriesProperty = serializedObject.FindProperty("entries");
            var biomeGroupsProperty = serializedObject.FindProperty("biomeGroups");
            entriesProperty.ClearArray();
            biomeGroupsProperty.ClearArray();

            var greaterWastelandEntriesProperty = AddBiomeGroup(biomeGroupsProperty, GreaterWastelandBiomeId);
            var boulderPrefabPaths = FindBoulderPrefabPaths();
            for (var i = 0; i < boulderPrefabPaths.Count; i++)
            {
                AddCatalogEntry(
                    greaterWastelandEntriesProperty,
                    $"boulder_32_{i + 1}",
                    boulderPrefabPaths[i],
                    PrototypeWorldPropCategory.Boulders,
                    PrototypeWorldPropSizeClass.Px32,
                    4.5f,
                    5f);
            }
            AddCatalogEntry(greaterWastelandEntriesProperty, "tall_prop_wide", TallPropWidePrefabPath, PrototypeWorldPropCategory.BuildingClutter, PrototypeWorldPropSizeClass.Px64, 1f, 1f);
            AddCatalogEntry(greaterWastelandEntriesProperty, "tall_prop_tall", TallPropTallPrefabPath, PrototypeWorldPropCategory.BuildingClutter, PrototypeWorldPropSizeClass.Px96, 0.8f, 1f);
            AddCatalogEntry(greaterWastelandEntriesProperty, "tall_prop_square", TallPropSquarePrefabPath, PrototypeWorldPropCategory.BuildingClutter, PrototypeWorldPropSizeClass.Px32, 1f, 1f);
            AddCatalogEntry(greaterWastelandEntriesProperty, "tall_prop_tiny", TallPropTinyPrefabPath, PrototypeWorldPropCategory.BuildingClutter, PrototypeWorldPropSizeClass.Px16, 0.9f, 1f);
            AddCatalogEntry(greaterWastelandEntriesProperty, "tall_prop_slim", TallPropSlimPrefabPath, PrototypeWorldPropCategory.BuildingClutter, PrototypeWorldPropSizeClass.Px16, 0.9f, 1f);

            AddBiomeGroup(biomeGroupsProperty, ReefBiomeId);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static GameObject[] LoadBoulderPrefabs()
        {
            var prefabPaths = FindBoulderPrefabPaths();
            var prefabs = new GameObject[prefabPaths.Count];
            for (var i = 0; i < prefabPaths.Count; i++)
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
            }

            return prefabs;
        }

        private static List<string> FindBoulderPrefabPaths()
        {
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { BoulderPrefabFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }

                if (!fileName.Contains("boulder_32", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                paths.Add(path);
            }

            paths.Sort(StringComparer.OrdinalIgnoreCase);
            return paths;
        }

        private static void AddCatalogEntry(
            SerializedProperty entriesProperty,
            string id,
            string prefabPath,
            PrototypeWorldPropCategory category,
            PrototypeWorldPropSizeClass sizeClass,
            float weight,
            float spawnChanceMultiplier)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return;
            }

            var index = entriesProperty.arraySize;
            entriesProperty.InsertArrayElementAtIndex(index);
            var entry = entriesProperty.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("category").enumValueIndex = (int)category;
            entry.FindPropertyRelative("sizeClass").enumValueIndex = (int)sizeClass;
            entry.FindPropertyRelative("weight").floatValue = weight;
            entry.FindPropertyRelative("spawnChanceMultiplier").floatValue = spawnChanceMultiplier;
            entry.FindPropertyRelative("enabled").boolValue = true;
        }

        private static SerializedProperty AddBiomeGroup(SerializedProperty biomeGroupsProperty, string biomeId)
        {
            var index = biomeGroupsProperty.arraySize;
            biomeGroupsProperty.InsertArrayElementAtIndex(index);
            var group = biomeGroupsProperty.GetArrayElementAtIndex(index);
            group.FindPropertyRelative("biomeId").stringValue = biomeId;
            var entriesProperty = group.FindPropertyRelative("entries");
            entriesProperty.ClearArray();
            return entriesProperty;
        }

        private static GameObject EnsureTallPropPrefab(TallPropPrefabDefinition definition, Sprite tallPropSprite, Sprite shadowSprite)
        {
            var folderPath = Path.GetDirectoryName(definition.PrefabPath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var prop = new GameObject(definition.Name);
            var collider = prop.AddComponent<BoxCollider2D>();
            collider.size = definition.ColliderSize;
            collider.offset = definition.ColliderOffset;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(prop.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(definition.VisualScale.x, definition.VisualScale.y, 1f);

            var spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = tallPropSprite;
            spriteRenderer.sortingOrder = ActorSortingOrder;
            SetSpriteSortPoint(spriteRenderer, SpriteSortPoint.Pivot);

            CreateShadowChild(prop.transform, shadowSprite, definition.ShadowLocalPosition, definition.ShadowLocalScale);

            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(prop, definition.PrefabPath);
            UnityEngine.Object.DestroyImmediate(prop);

            if (prefabAsset == null)
            {
                throw new System.InvalidOperationException(
                    $"Unable to save prefab asset at '{definition.PrefabPath}'.");
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath);
        }

        private static Sprite[] EnsureSandSprites()
        {
            var sprites = LoadPsdSprites(GroundSandPsdPath, "tilemap_sand");

            if (sprites.Length > 0)
            {
                return sprites;
            }

            var definitions = new[]
            {
                new GroundSpriteDefinition("SandRustA.png", new Color32(173, 136, 86, 255), new Color32(197, 160, 106, 255), new Color32(112, 79, 39, 255), GroundPattern.CrossHatch),
                new GroundSpriteDefinition("SandRustB.png", new Color32(179, 141, 88, 255), new Color32(202, 166, 111, 255), new Color32(116, 83, 43, 255), GroundPattern.Diamond),
                new GroundSpriteDefinition("SandRustC.png", new Color32(169, 132, 82, 255), new Color32(194, 155, 100, 255), new Color32(106, 75, 36, 255), GroundPattern.Ring),
                new GroundSpriteDefinition("SandRustD.png", new Color32(181, 145, 92, 255), new Color32(206, 170, 114, 255), new Color32(119, 85, 45, 255), GroundPattern.Dots)
            };

            var fallbackSprites = new List<Sprite>(definitions.Length);
            foreach (var definition in definitions)
            {
                var spritePath = Path.Combine(GroundSandFolder, definition.FileName);
                EnsureSpriteAsset(spritePath, definition.BaseColor, definition.HighlightColor, definition.BorderColor, definition.Pattern);
                fallbackSprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath));
            }

            return fallbackSprites.ToArray();
        }

        private static Sprite[] EnsureSandOverlaySprites()
        {
            var sprites = LoadPsdSprites(GroundSandOverlayPsdPath, "tilemap_sand_overlay_128");
            if (sprites.Length > 0)
            {
                return sprites;
            }

            return Array.Empty<Sprite>();
        }

        private static RuleTile LoadSandPatchRuleTileAsset()
        {
            var folderPath = Path.GetDirectoryName(SandPatchRuleTilePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(SandPatchRuleTilePath);
            if (ruleTile == null)
            {
                throw new System.InvalidOperationException(
                    $"Missing sand patch rule tile asset at '{SandPatchRuleTilePath}'.");
            }

            return AssetDatabase.LoadAssetAtPath<RuleTile>(SandPatchRuleTilePath);
        }

        private static Sprite[] LoadPsdSprites(string path, string spritePrefix)
        {
            return AssetDatabase
                .LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .Where(sprite => sprite.name.StartsWith(spritePrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(GetSpriteSortKey)
                .ThenBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static int GetSpriteSortKey(Sprite sprite)
        {
            if (sprite == null || string.IsNullOrEmpty(sprite.name))
            {
                return int.MaxValue;
            }

            var name = sprite.name;
            var end = name.Length - 1;
            while (end >= 0 && char.IsDigit(name[end]))
            {
                end--;
            }

            if (end < name.Length - 1 &&
                int.TryParse(name[(end + 1)..], out var index))
            {
                return index;
            }

            return int.MaxValue;
        }

        private static Sprite[] EnsurePebbleSprites()
        {
            var definitions = new[]
            {
                new GroundOverlayDefinition("PebblePatchA.png", new Color32(118, 102, 77, 235), GroundOverlayPattern.PebblesSmall),
                new GroundOverlayDefinition("PebblePatchB.png", new Color32(126, 109, 85, 235), GroundOverlayPattern.PebblesPatch),
                new GroundOverlayDefinition("PebblePatchC.png", new Color32(109, 95, 72, 235), GroundOverlayPattern.PebblesMixed),
                new GroundOverlayDefinition("PebblePatchD.png", new Color32(98, 86, 66, 235), GroundOverlayPattern.PebblesSparse)
            };

            var sprites = new List<Sprite>(definitions.Length);
            foreach (var definition in definitions)
            {
                var spritePath = Path.Combine(GroundPebbleFolder, definition.FileName);
                EnsureSpriteAsset(spritePath, definition.TintColor, definition.Pattern);
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath));
            }

            return sprites.ToArray();
        }

        private static Sprite[] EnsureRockSprites()
        {
            var definitions = new[]
            {
                new GroundOverlayDefinition("RockPatchA.png", new Color32(105, 103, 99, 240), GroundOverlayPattern.RocksSmall),
                new GroundOverlayDefinition("RockPatchB.png", new Color32(117, 114, 109, 240), GroundOverlayPattern.RocksPatch),
                new GroundOverlayDefinition("RockPatchC.png", new Color32(127, 123, 118, 235), GroundOverlayPattern.RocksMixed),
                new GroundOverlayDefinition("RockPatchD.png", new Color32(91, 89, 85, 240), GroundOverlayPattern.RocksSparse)
            };

            var sprites = new List<Sprite>(definitions.Length);
            foreach (var definition in definitions)
            {
                var spritePath = Path.Combine(GroundRockFolder, definition.FileName);
                EnsureSpriteAsset(spritePath, definition.TintColor, definition.Pattern);
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath));
            }

            return sprites.ToArray();
        }

        private static Sprite[] EnsureSmoothSprites()
        {
            var definitions = new[]
            {
                new GroundOverlayDefinition("SmoothSandA.png", new Color32(225, 200, 143, 140), GroundOverlayPattern.SmoothPatchA),
                new GroundOverlayDefinition("SmoothSandB.png", new Color32(233, 208, 149, 135), GroundOverlayPattern.SmoothPatchB),
                new GroundOverlayDefinition("SmoothSandC.png", new Color32(217, 191, 134, 140), GroundOverlayPattern.SmoothPatchC),
                new GroundOverlayDefinition("SmoothSandD.png", new Color32(228, 203, 145, 135), GroundOverlayPattern.SmoothPatchD)
            };

            var sprites = new List<Sprite>(definitions.Length);
            foreach (var definition in definitions)
            {
                var spritePath = Path.Combine(GroundSmoothFolder, definition.FileName);
                EnsureSpriteAsset(spritePath, definition.TintColor, definition.Pattern);
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath));
            }

            return sprites.ToArray();
        }

        private static Texture2D CreatePrototypeTexture()
        {
            const int width = 26;
            const int height = 32;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var transparent = new Color32(0, 0, 0, 0);
            var outline = new Color32(74, 54, 30, 255);
            var fill = new Color32(214, 176, 90, 255);
            var highlight = new Color32(238, 210, 132, 255);
            var shadow = new Color32(178, 136, 54, 255);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var color = transparent;
                    var insideBody = x >= 1 && x < width - 1 && y >= 1 && y < height - 1;

                    if (insideBody)
                    {
                        color = fill;

                        var onEdge = x == 1 || x == width - 2 || y == 1 || y == height - 2;
                        var inHighlightBand = x >= 7 && x <= 16 && y >= 11 && y <= 27;
                        var inShadowBand = x >= 17 || y <= 6;

                        if (onEdge)
                        {
                            color = outline;
                        }
                        else if (inHighlightBand)
                        {
                            color = highlight;
                        }
                        else if (inShadowBand)
                        {
                            color = shadow;
                        }
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateTallPropTexture()
        {
            const int width = 32;
            const int height = 64;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var outline = new Color32(74, 54, 30, 255);
            var fill = new Color32(173, 136, 86, 255);
            var highlight = new Color32(202, 166, 111, 255);
            var shadow = new Color32(116, 83, 43, 255);
            var accent = new Color32(230, 191, 125, 255);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var color = fill;
                    var baseZone = y < 32;
                    var columnZone = y >= 20;
                    var onOuterEdge = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    var inColumn = columnZone && x >= 10 && x <= 21;
                    var inCap = y >= 50 && x >= 8 && x <= 23;
                    var inBaseInset = baseZone && x >= 5 && x <= 26 && y >= 6 && y <= 28;

                    if (onOuterEdge)
                    {
                        color = outline;
                    }
                    else if (inCap)
                    {
                        color = accent;
                    }
                    else if (inColumn)
                    {
                        color = Blend(fill, highlight, (x - 10) / 11f);
                    }
                    else if (inBaseInset)
                    {
                        color = Blend(fill, shadow, 0.35f);
                    }
                    else if (baseZone && y >= 29)
                    {
                        color = shadow;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            for (var y = 29; y < 34; y++)
            {
                for (var x = 6; x < 26; x++)
                {
                    texture.SetPixel(x, y, Blend(fill, outline, 0.25f));
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateShadowTexture()
        {
            const int width = 64;
            const int height = 16;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var transparent = new Color32(0, 0, 0, 0);
            var shadow = new Color32(52, 42, 28, 180);
            var core = new Color32(38, 31, 22, 210);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var nx = x / (width - 1f);
                    var ny = Mathf.Abs((y - (height - 1) * 0.5f) / ((height - 1) * 0.5f));
                    var body = 1f - Mathf.Clamp01(ny * ny * 1.5f);
                    var trail = 1f - Mathf.Clamp01((nx - 0.08f) / 0.92f);
                    var rootFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((x - 2f) / 8f));
                    var alpha = body * Mathf.Max(trail, 0.08f) * Mathf.Lerp(0.25f, 1f, rootFade);

                    if (alpha <= 0.01f)
                    {
                        texture.SetPixel(x, y, transparent);
                        continue;
                    }

                    var color = Blend(shadow, core, Mathf.Clamp01((1f - nx) * 0.7f));
                    color.a = (byte)Mathf.RoundToInt(255f * alpha);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static void CreateShadowChild(Transform parent, Sprite shadowSprite, Vector2 localPosition, Vector2 localScale)
        {
            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(parent, false);
            shadowObject.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            shadowObject.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);

            var spriteRenderer = shadowObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = shadowSprite;
            spriteRenderer.sortingOrder = ShadowSortingOrder;
            spriteRenderer.color = Color.white;
            SetSpriteSortPoint(spriteRenderer, SpriteSortPoint.Pivot);
        }

        private static void EnsureSpriteAsset(
            string assetPath,
            Color32 fill,
            Color32 highlight,
            Color32 border,
            GroundPattern pattern)
        {
            var folderPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var texture = CreateTileTexture(fill, highlight, border, pattern);
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }

        private static void EnsureSpriteAsset(
            string assetPath,
            Color32 tint,
            GroundOverlayPattern pattern)
        {
            var folderPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var texture = CreateOverlayTexture(tint, pattern);
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static Texture2D CreateTileTexture(Color32 fill, Color32 highlight, Color32 border, GroundPattern pattern)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var seed = GetSeed(fill, highlight, border, pattern);
            var contrast = pattern switch
            {
                GroundPattern.Square => 0.30f,
                GroundPattern.CrossHatch => 0.24f,
                GroundPattern.Diamond => 0.28f,
                GroundPattern.Ring => 0.22f,
                GroundPattern.Dots => 0.18f,
                _ => 0.24f
            };
            var grainStrength = pattern switch
            {
                GroundPattern.Square => 0.08f,
                GroundPattern.CrossHatch => 0.10f,
                GroundPattern.Diamond => 0.08f,
                GroundPattern.Ring => 0.10f,
                GroundPattern.Dots => 0.12f,
                _ => 0.08f
            };

            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    var low = TileableValueNoise(seed, x, y, 4);
                    var mid = TileableValueNoise(seed + 17, x, y, 8);
                    var fine = TileableValueNoise(seed + 41, x, y, 16);
                    var grain = TileableValueNoise(seed + 73, x, y, 32);

                    var color = Blend(fill, highlight, 0.5f + low * contrast + mid * 0.12f + fine * 0.05f);
                    if (grain > 0.52f)
                    {
                        color = Blend(color, border, (grain - 0.52f) * grainStrength);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateOverlayTexture(Color32 tint, GroundOverlayPattern pattern)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var rng = new System.Random(GetSeed(tint, pattern));
            for (var y = 1; y < 31; y++)
            {
                for (var x = 1; x < 31; x++)
                {
                    texture.SetPixel(x, y, new Color32(0, 0, 0, 0));
                }
            }

            switch (pattern)
            {
                case GroundOverlayPattern.PebblesSmall:
                    DrawPebbleTile(texture, tint, rng, 2, 0);
                    break;
                case GroundOverlayPattern.PebblesPatch:
                    DrawPebbleTile(texture, tint, rng, 2, 1);
                    break;
                case GroundOverlayPattern.PebblesMixed:
                    DrawPebbleTile(texture, tint, rng, 3, 0);
                    break;
                case GroundOverlayPattern.PebblesSparse:
                    DrawPebbleTile(texture, tint, rng, 1, 1);
                    break;
                case GroundOverlayPattern.RocksSmall:
                    DrawRockTile(texture, tint, rng, 3, 0);
                    break;
                case GroundOverlayPattern.RocksPatch:
                    DrawRockTile(texture, tint, rng, 3, 1);
                    break;
                case GroundOverlayPattern.RocksMixed:
                    DrawRockTile(texture, tint, rng, 4, 0);
                    break;
                case GroundOverlayPattern.RocksSparse:
                    DrawRockTile(texture, tint, rng, 2, 1);
                    break;
                case GroundOverlayPattern.SmoothPatchA:
                    DrawSmoothPatch(texture, tint, rng, 6, 6, 25, 25, 4);
                    break;
                case GroundOverlayPattern.SmoothPatchB:
                    DrawSmoothPatch(texture, tint, rng, 5, 7, 26, 24, 3);
                    break;
                case GroundOverlayPattern.SmoothPatchC:
                    DrawSmoothPatch(texture, tint, rng, 7, 5, 24, 26, 4);
                    break;
                case GroundOverlayPattern.SmoothPatchD:
                    DrawSmoothPatch(texture, tint, rng, 6, 6, 23, 24, 5);
                    break;
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateRuleGroundTexture(int mask)
        {
            var size = RuleGroundTextureSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var seed = 0x5F4B19 + mask * 173;
            var baseColor = new Color32(172, 138, 86, 255);
            var highlightColor = new Color32(196, 166, 111, 255);
            var shadowColor = new Color32(112, 82, 48, 255);
            var gritColor = new Color32(139, 112, 73, 255);
            var rng = new System.Random(seed);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var low = TileableValueNoise(seed, x, y, size, 2);
                    var mid = TileableValueNoise(seed + 17, x, y, size, 4);
                    var fine = TileableValueNoise(seed + 41, x, y, size, 8);
                    var color = Blend(baseColor, highlightColor, 0.45f + low * 0.16f + mid * 0.08f + fine * 0.04f);

                    if (fine > 0.1f)
                    {
                        color = Blend(color, gritColor, (fine - 0.1f) * 0.14f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            ApplyRuleGroundEdge(texture, mask, 1, shadowColor, highlightColor);
            ApplyRuleGroundEdge(texture, mask, 2, shadowColor, highlightColor);
            ApplyRuleGroundEdge(texture, mask, 4, shadowColor, highlightColor);
            ApplyRuleGroundEdge(texture, mask, 8, shadowColor, highlightColor);
            SprinkleRuleGroundSpecks(texture, rng, highlightColor, shadowColor);

            texture.Apply(false, false);
            return texture;
        }

        private static void ApplyRuleGroundEdge(Texture2D texture, int mask, int bit, Color32 shadowColor, Color32 highlightColor)
        {
            var size = texture.width;
            var edgeSize = Mathf.Max(1, size / 8);
            var max = size - 1;
            var inner = Mathf.Max(0, size - edgeSize);
            var connected = (mask & bit) != 0;
            switch (bit)
            {
                case 1:
                {
                    for (var y = 0; y < edgeSize; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            var t = connected ? 0.08f : 0.38f;
                            var color = Blend(texture.GetPixel(x, y), connected ? highlightColor : shadowColor, t);
                            texture.SetPixel(x, y, color);
                        }
                    }
                    break;
                }
                case 2:
                {
                    for (var x = inner; x <= max; x++)
                    {
                        for (var y = 0; y < size; y++)
                        {
                            var t = connected ? 0.08f : 0.38f;
                            var color = Blend(texture.GetPixel(x, y), connected ? highlightColor : shadowColor, t);
                            texture.SetPixel(x, y, color);
                        }
                    }
                    break;
                }
                case 4:
                {
                    for (var y = inner; y <= max; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            var t = connected ? 0.08f : 0.38f;
                            var color = Blend(texture.GetPixel(x, y), connected ? shadowColor : shadowColor, t);
                            texture.SetPixel(x, y, color);
                        }
                    }
                    break;
                }
                case 8:
                {
                    for (var x = 0; x < edgeSize; x++)
                    {
                        for (var y = 0; y < size; y++)
                        {
                            var t = connected ? 0.08f : 0.38f;
                            var color = Blend(texture.GetPixel(x, y), connected ? highlightColor : shadowColor, t);
                            texture.SetPixel(x, y, color);
                        }
                    }
                    break;
                }
            }
        }

        private static void SprinkleRuleGroundSpecks(Texture2D texture, System.Random rng, Color32 highlightColor, Color32 shadowColor)
        {
            var size = texture.width;
            var min = Mathf.Min(2, size - 2);
            var max = Mathf.Max(min + 1, size - 2);
            for (var i = 0; i < 6; i++)
            {
                var x = rng.Next(min, max);
                var y = rng.Next(min, max);
                var color = (i & 1) == 0 ? highlightColor : shadowColor;
                texture.SetPixel(x, y, color);

                if (rng.NextDouble() > 0.55)
                {
                    texture.SetPixel(x + Mathf.Clamp(rng.Next(-1, 2), -1, 1), y, color);
                }
            }
        }

        private static void EnsureSpriteAsset(string assetPath, Texture2D texture, bool alphaIsTransparency)
        {
            var folderPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = alphaIsTransparency;
            importer.SaveAndReimport();
        }

        private static void DrawPebbleTile(Texture2D texture, Color32 tint, System.Random rng, int radius, int variantBias)
        {
            var centerX = 16 + rng.Next(-1 - variantBias, 2 + variantBias);
            var centerY = 16 + rng.Next(-1, 2);
            var highlight = Blend(tint, new Color32(223, 216, 200, tint.a), 0.25f);
            var shadow = Blend(tint, new Color32(74, 63, 52, tint.a), 0.35f);

            for (var y = -radius - 1; y <= radius + 1; y++)
            {
                for (var x = -radius - 1; x <= radius + 1; x++)
                {
                    var distance = Mathf.Sqrt(x * x + y * y);
                    if (distance > radius + 0.35f)
                    {
                        continue;
                    }

                    var baseColor = Blend(shadow, tint, Mathf.InverseLerp(radius + 0.35f, 0f, distance));
                    Put(texture, centerX + x, centerY + y, baseColor);
                }
            }

            Put(texture, centerX, centerY, highlight);
            Put(texture, centerX + 1, centerY, highlight);
        }

        private static void DrawRockTile(Texture2D texture, Color32 tint, System.Random rng, int radius, int variantBias)
        {
            var centerX = 16 + rng.Next(-1 - variantBias, 2 + variantBias);
            var centerY = 16 + rng.Next(-1, 2);
            var highlight = Blend(tint, new Color32(232, 230, 226, tint.a), 0.2f);
            var shadow = Blend(tint, new Color32(54, 52, 50, tint.a), 0.45f);

            for (var y = -radius - 1; y <= radius + 1; y++)
            {
                for (var x = -radius - 1; x <= radius + 1; x++)
                {
                    var chebyshev = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    if (chebyshev > radius)
                    {
                        continue;
                    }

                    if (chebyshev == radius && ((x + y + variantBias) & 1) == 1 && NextSignedNoise(rng) < 0.4f)
                    {
                        continue;
                    }

                    var falloff = Mathf.InverseLerp(radius + 0.5f, 0f, chebyshev);
                    var baseColor = Blend(shadow, tint, falloff);
                    Put(texture, centerX + x, centerY + y, baseColor);
                }
            }

            Put(texture, centerX, centerY, highlight);
        }

        private static void DrawSmoothPatch(Texture2D texture, Color32 tint, System.Random rng, int minX, int minY, int maxX, int maxY, int step)
        {
            for (var y = minY; y <= maxY; y += step)
            {
                for (var x = minX; x <= maxX; x += step)
                {
                    if (NextSignedNoise(rng) < -0.2f)
                    {
                        continue;
                    }

                    Put(texture, x, y, tint);
                    Put(texture, x + 1, y, tint);
                    Put(texture, x, y + 1, tint);
                    Put(texture, x + 1, y + 1, tint);
                }
            }
        }

        private static void Put(Texture2D texture, int x, int y, Color32 color)
        {
            if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static int GetSeed(Color32 a, Color32 b, Color32 c, GroundPattern pattern)
        {
            unchecked
            {
                return a.r
                    | (a.g << 8)
                    | (a.b << 16)
                    | (b.r << 1)
                    | (b.g << 9)
                    | (b.b << 17)
                    | (c.r << 2)
                    | (c.g << 10)
                    | (c.b << 18)
                    | (((int)pattern) << 24);
            }
        }

        private static int GetSeed(Color32 a, GroundOverlayPattern pattern)
        {
            unchecked
            {
                return a.r
                    | (a.g << 8)
                    | (a.b << 16)
                    | (((int)pattern) << 24);
            }
        }

        private static float NextSignedNoise(System.Random rng)
        {
            return (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        private static float Hash01(int worldSeed, int x, int y)
        {
            unchecked
            {
                var hash = (uint)worldSeed;
                hash ^= (uint)(x * 374761393);
                hash = (hash << 13) ^ hash;
                hash ^= (uint)(y * 668265263);
                hash *= 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static float TileableValueNoise(int seed, int x, int y, int cellsPerAxis)
        {
            return TileableValueNoise(seed, x, y, 32, cellsPerAxis);
        }

        private static float TileableValueNoise(int seed, int x, int y, int tileSize, int cellsPerAxis)
        {
            cellsPerAxis = Mathf.Max(1, cellsPerAxis);
            tileSize = Mathf.Max(1, tileSize);

            var fx = (x / (float)tileSize) * cellsPerAxis;
            var fy = (y / (float)tileSize) * cellsPerAxis;
            var x0 = Mathf.FloorToInt(fx);
            var y0 = Mathf.FloorToInt(fy);
            var tx = SmoothStep01(fx - x0);
            var ty = SmoothStep01(fy - y0);

            x0 = RepeatInt(x0, cellsPerAxis);
            y0 = RepeatInt(y0, cellsPerAxis);
            var x1 = RepeatInt(x0 + 1, cellsPerAxis);
            var y1 = RepeatInt(y0 + 1, cellsPerAxis);

            var n00 = Hash01(seed, x0, y0);
            var n10 = Hash01(seed, x1, y0);
            var n01 = Hash01(seed, x0, y1);
            var n11 = Hash01(seed, x1, y1);

            var nx0 = Mathf.Lerp(n00, n10, tx);
            var nx1 = Mathf.Lerp(n01, n11, tx);
            return Mathf.Lerp(nx0, nx1, ty) * 2f - 1f;
        }

        private static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static int RepeatInt(int value, int modulus)
        {
            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static Color32 Blend(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
        }

        private static void UpdateBuildSettings()
        {
            var scenePaths = new List<string>();
            if (File.Exists(PrototypeScenePath))
            {
                scenePaths.Add(PrototypeScenePath);
            }

            var sampleScenePath = "Assets/_Project/Scenes/SampleScene.unity";
            if (File.Exists(sampleScenePath) && !scenePaths.Contains(sampleScenePath))
            {
                scenePaths.Add(sampleScenePath);
            }

            var scenes = scenePaths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            EditorBuildSettings.scenes = scenes;
        }

        private static void SetObjectReference<T>(UnityEngine.Object target, string fieldName, T value)
            where T : UnityEngine.Object
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    $"Unable to find serialized field '{fieldName}' on {target.name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray<T>(UnityEngine.Object target, string fieldName, T[] values)
            where T : UnityEngine.Object
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    $"Unable to find serialized field '{fieldName}' on {target.name}.");
            }

            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetInt(UnityEngine.Object target, string fieldName, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    $"Unable to find serialized field '{fieldName}' on {target.name}.");
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsurePrefabInObjectArray<T>(UnityEngine.Object target, string fieldName, T value)
            where T : UnityEngine.Object
        {
            if (value == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null || !property.isArray)
            {
                throw new InvalidOperationException(
                    $"Unable to find serialized array field '{fieldName}' on {target.name}.");
            }

            for (var i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    return;
                }
            }

            var index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            property.GetArrayElementAtIndex(index).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(UnityEngine.Object target, string fieldName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    $"Unable to find serialized field '{fieldName}' on {target.name}.");
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSpriteSortPoint(UnityEngine.Object target, SpriteSortPoint value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty("m_SpriteSortPoint");
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    $"Unable to find serialized field 'm_SpriteSortPoint' on {target.name}.");
            }

            property.intValue = (int)value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private enum GroundPattern
        {
            Square,
            CrossHatch,
            Diamond,
            Ring,
            Dots
        }

        private enum GroundOverlayPattern
        {
            PebblesSmall,
            PebblesPatch,
            PebblesMixed,
            PebblesSparse,
            RocksSmall,
            RocksPatch,
            RocksMixed,
            RocksSparse,
            SmoothPatchA,
            SmoothPatchB,
            SmoothPatchC,
            SmoothPatchD
        }

        private readonly struct GroundSpriteDefinition
        {
            public GroundSpriteDefinition(string fileName, Color32 baseColor, Color32 highlightColor, Color32 borderColor, GroundPattern pattern)
            {
                FileName = fileName;
                BaseColor = baseColor;
                HighlightColor = highlightColor;
                BorderColor = borderColor;
                Pattern = pattern;
            }

            public string FileName { get; }
            public Color32 BaseColor { get; }
            public Color32 HighlightColor { get; }
            public Color32 BorderColor { get; }
            public GroundPattern Pattern { get; }
        }

        private readonly struct GroundOverlayDefinition
        {
            public GroundOverlayDefinition(string fileName, Color32 tintColor, GroundOverlayPattern pattern)
            {
                FileName = fileName;
                TintColor = tintColor;
                Pattern = pattern;
            }

            public string FileName { get; }
            public Color32 TintColor { get; }
            public GroundOverlayPattern Pattern { get; }
        }

        private readonly struct TallPropPrefabDefinition
        {
            public TallPropPrefabDefinition(
                string prefabPath,
                string name,
                Vector2 visualScale,
                Vector2 colliderSize,
                Vector2 colliderOffset,
                Vector2 shadowLocalPosition,
                Vector2 shadowLocalScale)
            {
                PrefabPath = prefabPath;
                Name = name;
                VisualScale = visualScale;
                ColliderSize = colliderSize;
                ColliderOffset = colliderOffset;
                ShadowLocalPosition = shadowLocalPosition;
                ShadowLocalScale = shadowLocalScale;
            }

            public string PrefabPath { get; }
            public string Name { get; }
            public Vector2 VisualScale { get; }
            public Vector2 ColliderSize { get; }
            public Vector2 ColliderOffset { get; }
            public Vector2 ShadowLocalPosition { get; }
            public Vector2 ShadowLocalScale { get; }
        }
    }
}
