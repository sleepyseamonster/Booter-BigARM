using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Prepared scalar/world state; installation cannot call observers or Unity presentation.</summary>
    internal sealed class TopDown3DPreparedWorldMutation
    {
        internal readonly Func<bool> Lock;
        internal readonly Action Install, Publish, Unlock;
        internal readonly IReadOnlyList<string> EquipmentIds;
        internal TopDown3DPreparedWorldMutation(Func<bool> acquire, Action install, Action publish, Action release,
            IReadOnlyList<string> equipmentIds = null)
        { Lock = acquire; Install = install; Publish = publish; Unlock = release; EquipmentIds = equipmentIds ?? Array.Empty<string>(); }
    }
}
