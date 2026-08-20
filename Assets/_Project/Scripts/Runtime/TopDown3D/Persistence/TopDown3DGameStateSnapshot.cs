using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DGameStateSnapshot
    {
        [SerializeField] private int version = 1;
        [SerializeField] private int worldSeed;
        [SerializeField] private Vector3 booterPosition;
        [SerializeField] private TopDown3DInventorySnapshot booterInventory;
        [SerializeField] private TopDown3DBigArmCompanionSnapshot bigArm;
        public int Version => version;
        public int WorldSeed => worldSeed;
        public Vector3 BooterPosition => booterPosition;
        public TopDown3DInventorySnapshot BooterInventory => booterInventory;
        public TopDown3DBigArmCompanionSnapshot BigArm => bigArm;
        internal static TopDown3DGameStateSnapshot Create(int seed, Vector3 position, TopDown3DInventorySnapshot inventory, TopDown3DBigArmCompanionSnapshot companion)
        { return new TopDown3DGameStateSnapshot { worldSeed = seed, booterPosition = position, booterInventory = inventory, bigArm = companion }; }
    }
}
