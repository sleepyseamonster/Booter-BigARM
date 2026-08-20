using System;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DItemDestination
    {
        public TopDown3DItemDestination(TopDown3DInventoryState inventory, bool bigArm)
        { Inventory = inventory; IsBigArm = bigArm; }
        public TopDown3DInventoryState Inventory { get; }
        public bool IsBigArm { get; }
    }

    public sealed class TopDown3DItemDestinationService
    {
        private readonly TopDown3DInventoryState booter;
        private readonly TopDown3DInventoryState bigArm;
        private readonly Func<bool> bigArmAccessible;

        public TopDown3DItemDestinationService(TopDown3DInventoryState playerInventory, TopDown3DInventoryState cargo, Func<bool> cargoAccess)
        { booter = playerInventory; bigArm = cargo; bigArmAccessible = cargoAccess ?? (() => false); }

        public TopDown3DItemDestination Choose(string itemId)
        {
            if (bigArm != null && bigArmAccessible() && bigArm.Policy.Allows(bigArm.ItemCatalog.GetRequiredDefinition(itemId)))
                return new TopDown3DItemDestination(bigArm, true);
            return new TopDown3DItemDestination(booter, false);
        }

        public TopDown3DTransferResult AddHarvest(string itemId, int quantity)
        {
            var destination = Choose(itemId);
            var result = destination.Inventory.TryAdd(new TopDown3DItemAmount(itemId, quantity));
            return new TopDown3DTransferResult(result.Code, result.Succeeded ? quantity : 0, result.Message);
        }
    }
}
