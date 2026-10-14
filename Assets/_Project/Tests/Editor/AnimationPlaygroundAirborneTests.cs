using System;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundAirborneTests
    {
        private GameObject actor,floor;
        private Rigidbody body;
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerAnimationDriver driver;
        private AnimationPlaygroundActionCoordinator coordinator;
        private AnimationPlaygroundAirborneController airborne;
        private AnimationPlaygroundAirborneProfile profile;
        private AnimationPlaygroundResetRegistry registry;
        private PlayableGraph graph;
        private bool paused;
        private static void Set(object value,string name,object field)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(value,field);
        private static object Call(object value,string method)=>value.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);
        [SetUp] public void Setup()
        {
            paused=false;var source=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>(AnimationPlaygroundAirborneAuthoring.ProfilePath);
            Assert.That(source,Is.Not.Null);profile=Object.Instantiate(source);profile.qualified=true;
            actor=new GameObject("Airborne control fixture");actor.SetActive(false);actor.transform.position=new Vector3(68000,.91f,68000);
            motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();body=actor.GetComponent<Rigidbody>();
            var capsule=actor.GetComponent<CapsuleCollider>();capsule.height=1.82f;capsule.radius=.2f;capsule.center=Vector3.zero;
            var visual=new GameObject("Owned visual");visual.transform.SetParent(actor.transform,false);var animator=visual.AddComponent<Animator>();
            graph=PlayableGraph.Create("Airborne owned graph");var mixer=AnimationMixerPlayable.Create(graph,22);
            var idle=AnimationClipPlayable.Create(graph,profile.standing.takeoff);idle.SetSpeed(0);graph.Connect(idle,0,mixer,0);mixer.SetInputWeight(0,1);
            var output=AnimationPlayableOutput.Create(graph,"One output",animator);output.SetSourcePlayable(mixer);graph.Play();
            Set(driver,"motor",motor);Set(driver,"visualInstance",visual);Set(driver,"visualLocalPosition",Vector3.zero);Set(driver,"playableGraph",graph);Set(driver,"mixer",mixer);
            registry=new AnimationPlaygroundResetRegistry();coordinator=actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,registry,new[]{actor.transform.position},()=>paused);
            airborne=actor.AddComponent<AnimationPlaygroundAirborneController>();airborne.Configure(motor,driver,coordinator,profile,registry,()=>paused,()=>false);
            actor.SetActive(true);Set(motor,"<IsGrounded>k__BackingField",true);
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(68000,-.1f,68000);floor.transform.localScale=new Vector3(10,.2f,10);
            floor.AddComponent<TopDown3DGroundSurface>();Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup()
        {
            airborne?.DisposeOwnedServices();coordinator?.DisposeOwnedServices();if(graph.IsValid())graph.Destroy();
            Object.DestroyImmediate(actor);Object.DestroyImmediate(floor);Object.DestroyImmediate(profile);Physics.SyncTransforms();
        }
        private void Tick(int count=1){for(var i=0;i<count;i++)Call(motor,"FixedUpdate");}
        private void Launch()
        {
            Assert.That(airborne.TryJump(),Is.True,airborne.LastFailure);
            for(var i=0;i<100 && airborne.State==AnimationPlaygroundAirborneState.Takeoff;i++)Tick();
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Rising));Assert.That(body.linearVelocity.y,Is.EqualTo(profile.standing.upwardSpeed));
            body.position=new Vector3(68000,1.91f,68000);body.linearVelocity=Vector3.up*4;Physics.SyncTransforms();Tick();
        }
        [Test] public void JumpLaunchesOnceAndLandingRequiresSurfaceContactRatherThanClipEnd()
        {
            var landings=0;airborne.Landed+=speed=>landings++;Launch();Tick(60);
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Rising));Assert.That(landings,Is.Zero);
            body.linearVelocity=Vector3.down*3;Tick();Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Falling));
            body.position=new Vector3(68000,1.21f,68000);Physics.SyncTransforms();Tick();
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Approach));Tick(30);Assert.That(landings,Is.Zero);
            body.position=new Vector3(68000,.91f,68000);body.linearVelocity=Vector3.down*.1f;Physics.SyncTransforms();Tick();
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Landing));Assert.That(landings,Is.EqualTo(1));
            Tick(100);Assert.That(airborne.Active,Is.False);Assert.That(coordinator.IsBusy,Is.False);Assert.That(motor.IsActionConstrained,Is.False);
            Assert.That(graph.GetOutputCount(),Is.EqualTo(1));Assert.That(landings,Is.EqualTo(1));
        }
        [Test] public void PauseFreezesPoseAndCancelKeepsFallingPhysicsOwned()
        {
            Launch();var time=coordinator.PhysicalStageSeconds;
            var pose=(double)typeof(TopDown3DPlayerAnimationDriver).GetField("animationPresentationTime",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver);
            paused=true;Tick(10);Assert.That(coordinator.PhysicalStageSeconds,Is.EqualTo(time));
            Assert.That((double)typeof(TopDown3DPlayerAnimationDriver).GetField("animationPresentationTime",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver),Is.EqualTo(pose));
            paused=false;Assert.That(airborne.TryCancel(),Is.True);Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Falling));
            Assert.That(coordinator.IsSequenceOwner(airborne),Is.True);Tick(100);Assert.That(coordinator.IsBusy,Is.True);
        }
        [Test] public void ResetInvalidatesTheSequenceAndRestoresOnlyTheExistingActor()
        {
            Launch();Assert.That(registry.TryReset(),Is.True,registry.LastFailure);
            Assert.That(airborne.Active,Is.False);Assert.That(coordinator.IsBusy,Is.False);Assert.That(driver.PoseBlendOwned,Is.False);
            Assert.That(body.position,Is.EqualTo(new Vector3(68000,.91f,68000)));
        }
        [Test] public void ExternalCancellationCannotLeaveAStaleAirborneController()
        {
            Launch();coordinator.Cancel();body.linearVelocity=Vector3.down*2;Set(motor,"<IsGrounded>k__BackingField",false);
            Call(airborne,"ObserveLocomotion");
            Assert.That(airborne.State,Is.EqualTo(AnimationPlaygroundAirborneState.Falling));
            Assert.That(coordinator.IsSequenceOwner(airborne),Is.True);
        }
        [Test] public void UnqualifiedAirborneProfileAndCrouchedPostureCannotLaunch()
        {
            profile.qualified=false;Assert.That(airborne.TryJump(),Is.False);profile.qualified=true;
            Assert.That(motor.TrySetAnimationPosture(coordinator,AnimationPlaygroundPosture.Crouched,1.57f,.4f),Is.True);
            Assert.That(airborne.TryJump(),Is.False);Assert.That(coordinator.IsBusy,Is.False);Assert.That(body.linearVelocity.y,Is.Zero);
        }
        [Test] public void DraftRolesRetainHumanoidDataAndAirPosesHaveNoAnimatedVerticalArc()
        {
            var source=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>(AnimationPlaygroundAirborneAuthoring.ProfilePath);
            Assert.That(source.qualified,Is.False);
            var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
            foreach(var variant in new[]{source.standing,source.running,source.unarmed})
            {
                foreach(var clip in new[]{variant.takeoff,variant.rising,variant.landing})Assert.That(clip.isHumanMotion,Is.True);
                var measured=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),AssetDatabase.GetAssetPath(variant.rising),
                    bindings.visualScale,bindings.visualOffset,60);
                var min=float.PositiveInfinity;var max=float.NegativeInfinity;var sole=float.PositiveInfinity;
                foreach(var frame in measured.frames){min=Mathf.Min(min,frame.hips.y);max=Mathf.Max(max,frame.hips.y);sole=Mathf.Min(sole,Mathf.Min(frame.leftSole.y,frame.rightSole.y));}
                Assert.That(max-min,Is.LessThan(.00001f));Assert.That(sole,Is.GreaterThan(-.91001f));
            }
        }
    }
}
