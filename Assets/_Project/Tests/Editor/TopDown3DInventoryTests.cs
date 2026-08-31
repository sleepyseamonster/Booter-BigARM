using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DInventoryTests
    {
        private readonly List<Object> ownedObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
        }

        [Test]
        public void TryAdd_FillsExistingStacksBeforeEmptySlots()
        {
            var state = CreateState(3, out var ironstone, out _);

            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 80)).Succeeded, Is.True);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 30)).Succeeded, Is.True);

            Assert.That(state.Slots[0].Quantity, Is.EqualTo(99));
            Assert.That(state.Slots[1].Quantity, Is.EqualTo(11));
            Assert.That(state.Slots[2].IsEmpty, Is.True);
        }

        [Test]
        public void TryAdd_IsAtomicWhenBatchCannotFit()
        {
            var state = CreateState(1, out var ironstone, out var shard);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 98)).Succeeded, Is.True);
            var before = JsonUtility.ToJson(state.CaptureSnapshot());
            var changed = 0;
            state.Changed += () => changed++;

            var result = state.TryAdd(new[]
            {
                new TopDown3DItemAmount(ironstone.ItemId, 1),
                new TopDown3DItemAmount(shard.ItemId, 1)
            });

            Assert.That(result.Code, Is.EqualTo(TopDown3DInventoryResultCode.InsufficientSpace));
            Assert.That(JsonUtility.ToJson(state.CaptureSnapshot()), Is.EqualTo(before));
            Assert.That(changed, Is.Zero);
        }

        [Test]
        public void Mutations_RejectInvalidRequestsWithoutChangingState()
        {
            var state = CreateState(2, out var ironstone, out _);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 3)).Succeeded, Is.True);
            var before = JsonUtility.ToJson(state.CaptureSnapshot());

            Assert.That(state.TryAdd(new TopDown3DItemAmount("unknown", 1)).Code,
                Is.EqualTo(TopDown3DInventoryResultCode.UnknownItem));
            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 0)).Code,
                Is.EqualTo(TopDown3DInventoryResultCode.InvalidRequest));
            Assert.That(state.TryRemove(ironstone.ItemId, 4).Code,
                Is.EqualTo(TopDown3DInventoryResultCode.InsufficientQuantity));
            Assert.That(state.TryMoveOrSwap(-1, 1).Code,
                Is.EqualTo(TopDown3DInventoryResultCode.InvalidSlot));
            Assert.That(JsonUtility.ToJson(state.CaptureSnapshot()), Is.EqualTo(before));
        }

        [Test]
        public void MoveSwapAndMerge_KeepFixedCapacityAndExactQuantities()
        {
            var state = CreateState(3, out var ironstone, out var shard);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 99)).Succeeded, Is.True);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 12)).Succeeded, Is.True);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(shard.ItemId, 1)).Succeeded, Is.True);

            Assert.That(state.TryMoveOrSwap(2, 0).Succeeded, Is.True);
            Assert.That(state.Slots[0].ItemId, Is.EqualTo(shard.ItemId));
            Assert.That(state.TryMerge(2, 1).Succeeded, Is.True);
            Assert.That(state.Slots[1].Quantity, Is.EqualTo(99));
            Assert.That(state.Slots[2].Quantity, Is.EqualTo(12));
            Assert.That(state.Capacity, Is.EqualTo(3));
        }

        [Test]
        public void Snapshot_RoundTripsExactSlotOrderAndEmitsOnce()
        {
            var source = CreateState(3, out var ironstone, out var shard);
            Assert.That(source.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 4)).Succeeded, Is.True);
            Assert.That(source.TryAdd(new TopDown3DItemAmount(shard.ItemId, 2)).Succeeded, Is.True);
            Assert.That(source.TryMoveOrSwap(0, 2).Succeeded, Is.True);
            var snapshot = source.CaptureSnapshot();
            var target = new TopDown3DInventoryState(sourceCatalog, 3);
            var changed = 0;
            target.Changed += () => changed++;

            Assert.That(target.ApplySnapshot(snapshot).Succeeded, Is.True);
            Assert.That(JsonUtility.ToJson(target.CaptureSnapshot()), Is.EqualTo(JsonUtility.ToJson(snapshot)));
            Assert.That(changed, Is.EqualTo(1));
        }

        [Test]
        public void ApplySnapshot_RejectsMalformedPayloadsWithoutPartialMutation()
        {
            var state = CreateState(2, out var ironstone, out _);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 4)).Succeeded, Is.True);
            var before = JsonUtility.ToJson(state.CaptureSnapshot());
            var changed = 0;
            state.Changed += () => changed++;

            AssertRejected(before.Replace("\"version\":1", "\"version\":99"));
            AssertRejected(before.Replace("\"capacity\":2", "\"capacity\":3"));
            AssertRejected(before.Replace(ironstone.ItemId, "resource.unknown"));
            AssertRejected(before.Replace("\"quantity\":4", "\"quantity\":100"));

            Assert.That(JsonUtility.ToJson(state.CaptureSnapshot()), Is.EqualTo(before));
            Assert.That(changed, Is.Zero);

            void AssertRejected(string json)
            {
                var snapshot = JsonUtility.FromJson<TopDown3DInventorySnapshot>(json);
                Assert.That(state.ApplySnapshot(snapshot).Code,
                    Is.EqualTo(TopDown3DInventoryResultCode.InvalidSnapshot));
            }
        }

        [Test]
        public void Remove_IsAllOrNothingAndEmitsOneChange()
        {
            var state = CreateState(2, out var ironstone, out _);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 99)).Succeeded, Is.True);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(ironstone.ItemId, 7)).Succeeded, Is.True);
            var changed = 0;
            state.Changed += () => changed++;

            Assert.That(state.TryRemove(ironstone.ItemId, 8).Succeeded, Is.True);

            Assert.That(state.Slots[0].Quantity, Is.EqualTo(98));
            Assert.That(state.Slots[1].IsEmpty, Is.True);
            Assert.That(changed, Is.EqualTo(1));
        }

        [Test]
        public void Catalog_RejectsDuplicateIdsInsteadOfLastWriteWins()
        {
            var icon = CreateIcon();
            var first = CreateDefinition("duplicate", 10, icon);
            var second = CreateDefinition("duplicate", 20, icon);
            var catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>();
            ownedObjects.Add(catalog);
            catalog.Configure(new[] { first, second });

            Assert.That(catalog.TryValidate(out var error), Is.False);
            StringAssert.Contains("duplicate ID", error);
            Assert.Throws<System.InvalidOperationException>(() => catalog.TryGetDefinition("duplicate", out _));
        }

        private TopDown3DItemCatalog sourceCatalog;

        private TopDown3DInventoryState CreateState(
            int capacity,
            out TopDown3DItemDefinition ironstone,
            out TopDown3DItemDefinition shard)
        {
            var icon = CreateIcon();
            ironstone = CreateDefinition("resource.ironstone_ore", 99, icon);
            shard = CreateDefinition("resource.test_shard", 10, icon);
            sourceCatalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>();
            ownedObjects.Add(sourceCatalog);
            sourceCatalog.Configure(new[] { ironstone, shard });
            return new TopDown3DInventoryState(sourceCatalog, capacity);
        }

        private TopDown3DItemDefinition CreateDefinition(string itemId, int maxStack, Sprite icon)
        {
            var definition = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            ownedObjects.Add(definition);
            definition.Configure(itemId, itemId, "Test item", "Resource", icon, maxStack, 0f);
            return definition;
        }

        private Sprite CreateIcon()
        {
            var texture = new Texture2D(2, 2);
            ownedObjects.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            ownedObjects.Add(sprite);
            return sprite;
        }
    }
}
