using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DPackingTests
    {
        private readonly List<Object> owned = new List<Object>();
        [TearDown] public void TearDown() { for (var i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]); owned.Clear(); }

        [Test]
        public void AutoPack_IsDeterministicAndPutsHeavyStacksLow()
        {
            var icon = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.one * .5f); owned.Add(icon); owned.Add(icon.texture);
            var ore = CreateItem("ore", 99, 2f); var fiber = CreateItem("fiber", 99, .1f);
            var catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>(); owned.Add(catalog); catalog.Configure(new[] { ore, fiber });
            var settings = ScriptableObject.CreateInstance<TopDown3DPackingSettings>(); owned.Add(settings); settings.ConfigureDefaults();
            var state = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 12, settings.HardMass));
            state.TryAdd(new TopDown3DItemAmount("fiber", 5)); state.TryAdd(new TopDown3DItemAmount("ore", 8));
            Assert.That(TopDown3DCargoAutoPacker.TryPack(state, settings, out var first), Is.True);
            Assert.That(state.ApplySnapshot(first).Succeeded, Is.True);
            Assert.That(TopDown3DCargoAutoPacker.TryPack(state, settings, out var second), Is.True);
            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
            Assert.That(state.Slots[0].ItemId, Is.EqualTo("ore"));
        }

        [Test]
        public void TransferMaximum_MovesOnlyWhatFitsAndPreservesRemainder()
        {
            var ore = CreateItem("ore", 99, 1f); var catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>(); owned.Add(catalog); catalog.Configure(new[] { ore });
            var source = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 2));
            var destination = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 1));
            source.TryAdd(new TopDown3DItemAmount("ore", 20));
            var result = TopDown3DInventoryTransferService.Transfer(source, destination, "ore", 20);
            Assert.That(result.Succeeded, Is.True); Assert.That(result.Quantity, Is.EqualTo(20)); Assert.That(source.OccupiedSlotCount, Is.Zero); Assert.That(destination.Slots[0].Quantity, Is.EqualTo(20));
        }

        private TopDown3DItemDefinition CreateItem(string id, int maxStack, float mass)
        {
            var item = ScriptableObject.CreateInstance<TopDown3DItemDefinition>(); owned.Add(item);
            var texture = new Texture2D(2, 2); owned.Add(texture); var icon = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f); owned.Add(icon);
            item.Configure(id, id, "test", "test", icon, maxStack, mass); return item;
        }
    }
}
