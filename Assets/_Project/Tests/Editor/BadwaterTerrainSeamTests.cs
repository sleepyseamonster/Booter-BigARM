using System.Collections;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BooterBigArm.Tests
{
    public sealed class BadwaterTerrainSeamTests
    {
        [Test]
        public void CanonicalValidators_AcceptSavedSceneAndAllSourceDerivedVertices()
        {
            BadwaterPlayableSceneBuilder.ValidateFromCli();
            BadwaterTerrainReadbackAudit.ValidateFromCli();
        }

        [Test]
        public void ContextGrid_PreservesEverySourceSampleAndInterpolatesNewVertices()
        {
            var source = new float[129, 129];
            for (int z = 0; z < 129; z++)
            for (int x = 0; x < 129; x++) source[z, x] = (z * z + x) / 20000f;
            var result = BadwaterHeightmapStitching.PromoteContextGrid(source);
            for (int z = 0; z < 129; z++)
            for (int x = 0; x < 129; x++) Assert.That(result[z * 2, x * 2], Is.EqualTo(source[z, x]));
            Assert.That(result[15, 21], Is.EqualTo((source[7, 10] + source[7, 11] + source[8, 10] + source[8, 11]) * .25f).Within(1e-7f));
            Assert.That(result[256, 256], Is.EqualTo(source[128, 128]));
        }

        [Test]
        public void SharedBorders_CloseEdgesAndFourWayCornerWithoutChangingInteriors()
        {
            var tiles = new Dictionary<Vector2Int, float[,]>();
            // Insert in reverse geographic order to exercise deterministic ownership.
            for (int z = 1; z >= 0; z--)
            for (int x = 1; x >= 0; x--)
            {
                var heights = new float[257, 257];
                for (int i = 0; i < 257; i++)
                for (int j = 0; j < 257; j++) heights[i, j] = .1f + x * .01f + z * .02f;
                tiles.Add(new Vector2Int(x, z), heights);
            }
            BadwaterHeightmapStitching.StitchBorders(tiles);
            for (int i = 0; i < 257; i++)
            {
                Assert.That(tiles[Vector2Int.zero][i, 256], Is.EqualTo(tiles[Vector2Int.right][i, 0]));
                Assert.That(tiles[Vector2Int.zero][256, i], Is.EqualTo(tiles[Vector2Int.up][0, i]));
                Assert.That(tiles[Vector2Int.one][0, i], Is.EqualTo(tiles[Vector2Int.right][256, i]));
                Assert.That(tiles[Vector2Int.one][i, 0], Is.EqualTo(tiles[Vector2Int.up][i, 256]));
            }
            Assert.That(tiles[Vector2Int.one][0, 0], Is.EqualTo(.1f));
            Assert.That(tiles[Vector2Int.one][128, 128], Is.EqualTo(.13f).Within(1e-7f));
        }

        [Test]
        public void SavedScene_PreservesMeasuredDemSamplesAndTileBounds()
        {
            var scene = EditorSceneManager.OpenScene(BadwaterTerrainSeamRepair.ScenePath, OpenSceneMode.Single);
            var grid = BadwaterTerrainSeamRepair.GetGrid(scene);
            float maximumError = 0f;
            for (int row = 0; row < 16; row++)
            for (int col = 0; col < 16; col++)
            {
                var tile = grid[row, col];
                Assert.That(tile.terrainData.heightmapResolution, Is.EqualTo(257));
                Assert.That(tile.terrainData.size, Is.EqualTo(new Vector3(256f, 1800f, 256f)));
                Assert.That(tile.transform.position, Is.EqualTo(new Vector3(col * 256f - 2048f, -100f, (15 - row) * 256f - 2048f)));
                Assert.That(tile.GetComponent<TerrainCollider>().terrainData, Is.SameAs(tile.terrainData));
                bool focus = row >= 12 && row <= 13 && col >= 1 && col <= 2;
                int size = focus ? 257 : 129, multiplier = focus ? 1 : 2;
                var raw = File.ReadAllBytes($"Assets/_Project/Art/Terrain/BadwaterFourSlices/Source/Heights/r{row:00}_c{col:00}.bytes");
                var saved = tile.terrainData.GetHeights(0, 0, 257, 257);
                for (int north = 0; north < size; north++)
                for (int x = 0; x < size; x++)
                {
                    // Only the pre-existing odd focus-perimeter samples were intentionally interpolated.
                    if (focus && ((row == 12 && north == 0 || row == 13 && north == 256) && x % 2 == 1 ||
                        (col == 1 && x == 0 || col == 2 && x == 256) && north % 2 == 1)) continue;
                    int offset = (north * size + x) * 2;
                    float expected = (raw[offset] | raw[offset + 1] << 8) / 65535f;
                    maximumError = Mathf.Max(maximumError, Mathf.Abs(saved[(size - 1 - north) * multiplier, x * multiplier] - expected) * 1800f);
                }
            }
            Assert.That(maximumError, Is.LessThanOrEqualTo(.04f), "Source DEM elevations changed beyond their existing quantization tolerance.");
            Debug.Log($"BADWATER_SOURCE_FIDELITY: maximum saved DEM sample error {maximumError:R} m.");
        }

        [UnityTest]
        public IEnumerator ReloadedScene_RestoresAllNeighborsAndExactBorders()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var scene = EditorSceneManager.OpenScene(BadwaterTerrainSeamRepair.ScenePath, OpenSceneMode.Single);
            yield return null;
            yield return null;
            // Deliberately do not call SetNeighbors/AutoConnect here: scene loading must restore it.
            BadwaterTerrainSeamRepair.Validate(scene);
        }

        [UnityTest]
        public IEnumerator PlayMode_RestoresAllNeighborsAndExactBorders()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            EditorSceneManager.LoadSceneInPlayMode(BadwaterTerrainSeamRepair.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            BadwaterTerrainSeamRepair.Validate(SceneManager.GetActiveScene());
        }

        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }

    [InitializeOnLoad]
    public static class BadwaterSeamEditorTestRun
    {
        private const string RunningKey = "BooterBigArm.BadwaterSeamTestRun";
        static BadwaterSeamEditorTestRun()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultCallback());
        }

        [MenuItem("Booter & BigARM/Badwater Terrain/Run Seam And Playability Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Save scene edits and leave Play mode before testing.");
            SessionState.SetBool(RunningKey, true);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
                testNames = new[] { "BooterBigArm.Tests.BadwaterTerrainSeamTests", "BooterBigArm.Tests.ScenePlayabilityAuditTests" }
            }));
        }

        private sealed class ResultCallback : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool(RunningKey, false)) return;
                SessionState.EraseBool(RunningKey);
                Directory.CreateDirectory("Logs");
                TestRunnerApi.SaveResultToFile(result, "Logs/badwater-seam-editor-tests.xml");
                Debug.Log($"BADWATER_EDITOR_TESTS: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");
                EditorApplication.delayCall += () => EditorSceneManager.OpenScene(BadwaterTerrainSeamRepair.ScenePath, OpenSceneMode.Single);
            }
        }
    }
}
