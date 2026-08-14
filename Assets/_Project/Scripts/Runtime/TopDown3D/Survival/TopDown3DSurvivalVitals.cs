using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DSurvivalVital
    {
        Health,
        Hunger,
        Thirst,
        Oxygen
    }

    [Serializable]
    public sealed class TopDown3DSurvivalSnapshot
    {
        [SerializeField] private int version;
        [SerializeField] private float health;
        [SerializeField] private float hunger;
        [SerializeField] private float thirst;
        [SerializeField] private float oxygen;

        public int Version => version;
        public float Health => health;
        public float Hunger => hunger;
        public float Thirst => thirst;
        public float Oxygen => oxygen;

        public static TopDown3DSurvivalSnapshot Create(
            float health,
            float hunger,
            float thirst,
            float oxygen)
        {
            return new TopDown3DSurvivalSnapshot
            {
                version = TopDown3DSurvivalVitals.CurrentSnapshotVersion,
                health = health,
                hunger = hunger,
                thirst = thirst,
                oxygen = oxygen
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class TopDown3DSurvivalVitals : MonoBehaviour
    {
        public const int CurrentSnapshotVersion = 1;

        [SerializeField] private TopDown3DSurvivalSettings settings;
        [SerializeField, Min(0f)] private float health = 100f;
        [SerializeField, Min(0f)] private float hunger = 100f;
        [SerializeField, Min(0f)] private float thirst = 100f;
        [SerializeField, Min(0f)] private float oxygen = 100f;
        [SerializeField, HideInInspector] private bool initialized;

        public TopDown3DSurvivalSettings Settings => settings;
        public float Health => health;
        public float Hunger => hunger;
        public float Thirst => thirst;
        public float Oxygen => oxygen;

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
            initialized = true;
            Changed?.Invoke();
        }

        public void Advance(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
            {
                return;
            }

            EnsureSettings();
            var nextHunger = Mathf.Max(0f, hunger - (settings.HungerDepletionPerSecond * elapsedSeconds));
            var nextThirst = Mathf.Max(0f, thirst - (settings.ThirstDepletionPerSecond * elapsedSeconds));
            if (Mathf.Approximately(nextHunger, hunger) && Mathf.Approximately(nextThirst, thirst))
            {
                return;
            }

            hunger = nextHunger;
            thirst = nextThirst;
            Changed?.Invoke();
        }

        public float GetValue(TopDown3DSurvivalVital vital)
        {
            return vital switch
            {
                TopDown3DSurvivalVital.Health => health,
                TopDown3DSurvivalVital.Hunger => hunger,
                TopDown3DSurvivalVital.Thirst => thirst,
                TopDown3DSurvivalVital.Oxygen => oxygen,
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
            }

            Changed?.Invoke();
        }

        public TopDown3DSurvivalSnapshot CaptureSnapshot()
        {
            return TopDown3DSurvivalSnapshot.Create(health, hunger, thirst, oxygen);
        }

        public bool ApplySnapshot(TopDown3DSurvivalSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Version != CurrentSnapshotVersion)
            {
                return false;
            }

            EnsureSettings();
            health = Mathf.Clamp(snapshot.Health, 0f, settings.MaximumHealth);
            hunger = Mathf.Clamp(snapshot.Hunger, 0f, settings.MaximumHunger);
            thirst = Mathf.Clamp(snapshot.Thirst, 0f, settings.MaximumThirst);
            oxygen = Mathf.Clamp(snapshot.Oxygen, 0f, settings.MaximumOxygen);
            initialized = true;
            Changed?.Invoke();
            return true;
        }

        private void Awake()
        {
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
            Advance(Time.deltaTime);
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
        }
    }
}
