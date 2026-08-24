using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DGameStateSnapshot
    {
        public const int CurrentVersion = 2;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private int worldSeed;
        [SerializeField] private int worldTopologyVersion;
        [SerializeField] private Vector3 booterPosition;
        [SerializeField] private TopDown3DInventorySnapshot booterInventory;
        [SerializeField] private TopDown3DBigArmCompanionSnapshot bigArm;
        public int Version => version;
        public int WorldSeed => worldSeed;
        public int WorldTopologyVersion => worldTopologyVersion;
        public Vector3 BooterPosition => booterPosition;
        public TopDown3DInventorySnapshot BooterInventory => booterInventory;
        public TopDown3DBigArmCompanionSnapshot BigArm => bigArm;
        internal bool IsCompatible(int expectedWorldSeed, int expectedTopologyVersion)
        {
            return version == CurrentVersion
                && worldSeed == expectedWorldSeed
                && worldTopologyVersion == expectedTopologyVersion;
        }

        internal static TopDown3DGameStateSnapshot Create(
            int seed,
            int topologyVersion,
            Vector3 position,
            TopDown3DInventorySnapshot inventory,
            TopDown3DBigArmCompanionSnapshot companion)
        {
            return new TopDown3DGameStateSnapshot
            {
                worldSeed = seed,
                worldTopologyVersion = topologyVersion,
                booterPosition = position,
                booterInventory = inventory,
                bigArm = companion
            };
        }
    }
}
