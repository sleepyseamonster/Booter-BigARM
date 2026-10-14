using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public struct TopDown3DStartingInventoryItem
    {
        [SerializeField] private string itemId;
        [SerializeField, Min(1)] private int quantity;

        public string ItemId => itemId;
        public int Quantity => quantity;

        public TopDown3DStartingInventoryItem(string stableItemId, int amount)
        {
            itemId = stableItemId;
            quantity = amount;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TopDown3DPlayerInventory : MonoBehaviour, ISerializationCallbackReceiver
    {
        public const int DefaultCapacity = 8;

        [SerializeField] private TopDown3DItemCatalog itemCatalog;
        [SerializeField, Min(1)] private int capacity = DefaultCapacity;
        [SerializeField] private bool campLoopLoadout;
        [SerializeField] private TopDown3DStartingInventoryItem[] startingItems =
            Array.Empty<TopDown3DStartingInventoryItem>();

        [NonSerialized] private TopDown3DInventoryState state;
        [SerializeField, HideInInspector] private TopDown3DInventorySnapshot reloadSnapshot;

        public TopDown3DItemCatalog ItemCatalog => TopDown3DCampLoopLoadoutProfile.Resolve(gameObject.scene, itemCatalog);
        public int Capacity => UsesLoadout ? TopDown3DLoadoutLayout.Capacity : capacity;
        public bool UsesLoadout => (campLoopLoadout || TopDown3DCampLoopLoadoutProfile.For(gameObject.scene, itemCatalog) != null) && TopDown3DAnimationPlaygroundSession.IsSessionOnly(gameObject.scene);
        public IReadOnlyList<TopDown3DStartingInventoryItem> StartingItems =>
            startingItems ?? Array.Empty<TopDown3DStartingInventoryItem>();
        public TopDown3DInventoryState State => EnsureState();

        internal void ConfigureLoadout(TopDown3DItemCatalog catalog, IReadOnlyList<TopDown3DItemAmount> items)
        { campLoopLoadout = true; Configure(catalog, TopDown3DLoadoutLayout.Capacity, items); }

        public void Configure(TopDown3DItemCatalog catalog, int slotCapacity = DefaultCapacity,
            IReadOnlyList<TopDown3DItemAmount> initialItems = null)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            itemCatalog = catalog;
            capacity = Mathf.Max(1, slotCapacity);
            if (initialItems == null)
                startingItems = Array.Empty<TopDown3DStartingInventoryItem>();
            else
            {
                startingItems = new TopDown3DStartingInventoryItem[initialItems.Count];
                for (var i = 0; i < initialItems.Count; i++)
                    startingItems[i] = new TopDown3DStartingInventoryItem(
                        initialItems[i].ItemId, initialItems[i].Quantity);
            }
            reloadSnapshot = null;
            state = CreateInitialState();
        }

        public TopDown3DInventorySnapshot CaptureSnapshot()
        {
            return EnsureState().CaptureSnapshot();
        }

        public TopDown3DInventoryResult ApplySnapshot(TopDown3DInventorySnapshot snapshot)
        {
            return EnsureState().ApplySnapshot(snapshot);
        }

        // State is intentionally lazy: scene deserialization can invoke Awake before the
        // scene path and authored marker are available. Installers resolve the layout first.

        private TopDown3DInventoryState EnsureState()
        {
            if (state != null && state.IsInitialized)
            {
                if (UsesLoadout && !state.Policy.IsLoadout)
                {
                    // A scene can resolve its marker after an early serialized consumer requested State.
                    // Only normalize an untouched starter grant; never replace a used/retained inventory.
                    var expected = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (var item in startingItems ?? Array.Empty<TopDown3DStartingInventoryItem>())
                        expected[item.ItemId] = expected.TryGetValue(item.ItemId, out var count) ? count + item.Quantity : item.Quantity;
                    foreach (var slot in state.Slots)
                        if (!slot.IsEmpty)
                        {
                            if (!expected.TryGetValue(slot.ItemId, out var count)) throw new InvalidOperationException("Loadout activation found nonstarter inventory; retained data requires explicit recovery.");
                            expected[slot.ItemId] = count - slot.Quantity;
                        }
                    foreach (var count in expected.Values)
                        if (count != 0) throw new InvalidOperationException("Loadout activation found changed starter contents; retained data requires explicit recovery.");
                    var prepared = CreateInitialState();
                    foreach (var slot in state.Slots)
                        if (!string.IsNullOrEmpty(slot.InstanceId) && prepared.ItemCatalog.GetRequiredDefinition(slot.ItemId).IsEquipment)
                            prepared.Slots[(int)prepared.ItemCatalog.GetRequiredDefinition(slot.ItemId).EquipmentSlot - 1].Set(slot.ItemId, slot.Quantity, slot.InstanceId);
                    if (!state.AdoptStartupLoadout(prepared))
                        throw new InvalidOperationException("Loadout activation cannot replace used/retained inventory; contents were preserved.");
                }
                return state;
            }

            if (itemCatalog == null)
            {
                throw new InvalidOperationException("TopDown3DPlayerInventory requires an authored item catalog.");
            }

            state = CreateInitialState();
            if (reloadSnapshot != null && reloadSnapshot.Version > 0 && !state.ApplySnapshot(reloadSnapshot).Succeeded)
            {
                state.RetainForRecovery();
                Debug.LogWarning("Retained backpack snapshot was incompatible and was preserved for recovery.", this);
            }
            return state;
        }

        public void OnBeforeSerialize()
        {
            if (Application.isPlaying && state != null && state.IsInitialized && !state.IsCommitting)
                reloadSnapshot = state.CaptureSnapshot();
        }
        public void OnAfterDeserialize() => state = null;

        private TopDown3DInventoryState CreateInitialState()
        {
            var catalog = ItemCatalog;
            var profile = TopDown3DCampLoopLoadoutProfile.For(gameObject.scene, itemCatalog);
            var loadout = profile != null || campLoopLoadout && TopDown3DAnimationPlaygroundSession.IsSessionOnly(gameObject.scene);
            var initialState = new TopDown3DInventoryState(catalog,
                new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, loadout ? TopDown3DLoadoutLayout.Capacity : capacity, 0f, loadout));
            initialState.ReleaseAllowed = id => !UsesLoadout || GetComponent<TopDown3DCampController>() == null
                || GetComponent<TopDown3DCampController>().Phase == TopDown3DCampPhase.Idle;
            var amounts = new List<TopDown3DItemAmount>();
            foreach (var item in startingItems ?? Array.Empty<TopDown3DStartingInventoryItem>())
                amounts.Add(new TopDown3DItemAmount(item.ItemId, item.Quantity));
            if (profile != null)
                foreach (var item in profile.startingEquipment)
                    amounts.Add(new TopDown3DItemAmount(item.ItemId, item.Quantity));
            if (amounts.Count == 0) return initialState;
            var result = initialState.TryAdd(amounts);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Invalid starting inventory: {result.Message}");
            return initialState;
        }
    }
}
