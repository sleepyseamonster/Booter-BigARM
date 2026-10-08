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
    public static class BadwaterTerrainExpansionBuilder
    {
        public static void BuildCandidateFromCli()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            if (File.Exists(Path.Combine(root, ".git")) || Directory.Exists(Path.Combine(root, ".git")))
                throw new InvalidOperationException("Build the candidate in an isolated validation copy, never the live checkout.");
            if (File.Exists(BadwaterTerrainExpansionSource.ProjectFile(BadwaterTerrainExpansionSource.CandidateScene)))
                throw new IOException("Candidate scene already exists; preserve it and select a fresh validation batch.");
            var manifest = BadwaterTerrainExpansionSource.ReadManifest(true);
            BadwaterTerrainExpansionSource.VerifyProtectedFiles(manifest);
            foreach (var tile in manifest.new_tiles)
                if (File.Exists(BadwaterTerrainExpansionSource.ProjectFile(tile.data_path)) || File.Exists(BadwaterTerrainExpansionSource.ProjectFile(tile.layer_path)))
                    throw new IOException("Refuse to overwrite candidate TerrainData/TerrainLayer: " + tile.geographic_key);

            Scene scene = EditorSceneManager.OpenScene(BadwaterTerrainExpansionSource.ProductionScene, OpenSceneMode.Single);
            var retained = BadwaterTerrainExpansionSource.Terrains(scene);
            if (retained.Count != 256) throw new InvalidDataException("Candidate must start from the retained 256-terrain scene.");
            Transform common = retained[0].transform.parent;
            foreach (var terrain in retained)
                if (terrain.transform.parent != common) throw new InvalidDataException("Retained common-parent contract changed.");
            var connectivity = common.GetComponent<BadwaterTerrainConnectivity>();
            if (connectivity == null) throw new InvalidDataException("Missing retained connectivity owner.");
            Material material = retained[0].materialTemplate;
            int layer = retained[0].gameObject.layer;
            var sections = new Dictionary<string, Transform>();
            foreach (string name in new[] { "west", "north", "northwest" })
            {
                var group = new GameObject("Terrain Section | " + name);
                group.transform.SetParent(common, false);
                sections.Add(name, group.transform);
            }
            int count = 0;
            foreach (var tile in manifest.new_tiles)
            {
                string colorPath = BadwaterTerrainExpansionSource.SourceRoot + "/" + tile.color_file;
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
                Directory.CreateDirectory(BadwaterTerrainExpansionSource.ProjectFile(Path.GetDirectoryName(tile.layer_path)));
                AssetDatabase.CreateAsset(terrainLayer, tile.layer_path);
                var data = new TerrainData { heightmapResolution = 257, size = new Vector3(256, 1800, 256),
                    alphamapResolution = 128, baseMapResolution = 128 };
                data.terrainLayers = new[] { terrainLayer };
                var alpha = new float[128, 128, 1];
                for (int z = 0; z < 128; z++) for (int x = 0; x < 128; x++) alpha[z, x, 0] = 1;
                data.SetAlphamaps(0, 0, alpha);
                data.SetHeights(0, 0, BadwaterTerrainExpansionSource.ReadNormalized(tile));
                Directory.CreateDirectory(BadwaterTerrainExpansionSource.ProjectFile(Path.GetDirectoryName(tile.data_path)));
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
            Directory.CreateDirectory(BadwaterTerrainExpansionSource.ProjectFile(BadwaterTerrainExpansionSource.AssetRoot + "/Prefabs"));
            foreach (var section in sections)
            {
                string path = BadwaterTerrainExpansionSource.AssetRoot + "/Prefabs/" + section.Key + ".prefab";
                if (File.Exists(BadwaterTerrainExpansionSource.ProjectFile(path))) throw new IOException("Section prefab already exists.");
                PrefabUtility.SaveAsPrefabAsset(section.Value.gameObject, path);
            }
            AssetDatabase.SaveAssets();
            BadwaterTerrainExpansionSource.VerifyProtectedFiles(manifest);
            BadwaterTerrainExpansionValidator.ValidateScene(scene, true);
            Directory.CreateDirectory(BadwaterTerrainExpansionSource.ProjectFile("Assets/_Project/Scenes/Reference"));
            if (!EditorSceneManager.SaveScene(scene, BadwaterTerrainExpansionSource.CandidateScene))
                throw new IOException("Could not save isolated candidate.");
            BadwaterTerrainExpansionSource.VerifyProtectedFiles(manifest);
            Debug.Log("EXPANSION_CANDIDATE_SAVED: 768 new terrains plus 256 unchanged retained terrains; production scene was not saved.");
        }

        /// <summary>Adds only verified section prefabs to a saved production scene; never copies stale gameplay objects.</summary>
        public static void IntegrateSavedSceneFromCli()
        {
            string project = Path.GetDirectoryName(Application.dataPath);
            if (File.Exists(Path.Combine(project, ".git")) || Directory.Exists(Path.Combine(project, ".git")))
                throw new InvalidOperationException("CLI integration is restricted to the isolated validation copy.");
            var scene = EditorSceneManager.OpenScene(BadwaterTerrainExpansionSource.ProductionScene, OpenSceneMode.Single);
            IntegrateVerifiedSections(scene);
        }

        public static void IntegrateVerifiedSections(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != BadwaterTerrainExpansionSource.ProductionScene || scene.isDirty)
                throw new InvalidOperationException("Integration requires the saved, unmodified production scene outside Play mode.");
            var manifest = BadwaterTerrainExpansionSource.ReadManifest(true);
            BadwaterTerrainExpansionSource.VerifyProtectedFiles(manifest);
            var retained = BadwaterTerrainExpansionSource.Terrains(scene);
            if (retained.Count != 256) throw new InvalidDataException("Production scene already contains candidate terrain or unrelated extra terrain.");
            Transform common = retained[0].transform.parent;
            var owner = common.GetComponent<BadwaterTerrainConnectivity>();
            if (owner == null) throw new InvalidDataException("Retained connectivity owner missing.");
            foreach (var terrain in retained) if (terrain.transform.parent != common) throw new InvalidDataException("Retained hierarchy changed.");
            var added = new List<GameObject>();
            try
            {
                foreach (string section in new[] { "west", "north", "northwest" })
                {
                    string path = BadwaterTerrainExpansionSource.AssetRoot + "/Prefabs/" + section + ".prefab";
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
                BadwaterTerrainExpansionValidator.ValidateScene(scene, true);
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
