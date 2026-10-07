using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DRockLandmarkArchetype
    {
        SpireComplex
    }

    /// <summary>
    /// Editor-facing parent for composing several editable rock formations into one landmark.
    /// Runtime world generation does not depend on this component; accepted landmark rules can
    /// later be translated into the absolute-space, stable-identity world planner.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DRockLandmarkAuthoring : MonoBehaviour
    {
        [Header("Landmark Generator")]
        [SerializeField, Tooltip("Selects the large-scale formation grammar used by this landmark.")]
        private TopDown3DRockLandmarkArchetype landmarkArchetype =
            TopDown3DRockLandmarkArchetype.SpireComplex;
        [SerializeField, Range(12f, 60f), Tooltip("Approximate horizontal landmark footprint in meters.")]
        private float generatedWidth = 32f;
        [SerializeField, Range(4f, 30f), Tooltip("Approximate height of the tallest landmark formation in meters.")]
        private float generatedHeight = 12f;
        [SerializeField, Range(0f, 1f), Tooltip("Moves the landmark from a balanced radial composition toward an offset, lopsided silhouette.")]
        private float generatedAsymmetry = 0.68f;
        [SerializeField, Range(0f, 1f), Tooltip("Reserves a quieter approach sector through the outer apron. The landmark remains populated on every side.")]
        private float approachOpening = 0.34f;

        [Header("Landmark Preview")]
        [SerializeField] private Material rockMaterial;
        [SerializeField, Tooltip("Reproduces the same formation roles, transforms, dimensions, and child seeds.")]
        private int landmarkSeed = 314159;
        [SerializeField, Tooltip("Uses one seed and world-space origin for rock material patches and long fractures across every child formation.")]
        private bool shareGeologicalField = true;
        [SerializeField, Tooltip("Seats generated child formations against the Landscape Authoring Sandbox terrain when it is available.")]
        private bool conformToTerrain = true;

        [Header("Child Rock Mesh")]
        [SerializeField, Range(0.025f, 0.18f), Tooltip("Surface-grid size copied into every generated child formation. Smaller values are finer and slower.")]
        private float memberVoxelSize = 0.12f;
        [SerializeField, Range(0f, 0.35f), Tooltip("Source-mass smoothing copied into every generated child formation.")]
        private float memberFusionSmoothness = 0.17f;
        [SerializeField, Range(0f, 1f), Tooltip("Finished-surface relaxation copied into every generated child formation.")]
        private float memberSurfaceRelaxation = 0.55f;

        [NonSerialized] private string previewStatus =
            "Generate a landmark or group selected Rock Formation Workbenches.";

        public TopDown3DRockLandmarkArchetype LandmarkArchetype => landmarkArchetype;
        public float GeneratedWidth => Mathf.Clamp(generatedWidth, 12f, 60f);
        public float GeneratedHeight => Mathf.Clamp(generatedHeight, 4f, 30f);
        public float GeneratedAsymmetry => Mathf.Clamp01(generatedAsymmetry);
        public float ApproachOpening => Mathf.Clamp01(approachOpening);
        public Material RockMaterial => rockMaterial;
        public int LandmarkSeed => landmarkSeed;
        public bool ShareGeologicalField => shareGeologicalField;
        public bool ConformToTerrain => conformToTerrain;
        public float MemberVoxelSize => Mathf.Clamp(memberVoxelSize, 0.025f, 0.18f);
        public float MemberFusionSmoothness => Mathf.Clamp(memberFusionSmoothness, 0f, 0.35f);
        public float MemberSurfaceRelaxation => Mathf.Clamp01(memberSurfaceRelaxation);
        public int GeneratedFormationCount => CalculateFormationCount(GeneratedWidth, GeneratedHeight);
        public string PreviewStatus => previewStatus;

        public static int CalculateFormationCount(float width, float height)
        {
            width = Mathf.Clamp(width, 12f, 60f);
            height = Mathf.Clamp(height, 4f, 30f);
            var magnitude = Mathf.Sqrt(width * width * 0.72f + height * height * 0.28f);
            return Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(12f, 29f, Mathf.InverseLerp(12f, 38f, magnitude))),
                12,
                29);
        }

        public void Configure(Material material, int seed)
        {
            rockMaterial = material;
            landmarkSeed = seed;
            shareGeologicalField = true;
        }

        public void ConfigureCaptured(
            Material material,
            int seed,
            float width,
            float height)
        {
            rockMaterial = material;
            landmarkSeed = seed;
            generatedWidth = Mathf.Clamp(width, 12f, 60f);
            generatedHeight = Mathf.Clamp(height, 4f, 30f);
            // Capturing an accepted composition must not silently reproject every child's
            // existing material field. The author can enable a shared field afterward.
            shareGeologicalField = false;
            previewStatus = "Captured the selected formations without regenerating them.";
        }

        public void SetLandmarkSeed(int seed)
        {
            landmarkSeed = seed;
        }

        public void SetPreviewStatus(string status)
        {
            previewStatus = string.IsNullOrWhiteSpace(status)
                ? "Landmark preview state was not reported."
                : status;
        }
    }
}
