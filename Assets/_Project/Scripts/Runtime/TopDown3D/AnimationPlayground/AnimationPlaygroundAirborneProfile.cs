using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable] public sealed class AnimationPlaygroundJumpVariant
    {
        public string sourceId;
        public AnimationClip takeoff,rising,landing;
        public float upwardSpeed=4.6f;
    }
    public sealed class AnimationPlaygroundAirborneProfile : ScriptableObject
    {
        public int schema=1;
        public bool qualified;
        public AnimationPlaygroundJumpVariant standing,running,unarmed;
        public AnimationClip falling,approach,hardLanding;
        public float runningThreshold=.6f,airControlAcceleration=3,maximumPlanarSpeed=3.8f;
        public float anticipationDistance=.65f,contactDistance=.03f,hardLandingSpeed=6.5f;
        public bool Validate(out string reason,bool requireQualified=true)
        {
            reason=null;
            if(schema!=1 || !Valid(standing) || !Valid(running) || !Valid(unarmed)
                || !Human(falling) || !falling.isLooping || !Human(approach) || !Human(hardLanding))
            {reason="A complete paired airborne profile is required.";return false;}
            foreach(var value in new[]{runningThreshold,airControlAcceleration,maximumPlanarSpeed,anticipationDistance,contactDistance,hardLandingSpeed})
                if(!float.IsFinite(value) || value<=0){reason="Airborne calibration must be finite and positive.";return false;}
            if(contactDistance>=anticipationDistance || maximumPlanarSpeed>12 || airControlAcceleration>20)
            {reason="Airborne movement or contact calibration exceeds its reviewed range.";return false;}
            if(requireQualified && !qualified){reason="Jump and landing qualification is pending.";return false;}
            return true;
        }
        private static bool Valid(AnimationPlaygroundJumpVariant value)=>value!=null && !string.IsNullOrWhiteSpace(value.sourceId)
            && Human(value.takeoff) && Human(value.rising) && Human(value.landing)
            && !value.takeoff.isLooping && !value.rising.isLooping && !value.landing.isLooping
            && float.IsFinite(value.upwardSpeed) && value.upwardSpeed>0 && value.upwardSpeed<=12;
        private static bool Human(AnimationClip clip)=>clip!=null && clip.isHumanMotion && clip.length>0;
    }
}
