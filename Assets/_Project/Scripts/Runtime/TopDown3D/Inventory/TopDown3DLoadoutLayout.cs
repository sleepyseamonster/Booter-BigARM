using System;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DEquipmentSlot { None, DustCanister, AlgaeCanister, GrapplingHook, DiggingTool, Cloak, Jacket }

    /// <summary>Stable equipment addresses and a distinct four-pocket supply region.</summary>
    public static class TopDown3DLoadoutLayout
    {
        public const int EquipmentCount = 6;
        public const int SupplyCount = 4;
        public const int Capacity = EquipmentCount + SupplyCount;
        private static readonly string[] Labels = { "Dust canister", "Algae canister", "Grappling hook", "Digging tool", "Cloak", "Jacket" };
        public static string Label(int index) => index >= 0 && index < EquipmentCount ? Labels[index] : $"Supply {index - EquipmentCount + 1}";
        public static bool Accepts(int index, TopDown3DItemDefinition item) => item != null && index >= 0 && index < Capacity
            && !item.IsResource && (index < EquipmentCount ? (int)item.EquipmentSlot == index + 1 : item.IsFieldSupply && item.EquipmentSlot == TopDown3DEquipmentSlot.None);
    }
}
