using System;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInventoryState
    {
        internal TopDown3DPreparedWorldMutation PrepareCanisterContentsAtStartup(bool campStorage = false)
        {
            if (transactionLocked || Revision > 3 && !policy.SupportsCanisterContents) return null;
            var nextPolicy = new TopDown3DInventoryPolicy(policy.Owner, policy.Capacity, policy.MaximumMass, policy.IsLoadout,
                campStorage || policy.IsCampStorage, true, policy.IsExpeditionLayout);
            var prepared = new TopDown3DInventoryState(catalog, nextPolicy);
            if (!prepared.ApplySnapshot(CaptureSnapshot()).Succeeded) return null;
            var expected = Revision;
            var ids = new System.Collections.Generic.List<string>();
            foreach (var slot in prepared.Slots) if (!string.IsNullOrEmpty(slot.InstanceId)) ids.Add(slot.InstanceId);
            return new TopDown3DPreparedWorldMutation(() => TryLock(expected), () => {
                policy = nextPolicy; InstallDraft(prepared);
            }, PublishDraft, Unlock, ids);
        }
        public float CalculateSlotMass(int index) => index >= 0 && index < slots.Count ? SlotMass(slots[index]) : 0;
        private float SlotMass(TopDown3DInventorySlot slot)
        {
            if (slot.IsEmpty) return 0;
            var mass = catalog.GetRequiredDefinition(slot.ItemId).Mass * slot.Quantity;
            if (slot.CanisterContents.IsPresent)
                mass += slot.CanisterContents.dust * catalog.GetRequiredDefinition(TopDown3DHarvesterSettings.DustItemId).Mass;
            return mass;
        }
        private bool ValidContents(string itemId, string instance, int quantity, TopDown3DCanisterContents contents)
        {
            if (!contents.IsPresent)
                return contents.dust == 0 && contents.fractionalSeconds == 0
                    && (!policy.SupportsCanisterContents || itemId != TopDown3DHarvesterSettings.CanisterItemId);
            return policy.SupportsCanisterContents && itemId == TopDown3DHarvesterSettings.CanisterItemId
                && quantity == 1 && Guid.TryParseExact(instance, "N", out _) && contents.IsValid()
                && catalog.TryGetDefinition(TopDown3DHarvesterSettings.DustItemId, out var dust) && dust.IsResource
                && (policy.Owner != TopDown3DInventoryOwner.Booter || contents.dust == 0);
        }
        internal TopDown3DInventoryResult SetCanisterContents(int index, TopDown3DCanisterContents contents)
        {
            if (transactionLocked || index < 0 || index >= slots.Count || slots[index].IsEmpty
                || !ValidContents(slots[index].ItemId, slots[index].InstanceId, slots[index].Quantity, contents))
                return Failure(TopDown3DInventoryResultCode.InvalidRequest, "Canister contents or destination are invalid.");
            var previous = slots[index].CanisterContents;
            slots[index].Set(slots[index].ItemId, slots[index].Quantity, slots[index].InstanceId, contents);
            if (policy.MaximumMass > 0 && CalculateMass() > policy.MaximumMass + .0001f)
            { slots[index].Set(slots[index].ItemId, slots[index].Quantity, slots[index].InstanceId, previous); return Failure(TopDown3DInventoryResultCode.TooHeavy, "Canister contents exceed the cargo limit."); }
            Revision++; PublishDraft(); return Success();
        }
    }
}
