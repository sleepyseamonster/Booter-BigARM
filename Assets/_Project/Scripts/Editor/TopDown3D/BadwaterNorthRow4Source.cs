using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Distinct six-step row contract; older manifests and assertions stay sealed.</summary>
    public static class BadwaterNorthRow4Source
    {
        public const string ProductionScene = BadwaterTerrainExpansionSource.ProductionScene;
        public const string SharedOwner = "Assets/_Project/Art/Terrain/NorthRow2Baseline2026-10-09";
        [Serializable] public sealed class NativeReference { public string path, sha256, orientation; public int samples; }
        [Serializable] public sealed class ColorCorner { public bool west_anchor_present; public int[] southwest_difference_rgb; public int west_exception_pixels, feather_pixels, feather_m; public string priority; }
        [Serializable] public sealed class Tile
        {
            public string section, local_id, geographic_key, source_version, data_path, data_guid, data_sha256, meta_sha256;
            public string render_file, render_sha256, height_file, height_sha256, color_file, color_sha256, layer_path;
            public int[] bounds_m;
            public float[] position, size;
            public int source_spacing_m, source_samples, render_samples;
            public NativeReference native_reference;
        }
        [Serializable] public sealed class Manifest
        {
            public int schema_version, row_step, tile_count, new_tile_count, normalized_shared_edges, exterior_slots;
            public string family, execution_date, batch, section, asset_root, working_crs, scene_guid, shared_snapshot_owner, status;
            public string predecessor_scene_sha256, predecessor_manifest, predecessor_manifest_sha256, prepared_manifest_sha256, authoring_checkpoint_sha256;
            public string blender_source_sha256, pilot_blend_sha256, combined_blend_sha256;
            public ColorCorner color_corner;
            public float minimum_m, range_m;
            public int[] bounds_m, unity_origin_m;
            public string[] protected_roots;
            public Tile[] retained_tiles, new_tiles;
            public BadwaterTerrainExpansionSource.ProtectedFile[] protected_files;
        }
        public sealed class Config
        {
            public readonly int Step, East, RetainedCount, TotalCount, EdgeCount;
            public readonly string Date, Batch, Section, AssetRoot, SourceRoot, ManifestPath, CandidateScene;
            public Config(int step, string date)
            {
                if (step < 1 || step > 6 || !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    throw new InvalidDataException("Ordered steps 1..6 and an explicit ISO execution date are required.");
                Step = step; Date = date; East = 499920 + 4096 * (step - 1);
                RetainedCount = 7680 + 256 * (step - 1); TotalCount = RetainedCount + 256; EdgeCount = 15680 + 512 * (step - 1);
                Batch = $"NorthRow4E{East}N4026680_{date}"; Section = $"north_row4_e{East}_n4026680";
                AssetRoot = "Assets/_Project/Art/Terrain/" + Batch; SourceRoot = AssetRoot + "/Source"; ManifestPath = SourceRoot + "/manifest.json";
                CandidateScene = "Assets/_Project/Scenes/Reference/" + Batch + "Candidate.unity";
            }
        }
        public static Config Selected
        {
            get
            {
                string step = Argument("-northRow4Step"), date = Argument("-northRow4Date");
                if (!int.TryParse(step, out int value)) throw new InvalidDataException("Missing fourth-row step.");
                return new Config(value, date);
            }
        }
        public static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
        public static string ProjectFile(string path) => BadwaterTerrainExpansionSource.ProjectFile(path);
        public static string Hash(string path) => BadwaterNorthRow4FileHash.Compute(ProjectFile(path));
        public static List<Terrain> Terrains(Scene scene) => BadwaterTerrainExpansionSource.Terrains(scene);
        public static bool HasExpansion(Scene scene) => Terrains(scene).Any(t => AssetDatabase.GetAssetPath(t.terrainData).StartsWith("Assets/_Project/Art/Terrain/NorthRow4E", StringComparison.Ordinal));
        public static Config ConfigForScene(Scene scene)
        {
            Config newest = null;
            var visitedOwners = new HashSet<string>(StringComparer.Ordinal);
            foreach (Terrain tile in Terrains(scene))
            {
                string path = AssetDatabase.GetAssetPath(tile.terrainData);
                if (!path.StartsWith("Assets/_Project/Art/Terrain/NorthRow4E", StringComparison.Ordinal)) continue;
                string root = path.Substring(0, path.IndexOf("/TerrainData/", StringComparison.Ordinal));
                if (!visitedOwners.Add(root)) continue;
                var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(ProjectFile(root + "/Source/manifest.json")));
                var candidate = new Config(m.row_step, m.execution_date);
                if (root != candidate.AssetRoot) throw new InvalidDataException("Unexpected row owner.");
                if (newest == null || candidate.Step > newest.Step) newest = candidate;
            }
            return newest ?? throw new InvalidDataException("No accepted fourth-row terrain in scene.");
        }
        public static HashSet<Vector2Int> Occupied(int acceptedSteps)
        {
            if (acceptedSteps < 0 || acceptedSteps > 6) throw new InvalidDataException("Invalid accepted row scope.");
            var cells = new HashSet<Vector2Int>();
            for (int n = 4006200; n < 4026680; n += 256) for (int e = 499920; e < 524496; e += 256) cells.Add(new Vector2Int(e, n));
            for (int n = 4026680; n < 4030776; n += 256) for (int e = 499920; e < 499920 + acceptedSteps * 4096; e += 256) cells.Add(new Vector2Int(e, n));
            return cells;
        }
        public static Manifest ReadManifest(Config c, bool sources = false)
        {
            var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(ProjectFile(c.ManifestPath)));
            ValidateManifest(c, m, sources);
            return m;
        }
        public static void ValidateManifest(Config c, Manifest m, bool sources)
        {
            if (m == null || m.schema_version != 2 || m.family != "north_row4" || m.row_step != c.Step || m.execution_date != c.Date || m.batch != c.Batch || m.section != c.Section || m.asset_root != c.AssetRoot || m.working_crs != "EPSG:26911" || m.scene_guid != "4de5dd018ee194314a00fd369e2d3eeb" || m.shared_snapshot_owner != SharedOwner || m.minimum_m != -100 || m.range_m != 1800 || m.tile_count != c.TotalCount || m.new_tile_count != 256 || m.normalized_shared_edges != c.EdgeCount || m.exterior_slots != 384 || m.new_tiles?.Length != 256 || m.retained_tiles?.Length != c.RetainedCount || m.bounds_m == null || !m.bounds_m.SequenceEqual(new[] {499920,4006200,524496,4030776}) || m.unity_origin_m == null || !m.unity_origin_m.SequenceEqual(new[] {522448,4008248}))
                throw new InvalidDataException("Unsupported fourth-row footprint, encoding or identity.");
            if (m.predecessor_scene_sha256?.Length != 64 || m.predecessor_manifest_sha256?.Length != 64 || m.prepared_manifest_sha256?.Length != 64 || m.authoring_checkpoint_sha256?.Length != 64 || m.blender_source_sha256?.Length != 64 || m.pilot_blend_sha256?.Length != 64 || m.combined_blend_sha256?.Length != 64)
                throw new InvalidDataException("Complete native/source/predecessor lineage required.");
            string predecessor = "Assets/_Project/Art/Terrain/NorthRow3E520400N4022584_2026-10-10/Source/manifest.json";
            if (c.Step == 1 && m.predecessor_manifest != predecessor) throw new InvalidDataException("First fourth-row step requires the complete sealed older row.");
            if (c.Step == 1)
            {
                var previous = JsonUtility.FromJson<BadwaterNorthRow3Source.Manifest>(File.ReadAllText(ProjectFile(predecessor)));
                if (previous?.family != "north_row3" || previous.row_step != 6 || previous.tile_count != c.RetainedCount)
                    throw new InvalidDataException("Third-row baseline requires the complete accepted third row.");
            }
            if (c.Step > 1)
            {
                var previous = JsonUtility.FromJson<Manifest>(File.ReadAllText(ProjectFile(m.predecessor_manifest)));
                if (previous?.family != "north_row4" || previous.row_step != c.Step-1 || previous.tile_count != c.RetainedCount || m.predecessor_manifest != new Config(c.Step-1,previous.execution_date).ManifestPath)
                    throw new InvalidDataException("Missing immediately preceding accepted row step.");
            }
            var corner=m.color_corner;
            if(corner==null || corner.feather_pixels!=128 || corner.feather_m!=256 || (c.Step==1 && (corner.west_anchor_present || (corner.southwest_difference_rgb!=null && corner.southwest_difference_rgb.Length!=0) || corner.west_exception_pixels!=0)) ||
                (c.Step>1 && (!corner.west_anchor_present || corner.southwest_difference_rgb?.Length!=3 || corner.southwest_difference_rgb.Any(v=>v<0 || v>255) || corner.west_exception_pixels!=(corner.southwest_difference_rgb.Any(v=>v!=0)?1:0))))
                throw new InvalidDataException("Invalid freshly audited single-pixel corner policy.");
            var expected = Occupied(c.Step - 1); var paths = new HashSet<string>(); var keys = new HashSet<string>();
            foreach (Tile tile in m.retained_tiles)
            {
                Vector2Int cell = CheckTile(tile, keys, paths);
                if (!expected.Remove(cell) || string.IsNullOrEmpty(tile.data_guid) || string.IsNullOrEmpty(tile.data_sha256) || string.IsNullOrEmpty(tile.meta_sha256) || tile.native_reference == null)
                    throw new InvalidDataException("Missing, duplicate or changed retained identity.");
                ValidateReference(m, tile.native_reference);
                string dataOwner=tile.data_path.Contains("/TerrainData/") ? tile.data_path.Substring(0,tile.data_path.IndexOf("/TerrainData/",StringComparison.Ordinal)) : null;
                if(dataOwner==null || m.protected_roots==null || !m.protected_roots.Contains(dataOwner)) throw new InvalidDataException("Retained TerrainData escaped its protected owner.");
                if (sources) ReadReference(m, tile.native_reference);
            }
            if (expected.Count != 0) throw new InvalidDataException("Missing predecessor cells.");
            var newer = Occupied(c.Step); newer.ExceptWith(Occupied(c.Step - 1));
            foreach (Tile tile in m.new_tiles)
            {
                Vector2Int cell = CheckTile(tile, keys, paths);
                int row = (4030776 - cell.y) / 256 - 1, col = (cell.x - c.East) / 256;
                string id = $"r{row:00}_c{col:00}";
                if (!newer.Remove(cell) || tile.section != c.Section || tile.local_id != id || tile.data_path != c.AssetRoot + "/TerrainData/" + c.Section + "/" + id + ".asset" || tile.layer_path != c.AssetRoot + "/TerrainLayers/" + c.Section + "/" + id + ".terrainlayer" || tile.source_spacing_m != 2 || tile.source_samples != 129 || tile.render_samples != 257 || string.IsNullOrEmpty(tile.source_version))
                    throw new InvalidDataException("Changed new tile ownership or sampling.");
                if (sources)
                {
                    VerifySource(c, tile.render_file, tile.render_sha256, 257 * 257 * 4);
                    VerifySource(c, tile.height_file, tile.height_sha256, 129 * 129 * 2);
                    VerifySource(c, tile.color_file, tile.color_sha256, null);
                }
            }
            if (newer.Count != 0) throw new InvalidDataException("Missing new cells.");
            var declaredRoots=new HashSet<string>(m.protected_roots ?? Array.Empty<string>(),StringComparer.Ordinal);
            var actualRoots=new HashSet<string>(m.retained_tiles.Select(t=>t.data_path.Substring(0,t.data_path.IndexOf("/TerrainData/",StringComparison.Ordinal))),StringComparer.Ordinal);
            if(declaredRoots.Count!=24+c.Step-1 || !declaredRoots.SetEquals(actualRoots)) throw new InvalidDataException("Retained root inventory differs from exact tile ownership.");
            if (sources && Hash(m.predecessor_manifest) != m.predecessor_manifest_sha256) throw new InvalidDataException("Predecessor manifest changed.");
        }
        private static Vector2Int CheckTile(Tile t, HashSet<string> keys, HashSet<string> paths)
        {
            if (t?.bounds_m == null || t.bounds_m.Length != 4 || t.position == null || t.position.Length != 3 || t.size == null || !t.size.SequenceEqual(new float[] {256,1800,256})) throw new InvalidDataException("Incomplete tile identity.");
            int e = t.bounds_m[0], n = t.bounds_m[1];
            if (t.bounds_m[2] != e + 256 || t.bounds_m[3] != n + 256 || t.geographic_key != $"epsg26911/e{e}/n{n}/size256" || !keys.Add(t.geographic_key) || !paths.Add(t.data_path) || t.position[0] != e - 522448 || t.position[1] != -100 || t.position[2] != n - 4008248)
                throw new InvalidDataException("Shifted/duplicate geographic identity.");
            ProjectFile(t.data_path);
            return new Vector2Int(e, n);
        }
        public static string SourceFile(Config c, string relative)
        {
            if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative) || relative.Contains("..") || relative.Contains("\\")) throw new InvalidDataException("Owned source-relative path required.");
            return ProjectFile(c.SourceRoot + "/" + relative);
        }
        private static void VerifySource(Config c, string relative, string expected, int? length)
        {
            string file = SourceFile(c, relative);
            if (BadwaterNorthRow4FileHash.Compute(file) != expected || (length.HasValue && new FileInfo(file).Length != length.Value)) throw new InvalidDataException("New source changed: " + relative);
        }
        public static void ValidateReference(Manifest m, NativeReference r)
        {
            if (r == null || r.samples != 257 || (r.orientation != "north-first" && r.orientation != "south-first") || r.sha256?.Length != 64 || string.IsNullOrEmpty(r.path) || r.path.Contains("..") || r.path.Contains("\\")) throw new InvalidDataException("Invalid native snapshot reference.");
            ProjectFile(r.path);
            if (m.protected_roots == null || !m.protected_roots.Concat(new[] {m.shared_snapshot_owner, m.asset_root}).Any(owner => r.path.StartsWith(owner + "/Source/", StringComparison.Ordinal))) throw new InvalidDataException("Snapshot escaped protected owners.");
        }
        public static float[,] ReadReference(Manifest m, NativeReference r)
        {
            ValidateReference(m, r);
            if (Hash(r.path) != r.sha256) throw new InvalidDataException("Native snapshot hash changed.");
            return Decode(File.ReadAllBytes(ProjectFile(r.path)), r.orientation == "north-first");
        }
        public static float[,] ReadRender(Config c, Tile t) => Decode(File.ReadAllBytes(SourceFile(c, t.render_file)), true);
        private static float[,] Decode(byte[] bytes, bool reverse)
        {
            if (!BitConverter.IsLittleEndian || bytes.Length != 257 * 257 * 4) throw new InvalidDataException("Little-endian 257-square normalized grid required.");
            var values = new float[257 * 257]; Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length); var result = new float[257,257];
            for (int r = 0; r < 257; r++) for (int x = 0; x < 257; x++)
            {
                float v = values[r * 257 + x];
                if (float.IsNaN(v) || float.IsInfinity(v) || v < 0 || v > 1) throw new InvalidDataException("Invalid normalized native sample.");
                result[reverse ? 256-r : r, x] = v;
            }
            return result;
        }
        public static void VerifyProtectedFiles(Manifest m)
        {
            if (m.protected_roots == null || m.protected_roots.Distinct().Count() != m.protected_roots.Length || m.protected_roots.Length != 24 + m.row_step - 1) throw new InvalidDataException("Incomplete retained terrain owner inventory.");
            var required = new HashSet<string>(StringComparer.Ordinal);
            foreach (string owner in m.protected_roots)
            {
                string dir = ProjectFile(owner);
                if (!Directory.Exists(dir)) throw new InvalidDataException("Missing protected owner.");
                foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) required.Add(file.Substring(Path.GetDirectoryName(Application.dataPath).Length + 1).Replace('\\','/'));
            }
            foreach (string path in new[] {"Packages/manifest.json", "Packages/packages-lock.json", "ProjectSettings/ProjectVersion.txt", "ProjectSettings/EditorBuildSettings.asset", ProductionScene + ".meta"}) required.Add(path);
            foreach (string file in Directory.GetFiles(ProjectFile(SharedOwner), "*", SearchOption.AllDirectories)) required.Add(file.Substring(Path.GetDirectoryName(Application.dataPath).Length + 1).Replace('\\','/'));
            if (m.protected_files == null || m.protected_files.Length != required.Count) throw new InvalidDataException("Incomplete protection receipt.");
            foreach (var f in m.protected_files)
                if (f == null || !required.Remove(f.path) || Hash(f.path) != f.sha256) throw new InvalidDataException("Retained protected bytes changed: " + f?.path);
            if (required.Count != 0) throw new InvalidDataException("Missing retained protection entries.");
        }
    }
}
