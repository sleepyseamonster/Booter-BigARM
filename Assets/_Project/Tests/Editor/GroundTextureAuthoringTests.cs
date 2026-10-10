using System;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class GroundTextureAuthoringTests
    {
        private static Terrain MakeTerrain(Scene scene, TerrainData data, float x)
        {
            var result = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
            SceneManager.MoveGameObjectToScene(result.gameObject, scene); result.transform.position = new Vector3(x, 0, 0);
            return result;
        }
        private static TerrainData Data(TerrainLayer baseline)
        {
            var data = new TerrainData { heightmapResolution = 33, alphamapResolution = 32, size = new Vector3(16, 4, 16), terrainLayers = new[] { baseline } };
            var heights = new float[33, 33];
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = .1f + x * .004f;
            data.SetHeights(0, 0, heights);
            var weights = new float[32, 32, 1]; for (int z = 0; z < 32; z++) for (int x = 0; x < 32; x++) weights[z, x, 0] = 1;
            data.SetAlphamaps(0, 0, weights); return data;
        }

        [Test]
        public void NativeBrushPreservesHeightsAndNormalizedWeightsAcrossTileBoundary()
        {
            var scene = EditorSceneManager.NewPreviewScene(); var baseline = new TerrainLayer(); var paint = new TerrainLayer();
            var leftData = Data(baseline); var rightData = Data(baseline);
            try
            {
                var left = MakeTerrain(scene, leftData, -16); var right = MakeTerrain(scene, rightData, 0);
                var before = leftData.GetHeights(0, 0, 33, 33); var holes = leftData.GetHoles(0, 0, 32, 32);
                Physics.SyncTransforms();
                Assert.That(GroundTextureAuthoring.PaintTerrain(left, paint, new Vector3(0, 0, 8), 4, .5f, .5f), Is.True);
                Assert.That(GroundTextureAuthoring.PaintTerrain(right, paint, new Vector3(0, 0, 8), 4, .5f, .5f), Is.True);
                Assert.That(leftData.GetHeights(0, 0, 33, 33), Is.EqualTo(before)); Assert.That(leftData.GetHoles(0, 0, 32, 32), Is.EqualTo(holes));
                var l = leftData.GetAlphamaps(0, 0, 32, 32); var r = rightData.GetAlphamaps(0, 0, 32, 32);
                for (int z = 0; z < 32; z++)
                {
                    Assert.That(l[z, 31, 1], Is.EqualTo(r[z, 0, 1]).Within(.002f));
                    for (int x = 0; x < 32; x++) Assert.That(l[z, x, 0] + l[z, x, 1], Is.EqualTo(1).Within(.002f));
                }
                Assert.That(l[16, 31, 1], Is.GreaterThan(.2f)); Assert.That(l[0, 0, 1], Is.Zero);
                Assert.That(leftData.terrainLayers[0], Is.SameAs(baseline));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(leftData); UnityEngine.Object.DestroyImmediate(rightData); UnityEngine.Object.DestroyImmediate(baseline); UnityEngine.Object.DestroyImmediate(paint); }
        }

        [Test]
        public void BrushUndoRestoresLayerAssignmentsAndWeightsForACompleteStroke()
        {
            var scene = EditorSceneManager.NewPreviewScene(); var baseline = new TerrainLayer(); var paint = new TerrainLayer(); var data = Data(baseline);
            try
            {
                var terrain = MakeTerrain(scene, data, 0); Physics.SyncTransforms();
                var before = data.GetAlphamaps(0, 0, 32, 32);
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); var targets = new System.Collections.Generic.HashSet<TerrainData>();
                GroundTextureAuthoring.PaintTerrain(terrain, paint, new Vector3(8, 0, 8), 3, .5f, .5f, targets);
                GroundTextureAuthoring.PaintTerrain(terrain, paint, new Vector3(9, 0, 8), 3, .5f, .5f, targets);
                Assert.That(targets.Count, Is.EqualTo(1)); Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group); Undo.PerformUndo();
                Assert.That(data.terrainLayers.Length, Is.EqualTo(1));
                Assert.That(data.GetAlphamaps(0, 0, 32, 32), Is.EqualTo(before));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(baseline); UnityEngine.Object.DestroyImmediate(paint); }
        }

        [Test]
        public void EmptyHoleOrOutsideBrushDoesNotAddALayer()
        {
            var scene = EditorSceneManager.NewPreviewScene(); var baseline = new TerrainLayer(); var paint = new TerrainLayer(); var data = Data(baseline);
            try
            {
                var terrain = MakeTerrain(scene, data, 0); data.SetHoles(0, 0, new bool[32, 32]); Physics.SyncTransforms();
                Assert.That(GroundTextureAuthoring.PaintTerrain(terrain, paint, new Vector3(8, 0, 8), 3, 1, .5f), Is.False);
                Assert.That(GroundTextureAuthoring.PaintTerrain(terrain, paint, new Vector3(100, 0, 100), 3, 1, .5f), Is.False);
                Assert.That(GroundTextureAuthoring.PaintTerrain(terrain, paint, new Vector3(8, 0, 8), float.NaN, 1, .5f), Is.False);
                Assert.That(data.terrainLayers.Length, Is.EqualTo(1));
                terrain.GetComponent<TerrainCollider>().enabled = false;
                Assert.That(GroundTextureAuthoring.PaintTerrain(terrain, paint, new Vector3(8, 0, 8), 3, 1, .5f), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(baseline); UnityEngine.Object.DestroyImmediate(paint); }
        }

        [Test]
        public void StickerGeometryPersistsAndNeverAddsCollisionOrChangesTerrain()
        {
            var scene = EditorSceneManager.NewPreviewScene(); var baseline = new TerrainLayer(); var data = Data(baseline);
            try
            {
                MakeTerrain(scene, data, 0); Physics.SyncTransforms(); var before = data.GetHeights(0, 0, 33, 33);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldGravelAlbedo.png");
                var sticker = GroundTextureAuthoring.Create(scene, new Vector3(8, 0, 8), texture, new Vector2(4, 4), 25);
                Assert.That(sticker.HasGeometry, Is.True); Assert.That(sticker.GetComponent<Collider>(), Is.Null);
                Assert.That(sticker.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>(GroundTextureAuthoring.MaterialPath)));
                var original = sticker.GetComponent<MeshFilter>().sharedMesh.vertices;
                string saved = EditorJsonUtility.ToJson(sticker);
                var clone = new GameObject("Reload proof"); SceneManager.MoveGameObjectToScene(clone, scene);
                var restored = clone.AddComponent<GroundTextureSticker>(); EditorJsonUtility.FromJsonOverwrite(saved, restored); restored.RebuildVisual();
                Assert.That(restored.AuthoredId, Is.EqualTo(sticker.AuthoredId)); Assert.That(restored.Texture, Is.SameAs(texture));
                Assert.That(restored.GetComponent<MeshFilter>().sharedMesh.vertices, Is.EqualTo(original));
                Assert.That(data.GetHeights(0, 0, 33, 33), Is.EqualTo(before));
                data.SetHoles(0, 0, new bool[32, 32]); Physics.SyncTransforms();
                Assert.That(GroundTextureAuthoring.Conform(sticker), Is.False); Assert.That(sticker.HasGeometry, Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(baseline); }
        }

        [Test]
        public void DuplicatingAStickerRetainsTheOriginalIdAndAssignsANewCopyId()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Original"); SceneManager.MoveGameObjectToScene(go, scene);
                var original = go.AddComponent<GroundTextureSticker>(); original.SetAuthoredId("original-author-id");
                var copy = UnityEngine.Object.Instantiate(go).GetComponent<GroundTextureSticker>();
                SceneManager.MoveGameObjectToScene(copy.gameObject, scene); GroundStickerIdentity.RepairIds(scene);
                Assert.That(original.AuthoredId, Is.EqualTo("original-author-id"));
                Assert.That(copy.AuthoredId, Is.Not.EqualTo(original.AuthoredId).And.Not.Empty);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void SavedDemoRetainsStickerGeometryPaintedLayersAndBuildExclusion()
        {
            var scene = EditorSceneManager.OpenPreviewScene(GroundTextureToolVerification.DemoScene);
            try
            {
                var roots = scene.GetRootGameObjects();
                var stickers = roots.SelectMany(r => r.GetComponentsInChildren<GroundTextureSticker>()).ToArray();
                var terrains = roots.SelectMany(r => r.GetComponentsInChildren<Terrain>()).ToArray();
                Assert.That(stickers.Length, Is.EqualTo(2)); Assert.That(terrains.Length, Is.EqualTo(2));
                Assert.That(stickers.Select(s => s.AuthoredId).Distinct().Count(), Is.EqualTo(2));
                foreach (var sticker in stickers)
                {
                    Assert.That(sticker.HasGeometry, Is.True); Assert.That(sticker.GetComponent<Collider>(), Is.Null);
                    Assert.That(sticker.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(4));
                    Assert.That(sticker.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>(GroundTextureAuthoring.MaterialPath)));
                }
                foreach (var terrain in terrains)
                    Assert.That(terrain.terrainData.terrainLayers.Length, Is.GreaterThan(1));
                foreach (var root in roots) foreach (var transform in root.GetComponentsInChildren<Transform>())
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero);
                Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == scene.path), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
