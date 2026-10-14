using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundPresentationTests
    {
        private GameObject actor,visual,targetObject;
        private AnimationPlaygroundActionCoordinator coordinator;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationClip clip;
        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private readonly object owner=new();
        private static void Set(object instance,string name,object value)=>instance.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(instance,value);
        [SetUp] public void Setup()
        {
            actor=new GameObject("Synthetic presentation fixture");actor.SetActive(false);actor.transform.position=new Vector3(25000,1,25000);
            driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();
            var shape=actor.GetComponent<CapsuleCollider>();shape.height=2;shape.radius=.5f;shape.center=Vector3.zero;
            visual=new GameObject("Visual");visual.transform.SetParent(actor.transform,false);var animator=visual.AddComponent<Animator>();
            clip=new AnimationClip();clip.SetCurve("",typeof(Transform),"m_LocalPosition.x",AnimationCurve.Linear(0,0,1,1));
            graph=PlayableGraph.Create("Owned presentation fixture");mixer=AnimationMixerPlayable.Create(graph,22);
            var output=AnimationPlayableOutput.Create(graph,"Fixture",animator);output.SetSourcePlayable(mixer);graph.Play();
            Set(driver,"motor",actor.GetComponent<TopDown3DPlayerMotor>());Set(driver,"visualInstance",visual);
            Set(driver,"visualLocalPosition",Vector3.zero);
            Set(driver,"playableGraph",graph);Set(driver,"mixer",mixer);
        }
        [TearDown] public void Cleanup()
        {
            if(coordinator!=null)typeof(AnimationPlaygroundActionCoordinator).GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(coordinator,null);
            driver.EndAnimationPresentation(owner);
            if(graph.IsValid())graph.Destroy();
            Object.DestroyImmediate(targetObject);Object.DestroyImmediate(actor);Object.DestroyImmediate(clip);
        }
        [Test] public void OwnedClockSupportsPreviewSeekAndSuppressesAllLegacyMixerInputs()
        {
            Assert.That(driver.TryBeginAnimationPresentation(owner,clip,true),Is.True);
            Assert.That(driver.TryBeginAnimationPresentation(new object(),clip),Is.False);
            foreach(var time in new[]{.8,.2})
            {
                Assert.That(driver.SetAnimationPresentationTime(owner,time),Is.True);
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(driver,null);
                Assert.That(mixer.GetInput(21).GetTime(),Is.EqualTo(time).Within(.00001));
                Assert.That(mixer.GetInput(21).GetSpeed(),Is.Zero);
                for(var i=0;i<21;i++)Assert.That(mixer.GetInputWeight(i),Is.Zero);
                Assert.That(mixer.GetInputWeight(21),Is.EqualTo(1));
            }
            Assert.That(driver.TryEmitAnimationFootContact(owner,TopDown3DFootSide.Left,2),Is.False);
            Assert.That(driver.EndAnimationPresentation(new object()),Is.False);
            Assert.That(driver.EndAnimationPresentation(owner),Is.True);Assert.That(driver.AnimationPresentationOwned,Is.False);
            Assert.That(mixer.GetInputWeight(21),Is.Zero);
        }
        [Test] public void NullOwnersCannotClaimStateAndDisableReleasesTheDynamicPlayable()
        {
            Assert.That(driver.SetAnimationPresentationTime(null,0),Is.False);Assert.That(driver.EndAnimationPresentation(null),Is.False);
            driver.TryBeginAnimationPresentation(owner,clip);
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(driver,null);
            Assert.That(driver.AnimationPresentationOwned,Is.False);Assert.That(mixer.GetInput(21).IsValid(),Is.False);
            Assert.That(driver.TryBeginAnimationPresentation(owner,clip),Is.True);
        }
        [Test] public void MotorBoundaryOwnsTheClockEffectsPauseAndResetCancellation()
        {
            coordinator=actor.AddComponent<AnimationPlaygroundActionCoordinator>();var registry=new AnimationPlaygroundResetRegistry();var motor=actor.GetComponent<TopDown3DPlayerMotor>();
            targetObject=GameObject.CreatePrimitive(PrimitiveType.Cube);targetObject.name="Authored return support fixture";
            targetObject.transform.position=new Vector3(27000,-.1f,27000);targetObject.transform.localScale=new Vector3(4,.2f,4);Physics.SyncTransforms();
            var paused=true;coordinator.Configure(motor,driver,registry,new[]{new Vector3(27000,1,27000)},()=>paused);
            var binding=new AnimationPlaygroundMotionBinding{clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
            var count=0;var dt=Time.fixedDeltaTime;
            Assert.That(coordinator.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.AlignedAction,new[]{new AnimationPlaygroundMarker(1,dt*.5)},
                time=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),key=>{count++;return true;}),Is.True);
            var tick=typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            tick.Invoke(motor,null);Assert.That(count,Is.Zero);Assert.That(motor.GetComponent<Rigidbody>().useGravity,Is.False);
            paused=false;tick.Invoke(motor,null);tick.Invoke(motor,null);Assert.That(count,Is.EqualTo(1));
            Assert.That(registry.TryReset(),Is.True);Assert.That(coordinator.IsBusy,Is.False);
            Assert.That(driver.AnimationPresentationOwned,Is.False);Assert.That(motor.IsActionConstrained,Is.False);
            tick.Invoke(motor,null);Assert.That(count,Is.EqualTo(1));
        }
        [Test] public void StaleTargetCancelsBeforeEffectAndPreviewCannotPromoteAnImportToPhysical()
        {
            coordinator=actor.AddComponent<AnimationPlaygroundActionCoordinator>();var registry=new AnimationPlaygroundResetRegistry();var motor=actor.GetComponent<TopDown3DPlayerMotor>();
            coordinator.Configure(motor,driver,registry,new[]{new Vector3(27000,1,27000)});
            targetObject=new GameObject("Captured station");var target=targetObject.AddComponent<TopDown3DAnimationPlaygroundTarget>();
            target.Configure("fixture.station",AnimationPlaygroundActionKind.Button,"Press",target.transform,3,null);
            var binding=new AnimationPlaygroundMotionBinding{clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};var count=0;
            Assert.That(coordinator.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.AlignedAction,new[]{new AnimationPlaygroundMarker(1,0)},
                time=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),key=>{count++;return true;},new TopDown3DInteractionTarget(target)),Is.True);
            target.Invalidate();typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motor,null);
            Assert.That(count,Is.Zero);Assert.That(coordinator.IsBusy,Is.False);Assert.That(motor.IsActionConstrained,Is.False);
            binding.readiness=AnimationPlaygroundMotionReadiness.ImportQualified;
            Assert.That(coordinator.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.AlignedAction,null,time=>default,null),Is.False);
            Assert.That(coordinator.TryBeginPreview(binding),Is.True);Assert.That(coordinator.SeekPreview(.8),Is.True);Assert.That(coordinator.SeekPreview(.2),Is.True);
            registry.CancelAll();Assert.That(coordinator.IsBusy,Is.False);Assert.That(count,Is.Zero);
        }
        [Test] public void OnlyTheOwnedPhysicalBranchEmitsAContactAtTheMotorBoundary()
        {
            var motor=actor.GetComponent<TopDown3DPlayerMotor>();var registry=new AnimationPlaygroundResetRegistry();
            targetObject=new GameObject("Contact ground");targetObject.transform.position=actor.transform.position+Vector3.down*1.05f;
            targetObject.AddComponent<BoxCollider>().size=new Vector3(10,.1f,10);targetObject.AddComponent<TopDown3DGroundSurface>();Physics.SyncTransforms();
            var foot=new GameObject("Contact foot");foot.transform.SetParent(visual.transform);foot.transform.position=actor.transform.position+Vector3.down;
            Set(driver,"leftFoot",foot.transform);
            var entries=(TopDown3DLocomotionClipEntry[])typeof(TopDown3DPlayerAnimationDriver).GetField("locomotionEntries",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver);
            entries[3]=new TopDown3DLocomotionClipEntry();Set(entries[3],"nominalForwardSpeed",7.4f);
            coordinator=actor.AddComponent<AnimationPlaygroundActionCoordinator>();coordinator.Configure(motor,driver,registry,new[]{actor.transform.position});
            var binding=new AnimationPlaygroundMotionBinding{clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};var contacts=0;var diagnostic="effect not invoked";
            driver.FootContact+=contact=>contacts++;
            Assert.That(coordinator.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.AlignedAction,new[]{new AnimationPlaygroundMarker(1,0)},
                time=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),
                key=>
                {
                    var supported=motor.AnimationSnapshot.Supported;
                    var emitted=driver.TryEmitAnimationFootContact(coordinator,TopDown3DFootSide.Left,2);
                    var hit=Physics.Raycast(foot.transform.position+Vector3.up*.28f,Vector3.down,out var ground,.9f,~0,QueryTriggerInteraction.Ignore);
                    diagnostic=$"supported={supported} foot={foot.transform.position} ray={hit} hit={(hit ? ground.collider.name : "none")} emitted={emitted}";
                    return emitted;
                }),Is.True);
            var tick=typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            tick.Invoke(motor,null);tick.Invoke(motor,null);Assert.That(contacts,Is.EqualTo(1),diagnostic+" failure="+coordinator.LastFailure);
            coordinator.Cancel();Assert.That(coordinator.TryBeginPreview(binding),Is.True);
            tick.Invoke(motor,null);Assert.That(driver.TryEmitAnimationFootContact(coordinator,TopDown3DFootSide.Left,2),Is.False);
            Assert.That(contacts,Is.EqualTo(1));
        }
    }
}
