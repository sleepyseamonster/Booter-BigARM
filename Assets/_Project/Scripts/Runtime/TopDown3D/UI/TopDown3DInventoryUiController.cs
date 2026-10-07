using UnityEngine;
using UnityEngine.EventSystems;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DInventoryUiController : MonoBehaviour
    {
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private TopDown3DPlayerInventory inventory;
        [SerializeField] private TopDown3DPlayerActionController actionController;
        [SerializeField] private TopDown3DInventoryCanvas canvas;
        [SerializeField] private EventSystem eventSystem;

        private int selectedSource = -1;
        private int lastSelectedSlot;
        private bool subscribed;
        private TopDown3DBigArmCargo cargo;
        private bool selectedCargoSource;
        private bool lastCargoAccess;
        private bool inspectingCargo;
        private int inspectedCargoSlot;
        private bool suspended, configured;
        private bool suspendedCargoFocus;
        private int suspendedSlot;
        public bool IsConfigured => configured;
        public bool IsSuspended => suspended;

        public bool IsOpen => canvas != null && canvas.IsVisible;
        public int SelectedSource => selectedSource;
        public EventSystem EventSystem => eventSystem;

        public void Configure(
            TopDown3DInputRouter inputRouter,
            TopDown3DPlayerInventory playerInventory,
            TopDown3DPlayerActionController playerAction,
            TopDown3DInventoryCanvas inventoryCanvas,
            EventSystem inventoryEventSystem)
        {
            Unsubscribe();
            input = inputRouter;
            inventory = playerInventory;
            actionController = playerAction;
            canvas = inventoryCanvas;
            eventSystem = inventoryEventSystem;
            cargo = TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmCargo>(gameObject.scene);
            if (cargo != null)
                canvas.InitializeDual(inventory.Capacity, cargo.State.Capacity, ActivateSlot, SelectSlot, ActivateCargoSlot, SelectCargoSlot, HandleAutoPack);
            else
                canvas.Initialize(inventory.Capacity, ActivateSlot, SelectSlot);
            canvas.SetVisible(false);
            Subscribe();
            Refresh();
            RefreshControlHints();
            configured = true;
        }

        public void SuspendForMenu()
        {
            if (!IsOpen || suspended) return;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var cargoIndex = selected != null ? System.Linq.Enumerable.ToList(canvas.CargoSlotViews)
                .FindIndex(slot => slot.gameObject == selected) : -1;
            suspendedCargoFocus = cargoIndex >= 0;
            suspendedSlot = suspendedCargoFocus ? cargoIndex : lastSelectedSlot;
            selectedSource = -1; selectedCargoSource = false;
            suspended = true;
            canvas.SetSuspended(true);
        }

        public void ResumeFromMenu()
        {
            if (!suspended) return;
            suspended = false; canvas.SetSuspended(false);
            RefreshCargoAccess(); Refresh();
            input.EnterInventoryMode();
            if (suspendedCargoFocus && lastCargoAccess && suspendedSlot < canvas.CargoSlotViews.Count)
                eventSystem?.SetSelectedGameObject(canvas.CargoSlotViews[suspendedSlot].gameObject);
            else RestoreSelection();
        }

        public void EndSuspensionWithoutResume()
        {
            if (!suspended) return;
            suspended = false; canvas.SetSuspended(false); canvas.SetVisible(false);
            selectedSource = -1; selectedCargoSource = false;
        }

        public void Open(bool focusCargo = false)
        {
            if (IsOpen || input == null || inventory == null || canvas == null)
            {
                return;
            }

            actionController?.CancelGather();
            selectedSource = -1;
            selectedCargoSource = false;
            inspectingCargo = focusCargo;
            canvas.SetVisible(true);
            input.EnterInventoryMode();
            RefreshControlHints();
            Refresh();
            RefreshCargoAccess();
            RestoreSelection();
            if (focusCargo && cargo != null && canvas.CargoSlotViews.Count > 0 && eventSystem != null)
                eventSystem.SetSelectedGameObject(canvas.CargoSlotViews[0].gameObject);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            RememberSelection();
            selectedSource = -1;
            selectedCargoSource = false;
            canvas.SetVisible(false);
            input?.EnterGameplayMode();
            Refresh();
        }

        internal void ActivateSlot(int slotIndex)
        {
            if (!IsOpen || suspended || input.Mode == TopDown3DInputMode.SystemMenu || slotIndex < 0 || slotIndex >= inventory.State.Capacity)
            {
                return;
            }

            var slot = inventory.State.Slots[slotIndex];
            if (selectedSource < 0)
            {
                if (!slot.IsEmpty)
                {
                    selectedSource = slotIndex;
                    selectedCargoSource = false;
                }

                SelectSlot(slotIndex);
                Refresh();
                return;
            }

            if (!selectedCargoSource && selectedSource == slotIndex)
            {
                selectedSource = -1;
                Refresh();
                return;
            }

            if (selectedCargoSource && cargo != null && !cargo.IsInAccessRange(inventory.transform.position)) return;
            if (selectedCargoSource && cargo != null)
                TopDown3DInventoryTransferService.Transfer(cargo.State, inventory.State, cargo.State.Slots[selectedSource].ItemId, cargo.State.Slots[selectedSource].Quantity);
            else
                inventory.State.TryMoveOrSwap(selectedSource, slotIndex);
            selectedSource = -1;
            selectedCargoSource = false;
            lastSelectedSlot = slotIndex;
            Refresh();
        }

        internal void ActivateCargoSlot(int slotIndex)
        {
            if (!IsOpen || suspended || input.Mode == TopDown3DInputMode.SystemMenu || cargo == null || slotIndex < 0 || slotIndex >= cargo.State.Capacity) return;
            if (!cargo.IsInAccessRange(inventory.transform.position)) return;
            if (selectedSource >= 0 && !selectedCargoSource)
            {
                var source = inventory.State.Slots[selectedSource];
                if (!source.IsEmpty) TopDown3DInventoryTransferService.Transfer(inventory.State, cargo.State, source.ItemId, source.Quantity);
                selectedSource = -1; selectedCargoSource = false; Refresh(); return;
            }
            var slot = cargo.State.Slots[slotIndex];
            if (selectedSource < 0 && !slot.IsEmpty) { selectedSource = slotIndex; selectedCargoSource = true; }
            else if (selectedSource == slotIndex && selectedCargoSource) { selectedSource = -1; selectedCargoSource = false; }
            else if (selectedSource >= 0 && selectedCargoSource) { cargo.State.TryMoveOrSwap(selectedSource, slotIndex); selectedSource = -1; selectedCargoSource = false; }
            Refresh();
        }

        internal void SelectCargoSlot(int slotIndex)
        {
            if (cargo == null || slotIndex < 0 || slotIndex >= cargo.State.Capacity) return;
            inspectingCargo = true; inspectedCargoSlot = slotIndex;
            var slot = cargo.State.Slots[slotIndex];
            var definition = !slot.IsEmpty && cargo.ItemCatalog.TryGetDefinition(slot.ItemId, out var found) ? found : null;
            canvas.SetDetails(definition != null ? definition.DisplayName : "Empty mount",
                definition != null ? definition.Description : "Choose a cargo stack.",
                cargo.IsInAccessRange(inventory.transform.position) ? "LEGGER CARGO" : "LEGGER OUT OF REACH");
        }

        private void HandleAutoPack()
        {
            if (suspended || input.Mode == TopDown3DInputMode.SystemMenu) return;
            if (cargo == null || !cargo.IsInAccessRange(inventory.transform.position)) return;
            if (cargo.TryAutoPack()) canvas.SetCargoSummary("Auto-packed: stable");
            Refresh();
        }

        internal void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inventory.State.Capacity)
            {
                return;
            }

            lastSelectedSlot = slotIndex;
            inspectingCargo = false;
            RefreshDetails(slotIndex);
        }

        private void HandleToggleRequested()
        {
            if (this == null || canvas == null || input == null) return;
            if (suspended || input.Mode == TopDown3DInputMode.SystemMenu) return;
            var radial = TopDown3DGameHudCanvas.FindInScene<TopDown3DRadialMenuController>(gameObject.scene);
            if (radial != null && radial.IsCustomizing) return;
            if (IsOpen && !suspended && input.Mode != TopDown3DInputMode.SystemMenu)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void HandleCancelRequested()
        {
            if (IsOpen && !suspended && input.Mode != TopDown3DInputMode.SystemMenu)
            {
                Close();
            }
        }

        private void Refresh()
        {
            if (inventory == null || canvas == null)
            {
                return;
            }

            var state = inventory.State;
            for (var i = 0; i < state.Capacity && i < canvas.SlotViews.Count; i++)
            {
                var slot = state.Slots[i];
                var definition = !slot.IsEmpty
                    && inventory.ItemCatalog.TryGetDefinition(slot.ItemId, out var found)
                    ? found
                    : null;
                canvas.SlotViews[i].Refresh(slot, definition, i == selectedSource);
            }
            if (cargo != null)
            {
                for (var i = 0; i < cargo.State.Capacity && i < canvas.CargoSlotViews.Count; i++)
                {
                    var slot = cargo.State.Slots[i];
                    var definition = !slot.IsEmpty && cargo.ItemCatalog.TryGetDefinition(slot.ItemId, out var found) ? found : null;
                    canvas.CargoSlotViews[i].Refresh(slot, definition, selectedCargoSource && i == selectedSource);
                }
                var profile = cargo.LoadProfile;
                canvas.SetCargoSummary(cargo.IsInAccessRange(inventory.transform.position)
                    ? $"{profile.Band}  {profile.TotalMass:0.0} / {profile.RecommendedMass:0.0} mass   balance {profile.Balance:P0}"
                    : "LEGGER OUT OF REACH — call him over to access cargo");
            }

            if (inspectingCargo && cargo != null) SelectCargoSlot(inspectedCargoSlot);
            else RefreshDetails(Mathf.Clamp(lastSelectedSlot, 0, state.Capacity - 1));
        }

        private void RefreshCargoAccess()
        {
            if (cargo == null || inventory == null || canvas == null) return;
            lastCargoAccess = cargo.IsInAccessRange(inventory.transform.position);
            canvas.SetCargoAccess(lastCargoAccess);
            if (!lastCargoAccess && selectedCargoSource) { selectedCargoSource = false; selectedSource = -1; }
        }

        private void Update()
        {
            if (!IsOpen || cargo == null || inventory == null) return;
            if (lastCargoAccess != cargo.IsInAccessRange(inventory.transform.position)) { RefreshCargoAccess(); Refresh(); }
        }

        private void RefreshControlHints()
        {
            if (input == null || canvas == null)
            {
                return;
            }

            var select = input.GetBindingDisplayName("UI/Submit", "Select");
            var toggle = input.GetBindingDisplayName("System/ToggleInventory", "Tab");
            var cancel = input.GetBindingDisplayName("UI/Cancel", "Cancel");
            canvas.SetControlHints(
                $"[{select}]: move / merge / swap     [{toggle}] or [{cancel}]: close");
        }

        private void RefreshDetails(int slotIndex)
        {
            var state = inventory.State;
            var slot = state.Slots[Mathf.Clamp(slotIndex, 0, state.Capacity - 1)];
            if (!slot.IsEmpty && inventory.ItemCatalog.TryGetDefinition(slot.ItemId, out var definition))
            {
                canvas.SetDetails(
                    definition.DisplayName,
                    definition.Description,
                    $"{state.OccupiedSlotCount} / {state.Capacity} SLOTS");
            }
            else
            {
                canvas.SetDetails(
                    "Empty Slot",
                    selectedSource >= 0 ? "Select this slot to move the chosen stack." : "Select an item stack to inspect or move it.",
                    $"{state.OccupiedSlotCount} / {state.Capacity} SLOTS");
            }
        }

        private void RestoreSelection()
        {
            if (eventSystem == null || canvas.SlotViews.Count == 0)
            {
                return;
            }

            lastSelectedSlot = Mathf.Clamp(lastSelectedSlot, 0, canvas.SlotViews.Count - 1);
            eventSystem.SetSelectedGameObject(canvas.SlotViews[lastSelectedSlot].gameObject);
        }

        private void RememberSelection()
        {
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var slot = selected != null ? selected.GetComponent<TopDown3DInventorySlotView>() : null;
            if (slot != null)
            {
                lastSelectedSlot = slot.SlotIndex;
            }
        }

        private void Subscribe()
        {
            if (subscribed || input == null || inventory == null)
            {
                return;
            }

            input.InventoryToggleRequested += HandleToggleRequested;
            input.UiCancelRequested += HandleCancelRequested;
            input.PromptDeviceChanged += HandlePromptDeviceChanged;
            inventory.State.Changed += Refresh;
            if (cargo != null) cargo.State.Changed += Refresh;
            if (actionController != null) actionController.CargoAccessRequested += HandleCargoAccessRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (input != null)
            {
                input.InventoryToggleRequested -= HandleToggleRequested;
                input.UiCancelRequested -= HandleCancelRequested;
                input.PromptDeviceChanged -= HandlePromptDeviceChanged;
            }

            if (inventory != null)
            {
                inventory.State.Changed -= Refresh;
            }
            if (cargo != null) cargo.State.Changed -= Refresh;
            if (actionController != null) actionController.CargoAccessRequested -= HandleCargoAccessRequested;

            subscribed = false;
        }

        private void HandleCargoAccessRequested(TopDown3DBigArmCargo requestedCargo)
        {
            if (requestedCargo == null || !requestedCargo.IsInAccessRange(inventory.transform.position)) return;
            cargo = requestedCargo;
            Open(true);
        }

        private void HandlePromptDeviceChanged(TopDown3DPromptDevice _)
        {
            RefreshControlHints();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (suspended) { suspended = false; canvas?.SetVisible(false); return; }
            if (IsOpen)
            {
                canvas.SetVisible(false);
                input?.EnterGameplayMode();
            }
        }
    }
}
