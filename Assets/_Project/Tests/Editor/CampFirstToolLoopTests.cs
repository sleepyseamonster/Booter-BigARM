using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CampFirstToolLoopTests
    {
        private Sprite icon;
        private TopDown3DItemDefinition ore, stock;
        private TopDown3DItemCatalog catalog;
        private TopDown3DCraftingRecipe recipe;
        [SetUp] public void Setup()
        {
            icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            ore = ScriptableObject.CreateInstance<TopDown3DItemDefinition>(); ore.Configure("test.ore", "Ore", "Ore", "Resource", icon, 20, .2f);
            stock = ScriptableObject.CreateInstance<TopDown3DItemDefinition>(); stock.Configure("test.stock", "Stock", "Stock", "Resource", icon, 20, .2f);
            catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>(); catalog.Configure(new[] { ore, stock });
            recipe = ScriptableObject.CreateInstance<TopDown3DCraftingRecipe>();
            recipe.Configure("test.same-store", new[] { new TopDown3DCraftingRecipe.Ingredient(ore.ItemId, 1) }, stock.ItemId);
        }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(recipe); Object.DestroyImmediate(catalog); Object.DestroyImmediate(ore); Object.DestroyImmediate(stock); Object.DestroyImmediate(icon); }
        private TopDown3DInventoryState Chest(int capacity = 2) => new(catalog,
            new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, capacity, campStorage: true));

        [Test] public void SameStoreCraftReusesFreedSlotAndPublishesExactlyOnce()
        {
            var chest = Chest(1); chest.TryAdd(new TopDown3DItemAmount(ore.ItemId, 1));
            var changed = 0; chest.Changed += () => changed++;
            Assert.That(TopDown3DCraftingService.Craft(recipe, chest, chest, () => true).Succeeded, Is.True);
            Assert.That(chest.Slots[0].ItemId, Is.EqualTo(stock.ItemId)); Assert.That(chest.Slots[0].Quantity, Is.EqualTo(1));
            Assert.That(changed, Is.EqualTo(1));
            var before = JsonUtility.ToJson(chest.CaptureSnapshot());
            Assert.That(TopDown3DCraftingService.Craft(recipe, chest, chest, () => true).Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(chest.CaptureSnapshot()), Is.EqualTo(before)); Assert.That(changed, Is.EqualTo(1));
        }
        [Test] public void SameStoreFullOutputPreservesAllInput()
        {
            var chest = Chest(1); chest.TryAdd(new TopDown3DItemAmount(ore.ItemId, 2));
            var before = JsonUtility.ToJson(chest.CaptureSnapshot());
            Assert.That(TopDown3DCraftingService.Craft(recipe, chest, chest, () => true).Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(chest.CaptureSnapshot()), Is.EqualTo(before));
        }
        [Test] public void SameStoreAccessLostDuringCommitPreservesAllInput()
        {
            var chest = Chest(); chest.TryAdd(new TopDown3DItemAmount(ore.ItemId, 1));
            var before = JsonUtility.ToJson(chest.CaptureSnapshot()); var checks = 0;
            Assert.That(TopDown3DCraftingService.Craft(recipe, chest, chest, () => ++checks == 1).Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(chest.CaptureSnapshot()), Is.EqualTo(before));
        }
        [Test] public void HarvestStockCannotDuplicateAndSurvivesSerialization()
        {
            var state = new CampFirstToolStock(); Assert.That(state.Consume(0), Is.True); Assert.That(state.Consume(0), Is.False);
            Assert.That(state.Consume(3), Is.True);
            var restored = JsonUtility.FromJson<CampFirstToolStock>(JsonUtility.ToJson(state));
            Assert.That(restored.Available(0), Is.False); Assert.That(restored.Available(3), Is.False); Assert.That(restored.Available(1), Is.True);
            Assert.That(restored.Available(-1), Is.False); Assert.That(restored.Available(6), Is.False);
            var epoch = restored.Epoch; restored.Reset(); Assert.That(restored.Epoch, Is.GreaterThan(epoch));
            for (int i = 0; i < CampFirstToolStock.Count; i++) Assert.That(restored.Available(i), Is.True);
        }
        [Test] public void InvalidRetainedStockFailsClosed()
        {
            var state = JsonUtility.FromJson<CampFirstToolStock>("{\"version\":2,\"consumed\":0,\"revision\":0}");
            Assert.That(state.Valid, Is.False); Assert.That(state.Consume(0), Is.False);
        }
        [Test] public void SourceAndOwnerTogglesPreserveSpentStockAndLivingBody()
        {
            var host = new GameObject("First-tool stock owner");
            try
            {
                var owner = host.AddComponent<CampFirstToolScenario>();
                var site = new GameObject("Living source"); site.transform.SetParent(host.transform);
                var body = new GameObject("Living body"); body.transform.SetParent(site.transform);
                var available = new GameObject("Available"); available.transform.SetParent(site.transform);
                var spent = new GameObject("Spent"); spent.transform.SetParent(site.transform);
                var source = site.AddComponent<CampFirstToolSource>();
                source.Configure(owner, 0, "test.live", ore.ItemId, "Jaw plates", 1, true, site.transform, null, available, spent, body.transform);
                Assert.That(owner.Stock.Consume(0), Is.True); var revision = owner.Stock.Revision;
                source.Refresh(); host.SetActive(false); host.SetActive(true); site.SetActive(false); site.SetActive(true); source.Refresh();
                Assert.That(owner.Stock.Available(0), Is.False); Assert.That(owner.Stock.Revision, Is.EqualTo(revision));
                Assert.That(available.activeSelf, Is.False); Assert.That(spent.activeSelf, Is.True);
                Assert.That(body.activeInHierarchy, Is.True); Assert.That(source.Health, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
