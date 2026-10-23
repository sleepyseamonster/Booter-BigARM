using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class CampExpeditionStation {public string id,name,purpose;}
    [Serializable]
    public sealed class CampExpeditionIngredient
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public sealed class CampExpeditionItem
    {
        public string id, name, purpose, category, tier, rarity, artName, sourceStatus;
        public string equipmentAddress;
        public CampExpeditionIngredient[] inputs = Array.Empty<CampExpeditionIngredient>();
        public float treeX, treeY;
        public bool visibleInTree;
        public Sprite art;
        public TopDown3DItemDefinition runtimeDefinition;
        public string DisplayName => runtimeDefinition != null ? runtimeDefinition.DisplayName : name;
    }

    /// <summary>Qualified, revision-bound prototype content. It owns definitions, never inventory copies.</summary>
    [CreateAssetMenu(menuName = "Booter & BigARM/Camp Expedition Definition")]
    public sealed class CampExpeditionDefinition : ScriptableObject
    {
        public const string ScenePath = "Assets/_Project/Scenes/Reference/CampLoopTesting.unity";
        public const string ToolZeroId = "author.tool.jawbone_salvage_digger";
        public const string ToolOneId = "author.tool.iron_headed_digger";
        public const string HarnessId = "tool.utility_harness";
        public const string PhagiaId = "equipment.algae_canister";
        public int version = 1;
        public string catalogRevision, rootId, quantityStatus, sourceFingerprint;
        public float treeWidth, treeHeight;
        public CampExpeditionItem[] items = Array.Empty<CampExpeditionItem>();
        public CampExpeditionStation[] stations=Array.Empty<CampExpeditionStation>();
        public TopDown3DItemCatalog itemCatalog;

        public CampExpeditionItem Find(string id)
        {
            foreach (var item in items) if (item != null && item.id == id) return item;
            return null;
        }

        public bool ValidatePresentation(out string reason)
        {
            reason = "Expedition menu content requires a revision and the three starter definitions.";
            if (version != 1 || string.IsNullOrEmpty(catalogRevision) || rootId != ToolOneId
                || Find(ToolZeroId) == null || Find(HarnessId) == null || Find(PhagiaId) == null) return false;
            var identities = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.id) || string.IsNullOrEmpty(item.name)
                    || !identities.Add(item.id)) return false;
                foreach (var input in item.inputs)
                    if (input == null || string.IsNullOrEmpty(input.itemId) || input.quantity < 1) return false;
            }
            reason = null;
            return true;
        }
    }
}
