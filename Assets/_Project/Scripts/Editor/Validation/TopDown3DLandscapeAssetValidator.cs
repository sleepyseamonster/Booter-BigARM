using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class TopDown3DLandscapeAssetValidator
    {
        private static readonly string[] ManifoldPluginPaths =
        {
            "Assets/_Project/Plugins/Manifold/macOS/libmanifold.dylib",
            "Assets/_Project/Plugins/Manifold/macOS/libmanifoldc.dylib"
        };

        [MenuItem("Booter & BigARM/Top Down 3D/Validate Production Landscape Assets")]
        public static void ValidateFromMenu()
        {
            var errors = CollectErrors();
            if (errors.Count == 0)
            {
                Debug.Log("TopDown3D production landscape asset validation passed.");
                return;
            }

            Debug.LogError("TopDown3D production landscape asset validation failed:\n- "
                + string.Join("\n- ", errors));
        }

        public static void ValidateFromCli()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "TopDown3D production landscape asset validation failed:\n- "
                    + string.Join("\n- ", errors));
            }
        }

        public static void ValidateRockFamilyFromCli()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DNaturalObjectCatalog>(
                TopDown3DPrototypeBuilder.NaturalObjectCatalogPath);
            var errors = CollectRockFamilyErrors(catalog);
            ValidateFusionPluginsAreEditorOnly(errors);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "TopDown3D production rock-family validation failed:\n- "
                    + string.Join("\n- ", errors));
            }

            Debug.Log("TopDown3D production rock-family validation passed.");
        }

        public static List<string> CollectErrors()
        {
            var errors = TopDown3DPrototypeValidator.CollectLandscapeErrors();
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DNaturalObjectCatalog>(
                TopDown3DPrototypeBuilder.NaturalObjectCatalogPath);
            errors.AddRange(CollectRockFamilyErrors(catalog));
            ValidateFusionPluginsAreEditorOnly(errors);
            return errors;
        }

        internal static List<string> CollectRockFamilyErrors(TopDown3DNaturalObjectCatalog catalog)
        {
            var errors = new List<string>();
            if (catalog == null)
            {
                errors.Add("The production natural-object catalog is missing.");
                return errors;
            }

            var expectedCount = Enum.GetValues(typeof(TopDown3DNaturalObjectShape)).Length
                * TopDown3DNaturalObjectCatalog.MeshVariantsPerShape;
            if (catalog.MeshFamilies.Count != expectedCount)
            {
                errors.Add(
                    $"The natural-object catalog requires exactly {expectedCount} baked rock families; found {catalog.MeshFamilies.Count}.");
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < catalog.MeshFamilies.Count; i++)
            {
                var family = catalog.MeshFamilies[i];
                if (family == null)
                {
                    errors.Add($"Baked rock family entry {i} is null.");
                    continue;
                }

                var key = $"{(int)family.Shape}:{family.Variant}";
                if (!keys.Add(key))
                {
                    errors.Add($"Baked rock family key {key} is duplicated.");
                }

                if (!stableIds.Add(family.StableId ?? string.Empty))
                {
                    errors.Add($"Baked rock stable ID '{family.StableId}' is empty or duplicated.");
                }

                var expectedId = TopDown3DProductionRockBaker.GetStableId(family.Shape, family.Variant);
                if (!string.Equals(family.StableId, expectedId, StringComparison.Ordinal))
                {
                    errors.Add(
                        $"Baked rock family {key} must use stable ID '{expectedId}', not '{family.StableId}'.");
                }

                if (!family.IsComplete)
                {
                    errors.Add($"Baked rock family '{family.StableId}' has incomplete LOD or collider data.");
                    continue;
                }

                ValidateLods(family, errors);
            }

            foreach (TopDown3DNaturalObjectShape shape in Enum.GetValues(typeof(TopDown3DNaturalObjectShape)))
            {
                for (var variant = 0; variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape; variant++)
                {
                    if (!catalog.TryGetMeshFamily(shape, variant, out _))
                    {
                        errors.Add($"The catalog is missing baked mesh family {shape}:{variant}.");
                    }
                }
            }

            return errors;
        }

        private static void ValidateLods(
            TopDown3DNaturalMeshFamily family,
            ICollection<string> errors)
        {
            var previousTriangleCount = int.MaxValue;
            for (var lod = 0; lod < 3; lod++)
            {
                var mesh = family.GetLod(lod);
                if (mesh == null)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(mesh);
                if (string.IsNullOrEmpty(path)
                    || !path.StartsWith(TopDown3DProductionRockBaker.OutputFolder + "/", StringComparison.Ordinal))
                {
                    errors.Add(
                        $"{family.StableId} LOD{lod} must be a baked asset under {TopDown3DProductionRockBaker.OutputFolder}.");
                }

                if (!mesh.isReadable)
                {
                    errors.Add($"{family.StableId} LOD{lod} must remain readable for deterministic chunk combination.");
                }

                var triangles = mesh.triangles;
                if (mesh.vertexCount == 0 || triangles.Length < 3 || triangles.Length % 3 != 0)
                {
                    errors.Add($"{family.StableId} LOD{lod} has invalid geometry.");
                }

                var triangleCount = triangles.Length / 3;
                if (triangleCount >= previousTriangleCount)
                {
                    errors.Add($"{family.StableId} LOD{lod} does not reduce triangle count from the previous LOD.");
                }

                previousTriangleCount = triangleCount;
                var bounds = mesh.bounds;
                if (!IsFinite(bounds.center)
                    || !IsFinite(bounds.size)
                    || bounds.size.x <= 0f
                    || bounds.size.y <= 0f
                    || bounds.size.z <= 0f)
                {
                    errors.Add($"{family.StableId} LOD{lod} has invalid bounds.");
                }

                if (TopDown3DProductionRockBaker.UsesApprovedRestingPose(family.Shape))
                {
                    if (bounds.min.y > 0.001f)
                    {
                        errors.Add(
                            $"{family.StableId} LOD{lod} resting geometry begins above the ground plane.");
                    }
                    else if (bounds.min.y < -bounds.size.y * 0.2f)
                    {
                        errors.Add(
                            $"{family.StableId} LOD{lod} burial exceeds the approved resting-depth envelope.");
                    }
                }
                else if (Mathf.Abs(bounds.min.y) > 0.001f)
                {
                    errors.Add($"{family.StableId} LOD{lod} pivot must sit on the ground plane.");
                }
            }

            var lod0Bounds = family.Lod0.bounds;
            var colliderSize = family.ColliderSize;
            if (colliderSize.x > lod0Bounds.size.x * 1.001f
                || colliderSize.y > lod0Bounds.size.y * 1.001f
                || colliderSize.z > lod0Bounds.size.z * 1.001f)
            {
                errors.Add($"{family.StableId} collider extends beyond its LOD0 render bounds.");
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void ValidateFusionPluginsAreEditorOnly(ICollection<string> errors)
        {
            for (var i = 0; i < ManifoldPluginPaths.Length; i++)
            {
                var path = ManifoldPluginPaths[i];
                var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                if (importer == null)
                {
                    errors.Add($"Missing editor-only rock-bake plugin importer at {path}.");
                    continue;
                }

                if (!importer.GetCompatibleWithEditor())
                {
                    errors.Add($"Rock-bake plugin {path} must remain available to the Editor.");
                }

                if (importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX))
                {
                    errors.Add($"Rock-bake plugin {path} must be excluded from Standalone players.");
                }
            }
        }
    }
}
