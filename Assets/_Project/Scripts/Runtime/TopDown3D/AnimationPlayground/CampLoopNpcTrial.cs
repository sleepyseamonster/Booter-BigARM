using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Lab-only lifecycle adapter. Generic NPC code has no knowledge of player reset or private art.
    public sealed class CampLoopNpcTrial : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        [SerializeField] private TopDown3DAnimationPlaygroundSession session;
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private NpcActor actor;
        [SerializeField] private Vector3 anchor;
        [SerializeField] private Quaternion facing = Quaternion.identity;
        private bool registered, bindingFailed, sessionSuspended;
        private uint restoredEpoch;
        private uint checkedActorEpoch;
        public string ResetId => "CampLoop.Npc.Lager"; // After Booter's Actor-stage IDs, before Input stage.
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.Actor;
        public string Failure { get; private set; }
        public Vector3 Anchor => anchor;
        public void Configure(TopDown3DAnimationPlaygroundSession lab, TopDown3DInputRouter router, NpcActor npc, Vector3 position)
        { session = lab; input = router; actor = npc; anchor = position; facing = npc.transform.rotation; }
        private void Update() => RefreshGate();
        private void OnEnable() { sessionSuspended = true; bindingFailed = false; }
        public void RefreshGate()
        {
            if (actor == null) return;
            if (!isActiveAndEnabled) { actor.SetSimulationAllowed(false); return; }
            if (!actor.isActiveAndEnabled) { actor.SetSimulationAllowed(false); Failure = "NPC actor is disabled."; return; }
            if (session == null || input == null || !session.IsLab || session.gameObject.scene != gameObject.scene
                || input.gameObject.scene != gameObject.scene || actor.gameObject.scene != gameObject.scene)
            { actor.SetSimulationAllowed(false); Failure = "Invalid explicit Camp Loop NPC binding."; return; }
            if (!session.isActiveAndEnabled || !session.PreviewReady)
            { sessionSuspended = true; actor.SetSimulationAllowed(false); Failure = "Waiting for Camp Loop: " + session.Readiness + ". " + session.UnavailableReason; return; }
            if (!registered)
            {
                if (bindingFailed) return;
                Physics.SyncTransforms();
                // Ground/clearance may become available after startup. A failed probe must not
                // permanently latch the optional trial off; retry without relocating the actor.
                if (!actor.Initialize(out var reason)) { Failure = reason; actor.SetSimulationAllowed(false); return; }
                try { session.ResetRegistry.Register(this); }
                catch (System.Exception error) { bindingFailed = true; Failure = error.Message; return; }
                registered = true; restoredEpoch = session.ResetRegistry.Epoch;
                checkedActorEpoch = actor.Epoch;
                session.ReadinessChanged += ReadinessChanged;
            }
            if (sessionSuspended && !input.LaboratoryResetBlocked && session.ResetRegistry.LastFailure == null)
            {
                // Session disable calls CancelAll. Resume at real position, never the fixture anchor.
                if (!actor.Initialize(out var reason)) { Failure = reason; actor.SetSimulationAllowed(false); return; }
                restoredEpoch = session.ResetRegistry.Epoch; sessionSuspended = false;
            }
            var ready = NpcMath.CanRearm(session.PreviewReady && session.isActiveAndEnabled,
                input.LaboratoryResetBlocked, session.ResetRegistry.LastFailure, session.ResetRegistry.Epoch, restoredEpoch);
            if (ready && actor.Epoch != checkedActorEpoch)
            {
                if (!actor.Initialize(out var reason)) { Failure = reason; actor.SetSimulationAllowed(false); return; }
                checkedActorEpoch = actor.Epoch;
            }
            actor.SetSimulationAllowed(ready);
            if (ready) Failure = null;
            else Failure = session.ResetRegistry.LastFailure ?? (input.LaboratoryResetBlocked
                ? "Waiting for laboratory reset input to be released." : "Waiting for a successful whole-session reset.");
        }
        public void InvalidateForReset(uint epoch) { actor?.Invalidate(); }
        private void ReadinessChanged()
        {
            if (session != null && !session.PreviewReady)
            { sessionSuspended = true; actor?.Invalidate(); }
        }
        public bool TryRestoreForReset(out string reason)
        {
            reason = "Missing registered NPC.";
            if (!registered || actor == null || !actor.isActiveAndEnabled || !isActiveAndEnabled || session == null) return false;
            Physics.SyncTransforms(); // Actor-stage predecessor moves must be visible to the clearance probe.
            if (!actor.Motor.Restore(anchor, facing, actor.Tuning, out reason)) { Failure = reason; return false; }
            restoredEpoch = session.ResetRegistry.Epoch;
            return true; // Motion stays disarmed until the whole registry and input restore succeed.
        }
        private void OnDisable()
        {
            actor?.Invalidate();
            if (registered && session != null)
            {
                try { session.ResetRegistry.Unregister(this); session.ReadinessChanged -= ReadinessChanged; registered = false; }
                catch (System.InvalidOperationException) { /* Retain registration until reset finishes; never mutate it in callbacks. */ }
            }
        }
    }
}
