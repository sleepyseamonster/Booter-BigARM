using System;
using System.Reflection;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldPersistenceTests
    {
        [Test]
        public void SavedPlaceCodec_IsDeterministicAndRoundTrips()
        {
            var record = CreateRecord();

            var firstPayload = SavedPlaceRecordCodec.Encode(record);
            var secondPayload = SavedPlaceRecordCodec.Encode(record);

            CollectionAssert.AreEqual(firstPayload, secondPayload);
            Assert.That(SavedPlaceRecordCodec.TryDecode(firstPayload, out var decoded, out var error), Is.True, error);
            Assert.That(decoded, Is.EqualTo(record));
            Assert.That(decoded.PlayerName, Is.EqualTo("The Long Descent"));
            Assert.That(decoded.ThematicCoordinate.HasValue, Is.True);
            Assert.That(decoded.FeatureReferences.Count, Is.EqualTo(2));
        }

        [Test]
        public void SavedPlaceCodec_MigratesLegacyV1WithoutInventingThematicCoordinates()
        {
            var record = CreateRecord();
            var encoder = typeof(SavedPlaceRecordCodec).GetMethod(
                "EncodeLegacyV1ForTests",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(encoder, Is.Not.Null);
            var payload = (byte[])encoder.Invoke(null, new object[] { record });

            Assert.That(SavedPlaceRecordCodec.TryDecode(payload, out var migrated, out var error), Is.True, error);
            Assert.That(migrated.PlayerName, Is.EqualTo("Saved place"));
            Assert.That(migrated.ThematicCoordinate.HasValue, Is.False);
            Assert.That(migrated.HasAnchorFeature, Is.True);
            Assert.That(migrated.AbsolutePosition, Is.EqualTo(default(AbsoluteWorldPosition)));
        }

        [Test]
        public void PersistenceManifest_RejectsTopologyMismatchWithTypedReason()
        {
            var model = new NonCanonCoordinateModel();
            var persistedWorld = WorldCreatorTestFactory.CreateWorld(topology: 4);
            var currentWorld = WorldCreatorTestFactory.CreateWorld(topology: 5);
            var manifest = WorldPersistenceManifest.CreateCurrent(persistedWorld, model);

            var result = manifest.CheckCompatibility(currentWorld, model);

            Assert.That(result.IsCompatible, Is.False);
            Assert.That(result.Issue, Is.EqualTo(WorldCompatibilityIssue.TopologyVersionMismatch));
            Assert.That(result.Message, Is.Not.Empty);
        }

        [Test]
        public void PersistenceManifest_RejectsCoordinateModelSubstitutionExplicitly()
        {
            var persistedModel = new NonCanonCoordinateModel("test.non-canon.persisted", 3);
            var currentModel = new NonCanonCoordinateModel("test.non-canon.current", 3);
            var world = WorldCreatorTestFactory.CreateWorld();
            var manifest = WorldPersistenceManifest.CreateCurrent(world, persistedModel);

            var result = manifest.CheckCompatibility(world, currentModel);

            Assert.That(result.IsCompatible, Is.False);
            Assert.That(result.Issue, Is.EqualTo(WorldCompatibilityIssue.CoordinateModelMismatch));
        }

        [Test]
        public void PersistenceManifest_RejectsSiteDomainChangeExplicitly()
        {
            var model = new NonCanonCoordinateModel();
            var persistedWorld = WorldCreatorTestFactory.CreateWorld(site: 1);
            var currentWorld = WorldCreatorTestFactory.CreateWorld(site: 2);
            var manifest = WorldPersistenceManifest.CreateCurrent(persistedWorld, model);

            var result = manifest.CheckCompatibility(currentWorld, model);

            Assert.That(result.IsCompatible, Is.False);
            Assert.That(result.Issue, Is.EqualTo(WorldCompatibilityIssue.SiteVersionMismatch));
            Assert.That(result.Message, Does.Contain("Site"));
        }

        [Test]
        public void SavedPlaceIdentityAndAddress_SurviveLocalOriginRebase()
        {
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var absolute = new AbsoluteWorldPosition(8_000_000_000_100d, 20d, -8_000_000_000_200d);
            var address = model.Encode(absolute);
            var firstOrigin = new AbsoluteWorldPosition(8_000_000_000_000d, 0d, -8_000_000_000_000d);
            var secondOrigin = new AbsoluteWorldPosition(8_000_000_000_080d, 10d, -8_000_000_000_180d);
            var firstFrame = new LocalOriginFrame(model.Encode(firstOrigin), firstOrigin, 1024d);
            var secondFrame = new LocalOriginFrame(model.Encode(secondOrigin), secondOrigin, 1024d);
            var record = CreateRecord(world, model, address);
            var payload = SavedPlaceRecordCodec.Encode(record);
            Assert.That(SavedPlaceRecordCodec.TryDecode(payload, out var reconstructed, out var error),
                Is.True,
                error);

            Assert.That(firstFrame.ToLocal(absolute), Is.Not.EqualTo(secondFrame.ToLocal(absolute)));
            Assert.That(reconstructed.Address, Is.EqualTo(address));
            Assert.That(reconstructed.AbsolutePosition, Is.EqualTo(absolute));
            Assert.That(reconstructed.SavedPlaceId,
                Is.EqualTo(CreateRecord(world, model, address).SavedPlaceId));
        }

        [Test]
        public void SavedPlaceCodec_RejectsCorruptionAndTrailingData()
        {
            var payload = SavedPlaceRecordCodec.Encode(CreateRecord());
            var corrupted = (byte[])payload.Clone();
            corrupted[0] ^= 0xff;
            var trailing = new byte[payload.Length + 1];
            Buffer.BlockCopy(payload, 0, trailing, 0, payload.Length);
            trailing[trailing.Length - 1] = 7;

            Assert.That(SavedPlaceRecordCodec.TryDecode(corrupted, out _, out var corruptError), Is.False);
            Assert.That(corruptError, Is.Not.Empty);
            Assert.That(SavedPlaceRecordCodec.TryDecode(trailing, out _, out var trailingError), Is.False);
            Assert.That(trailingError, Does.Contain("trailing"));
        }

        private static SavedPlaceRecord CreateRecord()
        {
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var address = model.Encode(new AbsoluteWorldPosition(123456789.25d, -12.5d, -987654321.75d));
            return CreateRecord(world, model, address);
        }

        private static SavedPlaceRecord CreateRecord(
            WorldIdentity world,
            NonCanonCoordinateModel model,
            WorldCoordinateAddress address)
        {
            var savedPlaceNamespace = new WorldSeedNamespace(WorldVersionDomain.Topology, "player.saved-place");
            var siteNamespace = new WorldSeedNamespace(WorldVersionDomain.Site, "site.landmark");
            var savedPlaceId = WorldFeatureId.Create(world, savedPlaceNamespace, address, "marker:0");
            var anchorId = WorldFeatureId.Create(world, siteNamespace, address, "landmark:0");
            Assert.That(model.TryResolve(address, out var absolute), Is.True);
            return new SavedPlaceRecord(
                savedPlaceId,
                WorldPersistenceManifest.CreateCurrent(world, model),
                "The Long Descent",
                address,
                absolute,
                new OptionalThematicCoordinatePayload(
                    true,
                    "test.future-coordinate-authority",
                    1,
                    "opaque-test-payload"),
                new[]
                {
                    new WorldFeatureReference("anchor", anchorId),
                    new WorldFeatureReference("overlook", savedPlaceId)
                });
        }
    }
}
