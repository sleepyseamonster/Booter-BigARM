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
    public sealed class TopDown3DPlayerInventory : MonoBehaviour
    {
        public const int DefaultCapacity = 8;

        [SerializeField] private TopDown3DItemCatalog itemCatalog;
        [SerializeField, Min(1)] private int capacity = DefaultCapacity;
        [SerializeField] private TopDown3DStartingInventoryItem[] startingItems =
            Array.Empty<TopDown3DStartingInventoryItem>();

        private TopDown3DInventoryState state;

        public TopDown3DItemCatalog ItemCatalog => itemCatalog;
        public int Capacity => capacity;
        public IReadOnlyList<TopDown3DStartingInventoryItem> StartingItems =>
            startingItems ?? Array.Empty<TopDown3DStartingInventoryItem>();
        public TopDown3DInventoryState State => EnsureState();

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

        private void Awake()
        {
            EnsureState();
        }

        private TopDown3DInventoryState EnsureState()
        {
            if (state != null)
            {
                return state;
            }

            if (itemCatalog == null)
            {
                throw new InvalidOperationException("TopDown3DPlayerInventory requires an authored item catalog.");
            }

            state = CreateInitialState();
            return state;
        }

        private TopDown3DInventoryState CreateInitialState()
        {
            var initialState = new TopDown3DInventoryState(itemCatalog, Mathf.Max(1, capacity));
            if (startingItems == null || startingItems.Length == 0) return initialState;
            var amounts = new TopDown3DItemAmount[startingItems.Length];
            for (var i = 0; i < startingItems.Length; i++)
                amounts[i] = new TopDown3DItemAmount(startingItems[i].ItemId,
                    startingItems[i].Quantity);
            var result = initialState.TryAdd(amounts);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Invalid starting inventory: {result.Message}");
            return initialState;
        }
    }
}
