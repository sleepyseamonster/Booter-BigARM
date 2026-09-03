using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DRockSourceShape
    {
        WeatheredBlock,
        Wedge,
        TaperedStone
    }

    /// <summary>
    /// One editable source volume used by a parent rock workbench. Its Transform controls
    /// the position, rotation, and size of a deterministic rock-shaped mass; the
    /// object does not render its own mesh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DRockVolumeNode : MonoBehaviour
    {
        [SerializeField, Tooltip("When disabled, this source volume remains in the hierarchy but is omitted from the fused preview.")]
        private bool contributesToRock = true;
        [SerializeField, InspectorName("Source Shape"), Tooltip("Controls the implicit stone volume contributed to the parent rock.")]
        private TopDown3DRockSourceShape sourceShape = TopDown3DRockSourceShape.WeatheredBlock;
        [SerializeField, HideInInspector]
        private int shapeSeed;

        public bool ContributesToRock => contributesToRock;
        public TopDown3DRockSourceShape SourceShape => sourceShape;
        public int ShapeSeed => shapeSeed;

        public void SetSourceShape(TopDown3DRockSourceShape shape)
        {
            sourceShape = shape;
        }

        public void SetShapeSeed(int seed)
        {
            shapeSeed = seed;
        }
    }
}
