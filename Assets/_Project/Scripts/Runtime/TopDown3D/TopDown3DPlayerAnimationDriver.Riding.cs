using UnityEngine;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private object mountedPresentationOwner;
        internal bool MountedPresentationAvailable => mountedPresentationOwner == null && !AnimationPresentationOwned
            && !CrouchPresentationOwned && !CrouchRecoveryOwned && !PoseBlendOwned && !IsGathering && campIndex < 0;
        internal bool TryBeginMountedPresentation(object owner)
        {
            if (owner == null || !MountedPresentationAvailable) return false;
            mountedPresentationOwner = owner; return true;
        }
        private bool PresentMountedPose()
        {
            if (mountedPresentationOwner == null) return false;
            if (playableGraph.IsValid())
            {
                for (int i = 0; i < ClipCount; i++) mixer.SetInputWeight(i, i == IdleIndex ? 1 : 0);
                // Placeholder idle only. No contact events or standing gait clock runs while attached.
            }
            if (visualInstance != null) visualInstance.transform.SetLocalPositionAndRotation(
                visualLocalPosition, Quaternion.Euler(visualLocalEulerAngles));
            CurrentLocomotionState = TopDown3DLocomotionState.ConstrainedAction;
            return true;
        }
        internal void EndMountedPresentation(object owner)
        {
            if (!ReferenceEquals(owner, mountedPresentationOwner)) return;
            mountedPresentationOwner = null; smoothedPlanarSpeed = 0; locomotionPhase = 0;
            ResetTransitionState(TopDown3DLocomotionState.Idle);
        }
        internal void AlignMountedVisual(object owner, Vector3 riderPosition, Quaternion rotation)
        {
            if (!ReferenceEquals(owner, mountedPresentationOwner) || visualInstance == null) return;
            // Align the rendering child to the mount's interpolated seat without changing either physics root.
            visualInstance.transform.SetPositionAndRotation(riderPosition + rotation * visualLocalPosition,
                rotation * Quaternion.Euler(visualLocalEulerAngles));
        }
    }
}
