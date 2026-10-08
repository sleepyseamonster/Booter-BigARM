using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>The section-aware extension of the retained Badwater source contract.</summary>
    public static class BadwaterTerrainExpansionSource
    {
        public const string AssetRoot = "Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07";
        public const string SourceRoot = AssetRoot + "/Source";
        public const string ManifestPath = SourceRoot + "/manifest.json";
        public const string RetainedRoot = "Assets/_Project/Art/Terrain/BadwaterFourSlices";
        public const string ProductionScene = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        public const string CandidateScene = "Assets/_Project/Scenes/Reference/TerrainExpansionCandidate.unity";

        [Serializable] public sealed class Manifest
        {
            public int schema_version, tile_count, new_tile_count;
            public string working_crs, asset_root, scene_guid, prepared_manifest_sha256;
            public int[] bounds_m, unity_origin_m;
            public float minimum_m, range_m;
            public Tile[] new_tiles, retained_tiles;
            public ProtectedFile[] protected_files;
        }
        [Serializable] public sealed class ProtectedFile { public string path, sha256; }
        [Serializable] public sealed class Tile
        {
            public string section, local_id, geographic_key, source_version, data_path, data_guid, data_sha256, meta_sha256;
            public string render_file, render_sha256, height_file, height_sha256, color_file, color_sha256, layer_path;
            public int[] bounds_m;
            public float[] position, size;
            public int source_spacing_m, source_samples, render_samples;
        }

        public static string ProjectFile(string relative)
        {
            if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("Expected a project-relative path.");
            string root = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));
            string result = Path.GetFullPath(Path.Combine(root, relative));
            if (!result.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Path escapes project: " + relative);
            return result;
        }

        public static string SourceFile(string relative)
        {
            string path = ProjectFile(SourceRoot + "/" + relative);
            string root = ProjectFile(SourceRoot);
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Resource escapes expansion source root.");
            return path;
        }

        public static string Hash(string absolute)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(absolute))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        public static Manifest ReadManifest(bool verifySources = false)
        {
            var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(ProjectFile(ManifestPath)));
            if (m == null || m.schema_version != 1 || m.working_crs != "EPSG:26911" || m.asset_root != AssetRoot ||
                m.tile_count != 1024 || m.new_tile_count != 768 || m.minimum_m != -100f || m.range_m != 1800f ||
                m.scene_guid != "4de5dd018ee194314a00fd369e2d3eeb" || m.unity_origin_m == null || m.unity_origin_m.Length != 2 ||
                m.unity_origin_m[0] != 522448 || m.unity_origin_m[1] != 4008248 || m.bounds_m == null || m.bounds_m.Length != 4 ||
                m.bounds_m[0] != 516304 || m.bounds_m[1] != 4006200 || m.bounds_m[2] != 524496 || m.bounds_m[3] != 4014392 ||
                m.new_tiles == null || m.new_tiles.Length != 768 || m.retained_tiles == null || m.retained_tiles.Length != 256)
                throw new InvalidDataException("Unsupported expansion footprint, encoding or identity.");
            var addresses = new HashSet<string>();
            var dataPaths = new HashSet<string>();
            var counts = new Dictionary<string, int> { { "west", 0 }, { "north", 0 }, { "northwest", 0 } };
            foreach (Tile tile in m.new_tiles)
            {
                if (!counts.ContainsKey(tile.section)) throw new InvalidDataException("Unknown section.");
                counts[tile.section]++;
                int row, col;
                ParseAddress(tile.local_id, out row, out col);
                int xmin = tile.section == "north" ? 520400 : 516304;
                int ymax = tile.section == "west" ? 4010296 : 4014392;
                int east = xmin + col * 256, north = ymax - (row + 1) * 256;
                string expectedKey = $"epsg26911/e{east}/n{north}/size256";
                if (tile.geographic_key != expectedKey || !addresses.Add(expectedKey) || tile.bounds_m == null || tile.bounds_m.Length != 4 ||
                    tile.bounds_m[0] != east || tile.bounds_m[1] != north || tile.bounds_m[2] != east + 256 || tile.bounds_m[3] != north + 256 ||
                    tile.source_spacing_m != 2 || tile.source_samples != 129 || tile.render_samples != 257 || string.IsNullOrEmpty(tile.source_version) ||
                    tile.data_path != AssetRoot + "/TerrainData/" + tile.section + "/" + tile.local_id + ".asset" || !dataPaths.Add(tile.data_path) ||
                    tile.layer_path != AssetRoot + "/TerrainLayers/" + tile.section + "/" + tile.local_id + ".terrainlayer")
                    throw new InvalidDataException("New tile identity/placement mismatch: " + tile.local_id);
                CheckDimensions(tile, east - 522448, north - 4008248);
                if (verifySources)
                {
                    VerifySource(tile.render_file, tile.render_sha256, 257 * 257 * 4);
                    VerifySource(tile.height_file, tile.height_sha256, 129 * 129 * 2);
                    VerifySource(tile.color_file, tile.color_sha256, null);
                }
            }
            foreach (int count in counts.Values) if (count != 256) throw new InvalidDataException("Incomplete section.");
            foreach (Tile tile in m.retained_tiles)
            {
                int row, col;
                ParseAddress(tile.local_id, out row, out col);
                int east = 520400 + col * 256, north = 4010296 - (row + 1) * 256;
                if (tile.geographic_key != $"epsg26911/e{east}/n{north}/size256" || !addresses.Add(tile.geographic_key) ||
                    tile.data_path != RetainedRoot + "/TerrainData/" + tile.local_id + ".asset" || !dataPaths.Add(tile.data_path))
                    throw new InvalidDataException("Retained ownership mismatch.");
                CheckDimensions(tile, east - 522448, north - 4008248);
                if (verifySources) VerifySource(tile.render_file, tile.render_sha256, 257 * 257 * 4);
            }
            if (addresses.Count != 1024) throw new InvalidDataException("Missing geographic keys.");
            return m;
        }

        private static void ParseAddress(string id, out int row, out int col)
        {
            if (id == null || id.Length != 7 || id[0] != 'r' || id[3] != '_' || id[4] != 'c' ||
                !int.TryParse(id.Substring(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out row) ||
                !int.TryParse(id.Substring(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out col) || row > 15 || col > 15 ||
                id != $"r{row:00}_c{col:00}") throw new InvalidDataException("Invalid local address: " + id);
        }

        private static void CheckDimensions(Tile tile, int x, int z)
        {
            if (tile.position == null || tile.size == null || tile.position.Length != 3 || tile.size.Length != 3 ||
                tile.position[0] != x || tile.position[1] != -100 || tile.position[2] != z ||
                tile.size[0] != 256 || tile.size[1] != 1800 || tile.size[2] != 256)
                throw new InvalidDataException("Unexpected native Terrain bounds.");
        }

        private static void VerifySource(string relative, string expectedHash, int? bytes)
        {
            string path = SourceFile(relative);
            if (Hash(path) != expectedHash || (bytes.HasValue && new FileInfo(path).Length != bytes.Value))
                throw new InvalidDataException("Source hash/length changed: " + relative);
        }

        public static float[,] ReadNormalized(Tile tile)
        {
            byte[] bytes = File.ReadAllBytes(SourceFile(tile.render_file));
            if (!BitConverter.IsLittleEndian || bytes.Length != 257 * 257 * 4 || Hash(SourceFile(tile.render_file)) != tile.render_sha256)
                throw new InvalidDataException("Invalid float32 render source.");
            var northFirst = new float[257 * 257];
            Buffer.BlockCopy(bytes, 0, northFirst, 0, bytes.Length);
            var result = new float[257, 257];
            for (int north = 0; north < 257; north++)
            for (int x = 0; x < 257; x++)
            {
                float v = northFirst[north * 257 + x];
                if (float.IsNaN(v) || float.IsInfinity(v) || v < 0f || v > 1f) throw new InvalidDataException("Clipped/invalid normalized source.");
                result[256 - north, x] = v;
            }
            return result;
        }

        public static void VerifyProtectedFiles(Manifest m)
        {
            if (m.protected_files == null || m.protected_files.Length != 2066) throw new InvalidDataException("Incomplete protection receipt.");
            foreach (var file in m.protected_files)
                if (Hash(ProjectFile(file.path)) != file.sha256) throw new InvalidDataException("Protected file changed: " + file.path);
        }

        public static List<Terrain> Terrains(Scene scene)
        {
            var result = new List<Terrain>();
            foreach (var root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<Terrain>(true));
            return result;
        }

        public static bool HasExpansion(Scene scene)
        {
            foreach (Terrain terrain in Terrains(scene))
                if (AssetDatabase.GetAssetPath(terrain.terrainData).StartsWith(AssetRoot + "/TerrainData/", StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
