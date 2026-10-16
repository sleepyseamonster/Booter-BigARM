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
                    || airborne != null && airborne.Active || turning != null && turning.Active);
        }
        private void RequestDodge()
        {
            if (!DodgeReady) { playerAction?.ReportFeedback("Dodge calibration is unavailable."); return; }
            if (!dodge.TryDodge()) playerAction.ReportFeedback(dodge.LastFailure);
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
