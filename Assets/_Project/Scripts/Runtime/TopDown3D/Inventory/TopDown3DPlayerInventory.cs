using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DPlayerInventory : MonoBehaviour
    {
        public const int DefaultCapacity = 8;

        [SerializeField] private TopDown3DItemCatalog itemCatalog;
        [SerializeField, Min(1)] private int capacity = DefaultCapacity;

        private TopDown3DInventoryState state;

        public TopDown3DItemCatalog ItemCatalog => itemCatalog;
        public int Capacity => capacity;
        public TopDown3DInventoryState State => EnsureState();

        public void Configure(TopDown3DItemCatalog catalog, int slotCapacity = DefaultCapacity)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            itemCatalog = catalog;
            capacity = Mathf.Max(1, slotCapacity);
            state = new TopDown3DInventoryState(itemCatalog, capacity);
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

            state = new TopDown3DInventoryState(itemCatalog, Mathf.Max(1, capacity));
            return state;
        }
    }
}
