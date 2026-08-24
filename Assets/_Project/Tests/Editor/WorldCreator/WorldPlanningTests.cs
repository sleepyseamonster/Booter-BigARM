using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldPlanningTests
    {
        [Test]
        public void PlanKey_IgnoresUnrelatedVersionDomains()
        {
            var model = new NonCanonCoordinateModel();
            var address = model.Encode(new AbsoluteWorldPosition(100d, 0d, 200d));
            var seedNamespace = new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.macro");
            var baseline = new WorldPlanKey(
                WorldCreatorTestFactory.CreateWorld(),
                seedNamespace,
                address,
                "macro-region");
            var materialOnlyChange = new WorldPlanKey(
                WorldCreatorTestFactory.CreateWorld(material: 22),
                seedNamespace,
                address,
                "macro-region");
            var landformChange = new WorldPlanKey(
                WorldCreatorTestFactory.CreateWorld(landform: 22),
                seedNamespace,
                address,
                "macro-region");
            var topologyChange = new WorldPlanKey(
                WorldCreatorTestFactory.CreateWorld(topology: 22),
                seedNamespace,
                address,
                "macro-region");
            var coordinateChange = new WorldPlanKey(
                WorldCreatorTestFactory.CreateWorld(coordinate: 22),
                seedNamespace,
                address,
                "macro-region");

            Assert.That(materialOnlyChange, Is.EqualTo(baseline));
            Assert.That(materialOnlyChange.StableId, Is.EqualTo(baseline.StableId));
            Assert.That(landformChange, Is.Not.EqualTo(baseline));
            Assert.That(landformChange.StableId, Is.Not.EqualTo(baseline.StableId));
            Assert.That(topologyChange, Is.Not.EqualTo(baseline));
            Assert.That(coordinateChange, Is.Not.EqualTo(baseline));
        }

        [Test]
        public void BoundaryKey_HasOneOwnerRegardlessOfRequestOrder()
        {
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var seedNamespace = new WorldSeedNamespace(WorldVersionDomain.Topology, "topology.boundary");
            var left = new WorldPlanKey(
                world,
                seedNamespace,
                model.Encode(new AbsoluteWorldPosition(-1d, 0d, 0d)),
                "planning-cell");
            var right = new WorldPlanKey(
                world,
                seedNamespace,
                model.Encode(new AbsoluteWorldPosition(1d, 0d, 0d)),
                "planning-cell");

            var leftFirst = new WorldPlanBoundaryKey(left, right);
            var rightFirst = new WorldPlanBoundaryKey(right, left);

            Assert.That(rightFirst, Is.EqualTo(leftFirst));
            Assert.That(rightFirst.StableId, Is.EqualTo(leftFirst.StableId));
            Assert.That(leftFirst.Owner.CompareTo(leftFirst.Neighbor), Is.LessThan(0));
        }

        [Test]
        public void BoundaryKey_RejectsCrossDomainOwnership()
        {
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var address = model.Encode(new AbsoluteWorldPosition(0d, 0d, 0d));
            var topology = new WorldPlanKey(
                world,
                new WorldSeedNamespace(WorldVersionDomain.Topology, "topology.boundary"),
                address,
                "planning-cell",
                0UL);
            var landform = new WorldPlanKey(
                world,
                new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.boundary"),
                model.Encode(new AbsoluteWorldPosition(1d, 0d, 0d)),
                "planning-cell",
                0UL);

            Assert.That(
                () => new WorldPlanBoundaryKey(topology, landform),
                Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void HaloPolicy_IsBoundedByTopologicalDistanceAndParticipantCount()
        {
            var policy = new WorldPlanHaloPolicy(2, 12);

            Assert.That(policy.IncludesDistance(0), Is.True);
            Assert.That(policy.IncludesDistance(2), Is.True);
            Assert.That(policy.IncludesDistance(3), Is.False);
            Assert.That(policy.IncludesDistance(-1), Is.False);
            Assert.That(policy.AcceptsParticipantCount(1), Is.True);
            Assert.That(policy.AcceptsParticipantCount(12), Is.True);
            Assert.That(policy.AcceptsParticipantCount(13), Is.False);
        }

        [Test]
        public void ImmutablePlanRecord_CanUseCacheContractWithoutChangingItsKey()
        {
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var seedNamespace = new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.test-plan");
            var address = model.Encode(new AbsoluteWorldPosition(25d, 0d, 50d));
            var key = new WorldPlanKey(world, seedNamespace, address, "fixture");
            var fingerprint = WorldFeatureId.Create(world, seedNamespace, address, "content");
            var plan = new TestPlan(new WorldPlanHeader(key, 1, fingerprint));
            IWorldPlanCache<TestPlan> cache = new TestPlanCache();

            cache.Store(plan);

            Assert.That(cache.Count, Is.EqualTo(1));
            Assert.That(cache.TryGet(key, out var restored), Is.True);
            Assert.That(restored.Header, Is.EqualTo(plan.Header));
            Assert.That(cache.Remove(key), Is.True);
            Assert.That(cache.Count, Is.Zero);
        }

        private readonly struct TestPlan : IWorldPlanRecord
        {
            public TestPlan(WorldPlanHeader header)
            {
                Header = header;
            }

            public WorldPlanHeader Header { get; }
        }

        private sealed class TestPlanCache : IWorldPlanCache<TestPlan>
        {
            private readonly Dictionary<WorldPlanKey, TestPlan> plans = new Dictionary<WorldPlanKey, TestPlan>();

            public int Count => plans.Count;

            public bool TryGet(WorldPlanKey key, out TestPlan plan)
            {
                return plans.TryGetValue(key, out plan);
            }

            public void Store(TestPlan plan)
            {
                plans[plan.Header.Key] = plan;
            }

            public bool Remove(WorldPlanKey key)
            {
                return plans.Remove(key);
            }

            public void Clear()
            {
                plans.Clear();
            }
        }
    }
}
