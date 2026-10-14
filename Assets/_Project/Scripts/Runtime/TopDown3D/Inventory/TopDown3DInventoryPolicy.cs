using System;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DInventoryOwner { Booter, BigArm, ResourceCache }

    public readonly struct TopDown3DInventoryPolicy
    {
        public TopDown3DInventoryPolicy(TopDown3DInventoryOwner owner, int capacity, float maximumMass = 0f, bool loadout = false, bool campStorage = false, bool canisterContents = false)
        {
            Owner = owner;
            Capacity = Math.Max(1, capacity);
            MaximumMass = Math.Max(0f, maximumMass);
            IsLoadout = loadout;
            IsCampStorage = campStorage;
            SupportsCanisterContents = canisterContents;
            if (loadout && (owner != TopDown3DInventoryOwner.Booter || capacity != TopDown3DLoadoutLayout.Capacity))
                throw new ArgumentException("Loadout requires exactly six equipment slots and four supply pockets on Booter.");
        }

        public TopDown3DInventoryOwner Owner { get; }
        public int Capacity { get; }
        public float MaximumMass { get; }
        public bool IsLoadout { get; }
        public bool IsCampStorage { get; }
        public bool SupportsCanisterContents { get; }
        public bool AllowsSlot(int index, TopDown3DItemDefinition item) => Allows(item) && (!IsLoadout || TopDown3DLoadoutLayout.Accepts(index, item));

        public bool Allows(TopDown3DItemDefinition definition)
        {
            if (definition == null) return false;
            if (Owner == TopDown3DInventoryOwner.ResourceCache) return IsCampStorage || definition.IsResource;
            return Owner == TopDown3DInventoryOwner.Booter
                ? !definition.IsResource && definition.CarryPreference != TopDown3DCarryPreference.BigArmOnly
                : definition.CarryPreference != TopDown3DCarryPreference.BooterOnly;
        }
    }
}
