using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Independent versioned layout, never an interpretation of legacy ten-slot saves.</summary>
    public static class CampExpeditionLayout
    {
        public const int Version = 1, BodyCount = 9, MountCount = 2, CarriedCount = 4, Capacity = BodyCount + MountCount + CarriedCount;
        private static readonly string[] addresses = { "back", "frame", "chest", "legs", "head", "hands", "feet", "weapon", "tool", "attachment-0", "attachment-1" };
        public static string Address(int index) => index >= 0 && index < addresses.Length ? addresses[index] : null;
        public static int Index(string address) => address!=null&&address.StartsWith("carried-",StringComparison.Ordinal)&&int.TryParse(address.Substring(8),out var carried)&&carried>=0&&carried<CarriedCount?BodyCount+MountCount+carried:Array.IndexOf(addresses,address);
        public static bool Allows(int index,TopDown3DItemDefinition item)
        {
            if(item==null || index<0 || index>=Capacity || item.IsResource)return false;
            if(index>=BodyCount+MountCount)return true; // Existing four carried positions, not newly invented garment pockets.
            return index switch
            {
                0=>item.EquipmentSlot==TopDown3DEquipmentSlot.Cloak,
                1=>item.EquipmentSlot==TopDown3DEquipmentSlot.BodyFrame,
                2=>item.EquipmentSlot==TopDown3DEquipmentSlot.Jacket,
                3=>item.EquipmentSlot==TopDown3DEquipmentSlot.Legs,
                4=>item.EquipmentSlot==TopDown3DEquipmentSlot.Head,
                5=>item.EquipmentSlot==TopDown3DEquipmentSlot.Hands,
                6=>item.EquipmentSlot==TopDown3DEquipmentSlot.Feet,
                7=>item.EquipmentSlot==TopDown3DEquipmentSlot.Weapon,
                8=>item.EquipmentSlot==TopDown3DEquipmentSlot.DiggingTool,
                _=>item.EquipmentSlot is TopDown3DEquipmentSlot.AlgaeCanister or TopDown3DEquipmentSlot.DustCanister or TopDown3DEquipmentSlot.GrapplingHook
            };
        }
        public static bool Valid(IReadOnlyList<TopDown3DInventorySlot> slots)
        {
            if(slots==null || slots.Count!=Capacity)return false;
            // Only the qualified two-mount frame is enabled in this prototype.
            return slots[1].ItemId==CampExpeditionDefinition.HarnessId || slots[9].IsEmpty && slots[10].IsEmpty;
        }
    }
}
