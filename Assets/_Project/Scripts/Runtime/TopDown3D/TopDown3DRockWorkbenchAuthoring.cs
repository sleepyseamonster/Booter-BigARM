using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DRockSilhouetteProfile
    {
        Auto,
        Boulder,
        Slab,
        AngularChunk,
        SplitLobe,
        Shard,
        FracturedBoulder,
        BlockyMonolith,
        BrokenSlab
    }

    public enum TopDown3DRockSurfacePreset
    {
        NeutralWeathered,
        DarkFracturedDesert
    }

    /// <summary>
    /// Editor-authoring root for experimenting with a rock as a union of editable stone volumes.
    /// This preview is intentionally independent from streamed procedural-world generation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class TopDown3DRockWorkbenchAuthoring : MonoBehaviour
    {
        public const int GoldenRockSeed = 2126351350;
        public const float GoldenRockWidth = 0.925f;
        public const float GoldenRockBodyLength = 1.12f;
        public const int CurrentStandalonePhysicalScaleVersion = 1;
        public const float LegacyStandalonePhysicalBake = 0.1f;
        public const float MinimumStandaloneRockWidth = 0.075f;
        public const float MaximumStandaloneRockWidth = 1.2f;
        public const float MinimumStandaloneRockHeight = 0.05f;
        public const float MaximumStandaloneRockHeight = 3f;
        public const float FinestStandaloneRockVoxelSize = 0.0025f;
        public const float CoarsestStandaloneRockVoxelSize = 0.018f;
        public const float MinimumTessellationDetail = 1f;
        public const float MaximumTessellationDetail = 5f;
        public const float CoarsestVoxelSize = 0.18f;
        public const float FinestVoxelSize = 0.025f;

        [Header("Preview")]
        [SerializeField] private Material rockMaterial;
        [SerializeField, Min(0.025f), Tooltip("Smaller voxels make a more detailed surface but take longer to rebuild.")]
        private float voxelSize = 0.1f;
        [SerializeField, Range(0f, 1f), Tooltip("Rounds and thickens the join between nearby source volumes. Zero produces a hard union.")]
        private float fusionSmoothness = 0.16f;
        [SerializeField, Range(0f, 1f), Tooltip("Relaxes the finished triangle surface to remove voxel-scale teeth and stair steps while preserving the rock's volume and ground contact.")]
        private float surfaceRelaxation = 0.45f;
        [SerializeField, Tooltip("Rebuild the preview shortly after a source volume or setting changes.")]
        private bool autoRebuild = true;
        [SerializeField, Tooltip("Show selectable wireframes for the source volumes in the Scene view.")]
        private bool showSourceVolumes = true;
        [SerializeField, Tooltip("Keep a MeshCollider synchronized with the temporary preview mesh.")]
        private bool updateCollider = true;

        [Header("Surface Language")]
        [SerializeField, Range(0.45f, 3f), InspectorName("Geology Scale"), Tooltip("Physical size of the stone grain and strata in meters. Higher values create broader geological structure.")]
        private float geologyScale = 1.1f;
        [SerializeField, Range(0f, 1f), Tooltip("Strength of broad seeded transitions between smoother worn stone and rough grainy stone.")]
        private float surfaceVariation = 0.72f;
        [SerializeField, Range(0f, 1f), Tooltip("Visibility of procedurally integrated fissures across top and side surfaces.")]
        private float crackAmount = 0.42f;
        [SerializeField, Range(0f, 1f), InspectorName("Side Grit"), Tooltip("Visibility and prevalence of deep, coarse grain patches on steep side faces.")]
        private float sideGrit = 0.3f;
        [SerializeField, Range(0f, 1f), InspectorName("Underside Shale"), Tooltip("Strength of brittle, broken shale on downward-facing surfaces beneath ledges and overhangs.")]
        private float undersideShale = 1f;
        [SerializeField, Range(0f, 1f), InspectorName("Side Shale Patches"), Tooltip("Strength of irregular brittle-shale patches mixed into steep side faces.")]
        private float sideShalePatches = 0.36f;
        [SerializeField, Range(0f, 1f), InspectorName("Top Shale Patches"), Tooltip("Strength of dispersed shattered-plate islands across upward-facing rock surfaces.")]
        private float topShalePatches = 0.42f;
        [SerializeField, Range(0f, 0.5f), Tooltip("Additional highlight response on smoother worn patches. The rock remains non-metallic.")]
        private float wornShine = 0.2f;

        [Header("Base Rock Generator")]
        [SerializeField, Tooltip("Regenerating this seed reproduces the same editable source-volume arrangement.")]
        private int generationSeed = 1729;
        // Retained so existing scenes and prefabs keep their previous size data.
        [SerializeField, HideInInspector]
        private Vector3 generatedOverallSize = new Vector3(4f, 3f, 3.5f);
        [SerializeField, Range(0.75f, 12f), InspectorName("Width"), Tooltip("Physical width and depth of the generated rock in meters.")]
        private float generatedOverallScale = 4f;
        [SerializeField, Range(0.5f, 30f), Tooltip("Physical height of the generated rock in meters. Height is independent from width.")]
        private float generatedHeight = 3f;
        // Retained for scene and prefab compatibility. Shape direction is now derived from
        // the independent physical width and height controls.
        [SerializeField, HideInInspector]
        private float generatedVerticality = 0.45f;
        [SerializeField, Range(0f, 1f), InspectorName("Lopsidedness"), Tooltip("Zero distributes masses around the core. One drives growth strongly toward one side with greater size and tilt variation.")]
        private float generatedAsymmetry = 0.65f;
        [SerializeField, Range(0f, 1f), InspectorName("Compaction"), Tooltip("Zero preserves distinct overlapping lobes. One pulls the masses tightly together into a dense fused body.")]
        private float generatedOverlap = 0.62f;
        [SerializeField, Range(0f, 1f), InspectorName("Major Fractures"), Tooltip("Adds one to three editable subtractive cuts that break the silhouette and create deep structural joints.")]
        private float generatedMajorFractures = 0.68f;
        [SerializeField, Range(0f, 1f), InspectorName("Edge Damage"), Tooltip("Controls how strongly corners and exposed edges are chipped by the implicit stone volumes.")]
        private float generatedEdgeDamage = 0.58f;
        [SerializeField, HideInInspector]
        private TopDown3DRockSilhouetteProfile generatedSilhouetteProfile =
            TopDown3DRockSilhouetteProfile.Auto;

        // Version zero is the earlier standalone-rock scale. The one-time conversion
        // preserves the accepted 0.1-sized result while keeping reusable roots at unit scale.
        // Formation members are excluded because their physical scale belongs to the formation.
        [FormerlySerializedAs("groundedGeologyPhysicalScaleVersion")]
        [SerializeField, HideInInspector]
        private int standalonePhysicalScaleVersion;

        [Header("Material Family")]
        [SerializeField, Tooltip("Selects a coherent material response while preserving per-rock seeded variation.")]
        private TopDown3DRockSurfacePreset surfacePreset =
            TopDown3DRockSurfacePreset.DarkFracturedDesert;
        [SerializeField, Range(0f, 1f), Tooltip("Controls restrained per-rock charcoal, blue-gray, and warm-black variation without creating material instances.")]
        private float colorVariation = 0.62f;
        [SerializeField, ColorUsage(false, false), Tooltip("Environmental dust deposited on upward-facing rock surfaces.")]
        private Color environmentDustColor = new Color(0.48f, 0.31f, 0.18f, 1f);

        [NonSerialized] private Mesh generatedMesh;
        [NonSerialized] private string previewStatus = "Waiting for a preview build.";

        public Material RockMaterial => rockMaterial;
        public bool IsFormationMember =>
            GetComponentInParent<TopDown3DRockWorkbenchFormationAuthoring>(true) != null;
        public bool UsesStandaloneMeterScale =>
            !IsFormationMember
            && standalonePhysicalScaleVersion >= CurrentStandalonePhysicalScaleVersion;
        public bool NeedsStandalonePhysicalScaleUpgrade =>
            !IsFormationMember && !UsesStandaloneMeterScale;
        public float PendingStandalonePhysicalScaleFactor =>
            NeedsStandalonePhysicalScaleUpgrade ? LegacyStandalonePhysicalBake : 1f;
        public float VoxelSize => UsesStandaloneMeterScale
            ? Mathf.Max(FinestStandaloneRockVoxelSize, voxelSize)
            : Mathf.Max(FinestVoxelSize, voxelSize);
        public float TessellationDetail => UsesStandaloneMeterScale
            ? StandaloneVoxelSizeToTessellationDetail(VoxelSize)
            : VoxelSizeToTessellationDetail(VoxelSize);
        public float FusionSmoothness => Mathf.Clamp(
            fusionSmoothness,
            0f,
            UsesStandaloneMeterScale ? 0.1f : 1f);
        public float SurfaceRelaxation => Mathf.Clamp01(surfaceRelaxation);
        public bool AutoRebuild => autoRebuild;
        public bool ShowSourceVolumes => showSourceVolumes;
        public bool UpdateCollider => updateCollider;
        public float GeologyScale => Mathf.Clamp(geologyScale, 0.45f, 3f);
        public float SurfaceVariation => Mathf.Clamp01(surfaceVariation);
        public float CrackAmount => Mathf.Clamp01(crackAmount);
        public float SideGrit => Mathf.Clamp01(sideGrit);
        public float UndersideShale => Mathf.Clamp01(undersideShale);
        public float SideShalePatches => Mathf.Clamp01(sideShalePatches);
        public float TopShalePatches => Mathf.Clamp01(topShalePatches);
        public float WornShine => Mathf.Clamp(wornShine, 0f, 0.5f);
        public int GenerationSeed => generationSeed;
        public float GeneratedWidth => UsesStandaloneMeterScale
            ? Mathf.Clamp(
                generatedOverallScale,
                MinimumStandaloneRockWidth,
                MaximumStandaloneRockWidth)
            : Mathf.Clamp(generatedOverallScale, 0.75f, 12f);
        public float GeneratedHeight => UsesStandaloneMeterScale
            ? Mathf.Clamp(
                generatedHeight,
                MinimumStandaloneRockHeight,
                MaximumStandaloneRockHeight)
            : Mathf.Clamp(generatedHeight, 0.5f, 30f);
        public float GeneratedOverallScale => GeneratedWidth;
        public int GeneratedCubeCount => CalculateSourceMassCount(
            GeneratedWidth,
            GeneratedHeight);
        public Vector3 GeneratedOverallSize => new Vector3(
            GeneratedWidth,
            GeneratedHeight,
            GeneratedWidth);
        public float GeneratedVerticality => CalculateAspectVerticality(
            GeneratedWidth,
            GeneratedHeight);
        public float GeneratedAsymmetry => Mathf.Clamp01(generatedAsymmetry);
        public float GeneratedOverlap => Mathf.Clamp01(generatedOverlap);
        public float GeneratedMajorFractures => Mathf.Clamp01(generatedMajorFractures);
        public int GeneratedFractureCount => GeneratedHeight / GeneratedWidth > 4f
            ? 0
            : CalculateFractureCount(GeneratedMajorFractures);
        public float GeneratedEdgeDamage => Mathf.Clamp01(generatedEdgeDamage);
        public TopDown3DRockSilhouetteProfile GeneratedSilhouetteProfile =>
            generatedSilhouetteProfile;
        public TopDown3DRockSurfacePreset SurfacePreset => surfacePreset;
        public float ColorVariation => Mathf.Clamp01(colorVariation);
        public Color EnvironmentDustColor => environmentDustColor;
        public Mesh GeneratedMesh => generatedMesh;
        public string PreviewStatus => previewStatus;

        internal static float CalculateAspectVerticality(float width, float height)
        {
            var aspect = Mathf.Max(0.01f, height) / Mathf.Max(0.01f, width);
            return Mathf.Clamp01(Mathf.InverseLerp(0.35f, 2.5f, aspect));
        }

        public static float TessellationDetailToVoxelSize(float detail)
        {
            var normalized = Mathf.InverseLerp(
                MinimumTessellationDetail,
                MaximumTessellationDetail,
                Mathf.Clamp(detail, MinimumTessellationDetail, MaximumTessellationDetail));
            return CoarsestVoxelSize * Mathf.Pow(FinestVoxelSize / CoarsestVoxelSize, normalized);
        }

        public static float VoxelSizeToTessellationDetail(float size)
        {
            size = Mathf.Clamp(size, FinestVoxelSize, CoarsestVoxelSize);
            var normalized = Mathf.Log(size / CoarsestVoxelSize)
                / Mathf.Log(FinestVoxelSize / CoarsestVoxelSize);
            return Mathf.Lerp(MinimumTessellationDetail, MaximumTessellationDetail, normalized);
        }

        public static float StandaloneTessellationDetailToVoxelSize(float detail)
        {
            var normalized = Mathf.InverseLerp(
                MinimumTessellationDetail,
                MaximumTessellationDetail,
                Mathf.Clamp(detail, MinimumTessellationDetail, MaximumTessellationDetail));
            return CoarsestStandaloneRockVoxelSize * Mathf.Pow(
                FinestStandaloneRockVoxelSize / CoarsestStandaloneRockVoxelSize,
                normalized);
        }

        public static float StandaloneVoxelSizeToTessellationDetail(float size)
        {
            size = Mathf.Clamp(
                size,
                FinestStandaloneRockVoxelSize,
                CoarsestStandaloneRockVoxelSize);
            var normalized = Mathf.Log(size / CoarsestStandaloneRockVoxelSize)
                / Mathf.Log(FinestStandaloneRockVoxelSize / CoarsestStandaloneRockVoxelSize);
            return Mathf.Lerp(MinimumTessellationDetail, MaximumTessellationDetail, normalized);
        }

        public static int CalculateSourceMassCount(float width, float height)
        {
            width = Mathf.Max(0.01f, width);
            height = Mathf.Max(0.01f, height);
            var physicalSpan = Mathf.Sqrt(
                width * width * 0.55f
                + height * height * 0.45f);
            return Mathf.Clamp(
                Mathf.RoundToInt(physicalSpan * 0.22f) + 1,
                2,
                4);
        }

        public static int CalculateFractureCount(float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (amount <= 0.001f) return 0;
            if (amount < 0.46f) return 1;
            return amount < 0.84f ? 2 : 3;
        }

        public void Configure(Material material)
        {
            rockMaterial = material;
        }

        /// <summary>
        /// Applies the user-approved Golden Rock as the starting language for a new standalone
        /// workbench. Generation bakes its resting pose into source geometry, so these values do
        /// not require a rotated or scaled reusable root.
        /// </summary>
        public void ConfigureGoldenRockDefaults()
        {
            generationSeed = GoldenRockSeed;
            generatedOverallSize = new Vector3(
                GoldenRockWidth,
                GoldenRockBodyLength,
                GoldenRockWidth);
            generatedOverallScale = GoldenRockWidth;
            generatedHeight = GoldenRockBodyLength;
            generatedAsymmetry = 0f;
            generatedOverlap = 0f;
            generatedMajorFractures = 0f;
            generatedEdgeDamage = 0.617f;
            generatedSilhouetteProfile = TopDown3DRockSilhouetteProfile.BlockyMonolith;

            voxelSize = 0.05f;
            fusionSmoothness = 0.0657f;
            surfaceRelaxation = 0.45f;
            geologyScale = 2.4f;
            surfaceVariation = 0.346f;
            crackAmount = 0.36f;
            sideGrit = 0.417f;
            undersideShale = 0.905f;
            sideShalePatches = 0f;
            topShalePatches = 0f;
            wornShine = 0.099f;
            surfacePreset = TopDown3DRockSurfacePreset.DarkFracturedDesert;
            colorVariation = 0.62f;
            standalonePhysicalScaleVersion = CurrentStandalonePhysicalScaleVersion;
        }

        public void SetGenerationSeed(int seed)
        {
            generationSeed = seed;
        }

        /// <summary>
        /// Converts an earlier standalone workbench to the accepted physical rock size.
        /// The factor is baked into meter-valued settings once; the reusable root remains unit scale.
        /// </summary>
        public bool ApplyStandalonePhysicalScale()
        {
            if (!NeedsStandalonePhysicalScaleUpgrade) return false;

            generatedOverallSize *= LegacyStandalonePhysicalBake;
            generatedOverallScale *= LegacyStandalonePhysicalBake;
            generatedHeight *= LegacyStandalonePhysicalBake;
            voxelSize *= LegacyStandalonePhysicalBake;
            fusionSmoothness *= LegacyStandalonePhysicalBake;
            standalonePhysicalScaleVersion = CurrentStandalonePhysicalScaleVersion;
            return true;
        }

        public void SetPreviewState(Mesh mesh, string status)
        {
            generatedMesh = mesh;
            previewStatus = string.IsNullOrWhiteSpace(status)
                ? "Preview state was not reported."
                : status;
        }
    }
}
