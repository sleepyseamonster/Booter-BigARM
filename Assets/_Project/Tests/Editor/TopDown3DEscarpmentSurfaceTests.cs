using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Tests.Editor
{
    public sealed class TopDown3DEscarpmentSurfaceTests
    {
        [Test]
        public void SurfaceData_IsDeterministicDenseAndSmoothlyShaded()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            try
            {
                Assert.That(TryFindDecoratedChunk(settings, out var coordinate), Is.True);
                var first = TopDown3DEscarpmentSurfaceDecorator.BuildData(settings, coordinate);
                var second = TopDown3DEscarpmentSurfaceDecorator.BuildData(settings, coordinate);

                Assert.That(first.Vertices.Length, Is.GreaterThan(0));
                Assert.That(first.Vertices.Length, Is.EqualTo(first.Normals.Length));
                Assert.That(first.Triangles.Length, Is.GreaterThan(first.Vertices.Length));
                Assert.That(second.Vertices, Is.EqualTo(first.Vertices));
                Assert.That(second.Normals, Is.EqualTo(first.Normals));
                Assert.That(second.Triangles, Is.EqualTo(first.Triangles));

                for (var index = 0; index < first.Normals.Length; index++)
                {
                    Assert.That(first.Normals[index].sqrMagnitude, Is.EqualTo(1f).Within(0.0001f));
                }

                var maximumTriangleEdge = 0f;
                for (var index = 0; index < first.Triangles.Length; index += 3)
                {
                    var a = first.Vertices[first.Triangles[index]];
                    var b = first.Vertices[first.Triangles[index + 1]];
                    var c = first.Vertices[first.Triangles[index + 2]];
                    maximumTriangleEdge = Mathf.Max(
                        maximumTriangleEdge,
                        Vector3.Distance(a, b),
                        Vector3.Distance(b, c),
                        Vector3.Distance(c, a));
                }

                Assert.That(maximumTriangleEdge, Is.LessThan(1.25f));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Decorator_CreatesPresentationOnlySurfaceWithoutDuplicateShadows()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            var chunkObject = new GameObject("Smooth Escarpment Test Chunk");
            try
            {
                Assert.That(TryFindDecoratedChunk(settings, out var coordinate), Is.True);
                var chunk = chunkObject.AddComponent<TopDown3DGeneratedChunk>();
                chunk.Initialize(coordinate, null);

                TopDown3DEscarpmentSurfaceDecorator.Decorate(chunk, settings, null);

                var surface = chunk.transform.Find("Smooth Rocky Escarpment Surface");
                Assert.That(surface, Is.Not.Null);
                Assert.That(surface.GetComponent<Collider>(), Is.Null);
                Assert.That(
                    surface.GetComponent<MeshRenderer>().shadowCastingMode,
                    Is.EqualTo(ShadowCastingMode.Off));
            }
            finally
            {
                var filters = chunkObject.GetComponentsInChildren<MeshFilter>();
                for (var index = 0; index < filters.Length; index++)
                {
                    if (filters[index].sharedMesh != null)
                    {
                        Object.DestroyImmediate(filters[index].sharedMesh);
                    }
                }

                Object.DestroyImmediate(chunkObject);
                Object.DestroyImmediate(settings);
            }
        }

        private static bool TryFindDecoratedChunk(
            TopDown3DWorldSettings settings,
            out Vector2Int coordinate)
        {
            for (var z = -6; z <= 6; z++)
            {
                for (var x = -6; x <= 6; x++)
                {
                    coordinate = new Vector2Int(x, z);
                    if (TopDown3DEscarpmentSurfaceDecorator.BuildData(settings, coordinate).Vertices.Length > 0)
                    {
                        return true;
                    }
                }
            }

            coordinate = default;
            return false;
        }
    }
}
