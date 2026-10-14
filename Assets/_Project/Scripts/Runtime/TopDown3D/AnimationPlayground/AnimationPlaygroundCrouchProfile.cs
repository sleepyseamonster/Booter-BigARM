using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundCrouchRole { Enter, Idle, Forward, Backward, Left, Right, Exit }
    [Serializable] public sealed class AnimationPlaygroundCrouchGroundSample
    {
        public int directionIndex,weightIndex;
        public AnimationCurve correction;
    }
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
        public float crouchRadius=.4f;
        public bool contactsQualified;
        public bool groundingQualified;
        public AnimationPlaygroundCrouchGroundSample[] grounding=Array.Empty<AnimationPlaygroundCrouchGroundSample>();
        public const int GroundDirectionCount=32,GroundWeightCount=8,GroundSampleCount=1+GroundDirectionCount*GroundWeightCount;
        private const int GroundQuadrantSteps=GroundDirectionCount/4;
        public AnimationPlaygroundCrouchEntry[] entries=Array.Empty<AnimationPlaygroundCrouchEntry>();
        public AnimationPlaygroundCrouchEntry Get(AnimationPlaygroundCrouchRole role)
        {foreach(var entry in entries)if(entry!=null && entry.role==role)return entry;return null;}
        public static Vector2 GroundDirection(int index)
        {
            if(index<0 || index>=GroundDirectionCount)throw new ArgumentOutOfRangeException(nameof(index));
            var progress=(index%GroundQuadrantSteps)/(float)GroundQuadrantSteps;var quadrant=index/GroundQuadrantSteps;
            var a=quadrant==0 ? Vector2.up : quadrant==1 ? Vector2.right : quadrant==2 ? Vector2.down : Vector2.left;
            var b=quadrant==0 ? Vector2.right : quadrant==1 ? Vector2.down : quadrant==2 ? Vector2.left : Vector2.up;
            return Vector2.Lerp(a,b,progress).normalized;
        }
        public float GroundCorrection(Vector2 direction,float movingWeight,float phase)
        {
            if(grounding==null || grounding.Length!=GroundSampleCount)return 0;
            var sum=Mathf.Abs(direction.x)+Mathf.Abs(direction.y);
            if(sum<.0001f || movingWeight<=0)return grounding[0].correction.Evaluate(Mathf.Repeat(phase,1));
            var quadrant=direction.x>=0 ? direction.y>=0 ? 0 : 1 : direction.y<0 ? 2 : 3;
            var along=(quadrant==0 || quadrant==2 ? Mathf.Abs(direction.x) : Mathf.Abs(direction.y))/sum;
            var coordinate=(quadrant+along)*GroundQuadrantSteps;
            var raw=Mathf.FloorToInt(coordinate);var lower=raw%GroundDirectionCount;var upper=(lower+1)%GroundDirectionCount;
            var weight=Mathf.Clamp01(movingWeight)*GroundWeightCount;var w0=Mathf.Min(GroundWeightCount-1,Mathf.FloorToInt(weight));var w1=w0+1;
            phase=Mathf.Repeat(phase,1);
            var first=Mathf.Lerp(GroundValue(lower,w0,phase),GroundValue(upper,w0,phase),coordinate-raw);
            var second=Mathf.Lerp(GroundValue(lower,w1,phase),GroundValue(upper,w1,phase),coordinate-raw);
            return Mathf.Lerp(first,second,weight-w0);
        }
        private float GroundValue(int direction,int weight,float phase)=>grounding[weight==0 ? 0 : 1+direction*GroundWeightCount+weight-1].correction.Evaluate(phase);
        public bool Validate(out string reason,bool requirePhysical=true)
        {
            reason=null;
            if(schema!=1 || !float.IsFinite(standingHeight) || !float.IsFinite(crouchHeight) || crouchHeight<=.4f || crouchHeight>=standingHeight
                || !float.IsFinite(crouchRadius) || crouchRadius<=0 || crouchRadius>.75f || 2*crouchRadius>crouchHeight
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
            if(grounding!=null && grounding.Length>0)
            {
                if(grounding.Length!=GroundSampleCount){reason="Crouch grounding requires the complete measured blend grid.";return false;}
                for(var i=0;i<grounding.Length;i++)
                {
                    var sample=grounding[i];
                    if(sample==null || sample.directionIndex!=(i==0 ? -1 : (i-1)/GroundWeightCount) || sample.weightIndex!=(i==0 ? 0 : (i-1)%GroundWeightCount+1)
                        || !CurveFinite(sample.correction,0,.14f) || Mathf.Abs(sample.correction.Evaluate(0)-sample.correction.Evaluate(1))>.001f)
                    {reason="Crouch grounding samples are missing, unordered or outside their reviewed envelope.";return false;}
                }
            }
            if(requirePhysical && (!groundingQualified || grounding==null || grounding.Length!=GroundSampleCount))
            {reason="Crouch blend grounding qualification is pending.";return false;}
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
