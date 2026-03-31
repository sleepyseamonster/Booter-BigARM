using UnityEngine;

namespace BooterBigArm.Runtime
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Prototype/Item Def", fileName = "ItemDef_")]
    public sealed class PrototypeItemDef : ScriptableObject
    {
        [SerializeField] private string itemId = "item.unknown";
        [SerializeField] private string displayName = "Unknown Item";
        [SerializeField] private PrototypeItemCategory category = PrototypeItemCategory.Unknown;
        [SerializeField, Min(1)] private int maxStack = 99;
        [SerializeField, Min(0f)] private float massPerUnit = 0.1f;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public PrototypeItemCategory Category => category;
        public int MaxStack => Mathf.Max(1, maxStack);
        public float MassPerUnit => Mathf.Max(0f, massPerUnit);

        private void OnValidate()
        {
            maxStack = Mathf.Max(1, maxStack);
            massPerUnit = Mathf.Max(0f, massPerUnit);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = "item.unknown";
            }
        }
    }
}
