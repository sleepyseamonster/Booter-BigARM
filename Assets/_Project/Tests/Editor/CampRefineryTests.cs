using System;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CampRefineryTests
    {
        [TestCase(9, false, 9)] [TestCase(10, true, 0)] [TestCase(13, true, 3)]
        public void RefiningUsesExactlyTenDustAndRetainsCanister(int dust, bool success, int remaining)
        {
            var state = new TopDown3DDustRefineryState { canisterInstanceId = Guid.NewGuid().ToString("N"), dust = dust,
                fractionalSeconds = 3, batteryItemId = TopDown3DDustRefineryState.EmptyBatteryId };
            Assert.That(state.TryRefine(out var prepared, out _), Is.EqualTo(success));
            Assert.That(state.dust, Is.EqualTo(dust), "Preparation must not mutate live state.");
            if (!success) return;
            Assert.That(prepared.dust, Is.EqualTo(remaining));
            Assert.That(prepared.canisterInstanceId, Is.EqualTo(state.canisterInstanceId));
            Assert.That(prepared.fractionalSeconds, Is.EqualTo(3));
            Assert.That(prepared.batteryItemId, Is.EqualTo(TopDown3DDustRefineryState.ChargedBatteryId));
            Assert.That(prepared.TryRefine(out _, out _), Is.False);
        }
        [Test] public void MalformedContentsNeverProduceBattery()
        {
            var state = new TopDown3DDustRefineryState { canisterInstanceId = "invalid", dust = 20,
                batteryItemId = TopDown3DDustRefineryState.EmptyBatteryId };
            Assert.That(state.TryRefine(out _, out _), Is.False);
        }

        [Test] public void SocketCommitPublishesCompleteOwnershipAndBlockedRetrievalRetainsBattery()
        {
            var icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            var canister = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            canister.Configure(TopDown3DHarvesterSettings.CanisterItemId, "Canister", "Tool", "Tool", icon, 1, .8f);
            canister.ConfigureLoadout(TopDown3DEquipmentSlot.DustCanister);
            var empty = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            empty.Configure(TopDown3DDustRefineryState.EmptyBatteryId, "Empty", "Battery", "Supply", icon, 1, .8f);
            var charged = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            charged.Configure(TopDown3DDustRefineryState.ChargedBatteryId, "Charged", "Battery", "Supply", icon, 1, .8f);
            var dust = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            dust.Configure(TopDown3DHarvesterSettings.DustItemId, "Dust", "Dust", "Resource", icon, 100, .01f, TopDown3DCarryPreference.BigArmOnly);
            var catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>(); catalog.Configure(new[] { canister, empty, charged, dust });
            var go = new GameObject("Refinery test"); var refinery = go.AddComponent<TopDown3DDustRefinery>();
            try
            {
                var inventory = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 2, 0, false, false, true));
                var id = Guid.NewGuid().ToString("N");
                inventory.TryAdd(new TopDown3DItemAmount(canister.ItemId, 1, id, TopDown3DCanisterContents.Create(13, 3)));
                inventory.TryAdd(new TopDown3DItemAmount(empty.ItemId, 1));
                var observed = false;
                inventory.Changed += () =>
                {
                    if (!observed && refinery.CaptureState().HasCanister)
                    { Assert.That(inventory.Slots[0].IsEmpty, Is.True); observed = true; }
                };
                Assert.That(refinery.InsertCanister(inventory, 0, () => true).Succeeded, Is.True);
                Assert.That(observed, Is.True);
                Assert.That(refinery.InsertBattery(inventory, 1, () => true).Succeeded, Is.True);
                Assert.That(refinery.Refine(() => true).Succeeded, Is.True);
                Assert.That(refinery.CaptureState().dust, Is.EqualTo(3));
                var full = new TopDown3DInventoryState(catalog, 1); full.TryAdd(new TopDown3DItemAmount(empty.ItemId, 1));
                Assert.That(refinery.RetrieveBattery(full, () => true).Succeeded, Is.False);
                Assert.That(refinery.CaptureState().batteryItemId, Is.EqualTo(charged.ItemId));
                Assert.That(refinery.RetrieveBattery(inventory, () => true).Succeeded, Is.True);
                Assert.That(refinery.CaptureState().HasBattery, Is.False);
                Assert.That(refinery.RetrieveCanister(inventory, () => true).Succeeded, Is.True);
                Assert.That(inventory.Slots[1].InstanceId, Is.EqualTo(id));
                Assert.That(inventory.Slots[1].CanisterContents.dust, Is.EqualTo(3));
                Assert.That(inventory.Slots[1].CanisterContents.fractionalSeconds, Is.EqualTo(3));
            }
            finally
            { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(canister);
                UnityEngine.Object.DestroyImmediate(empty); UnityEngine.Object.DestroyImmediate(charged); UnityEngine.Object.DestroyImmediate(dust); UnityEngine.Object.DestroyImmediate(icon); }
        }

        [Test] public void AccessLossBetweenPreparationAndCommitPreservesSourceAndSocket()
        {
            var icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            var battery = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            battery.Configure(TopDown3DDustRefineryState.EmptyBatteryId, "Empty", "Battery", "Supply", icon, 1, .8f);
            var catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>(); catalog.Configure(new[] { battery });
            var go = new GameObject("Refinery test"); var refinery = go.AddComponent<TopDown3DDustRefinery>();
            try
            {
                var inventory = new TopDown3DInventoryState(catalog, 1); inventory.TryAdd(new TopDown3DItemAmount(battery.ItemId, 1));
                var checks = 0;
                Assert.That(refinery.InsertBattery(inventory, 0, () => ++checks == 1).Succeeded, Is.False);
                Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1));
                Assert.That(refinery.CaptureState().HasBattery, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(battery); UnityEngine.Object.DestroyImmediate(icon); }
        }
    }
}
