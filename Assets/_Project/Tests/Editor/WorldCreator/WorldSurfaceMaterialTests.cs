using System;
using System.Threading.Tasks;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldSurfaceMaterialTests
    {
        private const string SettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";

        [Test]
        public void SemanticMaterialSample_IsDeterministicBoundedAndContextDriven()
        {
            using var runtime = WorldCreatorProductionRuntime.Create(3496479);
            var position = new AbsoluteWorldPosition(137.25d, 0d, -418.75d);
            Assert.That(runtime.Materials.TrySample(position, out var first, out var error), Is.True, error);
            Assert.That(runtime.Materials.TrySample(position, out var second, out error), Is.True, error);
            Assert.That(second, Is.EqualTo(first));
            AssertNormalized(first.StrataExposure);
            AssertNormalized(first.Erosion);
            AssertNormalized(first.WindExposure);
            AssertNormalized(first.Shelter);
            AssertNormalized(first.Deposit);
            AssertNormalized(first.Weathering);
            AssertNormalized(first.Sediment);
            AssertNormalized(first.StructuralDirection);
            AssertNormalized(first.PrevailingWindDirection);
            Assert.That(first.SlopeDegrees, Is.InRange(0f, 90f));
        }

        [Test]
        public void SharedBoundary_HasExactMaterialContinuity()
        {
            using var runtime = WorldCreatorProductionRuntime.Create(3496479);
            const double boundaryA = 32d * 17d;
            for (var b = -160d; b <= 160d; b += 8d)
            {
                var position = new AbsoluteWorldPosition(boundaryA, 0d, b);
                Assert.That(runtime.Materials.TrySample(position, out var left, out var error), Is.True, error);
                Assert.That(runtime.Materials.TrySample(position, out var right, out error), Is.True, error);
                Assert.That(right, Is.EqualTo(left));
                Assert.That(WorldTerrainMaterialPackingAdapter.Pack(right),
                    Is.EqualTo(WorldTerrainMaterialPackingAdapter.Pack(left)));
            }
        }

        [Test]
        public void CausalSurfaceResponse_DistinguishesExposedAndDepositionalGround()
        {
            var profile = WorldCreatorProductionProfile.LoadRequired();
            var identity = new WorldIdentity(3496479, profile.CreateVersionManifest());
            var coordinateModel = new NonCanonTechnicalCoordinateModel();
            var context = new WorldCoordinateContextSampler(
                profile.ProvinceCatalog,
                profile.StrataCatalog,
                profile.InfluenceProfile);
            var query = new UnboundedHybridWorldQueryService(
                identity,
                coordinateModel,
                context,
                CanyonPlannerProfile.CreateNonCanonTechnicalProofProfile());
            var materials = new WorldSurfaceMaterialService(
                identity,
                coordinateModel,
                context,
                query);
            var exposedCount = 0;
            var depositedCount = 0;
            var exposedStrata = 0f;
            var depositedStrata = 0f;
            var exposedDeposit = 0f;
            var depositedDeposit = 0f;
            for (var b = -640d; b <= 640d; b += 32d)
            {
                for (var a = -640d; a <= 640d; a += 32d)
                {
                    var position = new AbsoluteWorldPosition(a, 0d, b);
                    Assert.That(query.TrySampleSurface(position, out var surface, out var error), Is.True, error);
                    Assert.That(materials.TrySample(position, out var material, out error), Is.True, error);
                    if ((surface.Semantic & WorldSurfaceSemantic.CanyonWall) != 0)
                    {
                        exposedCount++;
                        exposedStrata += material.StrataExposure;
                        exposedDeposit += material.Deposit;
                    }
                    if ((surface.Semantic & (WorldSurfaceSemantic.CanyonFloor | WorldSurfaceSemantic.Buried)) != 0)
                    {
                        depositedCount++;
                        depositedStrata += material.StrataExposure;
                        depositedDeposit += material.Deposit;
                    }
                }
            }

            Assert.That(exposedCount, Is.GreaterThan(8));
            Assert.That(depositedCount, Is.GreaterThan(8));
            Assert.That(exposedStrata / exposedCount, Is.GreaterThan(depositedStrata / depositedCount));
            Assert.That(depositedDeposit / depositedCount, Is.GreaterThan(exposedDeposit / exposedCount));
        }

        [Test]
        public async Task NearMidFarRepresentations_ShareOneMaterialContract()
        {
            using var runtime = WorldCreatorProductionRuntime.Create(3496479);
            var keys = new[]
            {
                runtime.CreateRepresentationKey(WorldRepresentationTier.Near, 3, -2, 96d),
                runtime.CreateRepresentationKey(WorldRepresentationTier.Mid, 3, -2, 96d),
                runtime.CreateRepresentationKey(WorldRepresentationTier.Far, 3, -2, 96d)
            };
            await Task.WhenAll(
                runtime.RequestAsync(keys[0]),
                runtime.RequestAsync(keys[1]),
                runtime.RequestAsync(keys[2]));
            runtime.DrainIntegrationQueue(3, TimeSpan.FromMilliseconds(100d));
            Assert.That(runtime.TryGetIntegrated(keys[0], out var near), Is.True);
            Assert.That(runtime.TryGetIntegrated(keys[1], out var mid), Is.True);
            Assert.That(runtime.TryGetIntegrated(keys[2], out var far), Is.True);

            AssertCornerContract(near, mid);
            AssertCornerContract(near, far);
            Assert.That(near.HasCollision, Is.True);
            Assert.That(mid.HasCollision, Is.False);
            Assert.That(far.HasCollision, Is.False);
        }

        [Test]
        public void RepresentationIdentity_ChangesWithMaterialVersion()
        {
            using var runtime = WorldCreatorProductionRuntime.Create(3496479);
            var versions = runtime.Identity.Versions;
            var changedMaterialWorld = new WorldIdentity(
                runtime.Identity.Seed,
                new WorldVersionManifest(
                    versions.Topology,
                    versions.Coordinate,
                    versions.Landform,
                    versions.Material + 1,
                    versions.Decoration,
                    versions.Resource,
                    versions.Site));
            var current = new WorldRepresentationKey(
                runtime.Identity,
                runtime.CoordinateModel,
                WorldRepresentationTier.Near,
                4,
                -7,
                32d);
            var changed = new WorldRepresentationKey(
                changedMaterialWorld,
                runtime.CoordinateModel,
                WorldRepresentationTier.Near,
                4,
                -7,
                32d);
            Assert.That(changed.StableId, Is.Not.EqualTo(current.StableId));
        }

        [Test]
        public void SemanticDustPlan_IsContinuousAndRebaseStable()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(SettingsPath);
            var exclusion = new Vector2(10000f, 10000f);
            var fixedGenerator = new TopDown3DWorldGenerator(settings);
            var left = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                fixedGenerator,
                settings.NaturalObjectCatalog,
                Vector2Int.zero,
                exclusion);
            var right = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                fixedGenerator,
                settings.NaturalObjectCatalog,
                Vector2Int.right,
                exclusion);
            for (var z = 0; z < left.VerticesPerAxis; z++)
                Assert.That(right.GetSample(0, z), Is.EqualTo(left.GetSample(left.QuadsPerAxis, z)));

            using var runtime = WorldCreatorProductionRuntime.Create(settings.WorldSeed);
            var before = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                new TopDown3DWorldGenerator(settings, runtime),
                settings.NaturalObjectCatalog,
                new Vector2Int(4, -3),
                exclusion);
            Assert.That(runtime.TryRebase(new AbsoluteWorldPosition(512d, 0d, -384d)), Is.True);
            var after = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                new TopDown3DWorldGenerator(settings, runtime),
                settings.NaturalObjectCatalog,
                new Vector2Int(4, -3),
                exclusion);
            for (var z = 0; z < before.VerticesPerAxis; z++)
                for (var x = 0; x < before.VerticesPerAxis; x++)
                    Assert.That(after.GetSample(x, z), Is.EqualTo(before.GetSample(x, z)));
        }

        private static void AssertCornerContract(
            WorldRepresentationBuildResult expected,
            WorldRepresentationBuildResult actual)
        {
            var expectedCorners = new[]
            {
                0,
                expected.Resolution - 1,
                expected.VertexCount - expected.Resolution,
                expected.VertexCount - 1
            };
            var actualCorners = new[]
            {
                0,
                actual.Resolution - 1,
                actual.VertexCount - actual.Resolution,
                actual.VertexCount - 1
            };
            for (var i = 0; i < expectedCorners.Length; i++)
            {
                var expectedMaterial = expected.GetMaterial(expectedCorners[i]);
                var actualMaterial = actual.GetMaterial(actualCorners[i]);
                Assert.That(actualMaterial, Is.EqualTo(expectedMaterial));
                Assert.That(WorldTerrainMaterialPackingAdapter.Pack(actualMaterial),
                    Is.EqualTo(WorldTerrainMaterialPackingAdapter.Pack(expectedMaterial)));
            }
        }

        private static void AssertNormalized(float value)
        {
            Assert.That(value, Is.InRange(0f, 1f));
        }
    }
}
