using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypeDustCanisterSaveData
    {
        [SerializeField] private bool isDeployed;
        [SerializeField] private Vector3 position;
        [SerializeField] private float storedDust;

        public bool IsDeployed => isDeployed;
        public Vector3 Position => position;
        public float StoredDust => storedDust;

        public static PrototypeDustCanisterSaveData Create(bool deployed, Vector3 canisterPosition, float dust)
        {
            return new PrototypeDustCanisterSaveData
            {
                isDeployed = deployed,
                position = canisterPosition,
                storedDust = Mathf.Max(0f, dust)
            };
        }
    }
}
