using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DAnimationPlaygroundSession
    {
        private AnimationPlaygroundAirborneController airborne;
        private AnimationPlaygroundTurnController turning;
        public bool JumpReady=>servicesBound && isActiveAndEnabled && PreviewReady && airborne!=null && bindings!=null
            && bindings.airborne!=null && bindings.airborne.qualified && bindings.footProjection!=null;
        private void BindAirborne()
        {
            if(bindings.airborne==null)return;
            airborne=player.gameObject.AddComponent<AnimationPlaygroundAirborneController>();
            airborne.Configure(player,presentation,coordinator,bindings.airborne,ResetRegistry,
                ()=>!isActiveAndEnabled || !servicesBound || !PreviewReady || !Application.isFocused
                    || playerInput.Mode!=TopDown3DInputMode.Gameplay,()=>playerAction.IsBusy || crouch!=null && crouch.Active || lowMotion!=null && lowMotion.Active);
            airborne.LandingRecovery=TrySelectedRollLanding;
            if(bindings.continuationReview!=null && presentation.LaboratoryContinuationConfigured)airborne.ConfigureContinuationReview(bindings.continuationReview);
            airborne.Landed+=CommitLandingDamage;
        }
        private void CommitLandingDamage(float speed)
        {
            var damage=bindings.airborne.LandingDamage(speed);
            if(damage<=0)return;
            var vitals=player.GetComponent<TopDown3DSurvivalVitals>();
            vitals.SetValue(TopDown3DSurvivalVital.Health,vitals.Health-damage);
            playerAction.ReportFeedback("Hard landing.");
        }
        private void RequestJump()
        {
            if(!JumpReady){playerAction?.ReportFeedback("Jump and landing qualification is still pending.");return;}
            var selected=bindings.Find(selectedSource);
            var unarmed=selected?.CanPreview==true && selected.clip.name==bindings.airborne.unarmed.sourceId;
            if(!airborne.TryJump(unarmed))playerAction.ReportFeedback(airborne.LastFailure);
        }
        private void DetachAirborne()
        {
            if(airborne==null)return;
            airborne.Landed-=CommitLandingDamage;
            airborne.DisposeOwnedServices();
            if(Application.isPlaying)Destroy(airborne);else DestroyImmediate(airborne);
            airborne=null;
        }
    }
}
