using System;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class CampLoopSalvageSession : MonoBehaviour, IAnimationPlaygroundResetParticipant,
        ITopDown3DInteractable, ITopDown3DTargetEpoch, ITopDown3DTargetColliderOwner
    {
        [SerializeField] private TopDown3DItemCatalog catalog;
        [SerializeField] private TopDown3DPlayerInventory kit;
        [SerializeField] private TopDown3DBigArmCargo cargo;
        [SerializeField] private TopDown3DResourceCache cache;
        [SerializeField] private TopDown3DSalvageWreckState wreck;
        private TopDown3DAnimationPlaygroundSession lab;
        private bool registered, failed;
        private TopDown3DSalvagePoint[] points;
        public bool Ready => registered && !failed && isActiveAndEnabled && lab != null && lab.isActiveAndEnabled && lab.PreviewReady;
        public UnityEngine.Object UnityObject => this;
        public string StableId => wreck != null ? wreck.StableId + ":scavenge" : string.Empty;
        public uint Epoch => wreck != null ? wreck.Epoch : 0;
        private TopDown3DSalvagePoint NextPart => points?.FirstOrDefault(p => p != null && p.IsAvailable);
        public bool IsAvailable => Ready && NextPart != null;
        public TopDown3DItemAmount Reward => NextPart?.Reward ?? default;
        public float ActionDuration => NextPart?.ActionDuration ?? 0;
        public float InteractionRange => 2.8f;
        public string Prompt => NextPart is { } part && catalog != null
            ? $"Scavenge machine — {part.Reward.Quantity} {catalog.GetRequiredDefinition(part.Reward.ItemId).DisplayName}"
            : string.Empty;
        public Vector3 InteractionPoint
        {
            get
            {
                var actor = kit != null ? kit.transform.position + Vector3.up * .8f : transform.position;
                var closest = transform.position; var distance = float.PositiveInfinity;
                foreach (var collider in GetComponentsInChildren<Collider>())
                {
                    if (!collider.enabled || collider.isTrigger) continue;
                    var point = collider.ClosestPoint(actor); var squared = (point - actor).sqrMagnitude;
                    if (squared < distance) { closest = point; distance = squared; }
                }
                return closest;
            }
        }
        // The carcass owns its hull and socket colliders; other world obstacles still block access.
        public bool OwnsCollider(Collider collider) => collider != null && collider.transform.IsChildOf(transform);
        public bool TryConsume(out int remainingUses)
        {
            remainingUses = 0;
            var part = NextPart;
            return Ready && part != null && part.TryConsume(out remainingUses);
        }
        public string ResetId => "CampLoop.Salvage.Wreck01";
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.StationState;
        public string FailureReason { get; private set; }
        public void Configure(TopDown3DItemCatalog items, TopDown3DPlayerInventory player, TopDown3DBigArmCargo carrier, TopDown3DResourceCache storage, TopDown3DSalvageWreckState state)
        { catalog = items; kit = player; cargo = carrier; cache = storage; wreck = state; }
        private TopDown3DInventoryState[] Inventories() => new[] { kit.State, cargo.State, cache.State };
        private void Update() => TryBindSession();
        internal void TryBindSession()
        {
            if (registered || failed) return;
            lab = TopDown3DAnimationPlaygroundSession.Find(gameObject.scene);
            if (!GreaterWastelandSceneScope.IsCampLoop(gameObject.scene) || lab == null) { Fail("Salvage requires the marked Camp Loop playground."); return; }
            if (lab.Readiness == AnimationPlaygroundReadiness.Failed) { Fail("Salvage is unavailable because playground initialization failed."); return; }
            if (!lab.PreviewReady) return;
            try
            {
                if (kit == null || cargo == null || cache == null || wreck == null || catalog == null || !catalog.TryValidate(out _)
                    || kit.gameObject.scene != gameObject.scene || cargo.gameObject.scene != gameObject.scene || cache.gameObject.scene != gameObject.scene
                    || kit.ItemCatalog != catalog || cargo.ItemCatalog != catalog || cache.ItemCatalog != catalog)
                    throw new InvalidOperationException("Private salvage catalog bindings are missing; inventory was preserved.");
                points = gameObject.GetComponentsInChildren<TopDown3DSalvagePoint>(true).OrderBy(p => p.StableId, StringComparer.Ordinal).ToArray();
                var wrecks = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TopDown3DSalvageWreckState>(true)).ToArray();
                if (points.Length != 3 || points.Select(p => p.StableId).Distinct().Count() != 3 || wrecks.Length != 1
                    || Inventories().Any(s => s.ItemCatalog != catalog) || !wreck.ValidateRewards(Inventories()))
                    throw new InvalidOperationException("Retained salvage inventory and wreck state do not match; extraction is disabled.");
                lab.ResetRegistry.Register(this); registered = true;
            }
            catch (Exception error) { Fail(error.Message); }
        }
        private void Fail(string reason) { failed = true; FailureReason = reason; Debug.LogWarning(reason, this); }
        public void InvalidateForReset(uint epoch)
        {
            // Unity objects can be destroyed before this session receives OnDisable.
            if (kit != null)
            {
                var action = kit.GetComponent<TopDown3DPlayerActionController>();
                if (action != null) action.CancelGather();
            }
            if (wreck != null) wreck.Invalidate();
        }
        public bool TryRestoreForReset(out string reason)
        {
            reason = "Salvage reset storage is missing.";
            if (kit == null || cargo == null || cache == null || wreck == null) return false;
            if (!wreck.TryResetRewards(Inventories(), out reason)) return false;
            foreach (var point in GetComponentsInChildren<TopDown3DSalvagePoint>(true)) point.RefreshPresentation();
            return true;
        }
        private void OnDisable()
        {
            InvalidateForReset(0);
            if (registered && lab != null) lab.ResetRegistry.Unregister(this);
            registered = false;
        }
    }
}
