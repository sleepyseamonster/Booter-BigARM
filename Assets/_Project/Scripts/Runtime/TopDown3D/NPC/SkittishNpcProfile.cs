using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class NpcTuning
    {
        public float detection = 20, calmDistance = 16, comfort = 10, panicRelease = 7, panic = 5;
        public float alertSeconds = .35f, memorySeconds = 2, calmSeconds = 3, maximumVerticalDistance = 4;
        public float retreatSpeed = 4.5f, fleeSpeed = 7.5f, acceleration = 12, braking = 18, turnDegrees = 300;
        public float perceptionSeconds = .2f, decisionSeconds = .1f, replanSeconds = .5f, commitmentSeconds = .6f;
        public float slopeDegrees = 35, stepUp = .35f, stepDown = .45f;
        public float stuckSeconds = 1.25f, progressMetres = .15f, switchMargin = .1f;
        public NpcTuning Copy() => (NpcTuning)MemberwiseClone();
        public bool Validate(out string reason)
        {
            reason = "Invalid NPC tuning; retain the last valid snapshot.";
            foreach (var field in typeof(NpcTuning).GetFields())
            {
                var value = (float)field.GetValue(this);
                if (!float.IsFinite(value) || value <= 0) return false;
            }
            if (!(detection > calmDistance && calmDistance > comfort && comfort > panicRelease && panicRelease > panic)
                || detection > 40 || retreatSpeed > 10 || fleeSpeed < retreatSpeed || fleeSpeed > 12
                || acceleration > 30 || braking > 40 || turnDegrees > 720 || slopeDegrees > 45
                || stepUp > .6f || stepDown > .6f || maximumVerticalDistance > 8
                || perceptionSeconds < .05f || decisionSeconds < .05f || replanSeconds < .25f
                || commitmentSeconds > 5 || switchMargin > 1 || progressMetres > 1) return false;
            reason = null; return true;
        }
    }

    [CreateAssetMenu(menuName = "Booter & BigARM/NPC/Skittish Profile")]
    public sealed class SkittishNpcProfile : ScriptableObject
    {
        [SerializeField] private NpcTuning tuning = new();
        public NpcTuning Snapshot() => tuning?.Copy();
    }
}
