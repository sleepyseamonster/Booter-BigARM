using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DSalvageWreckState : MonoBehaviour
    {
        public const string WreckId = "camp-loop:salvage:v1:authored:01";
        public static readonly string[] ItemIds = { "lab.salvage.metal_plate", "lab.salvage.wire_bundle", "lab.salvage.drive_assembly" };
        public static readonly int[] Quantities = { 2, 1, 1 };
        [SerializeField] private string stableId = WreckId;
        [SerializeField] private int version = 1;
        [SerializeField] private int removedMask;
        [SerializeField] private uint epoch;
        public string StableId => stableId;
        public uint Epoch => epoch;
        public int RemovedMask => removedMask;
        public bool IsValid => version == 1 && stableId == WreckId && removedMask >= 0 && removedMask <= 7;
        public bool IsRemoved(int index) => index < 0 || index > 2 || (removedMask & (1 << index)) != 0;
        private bool resetLocked;
        internal TopDown3DPreparedWorldMutation PrepareTrialRestore(int mask)
        {
            if (!IsValid || mask < 0 || mask > 7) return null;
            var before = removedMask; var beforeEpoch = epoch;
            return new TopDown3DPreparedWorldMutation(
                () => { if (resetLocked || removedMask != before || epoch != beforeEpoch) return false; resetLocked = true; return true; },
                () => { removedMask = mask; epoch = unchecked(epoch + 1); },
                () => { foreach (var point in GetComponentsInChildren<TopDown3DSalvagePoint>(true)) point.RefreshPresentation(); }, () => resetLocked = false);
        }
        // No Unity or observer calls inside the inventory commit condition.
        public bool TryConsume(int index)
        {
            if (resetLocked || !IsValid || IsRemoved(index)) return false;
            removedMask |= 1 << index;
            return true;
        }
        public void Invalidate() => epoch = unchecked(epoch + 1);
        public bool ValidateRewards(IReadOnlyList<TopDown3DInventoryState> inventories)
        {
            if (resetLocked || !IsValid || inventories == null || inventories.Count != 3 || inventories.Any(s => s == null || !s.IsInitialized)) return false;
            for (var i = 0; i < 3; i++)
            {
                var total = inventories.Sum(s => s.Slots.Where(slot => slot.ItemId == ItemIds[i]).Sum(slot => slot.Quantity));
                if (total != (IsRemoved(i) ? Quantities[i] : 0)) return false;
            }
            return true;
        }
        public bool TryResetRewards(IReadOnlyList<TopDown3DInventoryState> inventories, out string reason)
        {
            reason = "Salvage reset requires all three lab inventories and valid retained part state.";
            if (resetLocked || !IsValid || inventories == null || inventories.Count != 3 || inventories.Any(s => s == null || !s.IsInitialized)) return false;
            var mutations = new List<TopDown3DInventoryTransaction.Mutation>();
            foreach (var inventory in inventories)
            {
                if (mutations.Any(m => ReferenceEquals(m.State, inventory))) return false;
                var mutation = new TopDown3DInventoryTransaction.Mutation(inventory);
                foreach (var id in ItemIds)
                {
                    var count = mutation.Draft.Slots.Where(s => s.ItemId == id).Sum(s => s.Quantity);
                    if (count > 0 && !mutation.Draft.TryRemove(id, count).Succeeded) return false;
                }
                mutations.Add(mutation);
            }
            var result = TopDown3DInventoryTransaction.Commit(mutations, () => { removedMask = 0; return true; });
            reason = result.Succeeded ? null : result.Message;
            return result.Succeeded;
        }
    }
}
