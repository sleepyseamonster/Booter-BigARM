using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DThirdPersonCameraSettings : MonoBehaviour
    {
        public TopDown3DThirdPersonCameraProfile profile;
        [Range(55, 75)] public float fieldOfView = 60;
    }
}
