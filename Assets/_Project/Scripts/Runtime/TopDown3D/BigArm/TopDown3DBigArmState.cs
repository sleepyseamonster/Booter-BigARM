using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DBigArmState : MonoBehaviour
    {
        [SerializeField] private string companionId = "bigarm.primary";
        [SerializeField] private Vector3 authoritativePosition;
        [SerializeField] private string task = "Follow";
        [SerializeField] private bool detailSimulationLoaded = true;
        public string CompanionId => companionId;
        public Vector3 AuthoritativePosition => authoritativePosition;
        public string Task => task;
        public bool DetailSimulationLoaded => detailSimulationLoaded;
        public event Action Changed;

        public void PublishPosition(Vector3 position)
        { authoritativePosition = position; Changed?.Invoke(); }

        public void SetSimulationLoaded(bool loaded)
        { detailSimulationLoaded = loaded; Changed?.Invoke(); }

        public TopDown3DBigArmCompanionSnapshot CaptureSnapshot(TopDown3DBigArmCargo cargo)
        { return TopDown3DBigArmCompanionSnapshot.Create(companionId, authoritativePosition, task, detailSimulationLoaded, cargo != null ? cargo.CaptureSnapshot() : null); }

        public bool ApplySnapshot(TopDown3DBigArmCompanionSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.CompanionId) || !IsFinite(snapshot.AuthoritativePosition)) return false;
            companionId = snapshot.CompanionId;
            authoritativePosition = snapshot.AuthoritativePosition;
            task = snapshot.Task;
            detailSimulationLoaded = snapshot.DetailSimulationLoaded;
            transform.position = authoritativePosition;
            Changed?.Invoke();
            return true;
        }

        private void Awake() { authoritativePosition = transform.position; }
        private static bool IsFinite(Vector3 value) => !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
    }
}
