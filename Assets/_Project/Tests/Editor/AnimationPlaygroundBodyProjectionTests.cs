using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundBodyProjectionTests
    {
        private GameObject actor,visual,wrong;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundFootProjection feet,body;
        private AnimationClip clip;
        private PlayableGraph graph;
        private readonly object owner=new();
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private static void Present(object target)=>target.GetType().GetMethod("PresentAnimationAction",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
        private AnimationPlaygroundFootProjection Data(float height)
        {
            var data=ScriptableObject.CreateInstance<AnimationPlaygroundFootProjection>();
            data.model=visual;data.bonePaths=new[]{""};data.vertices=new[]{new AnimationPlaygroundFootVertex{
                points=new[]{new AnimationPlaygroundSkinPoint{bone=0,position=Vector3.up*height,weight=1}}}};
            return data;
        }
        [SetUp] public void Setup()
        {
            actor=new GameObject("Owned projection fixture");actor.SetActive(false);actor.AddComponent<TopDown3DPlayerMotor>();
            driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();visual=new GameObject("Owned visual");visual.transform.SetParent(actor.transform,false);
            var animator=visual.AddComponent<Animator>();clip=new AnimationClip();
            graph=PlayableGraph.Create("Whole-body projection ownership");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var mixer=AnimationMixerPlayable.Create(graph,22);var idle=AnimationClipPlayable.Create(graph,clip);idle.SetSpeed(0);
            graph.Connect(idle,0,mixer,0);mixer.SetInputWeight(0,1);
            var output=AnimationPlayableOutput.Create(graph,"Single output",animator);output.SetSourcePlayable(mixer);graph.Play();
            Set(driver,"visualInstance",visual);Set(driver,"visualLocalPosition",Vector3.zero);Set(driver,"playableGraph",graph);Set(driver,"mixer",mixer);
            feet=Data(0);body=Data(-.08f);Set(driver,"laboratoryFootProjection",feet);Set(driver,"footProjectionSampler",feet.Bind(visual.transform));
        }
        [TearDown] public void Cleanup()
        {
            driver.EndAnimationPresentation(owner);if(graph.IsValid())graph.Destroy();
            Object.DestroyImmediate(actor);Object.DestroyImmediate(wrong);Object.DestroyImmediate(feet);Object.DestroyImmediate(body);Object.DestroyImmediate(clip);
        }
        [Test] public void ProjectionRequiresThePhysicalOwnerAndItsSelectedModel()
        {
            Assert.That(driver.SetAnimationBodyProjection(owner,body),Is.False);
            Assert.That(driver.TryBeginAnimationPresentation(owner,clip),Is.True);
            Assert.That(driver.SetAnimationBodyProjection(new object(),body),Is.False);
            wrong=new GameObject("Other model");body.model=wrong;Assert.That(driver.SetAnimationBodyProjection(owner,body),Is.False);
        }
        [Test] public void PreviewCannotAcquireBodyProjectionOrAlterTheActor()
        {
            Assert.That(driver.TryBeginAnimationPresentation(owner,clip,true),Is.True);
            Assert.That(driver.SetAnimationBodyProjection(owner,body),Is.False);Present(driver);
            Assert.That(visual.transform.localPosition,Is.EqualTo(Vector3.zero));
        }
        [Test] public void BodyProjectionUsesNonFootPointsAndReleasesBeforeTheNextAction()
        {
            Assert.That(driver.TryBeginAnimationPresentation(owner,clip),Is.True);
            Assert.That(driver.SetAnimationBodyProjection(owner,body),Is.True);Present(driver);
            Assert.That(driver.LastPoseBlendGroundCorrection,Is.EqualTo(.08f).Within(.00001f));
            Assert.That(visual.transform.localPosition.y,Is.EqualTo(.08f).Within(.00001f));Assert.That(graph.GetOutputCount(),Is.EqualTo(1));
            driver.EndAnimationPresentation(owner);Assert.That(driver.TryBeginAnimationPresentation(owner,clip),Is.True);
            driver.SetAnimationFootProjection(owner,true);Present(driver);
            Assert.That(driver.LastPoseBlendGroundCorrection,Is.Zero.Within(.00001f));
        }
        [Test] public void WholeBodyProjectionRetainsTheExistingCorrectionCeiling()
        {
            body.vertices[0].points[0].position=Vector3.down*.3f;
            Assert.That(driver.TryBeginAnimationPresentation(owner,clip),Is.True);
            Assert.That(driver.SetAnimationBodyProjection(owner,body),Is.True);
            var error=Assert.Throws<TargetInvocationException>(()=>Present(driver));
            Assert.That(error.InnerException.Message,Does.Contain("qualified envelope"));
        }
        [Test] public void OwnedRuntimeClothWeightsKeepTheirBonesWhenOrderedForSkinning()
        {
            var mesh=new Mesh();mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up};
            var original=new BoneWeight{boneIndex0=0,weight0=.2f,boneIndex1=1,weight1=.8f};
            mesh.boneWeights=new[]{original,original,original};
            var meshes=(System.Collections.Generic.List<Mesh>)driver.GetType().GetField("cloakMeshes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver);
            meshes.Add(mesh);Set(driver,"cloakBound",true);
            Assert.That(driver.TryBeginAnimationPresentation(owner,clip),Is.True);
            Assert.That(driver.SetAnimationBodyProjection(owner,body),Is.True);
            var result=mesh.boneWeights[0];
            Assert.That(result.boneIndex0,Is.EqualTo(1));Assert.That(result.weight0,Is.EqualTo(.8f).Within(.00001f));
            Assert.That(result.boneIndex1,Is.Zero);Assert.That(result.weight1,Is.EqualTo(.2f).Within(.00001f));
        }
    }
}
