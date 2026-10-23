using UnityEngine;
namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DAnimationPlaygroundSession
    {
        private AnimationPlaygroundLocomotionTransitionController locomotionTransitions;
        private void BindLocomotionTransitions()
        {
            if (bindings.locomotionTransitions==null) return;
            locomotionTransitions=player.gameObject.AddComponent<AnimationPlaygroundLocomotionTransitionController>();
            locomotionTransitions.Configure(player,presentation,coordinator,bindings.locomotionTransitions,ResetRegistry,
                ()=>!servicesBound || !PreviewReady || !isActiveAndEnabled || !Application.isFocused || playerInput.Mode!=TopDown3DInputMode.Gameplay,
                ()=>playerAction.IsBusy || crouch!=null && crouch.Active || lowMotion!=null && lowMotion.Active
                    || airborne!=null && airborne.Active || dodge!=null && dodge.Active || sprintEntries!=null && sprintEntries.Active || turning!=null && turning.Active);
        }
        private void DetachLocomotionTransitions()
        {
            if (locomotionTransitions==null) return;
            locomotionTransitions.DisposeOwnedServices();
            if (Application.isPlaying) Destroy(locomotionTransitions); else DestroyImmediate(locomotionTransitions);
            locomotionTransitions=null;
        }
    }
}
