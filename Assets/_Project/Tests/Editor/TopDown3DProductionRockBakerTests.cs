using System;
using System.Collections.Generic;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DProductionRockBakerTests
    {
        [Test]
        public void GeneratedRockFamily_IsDeterministicFiniteAndProgressivelySimplified()
        {
            foreach (TopDown3DNaturalObjectShape shape in Enum.GetValues(typeof(TopDown3DNaturalObjectShape)))
            {
                for (var variant = 0; variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape; variant++)
                {
                    Mesh previous = null;
                    for (var lod = 0; lod < 3; lod++)
                    {
                        var first = TopDown3DProductionRockBaker.BuildMesh(shape, variant, lod);
                        var second = TopDown3DProductionRockBaker.BuildMesh(shape, variant, lod);
                        try
                        {
                            AssertMeshIsFinite(first, shape, variant, lod);
                            CollectionAssert.AreEqual(first.vertices, second.vertices);
                            CollectionAssert.AreEqual(first.normals, second.normals);
                            CollectionAssert.AreEqual(first.triangles, second.triangles);
                            Assert.That(first.bounds.min.y, Is.EqualTo(0f).Within(0.0001f));
                            if (previous != null)
                            {
                                Assert.That(
                                    first.triangles.Length,
                                    Is.LessThan(previous.triangles.Length),
                                    $"{shape}:{variant} LOD{lod} must simplify geometry.");
                                UnityEngine.Object.DestroyImmediate(previous);
                            }

                            previous = first;
                        }
                        finally
                        {
                            UnityEngine.Object.DestroyImmediate(second);
                        }
                    }

                    if (previous != null)
                    {
                        UnityEngine.Object.DestroyImmediate(previous);
                    }
                }
            }
        }

        [Test]
        public void StableIds_AreUniqueAcrossTheRepresentativeFamily()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (TopDown3DNaturalObjectShape shape in Enum.GetValues(typeof(TopDown3DNaturalObjectShape)))
            {
                for (var variant = 0; variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape; variant++)
                {
                    Assert.That(ids.Add(TopDown3DProductionRockBaker.GetStableId(shape, variant)), Is.True);
                }
            }
        }

        [Test]
        public void CatalogLookup_NormalizesPlannerVariantsToBakedFamilies()
        {
            var catalog = ScriptableObject.CreateInstance<TopDown3DNaturalObjectCatalog>();
            var meshes = new[]
            {
                TopDown3DProductionRockBaker.BuildMesh(TopDown3DNaturalObjectShape.Boulder, 1, 0),
                TopDown3DProductionRockBaker.BuildMesh(TopDown3DNaturalObjectShape.Boulder, 1, 1),
                TopDown3DProductionRockBaker.BuildMesh(TopDown3DNaturalObjectShape.Boulder, 1, 2)
            };
            try
            {
                var family = new TopDown3DNaturalMeshFamily();
                family.Configure(
                    TopDown3DProductionRockBaker.GetStableId(TopDown3DNaturalObjectShape.Boulder, 1),
                    TopDown3DNaturalObjectShape.Boulder,
                    1,
                    meshes[0],
                    meshes[1],
                    meshes[2],
                    meshes[0].bounds,
                    0.24f,
                    0.085f,
                    0.015f);
                catalog.ReplaceBakedMeshFamilies(new[] { family });

                Assert.That(catalog.TryGetMeshFamily(TopDown3DNaturalObjectShape.Boulder, 4, out var resolved), Is.True);
                Assert.That(resolved, Is.SameAs(family));
                Assert.That(catalog.GetRequiredMeshFamily(TopDown3DNaturalObjectShape.Boulder, 4), Is.SameAs(family));
                Assert.Throws<InvalidOperationException>(
                    () => catalog.GetRequiredMeshFamily(TopDown3DNaturalObjectShape.Cliff, 0));
            }
            finally
            {
                for (var i = 0; i < meshes.Length; i++)
                {
                    UnityEngine.Object.DestroyImmediate(meshes[i]);
                }

                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        private static void AssertMeshIsFinite(
            Mesh mesh,
            TopDown3DNaturalObjectShape shape,
            int variant,
            int lod)
        {
            Assert.That(mesh.vertexCount, Is.GreaterThan(0));
            Assert.That(mesh.triangles.Length, Is.GreaterThan(0));
            Assert.That(mesh.triangles.Length % 3, Is.Zero);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            Assert.That(normals.Length, Is.EqualTo(vertices.Length));
            for (var i = 0; i < vertices.Length; i++)
            {
                Assert.That(IsFinite(vertices[i]), Is.True, $"Non-finite vertex in {shape}:{variant} LOD{lod}.");
                Assert.That(IsFinite(normals[i]), Is.True, $"Non-finite normal in {shape}:{variant} LOD{lod}.");
                Assert.That(normals[i].sqrMagnitude, Is.EqualTo(1f).Within(0.0001f));
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
