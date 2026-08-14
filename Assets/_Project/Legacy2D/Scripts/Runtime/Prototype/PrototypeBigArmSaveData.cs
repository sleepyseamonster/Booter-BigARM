using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypeBigArmSaveData
    {
        [SerializeField] private Vector3 position = Vector3.zero;

        public Vector3 Position => position;

        public static PrototypeBigArmSaveData FromPosition(Vector3 value)
        {
            return new PrototypeBigArmSaveData
            {
                position = value
            };
        }
    }
}
