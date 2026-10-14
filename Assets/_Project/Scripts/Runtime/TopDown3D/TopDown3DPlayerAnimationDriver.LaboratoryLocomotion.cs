using System;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        // Session-only diagnostic opt-in. It does not change the serialized twelve-role
        // profile or authorize the experimental family for the author's player.
        public bool LaboratorySustainedDiagnostic { get; private set; }

        public void SetLaboratorySustainedDiagnostic(bool enabled)
        {
            if (enabled && laboratoryAvatar == null)
                throw new InvalidOperationException("Sustained diagnostics require a configured laboratory character.");
            if (LaboratorySustainedDiagnostic == enabled) return;
            LaboratorySustainedDiagnostic = enabled;
            ResetTransitionState(TopDown3DLocomotionState.Idle);
            lastActionIndex = -1;
        }
    }
}
