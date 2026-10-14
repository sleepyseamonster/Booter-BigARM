using System;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public readonly struct TopDown3DItemAmount
    {
        public TopDown3DItemAmount(string itemId, int quantity, string instanceId = null, TopDown3DCanisterContents contents = default)
        {
            ItemId = itemId;
            Quantity = quantity;
            InstanceId = instanceId;
            CanisterContents = contents;
        }

        public string ItemId { get; }
        public int Quantity { get; }
        public string InstanceId { get; }
        public TopDown3DCanisterContents CanisterContents { get; }
    }
}
