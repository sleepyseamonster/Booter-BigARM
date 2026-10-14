using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Legger Ride Profile")]
    public sealed class TopDown3DLeggerRideProfile : ScriptableObject
    {
        public float normalSpeed = 6, fastSpeed = 10, acceleration = 16, braking = 24;
        public float slowTurn = 500, fastTurn = 360;
        public float slopeDegrees = 55, stepUp = 1, stepDown = 1.25f, stepSpeed = 2.5f;
        public float reach = 2.4f, entryTimeout = 2, exitSpeed = .15f, skin = .025f;
        public int queryBudget = 256, substepBudget = 8;
        public float substepDistance = .15f;
        public bool Validate(out string reason)
        {
            reason = "Legger riding profile contains unsupported values.";
            foreach (var v in new[] { normalSpeed, fastSpeed, acceleration, braking, slowTurn, fastTurn,
                slopeDegrees, stepUp, stepDown, stepSpeed, reach, entryTimeout, exitSpeed, skin, substepDistance })
                if (!float.IsFinite(v) || v <= 0) return false;
            if (normalSpeed > fastSpeed || fastSpeed > 12 || acceleration > 40 || braking > 40
                || slowTurn > 600 || fastTurn > slowTurn || slopeDegrees > 55 || stepUp > 1
                || stepDown > 1.25f || stepSpeed > 3 || reach > 3 || entryTimeout > 5 || exitSpeed > .2f
                || skin > .05f || substepDistance > .15f || queryBudget < 32 || queryBudget > 256
                || substepBudget < 1 || substepBudget > 8) return false;
            reason = null; return true;
        }
    }
}
