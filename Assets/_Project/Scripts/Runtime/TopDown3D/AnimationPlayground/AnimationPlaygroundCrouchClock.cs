using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct AnimationPlaygroundCrouchPose
    {
        public readonly Vector2 Direction;
        public readonly float MovingWeight,Phase,IdleSeconds;
        public readonly bool Supported;
        public AnimationPlaygroundCrouchPose(Vector2 direction,float movingWeight,float phase,float idleSeconds,bool supported)
        {Direction=direction;MovingWeight=movingWeight;Phase=phase;IdleSeconds=idleSeconds;Supported=supported;}
    }
    // Sustained posture has no finite action quota. Contact crossings derive from one fixed-step semantic clock.
    public sealed class AnimationPlaygroundCrouchClock
    {
        private double cycles,idleSeconds;
        public AnimationPlaygroundCrouchPose Pose {get;private set;}
        public event Action<TopDown3DFootSide> Contact;
        public void Reset(){cycles=idleSeconds=0;Pose=default;}
        public void Advance(float seconds,Vector3 velocity,Vector3 facing,float nominalSpeed,float nominalCycleSeconds,bool supported,bool paused)
        {
            if(!float.IsFinite(seconds) || seconds<0 || seconds>1 || !Finite(velocity) || !Finite(facing) || !float.IsFinite(nominalSpeed) || nominalSpeed<=0
                || !float.IsFinite(nominalCycleSeconds) || nominalCycleSeconds<.1f)throw new ArgumentException("Crouch clock inputs must be finite and calibrated fixed steps.");
            facing=Vector3.ProjectOnPlane(facing,Vector3.up);
            if(facing.sqrMagnitude<.0001f)throw new ArgumentException("Crouch facing is required.");
            facing.Normalize();var right=Vector3.Cross(Vector3.up,facing);var planar=Vector3.ProjectOnPlane(velocity,Vector3.up);
            var speed=planar.magnitude;var direction=new Vector2(Vector3.Dot(planar,right),Vector3.Dot(planar,facing));
            if(direction.sqrMagnitude>.0001f)direction.Normalize();else direction=Vector2.zero;
            if(paused)return;
            idleSeconds+=seconds;
            if(supported && speed>.08f)
            {
                var before=cycles;cycles+=seconds/nominalCycleSeconds*Mathf.Clamp(speed/nominalSpeed,.5f,1.5f);
                var first=(long)Math.Floor(before*4)+1;var last=(long)Math.Floor(cycles*4);
                for(var marker=first;marker<=last;marker++)
                {
                    Pose=new AnimationPlaygroundCrouchPose(direction,Mathf.Clamp01(speed/.2f),(float)((marker%4)*.25), (float)(idleSeconds%3600),true);
                    Contact?.Invoke((marker&1)==0 ? TopDown3DFootSide.Right : TopDown3DFootSide.Left);
                }
            }
            Pose=new AnimationPlaygroundCrouchPose(direction,Mathf.Clamp01(speed/.2f),(float)(cycles-Math.Floor(cycles)),(float)(idleSeconds%3600),supported);
        }
        private static bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
    }
}
