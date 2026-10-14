using System;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CampCanisterLifecycleTests
    {
        private GameObject owner;
        private TopDown3DPlacedHarvesterState devices;
        private TopDown3DItemCatalog catalog;
        private TopDown3DItemDefinition tool, dust;
        private Sprite icon;
        private TopDown3DInventoryState kit, cargo;
        [SetUp] public void Setup()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene);
            icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            tool = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            tool.Configure(TopDown3DHarvesterSettings.CanisterItemId, "Canister", "Tool", "Equipment", icon, 1, .8f);
            tool.ConfigureLoadout(TopDown3DEquipmentSlot.DustCanister);
            dust = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            dust.Configure(TopDown3DHarvesterSettings.DustItemId, "Dust", "Resource", "Resource", icon, 100, .05f);
            catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>(); catalog.Configure(new[] { tool, dust });
            kit = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 10, loadout: true, canisterContents: true));
            cargo = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 12, canisterContents: true));
            owner = new GameObject("Device authority"); devices = owner.AddComponent<TopDown3DPlacedHarvesterState>();
            var settings = TopDown3DHarvesterSettings.Load(); Assert.That(settings, Is.Not.Null, "Hydrate dynamic harvester settings.");
            devices.ConfigureAuthored(TopDown3DPlacedHarvesterState.AuthoredWorldIdentity, settings);
            devices.ConfigureCanisterContents(true);
        }
        [TearDown] public void Cleanup()
        { UnityEngine.Object.DestroyImmediate(owner); UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(tool); UnityEngine.Object.DestroyImmediate(dust); UnityEngine.Object.DestroyImmediate(icon);
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene); }
        private void Clock(double now) => typeof(TopDown3DPlacedHarvesterState).GetField("simulationSeconds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(devices, now);

        [Test] public void PickupAndRedeploymentPreserveIdentityAndDoNotCollectWhileCarried()
        {
            var id = Guid.NewGuid().ToString("N");
            Assert.That(kit.TryAdd(new TopDown3DItemAmount(tool.ItemId, 1, id, TopDown3DCanisterContents.Create())).Succeeded, Is.True);
            Assert.That(devices.TryPlaceCanister(Vector3.zero, kit, 0, () => true, out var placement), Is.True);
            Assert.That(kit.OccupiedSlotCount, Is.Zero);
            Clock(13);
            Assert.That(devices.TryPickup(placement, kit, cargo, out var collected, () => true), Is.True);
            Assert.That(collected, Is.EqualTo(1));
            Assert.That(cargo.Slots[0].InstanceId, Is.EqualTo(id));
            Assert.That(cargo.Slots[0].CanisterContents.fractionalSeconds, Is.EqualTo(3));
            Clock(1013);
            Assert.That(cargo.Slots[0].CanisterContents.dust, Is.EqualTo(1));
            Assert.That(devices.TryPlaceCanister(Vector3.one, cargo, 0, () => true, out var secondPlacement), Is.True);
            Clock(1020);
            Assert.That(devices.TryGet(secondPlacement, out var record), Is.True);
            Assert.That(record.EquipmentInstanceId, Is.EqualTo(id));
            Assert.That(record.Dust, Is.EqualTo(2));
            Assert.That(record.FractionalSeconds, Is.Zero);
        }

        [Test] public void EmptyPickupFallsBackToAccessibleCargoWhenNamedKitSlotIsOccupied()
        {
            var starter = Guid.NewGuid().ToString("N"); var spare = Guid.NewGuid().ToString("N");
            Assert.That(kit.TryAdd(new TopDown3DItemAmount(tool.ItemId, 1, starter, TopDown3DCanisterContents.Create())).Succeeded, Is.True);
            Assert.That(cargo.TryAdd(new TopDown3DItemAmount(tool.ItemId, 1, spare, TopDown3DCanisterContents.Create(0, 3))).Succeeded, Is.True);
            Assert.That(devices.TryPlaceCanister(Vector3.zero, cargo, 0, () => true, out var placement), Is.True);
            Assert.That(devices.TryPickup(placement, kit, cargo, out _, () => false), Is.False);
            Assert.That(devices.DeployedCount, Is.EqualTo(1));
            Assert.That(devices.TryPickup(placement, kit, cargo, out _, () => true), Is.True);
            Assert.That(kit.Slots[0].InstanceId, Is.EqualTo(starter)); Assert.That(cargo.Slots[0].InstanceId, Is.EqualTo(spare));
            Assert.That(cargo.Slots[0].CanisterContents.fractionalSeconds, Is.EqualTo(3));
        }

        [Test] public void AccessAndCapacityFailuresNeverRemovePlacedCanister()
        {
            var id = Guid.NewGuid().ToString("N");
            cargo.TryAdd(new TopDown3DItemAmount(tool.ItemId, 1, id, TopDown3DCanisterContents.Create(100)));
            Assert.That(devices.TryPlaceCanister(Vector3.zero, cargo, 0, () => false, out _), Is.False);
            Assert.That(cargo.Slots[0].Quantity, Is.EqualTo(1));
            Assert.That(devices.TryPlaceCanister(Vector3.zero, cargo, 0, () => true, out var placement), Is.True);
            Assert.That(devices.TryPickup(placement, kit, cargo, out _, () => false), Is.False);
            Assert.That(devices.DeployedCount, Is.EqualTo(1));
            var heavyLimit = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 12, 5, canisterContents: true));
            Assert.That(devices.TryPickup(placement, kit, heavyLimit, out _, () => true), Is.False);
            Assert.That(devices.DeployedCount, Is.EqualTo(1));
            Assert.That(devices.TryGet(placement, out var record), Is.True);
            Assert.That(record.Dust, Is.EqualTo(100));
            Assert.That(devices.TryPickup(placement, kit, cargo, out _, () => true), Is.True);
            Assert.That(devices.TryPickup(placement, kit, cargo, out _, () => true), Is.False);
            Assert.That(cargo.Slots[0].CanisterContents.dust, Is.EqualTo(100));
        }
    }
}
