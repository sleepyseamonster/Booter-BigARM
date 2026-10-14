using System;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Contents only; equipment identity belongs to the inventory's canonical InstanceId.</summary>
    [Serializable]
    public struct TopDown3DCanisterContents
    {
        public int version;
        public int dust;
        public double fractionalSeconds;
        public bool IsPresent => version != 0;
        public bool IsValid(int capacity = 100, double secondsPerUnit = 10) => version == 1
            && capacity > 0 && double.IsFinite(secondsPerUnit) && secondsPerUnit > 0
            && dust >= 0 && dust <= capacity && double.IsFinite(fractionalSeconds)
            && fractionalSeconds >= 0 && fractionalSeconds < secondsPerUnit
            && (dust < capacity || fractionalSeconds == 0);
        public static TopDown3DCanisterContents Create(int amount = 0, double remainder = 0) =>
            new() { version = 1, dust = amount, fractionalSeconds = remainder };
    }
}
