using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Read-only demand for one tool, accounting for already manufactured parts.</summary>
    public sealed class CampFirstToolProgress
    {
        public bool Valid { get; private set; } = true;
        public bool Complete { get; private set; }
        public bool[] Needed { get; }
        public Dictionary<string, int> MissingRaw { get; } = new(StringComparer.Ordinal);
        private readonly IReadOnlyList<TopDown3DCraftingRecipe> recipes;
        public CampFirstToolProgress(IReadOnlyList<TopDown3DCraftingRecipe> roster, params TopDown3DInventoryState[] stores)
        {
            recipes = roster; Needed = new bool[roster.Count];
            var owned = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var store in stores) if (store != null) foreach (var slot in store.Slots)
                if (!slot.IsEmpty) owned[slot.ItemId] = owned.GetValueOrDefault(slot.ItemId) + slot.Quantity;
            Complete = owned.GetValueOrDefault(CampFirstToolScenario.ToolId) > 0;
            var producers = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < roster.Count; i++)
                if (roster[i] == null || !producers.TryAdd(roster[i].Output.ItemId, i)) { Valid = false; return; }
            Need(CampFirstToolScenario.ToolId, 1, new HashSet<string>(StringComparer.Ordinal));
            void Need(string item, int quantity, HashSet<string> path)
            {
                var use = Math.Min(quantity, owned.GetValueOrDefault(item));
                owned[item] = owned.GetValueOrDefault(item) - use; quantity -= use;
                if (quantity <= 0 || !Valid) return;
                if (!producers.TryGetValue(item, out var index))
                { MissingRaw[item] = MissingRaw.GetValueOrDefault(item) + quantity; return; }
                if (!path.Add(item) || path.Count > 32) { Valid = false; return; }
                Needed[index] = true; var recipe = roster[index];
                if (recipe.Output.Quantity <= 0) { Valid = false; return; }
                var batches = (quantity + recipe.Output.Quantity - 1) / recipe.Output.Quantity;
                foreach (var input in recipe.Ingredients) Need(input.itemId, input.quantity * batches, path);
                owned[item] += batches * recipe.Output.Quantity - quantity; path.Remove(item);
            }
        }
        public static int Count(TopDown3DInventoryState store, string item)
        { var count = 0; if (store != null) foreach (var slot in store.Slots) if (slot.ItemId == item) count += slot.Quantity; return count; }
        public bool CanCraft(int index, TopDown3DInventoryState chest, TopDown3DInventoryState player)
        {
            if (!Valid || Complete || index < 0 || index >= Needed.Length || !Needed[index] || chest == null || player == null) return false;
            var recipe = recipes[index]; var draft = chest.CreateDraft();
            foreach (var input in recipe.Ingredients) if (!draft.TryRemove(input.itemId, input.quantity).Succeeded) return false;
            var gear = player.ItemCatalog.GetRequiredDefinition(recipe.Output.ItemId).IsEquipment;
            return (gear ? player : draft).CheckAdd(recipe.Output).Succeeded;
        }
        public int Next(TopDown3DInventoryState chest, TopDown3DInventoryState player)
        {
            for (int i = 0; i < Needed.Length; i++) if (CanCraft(i, chest, player)) return i;
            return Array.FindIndex(Needed, n => n);
        }
        public string Status(int index, TopDown3DInventoryState chest, TopDown3DInventoryState player)
        {
            if (!Valid) return "UNAVAILABLE";
            if (!Needed[index]) return "DONE";
            foreach (var input in recipes[index].Ingredients)
                if (Count(chest, input.itemId) < input.quantity) return "NEED CHEST INPUTS";
            return CanCraft(index, chest, player) ? "READY" : "OUTPUT BLOCKED";
        }
        public static CampFirstToolProgress For(CampFirstToolScenario scenario, TopDown3DCampLoopEconomySession session) =>
            new(scenario.Recipes, session.Kit.State, session.Cargo.State, session.Cache.State);
        public static string ShortName(string id) => id switch
        {
            "proposal.bio.jaw" => "Jaw plates", "author.bio.stone_bone" => "Stone bone", "proposal.bio.shell" => "Shell",
            "resource.ironstone_ore" => "Ironstone", "proposal.bio.insert" => "Jaw insert", "component.mining.bone_shaft" => "Bone shaft",
            "component.mining.shell_head_collar" => "Shell collar", "component.mining.shell_butt_cap" => "Shell butt cap",
            "proposal.t1.iron_bloom" => "Iron bloom", "proposal.natural.iron_stock" => "Iron stock",
            "component.mining.iron_retaining_pin" => "Iron pin", CampFirstToolScenario.ToolId => "Assemble multi-tool", _ => id
        };
    }
}
