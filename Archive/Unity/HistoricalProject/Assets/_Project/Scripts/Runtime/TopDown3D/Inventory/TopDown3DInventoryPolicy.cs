using System;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DInventoryOwner { Booter, BigArm }

    public readonly struct TopDown3DInventoryPolicy
    {
        public TopDown3DInventoryPolicy(TopDown3DInventoryOwner owner, int capacity, float maximumMass = 0f)
        {
            Owner = owner;
            Capacity = Math.Max(1, capacity);
            MaximumMass = Math.Max(0f, maximumMass);
        }

        public TopDown3DInventoryOwner Owner { get; }
        public int Capacity { get; }
        public float MaximumMass { get; }

        public bool Allows(TopDown3DItemDefinition definition)
        {
            if (definition == null) return false;
            return Owner == TopDown3DInventoryOwner.Booter
                ? definition.CarryPreference != TopDown3DCarryPreference.BigArmOnly
                : definition.CarryPreference != TopDown3DCarryPreference.BooterOnly;
        }
    }
}
