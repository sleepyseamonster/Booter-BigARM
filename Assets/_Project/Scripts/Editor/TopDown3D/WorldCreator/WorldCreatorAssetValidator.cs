using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator
{
    public static class WorldCreatorAssetValidator
    {
        private static readonly string[] ProtectedConsumerPaths =
        {
            "Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity",
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset"
        };

        [MenuItem("Booter & BigARM/World Creator/Validate Batch 2 Non-Canon Proof")]
        public static void ValidateFromMenu()
        {
            var errors = CollectErrors();
            if (errors.Count == 0)
            {
                Debug.Log("World Creator Batch 2 non-canon proof validation passed.");
                return;
            }

            Debug.LogError("World Creator Batch 2 validation failed:\n- " + string.Join("\n- ", errors));
        }

        public static void ValidateFromCli()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "World Creator Batch 2 validation failed:\n- " + string.Join("\n- ", errors));
            }

            Debug.Log("World Creator Batch 2 non-canon proof validation passed.");
        }

        public static List<string> CollectErrors()
        {
            var errors = new List<string>();
            var provinces = AssetDatabase.LoadAssetAtPath<GeologicProvinceCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.ProvinceCatalogPath);
            var strata = AssetDatabase.LoadAssetAtPath<StrataFamilyCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.StrataCatalogPath);
            var influence = AssetDatabase.LoadAssetAtPath<NonCanonProofInfluenceProfile>(
                WorldCreatorBatch2ProofAssetBuilder.InfluenceProfilePath);
            ValidateCatalog(provinces, errors);
            ValidateCatalog(strata, errors);
            ValidateInfluence(influence, provinces, strata, errors);
            ValidateDormantAssetBoundary(errors);
            return errors;
        }

        private static void ValidateCatalog(GeologicProvinceCatalog catalog, ICollection<string> errors)
        {
            if (catalog == null)
            {
                errors.Add("The non-canon geologic province catalog is missing.");
                return;
            }

            if (!catalog.NonCanonProofOnly)
            {
                errors.Add("The Batch 2 geologic province catalog lost its non-canon proof marker.");
            }

            if (!catalog.TryValidate(out var error))
            {
                errors.Add(error);
                return;
            }

            for (var i = 0; i < catalog.Definitions.Count; i++)
            {
                ValidateProofId(catalog.Definitions[i].StableId, errors);
            }
        }

        private static void ValidateCatalog(StrataFamilyCatalog catalog, ICollection<string> errors)
        {
            if (catalog == null)
            {
                errors.Add("The non-canon strata family catalog is missing.");
                return;
            }

            if (!catalog.NonCanonProofOnly)
            {
                errors.Add("The Batch 2 strata family catalog lost its non-canon proof marker.");
            }

            if (!catalog.TryValidate(out var error))
            {
                errors.Add(error);
                return;
            }

            for (var i = 0; i < catalog.Definitions.Count; i++)
            {
                ValidateProofId(catalog.Definitions[i].StableId, errors);
            }
        }

        private static void ValidateInfluence(
            NonCanonProofInfluenceProfile influence,
            GeologicProvinceCatalog provinces,
            StrataFamilyCatalog strata,
            ICollection<string> errors)
        {
            if (influence == null)
            {
                errors.Add("The non-canon landscape influence profile is missing.");
                return;
            }

            if (!influence.TryValidate(out var error))
            {
                errors.Add(error);
                return;
            }

            if (provinces == null || strata == null)
            {
                return;
            }

            var sampler = new WorldCoordinateContextSampler(provinces, strata, influence);
            var model = new NonCanonProofCoordinateModel();
            var world = new WorldIdentity(24681357L, new WorldVersionManifest(1, 1, 1, 1, 1, 1, 1));
            var proofPositions = new[]
            {
                new AbsoluteWorldPosition(-1200d, 0d, 0d),
                new AbsoluteWorldPosition(0d, 0d, 0d),
                new AbsoluteWorldPosition(1200d, 0d, 0d),
                new AbsoluteWorldPosition(0d, 0d, influence.IntentionalBoundaryHorizontalB + 1d)
            };
            for (var i = 0; i < proofPositions.Length; i++)
            {
                var address = model.Encode(proofPositions[i]);
                if (!sampler.TrySample(world, model, address, out _, out error))
                {
                    errors.Add($"Proof context sample {i} failed: {error}");
                }
            }
        }

        private static void ValidateDormantAssetBoundary(ICollection<string> errors)
        {
            var proofAssets = new HashSet<string>(StringComparer.Ordinal)
            {
                WorldCreatorBatch2ProofAssetBuilder.ProvinceCatalogPath,
                WorldCreatorBatch2ProofAssetBuilder.StrataCatalogPath,
                WorldCreatorBatch2ProofAssetBuilder.InfluenceProfilePath
            };
            for (var i = 0; i < ProtectedConsumerPaths.Length; i++)
            {
                var dependencies = AssetDatabase.GetDependencies(ProtectedConsumerPaths[i], true);
                for (var dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
                {
                    if (proofAssets.Contains(dependencies[dependencyIndex]))
                    {
                        errors.Add(
                            $"Protected production asset '{ProtectedConsumerPaths[i]}' references dormant proof asset '{dependencies[dependencyIndex]}'.");
                    }
                }
            }
        }

        private static void ValidateProofId(string stableId, ICollection<string> errors)
        {
            if (string.IsNullOrEmpty(stableId)
                || !stableId.StartsWith(NonCanonProofInfluenceProfile.RequiredIdPrefix, StringComparison.Ordinal))
            {
                errors.Add(
                    $"Batch 2 authored geology ID '{stableId}' must remain explicitly non-canon.");
            }
        }
    }
}
