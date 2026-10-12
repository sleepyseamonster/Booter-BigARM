using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DSurvivalVital
    {
        Health,
        Hunger,
        Thirst,
        Oxygen,
        Reserve
    }

    [Serializable]
    public sealed class TopDown3DSurvivalSnapshot
    {
        [SerializeField] private int version;
        [SerializeField] private float health;
        [SerializeField] private float hunger;
        [SerializeField] private float thirst;
        [SerializeField] private float oxygen;
        [SerializeField] private float reserve;

        public int Version => version;
        public float Health => health;
        public float Hunger => hunger;
        public float Thirst => thirst;
        public float Oxygen => oxygen;
        public float Reserve => reserve;

        public static TopDown3DSurvivalSnapshot Create(
            float health,
            float hunger,
            float thirst,
            float oxygen,
            float reserve)
        {
            return new TopDown3DSurvivalSnapshot
            {
                version = TopDown3DSurvivalVitals.CurrentSnapshotVersion,
                health = health,
                hunger = hunger,
                thirst = thirst,
                oxygen = oxygen,
                reserve = reserve
            };
        }
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(500)]
    public sealed class TopDown3DSurvivalVitals : MonoBehaviour
    {
        public const int CurrentSnapshotVersion = 2;

        [SerializeField] private TopDown3DSurvivalSettings settings;
        [SerializeField, Min(0f)] private float health = 100f;
        [SerializeField, Min(0f)] private float hunger = 100f;
        [SerializeField, Min(0f)] private float thirst = 100f;
        [SerializeField, Min(0f)] private float oxygen = 100f;
        [SerializeField, Min(0f)] private float reserve = 100f;
        [SerializeField, HideInInspector] private bool initialized;
        private TopDown3DPlayerMotor motor;
        private readonly List<ActivityTransition> transitions = new();
        private TopDown3DCampSettings campSettings;
        private TopDown3DSurvivalActivity activity;
        private double activityRemaining;
        private double clockCursor;
        private bool clockStarted;
        private double exactHunger, exactThirst, exactOxygen, exactReserve;
        private bool precisionStarted;
        private void AlignPrecision()
        { exactHunger = hunger; exactThirst = thirst; exactOxygen = oxygen; exactReserve = reserve; precisionStarted = true; }
        private void EnsurePrecision() { if (!precisionStarted) AlignPrecision(); }
        private struct ActivityTransition { public double time; public TopDown3DSurvivalActivity mode; public TopDown3DCampSettings settings; }
        public TopDown3DSurvivalActivity Activity => activity;

        public TopDown3DSurvivalSettings Settings => settings;
        public float Health => health;
        public float Hunger => hunger;
        public float Thirst => thirst;
        public float Oxygen => oxygen;
        public float Reserve => reserve;

        public event Action Changed;
        public event Action HealthReduced;
        public event Action ResetPerformed;
        public event Action<string> ActivityCompleted;

        public void QueueActivity(TopDown3DSurvivalActivity mode, TopDown3DCampSettings config, double timestamp)
        {
            if (!double.IsFinite(timestamp) || (mode != TopDown3DSurvivalActivity.Ordinary && config == null))
                throw new ArgumentException("Activity needs a finite simulation timestamp and authored settings.");
            var entry = new ActivityTransition { time = timestamp, mode = mode, settings = config };
            var index = transitions.FindIndex(value => value.time > timestamp);
            if (index < 0) transitions.Add(entry); else transitions.Insert(index, entry);
        }

        internal void SetActivity(TopDown3DSurvivalActivity mode, TopDown3DCampSettings config)
        {
            activity = mode; campSettings = config;
            activityRemaining = mode == TopDown3DSurvivalActivity.Rest ? config.restDuration : float.PositiveInfinity;
        }

        public void AdvanceActivity(float elapsedSeconds)
        {
            if (!(elapsedSeconds > 0) || !float.IsFinite(elapsedSeconds)) return;
            if (activity == TopDown3DSurvivalActivity.Ordinary) { Advance(elapsedSeconds); return; }
            var mode = activity;
            EnsurePrecision();
            var quote = TopDown3DSleepRules.Quote(this, campSettings, mode, Mathf.Min(elapsedSeconds, (float)activityRemaining));
            var used = quote.Seconds;
            exactHunger = Math.Max(0, exactHunger - quote.Food);
            exactThirst = Math.Max(0, exactThirst - quote.Water);
            exactOxygen = Math.Max(0, exactOxygen - quote.Oxygen);
            var target = (mode == TopDown3DSurvivalActivity.Sleep ? campSettings.sleepTarget : campSettings.restTarget) * settings.MaximumReserve;
            if (exactReserve < target) exactReserve = Math.Min(target, exactReserve + quote.Gain);
            hunger = (float)exactHunger; thirst = (float)exactThirst; oxygen = (float)exactOxygen; reserve = (float)exactReserve;
            activityRemaining -= used;
            if (used > 0) Changed?.Invoke();
            var more = TopDown3DSleepRules.Quote(this, campSettings, mode, (float)activityRemaining);
            if (used < elapsedSeconds - .0001f || !more.CanStart)
            {
                activity = TopDown3DSurvivalActivity.Ordinary;
                ActivityCompleted?.Invoke(activityRemaining <= .001f ? "Rest complete" : "Supplies getting low");
                Advance(Mathf.Max(0, elapsedSeconds - used));
            }
        }

        private void ResetActivityClock()
        {
            transitions.Clear(); activity = TopDown3DSurvivalActivity.Ordinary; clockStarted = false;
            AlignPrecision();
        }

        public void Configure(TopDown3DSurvivalSettings authoredSettings, bool refillVitals = false)
        {
            settings = authoredSettings;
            EnsureSettings();
            if (refillVitals)
            {
                ResetToFull();
                return;
            }

            ClampVitals();
        }

        public void ResetToFull()
        {
            EnsureSettings();
            health = settings.MaximumHealth;
            hunger = settings.MaximumHunger;
            thirst = settings.MaximumThirst;
            oxygen = settings.MaximumOxygen;
            reserve = settings.MaximumReserve;
            initialized = true;
            ResetActivityClock();
            ResetPerformed?.Invoke();
            Changed?.Invoke();
        }

        public void Advance(float elapsedSeconds)
        {
            Advance(elapsedSeconds, false);
        }

        public void Advance(float elapsedSeconds, bool exerting)
        {
            if (elapsedSeconds <= 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
            {
                return;
            }

            EnsureSettings();
            EnsurePrecision();
            exactHunger = Math.Max(0, exactHunger - (double)settings.HungerDepletionPerSecond * elapsedSeconds);
            exactThirst = Math.Max(0, exactThirst - (double)settings.ThirstDepletionPerSecond * elapsedSeconds);
            exactReserve = Math.Max(0, exactReserve - (double)(settings.ReserveDepletionPerSecond
                + (exerting ? settings.ExertionReserveDepletionPerSecond : 0f)) * elapsedSeconds);
            var nextHunger = (float)exactHunger; var nextThirst = (float)exactThirst; var nextReserve = (float)exactReserve;
            if (nextHunger == hunger && nextThirst == thirst && nextReserve == reserve)
            {
                return;
            }

            hunger = nextHunger;
            thirst = nextThirst;
            reserve = nextReserve;
            Changed?.Invoke();
        }

        internal static bool IsReserveExertion(
            bool sprintHeld,
            bool sprintActive,
            float planarSpeed,
            TopDown3DClimbMode climbMode,
            TopDown3DTraversalMove traversal)
        {
            return sprintHeld && ((sprintActive && planarSpeed > 0.2f)
                || climbMode != TopDown3DClimbMode.None
                || traversal != TopDown3DTraversalMove.None);
        }

        public float GetValue(TopDown3DSurvivalVital vital)
        {
            return vital switch
            {
                TopDown3DSurvivalVital.Health => health,
                TopDown3DSurvivalVital.Hunger => hunger,
                TopDown3DSurvivalVital.Thirst => thirst,
                TopDown3DSurvivalVital.Oxygen => oxygen,
                TopDown3DSurvivalVital.Reserve => reserve,
                _ => 0f
            };
        }

        public float GetMaximum(TopDown3DSurvivalVital vital)
        {
            EnsureSettings();
            return vital switch
            {
                TopDown3DSurvivalVital.Health => settings.MaximumHealth,
                TopDown3DSurvivalVital.Hunger => settings.MaximumHunger,
                TopDown3DSurvivalVital.Thirst => settings.MaximumThirst,
                TopDown3DSurvivalVital.Oxygen => settings.MaximumOxygen,
                TopDown3DSurvivalVital.Reserve => settings.MaximumReserve,
                _ => 1f
            };
        }

        public float GetNormalizedValue(TopDown3DSurvivalVital vital)
        {
            return Mathf.Clamp01(GetValue(vital) / Mathf.Max(1f, GetMaximum(vital)));
        }

        public void SetValue(TopDown3DSurvivalVital vital, float value)
        {
            EnsurePrecision();
            if (!float.IsFinite(value)) throw new ArgumentException("Vital value must be finite.");
            var previousHealth = health;
            var clampedValue = Mathf.Clamp(value, 0f, GetMaximum(vital));
            if (GetValue(vital) == clampedValue)
            {
                return;
            }

            switch (vital)
            {
                case TopDown3DSurvivalVital.Health:
                    health = clampedValue;
                    break;
                case TopDown3DSurvivalVital.Hunger:
                    hunger = clampedValue;
                    exactHunger = clampedValue;
                    break;
                case TopDown3DSurvivalVital.Thirst:
                    thirst = clampedValue;
                    exactThirst = clampedValue;
                    break;
                case TopDown3DSurvivalVital.Oxygen:
                    oxygen = clampedValue;
                    exactOxygen = clampedValue;
                    break;
                case TopDown3DSurvivalVital.Reserve:
                    reserve = clampedValue;
                    exactReserve = clampedValue;
                    break;
            }

            if (vital == TopDown3DSurvivalVital.Health && health < previousHealth) HealthReduced?.Invoke();
            Changed?.Invoke();
        }

        internal double GetPreciseValue(TopDown3DSurvivalVital vital)
        {
            EnsurePrecision();
            return vital == TopDown3DSurvivalVital.Thirst ? exactThirst
                : vital == TopDown3DSurvivalVital.Hunger ? exactHunger : GetValue(vital);
        }
        internal void ApplyPreciseRestoration(TopDown3DSurvivalVital vital, double value)
        {
            if (vital == TopDown3DSurvivalVital.Thirst) { exactThirst = value; thirst = (float)value; }
            else if (vital == TopDown3DSurvivalVital.Hunger) { exactHunger = value; hunger = (float)value; }
            else if (vital == TopDown3DSurvivalVital.Health) health = (float)value;
        }
        internal void PublishRestoration() => Changed?.Invoke();

        public TopDown3DSurvivalSnapshot CaptureSnapshot()
        {
            return TopDown3DSurvivalSnapshot.Create(health, hunger, thirst, oxygen, reserve);
        }

        public bool ApplySnapshot(TopDown3DSurvivalSnapshot snapshot)
        {
            if (snapshot == null || (snapshot.Version != 1 && snapshot.Version != CurrentSnapshotVersion))
            {
                return false;
            }

            EnsureSettings();
            health = Mathf.Clamp(snapshot.Health, 0f, settings.MaximumHealth);
            hunger = Mathf.Clamp(snapshot.Hunger, 0f, settings.MaximumHunger);
            thirst = Mathf.Clamp(snapshot.Thirst, 0f, settings.MaximumThirst);
            oxygen = Mathf.Clamp(snapshot.Oxygen, 0f, settings.MaximumOxygen);
            reserve = snapshot.Version == 1
                ? settings.MaximumReserve
                : Mathf.Clamp(snapshot.Reserve, 0f, settings.MaximumReserve);
            initialized = true;
            ResetActivityClock();
            ResetPerformed?.Invoke();
            Changed?.Invoke();
            return true;
        }

        private void Awake()
        {
            motor = GetComponent<TopDown3DPlayerMotor>();
            EnsureSettings();
            if (initialized)
            {
                ClampVitals();
            }
            else
            {
                ResetToFull();
            }
        }

        private void Update()
        {
            SynchronizeRuntimeClock();
        }

        internal void SynchronizeRuntimeClock()
        {
            var elapsed = Time.deltaTime;
            var exerting = motor != null && IsReserveExertion(
                motor.SprintHeld,
                motor.SprintActive,
                Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).magnitude,
                motor.ClimbMode,
                motor.ActiveTraversal);
            ReconcileRuntimeClock(Time.timeAsDouble, elapsed, exerting);
        }

        internal void ReconcileRuntimeClock(double now, float elapsed, bool exerting)
        {
            if (!clockStarted) { clockCursor = now - elapsed; clockStarted = true; }
            while (transitions.Count > 0 && transitions[0].time <= now)
            {
                var entry = transitions[0]; transitions.RemoveAt(0);
                var boundary = Math.Max(clockCursor, entry.time);
                if (activity == TopDown3DSurvivalActivity.Ordinary) Advance((float)(boundary - clockCursor), exerting);
                else AdvanceActivity((float)(boundary - clockCursor));
                clockCursor = boundary;
                activity = entry.mode; campSettings = entry.settings;
                activityRemaining = activity == TopDown3DSurvivalActivity.Rest ? campSettings.restDuration : float.PositiveInfinity;
            }
            if (activity == TopDown3DSurvivalActivity.Ordinary) Advance((float)(now - clockCursor), exerting);
            else AdvanceActivity((float)(now - clockCursor));
            clockCursor = now;
        }

        private void OnValidate()
        {
            if (settings != null)
            {
                ClampVitals();
            }
        }

        private void EnsureSettings()
        {
            if (settings != null)
            {
                return;
            }

            settings = TopDown3DSurvivalSettings.Load();
            if (settings == null)
            {
                throw new InvalidOperationException(
                    $"Missing Resources/{TopDown3DSurvivalSettings.ResourceName} survival settings asset.");
            }
        }

        private void ClampVitals()
        {
            health = Mathf.Clamp(health, 0f, settings.MaximumHealth);
            hunger = Mathf.Clamp(hunger, 0f, settings.MaximumHunger);
            thirst = Mathf.Clamp(thirst, 0f, settings.MaximumThirst);
            oxygen = Mathf.Clamp(oxygen, 0f, settings.MaximumOxygen);
            reserve = Mathf.Clamp(reserve, 0f, settings.MaximumReserve);
            AlignPrecision();
        }
    }
}
