using System;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DTransferMode { Exact, Maximum }

    public readonly struct TopDown3DTransferResult
    {
        public TopDown3DTransferResult(TopDown3DInventoryResultCode code, int quantity, string message = null)
        { Code = code; Quantity = quantity; Message = message; }
        public TopDown3DInventoryResultCode Code { get; }
        public int Quantity { get; }
        public string Message { get; }
        public bool Succeeded => Code == TopDown3DInventoryResultCode.Success && Quantity > 0;
    }

    public static class TopDown3DInventoryTransferService
    {
        public static TopDown3DTransferResult Transfer(
            TopDown3DInventoryState source,
            TopDown3DInventoryState destination,
            string itemId,
            int requestedQuantity,
            TopDown3DTransferMode mode = TopDown3DTransferMode.Maximum)
        {
            if (source == null || destination == null || source == destination || string.IsNullOrWhiteSpace(itemId) || requestedQuantity <= 0)
                return new TopDown3DTransferResult(TopDown3DInventoryResultCode.InvalidRequest, 0, "Transfer requires two inventories, an item, and a positive quantity.");
            if (!source.ItemCatalog.TryGetDefinition(itemId, out _))
                return new TopDown3DTransferResult(TopDown3DInventoryResultCode.UnknownItem, 0, $"Unknown item '{itemId}'.");

            var quantity = mode == TopDown3DTransferMode.Exact
                ? requestedQuantity
                : FindMaximum(source, destination, itemId, requestedQuantity);
            if (quantity <= 0)
                return new TopDown3DTransferResult(TopDown3DInventoryResultCode.InsufficientSpace, 0, "No portion of this stack fits the destination.");

            var remove = source.TryRemove(itemId, quantity);
            if (!remove.Succeeded)
                return new TopDown3DTransferResult(remove.Code, 0, remove.Message);
            var add = destination.TryAdd(new TopDown3DItemAmount(itemId, quantity));
            if (!add.Succeeded)
            {
                source.TryAdd(new TopDown3DItemAmount(itemId, quantity));
                return new TopDown3DTransferResult(add.Code, 0, add.Message);
            }
            return new TopDown3DTransferResult(TopDown3DInventoryResultCode.Success, quantity);
        }

        private static int FindMaximum(TopDown3DInventoryState source, TopDown3DInventoryState destination, string itemId, int requested)
        {
            var available = 0;
            for (var i = 0; i < source.Slots.Count; i++)
                if (string.Equals(source.Slots[i].ItemId, itemId, StringComparison.Ordinal)) available += source.Slots[i].Quantity;
            var low = 0; var high = Math.Min(requested, available);
            while (low < high)
            {
                var candidate = low + (high - low + 1) / 2;
                if (destination.CanAdd(new TopDown3DItemAmount(itemId, candidate))) low = candidate; else high = candidate - 1;
            }
            return low;
        }
    }
}
