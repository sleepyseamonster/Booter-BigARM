using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainDressingTests
    {
        [Test]
        public void NativeCollisionQueriesRespectNegativeCellsSeamsOuterEdgesAndDisabledTiles()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(32, 100, 32) };
            try
            {
                var heights = new float[33, 33];
                for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = .5f;
                data.SetHeights(0, 0, heights);
                var before = data.GetHeights(0, 0, 33, 33);
                var a = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
                var b = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
                SceneManager.MoveGameObjectToScene(a.gameObject, scene); SceneManager.MoveGameObjectToScene(b.gameObject, scene);
                a.transform.position = new Vector3(-32, -25, -32); b.transform.position = new Vector3(0, -25, -32);
                Physics.SyncTransforms();
                var first = new FixedTerrainSurfaceIndex(new[] { a, b }, "fixture", 32);
                var reversed = new FixedTerrainSurfaceIndex(new[] { b, a }, "fixture", 32);
                Assert.That(first.TrySample(new Vector2(-16, -16), out var sample), Is.True);
                Assert.That(sample.Position.y, Is.EqualTo(25).Within(.01));
                Assert.That(sample.SlopeDegrees, Is.EqualTo(0).Within(.01));
                Assert.That(first.TrySample(new Vector2(0, -16), out var seam), Is.True);
                Assert.That(reversed.TrySample(new Vector2(0, -16), out var reverseSeam), Is.True);
                Assert.That(seam.Terrain, Is.SameAs(reverseSeam.Terrain));
                Assert.That(first.TrySample(new Vector2(32, -16), out _), Is.True);
                Assert.That(first.TrySample(new Vector2(32.01f, -16), out _), Is.False);
                Assert.That(first.TrySample(new Vector2(float.NaN, 0), out _), Is.False);
                b.GetComponent<TerrainCollider>().enabled = false;
                Assert.That(first.TrySample(new Vector2(16, -16), out _), Is.False);
                Assert.Throws<ArgumentException>(() => new FixedTerrainSurfaceIndex(new[] { a, a }, "fixture", 32));
                a.transform.rotation = Quaternion.Euler(0, 20, 0);
                Assert.Throws<ArgumentException>(() => new FixedTerrainSurfaceIndex(new[] { a }, "fixture", 32));
                Assert.That(data.GetHeights(0, 0, 33, 33), Is.EqualTo(before));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); }
        }

        [Test]
        public void HolesAndColliderDataMismatchFailClosed()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(32, 100, 32) };
            var other = new TerrainData { heightmapResolution = 33, size = new Vector3(32, 100, 32) };
            try
            {
                data.SetHoles(0, 0, new bool[32, 32]);
                var terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
                SceneManager.MoveGameObjectToScene(terrain.gameObject, scene); Physics.SyncTransforms();
                var index = new FixedTerrainSurfaceIndex(new[] { terrain }, "holes", 32);
                Assert.That(index.TrySample(new Vector2(16, 16), out _), Is.False);
                terrain.GetComponent<TerrainCollider>().terrainData = other;
                Assert.That(index.TrySample(new Vector2(16, 16), out _), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(other); }
        }

        [Test]
        public void ContactFieldIsOrderIndependentBoundedAndWindDirected()
        {
            var a = new FixedTerrainRockContactField.Footprint(Vector2.zero, Vector2.one);
            var b = new FixedTerrainRockContactField.Footprint(new Vector2(0, 2), Vector2.one * .5f);
            var first = new FixedTerrainRockContactField(new[] { a, b }, Vector2.right, 7);
            var second = new FixedTerrainRockContactField(new[] { b, a }, Vector2.right, 7);
            for (int z = -8; z <= 8; z++) for (int x = -8; x <= 8; x++)
            {
                var position = new Vector2(x * .5f, z * .5f);
                var result = first.Evaluate(position, 0); var reverse = second.Evaluate(position, 0);
                Assert.That(result.Sand, Is.EqualTo(reverse.Sand)); Assert.That(result.Gravel, Is.EqualTo(reverse.Gravel));
                Assert.That(result.Relief, Is.EqualTo(reverse.Relief).And.InRange(0, .08f));
                Assert.That(result.Sand, Is.InRange(0, 1)); Assert.That(result.Gravel, Is.InRange(0, 1));
            }
            var one = new FixedTerrainRockContactField(new[] { a }, Vector2.right, 7);
            Assert.That(one.Evaluate(new Vector2(2, 0), 0).Relief, Is.GreaterThan(0));
            Assert.That(one.Evaluate(new Vector2(-2, 0), 0).Relief, Is.Zero);
            Assert.That(one.Evaluate(new Vector2(2, 0), 45).Relief, Is.Zero);
            Assert.That(one.Evaluate(new Vector2(100, 100), 0).Sand, Is.Zero);
            Assert.Throws<ArgumentException>(() => new FixedTerrainRockContactField(new[] { a }, Vector2.zero, 7));
            Assert.Throws<ArgumentException>(() => new FixedTerrainRockContactField(new[] { default(FixedTerrainRockContactField.Footprint) }, Vector2.right, 7));
        }

        [Test]
        public void SavedStudyUsesMatchedInputsAndActualReliefCollision()
        {
            const string folder = BooterBigArm.Editor.FixedTerrainDressingStudyBuilder.AssetFolder;
            if (!System.IO.File.Exists(BooterBigArm.Editor.FixedTerrainDressingStudyBuilder.ScenePath))
                Assert.Fail("Build the bounded study before running its serialized comparison gate.");
            for (int row = 0; row < 2; row++)
            {
                var baseline = AssetDatabase.LoadAssetAtPath<Mesh>($"{folder}/StudyGround_{row}_0.asset");
                var contact = AssetDatabase.LoadAssetAtPath<Mesh>($"{folder}/StudyGround_{row}_1.asset");
                var relief = AssetDatabase.LoadAssetAtPath<Mesh>($"{folder}/StudyGround_{row}_2.asset");
                Assert.That(contact.vertices, Is.EqualTo(baseline.vertices));
                Assert.That(contact.uv, Is.EqualTo(relief.uv)); Assert.That(contact.colors, Is.EqualTo(relief.colors));
                var before = contact.vertices; var after = relief.vertices; float max = 0;
                for (int i = 0; i < before.Length; i++)
                {
                    Assert.That(after[i].x, Is.EqualTo(before[i].x)); Assert.That(after[i].z, Is.EqualTo(before[i].z));
                    float change = after[i].y - before[i].y; Assert.That(change, Is.InRange(-.000001f, .080001f)); max = Mathf.Max(max, change);
                }
                Assert.That(max, Is.GreaterThan(.001f));
            }
            var scene = EditorSceneManager.OpenPreviewScene(BooterBigArm.Editor.FixedTerrainDressingStudyBuilder.ScenePath);
            try
            {
                foreach (var root in scene.GetRootGameObjects()) foreach (var collider in root.GetComponentsInChildren<MeshCollider>())
                    Assert.That(collider.sharedMesh, Is.SameAs(collider.GetComponent<MeshFilter>().sharedMesh));
                Assert.That(scene.GetRootGameObjects().Length, Is.EqualTo(8));
                foreach (var root in scene.GetRootGameObjects())
                {
                    Assert.That(root.GetComponentsInChildren<Terrain>().Length, Is.Zero);
                    foreach (var transform in root.GetComponentsInChildren<Transform>())
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero);
                }
                var roots = scene.GetRootGameObjects().Where(r => r.GetComponentInChildren<MeshCollider>() != null).ToArray();
                for (int row = 0; row < 2; row++)
                {
                    var controls = roots[row * 3].GetComponentsInChildren<MeshFilter>().Where(m => m.GetComponent<MeshCollider>() == null).ToArray();
                    for (int panel = 1; panel < 3; panel++)
                    {
                        var candidates = roots[row * 3 + panel].GetComponentsInChildren<MeshFilter>().Where(m => m.GetComponent<MeshCollider>() == null).ToArray();
                        Assert.That(candidates.Length, Is.EqualTo(controls.Length));
                        for (int i = 0; i < controls.Length; i++)
                        {
                            Assert.That(candidates[i].sharedMesh, Is.SameAs(controls[i].sharedMesh));
                            Assert.That(candidates[i].transform.localPosition, Is.EqualTo(controls[i].transform.localPosition));
                            Assert.That(candidates[i].transform.localRotation, Is.EqualTo(controls[i].transform.localRotation));
                            Assert.That(candidates[i].transform.localScale, Is.EqualTo(controls[i].transform.localScale));
                        }
                    }
                }
                Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == scene.path), Is.False);
                var shader = Shader.Find("BooterBigArm/Studies/Fixed Terrain Dressing");
                Assert.That(shader, Is.Not.Null);
                Assert.That(ShaderUtil.GetShaderMessages(shader).Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
