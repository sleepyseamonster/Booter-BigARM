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
        private const string GroundSandFolder = "Assets/_Project/Art/Prototype/Ground/Sand";
        private const string GroundPebbleFolder = "Assets/_Project/Art/Prototype/Ground/Pebbles";
        private const string GroundRockFolder = "Assets/_Project/Art/Prototype/Ground/Rocks";
        private const string GroundSmoothFolder = "Assets/_Project/Art/Prototype/Ground/Smooth";
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        private const string VolumeProfilePath = "Assets/_Project/Settings/Profiles/DefaultVolumeProfile.asset";

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
            var sandSprites = EnsureSandSprites();
            var pebbleSprites = EnsurePebbleSprites();
            var rockSprites = EnsureRockSprites();
            var smoothSprites = EnsureSmoothSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PrototypeScene";

            var player = CreatePlayer(inputActions, playerSprite);
            var cameraTarget = CreateCameraTarget(player);
            var world = CreateWorld(player.transform, sandSprites, pebbleSprites, rockSprites, smoothSprites);
            CreateCamera(cameraTarget);
            CreateLighting();
            CreateVolume(volumeProfile);
            CreateDebugOverlay(player.GetComponent<PlayerMotor2D>(), world);

            EditorSceneManager.SaveScene(scene, PrototypeScenePath);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(PrototypeScenePath);
        }

        private static GameObject CreatePlayer(InputActionAsset inputActions, Sprite prototypeSprite)
        {
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var spriteRenderer = player.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = prototypeSprite;
            spriteRenderer.color = new Color(0.95f, 0.66f, 0.28f);
            spriteRenderer.sortingOrder = 10;

            var rigidbody = player.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f);

            var inputAdapter = player.AddComponent<PlayerInputAdapter>();
            SetObjectReference(inputAdapter, "inputActions", inputActions);

            player.AddComponent<PlayerMotor2D>();
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

        private static PrototypeWorldGenerator CreateWorld(
            Transform player,
            Sprite[] sandSprites,
            Sprite[] pebbleSprites,
            Sprite[] rockSprites,
            Sprite[] smoothSprites)
        {
            var worldRoot = new GameObject("World");

            var grid = worldRoot.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var sandTilemap = CreateTilemapLayer(worldRoot.transform, "Sand Tilemap", 0);
            var pebbleTilemap = CreateTilemapLayer(worldRoot.transform, "Pebble Tilemap", 1);
            var rockTilemap = CreateTilemapLayer(worldRoot.transform, "Rock Tilemap", 2);
            var smoothTilemap = CreateTilemapLayer(worldRoot.transform, "Smooth Tilemap", 3);

            var generator = sandTilemap.gameObject.AddComponent<PrototypeWorldGenerator>();
            SetObjectReference(generator, "target", player);
            SetObjectArray(generator, "tileSprites", sandSprites);
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

        private static Tilemap CreateTilemapLayer(Transform parent, string name, int sortingOrder)
        {
            var layerObject = new GameObject(name);
            layerObject.transform.SetParent(parent, false);

            layerObject.AddComponent<Tilemap>();
            var renderer = layerObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return layerObject.GetComponent<Tilemap>();
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

            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityEngine.U2D.PixelPerfectCamera>();

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
            positionComposer.Damping = new Vector3(0.25f, 0.25f, 0.1f);
            positionComposer.Lookahead = new LookaheadSettings
            {
                Enabled = true,
                Time = 0.22f,
                Smoothing = 6f,
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

        private static void CreateDebugOverlay(PlayerMotor2D motor, PrototypeWorldGenerator generator)
        {
            var overlayObject = new GameObject("Debug Overlay");
            var overlay = overlayObject.AddComponent<PrototypeDebugOverlay>();
            SetObjectReference(overlay, "playerMotor", motor);
            SetObjectReference(overlay, "worldGenerator", generator);
        }

        private static Sprite EnsurePlayerSpriteAsset()
        {
            var folderPath = Path.GetDirectoryName(PlayerSpritePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (!File.Exists(PlayerSpritePath))
            {
                var texture = CreatePrototypeTexture();
                File.WriteAllBytes(PlayerSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(PlayerSpritePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(PlayerSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        }

        private static Sprite[] EnsureSandSprites()
        {
            var definitions = new[]
            {
                new GroundSpriteDefinition("SandRustA.png", new Color32(173, 136, 86, 255), new Color32(197, 160, 106, 255), new Color32(112, 79, 39, 255), GroundPattern.CrossHatch),
                new GroundSpriteDefinition("SandRustB.png", new Color32(179, 141, 88, 255), new Color32(202, 166, 111, 255), new Color32(116, 83, 43, 255), GroundPattern.Diamond),
                new GroundSpriteDefinition("SandRustC.png", new Color32(169, 132, 82, 255), new Color32(194, 155, 100, 255), new Color32(106, 75, 36, 255), GroundPattern.Ring),
                new GroundSpriteDefinition("SandRustD.png", new Color32(181, 145, 92, 255), new Color32(206, 170, 114, 255), new Color32(119, 85, 45, 255), GroundPattern.Dots)
            };

            var sprites = new List<Sprite>(definitions.Length);
            foreach (var definition in definitions)
            {
                var spritePath = Path.Combine(GroundSandFolder, definition.FileName);
                EnsureSpriteAsset(spritePath, definition.BaseColor, definition.HighlightColor, definition.BorderColor, definition.Pattern);
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath));
            }

            return sprites.ToArray();
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
            return CreateTileTexture(
                new Color32(214, 176, 90, 255),
                new Color32(238, 210, 132, 255),
                new Color32(74, 54, 30, 255),
                GroundPattern.Square);
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
            Object.DestroyImmediate(texture);

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
            Object.DestroyImmediate(texture);

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

            var rng = new System.Random(GetSeed(fill, highlight, border, pattern));
            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    var color = fill;
                    var localNoise = NextSignedNoise(rng);
                    var broadNoise = NextSignedNoise(rng);
                    var centerWeight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 16f);
                    var tintAmount = 0.5f + centerWeight * 0.35f + localNoise * 0.15f;

                    switch (pattern)
                    {
                        case GroundPattern.Square:
                            color = Blend(fill, highlight, tintAmount * 0.45f + broadNoise * 0.1f);
                            break;
                        case GroundPattern.CrossHatch:
                            color = ((x + y) & 3) < 2 ? Blend(fill, highlight, 0.28f + localNoise * 0.1f) : Blend(fill, border, 0.15f + broadNoise * 0.08f);
                            break;
                        case GroundPattern.Diamond:
                            color = Blend(fill, highlight, 0.35f + Mathf.Abs(localNoise) * 0.18f);
                            break;
                        case GroundPattern.Ring:
                            color = Blend(fill, border, 0.2f + broadNoise * 0.12f);
                            break;
                        case GroundPattern.Dots:
                            color = Blend(fill, highlight, 0.22f + localNoise * 0.08f);
                            break;
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
                    DrawPebblePatch(texture, tint, rng, 6, 6, 25, 25, 4);
                    break;
                case GroundOverlayPattern.PebblesPatch:
                    DrawPebblePatch(texture, tint, rng, 5, 5, 26, 26, 3);
                    break;
                case GroundOverlayPattern.PebblesMixed:
                    DrawPebblePatch(texture, tint, rng, 4, 6, 25, 24, 4);
                    DrawPebbleCluster(texture, tint, 17, 18, 4, rng);
                    break;
                case GroundOverlayPattern.PebblesSparse:
                    DrawPebbleCluster(texture, tint, 11, 11, 2, rng);
                    DrawPebbleCluster(texture, tint, 22, 21, 3, rng);
                    break;
                case GroundOverlayPattern.RocksSmall:
                    DrawRockPatch(texture, tint, rng, 7, 7, 24, 24, 5);
                    break;
                case GroundOverlayPattern.RocksPatch:
                    DrawRockPatch(texture, tint, rng, 5, 5, 26, 26, 4);
                    break;
                case GroundOverlayPattern.RocksMixed:
                    DrawRockPatch(texture, tint, rng, 6, 6, 25, 24, 4);
                    DrawRockCluster(texture, tint, 18, 16, 5, rng);
                    break;
                case GroundOverlayPattern.RocksSparse:
                    DrawRockCluster(texture, tint, 12, 10, 2, rng);
                    DrawRockCluster(texture, tint, 22, 22, 3, rng);
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

        private static void DrawPebblePatch(Texture2D texture, Color32 tint, System.Random rng, int minX, int minY, int maxX, int maxY, int step)
        {
            for (var y = minY; y <= maxY; y += step)
            {
                for (var x = minX; x <= maxX; x += step)
                {
                    DrawPebbleCluster(texture, tint, x, y, Mathf.Max(1, step - 1), rng);
                }
            }
        }

        private static void DrawPebbleCluster(Texture2D texture, Color32 tint, int centerX, int centerY, int radius, System.Random rng)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    var distance = Mathf.Abs(x) + Mathf.Abs(y);
                    if (distance > radius + 1)
                    {
                        continue;
                    }

                    if (NextSignedNoise(rng) < -0.35f)
                    {
                        continue;
                    }

                    Put(texture, centerX + x, centerY + y, tint);
                }
            }
        }

        private static void DrawRockPatch(Texture2D texture, Color32 tint, System.Random rng, int minX, int minY, int maxX, int maxY, int step)
        {
            for (var y = minY; y <= maxY; y += step)
            {
                for (var x = minX; x <= maxX; x += step)
                {
                    DrawRockCluster(texture, tint, x, y, Mathf.Max(2, step - 1), rng);
                }
            }
        }

        private static void DrawRockCluster(Texture2D texture, Color32 tint, int centerX, int centerY, int radius, System.Random rng)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    var chebyshev = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    if (chebyshev > radius)
                    {
                        continue;
                    }

                    if (chebyshev == radius && ((x + y) & 1) == 1 && NextSignedNoise(rng) < 0.15f)
                    {
                        continue;
                    }

                    Put(texture, centerX + x, centerY + y, tint);
                }
            }
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
            if (x < 0 || x >= 32 || y < 0 || y >= 32)
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

        private static void SetObjectReference<T>(Object target, string fieldName, T value)
            where T : Object
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

        private static void SetObjectArray<T>(Object target, string fieldName, T[] values)
            where T : Object
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

        private static void SetInt(Object target, string fieldName, int value)
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
    }
}
