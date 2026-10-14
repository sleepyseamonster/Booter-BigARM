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
        [SerializeField] private TopDown3DCanisterContents canisterContents;

        public string ItemId => itemId;
        public int Quantity => quantity;
        public string InstanceId => instanceId;
        public TopDown3DCanisterContents CanisterContents => canisterContents;
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity == 0;

        internal TopDown3DInventorySlot Clone()
        {
            var clone = new TopDown3DInventorySlot();
            clone.Set(itemId, quantity, instanceId, canisterContents);
            return clone;
        }

        internal void Set(string nextItemId, int nextQuantity, string nextInstanceId = null, TopDown3DCanisterContents contents = default)
        {
            itemId = nextQuantity > 0 ? nextItemId : null;
            quantity = nextQuantity > 0 ? nextQuantity : 0;
            instanceId = nextQuantity > 0 ? nextInstanceId : null;
            canisterContents = nextQuantity > 0 ? contents : default;
        }
    }
}
