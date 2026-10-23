using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Legger Ride Profile")]
    public sealed class TopDown3DLeggerRideProfile : ScriptableObject
    {
        public float normalSpeed = 10, fastSpeed = 24, acceleration = 16, braking = 24;
        public float slowTurn = 500, fastTurn = 360;
        public float slopeDegrees = 75, stepUp = 1, stepDown = 1.25f, stepSpeed = 2.5f;
        public float fullSpeedSlopeDegrees = 45, steepSlopeSpeedMultiplier = .35f;
        public float reach = 2.4f, entryTimeout = 2, exitSpeed = .15f, skin = .025f;
        public int queryBudget = 256, substepBudget = 8;
        public float substepDistance = .15f;
        public bool Validate(out string reason)
        {
            reason = "Legger riding profile contains unsupported values.";
            foreach (var v in new[] { normalSpeed, fastSpeed, acceleration, braking, slowTurn, fastTurn,
                slopeDegrees, fullSpeedSlopeDegrees, steepSlopeSpeedMultiplier, stepUp, stepDown, stepSpeed, reach, entryTimeout, exitSpeed, skin, substepDistance })
                if (!float.IsFinite(v) || v <= 0) return false;
            if (normalSpeed > fastSpeed || fastSpeed > 24 || acceleration > 40 || braking > 40
                || slowTurn > 600 || fastTurn > slowTurn || slopeDegrees > 75 || fullSpeedSlopeDegrees > 75
                || steepSlopeSpeedMultiplier > 1 || stepUp > 1
                || stepDown > 1.25f || stepSpeed > 3 || reach > 3 || entryTimeout > 5 || exitSpeed > .2f
                || skin > .05f || substepDistance > .15f || queryBudget < 32 || queryBudget > 256
                || substepBudget < 1 || substepBudget > 8) return false;
            reason = null; return true;
        }
        public float SpeedMultiplierForSlope(Vector3 normal)
        {
            float degrees = Vector3.Angle(normal, Vector3.up);
            if (degrees <= fullSpeedSlopeDegrees) return 1;
            float end = Mathf.Max(fullSpeedSlopeDegrees + .01f, slopeDegrees);
            return Mathf.Lerp(1, steepSlopeSpeedMultiplier, Mathf.InverseLerp(fullSpeedSlopeDegrees, end, degrees));
        }
    }
}
