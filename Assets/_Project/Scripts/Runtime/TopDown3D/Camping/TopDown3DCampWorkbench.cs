using UnityEngine;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DCampWorkbench : MonoBehaviour, ITopDown3DInteractable
    {
        [SerializeField] private string stationId = "camp-loop:workbench:v1";
        [SerializeField] private TopDown3DCraftingRecipe recipe;
        [SerializeField] private TopDown3DResourceCache chest;
        public TopDown3DCraftingRecipe Recipe => recipe;
        public IReadOnlyList<TopDown3DCraftingRecipe> Recipes => CampFirstToolScenario.Find(gameObject.scene)?.Recipes
            ?? new[] { recipe };
        public bool HasRecipeRoster => CampFirstToolScenario.Find(gameObject.scene) != null;
        public TopDown3DResourceCache Chest => chest;
        public string StableId => stationId;
        public Object UnityObject => this;
        public string Prompt => "Use Workbench";
        public Vector3 InteractionPoint => transform.position + Vector3.up * .8f;
        public float InteractionRange => 2.8f;
        public float ActionDuration => .01f;
        public TopDown3DItemAmount Reward => default;
        public bool IsAvailable => isActiveAndEnabled && recipe != null && chest != null;
        public bool TryConsume(out int remainingUses) { remainingUses = 1; return false; }
        public bool CanAccess(Transform actor) => IsAvailable && actor != null && actor.gameObject.scene == gameObject.scene
            && Vector3.Distance(actor.position, InteractionPoint) <= InteractionRange
            && TopDown3DCargoAccess.HasLineOfSight(actor, transform) && chest.CanAccess(actor);

        public TopDown3DInventoryResult Craft(TopDown3DPlayerInventory player) =>
            player == null ? TopDown3DCraftingService.Failure("Player inventory is unavailable.")
                : TopDown3DCraftingService.Craft(recipe, chest.State, player.State, () => CanAccess(player.transform));

        public TopDown3DInventoryResult CraftSelected(TopDown3DPlayerInventory player, int index)
        {
            if (player == null || index < 0 || index >= Recipes.Count) return TopDown3DCraftingService.Failure("Select a valid recipe.");
            var selected = Recipes[index];
            var output = player.ItemCatalog.GetRequiredDefinition(selected.Output.ItemId);
            return TopDown3DCraftingService.Craft(selected, chest.State, output.IsEquipment ? player.State : chest.State,
                () => isActiveAndEnabled && index < Recipes.Count && Recipes[index] == selected && CanAccess(player.transform));
        }

        internal void Configure(TopDown3DCraftingRecipe value, TopDown3DResourceCache storage)
        { recipe = value; chest = storage; }
    }
}
