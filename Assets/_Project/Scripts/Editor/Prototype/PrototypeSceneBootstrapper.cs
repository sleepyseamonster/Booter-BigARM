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
        private const string GroundBaseFolder = "Assets/_Project/Art/Prototype/Ground/Base";
        private const string GroundOverlayFolder = "Assets/_Project/Art/Prototype/Ground/Overlay";
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
            var groundSprites = EnsureGroundBaseSprites();
            var groundOverlaySprites = EnsureGroundOverlaySprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PrototypeScene";

            var player = CreatePlayer(inputActions, playerSprite);
            var world = CreateWorld(player.transform, groundSprites, groundOverlaySprites);
            CreateCamera(player.transform);
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

        private static PrototypeWorldGenerator CreateWorld(Transform player, Sprite[] groundSprites, Sprite[] overlaySprites)
        {
            var worldRoot = new GameObject("World");

            var grid = worldRoot.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var groundTilemapObject = new GameObject("Ground Tilemap");
            groundTilemapObject.transform.SetParent(worldRoot.transform, false);

            groundTilemapObject.AddComponent<Tilemap>();
            var tilemapRenderer = groundTilemapObject.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortingOrder = 0;

            var detailTilemapObject = new GameObject("Ground Detail Tilemap");
            detailTilemapObject.transform.SetParent(worldRoot.transform, false);

            var detailTilemap = detailTilemapObject.AddComponent<Tilemap>();
            var detailTilemapRenderer = detailTilemapObject.AddComponent<TilemapRenderer>();
            detailTilemapRenderer.sortingOrder = 1;

            var generator = groundTilemapObject.AddComponent<PrototypeWorldGenerator>();
            SetObjectReference(generator, "target", player);
            SetObjectArray(generator, "tileSprites", groundSprites);
            SetObjectReference(generator, "overlayTilemap", detailTilemap);
            SetObjectArray(generator, "overlayTileSprites", overlaySprites);
            SetInt(generator, "seed", 4829);
            SetInt(generator, "chunkSize", 16);
            SetInt(generator, "chunkRadius", 4);

            return generator;
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
            cinemachineCameraObject.AddComponent<CinemachineFollow>();
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

        private static Sprite[] EnsureGroundBaseSprites()
        {
            var definitions = new[]
            {
                new GroundSpriteDefinition("GroundSand.png", new Color32(199, 171, 101, 255), new Color32(234, 210, 151, 255), new Color32(122, 95, 44, 255), GroundPattern.CrossHatch),
                new GroundSpriteDefinition("GroundGrass.png", new Color32(88, 148, 74, 255), new Color32(146, 198, 103, 255), new Color32(58, 94, 48, 255), GroundPattern.Diamond),
                new GroundSpriteDefinition("GroundStone.png", new Color32(121, 124, 131, 255), new Color32(177, 181, 188, 255), new Color32(67, 69, 74, 255), GroundPattern.Ring),
                new GroundSpriteDefinition("GroundMire.png", new Color32(76, 118, 93, 255), new Color32(125, 160, 111, 255), new Color32(38, 60, 45, 255), GroundPattern.Dots)
            };

            var sprites = new List<Sprite>(definitions.Length);
            foreach (var definition in definitions)
            {
                var spritePath = Path.Combine(PrototypeArtFolder, definition.FileName);
                EnsureSpriteAsset(spritePath, definition.BaseColor, definition.HighlightColor, definition.BorderColor, definition.Pattern);
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath));
            }

            return sprites.ToArray();
        }

        private static Sprite[] EnsureGroundOverlaySprites()
        {
            var definitions = new[]
            {
                new GroundOverlayDefinition("GroundOverlaySpeckles.png", new Color32(25, 24, 17, 150), GroundOverlayPattern.Speckles),
                new GroundOverlayDefinition("GroundOverlayCracks.png", new Color32(58, 43, 20, 140), GroundOverlayPattern.Cracks),
                new GroundOverlayDefinition("GroundOverlayTufts.png", new Color32(20, 42, 21, 135), GroundOverlayPattern.Tufts),
                new GroundOverlayDefinition("GroundOverlayFlecks.png", new Color32(54, 45, 36, 120), GroundOverlayPattern.Flecks)
            };

            var sprites = new List<Sprite>(definitions.Length);
            foreach (var definition in definitions)
            {
                var spritePath = Path.Combine(GroundOverlayFolder, definition.FileName);
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

            if (!File.Exists(assetPath))
            {
                var texture = CreateTileTexture(fill, highlight, border, pattern);
                File.WriteAllBytes(assetPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

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

            if (!File.Exists(assetPath))
            {
                var texture = CreateOverlayTexture(tint, pattern);
                File.WriteAllBytes(assetPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

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

            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    var color = border;
                    var localX = x - 16;
                    var localY = y - 16;
                    var distance = Mathf.Sqrt(localX * localX + localY * localY);
                    var ringBand = distance > 7f && distance < 10.5f;

                    switch (pattern)
                    {
                        case GroundPattern.Square:
                            if (x > 3 && x < 28 && y > 3 && y < 28)
                            {
                                color = fill;
                            }

                            if (x > 8 && x < 23 && y > 8 && y < 23)
                            {
                                color = highlight;
                            }
                            break;
                        case GroundPattern.CrossHatch:
                            if (x > 2 && x < 29 && y > 2 && y < 29)
                            {
                                color = ((x + y) & 3) < 2 ? fill : highlight;
                            }
                            break;
                        case GroundPattern.Diamond:
                            if (Mathf.Abs(localX) + Mathf.Abs(localY) < 16)
                            {
                                color = fill;
                            }

                            if (Mathf.Abs(localX) + Mathf.Abs(localY) < 8)
                            {
                                color = highlight;
                            }
                            break;
                        case GroundPattern.Ring:
                            if (distance < 12f)
                            {
                                color = fill;
                            }

                            if (ringBand)
                            {
                                color = highlight;
                            }
                            break;
                        case GroundPattern.Dots:
                            if (x > 3 && x < 28 && y > 3 && y < 28)
                            {
                                color = fill;
                            }

                            if (((x / 4) + (y / 4)) % 2 == 0 && x > 6 && x < 26 && y > 6 && y < 26)
                            {
                                color = highlight;
                            }
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

            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    texture.SetPixel(x, y, new Color32(0, 0, 0, 0));
                }
            }

            switch (pattern)
            {
                case GroundOverlayPattern.Speckles:
                    for (var y = 4; y < 28; y += 3)
                    {
                        for (var x = 4; x < 28; x += 3)
                        {
                            if (((x + y) & 1) == 0)
                            {
                                texture.SetPixel(x, y, tint);
                            }
                        }
                    }
                    break;
                case GroundOverlayPattern.Cracks:
                    for (var i = 4; i < 28; i++)
                    {
                        texture.SetPixel(i, 10 + (i / 4), tint);
                        texture.SetPixel(31 - i, 21 - (i / 4), tint);
                    }
                    break;
                case GroundOverlayPattern.Tufts:
                    for (var i = 0; i < 6; i++)
                    {
                        var baseX = 5 + i * 4;
                        texture.SetPixel(baseX, 20, tint);
                        texture.SetPixel(baseX + 1, 19, tint);
                        texture.SetPixel(baseX + 1, 18, tint);
                        texture.SetPixel(baseX + 2, 19, tint);
                    }
                    break;
                case GroundOverlayPattern.Flecks:
                    for (var y = 6; y < 26; y += 4)
                    {
                        for (var x = 6; x < 26; x += 4)
                        {
                            texture.SetPixel(x, y, tint);
                            if ((x + y) % 8 == 0)
                            {
                                texture.SetPixel(x + 1, y, tint);
                            }
                        }
                    }
                    break;
            }

            texture.Apply(false, false);
            return texture;
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
            Speckles,
            Cracks,
            Tufts,
            Flecks
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
