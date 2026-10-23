using System;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInventoryState
    {
        // Called only under the composite session lock after durable acknowledgement.
        // The authority object/subscriptions remain stable even when purchased storage grows.
        internal void InstallExpeditionDraft(TopDown3DInventoryState draft)
        {
            if(draft.Capacity!=Capacity && (!policy.IsCampStorage || !draft.Policy.IsCampStorage || draft.Capacity<Capacity))
                throw new InvalidOperationException("Only purchased camp storage may grow through this path.");
            policy=draft.Policy;slots.Clear();foreach(var slot in draft.Slots)slots.Add(slot.Clone());Revision++;
        }
    }
}
