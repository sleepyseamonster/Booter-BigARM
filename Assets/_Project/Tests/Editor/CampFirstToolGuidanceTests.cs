using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.Tests
{
    public sealed class CampFirstToolGuidanceTests
    {
        private TopDown3DItemCatalog catalog;
        private TopDown3DCraftingRecipe[] recipes;
        private TopDown3DInventoryState chest, cargo, player;
        [SetUp] public void Setup()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DCampLoopLoadoutProfile>(CampFirstToolAuthoring.Settings + "/FirstToolLoadout.asset");
            catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>();
            catalog.Configure(profile.sourceCatalog.Definitions.Concat(profile.definitions).GroupBy(d => d.ItemId).Select(g => g.Last()));
            recipes = Enumerable.Range(0, 8).Select(i => AssetDatabase.LoadAssetAtPath<TopDown3DCraftingRecipe>(CampFirstToolAuthoring.Settings + "/Recipe" + i + ".asset")).ToArray();
            chest = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, 24, campStorage: true));
            cargo = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 24));
            player = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 10, loadout: true));
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(catalog);
        private CampFirstToolProgress Progress() => new(recipes, player, cargo, chest);
        private void Bill(TopDown3DInventoryState store)
        {
            foreach (var item in new[] { new TopDown3DItemAmount("proposal.bio.jaw", 1), new TopDown3DItemAmount("author.bio.stone_bone", 1),
                new TopDown3DItemAmount("proposal.bio.shell", 2), new TopDown3DItemAmount("resource.ironstone_ore", 2) })
                Assert.That(store.TryAdd(item).Succeeded, Is.True);
        }
        private void Craft(int index)
        { Assert.That(TopDown3DCraftingService.Craft(recipes[index], chest, index == 7 ? player : chest, () => true).Succeeded, Is.True); }

        [Test] public void FreshStartExplainsTheExactRawBill()
        {
            var p = Progress(); Assert.That(p.Valid, Is.True); Assert.That(p.Complete, Is.False);
            Assert.That(p.MissingRaw.Count, Is.EqualTo(4));
            Assert.That(p.MissingRaw["proposal.bio.jaw"], Is.EqualTo(1)); Assert.That(p.MissingRaw["author.bio.stone_bone"], Is.EqualTo(1));
            Assert.That(p.MissingRaw["proposal.bio.shell"], Is.EqualTo(2)); Assert.That(p.MissingRaw["resource.ironstone_ore"], Is.EqualTo(2));
            Assert.That(p.Next(chest, player), Is.Zero); Assert.That(p.CanCraft(0, chest, player), Is.False);
        }
        [Test] public void CargoMaterialsStopGatherRequestsButCannotCraftRemotely()
        {
            Bill(cargo); var p = Progress();
            Assert.That(p.MissingRaw, Is.Empty); Assert.That(Enumerable.Range(0, 8).Any(i => p.CanCraft(i, chest, player)), Is.False);
            Assert.That(chest.OccupiedSlotCount, Is.Zero);
        }
        [Test] public void FullRecipeSequenceAdvancesWithoutAskingForConsumedMaterialsAgain()
        {
            Bill(chest);
            for (int i = 0; i < 8; i++)
            {
                var p = Progress(); var revision = chest.Revision;
                Assert.That(p.MissingRaw, Is.Empty, "Step " + i); Assert.That(p.Next(chest, player), Is.EqualTo(i));
                Assert.That(p.CanCraft(i, chest, player), Is.True); Assert.That(chest.Revision, Is.EqualTo(revision), "Guidance must not publish a mutation");
                for (int j = 0; j < i; j++) Assert.That(p.Needed[j], Is.False, "Consumed precursor requested again at " + i);
                Craft(i);
            }
            var done = Progress(); Assert.That(done.Complete, Is.True); Assert.That(done.Needed.Any(n => n), Is.False);
            Assert.That(done.Next(chest, player), Is.EqualTo(-1)); Assert.That(done.CanCraft(7, chest, player), Is.False);
            Assert.That(chest.OccupiedSlotCount, Is.Zero); Assert.That(CampFirstToolProgress.Count(player, CampFirstToolScenario.ToolId), Is.EqualTo(1));
        }
        [Test] public void SharedShellBranchesCountTheRemainingPieceOnce()
        {
            Assert.That(chest.TryAdd(new TopDown3DItemAmount("proposal.bio.shell", 1)).Succeeded, Is.True); Craft(2);
            var p = Progress(); Assert.That(p.Needed[2], Is.False); Assert.That(p.Needed[3], Is.True);
            Assert.That(p.MissingRaw["proposal.bio.shell"], Is.EqualTo(1));
        }
        [Test] public void OccupiedEquipmentSlotDisablesFinalCraftWithoutConsumingParts()
        {
            Bill(chest); for (int i = 0; i < 7; i++) Craft(i);
            Assert.That(player.TryAdd(new TopDown3DItemAmount("equipment.digging_tool", 1)).Succeeded, Is.True);
            var before = JsonUtility.ToJson(chest.CaptureSnapshot()); var p = Progress();
            Assert.That(p.Complete, Is.False); Assert.That(p.CanCraft(7, chest, player), Is.False);
            Assert.That(p.Status(7, chest, player), Is.EqualTo("OUTPUT BLOCKED"));
            Assert.That(JsonUtility.ToJson(chest.CaptureSnapshot()), Is.EqualTo(before));
        }
        [Test] public void RecipeLayoutSupportsLargeTextAndRestoresTheStorageGrid()
        {
            var go = new GameObject("Recipe layout", typeof(RectTransform));
            try
            {
                var view = go.AddComponent<TopDown3DCampWorkstationCanvas>(); view.Initialize(_ => { }, _ => { });
                var labels = recipes.Select((r, i) => (i + 1) + ". " + CampFirstToolProgress.ShortName(r.Output.ItemId) + " · NEED MATERIALS / SPACE").ToArray();
                view.Present("FIRST TOOL", "Recipe", "Instructions", labels, 0, true, true); view.SetLargeText(true); Canvas.ForceUpdateCanvases();
                var grid = go.GetComponentInChildren<GridLayoutGroup>(true); Assert.That(grid.constraintCount, Is.EqualTo(1));
                foreach (var row in grid.GetComponentsInChildren<Button>(true))
                    Assert.That(row.GetComponentInChildren<Text>().preferredHeight, Is.LessThanOrEqualTo(grid.cellSize.y));
                view.Present("CHEST", "Store", "Transfer", new[] { "Resource" }, 0, true);
                Assert.That(grid.constraintCount, Is.EqualTo(2)); Assert.That(grid.cellSize.y, Is.EqualTo(38));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void GuideViewTogglesDoNotDuplicateCanvasOrCaptureInput()
        {
            var host = new GameObject("Guidance lifecycle");
            try
            {
                host.AddComponent<CampFirstToolScenario>(); var guide = host.AddComponent<CampFirstToolGuideHud>();
                // Ordinary runtime MonoBehaviours do not receive these callbacks in EditMode tests.
                void Lifecycle(string method) => typeof(CampFirstToolGuideHud).GetMethod(method,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(guide, null);
                Lifecycle("OnEnable");
                for (int i = 0; i < 2; i++) { host.SetActive(false); Lifecycle("OnDisable"); host.SetActive(true); Lifecycle("OnEnable"); }
                Assert.That(host.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
                Assert.That(host.GetComponentsInChildren<GraphicRaycaster>(true), Is.Empty);
                Assert.That(host.GetComponentInChildren<CanvasGroup>(true).blocksRaycasts, Is.False);
                Assert.That(guide.Visible, Is.False, "An unready scenario must not expose guidance");
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
