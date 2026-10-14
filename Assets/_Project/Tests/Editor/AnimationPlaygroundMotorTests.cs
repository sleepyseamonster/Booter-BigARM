using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundMotorTests
    {
        private GameObject actor,obstacle;
        private TopDown3DPlayerMotor motor;
        private Rigidbody body;
        private CapsuleCollider shape;
        private readonly object owner=new();
        private static void Tick(TopDown3DPlayerMotor value)=>typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);
        [SetUp] public void Setup()
        {
            actor=new GameObject("Owned motor fixture");actor.SetActive(false);actor.transform.position=new Vector3(31000,1,31000);
            motor=actor.AddComponent<TopDown3DPlayerMotor>();body=actor.GetComponent<Rigidbody>();shape=actor.GetComponent<CapsuleCollider>();
            shape.height=2;shape.radius=.5f;shape.center=Vector3.zero;body.useGravity=true;motor.BindAnimationPostureOwner(owner);actor.SetActive(true);Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(obstacle);Object.DestroyImmediate(actor);Physics.SyncTransforms();}
        [Test] public void ControlledMotionAdvancesInsideTheExistingConstraintAndCannotBeStolen()
        {
            Assert.That(motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Slide),Is.True);
            Assert.That(motor.TryBeginAnimationMotion(new object(),AnimationPlaygroundMotionMode.Ladder,true),Is.False);
            Assert.That(motor.SetAnimationMotorIntent(new object(),new AnimationPlaygroundMotorIntent(Vector3.right,Vector3.forward)),Is.False);
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.right*3,Vector3.forward,phase:.5f));Tick(motor);
            Assert.That(body.linearVelocity.x,Is.EqualTo(3).Within(.001));
            Assert.That(motor.LocomotionSnapshot.CurrentPlanarVelocity.x,Is.EqualTo(3).Within(.001));
            Assert.That(motor.AnimationSnapshot.Mode,Is.EqualTo(AnimationPlaygroundMotionMode.Slide));
            Assert.That(motor.AnimationSnapshot.Phase,Is.EqualTo(.5f));
            motor.EndActionConstraint(owner);Assert.That(motor.IsActionConstrained,Is.False);Assert.That(motor.AnimationMotionMode,Is.EqualTo(AnimationPlaygroundMotionMode.None));
        }
        [Test] public void SweepAndPauseConstrainMotionAndReleaseRestoresGravity()
        {
            Assert.That(motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Ladder,true),Is.True);Assert.That(body.useGravity,Is.False);
            obstacle=new GameObject("Close obstruction");obstacle.transform.position=actor.transform.position+Vector3.right*.72f;
            obstacle.AddComponent<BoxCollider>().size=new Vector3(.1f,4,2);Physics.SyncTransforms();
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.right*10,Vector3.right,true));Tick(motor);
            Assert.That(motor.AnimationSnapshot.Blocked,Is.True);Assert.That(body.linearVelocity.x,Is.LessThan(10));
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.up*2,Vector3.forward,true,paused:true));Tick(motor);
            Assert.That(body.linearVelocity,Is.EqualTo(Vector3.zero));Assert.That(body.useGravity,Is.False);
            Assert.That(motor.ReleaseAnimationMotion(owner),Is.True);Assert.That(body.useGravity,Is.True);Assert.That(motor.IsActionConstrained,Is.False);
        }
        [Test] public void PostureAnchorsFeetAndRefusesStandingThroughACeiling()
        {
            var bottom=shape.center.y-shape.height*.5f;
            Assert.That(motor.TrySetAnimationPosture(null,AnimationPlaygroundPosture.Crouched,1.1f),Is.False);
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Crouched,1.1f),Is.True);
            Assert.That(shape.center.y-shape.height*.5f,Is.EqualTo(bottom).Within(.0001));
            obstacle=new GameObject("Ceiling");obstacle.transform.position=actor.transform.position+Vector3.up*.8f;
            obstacle.AddComponent<BoxCollider>().size=new Vector3(3,.2f,3);Physics.SyncTransforms();
            Assert.That(motor.TryRestoreAnimationPosture(owner),Is.False);Assert.That(shape.height,Is.EqualTo(1.1f));
            Object.DestroyImmediate(obstacle);Physics.SyncTransforms();Assert.That(motor.TryRestoreAnimationPosture(owner),Is.True);
            Assert.That(shape.height,Is.EqualTo(2));Assert.That(shape.center,Is.EqualTo(Vector3.zero));
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Prone,.6f),Is.True);
            Assert.That(shape.radius,Is.EqualTo(.3f).Within(.001));Assert.That(motor.TryRestoreAnimationPosture(owner),Is.True);Assert.That(shape.radius,Is.EqualTo(.5f));
        }
        [Test] public void DisableReleasesOnlyOwnedStateAndPreservesTheOriginalGravitySetting()
        {
            body.useGravity=false;motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.AlignedAction);
            Assert.That(body.useGravity,Is.True);
            typeof(TopDown3DPlayerMotor).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motor,null);
            Assert.That(body.useGravity,Is.False);Assert.That(motor.IsActionConstrained,Is.False);
        }
        [Test] public void ResetRefusesOccupiedReturnAndNeverTakesAForeignLease()
        {
            var destination=actor.transform.position+Vector3.right*4;
            obstacle=new GameObject("Occupied return");obstacle.transform.position=destination;obstacle.AddComponent<BoxCollider>();Physics.SyncTransforms();
            motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Ladder,true);
            var original=body.position;
            Assert.That(motor.TryResetAnimationActor(owner,destination,Vector3.forward),Is.False);
            Assert.That(body.position,Is.EqualTo(original));Assert.That(motor.IsActionConstrained,Is.True);
            Object.DestroyImmediate(obstacle);Physics.SyncTransforms();
            Assert.That(motor.TryResetAnimationActor(owner,destination,Vector3.forward),Is.True);
            Assert.That(body.position,Is.EqualTo(destination));Assert.That(body.useGravity,Is.True);Assert.That(motor.IsActionConstrained,Is.False);
            var foreign=new object();motor.TryBeginActionConstraint(foreign);
            Assert.That(motor.TryResetAnimationActor(owner,original,Vector3.forward),Is.False);Assert.That(motor.IsActionConstrained,Is.True);
            Assert.That(motor.ReleaseAnimationMotion(null),Is.False);
        }
        [Test] public void FloorSupportDoesNotBlockPlanarMotionAndTakeoffReleasesSupport()
        {
            obstacle=new GameObject("Ground support");obstacle.transform.position=actor.transform.position+Vector3.down*1.05f;
            obstacle.AddComponent<BoxCollider>().size=new Vector3(10,.1f,10);obstacle.AddComponent<TopDown3DGroundSurface>();Physics.SyncTransforms();
            motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Jump);
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.right*2,Vector3.right));Tick(motor);
            Assert.That(motor.AnimationSnapshot.Supported,Is.True);Assert.That(body.linearVelocity.x,Is.EqualTo(2).Within(.001));
            Assert.That(motor.AnimationSnapshot.Blocked,Is.False);
            Assert.That(motor.TryAnimationTakeoff(owner,4),Is.True);Tick(motor);
            Assert.That(motor.AnimationSnapshot.Supported,Is.False);Assert.That(body.linearVelocity.y,Is.EqualTo(4));
        }
        [Test] public void AirbornePauseRestoresVerticalVelocityOnResumeOrCancellation()
        {
            motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Fall);body.linearVelocity=Vector3.down*4;
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward,paused:true));Tick(motor);
            Assert.That(body.linearVelocity,Is.EqualTo(Vector3.zero));
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward));Tick(motor);
            Assert.That(body.linearVelocity.y,Is.EqualTo(-4));Assert.That(body.useGravity,Is.True);
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward,paused:true));Tick(motor);
            motor.ReleaseAnimationMotion(owner);Assert.That(body.linearVelocity.y,Is.EqualTo(-4));Assert.That(body.useGravity,Is.True);
        }
        [Test] public void CompactShapeCannotBeLabelledStandingOrAnUnknownMode()
        {
            Assert.That(motor.TryBeginAnimationMotion(owner,(AnimationPlaygroundMotionMode)999),Is.False);
            Assert.That(motor.TrySetAnimationPosture(owner,(AnimationPlaygroundPosture)999,1.1f),Is.False);
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Crouched,1.1f),Is.True);
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Standing,1.1f),Is.False);
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Crouched));
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Standing,2),Is.True);
            Assert.That(shape.height,Is.EqualTo(2));
        }
    }
}
