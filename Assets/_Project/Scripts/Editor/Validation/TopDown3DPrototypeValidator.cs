using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.Editor
{
    public static class TopDown3DPrototypeValidator
    {
        [MenuItem("Booter & BigARM/Top Down 3D/Validate Perspective Prototype")]
        public static void ValidateFromMenu()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
            {
                Debug.LogError(FormatErrors(errors));
                return;
            }

            Debug.Log("Perspective top-down 3D prototype validation passed.");
        }

        public static void ValidateFromCli()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
            {
                throw new BuildFailedException(FormatErrors(errors));
            }
        }

        public static List<string> CollectErrors()
        {
            var errors = ConversionBaselineValidator.CollectErrors();
            errors.AddRange(CollectLandscapeErrors());
            ValidateAssetExists(TopDown3DPrototypeBuilder.PrototypeHumanoidModelPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.LocomotionClipProfilePath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.PrototypeHumanoidSideStepLeftPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.PrototypeHumanoidSideStepRightPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.PrototypeHumanoidVaultPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.PrototypeHumanoidGatherPath, errors);
            ValidateLocomotionProfile(errors);
            ValidateAssetExists(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.ItemCatalogPath,
                errors);
            ValidateAssetExists(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.IronstoneItemPath,
                errors);
            ValidateAssetExists(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.ResourceCatalogPath,
                errors);
            ValidateAssetExists(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.IronstoneResourcePath,
                errors);
            ValidateAssetExists(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.ActiveMaterialPath,
                errors);
            ValidateAssetExists(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.DepletedMaterialPath,
                errors);
            ValidateAssetExists(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.OreIconPath,
                errors);
            ValidateIronstoneAssets(errors);
            ValidateCameraInput(errors);
            ValidateVolumetricDustShader(errors);
            ValidateVolumetricDustRenderer(errors);
            ValidateBuildSettings(errors);
            ValidateScene(errors);
            return errors;
        }

        internal static List<string> CollectLandscapeErrors()
        {
            var errors = new List<string>();
            ValidateAssetExists(TopDown3DPrototypeBuilder.ScenePath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.WorldSettingsPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.PackingSettingsPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainShaderPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.VolumetricDustShaderPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainSweptSandAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainSweptSandTransitionAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainGravelAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainGravelTransitionAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainRockyAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainRockyHeightPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainRockyNormalPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainRockyMidTransitionAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainRockyMidTransitionHeightPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainRockyMidTransitionNormalPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainRockyTransitionAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainRockyTransitionHeightPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainRockyTransitionNormalPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TerrainMaterialPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.RockShaderPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.RockAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.DarkRockAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TealRockAlbedoPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TealRockLusterMaskPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.RockMaterialPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.DarkRockMaterialPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.TealRockMaterialPath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.FineGrayClutterMaterialPath, errors);
            ValidateTerrainMaterial(errors);
            ValidateRockMaterial(errors);
            ValidateWorldCoverage(errors);
            ValidateNaturalObjectCatalog(errors);
            return errors;
        }

        private static void ValidateWorldCoverage(ICollection<string> errors)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            if (settings == null)
            {
                return;
            }

            if (settings.StreamingRadius < 7)
            {
                errors.Add(
                    "TopDown3D world streaming must retain a seven-chunk radius so the 25-unit landscape camera cannot expose empty space at supported orbit angles.");
            }

            if (settings.ImmediateLoadRadius < 2)
            {
                errors.Add(
                    "TopDown3D initial loading must build a two-chunk radius before budgeted outer-ring streaming begins.");
            }

            if (settings.DecorationStreamingRadius > 3)
            {
                errors.Add(
                    "TopDown3D presentation decoration must stay within a three-chunk radius; terrain and collision coverage remain authoritative at the seven-chunk streaming radius.");
            }

            if (settings.TerrainGenerationVersion < 2)
            {
                errors.Add("TopDown3D terrain generation must use the versioned geological world generator.");
            }

            var geology = settings.GeologyProfile;
            if (geology == null
                || geology.RegionSize < 72f
                || geology.BasinRelief <= 0f
                || geology.RidgeRelief <= 0f
                || geology.MesaRelief <= 0f
                || geology.DrainageDepth <= 0f)
            {
                errors.Add(
                    "TopDown3D world settings require a valid shared geology profile with regional relief and drainage.");
            }
        }

        private static void ValidateNaturalObjectCatalog(ICollection<string> errors)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            var catalog = settings != null ? settings.NaturalObjectCatalog : null;
            if (catalog == null)
            {
                errors.Add("TopDown3D world settings require a natural-object catalog.");
                return;
            }

            var grayMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.FineGrayClutterMaterialPath);
            if (settings.FineGrayClutterMaterial == null
                || settings.FineGrayClutterMaterial != grayMaterial)
            {
                errors.Add("TopDown3D world settings must reference the shared fine-gray clutter material.");
            }

            var darkMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.DarkRockMaterialPath);
            var tealMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.TealRockMaterialPath);
            if (settings.DarkRockMaterial == null || settings.DarkRockMaterial != darkMaterial)
            {
                errors.Add("TopDown3D world settings must reference the shared dark-rock material.");
            }

            if (settings.TealRockMaterial == null || settings.TealRockMaterial != tealMaterial)
            {
                errors.Add("TopDown3D world settings must reference the shared teal-rock material.");
            }

            if (settings.PhysicalRockGenerationVersion < 1)
            {
                errors.Add("Physical rock generation requires its own positive generation version.");
            }

            if (!(settings.SmallRocksPerChunk > settings.MediumRocksPerChunk
                && settings.MediumRocksPerChunk > settings.PropsPerChunk
                && settings.PropsPerChunk > settings.ExtraLargeRocksPerChunk
                && settings.ExtraLargeRocksPerChunk > settings.MassiveRocksPerChunk
                && settings.MassiveRocksPerChunk > settings.LandmarksPerChunk))
            {
                errors.Add("Physical-rock root density must descend from Small through Towering.");
            }

            if (settings.PhysicalFormationMaximumMembers < 1
                || settings.PhysicalFormationMaximumDepth < 0
                || settings.FormationMaximumChildrenPerParent < 1
                || settings.FormationMinimumParentDistanceRatio < 0.15f
                || settings.FormationMaximumParentDistanceRatio
                    < settings.FormationMinimumParentDistanceRatio
                || settings.FormationMaximumParentDistanceRatio > 1f)
            {
                errors.Add("Physical rock formation caps or contact tuning are outside supported bounds.");
            }

            foreach (TopDown3DNaturalObjectLayer layer in Enum.GetValues(typeof(TopDown3DNaturalObjectLayer)))
            {
                if (!catalog.HasLayer(layer))
                {
                    errors.Add($"Natural-object catalog is missing the {layer} layer.");
                }
            }

            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var physicalTiers = new HashSet<TopDown3DRockSizeTier>();
            for (var i = 0; i < catalog.Definitions.Count; i++)
            {
                var definition = catalog.Definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.StableId))
                {
                    errors.Add($"Natural-object catalog definition {i} has no stable ID.");
                }
                else if (!stableIds.Add(definition.StableId))
                {
                    errors.Add($"Natural-object catalog repeats stable ID '{definition.StableId}'.");
                }

                if (definition != null && definition.RockSizeTier != TopDown3DRockSizeTier.None)
                {
                    physicalTiers.Add(definition.RockSizeTier);
                }
            }

            foreach (var tier in new[]
                     {
                         TopDown3DRockSizeTier.Small,
                         TopDown3DRockSizeTier.Medium,
                         TopDown3DRockSizeTier.Large,
                         TopDown3DRockSizeTier.ExtraLarge,
                         TopDown3DRockSizeTier.Massive,
                         TopDown3DRockSizeTier.Towering
                     })
            {
                if (!physicalTiers.Contains(tier))
                {
                    errors.Add($"Natural-object catalog is missing the {tier} physical rock tier.");
                }
            }
        }

        private static void ValidateTerrainMaterial(ICollection<string> errors)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TopDown3DPrototypeBuilder.TerrainMaterialPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(TopDown3DPrototypeBuilder.TerrainShaderPath);
            var baseAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TopDown3DPrototypeBuilder.TerrainAlbedoPath);
            var sweptSand = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.TerrainSweptSandAlbedoPath);
            var sweptSandTransition = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.TerrainSweptSandTransitionAlbedoPath);
            var gravel = AssetDatabase.LoadAssetAtPath<Texture2D>(TopDown3DPrototypeBuilder.TerrainGravelAlbedoPath);
            var gravelTransition = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.TerrainGravelTransitionAlbedoPath);
            var rocky = AssetDatabase.LoadAssetAtPath<Texture2D>(TopDown3DPrototypeBuilder.TerrainRockyAlbedoPath);
            var rockyHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(TopDown3DPrototypeBuilder.TerrainRockyHeightPath);
            var rockyNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TopDown3DPrototypeBuilder.TerrainRockyNormalPath);
            var rockyMidTransition = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.TerrainRockyMidTransitionAlbedoPath);
            var rockyMidTransitionHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.TerrainRockyMidTransitionHeightPath);
            var rockyMidTransitionNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.TerrainRockyMidTransitionNormalPath);
            var rockyTransition = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.TerrainRockyTransitionAlbedoPath);
            var rockyTransitionHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.TerrainRockyTransitionHeightPath);
            var rockyTransitionNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.TerrainRockyTransitionNormalPath);
            if (material == null || shader == null || baseAlbedo == null || sweptSand == null || gravel == null
                || rocky == null || sweptSandTransition == null || gravelTransition == null || rockyTransition == null
                || rockyMidTransition == null || rockyMidTransitionHeight == null
                || rockyMidTransitionNormal == null
                || rockyHeight == null || rockyNormal == null || rockyTransitionHeight == null
                || rockyTransitionNormal == null)
            {
                return;
            }

            if (material.shader != shader)
            {
                errors.Add("TopDown3D terrain material must use the Broken World layered terrain shader.");
            }

            if (material.GetTexture("_BaseMap") != baseAlbedo)
            {
                errors.Add("TopDown3D terrain material must use the Broken World sand-dirt albedo texture.");
            }

            if (material.GetTexture("_SweptSandMap") != sweptSand)
            {
                errors.Add("TopDown3D terrain material must use the sparse swept-sand albedo texture.");
            }

            if (material.GetTexture("_SweptSandTransitionMap") != sweptSandTransition)
            {
                errors.Add("TopDown3D terrain material must use the swept-sand transition albedo texture.");
            }

            if (material.GetTexture("_GravelMap") != gravel)
            {
                errors.Add("TopDown3D terrain material must use the sparse gravel albedo texture.");
            }

            if (material.GetTexture("_GravelTransitionMap") != gravelTransition)
            {
                errors.Add("TopDown3D terrain material must use the gravel transition albedo texture.");
            }

            if (material.GetTexture("_RockyMap") != rocky)
            {
                errors.Add("TopDown3D terrain material must use the sparse mixed rocky albedo texture.");
            }

            if (material.GetTexture("_RockyTransitionMap") != rockyTransition)
            {
                errors.Add("TopDown3D terrain material must use the mixed-rock transition albedo texture.");
            }

            if (material.GetTexture("_RockyMidTransitionMap") != rockyMidTransition
                || material.GetTexture("_RockyMidTransitionHeightMap") != rockyMidTransitionHeight
                || material.GetTexture("_RockyMidTransitionNormalMap") != rockyMidTransitionNormal)
            {
                errors.Add("TopDown3D terrain material must use the medium-shale transition texture set.");
            }

            if (material.GetTexture("_RockyHeightMap") != rockyHeight
                || material.GetTexture("_RockyNormalMap") != rockyNormal)
            {
                errors.Add("TopDown3D terrain material must use the mixed-rock height and normal textures.");
            }

            if (material.GetTexture("_RockyTransitionHeightMap") != rockyTransitionHeight
                || material.GetTexture("_RockyTransitionNormalMap") != rockyTransitionNormal)
            {
                errors.Add("TopDown3D terrain material must use the mixed-rock transition height and normal textures.");
            }

            var detailFadeStart = material.GetFloat("_DetailFadeStart");
            var detailFadeEnd = material.GetFloat("_DetailFadeEnd");
            if (detailFadeStart < 25f || detailFadeEnd <= detailFadeStart)
            {
                errors.Add(
                    "TopDown3D terrain detail must fade in a valid distance band beyond the gameplay camera to control shimmer and texture noise.");
            }
        }

        private static void ValidateRockMaterial(ICollection<string> errors)
        {
            ValidateRockMaterial(
                TopDown3DPrototypeBuilder.RockMaterialPath,
                TopDown3DPrototypeBuilder.RockAlbedoPath,
                "regular",
                null,
                errors);
            ValidateRockMaterial(
                TopDown3DPrototypeBuilder.DarkRockMaterialPath,
                TopDown3DPrototypeBuilder.DarkRockAlbedoPath,
                "dark",
                null,
                errors);
            ValidateRockMaterial(
                TopDown3DPrototypeBuilder.TealRockMaterialPath,
                TopDown3DPrototypeBuilder.TealRockAlbedoPath,
                "teal",
                TopDown3DPrototypeBuilder.TealRockLusterMaskPath,
                errors);
        }

        private static void ValidateRockMaterial(
            string materialPath,
            string albedoPath,
            string surfaceName,
            string lusterMaskPath,
            ICollection<string> errors)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(TopDown3DPrototypeBuilder.RockShaderPath);
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            var lusterMask = string.IsNullOrEmpty(lusterMaskPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Texture2D>(lusterMaskPath);
            if (material == null || shader == null || albedo == null
                || (!string.IsNullOrEmpty(lusterMaskPath) && lusterMask == null))
            {
                return;
            }

            if (material.shader != shader)
            {
                errors.Add($"TopDown3D {surfaceName} rocks must use the Broken World triplanar rock shader.");
            }

            if (material.GetTexture("_BaseMap") != albedo)
            {
                errors.Add($"TopDown3D {surfaceName} rocks must use their assigned rock-surface albedo texture.");
            }

            if (lusterMask != null)
            {
                if (material.GetTexture("_LusterMask") != lusterMask
                    || !Mathf.Approximately(
                        material.GetFloat("_LusterStrength"),
                        TopDown3DPrototypeBuilder.TealRockLusterStrength)
                    || !Mathf.Approximately(
                        material.GetFloat("_LusterSmoothness"),
                        TopDown3DPrototypeBuilder.TealRockLusterSmoothness)
                    || !Mathf.Approximately(
                        material.GetFloat("_LusterMetallic"),
                        TopDown3DPrototypeBuilder.TealRockLusterMetallic))
                {
                    errors.Add("TopDown3D teal rocks must use the canonical sparse mineral-luster treatment.");
                }
            }
            else if (material.GetFloat("_LusterStrength") > 0.0001f)
            {
                errors.Add($"TopDown3D {surfaceName} rocks must remain matte without the teal mineral-luster treatment.");
            }

            if (!Mathf.Approximately(material.GetFloat("_RockMetersPerTile"), TopDown3DPrototypeBuilder.RockMetersPerTile)
                || !Mathf.Approximately(material.GetFloat("_Smoothness"), TopDown3DPrototypeBuilder.RockSmoothness))
            {
                errors.Add($"TopDown3D {surfaceName}-rock material tuning does not match the canonical builder values.");
            }
        }

        private static void ValidateCameraInput(ICollection<string> errors)
        {
            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(TopDown3DPrototypeBuilder.InputActionsPath);
            if (input == null)
            {
                errors.Add("The perspective camera requires the shared input asset.");
                return;
            }

            var gameplay = input?.FindActionMap("Gameplay", false);
            var look = gameplay?.FindAction("Look", false);
            if (look == null)
            {
                errors.Add("The perspective camera requires Gameplay/Look in the shared input asset.");
            }
            else if (!look.bindings.Any(binding =>
                    binding.path == "<Gamepad>/rightStick" && binding.groups.Contains("Gamepad")))
            {
                errors.Add("Gameplay/Look must bind the gamepad right stick for perspective camera control.");
            }

            var lookAhead = gameplay?.FindAction("CameraLookAhead", false);
            if (lookAhead == null
                || !lookAhead.bindings.Any(binding =>
                    binding.path == "<Gamepad>/leftTrigger" && binding.groups.Contains("Gamepad")))
            {
                errors.Add("Gameplay/CameraLookAhead must bind the gamepad left trigger as the right-stick translation modifier.");
            }

            var toggle = input.FindAction("System/ToggleInventory", false);
            if (toggle == null
                || toggle.id.ToString() != "3ee79022-8c1b-44bc-a15d-7a8f18e87d38"
                || !toggle.bindings.Any(binding => binding.path == "<Keyboard>/tab")
                || !toggle.bindings.Any(binding => binding.path == "<Gamepad>/buttonNorth"))
            {
                errors.Add("System/ToggleInventory must retain its canonical ID and Tab/Gamepad North bindings.");
            }

            var legacyOpen = input.FindAction("Gameplay/OpenInventory", false);
            if (legacyOpen == null
                || legacyOpen.id.ToString() != "c52a0d4c-3d7f-4e48-98e9-16be3c4f7c99")
            {
                errors.Add("Gameplay/OpenInventory must remain intact for legacy consumers.");
            }
        }

        private static void ValidateLocomotionProfile(ICollection<string> errors)
        {
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(
                TopDown3DPrototypeBuilder.LocomotionClipProfilePath);
            if (profile == null)
            {
                return;
            }

            if (!profile.TryValidate(out var profileError))
            {
                errors.Add($"Booter's locomotion profile is invalid: {profileError}");
                return;
            }

            foreach (TopDown3DLocomotionClipRole role in Enum.GetValues(
                         typeof(TopDown3DLocomotionClipRole)))
            {
                var entry = profile.GetRequiredEntry(role);
                var assetPath = AssetDatabase.GetAssetPath(entry.Clip);
                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (!assetPath.StartsWith("Assets/_Project/", StringComparison.Ordinal)
                    || !assetPath.Contains("_NoRM", StringComparison.Ordinal)
                    && role != TopDown3DLocomotionClipRole.Idle)
                {
                    errors.Add($"Locomotion role '{role}' must reference a project-owned no-root-motion asset.");
                }

                if (importer == null
                    || importer.animationType != ModelImporterAnimationType.Human
                    || !entry.Clip.isHumanMotion)
                {
                    errors.Add($"Locomotion role '{role}' must import as Humanoid motion.");
                    continue;
                }

                var importClip = importer.clipAnimations.FirstOrDefault();
                if (entry.IntentionalMirror != (importClip != null && importClip.mirror))
                {
                    errors.Add($"Locomotion role '{role}' mirror metadata disagrees with its import settings.");
                }
            }
        }

        private static void ValidateIronstoneAssets(ICollection<string> errors)
        {
            var itemCatalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.ItemCatalogPath);
            var item = AssetDatabase.LoadAssetAtPath<TopDown3DItemDefinition>(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.IronstoneItemPath);
            var itemError = "catalog missing";
            var itemCatalogValid = itemCatalog != null && itemCatalog.TryValidate(out itemError);
            if (!itemCatalogValid
                || item == null || itemCatalog.Definitions.Count != 1
                || itemCatalog.Definitions[0] != item
                || item.ItemId != "resource.ironstone_ore" || item.MaxStack != 99)
            {
                errors.Add($"Ironstone item catalog is invalid or non-canonical: {itemError}");
            }

            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            var resourceCatalog = AssetDatabase.LoadAssetAtPath<TopDown3DResourceCatalog>(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.ResourceCatalogPath);
            var resource = AssetDatabase.LoadAssetAtPath<TopDown3DResourceDefinition>(
                global::BooterBigArm.TopDown3D.Editor.TopDown3DIronstoneAssetBuilder.IronstoneResourcePath);
            var resourceError = "catalog or settings missing";
            var resourceCatalogValid = settings != null && resourceCatalog != null
                && resourceCatalog.TryValidate(settings.NaturalObjectCatalog, out resourceError);
            if (settings == null || resourceCatalog == null
                || settings.ResourceCatalog != resourceCatalog
                || settings.ResourceGenerationVersion < 1
                || !resourceCatalogValid
                || resource == null || resourceCatalog.Definitions.Count != 1
                || resourceCatalog.Definitions[0] != resource
                || resource.ResourceId != "resource.ironstone_node"
                || resource.YieldedItem != item || resource.YieldQuantity != 1
                || resource.MaximumUses != 1)
            {
                errors.Add($"Ironstone resource catalog or world reference is invalid: {resourceError}");
            }

            var gather = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                TopDown3DPrototypeBuilder.PrototypeHumanoidGatherPath);
            if (gather == null || !gather.humanMotion || gather.isLooping
                || AnimationUtility.GetCurveBindings(gather).Any(binding =>
                    binding.propertyName.StartsWith("RootT", StringComparison.Ordinal)
                    || binding.propertyName.StartsWith("RootQ", StringComparison.Ordinal)
                    || binding.propertyName.StartsWith("MotionT", StringComparison.Ordinal)
                    || binding.propertyName.StartsWith("MotionQ", StringComparison.Ordinal)))
            {
                errors.Add("Booter's Ironstone gather clip must be Humanoid, non-looping, and contain no root-motion curves.");
            }
        }

        private static void ValidateAssetExists(string path, ICollection<string> errors)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                errors.Add($"Missing perspective prototype asset: {path}");
            }
        }

        private static void ValidateVolumetricDustRenderer(ICollection<string> errors)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                TopDown3DPrototypeBuilder.VolumetricDustShaderPath);
            if (renderer == null || shader == null)
            {
                return;
            }

            var features = renderer.rendererFeatures
                .OfType<TopDown3DVolumetricDustFeature>()
                .ToArray();
            if (features.Length != 1)
            {
                errors.Add(
                    $"The perspective renderer must contain exactly one TopDown3D volumetric dust feature; found {features.Length}.");
                return;
            }

            var feature = features[0];
            if (!feature.isActive)
            {
                errors.Add("The TopDown3D volumetric dust renderer feature must be active.");
            }

            if (feature.VolumetricShader != shader)
            {
                errors.Add("The TopDown3D volumetric dust renderer feature must reference the canonical shader.");
            }

            if (feature.Downsample != 2
                || feature.RaymarchSteps != 16
                || feature.ShadowSamples != 8
                || !Mathf.Approximately(feature.DepthEdgeSharpness, 96f))
            {
                errors.Add("The TopDown3D volumetric dust renderer feature does not match the accepted high-quality spatial preset.");
            }

        }

        private static void ValidateVolumetricDustShader(ICollection<string> errors)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                TopDown3DPrototypeBuilder.VolumetricDustShaderPath);
            if (shader == null)
            {
                return;
            }

            var messages = ShaderUtil.GetShaderMessages(shader);
            if (!shader.isSupported || messages.Length > 0)
            {
                errors.Add(
                    $"The volumetric dust shader must compile without messages on the active editor platform. Supported={shader.isSupported}; messages={messages.Length}.");
            }
        }

        private static void ValidateBuildSettings(ICollection<string> errors)
        {
            var foundProductionScene = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (string.Equals(scene.path, TopDown3DPrototypeBuilder.ScenePath, StringComparison.Ordinal))
                {
                    foundProductionScene = true;
                    if (!scene.enabled)
                    {
                        errors.Add("TopDown3DPrototype must be enabled as the primary production scene.");
                    }
                }
            }

            if (!foundProductionScene)
            {
                errors.Add("TopDown3DPrototype must be present in production Build Settings.");
            }
        }

        private static void ValidateScene(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TopDown3DPrototypeBuilder.ScenePath) == null)
            {
                return;
            }

            var loadedScene = SceneManager.GetSceneByPath(TopDown3DPrototypeBuilder.ScenePath);
            var openedForValidation = !loadedScene.IsValid() || !loadedScene.isLoaded;
            if (openedForValidation && !Application.isBatchMode && HasDirtyLoadedScene())
            {
                errors.Add("Cannot inspect TopDown3DPrototype while another loaded scene has unsaved changes.");
                return;
            }

            var scene = openedForValidation
                ? EditorSceneManager.OpenScene(
                    TopDown3DPrototypeBuilder.ScenePath,
                    Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive)
                : loadedScene;
            try
            {
                var roots = scene.GetRootGameObjects();
                ValidateSingle<TopDown3DInputRouter>(roots, errors);
                ValidateSingle<TopDown3DPlayerMotor>(roots, errors);
                ValidateSingle<TopDown3DPlayerAnimationDriver>(roots, errors);
                ValidateSingle<TopDown3DFootstepDust>(roots, errors);
                ValidateSingle<TopDown3DCameraRig>(roots, errors);
                ValidateSingle<TopDown3DProceduralWorld>(roots, errors);
                ValidateSingle<TopDown3DBigArmFollower>(roots, errors);
                ValidateSingle<TopDown3DBigArmCargo>(roots, errors);
                ValidateSingle<TopDown3DBigArmState>(roots, errors);
                ValidateSingle<TopDown3DBigArmCargoAccess>(roots, errors);
                ValidateSingle<TopDown3DGameStateSaveService>(roots, errors);
                ValidateSingle<TopDown3DPlayerInventory>(roots, errors);
                ValidateSingle<TopDown3DInteractionController>(roots, errors);
                ValidateSingle<TopDown3DPlayerActionController>(roots, errors);
                ValidateSingle<TopDown3DResourceWorldState>(roots, errors);
                ValidateSingle<TopDown3DInventoryCanvas>(roots, errors);
                ValidateSingle<TopDown3DInventoryUiController>(roots, errors);
                ValidateSingle<TopDown3DInteractionFeedbackHud>(roots, errors);
                ValidateSingle<EventSystem>(roots, errors);
                ValidateSingle<InputSystemUIInputModule>(roots, errors);

                var cameras = FindComponents<Camera>(roots);
                if (cameras.Length != 1 || cameras[0].orthographic)
                {
                    errors.Add("TopDown3DPrototype must contain exactly one perspective Camera.");
                }
                else
                {
                    ValidateCameraRenderer(cameras[0], errors);
                }

                var cameraRig = FindComponents<TopDown3DCameraRig>(roots).SingleOrDefault();
                if (cameraRig != null
                    && (!Mathf.Approximately(
                            cameraRig.MaximumLookAheadDistance,
                            TopDown3DCameraRig.DefaultMaximumLookAheadDistance)
                        || !Mathf.Approximately(
                            cameraRig.LookAheadSpeed,
                            TopDown3DCameraRig.DefaultLookAheadSpeed)
                        || !Mathf.Approximately(
                            cameraRig.LookAheadReturnSpeed,
                            TopDown3DCameraRig.DefaultLookAheadReturnSpeed)))
                {
                    errors.Add("TopDown3DPrototype camera look-ahead range and outward/return speeds must match the canonical tuning.");
                }

                var bigArm = FindComponents<TopDown3DBigArmFollower>(roots).SingleOrDefault();
                var cargo = FindComponents<TopDown3DBigArmCargo>(roots).SingleOrDefault();
                var packing = AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>(TopDown3DPrototypeBuilder.PackingSettingsPath);
                var packingError = packing == null ? "missing" : null;
                var packingValid = packing != null && packing.TryValidate(out packingError);
                if (cargo == null || !packingValid || cargo.PackingSettings != packing || cargo.State.Capacity != TopDown3DPackingSettings.MountCount)
                {
                    errors.Add($"BigARM cargo must use the canonical 12-mount packing settings: {packingError}");
                }
                var box = bigArm != null ? bigArm.GetComponent<BoxCollider>() : null;
                if (box == null || box.size.x > 1.75f || box.size.z > 2f)
                {
                    errors.Add("BigARM must use the compact foundation footprint, not the original oversized spike volume.");
                }

                var animationDriver = FindComponents<TopDown3DPlayerAnimationDriver>(roots).SingleOrDefault();
                if (animationDriver != null && !animationDriver.HasCompleteAnimationSet)
                {
                    errors.Add("Booter's prototype Humanoid animation driver must reference the full locomotion, traversal, and Ironstone gather set.");
                }
                else if (animationDriver != null
                    && animationDriver.LocomotionProfile != AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(
                        TopDown3DPrototypeBuilder.LocomotionClipProfilePath))
                {
                    errors.Add("Booter's scene and builder must reference the same canonical locomotion profile.");
                }

                var inventoryCanvas = FindComponents<TopDown3DInventoryCanvas>(roots).SingleOrDefault();
                var gameHud = FindComponents<TopDown3DGameHudCanvas>(roots).SingleOrDefault();
                if (inventoryCanvas == null
                    || inventoryCanvas.GetComponent<GraphicRaycaster>() == null
                    || gameHud == null
                    || gameHud.GetComponent<GraphicRaycaster>() != null)
                {
                    errors.Add("Inventory must own the scene's interactive raycaster while the shared game HUD remains passive.");
                }

                var playerMotor = FindComponents<TopDown3DPlayerMotor>(roots).SingleOrDefault();
                var playerCapsule = playerMotor != null ? playerMotor.GetComponent<CapsuleCollider>() : null;
                var playerRenderer = playerMotor != null ? playerMotor.GetComponent<MeshRenderer>() : null;
                if (playerMotor != null
                    && Vector3.Distance(playerMotor.transform.localScale, Vector3.one) > 0.0001f)
                {
                    errors.Add("Booter's controller root must remain uniformly scaled so the Humanoid is not distorted.");
                }

                if (playerMotor != null)
                {
                    var serializedMotor = new SerializedObject(playerMotor);
                    if (!HasFloat(serializedMotor, "walkSpeed", 4.2f)
                        || !HasFloat(serializedMotor, "sprintSpeed", 7.4f)
                        || !HasFloat(serializedMotor, "acceleration", 8.5f)
                        || !HasFloat(serializedMotor, "deceleration", 12.5f)
                        || !HasFloat(serializedMotor, "stopDeceleration", 20f)
                        || !HasFloat(serializedMotor, "directionChangeAcceleration", 13.5f)
                        || !HasFloat(serializedMotor, "maxWalkableSlope", 48f))
                    {
                        errors.Add(
                            "Locomotion animation integration must preserve the approved motor speeds, commanded response rates, planted stop rate, and 48-degree slope limit.");
                    }
                }

                if (playerCapsule == null
                    || !Mathf.Approximately(playerCapsule.height, TopDown3DPrototypeBuilder.PlayerColliderHeight)
                    || !Mathf.Approximately(playerCapsule.radius, TopDown3DPrototypeBuilder.PlayerColliderRadius))
                {
                    errors.Add("Booter's gameplay capsule must match the enlarged prototype Humanoid body.");
                }

                if (playerRenderer == null || playerRenderer.enabled)
                {
                    errors.Add("Booter's controller pill must remain hidden; the Humanoid is the only player visual authority.");
                }

                if (animationDriver != null
                    && (!Mathf.Approximately(
                            animationDriver.VisualScale,
                            TopDown3DPlayerAnimationDriver.PrototypeVisualScale)
                        || !Mathf.Approximately(
                            animationDriver.VisualLocalPosition.y,
                            TopDown3DPlayerAnimationDriver.PrototypeVisualGroundOffset)))
                {
                    errors.Add("Booter's Humanoid must use the enlarged, ground-aligned prototype presentation scale.");
                }

                for (var i = 0; i < roots.Length; i++)
                {
                    var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                    for (var j = 0; j < transforms.Length; j++)
                    {
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[j].gameObject) > 0)
                        {
                            errors.Add($"Missing script on '{transforms[j].name}' in TopDown3DPrototype.");
                        }
                    }
                }
            }
            finally
            {
                if (openedForValidation && !Application.isBatchMode && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateCameraRenderer(Camera camera, ICollection<string> errors)
        {
            var additional = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additional == null)
            {
                errors.Add("Perspective camera is missing UniversalAdditionalCameraData.");
                return;
            }

            var serializedCamera = new SerializedObject(additional);
            var rendererIndex = serializedCamera.FindProperty("m_RendererIndex");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                ConversionBaselineValidator.PipelineAssetPath);
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            if (rendererIndex == null || pipeline == null || renderer == null)
            {
                errors.Add("Perspective camera renderer relationship could not be inspected.");
                return;
            }

            var serializedPipeline = new SerializedObject(pipeline);
            var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            var index = rendererIndex.intValue;
            if (rendererList == null
                || !rendererList.isArray
                || index <= 0
                || index >= rendererList.arraySize
                || rendererList.GetArrayElementAtIndex(index).objectReferenceValue != renderer)
            {
                errors.Add("Perspective camera renderer index does not resolve to the protected 3D renderer asset.");
            }
        }

        private static bool HasFloat(SerializedObject serializedObject, string name, float expected)
        {
            var property = serializedObject.FindProperty(name);
            return property != null && Mathf.Approximately(property.floatValue, expected);
        }

        private static void ValidateSingle<T>(GameObject[] roots, ICollection<string> errors) where T : Component
        {
            var count = FindComponents<T>(roots).Length;
            if (count != 1)
            {
                errors.Add($"TopDown3DPrototype must contain exactly one {typeof(T).Name}; found {count}.");
            }
        }

        private static T[] FindComponents<T>(GameObject[] roots) where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static bool HasDirtyLoadedScene()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatErrors(IReadOnlyList<string> errors)
        {
            return "Perspective top-down 3D prototype validation failed:\n- " + string.Join("\n- ", errors);
        }
    }
}
