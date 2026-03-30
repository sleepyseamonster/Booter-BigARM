using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    public enum PrototypeWorldPropCategory
    {
        Boulders,
        MetalObjects,
        BuildingClutter
    }

    public enum PrototypeWorldPropSizeClass
    {
        Px16,
        Px32,
        Px64,
        Px96,
        Px128,
        Px256
    }

    [Serializable]
    public sealed class PrototypeWorldPropDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private GameObject prefab;
        [SerializeField] private PrototypeWorldPropCategory category;
        [SerializeField] private PrototypeWorldPropSizeClass sizeClass;
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(0f)] private float spawnChanceMultiplier = 1f;
        [SerializeField] private bool enabled = true;

        public string Id => id;
        public GameObject Prefab => prefab;
        public PrototypeWorldPropCategory Category => category;
        public PrototypeWorldPropSizeClass SizeClass => sizeClass;
        public float Weight => Mathf.Max(0f, weight);
        public float SpawnChanceMultiplier => Mathf.Max(0f, spawnChanceMultiplier);
        public bool Enabled => enabled;
        public bool IsValid => enabled && prefab != null && weight > 0f && spawnChanceMultiplier > 0f;
    }

    [Serializable]
    public sealed class PrototypeWorldBiomePropGroup
    {
        [SerializeField] private string biomeId = "Greater Wasteland";
        [SerializeField] private List<PrototypeWorldPropDefinition> entries = new List<PrototypeWorldPropDefinition>();

        public string BiomeId => biomeId;
        public IReadOnlyList<PrototypeWorldPropDefinition> Entries => entries;
    }

    [CreateAssetMenu(
        fileName = "PrototypeWorldPropCatalog",
        menuName = "Booter & BigARM/World/Prototype Prop Catalog")]
    public sealed class PrototypeWorldPropCatalog : ScriptableObject
    {
        [SerializeField] private List<PrototypeWorldPropDefinition> entries = new List<PrototypeWorldPropDefinition>();
        [SerializeField] private List<PrototypeWorldBiomePropGroup> biomeGroups = new List<PrototypeWorldBiomePropGroup>();

        public IReadOnlyList<PrototypeWorldPropDefinition> Entries => entries;
        public IReadOnlyList<PrototypeWorldBiomePropGroup> BiomeGroups => biomeGroups;
    }
}
