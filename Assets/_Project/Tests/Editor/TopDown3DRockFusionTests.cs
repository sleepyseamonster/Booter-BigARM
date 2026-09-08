using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRockFusionTests
    {
        private const string WorldSettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        private static readonly Vector2 DistantExclusion = new Vector2(10000f, 10000f);

        [Test]
        public void EveryBakedRockVariant_NormalizesToPositiveClosedManifold()
        {
            var catalog = LoadSettings().NaturalObjectCatalog;
            foreach (TopDown3DNaturalObjectShape shape in
                     System.Enum.GetValues(typeof(TopDown3DNaturalObjectShape)))
            {
                for (var variant = 0;
                     variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape;
                     variant++)
                {
                    var topology = TopDown3DRockMeshTopology.Get(catalog, shape, variant);
                    var report = TopDown3DRockMeshTopology.Validate(topology);
                    Assert.That(report.IsValid, Is.True, $"{shape}:{variant}: {report.Error}");
                    Assert.That(report.SignedVolume, Is.GreaterThan(0.0), $"{shape}:{variant}");
                    Assert.That(report.VertexCount, Is.LessThan(topology.Triangles.Length));
                }
            }
        }

        [Test]
        public void NativeBatchUnion_ProducesOneWatertightCubeSolid()
        {
            Assert.That(TopDown3DManifoldNative.IsSupportedPlatform, Is.True);
            var inputs = new[]
            {
                CreateCube(Vector3.zero),
                CreateCube(new Vector3(0.5f, 0f, 0f))
            };

            Assert.That(
                TopDown3DManifoldNative.TryUnionMany(
                    inputs,
                    out var result,
                    out var volume,
                    out var error),
                Is.True,
                error);
            Assert.That(volume, Is.EqualTo(1.5).Within(1e-9));
            var report = TopDown3DRockMeshTopology.Validate(result);
            Assert.That(report.IsValid, Is.True, report.Error);
            Assert.That(report.SignedVolume, Is.GreaterThan(0.0));
        }

        [Test]
        public void NativeFloatConversion_RemovesOnlyExactlyCollapsedFaces()
        {
            var cube = CreateCube(Vector3.zero);
            var vertices = cube.Vertices.Concat(new[] { cube.Vertices[0] }).ToArray();
            var triangles = cube.Triangles.Concat(new[] { 0, 8, 1 }).ToArray();
            var expected = TopDown3DRockMeshTopology.Normalize(cube.Vertices, cube.Triangles);
            var result = TopDown3DManifoldNative.NormalizeConvertedMesh(vertices, triangles);

            Assert.That(result.Vertices, Is.EqualTo(expected.Vertices));
            Assert.That(result.Triangles, Is.EqualTo(expected.Triangles));
            var report = TopDown3DRockMeshTopology.Validate(result);
            Assert.That(report.IsValid, Is.True, report.Error);
            Assert.That(report.SignedVolume, Is.EqualTo(1d).Within(1e-9));
        }

        [Test]
        public void NativeFloatConversion_PreservesRepresentableSlivers()
        {
            var cube = CreateCube(Vector3.zero);
            var vertices = cube.Vertices.Concat(new[] { cube.Vertices[0] + Vector3.up * 0.000001f }).ToArray();
            var triangles = cube.Triangles.Concat(new[] { 0, 8, 1 }).ToArray();
            var result = TopDown3DManifoldNative.NormalizeConvertedMesh(vertices, triangles);

            Assert.That(result.Triangles.Length, Is.EqualTo(triangles.Length));
            Assert.That(result.Vertices.Length, Is.EqualTo(vertices.Length));
        }

        [Test]
        public void NativeFloatConversion_DoesNotHideAnOpenSurface()
        {
            var cube = CreateCube(Vector3.zero);
            var triangles = cube.Triangles.Skip(3).Concat(new[] { 0, 0, 1 }).ToArray();
            var result = TopDown3DManifoldNative.NormalizeConvertedMesh(cube.Vertices, triangles);

            Assert.That(TopDown3DRockMeshTopology.Validate(result).IsValid, Is.False);
        }

        [Test]
        public void SeededPrecisionBoundary_ProducesAClosedUnion()
        {
            var settings = LoadSettings();
            var formation = TopDown3DRockFormationPlanner.BuildPhysicalFormations(
                    settings, new TopDown3DWorldGenerator(settings), settings.NaturalObjectCatalog,
                    new Vector2Int(3, -3), DistantExclusion)
                .First(candidate => candidate.StableId.EndsWith(":ExtraLarge:4:-4"));
            var root = formation.Members[0];
            var solids = TopDown3DRockFormationMeshBuilder.PrepareUnionSolids(
                formation, settings.NaturalObjectCatalog,
                Matrix4x4.TRS(root.Position, root.Rotation, Vector3.one).inverse);

            Assert.That(TopDown3DManifoldNative.TryUnionMany(
                new[] { solids[0], solids[1] }, out var result, out _, out var error), Is.True, error);
            Assert.That(TopDown3DRockMeshTopology.Validate(result).IsValid, Is.True);
        }

        [Test]
        public void FusionWorker_DropsCompletionAfterChunkInvalidation()
        {
            using (var service = new TopDown3DRockFusionService())
            {
                var applied = false;
                service.Enqueue(
                    "worker-invalidation-probe",
                    0,
                    0,
                    0,
                    new[]
                    {
                        CreateCube(Vector3.zero),
                        CreateCube(new Vector3(0.5f, 0f, 0f))
                    },
                    41L,
                    () => false,
                    (_, __) => applied = true,
                    _ => applied = true);

                WaitForCompletions(service);
                Assert.That(service.OutstandingCount, Is.Zero);
                Assert.That(applied, Is.False);
            }
        }

        [Test]
        public void FusionWorker_ChunkUnloadReloadAppliesOnlyNewGeneration()
        {
            using (var service = new TopDown3DRockFusionService())
            {
                var oldGenerationApplied = false;
                var newGenerationApplied = false;
                var solids = new[]
                {
                    CreateCube(Vector3.zero),
                    CreateCube(new Vector3(0.5f, 0f, 0f))
                };
                service.Enqueue(
                    "worker-unloaded-generation",
                    0,
                    4,
                    -2,
                    solids,
                    41L,
                    () => true,
                    (_, __) => oldGenerationApplied = true,
                    _ => oldGenerationApplied = true);
                service.CancelChunk(41L);
                service.Enqueue(
                    "worker-reloaded-generation",
                    0,
                    4,
                    -2,
                    solids,
                    42L,
                    () => true,
                    (_, __) => newGenerationApplied = true,
                    _ => newGenerationApplied = false);

                WaitForCompletions(service);
                Assert.That(oldGenerationApplied, Is.False);
                Assert.That(newGenerationApplied, Is.True);
            }
        }

        [Test]
        // Detailed approved meshes make the unchanged legacy-planner fixture expensive.
        // Keep all 48 formations and every assertion; only override the runner's 3-minute default.
        [Timeout(15 * 60 * 1000)]
        public void SeededMultiMemberFormations_FuseDeterministicallyIntoOneComponent()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.NaturalObjectCatalog, Is.Not.Null);

            var formations = CollectFormations(settings, -3, 3)
                .Where(formation => formation.Members.Count > 1)
                .Take(48)
                .ToArray();
            Assert.That(formations, Is.Not.Empty);

            var stopwatch = Stopwatch.StartNew();
            foreach (var formation in formations)
            {
                var root = formation.Members[0];
                var worldToRoot = Matrix4x4.TRS(
                    root.Position,
                    root.Rotation,
                    Vector3.one).inverse;
                var solids = TopDown3DRockFormationMeshBuilder.PrepareUnionSolids(
                    formation,
                    settings.NaturalObjectCatalog,
                    worldToRoot);
                for (var memberIndex = 1;
                     memberIndex < formation.Members.Count;
                     memberIndex++)
                {
                    var member = formation.Members[memberIndex];
                    Assert.That(
                        TopDown3DManifoldNative.TryUnionMany(
                            new[] { solids[member.ParentIndex], solids[memberIndex] },
                            out _,
                            out _,
                            out var edgeError),
                        Is.True,
                        $"{formation.StableId} edge {member.ParentIndex}->{memberIndex}: " +
                        $"{edgeError}\n{DescribeFormation(formation)}");
                }

                var buildSucceeded = TopDown3DRockFormationMeshBuilder.TryBuild(
                    formation,
                    settings.NaturalObjectCatalog,
                    worldToRoot,
                    out var fused,
                    out var error);
                Assert.That(
                    buildSucceeded,
                    Is.True,
                    $"{formation.StableId}: {error}\n{DescribeFormation(formation)}\n" +
                    DescribeNativePairGraph(formation, solids));

                var report = TopDown3DRockMeshTopology.Validate(
                    new TopDown3DIndexedMeshData(fused.Vertices, fused.Triangles));
                Assert.That(report.IsValid, Is.True, $"{formation.StableId}: {report.Error}");
                Assert.That(report.SignedVolume, Is.GreaterThan(0.0));
                Assert.That(fused.Normals.All(IsFiniteUnitNormal), Is.True, formation.StableId);

                var firstHash = TopDown3DRockFormationMeshBuilder.ComputeDeterministicHash(fused);
                Assert.That(
                    TopDown3DRockFormationMeshBuilder.TryBuild(
                        formation,
                        settings.NaturalObjectCatalog,
                        worldToRoot,
                        out var repeated,
                        out error),
                    Is.True,
                    error);
                Assert.That(
                    TopDown3DRockFormationMeshBuilder.ComputeDeterministicHash(repeated),
                    Is.EqualTo(firstHash));
            }

            stopwatch.Stop();
            TestContext.WriteLine(
                $"Fused {formations.Length} seeded formations in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        }

        [Test]
        public void SingleMemberFormation_PreservesSourceTriangleSoup()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            var formation = CollectFormations(settings, -2, 2)
                .First(candidate => candidate.Members.Count == 1);
            var root = formation.Members[0];
            var worldToRoot = Matrix4x4.TRS(
                root.Position,
                root.Rotation,
                Vector3.one).inverse;
            var source = settings.NaturalObjectCatalog.GetRequiredLod0Data(
                root.Shape,
                root.Variant);

            Assert.That(
                TopDown3DRockFormationMeshBuilder.TryBuild(
                    formation,
                    settings.NaturalObjectCatalog,
                    worldToRoot,
                    out var result,
                    out var error),
                Is.True,
                error);
            Assert.That(result.Vertices.Length, Is.EqualTo(source.Vertices.Length));
            Assert.That(result.Normals.Length, Is.EqualTo(source.Normals.Length));
            Assert.That(result.Triangles, Is.EqualTo(source.Triangles));
        }

        private static IEnumerable<TopDown3DRockFormationPlan> CollectFormations(
            TopDown3DWorldSettings settings,
            int minimumCoordinate,
            int maximumCoordinate)
        {
            for (var z = minimumCoordinate; z <= maximumCoordinate; z++)
            {
                for (var x = minimumCoordinate; x <= maximumCoordinate; x++)
                {
                    var formations = TopDown3DRockFormationPlanner.BuildPhysicalFormations(
                        settings,
                        new TopDown3DWorldGenerator(settings),
                        settings.NaturalObjectCatalog,
                        new Vector2Int(x, z),
                        DistantExclusion);
                    for (var i = 0; i < formations.Count; i++)
                    {
                        yield return formations[i];
                    }
                }
            }
        }

        private static TopDown3DWorldSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.NaturalObjectCatalog, Is.Not.Null);
            return settings;
        }

        private static TopDown3DIndexedMeshData CreateCube(Vector3 offset)
        {
            var vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f) + offset,
                new Vector3(0.5f, -0.5f, -0.5f) + offset,
                new Vector3(0.5f, 0.5f, -0.5f) + offset,
                new Vector3(-0.5f, 0.5f, -0.5f) + offset,
                new Vector3(-0.5f, -0.5f, 0.5f) + offset,
                new Vector3(0.5f, -0.5f, 0.5f) + offset,
                new Vector3(0.5f, 0.5f, 0.5f) + offset,
                new Vector3(-0.5f, 0.5f, 0.5f) + offset
            };
            var triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5
            };
            return new TopDown3DIndexedMeshData(vertices, triangles);
        }

        private static string DescribeFormation(TopDown3DRockFormationPlan formation)
        {
            return string.Join(
                "\n",
                formation.Members.Select(member =>
                    $"member={member.MemberIndex} parent={member.ParentIndex} " +
                    $"tier={member.Tier} shape={member.Shape}:{member.Variant} " +
                    $"position=({member.Position.x:R},{member.Position.y:R},{member.Position.z:R}) " +
                    $"scale=({member.Scale.x:R},{member.Scale.y:R},{member.Scale.z:R}) " +
                    $"support={member.SupportRadius:R}"));
        }

        private static string DescribeNativePairGraph(
            TopDown3DRockFormationPlan formation,
            IReadOnlyList<TopDown3DIndexedMeshData> solids)
        {
            var connectedPairs = new List<string>();
            for (var first = 0; first < solids.Count; first++)
            {
                for (var second = first + 1; second < solids.Count; second++)
                {
                    if (TopDown3DManifoldNative.TryUnionMany(
                            new[] { solids[first], solids[second] },
                            out _,
                            out _,
                            out _))
                    {
                        var relationship = formation.Members[second].ParentIndex == first
                            ? "parent"
                            : "non-parent";
                        connectedPairs.Add($"{first}-{second} ({relationship})");
                    }
                }
            }

            return "Manifold-connected input pairs: " + string.Join(", ", connectedPairs);
        }

        private static bool IsFiniteUnitNormal(Vector3 normal)
        {
            return !float.IsNaN(normal.x) && !float.IsInfinity(normal.x)
                && !float.IsNaN(normal.y) && !float.IsInfinity(normal.y)
                && !float.IsNaN(normal.z) && !float.IsInfinity(normal.z)
                && Mathf.Abs(normal.magnitude - 1f) <= 0.001f;
        }

        private static void WaitForCompletions(TopDown3DRockFusionService service)
        {
            var timeout = Stopwatch.StartNew();
            while (service.OutstandingCount > 0 && timeout.Elapsed.TotalSeconds < 10.0)
            {
                service.TryApplyOneCompletion();
                System.Threading.Thread.Sleep(1);
            }

            Assert.That(service.OutstandingCount, Is.Zero, "Timed out waiting for rock fusion worker.");
        }
    }
}
