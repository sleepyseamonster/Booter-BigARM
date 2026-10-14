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
        CommitConditionRejected,
        CarrierRestricted,
        TooHeavy,
        DestinationUnavailable,
        MutationBusy
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
    public sealed partial class TopDown3DInventoryState
    {
        public const int CurrentSnapshotVersion = 1;

        private readonly TopDown3DItemCatalog catalog;
        private readonly List<TopDown3DInventorySlot> slots;
        private TopDown3DInventoryPolicy policy;

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
        public Func<string, bool> ReleaseAllowed { get; set; }
        public Func<string, string, bool> EquipmentAdmission { get; set; }
        internal bool AdoptStartupLoadout(TopDown3DInventoryState prepared)
        {
            if (transactionLocked || policy.IsLoadout || !prepared.Policy.IsLoadout || Revision > 1) return false;
            // Preserve this authority object and its subscriptions during the scene initialization barrier.
            policy = prepared.Policy;
            slots.Clear(); foreach (var slot in prepared.Slots) slots.Add(slot.Clone());
            Revision++; PublishDraft(); return true;
        }
        public bool CanRelease(int index) => index >= 0 && index < slots.Count && !slots[index].IsEmpty
            && !(policy.IsLoadout && catalog.GetRequiredDefinition(slots[index].ItemId).EquipmentSlot == TopDown3DEquipmentSlot.AlgaeCanister)
            && (ReleaseAllowed == null || ReleaseAllowed(slots[index].ItemId));
        private bool Accepts(int index, TopDown3DItemDefinition definition, string instance = null)
            => policy.AllowsSlot(index, definition) && (!definition.IsEquipment || EquipmentAdmission == null || EquipmentAdmission(definition.ItemId, instance));

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
            if (transactionLocked) return Failure(TopDown3DInventoryResultCode.MutationBusy, "Inventory is committing another operation.");
            if (!SimulateAdd(amounts, out var nextSlots, out var failure))
            {
                return failure;
            }

            if (!TryLock(Revision)) return Failure(TopDown3DInventoryResultCode.MutationBusy, "Inventory changed during preparation.");
            try
            {
                if (commitCondition != null && !commitCondition())
                    return Failure(TopDown3DInventoryResultCode.CommitConditionRejected, "The inventory transaction's commit condition was rejected.");
                Commit(nextSlots);
                return Success();
            }
            finally { Unlock(); }
        }

        public TopDown3DInventoryResult TryRemove(string itemId, int quantity)
        {
            return TryRemove(itemId, quantity, null);
        }

        internal TopDown3DInventoryResult TryRemove(
            string itemId,
            int quantity,
            Func<bool> commitCondition)
        {
            if (transactionLocked) return Failure(TopDown3DInventoryResultCode.MutationBusy, "Inventory is committing another operation.");
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return Failure(TopDown3DInventoryResultCode.InvalidRequest, "Remove requires an item ID and positive quantity.");
            }

            if (!catalog.TryGetDefinition(itemId, out _))
            {
                return Failure(TopDown3DInventoryResultCode.UnknownItem, $"Unknown item '{itemId}'.");
            }

            if (policy.SupportsCanisterContents && itemId == TopDown3DHarvesterSettings.CanisterItemId)
                return Failure(TopDown3DInventoryResultCode.InvalidRequest, "Select the exact canister instance.");
            for (var i = 0; i < slots.Count; i++)
                if (slots[i].ItemId == itemId && !CanRelease(i))
                    return Failure(TopDown3DInventoryResultCode.CommitConditionRejected, "Equipment is attached or in use.");
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
                slot.Set(itemId, slot.Quantity - removed, slot.InstanceId, slot.CanisterContents);
                remaining -= removed;
            }

            if (!TryLock(Revision)) return Failure(TopDown3DInventoryResultCode.MutationBusy, "Inventory changed during preparation.");
            try
            {
                if (commitCondition != null && !commitCondition())
                    return Failure(TopDown3DInventoryResultCode.CommitConditionRejected, "The inventory transaction's commit condition was rejected.");
                Commit(nextSlots);
                return Success();
            }
            finally { Unlock(); }
        }

        public TopDown3DInventoryResult TryMoveOrSwap(int sourceIndex, int destinationIndex)
        {
            if (transactionLocked) return Failure(TopDown3DInventoryResultCode.MutationBusy, "Inventory is committing another operation.");
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

            if (!CanRelease(sourceIndex) || !destination.IsEmpty && !CanRelease(destinationIndex)
                || !Accepts(destinationIndex, catalog.GetRequiredDefinition(source.ItemId), source.InstanceId)
                || !destination.IsEmpty && !Accepts(sourceIndex, catalog.GetRequiredDefinition(destination.ItemId), destination.InstanceId))
                return Failure(TopDown3DInventoryResultCode.IncompatibleSlots, "Equipment only fits its named slot; attached or in-use gear cannot move.");
            var sourceContents = source.CanisterContents;
            var sourceInstance = source.InstanceId;
            var sourceId = source.ItemId;
            var sourceQuantity = source.Quantity;
            source.Set(destination.ItemId, destination.Quantity, destination.InstanceId, destination.CanisterContents);
            destination.Set(sourceId, sourceQuantity, sourceInstance, sourceContents);
            Revision++;
            PublishDraft();
            return Success();
        }

        public TopDown3DInventoryResult TryMerge(int sourceIndex, int destinationIndex)
        {
            if (transactionLocked) return Failure(TopDown3DInventoryResultCode.MutationBusy, "Inventory is committing another operation.");
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
            if (definition.IsEquipment) return Failure(TopDown3DInventoryResultCode.IncompatibleSlots, "Equipment instances do not merge.");
            if (!CanRelease(sourceIndex) || !Accepts(destinationIndex, definition)) return Failure(TopDown3DInventoryResultCode.IncompatibleSlots, "Item does not fit this slot.");
            var room = definition.MaxStack - destination.Quantity;
            if (room <= 0)
            {
                return Failure(TopDown3DInventoryResultCode.InsufficientSpace, "Destination stack is full.");
            }

            var moved = Math.Min(room, source.Quantity);
            destination.Set(destination.ItemId, destination.Quantity + moved);
            source.Set(source.ItemId, source.Quantity - moved);
            Revision++;
            PublishDraft();
            return Success();
        }

        public TopDown3DInventorySnapshot CaptureSnapshot()
        {
            return TopDown3DInventorySnapshot.Create(policy.SupportsCanisterContents ? 3 : policy.IsLoadout || catalog.HasEquipmentDefinitions ? 2 : CurrentSnapshotVersion, slots);
        }

        public TopDown3DInventoryResult ApplySnapshot(TopDown3DInventorySnapshot snapshot)
        {
            if (transactionLocked) return Failure(TopDown3DInventoryResultCode.MutationBusy, "Inventory is committing another operation.");
            if (snapshot == null
                || (snapshot.Version != CurrentSnapshotVersion && snapshot.Version != 2 && !(snapshot.Version == 3 && policy.SupportsCanisterContents))
                || snapshot.Version == 2 && !policy.IsLoadout && !catalog.HasEquipmentDefinitions
                || snapshot.Capacity != slots.Count
                || snapshot.Slots == null
                || snapshot.Slots.Count != slots.Count)
            {
                return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, "Snapshot version or capacity does not match this inventory.");
            }

            var nextSlots = new List<TopDown3DInventorySlot>(slots.Count);
            var instances = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < snapshot.Slots.Count; i++)
            {
                var record = snapshot.Slots[i];
                if (record == null)
                {
                    return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, $"Snapshot slot {i} is null.");
                }

                var next = new TopDown3DInventorySlot();
                if (record.Quantity == 0 && string.IsNullOrEmpty(record.ItemId) && string.IsNullOrEmpty(record.InstanceId) && !record.CanisterContents.IsPresent && record.CanisterContents.dust == 0 && record.CanisterContents.fractionalSeconds == 0)
                {
                    nextSlots.Add(next);
                    continue;
                }

                if (record.Quantity <= 0
                    || string.IsNullOrWhiteSpace(record.ItemId)
                    || !catalog.TryGetDefinition(record.ItemId, out var definition)
                    || !policy.AllowsSlot(i, definition)
                    || record.Quantity > definition.MaxStack)
                {
                    return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, $"Snapshot slot {i} is invalid.");
                }

                if (definition.IsEquipment ? (snapshot.Version != 2 && snapshot.Version != 3) || record.Quantity != 1 || !Guid.TryParseExact(record.InstanceId, "N", out _) || !instances.Add(record.InstanceId) : !string.IsNullOrEmpty(record.InstanceId))
                    return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, $"Invalid or duplicated equipment identity in slot {i}: {record.ItemId}, instance '{record.InstanceId}', version {snapshot.Version}.");
                var contents = record.CanisterContents;
                if (snapshot.Version < 3 && (contents.IsPresent || contents.dust != 0 || contents.fractionalSeconds != 0))
                    return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, "Legacy snapshots cannot contain canister payloads.");
                if (snapshot.Version < 3 && policy.SupportsCanisterContents && record.ItemId == TopDown3DHarvesterSettings.CanisterItemId)
                    contents = TopDown3DCanisterContents.Create();
                if (!ValidContents(record.ItemId, record.InstanceId, record.Quantity, contents))
                    return Failure(TopDown3DInventoryResultCode.InvalidSnapshot, "Canister contents or carrier are invalid.");
                next.Set(record.ItemId, record.Quantity, record.InstanceId, contents);
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
                    failure = Failure(TopDown3DInventoryResultCode.CarrierRestricted, $"{policy.Owner} cannot carry '{amount.ItemId}'.");
                    return false;
                }

                if (amount.InstanceId != null && (!definition.IsEquipment || amount.Quantity != 1 || !Guid.TryParseExact(amount.InstanceId, "N", out _)
                    || simulated.Exists(s => s.InstanceId == amount.InstanceId)))
                { failure = Failure(TopDown3DInventoryResultCode.InvalidRequest, "Invalid or duplicated equipment instance."); return false; }
                if (!ValidContents(amount.ItemId, amount.InstanceId, amount.Quantity, amount.CanisterContents))
                { failure = Failure(TopDown3DInventoryResultCode.CarrierRestricted, "Filled canisters stay in Legger cargo or camp storage; select the exact canister."); return false; }
                var remaining = amount.Quantity;
                for (var i = 0; i < simulated.Count && remaining > 0; i++)
                {
                    var slot = simulated[i];
                    if (definition.IsEquipment || !Accepts(i, definition, amount.InstanceId) || !string.Equals(slot.ItemId, amount.ItemId, StringComparison.Ordinal)
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
                    if (!slot.IsEmpty || !Accepts(i, definition, amount.InstanceId))
                    {
                        continue;
                    }

                    var added = Math.Min(definition.MaxStack, remaining);
                    slot.Set(amount.ItemId, added, definition.IsEquipment ? amount.InstanceId ?? Guid.NewGuid().ToString("N") : null, amount.CanisterContents);
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
                failure = Failure(TopDown3DInventoryResultCode.TooHeavy, "The load exceeds this inventory's mass limit.");
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
                    total += SlotMass(source[i]);
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
                slots[i].Set(nextSlots[i].ItemId, nextSlots[i].Quantity, nextSlots[i].InstanceId, nextSlots[i].CanisterContents);
            }

            Revision++;
            PublishDraft();
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
