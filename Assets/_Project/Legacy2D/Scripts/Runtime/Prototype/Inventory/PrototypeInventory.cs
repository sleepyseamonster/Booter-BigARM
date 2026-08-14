using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInventory : MonoBehaviour, IPrototypeItemReceiver
    {
        [SerializeField] private PrototypeItemDatabase itemDatabase;
        [SerializeField, Min(1)] private int slotCapacity = 12;
        [SerializeField, Min(0f)] private float maxCarryMass = 0f;
        [SerializeField] private List<PrototypeInventorySlot> slots = new List<PrototypeInventorySlot>();

        public PrototypeItemDatabase ItemDatabase => itemDatabase;
        public int SlotCapacity => Mathf.Max(Mathf.Max(1, slotCapacity), slots != null ? slots.Count : 0);
        public float MaxCarryMass => Mathf.Max(0f, maxCarryMass);

        public int SlotsUsed
        {
            get
            {
                EnsureSlotCapacity();
                var used = 0;
                for (var i = 0; i < slots.Count; i++)
                {
                    if (!slots[i].IsEmpty)
                    {
                        used++;
                    }
                }

                return used;
            }
        }

        public float CurrentCarryMass
        {
            get
            {
                EnsureSlotCapacity();
                var total = 0f;
                for (var i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot.IsEmpty)
                    {
                        continue;
                    }

                    if (TryGetItemDef(slot.ItemId, out var itemDef) && itemDef.CountsAgainstCarry)
                    {
                        total += itemDef.MassPerUnit * slot.Count;
                    }
                }

                return total;
            }
        }

        public float BurdenFraction
        {
            get
            {
                if (maxCarryMass <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01(CurrentCarryMass / Mathf.Max(0.01f, maxCarryMass));
            }
        }

        public bool CanStore(string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return false;
            }

            EnsureSlotCapacity();

            var massPerUnit = GetMassPerUnit(itemId);
            var countsAgainstCarry = CountsAgainstCarry(itemId);

            if (!countsAgainstCarry)
            {
                return true;
            }

            if (maxCarryMass > 0f && massPerUnit > 0f)
            {
                var projectedMass = CurrentCarryMass + (count * massPerUnit);
                if (projectedMass > maxCarryMass)
                {
                    return false;
                }
            }

            return true;
        }

        public event Action InventoryChanged;

        public bool Has(string itemId, int minCount = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId) || minCount <= 0)
            {
                return false;
            }

            EnsureSlotCapacity();
            var total = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.ItemId != itemId)
                {
                    continue;
                }

                total += slot.Count;
                if (total >= minCount)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            EnsureSlotCapacity();
            var total = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty && slot.ItemId == itemId)
                {
                    total += slot.Count;
                }
            }

            return total;
        }

        public bool TryAdd(string itemId, int count, out int accepted)
        {
            return TryAddInternal(itemId, count, out accepted, true);
        }

        public bool TryAddItems(PrototypeItemAmount[] items)
        {
            if (items == null || items.Length == 0)
            {
                return false;
            }

            EnsureSlotCapacity();
            var snapshot = slots.ToArray();
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.Amount <= 0 || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    slots.Clear();
                    slots.AddRange(snapshot);
                    return false;
                }

                if (!TryAddInternal(item.ItemId, item.Amount, out var accepted, false) || accepted != item.Amount)
                {
                    slots.Clear();
                    slots.AddRange(snapshot);
                    return false;
                }
            }

            InventoryChanged?.Invoke();

            return true;
        }

        public bool TryRemove(string itemId, int count, out int removed)
        {
            removed = 0;
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return false;
            }

            EnsureSlotCapacity();

            var remaining = count;
            for (var i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.ItemId != itemId)
                {
                    continue;
                }

                var takeNow = Mathf.Min(slot.Count, remaining);
                var newCount = slot.Count - takeNow;
                slots[i] = newCount > 0 ? slot.WithCount(newCount) : slot.Clear();
                removed += takeNow;
                remaining -= takeNow;
            }

            if (removed > 0)
            {
                InventoryChanged?.Invoke();
            }

            return removed > 0;
        }

        public bool TryTransferItemTo(PrototypeInventory destination, string itemId, int count, out int transferred)
        {
            transferred = 0;
            if (destination == null || ReferenceEquals(destination, this) || string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return false;
            }

            var available = GetItemCount(itemId);
            if (available <= 0)
            {
                return false;
            }

            var desired = Mathf.Min(count, available);
            if (!destination.TryAdd(itemId, desired, out var accepted) || accepted <= 0)
            {
                return false;
            }

            if (!TryRemove(itemId, accepted, out transferred) || transferred <= 0)
            {
                return false;
            }

            return true;
        }

        public bool TryTransferAllTo(PrototypeInventory destination, out int transferred)
        {
            transferred = 0;
            if (destination == null || ReferenceEquals(destination, this))
            {
                return false;
            }

            var summaries = GetItemSummaries();
            var movedAny = false;
            for (var i = 0; i < summaries.Length; i++)
            {
                var summary = summaries[i];
                if (summary.TotalCount <= 0 || string.IsNullOrWhiteSpace(summary.ItemId))
                {
                    continue;
                }

                if (TryTransferItemTo(destination, summary.ItemId, summary.TotalCount, out var moved) && moved > 0)
                {
                    transferred += moved;
                    movedAny = true;
                }
            }

            return movedAny;
        }

        public PrototypeInventorySaveData CaptureSaveData()
        {
            EnsureSlotCapacity();
            return PrototypeInventorySaveData.FromSnapshot(SlotCapacity, slots.ToArray());
        }

        public void ApplySaveData(PrototypeInventorySaveData saveData)
        {
            if (saveData == null || saveData.Slots == null)
            {
                return;
            }

            if (saveData.SlotCapacity > 0)
            {
                slotCapacity = Mathf.Max(1, saveData.SlotCapacity);
            }

            EnsureSlotCapacity();

            // Copy as much as will fit. Any extra is dropped (future: spill to world at load).
            var copyCount = Mathf.Min(slots.Count, saveData.Slots.Length);
            for (var i = 0; i < slots.Count; i++)
            {
                slots[i] = i < copyCount ? saveData.Slots[i] : PrototypeInventorySlot.Empty();
            }

            InventoryChanged?.Invoke();
        }

        public bool TryGetSlot(int index, out PrototypeInventorySlot slot)
        {
            EnsureSlotCapacity();
            if (index < 0 || index >= slots.Count)
            {
                slot = default;
                return false;
            }

            slot = slots[index];
            return true;
        }

        public bool TryGetItemDisplayName(string itemId, out string displayName)
        {
            if (TryGetItemDef(itemId, out var itemDef))
            {
                displayName = itemDef.DisplayName;
                return true;
            }

            displayName = itemId;
            return false;
        }

        public PrototypeInventoryItemSummary[] GetItemSummaries()
        {
            EnsureSlotCapacity();
            if (slots.Count == 0)
            {
                return Array.Empty<PrototypeInventoryItemSummary>();
            }

            var order = new List<string>();
            var summaries = new Dictionary<string, PrototypeInventoryItemSummary>(StringComparer.Ordinal);

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                if (!summaries.TryGetValue(slot.ItemId, out var summary))
                {
                    order.Add(slot.ItemId);
                    summary = new PrototypeInventoryItemSummary
                    {
                        ItemId = slot.ItemId,
                        DisplayName = slot.ItemId,
                        MaxStack = GetStackLimit(slot.ItemId)
                    };

                    if (TryGetItemDisplayName(slot.ItemId, out var displayName))
                    {
                        summary.DisplayName = displayName;
                    }
                }

                summary.TotalCount += slot.Count;
                summary.StackCount++;
                summaries[slot.ItemId] = summary;
            }

            var result = new PrototypeInventoryItemSummary[order.Count];
            for (var i = 0; i < order.Count; i++)
            {
                result[i] = summaries[order[i]];
            }

            return result;
        }

        public void ClearAll()
        {
            EnsureSlotCapacity();
            for (var i = 0; i < slots.Count; i++)
            {
                slots[i] = PrototypeInventorySlot.Empty();
            }

            InventoryChanged?.Invoke();
        }

        private void Awake()
        {
            EnsureSlotCapacity();
        }

        private void OnValidate()
        {
            slotCapacity = Mathf.Max(1, slotCapacity);
            maxCarryMass = Mathf.Max(0f, maxCarryMass);
            EnsureSlotCapacity();
        }

        private void EnsureSlotCapacity()
        {
            slotCapacity = Mathf.Max(1, slotCapacity);
            if (slots == null)
            {
                slots = new List<PrototypeInventorySlot>(slotCapacity);
            }

            while (slots.Count < slotCapacity)
            {
                slots.Add(PrototypeInventorySlot.Empty());
            }
        }

        private bool TryGetItemDef(string itemId, out PrototypeItemDef itemDef)
        {
            if (itemDatabase == null)
            {
                itemDef = null;
                return false;
            }

            return itemDatabase.TryGet(itemId, out itemDef);
        }

        private int GetStackLimit(string itemId)
        {
            if (TryGetItemDef(itemId, out var itemDef))
            {
                return itemDef.MaxStack;
            }

            return 99;
        }

        private float GetMassPerUnit(string itemId)
        {
            if (TryGetItemDef(itemId, out var itemDef))
            {
                return itemDef.MassPerUnit;
            }

            return 0f;
        }

        private bool CountsAgainstCarry(string itemId)
        {
            if (TryGetItemDef(itemId, out var itemDef))
            {
                return itemDef.CountsAgainstCarry;
            }

            return false;
        }

        private bool TryAddInternal(string itemId, int count, out int accepted, bool notify)
        {
            accepted = 0;
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                return false;
            }

            EnsureSlotCapacity();

            var remaining = count;
            var stackLimit = GetStackLimit(itemId);
            var massPerUnit = GetMassPerUnit(itemId);
            var countsAgainstCarry = CountsAgainstCarry(itemId);
            var currentMass = CurrentCarryMass;

            // Fill existing stacks first.
            for (var i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.ItemId != itemId)
                {
                    continue;
                }

                var space = Mathf.Max(0, stackLimit - slot.Count);
                if (space <= 0)
                {
                    continue;
                }

                var addNow = Mathf.Min(space, remaining);
                addNow = ApplyMassLimit(addNow, massPerUnit, countsAgainstCarry, ref currentMass);
                if (addNow <= 0)
                {
                    continue;
                }

                slots[i] = slot.WithCount(slot.Count + addNow);
                accepted += addNow;
                remaining -= addNow;
            }

            // Then allocate new slots, growing the container as needed for resource stacks.
            for (var i = 0; remaining > 0; i++)
            {
                if (i >= slots.Count)
                {
                    slots.Add(PrototypeInventorySlot.Empty());
                }

                var slot = slots[i];
                if (!slot.IsEmpty)
                {
                    continue;
                }

                var addNow = Mathf.Min(stackLimit, remaining);
                addNow = ApplyMassLimit(addNow, massPerUnit, countsAgainstCarry, ref currentMass);
                if (addNow <= 0)
                {
                    continue;
                }

                slots[i] = PrototypeInventorySlot.Of(itemId, addNow);
                accepted += addNow;
                remaining -= addNow;
            }

            if (accepted > 0 && notify)
            {
                InventoryChanged?.Invoke();
            }

            return accepted > 0;
        }

        private int ApplyMassLimit(int desiredAddCount, float massPerUnit, bool countsAgainstCarry, ref float currentMass)
        {
            if (desiredAddCount <= 0)
            {
                return 0;
            }

            if (!countsAgainstCarry || maxCarryMass <= 0f || massPerUnit <= 0f)
            {
                return desiredAddCount;
            }

            var remainingMass = maxCarryMass - currentMass;
            if (remainingMass <= 0f)
            {
                return 0;
            }

            var fit = Mathf.FloorToInt(remainingMass / massPerUnit);
            var allowed = Mathf.Clamp(fit, 0, desiredAddCount);
            currentMass += allowed * massPerUnit;
            return allowed;
        }
    }
}
