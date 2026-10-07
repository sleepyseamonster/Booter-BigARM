using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DBigArmCargo : MonoBehaviour
    {
        public const int DefaultCapacity = 12;
        [SerializeField] private TopDown3DItemCatalog itemCatalog;
        [SerializeField] private TopDown3DPackingSettings packingSettings;
        [SerializeField, Min(0.1f)] private float accessRange = 3.2f;
        private TopDown3DInventoryState state;
        private TopDown3DBigArmLoadProfile loadProfile;

        public TopDown3DItemCatalog ItemCatalog => itemCatalog;
        public TopDown3DPackingSettings PackingSettings => packingSettings;
        public TopDown3DInventoryState State => EnsureState();
        public TopDown3DBigArmLoadProfile LoadProfile => loadProfile;
        public float AccessRange => accessRange;
        public event Action Changed;

        public void Configure(TopDown3DItemCatalog catalog, TopDown3DPackingSettings settings)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (!settings.TryValidate(out var error))
            {
                throw new ArgumentException(error ?? "Packing settings are required.", nameof(settings));
            }
            itemCatalog = catalog; packingSettings = settings;
            state = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, DefaultCapacity, settings.HardMass));
            Recalculate();
        }

        public bool IsInAccessRange(Vector3 booterPosition)
        { return Vector3.Distance(transform.position, booterPosition) <= accessRange; }

        public bool TryAutoPack()
        {
            if (!TopDown3DCargoAutoPacker.TryPack(State, packingSettings, out var snapshot)) return false;
            var result = State.ApplySnapshot(snapshot);
            if (result.Succeeded) { Recalculate(); Changed?.Invoke(); }
            return result.Succeeded;
        }

        public TopDown3DBigArmCargoSnapshot CaptureSnapshot() => TopDown3DBigArmCargoSnapshot.Create(State.CaptureSnapshot());

        public TopDown3DInventoryResult ApplySnapshot(TopDown3DBigArmCargoSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Version != 1 || snapshot.LoadframeVersion != 1 || snapshot.Inventory == null)
                return new TopDown3DInventoryResult(TopDown3DInventoryResultCode.InvalidSnapshot, "Invalid BigARM cargo snapshot.");
            var result = State.ApplySnapshot(snapshot.Inventory);
            if (result.Succeeded) { Recalculate(); Changed?.Invoke(); }
            return result;
        }

        private void Awake() { if (itemCatalog != null && packingSettings != null) EnsureState(); }

        private TopDown3DInventoryState EnsureState()
        {
            if (state != null) return state;
            if (itemCatalog == null || packingSettings == null) throw new InvalidOperationException("BigARM cargo requires an item catalog and packing settings.");
            state = new TopDown3DInventoryState(itemCatalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, DefaultCapacity, packingSettings.HardMass));
            Recalculate();
            return state;
        }

        private void Recalculate()
        {
            if (state == null || packingSettings == null) return;
            var left = 0f; var right = 0f;
            for (var i = 0; i < state.Slots.Count && i < packingSettings.Mounts.Count; i++)
            {
                var slot = state.Slots[i]; if (slot.IsEmpty) continue;
                var mass = itemCatalog.GetRequiredDefinition(slot.ItemId).Mass * slot.Quantity;
                if (packingSettings.Mounts[i].Side == TopDown3DMountSide.Left) left += mass; else right += mass;
            }
            loadProfile = new TopDown3DBigArmLoadProfile(state.CalculateMass(), left, right, packingSettings.RecommendedMass, packingSettings.HardMass);
        }
    }
}
