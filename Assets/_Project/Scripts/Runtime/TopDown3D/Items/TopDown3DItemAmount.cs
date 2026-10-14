using System;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public readonly struct TopDown3DItemAmount
    {
        public TopDown3DItemAmount(string itemId, int quantity, string instanceId = null)
        {
            ItemId = itemId;
            Quantity = quantity;
            InstanceId = instanceId;
        }

        public string ItemId { get; }
        public int Quantity { get; }
        public string InstanceId { get; }
    }
}
