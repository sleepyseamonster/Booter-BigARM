using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D.Editor
{
    public static class TopDown3DIronstoneAssetBuilder
    {
        public const string GatherAnimationPath =
            "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/Animations/Booter_GatherIronstone.anim";
        public const string ItemCatalogPath =
            "Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset";
        public const string IronstoneItemPath =
            "Assets/_Project/Settings/Items/Item_IronstoneOre.asset";
        public const string ResourceCatalogPath =
            "Assets/_Project/Settings/World/TopDown3DResourceCatalog.asset";
        public const string IronstoneResourcePath =
            "Assets/_Project/Settings/World/Resource_IronstoneNode.asset";
        public const string ActiveMaterialPath =
            "Assets/_Project/Materials/TopDown3D/Resources/IronstoneNodeActive.mat";
        public const string DepletedMaterialPath =
            "Assets/_Project/Materials/TopDown3D/Resources/IronstoneNodeDepleted.mat";
        public const string OreIconPath = "Assets/_Project/UI/Items/IronstoneOre.png";
        private const string WorldSettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        private const string EvidenceFolder = "Docs/Evidence/Ironstone";

        [MenuItem("Booter & BigARM/Top Down 3D/Capture Ironstone Evidence")]
        public static void CaptureEvidence()
        {
            Directory.CreateDirectory(EvidenceFolder);
            CaptureInventoryEvidence();
            CaptureNodeStateEvidence();
            CaptureFeedbackEvidence("[E] Gather Ironstone", "interaction-prompt.png");
            CaptureFeedbackEvidence("+1 Ironstone Ore", "gather-receipt.png");
            Debug.Log($"Captured fixed-view Ironstone evidence under '{EvidenceFolder}'.");
        }

        [MenuItem("Booter & BigARM/Top Down 3D/Build Ironstone Assets")]
        public static void BuildIronstoneAssets()
        {
            EnsureFolder("Assets/_Project/Settings/Items");
            EnsureFolder("Assets/_Project/Materials/TopDown3D/Resources");
            EnsureFolder("Assets/_Project/UI/Items");
            ConfigureIconImporter();
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(OreIconPath);
            if (icon == null)
            {
                throw new InvalidOperationException($"Ironstone Ore icon did not import as a Sprite at '{OreIconPath}'.");
            }

            var item = LoadOrCreate<TopDown3DItemDefinition>(IronstoneItemPath);
            item.Configure(
                "resource.ironstone_ore",
                "Ironstone Ore",
                "Dense, rust-veined ore gathered from exposed ironstone deposits.",
                "Resource",
                icon,
                99,
                1.25f,
                TopDown3DCarryPreference.BigArmPreferred,
                TopDown3DPackingPreference.Normal);
            EditorUtility.SetDirty(item);
            var itemCatalog = LoadOrCreate<TopDown3DItemCatalog>(ItemCatalogPath);
            itemCatalog.Configure(new[] { item });
            EditorUtility.SetDirty(itemCatalog);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP/Lit is required for Ironstone node materials.");
            }

            var activeMaterial = LoadOrCreateMaterial(ActiveMaterialPath, shader);
            ConfigureMaterial(activeMaterial, new Color32(108, 61, 32, 255), 0.46f, 0.48f);
            var depletedMaterial = LoadOrCreateMaterial(DepletedMaterialPath, shader);
            ConfigureMaterial(depletedMaterial, new Color32(46, 48, 50, 255), 0.02f, 0.18f);

            var resource = LoadOrCreate<TopDown3DResourceDefinition>(IronstoneResourcePath);
            resource.Configure(
                "resource.ironstone_node",
                "broken-world-outcrop-00",
                TopDown3DNaturalObjectShape.Outcrop,
                0,
                activeMaterial,
                depletedMaterial,
                item,
                1,
                1,
                2.2f,
                1.1f,
                "Gather Ironstone",
                0.35f,
                24f,
                12f);
            resource.ConfigurePlacementConstraints(45f, 0f, 0f, 0.15f, 0.65f);
            EditorUtility.SetDirty(resource);
            var resourceCatalog = LoadOrCreate<TopDown3DResourceCatalog>(ResourceCatalogPath);
            resourceCatalog.Configure(new[] { resource });
            EditorUtility.SetDirty(resourceCatalog);

            var worldSettings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            if (worldSettings == null)
            {
                throw new InvalidOperationException($"Required world settings missing at '{WorldSettingsPath}'.");
            }

            worldSettings.ConfigureResourceAssets(resourceCatalog, 1);
            EditorUtility.SetDirty(worldSettings);
            BuildGatherAnimation();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built canonical Ironstone item, resource, materials, icon importer, and gather animation assets.");
        }

        [MenuItem("Booter & BigARM/Top Down 3D/Build Ironstone Gather Animation")]
        public static void BuildGatherAnimation()
        {
            var folder = Path.GetDirectoryName(GatherAnimationPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                throw new InvalidOperationException($"Required animation folder '{folder}' does not exist.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GatherAnimationPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Booter Gather Ironstone" };
                AssetDatabase.CreateAsset(clip, GatherAnimationPath);
            }

            clip.frameRate = 30f;
            ClearCurves(clip);
            SetMuscleCurve(clip, "Spine Front-Back", 0f, 0.26f, 0.38f, 0.24f, 0f);
            SetMuscleCurve(clip, "Chest Front-Back", 0f, 0.14f, 0.22f, 0.12f, 0f);
            SetMuscleCurve(clip, "Left Arm Down-Up", -0.08f, 0.52f, 0.18f, 0.66f, -0.08f);
            SetMuscleCurve(clip, "Right Arm Down-Up", -0.08f, 0.52f, 0.18f, 0.66f, -0.08f);
            SetMuscleCurve(clip, "Left Arm Front-Back", 0.05f, -0.32f, -0.5f, -0.38f, 0.05f);
            SetMuscleCurve(clip, "Right Arm Front-Back", 0.05f, -0.32f, -0.5f, -0.38f, 0.05f);
            SetMuscleCurve(clip, "Left Forearm Stretch", 0.05f, 0.72f, 0.42f, 0.76f, 0.05f);
            SetMuscleCurve(clip, "Right Forearm Stretch", 0.05f, 0.72f, 0.42f, 0.76f, 0.05f);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(GatherAnimationPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Authored in-place Humanoid gather animation at '{GatherAnimationPath}'.");
        }

        private static void SetMuscleCurve(
            AnimationClip clip,
            string muscleName,
            float start,
            float lift,
            float contact,
            float secondLift,
            float end)
        {
            if (Array.IndexOf(HumanTrait.MuscleName, muscleName) < 0)
            {
                throw new InvalidOperationException($"Humanoid muscle '{muscleName}' is unavailable.");
            }

            var curve = new AnimationCurve(
                new Keyframe(0f, start),
                new Keyframe(0.26f, lift),
                new Keyframe(0.53f, contact),
                new Keyframe(0.78f, secondLift),
                new Keyframe(1.1f, end));
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), muscleName),
                curve);
        }

        private static void ClearCurves(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            for (var i = 0; i < bindings.Length; i++)
            {
                AnimationUtility.SetEditorCurve(clip, bindings[i], null);
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.shader = shader;
                return material;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureMaterial(Material material, Color color, float metallic, float smoothness)
        {
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureIconImporter()
        {
            AssetDatabase.ImportAsset(OreIconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(OreIconPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Ironstone Ore icon is missing at '{OreIconPath}'.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            var normalized = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            var name = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException($"Cannot create asset folder '{path}'.");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void CaptureInventoryEvidence()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Evidence Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(30, 27, 24, 255);
            var canvasObject = new GameObject("Inventory Evidence", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var inventoryCanvas = canvasObject.AddComponent<TopDown3DInventoryCanvas>();
            inventoryCanvas.Initialize(16, _ => { }, _ => { });
            inventoryCanvas.SetVisible(true);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            var item = AssetDatabase.LoadAssetAtPath<TopDown3DItemDefinition>(IronstoneItemPath);
            var occupied = new TopDown3DInventorySlot();
            occupied.Set(item.ItemId, 37);
            for (var i = 0; i < inventoryCanvas.SlotViews.Count; i++)
            {
                inventoryCanvas.SlotViews[i].Refresh(
                    i == 0 ? occupied : new TopDown3DInventorySlot(),
                    i == 0 ? item : null,
                    i == 0);
            }
            inventoryCanvas.SetDetails(
                item.DisplayName,
                item.Description,
                "1 / 16 SLOTS");
            Canvas.ForceUpdateCanvases();
            RenderCamera(camera, $"{EvidenceFolder}/inventory-ui.png");
        }

        private static void CaptureNodeStateEvidence()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Evidence Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(32, 28, 24, 255);
            camera.fieldOfView = 32f;
            camera.transform.position = new Vector3(0f, 3.4f, -9f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.8f, 0f) - camera.transform.position);
            var lightObject = new GameObject("Evidence Light", typeof(Light));
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color32(255, 224, 188, 255);
            lightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color32(82, 88, 94, 255);

            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            var resource = AssetDatabase.LoadAssetAtPath<TopDown3DResourceDefinition>(IronstoneResourcePath);
            var family = settings.NaturalObjectCatalog.GetRequiredMeshFamily(
                resource.MeshShape,
                resource.MeshVariant);
            CreateEvidenceRock("ACTIVE IRONSTONE", new Vector3(-1.75f, 0f, 0f), family.GetLod(0), resource.ActiveMaterial);
            CreateEvidenceRock("DEPLETED IRONSTONE", new Vector3(1.75f, 0f, 0f), family.GetLod(0), resource.DepletedMaterial);
            RenderCamera(camera, $"{EvidenceFolder}/node-states.png");
        }

        private static void CaptureFeedbackEvidence(string message, string fileName)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Evidence Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(30, 27, 24, 255);

            var canvasObject = new GameObject(
                "Interaction Evidence",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            var feedbackObject = new GameObject("Interaction Feedback", typeof(RectTransform));
            feedbackObject.transform.SetParent(canvasObject.transform, false);
            var feedback = feedbackObject.AddComponent<TopDown3DInteractionFeedbackHud>();
            feedback.ShowFeedback(message);
            Canvas.ForceUpdateCanvases();
            RenderCamera(camera, $"{EvidenceFolder}/{fileName}");
        }

        private static void CreateEvidenceRock(
            string label,
            Vector3 position,
            Mesh mesh,
            Material material)
        {
            var rock = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            rock.transform.position = position;
            rock.transform.rotation = Quaternion.Euler(0f, position.x < 0f ? 24f : -24f, 0f);
            rock.GetComponent<MeshFilter>().sharedMesh = mesh;
            rock.GetComponent<MeshRenderer>().sharedMaterial = material;
            var textObject = new GameObject($"{label} Label", typeof(TextMesh));
            textObject.transform.position = position + new Vector3(0f, -0.85f, -0.25f);
            textObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            var text = textObject.GetComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 32;
            text.characterSize = 0.055f;
            text.color = new Color32(238, 222, 191, 255);
        }

        private static void RenderCamera(Camera camera, string outputPath)
        {
            var renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
