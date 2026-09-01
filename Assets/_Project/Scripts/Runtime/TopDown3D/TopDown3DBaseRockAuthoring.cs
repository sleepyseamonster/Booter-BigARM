using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Live controls for one procedural base rock in the authoring scene. This is an
    /// independent building block: formations may use these rocks later, but it does
    /// not participate in streamed-world generation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DBaseRockAuthoring : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private Material rockMaterial;

        [Header("Cube Rock Controls")]
        [SerializeField] private int seed = 1357911;
        [SerializeField, Range(1, 16)] private int subdivisions = 5;
        [SerializeField] private Vector3 size = new Vector3(2.4f, 2.1f, 2.4f);
        [SerializeField, Range(0f, 0.45f)] private float surfaceRoughness;
        [SerializeField, Range(0f, 0.35f)] private float axisVariation;
        [SerializeField, HideInInspector] private Mesh generatedMesh;

        public Material RockMaterial => rockMaterial;
        public int Seed => seed;
        public int Subdivisions => Mathf.Clamp(subdivisions, 1, 16);
        public Vector3 Size => new Vector3(
            Mathf.Max(0.1f, size.x),
            Mathf.Max(0.1f, size.y),
            Mathf.Max(0.1f, size.z));
        public float SurfaceRoughness => Mathf.Clamp(surfaceRoughness, 0f, 0.45f);
        public float AxisVariation => Mathf.Clamp(axisVariation, 0f, 0.35f);
        public Mesh GeneratedMesh => generatedMesh;

        public void Configure(Material material)
        {
            rockMaterial = material;
        }

        public void SetSeed(int value)
        {
            seed = value;
        }

        public void SetGeneratedMesh(Mesh mesh)
        {
            generatedMesh = mesh;
        }

        public void CopyControlsFrom(TopDown3DBaseRockAuthoring source)
        {
            if (source == null) return;
            rockMaterial = source.rockMaterial;
            subdivisions = source.subdivisions;
            size = source.size;
            surfaceRoughness = source.surfaceRoughness;
            axisVariation = source.axisVariation;
        }
    }
}
