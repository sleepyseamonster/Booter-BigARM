using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Editor-authoring root for experimenting with a rock as a union of editable box volumes.
    /// This preview is intentionally independent from streamed procedural-world generation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class TopDown3DRockWorkbenchAuthoring : MonoBehaviour
    {
        [Header("Preview")]
        [SerializeField] private Material rockMaterial;
        [SerializeField, Min(0.04f), Tooltip("Smaller voxels make a more detailed surface but take longer to rebuild.")]
        private float voxelSize = 0.18f;
        [SerializeField, Range(0f, 1f), Tooltip("Rounds and thickens the join between nearby cube volumes. Zero produces a hard union.")]
        private float fusionSmoothness = 0.16f;
        [SerializeField, Tooltip("Rebuild the preview shortly after a source cube or setting changes.")]
        private bool autoRebuild = true;
        [SerializeField, Tooltip("Show selectable wire boxes for the source cube volumes in the Scene view.")]
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
        [SerializeField, Range(0f, 0.5f), Tooltip("Additional highlight response on smoother worn patches. The rock remains non-metallic.")]
        private float wornShine = 0.2f;

        [Header("Base Rock Generator")]
        [SerializeField, Tooltip("Regenerating this seed reproduces the same editable cube arrangement.")]
        private int generationSeed = 1729;
        // Retained so existing scenes and prefabs keep their previous size data.
        [SerializeField, HideInInspector]
        private Vector3 generatedOverallSize = new Vector3(4f, 3f, 3.5f);
        [SerializeField, Range(0.75f, 10f), InspectorName("Overall Size"), Tooltip("Uniformly scales the generated rock. Larger rocks automatically use more cube masses for richer silhouettes.")]
        private float generatedOverallScale = 4f;
        [SerializeField, Range(0f, 1f), Tooltip("Low values spread the rock horizontally. High values build a tapered upward spine.")]
        private float generatedVerticality = 0.45f;
        [SerializeField, Range(0f, 1f), InspectorName("Lopsidedness"), Tooltip("Zero distributes masses around the core. One drives growth strongly toward one side with greater size and tilt variation.")]
        private float generatedAsymmetry = 0.65f;
        [SerializeField, Range(0f, 1f), InspectorName("Compaction"), Tooltip("Zero preserves distinct overlapping lobes. One pulls the masses tightly together into a dense fused body.")]
        private float generatedOverlap = 0.62f;

        [NonSerialized] private Mesh generatedMesh;
        [NonSerialized] private string previewStatus = "Waiting for a preview build.";

        public Material RockMaterial => rockMaterial;
        public float VoxelSize => Mathf.Max(0.04f, voxelSize);
        public float FusionSmoothness => Mathf.Clamp(fusionSmoothness, 0f, 1f);
        public bool AutoRebuild => autoRebuild;
        public bool ShowSourceVolumes => showSourceVolumes;
        public bool UpdateCollider => updateCollider;
        public float GeologyScale => Mathf.Clamp(geologyScale, 0.45f, 3f);
        public float SurfaceVariation => Mathf.Clamp01(surfaceVariation);
        public float CrackAmount => Mathf.Clamp01(crackAmount);
        public float WornShine => Mathf.Clamp(wornShine, 0f, 0.5f);
        public int GenerationSeed => generationSeed;
        public float GeneratedOverallScale => Mathf.Clamp(generatedOverallScale, 0.75f, 10f);
        public int GeneratedCubeCount => Mathf.Clamp(
            Mathf.CeilToInt(GeneratedOverallScale * 0.9f) + 1,
            2,
            10);
        public Vector3 GeneratedOverallSize => Vector3.one * GeneratedOverallScale;
        public float GeneratedVerticality => Mathf.Clamp01(generatedVerticality);
        public float GeneratedAsymmetry => Mathf.Clamp01(generatedAsymmetry);
        public float GeneratedOverlap => Mathf.Clamp01(generatedOverlap);
        public Mesh GeneratedMesh => generatedMesh;
        public string PreviewStatus => previewStatus;

        public void Configure(Material material)
        {
            rockMaterial = material;
        }

        public void SetGenerationSeed(int seed)
        {
            generationSeed = seed;
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
