using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CampCraftingTests
    {
        private TopDown3DItemCatalog catalog;
        private TopDown3DCraftingRecipe recipe;
        private Sprite icon;
        private TopDown3DItemDefinition ore, battery;
        [SetUp] public void Setup()
        {
            icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            ore = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            ore.Configure("resource.ironstone_ore", "Ore", "Ore", "Resource", icon, 99, 1.25f);
            battery = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            battery.Configure(TopDown3DDustRefineryState.EmptyBatteryId, "Empty battery", "Battery", "Supply", icon, 1, .8f);
            battery.ConfigureLoadout(TopDown3DEquipmentSlot.None, true);
            catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>();
            catalog.Configure(new[] { ore, battery });
            recipe = ScriptableObject.CreateInstance<TopDown3DCraftingRecipe>();
            recipe.Configure("test-battery", new[] { new TopDown3DCraftingRecipe.Ingredient(ore.ItemId, 4) }, battery.ItemId);
        }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(recipe); Object.DestroyImmediate(catalog); Object.DestroyImmediate(ore); Object.DestroyImmediate(battery); Object.DestroyImmediate(icon); }

        [Test] public void BatteryFitsSupplyPocketAndConsumesExactlyOneRecipe()
        {
            var source = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, 24));
            var destination = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 10, loadout: true));
            Assert.That(source.TryAdd(new TopDown3DItemAmount(ore.ItemId, 5)).Succeeded, Is.True);
            Assert.That(TopDown3DCraftingService.Craft(recipe, source, destination, () => true).Succeeded, Is.True);
            Assert.That(source.Slots[0].Quantity, Is.EqualTo(1));
            Assert.That(destination.Slots[6].ItemId, Is.EqualTo(battery.ItemId));
            Assert.That(destination.Slots[0].IsEmpty, Is.True);
        }

        [Test] public void MissingIngredientsAndFullOutputPreserveBothOwners()
        {
            var source = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, 24));
            var destination = new TopDown3DInventoryState(catalog, 1);
            source.TryAdd(new TopDown3DItemAmount(ore.ItemId, 3));
            var before = JsonUtility.ToJson(source.CaptureSnapshot());
            Assert.That(TopDown3DCraftingService.Craft(recipe, source, destination, () => true).Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(source.CaptureSnapshot()), Is.EqualTo(before));
            source.TryAdd(new TopDown3DItemAmount(ore.ItemId, 1));
            destination.TryAdd(new TopDown3DItemAmount(battery.ItemId, 1));
            before = JsonUtility.ToJson(source.CaptureSnapshot());
            Assert.That(TopDown3DCraftingService.Craft(recipe, source, destination, () => true).Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(source.CaptureSnapshot()), Is.EqualTo(before));
        }

        [Test] public void AccessLostAtCommitDoesNotConsumeIngredients()
        {
            var source = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, 24));
            var destination = new TopDown3DInventoryState(catalog, 1);
            source.TryAdd(new TopDown3DItemAmount(ore.ItemId, 4));
            var calls = 0;
            Assert.That(TopDown3DCraftingService.Craft(recipe, source, destination, () => ++calls == 1).Succeeded, Is.False);
            Assert.That(source.Slots[0].Quantity, Is.EqualTo(4));
            Assert.That(destination.OccupiedSlotCount, Is.Zero);
        }

        [Test] public void DataDrivenEquipmentRecipeCreatesOneCanonicalInstanceInItsNamedSlot()
        {
            var tool = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            try
            {
                tool.Configure("tool.test_digging", "Digging tool", "Tool", "Tool", icon, 1, 1);
                tool.ConfigureLoadout(TopDown3DEquipmentSlot.DiggingTool);
                catalog.Configure(new[] { ore, battery, tool });
                recipe.Configure("test-tool", new[] { new TopDown3DCraftingRecipe.Ingredient(ore.ItemId, 4) }, tool.ItemId);
                var source = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, 24));
                var destination = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 10, loadout: true));
                source.TryAdd(new TopDown3DItemAmount(ore.ItemId, 4));
                Assert.That(TopDown3DCraftingService.Craft(recipe, source, destination, () => true).Succeeded, Is.True);
                Assert.That(destination.Slots[3].ItemId, Is.EqualTo(tool.ItemId));
                Assert.That(System.Guid.TryParseExact(destination.Slots[3].InstanceId, "N", out _), Is.True);
                Assert.That(destination.OccupiedSlotCount, Is.EqualTo(1));
                Assert.That(source.OccupiedSlotCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(tool); }
        }
    }
}
