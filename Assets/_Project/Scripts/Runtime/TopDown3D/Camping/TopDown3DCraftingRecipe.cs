using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Top Down 3D/Crafting Recipe")]
    public sealed class TopDown3DCraftingRecipe : ScriptableObject
    {
        [Serializable]
        public struct Ingredient
        {
            public string itemId;
            public int quantity;
            public Ingredient(string id, int count) { itemId = id; quantity = count; }
        }

        [SerializeField] private string recipeId = "camp.empty-arc-battery.v1";
        [SerializeField] private int version = 1;
        [SerializeField] private Ingredient[] ingredients = Array.Empty<Ingredient>();
        [SerializeField] private string outputItemId = "equipment.arc_battery_empty";
        [SerializeField] private int outputQuantity = 1;

        public string RecipeId => recipeId;
        public int Version => version;
        public IReadOnlyList<Ingredient> Ingredients => ingredients;
        public TopDown3DItemAmount Output => new(outputItemId, outputQuantity);

        public bool TryValidate(TopDown3DItemCatalog catalog, out string reason)
        {
            reason = "Recipe configuration is invalid.";
            if (catalog == null || string.IsNullOrWhiteSpace(recipeId) || version < 1
                || ingredients == null || ingredients.Length == 0 || ingredients.Length > 16
                || outputQuantity <= 0 || !catalog.TryGetDefinition(outputItemId, out var output)
                || output.IsEquipment && outputQuantity != 1) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ingredient in ingredients)
                if (ingredient.quantity <= 0 || string.IsNullOrWhiteSpace(ingredient.itemId)
                    || !ids.Add(ingredient.itemId) || ingredient.itemId == outputItemId
                    || !catalog.TryGetDefinition(ingredient.itemId, out var definition) || definition.IsEquipment)
                    return false;
            reason = null;
            return true;
        }

        internal void Configure(string id, Ingredient[] inputs, string output, int quantity = 1)
        { recipeId = id; ingredients = inputs == null ? Array.Empty<Ingredient>() : (Ingredient[])inputs.Clone(); outputItemId = output; outputQuantity = quantity; }
    }
}
