using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DAnimationPlaygroundSession
    {
        private AnimationPlaygroundLowMotionController lowMotion;
        public bool SlideReady=>servicesBound && isActiveAndEnabled && PreviewReady && lowMotion!=null
            && bindings?.lowMotion!=null && bindings.lowMotion.qualified;
        private void BindLowMotion()
        {
            if(bindings.lowMotion==null)return;
            lowMotion=player.gameObject.AddComponent<AnimationPlaygroundLowMotionController>();
            lowMotion.Configure(player,presentation,coordinator,bindings.lowMotion,ResetRegistry,
                ()=>!isActiveAndEnabled || !servicesBound || !PreviewReady || !Application.isFocused
                    || playerInput.Mode!=TopDown3DInputMode.Gameplay,()=>playerAction.IsBusy || crouch!=null && crouch.Active,
                owner=>crouch!=null && crouch.TryReceiveLowRecovery(owner));
        }
        private void RequestSlide()
        {
            if(!SlideReady){playerAction?.ReportFeedback("Sliding and roll qualification is still pending.");return;}
            if(!lowMotion.TrySlide())playerAction.ReportFeedback(lowMotion.LastFailure);
        }
        private bool TrySelectedRollLanding(float speed)
        {
            var selected=bindings.Find(selectedSource);
            if(!SlideReady || selected?.CanPreview!=true || selected.clip.name!="FallRollLanding")return false;
            if(bindings.continuationReview!=null && presentation.LaboratoryContinuationConfigured)
            {
                var planar=Vector3.ProjectOnPlane(player.Velocity,Vector3.up);
                if(planar.magnitude>3.8f || planar.magnitude>.1f && Vector3.Dot(planar.normalized,player.FacingDirection)<.7f
                    || !presentation.TryGetContinuationLandingPlane(out var plane) || Vector3.Angle(plane.normal,Vector3.up)>.25f)return false;
            }
            return lowMotion.TryTakeLandingSequence(airborne,speed);
        }
        private void DetachLowMotion()
        {
            if(lowMotion==null)return;
            lowMotion.DisposeOwnedServices();
            if(Application.isPlaying)Destroy(lowMotion);else DestroyImmediate(lowMotion);
            lowMotion=null;
        }
    }
}
