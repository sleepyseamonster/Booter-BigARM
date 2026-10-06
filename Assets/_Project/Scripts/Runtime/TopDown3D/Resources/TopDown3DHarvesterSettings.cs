using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Top Down 3D/Micro Dust Harvester Settings")]
    public sealed class TopDown3DHarvesterSettings : ScriptableObject
    {
        public const string ResourcePath = "Harvester/MicroDustHarvesterSettings";
        public const string CanisterItemId = "tool.micro_dust_harvester";
        public const string DustItemId = "resource.airborne_dust";

        [SerializeField, Min(1)] private int capacity = 100;
        [SerializeField, Min(0.1f)] private float secondsPerUnit = 10f;
        [SerializeField, Range(1, 128)] private int maximumDeployed = 32;
        [SerializeField, Range(0.5f, 3f)] private float placementDistance = 1.2f;
        [SerializeField, Range(1f, 4f)] private float pickupRange = 2.2f;
        [SerializeField, Range(0f, 60f)] private float maximumSlopeDegrees = 35f;

        public int Capacity => capacity;
        public float SecondsPerUnit => secondsPerUnit;
        public int MaximumDeployed => maximumDeployed;
        public float PlacementDistance => placementDistance;
        public float PickupRange => pickupRange;
        public float MaximumSlopeDegrees => maximumSlopeDegrees;

        public static TopDown3DHarvesterSettings Load()
        {
            return Resources.Load<TopDown3DHarvesterSettings>(ResourcePath);
        }
    }
}
