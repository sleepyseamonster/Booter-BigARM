using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DBigArmCompanionSnapshot
    {
        [SerializeField] private int version = 1;
        [SerializeField] private string companionId;
        [SerializeField] private Vector3 authoritativePosition;
        [SerializeField] private string task;
        [SerializeField] private bool detailSimulationLoaded;
        [SerializeField] private TopDown3DBigArmCargoSnapshot cargo;
        public int Version => version;
        public string CompanionId => companionId;
        public Vector3 AuthoritativePosition => authoritativePosition;
        public string Task => task;
        public bool DetailSimulationLoaded => detailSimulationLoaded;
        public TopDown3DBigArmCargoSnapshot Cargo => cargo;
        internal static TopDown3DBigArmCompanionSnapshot Create(string id, Vector3 position, string currentTask, bool loaded, TopDown3DBigArmCargoSnapshot cargoSnapshot)
        { return new TopDown3DBigArmCompanionSnapshot { companionId = id, authoritativePosition = position, task = currentTask, detailSimulationLoaded = loaded, cargo = cargoSnapshot }; }
    }
}
