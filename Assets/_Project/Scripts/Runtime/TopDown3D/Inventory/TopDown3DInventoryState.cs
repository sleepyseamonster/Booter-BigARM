using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DInventoryResultCode
    {
        Success,
        InvalidRequest,
        UnknownItem,
        InsufficientSpace,
        InsufficientQuantity,
        InvalidSlot,
        IncompatibleSlots,
        InvalidSnapshot,
        CommitConditionRejected
    }

    public readonly struct TopDown3DInventoryResult
    {
        public TopDown3DInventoryResult(TopDown3DInventoryResultCode code, string message = null)
        {
            Code = code;
            Message = message;
        }

        public TopDown3DInventoryResultCode Code { get; }
        public string Message { get; }
        public bool Succeeded => Code == TopDown3DInventoryResultCode.Success;
    }

    [Serializable]
    public sealed class TopDown3DInventoryState
    {
        public const int CurrentSnapshotVersion = 1;

        private readonly TopDown3DItemCatalog catalog;
        private readonly List<TopDown3DInventorySlot> slots;
        private readonly TopDown3DInventoryPolicy policy;

        public TopDown3DInventoryState(TopDown3DItemCatalog itemCatalog, int capacity)
            : this(itemCatalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, capacity))
        {
        }

        public TopDown3DInventoryState(TopDown3DItemCatalog itemCatalog, TopDown3DInventoryPolicy inventoryPolicy)
        {
            if (itemCatalog == null)
            {
                throw new ArgumentNullException(nameof(itemCatalog));
            }

            if (!itemCatalog.TryValidate(out var error))
            {
                throw new ArgumentException(error, nameof(itemCatalog));
            }

            if (inventoryPolicy.Capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(inventoryPolicy));
            }

            catalog = itemCatalog;
            policy = inventoryPolicy;
            slots = new List<TopDown3DInventorySlot>(inventoryPolicy.Capacity);
            for (var i = 0; i < inventoryPolicy.Capacity; i++)
            {
                slots.Add(new TopDown3DInventorySlot());
            }
        }

        public IReadOnlyList<TopDown3DInventorySlot> Slots => slots;
        public TopDown3DItemCatalog ItemCatalog => catalog;
        public TopDown3DInventoryPolicy Policy => policy;
        public int Capacity => slots.Count;
        public int OccupiedSlotCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < slots.Count; i++)
                {
                    if (!slots[i].IsEmpty)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public event Action Changed;

        public bool CanAdd(TopDown3DItemAmount amount)
        {
            return SimulateAdd(new[] { amount }, out _, out _);
        }

        public bool CanAdd(IReadOnlyList<TopDown3DItemAmount> amounts)
        {
            return SimulateAdd(amounts, out _, out _);
        }

        public TopDown3DInventoryResult TryAdd(TopDown3DItemAmount amount)
        {
            return TryAdd(new[] { amount });
        }

        public TopDown3DInventoryResult TryAdd(IReadOnlyList<TopDown3DItemAmount> amounts)
        {
            return TryAdd(amounts, null);
        }

        internal TopDown3DInventoryResult TryAdd(
            IReadOnlyList<TopDown3DItemAmount> amounts,
            Func<bool> commitCondition)
        {
            if (!SimulateAdd(amounts, out var nextSlots, out var failure))
            {
                return failure;
            }

            if (commitCondition != null && !commitCondition())
            {
                return Failure(
                    TopDown3DInventoryResultCode.CommitConditionRejected,
                    "The inventory transaction's commit condition was rejected.");
            }

            Commit(nextSlots);
            return Success();
        }

        public TopDown3DInventoryResult TryRemove(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return Failure(TopDown3DInventoryResultCode.InvalidRequest, "Remove requires an item ID and positive quantity.");
            }

            if (!catalog.TryGetDefinition(itemId, out _))
            {
                return Failure(TopDown3DInventoryResultCode.UnknownItem, $"Unknown item '{itemId}'.");
            }

            var available = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                if (string.Equals(slots[i].ItemId, itemId, StringComparison.Ordinal))
                {
                    available += slots[i].Quantity;
                }
            }

            if (available < quantity)
            {
                return Failure(TopDown3DInventoryResultCode.InsufficientQuantity, "Inventory does not contain the full requested quantity.");
            }

            var nextSlots = CloneSlots();
            var remaining = quantity;
            for (var i = nextSlots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = nextSlots[i];
                if (!string.Equals(slot.ItemId, itemId, StringComparison.Ordinal))
                {
                    continue;
                }

                var removed = Math.Min(slot.Quantity, remaining);
                slot.Set(itemId, slot.Quantity - removed);
                remaining -= removed;
            }

            Commit(nextSlots);
            return Success();
        }

        public TopDown3DInventoryResult TryMoveOrSwap(int sourceIndex, int destinationIndex)
        {
            if (!AreValidDistinctSlots(sourceIndex, destinationIndex))
            {
                return Failure(TopDown3DInventoryResultCode.InvalidSlot, "Move requires two distinct valid slots.");
            }

            var source = slots[sourceIndex];
            var destination = slots[destinationIndex];
            if (source.IsEmpty)
            {
                return Failure(TopDown3DInventoryResultCode.InvalidRequest, "The source slot is empty.");
            }

            if (!destination.IsEmpty
                && string.Equals(source.ItemId, destination.ItemId, StringComparison.Ordinal))
            {
                return TryMerge(sourceIndex, destinationIndex);
            }

            var sourceId = source.ItemId;
            var sourceQuantity = source.Quantity;
            source.Set(destination.ItemId, destination.Quantity);
            destination.Set(sourceId, sourceQuantity);
            Changed?.Invoke();
            return Success();
        }

        public TopDown3DInventoryResult TryMerge(int sourceIndex, int destinationIndex)
        {
            if (!AreValidDistinctSlots(sourceIndex, destinationIndex))
            {
                return Failure(TopDown3DInventoryResultCode.InvalidSlot, "Merge requires two distinct valid slots.");
            }

            var source = slots[sourceIndex];
            var destination = slots[destinationIndex];
            if (source.IsEmpty || destination.IsEmpty
                || !string.Equals(source.ItemId, destination.ItemId, StringComparison.Ordinal))
            {
                return Failure(TopDown3DInventoryResultCode.IncompatibleSlots, "Merge requires matching occupied slots.");
            }

            var definition = catalog.GetRequiredDefinition(source.ItemId);
            var room = definition.MaxStack - destination.Quantity;
            if (room <= 0)
            {
                return Failure(TopDown3DInventoryResultCode.InsufficientSpace, "Destination stack is full.");
            }

            var moved = Math.Min(room, source.Quantity);
            destination.Set(destination.ItemId, destination.Quantity + moved);
            source.Set(source.ItemId, source.Quantity - moved);
            Changed?.Invoke();
            return Success();
        }

        public TopDown3DInventorySnapshot CaptureSnapshot()
        {
            return TopDown3DInventorySnapshot.Create(CurrentSnapshotVersion, slots);
        }

        public TopDown3DInventoryResult ApplySnapshot(TopDown3DInventorySnapshot snapshot)
        {
            if (snapshot == null
                || snapshot.Version != CurrentSnapshotVersion
                || snapshot.Capacity != slots.Count
                || snapshot.Slots == null
                || snapshot.Slots.Count != slots.Count)
            {
                return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, "Snapshot version or capacity does not match this inventory.");
            }

            var nextSlots = new List<TopDown3DInventorySlot>(slots.Count);
            for (var i = 0; i < snapshot.Slots.Count; i++)
            {
                var record = snapshot.Slots[i];
                if (record == null)
                {
                    return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, $"Snapshot slot {i} is null.");
                }

                var next = new TopDown3DInventorySlot();
                if (record.Quantity == 0 && string.IsNullOrEmpty(record.ItemId))
                {
                    nextSlots.Add(next);
                    continue;
                }

                if (record.Quantity <= 0
                    || string.IsNullOrWhiteSpace(record.ItemId)
                    || !catalog.TryGetDefinition(record.ItemId, out var definition)
                    || !policy.Allows(definition)
                    || record.Quantity > definition.MaxStack)
                {
                    return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, $"Snapshot slot {i} is invalid.");
                }

                next.Set(record.ItemId, record.Quantity);
                nextSlots.Add(next);
            }

            if (policy.MaximumMass > 0f && CalculateMass(nextSlots) > policy.MaximumMass + 0.0001f)
            {
                return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, "Snapshot exceeds the inventory mass limit.");
            }

            Commit(nextSlots);
            return Success();
        }

        private bool SimulateAdd(
            IReadOnlyList<TopDown3DItemAmount> amounts,
            out List<TopDown3DInventorySlot> nextSlots,
            out TopDown3DInventoryResult failure)
        {
            nextSlots = null;
            if (amounts == null || amounts.Count == 0)
            {
                failure = Failure(TopDown3DInventoryResultCode.InvalidRequest, "Add requires at least one item amount.");
                return false;
            }

            var simulated = CloneSlots();
            for (var amountIndex = 0; amountIndex < amounts.Count; amountIndex++)
            {
                var amount = amounts[amountIndex];
                if (string.IsNullOrWhiteSpace(amount.ItemId) || amount.Quantity <= 0)
                {
                    failure = Failure(TopDown3DInventoryResultCode.InvalidRequest, "Add requires item IDs and positive quantities.");
                    return false;
                }

                if (!catalog.TryGetDefinition(amount.ItemId, out var definition))
                {
                    failure = Failure(TopDown3DInventoryResultCode.UnknownItem, $"Unknown item '{amount.ItemId}'.");
                    return false;
                }

                if (!policy.Allows(definition))
                {
                    failure = Failure(TopDown3DInventoryResultCode.InvalidRequest, $"{policy.Owner} cannot carry '{amount.ItemId}'.");
                    return false;
                }

                var remaining = amount.Quantity;
                for (var i = 0; i < simulated.Count && remaining > 0; i++)
                {
                    var slot = simulated[i];
                    if (!string.Equals(slot.ItemId, amount.ItemId, StringComparison.Ordinal)
                        || slot.Quantity >= definition.MaxStack)
                    {
                        continue;
                    }

                    var added = Math.Min(definition.MaxStack - slot.Quantity, remaining);
                    slot.Set(amount.ItemId, slot.Quantity + added);
                    remaining -= added;
                }

                for (var i = 0; i < simulated.Count && remaining > 0; i++)
                {
                    var slot = simulated[i];
                    if (!slot.IsEmpty)
                    {
                        continue;
                    }

                    var added = Math.Min(definition.MaxStack, remaining);
                    slot.Set(amount.ItemId, added);
                    remaining -= added;
                }

                if (remaining > 0)
                {
                    failure = Failure(TopDown3DInventoryResultCode.InsufficientSpace, "Inventory cannot accept the full transaction.");
                    return false;
                }
            }

            if (policy.MaximumMass > 0f && CalculateMass(simulated) > policy.MaximumMass + 0.0001f)
            {
                failure = Failure(TopDown3DInventoryResultCode.InsufficientSpace, "The load exceeds this inventory's mass limit.");
                return false;
            }

            nextSlots = simulated;
            failure = Success();
            return true;
        }

        public float CalculateMass()
        {
            return CalculateMass(slots);
        }

        private float CalculateMass(IReadOnlyList<TopDown3DInventorySlot> source)
        {
            var total = 0f;
            for (var i = 0; i < source.Count; i++)
            {
                if (!source[i].IsEmpty && catalog.TryGetDefinition(source[i].ItemId, out var definition))
                    total += definition.Mass * source[i].Quantity;
            }
            return total;
        }

        private List<TopDown3DInventorySlot> CloneSlots()
        {
            var clones = new List<TopDown3DInventorySlot>(slots.Count);
            for (var i = 0; i < slots.Count; i++)
            {
                clones.Add(slots[i].Clone());
            }

            return clones;
        }

        private void Commit(IReadOnlyList<TopDown3DInventorySlot> nextSlots)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                slots[i].Set(nextSlots[i].ItemId, nextSlots[i].Quantity);
            }

            Changed?.Invoke();
        }

        private bool AreValidDistinctSlots(int sourceIndex, int destinationIndex)
        {
            return sourceIndex >= 0
                && sourceIndex < slots.Count
                && destinationIndex >= 0
                && destinationIndex < slots.Count
                && sourceIndex != destinationIndex;
        }

        private static TopDown3DInventoryResult Success()
        {
            return new TopDown3DInventoryResult(TopDown3DInventoryResultCode.Success);
        }

        private static TopDown3DInventoryResult Failure(TopDown3DInventoryResultCode code, string message)
        {
            return new TopDown3DInventoryResult(code, message);
        }
    }
}
