using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor.WorldCreator
{
    public static class WorldCreatorProductionPathValidator
    {
        public const string ProductionScenePath =
            "Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity";
        public const string ProductionProfilePath =
            "Assets/_Project/Resources/WorldCreator/ProductionWorldCreatorProfile.asset";

        private const string GeneratorSourcePath =
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DWorldGenerator.cs";
        private const string WorldSourcePath =
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DProceduralWorld.cs";
        private const string FarSourcePath =
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DFarLandscape.cs";
        private const string NaturalPlannerSourcePath =
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DNaturalObjectPlanner.cs";
        private const string GeologicalPlannerSourcePath =
            "Assets/_Project/Scripts/Runtime/TopDown3D/WorldCreator/Geology/WorldRockFormationPlanner.cs";
        private const string ChunkMeshSourcePath =
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DChunkMeshBuilder.cs";
        private const string DustPlannerSourcePath =
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DDustDepositionPlanner.cs";

        [MenuItem("Booter & BigARM/Validation/Validate World Creator Production Path")]
        public static void ValidateMenu()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "World Creator production-path validation failed:\n- "
                    + string.Join("\n- ", errors));
            }

            Debug.Log("World Creator production-path validation passed.");
        }

        public static IReadOnlyList<string> CollectErrors()
        {
            var errors = new List<string>();
            ValidateProfile(errors);
            ValidateSourceAuthority(errors);
            ValidateProductionScene(errors);
            return errors.AsReadOnly();
        }

        private static void ValidateProfile(ICollection<string> errors)
        {
            var profile = AssetDatabase.LoadAssetAtPath<WorldCreatorProductionProfile>(
                ProductionProfilePath);
            if (profile == null)
            {
                errors.Add("The Resources-backed World Creator production profile is missing.");
                return;
            }

            if (!profile.TryValidate(out var error))
            {
                errors.Add(error);
            }

            if (!profile.NonCanonProofOnly
                || profile.CreateVersionManifest().Topology
                    != WorldCreatorProductionProfile.CurrentTopologyVersion)
            {
                errors.Add("The production profile must remain non-canon and topology v2.");
            }

            if (TopDown3DGameStateSnapshot.CurrentVersion != 2)
            {
                errors.Add("Prototype saves must use the strict topology-v2 snapshot format.");
            }
        }

        private static void ValidateSourceAuthority(ICollection<string> errors)
        {
            var generator = ReadSource(GeneratorSourcePath, errors);
            var world = ReadSource(WorldSourcePath, errors);
            var far = ReadSource(FarSourcePath, errors);
            var naturalPlanner = ReadSource(NaturalPlannerSourcePath, errors);
            var geologicalPlanner = ReadSource(GeologicalPlannerSourcePath, errors);
            var chunkMesh = ReadSource(ChunkMeshSourcePath, errors);
            var dustPlanner = ReadSource(DustPlannerSourcePath, errors);
            if (generator == null || world == null || far == null
                || naturalPlanner == null || geologicalPlanner == null
                || chunkMesh == null || dustPlanner == null)
            {
                return;
            }

            if (generator.Contains("SampleCore", StringComparison.Ordinal)
                || generator.Contains("FractalNoise", StringComparison.Ordinal)
                || generator.Contains("Mathf.PerlinNoise", StringComparison.Ordinal)
                || !generator.Contains("IWorldQueryService", StringComparison.Ordinal))
            {
                errors.Add("TopDown3DWorldGenerator still contains or bypasses the retired scalar macro authority.");
            }

            if (!world.Contains("WorldCreatorProductionRuntime", StringComparison.Ordinal)
                || !world.Contains("WorldRepresentationTier.Near", StringComparison.Ordinal)
                || !world.Contains("DrainIntegrationQueue", StringComparison.Ordinal)
                || world.Contains(
                    "TopDown3DChunkMeshBuilder.BuildMesh(settings, worldGenerator",
                    StringComparison.Ordinal))
            {
                errors.Add("Near terrain is not exclusively routed through the World Creator scheduler.");
            }

            if (!far.Contains("WorldRepresentationTier.Mid", StringComparison.Ordinal)
                || !far.Contains("WorldRepresentationTier.Far", StringComparison.Ordinal)
                || far.Contains("generator.Sample(", StringComparison.Ordinal))
            {
                errors.Add("Middle or far terrain bypasses the canonical representation scheduler.");
            }

            if (!naturalPlanner.Contains(
                    "TopDown3DGeologicalRockAdapter.BuildPhysicalFormations",
                    StringComparison.Ordinal)
                || naturalPlanner.Contains(
                    "TopDown3DRockFormationPlanner.BuildPhysicalFormations",
                    StringComparison.Ordinal)
                || !geologicalPlanner.Contains("WorldRockFormationPlanner", StringComparison.Ordinal)
                || !geologicalPlanner.Contains("WorldVersionDomain.Decoration", StringComparison.Ordinal)
                || !geologicalPlanner.Contains("NoveltyFingerprint", StringComparison.Ordinal)
                || !geologicalPlanner.Contains("StructuralDirection", StringComparison.Ordinal))
            {
                errors.Add("Physical rocks are not exclusively authored by the geological reservation planner.");
            }

            if (!chunkMesh.Contains("WorldTerrainMaterialPackingAdapter.Pack", StringComparison.Ordinal)
                || chunkMesh.Contains("SemanticColor(", StringComparison.Ordinal)
                || !dustPlanner.Contains("Authority.Materials.TrySample", StringComparison.Ordinal)
                || !dustPlanner.Contains(
                    "TopDown3DGeologicalRockAdapter.BuildPhysicalFormations",
                    StringComparison.Ordinal)
                || dustPlanner.Contains(
                    "TopDown3DRockFormationPlanner.BuildPhysicalFormations",
                    StringComparison.Ordinal))
            {
                errors.Add("Terrain packing or dust bypasses the semantic surface-material authority.");
            }
        }

        private static void ValidateProductionScene(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ProductionScenePath) == null)
            {
                errors.Add("The production TopDown3D scene is missing.");
                return;
            }

            var loaded = SceneManager.GetSceneByPath(ProductionScenePath);
            var opened = !loaded.IsValid() || !loaded.isLoaded;
            if (opened && !Application.isBatchMode && HasDirtyLoadedScene())
            {
                errors.Add("Cannot inspect the production scene while another loaded scene is dirty.");
                return;
            }

            var scene = opened
                ? EditorSceneManager.OpenScene(
                    ProductionScenePath,
                    Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive)
                : loaded;
            try
            {
                var worlds = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TopDown3DProceduralWorld>(true))
                    .ToArray();
                if (worlds.Length != 1)
                {
                    errors.Add($"The production scene must contain exactly one TopDown3DProceduralWorld; found {worlds.Length}.");
                    return;
                }

                var serialized = new SerializedObject(worlds[0]);
                if (serialized.FindProperty("settings")?.objectReferenceValue == null
                    || serialized.FindProperty("streamingTarget")?.objectReferenceValue == null
                    || serialized.FindProperty("groundMaterial")?.objectReferenceValue == null)
                {
                    errors.Add("The production procedural world is missing settings, target, or ground material wiring.");
                }
            }
            finally
            {
                if (opened && !Application.isBatchMode && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static string ReadSource(string path, ICollection<string> errors)
        {
            if (!File.Exists(path))
            {
                errors.Add($"Missing production source: {path}");
                return null;
            }

            return File.ReadAllText(path);
        }

        private static bool HasDirtyLoadedScene()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
