using System;
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

            Assert.That(firstFrame.ToLocal(absolute), Is.Not.EqualTo(secondFrame.ToLocal(absolute)));
            Assert.That(record.Address, Is.EqualTo(address));
            Assert.That(record.SavedPlaceId, Is.EqualTo(CreateRecord(world, model, address).SavedPlaceId));
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
            return new SavedPlaceRecord(
                savedPlaceId,
                WorldPersistenceManifest.CreateCurrent(world, model),
                address,
                true,
                anchorId);
        }
    }
}
