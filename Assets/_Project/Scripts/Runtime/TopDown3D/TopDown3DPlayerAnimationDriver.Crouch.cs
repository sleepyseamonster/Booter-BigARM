using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private const int CrouchInputIndex=22;
        private object crouchPresentationOwner;
        private AnimationPlaygroundCrouchProfile crouchProfile;
        private AnimationMixerPlayable crouchMixer;
        private readonly AnimationClipPlayable[] crouchClips=new AnimationClipPlayable[5];
        private readonly float[] crouchWeights=new float[5];
        private AnimationPlaygroundCrouchPose crouchPose;
        private bool crouchRecoveryOwned;
        public bool CrouchRecoveryOwned=>crouchRecoveryOwned;
        public bool CrouchPresentationOwned=>crouchPresentationOwner!=null;
        public bool TryBeginCrouchPresentation(object owner,AnimationPlaygroundCrouchProfile profile)
        {
            if(owner==null || profile==null || !profile.Validate(out _) || crouchPresentationOwner!=null || !playableGraph.IsValid()
                || motor==null || visualInstance==null || IsGathering || campIndex>=0 || animationPresentationOwner!=null)return false;
            if(mixer.GetInputCount()<=CrouchInputIndex)mixer.SetInputCount(CrouchInputIndex+1);
            crouchMixer=AnimationMixerPlayable.Create(playableGraph,5);
            try
            {
                for(var i=0;i<5;i++)
                {
                    var entry=profile.Get((AnimationPlaygroundCrouchRole)(i+1));var clip=AnimationClipPlayable.Create(playableGraph,entry.clip);
                    clip.SetApplyFootIK(false);clip.SetApplyPlayableIK(false);clip.SetSpeed(0);
                    playableGraph.Connect(clip,0,crouchMixer,i);crouchClips[i]=clip;
                }
                playableGraph.Connect(crouchMixer,0,mixer,CrouchInputIndex);crouchPresentationOwner=owner;crouchProfile=profile;crouchPose=default;return true;
            }
            catch{ReleaseCrouchPresentation();throw;}
        }
        public bool SetCrouchPose(object owner,AnimationPlaygroundCrouchPose pose)
        {
            if(owner==null || !ReferenceEquals(owner,crouchPresentationOwner) || !float.IsFinite(pose.Phase) || !float.IsFinite(pose.MovingWeight)
                || !float.IsFinite(pose.Direction.x) || !float.IsFinite(pose.Direction.y) || !float.IsFinite(pose.IdleSeconds))return false;
            crouchPose=pose;return true;
        }
        public bool EndCrouchPresentation(object owner)
        {if(owner==null || !ReferenceEquals(owner,crouchPresentationOwner))return false;ReleaseCrouchPresentation();return true;}
        public bool TryAdoptCrouchRecovery(object previousOwner,object postureOwner)
        {
            if(previousOwner==null || !ReferenceEquals(previousOwner,crouchPresentationOwner) || motor==null
                || !motor.TransferAnimationPostureOwner(postureOwner,this))return false;
            crouchPresentationOwner=this;crouchRecoveryOwned=true;motor.SetLaboratoryPostureSuspended(this,true);return true;
        }
        private void AdvanceCrouchRecovery()
        {
            if(!crouchRecoveryOwned || motor==null)return;
            if(!motor.IsAnimationPostureOwner(this))
            {
                try{motor.BindAnimationPostureOwner(this);motor.SetLaboratoryPostureSuspended(this,true);}
                catch(InvalidOperationException){return;}
            }
            if(!motor.TryRestoreAnimationPosture(this))return;
            motor.ReleaseAnimationPostureOwner(this);ReleaseCrouchPresentation();
        }
        private bool PresentCrouchPose()
        {
            if(crouchPresentationOwner==null || animationPresentationOwner!=null || IsGathering || campIndex>=0 || motor.AnimationPosture!=AnimationPlaygroundPosture.Crouched)
            {if(mixer.IsValid() && mixer.GetInputCount()>CrouchInputIndex)mixer.SetInputWeight(CrouchInputIndex,0);return false;}
            if(crouchPresentationOwner is UnityEngine.Object owner && owner==null){ReleaseCrouchPresentation();return false;}
            for(var i=0;i<ClipCount;i++)mixer.SetInputWeight(i,0);mixer.SetInputWeight(CrouchInputIndex,1);
            var x=crouchPose.Direction.x;var y=crouchPose.Direction.y;var sum=Mathf.Abs(x)+Mathf.Abs(y);
            var moving=Mathf.Clamp01(crouchPose.MovingWeight);
            crouchWeights[0]=sum>.0001f ? 1-moving : 1;
            crouchWeights[1]=Mathf.Max(0,y);crouchWeights[2]=Mathf.Max(0,-y);
            crouchWeights[3]=Mathf.Max(0,-x);crouchWeights[4]=Mathf.Max(0,x);
            var correction=0f;
            for(var i=0;i<5;i++)
            {
                var entry=crouchProfile.Get((AnimationPlaygroundCrouchRole)(i+1));
                var weight=i==0 ? crouchWeights[0] : sum>.0001f ? crouchWeights[i]/sum*moving : 0;
                var phase=i==0 ? Mathf.Repeat(crouchPose.IdleSeconds/entry.clip.length,1)
                    : Mathf.Repeat(TopDown3DAnimationMath.MapSemanticPhaseToSource(crouchPose.Phase,entry.leftContacts,entry.rightContacts)+entry.sourcePhaseOffset,1);
                crouchClips[i].SetTime(phase*entry.clip.length);crouchMixer.SetInputWeight(i,weight);correction+=entry.Correction(phase)*weight;
            }
            correction+=crouchProfile.GroundCorrection(crouchPose.Direction,crouchPose.MovingWeight,crouchPose.Phase);
            visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition+Vector3.up*correction,Quaternion.Euler(visualLocalEulerAngles));
            var blending=PoseBlendOwned;ApplyPoseBlend();
            if(!blending)ProjectLaboratoryFootPlane();
            CurrentLocomotionState=TopDown3DLocomotionState.Sustained;return true;
        }
        public bool TryEmitCrouchFootContact(object owner,TopDown3DFootSide side,float speed)
        {
            if(owner==null || !ReferenceEquals(owner,crouchPresentationOwner) || !crouchPose.Supported || animationPresentationOwner!=null || PoseBlendOwned || !float.IsFinite(speed) || speed<0)return false;
            if(!PresentCrouchPose())return false;playableGraph.Evaluate(0);return EmitFootContact(side,side==TopDown3DFootSide.Left ? leftFoot : rightFoot,speed);
        }
        private void ReleaseCrouchPresentation()
        {
            if(playableGraph.IsValid() && crouchMixer.IsValid())
            {
                if(mixer.GetInputCount()>CrouchInputIndex){playableGraph.Disconnect(mixer,CrouchInputIndex);mixer.SetInputWeight(CrouchInputIndex,0);}
                for(var i=0;i<crouchClips.Length;i++)if(crouchClips[i].IsValid())playableGraph.DestroyPlayable(crouchClips[i]);
                playableGraph.DestroyPlayable(crouchMixer);
            }
            Array.Clear(crouchClips,0,crouchClips.Length);crouchMixer=default;crouchPresentationOwner=null;crouchProfile=null;crouchRecoveryOwned=false;
            if(mixer.IsValid())mixer.SetInputWeight(IdleIndex,1);
            if(visualInstance!=null)visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition,Quaternion.Euler(visualLocalEulerAngles));
            ResetTransitionState(TopDown3DLocomotionState.Idle);locomotionPhase=0;smoothedPlanarSpeed=0;lastActionIndex=-1;oneShotContactEmitted=false;
        }
    }
}
