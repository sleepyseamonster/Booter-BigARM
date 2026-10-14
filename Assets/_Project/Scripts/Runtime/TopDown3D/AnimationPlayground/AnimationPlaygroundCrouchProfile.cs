using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundCrouchRole { Enter, Idle, Forward, Backward, Left, Right, Exit }
    [Serializable]
    public sealed class AnimationPlaygroundCrouchEntry
    {
        public AnimationPlaygroundCrouchRole role;
        public AnimationClip clip;
        public bool looping;
        public float nominalSpeed;
        public float sourcePhaseOffset;
        public AnimationCurve heightCorrection;
        public AnimationCurve capsuleHeight;
        public float[] leftContacts=Array.Empty<float>(),rightContacts=Array.Empty<float>();
        public float Correction(float phase)=>heightCorrection?.Evaluate(Mathf.Clamp01(phase)) ?? 0;
    }
    public sealed class AnimationPlaygroundCrouchProfile : ScriptableObject
    {
        public int schema=1;
        public float standingHeight=1.82f,crouchHeight=1.52f;
        public bool contactsQualified;
        public AnimationPlaygroundCrouchEntry[] entries=Array.Empty<AnimationPlaygroundCrouchEntry>();
        public AnimationPlaygroundCrouchEntry Get(AnimationPlaygroundCrouchRole role)
        {foreach(var entry in entries)if(entry!=null && entry.role==role)return entry;return null;}
        public bool Validate(out string reason,bool requirePhysical=true)
        {
            reason=null;
            if(schema!=1 || !float.IsFinite(standingHeight) || !float.IsFinite(crouchHeight) || crouchHeight<=.4f || crouchHeight>=standingHeight
                || entries==null || entries.Length!=7){reason="A complete calibrated seven-role crouch profile is required.";return false;}
            var roles=new bool[7];
            foreach(var entry in entries)
            {
                if(entry==null || !Enum.IsDefined(typeof(AnimationPlaygroundCrouchRole),entry.role) || roles[(int)entry.role]
                    || entry.clip==null || !entry.clip.isHumanMotion || entry.clip.length<=0){reason="Crouch roles require distinct Humanoid clips.";return false;}
                roles[(int)entry.role]=true;
                var expectedLoop=entry.role!=AnimationPlaygroundCrouchRole.Enter && entry.role!=AnimationPlaygroundCrouchRole.Exit;
                if(entry.looping!=expectedLoop || expectedLoop && !entry.clip.isLooping){reason="Crouch loop and transition policies differ.";return false;}
                if(!CurveFinite(entry.heightCorrection,0,.18f) || !CurveFinite(entry.capsuleHeight,crouchHeight,standingHeight))
                {reason="Crouch calibration curves exceed their measured envelope.";return false;}
                if(entry.role>=AnimationPlaygroundCrouchRole.Forward && entry.role<=AnimationPlaygroundCrouchRole.Right)
                {
                    if(!float.IsFinite(entry.nominalSpeed) || entry.nominalSpeed<=0 || !float.IsFinite(entry.sourcePhaseOffset)
                        || entry.sourcePhaseOffset<0 || entry.sourcePhaseOffset>=1){reason="Each directional crouch gait needs a measured speed and phase origin.";return false;}
                    if(requirePhysical && !OrderedContacts(entry.rightContacts,entry.leftContacts)){reason="Crouch contacts require cyclic R-L-R-L order.";return false;}
                }
            }
            if(requirePhysical && !contactsQualified){reason="Crouch motion/contact qualification is pending.";return false;}
            return true;
        }
        public static bool OrderedContacts(float[] right,float[] left)
            => right!=null && left!=null && right.Length==2 && left.Length==2
                && float.IsFinite(right[0]) && float.IsFinite(right[1]) && float.IsFinite(left[0]) && float.IsFinite(left[1])
                && right[0]>=0 && right[0]<left[0] && left[0]<right[1] && right[1]<left[1] && left[1]<1;
        private static bool CurveFinite(AnimationCurve curve,float min,float max)
        {
            if(curve==null || curve.length<2)return false;
            foreach(var key in curve.keys)if(!float.IsFinite(key.time) || !float.IsFinite(key.value) || key.time<0 || key.time>1 || key.value<min-.0001f || key.value>max+.0001f)return false;
            for(var i=0;i<=240;i++){var value=curve.Evaluate(i/240f);if(!float.IsFinite(value) || value<min-.0001f || value>max+.0001f)return false;}
            return true;
        }
    }
}
