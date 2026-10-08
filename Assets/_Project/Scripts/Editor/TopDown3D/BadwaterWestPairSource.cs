using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Manifest = BooterBigArm.Editor.BadwaterTerrainExpansionSource.Manifest;
using Tile = BooterBigArm.Editor.BadwaterTerrainExpansionSource.Tile;

namespace BooterBigArm.Editor
{
    /// <summary>Blender-derived west pair; all 1,024 accepted terrains are immutable.</summary>
    public static class BadwaterWestPairSource
    {
        public const string AssetRoot = "Assets/_Project/Art/Terrain/WestPair2026-10-08";
        public const string SourceRoot = AssetRoot + "/Source";
        public const string ManifestPath = SourceRoot + "/manifest.json";
        public const string ProductionScene = BadwaterTerrainExpansionSource.ProductionScene;
        public const string CandidateScene = "Assets/_Project/Scenes/Reference/TerrainWestPairCandidate.unity";
        public static string ProjectFile(string relative) => BadwaterTerrainExpansionSource.ProjectFile(relative);
        public static string Hash(string absolute) => BadwaterTerrainExpansionSource.Hash(absolute);
        public static List<Terrain> Terrains(Scene scene) => BadwaterTerrainExpansionSource.Terrains(scene);

        public static string SourceFile(string relative)
        {
            if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("Expected an owned relative source path.");
            string root = ProjectFile(SourceRoot), path = ProjectFile(SourceRoot + "/" + relative);
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Source path escaped its owner.");
            return path;
        }

        public static Manifest ReadManifest(bool verifySources = false)
        {
            string json = File.ReadAllText(ProjectFile(ManifestPath));
            var m = JsonUtility.FromJson<Manifest>(json);
            if (m == null || m.schema_version != 1 || m.working_crs != "EPSG:26911" || m.asset_root != AssetRoot ||
                m.tile_count != 1536 || m.new_tile_count != 512 || m.minimum_m != -100 || m.range_m != 1800 ||
                m.scene_guid != "4de5dd018ee194314a00fd369e2d3eeb" || m.unity_origin_m == null || m.unity_origin_m.Length != 2 ||
                m.unity_origin_m[0] != 522448 || m.unity_origin_m[1] != 4008248 || m.bounds_m == null || m.bounds_m.Length != 4 ||
                m.bounds_m[0] != 512208 || m.bounds_m[1] != 4006200 || m.bounds_m[2] != 524496 || m.bounds_m[3] != 4014392 ||
                m.new_tiles == null || m.new_tiles.Length != 512 || m.retained_tiles == null || m.retained_tiles.Length != 1024)
                throw new InvalidDataException("Unsupported west-pair footprint, encoding or identity.");
            var keys = new HashSet<string>();
            var paths = new HashSet<string>();
            int south = 0, north = 0;
            foreach (Tile t in m.new_tiles)
            {
                int r, c;
                if (t.local_id == null || t.local_id.Length != 7 || !int.TryParse(t.local_id.Substring(1, 2), out r) ||
                    !int.TryParse(t.local_id.Substring(5, 2), out c) || r < 0 || r > 15 || c < 0 || c > 15 || t.local_id != $"r{r:00}_c{c:00}")
                    throw new InvalidDataException("Invalid local address.");
                int ymax;
                if (t.section == "west_outer") { ymax = 4010296; south++; }
                else if (t.section == "northwest_outer") { ymax = 4014392; north++; }
                else throw new InvalidDataException("Unexpected section.");
                int e = 512208 + c * 256, n = ymax - (r + 1) * 256;
                CheckTile(t, e, n, keys, paths);
                if (t.bounds_m == null || t.bounds_m.Length != 4 || t.bounds_m[0] != e || t.bounds_m[1] != n || t.bounds_m[2] != e+256 || t.bounds_m[3] != n+256 ||
                    t.data_path != AssetRoot + "/TerrainData/" + t.section + "/" + t.local_id + ".asset" ||
                    t.layer_path != AssetRoot + "/TerrainLayers/" + t.section + "/" + t.local_id + ".terrainlayer" ||
                    t.source_spacing_m != 2 || t.source_samples != 129 || t.render_samples != 257 || string.IsNullOrEmpty(t.source_version))
                    throw new InvalidDataException("New tile ownership or sampling changed.");
                if (verifySources) { Verify(t.render_file, t.render_sha256, 257*257*4); Verify(t.height_file, t.height_sha256, 129*129*2); Verify(t.color_file, t.color_sha256, null); }
            }
            if (south != 256 || north != 256) throw new InvalidDataException("Incomplete new sections.");
            foreach (Tile t in m.retained_tiles)
            {
                if (t.position == null || t.position.Length != 3) throw new InvalidDataException("Missing retained placement.");
                int e = Mathf.RoundToInt(t.position[0])+522448, n = Mathf.RoundToInt(t.position[2])+4008248;
                if (e < 516304 || e >= 524496 || n < 4006200 || n >= 4014392 || (e-516304)%256 != 0 || (n-4006200)%256 != 0 || !string.IsNullOrEmpty(t.section))
                    throw new InvalidDataException("Retained footprint changed.");
                CheckTile(t, e, n, keys, paths);
                if (!(t.data_path.StartsWith(BadwaterTerrainExpansionSource.RetainedRoot+"/TerrainData/", StringComparison.Ordinal) ||
                      t.data_path.StartsWith(BadwaterTerrainExpansionSource.AssetRoot+"/TerrainData/", StringComparison.Ordinal)) ||
                    string.IsNullOrEmpty(t.data_guid) || string.IsNullOrEmpty(t.data_sha256) || string.IsNullOrEmpty(t.meta_sha256))
                    throw new InvalidDataException("Retained asset ownership missing.");
                if (verifySources) Verify(t.render_file, t.render_sha256, 257*257*4);
            }
            return m;
        }

        private static void CheckTile(Tile t, int e, int n, HashSet<string> keys, HashSet<string> paths)
        {
            if (t.geographic_key != $"epsg26911/e{e}/n{n}/size256" || !keys.Add(t.geographic_key) || !paths.Add(t.data_path) ||
                t.position == null || t.position.Length != 3 || t.position[0] != e-522448 || t.position[1] != -100 || t.position[2] != n-4008248 ||
                t.size == null || t.size.Length != 3 || t.size[0] != 256 || t.size[1] != 1800 || t.size[2] != 256)
                throw new InvalidDataException("Duplicate, shifted or resized terrain identity.");
            ProjectFile(t.data_path);
        }

        private static void Verify(string file, string hash, int? length)
        {
            string path = SourceFile(file);
            if (Hash(path) != hash || (length.HasValue && new FileInfo(path).Length != length.Value)) throw new InvalidDataException("Changed source payload.");
        }

        public static float[,] ReadNormalized(Tile tile)
        {
            Verify(tile.render_file, tile.render_sha256, 257*257*4);
            if (!BitConverter.IsLittleEndian) throw new InvalidDataException("Little-endian source required.");
            var bytes = File.ReadAllBytes(SourceFile(tile.render_file));
            var values = new float[257*257]; Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            var result = new float[257,257];
            for (int r=0; r<257; r++) for (int c=0; c<257; c++)
            {
                float v = values[r*257+c];
                if (float.IsNaN(v) || float.IsInfinity(v) || v<0 || v>1) throw new InvalidDataException("Invalid normalized height.");
                result[256-r,c]=v;
            }
            return result;
        }

        public static void VerifyProtectedFiles(Manifest m)
        {
            if (m.protected_files == null || m.protected_files.Length != 10290) throw new InvalidDataException("Incomplete protection receipt.");
            foreach (var file in m.protected_files)
                if (!HashMatches(file.path, file.sha256)) throw new InvalidDataException("Retained file changed: "+file.path);
        }

        public static bool HashMatches(string path, string expected)
        {
            return Hash(ProjectFile(path)) == expected;
        }

        public static bool HasExpansion(Scene scene)
        {
            foreach (Terrain t in Terrains(scene))
                if (AssetDatabase.GetAssetPath(t.terrainData).StartsWith(AssetRoot+"/TerrainData/", StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
