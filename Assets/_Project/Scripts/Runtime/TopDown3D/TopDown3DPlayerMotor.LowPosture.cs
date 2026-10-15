using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        public bool AnimationLowShapeActive=>animationShapeCaptured && capsule!=null && capsule.direction==2;
        public bool LaboratoryModalPaused=>input!=null && input.Mode!=TopDown3DInputMode.Gameplay;
        public bool TryGetLaboratoryGroundPlane(out Plane plane)
        {
            plane=default;
            if(animationPostureOwner==null && animationMotionOwner==null)return false;
            EnsureAnimationShape();var origin=AnimationCurrentFootPoint()+Vector3.up*.1f;int count;
            while(true)
            {
                count=Physics.RaycastNonAlloc(origin,Vector3.down,animationReturnHits,.4f,groundMask,QueryTriggerInteraction.Ignore);
                if(count<animationReturnHits.Length)break;
                if(animationReturnHits.Length>=4096)return false;
                Array.Resize(ref animationReturnHits,animationReturnHits.Length*2);
            }
            var nearest=float.PositiveInfinity;var found=false;
            for(var i=0;i<count;i++)
            {
                var hit=animationReturnHits[i];
                if(hit.collider==null || hit.collider.transform.IsChildOf(transform) || hit.distance>=nearest
                    || hit.collider.GetComponentInParent<TopDown3DGroundSurface>()==null || !TopDown3DSlopeMath.IsWalkable(hit.normal,maxWalkableSlope))continue;
                nearest=hit.distance;plane=new Plane(hit.normal,hit.point);found=true;
            }
            return found;
        }

        // A longitudinal capsule retains the authored foot plane and follows the motor's facing.
        public bool TrySetAnimationLowShape(object owner,float length,float radius,float forwardOffset)
        {
            if(owner==null || !IsAnimationMotionOwner(owner) && !IsAnimationPostureOwner(owner))return false;
            EnsureAnimationShape();
            if(!float.IsFinite(length) || !float.IsFinite(radius) || !float.IsFinite(forwardOffset)
                || radius<.1f || radius>.8f || length<radius*2 || length>3
                || radius*2>animationOriginalHeight || Mathf.Abs(forwardOffset)>1)return false;
            var center=animationOriginalCenter+new Vector3(0,radius-animationOriginalHeight*.5f,forwardOffset);
            if(!AnimationLowShapeClear(length,radius,center))return false;
            capsule.direction=2;capsule.height=length;capsule.radius=radius;capsule.center=center;
            AnimationPosture=AnimationPlaygroundPosture.Prone;return true;
        }

        private void AnimationLowShapeGeometry(float length,float radius,Vector3 center,float margin,
            out Vector3 a,out Vector3 b,out float worldRadius)
        {
            var scale=transform.lossyScale;
            worldRadius=Mathf.Max(.01f,radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y))-margin);
            var segment=Mathf.Max(0,length*Mathf.Abs(scale.z)*.5f-worldRadius-margin);
            var middle=body.position+transform.rotation*Vector3.Scale(center,scale);
            a=middle+transform.forward*segment;b=middle-transform.forward*segment;
        }
        private bool AnimationLowShapeClear(float length,float radius,Vector3 center)
        {
            AnimationLowShapeGeometry(length,radius,center,.02f,out var a,out var b,out var worldRadius);
            int count;
            while(true)
            {
                count=Physics.OverlapCapsuleNonAlloc(a,b,worldRadius,animationShapeHits,~0,QueryTriggerInteraction.Ignore);
                if(count<animationShapeHits.Length)break;
                if(animationShapeHits.Length>=4096)return false;
                Array.Resize(ref animationShapeHits,animationShapeHits.Length*2);
            }
            for(var i=0;i<count;i++)
                if(animationShapeHits[i]!=null && !animationShapeHits[i].transform.IsChildOf(transform))return false;
            return true;
        }
        private bool TryGetAnimationLowGroundNormal(out Vector3 normal)
        {
            normal=Vector3.up;
            AnimationLowShapeGeometry(capsule.height,capsule.radius,capsule.center,0,out var a,out var b,out var radius);
            int count;
            while(true)
            {
                count=Physics.CapsuleCastNonAlloc(a,b,radius*.88f,Vector3.down,animationReturnHits,
                    radius*.12f+groundProbeDistance,groundMask,QueryTriggerInteraction.Ignore);
                if(count<animationReturnHits.Length)break;
                if(animationReturnHits.Length>=4096)return false;
                Array.Resize(ref animationReturnHits,animationReturnHits.Length*2);
            }
            var nearest=float.PositiveInfinity;Collider support=null;var contact=Vector3.zero;
            for(var i=0;i<count;i++)
            {
                var hit=animationReturnHits[i];
                if(hit.collider==null || hit.collider.transform.IsChildOf(transform)
                    || hit.collider.GetComponentInParent<TopDown3DGroundSurface>()==null || hit.distance>=nearest)continue;
                nearest=hit.distance;support=hit.collider;contact=hit.point;
            }
            return support!=null && TryReadTrueSurfaceNormal(contact+Vector3.up*GroundNormalRayLift,
                GroundNormalRayLift*2+groundProbeDistance,support,out normal,out _)
                && TopDown3DSlopeMath.IsWalkable(normal,maxWalkableSlope);
        }
        private Vector3 AnimationCurrentFootPoint()
        {
            var extent=capsule.direction==2 ? capsule.radius : capsule.height*.5f;
            return body.position+transform.rotation*Vector3.Scale(capsule.center-Vector3.up*extent,transform.lossyScale);
        }
    }
}
