using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypeInventorySaveData
    {
        [SerializeField] private int slotCapacity;
        [SerializeField] private PrototypeInventorySlot[] slots;

        public int SlotCapacity => slotCapacity;
        public PrototypeInventorySlot[] Slots => slots;

        public static PrototypeInventorySaveData FromSnapshot(int capacity, PrototypeInventorySlot[] snapshot)
        {
            return new PrototypeInventorySaveData
            {
                slotCapacity = Mathf.Max(0, capacity),
                slots = snapshot
            };
        }
    }
}
