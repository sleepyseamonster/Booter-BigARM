using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeWorldSettings : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float propSpawnChance = 0.12f;
        [SerializeField] private PrototypeWorldPropCatalog propCatalog;

        public float PropSpawnChance => propSpawnChance;
        public PrototypeWorldPropCatalog PropCatalog => propCatalog;
    }
}
