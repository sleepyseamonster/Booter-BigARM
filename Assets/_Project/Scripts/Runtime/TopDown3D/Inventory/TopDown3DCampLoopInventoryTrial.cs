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
        public Func<string> ExternalResetBlockReason { get; set; }

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
            captured = baseline != null; Changed?.Invoke();
        }
        public bool CancelConfirmation()
        { if (!ConfirmationPending) return false; ConfirmationPending = false; Changed?.Invoke(); return true; }
        public string RequestReset()
        {
            var externalReason = ExternalResetBlockReason?.Invoke();
            if (!string.IsNullOrEmpty(externalReason)) { CancelConfirmation(); return externalReason; }
            if (!Ready) return FailureReason ?? "Inventory trial is not ready.";
            var input = kit.GetComponent<TopDown3DPlayerMotor>()?.InputRouter;
            if (input == null || input.Mode != TopDown3DInputMode.Inventory) return "Open inventory to reset the trial.";
            if (!ConfirmationPending)
            {
                ConfirmationPending = true; Changed?.Invoke();
                return "Confirm reset: equipment, supplies, cargo, cache, harvests and algae return to the starting trial. Actors and vitals stay unchanged. Back cancels.";
            }
            ConfirmationPending = false;
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
                if (world.Exists(mutation => mutation == null)) return "World state cannot be restored; reset cancelled without changes.";
                var result = TopDown3DInventoryTransaction.Commit(mutations,
                    () => kit.UsesLoadout && input.Mode == TopDown3DInputMode.Inventory && lab.PreviewReady, world);
                return result.Succeeded ? "Inventory trial restored. Actors and vitals were preserved." : result.Message;
            }
            finally { resetting = false; Changed?.Invoke(); }
        }
        private void OnDestroy() { if (lab != null) lab.ReadinessChanged -= Initialize; }
    }
}
