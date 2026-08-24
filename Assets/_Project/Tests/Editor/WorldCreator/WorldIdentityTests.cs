using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldIdentityTests
    {
        [Test]
        public void VersionManifest_ExposesIndependentDomains()
        {
            var versions = new WorldVersionManifest(11, 12, 13, 14, 15, 16, 17);

            Assert.That(versions.GetVersion(WorldVersionDomain.Topology), Is.EqualTo(11));
            Assert.That(versions.GetVersion(WorldVersionDomain.Coordinate), Is.EqualTo(12));
            Assert.That(versions.GetVersion(WorldVersionDomain.Landform), Is.EqualTo(13));
            Assert.That(versions.GetVersion(WorldVersionDomain.Material), Is.EqualTo(14));
            Assert.That(versions.GetVersion(WorldVersionDomain.Decoration), Is.EqualTo(15));
            Assert.That(versions.GetVersion(WorldVersionDomain.Resource), Is.EqualTo(16));
            Assert.That(versions.GetVersion(WorldVersionDomain.Site), Is.EqualTo(17));
        }

        [Test]
        public void NamespacedSeed_IsDeterministicAndDomainIsolated()
        {
            var model = new NonCanonCoordinateModel();
            var address = model.Encode(new AbsoluteWorldPosition(128.25d, -4d, -8192.5d));
            var landformNamespace = new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.canyon");
            var decorationNamespace = new WorldSeedNamespace(WorldVersionDomain.Decoration, "landform.canyon");
            var world = WorldCreatorTestFactory.CreateWorld();
            var materialOnlyChange = WorldCreatorTestFactory.CreateWorld(material: 99);
            var landformChange = WorldCreatorTestFactory.CreateWorld(landform: 2);
            var topologyChange = WorldCreatorTestFactory.CreateWorld(topology: 2);
            var coordinateChange = WorldCreatorTestFactory.CreateWorld(coordinate: 2);

            var first = landformNamespace.DeriveSeed(world, address, 3UL);

            Assert.That(landformNamespace.DeriveSeed(world, address, 3UL), Is.EqualTo(first));
            Assert.That(landformNamespace.DeriveSeed(materialOnlyChange, address, 3UL), Is.EqualTo(first));
            Assert.That(landformNamespace.DeriveSeed(landformChange, address, 3UL), Is.Not.EqualTo(first));
            Assert.That(landformNamespace.DeriveSeed(topologyChange, address, 3UL), Is.Not.EqualTo(first));
            Assert.That(landformNamespace.DeriveSeed(coordinateChange, address, 3UL), Is.Not.EqualTo(first));
            Assert.That(decorationNamespace.DeriveSeed(world, address, 3UL), Is.Not.EqualTo(first));
            Assert.That(landformNamespace.DeriveSeed(world, address, 4UL), Is.Not.EqualTo(first));
        }

        [Test]
        public void FeatureIdentity_IsBuildOrderIndependentAndRoundTripsText()
        {
            var world = WorldCreatorTestFactory.CreateWorld();
            var model = new NonCanonCoordinateModel();
            var seedNamespace = new WorldSeedNamespace(WorldVersionDomain.Site, "site.ruin");
            var addresses = new[]
            {
                model.Encode(new AbsoluteWorldPosition(-10d, 0d, 40d)),
                model.Encode(new AbsoluteWorldPosition(900d, 12d, -700d)),
                model.Encode(new AbsoluteWorldPosition(9_000_000_000_000d, 4d, -9_000_000_000_000d))
            };
            var forward = BuildIdentities(world, seedNamespace, addresses, false);
            var reverse = BuildIdentities(world, seedNamespace, addresses, true);

            CollectionAssert.AreEquivalent(forward, reverse);
            foreach (var featureId in forward)
            {
                Assert.That(WorldFeatureId.TryParse(featureId.ToString(), out var parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(featureId));
            }
        }

        [Test]
        public void FeatureIdentity_FixtureSetHasNoDuplicates()
        {
            var world = WorldCreatorTestFactory.CreateWorld();
            var model = new NonCanonCoordinateModel();
            var seedNamespace = new WorldSeedNamespace(WorldVersionDomain.Resource, "resource.ironstone");
            var identities = new HashSet<WorldFeatureId>();

            for (var i = 0; i < 4096; i++)
            {
                var address = model.Encode(new AbsoluteWorldPosition(i * 17d, i % 31, i * -29d));
                var identity = WorldFeatureId.Create(world, seedNamespace, address, $"candidate:{i % 13}");
                Assert.That(identity.IsEmpty, Is.False);
                Assert.That(identities.Add(identity), Is.True, $"Duplicate fixture identity at {i}.");
            }
        }

        [Test]
        public void StableHash_GoldenVectorsPreventSilentWorldReshuffle()
        {
            var world = WorldCreatorTestFactory.CreateWorld();
            var model = new NonCanonCoordinateModel();
            var address = model.Encode(new AbsoluteWorldPosition(123.25d, -4.5d, -987.75d));
            var seedNamespace = new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.golden");
            var featureId = WorldFeatureId.Create(world, seedNamespace, address, "feature:golden");
            var planKey = new WorldPlanKey(world, seedNamespace, address, "plan.golden", 3UL);

            Assert.That(seedNamespace.DeriveSeed(world, address, 7UL), Is.EqualTo(7070271042516370309UL));
            Assert.That(featureId.ToString(), Is.EqualTo("b76c9eee29fba7a1b835e73824dff000"));
            Assert.That(planKey.StableId.ToString(), Is.EqualTo("1b99de0bb547c818e48dcb0209d4dda5"));
        }

        private static WorldFeatureId[] BuildIdentities(
            WorldIdentity world,
            WorldSeedNamespace seedNamespace,
            WorldCoordinateAddress[] addresses,
            bool reverse)
        {
            var result = new WorldFeatureId[addresses.Length];
            for (var step = 0; step < addresses.Length; step++)
            {
                var index = reverse ? addresses.Length - 1 - step : step;
                result[index] = WorldFeatureId.Create(world, seedNamespace, addresses[index], $"feature:{index}");
            }

            return result;
        }
    }
}
