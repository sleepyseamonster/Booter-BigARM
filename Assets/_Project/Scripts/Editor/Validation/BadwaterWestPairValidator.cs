using Report = BooterBigArm.Editor.BadwaterTerrainExpansionValidator.Report;
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
    public static class BadwaterWestPairValidator
    {
        public static void ValidateFromCli()
        {
            string path = Argument("-terrainExpansionScene") ?? BadwaterWestPairSource.CandidateScene;
            if (path != BadwaterWestPairSource.CandidateScene && path != BadwaterWestPairSource.ProductionScene)
                throw new InvalidDataException("Unexpected expansion validation scene.");
            var report = ValidateScene(EditorSceneManager.OpenScene(path, OpenSceneMode.Single), true);
            string output = Argument("-terrainExpansionReport");
            if (!string.IsNullOrEmpty(output))
            {
                if (File.Exists(output)) throw new IOException("Report path already exists.");
                File.WriteAllText(output, JsonUtility.ToJson(report, true) + "\n");
            }
        }

        public static Report ValidateScene(Scene scene, bool physics = false)
        {
            var manifest = BadwaterWestPairSource.ReadManifest(true);
            BadwaterWestPairSource.VerifyProtectedFiles(manifest);
            var terrains = BadwaterWestPairSource.Terrains(scene);
            if (terrains.Count != 1536) throw new InvalidDataException("Expansion must contain exactly 1536 terrains.");
            var records = new Dictionary<string, BadwaterTerrainExpansionSource.Tile>();
            foreach (var t in manifest.new_tiles) records.Add(t.data_path, t);
            foreach (var t in manifest.retained_tiles) records.Add(t.data_path, t);
            var grid = new Dictionary<Vector2Int, Terrain>();
            var heights = new Dictionary<Vector2Int, float[,]>();
            var dataPaths = new HashSet<string>();
            var report = new Report { terrains = 1536, retained = 1024, new_terrains = 512, scene = scene.path,
                scene_sha256 = BadwaterWestPairSource.Hash(BadwaterWestPairSource.ProjectFile(scene.path)),
                manifest_sha256 = BadwaterWestPairSource.Hash(BadwaterWestPairSource.ProjectFile(BadwaterWestPairSource.ManifestPath)) };
            BadwaterTerrainConnectivity owner = null;
            if (physics) Physics.SyncTransforms();
            foreach (Terrain terrain in terrains)
            {
                string path = AssetDatabase.GetAssetPath(terrain.terrainData);
                BadwaterTerrainExpansionSource.Tile tile;
                if (!records.TryGetValue(path, out tile) || !dataPaths.Add(path)) throw new InvalidDataException("Unknown/duplicate TerrainData ownership.");
                var data = terrain.terrainData;
                if (terrain.transform.position != new Vector3(tile.position[0], tile.position[1], tile.position[2]) ||
                    data.size != new Vector3(256, 1800, 256) || data.heightmapResolution != 257 ||
                    terrain.GetComponent<TerrainCollider>()?.terrainData != data || terrain.GetComponent<TopDown3DGroundSurface>() == null ||
                    !terrain.allowAutoConnect || terrain.groupingID != 26911 || terrain.materialTemplate == null ||
                    terrain.materialTemplate.shader.name != "Universal Render Pipeline/Terrain/Lit" || data.terrainLayers.Length != 1 ||
                    data.terrainLayers[0].diffuseTexture == null)
                    throw new InvalidDataException("Native terrain contract mismatch: " + path);
                var connection = terrain.GetComponentInParent<BadwaterTerrainConnectivity>();
                if (connection == null || (owner != null && owner != connection)) throw new InvalidDataException("Cross-section connectivity has competing owners.");
                owner = connection;
                var cell = new Vector2Int(Mathf.RoundToInt(terrain.transform.position.x / 256), Mathf.RoundToInt(terrain.transform.position.z / 256));
                if (grid.ContainsKey(cell)) throw new InvalidDataException("Overlapping chunk interiors.");
                grid.Add(cell, terrain);
                float[,] actual = data.GetHeights(0, 0, 257, 257), expected = BadwaterWestPairSource.ReadNormalized(tile);
                bool retained = string.IsNullOrEmpty(tile.section);
                if (retained && (AssetDatabase.AssetPathToGUID(path) != tile.data_guid ||
                    !BadwaterWestPairSource.HashMatches(path, tile.data_sha256) ||
                    BadwaterWestPairSource.Hash(BadwaterWestPairSource.ProjectFile(path + ".meta")) != tile.meta_sha256))
                    throw new InvalidDataException("Retained GUID/payload changed: " + path);
                for (int z = 0; z < 257; z++) for (int x = 0; x < 257; x++)
                {
                    float error = Mathf.Abs(actual[z, x] - expected[z, x]) * 1800;
                    if (retained && error != 0) throw new InvalidDataException("Retained full readback changed.");
                    if (!retained) report.max_new_readback_error_m = Mathf.Max(report.max_new_readback_error_m, error);
                }
                heights.Add(cell, actual);
                if (physics)
                {
                    var collider = terrain.GetComponent<TerrainCollider>();
                    foreach (int z in new[] { 1, 64, 128, 192, 255 }) foreach (int x in new[] { 1, 64, 128, 192, 255 })
                    {
                        Vector3 point = terrain.transform.position + new Vector3(x, 0, z);
                        float target = terrain.transform.position.y + data.GetInterpolatedHeight(x / 256f, z / 256f);
                        RaycastHit hit;
                        if (!collider.Raycast(new Ray(new Vector3(point.x, 3000, point.z), Vector3.down), out hit, 5000))
                            throw new InvalidDataException("Missing terrain collision: " + path);
                        report.max_collider_error_m = Mathf.Max(report.max_collider_error_m, Mathf.Abs(hit.point.y - target));
                        report.collider_samples++;
                    }
                }
            }
            foreach (var pair in grid)
            {
                Terrain left, top, right, bottom;
                grid.TryGetValue(pair.Key + Vector2Int.left, out left); grid.TryGetValue(pair.Key + Vector2Int.up, out top);
                grid.TryGetValue(pair.Key + Vector2Int.right, out right); grid.TryGetValue(pair.Key + Vector2Int.down, out bottom);
                if (pair.Value.leftNeighbor != left || pair.Value.topNeighbor != top || pair.Value.rightNeighbor != right || pair.Value.bottomNeighbor != bottom)
                    throw new InvalidDataException("Reciprocal neighbor mismatch at " + pair.Key);
                if (left == null) report.outer_edges++; if (top == null) report.outer_edges++;
                if (right == null) report.outer_edges++; if (bottom == null) report.outer_edges++;
                float[,] a = heights[pair.Key];
                if (right != null)
                {
                    float[,] b = heights[pair.Key + Vector2Int.right];
                    for (int i = 0; i < 257; i++) if (a[i, 256] != b[i, 0]) throw new InvalidDataException("E/W border differs at " + pair.Key + ", " + i);
                    report.edges++;
                }
                if (top != null)
                {
                    float[,] b = heights[pair.Key + Vector2Int.up];
                    for (int i = 0; i < 257; i++) if (a[256, i] != b[0, i]) throw new InvalidDataException("N/S border differs at " + pair.Key + ", " + i);
                    report.edges++;
                }
            }
            if (report.edges != 2992 || report.outer_edges != 160 || report.max_new_readback_error_m > 1800f / 65532f + 0.00025f ||
                (physics && (report.collider_samples != 38400 || report.max_collider_error_m > 8 * 1.1920929e-7f * 5000)))
                throw new InvalidDataException("Expansion edge/readback/physics precision gate failed: " + JsonUtility.ToJson(report));
            if (owner.GetComponentsInChildren<BadwaterTerrainConnectivity>(true).Length != 1)
                throw new InvalidDataException("Competing child connectivity owner.");
            report.status = physics ? "validated_full_grid_and_collider_samples" : "validated_full_grid_readback_and_neighbors";
            Debug.Log("EXPANSION_VALIDATED: " + JsonUtility.ToJson(report));
            return report;
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
