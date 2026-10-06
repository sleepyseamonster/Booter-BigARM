using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TerrainUtils;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class BadwaterTerrainSeamRepair
    {
        public const string ScenePath = "Assets/_Project/Scenes/TopDown3D/GreaterWasteland.unity";
        private const string SourceRoot = "Assets/_Project/Art/Terrain/BadwaterFourSlices";

        [MenuItem("Booter & BigARM/Badwater Terrain/Repair Saved Terrain Seams")]
        public static void RepairActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved, unmodified Badwater scene outside Play mode before repair.");
            var grid = GetGrid(scene);
            var heightmaps = new Dictionary<Vector2Int, float[,]>();
            for (int row = 0; row < 16; row++)
            for (int col = 0; col < 16; col++)
            {
                Terrain tile = grid[row, col];
                string id = $"r{row:00}_c{col:00}";
                string path = SourceRoot + "/TerrainData/" + id + ".asset";
                if (AssetDatabase.GetAssetPath(tile.terrainData) != path ||
                    tile.transform.position != new Vector3(col * 256f - 2048f, -100f, (15 - row) * 256f - 2048f) ||
                    tile.terrainData.size != new Vector3(256f, 1800f, 256f) ||
                    tile.GetComponent<TerrainCollider>()?.terrainData != tile.terrainData)
                    throw new InvalidDataException("Unexpected terrain ownership or placement: " + id);
                int samples = row >= 12 && row <= 13 && col >= 1 && col <= 2 ? 257 : 129;
                byte[] raw = File.ReadAllBytes(SourceRoot + "/Source/Heights/" + id + ".bytes");
                heightmaps.Add(new Vector2Int(col, 15 - row), BadwaterHeightmapStitching.PromoteContextGrid(
                    BadwaterHeightmapStitching.DecodeSource(raw, samples, row, col)));
            }
            BadwaterHeightmapStitching.StitchBorders(heightmaps);
            // Back up originals before the first mutation; never replace the TerrainData asset or GUID.
            Directory.CreateDirectory("Logs/BadwaterSeamRepairBackup");
            Backup(ScenePath, "Logs/BadwaterSeamRepairBackup/BadwaterFourSlices.unity");
            foreach (Terrain tile in grid)
            {
                string path = AssetDatabase.GetAssetPath(tile.terrainData);
                Backup(path, "Logs/BadwaterSeamRepairBackup/" + Path.GetFileName(path));
            }
            for (int row = 0; row < 16; row++)
            for (int col = 0; col < 16; col++)
            {
                Terrain tile = grid[row, col];
                tile.terrainData.heightmapResolution = BadwaterHeightmapStitching.RenderResolution;
                // Setting resolution can change the physical size; restore the geographic bounds.
                tile.terrainData.size = new Vector3(256f, 1800f, 256f);
                tile.terrainData.SetHeights(0, 0, heightmaps[new Vector2Int(col, 15 - row)]);
                tile.groupingID = 26911;
                tile.allowAutoConnect = true;
                if (tile.GetComponent<TopDown3DGroundSurface>() == null)
                    tile.gameObject.AddComponent<TopDown3DGroundSurface>();
                EditorUtility.SetDirty(tile.terrainData);
            }
            AssetDatabase.SaveAssets();
            GameObject terrainRoot = grid[0, 0].transform.parent.gameObject;
            if (terrainRoot.GetComponent<BadwaterTerrainConnectivity>() == null)
                terrainRoot.AddComponent<BadwaterTerrainConnectivity>();
            TerrainUtility.AutoConnect();
            Validate(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save terrain seam repair.");
            Debug.Log("BADWATER_SEAMS_REPAIRED: 256 compatible terrain grids, 480 identical shared edges, persistent neighbor stitching and ground markers.");
        }

        [MenuItem("Booter & BigARM/Badwater Terrain/Validate Saved Terrain Seams")]
        public static void ValidateActiveScene() => Validate(SceneManager.GetActiveScene());

        public static Terrain[,] GetGrid(Scene scene)
        {
            if (scene.path != ScenePath) throw new InvalidDataException("Expected the Badwater scene.");
            var grid = new Terrain[16, 16];
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Terrain tile in root.GetComponentsInChildren<Terrain>(true))
            {
                string name = tile.name;
                string id = name.Substring(name.LastIndexOf("r", StringComparison.Ordinal));
                int row = int.Parse(id.Substring(1, 2), CultureInfo.InvariantCulture);
                int col = int.Parse(id.Substring(5, 2), CultureInfo.InvariantCulture);
                if (row < 0 || row >= 16 || col < 0 || col >= 16 || grid[row, col] != null)
                    throw new InvalidDataException("Invalid or duplicated terrain tile: " + name);
                grid[row, col] = tile;
                count++;
            }
            if (count != 256) throw new InvalidDataException("Expected 256 terrain tiles.");
            return grid;
        }

        public static void Validate(Scene scene)
        {
            var grid = GetGrid(scene);
            if (grid[0, 0].transform.parent.GetComponent<BadwaterTerrainConnectivity>() == null)
                throw new InvalidDataException("Missing Badwater terrain connection lifecycle component.");
            var heights = new float[256][,];
            for (int row = 0; row < 16; row++)
            for (int col = 0; col < 16; col++)
            {
                Terrain tile = grid[row, col];
                if (!tile.allowAutoConnect || tile.groupingID != 26911 ||
                    tile.terrainData.heightmapResolution != 257 ||
                    tile.GetComponent<TopDown3DGroundSurface>() == null)
                    throw new InvalidDataException("Terrain stitching/ground contract missing: " + tile.name);
                if (tile.leftNeighbor != (col > 0 ? grid[row, col - 1] : null) ||
                    tile.rightNeighbor != (col < 15 ? grid[row, col + 1] : null) ||
                    tile.topNeighbor != (row > 0 ? grid[row - 1, col] : null) ||
                    tile.bottomNeighbor != (row < 15 ? grid[row + 1, col] : null))
                    throw new InvalidDataException("Terrain neighbor wiring mismatch: " + tile.name);
                heights[row * 16 + col] = tile.terrainData.GetHeights(0, 0, 257, 257);
            }
            for (int row = 0; row < 16; row++)
            for (int col = 0; col < 16; col++)
            for (int i = 0; i < 257; i++)
            {
                var a = heights[row * 16 + col];
                if (col < 15 && a[i, 256] != heights[row * 16 + col + 1][i, 0] ||
                    row < 15 && a[0, i] != heights[(row + 1) * 16 + col][256, i])
                    throw new InvalidDataException($"Open terrain edge at r{row:00}_c{col:00}, sample {i}.");
            }
            Debug.Log("BADWATER_SEAMS_VALIDATED: all 960 directed neighbor links and 480 shared edges, including corners, match exactly.");
        }

        private static void Backup(string source, string destination)
        {
            if (!File.Exists(destination)) File.Copy(source, destination);
        }
    }
}
