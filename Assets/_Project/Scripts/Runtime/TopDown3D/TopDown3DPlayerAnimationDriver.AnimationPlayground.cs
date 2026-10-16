using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private const int AnimationActionIndex = 21;
        private object animationPresentationOwner;
        private AnimationClipPlayable animationActionPlayable;
        private double animationPresentationTime;
        private bool animationPreview;
        private float animationHeightCorrection;
        private bool animationFootProjection;
        private bool animationBodyProjection;
        public bool AnimationPresentationOwned => animationPresentationOwner != null;
        public bool IsAnimationPresentationOwner(object owner)=>owner!=null && ReferenceEquals(owner,animationPresentationOwner);

        public bool TryBeginAnimationPresentation(object owner, AnimationClip clip, bool preview = false)
        {
            if (owner == null || clip == null || animationPresentationOwner != null || !playableGraph.IsValid()
                || IsGathering || campIndex >= 0 || crouchRecoveryOwned) return false;
            animationActionPlayable = AnimationClipPlayable.Create(playableGraph, clip);
            animationActionPlayable.SetApplyFootIK(false);
            animationActionPlayable.SetApplyPlayableIK(false);
            animationActionPlayable.SetSpeed(0); // The owning simulation or preview clock supplies time.
            playableGraph.Connect(animationActionPlayable, 0, mixer, AnimationActionIndex);
            animationPresentationOwner = owner;
            animationPreview = preview;
            animationPresentationTime = 0;
            return true;
        }

        public bool SetAnimationPresentationTime(object owner, double seconds)
        {
            if (owner == null || !ReferenceEquals(owner, animationPresentationOwner) || !playableGraph.IsValid()
                || !animationActionPlayable.IsValid() || !double.IsFinite(seconds) || seconds < 0) return false;
            if(JumpPresentationOwned && jumpPoseSampleTime!=Time.fixedTimeAsDouble)
            {
                jumpPreviousSourceTime=animationPresentationTime;jumpPoseSampleTime=Time.fixedTimeAsDouble;
                var delta=seconds-animationPresentationTime;var clip=animationActionPlayable.GetAnimationClip();
                if(clip.isLooping && delta<0)delta+=clip.length;
                jumpSourceRate=Mathf.Clamp((float)(delta/Math.Max(.0001,Time.fixedDeltaTime)),0,3);
            }
            animationPresentationTime = seconds;
            return true;
        }
        public bool TryReplaceAnimationPresentation(object owner,AnimationClip clip)
        {
            if(owner==null || !ReferenceEquals(owner,animationPresentationOwner) || animationPreview || clip==null || clip.length<=0
                || !playableGraph.IsValid() || !animationActionPlayable.IsValid())return false;
            var replacement=AnimationClipPlayable.Create(playableGraph,clip);
            replacement.SetApplyFootIK(false);replacement.SetApplyPlayableIK(false);replacement.SetSpeed(0);replacement.SetTime(0);
            playableGraph.Disconnect(mixer,AnimationActionIndex);playableGraph.Connect(replacement,0,mixer,AnimationActionIndex);
            playableGraph.DestroyPlayable(animationActionPlayable);animationActionPlayable=replacement;
            animationPresentationTime=0;jumpPreviousSourceTime=0;jumpPoseSampleTime=double.NegativeInfinity;
            animationHeightCorrection=0;animationFootProjection=false;animationBodyProjection=false;animationGarmentFloor=false;return true;
        }

        public bool SetAnimationHeightCorrection(object owner,float correction)
        {
            if(owner==null || !ReferenceEquals(owner,animationPresentationOwner) || !float.IsFinite(correction) || correction<0 || correction>.18f)return false;
            animationHeightCorrection=correction;return true;
        }

        public bool EndAnimationPresentation(object owner)
        {
            if (owner == null || !ReferenceEquals(owner, animationPresentationOwner)) return false;
            ReleaseAnimationPresentation();
            return true;
        }
        public bool SetAnimationFootProjection(object owner,bool enabled)
        {
            if(owner==null || !ReferenceEquals(owner,animationPresentationOwner) || animationPreview)return false;
            animationFootProjection=enabled;return true;
        }
        public bool SetAnimationBodyProjection(object owner,AnimationPlaygroundFootProjection data)
        {
            if(owner==null || !ReferenceEquals(owner,animationPresentationOwner) || animationPreview)return false;
            if(data==null){animationBodyProjection=false;return true;}
            if(laboratoryFootProjection==null || !data.Validate(laboratoryFootProjection.model,out _))return false;
            NormalizeCampSkinWeights();
            if(laboratoryBodyProjection!=data)
            {bodyProjectionSampler=data.Bind(visualInstance.transform);laboratoryBodyProjection=data;}
            animationBodyProjection=true;return true;
        }

        private bool PresentAnimationAction()
        {
            if (animationPresentationOwner == null) return false;
            if (animationPresentationOwner is UnityEngine.Object owner && owner == null)
            { ReleaseAnimationPresentation(); return false; }
            if(JumpPresentationOwned)return PresentJumpAction();
            animationActionPlayable.SetTime(animationPresentationTime);
            if(mixer.GetInputCount()>CrouchInputIndex)mixer.SetInputWeight(CrouchInputIndex,0);
            for (var i = 0; i < ClipCount; i++) mixer.SetInputWeight(i, i == AnimationActionIndex ? 1 : 0);
            visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition+Vector3.up*animationHeightCorrection, Quaternion.Euler(visualLocalEulerAngles));
            CurrentLocomotionState = TopDown3DLocomotionState.ConstrainedAction;
            PrepareContinuationActionTerrain();
            var blending=PoseBlendOwned;ApplyPoseBlend();
            PrepareAdaptedTurnPose();
            if((animationFootProjection || animationBodyProjection) && !blending)ProjectLaboratoryFootPlane();
            else if(!blending)LastPoseBlendGroundCorrection=0;
            ApplyGarmentFloor();
            // No legacy locomotion/contact clock advances while this branch owns the pose.
            return true;
        }

        public bool TryEmitAnimationFootContact(object owner, TopDown3DFootSide side, float speed)
        {
            if (owner == null || !ReferenceEquals(owner, animationPresentationOwner) || animationPreview || PoseBlendOwned || motor == null
                || !motor.AnimationSnapshot.Supported || !float.IsFinite(speed) || speed < 0) return false;
            if (!PresentAnimationAction()) return false;
            playableGraph.Evaluate(0); // Sample the owned pose before reading its contact position.
            return EmitFootContact(side, side == TopDown3DFootSide.Left ? leftFoot : rightFoot, speed);
        }

        private void ReleaseAnimationPresentation()
        {
            if (animationPresentationOwner == null) return;
            var wasJump=JumpPresentationOwned;
            ReleaseAdaptedTurnPose();
            continuationActionTerrainApplied=false;forwardTerrainApplied=false;forwardTerrainPose?.Disable();
            if (playableGraph.IsValid())
            {
                playableGraph.Disconnect(mixer, AnimationActionIndex);
                if (animationActionPlayable.IsValid()) playableGraph.DestroyPlayable(animationActionPlayable);
                mixer.SetInputWeight(AnimationActionIndex, 0);
                if(!wasJump)mixer.SetInputWeight(IdleIndex, 1);
                else
                {
                    var total=0f;for(var i=0;i<mixer.GetInputCount();i++)total+=mixer.GetInputWeight(i);
                    if(total<.00001f)mixer.SetInputWeight(IdleIndex,1);
                    else for(var i=0;i<mixer.GetInputCount();i++)mixer.SetInputWeight(i,mixer.GetInputWeight(i)/total);
                }
            }
            animationPresentationOwner = null;
            animationActionPlayable = default;
            animationPresentationTime = 0;
            animationPreview = false;
            animationHeightCorrection=0;
            animationFootProjection=false;
            animationBodyProjection=false;
            animationGarmentFloor=false;
            jumpPresentation=false;jumpComposingUnderlay=false;jumpPoseSampleTime=double.NegativeInfinity;
            if(!poseBlendGarmentFloor)RestoreGarmentFloor();
            if(!wasJump){ResetTransitionState(TopDown3DLocomotionState.Idle);locomotionPhase = 0;smoothedPlanarSpeed = 0;}
            lastActionIndex = -1;
            oneShotContactEmitted = false;
        }
    }
}
