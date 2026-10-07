using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypeSurvivalSaveData
    {
        [SerializeField] private float algaeReserve = 100f;

        public float AlgaeReserve => algaeReserve;

        public static PrototypeSurvivalSaveData FromReserve(float reserve)
        {
            return new PrototypeSurvivalSaveData
            {
                algaeReserve = reserve
            };
        }
    }
}
