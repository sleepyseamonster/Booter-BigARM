using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainPopulationSectorStudyTests
    {
        [Test]
        public void SavedReferenceSectorRetainsAuthoredDataWorldPosesAndBuildExclusion()
        {
            FixedTerrainPopulationSectorBuilder.ValidateFromCli();
            var source = JsonUtility.FromJson<FixedTerrainPopulationSectorBuilder.SourceManifest>(File.ReadAllText(
                Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs/Evidence/Performance/population-sector-native-inputs-2026-10-12.json")));
            var scene = EditorSceneManager.OpenScene(FixedTerrainPopulationSectorBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var tiles = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).ToArray();
                Assert.That(tiles.Length, Is.EqualTo(16));
                foreach (var record in source.tiles)
                {
                    var tile = tiles.Single(t => AssetDatabase.GetAssetPath(t.terrainData) == record.path);
                    Assert.That(tile.transform.position, Is.EqualTo(record.position));
                    Assert.That(tile.enabled && tile.GetComponent<TerrainCollider>().enabled && tile.gameObject.activeInHierarchy, Is.True);
                    Assert.That(AssetDatabase.AssetPathToGUID(record.path), Is.EqualTo(record.guid));
                    Assert.That(Hash(record.path), Is.EqualTo(record.sha256));
                    Assert.That(tile.heightmapPixelError, Is.EqualTo(record.pixelError));
                    Assert.That(tile.drawInstanced, Is.EqualTo(record.instanced));
                    Assert.That((int)tile.shadowCastingMode, Is.EqualTo(record.shadowCastingMode));
                }
                Assert.That(EditorBuildSettings.scenes.Any(s => s.path == FixedTerrainPopulationSectorBuilder.ScenePath && s.enabled), Is.False);
                Assert.That(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Any(m => m == null), Is.False);
                Assert.That(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GreaterWastelandInventorySaveService>(true)), Is.Empty);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void AuthoredSectorDemandRemovalRestoreAndDisablePreserveNativeTerrain()
        {
            var scene = EditorSceneManager.OpenScene(FixedTerrainPopulationSectorBuilder.ScenePath, OpenSceneMode.Additive);
            var study = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FixedTerrainPopulationSectorStudy>(true)).Single();
            var tiles = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).ToArray();
            var hashes = tiles.Select(t => Hash(AssetDatabase.GetAssetPath(t.terrainData))).ToArray();
            try
            {
                study.Initialize(); double time = Time.realtimeSinceStartupAsDouble + 1;
                var markers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>())
                    .Where(t => t.name.Contains("demand marker (no gameplay actor)")).ToArray();
                Assert.That(markers.Length, Is.EqualTo(2));
                Assert.That(markers.All(study.IsActorCoverageReady), Is.False, "Startup demand must await full collision publication.");
                Finish(study, time); Assert.That(study.ActiveCount, Is.GreaterThan(0)); Assert.That(study.FaultedCells, Is.Zero);
                var edgeMarker = markers.Single(t => t.name.StartsWith("BigARM"));
                Assert.That(study.IsActorCoverageReady(edgeMarker), Is.False, "Saved edge marker requires coverage beyond this isolated sector.");
                edgeMarker.position += new Vector3(-64, 0, -64); // EditMode demand fixture only; no gameplay actor movement.
                Finish(study, time);
                Assert.That(markers.All(study.IsActorCoverageReady), Is.True);
                var receipt = study.State.CaptureSnapshot().cells.First(c => c.acceptedCandidates.Count > 0);
                var candidate = receipt.acceptedCandidates[0]; var cell = new Vector2Int(candidate.x, candidate.z);
                string id = study.State.StableId(cell, candidate.recipeId, candidate.ordinal);
                Assert.That(study.State.TryRemove(cell, candidate.recipeId, candidate.ordinal), Is.True);
                Assert.That(study.GetComponentsInChildren<Collider>().Any(c => c.gameObject.name == id), Is.False);
                var saved = study.CapturePopulationJson(); int before = study.ActiveCount;
                Assert.That(study.RestorePopulationJson(saved), Is.True); Assert.That(study.ActiveCount, Is.Zero);
                Assert.That(markers.All(study.IsActorCoverageReady), Is.False);
                Finish(study, time); Assert.That(study.ActiveCount, Is.EqualTo(before));
                Assert.That(study.GetComponentsInChildren<Collider>().Any(c => c.gameObject.name == id), Is.False);
                var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single(); camera.enabled = false;
                Finish(study, time + 3);
                Assert.That(study.GetComponentsInChildren<MeshRenderer>().All(r => !r.enabled), Is.True);
                Assert.That(study.GetComponentsInChildren<Collider>().Any(c => c.enabled), Is.True, "Actor demand survives absent camera coverage.");
                Assert.That(markers.All(study.IsActorCoverageReady), Is.True);
                var position = markers[1].position; markers[1].position += Vector3.right * 4096;
                study.UpdateDemand(time + 3);
                Assert.That(study.IsActorCoverageReady(markers[1]), Is.False, "Leaving the explicit area cannot report complete coverage.");
                markers[1].position = position; Finish(study, time + 3);
                Assert.That(markers.All(study.IsActorCoverageReady), Is.True);
                study.Shutdown(); Assert.That(study.CapturePopulationJson(), Is.EqualTo(saved));
                study.Initialize(); Finish(study, Time.realtimeSinceStartupAsDouble + 1);
                Assert.That(study.State.IsRemoved(cell, candidate.recipeId, candidate.ordinal), Is.True);
                Assert.That(tiles.All(t => t.enabled && t.GetComponent<TerrainCollider>().enabled), Is.True);
                Assert.That(tiles.Select(t => Hash(AssetDatabase.GetAssetPath(t.terrainData))), Is.EqualTo(hashes));
            }
            finally { study.Shutdown(); EditorSceneManager.CloseScene(scene, true); }
        }
        private static void Finish(FixedTerrainPopulationSectorStudy study, double time)
        {
            for (int i = 0; i < 10000; i++)
            {
                study.AdvanceStudy(256, 100, time);
                if (study.UnreadyCells == 0 && study.PendingBuilds == 0 && study.PendingPresentations == 0 && study.ActiveCount > 0) return;
            }
            Assert.Fail("Reference study did not integrate within bounded work.");
        }
        private static string Hash(string path)
        { using var hash = SHA256.Create(); return string.Concat(hash.ComputeHash(File.ReadAllBytes(path)).Select(v => v.ToString("x2"))); }
    }
}
