using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Camp Process Recipe")]
    public sealed class TopDown3DProcessRecipe : ScriptableObject
    {
        public const string Ore = "resource.ironstone_ore";
        public static readonly string[] Products = { "lab.ironstone.aggregate", "lab.ironstone.concentrate", "lab.ironstone.sand", "lab.ironstone.mineral_dust" };
        [SerializeField] private string recipeId = "lab.ironstone.dry-sort";
        [SerializeField] private int version = 1;
        [SerializeField] private TopDown3DCraftingRecipe.Ingredient[] inputs = Array.Empty<TopDown3DCraftingRecipe.Ingredient>();
        [SerializeField] private TopDown3DCraftingRecipe.Ingredient[] outputs = Array.Empty<TopDown3DCraftingRecipe.Ingredient>();
        public string RecipeId => recipeId;
        public int Version => version;
        public IReadOnlyList<TopDown3DCraftingRecipe.Ingredient> Inputs => inputs;
        public IReadOnlyList<TopDown3DCraftingRecipe.Ingredient> Outputs => outputs;
        internal void Configure(TopDown3DCraftingRecipe.Ingredient[] source, TopDown3DCraftingRecipe.Ingredient[] results)
        { inputs = (TopDown3DCraftingRecipe.Ingredient[])source.Clone(); outputs = (TopDown3DCraftingRecipe.Ingredient[])results.Clone(); }

        public bool TryFingerprint(TopDown3DItemCatalog catalog, out string fingerprint, out string reason)
        {
            fingerprint = null; reason = "Process recipe or material definitions are invalid.";
            if (catalog == null || string.IsNullOrWhiteSpace(recipeId) || version < 1 || inputs == null || outputs == null
                || inputs.Length < 1 || inputs.Length > 8 || outputs.Length < 1 || outputs.Length > 8) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var text = new StringBuilder(recipeId).Append('|').Append(version);
            double sourceMass = 0, resultMass = 0;
            for (var group = 0; group < 2; group++)
            {
                text.Append(group == 0 ? "|IN" : "|OUT");
                foreach (var amount in group == 0 ? inputs : outputs)
                {
                    if (string.IsNullOrWhiteSpace(amount.itemId) || !ids.Add(amount.itemId) || amount.quantity <= 0 || amount.quantity > 1000000
                        || amount.itemId == "resource.airborne_dust" || !catalog.TryGetDefinition(amount.itemId, out var item)
                        || !item.IsResource || item.IsEquipment || item.IsFieldSupply || item.MaxStack < 1 || item.MaxStack > 1000000
                        || item.CarryPreference != TopDown3DCarryPreference.BigArmOnly || !float.IsFinite(item.Mass) || item.Mass < 0) return false;
                    var mass = (double)item.Mass * amount.quantity;
                    if (group == 0) sourceMass += mass; else resultMass += mass;
                    text.Append('|').Append(amount.itemId.Length).Append(':').Append(amount.itemId).Append(':').Append(amount.quantity)
                        .Append(':').Append(item.Mass.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(item.MaxStack);
                }
            }
            if (sourceMass > float.MaxValue || resultMass > float.MaxValue || Math.Abs(sourceMass - resultMass) > .0001)
            { reason = "Process recipe has unbounded mass or does not conserve material mass."; return false; }
            fingerprint = text.ToString(); reason = null; return true;
        }
    }
}
