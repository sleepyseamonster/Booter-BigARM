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
        [SerializeField] private string instanceId;
        [SerializeField] private TopDown3DCanisterContents canisterContents;

        public string ItemId => itemId;
        public int Quantity => quantity;
        public string InstanceId => instanceId;
        public TopDown3DCanisterContents CanisterContents => canisterContents;

        internal static TopDown3DInventorySlotSnapshot Create(string id, int amount, string instance = null, TopDown3DCanisterContents contents = default)
        {
            return new TopDown3DInventorySlotSnapshot
            {
                itemId = amount > 0 ? id : null,
                quantity = amount > 0 ? amount : 0,
                instanceId = amount > 0 ? instance : null,
                canisterContents = amount > 0 ? contents : default
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
                    source[i].Quantity, source[i].InstanceId, source[i].CanisterContents));
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
