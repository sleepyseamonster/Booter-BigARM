using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public struct PrototypeInventorySlot
    {
        [SerializeField] private string itemId;
        [SerializeField] private int count;

        public string ItemId => itemId;
        public int Count => count;
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || count <= 0;

        public static PrototypeInventorySlot Empty()
        {
            return new PrototypeInventorySlot
            {
                itemId = string.Empty,
                count = 0
            };
        }

        public static PrototypeInventorySlot Of(string id, int quantity)
        {
            return new PrototypeInventorySlot
            {
                itemId = id ?? string.Empty,
                count = Mathf.Max(0, quantity)
            };
        }

        public PrototypeInventorySlot WithCount(int newCount)
        {
            return new PrototypeInventorySlot
            {
                itemId = itemId,
                count = Mathf.Max(0, newCount)
            };
        }

        public PrototypeInventorySlot Clear()
        {
            return Empty();
        }
    }
}
