using System;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerActionController
    {
        public event Action<ITopDown3DInteractable> CampStationAccessRequested;
        public event Action CanisterSelectionRequested;
        public Func<bool> AccessibleCargoCanister { get; set; }
        private bool campModalOpen;
        private TopDown3DInventoryState selectedCanisterOwner;
        private int selectedCanisterIndex = -1;
        private long selectedCanisterRevision;
        private string selectedCanisterId;
        private Func<bool> selectedCanisterAccess;
        public void SetCampModalOpen(bool value) => campModalOpen = value;
        private bool TryOpenCampTarget(ITopDown3DInteractable target)
        {
            if (CampStationAccessRequested == null || !(target is TopDown3DCampWorkbench || target is TopDown3DDustRefinery
                || target is TopDown3DResourceCache cache && cache.State.Policy.IsCampStorage)) return false;
            if (!motor.LaboratoryStandingActionsAllowed || IsBusy) return false;
            CampStationAccessRequested.Invoke(target); return campModalOpen;
        }
        private bool SelectedCanisterValid() => selectedCanisterOwner != null && selectedCanisterAccess != null
            && selectedCanisterAccess() && selectedCanisterOwner.Revision == selectedCanisterRevision
            && selectedCanisterIndex >= 0 && selectedCanisterIndex < selectedCanisterOwner.Capacity
            && selectedCanisterOwner.Slots[selectedCanisterIndex].ItemId == TopDown3DHarvesterSettings.CanisterItemId
            && selectedCanisterOwner.Slots[selectedCanisterIndex].InstanceId == selectedCanisterId;
        private void ClearSelectedCanister()
        { selectedCanisterOwner = null; selectedCanisterAccess = null; selectedCanisterIndex = -1; selectedCanisterId = null; }
        private bool SelectStarterForPreview()
        {
            if (SelectedCanisterValid()) return true;
            ClearSelectedCanister();
            for (var i = 0; i < inventory.State.Capacity; i++)
                if (inventory.State.Slots[i].ItemId == TopDown3DHarvesterSettings.CanisterItemId)
                { CaptureCanister(inventory.State, i, () => true); return true; }
            CanisterSelectionRequested?.Invoke(); return false;
        }
        private void CaptureCanister(TopDown3DInventoryState source, int index, Func<bool> access)
        {
            selectedCanisterOwner = source; selectedCanisterIndex = index; selectedCanisterAccess = access;
            selectedCanisterId = source.Slots[index].InstanceId; selectedCanisterRevision = source.Revision;
        }
        public bool SelectCarriedCanister(TopDown3DInventoryState source, int index, Func<bool> access)
        {
            if (source == null || access == null || !access() || index < 0 || index >= source.Capacity
                || source.Slots[index].ItemId != TopDown3DHarvesterSettings.CanisterItemId || IsBusy
                || input.Mode != TopDown3DInputMode.Gameplay || harvesterState == null || !harvesterState.RetainsCanisterContents) return false;
            CaptureCanister(source, index, access); HandleDeployCanisterRequested(); return placementPreview != null;
        }
        private bool CommitSelectedCanister(UnityEngine.Vector3 position)
        {
            if (!SelectedCanisterValid()) { ReportFeedback("Canister selection changed or cargo moved out of reach."); return false; }
            return harvesterState.TryPlaceCanister(position, selectedCanisterOwner, selectedCanisterIndex, selectedCanisterAccess, out _);
        }
    }
}
