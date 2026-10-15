using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        private bool laboratoryTurnReview;
        private Func<bool> laboratoryTurnSupport;
        public bool LaboratoryTurnBraking {get;private set;}
        public void ConfigureLaboratoryTurnReview(object owner,bool enabled,Func<bool> supportAvailable=null)
        {
            if(!IsAnimationPostureOwner(owner) || enabled && supportAvailable==null)
                throw new ArgumentException("The laboratory posture owner and scoped support configure turns.");
            laboratoryTurnReview=enabled;laboratoryTurnSupport=enabled ? supportAvailable : null;LaboratoryTurnBraking=false;
        }
        private void ApplyLaboratoryTurnBraking(ref Vector3 direction)
        {
            LaboratoryTurnBraking=laboratoryTurnReview && animationPostureOwner!=null && IsGrounded && !standingStrafeLocked
                && !laboratoryPostureSuspended && AnimationPosture==AnimationPlaygroundPosture.Standing
                && input!=null && !input.LaboratoryStandingFacingHeld && !input.LaboratoryStandingFacingDisarmed && direction.sqrMagnitude>.01f
                && TryGetLaboratorySupportPlane(out var plane,true) && Vector3.Angle(plane.normal,Vector3.up)<=.25f
                && Vector3.Angle(body.rotation*Vector3.forward,Vector3.ProjectOnPlane(direction,Vector3.up))>=150
                && laboratoryTurnSupport?.Invoke()==true;
            if(LaboratoryTurnBraking)direction=Vector3.zero;
        }
    }
}
