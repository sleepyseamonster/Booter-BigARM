using UnityEngine;

namespace BooterBigArm.Runtime
{
    /// <summary>
    /// Shared screen-to-world movement math for the protected isometric conversion lane.
    /// The result always lies on the XZ traversal plane and never exceeds the input magnitude.
    /// </summary>
    public static class IsometricMovementBasis
    {
        private const float DirectionEpsilon = 0.0001f;

        public static Vector3 ToWorldDirection(Vector2 input, Vector3 cameraForward, Vector3 cameraRight)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            if (input.sqrMagnitude <= DirectionEpsilon)
            {
                return Vector3.zero;
            }

            var forward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
            if (forward.sqrMagnitude <= DirectionEpsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            var right = Vector3.ProjectOnPlane(cameraRight, Vector3.up);
            if (right.sqrMagnitude <= DirectionEpsilon)
            {
                right = Vector3.Cross(Vector3.up, forward);
            }

            right.Normalize();
            var worldDirection = (right * input.x) + (forward * input.y);
            return Vector3.ClampMagnitude(worldDirection, input.magnitude);
        }
    }
}
