using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypeHarvestNodeSaveData
    {
        [SerializeField] private string nodeId = string.Empty;
        [SerializeField] private int remainingUses;

        public string NodeId => nodeId;
        public int RemainingUses => remainingUses;

        public static PrototypeHarvestNodeSaveData Create(string id, int uses)
        {
            return new PrototypeHarvestNodeSaveData
            {
                nodeId = id ?? string.Empty,
                remainingUses = Mathf.Max(0, uses)
            };
        }
    }
}
