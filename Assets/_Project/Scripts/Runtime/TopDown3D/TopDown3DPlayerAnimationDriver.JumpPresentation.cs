using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private bool jumpPresentation,jumpComposingUnderlay,jumpGrounded,jumpPaused;
        private float jumpActionWeight=1,jumpPreviousWeight=1;
        private float jumpRawSourceHeight,jumpSourceRate=1;
        private float jumpSupportBlend=1,jumpSupportFrom=1,jumpSupportElapsed,jumpGroundVisualDelta;
        private bool jumpHadGroundSupportPose;
        private Collider jumpPredictionCollider;
        private Plane jumpPredictionPlane;
        private float jumpPredictionBlend,jumpPredictionPreviousBlend,jumpSupportDuration=.06f;
        private double jumpPredictionSampleTime=double.NegativeInfinity;
        private Plane jumpLandingReferencePlane;
        private bool jumpLandingReferenceValid;
        private float jumpLiftSourceTime=-1;
        private bool jumpRenderGroundBoundary;
        private Plane jumpLastSupportPlane;
        private Collider jumpLastSupportCollider;
        private bool JumpPresentationSupportCurrent=>JumpPredictionCurrent || JumpPresentationOwned && jumpRenderGroundBoundary
            && jumpLastSupportCollider!=null && jumpLastSupportCollider.enabled && jumpLastSupportCollider.gameObject.activeInHierarchy;
        private bool JumpPredictionCurrent=>JumpPresentationOwned && !jumpGrounded && jumpPredictionCollider!=null
            && jumpPredictionCollider.enabled && jumpPredictionCollider.gameObject.activeInHierarchy && jumpPredictionBlend>0;
        private double jumpPreviousSourceTime,jumpPoseSampleTime=double.NegativeInfinity,jumpWeightSampleTime=double.NegativeInfinity;
        public bool JumpPresentationOwned=>jumpPresentation && AnimationPresentationOwned;
        public bool ConfigureJumpPresentation(object owner,float initialActionWeight=0,float liftSourceTime=-1)
        {
            if(!IsAnimationPresentationOwner(owner) || motor==null || !motor.JumpLocomotionActive)return false;
            if(!jumpPresentation)
            {jumpActionWeight=jumpPreviousWeight=initialActionWeight;jumpPreviousSourceTime=animationPresentationTime;
                jumpGrounded=motor.IsGrounded;jumpSupportBlend=jumpSupportFrom=jumpGrounded ? 1 : 0;jumpSupportElapsed=0;
                jumpGroundVisualDelta=0;jumpHadGroundSupportPose=false;jumpSourceRate=1;
                jumpPredictionCollider=null;jumpPredictionBlend=jumpPredictionPreviousBlend=0;jumpPredictionSampleTime=double.NegativeInfinity;
                jumpLandingReferenceValid=false;
                jumpLastSupportCollider=null;jumpRenderGroundBoundary=false;
                jumpPoseSampleTime=double.NegativeInfinity;jumpWeightSampleTime=double.NegativeInfinity;}
            jumpLiftSourceTime=liftSourceTime;jumpPresentation=true;return true;
        }
        public bool SetJumpPresentationState(object owner,float actionWeight,bool grounded,bool paused)
        {
            if(!IsAnimationPresentationOwner(owner) || !JumpPresentationOwned || !float.IsFinite(actionWeight))return false;
            if(jumpWeightSampleTime!=Time.fixedTimeAsDouble)
            {jumpPreviousWeight=jumpActionWeight;jumpWeightSampleTime=Time.fixedTimeAsDouble;}
            if(jumpGrounded!=grounded)
            {
                jumpSupportFrom=jumpSupportBlend;jumpSupportElapsed=0;
                jumpSupportDuration=grounded && JumpPredictionCurrent ? Time.fixedDeltaTime : .06f;
                if(grounded)
                {
                    jumpLandingReferenceValid=JumpPredictionCurrent;
                    jumpLandingReferencePlane=jumpPredictionPlane;jumpPredictionCollider=null;
                }
            }
            jumpActionWeight=Mathf.Clamp01(actionWeight);jumpGrounded=grounded;jumpPaused=paused;
            animationFootProjection=grounded;return true;
        }
        public bool SetJumpSupportPrediction(object owner,Collider support,Plane plane,float blend)
        {
            if(!IsAnimationPresentationOwner(owner) || !JumpPresentationOwned || !float.IsFinite(blend) || blend<0 || blend>1)return false;
            if(support!=null && (!float.IsFinite(plane.distance+plane.normal.sqrMagnitude)
                || Mathf.Abs(plane.normal.sqrMagnitude-1)>.0001f || plane.normal.y<.6691f))return false;
            if(jumpGrounded){jumpPredictionCollider=null;return true;}
            if(JumpPredictionCurrent && support==null)
            {jumpSupportFrom=jumpSupportBlend;jumpSupportElapsed=0;jumpSupportDuration=.06f;}
            if(jumpPredictionSampleTime!=Time.fixedTimeAsDouble)
            {jumpPredictionPreviousBlend=jumpPredictionBlend;jumpPredictionSampleTime=Time.fixedTimeAsDouble;}
            jumpPredictionCollider=support;jumpPredictionPlane=plane;jumpPredictionBlend=support!=null ? blend : 0;
            return true;
        }
        public bool EndJumpPresentationMode(object owner)
        {
            if(!IsAnimationPresentationOwner(owner))return false;
            jumpPresentation=false;jumpComposingUnderlay=false;jumpPredictionCollider=null;return true;
        }
        private float JumpRenderAlpha()=>jumpPaused ? 1 : Mathf.Clamp01((float)((Time.timeAsDouble-Time.fixedTimeAsDouble)/Math.Max(.0001,Time.fixedDeltaTime)));
        private float JumpFeetReferenceY(float groundedY)
        {
            if(!JumpPresentationOwned)return groundedY;
            var predicted=JumpPredictionCurrent;var boundary=JumpPresentationSupportCurrent && jumpRenderGroundBoundary;
            if(!predicted && !boundary && !(jumpGrounded && jumpLandingReferenceValid && jumpSupportElapsed<.06f))return groundedY;
            var plane=predicted ? jumpPredictionPlane : boundary ? jumpLastSupportPlane : jumpLandingReferencePlane;
            var foot=transform.TransformPoint(Vector3.up*laboratoryFeetPlane);
            var rest=foot.y-plane.GetDistanceToPoint(foot)/plane.normal.y
                +motor.JumpCapsuleRadius*(1/plane.normal.y-1);
            return predicted || boundary ? rest : Mathf.Lerp(rest,groundedY,Mathf.SmoothStep(0,1,jumpSupportElapsed/.06f));
        }
        private float JumpSnapshotRate(AnimationClip clip)
        {
            if(clip==null)return 0;
            if(animationActionPlayable.IsValid() && animationActionPlayable.GetAnimationClip()==clip)return jumpSourceRate;
            if(laboratoryForwardProfile!=null)
            {
                foreach(var gait in laboratoryForwardProfile.gaits)
                    if(gait.sourceClip==clip)return GaitSnapshotRate(gait);
                if(standingStrafeReview!=null && standingStrafeReview.directionalGaits!=null)
                    foreach(var gait in standingStrafeReview.directionalGaits)
                        if(gait.sourceClip==clip)return GaitSnapshotRate(gait);
            }
            return clip.isLooping ? 1 : 0;
        }
        private float GaitSnapshotRate(AnimationPlaygroundForwardGait gait)
        {
            const double seconds=.001;
            var cycle=LaboratoryRenderedCycles;
            var next=motor!=null && motor.LaboratoryStandingLateral
                ? cycle+forwardSpeed*seconds/DirectionalCycleDistance(forwardSpeed)
                : laboratoryForwardProfile.AdvanceCycles(forwardSpeed,cycle,forwardSpeed*seconds);
            var delta=gait.SourceSeconds(next)-gait.SourceSeconds(cycle);
            if(delta<0)delta+=gait.sourceClip.length;
            return Mathf.Clamp((float)(delta/seconds),0,3);
        }
        private bool PresentJumpAction(bool fixedPose=false)
            =>PresentJumpActionAt(fixedPose ? 1 : JumpRenderAlpha(),fixedPose ? 0 : Time.deltaTime);
        private bool PresentJumpActionAt(float alpha,float renderDelta)
        {
            if(!float.IsFinite(alpha+renderDelta) || alpha<0 || alpha>1 || renderDelta<0)
                throw new ArgumentException("Jump rendering needs a finite interpolation and nonnegative frame duration.");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var previous=jumpPreviousSourceTime;var current=animationPresentationTime;
            var actionClip=animationActionPlayable.GetAnimationClip();
            if(actionClip.isLooping && current<previous)previous-=actionClip.length;
            var seconds=Math.Max(0,previous+(current-previous)*alpha);
            jumpRenderGroundBoundary=!jumpGrounded && jumpLiftSourceTime>=0 && seconds<=jumpLiftSourceTime+.000001
                && jumpLastSupportCollider!=null;
            if(jumpRenderGroundBoundary){jumpSupportElapsed=0;jumpSupportFrom=jumpSupportBlend=1;}
            else
            {
                if(!jumpPaused)jumpSupportElapsed+=renderDelta;
                jumpSupportBlend=JumpPredictionCurrent ? Mathf.Lerp(jumpPredictionPreviousBlend,jumpPredictionBlend,alpha)
                    : Mathf.Lerp(jumpSupportFrom,jumpGrounded ? 1 : 0,Mathf.SmoothStep(0,1,jumpSupportElapsed/Mathf.Max(.0001f,jumpSupportDuration)));
            }
            animationActionPlayable.SetTime(seconds);
            var actionWeight=Mathf.Lerp(jumpPreviousWeight,jumpActionWeight,alpha);
            var sourceHeight=animationHeightCorrection;
            if(actionWeight<.99999f && LaboratoryForwardReady && forwardClock!=null)
            {
                jumpComposingUnderlay=true;
                try
                {
                    var speed=Mathf.Lerp(forwardPreviousSpeed,forwardSpeed,alpha);
                    if(speed<.001f && motor.JumpObservation.Supported)speed=Vector3.ProjectOnPlane(motor.JumpObservation.Velocity,motor.JumpObservation.SupportNormal).magnitude;
                    var cycles=forwardClock.RenderCycles(alpha,forwardSpeed,laboratoryForwardProfile);
                    var lateral=0f;for(var i=6;i<DirectionalChannels;i++)lateral+=directionalWeights[i];
                    if(LaboratoryDirectionalConfigured && (lateral>.0001f || AnimationPlaygroundDirectionalMath.Weights(directionalLocal).x<.99999f))
                        PresentDirectionalFrame(speed,cycles,jumpPaused ? 0 : renderDelta);
                    else PresentForwardFrame(speed,cycles,jumpPaused ? 0 : renderDelta);
                    sourceHeight=Mathf.Lerp(visualInstance.transform.localPosition.y-visualLocalPosition.y,animationHeightCorrection,actionWeight);
                    for(var i=0;i<mixer.GetInputCount();i++)mixer.SetInputWeight(i,mixer.GetInputWeight(i)*(1-actionWeight));
                }
                finally{jumpComposingUnderlay=false;}
            }
            else
            {
                actionWeight=1;
                for(var i=0;i<mixer.GetInputCount();i++)mixer.SetInputWeight(i,0);
            }
            mixer.SetInputWeight(AnimationActionIndex,actionWeight);
            visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition+Vector3.up*sourceHeight,Quaternion.Euler(visualLocalEulerAngles));
            ApplyPoseBlend(true);
            jumpRawSourceHeight=visualInstance.transform.localPosition.y-visualLocalPosition.y;
            if(jumpGrounded && motor.IsGrounded || JumpPresentationSupportCurrent)
            {
                PrepareContinuationActionTerrain();ProjectLaboratoryFootPlane();
                jumpGroundVisualDelta=visualInstance.transform.localPosition.y-visualLocalPosition.y-jumpRawSourceHeight;
                visualInstance.transform.localPosition=visualLocalPosition+Vector3.up*(jumpRawSourceHeight+jumpGroundVisualDelta*jumpSupportBlend);
                jumpHadGroundSupportPose=forwardTerrainApplied;
                if(jumpGrounded)
                {jumpLastSupportPlane=forwardTerrainPlane;jumpLastSupportCollider=continuationActionSupport ?? motor.LaboratorySupportCollider;}
            }
            else
            {
                if(!jumpHadGroundSupportPose || forwardTerrainPose?.ReleaseSupport(jumpSupportBlend)!=true)forwardTerrainPose?.Disable();
                visualInstance.transform.localPosition+=Vector3.up*(jumpGroundVisualDelta*jumpSupportBlend);
                forwardTerrainApplied=false;LastPoseBlendGroundCorrection=0;
            }
            ApplyGarmentFloor();
            playableGraph.Evaluate(0);
            CurrentLocomotionState=TopDown3DLocomotionState.ConstrainedAction;
            return true;
        }
        public bool TryEmitJumpLandingContact(object owner,float speed)
        {
            if(!IsAnimationPresentationOwner(owner) || !JumpPresentationOwned || !motor.JumpObservation.Supported
                || !float.IsFinite(speed) || speed<0)return false;
            PresentJumpAction(true);playableGraph.Evaluate(0);
            var side=JumpContactSide();
            forwardClock?.RebaseContact(side);
            forwardPlantCount=forwardPlantStart=0;
            return EmitFootContact(side,side==TopDown3DFootSide.Left ? leftFoot : rightFoot,speed);
        }
        private TopDown3DFootSide JumpContactSide()
        {
            if(footProjectionSampler!=null && laboratoryFootProjection!=null && humanoidAnimator!=null)
            {
                int vertex;
                if(nativeGarmentHeight!=null)footProjectionSampler.MinimumTerrainDistance(nativeGarmentHeight,out vertex);
                else footProjectionSampler.MinimumWorldY(out vertex);
                if(vertex>=0 && vertex<laboratoryFootProjection.vertices.Length)
                {
                    var left=humanoidAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                    var right=humanoidAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                    var balance=0f;
                    foreach(var point in laboratoryFootProjection.vertices[vertex].points)
                    {
                        var path=laboratoryFootProjection.bonePaths[point.bone];
                        var bone=path.Length==0 ? visualInstance.transform : visualInstance.transform.Find(path);
                        if(bone!=null && left!=null && bone.IsChildOf(left))balance+=point.weight;
                        if(bone!=null && right!=null && bone.IsChildOf(right))balance-=point.weight;
                    }
                    if(Mathf.Abs(balance)>.00001f)return balance>0 ? TopDown3DFootSide.Left : TopDown3DFootSide.Right;
                }
            }
            return leftFoot!=null && rightFoot!=null && leftFoot.position.y<rightFoot.position.y ? TopDown3DFootSide.Left : TopDown3DFootSide.Right;
        }
    }
}
