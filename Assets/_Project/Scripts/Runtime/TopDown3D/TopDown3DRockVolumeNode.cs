using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// One editable source volume used by a parent rock workbench. Its unit-cube Transform
    /// controls the position, rotation, and size of a deterministic rock-shaped mass; the
    /// object does not render its own mesh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DRockVolumeNode : MonoBehaviour
    {
        [SerializeField, Tooltip("When disabled, this source cube remains in the hierarchy but is omitted from the fused preview.")]
        private bool contributesToRock = true;
        [SerializeField, HideInInspector]
        private int shapeSeed;

        public bool ContributesToRock => contributesToRock;
        public int ShapeSeed => shapeSeed;

        public void SetShapeSeed(int seed)
        {
            shapeSeed = seed;
        }
    }
}
