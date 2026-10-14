using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DDustRefinery : MonoBehaviour, ITopDown3DInteractable
    {
        [SerializeField] private string stationId = "camp-loop:dust-refinery:v1";
        [SerializeField, Min(1)] private int dustPerBattery = TopDown3DDustRefineryState.DustPerBattery;
        [SerializeField] private TopDown3DDustRefineryState state = new();
        [SerializeField] private GameObject canisterVisual, batteryVisual;
        [SerializeField] private Transform dustFill;
        [SerializeField] private Renderer batteryRenderer;
        private MaterialPropertyBlock batteryProperties;
        [NonSerialized] private bool locked;
        public TopDown3DDustRefineryState CaptureState() => state.Copy();
        public int DustPerBattery => dustPerBattery;
        public bool CanRefine => IsAvailable && state.HasCanister && state.dust >= dustPerBattery
            && state.batteryItemId == TopDown3DDustRefineryState.EmptyBatteryId;
        public string StableId => stationId;
        public UnityEngine.Object UnityObject => this;
        public string Prompt => "Use Dust Refinery";
        public Vector3 InteractionPoint => transform.position + Vector3.up * .8f;
        public float InteractionRange => 2.8f;
        public float ActionDuration => .01f;
        public TopDown3DItemAmount Reward => default;
        public bool IsAvailable => isActiveAndEnabled && state != null && state.IsValid && state.revision < long.MaxValue
            && dustPerBattery > 0 && dustPerBattery <= 100;
        public bool TryConsume(out int remainingUses) { remainingUses = 1; return false; }
        public event Action Changed;
        public bool CanAccess(Transform actor) => IsAvailable && actor != null && actor.gameObject.scene == gameObject.scene
            && Vector3.Distance(actor.position, InteractionPoint) <= InteractionRange && TopDown3DCargoAccess.HasLineOfSight(actor, transform);

        internal TopDown3DPreparedWorldMutation Prepare(TopDown3DDustRefineryState next)
        {
            if (next == null || !next.IsValid) throw new ArgumentException("Invalid prepared refinery state.");
            var expected = state.revision;
            if (expected == long.MaxValue) throw new InvalidOperationException("Refinery revision exhausted.");
            var prepared = next.Copy(); prepared.revision = expected + 1;
            return new TopDown3DPreparedWorldMutation(
                () => { if (locked || state.revision != expected) return false; locked = true; return true; },
                () => state = prepared,
                Publish,
                () => locked = false,
                prepared.HasCanister ? new[] { prepared.canisterInstanceId } : Array.Empty<string>());
        }
        private void Publish()
        {
            RefreshPresentation();
            var listeners = Changed; if (listeners == null) return;
            foreach (Action listener in listeners.GetInvocationList())
                try { listener(); } catch (Exception error) { Debug.LogException(error, this); }
        }
        private void OnEnable() => RefreshPresentation();
        private void RefreshPresentation()
        {
            if (state == null) return;
            if (canisterVisual != null) canisterVisual.SetActive(state.HasCanister);
            if (batteryVisual != null) batteryVisual.SetActive(state.HasBattery);
            if (dustFill != null)
            {
                var scale = dustFill.localScale; scale.y = Mathf.Max(.001f, state.dust / 100f * .14f); dustFill.localScale = scale;
                var position = dustFill.localPosition; position.y = .08f + scale.y; dustFill.localPosition = position;
                dustFill.gameObject.SetActive(state.dust > 0);
            }
            if (batteryRenderer != null)
            {
                batteryProperties ??= new MaterialPropertyBlock();
                batteryRenderer.GetPropertyBlock(batteryProperties);
                batteryProperties.SetColor("_BaseColor", state.batteryItemId == TopDown3DDustRefineryState.ChargedBatteryId
                    ? new Color(.15f, .85f, .85f) : new Color(.32f, .35f, .38f));
                batteryRenderer.SetPropertyBlock(batteryProperties);
            }
        }
        internal void ConfigureVisuals(GameObject canister, GameObject battery, Transform fill, Renderer batteryMesh)
        { canisterVisual = canister; batteryVisual = battery; dustFill = fill; batteryRenderer = batteryMesh; RefreshPresentation(); }

        public TopDown3DInventoryResult InsertBattery(TopDown3DInventoryState source, int index, Func<bool> access)
        {
            if (source == null || index < 0 || index >= source.Capacity || access == null || !access()
                || !IsAvailable || state.HasBattery || source.Slots[index].ItemId != TopDown3DDustRefineryState.EmptyBatteryId)
                return TopDown3DCraftingService.Failure("Choose an empty battery and an empty socket within reach.");
            var inventory = new TopDown3DInventoryTransaction.Mutation(source);
            var removed = inventory.Draft.RemoveFromSlot(index, 1); if (!removed.Succeeded) return removed;
            var next = state.Copy(); next.batteryItemId = TopDown3DDustRefineryState.EmptyBatteryId;
            return TopDown3DInventoryTransaction.Commit(new[] { inventory }, () => IsAvailable && access(), new[] { Prepare(next) });
        }

        public TopDown3DInventoryResult InsertCanister(TopDown3DInventoryState source, int index, Func<bool> access)
        {
            var contents = source != null && index >= 0 && index < source.Capacity ? source.Slots[index].CanisterContents : default;
            if (source == null || index < 0 || index >= source.Capacity || access == null || !access() || !IsAvailable
                || state.HasCanister || !contents.IsValid() || source.Slots[index].ItemId != TopDown3DHarvesterSettings.CanisterItemId
                || source.Slots[index].Quantity != 1 || !Guid.TryParseExact(source.Slots[index].InstanceId, "N", out _))
                return TopDown3DCraftingService.Failure("Choose a dust canister and an empty socket within reach.");
            var inventory = new TopDown3DInventoryTransaction.Mutation(source);
            var id = source.Slots[index].InstanceId;
            var removed = inventory.Draft.RemoveFromSlot(index, 1); if (!removed.Succeeded) return removed;
            var next = state.Copy(); next.canisterInstanceId = id; next.dust = contents.dust; next.fractionalSeconds = contents.fractionalSeconds;
            return TopDown3DInventoryTransaction.Commit(new[] { inventory }, () => IsAvailable && access(), new[] { Prepare(next) });
        }

        public TopDown3DInventoryResult Refine(Func<bool> access)
        {
            if (access == null || !access() || !IsAvailable) return TopDown3DCraftingService.Failure("Refinery is out of reach.");
            if (!state.TryRefine(out var next, out var reason, dustPerBattery)) return TopDown3DCraftingService.Failure(reason);
            return TopDown3DInventoryTransaction.Commit(Array.Empty<TopDown3DInventoryTransaction.Mutation>(),
                () => IsAvailable && access(), new[] { Prepare(next) });
        }

        public TopDown3DInventoryResult RetrieveBattery(TopDown3DInventoryState destination, Func<bool> access)
        {
            if (destination == null || access == null || !access() || !IsAvailable || !state.HasBattery)
                return TopDown3DCraftingService.Failure("No battery is available within reach.");
            var inventory = new TopDown3DInventoryTransaction.Mutation(destination);
            var added = inventory.Draft.TryAdd(new TopDown3DItemAmount(state.batteryItemId, 1)); if (!added.Succeeded) return added;
            var next = state.Copy(); next.batteryItemId = null;
            return TopDown3DInventoryTransaction.Commit(new[] { inventory }, () => IsAvailable && access(), new[] { Prepare(next) });
        }
        public TopDown3DInventoryResult RetrieveCanister(TopDown3DInventoryState destination, Func<bool> access)
        {
            if (destination == null || access == null || !access() || !IsAvailable || !state.HasCanister)
                return TopDown3DCraftingService.Failure("No canister is available within reach.");
            var inventory = new TopDown3DInventoryTransaction.Mutation(destination);
            var added = inventory.Draft.TryAdd(new TopDown3DItemAmount(TopDown3DHarvesterSettings.CanisterItemId, 1,
                state.canisterInstanceId, TopDown3DCanisterContents.Create(state.dust, state.fractionalSeconds)));
            if (!added.Succeeded) return added;
            var next = state.Copy(); next.canisterInstanceId = null; next.dust = 0; next.fractionalSeconds = 0;
            return TopDown3DInventoryTransaction.Commit(new[] { inventory }, () => IsAvailable && access(), new[] { Prepare(next) });
        }
    }
}
