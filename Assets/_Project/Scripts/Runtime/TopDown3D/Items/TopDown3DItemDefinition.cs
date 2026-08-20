using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DCarryPreference { Either, BooterPreferred, BigArmPreferred, BooterOnly, BigArmOnly }
    public enum TopDown3DPackingPreference { Normal, QuickAccess, Protected, UpperAllowed }

    [CreateAssetMenu(
        menuName = "Booter & BigARM/Top Down 3D/Item Definition",
        fileName = "TopDown3DItemDefinition")]
    public sealed class TopDown3DItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string category;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField, Min(0f)] private float mass;
        [SerializeField] private TopDown3DCarryPreference carryPreference;
        [SerializeField] private TopDown3DPackingPreference packingPreference;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public string Category => category;
        public Sprite Icon => icon;
        public int MaxStack => maxStack;
        public float Mass => mass;
        public TopDown3DCarryPreference CarryPreference => carryPreference;
        public TopDown3DPackingPreference PackingPreference => packingPreference;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                error = "Item definition has an empty stable ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = $"Item '{itemId}' has an empty display name.";
                return false;
            }

            if (icon == null)
            {
                error = $"Item '{itemId}' is missing its required icon.";
                return false;
            }

            if (maxStack < 1)
            {
                error = $"Item '{itemId}' has an invalid max stack of {maxStack}.";
                return false;
            }

            if (mass < 0f || float.IsNaN(mass) || float.IsInfinity(mass))
            {
                error = $"Item '{itemId}' has invalid mass metadata.";
                return false;
            }

            error = null;
            return true;
        }

        internal void Configure(
            string stableItemId,
            string authoredDisplayName,
            string authoredDescription,
            string authoredCategory,
            Sprite authoredIcon,
            int authoredMaxStack,
            float authoredMass,
            TopDown3DCarryPreference authoredCarryPreference = TopDown3DCarryPreference.Either,
            TopDown3DPackingPreference authoredPackingPreference = TopDown3DPackingPreference.Normal)
        {
            itemId = stableItemId;
            displayName = authoredDisplayName;
            description = authoredDescription;
            category = authoredCategory;
            icon = authoredIcon;
            maxStack = authoredMaxStack;
            mass = authoredMass;
            carryPreference = authoredCarryPreference;
            packingPreference = authoredPackingPreference;
        }
    }
}
