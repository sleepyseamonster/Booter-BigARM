using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests.Editor
{
    public sealed class TopDown3DEscarpmentTests
    {
        [Test]
        public void EscarpmentSampling_IsDeterministicAndLeavesOpenGround()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            try
            {
                var elevatedSamples = 0;
                var openSamples = 0;
                for (var z = -90; z <= 90; z += 3)
                {
                    for (var x = -90; x <= 90; x += 3)
                    {
                        var first = TopDown3DEscarpmentSampler.SampleElevation(settings, x, z);
                        var second = TopDown3DEscarpmentSampler.SampleElevation(settings, x, z);
                        Assert.That(second, Is.EqualTo(first).Within(0.000001f));
                        if (first > 0.2f)
                        {
                            elevatedSamples++;
                        }
                        else if (first <= 0.001f)
                        {
                            openSamples++;
                        }
                    }
                }

                Assert.That(elevatedSamples, Is.GreaterThan(20));
                Assert.That(openSamples, Is.GreaterThan(elevatedSamples * 3));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void EscarpmentFeatures_AreStableUniqueAndStayWithinVaultHeight()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            try
            {
                var first = new List<TopDown3DEscarpmentFeature>();
                var second = new List<TopDown3DEscarpmentFeature>();
                var bounds = new Rect(-180f, -180f, 360f, 360f);
                TopDown3DEscarpmentSampler.CollectFeatures(settings, bounds, first);
                TopDown3DEscarpmentSampler.CollectFeatures(settings, bounds, second);

                Assert.That(first.Count, Is.GreaterThan(0));
                Assert.That(second.Count, Is.EqualTo(first.Count));
                var cells = new HashSet<Vector2Int>();
                for (var index = 0; index < first.Count; index++)
                {
                    Assert.That(first[index].CellX, Is.EqualTo(second[index].CellX));
                    Assert.That(first[index].CellZ, Is.EqualTo(second[index].CellZ));
                    Assert.That(first[index].Center, Is.EqualTo(second[index].Center));
                    Assert.That(first[index].Height, Is.EqualTo(second[index].Height));
                    Assert.That(first[index].Height, Is.LessThanOrEqualTo(0.8f));
                    Assert.That(
                        cells.Add(new Vector2Int(first[index].CellX, first[index].CellZ)),
                        Is.True);
                }
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void EscarpmentFaceData_IsDeterministicFacetedAndClimbable()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            try
            {
                Assert.That(TryFindDecoratedChunk(settings, out var coordinate), Is.True);
                var first = TopDown3DEscarpmentDecorator.BuildData(settings, coordinate);
                var second = TopDown3DEscarpmentDecorator.BuildData(settings, coordinate);

                Assert.That(first.Vertices.Length, Is.GreaterThan(0));
                Assert.That(first.Vertices.Length, Is.EqualTo(first.Normals.Length));
                Assert.That(first.Triangles.Length, Is.EqualTo(first.Vertices.Length));
                Assert.That(second.Vertices, Is.EqualTo(first.Vertices));
                Assert.That(second.Triangles, Is.EqualTo(first.Triangles));
                Assert.That(second.Walls.Length, Is.EqualTo(first.Walls.Length));
                Assert.That(first.Walls.Length, Is.GreaterThan(0));

                var rockyNormals = 0;
                for (var index = 0; index < first.Normals.Length; index++)
                {
                    if (Mathf.Abs(Vector3.Dot(first.Normals[index], Vector3.up)) < 0.9f)
                    {
                        rockyNormals++;
                    }
                }

                Assert.That(rockyNormals, Is.GreaterThan(first.Normals.Length / 2));
                for (var index = 0; index < first.Walls.Length; index++)
                {
                    Assert.That(first.Walls[index].Size.y, Is.GreaterThan(0f));
                    Assert.That(first.Walls[index].Size.y, Is.LessThanOrEqualTo(0.7801f));
                    Assert.That(first.Walls[index].Size.x, Is.GreaterThan(0.1f));
                    Assert.That(first.Walls[index].Size.z, Is.GreaterThan(0.1f));
                }
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void EscarpmentDecorator_CreatesRockFacesAndTraversalMarkers()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            var chunkObject = new GameObject("Escarpment Test Chunk");
            try
            {
                Assert.That(TryFindDecoratedChunk(settings, out var coordinate), Is.True);
                var chunk = chunkObject.AddComponent<TopDown3DGeneratedChunk>();
                chunk.Initialize(coordinate, null);

                TopDown3DEscarpmentDecorator.Decorate(chunk, settings, null);

                Assert.That(chunk.transform.Find("Rocky Escarpment Faces"), Is.Not.Null);
                var collisionRoot = chunk.transform.Find("Climbable Escarpment Walls");
                Assert.That(collisionRoot, Is.Not.Null);
                Assert.That(
                    collisionRoot.GetComponentsInChildren<TopDown3DTraversalObstacle>().Length,
                    Is.GreaterThan(0));
                Assert.That(
                    collisionRoot.GetComponentsInChildren<BoxCollider>().Length,
                    Is.GreaterThan(0));
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
                    if (TopDown3DEscarpmentDecorator.BuildData(settings, coordinate).Vertices.Length > 0)
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
