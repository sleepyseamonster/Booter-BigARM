using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tile = BooterBigArm.Editor.BadwaterTerrainExpansionSource.Tile;

namespace BooterBigArm.Editor
{
    /// <summary>Five sealed eastward steps; each preserves its complete immediately preceding terrain footprint.</summary>
    public static class BadwaterNorthRowSource
    {
        public const string ProductionScene = BadwaterTerrainExpansionSource.ProductionScene;
        [Serializable] public sealed class Manifest
        {
            public int schema_version, row_step, tile_count, new_tile_count;
            public string working_crs, asset_root, scene_guid, prepared_manifest_sha256, predecessor_scene_sha256, predecessor_manifest_sha256;
            public int[] bounds_m, unity_origin_m;
            public float minimum_m, range_m;
            public Tile[] new_tiles, retained_tiles;
            public BadwaterTerrainExpansionSource.ProtectedFile[] protected_files;
        }
        public sealed class Config
        {
            public readonly int Step, East, RetainedCount, TotalCount, EdgeCount, ColliderSamples, NorthernColumns;
            public readonly string Batch, Section, AssetRoot, SourceRoot, ManifestPath, CandidateScene, PredecessorManifestPath;
            internal Config(int step)
            {
                Step=step; East=499920+4096*step; RetainedCount=3328+256*(step-1); TotalCount=RetainedCount+256;
                EdgeCount=6512+512*step; ColliderSamples=TotalCount*25; NorthernColumns=16*(step+1);
                Batch=$"NorthRowE{East}2026-10-09"; Section=$"north_row_e{East}";
                AssetRoot="Assets/_Project/Art/Terrain/"+Batch; SourceRoot=AssetRoot+"/Source"; ManifestPath=SourceRoot+"/manifest.json";
                CandidateScene=$"Assets/_Project/Scenes/Reference/TerrainNorthRowE{East}Candidate.unity";
                PredecessorManifestPath=step==1 ? BadwaterNorthRidgeSource.ManifestPath : $"Assets/_Project/Art/Terrain/NorthRowE{East-4096}2026-10-09/Source/manifest.json";
            }
        }
        public static Config ForStep(int step, string batch = null)
        {
            if (step<1 || step>5) throw new InvalidDataException("Only the five ordered northern-row steps are supported.");
            var config=new Config(step);
            if (batch!=null && batch!=config.Batch) throw new InvalidDataException("Batch does not match its sealed row step.");
            return config;
        }
        public static Config Selected
        {
            get
            {
                var args=Environment.GetCommandLineArgs(); string batch=null; int step=0;
                for(int i=0;i<args.Length-1;i++)
                {
                    if(args[i]=="-northRowStep" && !int.TryParse(args[i+1],out step)) throw new InvalidDataException("Invalid row step.");
                    if(args[i]=="-northRowBatch") batch=args[i+1];
                }
                return ForStep(step,batch);
            }
        }
        public static Config ConfigForScene(Scene scene)
        {
            var terrains=Terrains(scene);
            for(int step=5;step>=1;step--)
            {
                var config=ForStep(step);
                foreach(var terrain in terrains)
                    if(AssetDatabase.GetAssetPath(terrain.terrainData).StartsWith(config.AssetRoot+"/TerrainData/",StringComparison.Ordinal)) return config;
            }
            throw new InvalidDataException("Scene has no northern-row terrain owner.");
        }
        public static string ProjectFile(string relative) => BadwaterTerrainExpansionSource.ProjectFile(relative);
        public static string Hash(string absolute) => BadwaterTerrainExpansionSource.Hash(absolute);
        public static List<Terrain> Terrains(Scene scene) => BadwaterTerrainExpansionSource.Terrains(scene);

        public static string SourceFile(Config config, string relative)
        {
            if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("Expected an owned relative source path.");
            string root = ProjectFile(config.SourceRoot), path = ProjectFile(config.SourceRoot + "/" + relative);
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Source path escaped its owner.");
            return path;
        }

        public static Manifest ReadManifest(Config config, bool verifySources = false)
        {
            string json = File.ReadAllText(ProjectFile(config.ManifestPath));
            var m = JsonUtility.FromJson<Manifest>(json);
            return ValidateManifest(config, m, verifySources);
        }

        public static Manifest ValidateManifest(Config config, Manifest m, bool verifySources = false)
        {
            if (m == null || m.schema_version != 1 || m.row_step != config.Step || m.working_crs != "EPSG:26911" || m.asset_root != config.AssetRoot ||
                m.tile_count != config.TotalCount || m.new_tile_count != 256 || m.minimum_m != -100 || m.range_m != 1800 ||
                m.scene_guid != "4de5dd018ee194314a00fd369e2d3eeb" || m.unity_origin_m == null || m.unity_origin_m.Length != 2 ||
                m.unity_origin_m[0] != 522448 || m.unity_origin_m[1] != 4008248 || m.bounds_m == null || m.bounds_m.Length != 4 ||
                m.bounds_m[0] != 499920 || m.bounds_m[1] != 4006200 || m.bounds_m[2] != 524496 || m.bounds_m[3] != 4018488 ||
                m.new_tiles == null || m.new_tiles.Length != 256 || m.retained_tiles == null || m.retained_tiles.Length != config.RetainedCount ||
                string.IsNullOrEmpty(m.predecessor_scene_sha256) || m.predecessor_scene_sha256.Length!=64 ||
                string.IsNullOrEmpty(m.predecessor_manifest_sha256) || m.predecessor_manifest_sha256.Length!=64)
                throw new InvalidDataException("Unsupported ordered-row footprint, encoding or predecessor identity.");
            if(verifySources && (Hash(ProjectFile(config.PredecessorManifestPath))!=m.predecessor_manifest_sha256 ||
                Hash(SourceFile(config,"Baseline/production_before.unity"))!=m.predecessor_scene_sha256))
                throw new InvalidDataException("Accepted predecessor manifest or sealed scene proof changed.");
            var keys = new HashSet<string>();
            var paths = new HashSet<string>();
            int north = 0;
            foreach (Tile t in m.new_tiles)
            {
                if (t == null) throw new InvalidDataException("Missing new tile record.");
                int r, c;
                if (t.local_id == null || t.local_id.Length != 7 || !int.TryParse(t.local_id.Substring(1, 2), out r) ||
                    !int.TryParse(t.local_id.Substring(5, 2), out c) || r < 0 || r > 15 || c < 0 || c > 15 || t.local_id != $"r{r:00}_c{c:00}")
                    throw new InvalidDataException("Invalid local address.");
                if (t.section != config.Section) throw new InvalidDataException("Unexpected section.");
                int ymax = 4018488;
                north++;
                int e = config.East + c * 256, n = ymax - (r + 1) * 256;
                CheckTile(t, e, n, keys, paths);
                if (t.bounds_m == null || t.bounds_m.Length != 4 || t.bounds_m[0] != e || t.bounds_m[1] != n || t.bounds_m[2] != e+256 || t.bounds_m[3] != n+256 ||
                    t.data_path != config.AssetRoot + "/TerrainData/" + t.section + "/" + t.local_id + ".asset" ||
                    t.layer_path != config.AssetRoot + "/TerrainLayers/" + t.section + "/" + t.local_id + ".terrainlayer" ||
                    t.source_spacing_m != 2 || t.source_samples != 129 || t.render_samples != 257 || string.IsNullOrEmpty(t.source_version))
                    throw new InvalidDataException("New tile ownership or sampling changed.");
                if (verifySources) { Verify(config,t.render_file, t.render_sha256, 257*257*4); Verify(config,t.height_file, t.height_sha256, 129*129*2); Verify(config,t.color_file, t.color_sha256, null); }
            }
            if (north != 256) throw new InvalidDataException("Incomplete northern section.");
            foreach (Tile t in m.retained_tiles)
            {
                if (t == null || t.position == null || t.position.Length != 3) throw new InvalidDataException("Missing retained placement.");
                int e = Mathf.RoundToInt(t.position[0])+522448, n = Mathf.RoundToInt(t.position[2])+4008248;
                if (!IsOccupied(config.Step-1,e,n) || (e-499920)%256 != 0 || (n-4006200)%256 != 0 || !string.IsNullOrEmpty(t.section))
                    throw new InvalidDataException("Retained footprint changed.");
                CheckTile(t, e, n, keys, paths);
                if (t.bounds_m == null || t.bounds_m.Length != 4 || t.bounds_m[0] != e || t.bounds_m[1] != n || t.bounds_m[2] != e+256 || t.bounds_m[3] != n+256)
                    throw new InvalidDataException("Retained bounds disagree with placement.");
                int sectionEast = e >= 520400 ? 520400 : e >= 516304 ? 516304 : e >= 512208 ? 512208 : e >= 508112 ? 508112 : e >= 504016 ? 504016 : 499920;
                int sectionNorth = n >= 4014392 ? 4018488 : n >= 4010296 ? 4014392 : 4010296;
                string local = $"r{(sectionNorth-n)/256-1:00}_c{(e-sectionEast)/256:00}";
                string expectedPath = n >= 4014392
                    ? (e<504016 ? BadwaterNorthRidgeSource.AssetRoot+"/TerrainData/north_ridge/" : ForStep((e-499920)/4096).AssetRoot+"/TerrainData/north_row_e"+sectionEast+"/")+local+".asset"
                    : e >= 520400 && n < 4010296
                    ? BadwaterTerrainExpansionSource.RetainedRoot + "/TerrainData/" + local + ".asset"
                    : e >= 516304
                    ? BadwaterTerrainExpansionSource.AssetRoot + "/TerrainData/" + (e >= 520400 ? "north" : n >= 4010296 ? "northwest" : "west") + "/" + local + ".asset"
                    : e >= 512208
                    ? BadwaterWestPairSource.AssetRoot + "/TerrainData/" + (n >= 4010296 ? "northwest_outer" : "west_outer") + "/" + local + ".asset"
                    : e >= 508112
                    ? BadwaterNextWestPairSource.AssetRoot + "/TerrainData/" + (n >= 4010296 ? "northwest_next" : "west_next") + "/" + local + ".asset"
                    : e >= 504016
                    ? BadwaterFarWestPairSource.AssetRoot + "/TerrainData/" + (n >= 4010296 ? "northwest_far" : "west_far") + "/" + local + ".asset"
                    : BadwaterWestRidgePairSource.AssetRoot + "/TerrainData/" + (n >= 4010296 ? "northwest_ridge" : "west_ridge") + "/" + local + ".asset";
                if (t.local_id != local || t.data_path != expectedPath ||
                    string.IsNullOrEmpty(t.data_guid) || string.IsNullOrEmpty(t.data_sha256) || string.IsNullOrEmpty(t.meta_sha256))
                    throw new InvalidDataException("Retained asset ownership missing.");
                if (verifySources) Verify(config,t.render_file, t.render_sha256, 257*257*4);
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

        public static bool IsOccupied(int acceptedSteps, int east, int north)
        {
            if(acceptedSteps<0 || acceptedSteps>5) throw new InvalidDataException("Unsupported occupied row scope.");
            return east>=499920 && east<524496 && north>=4006200 && north<4014392 ||
                east>=499920 && east<499920+4096*(acceptedSteps+1) && north>=4014392 && north<4018488;
        }

        private static void Verify(Config config, string file, string hash, int? length)
        {
            string path = SourceFile(config,file);
            if (Hash(path) != hash || (length.HasValue && new FileInfo(path).Length != length.Value)) throw new InvalidDataException("Changed source payload.");
        }

        public static float[,] ReadNormalized(Config config, Tile tile)
        {
            Verify(config,tile.render_file, tile.render_sha256, 257*257*4);
            if (!BitConverter.IsLittleEndian) throw new InvalidDataException("Little-endian source required.");
            var bytes = File.ReadAllBytes(SourceFile(config,tile.render_file));
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

        public static void VerifyProtectedFiles(Config config, Manifest m)
        {
            var required = new HashSet<string>(StringComparer.Ordinal);
            foreach (string root in new[] { BadwaterTerrainExpansionSource.RetainedRoot, BadwaterTerrainExpansionSource.AssetRoot, BadwaterWestPairSource.AssetRoot, BadwaterNextWestPairSource.AssetRoot, BadwaterFarWestPairSource.AssetRoot, BadwaterWestRidgePairSource.AssetRoot })
                foreach (string path in Directory.GetFiles(ProjectFile(root), "*", SearchOption.AllDirectories))
                    required.Add(path.Substring(Path.GetDirectoryName(Application.dataPath).Length + 1).Replace('\\', '/'));
            foreach (string path in new[] { "Packages/manifest.json", "Packages/packages-lock.json", "ProjectSettings/ProjectVersion.txt", "ProjectSettings/EditorBuildSettings.asset" })
                required.Add(path);
            foreach(string root in PriorRowRoots(config))
                foreach(string path in Directory.GetFiles(ProjectFile(root),"*",SearchOption.AllDirectories))
                    required.Add(path.Substring(Path.GetDirectoryName(Application.dataPath).Length+1).Replace('\\','/'));
            if (m.protected_files == null || m.protected_files.Length != required.Count) throw new InvalidDataException("Incomplete protection receipt.");
            foreach (var file in m.protected_files)
                if (file == null || !required.Remove(file.path) || !HashMatches(file.path, file.sha256))
                    throw new InvalidDataException("Retained file missing, duplicated or changed: " + file?.path);
            if (required.Count != 0) throw new InvalidDataException("Protection receipt omitted retained files.");
        }

        public static bool HashMatches(string path, string expected)
        {
            return Hash(ProjectFile(path)) == expected;
        }

        public static bool HasExpansion(Scene scene)
        {
            foreach (Terrain t in Terrains(scene)) for(int step=1;step<=5;step++)
                if (AssetDatabase.GetAssetPath(t.terrainData).StartsWith(ForStep(step).AssetRoot+"/TerrainData/", StringComparison.Ordinal)) return true;
            return false;
        }
        private static IEnumerable<string> PriorRowRoots(Config config)
        {
            yield return BadwaterNorthRidgeSource.AssetRoot;
            for(int step=1;step<config.Step;step++) yield return ForStep(step).AssetRoot;
        }
    }
}
