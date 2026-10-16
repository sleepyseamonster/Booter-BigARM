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
        [Test] public void JumpPolicyPreservesMomentumWhileLegacyRecoveryStillStops()
        {
            var velocity=new Vector3(4,-2,3);body.linearVelocity=velocity;
            Assert.That(motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Jump,
                policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.True);
            Assert.That(body.linearVelocity,Is.EqualTo(velocity));
            Assert.That(motor.ReleaseAnimationMotion(owner),Is.True);
            Assert.That(body.linearVelocity,Is.EqualTo(velocity));Assert.That(motor.IsActionConstrained,Is.False);
            Assert.That(motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Recovery),Is.True);
            Assert.That(body.linearVelocity,Is.EqualTo(Vector3.down*2));
        }
        [Test] public void JumpSnapshotSeparatesCompletedVelocityFromTheUpcomingCommand()
        {
            body.linearVelocity=new Vector3(3,-2,1);
            motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Fall,policy:AnimationPlaygroundMotionPolicy.JumpLocomotion);
            motor.SetJumpGroundedLocomotion(owner,false);
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.right*4,Vector3.forward));Tick(motor);
            Assert.That(motor.JumpObservation.Velocity,Is.EqualTo(new Vector3(3,-2,1)));
            Assert.That(motor.AnimationSnapshot.Velocity,Is.EqualTo(motor.JumpObservation.Velocity));
            Assert.That(body.linearVelocity,Is.EqualTo(new Vector3(4,-2,0)));
        }
        [Test] public void JumpPauseAndReleaseRestoreTheEntireRetainedVelocity()
        {
            var velocity=new Vector3(4,-3,2);body.linearVelocity=velocity;
            motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Fall,policy:AnimationPlaygroundMotionPolicy.JumpLocomotion);
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward,paused:true));Tick(motor);
            Assert.That(body.linearVelocity,Is.EqualTo(Vector3.zero));Assert.That(body.useGravity,Is.False);
            motor.ReleaseAnimationMotion(owner);
            Assert.That(body.linearVelocity,Is.EqualTo(velocity));Assert.That(body.useGravity,Is.True);
        }
        [TestCase(1f)] [TestCase(7f)]
        public void LaunchedJumpAddsMassIndependentGravityOnceAndPauseAddsNone(float mass)
        {
            motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Jump,policy:AnimationPlaygroundMotionPolicy.JumpLocomotion);
            Assert.That(motor.SetJumpGravityScale(owner,1.6f),Is.True);body.mass=mass;
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,true);
            body.linearVelocity=Vector3.forward*3;
            Assert.That(motor.TryAnimationTakeoff(owner,4.6f),Is.True);
            var force=Physics.gravity*.6f*mass;
            Assert.That(Vector3.Distance(body.GetAccumulatedForce(),force),Is.LessThan(.00001f));
            Assert.That(body.linearVelocity,Is.EqualTo(new Vector3(0,4.6f,3)));
            Assert.That(motor.JumpGravityMagnitude,Is.EqualTo(-Physics.gravity.y*1.6f).Within(.00001f));
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.forward*3,Vector3.forward));Tick(motor);
            Assert.That(Vector3.Distance(body.GetAccumulatedForce(),force),Is.LessThan(.00001f),"A duplicate command in the same physics timestamp must not double the acceleration.");
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward,paused:true));Tick(motor);
            Assert.That(Vector3.Distance(body.GetAccumulatedForce(),force),Is.LessThan(.00001f));Assert.That(body.useGravity,Is.False);
            motor.ReleaseAnimationMotion(owner);Assert.That(motor.JumpGravityMagnitude,Is.EqualTo(-Physics.gravity.y).Within(.00001f));
        }
        [Test] public void PassiveFallAndForeignOrInvalidCalibrationCannotEnableJumpGravity()
        {
            Assert.That(motor.SetJumpGravityScale(owner,1.6f),Is.False);
            motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Fall,policy:AnimationPlaygroundMotionPolicy.JumpLocomotion);
            Assert.That(motor.SetJumpGravityScale(new object(),1.6f),Is.False);
            foreach(var scale in new[]{float.NaN,.9f,3.1f})Assert.That(motor.SetJumpGravityScale(owner,scale),Is.False);
            Assert.That(motor.SetJumpGravityScale(owner,1.6f),Is.True);body.linearVelocity=Vector3.down*2;
            motor.SetAnimationMotorIntent(owner,new AnimationPlaygroundMotorIntent(Vector3.forward*3,Vector3.forward));Tick(motor);
            Assert.That(body.GetAccumulatedForce(),Is.EqualTo(Vector3.zero));
            Assert.That(motor.JumpGravityMagnitude,Is.EqualTo(-Physics.gravity.y).Within(.00001f));
            motor.ReleaseAnimationMotion(owner);motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Jump,policy:AnimationPlaygroundMotionPolicy.JumpLocomotion);
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,true);motor.TryAnimationTakeoff(owner,4.6f);
            Assert.That(body.GetAccumulatedForce(),Is.EqualTo(Vector3.zero),"A new lease starts at world gravity until explicitly calibrated.");
        }
        [Test] public void LandingForecastDoesNotDeclareContactOrChangePhysicalVelocity()
        {
            obstacle=new GameObject("Forecast floor");obstacle.transform.position=actor.transform.position+Vector3.down*1.05f;
            obstacle.AddComponent<BoxCollider>().size=new Vector3(10,.1f,10);obstacle.AddComponent<TopDown3DGroundSurface>();Physics.SyncTransforms();
            var velocity=new Vector3(3,-2,0);body.linearVelocity=velocity;
            motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Fall,policy:AnimationPlaygroundMotionPolicy.JumpLocomotion);
            Assert.That(motor.TryPredictJumpSupport(owner,.65f,out var predicted),Is.True);
            Assert.That(predicted.distance,Is.LessThan(.01f));Assert.That(body.linearVelocity,Is.EqualTo(velocity));
            Assert.That(motor.JumpObservation.Supported,Is.False);Assert.That(motor.JumpObservation.TouchdownId,Is.Zero);
        }
        [Test] public void JumpRejectionCannotStealAnotherActionsLeaseOrVelocity()
        {
            var foreign=new object();motor.TryBeginActionConstraint(foreign);body.linearVelocity=Vector3.right*5;
            Assert.That(motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Jump,
                policy:AnimationPlaygroundMotionPolicy.JumpLocomotion),Is.False);
            Assert.That(body.linearVelocity,Is.EqualTo(Vector3.right*5));Assert.That(motor.IsActionConstrained,Is.True);
            motor.EndActionConstraint(foreign);
        }
        [Test] public void DistinctCompletedSupportEpisodesGetDistinctTouchdownIds()
        {
            // Inject completed contact observations, not a simulated physics trajectory.
            obstacle=new GameObject("Recorded walkable contact");var contact=obstacle.AddComponent<BoxCollider>();
            motor.TryBeginAnimationMotion(owner,AnimationPlaygroundMotionMode.Fall,policy:AnimationPlaygroundMotionPolicy.JumpLocomotion);
            void Set(string name,object value)=>typeof(TopDown3DPlayerMotor).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(motor,value);
            Set("jumpContactTime",Time.fixedTimeAsDouble);Set("jumpSupportCollider",contact);Set("jumpSupportNormal",Vector3.up);
            Set("jumpContactDownwardSpeed",4f);Tick(motor);
            var first=motor.JumpObservation.TouchdownId;
            Assert.That(motor.JumpObservation.Touchdown,Is.True);Assert.That(motor.JumpObservation.DownwardImpactSpeed,Is.EqualTo(4));
            Tick(motor);Assert.That(motor.JumpObservation.Touchdown,Is.False);
            Set("jumpSupportCollider",null);Tick(motor);
            Set("jumpSupportCollider",contact);Tick(motor);
            Assert.That(motor.JumpObservation.Touchdown,Is.True);Assert.That(motor.JumpObservation.TouchdownId,Is.EqualTo(first+1));
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
        [Test] public void GroundDistanceRequiresTheLeaseAndMeasuresTheSurfaceBelowTheFeet()
        {
            obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=actor.transform.position+Vector3.down*1.4f;
            obstacle.transform.localScale=new Vector3(4,.2f,4);Physics.SyncTransforms();
            Assert.That(motor.TryAnimationGroundDistance(new object(),1,out _),Is.False);
            Assert.That(motor.TryAnimationGroundDistance(owner,1,out var distance),Is.True);
            Assert.That(distance,Is.EqualTo(.3f).Within(.002f));
            Assert.That(motor.TryAnimationGroundDistance(owner,.1f,out _),Is.False);
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
        [Test] public void ReviewedRadiusExpansionRequiresClearanceAndPreservesTheFootPlane()
        {
            obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=actor.transform.position+Vector3.right*.65f;
            obstacle.transform.localScale=new Vector3(.1f,3,3);Physics.SyncTransforms();
            Assert.That(motor.IsAnimationPostureShapeClear(1.6f,.7f),Is.False);
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Crouched,1.6f,.7f),Is.False);
            Assert.That(shape.radius,Is.EqualTo(.5f));Assert.That(shape.height,Is.EqualTo(2));
            Object.DestroyImmediate(obstacle);Physics.SyncTransforms();
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Crouched,1.6f,.7f),Is.True);
            Assert.That(shape.center.y-shape.height*.5f,Is.EqualTo(-1).Within(.00001f));
            Assert.That(motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Standing,2,float.NaN),Is.False);
            Assert.That(motor.TryRestoreAnimationPosture(owner),Is.True);Assert.That(shape.radius,Is.EqualTo(.5f));
        }
    }
}
