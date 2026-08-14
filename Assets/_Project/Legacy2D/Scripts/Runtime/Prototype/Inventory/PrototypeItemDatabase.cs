using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Prototype/Item Database", fileName = "PrototypeItemDatabase")]
    public sealed class PrototypeItemDatabase : ScriptableObject
    {
        [SerializeField] private List<PrototypeItemDef> items = new List<PrototypeItemDef>();

        private Dictionary<string, PrototypeItemDef> itemsById;

        public bool TryGet(string itemId, out PrototypeItemDef itemDef)
        {
            EnsureLookup();
            if (string.IsNullOrWhiteSpace(itemId) || itemsById == null)
            {
                itemDef = null;
                return false;
            }

            return itemsById.TryGetValue(itemId, out itemDef) && itemDef != null;
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        private void EnsureLookup()
        {
            if (itemsById == null)
            {
                RebuildLookup();
            }
        }

        private void RebuildLookup()
        {
            if (itemsById == null)
            {
                itemsById = new Dictionary<string, PrototypeItemDef>();
            }
            else
            {
                itemsById.Clear();
            }

            if (items == null)
            {
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                // Last-write wins so designers can override without editor errors.
                itemsById[item.ItemId] = item;
            }
        }
    }
}
