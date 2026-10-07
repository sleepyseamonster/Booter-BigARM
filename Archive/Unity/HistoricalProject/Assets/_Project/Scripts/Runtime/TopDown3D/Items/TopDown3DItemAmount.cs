using System;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public readonly struct TopDown3DItemAmount
    {
        public TopDown3DItemAmount(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        public string ItemId { get; }
        public int Quantity { get; }
    }
}
