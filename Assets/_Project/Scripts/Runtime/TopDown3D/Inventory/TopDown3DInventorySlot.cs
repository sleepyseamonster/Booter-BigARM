using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DInventorySlot
    {
        [SerializeField] private string itemId;
        [SerializeField] private int quantity;
        [SerializeField] private string instanceId;

        public string ItemId => itemId;
        public int Quantity => quantity;
        public string InstanceId => instanceId;
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity == 0;

        internal TopDown3DInventorySlot Clone()
        {
            var clone = new TopDown3DInventorySlot();
            clone.Set(itemId, quantity, instanceId);
            return clone;
        }

        internal void Set(string nextItemId, int nextQuantity, string nextInstanceId = null)
        {
            itemId = nextQuantity > 0 ? nextItemId : null;
            quantity = nextQuantity > 0 ? nextQuantity : 0;
            instanceId = nextQuantity > 0 ? nextInstanceId : null;
        }
    }
}
