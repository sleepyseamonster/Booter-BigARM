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
        [SerializeField, Range(0f, 0.15f)] private float rockBurial = 0.035f;
        [SerializeField, Range(0f, 0.3f)] private float maximumRockBurial = 0.15f;
        [SerializeField, Range(0f, 35f)] private float maximumRockTilt = 20f;

        public GameObject RockReference => rockReference;
        public float RockBurial => Mathf.Clamp(rockBurial, 0f, 0.15f);
        public float MaximumRockBurial => Mathf.Clamp(maximumRockBurial, RockBurial, 0.3f);
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
        }
    }
}
