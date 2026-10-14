using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CanisterInstanceInventoryTests
    {
        private TopDown3DItemCatalog catalog;
        private TopDown3DItemDefinition tool, dust;
        private Sprite icon;
        [SetUp] public void Setup()
        {
            icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            tool = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            tool.Configure(TopDown3DHarvesterSettings.CanisterItemId, "Canister", "Tool", "Equipment", icon, 1, .8f);
            tool.ConfigureLoadout(TopDown3DEquipmentSlot.DustCanister);
            dust = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            dust.Configure(TopDown3DHarvesterSettings.DustItemId, "Dust", "Resource", "Resource", icon, 100, .05f);
            catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>(); catalog.Configure(new[] { tool, dust });
        }
        [TearDown] public void Cleanup()
        { UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(tool); UnityEngine.Object.DestroyImmediate(dust); UnityEngine.Object.DestroyImmediate(icon); }
        private TopDown3DInventoryState Cargo(int capacity = 12, float maximumMass = 0) => new(catalog,
            new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, capacity, maximumMass, canisterContents: true));
        private TopDown3DItemAmount Canister(string id, int amount, double remainder = 0) =>
            new(tool.ItemId, 1, id, TopDown3DCanisterContents.Create(amount, remainder));

        [TestCase(13, 0d)] [TestCase(0, 3d)]
        public void LegacyPayloadWithoutPresenceFlagCannotSilentlyDiscardDustOrRemainder(int amount, double remainder)
        {
            var state = Cargo(); state.TryAdd(Canister(Guid.NewGuid().ToString("N"), amount, remainder));
            var before = JsonUtility.ToJson(state.CaptureSnapshot());
            var malformed = before.Replace("\"version\":3", "\"version\":2")
                .Replace("\"canisterContents\":{\"version\":1", "\"canisterContents\":{\"version\":0");
            Assert.That(state.ApplySnapshot(JsonUtility.FromJson<TopDown3DInventorySnapshot>(malformed)).Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(state.CaptureSnapshot()), Is.EqualTo(before));
        }

        [Test] public void ExactTransfersPreserveTwoDifferentFillsAndDoNotMerge()
        {
            var cargo = Cargo(); var chest = new TopDown3DInventoryState(catalog,
                new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, 24, campStorage: true, canisterContents: true));
            var first = Guid.NewGuid().ToString("N"); var second = Guid.NewGuid().ToString("N");
            Assert.That(cargo.TryAdd(Canister(first, 13, 3)).Succeeded, Is.True);
            Assert.That(cargo.TryAdd(Canister(second, 79, 2)).Succeeded, Is.True);
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(cargo, chest, 1, () => true).Succeeded, Is.True);
            Assert.That(chest.Slots[0].InstanceId, Is.EqualTo(second));
            Assert.That(chest.Slots[0].CanisterContents.dust, Is.EqualTo(79));
            Assert.That(cargo.Slots[0].CanisterContents.dust, Is.EqualTo(13));
            Assert.That(cargo.TryAdd(Canister(first, 1)).Succeeded, Is.False);
            Assert.That(cargo.TryRemove(tool.ItemId, 1).Succeeded, Is.False);
        }

        [Test] public void FilledCanisterCannotEnterBooterOrAnOrdinaryOwnerAndWeightIncludesDust()
        {
            var source = Cargo(); var id = Guid.NewGuid().ToString("N"); source.TryAdd(Canister(id, 100));
            Assert.That(source.CalculateMass(), Is.EqualTo(5.8f).Within(.001f));
            var kit = new TopDown3DInventoryState(catalog,
                new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 10, loadout: true, canisterContents: true));
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(source, kit, 0).Succeeded, Is.False);
            Assert.That(source.Slots[0].Quantity, Is.EqualTo(1));
            var fullWeight = Cargo(12, 5);
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(source, fullWeight, 0).Succeeded, Is.False);
            Assert.That(source.Slots[0].CanisterContents.dust, Is.EqualTo(100));
            var legacy = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 12));
            Assert.That(legacy.TryAdd(Canister(Guid.NewGuid().ToString("N"), 1)).Succeeded, Is.False);
            Assert.That(source.SetCanisterContents(0, TopDown3DCanisterContents.Create(0, 3)).Succeeded, Is.True);
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(source, kit, 0).Succeeded, Is.True);
            Assert.That(kit.Slots[0].CanisterContents.fractionalSeconds, Is.EqualTo(3));
        }

        [Test] public void DraftSwapAndJsonRoundTripKeepContentsAndMalformedSnapshotDoesNotApply()
        {
            var state = Cargo(); var first = Guid.NewGuid().ToString("N"); var second = Guid.NewGuid().ToString("N");
            state.TryAdd(Canister(first, 13, 3)); state.TryAdd(Canister(second, 79, 2));
            var draft = state.CreateDraft(); Assert.That(draft.TryMoveOrSwap(0, 2).Succeeded, Is.True);
            Assert.That(draft.Slots[2].InstanceId, Is.EqualTo(first));
            Assert.That(draft.Slots[2].CanisterContents.dust, Is.EqualTo(13));
            Assert.That(state.Slots[0].InstanceId, Is.EqualTo(first));
            Assert.That(state.CaptureSnapshot().Version, Is.EqualTo(3));
            var text = JsonUtility.ToJson(draft.CaptureSnapshot());
            var copy = Cargo(); Assert.That(copy.ApplySnapshot(JsonUtility.FromJson<TopDown3DInventorySnapshot>(text)).Succeeded, Is.True);
            Assert.That(copy.Slots[2].CanisterContents.fractionalSeconds, Is.EqualTo(3));
            var before = JsonUtility.ToJson(copy.CaptureSnapshot());
            var malformed = text.Replace("\"dust\":13", "\"dust\":101");
            Assert.That(copy.ApplySnapshot(JsonUtility.FromJson<TopDown3DInventorySnapshot>(malformed)).Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(copy.CaptureSnapshot()), Is.EqualTo(before));
        }

        [Test] public void AutoPackPreservesIdentityFillAndWeightedMass()
        {
            var state = Cargo(); var first = Guid.NewGuid().ToString("N"); var second = Guid.NewGuid().ToString("N");
            state.TryAdd(Canister(first, 100)); state.TryAdd(Canister(second, 13, 3));
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>("Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset");
            Assert.That(TopDown3DCargoAutoPacker.TryPack(state, settings, out var snapshot), Is.True);
            Assert.That(state.ApplySnapshot(snapshot).Succeeded, Is.True);
            Assert.That(state.Slots.Single(s => s.InstanceId == first).CanisterContents.dust, Is.EqualTo(100));
            Assert.That(state.Slots.Single(s => s.InstanceId == second).CanisterContents.fractionalSeconds, Is.EqualTo(3));
            Assert.That(state.CalculateMass(), Is.EqualTo(7.25f).Within(.001f));
        }
    }
}
