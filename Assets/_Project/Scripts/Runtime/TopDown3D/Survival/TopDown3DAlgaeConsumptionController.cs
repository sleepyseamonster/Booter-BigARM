using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent, DefaultExecutionOrder(600)]
    public sealed class TopDown3DAlgaeConsumptionController : MonoBehaviour
    {
        private TopDown3DInputRouter input;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DAlgaeCanisterState state;
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerActionController actions;
        private TopDown3DAlgaeUse? pending;
        private bool conflict, committing;
        private double nextUseTime;
        public TopDown3DAlgaeCanisterState State => state;
        public TopDown3DSurvivalVitals Vitals => vitals;
        public TopDown3DInputRouter Input => input;
        public event Action<TopDown3DAlgaeUse, double, double> Consumed;
        public event Action<string> Rejected;

        public void Configure(TopDown3DInputRouter router, TopDown3DSurvivalVitals survival, TopDown3DAlgaeCanisterState canister)
        {
            Unsubscribe(); ClearPending(); input = router; vitals = survival; state = canister;
            motor = GetComponent<TopDown3DPlayerMotor>(); actions = GetComponent<TopDown3DPlayerActionController>();
            Subscribe();
        }
        public string UnavailableReason(TopDown3DAlgaeUse use)
        {
            if(CampExpeditionFeature.Enabled(gameObject.scene) && (CampExpeditionSession.Find(gameObject.scene)?.HasMountedPhagia!=true || !CampExpeditionFeature.FieldAllowed(gameObject.scene)))return "Mount the Phagia canister on the Utility harness first";
            if (state == null || vitals == null || input == null || !input.HasAlgaeActions) return "Canister unavailable";
            var inventory = GetComponent<TopDown3DPlayerInventory>();
            if (inventory != null && inventory.UsesLoadout && inventory.State.IsCommitting) return "Inventory unavailable";
            if (inventory != null && inventory.UsesLoadout && inventory.State.Slots[1].IsEmpty) return "Algae canister missing";
            if (input.Mode != TopDown3DInputMode.Gameplay) return "Unavailable in menus or camp";
            if (vitals.Health <= 0) return "Cannot treat after death";
            if (vitals.Activity != TopDown3DSurvivalActivity.Ordinary || motor != null && (motor.IsActionConstrained
                || motor.ClimbMode != TopDown3DClimbMode.None || motor.ActiveTraversal != TopDown3DTraversalMove.None)
                || actions != null && actions.IsBusy) return "Finish current action";
            if (state.Remaining <= 0) return "Canister empty";
            var vital = use == TopDown3DAlgaeUse.Drink ? TopDown3DSurvivalVital.Thirst
                : use == TopDown3DAlgaeUse.Eat ? TopDown3DSurvivalVital.Hunger : TopDown3DSurvivalVital.Health;
            if (vitals.GetPreciseValue(vital) >= vitals.GetMaximum(vital))
                return use == TopDown3DAlgaeUse.Drink ? "Already hydrated" : use == TopDown3DAlgaeUse.Eat ? "Already nourished" : "Health full";
            return string.Empty;
        }
        internal bool TryUse(TopDown3DAlgaeUse use, double unscaledNow)
        {
            if (committing) return false;
            if (Application.isPlaying && !Application.isFocused) { ClearPending(); return false; }
            committing = true;
            try
            {
                if (Application.isPlaying) vitals?.SynchronizeRuntimeClock();
                var reason = UnavailableReason(use);
                if (reason.Length > 0) { Rejected?.Invoke(reason); return false; }
                if (unscaledNow < nextUseTime) return false;
                if (!state.TryConsume(vitals, use, out var gain, out var cost)) { Rejected?.Invoke("No useful dose available"); return false; }
                nextUseTime = unscaledNow + state.Settings.cooldownSeconds;
                Consumed?.Invoke(use, gain, cost); return true;
            }
            finally { committing = false; }
        }
        private void Queue(TopDown3DAlgaeUse use) { if (pending.HasValue) conflict = true; pending = use; }
        private void Update()
        {
            var request = pending; var ambiguous = conflict; ClearPending();
            if (request.HasValue && !ambiguous) TryUse(request.Value, Time.unscaledTimeAsDouble);
        }
        private void ModeChanged(TopDown3DInputMode mode) { ClearPending(); }
        private void ClearPending() { pending = null; conflict = false; }
        private void ResetTransient() { ClearPending(); nextUseTime = 0; }
        private void OnApplicationFocus(bool focused) { if (!focused) ClearPending(); }
        private void OnEnable() { ClearPending(); Subscribe(); }
        private void OnDisable() { Unsubscribe(); ClearPending(); }
        private void Subscribe()
        {
            Unsubscribe();
            if (input != null) { input.AlgaeUseRequested += Queue; input.ModeChanged += ModeChanged; }
            if (vitals != null) vitals.ResetPerformed += ResetTransient;
            if (state != null) state.Restored += ResetTransient;
        }
        private void Unsubscribe()
        {
            if (input != null) { input.AlgaeUseRequested -= Queue; input.ModeChanged -= ModeChanged; }
            if (vitals != null) vitals.ResetPerformed -= ResetTransient;
            if (state != null) state.Restored -= ResetTransient;
        }
        public static TopDown3DAlgaeConsumptionController TryInstallForScene(Scene scene)
        {
            var player = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerMotor>(scene);
            if (player == null) return null;
            var settings = TopDown3DAlgaeCanisterSettings.Load();
            if (settings == null || !settings.IsValid) { Debug.LogError("Valid algae canister settings are required"); return null; }
            var vitals = player.GetComponent<TopDown3DSurvivalVitals>();
            var router = player.InputRouter;
            if (vitals == null || router == null) return null;
            var state = player.GetComponent<TopDown3DAlgaeCanisterState>() ?? player.gameObject.AddComponent<TopDown3DAlgaeCanisterState>();
            state.Configure(settings);
            var controller = player.GetComponent<TopDown3DAlgaeConsumptionController>() ?? player.gameObject.AddComponent<TopDown3DAlgaeConsumptionController>();
            controller.Configure(router, vitals, state);
            return controller;
        }
    }
}
