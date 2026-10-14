using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DAlgaeCanisterSnapshot
    {
        public int version = 1;
        public double remaining;
        public float capacity;
    }

    [DisallowMultipleComponent, DefaultExecutionOrder(550)]
    public sealed class TopDown3DAlgaeCanisterState : MonoBehaviour
    {
        [SerializeField] private TopDown3DAlgaeCanisterSettings settings;
        [SerializeField] private double remaining;
        [SerializeField] private bool initialized;
        private bool transactionActive;
        public TopDown3DAlgaeCanisterSettings Settings => settings;
        public double Remaining => remaining;
        public event Action Changed;
        public event Action Restored;

        internal TopDown3DPreparedWorldMutation PrepareTrialRestore(TopDown3DAlgaeCanisterSnapshot snapshot)
        {
            if (snapshot == null || settings == null || snapshot.version != 1 || snapshot.capacity != settings.capacity
                || !double.IsFinite(snapshot.remaining) || snapshot.remaining < 0 || snapshot.remaining > settings.capacity) return null;
            var value = snapshot.remaining; var before = remaining;
            return new TopDown3DPreparedWorldMutation(
                () => { if (transactionActive || remaining != before) return false; transactionActive = true; return true; },
                () => { remaining = value; initialized = true; },
                () => { Restored?.Invoke(); Changed?.Invoke(); }, () => transactionActive = false);
        }

        private void Update() => Advance(Time.deltaTime);

        /// <summary>Grow on the survival simulation timeline, including accelerated sleep; no offline catch-up.</summary>
        public void Advance(double elapsedSeconds)
        {
            if (transactionActive || !initialized || settings == null || !settings.IsValid
                || !double.IsFinite(elapsedSeconds) || elapsedSeconds <= 0) return;
            var next = Math.Min(settings.capacity, remaining + (double)settings.replenishmentPerSecond * elapsedSeconds);
            if (next <= remaining) return;
            transactionActive = true;
            try { remaining = next; Changed?.Invoke(); }
            finally { transactionActive = false; }
        }

        public void Configure(TopDown3DAlgaeCanisterSettings config)
        {
            if (config == null || !config.IsValid) throw new ArgumentException("Invalid algae settings");
            if (initialized && (!double.IsFinite(remaining) || remaining < 0)) throw new InvalidOperationException("Invalid retained algae contents");
            settings = config;
            remaining = initialized ? Math.Min(remaining, config.capacity) : config.initialFill;
            initialized = true;
        }
        public TopDown3DAlgaeCanisterSnapshot CaptureSnapshot() => new() { remaining = remaining, capacity = settings.capacity };
        public bool ApplySnapshot(TopDown3DAlgaeCanisterSnapshot snapshot)
        {
            if (transactionActive || settings == null || !settings.IsValid || snapshot == null || snapshot.version != 1
                || !double.IsFinite(snapshot.remaining) || !float.IsFinite(snapshot.capacity) || snapshot.capacity <= 0
                || snapshot.remaining < 0 || snapshot.remaining > snapshot.capacity) return false;
            // A smaller authored capacity clamps retained contents; changing capacity never scales/refills them.
            remaining = Math.Min(snapshot.remaining, settings.capacity); initialized = true;
            Restored?.Invoke(); Changed?.Invoke(); return true;
        }

        internal bool TryConsume(TopDown3DSurvivalVitals vitals, TopDown3DAlgaeUse use, out double gain, out double cost)
        {
            gain = cost = 0;
            if (transactionActive || !initialized || settings == null || !settings.IsValid || vitals == null
                || vitals.Health <= 0 || remaining <= 0 || !Enum.IsDefined(typeof(TopDown3DAlgaeUse), use)) return false;
            transactionActive = true;
            try
            {
                var vital = use == TopDown3DAlgaeUse.Drink ? TopDown3DSurvivalVital.Thirst
                    : use == TopDown3DAlgaeUse.Eat ? TopDown3DSurvivalVital.Hunger : TopDown3DSurvivalVital.Health;
                var before = vitals.GetPreciseValue(vital);
                var ratio = (double)settings.CostPerPoint(use);
                var amount = Math.Min(vitals.GetMaximum(vital) - before, Math.Min(settings.Gain(use), remaining / ratio));
                var after = Math.Min(vitals.GetMaximum(vital), before + amount);
                if (vital == TopDown3DSurvivalVital.Health)
                {
                    var represented = (float)after;
                    if (represented > after) represented = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(represented) - 1);
                    after = represented;
                }
                gain = after - before; cost = gain * ratio;
                // Reject sub-visible restoration rather than spending contents without changing the public meter.
                if (!(gain >= .000001) || (float)after == vitals.GetValue(vital) || cost > remaining + 1e-10)
                { gain = cost = 0; return false; }
                cost = Math.Min(cost, remaining);
                if (!(cost > 0) || remaining - cost == remaining) { gain = cost = 0; return false; }
                remaining -= cost;
                vitals.ApplyPreciseRestoration(vital, after); // silent: both owners commit before either notification
                vitals.PublishRestoration(); Changed?.Invoke();
                return true;
            }
            finally { transactionActive = false; }
        }
    }
}
