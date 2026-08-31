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
    }
}
