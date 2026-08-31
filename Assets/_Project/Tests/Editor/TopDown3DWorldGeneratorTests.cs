using System;
using System.IO;
using System.Threading.Tasks;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DWorldGeneratorTests
    {
        private const string WorldSettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        private const string GeneratorSourcePath =
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DWorldGenerator.cs";

        [Test]
        public void ProductionProfile_IsExplicitlyNonCanonAndTopologyV2()
        {
            var profile = WorldCreatorProductionProfile.LoadRequired();

            Assert.That(profile.NonCanonProofOnly, Is.True);
            Assert.That(profile.IncludeCanyonsInInitialPlayableArea, Is.False);
            Assert.That(profile.InfluenceProfile.StableId, Does.StartWith("proof.non-canon."));
            Assert.That(profile.CreateVersionManifest().Topology,
                Is.EqualTo(WorldCreatorProductionProfile.CurrentTopologyVersion));
            Assert.That(profile.CreateVersionManifest().Landform,
                Is.EqualTo(WorldCreatorProductionProfile.CurrentLandformVersion));
            Assert.That(profile.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void InitialPlayableArea_SuppressesCanyonsWithoutFlatteningTerrain()
        {
            using var runtime = WorldCreatorProductionRuntime.Create(LoadSettings().WorldSeed);
            const int size = 21;
            const double spacing = 18d;
            var heights = new double[size, size];
            var minimum = double.MaxValue;
            var maximum = double.MinValue;
            var canyonSemantics = WorldSurfaceSemantic.CanyonFloor
                | WorldSurfaceSemantic.CanyonShelf
                | WorldSurfaceSemantic.CanyonWall;
            for (var z = 0; z < size; z++)
            {
                for (var x = 0; x < size; x++)
                {
                    var position = new AbsoluteWorldPosition(
                        (x - size / 2) * spacing,
                        0d,
                        (z - size / 2) * spacing);
                    Assert.That(runtime.Query.TrySampleSurface(position, out var sample, out var error), Is.True, error);
                    Assert.That(sample.Semantic & canyonSemantics, Is.EqualTo(WorldSurfaceSemantic.None));
                    heights[x, z] = sample.Position.Vertical;
                    minimum = Math.Min(minimum, sample.Position.Vertical);
                    maximum = Math.Max(maximum, sample.Position.Vertical);
                }
            }

            var localHighs = 0;
            var localLows = 0;
            for (var z = 1; z < size - 1; z++)
            {
                for (var x = 1; x < size - 1; x++)
                {
                    var center = heights[x, z];
                    if (center > heights[x - 1, z]
                        && center > heights[x + 1, z]
                        && center > heights[x, z - 1]
                        && center > heights[x, z + 1])
                    {
                        localHighs++;
                    }

                    if (center < heights[x - 1, z]
                        && center < heights[x + 1, z]
                        && center < heights[x, z - 1]
                        && center < heights[x, z + 1])
                    {
                        localLows++;
                    }
                }
            }

            Assert.That(maximum - minimum, Is.GreaterThan(12d));
            Assert.That(localHighs, Is.GreaterThan(0));
            Assert.That(localLows, Is.GreaterThan(0));
        }

        [Test]
        public void SurfaceSample_IsDeterministicAndOwnedByWorldQueryService()
        {
            var settings = LoadSettings();
            var firstGenerator = new TopDown3DWorldGenerator(settings);
            var secondGenerator = new TopDown3DWorldGenerator(settings);

            var first = firstGenerator.Sample(137.25f, -418.75f);
            var second = secondGenerator.Sample(137.25f, -418.75f);

            Assert.That(firstGenerator.QueryService, Is.TypeOf<UnboundedHybridWorldQueryService>());
            Assert.That(firstGenerator.GenerationVersion,
                Is.EqualTo(WorldCreatorProductionProfile.CurrentTopologyVersion));
            Assert.That(second.Height, Is.EqualTo(first.Height));
            Assert.That(second.Normal, Is.EqualTo(first.Normal));
            Assert.That(second.FeatureId, Is.EqualTo(first.FeatureId));
            Assert.That(first.Normal.sqrMagnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void NeighboringChunkBoundary_UsesTheExactSameAbsoluteSample()
        {
            var settings = LoadSettings();
            var generator = new TopDown3DWorldGenerator(settings);
            var boundaryX = settings.ChunkSize * 11f;
            var worldZ = settings.ChunkSize * -7.35f;

            var leftOwner = generator.Sample(boundaryX, worldZ);
            var rightOwner = generator.Sample(boundaryX, worldZ);

            Assert.That(rightOwner.Height, Is.EqualTo(leftOwner.Height));
            Assert.That(rightOwner.Normal, Is.EqualTo(leftOwner.Normal));
            Assert.That(rightOwner.FeatureId, Is.EqualTo(leftOwner.FeatureId));
        }

        [Test]
        public void FarAbsoluteCoordinates_RemainDeterministicAndFinite()
        {
            var generator = new TopDown3DWorldGenerator(LoadSettings());
            var position = new AbsoluteWorldPosition(8_000_000d, 0d, -6_000_000d);

            Assert.That(generator.QueryService.TrySampleSurface(
                position,
                out var first,
                out var firstError), Is.True, firstError);
            Assert.That(generator.QueryService.TrySampleSurface(
                position,
                out var second,
                out var secondError), Is.True, secondError);

            Assert.That(double.IsNaN(first.Position.Vertical)
                || double.IsInfinity(first.Position.Vertical), Is.False);
            Assert.That(second.Position.Vertical, Is.EqualTo(first.Position.Vertical));
            Assert.That(second.DominantFeatureId, Is.EqualTo(first.DominantFeatureId));
        }

        [Test]
        public async Task RepresentationScheduler_ProducesNearCollisionAndNonCollidingFarData()
        {
            var settings = LoadSettings();
            using var runtime = WorldCreatorProductionRuntime.Create(settings.WorldSeed);
            var near = runtime.CreateRepresentationKey(
                WorldRepresentationTier.Near,
                0,
                0,
                settings.ChunkSize);
            var far = runtime.CreateRepresentationKey(
                WorldRepresentationTier.Far,
                0,
                0,
                settings.ChunkSize * 16d);

            var outcomes = await Task.WhenAll(runtime.RequestAsync(near), runtime.RequestAsync(far));
            Assert.That(outcomes, Has.All.Matches<WorldRepresentationRequestOutcome>(
                outcome => outcome.State == WorldRepresentationRequestState.QueuedForIntegration));
            Assert.That(runtime.DrainIntegrationQueue(4, TimeSpan.FromMilliseconds(50d)), Is.EqualTo(2));
            Assert.That(runtime.TryGetIntegrated(near, out var nearResult), Is.True);
            Assert.That(runtime.TryGetIntegrated(far, out var farResult), Is.True);
            Assert.That(nearResult.HasCollision, Is.True);
            Assert.That(farResult.HasCollision, Is.False);
            Assert.That(nearResult.SourceFingerprint, Is.EqualTo(runtime.SourceFingerprint));
            Assert.That(farResult.SourceFingerprint, Is.EqualTo(runtime.SourceFingerprint));
        }

        [Test]
        public async Task OriginRebase_PreservesAbsoluteRepresentationIdentity()
        {
            var settings = LoadSettings();
            using var runtime = WorldCreatorProductionRuntime.Create(settings.WorldSeed);
            var key = runtime.CreateRepresentationKey(
                WorldRepresentationTier.Near,
                42,
                -37,
                settings.ChunkSize);
            var outcome = await runtime.RequestAsync(key);
            Assert.That(outcome.State, Is.EqualTo(WorldRepresentationRequestState.QueuedForIntegration));
            runtime.DrainIntegrationQueue(2, TimeSpan.FromMilliseconds(50d));
            Assert.That(runtime.TryGetIntegrated(key, out var before), Is.True);
            var stableId = before.Key.StableId;

            var nextOrigin = new AbsoluteWorldPosition(720d, 0d, -540d);
            Assert.That(runtime.TryRebase(nextOrigin), Is.True);
            Assert.That(runtime.TryGetIntegrated(key, out var after), Is.True);
            Assert.That(after.Key.StableId, Is.EqualTo(stableId));
            Assert.That(after.LocalFrame.OriginPosition, Is.EqualTo(nextOrigin));
        }

        [Test]
        public async Task DistantRebase_EvictsDisposableLocalDataWithoutChangingAbsoluteKeys()
        {
            var settings = LoadSettings();
            using var runtime = WorldCreatorProductionRuntime.Create(settings.WorldSeed);
            var key = runtime.CreateRepresentationKey(
                WorldRepresentationTier.Near,
                0,
                0,
                settings.ChunkSize);
            await runtime.RequestAsync(key);
            runtime.DrainIntegrationQueue(2, TimeSpan.FromMilliseconds(50d));
            Assert.That(runtime.TryGetIntegrated(key, out _), Is.True);

            Assert.That(runtime.TryRebase(new AbsoluteWorldPosition(1_000_000d, 0d, -1_000_000d)), Is.True);
            Assert.That(runtime.TryGetIntegrated(key, out _), Is.False);
            var rebuiltKey = runtime.CreateRepresentationKey(
                WorldRepresentationTier.Near,
                0,
                0,
                settings.ChunkSize);
            Assert.That(rebuiltKey.StableId, Is.EqualTo(key.StableId));
        }

        [Test]
        public void PrototypeSaveCompatibility_RejectsTopologyV1AndWrongWorld()
        {
            var legacy = JsonUtility.FromJson<TopDown3DGameStateSnapshot>(
                "{\"version\":1,\"worldSeed\":24681357}");
            var current = TopDown3DGameStateSnapshot.Create(
                24681357,
                WorldCreatorProductionProfile.CurrentTopologyVersion,
                Vector3.zero,
                null,
                null);

            Assert.That(legacy.IsCompatible(
                24681357,
                WorldCreatorProductionProfile.CurrentTopologyVersion), Is.False);
            Assert.That(current.IsCompatible(
                24681357,
                WorldCreatorProductionProfile.CurrentTopologyVersion), Is.True);
            Assert.That(current.IsCompatible(
                1,
                WorldCreatorProductionProfile.CurrentTopologyVersion), Is.False);
        }

        [Test]
        public void ProductionGeneratorAdapter_ContainsNoLegacyMacroTerrainAlgorithm()
        {
            var source = File.ReadAllText(GeneratorSourcePath);

            Assert.That(source, Does.Not.Contain("SampleCore"));
            Assert.That(source, Does.Not.Contain("FractalNoise"));
            Assert.That(source, Does.Not.Contain("Mathf.PerlinNoise"));
            Assert.That(source, Does.Not.Contain("TopDown3DGeologyProfile"));
            Assert.That(source, Does.Contain("IWorldQueryService"));
        }

        private static TopDown3DWorldSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            return settings;
        }
    }
}
