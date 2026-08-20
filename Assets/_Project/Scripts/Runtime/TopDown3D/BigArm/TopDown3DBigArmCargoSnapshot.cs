using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DBigArmCargoSnapshot
    {
        [SerializeField] private int version = 1;
        [SerializeField] private int loadframeVersion = 1;
        [SerializeField] private TopDown3DInventorySnapshot inventory;
        public int Version => version;
        public int LoadframeVersion => loadframeVersion;
        public TopDown3DInventorySnapshot Inventory => inventory;
        internal static TopDown3DBigArmCargoSnapshot Create(TopDown3DInventorySnapshot state)
        { return new TopDown3DBigArmCargoSnapshot { inventory = state }; }
    }
}
