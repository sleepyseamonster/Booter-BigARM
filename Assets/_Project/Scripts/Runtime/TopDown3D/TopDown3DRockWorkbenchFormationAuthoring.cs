using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DRockFormationJoinStyle
    {
        PreserveNaturalSeams,
        SmoothFusedPreview
    }

    /// <summary>
    /// Editor-facing parent for arranging several editable rock workbenches as one formation.
    /// Runtime world generation does not depend on this component yet; its stable seed and
    /// controls are intended to become reproducible formation rules after visual approval.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class TopDown3DRockWorkbenchFormationAuthoring : MonoBehaviour
    {
        [Header("Formation Generator")]
        [SerializeField, Range(4f, 30f), Tooltip("Approximate width of the generated formation in meters. Larger formations automatically use more rocks.")]
        private float generatedOverallSize = 12f;
        [SerializeField, Range(0f, 1f), Tooltip("Low values create a few bold masses. High values create more rocks and a busier silhouette.")]
        private float generatedComplexity = 0.55f;
        [SerializeField, Range(0f, 1f), Tooltip("Low values create a low spreading formation. High values favor taller member rocks.")]
        private float generatedVerticality = 0.5f;

        [Header("Formation Preview")]
        [SerializeField] private Material rockMaterial;
        [SerializeField, Tooltip("Keeps the formation-wide material pattern and fractures repeatable.")]
        private int formationSeed = 481516;
        [SerializeField, Tooltip("Preserve Natural Seams keeps each rock editable and distinct. Smooth Fused Preview remeshes every source cube as one surface.")]
        private TopDown3DRockFormationJoinStyle joinStyle =
            TopDown3DRockFormationJoinStyle.PreserveNaturalSeams;
        [SerializeField, Tooltip("Rebuild shortly after a member rock or source cube is moved or changed.")]
        private bool autoRebuild = true;
        [SerializeField, Tooltip("Keep a collider on the optional fused preview. Individual rock colliders remain active when seams are preserved.")]
        private bool updateCollider = true;

        [Header("Formation Fractures")]
        [SerializeField, Range(0f, 1f), Tooltip("Strength of a few long fractures projected coherently over the entire formation.")]
        private float longFractures = 0.58f;
        [SerializeField, Range(1.5f, 16f), Tooltip("Average spacing in meters between formation-scale fractures.")]
        private float fractureSpacing = 6f;

        [Header("Smooth Fused Preview")]
        [SerializeField, Min(0.04f), Tooltip("Smaller voxels preserve more detail but make formation rebuilds slower.")]
        private float fusedVoxelSize = 0.22f;
        [SerializeField, Range(0f, 0.5f), Tooltip("Rounds only the joins in Smooth Fused Preview. This has no effect while natural seams are preserved.")]
        private float fusedJoinSoftness = 0.08f;

        [NonSerialized] private Mesh generatedMesh;
        [NonSerialized] private string previewStatus =
            "Add or group at least two Rock Workbenches to build a formation.";

        public Material RockMaterial => rockMaterial;
        public float GeneratedOverallSize => Mathf.Clamp(generatedOverallSize, 4f, 30f);
        public float GeneratedComplexity => Mathf.Clamp01(generatedComplexity);
        public float GeneratedVerticality => Mathf.Clamp01(generatedVerticality);
        public int GeneratedRockCount => Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Lerp(3f, 7f, GeneratedComplexity))
            + Mathf.RoundToInt(Mathf.InverseLerp(4f, 30f, GeneratedOverallSize) * 2f),
            3,
            9);
        public int FormationSeed => formationSeed;
        public TopDown3DRockFormationJoinStyle JoinStyle => joinStyle;
        public bool AutoRebuild => autoRebuild;
        public bool UpdateCollider => updateCollider;
        public float LongFractures => Mathf.Clamp01(longFractures);
        public float FractureSpacing => Mathf.Clamp(fractureSpacing, 1.5f, 16f);
        public float FusedVoxelSize => Mathf.Max(0.04f, fusedVoxelSize);
        public float FusedJoinSoftness => Mathf.Clamp(fusedJoinSoftness, 0f, 0.5f);
        public Mesh GeneratedMesh => generatedMesh;
        public string PreviewStatus => previewStatus;

        public void Configure(Material material, int seed)
        {
            rockMaterial = material;
            formationSeed = seed;
        }

        public void SetFormationSeed(int seed)
        {
            formationSeed = seed;
        }

        public void SetPreviewState(Mesh mesh, string status)
        {
            generatedMesh = mesh;
            previewStatus = string.IsNullOrWhiteSpace(status)
                ? "Formation preview state was not reported."
                : status;
        }
    }
}
