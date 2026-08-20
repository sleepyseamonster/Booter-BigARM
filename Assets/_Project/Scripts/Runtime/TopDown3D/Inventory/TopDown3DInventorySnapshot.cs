using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DInventorySlotSnapshot
    {
        [SerializeField] private string itemId;
        [SerializeField] private int quantity;

        public string ItemId => itemId;
        public int Quantity => quantity;

        internal static TopDown3DInventorySlotSnapshot Create(string id, int amount)
        {
            return new TopDown3DInventorySlotSnapshot
            {
                itemId = amount > 0 ? id : null,
                quantity = amount > 0 ? amount : 0
            };
        }
    }

    [Serializable]
    public sealed class TopDown3DInventorySnapshot
    {
        [SerializeField] private int version;
        [SerializeField] private int capacity;
        [SerializeField] private List<TopDown3DInventorySlotSnapshot> slots =
            new List<TopDown3DInventorySlotSnapshot>();

        public int Version => version;
        public int Capacity => capacity;
        public IReadOnlyList<TopDown3DInventorySlotSnapshot> Slots => slots;

        internal static TopDown3DInventorySnapshot Create(
            int snapshotVersion,
            IReadOnlyList<TopDown3DInventorySlot> source)
        {
            var snapshot = new TopDown3DInventorySnapshot
            {
                version = snapshotVersion,
                capacity = source.Count,
                slots = new List<TopDown3DInventorySlotSnapshot>(source.Count)
            };
            for (var i = 0; i < source.Count; i++)
            {
                snapshot.slots.Add(TopDown3DInventorySlotSnapshot.Create(
                    source[i].ItemId,
                    source[i].Quantity));
            }

            return snapshot;
        }

        internal static TopDown3DInventorySnapshot Create(
            int snapshotVersion,
            IReadOnlyList<TopDown3DInventorySlotSnapshot> source)
        {
            return new TopDown3DInventorySnapshot
            {
                version = snapshotVersion,
                capacity = source.Count,
                slots = new List<TopDown3DInventorySlotSnapshot>(source)
            };
        }
    }
}
