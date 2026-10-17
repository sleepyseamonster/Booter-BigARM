using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundSurfaceRecoveryTests
    {
        private Scene scene;
        private TopDown3DAnimationPlaygroundSession session;
        private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp] public void OpenHeldCampLoop()
        {
            scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
            session = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TopDown3DAnimationPlaygroundSession>(true)).Single();
            Assert.That(session.CheckDependencies(), Is.True, session.UnavailableReason);
            typeof(TopDown3DAnimationPlaygroundSession).GetMethod("PreparePlayer", Hidden).Invoke(session, null);
            Assert.That(session.Readiness, Is.Not.EqualTo(AnimationPlaygroundReadiness.Failed), session.UnavailableReason);
        }
        [TearDown] public void CloseHeldCampLoop() { if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }

        [Test] public void PreparationRegistersEveryStaticStationSolid()
        {
            var solids = session.Stations.SelectMany(s => s.GetComponentsInChildren<Collider>(true))
                .Where(c => c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger && c.attachedRigidbody == null).ToArray();
            Assert.That(solids.Length, Is.GreaterThan(200));
            Assert.That(solids.Where(c => c.GetComponentInParent<TopDown3DGroundSurface>() == null).Select(c => c.name), Is.Empty,
                "A solid station surface must support the same ground/landing contract as AP02.");
        }

        [Test] public void RegistrationAlsoCoversCampRocksAndWorkshopSolidsIdempotently()
        {
            var solids = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true))
                .Where(c => c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger && c.attachedRigidbody == null
                    && (session.Player.GroundMask.value & (1 << c.gameObject.layer)) != 0).ToArray();
            Assert.That(solids.Length, Is.GreaterThan(280));
            Assert.That(solids.Where(c => c.GetComponentInParent<TopDown3DGroundSurface>() == null).Select(c => c.name), Is.Empty);
            var before = scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<TopDown3DGroundSurface>(true).Length);
            typeof(TopDown3DAnimationPlaygroundSession).GetMethod("RegisterStaticCampGround", Hidden).Invoke(session, null);
            var after = scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<TopDown3DGroundSurface>(true).Length);
            Assert.That(after, Is.EqualTo(before));
        }

        [Test] public void RegistrationExcludesTriggersMovingBodiesInactiveAndMaskedGeometry()
        {
            foreach (var kind in new[] { "trigger", "moving", "disabled", "inactive", "masked" })
            {
                var value = new GameObject("Excluded " + kind); SceneManager.MoveGameObjectToScene(value, scene);
                var solid = value.AddComponent<BoxCollider>();
                if (kind == "trigger") solid.isTrigger = true;
                if (kind == "moving") value.AddComponent<Rigidbody>().isKinematic = true;
                if (kind == "disabled") solid.enabled = false;
                if (kind == "inactive") value.SetActive(false);
                if (kind == "masked")
                {
                    value.layer = 31;
                    typeof(TopDown3DPlayerMotor).GetField("groundMask", Hidden).SetValue(session.Player, (LayerMask)~(1 << 31));
                }
                typeof(TopDown3DAnimationPlaygroundSession).GetMethod("RegisterStaticCampGround", Hidden).Invoke(session, null);
                Assert.That(value.GetComponent<TopDown3DGroundSurface>(), Is.Null, kind);
                Object.DestroyImmediate(value);
            }
        }

        [Test] public void MovableCampPropSupportsPhysicalLandingAndRejumpWithoutStaticPoseQualification()
        {
            var prop = new GameObject("Movable landing support"); SceneManager.MoveGameObjectToScene(prop, scene);
            prop.transform.position = new Vector3(23000, -.1f, 23000);
            var support = prop.AddComponent<BoxCollider>(); support.size = new Vector3(3, .2f, 3);
            prop.AddComponent<Rigidbody>().isKinematic = true;
            var motor = session.Player; var body = motor.GetComponent<Rigidbody>(); var owner = new object();
            motor.BindAnimationPostureOwner(owner);
            body.position = new Vector3(23000, .92f, 23000); motor.transform.position = body.position; Physics.SyncTransforms();
            Assert.That(support.GetComponentInParent<TopDown3DGroundSurface>(), Is.Null);
            Assert.That(session.TryGetStaticStationSupportIdentity(support, out _), Is.False);
            var args = new object[] { Vector3.zero };
            Assert.That((bool)typeof(TopDown3DPlayerMotor).GetMethod("TryGetGroundNormal", Hidden).Invoke(motor, args), Is.True);
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor, true);
            typeof(TopDown3DPlayerMotor).GetMethod("ObserveCompletedLaboratoryMotionAt", Hidden).Invoke(motor, new object[] { 1d });
            Assert.That(motor.LaboratoryGroundedMotion.Available, Is.False, "Moving support must not enter the static gait/pose clock.");
            Assert.That(motor.CanBeginJumpMotion(owner), Is.True);
            Assert.That(motor.TryBeginAnimationMotion(owner, AnimationPlaygroundMotionMode.Jump,
                policy: AnimationPlaygroundMotionPolicy.JumpLocomotion), Is.True);
            Assert.That(motor.TryPredictJumpSupport(owner, .65f, out var forecast), Is.True);
            Assert.That(forecast.collider, Is.SameAs(support));
            Assert.That(motor.JumpObservation.Supported, Is.False, "Prediction still cannot manufacture touchdown.");
            motor.ReleaseAnimationMotion(owner);
        }

        [Test] public void UnregisteredStationAndMovingSupportCannotShareAStaticObservationIdentity()
        {
            var value = new GameObject("Unregistered station"); value.transform.SetParent(session.transform);
            value.AddComponent<TopDown3DAnimationPlaygroundStation>(); var solid = value.AddComponent<BoxCollider>();
            value.AddComponent<TopDown3DGroundSurface>();
            Assert.That(session.TryGetStaticStationSupportIdentity(solid, out _), Is.False);
            var registered = session.Stations[0].GetComponentsInChildren<Collider>().First(c => !c.isTrigger && c.attachedRigidbody == null);
            Assert.That(session.TryGetStaticStationSupportIdentity(registered, out _), Is.True);
            registered.gameObject.AddComponent<Rigidbody>().isKinematic = true;
            Assert.That(session.TryGetStaticStationSupportIdentity(registered, out _), Is.False);
        }

        [TestCase("AP01")] [TestCase("AP02")] [TestCase("AP03")] [TestCase("AP04")]
        [TestCase("AP05")] [TestCase("AP06")] [TestCase("AP07")] [TestCase("AP08")]
        [TestCase("AP09")] [TestCase("AP10")] [TestCase("AP11")] [TestCase("AP12")]
        public void EveryStationDeckAndApproachHasWalkableSupport(string id)
        {
            var station = session.Stations.Single(s => s.StationId == id);
            foreach (var surface in station.GetComponentsInChildren<Collider>(true).Where(c => c.name == "Flat deck" || c.name == "Ramp"))
            {
                Assert.That(surface.GetComponentInParent<TopDown3DGroundSurface>(), Is.Not.Null, id + " " + surface.name);
                Assert.That(surface.attachedRigidbody, Is.Null);
            }
        }

        [Test] public void LandingForecastRecognizesAllAuthoredDecksWithoutDeclaringContact()
        {
            var motor = session.Player; var body = motor.GetComponent<Rigidbody>(); var owner = new object();
            motor.BindAnimationPostureOwner(owner); Assert.That(motor.IsAnimationPostureOwner(owner), Is.True);
            Assert.That(motor.TryBeginAnimationMotion(owner, AnimationPlaygroundMotionMode.Fall,
                policy: AnimationPlaygroundMotionPolicy.JumpLocomotion), Is.True);
            foreach (var station in session.Stations)
            {
                var position = station.SafeReturn.position + Vector3.up * .08f;
                body.position = position; motor.transform.position = position; Physics.SyncTransforms();
                Assert.That(motor.TryPredictJumpSupport(owner, .65f, out var hit), Is.True, station.StationId);
                Assert.That(hit.collider.GetComponentInParent<TopDown3DGroundSurface>(), Is.Not.Null);
                Assert.That(motor.JumpObservation.Supported, Is.False, "A forecast must not commit a touchdown.");
            }
            motor.ReleaseAnimationMotion(owner);
        }

        [TestCase("continuous")] [TestCase("moved")] [TestCase("disabled")] [TestCase("destroyed")]
        public void StairContinuityRetainsPhaseOnlyWhilePreviousStaticSupportRemainsValid(string mutation)
        {
            var station = session.Stations.Single(s => s.StationId == "AP04");
            var treads = station.GetComponentsInChildren<BoxCollider>().Where(c => c.name.StartsWith("Platform return stairs "))
                .OrderBy(c => int.Parse(c.name.Substring("Platform return stairs ".Length))).Take(2).ToArray();
            Assert.That(treads.Length, Is.EqualTo(2));
            var motor = session.Player; var body = motor.GetComponent<Rigidbody>(); var owner = new object();
            motor.BindAnimationPostureOwner(owner); Assert.That(motor.IsAnimationPostureOwner(owner), Is.True);
            var positions = treads.Select(tread => tread.transform.TransformPoint(new Vector3(0, .5f, 0)) + Vector3.up * .915f).ToArray();
            AnimationPlaygroundGroundedMotion Observe(Vector3 position, double time)
            {
                body.position = position; motor.transform.position = position; Physics.SyncTransforms();
                var args = new object[] { Vector3.zero };
                Assert.That((bool)typeof(TopDown3DPlayerMotor).GetMethod("TryGetGroundNormal", Hidden).Invoke(motor, args), Is.True);
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor, true);
                typeof(TopDown3DPlayerMotor).GetMethod("ObserveCompletedLaboratoryMotionAt", Hidden).Invoke(motor, new object[] { time });
                return motor.LaboratoryGroundedMotion;
            }
            var first = Observe(positions[0], 1);
            var previous = motor.LaboratorySupportCollider;
            Assert.That(previous, Is.Not.Null);
            if (mutation == "moved") previous.transform.position += Vector3.right * .02f;
            if (mutation == "disabled") previous.enabled = false;
            if (mutation == "destroyed") Object.DestroyImmediate(previous);
            Physics.SyncTransforms();
            var second = Observe(positions[1], 1.2);
            if (mutation != "continuous")
            {
                Assert.That(second.Available, Is.False); Assert.That(second.Rebased, Is.True);
                Assert.That(second.Epoch, Is.GreaterThan(first.Epoch)); return;
            }
            Assert.That(second.Available, Is.True); Assert.That(second.Epoch, Is.EqualTo(first.Epoch));
            Assert.That(second.Distance, Is.GreaterThan(.4));
            var clock = new AnimationPlaygroundGaitClock(); clock.Advance(first, 3.2217062); clock.Advance(second, 3.2217062);
            Assert.That(clock.Cycles, Is.GreaterThan(.1), "The accepted distance clock must advance across treads.");
        }
    }
}
