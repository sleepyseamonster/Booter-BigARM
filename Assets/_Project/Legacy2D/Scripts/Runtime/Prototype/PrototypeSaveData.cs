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
        [SerializeField] private PrototypeBigArmSaveData bigArm = PrototypeBigArmSaveData.FromPosition(Vector3.zero);
        [SerializeField] private PrototypeSurvivalSaveData survival = PrototypeSurvivalSaveData.FromReserve(100f);
        [SerializeField] private PrototypeInventorySaveData inventory = PrototypeInventorySaveData.FromSnapshot(0, Array.Empty<PrototypeInventorySlot>());
        [SerializeField] private PrototypeInventorySaveData bigArmStorage = PrototypeInventorySaveData.FromSnapshot(0, Array.Empty<PrototypeInventorySlot>());
        [SerializeField] private PrototypeHarvestNodeSaveData[] harvestNodes = Array.Empty<PrototypeHarvestNodeSaveData>();
        [SerializeField] private PrototypeDustCanisterSaveData dustCanister = PrototypeDustCanisterSaveData.Create(false, Vector3.zero, 0f);

        public int Version => version;
        public PrototypeWorldIdentity WorldIdentity => worldIdentity;
        public PrototypePlayerSaveData Player => player;
        public PrototypeBigArmSaveData BigArm => bigArm;
        public PrototypeSurvivalSaveData Survival => survival;
        public PrototypeInventorySaveData Inventory => inventory;
        public PrototypeInventorySaveData BigArmStorage => bigArmStorage;
        public PrototypeHarvestNodeSaveData[] HarvestNodes => harvestNodes;
        public PrototypeDustCanisterSaveData DustCanister => dustCanister;

        public static PrototypeSaveData Create(
            PrototypeWorldIdentity worldIdentity,
            PrototypePlayerSaveData player,
            PrototypeBigArmSaveData bigArm,
            PrototypeSurvivalSaveData survival,
            PrototypeInventorySaveData inventory = null,
            PrototypeInventorySaveData bigArmStorage = null,
            PrototypeHarvestNodeSaveData[] harvestNodes = null,
            PrototypeDustCanisterSaveData dustCanister = null)
        {
            return new PrototypeSaveData
            {
                version = PrototypeSaveSchema.CurrentSaveVersion,
                worldIdentity = worldIdentity ?? PrototypeWorldIdentity.Create(0, 0, Vector2Int.zero),
                player = player ?? PrototypePlayerSaveData.FromPosition(Vector3.zero),
                bigArm = bigArm ?? PrototypeBigArmSaveData.FromPosition(Vector3.zero),
                survival = survival ?? PrototypeSurvivalSaveData.FromReserve(100f),
                inventory = inventory ?? PrototypeInventorySaveData.FromSnapshot(0, Array.Empty<PrototypeInventorySlot>()),
                bigArmStorage = bigArmStorage ?? PrototypeInventorySaveData.FromSnapshot(0, Array.Empty<PrototypeInventorySlot>()),
                harvestNodes = harvestNodes ?? Array.Empty<PrototypeHarvestNodeSaveData>(),
                dustCanister = dustCanister ?? PrototypeDustCanisterSaveData.Create(false, Vector3.zero, 0f)
            };
        }
    }
}
