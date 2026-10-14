using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Explicit inventory trial reset. Animation reset never replenishes this baseline.</summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DCampLoopInventoryTrial : MonoBehaviour
    {
        [Serializable] private sealed class Baseline
        {
            public TopDown3DInventorySnapshot kit, cargo, cache;
            public TopDown3DResourceWorldSnapshot resources;
            public TopDown3DPlacedHarvesterSnapshot devices;
            public TopDown3DAlgaeCanisterSnapshot algae;
            public int salvageMask;
            public int stationBaselineVersion;
            public string stationId;
            public TopDown3DInventorySnapshot stationTray;
        }
        [SerializeField, HideInInspector] private string baselineJson;
        private Baseline baseline;
        private TopDown3DPlayerInventory kit;
        private TopDown3DBigArmCargo cargo;
        private TopDown3DResourceCache cache;
        private TopDown3DResourceWorldState resources;
        private TopDown3DPlacedHarvesterState devices;
        private TopDown3DAlgaeCanisterState algae;
        private TopDown3DCampController camp;
        private TopDown3DSalvageWreckState wreck;
        private TopDown3DAnimationPlaygroundSession lab;
        private bool resetting, captured;
        public bool ConfirmationPending { get; private set; }
        public bool Ready => captured && baseline != null && !resetting;
        public string FailureReason { get; private set; }
        public event Action Changed;
        // A later station owner must install this before enabling socket commands.
        // It prevents restoring starter equipment while that equipment is owned by an external socket.
        private readonly Dictionary<string, TopDown3DCampResetParticipant> participants = new(StringComparer.Ordinal);
        private string confirmationStamp;
        internal TopDown3DInventorySnapshot StationBaseline => baseline?.stationTray;
        internal bool PreflightCampBaseline(TopDown3DCampLoopEconomySession economy)
        {
            if (string.IsNullOrEmpty(baselineJson)) return true;
            try
            {
                var saved = JsonUtility.FromJson<Baseline>(baselineJson);
                if (saved == null || saved.kit == null || saved.cargo == null || saved.cache == null || saved.resources == null || saved.devices == null || saved.algae == null)
                    return BaselineFailure("Retained trial baseline is incomplete; no grants or reset were applied.");
                var states = new[] { economy.Kit.State, economy.Cargo.State, economy.Cache.State };
                var snapshots = new[] { saved.kit, saved.cargo, saved.cache };
                for (var i = 0; i < states.Length; i++)
                    if (!new TopDown3DInventoryState(states[i].ItemCatalog, states[i].Policy).ApplySnapshot(snapshots[i]).Succeeded)
                        return BaselineFailure("Retained trial inventory baseline is incompatible.");
                var resourceOwner = TopDown3DGameHudCanvas.FindInScene<TopDown3DResourceWorldState>(gameObject.scene);
                var algaeOwner = economy.Kit.GetComponent<TopDown3DAlgaeCanisterState>();
                if (resourceOwner == null || resourceOwner.PrepareTrialRestore(saved.resources) == null || economy.Devices.PrepareTrialRestore(saved.devices) == null
                    || algaeOwner == null || algaeOwner.PrepareTrialRestore(saved.algae) == null)
                    return BaselineFailure("Retained trial world baseline is incompatible.");
                if (economy.Site != null)
                {
                    var tray = economy.Site.Processor.Output;
                    if (!tray.Ready) return BaselineFailure("Retained station requires recovery before trial reset.");
                    if (saved.stationBaselineVersion == 0 && saved.stationTray == null && tray.WasFresh) return true;
                    if (saved.stationBaselineVersion != 1 || saved.stationId != tray.StableId || saved.stationTray == null)
                        return BaselineFailure("Retained station baseline is incomplete.");
                    var draft = new TopDown3DInventoryState(tray.State.ItemCatalog, tray.State.Policy);
                    if (!draft.ApplySnapshot(saved.stationTray).Succeeded || draft.OccupiedSlotCount != 0)
                        return BaselineFailure("Retained station initial baseline must be empty.");
                }
                return true;
            }
            catch (Exception) { return BaselineFailure("Retained trial baseline cannot be decoded; its bytes were preserved."); }
        }
        private bool BaselineFailure(string reason) { captured = false; FailureReason = reason; return false; }
        internal void RegisterResetParticipant(string id, Func<string> block, Func<TopDown3DCampResetPreparation> prepare = null, Func<string> stamp = null)
        {
            if (string.IsNullOrWhiteSpace(id) || block == null) throw new ArgumentException("Reset participant identity and blocker are required.");
            participants[id] = new TopDown3DCampResetParticipant { Id = id, Block = block, Prepare = prepare, Stamp = stamp };
        }
        private string BlockReason()
        { foreach (var participant in participants.Values) { var reason = participant.Block(); if (!string.IsNullOrEmpty(reason)) return reason; } return null; }
        private string ResetStamp()
        {
            var value = kit.State.Revision + ":" + cargo.State.Revision + ":" + cache.State.Revision
                + ":" + JsonUtility.ToJson(resources.CaptureSnapshot()) + ":" + JsonUtility.ToJson(algae.CaptureSnapshot())
                + ":" + (wreck != null ? wreck.RemovedMask : 0) + ":" + devices.DeployedCount + ":" + devices.CaptureSnapshot().NextSequence;
            foreach (var participant in participants.Values) value += "|" + participant.Id + ":" + participant.Stamp?.Invoke();
            return value;
        }
        internal bool CompleteCampBaseline(TopDown3DCampLoopEconomySession economy)
        {
            Initialize(); if (baseline == null) return false;
            if (economy.Site == null) return true;
            var tray = economy.Site.Processor.Output;
            if (baseline.stationBaselineVersion == 0 && baseline.stationTray == null && tray.WasFresh)
            { baseline.stationBaselineVersion = 1; baseline.stationId = tray.StableId; baseline.stationTray = tray.EmptyBaseline(); }
            if (baseline.stationBaselineVersion != 1 || baseline.stationId != tray.StableId || baseline.stationTray == null
                || baseline.stationTray.Slots == null || baseline.stationTray.Slots.Count != 8)
            { captured = false; FailureReason = "Retained station baseline requires recovery; data was preserved."; return false; }
            var prepared = new TopDown3DInventoryState(tray.State.ItemCatalog, tray.State.Policy);
            if (!prepared.ApplySnapshot(baseline.stationTray).Succeeded || prepared.OccupiedSlotCount != 0)
            { captured = false; FailureReason = "Station initial baseline must be valid and empty."; return false; }
            baselineJson = JsonUtility.ToJson(baseline); captured = true; return true;
        }
        private TopDown3DInventorySnapshot[] economyBaseline;
        internal void AdoptEconomyBaseline(TopDown3DInventorySnapshot player, TopDown3DInventorySnapshot carrier, TopDown3DInventorySnapshot storage)
        {
            economyBaseline = new[] { player, carrier, storage };
            if (baseline != null) ApplyEconomyBaseline();
        }
        private void ApplyEconomyBaseline()
        {
            if (economyBaseline == null || baseline == null) return;
            baseline.kit = economyBaseline[0]; baseline.cargo = economyBaseline[1]; baseline.cache = economyBaseline[2];
            baselineJson = JsonUtility.ToJson(baseline); economyBaseline = null;
        }

        public void Configure(TopDown3DPlayerInventory player, TopDown3DBigArmCargo carrier)
        {
            if (kit != null) return;
            kit = player; cargo = carrier; lab = TopDown3DAnimationPlaygroundSession.Find(gameObject.scene);
            if (lab == null || !kit.UsesLoadout) return;
            TopDown3DAlgaeConsumptionController.TryInstallForScene(gameObject.scene);
            lab.ReadinessChanged += Initialize;
            Initialize();
        }
        private void Initialize()
        {
            if (captured || lab == null || !lab.PreviewReady) return;
            var economy = TopDown3DGameHudCanvas.FindInScene<TopDown3DCampLoopEconomySession>(gameObject.scene);
            if (economy != null && economy.Site != null && !economy.Ready) return;
            cache = TopDown3DGameHudCanvas.FindInScene<TopDown3DResourceCache>(gameObject.scene);
            resources = TopDown3DGameHudCanvas.FindInScene<TopDown3DResourceWorldState>(gameObject.scene);
            devices = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlacedHarvesterState>(gameObject.scene);
            wreck = TopDown3DGameHudCanvas.FindInScene<TopDown3DSalvageWreckState>(gameObject.scene);
            algae = kit.GetComponent<TopDown3DAlgaeCanisterState>(); camp = kit.GetComponent<TopDown3DCampController>();
            if (cargo == null || cache == null || resources == null || devices == null || devices.Settings == null || !devices.IsAuthored
                || algae == null || camp == null || !camp.IsConfigured)
            { FailureReason = "Inventory trial dependencies are not ready."; Changed?.Invoke(); return; }
            if (kit.State.IsCommitting || cargo.State.IsCommitting || cache.State.IsCommitting)
            { FailureReason = "Retained inventory requires recovery; a reset baseline was not granted."; Changed?.Invoke(); return; }
            if (!string.IsNullOrEmpty(baselineJson))
            {
                try { baseline = JsonUtility.FromJson<Baseline>(baselineJson); }
                catch (ArgumentException) { FailureReason = "Retained trial baseline is invalid; contents were preserved."; return; }
            }
            else
            {
                if (camp.Record != null || devices.DeployedCount > 0)
                { FailureReason = "A fresh packed trial baseline is required; current equipment was preserved."; return; }
                baseline = new Baseline { kit = kit.CaptureSnapshot(), cargo = cargo.State.CaptureSnapshot(), cache = cache.State.CaptureSnapshot(),
                    resources = resources.CaptureSnapshot(), devices = devices.CaptureSnapshot(), algae = algae.CaptureSnapshot(), salvageMask = wreck != null ? wreck.RemovedMask : 0 };
                baselineJson = JsonUtility.ToJson(baseline);
            }
            ApplyEconomyBaseline(); captured = baseline != null; Changed?.Invoke();
        }
        public bool CancelConfirmation()
        { if (!ConfirmationPending) return false; ConfirmationPending = false; Changed?.Invoke(); return true; }
        public string RequestReset()
        {
            var actorVitals = kit != null ? kit.GetComponent<TopDown3DSurvivalVitals>() : null;
            if (kit == null || !kit.isActiveAndEnabled || actorVitals == null || actorVitals.Health <= 0)
            { CancelConfirmation(); return "A live Booter is required to reset the trial."; }
            var externalReason = BlockReason();
            if (!string.IsNullOrEmpty(externalReason)) { CancelConfirmation(); return externalReason; }
            if (!Ready) return FailureReason ?? "Inventory trial is not ready.";
            var input = kit.GetComponent<TopDown3DPlayerMotor>()?.InputRouter;
            if (input == null || input.Mode != TopDown3DInputMode.Inventory) return "Open inventory to reset the trial.";
            if (!ConfirmationPending)
            {
                ConfirmationPending = true; confirmationStamp = ResetStamp(); Changed?.Invoke();
                return "Confirm reset: equipment, supplies, cargo, cache, sorter output, harvests and algae return to the starting trial. Actors and vitals stay unchanged. Back cancels.";
            }
            ConfirmationPending = false;
            if (confirmationStamp != ResetStamp()) return "Trial contents changed. Request and confirm reset again.";
            if (kit.GetComponent<TopDown3DPlayerActionController>()?.IsBusy == true || camp.Phase != TopDown3DCampPhase.Idle)
                return "Finish the current action before resetting.";
            resetting = true;
            try
            {
                var mutations = new List<TopDown3DInventoryTransaction.Mutation>();
                var states = new[] { kit.State, cargo.State, cache.State };
                var snapshots = new[] { baseline.kit, baseline.cargo, baseline.cache };
                for (var i = 0; i < states.Length; i++)
                {
                    var mutation = new TopDown3DInventoryTransaction.Mutation(states[i]);
                    if (!mutation.Draft.ApplySnapshot(snapshots[i]).Succeeded) return "Trial baseline is incompatible; reset cancelled without changes.";
                    mutations.Add(mutation);
                }
                var world = new List<TopDown3DPreparedWorldMutation> { resources.PrepareTrialRestore(baseline.resources),
                    devices.PrepareTrialRestore(baseline.devices), algae.PrepareTrialRestore(baseline.algae), camp.PrepareTrialRestore(null, false) };
                if (wreck != null) world.Add(wreck.PrepareTrialRestore(baseline.salvageMask));
                foreach (var participant in participants.Values)
                {
                    if (participant.Prepare == null) continue;
                    var prepared = participant.Prepare();
                    if (prepared == null || !prepared.IsValid) return "Reset participant requires recovery; nothing changed.";
                    mutations.AddRange(prepared.Inventories); world.AddRange(prepared.World);
                }
                var aggregate = new TopDown3DCampResetPreparation(); aggregate.Inventories.AddRange(mutations); aggregate.World.AddRange(world);
                if (!aggregate.IsValid) return "Duplicate or unavailable reset owners; nothing changed.";
                if (world.Exists(mutation => mutation == null)) return "World state cannot be restored; reset cancelled without changes.";
                var result = TopDown3DInventoryTransaction.Commit(mutations,
                    () => kit.UsesLoadout && input.Mode == TopDown3DInputMode.Inventory && lab.PreviewReady
                        && string.IsNullOrEmpty(BlockReason()), world);
                return result.Succeeded ? "Inventory trial restored. Actors and vitals were preserved." : result.Message;
            }
            finally { resetting = false; Changed?.Invoke(); }
        }
        private void OnDestroy() { if (lab != null) lab.ReadinessChanged -= Initialize; }
    }
}
