using System;

namespace BooterBigArm.TopDown3D
{
    public static partial class TopDown3DInventoryTransferService
    {
        public static TopDown3DTransferResult TransferExactSlot(TopDown3DInventoryState source, TopDown3DInventoryState destination,
            int index, long expectedSourceRevision, long expectedDestinationRevision, Func<bool> access)
        {
            if (source == null || destination == null || source == destination || access == null || !access()
                || source.IsCommitting || destination.IsCommitting || source.Revision != expectedSourceRevision || destination.Revision != expectedDestinationRevision
                || source.Revision == long.MaxValue || destination.Revision == long.MaxValue || index < 0 || index >= source.Capacity
                || source.Slots[index].IsEmpty || source.ItemCatalog != destination.ItemCatalog || !source.CanRelease(index))
                return new TopDown3DTransferResult(TopDown3DInventoryResultCode.InvalidRequest, 0, "Selected stack, destination or access changed.");
            var a = new TopDown3DInventoryTransaction.Mutation(source);
            var b = new TopDown3DInventoryTransaction.Mutation(destination);
            var slot = a.Draft.Slots[index]; var quantity = slot.Quantity;
            var result = b.Draft.TryAdd(new TopDown3DItemAmount(slot.ItemId, quantity, slot.InstanceId, slot.CanisterContents));
            if (!result.Succeeded) return new TopDown3DTransferResult(result.Code, 0, "Entire stack must fit: " + result.Message);
            result = a.Draft.RemoveFromSlot(index, quantity);
            if (!result.Succeeded) return new TopDown3DTransferResult(result.Code, 0, result.Message);
            result = TopDown3DInventoryTransaction.Commit(new[] { a, b }, access);
            return new TopDown3DTransferResult(result.Code, result.Succeeded ? quantity : 0, result.Message);
        }
    }
}
