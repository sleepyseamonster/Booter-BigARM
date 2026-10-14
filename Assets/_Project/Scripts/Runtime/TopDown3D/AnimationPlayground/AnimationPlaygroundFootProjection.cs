using System;
using UnityEngine;
namespace BooterBigArm.TopDown3D
{
    [Serializable] public struct AnimationPlaygroundSkinPoint
    {public int bone;public Vector3 position;public float weight;}
    [Serializable] public sealed class AnimationPlaygroundFootVertex
    {public AnimationPlaygroundSkinPoint[] points=Array.Empty<AnimationPlaygroundSkinPoint>();}
    public sealed class AnimationPlaygroundFootProjection : ScriptableObject
    {
        public int schema=1;
        public GameObject model;
        public string[] bonePaths=Array.Empty<string>();
        public AnimationPlaygroundFootVertex[] vertices=Array.Empty<AnimationPlaygroundFootVertex>();
        public bool Validate(GameObject expected,out string reason)
        {
            reason=null;
            if(schema!=1 || model==null || model!=expected || bonePaths==null || bonePaths.Length==0 || vertices==null || vertices.Length==0)
            {reason="A paired foot projection for the selected target model is required.";return false;}
            foreach(var path in bonePaths)if(path==null){reason="Foot projection bone paths are missing.";return false;}
            foreach(var vertex in vertices)
            {
                if(vertex==null || vertex.points==null || vertex.points.Length==0){reason="Foot projection vertices are incomplete.";return false;}
                var sum=0f;
                foreach(var point in vertex.points)
                {
                    if(point.bone<0 || point.bone>=bonePaths.Length || !float.IsFinite(point.weight) || point.weight<=0
                        || !float.IsFinite(point.position.x) || !float.IsFinite(point.position.y) || !float.IsFinite(point.position.z))
                    {reason="Foot projection influences are invalid.";return false;}
                    sum+=point.weight;
                }
                if(Mathf.Abs(sum-1)>.001f){reason="Foot projection influences must retain complete normalized weights.";return false;}
            }
            return true;
        }
        public Sampler Bind(Transform visual)
        {
            string reason=null;
            if(visual==null || !Validate(model,out reason))throw new ArgumentException(reason ?? "The active visual is missing.");
            return new Sampler(this,visual);
        }
        public sealed class Sampler
        {
            private readonly AnimationPlaygroundFootProjection data;
            private readonly Transform[] bones;
            private readonly Matrix4x4[] matrices;
            private readonly SkinnedMeshRenderer[] renderers;
            internal Sampler(AnimationPlaygroundFootProjection source,Transform visual)
            {
                data=source;bones=new Transform[source.bonePaths.Length];matrices=new Matrix4x4[bones.Length];
                renderers=visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for(var i=0;i<bones.Length;i++)
                {
                    bones[i]=source.bonePaths[i].Length==0 ? visual : visual.Find(source.bonePaths[i]);
                    if(bones[i]==null)throw new ArgumentException("The foot projection bone is absent: "+source.bonePaths[i]);
                }
            }
            public float MinimumWorldY()
            {
                var influenceLimit=int.MaxValue;
                foreach(var renderer in renderers)
                {
                    var limit=renderer.quality==SkinQuality.Auto ? (int)QualitySettings.skinWeights : (int)renderer.quality;
                    if(influenceLimit!=int.MaxValue && influenceLimit!=limit)
                        throw new InvalidOperationException("The paired foot projection requires one consistent skin-weight quality.");
                    influenceLimit=limit;
                }
                for(var i=0;i<bones.Length;i++)
                {
                    if(bones[i]==null)throw new InvalidOperationException("Foot projection ownership lost a target bone.");
                    matrices[i]=bones[i].localToWorldMatrix;
                }
                var minimum=float.PositiveInfinity;
                foreach(var vertex in data.vertices)
                {
                    var point=Vector3.zero;var total=0f;
                    for(var index=0;index<vertex.points.Length;index++)
                    {
                        var influence=vertex.points[index];var rank=0;
                        if(vertex.points.Length>influenceLimit)
                            for(var other=0;other<vertex.points.Length;other++)
                                if(vertex.points[other].weight>influence.weight || vertex.points[other].weight==influence.weight && other<index)rank++;
                        if(rank>=influenceLimit)continue;
                        point+=matrices[influence.bone].MultiplyPoint3x4(influence.position)*influence.weight;total+=influence.weight;
                    }
                    point/=total;
                    if(!float.IsFinite(point.y))throw new InvalidOperationException("The active foot projection is not finite.");
                    minimum=Mathf.Min(minimum,point.y);
                }
                return minimum;
            }
        }
    }
}
