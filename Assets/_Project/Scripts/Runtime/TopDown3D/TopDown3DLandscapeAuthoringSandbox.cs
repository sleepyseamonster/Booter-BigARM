using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Stores the deterministic terrain context for a dedicated landscape-authoring
    /// scene. Its editor companion builds a disposable terrain view from the same
    /// terrain generator used by the streamed world.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DLandscapeAuthoringSandbox : MonoBehaviour
    {
        [SerializeField] private TopDown3DWorldSettings worldSettings;
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private Material regularRockMaterial;
        [SerializeField] private Vector2Int centerChunk;
        [SerializeField, Range(0, 2)] private int terrainRadiusInChunks = 1;
        [SerializeField] private GameObject rockReference;
        [SerializeField, Range(0f, 1f)] private float rockBurial = 0.035f;
        [SerializeField, Range(0f, 1f)] private float maximumRockBurial = 0.6f;
        [SerializeField, HideInInspector] private int burialRangeVersion;
        [SerializeField, Range(0f, 35f)] private float maximumRockTilt = 20f;

        public GameObject RockReference => rockReference;
        public float RockBurial => Mathf.Clamp01(rockBurial);
        public float MaximumRockBurial => Mathf.Clamp(
            burialRangeVersion == 0 ? Mathf.Max(0.6f, maximumRockBurial) : maximumRockBurial, RockBurial, 1f);
        public float MaximumRockTilt => Mathf.Clamp(maximumRockTilt, 0f, 35f);

        public TopDown3DWorldSettings WorldSettings => worldSettings;
        public Material TerrainMaterial => terrainMaterial;
        public Material RegularRockMaterial => regularRockMaterial;
        public Vector2Int CenterChunk => centerChunk;
        public int TerrainRadiusInChunks => terrainRadiusInChunks;

        public void Configure(
            TopDown3DWorldSettings settings,
            Material groundMaterial,
            Material rockMaterial,
            Vector2Int initialCenterChunk)
        {
            worldSettings = settings;
            terrainMaterial = groundMaterial;
            regularRockMaterial = rockMaterial;
            centerChunk = initialCenterChunk;
            terrainRadiusInChunks = Mathf.Clamp(terrainRadiusInChunks, 0, 2);
        }

        public void ConfigureRockReference(GameObject reference)
        {
            rockReference = reference;
            UpgradeBurialRange();
        }

        private void OnValidate()
        {
            if (rockReference != null) UpgradeBurialRange();
        }

        private void UpgradeBurialRange()
        {
            if (burialRangeVersion != 0) return;
            // Apply the requested deeper default to existing setups as well as new ones.
            // Preserve the shallow endpoint, and never repeat this after the user tunes it.
            maximumRockBurial = Mathf.Max(0.6f, maximumRockBurial);
            burialRangeVersion = 1;
        }
    }
}
