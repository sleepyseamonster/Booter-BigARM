using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Booter & BigARM/Camp Look")]
    public sealed class TopDown3DCampLook : MonoBehaviour
    {
        public Camera gameCamera;
        public PerpetualTwilightSun sun;
        public bool previewInEditor = true;
        public bool previewSceneViewEffects = true;
    }
}
