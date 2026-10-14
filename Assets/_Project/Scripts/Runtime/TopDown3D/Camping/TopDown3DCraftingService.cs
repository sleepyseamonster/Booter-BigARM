using System;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DCraftingService
    {
        public static TopDown3DInventoryResult Craft(TopDown3DCraftingRecipe recipe,
            TopDown3DInventoryState source, TopDown3DInventoryState destination, Func<bool> access)
        {
            if (recipe == null || source == null || destination == null || source == destination || access == null)
                return Failure("A local workbench, chest and output destination are required.");
            if (!recipe.TryValidate(source.ItemCatalog, out var reason)) return Failure(reason);
            if (!access()) return Failure("Workbench or chest is out of reach.");
            var input = new TopDown3DInventoryTransaction.Mutation(source);
            var output = new TopDown3DInventoryTransaction.Mutation(destination);
            foreach (var ingredient in recipe.Ingredients)
            {
                var result = input.Draft.TryRemove(ingredient.itemId, ingredient.quantity);
                if (!result.Succeeded) return result;
            }
            var added = output.Draft.TryAdd(recipe.Output);
            if (!added.Succeeded) return added;
            return TopDown3DInventoryTransaction.Commit(new[] { input, output }, access);
        }

        internal static TopDown3DInventoryResult Failure(string message) =>
            new(TopDown3DInventoryResultCode.CommitConditionRejected, message);
    }
}
