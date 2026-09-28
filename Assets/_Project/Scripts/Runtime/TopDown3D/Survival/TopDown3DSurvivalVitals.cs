using System;
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
        private float idleSeconds;

        public TopDown3DSurvivalSettings Settings => settings;
        public float Health => health;
        public float Hunger => hunger;
        public float Thirst => thirst;
        public float Oxygen => oxygen;
        public float Reserve => reserve;

        public event Action Changed;

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
            idleSeconds = 0f;
            initialized = true;
            Changed?.Invoke();
        }

        public void Advance(float elapsedSeconds)
        {
            Advance(elapsedSeconds, false, false);
        }

        public void Advance(float elapsedSeconds, bool exerting, bool resting)
        {
            if (elapsedSeconds <= 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
            {
                return;
            }

            EnsureSettings();
            var nextHunger = Mathf.Max(0f, hunger - (settings.HungerDepletionPerSecond * elapsedSeconds));
            var nextThirst = Mathf.Max(0f, thirst - (settings.ThirstDepletionPerSecond * elapsedSeconds));
            var nextReserve = resting
                ? Mathf.Min(settings.MaximumReserve, reserve + settings.ReserveRestorationPerSecond * elapsedSeconds)
                : Mathf.Max(0f, reserve - (settings.ReserveDepletionPerSecond
                    + (exerting ? settings.ExertionReserveDepletionPerSecond : 0f)) * elapsedSeconds);
            if (Mathf.Approximately(nextHunger, hunger) && Mathf.Approximately(nextThirst, thirst)
                && Mathf.Approximately(nextReserve, reserve))
            {
                return;
            }

            hunger = nextHunger;
            thirst = nextThirst;
            reserve = nextReserve;
            Changed?.Invoke();
        }

        internal void AdvanceActivity(float elapsedSeconds, bool exerting, bool idle)
        {
            if (elapsedSeconds <= 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
                return;

            EnsureSettings();
            if (!idle)
            {
                idleSeconds = 0f;
                Advance(elapsedSeconds, exerting, false);
                return;
            }

            var beforeRest = Mathf.Min(elapsedSeconds, Mathf.Max(0f, settings.RestDelaySeconds - idleSeconds));
            if (beforeRest > 0f)
                Advance(beforeRest, false, false);
            var restingTime = elapsedSeconds - beforeRest;
            if (restingTime > 0f)
                Advance(restingTime, false, true);
            idleSeconds += elapsedSeconds;
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
            var clampedValue = Mathf.Clamp(value, 0f, GetMaximum(vital));
            if (Mathf.Approximately(GetValue(vital), clampedValue))
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
                    break;
                case TopDown3DSurvivalVital.Thirst:
                    thirst = clampedValue;
                    break;
                case TopDown3DSurvivalVital.Oxygen:
                    oxygen = clampedValue;
                    break;
                case TopDown3DSurvivalVital.Reserve:
                    reserve = clampedValue;
                    break;
            }

            Changed?.Invoke();
        }

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
            idleSeconds = 0f;
            initialized = true;
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
            var elapsed = Time.deltaTime;
            var exerting = motor != null && motor.SprintHeld
                && (motor.SprintActive
                    && Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).sqrMagnitude > 0.04f
                    || motor.ClimbMode != TopDown3DClimbMode.None
                    || motor.ActiveTraversal != TopDown3DTraversalMove.None);
            var idle = motor != null && motor.IsGrounded && !motor.SprintHeld
                && !motor.HasMovementInput && !motor.IsActionConstrained
                && motor.ActiveTraversal == TopDown3DTraversalMove.None
                && motor.ClimbMode == TopDown3DClimbMode.None
                && Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).sqrMagnitude < 0.04f;
            AdvanceActivity(elapsed, exerting, idle);
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
        }
    }
}
