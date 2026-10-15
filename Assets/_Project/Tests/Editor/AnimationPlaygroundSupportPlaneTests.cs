using System;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundSupportPlaneTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(0f)] [TestCase(20f)] [TestCase(40f)]
        public void ExistingGroundProbePublishesExactCompletedPlaneAndVetoesStaleSupport(float slope)
        {
            var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
            var actor = new GameObject("Static support observation"); actor.SetActive(false);
            var ground = new GameObject("Marked tilted support");
            SceneManager.MoveGameObjectToScene(actor, scene); SceneManager.MoveGameObjectToScene(ground, scene);
            try
            {
                var point = new Vector3(250, 100, 250);
                var normal = Quaternion.AngleAxis(slope, Vector3.right) * Vector3.up;
                ground.transform.SetPositionAndRotation(point - normal * .05f, Quaternion.AngleAxis(slope, Vector3.right));
                var collider = ground.AddComponent<BoxCollider>(); collider.size = new Vector3(20, .1f, 20);
                ground.AddComponent<TopDown3DGroundSurface>();
                var motor = actor.AddComponent<TopDown3DPlayerMotor>();
                var capsule = actor.GetComponent<CapsuleCollider>(); capsule.height = 1.82f; capsule.radius = .2f;
                actor.transform.position = point + Vector3.up * (.91f + .2f * (1 / normal.y - 1));
                typeof(TopDown3DPlayerMotor).GetMethod("Awake", Private).Invoke(motor, null);
                var body = actor.GetComponent<Rigidbody>(); body.position = actor.transform.position;
                Physics.SyncTransforms();
                var query = typeof(TopDown3DPlayerMotor).GetMethod("TryGetGroundNormal", Private);
                var arguments = new object[] { Vector3.zero };
                Assert.That(query.Invoke(motor, arguments), Is.True);
                Assert.That(Vector3.Angle((Vector3)arguments[0], normal), Is.LessThan(.01f));
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor, true);
                motor.BindAnimationPostureOwner(new object());
                // Binding clears prior support; query the completed position again.
                Assert.That(query.Invoke(motor, arguments), Is.True);
                var observe = (Action<double>)Delegate.CreateDelegate(typeof(Action<double>), motor,
                    typeof(TopDown3DPlayerMotor).GetMethod("ObserveCompletedLaboratoryMotionAt", Private));
                Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.False);
                observe(1); Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.False);
                observe(1.02);
                Assert.That(motor.TryGetLaboratorySupportPlane(out var plane), Is.True);
                Assert.That(plane.GetDistanceToPoint(point), Is.EqualTo(0).Within(.0001));
                Assert.That(Vector3.Angle(plane.normal, normal), Is.LessThan(.01f));
                // Readout uses the recorded plane, with no new ray. Disabled,
                // moved or externally owned supports cannot leak that snapshot.
                collider.enabled = false;
                Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.False);
                collider.enabled = true; Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.True);
                ground.transform.position += Vector3.up * .01f;
                Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.False);
                observe(1.04); Assert.That(motor.LaboratoryGroundedMotion.Available, Is.False);
                ground.transform.position -= Vector3.up * .01f; Physics.SyncTransforms();
                Assert.That(query.Invoke(motor, arguments), Is.True);
                observe(1.06); observe(1.08); observe(1.10);
                Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.True);
                var movingBody = ground.AddComponent<Rigidbody>(); movingBody.isKinematic = true;
                Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.False);
                Object.DestroyImmediate(movingBody);
                Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.True);
                var owner = new object(); Assert.That(motor.TryBeginActionConstraint(owner), Is.True);
                Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.False);
                motor.EndActionConstraint(owner);
                Assert.That(motor.TryGetLaboratorySupportPlane(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(actor); Object.DestroyImmediate(ground);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
