using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class BadwaterNorthRow3Validator
    {
        [Serializable] public sealed class Report
        {
            public int schema_version = 2, row_step, terrains, retained, new_terrains, edges, outer_edges, collider_samples, sectors;
            public string family = "north_row3", scene, scene_sha256, manifest_sha256, status;
            public float max_new_readback_error_m, max_collider_error_m;
        }
        public static void ValidateFromCli()
        {
            var c = BadwaterNorthRow3Source.Selected;
            string path = BadwaterNorthRow3Source.Argument("-terrainExpansionScene") ?? c.CandidateScene;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var r = ValidateScene(scene, c, true);
            string output = BadwaterNorthRow3Source.Argument("-terrainExpansionReport");
            if (string.IsNullOrEmpty(output) || File.Exists(output)) throw new IOException("Fresh native report path required.");
            File.WriteAllText(output, JsonUtility.ToJson(r, true) + "\n");
        }
        public static Report ValidateScene(Scene scene, BadwaterNorthRow3Source.Config c, bool physics)
        {
            if (scene.path != BadwaterNorthRow3Source.ProductionScene && scene.path != c.CandidateScene) throw new InvalidDataException("Unexpected native scene owner.");
            var m = BadwaterNorthRow3Source.ReadManifest(c, true);
            BadwaterNorthRow3Source.VerifyProtectedFiles(m);
            var terrains = BadwaterNorthRow3Source.Terrains(scene);
            if (terrains.Count != c.TotalCount) throw new InvalidDataException("Incorrect accepted terrain count.");
            var records = m.retained_tiles.Concat(m.new_tiles).ToDictionary(t => t.data_path);
            var newPaths = new HashSet<string>(m.new_tiles.Select(t => t.data_path));
            var grid = new Dictionary<Vector2Int, Terrain>(); var heights = new Dictionary<Vector2Int, float[,]>(); var dataPaths = new HashSet<string>();
            var report = new Report { row_step = c.Step, terrains = terrains.Count, retained = c.RetainedCount, new_terrains = 256, scene = scene.path, scene_sha256 = BadwaterNorthRow3Source.Hash(scene.path), manifest_sha256 = BadwaterNorthRow3Source.Hash(c.ManifestPath) };
            BadwaterTerrainConnectivity owner = null;
            if (physics) Physics.SyncTransforms();
            foreach (Terrain terrain in terrains)
            {
                string path = AssetDatabase.GetAssetPath(terrain.terrainData);
                if (!records.TryGetValue(path, out var tile) || !dataPaths.Add(path)) throw new InvalidDataException("Unknown/duplicate native TerrainData.");
                var d = terrain.terrainData;
                bool newer = newPaths.Contains(path);
                if (!terrain.enabled || !terrain.gameObject.activeInHierarchy || terrain.transform.position != new Vector3(tile.position[0], tile.position[1], tile.position[2]) || terrain.transform.rotation != Quaternion.identity || terrain.transform.lossyScale != Vector3.one || d.size != new Vector3(256,1800,256) || d.heightmapResolution != 257 || terrain.GetComponent<TerrainCollider>()?.terrainData != d || !terrain.GetComponent<TerrainCollider>().enabled || terrain.GetComponent<TopDown3DGroundSurface>() == null || !terrain.allowAutoConnect || terrain.groupingID != 26911 || terrain.materialTemplate == null || terrain.materialTemplate.shader.name != "Universal Render Pipeline/Terrain/Lit" || d.terrainLayers.Length != 1 || d.terrainLayers[0].diffuseTexture == null)
                    throw new InvalidDataException("Native terrain contract changed: " + path);
                var connection = terrain.GetComponentInParent<BadwaterTerrainConnectivity>();
                if (connection == null || (owner != null && owner != connection)) throw new InvalidDataException("Competing/missing connectivity owner.");
                owner = connection;
                var cell = new Vector2Int(tile.bounds_m[0], tile.bounds_m[1]);
                if (grid.ContainsKey(cell)) throw new InvalidDataException("Overlapping terrain interiors.");
                grid.Add(cell, terrain);
                var actual = d.GetHeights(0,0,257,257);
                var expected = newer ? BadwaterNorthRow3Source.ReadRender(c, tile) : BadwaterNorthRow3Source.ReadReference(m, tile.native_reference);
                if (!newer && (AssetDatabase.AssetPathToGUID(path) != tile.data_guid || BadwaterNorthRow3Source.Hash(path) != tile.data_sha256 || BadwaterNorthRow3Source.Hash(path + ".meta") != tile.meta_sha256)) throw new InvalidDataException("Retained native GUID/payload changed.");
                for (int z=0; z<257; z++) for (int x=0; x<257; x++)
                {
                    float error = Mathf.Abs(actual[z,x] - expected[z,x]) * 1800;
                    if (!newer && error != 0) throw new InvalidDataException("Retained full native height readback differs.");
                    if (newer) report.max_new_readback_error_m = Mathf.Max(report.max_new_readback_error_m, error);
                }
                if (newer && tile.native_reference != null)
                {
                    var snapshot = BadwaterNorthRow3Source.ReadReference(m, tile.native_reference);
                    if (tile.data_guid != AssetDatabase.AssetPathToGUID(path) || tile.data_sha256 != BadwaterNorthRow3Source.Hash(path) || tile.meta_sha256 != BadwaterNorthRow3Source.Hash(path + ".meta")) throw new InvalidDataException("Imported new native seal changed.");
                    for (int z=0; z<257; z++) for (int x=0; x<257; x++) if (snapshot[z,x] != actual[z,x]) throw new InvalidDataException("New exact native snapshot changed.");
                }
                heights.Add(cell, actual);
                if (physics)
                {
                    var collider = terrain.GetComponent<TerrainCollider>();
                    foreach (int z in new[] {1,64,128,192,255}) foreach (int x in new[] {1,64,128,192,255})
                    {
                        float target = terrain.transform.position.y + d.GetInterpolatedHeight(x/256f, z/256f);
                        var start = new Vector3(terrain.transform.position.x+x, 3000, terrain.transform.position.z+z);
                        if (!collider.Raycast(new Ray(start, Vector3.down), out var hit, 5000)) throw new InvalidDataException("Missing native terrain collision.");
                        report.max_collider_error_m = Mathf.Max(report.max_collider_error_m, Mathf.Abs(hit.point.y-target)); report.collider_samples++;
                    }
                }
            }
            if (!BadwaterNorthRow3Source.Occupied(c.Step).SetEquals(grid.Keys)) throw new InvalidDataException("Occupied footprint has holes, shifts or premature upper-east fill.");
            foreach (var pair in grid)
            {
                grid.TryGetValue(pair.Key + new Vector2Int(-256,0), out var left); grid.TryGetValue(pair.Key + new Vector2Int(256,0), out var right);
                grid.TryGetValue(pair.Key + new Vector2Int(0,256), out var top); grid.TryGetValue(pair.Key + new Vector2Int(0,-256), out var bottom);
                if (pair.Value.leftNeighbor != left || pair.Value.rightNeighbor != right || pair.Value.topNeighbor != top || pair.Value.bottomNeighbor != bottom) throw new InvalidDataException("Reciprocal neighbor mismatch.");
                if (left == null) report.outer_edges++; if (right == null) report.outer_edges++; if (top == null) report.outer_edges++; if (bottom == null) report.outer_edges++;
                var h = heights[pair.Key];
                if (right != null) { var b = heights[pair.Key + new Vector2Int(256,0)]; for (int i=0;i<257;i++) if (h[i,256] != b[i,0]) throw new InvalidDataException("Exact E/W edge changed."); report.edges++; }
                if (top != null) { var b = heights[pair.Key + new Vector2Int(0,256)]; for (int i=0;i<257;i++) if (h[256,i] != b[0,i]) throw new InvalidDataException("Exact N/S edge changed."); report.edges++; }
            }
            if (report.edges != c.EdgeCount || report.outer_edges != 352 || report.max_new_readback_error_m > 1800f/65532f + .00025f || (physics && (report.collider_samples != c.TotalCount*25 || report.max_collider_error_m > 8*1.1920929e-7f*5000))) throw new InvalidDataException("Native topology/readback/collision precision gate failed.");
            if (scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BadwaterTerrainConnectivity>(true)).Count()!=1) throw new InvalidDataException("Global connectivity owner count changed.");
            var sections = owner.transform.Cast<Transform>().Where(t => t.GetComponentsInChildren<Terrain>(true).Length > 0).ToArray();
            if (sections.Length != 24+c.Step || sections.Any(s => s.childCount!=16 || s.GetComponentsInChildren<Terrain>(true).Length!=256 || s.Cast<Transform>().Any(sector=>sector.GetComponentsInChildren<Terrain>(true).Length!=16))) throw new InvalidDataException("Section/sector hierarchy changed.");
            report.sectors = sections.Length*16;
            report.status = physics ? "validated_full_grid_and_collider_samples" : "validated_full_grid_readback_and_neighbors";
            Debug.Log("NORTH_ROW3_VALIDATED: " + JsonUtility.ToJson(report));
            return report;
        }
    }
}
