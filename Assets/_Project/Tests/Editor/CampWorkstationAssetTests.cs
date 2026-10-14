using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CampWorkstationAssetTests
    {
        [Test] public void BatteryAssetsFitSupplyPocketsAndHaveRequiredIcons()
        {
            foreach (var path in new[] { CampWorkstationAssetAuthoring.EmptyPath, CampWorkstationAssetAuthoring.ChargedPath })
            {
                var item = AssetDatabase.LoadAssetAtPath<TopDown3DItemDefinition>(path);
                Assert.That(item, Is.Not.Null, path);
                Assert.That(item.TryValidate(out var reason), Is.True, reason);
                Assert.That(item.IsFieldSupply, Is.True);
                Assert.That(item.IsEquipment || item.IsResource, Is.False);
                Assert.That(item.MaxStack, Is.EqualTo(1));
                Assert.That(TopDown3DLoadoutLayout.Accepts(6, item), Is.True);
                Assert.That(TopDown3DLoadoutLayout.Accepts(0, item), Is.False);
            }
        }
        [Test] public void StarterRecipeContainsExactOreCostAndAnEmptyBatteryOutput()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<TopDown3DCraftingRecipe>(CampWorkstationAssetAuthoring.RecipePath);
            Assert.That(recipe.Ingredients.Count, Is.EqualTo(1));
            Assert.That(recipe.Ingredients[0].itemId, Is.EqualTo("resource.ironstone_ore"));
            Assert.That(recipe.Ingredients[0].quantity, Is.EqualTo(4));
            Assert.That(recipe.Output.ItemId, Is.EqualTo(TopDown3DDustRefineryState.EmptyBatteryId));
            Assert.That(recipe.Output.Quantity, Is.EqualTo(1));
        }
        [Test] public void OwnedPrefabsHavePhysicalHostsAndDormantSocketVisuals()
        {
            var bench = AssetDatabase.LoadAssetAtPath<GameObject>(CampWorkstationAssetAuthoring.Prefabs + "/CampWorkbench.prefab");
            var refinery = AssetDatabase.LoadAssetAtPath<GameObject>(CampWorkstationAssetAuthoring.Prefabs + "/DustRefinery.prefab");
            Assert.That(bench.GetComponent<TopDown3DCampWorkbench>().Recipe, Is.Not.Null);
            Assert.That(bench.GetComponentsInChildren<Collider>(true).Length, Is.GreaterThan(0));
            Assert.That(refinery.GetComponent<TopDown3DDustRefinery>().CaptureState().IsValid, Is.True);
            Assert.That(refinery.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
            Assert.That(refinery.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Docked Canister").gameObject.activeSelf, Is.False);
            Assert.That(refinery.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Docked Battery").gameObject.activeSelf, Is.False);
        }
    }
}
