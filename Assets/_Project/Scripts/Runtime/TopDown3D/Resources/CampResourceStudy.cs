using System;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class CampResourceStudy : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        public const string ResetKey = "CampLoop.Resources.FiveSources";
        [SerializeField] private CampResourceLedger ledger = new();
        [SerializeField] private CampResourceSource[] sources;
        [SerializeField] private CampHuskBeetle beetle;
        [SerializeField] private AnimationClip attackClip;
        private TopDown3DAnimationPlaygroundSession lab;
        private TopDown3DCampLoopInventoryTrial trial;
        private TopDown3DPlayerActionController action;
        private TopDown3DPlayerMotor player;
        private TopDown3DPlayerAnimationDriver presentation;
        private TopDown3DInputRouter input;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DItemCatalog catalog;
        private bool bound, resetLocked;
        private readonly CampResourceStrike strike = new();
        public CampResourceLedger Ledger => ledger;
        public TopDown3DItemCatalog ItemCatalog => catalog;
        public TopDown3DPlayerMotor Player => player;
        public TopDown3DSurvivalVitals Vitals => vitals;
        public string Failure { get; private set; }
        public bool Ready => bound && isActiveAndEnabled && lab != null && lab.isActiveAndEnabled && lab.PreviewReady
            && ledger.Valid && !resetLocked && trial != null && trial.Ready;
        public bool SimulationAllowed => Ready && input.Mode == TopDown3DInputMode.Gameplay && !input.LaboratoryResetBlocked
            && lab.ResetRegistry.LastFailure == null && Time.timeScale > 0 && Application.isFocused && vitals.Health > 0;
        public string ResetId => ResetKey;
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.HeldAndSpawnedProps;
        public void Configure(CampResourceSource[] points, CampHuskBeetle actor, AnimationClip clip)
        { sources = points; beetle = actor; attackClip = clip; }
        private T One<T>() where T : Component
        { var all = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray(); return all.Length == 1 ? all[0] : null; }
        private void Update()
        {
            if (bound) return;
            lab = One<TopDown3DAnimationPlaygroundSession>();
            if (lab == null || !lab.IsLab || !lab.PreviewReady) return;
            player = lab.Player; if (player == null) return;
            input = player.InputRouter; action = player.GetComponent<TopDown3DPlayerActionController>();
            presentation = player.GetComponent<TopDown3DPlayerAnimationDriver>(); vitals = player.GetComponent<TopDown3DSurvivalVitals>();
            trial = player.GetComponent<TopDown3DCampLoopInventoryTrial>();
            if (trial == null || !trial.Ready) return;
            catalog = player.GetComponent<TopDown3DPlayerInventory>().ItemCatalog;
            if (!ledger.Valid || sources == null || sources.Length != 5 || sources.Any(s => s == null)
                || sources.Select(s => s.Index).Distinct().Count() != 5 || beetle == null || attackClip == null
                || action == null || presentation == null || input == null || vitals == null || lab.ResourceAttackRequested != null
                || CampResourceLedger.Items.Any(id => !catalog.TryGetDefinition(id, out _)))
            { Failure = "Resource study bindings or retained state require recovery; inventory preserved."; return; }
            lab.ResetRegistry.Register(this);
            trial.RegisterResetParticipant(ResetKey, () => !bound || !ledger.Valid || !isActiveAndEnabled ? "Resource study is unavailable." : null, PrepareReset,
                () => ledger.Revision + ":" + ledger.Epoch, "Resource sources and the husk beetle also return to their starting trial.");
            lab.ResourceAttackRequested = BeginAttack;
            lab.ResourceCombatBusy = CombatBusy;
            input.AnimationCancelRequested += CancelAttack;
            input.ModeChanged += ModeChanged;
            lab.ReadinessChanged += ReadinessChanged;
            bound = true; Failure = null;
        }
        public bool TryConsume(int index) => Ready && ledger.Consume(index);
        internal TopDown3DCampResetPreparation PrepareReset()
        {
            if (!bound || resetLocked || !ledger.Valid || strike.Active || !beetle.CanRestore()) return null;
            int revision = ledger.Revision; uint epoch = ledger.Epoch;
            var prepared = new TopDown3DCampResetPreparation();
            prepared.World.Add(new TopDown3DPreparedWorldMutation(
                () => { if (resetLocked || ledger.Revision != revision || ledger.Epoch != epoch || !beetle.CanRestore()) return false; resetLocked = true; return true; },
                () => ledger.RestoreTrial(),
                () => { CancelAttack(); beetle.RestoreAuthored(); foreach (var source in sources) source.Refresh(); },
                () => resetLocked = false));
            return prepared;
        }
        private bool BeginAttack()
        {
            if (!SimulationAllowed || action.IsBusy || !player.LaboratoryStandingActionsAllowed || !player.IsGrounded)
            { action?.ReportFeedback("Stand and finish the current activity before attacking."); return true; }
            if (!player.TryBeginActionConstraint(this)) return true;
            if (!presentation.TryBeginAnimationPresentation(this, attackClip, false)) { player.EndActionConstraint(this); return true; }
            strike.Begin(); return true;
        }
        private bool CombatBusy() => strike.Active;
        private void FixedUpdate()
        {
            if (!strike.Active) return;
            if (!SimulationAllowed || !player.IsGrounded || !presentation.IsAnimationPresentationOwner(this)) { CancelAttack(); return; }
            bool hit = strike.Advance(Time.fixedDeltaTime, true);
            presentation.SetAnimationPresentationTime(this, strike.Seconds / .85f * attackClip.length);
            if (hit && CampHuskBeetle.CanHit(player.Position, player.FacingDirection, beetle.transform.position, 2.1f, .15f)
                && beetle.ClearToPlayer()) beetle.TakeHit(10);
            if (!strike.Active) CancelAttack();
        }
        private void CancelAttack()
        { strike.Cancel(); presentation?.EndAnimationPresentation(this); player?.EndActionConstraint(this); }
        private void ModeChanged(TopDown3DInputMode mode) { if (mode != TopDown3DInputMode.Gameplay) { CancelAttack(); beetle?.CancelIntent(); } }
        private void ReadinessChanged() { if (lab == null || !lab.PreviewReady) { CancelAttack(); beetle?.CancelIntent(); } }
        private void OnApplicationFocus(bool focused) { if (!focused) { CancelAttack(); beetle?.CancelIntent(); } }
        private void OnApplicationPause(bool paused) { if (paused) { CancelAttack(); beetle?.CancelIntent(); } }
        public void InvalidateForReset(uint epoch) { CancelAttack(); ledger.Invalidate(); beetle?.CancelIntent(); }
        public bool TryRestoreForReset(out string reason) { reason = null; return true; } // Animation reset never restores stock/health.
        private void OnDisable()
        {
            CancelAttack(); ledger.Invalidate(); beetle?.CancelIntent();
            if (!bound) return;
            lab.ResetRegistry.Unregister(this); trial.UnregisterResetParticipant(ResetKey);
            if (lab.ResourceAttackRequested == BeginAttack) lab.ResourceAttackRequested = null;
            if (lab.ResourceCombatBusy == CombatBusy) lab.ResourceCombatBusy = null;
            input.AnimationCancelRequested -= CancelAttack; bound = false;
            input.ModeChanged -= ModeChanged; lab.ReadinessChanged -= ReadinessChanged;
        }
    }
    public sealed partial class TopDown3DAnimationPlaygroundSession
    { public Func<bool> ResourceAttackRequested; public Func<bool> ResourceCombatBusy; }
}
