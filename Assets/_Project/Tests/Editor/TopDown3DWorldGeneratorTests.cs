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
        public void ProductionProfile_KeepsSeededGentlePrototypeStart()
        {
            var profile = WorldCreatorProductionProfile.LoadRequired();

            Assert.That(profile.NonCanonProofOnly, Is.True);
            Assert.That(profile.IncludeCanyonsInInitialPlayableArea, Is.False);
            Assert.That(profile.GentlePrototypeRadius, Is.EqualTo(360f));
            Assert.That(profile.GentlePrototypeTransition, Is.EqualTo(120f));
            Assert.That(profile.TryGetCanyonShowcasePosition(24681357, out var showcase, out var yaw, out var pitch), Is.True);
            Assert.That(showcase.x, Is.EqualTo(488f).Within(0.01f));
            Assert.That(showcase.y, Is.EqualTo(312f).Within(0.01f));
            Assert.That(yaw, Is.EqualTo(270f));
            Assert.That(pitch, Is.EqualTo(26f));
            Assert.That(profile.TryGetCanyonShowcasePosition(24681358, out _, out _, out _), Is.False);
            Assert.That(profile.InfluenceProfile.StableId, Does.StartWith("proof.non-canon."));
            Assert.That(profile.CreateVersionManifest().Topology,
                Is.EqualTo(WorldCreatorProductionProfile.CurrentTopologyVersion));
            Assert.That(profile.CreateVersionManifest().Landform,
                Is.EqualTo(WorldCreatorProductionProfile.CurrentLandformVersion));
            Assert.That(profile.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void InitialPlayableArea_HasGentleNoiseInsteadOfDeepCanyon()
        {
            using var runtime = WorldCreatorProductionRuntime.Create(LoadSettings().WorldSeed);
            var flatQuery = new UnboundedHybridWorldQueryService(runtime.Identity,
                runtime.CoordinateModel, runtime.ContextProvider,
                CanyonPlannerProfile.CreateNonCanonTechnicalProofProfile(),
                historyProvider: new NonCanonProofHistoryProvider(runtime.Identity,
                    runtime.CoordinateModel, runtime.NonCanonProofHistory),
                includeCanyonExcavation: false);
            var fullQuery = new UnboundedHybridWorldQueryService(runtime.Identity,
                runtime.CoordinateModel, runtime.ContextProvider,
                CanyonPlannerProfile.CreateNonCanonTechnicalProofProfile(),
                historyProvider: new NonCanonProofHistoryProvider(runtime.Identity,
                    runtime.CoordinateModel, runtime.NonCanonProofHistory));
            var center = new AbsoluteWorldPosition(488d, 0d, 222d);
            var rim = new AbsoluteWorldPosition(488d, 0d, 312d);
            Assert.That(runtime.Query.TrySampleSurface(center, out var floor, out var error),
                Is.True, error);
            Assert.That(runtime.Query.TrySampleSurface(rim, out var shoulder, out error),
                Is.True, error);
            Assert.That(floor.Semantic & (WorldSurfaceSemantic.CanyonFloor
                | WorldSurfaceSemantic.CanyonShelf | WorldSurfaceSemantic.CanyonWall),
                Is.EqualTo(WorldSurfaceSemantic.None));
            Assert.That(shoulder.Semantic & (WorldSurfaceSemantic.CanyonFloor
                | WorldSurfaceSemantic.CanyonShelf | WorldSurfaceSemantic.CanyonWall),
                Is.EqualTo(WorldSurfaceSemantic.None));
            Assert.That(flatQuery.TrySampleSurface(center, out var originalNoise,
                out error), Is.True, error);
            Assert.That(floor.Position.Vertical,
                Is.EqualTo(originalNoise.Position.Vertical).Within(0.00001d));
            var outside = new AbsoluteWorldPosition(1088d, 0d, 312d);
            Assert.That(runtime.Query.TrySampleSurface(outside, out var outerSurface,
                out error), Is.True, error);
            Assert.That(fullQuery.TrySampleSurface(outside, out var originalCanyon,
                out error), Is.True, error);
            Assert.That(outerSurface.Position.Vertical,
                Is.EqualTo(originalCanyon.Position.Vertical).Within(0.00001d));
            Assert.That(outerSurface.Semantic, Is.EqualTo(originalCanyon.Semantic));
            Assert.That(Math.Abs(shoulder.Position.Vertical - floor.Position.Vertical),
                Is.LessThan(12d));
            Assert.That(runtime.Query.TrySampleAffordance(rim, WorldAgentProfile.BooterProof,
                out var affordance, out error), Is.True, error);
            Assert.That(affordance.Walkable, Is.True);
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
