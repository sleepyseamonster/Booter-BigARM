using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class BadwaterNorthRow2Builder
    {
        public static void BuildCandidateFromCli()
        {
            RequireIsolation();
            var c = BadwaterNorthRow2Source.Selected;
            if (File.Exists(BadwaterNorthRow2Source.ProjectFile(c.CandidateScene))) throw new IOException("Preserve existing candidate; select a fresh attempt.");
            var m = BadwaterNorthRow2Source.ReadManifest(c, true);
            if (BadwaterNorthRow2Source.Hash(BadwaterNorthRow2Source.ProductionScene) != m.predecessor_scene_sha256) throw new InvalidDataException("Saved predecessor scene changed.");
            BadwaterNorthRow2Source.VerifyProtectedFiles(m);
            var scene = EditorSceneManager.OpenScene(BadwaterNorthRow2Source.ProductionScene, OpenSceneMode.Single);
            var retained = BadwaterNorthRow2Source.Terrains(scene);
            if (retained.Count != c.RetainedCount) throw new InvalidDataException("Wrong predecessor terrain count.");
            var owner = retained[0].GetComponentInParent<BadwaterTerrainConnectivity>();
            if (owner == null || retained.Any(t => t.GetComponentInParent<BadwaterTerrainConnectivity>() != owner)) throw new InvalidDataException("Expected single existing terrain parent.");
            var group = new GameObject("Terrain Section | " + c.Section);
            group.transform.SetParent(owner.transform, false);
            var sectors = new Transform[4,4];
            for (int z=0;z<4;z++) for (int x=0;x<4;x++)
            {
                var sector = new GameObject($"{GreaterWastelandTerrainWorkspace.SectorPrefix}z{z:00}_x{x:00} | 1 km");
                sector.transform.SetParent(group.transform, false); sectors[z,x] = sector.transform;
            }
            foreach (var tile in m.new_tiles)
            {
                if (File.Exists(BadwaterNorthRow2Source.ProjectFile(tile.data_path)) || File.Exists(BadwaterNorthRow2Source.ProjectFile(tile.layer_path))) throw new IOException("Refuse to overwrite native candidate asset.");
                string colorPath = c.SourceRoot + "/" + tile.color_file;
                var importer = AssetImporter.GetAtPath(colorPath) as TextureImporter;
                if (importer == null) throw new InvalidDataException("Missing imported new geographic imagery.");
                importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true; importer.alphaSource = TextureImporterAlphaSource.None;
                importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 128;
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
                if (texture == null || texture.width != 128 || texture.height != 128) throw new InvalidDataException("Incorrect 2 m geographic crop dimensions.");
                Directory.CreateDirectory(BadwaterNorthRow2Source.ProjectFile(Path.GetDirectoryName(tile.layer_path)));
                var terrainLayer = new TerrainLayer { diffuseTexture = texture, tileSize = new Vector2(256,256), tileOffset = Vector2.zero };
                AssetDatabase.CreateAsset(terrainLayer, tile.layer_path);
                var data = new TerrainData { heightmapResolution = 257, size = new Vector3(256,1800,256), alphamapResolution = 128, baseMapResolution = 128 };
                data.terrainLayers = new[] {terrainLayer}; var alpha = new float[128,128,1];
                for (int z=0;z<128;z++) for (int x=0;x<128;x++) alpha[z,x,0] = 1;
                data.SetAlphamaps(0,0,alpha); data.SetHeights(0,0,BadwaterNorthRow2Source.ReadRender(c,tile));
                Directory.CreateDirectory(BadwaterNorthRow2Source.ProjectFile(Path.GetDirectoryName(tile.data_path)));
                AssetDatabase.CreateAsset(data, tile.data_path);
                var go = Terrain.CreateTerrainGameObject(data); go.name = "Terrain_" + c.Section + "_2m_" + tile.local_id; go.layer = retained[0].gameObject.layer;
                int row = (4022584-tile.bounds_m[1])/256-1, col = (tile.bounds_m[0]-c.East)/256;
                go.transform.SetParent(sectors[(15-row)/4,col/4], false); go.transform.position = new Vector3(tile.position[0],tile.position[1],tile.position[2]);
                go.AddComponent<TopDown3DGroundSurface>(); var terrain = go.GetComponent<Terrain>();
                terrain.materialTemplate = retained[0].materialTemplate; terrain.groupingID = 26911; terrain.allowAutoConnect = true;
                terrain.drawInstanced = retained[0].drawInstanced; terrain.heightmapPixelError = retained[0].heightmapPixelError; terrain.basemapDistance = retained[0].basemapDistance;
            }
            owner.Reconnect();
            Directory.CreateDirectory(BadwaterNorthRow2Source.ProjectFile(c.AssetRoot + "/Prefabs"));
            PrefabUtility.SaveAsPrefabAsset(group, c.AssetRoot + "/Prefabs/" + c.Section + ".prefab");
            AssetDatabase.SaveAssets();
            SealNewNativeAssets(c, m);
            BadwaterNorthRow2Source.VerifyProtectedFiles(m);
            if (!EditorSceneManager.SaveScene(scene, c.CandidateScene)) throw new IOException("Candidate save failed.");
            Debug.Log("NORTH_ROW2_CANDIDATE_SAVED: 256 native terrains, 16 sectors; predecessor scene not saved.");
        }
        private static void SealNewNativeAssets(BadwaterNorthRow2Source.Config c, BadwaterNorthRow2Source.Manifest m)
        {
            foreach (var tile in m.new_tiles)
            {
                var data = AssetDatabase.LoadAssetAtPath<TerrainData>(tile.data_path);
                var heights = data.GetHeights(0,0,257,257); var raw = new byte[heights.Length*4]; Buffer.BlockCopy(heights,0,raw,0,raw.Length);
                string path = c.SourceRoot + "/NativeSnapshot/" + Path.GetFileNameWithoutExtension(tile.data_path) + ".bytes";
                Directory.CreateDirectory(BadwaterNorthRow2Source.ProjectFile(Path.GetDirectoryName(path)));
                if (File.Exists(BadwaterNorthRow2Source.ProjectFile(path))) throw new IOException("Native snapshot already exists.");
                File.WriteAllBytes(BadwaterNorthRow2Source.ProjectFile(path),raw);
                tile.native_reference = new BadwaterNorthRow2Source.NativeReference { path = path, sha256 = BadwaterNorthRow2Source.Hash(path), orientation = "south-first", samples = 257 };
                tile.data_guid = AssetDatabase.AssetPathToGUID(tile.data_path); tile.data_sha256 = BadwaterNorthRow2Source.Hash(tile.data_path); tile.meta_sha256 = BadwaterNorthRow2Source.Hash(tile.data_path + ".meta");
            }
            m.status = "native_assets_created_pending_saved_scene_validation";
            File.WriteAllText(BadwaterNorthRow2Source.ProjectFile(c.ManifestPath),JsonUtility.ToJson(m,true)+"\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        public static void IntegrateSavedSceneFromCli()
        {
            RequireIsolation(); var c = BadwaterNorthRow2Source.Selected; var m = BadwaterNorthRow2Source.ReadManifest(c,true);
            if (BadwaterNorthRow2Source.Hash(BadwaterNorthRow2Source.ProductionScene) != m.predecessor_scene_sha256) throw new InvalidDataException("Saved predecessor changed.");
            BadwaterNorthRow2Source.VerifyProtectedFiles(m);
            var scene = EditorSceneManager.OpenScene(BadwaterNorthRow2Source.ProductionScene,OpenSceneMode.Single);
            var retained = BadwaterNorthRow2Source.Terrains(scene);
            if (retained.Count != c.RetainedCount || scene.isDirty) throw new InvalidDataException("Expected clean retained production scene.");
            var owner = retained[0].GetComponentInParent<BadwaterTerrainConnectivity>();
            string prefabPath = c.AssetRoot + "/Prefabs/" + c.Section + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || prefab.transform.childCount != 16 || prefab.GetComponentsInChildren<Terrain>(true).Length != 256 || prefab.GetComponentsInChildren<BadwaterTerrainConnectivity>(true).Length != 0) throw new InvalidDataException("Unverified section prefab.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            instance.transform.SetParent(owner.transform,false); owner.Reconnect();
            if (AssetDatabase.AssetPathToGUID(scene.path) != m.scene_guid || !EditorSceneManager.SaveScene(scene)) throw new IOException("Production integration save/GUID gate failed.");
        }
        private static void RequireIsolation()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            if (Directory.Exists(Path.Combine(root,".git")) || File.Exists(Path.Combine(root,".git"))) throw new InvalidOperationException("Native construction/integration is isolated-only.");
        }
    }
}
