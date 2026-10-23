using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInventoryUiController
    {
        private TopDown3DResourceCache cacheContext;
        private InputAction nextInventory, previousInventory, quickTransfer;
        private readonly System.Collections.Generic.HashSet<InputControl> blockedInventoryControls = new();
        private static readonly string[] InventoryCommandPaths =
            { "UI/Submit", "UI/Click", "UI/QuickTransfer", "UI/NextInventory", "UI/PreviousInventory" };
        private static readonly string[] InventoryActivationPaths = { "UI/Submit", "UI/Click" };

        private void ArmInventoryControls()
        {
            blockedInventoryControls.Clear();
            foreach (var name in InventoryCommandPaths)
            {
                var action = input?.InputActions?.FindAction(name, false);
                if (action == null) continue;
                foreach (var control in action.controls)
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed)
                        blockedInventoryControls.Add(control);
            }
        }
        private void ReleaseInventoryControls()
        {
            if (blockedInventoryControls.Count == 0) return;
            blockedInventoryControls.RemoveWhere(control => !control.device.added
                || control is not UnityEngine.InputSystem.Controls.ButtonControl button || !button.isPressed);
        }
        private bool InventoryActivationBlocked
        {
            get
            {
                foreach (var name in InventoryActivationPaths)
                {
                    var action = input?.InputActions?.FindAction(name, false);
                    if (action == null) continue;
                    foreach (var control in action.controls)
                        if (blockedInventoryControls.Contains(control)) return true;
                }
                return false;
            }
        }
        private bool mutating;
        private bool rememberedCargoFocus;
        private long pendingRevision;
        private string pendingItem;
        private TopDown3DInventoryState LeftState => cacheContext != null ? cacheContext.State : inventory.State;
        private bool CargoAccessible => TopDown3DCargoAccess.CanAccess(cargo, inventory != null ? inventory.transform : null);
        private bool CanMutateLeft() => cacheContext == null || cacheContext.CanAccess(inventory.transform);
        private bool CommandsAllowed => IsOpen && !suspended && input != null && input.Mode == TopDown3DInputMode.Inventory;

        private void BindCommands()
        {
            if(CampExpeditionFeature.Enabled(gameObject.scene))return;
            UnbindCommands();
            nextInventory = input?.InputActions?.FindAction("UI/NextInventory", false);
            previousInventory = input?.InputActions?.FindAction("UI/PreviousInventory", false);
            quickTransfer = input?.InputActions?.FindAction("UI/QuickTransfer", false);
            if (nextInventory != null) nextInventory.performed += SwitchPanel;
            if (previousInventory != null) previousInventory.performed += SwitchPanel;
            if (quickTransfer != null) quickTransfer.performed += QuickTransfer;
            InputSystem.onAfterUpdate += ReleaseInventoryControls;
        }
        private void UnbindCommands()
        {
            InputSystem.onAfterUpdate -= ReleaseInventoryControls;
            blockedInventoryControls.Clear();
            if (nextInventory != null) nextInventory.performed -= SwitchPanel;
            if (previousInventory != null) previousInventory.performed -= SwitchPanel;
            if (quickTransfer != null) quickTransfer.performed -= QuickTransfer;
            nextInventory = previousInventory = quickTransfer = null;
        }
        private void SwitchPanel(InputAction.CallbackContext context)
        {
            if (!blockedInventoryControls.Contains(context.control)) SelectPanel(context.action == nextInventory);
        }
        public void SwitchPanel() => SelectPanel(!inspectingCargo);
        public void SelectPanel(bool selectCargo)
        {
            if (!CommandsAllowed || cargo == null || eventSystem == null) return;
            RememberSelection();
            if (inspectingCargo == selectCargo) return;
            inspectingCargo = selectCargo;
            if (inspectingCargo)
            {
                RequestCargoApproach();
                eventSystem.SetSelectedGameObject(canvas.CargoSlotViews[Mathf.Clamp(inspectedCargoSlot, 0, cargo.State.Capacity - 1)].gameObject);
            }
            else eventSystem.SetSelectedGameObject(canvas.SlotViews[Mathf.Clamp(lastSelectedSlot, 0, LeftState.Capacity - 1)].gameObject);
        }
        private void QuickTransfer(InputAction.CallbackContext context)
        {
            if (!blockedInventoryControls.Contains(context.control)) QuickTransfer();
        }
        public void QuickTransfer()
        {
            if (!CommandsAllowed || cargo == null) return;
            if (!CargoAccessible || !CanMutateLeft()) { ShowNotice(cacheContext != null ? "Both the Legger and local cache must be within reach." : "Legger cargo is out of reach or obstructed."); return; }
            var source = inspectingCargo ? cargo.State : LeftState;
            var destination = inspectingCargo ? LeftState : cargo.State;
            var index = inspectingCargo ? inspectedCargoSlot : lastSelectedSlot;
            if (index < 0 || index >= source.Capacity || source.Slots[index].IsEmpty) return;
            var definition = source.ItemCatalog.GetRequiredDefinition(source.Slots[index].ItemId);
            var before = source.Slots[index].Quantity;
            selectedSource = -1; selectedCargoSource = false;
            mutating = true;
            var equipping = inspectingCargo && destination.Policy.IsLoadout && definition.IsEquipment;
            var result = equipping
                ? TopDown3DInventoryTransferService.EquipSlot(source, destination, index, () => CargoAccessible && CanMutateLeft())
                : TopDown3DInventoryTransferService.TransferSlot(source, destination, index, () => CargoAccessible && CanMutateLeft());
            mutating = false;
            ShowNotice(result.Succeeded ? equipping ? result.Message : $"Moved {result.Quantity} {definition.DisplayName}; {before - result.Quantity} remain."
                : definition.IsResource && cacheContext == null && inspectingCargo
                    ? "Resources stay with the Legger. Use a local resource cache to unload."
                    : result.Message);
            Refresh();
        }

        public void OpenCache(TopDown3DResourceCache cache)
        {
            if (cache == null || !cache.CanAccess(inventory.transform)) return;
            if (IsOpen) Close();
            cacheContext = cache;
            cache.State.Changed += Refresh;
            canvas.InitializeDual(cache.State.Capacity, cargo != null ? cargo.State.Capacity : 0,
                ActivateSlot, SelectSlot, ActivateCargoSlot, SelectCargoSlot, HandleAutoPack);
            canvas.SetLeftTitle("RESOURCE CACHE");
            canvas.ConfigureCache(true, UnloadCargo);
            canvas.ConfigureLoadout(false);
            Open(true);
        }

        public void UnloadCargo()
        {
            if (!CommandsAllowed || cacheContext == null || cargo == null) return;
            if (!CargoAccessible || !CanMutateLeft()) { ShowNotice("Bring the Legger beside the cache to unload."); return; }
            selectedSource = -1; selectedCargoSource = false;
            mutating = true;
            var result = TopDown3DInventoryTransferService.UnloadResources(cargo.State, cacheContext.State,
                () => CargoAccessible && CanMutateLeft());
            mutating = false;
            ShowNotice(result.Succeeded ? $"Stored {result.Quantity} resource units. Remaining cargo stays with the Legger." : result.Message);
            Refresh();
        }

        private void ClearCacheContext(bool rebuild = true)
        {
            if (cacheContext == null) return;
            cacheContext.State.Changed -= Refresh;
            cacheContext = null;
            if (!rebuild) return;
            canvas.InitializeDual(inventory.Capacity, cargo != null ? cargo.State.Capacity : 0,
                ActivateSlot, SelectSlot, ActivateCargoSlot, SelectCargoSlot, HandleAutoPack);
            canvas.SetLeftTitle("BOOTER KIT");
            canvas.ConfigureCache(false, null);
            canvas.ConfigureLoadout(inventory.UsesLoadout);
        }
        private void RequestCargoApproach()
        {
            if (cargo != null && cargo.isActiveAndEnabled && !CargoAccessible
                && Vector3.Distance(cargo.transform.position, inventory.transform.position) <= 24f)
                cargo.GetComponent<TopDown3DBigArmFollower>()?.RequestCallOver();
        }
        private void RememberPending()
        {
            var state = selectedCargoSource ? cargo.State : LeftState;
            pendingRevision = state.Revision;
            pendingItem = state.Slots[selectedSource].ItemId;
        }
        private void ValidatePending()
        {
            if (selectedSource < 0 || mutating) return;
            var state = selectedCargoSource && cargo != null ? cargo.State : LeftState;
            if (selectedSource >= state.Capacity || state.Revision != pendingRevision || state.Slots[selectedSource].ItemId != pendingItem)
            { selectedSource = -1; selectedCargoSource = false; ShowNotice("Stack changed; selection cancelled safely."); }
        }
        private void ShowNotice(string text)
        {
            actionController?.ReportFeedback(text);
            if (canvas != null && input != null)
                canvas.SetControlHints(text + "\n[" + input.GetBindingDisplayName("UI/QuickTransfer", "R") + "]: quick transfer  ["
                    + input.GetBindingDisplayName("UI/Cancel", "Back") + "]: cancel / back");
        }
    }
}
