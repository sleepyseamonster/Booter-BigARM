using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>One editable box. Its exterior is reconstructed by the Editor, not at runtime.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SmoothRockCube : MonoBehaviour { }
}
