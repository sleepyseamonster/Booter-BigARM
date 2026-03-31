namespace BooterBigArm.Runtime
{
    /// <summary>
    /// Inventory-facing seam for harvest nodes and pickups.
    /// Implementations should treat TryAddItems as atomic (all-or-nothing) so harvest nodes can avoid consuming
    /// themselves when the inventory is full.
    /// </summary>
    public interface IPrototypeItemReceiver
    {
        bool TryAddItems(PrototypeItemAmount[] items);
    }
}
