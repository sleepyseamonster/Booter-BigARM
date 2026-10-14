using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundLowMotionRole { SlideEnter,SlideActive,SlideRecover,RollContact,RollActive,RollRecover }
    [Serializable] public sealed class AnimationPlaygroundLowMotionEntry
    {
        public AnimationPlaygroundLowMotionRole role;
        public AnimationClip clip;
        public bool horizontal;
        public AnimationCurve capsuleHeight;
    }
    public sealed class AnimationPlaygroundLowMotionProfile : ScriptableObject
    {
        public int schema=1;
        public bool qualified;
        public GameObject targetModel;
        public AnimationPlaygroundFootProjection bodyProjection;
        public AnimationPlaygroundLowMotionEntry[] entries=Array.Empty<AnimationPlaygroundLowMotionEntry>();
        public float standingHeight=1.82f,verticalRadius=.4f,lowLength=2.2f,lowRadius=.55f,lowForwardOffset=.05f;
        public float minimumSlideSpeed=3,slideDeceleration=4,rollSpeed=2.2f;
        public AnimationPlaygroundLowMotionEntry Get(AnimationPlaygroundLowMotionRole role)
        {
            foreach(var entry in entries ?? Array.Empty<AnimationPlaygroundLowMotionEntry>())
                if(entry!=null && entry.role==role)return entry;
            return null;
        }
        public bool Validate(out string reason,bool requireQualified=true)
        {
            reason=null;
            if(schema!=1 || targetModel==null || bodyProjection==null || !bodyProjection.Validate(targetModel,out reason))
            {reason="A paired low-motion model and body projection are required.";return false;}
            foreach(var role in (AnimationPlaygroundLowMotionRole[])Enum.GetValues(typeof(AnimationPlaygroundLowMotionRole)))
            {
                var entry=Get(role);
                if(entry==null || entry.clip==null || !entry.clip.isHumanMotion || entry.clip.length<=0 || entry.clip.isLooping
                    || entry.capsuleHeight==null || entry.capsuleHeight.length<2
                    || !Mathf.Approximately(entry.capsuleHeight[0].time,0)
                    || !Mathf.Approximately(entry.capsuleHeight[entry.capsuleHeight.length-1].time,1))
                {reason="Every slide and roll phase needs a bounded Humanoid pose and shape.";return false;}
                var horizontal=role==AnimationPlaygroundLowMotionRole.SlideActive || role==AnimationPlaygroundLowMotionRole.RollActive;
                if(entry.horizontal!=horizontal){reason="Only the low active phases use the longitudinal shape.";return false;}
                foreach(var key in entry.capsuleHeight.keys)
                    if(!float.IsFinite(key.time) || !float.IsFinite(key.value) || key.time<0 || key.time>1
                        || key.value<verticalRadius*2 || key.value>standingHeight)
                    {reason="Low-motion shape curves are outside the reviewed vertical range.";return false;}
                for(var i=0;i<=64;i++)
                {
                    var height=entry.capsuleHeight.Evaluate(i/64f);
                    if(!float.IsFinite(height) || height<verticalRadius*2-.0001f || height>standingHeight+.0001f)
                    {reason="Low-motion shape interpolation exceeds the reviewed vertical range.";return false;}
                }
            }
            if(entries.Length!=6){reason="Low-motion roles must be unique.";return false;}
            foreach(var value in new[]{standingHeight,verticalRadius,lowLength,lowRadius,minimumSlideSpeed,slideDeceleration,rollSpeed})
                if(!float.IsFinite(value) || value<=0){reason="Low-motion calibration must be finite and positive.";return false;}
            if(!float.IsFinite(lowForwardOffset) || Mathf.Abs(lowForwardOffset)>1 || lowLength<lowRadius*2 || lowLength>3
                || lowRadius>.8f || lowRadius*2>standingHeight || verticalRadius*2>standingHeight
                || minimumSlideSpeed>12 || rollSpeed>12)
            {reason="Low-motion calibration exceeds the reviewed range.";return false;}
            if(requireQualified && !qualified){reason="Slide and roll qualification is pending.";return false;}
            return true;
        }
    }
}
