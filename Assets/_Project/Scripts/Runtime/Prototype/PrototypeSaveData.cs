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

        public int Version => version;
        public PrototypeWorldIdentity WorldIdentity => worldIdentity;
        public PrototypePlayerSaveData Player => player;
        public PrototypeSurvivalSaveData Survival => survival;

        public static PrototypeSaveData Create(
            PrototypeWorldIdentity worldIdentity,
            PrototypePlayerSaveData player,
            PrototypeSurvivalSaveData survival)
        {
            return new PrototypeSaveData
            {
                version = PrototypeSaveSchema.CurrentSaveVersion,
                worldIdentity = worldIdentity ?? PrototypeWorldIdentity.Create(0, 0, Vector2Int.zero),
                player = player ?? PrototypePlayerSaveData.FromPosition(Vector3.zero),
                survival = survival ?? PrototypeSurvivalSaveData.FromReserve(100f)
            };
        }
    }
}
