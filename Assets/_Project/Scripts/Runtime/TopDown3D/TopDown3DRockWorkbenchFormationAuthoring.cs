using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DRockFormationArchetype
    {
        ConnectedOutcrop,
        ScatteredRocks,
        PileOfRocks
    }

    public enum TopDown3DRockFormationJoinStyle
    {
        PreserveNaturalSeams,
        SmoothFusedPreview,
        FusedGeologicalSeams
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
        [SerializeField, Tooltip("Selects the broad geological composition rules used when generating member rocks.")]
        private TopDown3DRockFormationArchetype formationArchetype =
            TopDown3DRockFormationArchetype.ConnectedOutcrop;
        [SerializeField, Range(4f, 30f), Tooltip("Approximate width of the generated formation in meters. Larger formations automatically use more rocks.")]
        private float generatedOverallSize = 12f;
        [SerializeField, Range(1f, 30f), Tooltip("Physical height of the generated formation in meters. Height is independent from width.")]
        private float generatedHeight = 8f;
        // Retained for scene and prefab compatibility. Complexity and verticality are now
        // derived from the formation's physical dimensions.
        [SerializeField, HideInInspector]
        private float generatedComplexity = 0.55f;
        [SerializeField, HideInInspector]
        private float generatedVerticality = 0.5f;

        [Header("Formation Preview")]
        [SerializeField] private Material rockMaterial;
        [SerializeField, Tooltip("Keeps the formation-wide material pattern and fractures repeatable.")]
        private int formationSeed = 481516;
        [SerializeField, Tooltip("Fused Geological Seams creates one exterior shell without hidden member geometry while retaining visible rock boundaries.")]
        private TopDown3DRockFormationJoinStyle joinStyle =
            TopDown3DRockFormationJoinStyle.FusedGeologicalSeams;
        [SerializeField, Tooltip("Rebuild shortly after a member rock or source cube is moved or changed.")]
        private bool autoRebuild = true;
        [SerializeField, Tooltip("Keep a collider on the optional fused preview. Individual rock colliders remain active when seams are preserved.")]
        private bool updateCollider = true;

        [Header("Member Rock Mesh")]
        [SerializeField, Range(0.025f, 0.18f), InspectorName("Tessellation Size"), Tooltip("Controls the surface grid for every generated member rock. Smaller values produce finer, less stepped silhouettes but rebuild more slowly.")]
        private float memberVoxelSize = 0.075f;
        [SerializeField, Range(0f, 0.35f), InspectorName("Rock Smoothing"), Tooltip("Rounds the blends between the editable source masses inside every member rock. This does not move or merge separate rocks.")]
        private float memberFusionSmoothness = 0.11f;
        [SerializeField, Range(0f, 1f), Tooltip("Relaxes each completed rock surface after meshing to remove voxel-scale teeth and stair steps while preserving volume and ground contact.")]
        private float memberSurfaceRelaxation = 0.55f;

        [Header("Formation Fractures")]
        [SerializeField, Range(0f, 1f), Tooltip("Strength of a few long fractures projected coherently over the entire formation.")]
        private float longFractures = 0.58f;
        [SerializeField, Range(1.5f, 16f), Tooltip("Average spacing in meters between formation-scale fractures.")]
        private float fractureSpacing = 6f;

        [Header("Smooth Fused Preview")]
        [SerializeField, Min(0.04f), Tooltip("Smaller voxels preserve more detail but make formation rebuilds slower.")]
        private float fusedVoxelSize = 0.22f;
        [SerializeField, Range(0f, 0.5f), Tooltip("Rounds all joins in Smooth Fused Preview. Fused Geological Seams uses only a restrained fraction so member boundaries remain readable.")]
        private float fusedJoinSoftness = 0.08f;

        [Header("Fused Geological Seams")]
        [SerializeField, Range(0.08f, 1.2f), Tooltip("Physical width in meters of the retained contact boundary between fused member rocks.")]
        private float geologicalSeamWidth = 0.34f;
        [SerializeField, Range(0f, 1f), Tooltip("Visual depth and darkness of retained rock-to-rock contact seams.")]
        private float geologicalSeamStrength = 0.78f;

        [NonSerialized] private Mesh generatedMesh;
        [NonSerialized] private string previewStatus =
            "Add or group at least two Rock Workbenches to build a formation.";

        public Material RockMaterial => rockMaterial;
        public TopDown3DRockFormationArchetype FormationArchetype => formationArchetype;
        public float GeneratedWidth => Mathf.Clamp(generatedOverallSize, 4f, 30f);
        public float GeneratedHeight => Mathf.Clamp(generatedHeight, 1f, 30f);
        public float GeneratedOverallSize => GeneratedWidth;
        private float GeneratedDimensionMagnitude => Mathf.Sqrt(
            GeneratedWidth * GeneratedWidth * 0.65f
            + GeneratedHeight * GeneratedHeight * 0.35f);
        public float GeneratedComplexity => Mathf.Clamp01(Mathf.InverseLerp(
            1f,
            23f,
            GeneratedDimensionMagnitude));
        public float GeneratedVerticality =>
            TopDown3DRockWorkbenchAuthoring.CalculateAspectVerticality(
                GeneratedWidth,
                GeneratedHeight);
        private float GeneratedCountScale => Mathf.InverseLerp(
            3.28f,
            30f,
            GeneratedDimensionMagnitude);
        public int GeneratedRockCount => FormationArchetype switch
        {
            TopDown3DRockFormationArchetype.ScatteredRocks => Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(5f, 20f, GeneratedCountScale)),
                5,
                20),
            TopDown3DRockFormationArchetype.PileOfRocks => Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(
                    5f,
                    20f,
                    Mathf.Sqrt(GeneratedCountScale))),
                5,
                20),
            _ => Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.Lerp(5f, 10f, GeneratedComplexity))
                    + Mathf.RoundToInt(
                        GeneratedComplexity * 4f),
                    5,
                    14)
        };
        public int FormationSeed => formationSeed;
        public int SurfaceFieldSeed
        {
            get
            {
                var landmark = GetComponentInParent<TopDown3DRockLandmarkAuthoring>();
                return landmark != null && landmark.ShareGeologicalField
                    ? landmark.LandmarkSeed
                    : FormationSeed;
            }
        }
        public Vector3 SurfaceFieldOrigin
        {
            get
            {
                var landmark = GetComponentInParent<TopDown3DRockLandmarkAuthoring>();
                return landmark != null && landmark.ShareGeologicalField
                    ? landmark.transform.position
                    : transform.position;
            }
        }
        public TopDown3DRockFormationJoinStyle JoinStyle => joinStyle;
        public bool AutoRebuild => autoRebuild;
        public bool UpdateCollider => updateCollider;
        public float MemberVoxelSize => Mathf.Clamp(memberVoxelSize, 0.025f, 0.18f);
        public float MemberTessellationDetail =>
            TopDown3DRockWorkbenchAuthoring.VoxelSizeToTessellationDetail(MemberVoxelSize);
        public float MemberFusionSmoothness => Mathf.Clamp(memberFusionSmoothness, 0f, 0.35f);
        public float MemberSurfaceRelaxation => Mathf.Clamp01(memberSurfaceRelaxation);
        public float LongFractures => Mathf.Clamp01(longFractures);
        public float FractureSpacing => Mathf.Clamp(fractureSpacing, 1.5f, 16f);
        public float FusedVoxelSize => Mathf.Max(0.04f, fusedVoxelSize);
        public float FusedJoinSoftness => Mathf.Clamp(fusedJoinSoftness, 0f, 0.5f);
        public float GeologicalSeamWidth => Mathf.Clamp(geologicalSeamWidth, 0.08f, 1.2f);
        public float GeologicalSeamStrength => Mathf.Clamp01(geologicalSeamStrength);
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
