using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Operates only on the driver's readable runtime garment copy.
    public sealed class AnimationPlaygroundGarmentFloor
    {
        private readonly SkinnedMeshRenderer skin;
        private readonly Mesh mesh;
        private readonly Vector3[] original,positions,originalNormals,normals;
        private readonly BoneWeight[] weights;
        private readonly Matrix4x4[] bindposes;
        private readonly Bounds originalBounds;
        private readonly Dictionary<float,Matrix4x4> inverses=new();
        private bool applied;
        public float MaximumDisplacement {get;private set;}
        public bool ExceededLimit {get;private set;}
        public AnimationPlaygroundGarmentFloor(SkinnedMeshRenderer renderer)
        {
            if(renderer==null || renderer.sharedMesh==null || !renderer.sharedMesh.isReadable || renderer.bones.Length!=2)
                throw new ArgumentException("A readable two-bone runtime garment is required.");
            skin=renderer;mesh=skin.sharedMesh;original=mesh.vertices;positions=(Vector3[])original.Clone();
            originalBounds=skin.localBounds;
            originalNormals=mesh.normals;normals=(Vector3[])originalNormals.Clone();weights=mesh.boneWeights;bindposes=mesh.bindposes;
            if(weights.Length!=original.Length || bindposes.Length!=2)throw new ArgumentException("Garment bind data is incomplete.");
        }
        public static Matrix4x4 Blend(Matrix4x4 chest,Matrix4x4 hips,float chestWeight)
        {
            var value=new Matrix4x4();
            for(var i=0;i<16;i++)value[i]=chest[i]*chestWeight+hips[i]*(1-chestWeight);
            return value;
        }
        public static bool TryProject(Vector3 original,Matrix4x4 skinMatrix,Plane plane,float clearance,float maximum,out Vector3 projected,out float displacement)
        {
            projected=original;displacement=Mathf.Max(0,clearance-plane.GetDistanceToPoint(skinMatrix.MultiplyPoint3x4(original)));
            if(!float.IsFinite(displacement) || displacement>maximum || Mathf.Abs(skinMatrix.determinant)<.000001f)return false;
            if(displacement<=0)return true;
            projected=skinMatrix.inverse.MultiplyPoint3x4(skinMatrix.MultiplyPoint3x4(original)+plane.normal*displacement);
            return float.IsFinite(projected.x) && float.IsFinite(projected.y) && float.IsFinite(projected.z);
        }
        public void Apply(Plane plane,float clearance,float maximum)
            => ApplySurface(plane,clearance,maximum,null);
        public void ApplyTerrain(Plane plane,float clearance,float maximum,Func<Vector3,float?> heightAt)
        {
            if(heightAt==null)throw new ArgumentNullException(nameof(heightAt));
            ApplySurface(plane,clearance,maximum,heightAt);
        }
        private void ApplySurface(Plane plane,float clearance,float maximum,Func<Vector3,float?> heightAt)
        {
            if(!float.IsFinite(clearance) || clearance<0 || !float.IsFinite(maximum) || maximum<=0)throw new ArgumentException("Bounded garment clearance is required.");
            MaximumDisplacement=0;ExceededLimit=false;inverses.Clear();
            applied=true;
            var chest=skin.bones[0].localToWorldMatrix*bindposes[0];var hips=skin.bones[1].localToWorldMatrix*bindposes[1];
            var limit=skin.quality==SkinQuality.Auto ? (int)QualitySettings.skinWeights : (int)skin.quality;
            for(var i=0;i<positions.Length;i++)
            {
                var weight=weights[i];
                var chestWeight=limit==1 ? weight.boneIndex0==0 ? 1f : 0f
                    : (weight.boneIndex0==0 ? weight.weight0 : 0)+(weight.boneIndex1==0 ? weight.weight1 : 0);
                var world=chest.MultiplyPoint3x4(original[i])*chestWeight+hips.MultiplyPoint3x4(original[i])*(1-chestWeight);
                positions[i]=original[i];if(normals.Length==positions.Length)normals[i]=originalNormals[i];
                var distance=Mathf.Max(0,clearance-plane.GetDistanceToPoint(world));
                var direction=plane.normal;
                if(heightAt!=null)
                {
                    direction=Vector3.up;distance/=plane.normal.y;
                    var height=heightAt(world);
                    if(height.HasValue)
                    {
                        if(!float.IsFinite(height.Value)){ExceededLimit=true;continue;}
                        distance=Mathf.Max(distance,height.Value+clearance-world.y);
                    }
                }
                if(!float.IsFinite(distance)){ExceededLimit=true;continue;}
                MaximumDisplacement=Mathf.Max(MaximumDisplacement,distance);
                if(distance<=0)continue;
                if(distance>maximum){ExceededLimit=true;continue;}
                if(!inverses.TryGetValue(chestWeight,out var inverse))
                {
                    var matrix=Blend(chest,hips,chestWeight);
                    if(Mathf.Abs(matrix.determinant)<.000001f){ExceededLimit=true;continue;}
                    inverse=matrix.inverse;inverses.Add(chestWeight,inverse);
                }
                positions[i]=inverse.MultiplyPoint3x4(world+direction*distance);
                if(normals.Length==positions.Length)normals[i]=inverse.MultiplyVector(direction).normalized;
            }
            mesh.vertices=positions;if(normals.Length==positions.Length)mesh.normals=normals;mesh.RecalculateBounds();
            var bounds=originalBounds;bounds.Encapsulate(mesh.bounds.min);bounds.Encapsulate(mesh.bounds.max);skin.localBounds=bounds;
        }
        public void Restore()
        {
            if(mesh==null || !applied)return;
            mesh.vertices=original;if(originalNormals.Length==original.Length)mesh.normals=originalNormals;mesh.RecalculateBounds();skin.localBounds=originalBounds;
            MaximumDisplacement=0;ExceededLimit=false;
            applied=false;
        }
    }
}
