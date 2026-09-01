using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// One editable unit-box volume used by a parent rock workbench. Its Transform controls
    /// the box position, rotation, and size; the object does not render its own mesh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DRockVolumeNode : MonoBehaviour
    {
        [SerializeField, Tooltip("When disabled, this source cube remains in the hierarchy but is omitted from the fused preview.")]
        private bool contributesToRock = true;

        public bool ContributesToRock => contributesToRock;
    }
}
