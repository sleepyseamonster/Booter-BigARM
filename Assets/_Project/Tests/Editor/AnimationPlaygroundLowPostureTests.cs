using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundLowPostureTests
    {
        private GameObject actor,floor,obstacle;
        private TopDown3DPlayerMotor motor;
        private Rigidbody body;
        private CapsuleCollider shape;
        private readonly object owner=new();
        private void Tick()=>typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motor,null);
        [SetUp] public void Setup()
        {
            actor=new GameObject("Longitudinal posture fixture");actor.SetActive(false);actor.transform.position=new Vector3(74000,.91f,74000);
            motor=actor.AddComponent<TopDown3DPlayerMotor>();body=actor.GetComponent<Rigidbody>();shape=actor.GetComponent<CapsuleCollider>();
            shape.height=1.82f;shape.radius=.2f;shape.center=Vector3.zero;motor.BindAnimationPostureOwner(owner);actor.SetActive(true);
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(74000,-.1f,74000);floor.transform.localScale=new Vector3(10,.2f,10);
            floor.AddComponent<TopDown3DGroundSurface>();Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(obstacle);Object.DestroyImmediate(floor);Object.DestroyImmediate(actor);Physics.SyncTransforms();}
        private void Low()
        {
            Assert.That(motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Slide),Is.True);
            Assert.That(motor.TrySetAnimationLowShape(owner,1.9f,.4f,.1f),Is.True);
        }
        [Test] public void ShapeRetainsFootPlaneLongitudinalExtentAndOwnedSupport()
        {
            Low();Physics.SyncTransforms();Tick();
            Assert.That(shape.direction,Is.EqualTo(2));Assert.That(shape.height,Is.EqualTo(1.9f));
            Assert.That(shape.center.y-shape.radius,Is.EqualTo(-.91f).Within(.00001f));
            Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Prone));
            Assert.That(motor.AnimationSnapshot.Supported,Is.True);
            Assert.That(motor.TryAnimationGroundDistance(owner,.1f,out var gap),Is.True);Assert.That(gap,Is.Zero.Within(.003f));
            Assert.That(motor.TrySetAnimationLowShape(new object(),1.9f,.4f,.1f),Is.False);
        }
        [Test] public void FootProbeDoesNotUseTheLongitudinalLengthAsVerticalHeight()
        {
            Low();body.position+=Vector3.up*.3f;Physics.SyncTransforms();Tick();
            Assert.That(motor.TryAnimationGroundDistance(owner,.1f,out _),Is.False);
            Assert.That(motor.TryAnimationGroundDistance(owner,.5f,out var gap),Is.True);Assert.That(gap,Is.EqualTo(.3f).Within(.01f));
            Assert.That(motor.AnimationSnapshot.Supported,Is.False);
        }
        [Test] public void LowBodySweepsBlockItsFrontBeyondTheFormerStandingRadius()
        {
            Low();obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position=actor.transform.position+new Vector3(0,-.5f,1.1f);obstacle.transform.localScale=new Vector3(2,.7f,.1f);
            Physics.SyncTransforms();motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.forward*12,Vector3.forward));Tick();
            Assert.That(motor.AnimationSnapshot.Blocked,Is.True);Assert.That(body.linearVelocity.z,Is.LessThan(12));
        }
        [Test] public void LongShapeMustFitBeforeItCanReplaceTheUprightCapsule()
        {
            obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=actor.transform.position+new Vector3(0,-.5f,.85f);
            obstacle.transform.localScale=new Vector3(2,.7f,.1f);Physics.SyncTransforms();
            Assert.That(motor.TrySetAnimationLowShape(owner,1.9f,.4f,.1f),Is.False);
            Assert.That(shape.direction,Is.EqualTo(1));Assert.That(shape.height,Is.EqualTo(1.82f));
        }
        [Test] public void BlockedStandRetainsLowShapeUntilClearThenRestoresOriginalGeometry()
        {
            Low();obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=actor.transform.position+Vector3.up*.3f;
            obstacle.transform.localScale=new Vector3(3,.1f,3);Physics.SyncTransforms();
            Assert.That(motor.TryRestoreAnimationPosture(owner),Is.False);Assert.That(motor.AnimationLowShapeActive,Is.True);
            Object.DestroyImmediate(obstacle);Physics.SyncTransforms();
            Assert.That(motor.TryRestoreAnimationPosture(owner),Is.True);Assert.That(shape.direction,Is.EqualTo(1));
            Assert.That(shape.center,Is.EqualTo(Vector3.zero));Assert.That(shape.radius,Is.EqualTo(.2f));
        }
        [Test] public void ConversionToCrouchChecksTheNewUprightVolumeAndRestoresTheFootPlane()
        {
            Low();obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=actor.transform.position+Vector3.up*.3f;
            obstacle.transform.localScale=new Vector3(3,.1f,3);Physics.SyncTransforms();
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Crouched,1.57f,.4f),Is.False);
            Object.DestroyImmediate(obstacle);Physics.SyncTransforms();
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Crouched,1.57f,.4f),Is.True);
            Assert.That(shape.direction,Is.EqualTo(1));Assert.That(shape.center.y-shape.height*.5f,Is.EqualTo(-.91f).Within(.0001f));
        }
        [Test] public void PauseReleaseAndResetKeepGravityAndClearOnlyOwnedGeometry()
        {
            Low();motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.forward*5,Vector3.forward,paused:true));Tick();
            Assert.That(body.useGravity,Is.False);Assert.That(body.linearVelocity,Is.EqualTo(Vector3.zero));
            Assert.That(motor.ReleaseAnimationMotion(owner),Is.True);Assert.That(body.useGravity,Is.True);
            Assert.That(motor.AnimationLowShapeActive,Is.True);
            Assert.That(motor.TryResetAnimationActor(owner,new Vector3(74002,.91f,74000),Vector3.forward),Is.True);
            Assert.That(motor.AnimationLowShapeActive,Is.False);Assert.That(motor.AnimationPosture,Is.EqualTo(AnimationPlaygroundPosture.Standing));
        }
        [TestCase(float.NaN,.4f,0)] [TestCase(1.9f,float.NaN,0)] [TestCase(1.9f,.4f,float.PositiveInfinity)]
        [TestCase(.5f,.4f,0)] [TestCase(4f,.4f,0)] [TestCase(1.9f,.4f,2)]
        public void InvalidGeometryCannotChangeTheActor(float length,float radius,float offset)
        {
            Assert.That(motor.TrySetAnimationLowShape(owner,length,radius,offset),Is.False);Assert.That(shape.direction,Is.EqualTo(1));
        }
    }
}
