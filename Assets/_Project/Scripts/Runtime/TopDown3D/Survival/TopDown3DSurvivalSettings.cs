using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(
        fileName = ResourceName,
        menuName = "Booter & BigARM/Top Down 3D/Survival Settings")]
    public sealed class TopDown3DSurvivalSettings : ScriptableObject
    {
        public const string ResourceName = "TopDown3DSurvivalSettings";

        [Header("Capacity")]
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(1f)] private float maximumHunger = 100f;
        [SerializeField, Min(1f)] private float maximumThirst = 100f;
        [SerializeField, Min(1f)] private float maximumOxygen = 100f;
        [SerializeField, Min(1f)] private float maximumReserve = 100f;

        [Header("First Survival Slice")]
        [SerializeField, Min(0f)] private float hungerDepletionPerSecond = 0.025f;
        [SerializeField, Min(0f)] private float thirstDepletionPerSecond = 0.04f;
        [Header("Reserve")]
        [SerializeField, Min(0f)] private float reserveDepletionPerSecond = 0.01f;
        [SerializeField, Min(0f)] private float exertionReserveDepletionPerSecond = 0.015f;
        [SerializeField, Min(0f)] private float reserveRestorationPerSecond = 0.8f;
        [SerializeField, Min(0f)] private float restDelaySeconds = 4f;

        public float MaximumHealth => maximumHealth;
        public float MaximumHunger => maximumHunger;
        public float MaximumThirst => maximumThirst;
        public float MaximumOxygen => maximumOxygen;
        public float MaximumReserve => maximumReserve;
        public float HungerDepletionPerSecond => hungerDepletionPerSecond;
        public float ThirstDepletionPerSecond => thirstDepletionPerSecond;
        public float ReserveDepletionPerSecond => reserveDepletionPerSecond;
        public float ExertionReserveDepletionPerSecond => exertionReserveDepletionPerSecond;
        public float ReserveRestorationPerSecond => reserveRestorationPerSecond;
        public float RestDelaySeconds => restDelaySeconds;

        public static TopDown3DSurvivalSettings Load()
        {
            return Resources.Load<TopDown3DSurvivalSettings>(ResourceName);
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1f, maximumHealth);
            maximumHunger = Mathf.Max(1f, maximumHunger);
            maximumThirst = Mathf.Max(1f, maximumThirst);
            maximumOxygen = Mathf.Max(1f, maximumOxygen);
            maximumReserve = Mathf.Max(1f, maximumReserve);
            hungerDepletionPerSecond = Mathf.Max(0f, hungerDepletionPerSecond);
            thirstDepletionPerSecond = Mathf.Max(0f, thirstDepletionPerSecond);
            reserveDepletionPerSecond = Mathf.Max(0f, reserveDepletionPerSecond);
            exertionReserveDepletionPerSecond = Mathf.Max(0f, exertionReserveDepletionPerSecond);
            reserveRestorationPerSecond = Mathf.Max(0f, reserveRestorationPerSecond);
            restDelaySeconds = Mathf.Max(0f, restDelaySeconds);
        }
    }
}
