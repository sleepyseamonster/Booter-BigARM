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

        public int SlotCapacity => Mathf.Max(1, slotCapacity);
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

                    if (TryGetItemDef(slot.ItemId, out var itemDef))
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

            var stackLimit = GetStackLimit(itemId);
            var massPerUnit = GetMassPerUnit(itemId);
            var freeSpaceInExistingStacks = 0;
            var emptySlots = 0;

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                {
                    emptySlots++;
                    continue;
                }

                if (slot.ItemId == itemId)
                {
                    freeSpaceInExistingStacks += Mathf.Max(0, stackLimit - slot.Count);
                }
            }

            var remaining = Mathf.Max(0, count - freeSpaceInExistingStacks);
            var slotsNeeded = remaining <= 0 ? 0 : Mathf.CeilToInt(remaining / (float)stackLimit);
            if (slotsNeeded > emptySlots)
            {
                return false;
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

        public PrototypeInventorySaveData CaptureSaveData()
        {
            EnsureSlotCapacity();
            return PrototypeInventorySaveData.FromSnapshot(slotCapacity, slots.ToArray());
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

            while (slots.Count > slotCapacity)
            {
                slots.RemoveAt(slots.Count - 1);
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
                addNow = ApplyMassLimit(addNow, massPerUnit, ref currentMass);
                if (addNow <= 0)
                {
                    continue;
                }

                slots[i] = slot.WithCount(slot.Count + addNow);
                accepted += addNow;
                remaining -= addNow;
            }

            // Then allocate new slots.
            for (var i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty)
                {
                    continue;
                }

                var addNow = Mathf.Min(stackLimit, remaining);
                addNow = ApplyMassLimit(addNow, massPerUnit, ref currentMass);
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

        private int ApplyMassLimit(int desiredAddCount, float massPerUnit, ref float currentMass)
        {
            if (desiredAddCount <= 0)
            {
                return 0;
            }

            if (maxCarryMass <= 0f || massPerUnit <= 0f)
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
