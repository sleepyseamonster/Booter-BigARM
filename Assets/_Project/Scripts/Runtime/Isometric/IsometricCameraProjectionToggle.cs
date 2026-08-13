using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class IsometricCameraProjectionToggle : MonoBehaviour
    {
        [SerializeField] private Camera outputCamera;
        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField, Min(0.1f)] private float orthographicSize = 8f;
        [SerializeField, Range(10f, 80f)] private float perspectiveFieldOfView = 48f;
        [SerializeField] private bool startOrthographic = true;

        public bool IsOrthographic { get; private set; }
        public string ProjectionLabel => IsOrthographic ? "Orthographic" : "Mild perspective";

        public void Configure(Camera camera, CinemachineCamera cineCamera)
        {
            outputCamera = camera;
            virtualCamera = cineCamera;
            Apply(startOrthographic);
        }

        public void ToggleProjection()
        {
            Apply(!IsOrthographic);
        }

        private void Awake()
        {
            Apply(startOrthographic);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                ToggleProjection();
            }
        }

        private void Apply(bool orthographic)
        {
            IsOrthographic = orthographic;
            if (outputCamera != null)
            {
                outputCamera.orthographic = orthographic;
                outputCamera.orthographicSize = orthographicSize;
                outputCamera.fieldOfView = perspectiveFieldOfView;
            }

            if (virtualCamera == null)
            {
                return;
            }

            var lens = virtualCamera.Lens;
            lens.ModeOverride = orthographic
                ? LensSettings.OverrideModes.Orthographic
                : LensSettings.OverrideModes.Perspective;
            lens.OrthographicSize = orthographicSize;
            lens.FieldOfView = perspectiveFieldOfView;
            virtualCamera.Lens = lens;
        }
    }
}
