using System.Collections.Generic;
using System.Linq;
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
        public void SavedCliffSourceHasFiveBakedRockRecipesWithDescendingLods()
        {
            foreach (var recipe in new[] { "A", "B", "C", "D", "E" })
            {
                var previousTriangles = int.MaxValue;
                for (var lod = 0; lod < 3; lod++)
                {
                    var mesh = Resources.Load<Mesh>($"WorldCreator/CliffStones/CliffStone_{recipe}_LOD{lod}");
                    Assert.That(mesh, Is.Not.Null, recipe + " LOD" + lod);
                    Assert.That(mesh.bounds.size.x, Is.GreaterThan(0.1f));
                    Assert.That(mesh.bounds.size.y, Is.GreaterThan(0.1f));
                    Assert.That(mesh.bounds.size.z, Is.GreaterThan(0.1f));
                    var triangles = mesh.triangles.Length / 3;
                    Assert.That(triangles, Is.LessThan(previousTriangles));
                    previousTriangles = triangles;
                }
            }
        }

        [Test]
        public void SteepWorldChunkBuildsTheSamePhysicalRockFormationAfterReload()
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
                Assert.That(first.rocks.Length, Is.GreaterThanOrEqualTo(3));
                Assert.That(first.rocks, Is.EqualTo(second.rocks));
                Assert.That(first.rocks, Is.EqualTo(rebased.rocks));
                Assert.That(first.chunk.GetComponentsInChildren<BoxCollider>().Length,
                    Is.EqualTo(first.rocks.Length));
                Assert.That(first.chunk.GetComponentsInChildren<LODGroup>().Length,
                    Is.EqualTo(first.rocks.Length));
                Assert.That(first.chunk.GetComponentsInChildren<MeshFilter>()
                    .All(filter => filter.sharedMesh.bounds.size.z > 0.1f), Is.True);
                Assert.That(first.chunk.GetComponentsInChildren<MeshFilter>()
                    .All(filter => filter.sharedMesh.name.StartsWith("CliffStone_")), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(first.chunk.gameObject);
                Object.DestroyImmediate(second.chunk.gameObject);
                Object.DestroyImmediate(rebased.chunk.gameObject);
            }
        }

        private static (TopDown3DGeneratedChunk chunk, string[] rocks) Build(
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
                chunk, settings, runtime, Vector2.zero,
                new List<TopDown3DRockFormationPlan>())) { }
            var rocks = chunk.GetComponentsInChildren<BoxCollider>()
                .Select(collider => collider.name + "|"
                    + collider.transform.localPosition + "|"
                    + collider.transform.localRotation + "|"
                    + collider.transform.localScale)
                .ToArray();
            return (chunk, rocks);
        }
    }
}
