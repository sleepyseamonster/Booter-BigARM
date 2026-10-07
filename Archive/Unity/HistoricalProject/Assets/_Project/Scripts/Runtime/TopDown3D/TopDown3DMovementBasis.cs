using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DMovementBasis
    {
        public static Vector3 ToWorldDirection(Vector2 input, Vector3 cameraForward, Vector3 cameraRight)
        {
            var clampedInput = Vector2.ClampMagnitude(input, 1f);
            if (clampedInput.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            var forward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            var right = Vector3.ProjectOnPlane(cameraRight, Vector3.up);
            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
            }

            forward.Normalize();
            right.Normalize();
            return Vector3.ClampMagnitude((forward * clampedInput.y) + (right * clampedInput.x), 1f);
        }
    }
}
