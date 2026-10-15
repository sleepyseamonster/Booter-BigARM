using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private const int PoseBlendInputIndex=23;
        private object poseBlendOwner;
        private AnimationMixerPlayable poseBlendSnapshot;
        private readonly List<AnimationClipPlayable> poseBlendClips=new();
        private double poseBlendElapsed;
        private float poseBlendDuration,poseBlendSourceHeight;
        private AnimationPlaygroundFootProjection laboratoryFootProjection;
        private AnimationPlaygroundFootProjection.Sampler footProjectionSampler;
        private AnimationPlaygroundFootProjection laboratoryBodyProjection;
        private AnimationPlaygroundFootProjection.Sampler bodyProjectionSampler;
        private bool poseBlendBodyProjection;
        public float LastPoseBlendGroundCorrection {get;private set;}
        public bool PoseBlendOwned=>poseBlendOwner!=null;
        public bool TransferPoseBlendOwner(object owner,object successor)
        {
            if(owner==null || successor==null)return false;
            if(poseBlendOwner==null)return true;
            if(!ReferenceEquals(owner,poseBlendOwner))return false;
            poseBlendOwner=successor;return true;
        }
        private readonly struct PoseSource
        {
            public readonly AnimationClip Clip;
            public readonly double Time;
            public readonly float Weight;
            public PoseSource(AnimationClip clip,double time,float weight){Clip=clip;Time=time;Weight=weight;}
        }
        public bool TryCapturePoseBlend(object owner,float seconds)
        {
            if(owner==null || !float.IsFinite(seconds) || seconds<=0 || seconds>1 || !playableGraph.IsValid()
                || !mixer.IsValid() || visualInstance==null || crouchRecoveryOwned || IsGathering || campIndex>=0
                || poseBlendOwner!=null && !ReferenceEquals(owner,poseBlendOwner))return false;
            if(animationPresentationOwner!=null)PresentAnimationAction();
            else if(crouchPresentationOwner!=null)PresentCrouchPose();
            var sources=new List<PoseSource>();
            if(!CollectPoseSources(mixer,1,sources,0) || sources.Count==0)return false;
            var height=visualInstance.transform.localPosition.y-visualLocalPosition.y;
            var bodyProjection=animationBodyProjection || poseBlendBodyProjection;
            var garmentFloor=animationGarmentFloor || poseBlendGarmentFloor;
            ReleasePoseBlend();
            poseBlendBodyProjection=bodyProjection;
            poseBlendGarmentFloor=garmentFloor;
            if(mixer.GetInputCount()<=PoseBlendInputIndex)mixer.SetInputCount(PoseBlendInputIndex+1);
            poseBlendSnapshot=AnimationMixerPlayable.Create(playableGraph,sources.Count);
            try
            {
                var total=0f;foreach(var source in sources)total+=source.Weight;
                for(var i=0;i<sources.Count;i++)
                {
                    var source=sources[i];var clip=AnimationClipPlayable.Create(playableGraph,source.Clip);
                    clip.SetApplyFootIK(false);clip.SetApplyPlayableIK(false);clip.SetSpeed(0);clip.SetTime(source.Time);
                    playableGraph.Connect(clip,0,poseBlendSnapshot,i);poseBlendSnapshot.SetInputWeight(i,source.Weight/total);poseBlendClips.Add(clip);
                }
                playableGraph.Connect(poseBlendSnapshot,0,mixer,PoseBlendInputIndex);mixer.SetInputWeight(PoseBlendInputIndex,0);
                poseBlendOwner=owner;poseBlendDuration=seconds;poseBlendElapsed=0;poseBlendSourceHeight=height;return true;
            }
            catch{ReleasePoseBlend();throw;}
        }
        private static bool CollectPoseSources(Playable playable,float weight,List<PoseSource> sources,int depth)
        {
            if(weight<=.000001f)return true;
            if(!playable.IsValid() || !float.IsFinite(weight) || depth>4 || sources.Count>=128)return false;
            if(playable.GetPlayableType()==typeof(AnimationClipPlayable))
            {
                var clip=(AnimationClipPlayable)playable;
                if(clip.GetAnimationClip()==null || !double.IsFinite(clip.GetTime()))return false;
                sources.Add(new PoseSource(clip.GetAnimationClip(),Math.Max(0,clip.GetTime()),weight));return true;
            }
            if(playable.GetPlayableType()!=typeof(AnimationMixerPlayable))return false;
            for(var i=0;i<playable.GetInputCount();i++)
                if(!CollectPoseSources(playable.GetInput(i),weight*playable.GetInputWeight(i),sources,depth+1))return false;
            return true;
        }
        public bool SetPoseBlendElapsed(object owner,double seconds)
        {
            if(owner==null || !ReferenceEquals(owner,poseBlendOwner) || !double.IsFinite(seconds) || seconds<poseBlendElapsed)return false;
            poseBlendElapsed=seconds;return true;
        }
        public bool EndPoseBlend(object owner)
        {if(owner==null || !ReferenceEquals(owner,poseBlendOwner))return false;ReleasePoseBlend();return true;}
        private void ApplyPoseBlend()
        {
            if(poseBlendOwner==null)return;
            if(poseBlendOwner is UnityEngine.Object owner && owner==null){ReleasePoseBlend();return;}
            var amount=Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(poseBlendElapsed/poseBlendDuration)));
            for(var i=0;i<mixer.GetInputCount();i++)
                if(i!=PoseBlendInputIndex)mixer.SetInputWeight(i,mixer.GetInputWeight(i)*amount);
            mixer.SetInputWeight(PoseBlendInputIndex,1-amount);
            var height=Mathf.Lerp(poseBlendSourceHeight,visualInstance.transform.localPosition.y-visualLocalPosition.y,amount);
            visualInstance.transform.localPosition=visualLocalPosition+Vector3.up*height;
            ProjectLaboratoryFootPlane();
            ApplyGarmentFloor();
            if(amount>=1)ReleasePoseBlend();
        }
        private void ProjectLaboratoryFootPlane()
        {
            LastPoseBlendGroundCorrection=0;
            var sampler=animationBodyProjection || poseBlendBodyProjection ? bodyProjectionSampler : footProjectionSampler;
            if(sampler==null)return;
            playableGraph.Evaluate(0);
            var minimum=sampler.MinimumWorldY();
            var plane=transform.TransformPoint(Vector3.up*laboratoryFeetPlane).y;
            var correction=forwardTerrainApplied ? Mathf.Max(0,-sampler.MinimumDistance(forwardTerrainPlane))/forwardTerrainPlane.normal.y
                : Mathf.Max(0,plane-minimum);
            if(!float.IsFinite(correction) || correction>.18f)throw new InvalidOperationException("The posture ground correction exceeds its qualified envelope.");
            visualInstance.transform.localPosition+=Vector3.up*correction;LastPoseBlendGroundCorrection=correction;
        }
        private bool PresentStandingPoseBlend()
        {
            if(poseBlendOwner==null)return false;
            for(var i=0;i<mixer.GetInputCount();i++)mixer.SetInputWeight(i,i==IdleIndex ? 1 : 0);
            if(clipPlayables[IdleIndex].IsValid()){clipPlayables[IdleIndex].SetTime(0);clipPlayables[IdleIndex].SetSpeed(0);}
            var correction=locomotionEntries[IdleIndex]?.HeightCorrection(0) ?? 0;
            visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition+Vector3.up*correction,Quaternion.Euler(visualLocalEulerAngles));
            ApplyPoseBlend();CurrentLocomotionState=TopDown3DLocomotionState.ConstrainedAction;return true;
        }
        private void ReleasePoseBlend()
        {
            if(playableGraph.IsValid())
            {
                if(mixer.IsValid() && mixer.GetInputCount()>PoseBlendInputIndex)
                {playableGraph.Disconnect(mixer,PoseBlendInputIndex);mixer.SetInputWeight(PoseBlendInputIndex,0);}
                foreach(var clip in poseBlendClips)if(clip.IsValid())playableGraph.DestroyPlayable(clip);
                if(poseBlendSnapshot.IsValid())playableGraph.DestroyPlayable(poseBlendSnapshot);
            }
            poseBlendClips.Clear();poseBlendSnapshot=default;poseBlendOwner=null;poseBlendElapsed=0;
            poseBlendBodyProjection=false;
            poseBlendGarmentFloor=false;
            if(!animationGarmentFloor)RestoreGarmentFloor();
        }
    }
}
