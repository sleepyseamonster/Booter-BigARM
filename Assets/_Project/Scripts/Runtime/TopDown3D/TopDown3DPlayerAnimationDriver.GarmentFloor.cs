using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private readonly List<AnimationPlaygroundGarmentFloor> garmentFloors=new();
        private bool animationGarmentFloor,poseBlendGarmentFloor;
        private float garmentMaximum=.6f,garmentClearance=.01f;
        private Plane? garmentFixturePlane;
        public float LastGarmentFloorDisplacement {get;private set;}
        public bool GarmentFloorExceeded {get;private set;}
        public static bool IsSoftGarment(SkinnedMeshRenderer skin)=>skin!=null && skin.name=="Cloak cloth skin";
        public bool SetAnimationGarmentFloor(object owner,bool enabled,float maximum=.6f,float clearance=.01f)
        {
            if(owner==null || !ReferenceEquals(owner,animationPresentationOwner) || animationPreview
                || !float.IsFinite(maximum) || maximum<=0 || maximum>1 || !float.IsFinite(clearance) || clearance<0 || clearance>.05f)return false;
            NormalizeCampSkinWeights();
            animationGarmentFloor=enabled;garmentMaximum=maximum;garmentClearance=clearance;return true;
        }
        private void ApplyGarmentFloor()
        {
            if(forwardTerrainApplied && !ForwardTerrainPoseCurrent)
            {forwardTerrainApplied=false;forwardTerrainPose?.Disable();RestoreGarmentFloor();return;}
            if(!animationGarmentFloor && !poseBlendGarmentFloor && !forwardTerrainApplied){RestoreGarmentFloor();return;}
            if(!cloakBound || visualInstance==null)return;
            NormalizeCampSkinWeights();
            if(garmentFloors.Count==0)
                foreach(var skin in visualInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if(IsSoftGarment(skin))garmentFloors.Add(new AnimationPlaygroundGarmentFloor(skin));
            Plane plane;
            if(forwardTerrainApplied)plane=forwardTerrainPlane;
            else if(garmentFixturePlane.HasValue)plane=garmentFixturePlane.Value;
            else if(motor==null || !motor.TryGetLaboratoryGroundPlane(out plane)){RestoreGarmentFloor();return;}
            if(!forwardTerrainApplied)playableGraph.Evaluate(0);
            LastGarmentFloorDisplacement=0;GarmentFloorExceeded=false;
            foreach(var garment in garmentFloors)
            {
                garment.Apply(plane,forwardTerrainApplied ? .001f : garmentClearance,
                    forwardTerrainApplied ? laboratoryForwardProfile.maximumTerrainGarmentDisplacement : garmentMaximum);
                LastGarmentFloorDisplacement=Mathf.Max(LastGarmentFloorDisplacement,garment.MaximumDisplacement);
                GarmentFloorExceeded|=garment.ExceededLimit;
            }
        }
        private void RestoreGarmentFloor()
        {
            foreach(var garment in garmentFloors)garment.Restore();
            LastGarmentFloorDisplacement=0;GarmentFloorExceeded=false;
        }
    }
}
