using UnityEngine;
using UnityEngine.Serialization;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// The broad composition language used by the standalone rock workshop.
    /// This is deliberately separate from the streamed-world geological planner.
    /// </summary>
    public enum TopDown3DRockFormationStyle
    {
        Cluster,
        Ridge,
        SpireCluster,
        Scatter
    }

    /// <summary>
    /// Serialized controls for a standalone, editor-authored rock generator. It uses
    /// the project's baked rock mesh library, but it does not query or reproduce the
    /// procedural world planner. A seed records a liked result so it can be revisited.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DRockFormationAuthoring : MonoBehaviour
    {
        [Header("Rock Library")]
        [SerializeField] private TopDown3DNaturalObjectCatalog rockMeshCatalog;
        [SerializeField, FormerlySerializedAs("regularRockMaterial")] private Material rockMaterial;
        // Retains the library reference on formations made before this became a standalone tool.
        // It is never used as a world-generator input.
        [SerializeField, HideInInspector, FormerlySerializedAs("worldSettings")]
        private TopDown3DWorldSettings legacyRockLibrarySource;

        [Header("Formation Controls")]
        [SerializeField, FormerlySerializedAs("sourceWorldSeed")] private int authoringSeed = 1357911;
        [SerializeField] private TopDown3DRockFormationStyle style = TopDown3DRockFormationStyle.Cluster;
        [SerializeField, Min(1)] private int minimumMemberCount = 4;
        [SerializeField, Min(1)] private int maximumMemberCount = 9;
        [SerializeField, Min(0.1f)] private float formationRadius = 4f;
        [SerializeField, Range(0f, 1f)] private float compactness = 0.7f;
        [SerializeField, Range(0f, 1f)] private float verticality = 0.55f;
        [SerializeField, Range(0f, 1f)] private float largeRockFrequency = 0.38f;
        [SerializeField] private bool autoFuseOverlappingMembers = true;
        [SerializeField, HideInInspector] private bool autoFusionPreferenceInitialized;
        [SerializeField, HideInInspector, TextArea(2, 3)] private string fusionStatus = "No fusion has run yet.";
        [SerializeField, HideInInspector] private Transform generatedMembersRoot;

        public TopDown3DNaturalObjectCatalog RockMeshCatalog => rockMeshCatalog != null
            ? rockMeshCatalog
            : legacyRockLibrarySource != null ? legacyRockLibrarySource.NaturalObjectCatalog : null;
        public Material RockMaterial => rockMaterial;
        public int AuthoringSeed => authoringSeed;
        public TopDown3DRockFormationStyle Style => style;
        public int MinimumMemberCount => Mathf.Max(1, minimumMemberCount);
        public int MaximumMemberCount => Mathf.Max(MinimumMemberCount, maximumMemberCount);
        public float FormationRadius => Mathf.Max(0.1f, formationRadius);
        public float Compactness => Mathf.Clamp01(compactness);
        public float Verticality => Mathf.Clamp01(verticality);
        public float LargeRockFrequency => Mathf.Clamp01(largeRockFrequency);
        public bool AutoFuseOverlappingMembers => autoFuseOverlappingMembers;
        public string FusionStatus => fusionStatus;
        public Transform GeneratedMembersRoot => generatedMembersRoot;

        public void Configure(TopDown3DNaturalObjectCatalog catalog, Material material)
        {
            rockMeshCatalog = catalog;
            rockMaterial = material;
            minimumMemberCount = Mathf.Max(1, minimumMemberCount);
            maximumMemberCount = Mathf.Max(minimumMemberCount, maximumMemberCount);
            formationRadius = Mathf.Max(0.1f, formationRadius);
            autoFuseOverlappingMembers = true;
            autoFusionPreferenceInitialized = true;
        }

        public void SetAuthoringSeed(int seed)
        {
            authoringSeed = seed;
        }

        public void CopyGeneratorSettingsFrom(TopDown3DRockFormationAuthoring source)
        {
            if (source == null) return;

            rockMeshCatalog = source.RockMeshCatalog;
            rockMaterial = source.rockMaterial;
            style = source.style;
            minimumMemberCount = source.minimumMemberCount;
            maximumMemberCount = source.maximumMemberCount;
            formationRadius = source.formationRadius;
            compactness = source.compactness;
            verticality = source.verticality;
            largeRockFrequency = source.largeRockFrequency;
            autoFuseOverlappingMembers = source.autoFuseOverlappingMembers;
            autoFusionPreferenceInitialized = source.autoFusionPreferenceInitialized;
        }

        public void SetFusionStatus(string value)
        {
            fusionStatus = value ?? string.Empty;
        }

        public void SetGeneratedMembersRoot(Transform root)
        {
            generatedMembersRoot = root;
        }

        private void OnValidate()
        {
            // Formations created before automatic fusion was introduced serialized this
            // as false. Migrate that first-time value without overriding a later choice.
            if (!autoFusionPreferenceInitialized)
            {
                autoFuseOverlappingMembers = true;
                autoFusionPreferenceInitialized = true;
            }
        }
    }
}
