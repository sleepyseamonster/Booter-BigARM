using System;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DItemDestination
    {
        public TopDown3DItemDestination(TopDown3DInventoryState inventory, bool bigArm,
            TopDown3DInventoryResultCode code = TopDown3DInventoryResultCode.Success, string message = null)
        { Inventory = inventory; IsBigArm = bigArm; Code = code; Message = message; }
        public TopDown3DInventoryState Inventory { get; }
        public bool IsBigArm { get; }
        public TopDown3DInventoryResultCode Code { get; }
        public string Message { get; }
    }

    public sealed class TopDown3DItemDestinationService
    {
        private readonly TopDown3DInventoryState booter;
        private readonly TopDown3DInventoryState bigArm;
        private readonly Func<bool> bigArmAccessible;

        public TopDown3DItemDestinationService(TopDown3DInventoryState playerInventory, TopDown3DInventoryState cargo, Func<bool> cargoAccess)
        { booter = playerInventory; bigArm = cargo; bigArmAccessible = cargoAccess ?? (() => false); }

        public TopDown3DItemDestination Choose(string itemId, int quantity = 1)
        {
            if (booter == null || !booter.ItemCatalog.TryGetDefinition(itemId, out var item))
                return new TopDown3DItemDestination(null, false, TopDown3DInventoryResultCode.UnknownItem, "Unknown item.");
            if (item.IsResource || item.CarryPreference == TopDown3DCarryPreference.BigArmOnly)
            {
                if (bigArm == null || !bigArmAccessible())
                    return new TopDown3DItemDestination(null, true, TopDown3DInventoryResultCode.DestinationUnavailable, "Legger cargo is out of reach.");
                var check = bigArm.CheckAdd(new TopDown3DItemAmount(itemId, quantity));
                return new TopDown3DItemDestination(check.Succeeded ? bigArm : null, true, check.Code,
                    check.Succeeded ? null : check.Code == TopDown3DInventoryResultCode.TooHeavy
                        ? "Legger cannot carry that much weight." : "Legger cargo has no room for this reward.");
            }
            if (item.CarryPreference == TopDown3DCarryPreference.BooterOnly
                || item.CarryPreference == TopDown3DCarryPreference.BooterPreferred)
                return new TopDown3DItemDestination(booter, false);
            if (bigArm != null && bigArmAccessible() && bigArm.Policy.Allows(bigArm.ItemCatalog.GetRequiredDefinition(itemId))
                && bigArm.CanAdd(new TopDown3DItemAmount(itemId, quantity)))
                return new TopDown3DItemDestination(bigArm, true);
            return new TopDown3DItemDestination(booter, false);
        }

        public TopDown3DTransferResult AddHarvest(string itemId, int quantity)
        {
            var destination = Choose(itemId, quantity);
            if (destination.Inventory == null)
                return new TopDown3DTransferResult(destination.Code, 0, destination.Message);
            var result = destination.Inventory.TryAdd(new TopDown3DItemAmount(itemId, quantity));
            return new TopDown3DTransferResult(result.Code, result.Succeeded ? quantity : 0, result.Message);
        }
    }
}
