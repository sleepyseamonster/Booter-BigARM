using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypePlayerSaveData
    {
        [SerializeField] private float positionX;
        [SerializeField] private float positionY;
        [SerializeField] private float positionZ;

        public Vector3 Position => new Vector3(positionX, positionY, positionZ);

        public static PrototypePlayerSaveData FromPosition(Vector3 position)
        {
            return new PrototypePlayerSaveData
            {
                positionX = position.x,
                positionY = position.y,
                positionZ = position.z
            };
        }
    }
}
