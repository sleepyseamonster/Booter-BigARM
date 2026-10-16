using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable] public sealed class AnimationPlaygroundJumpVariant
    {
        public string sourceId;
        public AnimationClip takeoff,rising,landing;
        public float upwardSpeed=4.6f;
        public AnimationClip continuous;
        public float liftTime,apexTime,contactTime;
        public bool HasContinuousSource=>continuous!=null && continuous.isHumanMotion && !continuous.isLooping
            && float.IsFinite(liftTime+apexTime+contactTime) && liftTime>0 && liftTime<=.10001f
            && apexTime>liftTime && contactTime>apexTime && continuous.length>contactTime;
    }
    public sealed class AnimationPlaygroundAirborneProfile : ScriptableObject
    {
        public int schema=1;
        public bool qualified;
        public AnimationPlaygroundJumpVariant standing,running,unarmed;
        public AnimationClip falling,approach,hardLanding;
        public float runningThreshold=.6f,airControlAcceleration=3,maximumPlanarSpeed=3.8f;
        public float gravityScale=1;
        public float anticipationDistance=.65f,contactDistance=.03f,hardLandingSpeed=6.5f;
        public float landingDamagePerSpeed=10f;
        public float LandingDamage(float downwardSpeed)=>float.IsFinite(downwardSpeed)
            ? Mathf.Max(0,downwardSpeed-hardLandingSpeed)*landingDamagePerSpeed : 0;
        public bool Validate(out string reason,bool requireQualified=true)
        {
            reason=null;
            if(schema!=1 && schema!=2 || !Valid(standing) || !Valid(running) || !Valid(unarmed)
                || !Human(falling) || !falling.isLooping || !Human(approach) || !Human(hardLanding))
            {reason="A complete paired airborne profile is required.";return false;}
            if(schema==2 && (!standing.HasContinuousSource || !running.HasContinuousSource || !unarmed.HasContinuousSource))
            {reason="The continuous jump sources and ordered landmarks are required.";return false;}
            foreach(var value in new[]{runningThreshold,airControlAcceleration,maximumPlanarSpeed,anticipationDistance,contactDistance,hardLandingSpeed,landingDamagePerSpeed})
                if(!float.IsFinite(value) || value<=0){reason="Airborne calibration must be finite and positive.";return false;}
            if(!float.IsFinite(gravityScale) || gravityScale<1 || gravityScale>3)
            {reason="Jump gravity must be between one and three times world gravity.";return false;}
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
