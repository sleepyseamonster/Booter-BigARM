using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Keeps the bounded Badwater review scene visible beyond the normal play-world horizon.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(500)]
    public sealed class BadwaterCameraRange : MonoBehaviour
    {
        [SerializeField, Min(1300f)] private float farClipPlane = 8000f;
        private Camera outputCamera;

        public float FarClipPlane => farClipPlane;

        public void Configure(float distance)
        {
            farClipPlane = Mathf.Max(1300f, distance);
            Apply();
        }

        private void OnEnable() => Apply();
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (outputCamera == null) outputCamera = GetComponent<Camera>();
            if (outputCamera != null && !Mathf.Approximately(outputCamera.farClipPlane, farClipPlane))
                outputCamera.farClipPlane = farClipPlane;
        }
    }
}
