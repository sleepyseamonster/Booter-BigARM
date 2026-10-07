using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class IsometricOccluder : MonoBehaviour
    {
        [SerializeField] private Renderer[] affectedRenderers;

        public void Configure(Renderer[] renderers)
        {
            affectedRenderers = renderers;
        }

        public void SetOccluded(bool occluded)
        {
            EnsureRenderers();
            for (var i = 0; i < affectedRenderers.Length; i++)
            {
                if (affectedRenderers[i] != null)
                {
                    affectedRenderers[i].forceRenderingOff = occluded;
                }
            }
        }

        private void OnDisable()
        {
            SetOccluded(false);
        }

        private void EnsureRenderers()
        {
            if (affectedRenderers == null || affectedRenderers.Length == 0)
            {
                affectedRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }
    }
}
