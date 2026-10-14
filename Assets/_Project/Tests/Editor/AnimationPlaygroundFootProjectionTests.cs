using System;
using BooterBigArm.TopDown3D;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundFootProjectionTests
    {
        [Test] public void ProjectionPreservesWeightedBoneMotionAndVisualScale()
        {
            var model=new GameObject("Projection model");var a=new GameObject("A");var b=new GameObject("B");
            a.transform.SetParent(model.transform,false);b.transform.SetParent(model.transform,false);
            var data=ScriptableObject.CreateInstance<AnimationPlaygroundFootProjection>();
            try
            {
                data.model=model;data.bonePaths=new[]{"A","B"};data.vertices=new[]{new AnimationPlaygroundFootVertex{points=new[]{
                    new AnimationPlaygroundSkinPoint{bone=0,position=Vector3.down,weight=.25f},
                    new AnimationPlaygroundSkinPoint{bone=1,position=Vector3.up,weight=.75f}}}};
                Assert.That(data.Validate(model,out var error),Is.True,error);var sampler=data.Bind(model.transform);
                model.transform.localScale=Vector3.one*2;a.transform.localPosition=Vector3.up*3;b.transform.localPosition=Vector3.up;
                Assert.That(sampler.MinimumWorldY(),Is.EqualTo(4).Within(.00001f));
                b.transform.localPosition=Vector3.down;Assert.That(sampler.MinimumWorldY(),Is.EqualTo(1).Within(.00001f));
                var skin=model.AddComponent<SkinnedMeshRenderer>();skin.quality=SkinQuality.Bone1;sampler=data.Bind(model.transform);
                Assert.That(sampler.MinimumWorldY(),Is.Zero.Within(.00001f));
                skin.quality=SkinQuality.Bone2;Assert.That(sampler.MinimumWorldY(),Is.EqualTo(1).Within(.00001f));
            }
            finally{Object.DestroyImmediate(data);Object.DestroyImmediate(model);}
        }
        [Test] public void ProjectionRejectsWrongModelsMissingBonesAndIncompleteWeights()
        {
            var model=new GameObject("Projection model");var wrong=new GameObject("Other model");
            var data=ScriptableObject.CreateInstance<AnimationPlaygroundFootProjection>();
            try
            {
                data.model=model;data.bonePaths=new[]{"Missing"};data.vertices=new[]{new AnimationPlaygroundFootVertex{points=new[]{
                    new AnimationPlaygroundSkinPoint{bone=0,position=Vector3.zero,weight=1}}}};
                Assert.That(data.Validate(wrong,out _),Is.False);Assert.Throws<ArgumentException>(()=>data.Bind(model.transform));
                data.vertices[0].points[0].weight=.5f;Assert.That(data.Validate(model,out _),Is.False);
                data.vertices[0].points[0].weight=1;data.vertices[0].points[0].bone=1;Assert.That(data.Validate(model,out _),Is.False);
            }
            finally{Object.DestroyImmediate(data);Object.DestroyImmediate(model);Object.DestroyImmediate(wrong);}
        }
        [Test,Category("PrivateAnimationQualification")] public void QualifiedCrouchCannotBootstrapWithoutItsPairedProjection()
        {
            var source=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
            Assert.That(source,Is.Not.Null,"Restore the sealed private binding before this gate.");
            var bindings=Object.Instantiate(source);var profile=Object.Instantiate(source.crouch);
            try
            {
                bindings.crouch=profile;profile.contactsQualified=true;profile.groundingQualified=true;
                Assert.That(bindings.ValidatePlayer(out var error),Is.True,error);
                bindings.footProjection=null;
                Assert.That(bindings.ValidatePlayer(out error),Is.False);
                Assert.That(error,Does.Contain("paired transition foot projection"));
            }
            finally{Object.DestroyImmediate(bindings);Object.DestroyImmediate(profile);}
        }
    }
}
