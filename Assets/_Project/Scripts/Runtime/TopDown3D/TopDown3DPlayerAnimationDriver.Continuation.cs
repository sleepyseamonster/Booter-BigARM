using System;
using UnityEngine;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private AnimationPlaygroundContinuationProfile continuationReview;
        private AnimationPlaygroundTangentFootPose turnLeftPose, turnRightPose;
        private object turnPoseOwner;
        private bool turnPoseLeft;
        private Vector3 turnPoseAnchor;
        private Quaternion turnPoseBasis;
        private double turnPreviousSourceTime,turnSourceSampleTime=double.NegativeInfinity;
        private void ObserveAdaptedTurnSourceTime()
        {
            if(turnPoseOwner!=null && turnSourceSampleTime!=Time.fixedTimeAsDouble)
            {turnPreviousSourceTime=animationPresentationTime;turnSourceSampleTime=Time.fixedTimeAsDouble;}
        }
        private double SampleAdaptedTurnSourceTime(float alpha)
            =>turnPreviousSourceTime+(animationPresentationTime-turnPreviousSourceTime)*Mathf.Clamp01(alpha);
        private double AdaptedTurnPresentationTime=>turnPoseOwner!=null && Application.isPlaying
            ? SampleAdaptedTurnSourceTime(JumpRenderAlpha()) : animationPresentationTime;
        private void ApplyAdaptedTurnVisualHeading()
        {
            if(turnPoseOwner==null || continuationReview==null || visualInstance==null)return;
            var take=turnPoseLeft ? continuationReview.left : continuationReview.right;
            take.Sample((float)animationActionPlayable.GetTime(),out _,out var yaw);
            // Physics yaw can lead its interpolated translation. Present the
            // mesh at the exact source yaw rather than pulling its planted foot.
            visualInstance.transform.localRotation=Quaternion.Inverse(transform.rotation)*turnPoseBasis*yaw*Quaternion.Euler(visualLocalEulerAngles);
        }
        private TopDown3DAnimationPlaygroundStation continuationJumpStation;
        private bool continuationActionTerrainApplied;
        private object continuationActionTerrainOwner;
        private Collider continuationActionSupport;
        private Matrix4x4 continuationActionSupportMatrix;
        private int continuationActionFrame;
        private Collider continuationRejectedSupport;
        private Matrix4x4 continuationRejectedMatrix;
        public string LaboratoryContinuationFailure {get;private set;}
        private string ContinuationTurnStatus => LaboratoryContinuationConfigured
            && TryGetComponent<AnimationPlaygroundTurnController>(out var turn) && !string.IsNullOrEmpty(turn.LastFailure)
                ? "\n"+turn.LastFailure : null;
        public void ResetLaboratoryContinuationReview()
        {continuationRejectedSupport=null;LaboratoryContinuationFailure=null;LaboratoryNativeGroundAdapted=false;LaboratoryNativeGroundFailure=null;LaboratoryNativeBodyLift=0;}
        private readonly Vector2[] continuationFootprint = { new(-.3f,-.45f), new(.3f,-.45f), new(-.3f,.45f), new(.3f,.45f) };
        public bool LaboratoryContinuationConfigured => continuationReview != null;
        private static bool TurnContactFailed(AnimationPlaygroundTangentFootPose.ContactJob sample)
            =>sample.enabled && !sample.available && sample.unavailableReason!=0;
        public bool LaboratoryTurnContactUnavailable => turnPoseOwner != null
            && TurnContactFailed((turnPoseLeft ? turnLeftPose : turnRightPose).Sample);

        public void ConfigureLaboratoryContinuation(AnimationPlaygroundContinuationProfile profile)
        {
            if (!LaboratoryForwardReviewConfigured || playableGraph.IsValid() || profile == null
                || !profile.Validate(humanoidPrefab,laboratoryFootProjection,out _))
                throw new ArgumentException("Configure the paired Camp Loop continuation before visual startup.");
            continuationReview = profile;
            continuationJumpStation=Array.Find(forwardReviewSession.Stations,s=>s!=null && s.StationId=="AP04");
            ConfigureNativeCampSupport();
        }

        private bool ContinuationSurface(Collider support) => support!=null && support.gameObject.scene==gameObject.scene
            && support.attachedRigidbody==null && (support.GetComponentInParent<TopDown3DGroundSurface>()!=null
                || continuationJumpStation!=null && support.transform.IsChildOf(continuationJumpStation.transform));

        private bool ContinuationReviewSupportAvailable()
        {
            if (continuationReview == null || forwardReviewSession == null || !forwardReviewSession.isActiveAndEnabled
                || forwardReviewSession.Readiness == AnimationPlaygroundReadiness.Failed || motor == null
                || !motor.TryGetLaboratorySupportPlane(out var plane,true)
                || Vector3.Angle(plane.normal,Vector3.up) > continuationReview.maximumSlope+.001f) return false;
            var support = motor.LaboratorySupportCollider;
            if (continuationReview.NativeCampGround && ContinuationSurface(support)) return true;
            if(support!=null && support==continuationRejectedSupport && support.transform.localToWorldMatrix==continuationRejectedMatrix)return false;
            if (!ContinuationSurface(support)) return false;
            // Reject uneven support and edges rather than projecting a uniform
            // plane across a missing foot support. Terrain remains always loaded.
            var centre = plane.ClosestPointOnPlane(motor.Position);
            foreach (var offset in continuationFootprint)
            {
                var point = centre + transform.right*offset.x + transform.forward*offset.y;
                if (!Physics.Raycast(point+Vector3.up*.3f,Vector3.down,out var hit,.6f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                    || hit.collider.transform.IsChildOf(transform) || !ContinuationSurface(hit.collider)
                    || Mathf.Abs(plane.GetDistanceToPoint(hit.point)) > continuationReview.maximumSupportResidual
                    || Vector3.Angle(hit.normal,plane.normal) > 2) return false;
            }
            return true;
        }

        private void PrepareContinuationActionTerrain()
        {
            continuationActionTerrainApplied=false;
            if(continuationReview==null || forwardTerrainPose==null || !animationFootProjection && !JumpPresentationSupportCurrent
                || turnPoseOwner!=null || motor==null || !motor.IsGrounded && !JumpPresentationSupportCurrent
                || motor.AnimationPosture!=AnimationPlaygroundPosture.Standing
                || motor.AnimationMotionMode!=AnimationPlaygroundMotionMode.Jump && motor.AnimationMotionMode!=AnimationPlaygroundMotionMode.Recovery
                    && !(JumpPresentationOwned && motor.AnimationMotionMode==AnimationPlaygroundMotionMode.Fall))return;
            if(!TryContinuationActionSupport(out var plane,out var support))return;
            var foot=transform.TransformPoint(Vector3.up*laboratoryFeetPlane);
            continuationActionTerrainApplied=forwardTerrainApplied=true;forwardTerrainPlane=plane;
            continuationActionTerrainOwner=animationPresentationOwner;continuationActionSupport=support;
            continuationActionSupportMatrix=support.transform.localToWorldMatrix;continuationActionFrame=Time.frameCount;
            forwardTerrainPose.Configure(plane,JumpFeetReferenceY(foot.y));
            if(JumpPresentationOwned)forwardTerrainPose.SetSupportBlend(jumpSupportBlend);
        }
        public bool TryGetContinuationLandingPlane(out Plane plane)=>TryContinuationActionSupport(out plane,out _);
        private bool TryContinuationActionSupport(out Plane plane,out Collider support)
        {
            plane=default;support=null;
            if(JumpPredictionCurrent){plane=jumpPredictionPlane;support=jumpPredictionCollider;return true;}
            if(JumpPresentationSupportCurrent && jumpRenderGroundBoundary){plane=jumpLastSupportPlane;support=jumpLastSupportCollider;return true;}
            if(continuationReview==null || motor==null || !motor.IsGrounded)return false;
            var foot=transform.TransformPoint(Vector3.up*laboratoryFeetPlane);
            if(!Physics.Raycast(foot+Vector3.up*.25f,Vector3.down,out var hit,.55f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                || !ContinuationSurface(hit.collider) || Vector3.Angle(hit.normal,Vector3.up)>continuationReview.maximumSlope+.001f)return false;
            plane=new Plane(hit.normal,hit.point);
            if(continuationReview.NativeCampGround) { support=hit.collider; return true; }
            foreach(var offset in continuationFootprint)
            {
                var point=foot+transform.right*offset.x+transform.forward*offset.y;
                if(!Physics.Raycast(point+Vector3.up*.3f,Vector3.down,out var probe,.6f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                    || !ContinuationSurface(probe.collider) || Mathf.Abs(plane.GetDistanceToPoint(probe.point))>continuationReview.maximumSupportResidual
                    || Vector3.Angle(probe.normal,plane.normal)>2)return false;
            }
            support=hit.collider;return true;
        }
        private bool ContinuationActionTerrainCurrent => continuationActionTerrainApplied && AnimationPresentationOwned
            && ReferenceEquals(continuationActionTerrainOwner,animationPresentationOwner) && continuationActionFrame==Time.frameCount
            && (motor.IsGrounded || JumpPresentationSupportCurrent) && continuationActionSupport!=null && continuationActionSupport.enabled
            && continuationActionSupport.gameObject.activeInHierarchy && continuationActionSupport.attachedRigidbody==null
            && continuationActionSupport.transform.localToWorldMatrix==continuationActionSupportMatrix;

        private void RejectContinuationPose(string reason)
        {
            LaboratoryContinuationFailure=reason;continuationRejectedSupport=motor.LaboratorySupportCollider;
            if(continuationRejectedSupport!=null)continuationRejectedMatrix=continuationRejectedSupport.transform.localToWorldMatrix;
            forwardTerrainApplied=false;continuationActionTerrainApplied=false;forwardTerrainPose?.Disable();
            RestoreStandingStrafeBaseline();RestoreForwardReviewBaseline();RestoreGarmentFloor();
            visualInstance.transform.SetLocalPositionAndRotation(visualLocalPosition,Quaternion.Euler(visualLocalEulerAngles));
            playableGraph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.GameTime);
            forwardClock.Reset();forwardSpeed=forwardPreviousSpeed=0;forwardPlantCount=forwardPlantStart=0;
            ResetStandingStrafePresentation();
        }

        public bool TryBeginAdaptedTurnPose(object owner, bool left, Vector3 anchor)
        {
            if (continuationReview == null || !IsAnimationPresentationOwner(owner) || turnPoseOwner != null
                || !float.IsFinite(anchor.sqrMagnitude)) return false;
            turnLeftPose ??= new AnimationPlaygroundTangentFootPose(humanoidAnimator,visualInstance.transform,
                laboratoryFootProjection,continuationReview.left.soleVertex,playableGraph);
            turnRightPose ??= new AnimationPlaygroundTangentFootPose(humanoidAnimator,visualInstance.transform,
                laboratoryFootProjection,continuationReview.right.soleVertex,playableGraph);
            turnPoseOwner=owner;turnPoseLeft=left;turnPoseAnchor=anchor;
            turnPoseBasis=Quaternion.LookRotation(Vector3.ProjectOnPlane(motor.FacingDirection,Vector3.up).normalized,Vector3.up);
            turnPreviousSourceTime=animationPresentationTime;turnSourceSampleTime=double.NegativeInfinity;
            return true;
        }
        private void PrepareAdaptedTurnPose()
        {
            turnLeftPose?.Disable();turnRightPose?.Disable();
            if(turnPoseOwner==null || !IsAnimationPresentationOwner(turnPoseOwner)) {turnPoseOwner=null;return;}
            if(PoseBlendOwned || motor.LaboratoryModalPaused || Application.isPlaying && !Application.isFocused)return;
            var influences=(int)QualitySettings.skinWeights;
            (turnPoseLeft ? turnLeftPose : turnRightPose).Configure(turnPoseAnchor,influences<=1 ? 1 : influences<=2 ? 2 : 4);
        }
        private void ReleaseAdaptedTurnPose()
        {turnPoseOwner=null;turnPreviousSourceTime=0;turnSourceSampleTime=double.NegativeInfinity;turnLeftPose?.Disable();turnRightPose?.Disable();}
        private void DisposeAdaptedTurnPose()
        {
            ReleaseAdaptedTurnPose();turnRightPose?.Dispose();turnRightPose=null;turnLeftPose?.Dispose();turnLeftPose=null;
        }
    }
}
