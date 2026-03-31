using System;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public struct PrototypeInventoryItemSummary
    {
        public string ItemId;
        public string DisplayName;
        public int TotalCount;
        public int StackCount;
        public int MaxStack;
    }
}
