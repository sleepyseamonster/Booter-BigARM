using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DMicroDustHarvesterTests
    {
        private readonly List<Object> owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        [Test]
        public void Accrual_IsFractionalCappedAndIdempotentAtSameTime()
        {
            var settings = Own(ScriptableObject.CreateInstance<TopDown3DHarvesterSettings>());
            var record = CreateRecord(0, 0d);
            record.Advance(25d, settings);
            Assert.That(record.Dust, Is.EqualTo(2));
            Assert.That(record.FractionalSeconds, Is.EqualTo(5d).Within(0.000001d));
            record.Advance(25d, settings);
            Assert.That(record.Dust, Is.EqualTo(2));
            record.Advance(2000d, settings);
            Assert.That(record.Dust, Is.EqualTo(settings.Capacity));
            Assert.That(record.FractionalSeconds, Is.Zero);
        }

        [Test]
        public void ConditionalRemove_RejectionKeepsInventoryUntouched()
        {
            var inventory = CreateInventory(2);
            Assert.That(inventory.TryAdd(new TopDown3DItemAmount(
                TopDown3DHarvesterSettings.CanisterItemId, 1)).Succeeded, Is.True);
            var before = JsonUtility.ToJson(inventory.CaptureSnapshot());
            var result = inventory.TryRemove(
                TopDown3DHarvesterSettings.CanisterItemId, 1, () => false);
            Assert.That(result.Code, Is.EqualTo(TopDown3DInventoryResultCode.CommitConditionRejected));
            Assert.That(JsonUtility.ToJson(inventory.CaptureSnapshot()), Is.EqualTo(before));
        }

        [Test]
        public void Pickup_RequiresRoomForCanisterAndEveryDustUnit()
        {
            var state = CreateState();
            var record = CreateRecord(5, 0d);
            Assert.That(state.ApplySnapshot(TopDown3DPlacedHarvesterSnapshot.Create(
                0, 0d, 1L, new[] { record })), Is.True);
            var inventory = CreateInventory(2);
            Assert.That(inventory.TryAdd(new TopDown3DItemAmount("test.blocker", 1)).Succeeded, Is.True);
            var before = JsonUtility.ToJson(inventory.CaptureSnapshot());

            Assert.That(state.TryPickup(record.StableId, inventory, out _), Is.False);
            Assert.That(state.DeployedCount, Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(inventory.CaptureSnapshot()), Is.EqualTo(before));

            Assert.That(inventory.TryRemove("test.blocker", 1).Succeeded, Is.True);
            Assert.That(state.TryPickup(record.StableId, inventory, out var collected), Is.True);
            Assert.That(collected, Is.EqualTo(5));
            Assert.That(state.DeployedCount, Is.Zero);
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo(TopDown3DHarvesterSettings.CanisterItemId));
            Assert.That(inventory.Slots[1].ItemId, Is.EqualTo(TopDown3DHarvesterSettings.DustItemId));
            Assert.That(inventory.Slots[1].Quantity, Is.EqualTo(5));
            Assert.That(state.TryPickup(record.StableId, inventory, out _), Is.False);
        }

        [Test]
        public void Snapshot_RestoresPartialProgressWithoutSharingMutableRecords()
        {
            var state = CreateState();
            var original = TopDown3DPlacedHarvesterRecord.Create(
                "00000000000000000000000000000001", 0L,
                new AbsoluteWorldPosition(1d, 2d, 3d), 2, 5d, 25d);
            var snapshot = TopDown3DPlacedHarvesterSnapshot.Create(0, 25d, 1L,
                new[] { original });
            original.Advance(95d, state.Settings);
            Assert.That(snapshot.Records[0].Dust, Is.EqualTo(2));
            var decoded = JsonUtility.FromJson<TopDown3DPlacedHarvesterSnapshot>(
                JsonUtility.ToJson(snapshot));
            Assert.That(state.ApplySnapshot(decoded), Is.True);
            Assert.That(state.TryGet(original.StableId, out var restored), Is.True);
            Assert.That(restored.Dust, Is.EqualTo(2));
            Assert.That(restored.FractionalSeconds, Is.EqualTo(5d).Within(0.000001d));
        }

        [Test]
        public void Snapshot_RejectsDuplicateIdAndDoesNotChangeLiveState()
        {
            var state = CreateState();
            var record = CreateRecord(3, 0d);
            var valid = TopDown3DPlacedHarvesterSnapshot.Create(0, 0d, 1L,
                new[] { record });
            Assert.That(state.ApplySnapshot(valid), Is.True);
            var before = JsonUtility.ToJson(state.CaptureSnapshot());
            var duplicate = TopDown3DPlacedHarvesterSnapshot.Create(0, 0d, 2L,
                new[] { record, record });
            Assert.That(state.ApplySnapshot(duplicate), Is.False);
            Assert.That(JsonUtility.ToJson(state.CaptureSnapshot()), Is.EqualTo(before));
        }

        private TopDown3DPlacedHarvesterState CreateState()
        {
            var worldObject = Own(new GameObject("Harvester test world"));
            var world = worldObject.AddComponent<TopDown3DProceduralWorld>();
            var state = worldObject.AddComponent<TopDown3DPlacedHarvesterState>();
            state.Configure(world, Own(ScriptableObject.CreateInstance<TopDown3DHarvesterSettings>()));
            return state;
        }

        private TopDown3DInventoryState CreateInventory(int capacity)
        {
            var iconTexture = Own(new Texture2D(2, 2));
            var icon = Own(Sprite.Create(iconTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f));
            var canister = CreateItem(TopDown3DHarvesterSettings.CanisterItemId, icon);
            var dust = CreateItem(TopDown3DHarvesterSettings.DustItemId, icon);
            var blocker = CreateItem("test.blocker", icon);
            var catalog = Own(ScriptableObject.CreateInstance<TopDown3DItemCatalog>());
            catalog.Configure(new[] { canister, dust, blocker });
            return new TopDown3DInventoryState(catalog, capacity);
        }

        private TopDown3DItemDefinition CreateItem(string id, Sprite icon)
        {
            var item = Own(ScriptableObject.CreateInstance<TopDown3DItemDefinition>());
            item.Configure(id, id, "Test item", "Test", icon, 100, 0f);
            return item;
        }

        private static TopDown3DPlacedHarvesterRecord CreateRecord(int dust, double fraction)
        {
            return TopDown3DPlacedHarvesterRecord.Create(
                "00000000000000000000000000000001", 0L,
                new AbsoluteWorldPosition(1d, 2d, 3d), dust, fraction, 0d);
        }

        private T Own<T>(T value) where T : Object
        {
            owned.Add(value);
            return value;
        }
    }
}
