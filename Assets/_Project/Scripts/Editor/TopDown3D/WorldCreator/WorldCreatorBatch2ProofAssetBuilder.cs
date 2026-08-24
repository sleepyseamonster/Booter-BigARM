using System;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator
{
    public static class WorldCreatorBatch2ProofAssetBuilder
    {
        public const string SettingsFolder = "Assets/_Project/Settings/WorldCreator";
        public const string ProvinceCatalogPath =
            SettingsFolder + "/Proof_NonCanon_GeologicProvinceCatalog.asset";
        public const string StrataCatalogPath =
            SettingsFolder + "/Proof_NonCanon_StrataFamilyCatalog.asset";
        public const string InfluenceProfilePath =
            SettingsFolder + "/Proof_NonCanon_LandscapeInfluence.asset";

        public const string BaselineProvinceId = "proof.non-canon.province.dry-shelf-baseline";
        public const string FracturedProvinceId = "proof.non-canon.province.fractured-canyon";
        public const string BaselineStrataId = "proof.non-canon.strata.iron-shelf";
        public const string FracturedStrataId = "proof.non-canon.strata.fractured-pale-iron";

        [MenuItem("Booter & BigARM/World Creator/Build Batch 2 Non-Canon Proof Assets")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCli()
        {
            Build();
        }

        public static void Build()
        {
            EnsureFolder(SettingsFolder);
            var provinces = LoadOrCreate<GeologicProvinceCatalog>(ProvinceCatalogPath);
            provinces.ConfigureProofData(new[]
            {
                new GeologicProvinceDefinition(
                    BaselineProvinceId,
                    "[NON-CANON PROOF] Dry Shelf Baseline",
                    new WorldLandscapeParameters(
                        -0.12f,
                        0.34f,
                        0.18f,
                        0.42f,
                        0.28f,
                        0.22f,
                        0.35f,
                        0.58f,
                        0.62f,
                        0.48f,
                        0.72f,
                        0.66f,
                        0.18f,
                        0.32f)),
                new GeologicProvinceDefinition(
                    FracturedProvinceId,
                    "[NON-CANON PROOF] Fractured Canyon Influence",
                    new WorldLandscapeParameters(
                        0.16f,
                        0.86f,
                        0.61f,
                        0.88f,
                        0.82f,
                        0.74f,
                        0.91f,
                        0.34f,
                        0.78f,
                        0.26f,
                        0.68f,
                        0.84f,
                        0.42f,
                        0.76f))
            });
            var strata = LoadOrCreate<StrataFamilyCatalog>(StrataCatalogPath);
            strata.ConfigureProofData(new[]
            {
                new StrataFamilyDefinition(
                    BaselineStrataId,
                    "[NON-CANON PROOF] Iron Shelf Strata",
                    0.62f,
                    0.18f,
                    0.36f,
                    0.76f,
                    0.28f,
                    0.44f),
                new StrataFamilyDefinition(
                    FracturedStrataId,
                    "[NON-CANON PROOF] Fractured Pale-Iron Strata",
                    0.78f,
                    0.57f,
                    0.91f,
                    0.88f,
                    0.63f,
                    0.52f)
            });
            var influence = LoadOrCreate<NonCanonProofInfluenceProfile>(InfluenceProfilePath);
            influence.ConfigureProofData(
                "proof.non-canon.influence.fractured-transect",
                1,
                BaselineProvinceId,
                BaselineStrataId,
                FracturedProvinceId,
                FracturedStrataId,
                -768d,
                768d,
                384d,
                0.35f);

            EditorUtility.SetDirty(provinces);
            EditorUtility.SetDirty(strata);
            EditorUtility.SetDirty(influence);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WorldCreatorAssetValidator.ValidateFromCli();
            Debug.Log("Built and validated the explicitly non-canon World Creator Batch 2 proof assets.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                throw new InvalidOperationException($"Asset path '{path}' contains an unexpected type.");
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
