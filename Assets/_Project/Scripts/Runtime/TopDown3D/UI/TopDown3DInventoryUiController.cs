using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed partial class TopDown3DInventoryUiController : MonoBehaviour
    {
        private TopDown3DCampLoopInventoryTrial inventoryTrial;
        private double lastDisplayedAlgae = double.NaN;
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
            canvas.ConfigureLoadout(inventory.UsesLoadout);
            if (inventory.UsesLoadout)
            {
                inventoryTrial = inventory.GetComponent<TopDown3DCampLoopInventoryTrial>();
                if (inventoryTrial == null) inventoryTrial = inventory.gameObject.AddComponent<TopDown3DCampLoopInventoryTrial>();
                inventoryTrial.Configure(inventory, cargo);
                var presentation = inventory.GetComponent<TopDown3DLoadoutPresentation>();
                if (presentation == null) presentation = inventory.gameObject.AddComponent<TopDown3DLoadoutPresentation>();
                presentation.Configure(inventory);
            }
            Subscribe();
            Refresh();
            RefreshControlHints();
            configured = true;
            BindCommands();
        }

        public void SuspendForMenu()
        {
            inventoryTrial?.CancelConfirmation();
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
            ArmInventoryControls();
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
            ClearCacheContext();
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
            inspectingCargo = focusCargo || rememberedCargoFocus;
            if (focusCargo) RequestCargoApproach();
            ArmInventoryControls();
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
            inventoryTrial?.CancelConfirmation();
            if (!IsOpen)
            {
                return;
            }

            RememberSelection();
            rememberedCargoFocus = inspectingCargo;
            selectedSource = -1;
            selectedCargoSource = false;
            canvas.SetVisible(false);
            input?.EnterGameplayMode();
            Refresh();
            ClearCacheContext();
        }

        internal void ActivateSlot(int slotIndex)
        {
            if (InventoryActivationBlocked) return;
            if (!IsOpen || suspended || input.Mode != TopDown3DInputMode.Inventory || slotIndex < 0 || slotIndex >= LeftState.Capacity)
            {
                return;
            }

            var slot = LeftState.Slots[slotIndex];
            if (LeftState.Policy.IsLoadout && slotIndex < TopDown3DLoadoutLayout.EquipmentCount)
            {
                selectedSource = -1;
                SelectSlot(slotIndex);
                Refresh();
                ShowNotice(slotIndex == 1 ? "Attached algae reserve: Eat, Drink and Treat use the existing gameplay controls."
                    : "Equipment has a fixed slot. Quick Transfer stores it with the nearby Legger; choose cargo equipment to fit a replacement.");
                return;
            }
            if (selectedSource < 0)
            {
                if (!slot.IsEmpty)
                {
                    selectedSource = slotIndex;
                    selectedCargoSource = false;
                    RememberPending();
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

            if (selectedCargoSource) { ShowNotice("Use Quick Transfer for automatic placement between inventories."); return; }
            if (!CanMutateLeft()) { ShowNotice("Resource cache is out of reach."); return; }
            mutating = true;
            var result = LeftState.TryMoveOrSwap(selectedSource, slotIndex);
            mutating = false;
            if (!result.Succeeded) { ShowNotice(result.Message); return; }
            selectedSource = -1;
            selectedCargoSource = false;
            lastSelectedSlot = slotIndex;
            Refresh();
        }

        internal void ActivateCargoSlot(int slotIndex)
        {
            if (InventoryActivationBlocked) return;
            if (!IsOpen || suspended || input.Mode != TopDown3DInputMode.Inventory || cargo == null || slotIndex < 0 || slotIndex >= cargo.State.Capacity) return;
            if (selectedSource >= 0 && !selectedCargoSource)
            {
                ShowNotice("Use Quick Transfer for automatic placement between inventories."); return;
            }
            var slot = cargo.State.Slots[slotIndex];
            if (selectedSource < 0 && !slot.IsEmpty && CargoAccessible) { selectedSource = slotIndex; selectedCargoSource = true; RememberPending(); }
            else if (selectedSource == slotIndex && selectedCargoSource) { selectedSource = -1; selectedCargoSource = false; }
            else if (selectedSource >= 0 && selectedCargoSource)
            {
                if (!CargoAccessible) { ShowNotice("Legger cargo is out of reach."); return; }
                mutating = true;
                var result = cargo.State.TryMoveOrSwap(selectedSource, slotIndex);
                mutating = false;
                if (!result.Succeeded) { ShowNotice(result.Message); return; }
                selectedSource = -1; selectedCargoSource = false;
            }
            Refresh();
        }

        internal void SelectCargoSlot(int slotIndex)
        {
            if (cargo == null || slotIndex < 0 || slotIndex >= cargo.State.Capacity) return;
            inspectingCargo = true; inspectedCargoSlot = slotIndex;
            var slot = cargo.State.Slots[slotIndex];
            var definition = !slot.IsEmpty && cargo.ItemCatalog.TryGetDefinition(slot.ItemId, out var found) ? found : null;
            canvas.SetDetails(definition != null ? definition.DisplayName : "Empty mount",
                definition != null ? $"{definition.Description}\n{slot.Quantity} units · {cargo.State.CalculateSlotMass(slotIndex):0.0} mass" + ContentsDescription(slot) : "Choose a cargo stack.",
                CargoAccessible ? "LEGGER CARGO" : "LEGGER CARGO — READ ONLY");
        }

        private void HandleAutoPack()
        {
            if (suspended || input.Mode == TopDown3DInputMode.SystemMenu || input.IsCampMode) return;
            if (cargo == null || !CargoAccessible) { ShowNotice("Legger cargo is out of reach."); return; }
            selectedSource = -1; selectedCargoSource = false;
            if (cargo.TryAutoPack()) ShowNotice("Cargo organized; quantities unchanged.");
            Refresh();
        }

        internal void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= LeftState.Capacity)
            {
                return;
            }

            lastSelectedSlot = slotIndex;
            inspectingCargo = false;
            RefreshDetails(slotIndex);
        }

        private void HandleToggleRequested()
        {
            if (CloseCampWorkstationOnToggle()) return;
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
            if (IsOpen && inventoryTrial != null && inventoryTrial.CancelConfirmation()) { Refresh(); RefreshControlHints(); return; }
            if (IsOpen && !suspended && input.Mode != TopDown3DInputMode.SystemMenu)
            {
                if (selectedSource >= 0) { selectedSource = -1; selectedCargoSource = false; Refresh(); }
                else Close();
            }
        }

        private void Refresh()
        {
            if (inventory == null || canvas == null)
            {
                return;
            }

            ValidatePending();
            var state = LeftState;
            for (var i = 0; i < state.Capacity && i < canvas.SlotViews.Count; i++)
            {
                var slot = state.Slots[i];
                var definition = !slot.IsEmpty
                    && state.ItemCatalog.TryGetDefinition(slot.ItemId, out var found)
                    ? found
                    : null;
                canvas.SlotViews[i].Refresh(slot, definition, !selectedCargoSource && i == selectedSource);
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
                canvas.SetCargoSummary($"LOAD {profile.TotalMass:0.0} / {cargo.State.Policy.MaximumMass:0.0}  "
                    + $"{cargo.State.OccupiedSlotCount}/{cargo.State.Capacity} SLOTS  {profile.Band}"
                    + (CargoAccessible ? "" : "\nREAD ONLY — Legger out of reach"));
            }

            if (inspectingCargo && cargo != null) SelectCargoSlot(inspectedCargoSlot);
            else RefreshDetails(Mathf.Clamp(lastSelectedSlot, 0, state.Capacity - 1));
            if (state.Policy.IsLoadout)
            {
                for (var i = 0; i < 6; i++)
                {
                    var status = state.Slots[i].IsEmpty ? i == 0 && actionController?.HarvesterState?.DeployedCount > 0 ? "Deployed" : "Missing" : "Equipped";
                    if (i == 1 && !state.Slots[i].IsEmpty)
                    {
                        var algae = inventory.GetComponent<TopDown3DAlgaeCanisterState>();
                        if (algae != null) status = $"{algae.Remaining:0.0} / {algae.Settings.capacity:0} algae";
                    }
                    canvas.SlotViews[i].SetLoadoutCaption(TopDown3DLoadoutLayout.Label(i) + "\n" + status);
                }
                var selected = inspectingCargo && cargo != null ? cargo.State : state;
                var index = inspectingCargo ? inspectedCargoSlot : lastSelectedSlot;
                var canTransfer = CargoAccessible && index >= 0 && index < selected.Capacity && selected.CanRelease(index);
                if (canTransfer)
                {
                    var item = selected.ItemCatalog.GetRequiredDefinition(selected.Slots[index].ItemId);
                    canTransfer = inspectingCargo ? !item.IsResource && (item.IsEquipment || item.IsFieldSupply)
                        : cargo.State.Policy.Allows(item);
                }
                canvas.ConfigureTrialControls(() => ShowNotice(inventoryTrial.RequestReset()), inventoryTrial != null && inventoryTrial.Ready,
                    inventoryTrial != null && inventoryTrial.ConfirmationPending, QuickTransfer, canTransfer,
                    inspectingCargo ? "FIT / TAKE SELECTED ITEM" : "STORE SELECTED ITEM",
                    () => { Close(); if (!actionController.SelectCanister()) actionController.ReportFeedback("Canister deployment is unavailable."); },
                    !state.Slots[0].IsEmpty && actionController?.CanSelectCanister == true);
            }
            else canvas.ConfigureTrialControls(null, false, false, null, false, "", null, false);
        }

        private void RefreshCargoAccess()
        {
            if (cargo == null || inventory == null || canvas == null) return;
            lastCargoAccess = CargoAccessible;
            canvas.SetCargoAccess(lastCargoAccess);
            if (!lastCargoAccess && selectedCargoSource) { selectedCargoSource = false; selectedSource = -1; }
        }

        private void Update()
        {
            if (!IsOpen || inventory == null) return;
            if (inventory.UsesLoadout)
            {
                var algae = inventory.GetComponent<TopDown3DAlgaeCanisterState>();
                var displayed = algae != null ? Math.Floor(algae.Remaining * 10) : -1;
                if (displayed != lastDisplayedAlgae) { lastDisplayedAlgae = displayed; Refresh(); }
            }
            if (cargo == null) return;
            if (lastCargoAccess != CargoAccessible) { RefreshCargoAccess(); Refresh(); }
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
                $"[{select}]: select / move  [{input.GetBindingDisplayName("UI/QuickTransfer", "R")}]: quick transfer  "
                + $"[{input.GetBindingDisplayName("UI/PreviousInventory", "Q")}]: {(cacheContext != null ? "cache" : "Booter")}  "
                + $"[{input.GetBindingDisplayName("UI/NextInventory", "E")}]: Legger\n"
                + $"[{cancel}]: cancel / back     [{toggle}]: close");
        }

        private void RefreshDetails(int slotIndex)
        {
            if (LeftState.Policy.IsLoadout && slotIndex < 6)
            {
                var equipment = LeftState.Slots[slotIndex];
                var description = equipment.IsEmpty ? "Missing equipment. Fit a compatible piece from physically accessible Legger cargo."
                    : LeftState.ItemCatalog.GetRequiredDefinition(equipment.ItemId).Description;
                if (slotIndex == 0 && equipment.IsEmpty && actionController?.HarvesterState?.DeployedCount > 0)
                    description = "Deployed: this slot is reserved. Recover the same canister; collected dust goes to accessible Legger cargo.";
                if (slotIndex == 1)
                {
                    var algae = inventory.GetComponent<TopDown3DAlgaeCanisterState>();
                    description += algae != null ? $"\nAlgae {algae.Remaining:0.0} / {algae.Settings.capacity:0.0}. Eat / Drink / Treat use the gameplay controls." : "\nAlgae service unavailable.";
                }
                description += ContentsDescription(equipment);
                canvas.SetDetails(TopDown3DLoadoutLayout.Label(slotIndex), description,
                    $"{LeftState.Slots.Take(6).Count(s => !s.IsEmpty)} / 6 EQUIPPED · {LeftState.Slots.Skip(6).Count(s => !s.IsEmpty)} / 4 SUPPLIES");
                return;
            }
            var state = LeftState;
            var slot = state.Slots[Mathf.Clamp(slotIndex, 0, state.Capacity - 1)];
            if (!slot.IsEmpty && state.ItemCatalog.TryGetDefinition(slot.ItemId, out var definition))
            {
                canvas.SetDetails(
                    definition.DisplayName,
                    $"{definition.Description}\n{slot.Quantity} units · {state.CalculateSlotMass(Mathf.Clamp(slotIndex, 0, state.Capacity - 1)):0.0} mass" + ContentsDescription(slot),
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
        private static string ContentsDescription(TopDown3DInventorySlot slot) => slot.CanisterContents.IsPresent
            ? $"\nArc Dust: {slot.CanisterContents.dust}/100. Collection pauses while carried or docked." : string.Empty;

        private void RestoreSelection()
        {
            if (eventSystem == null || canvas.SlotViews.Count == 0)
            {
                return;
            }

            if (inspectingCargo && cargo != null && canvas.CargoSlotViews.Count > 0)
            {
                eventSystem.SetSelectedGameObject(canvas.CargoSlotViews[Mathf.Clamp(inspectedCargoSlot, 0, cargo.State.Capacity - 1)].gameObject);
                return;
            }
            lastSelectedSlot = Mathf.Clamp(lastSelectedSlot, 0, LeftState.Capacity - 1);
            eventSystem.SetSelectedGameObject(canvas.SlotViews[lastSelectedSlot].gameObject);
        }

        private void RememberSelection()
        {
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var slot = selected != null ? selected.GetComponent<TopDown3DInventorySlotView>() : null;
            if (slot != null)
            {
                if (System.Linq.Enumerable.Contains(canvas.CargoSlotViews, slot)) { inspectingCargo = true; inspectedCargoSlot = slot.SlotIndex; }
                else { inspectingCargo = false; lastSelectedSlot = slot.SlotIndex; }
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
            if (inventoryTrial != null) inventoryTrial.Changed += Refresh;
            if (cargo != null) cargo.State.Changed += Refresh;
            if (actionController != null) actionController.CargoAccessRequested += HandleCargoAccessRequested;
            if (actionController != null) actionController.CacheAccessRequested += OpenCache;
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
                if (inventoryTrial != null) inventoryTrial.Changed -= Refresh;
            }
            if (cargo != null) cargo.State.Changed -= Refresh;
            if (actionController != null) actionController.CargoAccessRequested -= HandleCargoAccessRequested;
            if (actionController != null) actionController.CacheAccessRequested -= OpenCache;

            subscribed = false;
        }

        private void HandleCargoAccessRequested(TopDown3DBigArmCargo requestedCargo)
        {
            if (input == null || input.Mode != TopDown3DInputMode.Gameplay
                || !TopDown3DCargoAccess.CanAccess(requestedCargo, inventory.transform)) return;
            cargo = requestedCargo;
            Open(true);
        }

        private void HandlePromptDeviceChanged(TopDown3DPromptDevice _)
        {
            RefreshControlHints();
        }

        private void OnEnable()
        {
            if (!configured) return;
            Subscribe();
            BindCommands();
        }

        private void OnDisable()
        {
            UnbindCommands();
            Unsubscribe();
            ClearCacheContext();
            if (suspended) { suspended = false; canvas?.SetVisible(false); return; }
            if (IsOpen)
            {
                canvas.SetVisible(false);
                input?.EnterGameplayMode();
            }
        }
    }
}
