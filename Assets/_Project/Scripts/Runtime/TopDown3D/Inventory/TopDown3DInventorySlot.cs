using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DInventorySlot
    {
        [SerializeField] private string itemId;
        [SerializeField] private int quantity;

        public string ItemId => itemId;
        public int Quantity => quantity;
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity == 0;

        internal TopDown3DInventorySlot Clone()
        {
            var clone = new TopDown3DInventorySlot();
            clone.Set(itemId, quantity);
            return clone;
        }

        internal void Set(string nextItemId, int nextQuantity)
        {
            itemId = nextQuantity > 0 ? nextItemId : null;
            quantity = nextQuantity > 0 ? nextQuantity : 0;
        }
    }
}
