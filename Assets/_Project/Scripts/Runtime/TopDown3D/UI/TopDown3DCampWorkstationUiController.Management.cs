using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DCampWorkstationUiController
    {
        private enum ManagementView { Detail, Items, Stations, Companions }
        private ManagementView management;
        private TopDown3DProcessingService.Quote processQuote;
        private string resetStamp;
        private Guid registryToken;
        private uint registryGeneration;
        private float statusAt;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DInventoryState observedTray;
        private ITopDown3DCampStation inspectedStation;
        private bool ActorAlive => session.Kit != null && session.Kit.isActiveAndEnabled
            && session.Kit.GetComponent<TopDown3DPlayerMotor>()?.isActiveAndEnabled == true
            && !session.Kit.GetComponent<TopDown3DPlayerMotor>().IsMounted && vitals != null && vitals.Health > 0;
        private bool RegistrationValid => registryToken == Guid.Empty ? !session.ManagementReady : session.ManagementReady && session.Site.Registry?.Current == true
            && session.Site.Registry.SessionToken == registryToken && session.Site.Registry.Generation == registryGeneration;
        private void BindManagement()
        {
            vitals = session.Kit.GetComponent<TopDown3DSurvivalVitals>();
            if (vitals != null) vitals.HealthReduced += Interrupt;
            observedTray = session.Site != null ? session.Site.Processor.Output.State : null;
            if (observedTray != null) observedTray.Changed += Refresh;
            InputSystem.onDeviceChange += DeviceChanged;
        }
        private void UnbindManagement()
        {
            if (!ReferenceEquals(vitals, null)) vitals.HealthReduced -= Interrupt;
            if (observedTray != null) observedTray.Changed -= Refresh;
            observedTray = null;
            InputSystem.onDeviceChange -= DeviceChanged;
        }
        private void Interrupt() => Close();
        private void OnApplicationFocus(bool focused) { if (!focused) Interrupt(); }
        private void DeviceChanged(InputDevice device, InputDeviceChange change)
        { if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected) Interrupt(); }
        private void StartManagement()
        {
            management = ManagementView.Detail; processQuote = null; inspectedStation = null;
            registryToken = session.ManagementReady ? session.Site.Registry.SessionToken : Guid.Empty;
            registryGeneration = session.ManagementReady ? session.Site.Registry.Generation : 0;
        }
        private bool ProcessPreviewFits => session.Site != null && session.Site.Processor.Recipe.Inputs.Count == 1 && session.Site.Processor.Recipe.Outputs.Count <= 4;
        private string ProcessDescription()
        {
            if (!ProcessPreviewFits) return "Recipe exceeds the trial preview capacity.\nProcessing is blocked until its interface is extended.";
            var recipe = session.Site.Processor.Recipe;
            var text = "Manual sorting · no power";
            foreach (var input in recipe.Inputs) text += "\n" + input.quantity + " " + session.Kit.ItemCatalog.GetRequiredDefinition(input.itemId).DisplayName + " →";
            foreach (var output in recipe.Outputs) text += "\n" + output.quantity + " " + session.Kit.ItemCatalog.GetRequiredDefinition(output.itemId).DisplayName;
            return text + "\nMineral dust is not Arc Dust.";
        }
        private string StationDescription(ITopDown3DCampStation station)
        {
            if (station == null) return "Three local stations.\nSelect any station to inspect its status.\nActions require physical access.";
            if (station.Target.UnityObject == null || !station.Target.IsAvailable) return station.Label + "\nUnavailable; retained stock stays owned.";
            var text = station.Label + (station.CanAccess(session.Kit.transform) ? " · within reach" : " · inspection only");
            if (station.Target is TopDown3DCampWorkbench bench)
            {
                if (bench.HasRecipeRoster) return text + "\n" + FirstToolBenchSummary();
                foreach (var ingredient in bench.Recipe.Ingredients)
                {
                    var count = 0; foreach (var slot in bench.Chest.State.Slots) if (slot.ItemId == ingredient.itemId) count += slot.Quantity;
                    text += "\n" + session.Kit.ItemCatalog.GetRequiredDefinition(ingredient.itemId).DisplayName + ": " + count + "/" + ingredient.quantity;
                }
                text += "\nOutput: " + session.Kit.ItemCatalog.GetRequiredDefinition(bench.Recipe.Output.ItemId).DisplayName;
                text += "\n" + (session.Kit.State.CheckAdd(bench.Recipe.Output).Succeeded ? "Booter output pocket available" : "Booter output pocket blocked");
            }
            else if (station.Target is TopDown3DDustRefinery refinery)
            {
                var state = refinery.CaptureState();
                text += "\nCanister: " + (state.HasCanister ? state.dust + "/100 Arc Dust" : "empty socket")
                    + "\nBattery: " + (state.HasBattery ? state.batteryItemId == TopDown3DDustRefineryState.ChargedBatteryId ? "charged" : "empty battery" : "empty socket")
                    + "\n" + (refinery.CanRefine ? "Ready to charge" : "Insert canister and empty battery; need " + refinery.DustPerBattery + " Arc Dust");
            }
            else if (station.Target is TopDown3DIronstoneProcessor processor)
                text += "\nOutput tray: " + processor.Output.State.OccupiedSlotCount + "/8 slots occupied\nManual dry sorting; no power.\nChoose one local cargo/chest source.\nA complete batch must fit.";
            return text;
        }
        private bool ManagementCommand(string id)
        {
            if (id.StartsWith("Overview", StringComparison.Ordinal))
            {
                if (target == null || !session.ManagementReady) return true;
                management = id == "OverviewStations" ? ManagementView.Stations : id == "OverviewCompanions" ? ManagementView.Companions : ManagementView.Items;
                processQuote = null; resetPending = false; selected = -1; inspectedStation = null; Refresh(); view.SelectFirst(events); return true;
            }
            if (id.StartsWith("Station", StringComparison.Ordinal))
            {
                if (session.Site == null || !int.TryParse(id.Substring(7), out var index) || index < 0 || index >= session.Site.Registry.Stations.Count) return true;
                var station = session.Site.Registry.Stations[index];
                inspectedStation = station;
                if (!station.CanAccess(session.Kit.transform)) { message = station.Label + " is out of reach. Approach it physically."; Refresh(); return true; }
                target = station.Target; management = ManagementView.Detail; source = target is TopDown3DIronstoneProcessor ? 2 : 0;
                PrepareFirstToolView();
                processQuote = null; resetPending = false; selected = -1; Arm(); Refresh(); view.SelectFirst(events); return true;
            }
            if (id == "Call")
            {
                if (management != ManagementView.Companions || !TopDown3DCampCompanionStatus.CanCall(session)) return true;
                Close();
                if (session.Input.Mode == TopDown3DInputMode.Gameplay && !session.Action.IsBusy && TopDown3DCampCompanionStatus.CanCall(session))
                { session.Cargo.GetComponent<TopDown3DBigArmFollower>().RequestCallOver(); session.Action.ReportFeedback("Legger call requested. He must reach you physically."); }
                return true;
            }
            if (id == "Sort")
            {
                if (management != ManagementView.Detail || target is not TopDown3DIronstoneProcessor processor || source == 0
                    || session.Owner(source) == processor.Output.State || !session.CanAccessOwner(source)) return true;
                if (!ProcessPreviewFits) { processQuote = null; message = "Recipe cannot be previewed completely; processing is blocked."; Refresh(); return true; }
                if (processQuote == null)
                {
                    var registry = session.Site.Registry; var generation = registry.Generation; var epoch = session.Epoch;
                    var selectedSource = session.Owner(source); var sourceIndex = source;
                    var result = TopDown3DProcessingService.Prepare(processor, selectedSource,
                        () => CanCommand && session.Site.Registry == registry && registry.Active && registry.Generation == generation
                            && session.Epoch == epoch && ReferenceEquals(session.Owner(sourceIndex), selectedSource)
                            && session.CanAccessOwner(sourceIndex) && processor.CanAccess(session.Kit.transform), out processQuote);
                    message = result.Succeeded ? "Confirm one batch from " + session.StoreLabel(source) + ". Consume the listed input and retain every listed output. All results must fit the eight-slot tray." : result.Message;
                }
                else
                {
                    var outcome = processQuote.Commit(); processQuote = null;
                    message = outcome.Succeeded ? "One complete Ironstone batch sorted. Retrieve the tray products." : outcome.Message;
                }
                Arm(); Refresh(); return true;
            }
            if (id == "Take" || id == "ManageTransfer")
            {
                if (id == "Take" && (management != ManagementView.Detail || target is not TopDown3DIronstoneProcessor || source != 3)
                    || id == "ManageTransfer" && (management != ManagementView.Items || !session.ManagementReady)) return true;
                if (selected < 0 || !session.CanAccessOwner(source) || !session.CanAccessOwner(destination) || !session.AcceptsTransfers(destination)) return true;
                var moved = TopDown3DInventoryTransferService.TransferExactSlot(session.Owner(source), session.Owner(destination), selected,
                    selectedRevision, session.Owner(destination).Revision,
                    () => CanCommand && session.CanAccessOwner(source) && session.CanAccessOwner(destination) && session.AcceptsTransfers(destination));
                message = moved.Succeeded ? "Moved the entire selected stack to " + session.StoreLabel(destination) + "." : moved.Message;
                selected = -1; processQuote = null; Arm(); Refresh(); return true;
            }
            return false;
        }
        private void PresentManagement()
        {
            var hasSite = session.ManagementReady && target != null;
            var overview = hasSite && management != ManagementView.Detail;
            var isProcessor = target is TopDown3DIronstoneProcessor;
            foreach (var id in new[] { "OverviewItems", "OverviewStations", "OverviewCompanions" })
                view.SetCommand(id, hasSite, hasSite);
            for (var i = 0; i < 3; i++)
            {
                var station = hasSite && i < session.Site.Registry.Stations.Count ? session.Site.Registry.Stations[i] : null;
                view.SetCommand("Station" + i, station != null, overview && management == ManagementView.Stations,
                    station?.Label + (station != null && !station.CanAccess(session.Kit.transform) ? " · OUT OF REACH" : ""));
            }
            view.SetCommand("Call", hasSite && TopDown3DCampCompanionStatus.CanCall(session), overview && management == ManagementView.Companions, "CALL LEGGER TO BOOTER");
            view.SetCommand("Sort", isProcessor && ProcessPreviewFits && source != 0 && session.CanAccessOwner(source) && session.Owner(source) != session.Site.Processor.Output.State,
                isProcessor && !overview, processQuote == null ? "PREPARE ONE SORTING BATCH" : "CONFIRM SORTING BATCH");
            view.SetCommand("Take", selected >= 0 && source != destination && session.CanAccessOwner(source) && session.CanAccessOwner(destination),
                isProcessor && !overview && source == 3, "TAKE ENTIRE SELECTED STACK");
            view.SetCommand("ManageTransfer", selected >= 0 && source != destination && session.CanAccessOwner(source) && session.CanAccessOwner(destination),
                overview && management == ManagementView.Items, "MOVE ENTIRE SELECTED STACK");
            if (overview || isProcessor)
            {
                foreach (var id in new[] { "Craft", "Insert", "Refine", "Battery", "Canister", "Deploy", "Transfer" }) view.SetCommand(id, false, false);
                var labels = new List<string>();
                if (!overview || management == ManagementView.Items)
                    foreach (var slot in session.Owner(source).Slots) labels.Add(slot.IsEmpty ? "Empty" : slot.CanisterContents.IsPresent
                        ? "Canister " + slot.CanisterContents.dust + "/100" : session.Kit.ItemCatalog.GetRequiredDefinition(slot.ItemId).DisplayName + " x" + slot.Quantity);
                var detail = management == ManagementView.Companions ? TopDown3DCampCompanionStatus.Describe(session)
                    : management == ManagementView.Stations ? StationDescription(inspectedStation)
                    : overview ? "One physical store at a time.\n" + session.StoreLabel(source) + ": " + session.Owner(source).CalculateMass().ToString("0.##") + " mass\n"
                        + (session.CanAccessOwner(source) ? "Within reach" : "Inspection only") + "\nOutput tray accepts no deposits."
                    : ProcessDescription();
                if (overview && management == ManagementView.Items && selected >= 0 && selected < session.Owner(source).Capacity)
                { var item = session.Owner(source).Slots[selected]; detail += "\n" + item.ItemId + (string.IsNullOrEmpty(item.InstanceId) ? "" : "\n" + item.InstanceId); }
                view.Present(overview ? "STARTING CAMP · " + management.ToString().ToUpperInvariant() : "IRONSTONE SORTER · 8-SLOT OUTPUT", detail,
                    message + "\n[" + session.Input.GetBindingDisplayName("UI/Cancel", "Back") + "] Back", labels, selected, session.CanAccessOwner(source));
                var showStores = !overview || management == ManagementView.Items;
                view.SetCommand("Source", true, showStores, "SOURCE: " + session.StoreLabel(source));
                view.SetCommand("Destination", true, showStores, "DESTINATION: " + session.StoreLabel(destination));
            }
            view.ArrangeCommands();
            if (events.currentSelectedGameObject == null || !events.currentSelectedGameObject.activeInHierarchy) view.SelectFirst(events);
        }
    }
}
