using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // One continuous source clock through ascent and descent. The shared apex
    // derivative avoids a source-speed discontinuity when physical vertical velocity changes sign.
    public static class AnimationPlaygroundJumpTiming
    {
        public const float AnticipationSeconds=.08f,SoftLandingSeconds=.22f,InputGraceSeconds=.1f;
        // Pose entry may finish just after lift without delaying the physical impulse.
        public const float EntryBlendSeconds=.12f;
        public static double ImpulseMarkerSeconds(float fixedStep)
        {
            if(!float.IsFinite(fixedStep) || fixedStep<=0)throw new ArgumentException("A positive simulation step is required.");
            // Treat a ratio within ten millionths of an integer as that tick;
            // serialized float drift must not add a whole extra physics frame.
            return Math.Max(1,Math.Ceiling(AnticipationSeconds/(double)fixedStep-.00001))*fixedStep;
        }
        public static float FlightSourceSeconds(AnimationPlaygroundJumpVariant value,float phase,float physicalHalfFlightSeconds=.46890927f)
        {
            if(value==null || !float.IsFinite(phase+physicalHalfFlightSeconds) || physicalHalfFlightSeconds<=0
                || value.apexTime<=value.liftTime || value.contactTime<=value.apexTime)
                throw new ArgumentException("A finite jump phase and ordered source landmarks are required.");
            phase=Mathf.Clamp01(phase);
            var rise=value.apexTime-value.liftTime;var fall=value.contactTime-value.apexTime;
            var shared=2*rise*fall/(rise+fall);
            var endpoint=Mathf.Min(physicalHalfFlightSeconds,2*Mathf.Min(rise,fall));
            return phase<=.5f
                ? Hermite(value.liftTime,value.apexTime,endpoint,shared,phase*2)
                : Hermite(value.apexTime,value.contactTime,shared,endpoint,(phase-.5f)*2);
        }
        private static float Hermite(float from,float to,float startSlope,float endSlope,float t)
        {
            var t2=t*t;var t3=t2*t;
            return (2*t3-3*t2+1)*from+(t3-2*t2+t)*startSlope+(-2*t3+3*t2)*to+(t3-t2)*endSlope;
        }
        public static float TimeToContact(float gap,float downwardSpeed,float gravityMagnitude)
        {
            if(!float.IsFinite(gap+downwardSpeed+gravityMagnitude) || gap<0 || gravityMagnitude<=0)
                throw new ArgumentException("A finite nonnegative gap and positive gravity are required.");
            return (Mathf.Sqrt(downwardSpeed*downwardSpeed+2*gravityMagnitude*gap)-downwardSpeed)/gravityMagnitude;
        }
    }
}
