using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DAnimationPlaygroundSession
    {
        private AnimationPlaygroundDodgeController dodge;
        public bool DodgeReady => servicesBound && isActiveAndEnabled && PreviewReady && dodge != null && dodge.Ready;
        private void BindDodge()
        {
            if (bindings.dodge == null) return;
            dodge = player.gameObject.AddComponent<AnimationPlaygroundDodgeController>();
            dodge.Configure(player, presentation, coordinator, bindings.dodge, ResetRegistry,
                () => !isActiveAndEnabled || !servicesBound || !PreviewReady || !Application.isFocused
                    || playerInput.Mode != TopDown3DInputMode.Gameplay,
                () => playerAction.IsBusy || crouch != null && crouch.Active || lowMotion != null && lowMotion.Active
                    || airborne != null && airborne.Active || turning != null && turning.Active || sprintEntries != null && sprintEntries.Active);
        }
        private void RequestDodge()
        {
            if (!DodgeReady) { playerAction?.ReportFeedback("Dodge calibration is unavailable."); return; }
            var activity=playerAction.Activity;
            // MotorOwned/StationActive include our outgoing action. Keep actual
            // work, placement, camp and other animation owners non-interruptible.
            if(dodge.CanAcceptHandoff && !activity.LegacyWork && !activity.Placement && !activity.CampActive
                && ResourceCombatBusy?.Invoke()!=true && !(crouch!=null && crouch.Active)
                && !(lowMotion!=null && lowMotion.Active) && !(airborne!=null && airborne.Active && !airborne.CanYieldToGroundedAction))
            {
                if(airborne!=null && airborne.Active)airborne.TryYieldToGroundedAction(dodge);
                if(sprintEntries!=null && sprintEntries.Active)sprintEntries.TryYieldTo(dodge);
                if(locomotionTransitions!=null && locomotionTransitions.Active)locomotionTransitions.TryYieldTo(dodge);
                if(turning!=null && turning.Active)turning.TryYieldTo(dodge);
            }
            if (!dodge.TryDodge())
            {
                if(!dodge.Active)presentation.EndPoseBlend(dodge);
                playerAction.ReportFeedback(dodge.LastFailure);
            }
        }
        private void DetachDodge()
        {
            if (dodge == null) return;
            dodge.DisposeOwnedServices();
            if (Application.isPlaying) Destroy(dodge); else DestroyImmediate(dodge);
            dodge = null;
        }
    }
}
