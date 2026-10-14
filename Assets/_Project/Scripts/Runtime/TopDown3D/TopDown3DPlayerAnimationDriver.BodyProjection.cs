using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        private bool campSkinWeightsNormalized;
        private void NormalizeCampSkinWeights()
        {
            if(campSkinWeightsNormalized || !cloakBound)return;
            foreach(var mesh in cloakMeshes)
            {
                // These are driver-owned runtime copies, bound only to chest and hips.
                var weights=mesh.boneWeights;
                for(var i=0;i<weights.Length;i++)
                {
                    var value=weights[i];
                    if(value.weight1>value.weight0)
                    {
                        (value.weight0,value.weight1)=(value.weight1,value.weight0);
                        (value.boneIndex0,value.boneIndex1)=(value.boneIndex1,value.boneIndex0);
                        weights[i]=value;
                    }
                }
                mesh.boneWeights=weights;
            }
            campSkinWeightsNormalized=true;
        }
    }
}
