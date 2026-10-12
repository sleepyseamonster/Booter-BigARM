using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DInventoryLoopTests
    {
        private TopDown3DItemCatalog catalog;
        private const string Ore = "resource.ironstone_ore";
        private const string Dust = "resource.airborne_dust";
        private const string Tool = "tool.micro_dust_harvester";

        [SetUp]
        public void SetUp() => catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(
            "Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");

        private TopDown3DInventoryState Kit(int slots = 8) => new(catalog, slots);
        private TopDown3DInventoryState Cargo(int slots = 12, float mass = 92f) => new(catalog,
            new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, slots, mass));
        private TopDown3DInventoryState Cache(int slots = 24) => new(catalog,
            new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, slots));

        [TestCase(Ore)]
        [TestCase(Dust)]
        public void StockResourcesAreForbiddenInBooterAndPermittedInCargoAndCache(string id)
        {
            var amount = new TopDown3DItemAmount(id, 1);
            Assert.That(catalog.GetRequiredDefinition(id).CarryPreference, Is.EqualTo(TopDown3DCarryPreference.BigArmOnly));
            Assert.That(Kit().TryAdd(amount).Code, Is.EqualTo(TopDown3DInventoryResultCode.CarrierRestricted));
            Assert.That(Cargo().TryAdd(amount).Succeeded, Is.True);
            Assert.That(Cache().TryAdd(amount).Succeeded, Is.True);
            Assert.That(Cache().TryAdd(new TopDown3DItemAmount(Tool, 1)).Succeeded, Is.False);
        }

        [Test]
        public void ResourceDeliveryNeverFallsBackAndChangesOnlyLegger()
        {
            var kit = Kit(); var cargo = Cargo(); var access = true;
            var destinations = new TopDown3DItemDestinationService(kit, cargo, () => access);
            var kitEvents = 0; var cargoEvents = 0;
            kit.Changed += () => kitEvents++; cargo.Changed += () => cargoEvents++;
            Assert.That(destinations.AddHarvest(Ore, 1).Succeeded, Is.True);
            Assert.That(kit.OccupiedSlotCount, Is.Zero);
            Assert.That(cargo.Slots[0].Quantity, Is.EqualTo(1));
            Assert.That(kitEvents, Is.Zero); Assert.That(cargoEvents, Is.EqualTo(1));
            access = false;
            Assert.That(destinations.AddHarvest(Ore, 1).Code, Is.EqualTo(TopDown3DInventoryResultCode.DestinationUnavailable));
            Assert.That(cargo.Slots[0].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void CapacityQueriesRespectBothWeightAndSlots()
        {
            var cargo = Cargo();
            Assert.That(cargo.GetMaxAddable(Ore, 99), Is.EqualTo(73));
            Assert.That(cargo.CheckAdd(new TopDown3DItemAmount(Ore, 74)).Code, Is.EqualTo(TopDown3DInventoryResultCode.TooHeavy));
            cargo.TryAdd(new TopDown3DItemAmount(Ore, 73));
            Assert.That(cargo.GetMaxAddable(Ore, 99), Is.Zero);
            var oneSlot = Cargo(1);
            oneSlot.TryAdd(new TopDown3DItemAmount(Dust, 100));
            Assert.That(oneSlot.GetMaxAddable(Ore, 1), Is.Zero);
        }

        [Test]
        public void JointTransferPublishesOnlyCompleteStateAndRejectsReentrantWrites()
        {
            var source = Cargo(); var cache = Cache();
            source.TryAdd(new TopDown3DItemAmount(Dust, 10));
            var sourceEvents = 0; var cacheEvents = 0;
            source.Changed += () =>
            {
                sourceEvents++;
                Assert.That(source.Slots.Sum(s => s.Quantity) + cache.Slots.Sum(s => s.Quantity), Is.EqualTo(10));
                Assert.That(cache.TryAdd(new TopDown3DItemAmount(Dust, 1)).Code, Is.EqualTo(TopDown3DInventoryResultCode.MutationBusy));
            };
            cache.Changed += () => cacheEvents++;
            Assert.That(TopDown3DInventoryTransferService.Transfer(source, cache, Dust, 10).Succeeded, Is.True);
            Assert.That(sourceEvents, Is.EqualTo(1)); Assert.That(cacheEvents, Is.EqualTo(1));
        }

        [Test]
        public void QuickTransferRemovesTheSelectedStackAndAccessLossLeavesBothUntouched()
        {
            var source = Cargo(); var cache = Cache();
            source.TryAdd(new TopDown3DItemAmount(Dust, 150));
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(source, cache, 0, () => false).Succeeded, Is.False);
            Assert.That(source.Slots[0].Quantity, Is.EqualTo(100)); Assert.That(cache.OccupiedSlotCount, Is.Zero);
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(source, cache, 0, () => true).Quantity, Is.EqualTo(100));
            Assert.That(source.Slots[0].IsEmpty, Is.True); Assert.That(source.Slots[1].Quantity, Is.EqualTo(50));
        }

        [Test]
        public void BulkUnloadKeepsEveryRemainderWhenCacheOnlyPartlyFits()
        {
            var source = Cargo(); var cache = Cache(1);
            source.TryAdd(new[] { new TopDown3DItemAmount(Dust, 20), new TopDown3DItemAmount(Ore, 2) });
            cache.TryAdd(new TopDown3DItemAmount(Dust, 95));
            Assert.That(TopDown3DInventoryTransferService.UnloadResources(source, cache, () => true).Quantity, Is.EqualTo(5));
            Assert.That(cache.Slots[0].Quantity, Is.EqualTo(100));
            Assert.That(source.Slots.Single(s => s.ItemId == Dust).Quantity, Is.EqualTo(15));
            Assert.That(source.Slots.Single(s => s.ItemId == Ore).Quantity, Is.EqualTo(2));
        }

        [Test]
        public void MixedPickupCommitsToolResourceAndWorldConditionTogether()
        {
            var kit = Kit(); var cargo = Cargo(); var available = true;
            var tools = new[] { new TopDown3DItemAmount(Tool, 1) };
            var dust = new[] { new TopDown3DItemAmount(Dust, 3) };
            Assert.That(TopDown3DInventoryTransaction.AddRewards(kit, tools, cargo, dust, () => false).Succeeded, Is.False);
            Assert.That(kit.OccupiedSlotCount, Is.Zero); Assert.That(cargo.OccupiedSlotCount, Is.Zero);
            Assert.That(TopDown3DInventoryTransaction.AddRewards(kit, tools, cargo, dust,
                () => { if (!available) return false; available = false; return true; }).Succeeded, Is.True);
            Assert.That(kit.Slots[0].ItemId, Is.EqualTo(Tool)); Assert.That(cargo.Slots[0].ItemId, Is.EqualTo(Dust));
            Assert.That(TopDown3DInventoryTransaction.AddRewards(kit, tools, cargo, dust, () => available).Succeeded, Is.False);
            Assert.That(kit.Slots[0].Quantity, Is.EqualTo(1)); Assert.That(cargo.Slots[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void SnapshotCannotReintroduceResourcesIntoBooter()
        {
            var source = Cargo(8); source.TryAdd(new TopDown3DItemAmount(Ore, 1));
            var kit = Kit();
            Assert.That(kit.ApplySnapshot(source.CaptureSnapshot()).Code, Is.EqualTo(TopDown3DInventoryResultCode.InvalidSnapshot));
            Assert.That(kit.OccupiedSlotCount, Is.Zero);
        }
    }
}
