using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DCampWorkstationUiController : MonoBehaviour
    {
        private TopDown3DCampLoopEconomySession session;
        private TopDown3DCampWorkstationCanvas view;
        private TopDown3DInventoryUiController inventoryUi;
        private TopDown3DMenuController menu;
        private EventSystem events;
        private ITopDown3DInteractable target;
        private bool open, suspended, waitingRelease, subscribed, resetPending;
        private int source, destination, selected = -1;
        private long selectedRevision;
        private uint openedEpoch;
        private string message;
        private GameObject suspendedSelection;
        private readonly List<TopDown3DInventoryState> observedStates = new();
        public bool IsOpen => open;
        public bool IsSuspended => suspended;

        public void Configure(TopDown3DCampLoopEconomySession owner)
        {
            if (subscribed) return;
            session = owner; view = GetComponent<TopDown3DCampWorkstationCanvas>(); view.Initialize(Command, Select);
            inventoryUi = TopDown3DGameHudCanvas.FindInScene<TopDown3DInventoryUiController>(gameObject.scene);
            menu = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene);
            events = inventoryUi != null ? inventoryUi.EventSystem : null;
            if (events == null || menu == null) throw new InvalidOperationException("Camp UI requires the existing menu and EventSystem.");
            menu.ConfigureCampWorkstations(this); inventoryUi.ConfigureCampWorkstations(this);
            session.Action.CampStationAccessRequested += OpenTarget;
            session.Action.CanisterSelectionRequested += OpenDeployment;
            session.Action.AccessibleCargoCanister = () => session.CanAccessOwner(1)
                && AnyCanister(session.Owner(1));
            session.Input.UiCancelRequested += Cancel;
            session.Input.ModeChanged += ModeChanged;
            observedStates.Clear(); observedStates.Add(session.Kit.State); observedStates.Add(session.Cargo.State); observedStates.Add(session.Cache.State);
            foreach (var state in observedStates) state.Changed += Refresh;
            session.Refinery.Changed += Refresh;
            BindManagement(); subscribed = true;
        }
        private static bool AnyCanister(TopDown3DInventoryState state)
        { foreach (var slot in state.Slots) if (slot.ItemId == TopDown3DHarvesterSettings.CanisterItemId) return true; return false; }
        private bool TargetAccessible => target == null || target.UnityObject != null &&
            (session.Site?.Registry?.FindStation(target)?.CanAccess(session.Kit.transform) == true
            || target is TopDown3DCampWorkbench bench && bench.CanAccess(session.Kit.transform)
            || target is TopDown3DDustRefinery refinery && refinery.CanAccess(session.Kit.transform)
            || target is TopDown3DResourceCache chest && chest.CanAccess(session.Kit.transform));

        private void OpenTarget(ITopDown3DInteractable value) => Open(value);
        private void OpenDeployment() => Open(null);
        private void Open(ITopDown3DInteractable value)
        {
            if (open || !session.Ready || !ActorAlive || value != null && !session.Kit.GetComponent<TopDown3DPlayerMotor>().LaboratoryStandingActionsAllowed || session.Input.Mode != TopDown3DInputMode.Gameplay) return;
            session.RebindManagement(); StartManagement(); target = value; if (!TargetAccessible) { target = null; return; }
            inventoryUi.Close(); session.Action.CancelGather();
            open = true; source = value is TopDown3DResourceCache ? 2 : value == null ? 1 : 0;
            if (value is TopDown3DIronstoneProcessor) source = 2;
            destination = 0; selected = -1; openedEpoch = session.Epoch; resetPending = false;
            message = "Choose a source and an item. Contents stay in their physical owner until an action succeeds.";
            session.Action.SetCampModalOpen(true); session.Input.EnterInventoryMode();
            view.SetVisible(true); Arm(); Refresh(); view.SelectFirst(events);
        }
        public void Close(bool restoreGameplay = true)
        {
            if (!open) return;
            open = false; suspended = false; resetPending = false; processQuote = null; selected = -1; target = null;
            if (view != null) { view.SetVisible(false); view.SetInputReady(false); }
            if (session.Action != null) session.Action.SetCampModalOpen(false);
            if (restoreGameplay && session.Input.Mode == TopDown3DInputMode.Inventory) session.Input.EnterGameplayMode();
        }
        public void SuspendForMenu()
        {
            if (!open || suspended) return;
            suspended = true; resetPending = false; processQuote = null; suspendedSelection = events.currentSelectedGameObject;
            view.SetVisible(false); view.SetInputReady(false);
        }
        public bool ResumeFromMenu()
        {
            if (!open || !suspended) return false;
            if (!session.Ready || !ActorAlive || !RegistrationValid || openedEpoch != session.Epoch || !TargetAccessible) { Close(false); return false; }
            suspended = false; session.Input.EnterInventoryMode(); view.SetVisible(true); Arm(); Refresh();
            if (suspendedSelection != null && suspendedSelection.activeInHierarchy) events.SetSelectedGameObject(suspendedSelection);
            else view.SelectFirst(events);
            return true;
        }
        private void Arm() { waitingRelease = true; view.SetInputReady(false); }
        private bool ControlsHeld()
        {
            foreach (var path in new[] { "UI/Submit", "UI/Click", "UI/Cancel" })
            {
                var action = session.Input.InputActions.FindAction(path, false); if (action == null) continue;
                foreach (var control in action.controls) if (control is ButtonControl button && button.isPressed) return true;
            }
            return false;
        }
        private void Update()
        {
            if (!open || suspended) return;
            if (!session.Ready || !ActorAlive || !RegistrationValid || !TargetAccessible || openedEpoch != session.Epoch) { Close(); return; }
            if (Time.unscaledTime >= statusAt) { statusAt = Time.unscaledTime + .4f; Refresh(); }
            if (waitingRelease && !ControlsHeld()) { waitingRelease = false; view.SetInputReady(true); }
        }
        private void ModeChanged(TopDown3DInputMode mode)
        { if (mode == TopDown3DInputMode.Disabled || open && !suspended && mode != TopDown3DInputMode.Inventory) Close(false); }
        private void Cancel()
        {
            if (!open || suspended || waitingRelease || session.Input.Mode != TopDown3DInputMode.Inventory) return;
            if (processQuote != null) { processQuote = null; message = "Sorting cancelled."; Refresh(); return; }
            if (resetPending) { resetPending = false; message = "Reset cancelled."; Refresh(); } else Close();
        }
        private void Select(int index)
        {
            if (!CanCommand || !session.CanAccessOwner(source)) return;
            var state = session.Owner(source); if (index < 0 || index >= state.Capacity) return;
            selected = index; selectedRevision = state.Revision; resetPending = false; Refresh();
        }
        private bool CanCommand => open && !suspended && !waitingRelease && session.Ready && openedEpoch == session.Epoch
            && ActorAlive && RegistrationValid && session.Input.Mode == TopDown3DInputMode.Inventory && TargetAccessible;

        private void Command(string id)
        {
            if (!CanCommand) return;
            if (ManagementCommand(id)) return;
            if (management != ManagementView.Detail && (id == "Craft" || id == "Insert" || id == "Refine" || id == "Battery"
                || id == "Canister" || id == "Deploy" || id == "Transfer")) return;
            if (id == "Craft" && target is not TopDown3DCampWorkbench
                || (id == "Insert" || id == "Refine" || id == "Battery" || id == "Canister") && target is not TopDown3DDustRefinery
                || id == "Transfer" && target is not TopDown3DResourceCache || id == "Reset" && target == null) return;
            if (id == "Close") { Close(); return; }
            if (id == "Source") { source = (source + 1) % session.OwnerCount; processQuote = null; selected = -1; resetPending = false; Refresh(); return; }
            if (id == "Destination") { do { destination = (destination + 1) % session.OwnerCount; } while (!session.AcceptsTransfers(destination)); processQuote = null; resetPending = false; Refresh(); return; }
            if (id == "Reset")
            {
                if (!resetPending) { resetPending = true; resetStamp = session.ResetStamp(); processQuote = null; message = "Confirm reset: remove trial batteries/canisters and Ironstone products, clear sorter tray and replace all chest recipe ore with four. Cargo ore, other items, salvage, actors and vitals remain. Back cancels."; Arm(); Refresh(); return; }
                if (resetStamp != session.ResetStamp()) { resetPending = false; message = "Camp contents changed. Request and confirm reset again."; Arm(); Refresh(); return; }
                var result = session.ResetEconomy(openedEpoch); message = result.Succeeded ? "Camp economy restored." : result.Message;
                openedEpoch = session.Epoch; selected = -1; resetPending = false; Arm(); Refresh(); return;
            }
            resetPending = false;
            TopDown3DInventoryResult outcome;
            if (id == "Craft") outcome = session.Workbench.Craft(session.Kit);
            else if (id == "Refine") outcome = session.Refinery.Refine(() => CanCommand);
            else if (id == "Battery") outcome = session.Refinery.RetrieveBattery(session.Owner(destination), () => CanCommand && session.CanAccessOwner(destination) && session.AcceptsTransfers(destination));
            else if (id == "Canister") outcome = session.Refinery.RetrieveCanister(session.Owner(destination), () => CanCommand && session.CanAccessOwner(destination) && session.AcceptsTransfers(destination));
            else
            {
                var state = session.Owner(source);
                if (selected < 0 || selected >= state.Capacity || selectedRevision != state.Revision || !session.CanAccessOwner(source))
                { selected = -1; message = "Selection changed or source is out of reach. Select the item again."; Refresh(); return; }
                var slot = state.Slots[selected];
                if (id == "Deploy")
                {
                    var index = selected; var owner = source; Close();
                    session.Action.SelectCarriedCanister(state, index, () => session.CanAccessOwner(owner)); return;
                }
                if (id == "Insert") outcome = slot.ItemId == TopDown3DHarvesterSettings.CanisterItemId
                    ? session.Refinery.InsertCanister(state, selected, () => CanCommand && session.CanAccessOwner(source))
                    : session.Refinery.InsertBattery(state, selected, () => CanCommand && session.CanAccessOwner(source));
                else if (id == "Transfer")
                {
                    var moved = TopDown3DInventoryTransferService.TransferSlot(state, session.Owner(destination), selected,
                        () => CanCommand && session.CanAccessOwner(source) && session.CanAccessOwner(destination) && session.AcceptsTransfers(destination));
                    message = moved.Succeeded ? $"Moved {moved.Quantity} item to {session.StoreLabel(destination)}." : moved.Message;
                    selected = -1; Refresh(); return;
                }
                else return;
            }
            message = outcome.Succeeded ? id switch { "Craft" => "Crafted one empty battery.", "Refine" => "Arc Energy battery charged; canister and leftover dust retained.", _ => "Item ownership transferred." } : outcome.Message;
            selected = -1; Arm(); Refresh();
        }

        private void Refresh()
        {
            if (!open || suspended) return;
            if (!ActorAlive || !RegistrationValid || !TargetAccessible) { Close(); return; }
            var state = session.Owner(source); if (selected >= 0 && selectedRevision != state.Revision) selected = -1;
            var labels = new List<string>(state.Capacity);
            foreach (var slot in state.Slots) labels.Add(slot.IsEmpty ? "Empty" : slot.CanisterContents.IsPresent
                ? $"Canister {slot.CanisterContents.dust}/100" : state.ItemCatalog.GetRequiredDefinition(slot.ItemId).DisplayName + $" x{slot.Quantity}");
            var refinery = session.Refinery.CaptureState(); var isBench = target is TopDown3DCampWorkbench; var isRefinery = target is TopDown3DDustRefinery;
            var recipeText = session.Kit.ItemCatalog.GetRequiredDefinition(session.Workbench.Recipe.Output.ItemId).DisplayName;
            foreach (var ingredient in session.Workbench.Recipe.Ingredients)
            {
                var available = 0; foreach (var slot in session.Cache.State.Slots) if (slot.ItemId == ingredient.itemId) available += slot.Quantity;
                recipeText += $"\n{session.Kit.ItemCatalog.GetRequiredDefinition(ingredient.itemId).DisplayName}: {available}/{ingredient.quantity}";
            }
            var description = isBench ? recipeText + "\nOutput: Booter supply pocket"
                : isRefinery ? $"Canister: {(refinery.HasCanister ? refinery.dust + "/100 dust" : "empty socket")}\nBattery: {(refinery.HasBattery ? refinery.batteryItemId == TopDown3DDustRefineryState.ChargedBatteryId ? "charged" : "empty battery" : "empty socket")}\nCost: {session.Refinery.DustPerBattery} dust per battery"
                : target is TopDown3DResourceCache ? "Local camp storage\nResources stay with Legger.\nEquipment and batteries may transfer to legal destinations."
                : "Select the exact canister to deploy.\nLegger cargo and camp chest require physical access.";
            var accessible = session.CanAccessOwner(source);
            view.Present(isBench ? "CAMP WORKBENCH" : isRefinery ? "DUST REFINERY" : target == null ? "DEPLOY CANISTER" : "CAMP CHEST",
                description, (accessible ? message : "Source is out of reach; inspection only.") + "\n[" + session.Input.GetBindingDisplayName("UI/Cancel", "Back") + "] Back", labels, selected, accessible);
            view.SetLargeText(menu.LargeText);
            view.SetCommand("Source", true, true, "SOURCE: " + session.StoreLabel(source));
            view.SetCommand("Destination", true, true, "DESTINATION: " + session.StoreLabel(destination));
            view.SetCommand("Craft", isBench, isBench);
            view.SetCommand("Insert", isRefinery && selected >= 0 && accessible, isRefinery);
            view.SetCommand("Refine", isRefinery && session.Refinery.CanRefine, isRefinery);
            view.SetCommand("Battery", refinery.HasBattery && session.CanAccessOwner(destination), isRefinery);
            view.SetCommand("Canister", refinery.HasCanister && session.CanAccessOwner(destination), isRefinery);
            view.SetCommand("Deploy", selected >= 0 && accessible && state.Slots[selected].ItemId == TopDown3DHarvesterSettings.CanisterItemId, !isRefinery && !isBench);
            view.SetCommand("Transfer", selected >= 0 && accessible && source != destination && session.CanAccessOwner(destination), target is TopDown3DResourceCache);
            view.SetCommand("Reset", true, target != null, resetPending ? "CONFIRM CAMP ECONOMY RESET" : "RESET CAMP ECONOMY");
            PresentManagement();
        }
        private void OnDisable() => Teardown();
        private void OnDestroy() => Teardown();
        public void Teardown()
        {
            if (subscribed) UnbindManagement();
            Close(false); if (!subscribed) return;
            session.Action.AccessibleCargoCanister = null;
            if (menu != null) menu.ConfigureCampWorkstations(null);
            if (inventoryUi != null) inventoryUi.ConfigureCampWorkstations(null);
            session.Action.CampStationAccessRequested -= OpenTarget; session.Action.CanisterSelectionRequested -= OpenDeployment;
            session.Input.UiCancelRequested -= Cancel; session.Input.ModeChanged -= ModeChanged;
            foreach (var state in observedStates) state.Changed -= Refresh;
            observedStates.Clear();
            session.Refinery.Changed -= Refresh; subscribed = false;
        }
    }
}
