using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Captures retained heights without repairing or saving any project asset.</summary>
    public static class BadwaterTerrainBaselineCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        private const string SourceRoot = "Assets/_Project/Art/Terrain/BadwaterFourSlices";

        [Serializable] public sealed class Report
        {
            public int schema_version = 1;
            public string scene, scene_guid, scene_sha256, captured_utc;
            public string encoding = "little-endian float32 normalized height, south-to-north rows, west-to-east columns";
            public List<Tile> tiles = new List<Tile>();
        }

        [Serializable] public sealed class Tile
        {
            public string id, geographic_key, data_path, data_guid, data_sha256, meta_sha256;
            public string heights_file, heights_sha256, source_file, source_sha256;
            public string material_path, material_guid;
            public Vector3 position, size;
            public int resolution;
            public bool collider_matches, ground_marker;
        }

        [Serializable] public sealed class QuantizationReport
        {
            public float[] offsets, max_world_errors_m;
            public int[] mismatched_samples;
        }

        /// <summary>Probe only temporary TerrainData; never writes retained heights.</summary>
        public static void ProbeHeightRoundtripFromCli()
        {
            string output = null;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-terrainQuantizationReport") output = Path.GetFullPath(args[i + 1]);
            if (string.IsNullOrEmpty(output) || File.Exists(output)) throw new InvalidOperationException("Use a fresh report file.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var terrains = new List<Terrain>();
            foreach (var root in scene.GetRootGameObjects()) terrains.AddRange(root.GetComponentsInChildren<Terrain>(true));
            var offsets = new[] { 0f, 0.0000001f, 0.25f / 65535f, 0.5f / 65535f };
            var report = new QuantizationReport { offsets = offsets, max_world_errors_m = new float[offsets.Length],
                mismatched_samples = new int[offsets.Length] };
            TerrainData candidate = new TerrainData { heightmapResolution = 257, size = new Vector3(256f, 1800f, 256f) };
            try
            {
                foreach (var terrain in terrains)
                {
                    float[,] expected = terrain.terrainData.GetHeights(0, 0, 257, 257);
                    for (int test = 0; test < offsets.Length; test++)
                    {
                        float[,] input = (float[,])expected.Clone();
                        for (int z = 0; z < 257; z++)
                        for (int x = 0; x < 257; x++) input[z, x] += offsets[test];
                        candidate.SetHeights(0, 0, input);
                        float[,] actual = candidate.GetHeights(0, 0, 257, 257);
                        for (int z = 0; z < 257; z++)
                        for (int x = 0; x < 257; x++)
                        {
                            float error = Mathf.Abs(actual[z, x] - expected[z, x]) * 1800f;
                            if (error != 0f) report.mismatched_samples[test]++;
                            report.max_world_errors_m[test] = Mathf.Max(report.max_world_errors_m[test], error);
                        }
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(candidate); }
            File.WriteAllText(output, JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("TERRAIN_QUANTIZATION_PROBED: " + JsonUtility.ToJson(report));
        }

        public static void CaptureFromCli()
        {
            string output = null;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-terrainBaselineOutput") output = Path.GetFullPath(args[i + 1]);
            if (string.IsNullOrEmpty(output) || Directory.Exists(output) || File.Exists(output))
                throw new InvalidOperationException("Provide a fresh -terrainBaselineOutput directory.");
            if (!BitConverter.IsLittleEndian) throw new PlatformNotSupportedException("Little-endian capture required.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var terrain = new List<Terrain>();
            foreach (var root in scene.GetRootGameObjects()) terrain.AddRange(root.GetComponentsInChildren<Terrain>(true));
            if (terrain.Count != 256) throw new InvalidDataException("Expected exactly 256 retained terrains.");
            terrain.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var report = new Report { scene = ScenePath, scene_guid = AssetDatabase.AssetPathToGUID(ScenePath),
                scene_sha256 = Hash(ScenePath), captured_utc = DateTime.UtcNow.ToString("O") };
            if (report.scene_guid != "4de5dd018ee194314a00fd369e2d3eeb") throw new InvalidDataException("Scene GUID changed.");
            var ownedFiles = new Dictionary<string, string>();
            ownedFiles.Add(ScenePath, report.scene_sha256);
            var addresses = new HashSet<string>();
            Directory.CreateDirectory(output);
            foreach (Terrain t in terrain)
            {
                string path = AssetDatabase.GetAssetPath(t.terrainData);
                string id = Path.GetFileNameWithoutExtension(path);
                if (!addresses.Add(id) || path != SourceRoot + "/TerrainData/" + id + ".asset")
                    throw new InvalidDataException("Unexpected or duplicated TerrainData ownership: " + path);
                int row = int.Parse(id.Substring(1, 2)), col = int.Parse(id.Substring(5, 2));
                if (row < 0 || row > 15 || col < 0 || col > 15 || id != $"r{row:00}_c{col:00}")
                    throw new InvalidDataException("Invalid tile address: " + id);
                var data = t.terrainData;
                if (data.heightmapResolution != 257 || data.size != new Vector3(256f, 1800f, 256f) ||
                    t.transform.position != new Vector3(col * 256f - 2048f, -100f, (15 - row) * 256f - 2048f))
                    throw new InvalidDataException("Retained placement/grid mismatch: " + id);
                string file = id + ".bytes", source = SourceRoot + "/Source/Heights/" + id + ".bytes";
                float[,] samples = data.GetHeights(0, 0, 257, 257);
                byte[] raw = new byte[257 * 257 * sizeof(float)];
                Buffer.BlockCopy(samples, 0, raw, 0, raw.Length);
                File.WriteAllBytes(Path.Combine(output, file), raw);
                var tile = new Tile { id = id, geographic_key = $"epsg26911/e{520400 + 256 * col}/n{4010296 - 256 * (row + 1)}/size256",
                    data_path = path, data_guid = AssetDatabase.AssetPathToGUID(path), data_sha256 = Hash(path),
                    meta_sha256 = Hash(path + ".meta"), heights_file = file, heights_sha256 = Hash(Path.Combine(output, file)),
                    source_file = source, source_sha256 = Hash(source), position = t.transform.position, size = data.size,
                    resolution = data.heightmapResolution, collider_matches = t.GetComponent<TerrainCollider>()?.terrainData == data,
                    ground_marker = t.GetComponent<BooterBigArm.TopDown3D.TopDown3DGroundSurface>() != null,
                    material_path = AssetDatabase.GetAssetPath(t.materialTemplate),
                    material_guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t.materialTemplate)) };
                if (!tile.collider_matches) throw new InvalidDataException("Collider data mismatch: " + id);
                ownedFiles.Add(path, tile.data_sha256);
                ownedFiles.Add(path + ".meta", tile.meta_sha256);
                ownedFiles.Add(source, tile.source_sha256);
                report.tiles.Add(tile);
            }
            foreach (var pair in ownedFiles)
                if (Hash(pair.Key) != pair.Value) throw new IOException("Asset changed during readback: " + pair.Key);
            File.WriteAllText(Path.Combine(output, "baseline.json"), JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("TERRAIN_BASELINE_CAPTURED: 256 retained full height grids; project assets were not saved.");
        }

        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
