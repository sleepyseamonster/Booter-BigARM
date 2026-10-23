using System.Collections.Generic;
using System.Linq;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DCampWorkstationUiController
    {
        private int firstToolRecipe;
        private long recipeChestRevision = -1, recipePlayerRevision = -1;
        private CampFirstToolScenario FirstTool => CampFirstToolScenario.Find(gameObject.scene);
        private bool FirstToolRecipes => management == ManagementView.Detail && target is TopDown3DCampWorkbench bench && bench.HasRecipeRoster;
        private CampFirstToolProgress FirstToolProgress => CampFirstToolProgress.For(FirstTool, session);
        private void PrepareFirstToolView()
        {
            if (FirstTool == null) return;
            if (target is TopDown3DResourceCache) { source = 1; destination = 2; message = "Move each gathered stack from LEGGER to CAMP CHEST, then use the workbench."; }
            if (target is TopDown3DCampWorkbench)
            { SelectNextFirstToolRecipe(); message = FirstToolProgress.Complete ? "Your multi-tool is equipped." : "Next useful step selected. Craft READY steps; inputs must be in this chest."; }
        }
        private void SelectNextFirstToolRecipe()
        {
            var next = FirstToolProgress.Next(session.Cache.State, session.Kit.State);
            firstToolRecipe = next >= 0 ? next : session.Workbench.Recipes.Count - 1;
            recipeChestRevision = session.Cache.State.Revision; recipePlayerRevision = session.Kit.State.Revision;
        }
        private void SelectFirstToolRecipe(int index)
        {
            if (!CanCommand || index < 0 || index >= session.Workbench.Recipes.Count) return;
            firstToolRecipe = index; recipeChestRevision = session.Cache.State.Revision; recipePlayerRevision = session.Kit.State.Revision;
            resetPending = false; message = "Select a READY step, then Craft. All inputs come from the camp chest."; Arm(); Refresh();
        }
        private void CraftFirstToolRecipe()
        {
            if (recipeChestRevision != session.Cache.State.Revision || recipePlayerRevision != session.Kit.State.Revision)
            { message = "Contents changed. Select the recipe again."; recipeChestRevision = -1; Arm(); Refresh(); return; }
            if (!FirstToolProgress.CanCraft(firstToolRecipe, session.Cache.State, session.Kit.State))
            { message = "This step is complete, lacks chest materials, or its output does not fit."; Arm(); Refresh(); return; }
            var selectedRecipe = session.Workbench.Recipes[firstToolRecipe];
            var result = session.Workbench.CraftSelected(session.Kit, firstToolRecipe);
            message = result.Succeeded ? "Crafted " + session.Kit.ItemCatalog.GetRequiredDefinition(selectedRecipe.Output.ItemId).DisplayName + "." : result.Message;
            if (result.Succeeded) { SelectNextFirstToolRecipe(); if (!FirstToolProgress.Complete) message += " Next step selected."; }
            Arm(); Refresh();
        }
        private string FirstToolBenchSummary()
        {
            var progress = FirstToolProgress;
            if (progress.Complete) return "Multi-tool equipped.\nThe first gathering/crafting loop is complete.";
            var next = progress.Next(session.Cache.State, session.Kit.State);
            return "First multi-tool · eight steps\nNext: " + (next >= 0 ? CampFirstToolProgress.ShortName(session.Workbench.Recipes[next].Output.ItemId) : "inspect materials")
                + "\nGather → Legger → camp chest → workbench.\nNo battery or sorter is needed for this tool.";
        }
        private void PresentFirstToolRecipes()
        {
            var recipes = session.Workbench.Recipes; var progress = FirstToolProgress;
            if (firstToolRecipe < 0 || firstToolRecipe >= recipes.Count) SelectNextFirstToolRecipe();
            var labels = new List<string>();
            for (var i = 0; i < recipes.Count; i++) labels.Add((i + 1) + ". " + CampFirstToolProgress.ShortName(recipes[i].Output.ItemId) + " · " + progress.Status(i, session.Cache.State, session.Kit.State));
            var recipe = recipes[firstToolRecipe]; var output = session.Kit.ItemCatalog.GetRequiredDefinition(recipe.Output.ItemId);
            var detail = output.DisplayName + "\n" + progress.Status(firstToolRecipe, session.Cache.State, session.Kit.State) + "\n";
            foreach (var ingredient in recipe.Ingredients)
                detail += CampFirstToolProgress.ShortName(ingredient.itemId) + ": " + CampFirstToolProgress.Count(session.Cache.State, ingredient.itemId) + "/" + ingredient.quantity + " in chest\n";
            detail += "Output: " + (output.IsEquipment ? "Booter multi-tool slot" : "Camp chest");
            if (progress.Complete) detail = "Bone salvage multi-tool\nEQUIPPED ON BOOTER\nAll eight crafting steps are complete.\nNo further materials are needed.";
            else if (!progress.Needed[firstToolRecipe]) detail = output.DisplayName + "\nDONE · already supplied\nStored or used in a later step.\nChoose the next READY row.";
            var notice = progress.Complete ? "Multi-tool equipped. Loop complete. Use the confirmed Inventory Trial reset to start again."
                : "Craft READY steps in order. Parts return to this chest; the final tool equips on Booter.\n"
                    + (progress.MissingRaw.Count > 0 ? "Still gathering: " + string.Join(", ", progress.MissingRaw.Select(p => p.Value + " " + CampFirstToolProgress.ShortName(p.Key)))
                        : session.Cargo.State.Slots.Any(s => !s.IsEmpty && recipes.Any(r => r.Ingredients.Any(i => i.itemId == s.ItemId)))
                            ? "All raw inputs collected. Transfer Legger's materials into CAMP CHEST."
                            : "Materials are at camp. Continue with the next READY step.");
            if (!string.IsNullOrEmpty(message)) notice += "\n" + message;
            view.Present("CAMP · FIRST MULTI-TOOL", detail, notice, labels, firstToolRecipe, true, true); view.SetLargeText(menu.LargeText);
            foreach (var id in new[] { "Source", "Destination", "Insert", "Refine", "Battery", "Canister", "Deploy", "Transfer", "Reset" }) view.SetCommand(id, false, false);
            var fresh = recipeChestRevision == session.Cache.State.Revision && recipePlayerRevision == session.Kit.State.Revision;
            view.SetCommand("Craft", fresh && progress.CanCraft(firstToolRecipe, session.Cache.State, session.Kit.State), true,
                progress.Complete ? "MULTI-TOOL COMPLETE" : "CRAFT STEP " + (firstToolRecipe + 1));
            PresentManagement();
        }
    }
}
