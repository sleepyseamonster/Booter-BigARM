using System;

namespace BooterBigArm.TopDown3D
{
    public static partial class TopDown3DInventoryTransferService
    {
        public static TopDown3DTransferResult EquipSlot(TopDown3DInventoryState source,
            TopDown3DInventoryState loadout, int sourceIndex, Func<bool> access = null)
        {
            if (source == null || loadout == null || source == loadout || !loadout.Policy.IsLoadout
                || sourceIndex < 0 || sourceIndex >= source.Capacity || source.Slots[sourceIndex].IsEmpty)
                return new TopDown3DTransferResult(TopDown3DInventoryResultCode.InvalidRequest, 0, "Choose equipment in accessible cargo.");
            var selected = source.Slots[sourceIndex];
            var item = source.ItemCatalog.GetRequiredDefinition(selected.ItemId);
            if (!item.IsEquipment) return TransferSlot(source, loadout, sourceIndex, access);
            var targetIndex = (int)item.EquipmentSlot - 1;
            var outgoing = loadout.Slots[targetIndex];
            if (!source.CanRelease(sourceIndex) || !outgoing.IsEmpty && !loadout.CanRelease(targetIndex))
                return new TopDown3DTransferResult(TopDown3DInventoryResultCode.CommitConditionRejected, 0, "Equipment is attached or in use.");
            var a = new TopDown3DInventoryTransaction.Mutation(source);
            var b = new TopDown3DInventoryTransaction.Mutation(loadout);
            var result = a.Draft.RemoveFromSlot(sourceIndex, 1);
            if (!result.Succeeded) return new TopDown3DTransferResult(result.Code, 0, result.Message);
            if (!outgoing.IsEmpty)
            {
                result = b.Draft.RemoveFromSlot(targetIndex, 1);
                if (!result.Succeeded) return new TopDown3DTransferResult(result.Code, 0, result.Message);
                result = a.Draft.TryAdd(new TopDown3DItemAmount(outgoing.ItemId, 1, outgoing.InstanceId));
                if (!result.Succeeded) return new TopDown3DTransferResult(result.Code, 0, result.Message);
            }
            result = b.Draft.TryAdd(new TopDown3DItemAmount(selected.ItemId, 1, selected.InstanceId));
            if (!result.Succeeded) return new TopDown3DTransferResult(result.Code, 0, result.Message);
            result = TopDown3DInventoryTransaction.Commit(new[] { a, b }, access);
            return new TopDown3DTransferResult(result.Code, result.Succeeded ? 1 : 0,
                result.Succeeded ? $"Equipped {item.DisplayName}; previous gear returned to cargo." : result.Message);
        }

        public static TopDown3DTransferResult TransferSlot(TopDown3DInventoryState source,
            TopDown3DInventoryState destination, int sourceIndex, Func<bool> access = null)
        {
            if (source == null || destination == null || source == destination
                || sourceIndex < 0 || sourceIndex >= source.Capacity || source.Slots[sourceIndex].IsEmpty)
                return new TopDown3DTransferResult(TopDown3DInventoryResultCode.InvalidRequest, 0, "Choose a valid stack and destination.");
            if (!source.CanRelease(sourceIndex)) return new TopDown3DTransferResult(TopDown3DInventoryResultCode.CommitConditionRejected, 0, "Equipment is attached or in use.");
            var a = new TopDown3DInventoryTransaction.Mutation(source);
            var b = new TopDown3DInventoryTransaction.Mutation(destination);
            var slot = a.Draft.Slots[sourceIndex];
            var id = slot.ItemId;
            var quantity = b.Draft.GetMaxAddable(id, slot.Quantity);
            if (quantity <= 0)
            {
                var check = b.Draft.CheckAdd(new TopDown3DItemAmount(id, 1));
                return new TopDown3DTransferResult(check.Code, 0, check.Message);
            }
            var result = b.Draft.TryAdd(new TopDown3DItemAmount(id, quantity, slot.InstanceId));
            if (!result.Succeeded) return new TopDown3DTransferResult(result.Code, 0, result.Message);
            result = a.Draft.RemoveFromSlot(sourceIndex, quantity);
            if (!result.Succeeded) return new TopDown3DTransferResult(result.Code, 0, result.Message);
            result = TopDown3DInventoryTransaction.Commit(new[] { a, b }, access);
            return new TopDown3DTransferResult(result.Code, result.Succeeded ? quantity : 0, result.Message);
        }

        public static TopDown3DTransferResult UnloadResources(TopDown3DInventoryState source,
            TopDown3DInventoryState cache, Func<bool> access = null)
        {
            if (source == null || cache == null || source == cache)
                return new TopDown3DTransferResult(TopDown3DInventoryResultCode.InvalidRequest, 0, "A local cache is required.");
            var a = new TopDown3DInventoryTransaction.Mutation(source);
            var b = new TopDown3DInventoryTransaction.Mutation(cache);
            var total = 0;
            for (var i = 0; i < a.Draft.Capacity; i++)
            {
                var slot = a.Draft.Slots[i];
                if (slot.IsEmpty || !source.ItemCatalog.GetRequiredDefinition(slot.ItemId).IsResource) continue;
                var id = slot.ItemId;
                var count = b.Draft.GetMaxAddable(id, slot.Quantity);
                if (count <= 0) continue;
                var result = b.Draft.TryAdd(new TopDown3DItemAmount(id, count));
                if (!result.Succeeded) return new TopDown3DTransferResult(result.Code, 0, result.Message);
                a.Draft.RemoveFromSlot(i, count);
                total += count;
            }
            if (total == 0) return new TopDown3DTransferResult(TopDown3DInventoryResultCode.InsufficientSpace,
                0, "No cargo fits the cache; all remaining goods stay with the Legger.");
            var committed = TopDown3DInventoryTransaction.Commit(new[] { a, b }, access);
            return new TopDown3DTransferResult(committed.Code, committed.Succeeded ? total : 0, committed.Message);
        }
    }
}
