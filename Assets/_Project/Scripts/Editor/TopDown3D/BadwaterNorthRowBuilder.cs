using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Creates new native Terrain assets in an isolated candidate; retained data is read-only.</summary>
    public static class BadwaterNorthRowBuilder
    {
        public static void ImportProductionProofFromCli()
        {
            var config=BadwaterNorthRowSource.Selected;
            string path=config.SourceRoot+"/unity_production_validation.json";
            var report=JsonUtility.FromJson<BadwaterTerrainExpansionValidator.Report>(File.ReadAllText(BadwaterNorthRowSource.ProjectFile(path)));
            if(report.status!="validated_full_grid_and_collider_samples" || report.terrains!=config.TotalCount ||
                report.scene!=BadwaterNorthRowSource.ProductionScene ||
                report.scene_sha256!=BadwaterNorthRowSource.Hash(BadwaterNorthRowSource.ProjectFile(report.scene)) ||
                report.manifest_sha256!=BadwaterNorthRowSource.Hash(BadwaterNorthRowSource.ProjectFile(config.ManifestPath)))
                throw new InvalidDataException("Production proof does not describe the saved scene and source.");
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            if(string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) throw new IOException("Production proof metadata import failed.");
        }

        public static void BuildCandidateFromCli()
        {
            var config=BadwaterNorthRowSource.Selected;
            string root = Path.GetDirectoryName(Application.dataPath);
            if (File.Exists(Path.Combine(root, ".git")) || Directory.Exists(Path.Combine(root, ".git")))
                throw new InvalidOperationException("Build the candidate in an isolated validation copy, never the live checkout.");
            if (File.Exists(BadwaterNorthRowSource.ProjectFile(config.CandidateScene)))
                throw new IOException("Candidate scene already exists; preserve it and select a fresh validation batch.");
            var manifest = BadwaterNorthRowSource.ReadManifest(config,true);
            if (BadwaterNorthRowSource.Hash(BadwaterNorthRowSource.ProjectFile(BadwaterNorthRowSource.ProductionScene))!=manifest.predecessor_scene_sha256) throw new InvalidDataException("Saved predecessor scene changed or step order was skipped.");
            BadwaterNorthRowSource.VerifyProtectedFiles(config,manifest);
            foreach (var tile in manifest.new_tiles)
                if (File.Exists(BadwaterNorthRowSource.ProjectFile(tile.data_path)) || File.Exists(BadwaterNorthRowSource.ProjectFile(tile.layer_path)))
                    throw new IOException("Refuse to overwrite candidate TerrainData/TerrainLayer: " + tile.geographic_key);

            Scene scene = EditorSceneManager.OpenScene(BadwaterNorthRowSource.ProductionScene, OpenSceneMode.Single);
            var retained = BadwaterNorthRowSource.Terrains(scene);
            if (retained.Count != config.RetainedCount) throw new InvalidDataException("Candidate must start from the retained ordered predecessor terrain scene.");
            Transform common = retained[0].GetComponentInParent<BadwaterTerrainConnectivity>()?.transform;
            if (common == null) throw new InvalidDataException("Missing common terrain owner.");
            foreach (var terrain in retained)
                if (terrain.GetComponentInParent<BadwaterTerrainConnectivity>()?.transform != common) throw new InvalidDataException("Retained common-parent contract changed.");
            var connectivity = common.GetComponent<BadwaterTerrainConnectivity>();
            if (connectivity == null) throw new InvalidDataException("Missing retained connectivity owner.");
            Material material = retained[0].materialTemplate;
            int layer = retained[0].gameObject.layer;
            var sections = new Dictionary<string, Transform>();
            foreach (string name in new[] { config.Section })
            {
                var group = new GameObject("Terrain Section | " + name);
                group.transform.SetParent(common, false);
                sections.Add(name, group.transform);
            }
            int count = 0;
            foreach (var tile in manifest.new_tiles)
            {
                string colorPath = config.SourceRoot + "/" + tile.color_file;
                var importer = AssetImporter.GetAtPath(colorPath) as TextureImporter;
                if (importer == null) throw new InvalidDataException("Missing imported color: " + colorPath);
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 128;
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
                if (texture == null || texture.width != 128 || texture.height != 128) throw new InvalidDataException("Incorrect geographic color dimensions.");
                var terrainLayer = new TerrainLayer { diffuseTexture = texture, tileSize = new Vector2(256, 256), tileOffset = Vector2.zero };
                Directory.CreateDirectory(BadwaterNorthRowSource.ProjectFile(Path.GetDirectoryName(tile.layer_path)));
                AssetDatabase.CreateAsset(terrainLayer, tile.layer_path);
                var data = new TerrainData { heightmapResolution = 257, size = new Vector3(256, 1800, 256),
                    alphamapResolution = 128, baseMapResolution = 128 };
                data.terrainLayers = new[] { terrainLayer };
                var alpha = new float[128, 128, 1];
                for (int z = 0; z < 128; z++) for (int x = 0; x < 128; x++) alpha[z, x, 0] = 1;
                data.SetAlphamaps(0, 0, alpha);
                data.SetHeights(0, 0, BadwaterNorthRowSource.ReadNormalized(config,tile));
                Directory.CreateDirectory(BadwaterNorthRowSource.ProjectFile(Path.GetDirectoryName(tile.data_path)));
                AssetDatabase.CreateAsset(data, tile.data_path);
                GameObject go = Terrain.CreateTerrainGameObject(data);
                go.name = "Terrain_" + tile.section + "_2m_" + tile.local_id;
                go.layer = layer;
                go.transform.SetParent(sections[tile.section], false);
                go.transform.position = new Vector3(tile.position[0], tile.position[1], tile.position[2]);
                go.AddComponent<TopDown3DGroundSurface>();
                var terrain = go.GetComponent<Terrain>();
                terrain.materialTemplate = material;
                terrain.groupingID = 26911;
                terrain.allowAutoConnect = true;
                terrain.drawInstanced = retained[0].drawInstanced;
                terrain.heightmapPixelError = retained[0].heightmapPixelError;
                terrain.basemapDistance = retained[0].basemapDistance;
                if (++count % 64 == 0) Debug.Log("EXPANSION_TERRAINS_CREATED: " + count);
            }
            connectivity.Reconnect();
            Directory.CreateDirectory(BadwaterNorthRowSource.ProjectFile(config.AssetRoot + "/Prefabs"));
            foreach (var section in sections)
            {
                string path = config.AssetRoot + "/Prefabs/" + section.Key + ".prefab";
                if (File.Exists(BadwaterNorthRowSource.ProjectFile(path))) throw new IOException("Section prefab already exists.");
                PrefabUtility.SaveAsPrefabAsset(section.Value.gameObject, path);
            }
            AssetDatabase.SaveAssets();
            BadwaterNorthRowSource.VerifyProtectedFiles(config,manifest);
            BadwaterNorthRowValidator.ValidateScene(scene, true);
            Directory.CreateDirectory(BadwaterNorthRowSource.ProjectFile("Assets/_Project/Scenes/Reference"));
            if (!EditorSceneManager.SaveScene(scene, config.CandidateScene))
                throw new IOException("Could not save isolated candidate.");
            BadwaterNorthRowSource.VerifyProtectedFiles(config,manifest);
            Debug.Log("EXPANSION_CANDIDATE_SAVED: 256 new terrains plus the unchanged predecessor grid; production scene was not saved.");
        }

        /// <summary>Adds only verified section prefabs to a saved production scene; never copies stale gameplay objects.</summary>
        public static void IntegrateSavedSceneFromCli()
        {
            string project = Path.GetDirectoryName(Application.dataPath);
            if (File.Exists(Path.Combine(project, ".git")) || Directory.Exists(Path.Combine(project, ".git")))
                throw new InvalidOperationException("CLI integration is restricted to the isolated validation copy.");
            var scene = EditorSceneManager.OpenScene(BadwaterNorthRowSource.ProductionScene, OpenSceneMode.Single);
            IntegrateVerifiedSections(scene);
        }

        public static void IntegrateVerifiedSections(Scene scene, int step = 0)
        {
            var config=step==0 ? BadwaterNorthRowSource.Selected : BadwaterNorthRowSource.ForStep(step);
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != BadwaterNorthRowSource.ProductionScene || scene.isDirty)
                throw new InvalidOperationException("Integration requires the saved, unmodified production scene outside Play mode.");
            var manifest = BadwaterNorthRowSource.ReadManifest(config,true);
            if (BadwaterNorthRowSource.Hash(BadwaterNorthRowSource.ProjectFile(BadwaterNorthRowSource.ProductionScene))!=manifest.predecessor_scene_sha256) throw new InvalidDataException("Saved predecessor scene changed or step order was skipped.");
            BadwaterNorthRowSource.VerifyProtectedFiles(config,manifest);
            var retained = BadwaterNorthRowSource.Terrains(scene);
            if (retained.Count != config.RetainedCount) throw new InvalidDataException("Production scene already contains candidate terrain or unrelated extra terrain.");
            Transform common = retained[0].GetComponentInParent<BadwaterTerrainConnectivity>()?.transform;
            if (common == null) throw new InvalidDataException("Missing common terrain owner.");
            var owner = common.GetComponent<BadwaterTerrainConnectivity>();
            if (owner == null) throw new InvalidDataException("Retained connectivity owner missing.");
            foreach (var terrain in retained) if (terrain.GetComponentInParent<BadwaterTerrainConnectivity>()?.transform != common) throw new InvalidDataException("Retained hierarchy changed.");
            var added = new List<GameObject>();
            try
            {
                foreach (string section in new[] { config.Section })
                {
                    string path = config.AssetRoot + "/Prefabs/" + section + ".prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null || prefab.GetComponentsInChildren<Terrain>(true).Length != 256 ||
                        prefab.GetComponentsInChildren<BadwaterTerrainConnectivity>(true).Length != 0)
                        throw new InvalidDataException("Unverified section prefab: " + path);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    // Keep production ownership explicit in scene YAML; source prefabs remain reusable.
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    instance.transform.SetParent(common, false);
                    added.Add(instance);
                }
                owner.Reconnect();
                BadwaterNorthRowValidator.ValidateScene(scene, true);
                if (AssetDatabase.AssetPathToGUID(scene.path) != manifest.scene_guid) throw new InvalidDataException("Production scene GUID changed.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Production scene save failed.");
            }
            catch
            {
                foreach (var go in added) if (go != null) UnityEngine.Object.DestroyImmediate(go);
                owner.Reconnect();
                throw;
            }
        }
    }
}
