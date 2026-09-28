using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DCliffFaceDecoratorTests
    {
        private const string SettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";

        [Test]
        public void SteepWorldChunkBuildsTheSameVisualFaceAfterReload()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(SettingsPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(Resources.Load<Material>("WorldCreator/CliffWall_LightGray"), Is.Not.Null);
            using var runtime = WorldCreatorProductionRuntime.Create(24681357);
            var coordinate = new Vector2Int(3, 2);
            var first = Build(coordinate, settings, runtime);
            var second = Build(coordinate, settings, runtime);
            Assert.That(runtime.TryRebase(new AbsoluteWorldPosition(18d, 0d, 0d)), Is.True);
            var rebased = Build(coordinate, settings, runtime);
            try
            {
                Assert.That(first.mesh, Is.Not.Null);
                Assert.That(first.mesh.triangles.Length, Is.GreaterThan(0));
                Assert.That(first.mesh.vertices, Is.EqualTo(second.mesh.vertices));
                Assert.That(first.mesh.triangles, Is.EqualTo(second.mesh.triangles));
                Assert.That(first.mesh.vertices, Is.EqualTo(rebased.mesh.vertices));
                Assert.That(first.chunk.GetComponentsInChildren<Collider>(), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(first.chunk.gameObject);
                Object.DestroyImmediate(second.chunk.gameObject);
                Object.DestroyImmediate(rebased.chunk.gameObject);
            }
        }

        private static (TopDown3DGeneratedChunk chunk, Mesh mesh) Build(
            Vector2Int coordinate, TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime runtime)
        {
            var root = new GameObject("Cliff test chunk");
            var absolute = new AbsoluteWorldPosition(
                coordinate.x * (double)settings.ChunkSize, 0d,
                coordinate.y * (double)settings.ChunkSize);
            Assert.That(runtime.TryToLocal(absolute, out var local), Is.True);
            root.transform.position = new Vector3(local.X, local.Y, local.Z);
            var chunk = root.AddComponent<TopDown3DGeneratedChunk>();
            chunk.Initialize(coordinate, null);
            foreach (var _ in TopDown3DCliffFaceDecorator.DecorateSteps(
                chunk, settings, runtime, new Vector2(-1000f, -1000f), null)) { }
            var filter = chunk.GetComponentInChildren<MeshFilter>();
            return (chunk, filter != null ? filter.sharedMesh : null);
        }
    }
}
