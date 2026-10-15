using UnityEngine;
namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Rendering/Sky Presentation")]
    public sealed class TopDown3DSkyProfile : ScriptableObject
    {
        public Material material;
        public float deepExposure = .65f, brightExposure = 1.05f;
        [Range(.05f, 5f)] public float sunAngularDiameterDegrees = .7f;
        public bool IsValid => material != null && material.shader != null && material.shader.isSupported
            && material.mainTexture != null && material.HasProperty("_SunDirection") && material.HasProperty("_Exposure");
    }
}
