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
        public bool AnimationPresentationOwned => animationPresentationOwner != null;

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
            if (owner == null || !ReferenceEquals(owner, animationPresentationOwner) || !double.IsFinite(seconds) || seconds < 0) return false;
            animationPresentationTime = seconds;
            return true;
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

        private bool PresentAnimationAction()
        {
            if (animationPresentationOwner == null) return false;
            if (animationPresentationOwner is UnityEngine.Object owner && owner == null)
            { ReleaseAnimationPresentation(); return false; }
            animationActionPlayable.SetTime(animationPresentationTime);
            if(mixer.GetInputCount()>CrouchInputIndex)mixer.SetInputWeight(CrouchInputIndex,0);
            for (var i = 0; i < ClipCount; i++) mixer.SetInputWeight(i, i == AnimationActionIndex ? 1 : 0);
            visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition+Vector3.up*animationHeightCorrection, Quaternion.Euler(visualLocalEulerAngles));
            CurrentLocomotionState = TopDown3DLocomotionState.ConstrainedAction;
            ApplyPoseBlend();
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
            if (playableGraph.IsValid())
            {
                playableGraph.Disconnect(mixer, AnimationActionIndex);
                if (animationActionPlayable.IsValid()) playableGraph.DestroyPlayable(animationActionPlayable);
                mixer.SetInputWeight(AnimationActionIndex, 0);
                mixer.SetInputWeight(IdleIndex, 1);
            }
            animationPresentationOwner = null;
            animationActionPlayable = default;
            animationPresentationTime = 0;
            animationPreview = false;
            animationHeightCorrection=0;
            ResetTransitionState(TopDown3DLocomotionState.Idle);
            locomotionPhase = 0;
            smoothedPlanarSpeed = 0;
            lastActionIndex = -1;
            oneShotContactEmitted = false;
        }
    }
}
