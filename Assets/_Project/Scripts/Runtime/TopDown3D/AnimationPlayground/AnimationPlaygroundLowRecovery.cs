using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Retains the existing motor/output after a blocked controller is disposed.
    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundLowRecovery : MonoBehaviour
    {
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private Vector3 facing;
        private AnimationPlaygroundLowMotionProfile profile;
        private AnimationPlaygroundLowMotionEntry entry;
        private float poseTime;
        private bool settling,disposed;
        private float elapsed;
        private const float BlendSeconds=.12f;
        public bool Active=>motor!=null && !disposed;
        public AnimationPlaygroundLowMotionRole? CurrentRole=>Active && entry!=null ? entry.role : null;
        private bool Paused=>motor.LaboratoryModalPaused || Application.isPlaying && !Application.isFocused;
        public bool TryAdopt(TopDown3DPlayerMotor actor,TopDown3DPlayerAnimationDriver presentation,
            object postureOwner,object blendOwner,AnimationPlaygroundLowMotionProfile calibration,
            AnimationPlaygroundLowMotionRole role,float time,Vector3 heading)
        {
            if(motor!=null || actor==null || presentation==null || postureOwner==null || blendOwner==null
                || calibration==null || !calibration.Validate(out _,false)
                || !actor.IsAnimationPostureOwner(postureOwner) || actor.IsActionConstrained)return false;
            var pose=calibration.Get(role);
            if(pose==null || !float.IsFinite(time) || time<0 || time>pose.clip.length)return false;
            if(!actor.TransferAnimationPostureOwner(postureOwner,this))return false;
            if(!actor.TryBeginAnimationMotion(this,AnimationPlaygroundMotionMode.Recovery))
            {actor.TransferAnimationPostureOwner(this,postureOwner);return false;}
            if(!presentation.TransferPoseBlendOwner(blendOwner,this) || !presentation.TryBeginAnimationPresentation(this,pose.clip))
            {
                actor.ReleaseAnimationMotion(this);actor.TransferAnimationPostureOwner(this,postureOwner);
                presentation.TransferPoseBlendOwner(this,blendOwner);return false;
            }
            try
            {
                if(!presentation.SetAnimationPresentationTime(this,time) || !presentation.SetAnimationBodyProjection(this,calibration.bodyProjection))
                    throw new InvalidOperationException("The retained low pose lost its paired presentation.");
                motor=actor;driver=presentation;facing=heading;profile=calibration;entry=pose;poseTime=time;
                motor.AnimationMotionPreparing+=Prepare;motor.AnimationMotionAdvanced+=Advance;
                motor.SetLaboratoryPostureSuspended(this,true);return true;
            }
            catch
            {
                presentation.EndAnimationPresentation(this);actor.ReleaseAnimationMotion(this);
                actor.TransferAnimationPostureOwner(this,postureOwner);presentation.TransferPoseBlendOwner(this,blendOwner);throw;
            }
        }
        private void Prepare()
        {
            if(!Active || !motor.IsAnimationMotionOwner(this))return;
            if(!settling && !Paused)
            {
                var next=Mathf.Min(entry.clip.length,poseTime+Time.fixedDeltaTime);
                if(Shape(entry,next))poseTime=next;
                driver.SetAnimationPresentationTime(this,poseTime);
                driver.SetPoseBlendElapsed(this,poseTime);
            }
            motor.SetAnimationMotorIntent(this,new AnimationPlaygroundMotorIntent(Vector3.zero,facing,paused:Paused));
        }
        private bool Shape(AnimationPlaygroundLowMotionEntry value,float time)=>value.horizontal
            ? motor.TrySetAnimationLowShape(this,profile.lowLength,profile.lowRadius,profile.lowForwardOffset)
            : motor.TrySetAnimationPosture(this,AnimationPlaygroundPosture.Crouched,
                value.capsuleHeight.Evaluate(Mathf.Clamp01(time/value.clip.length)),profile.verticalRadius);
        private bool NextStage(AnimationPlaygroundLowMotionRole role)
        {
            var next=profile.Get(role);
            if(!Shape(next,0) || !driver.TryCapturePoseBlend(this,BlendSeconds)
                || !driver.TryReplaceAnimationPresentation(this,next.clip))return false;
            driver.SetAnimationBodyProjection(this,profile.bodyProjection);entry=next;poseTime=0;return true;
        }
        private void Advance()
        {
            if(!Active || !motor.IsAnimationMotionOwner(this) || Paused)return;
            if(!settling)
            {
                if(poseTime<entry.clip.length-.000001f || !motor.IsGrounded)return;
                var next=entry.role switch{
                    AnimationPlaygroundLowMotionRole.SlideEnter=>AnimationPlaygroundLowMotionRole.SlideActive,
                    AnimationPlaygroundLowMotionRole.RollContact=>AnimationPlaygroundLowMotionRole.RollActive,
                    AnimationPlaygroundLowMotionRole.SlideActive=>AnimationPlaygroundLowMotionRole.SlideRecover,
                    AnimationPlaygroundLowMotionRole.RollActive=>AnimationPlaygroundLowMotionRole.RollRecover,
                    _=>(AnimationPlaygroundLowMotionRole?)null};
                if(next.HasValue){NextStage(next.Value);return;}
                if(!motor.IsGrounded || !motor.IsStandingAnimationShapeClear())return;
                if(!driver.TryCapturePoseBlend(this,BlendSeconds) || !motor.TryRestoreAnimationPosture(this))return;
                driver.EndAnimationPresentation(this);settling=true;elapsed=0;
            }
            elapsed+=Time.fixedDeltaTime;driver.SetPoseBlendElapsed(this,elapsed);
            if(elapsed>=BlendSeconds)DisposeOwnedServices();
        }
        public void DisposeOwnedServices()
        {
            if(disposed)return;disposed=true;
            if(motor!=null)
            {
                motor.AnimationMotionPreparing-=Prepare;motor.AnimationMotionAdvanced-=Advance;
                driver?.EndAnimationPresentation(this);driver?.EndPoseBlend(this);
                motor.ReleaseAnimationMotion(this);motor.SetLaboratoryPostureSuspended(this,false);motor.ReleaseAnimationPostureOwner(this);
            }
            motor=null;driver=null;
            if(Application.isPlaying)Destroy(this);else if(this!=null)DestroyImmediate(this);
        }
        private void OnDestroy()
        {
            if(disposed)return;disposed=true;
            if(motor==null)return;
            motor.AnimationMotionPreparing-=Prepare;motor.AnimationMotionAdvanced-=Advance;
            driver?.EndAnimationPresentation(this);driver?.EndPoseBlend(this);
            motor.ReleaseAnimationMotion(this);motor.SetLaboratoryPostureSuspended(this,false);motor.ReleaseAnimationPostureOwner(this);
        }
    }
}
