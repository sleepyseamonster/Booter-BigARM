using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Scene-owned elevated framing. The output rig retains legacy serialized fallback values.</summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DElevatedCameraSettings : MonoBehaviour
    {
        [SerializeField] public Vector3 targetOffset = new Vector3(0f, 1.15f, 0f);
        [SerializeField, Range(20f, 75f)] public float pitchDegrees = 50f;
        [SerializeField] public float yawDegrees = 40f;
        [SerializeField, Range(20f, 75f)] public float minimumPitchDegrees = TopDown3DCameraRig.DefaultMinimumPitchDegrees;
        [SerializeField, Range(20f, 75f)] public float maximumPitchDegrees = 65f;
        [SerializeField, Range(30f, 240f)] public float yawSpeedDegrees = 120f;
        [SerializeField, Range(20f, 180f)] public float pitchSpeedDegrees = 70f;
        [SerializeField, Min(0f)] public float mouseSensitivityDegreesPerPixel = 0.30f;
        [SerializeField, Min(2f)] public float distance = 25f;
        [SerializeField, Range(20f, 80f)] public float fieldOfView = 48f;
        [SerializeField, Min(0f)] public float followSmoothTime = 0.14f;
        [SerializeField, Min(0f)] public float maximumLookAheadDistance = TopDown3DCameraRig.DefaultMaximumLookAheadDistance;
        [SerializeField, Min(0f)] public float lookAheadSpeed = TopDown3DCameraRig.DefaultLookAheadSpeed;
        [SerializeField, Min(0f)] public float lookAheadReturnSpeed = TopDown3DCameraRig.DefaultLookAheadReturnSpeed;
        [SerializeField, Min(0.05f)] public float obstructionRadius = 0.35f;
        [SerializeField, Min(1f)] public float minimumDistance = 4f;
        [SerializeField] public LayerMask obstructionMask = ~0;
        internal int Revision { get; private set; }
        private void OnValidate() => Revision++;
    }
}
