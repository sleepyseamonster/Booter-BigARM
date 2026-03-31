using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypeSaveData
    {
        [SerializeField] private int version = PrototypeSaveSchema.CurrentSaveVersion;
        [SerializeField] private PrototypeWorldIdentity worldIdentity = PrototypeWorldIdentity.Create(0, 0, Vector2Int.zero);
        [SerializeField] private PrototypePlayerSaveData player = PrototypePlayerSaveData.FromPosition(Vector3.zero);
        [SerializeField] private PrototypeSurvivalSaveData survival = PrototypeSurvivalSaveData.FromReserve(100f);
        [SerializeField] private PrototypeInventorySaveData inventory = PrototypeInventorySaveData.FromSnapshot(0, Array.Empty<PrototypeInventorySlot>());
        [SerializeField] private PrototypeHarvestNodeSaveData[] harvestNodes = Array.Empty<PrototypeHarvestNodeSaveData>();

        public int Version => version;
        public PrototypeWorldIdentity WorldIdentity => worldIdentity;
        public PrototypePlayerSaveData Player => player;
        public PrototypeSurvivalSaveData Survival => survival;
        public PrototypeInventorySaveData Inventory => inventory;
        public PrototypeHarvestNodeSaveData[] HarvestNodes => harvestNodes;

        public static PrototypeSaveData Create(
            PrototypeWorldIdentity worldIdentity,
            PrototypePlayerSaveData player,
            PrototypeSurvivalSaveData survival,
            PrototypeInventorySaveData inventory = null,
            PrototypeHarvestNodeSaveData[] harvestNodes = null)
        {
            return new PrototypeSaveData
            {
                version = PrototypeSaveSchema.CurrentSaveVersion,
                worldIdentity = worldIdentity ?? PrototypeWorldIdentity.Create(0, 0, Vector2Int.zero),
                player = player ?? PrototypePlayerSaveData.FromPosition(Vector3.zero),
                survival = survival ?? PrototypeSurvivalSaveData.FromReserve(100f),
                inventory = inventory ?? PrototypeInventorySaveData.FromSnapshot(0, Array.Empty<PrototypeInventorySlot>()),
                harvestNodes = harvestNodes ?? Array.Empty<PrototypeHarvestNodeSaveData>()
            };
        }
    }
}
