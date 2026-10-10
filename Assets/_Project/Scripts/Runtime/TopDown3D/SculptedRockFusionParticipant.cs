using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Authored rock membership only. Live unions are computed by editor tooling.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Booter & BigARM/Art/Sculpted Rock Live Fusion")]
    public sealed class SculptedRockFusionParticipant : MonoBehaviour
    {
        [SerializeField, Tooltip("Preview an exterior union with overlapping sculpted rocks in Edit Mode. Sources retain editable transforms.")]
        private bool liveFusion = true;
        [SerializeField, Tooltip("Only rocks in the same scene with the same set number fuse together.")]
        private int fusionSet;
        public bool LiveFusion => liveFusion && enabled;
        public int FusionSet => fusionSet;
    }
}
