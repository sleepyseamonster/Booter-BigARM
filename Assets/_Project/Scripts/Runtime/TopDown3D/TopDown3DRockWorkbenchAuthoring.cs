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

        [NonSerialized] private Mesh generatedMesh;
        [NonSerialized] private string previewStatus = "Waiting for a preview build.";

        public Material RockMaterial => rockMaterial;
        public float VoxelSize => Mathf.Max(0.04f, voxelSize);
        public float FusionSmoothness => Mathf.Clamp(fusionSmoothness, 0f, 1f);
        public bool AutoRebuild => autoRebuild;
        public bool ShowSourceVolumes => showSourceVolumes;
        public bool UpdateCollider => updateCollider;
        public Mesh GeneratedMesh => generatedMesh;
        public string PreviewStatus => previewStatus;

        public void Configure(Material material)
        {
            rockMaterial = material;
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
