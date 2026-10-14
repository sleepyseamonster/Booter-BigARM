using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundActionCoordinator : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver presentation;
        private AnimationPlaygroundResetRegistry registry;
        private AnimationPlaygroundActionClock clock;
        private AnimationPlaygroundMotionBinding preview;
        private TopDown3DInteractionTarget? capturedTarget;
        private Func<double,AnimationPlaygroundMotorIntent> motion;
        private Func<AnimationPlaygroundEffectKey,bool> effect;
        private Func<bool> pause;
        private Vector3[] returnPositions;
        private Vector3 initialFacing;
        private double previewTime;
        private ulong nextAction;
        private bool holdAtEnd,completionSent;
        public bool IsBusy => clock!=null || preview!=null;
        public bool IsPreview => preview!=null;
        public string LastFailure {get;private set;}
        public string ResetId => "CampLoop.Booter.Action";
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.Actor;
        public event Action ActionCompleted;

        // The scene bootstrap gates configuration on private readiness. Fixtures can supply synthetic art.
        public void Configure(TopDown3DPlayerMotor playerMotor,TopDown3DPlayerAnimationDriver driver,
            AnimationPlaygroundResetRegistry resetRegistry,Vector3[] safeReturns,Func<bool> shouldPause=null)
        {
            if(motor!=null || playerMotor==null || driver==null || resetRegistry==null || safeReturns==null || safeReturns.Length==0)
                throw new ArgumentException("Configure one motor/presentation owner with authored return positions.");
            resetRegistry.Register(this);
            try{playerMotor.BindAnimationPostureOwner(this);}
            catch{resetRegistry.Unregister(this);throw;}
            motor=playerMotor;presentation=driver;registry=resetRegistry;returnPositions=(Vector3[])safeReturns.Clone();
            initialFacing=motor.FacingDirection;pause=shouldPause;
            motor.AnimationMotionPreparing+=PrepareMotion;motor.AnimationMotionAdvanced+=CommitEffects;
        }

        public bool TryBeginPhysical(AnimationPlaygroundMotionBinding binding,AnimationPlaygroundMotionMode mode,
            AnimationPlaygroundMarker[] markers,Func<double,AnimationPlaygroundMotorIntent> motionAtTime,
            Func<AnimationPlaygroundEffectKey,bool> commitEffect,TopDown3DInteractionTarget? target=null,
            int cycles=1,bool disableGravity=false,bool terminalHold=false)
        {
            if(binding==null || !binding.CanPerform || IsBusy || motor==null || motionAtTime==null || target.HasValue && !target.Value.IsValid)
                return false;
            var candidate=new AnimationPlaygroundActionClock(++nextAction,registry.Epoch,binding.clip.length,cycles,markers);
            if(markers!=null && markers.Length>0 && commitEffect==null)throw new ArgumentException("Effect markers require an atomic idempotent commit adapter.");
            if(!Acquire(binding.clip,mode,disableGravity,false))return false;
            clock=candidate;motion=motionAtTime;effect=commitEffect;capturedTarget=target;
            holdAtEnd=terminalHold;completionSent=false;LastFailure=null;return true;
        }

        public bool TryBeginPreview(AnimationPlaygroundMotionBinding binding)
        {
            if(binding==null || !binding.CanPreview || IsBusy || motor==null || !Acquire(binding.clip,AnimationPlaygroundMotionMode.AlignedAction,true,true))return false;
            preview=binding;previewTime=0;LastFailure=null;return true;
        }

        public bool SeekPreview(double seconds)
        {
            if(preview==null || !double.IsFinite(seconds) || seconds<0)return false;
            previewTime=Math.Min(seconds,preview.clip.length);return presentation.SetAnimationPresentationTime(this,previewTime);
        }

        private bool Acquire(AnimationClip clip,AnimationPlaygroundMotionMode mode,bool disableGravity,bool isPreview)
        {
            if(!motor.TryBeginAnimationMotion(this,mode,disableGravity))return false;
            if(presentation.TryBeginAnimationPresentation(this,clip,isPreview))return true;
            motor.ReleaseAnimationMotion(this);return false;
        }

        private void PrepareMotion()
        {
            try{PrepareMotionCore();}
            catch(Exception error){Cancel("Action preparation failed: "+error.Message);Debug.LogException(error,this);}
        }
        private void PrepareMotionCore()
        {
            if(!IsBusy)return;
            if(capturedTarget.HasValue && !capturedTarget.Value.IsValid || clock!=null && clock.Epoch!=registry.Epoch)
            {Cancel("The captured target or session epoch changed.");return;}
            var paused=pause?.Invoke()==true;
            if(preview!=null)
            {
                if(preview.clip==null || preview.clip.length<=0){Cancel("Preview clip is unavailable.");return;}
                if(!paused)previewTime+=Time.fixedDeltaTime;
                var time=preview.loop ? previewTime%preview.clip.length : Math.Min(previewTime,preview.clip.length);
                presentation.SetAnimationPresentationTime(this,time);
                motor.SetAnimationMotorIntent(this,new AnimationPlaygroundMotorIntent(Vector3.zero,motor.FacingDirection,true,paused:true));
                return;
            }
            clock.Paused=paused;clock.Advance(Time.fixedDeltaTime);
            var intent=motion(clock.Elapsed);
            if(paused)intent=new AnimationPlaygroundMotorIntent(Vector3.zero,intent.Facing,intent.OwnsVertical,intent.Phase,true);
            if(!motor.SetAnimationMotorIntent(this,intent)){Cancel("Action supplied an invalid motor intent.");return;}
            presentation.SetAnimationPresentationTime(this,clock.ClipTime);
        }

        private void CommitEffects()
        {
            try{CommitEffectsCore();}
            catch(Exception error){Cancel("Action effect failed: "+error.Message);Debug.LogException(error,this);}
        }
        private void CommitEffectsCore()
        {
            if(clock==null || clock.Paused)return;
            var active=clock;
            while(active.TryTake(out var key))
            {
                if(capturedTarget.HasValue && !capturedTarget.Value.IsValid || active.Epoch!=registry.Epoch)
                {Cancel("Effect target or epoch is stale.");return;}
                // Adapters must commit atomically by key; false means no effect was applied.
                if(effect==null || !effect(key)){Cancel("Action effect could not commit.");return;}
                if(!ReferenceEquals(clock,active))return;
                if(!active.Commit(key)){Cancel("Action effect ledger rejected its claimed marker.");return;}
                if(!ReferenceEquals(clock,active))return;
            }
            if(!active.Complete || completionSent)return;
            completionSent=true;ActionCompleted?.Invoke();
            if(!holdAtEnd && ReferenceEquals(clock,active))Cancel();
        }

        public void Cancel(string reason=null)
        {
            clock?.Cancel();clock=null;preview=null;capturedTarget=null;motion=null;effect=null;
            if(presentation!=null)presentation.EndAnimationPresentation(this);
            if(motor!=null)motor.ReleaseAnimationMotion(this);
            LastFailure=reason;
        }
        public void InvalidateForReset(uint epoch)=>Cancel();
        public bool TryRestoreForReset(out string reason)
        {
            foreach(var position in returnPositions)
                if(motor.IsAnimationReturnSupported(position) && motor.TryResetAnimationActor(this,position,initialFacing)){reason=null;return true;}
            reason="No authored return position has ground support and standing clearance.";return false;
        }
        private void OnDisable()=>Cancel();
        public void DisposeOwnedServices()
        {
            Cancel();
            if(motor!=null){motor.AnimationMotionPreparing-=PrepareMotion;motor.AnimationMotionAdvanced-=CommitEffects;motor.ReleaseAnimationPostureOwner(this);}
            registry?.Unregister(this);
            motor=null;presentation=null;registry=null;
        }
        private void OnDestroy()=>DisposeOwnedServices();
    }
}
