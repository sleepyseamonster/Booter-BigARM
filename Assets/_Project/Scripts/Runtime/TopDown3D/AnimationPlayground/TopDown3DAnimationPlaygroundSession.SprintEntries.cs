using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DAnimationPlaygroundSession
    {
        private AnimationPlaygroundSprintEntryController sprintEntries;
        public bool SprintEntriesReady => servicesBound && isActiveAndEnabled && PreviewReady && sprintEntries != null && sprintEntries.Ready;
        private void BindSprintEntries()
        {
            if (bindings.sprintEntries == null) return;
            sprintEntries = player.gameObject.AddComponent<AnimationPlaygroundSprintEntryController>();
            sprintEntries.Configure(player, presentation, coordinator, crouch, bindings.sprintEntries, ResetRegistry,
                () => !isActiveAndEnabled || !servicesBound || !PreviewReady || !Application.isFocused
                    || playerInput.Mode != TopDown3DInputMode.Gameplay,
                () => playerAction.IsBusy || dodge != null && dodge.Active || lowMotion != null && lowMotion.Active
                    || airborne != null && airborne.Active || turning != null && turning.Active, playerAction.ReportFeedback);
        }
        private void DetachSprintEntries()
        {
            if (sprintEntries == null) return;
            sprintEntries.DisposeOwnedServices();
            if (Application.isPlaying) Destroy(sprintEntries); else DestroyImmediate(sprintEntries);
            sprintEntries = null;
        }
    }
}
