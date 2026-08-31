using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Scene data for a deliberately authored rock formation. The editor realizes a
    /// selected deterministic formation as normal child GameObjects, which can then
    /// be adjusted with Unity's regular scene tools or made into a prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DRockFormationAuthoring : MonoBehaviour
    {
        [SerializeField] private TopDown3DWorldSettings worldSettings;
        [SerializeField] private Material regularRockMaterial;
        [SerializeField] private int sourceWorldSeed;
        [SerializeField] private Vector2 sourceWorldPosition;
        [SerializeField, Range(0, 8)] private int searchRadiusInChunks = 1;
        [SerializeField] private int selectedFormationIndex;
        [SerializeField] private string selectedFormationStableId;
        [SerializeField, HideInInspector] private int randomSelectionIndex;
        [SerializeField, HideInInspector] private Transform generatedMembersRoot;

        public TopDown3DWorldSettings WorldSettings => worldSettings;
        public Material RegularRockMaterial => regularRockMaterial;
        public int SourceWorldSeed => sourceWorldSeed;
        public Vector2 SourceWorldPosition => sourceWorldPosition;
        public int SearchRadiusInChunks => searchRadiusInChunks;
        public int SelectedFormationIndex => selectedFormationIndex;
        public string SelectedFormationStableId => selectedFormationStableId;
        public int RandomSelectionIndex => randomSelectionIndex;
        public Transform GeneratedMembersRoot => generatedMembersRoot;

        public void Configure(
            TopDown3DWorldSettings settings,
            Material rockMaterial,
            Vector2 sourcePosition)
        {
            worldSettings = settings;
            regularRockMaterial = rockMaterial;
            sourceWorldSeed = settings != null ? settings.WorldSeed : 0;
            sourceWorldPosition = sourcePosition;
            searchRadiusInChunks = Mathf.Clamp(searchRadiusInChunks, 0, 8);
        }

        public void SetSelectedFormation(int index, string stableId)
        {
            selectedFormationIndex = Mathf.Max(0, index);
            selectedFormationStableId = stableId ?? string.Empty;
        }

        public void SetSourceWorldSeed(int seed)
        {
            sourceWorldSeed = seed;
        }

        public void AdvanceRandomSelection()
        {
            randomSelectionIndex = randomSelectionIndex == int.MaxValue
                ? 0
                : randomSelectionIndex + 1;
        }

        public void SetGeneratedMembersRoot(Transform root)
        {
            generatedMembersRoot = root;
        }
    }
}
