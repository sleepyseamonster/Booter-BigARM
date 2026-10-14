using System;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Two sockets own inserted objects. Presentation never owns their contents.</summary>
    [Serializable]
    public sealed class TopDown3DDustRefineryState
    {
        public const string EmptyBatteryId = "equipment.arc_battery_empty";
        public const string ChargedBatteryId = "equipment.arc_battery_charged";
        public const int DustPerBattery = 10;
        public int version = 1;
        public string canisterInstanceId;
        public int dust;
        public double fractionalSeconds;
        public string batteryItemId;
        public long revision;

        public bool HasCanister => !string.IsNullOrEmpty(canisterInstanceId);
        public bool HasBattery => !string.IsNullOrEmpty(batteryItemId);
        public bool CanRefine => HasCanister && dust >= DustPerBattery && batteryItemId == EmptyBatteryId;
        public bool IsValid => version == 1 && revision >= 0
            && (HasCanister ? Guid.TryParseExact(canisterInstanceId, "N", out _) && dust >= 0 && dust <= 100
                && double.IsFinite(fractionalSeconds) && fractionalSeconds >= 0 && fractionalSeconds < 10
                && (dust < 100 || fractionalSeconds == 0) : dust == 0 && fractionalSeconds == 0)
            && (!HasBattery || batteryItemId == EmptyBatteryId || batteryItemId == ChargedBatteryId);

        public TopDown3DDustRefineryState Copy() => new()
        {
            version = version, canisterInstanceId = canisterInstanceId, dust = dust,
            fractionalSeconds = fractionalSeconds, batteryItemId = batteryItemId, revision = revision
        };

        internal bool TryRefine(out TopDown3DDustRefineryState prepared, out string reason, int cost = DustPerBattery)
        {
            prepared = null;
            reason = !HasCanister ? "Insert a dust canister." : batteryItemId != EmptyBatteryId
                ? "Insert an empty battery." : $"The canister needs at least {cost} dust.";
            if (!IsValid || cost <= 0 || !HasCanister || dust < cost || batteryItemId != EmptyBatteryId || revision == long.MaxValue) return false;
            prepared = Copy();
            prepared.dust -= cost;
            prepared.batteryItemId = ChargedBatteryId;
            prepared.revision++;
            reason = null;
            return true;
        }
    }
}
