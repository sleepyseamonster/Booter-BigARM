using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D
{
    internal sealed class TopDown3DCampResetPreparation
    {
        internal readonly List<TopDown3DInventoryTransaction.Mutation> Inventories = new();
        internal readonly List<TopDown3DPreparedWorldMutation> World = new();
        internal bool IsValid
        {
            get
            {
                var states = new HashSet<TopDown3DInventoryState>();
                foreach (var mutation in Inventories) if (mutation == null || !states.Add(mutation.State) || mutation.State.Revision == long.MaxValue) return false;
                var world = new HashSet<TopDown3DPreparedWorldMutation>();
                foreach (var mutation in World) if (mutation == null || !world.Add(mutation)) return false;
                return true;
            }
        }
    }
    internal sealed class TopDown3DCampResetParticipant
    {
        internal string Id;
        internal Func<string> Block;
        internal Func<TopDown3DCampResetPreparation> Prepare;
        internal Func<string> Stamp;
    }
}
