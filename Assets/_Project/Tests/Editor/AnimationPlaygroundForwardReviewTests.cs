using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundForwardReviewTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string PolicyPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset";

        [Test, Category("PrivateAnimationQualification")]
        public void ReviewIsAnExplicitRouteAndDoesNotRelaxTheNormalBindingGate()
        {
            using var f = new Fixture(false);
            Assert.That(f.Policy.Validate(f.Session.Bindings.targetModel, f.Policy.locomotion, out _), Is.False);
            f.Policy.qualified = true;
            Assert.Throws<ArgumentException>(() => f.Driver.ConfigureLaboratoryForwardReview(f.Policy, f.Station));
            f.Policy.qualified = false; f.Policy.terrainQualified = true;
            Assert.Throws<ArgumentException>(() => f.Driver.ConfigureLaboratoryForwardReview(f.Policy, f.Station));
            f.Policy.terrainQualified = false;
            Assert.DoesNotThrow(() => f.Driver.ConfigureLaboratoryForwardReview(f.Policy, f.Station));
            Assert.That(AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(PolicyPath).qualified, Is.False);
            f.Driver.ConfigureLaboratory(f.Session.Bindings);
            Assert.That(f.Driver.LaboratoryForwardReviewConfigured, Is.False, "Reconfiguration must not retain a previous review route.");
        }

        [Test, Category("PrivateAnimationQualification")]
        public void SessionPreparesTheExplicitReviewRouteWithoutReplacingItsPrivateBinding()
        {
            using var f = new Fixture(false); var binding = f.Session.Bindings;
            f.Session.ConfigureForwardReview(f.Policy); Assert.That(f.Session.CheckDependencies(), Is.True);
            typeof(TopDown3DAnimationPlaygroundSession).GetMethod("PreparePlayer", Private).Invoke(f.Session, null);
            Assert.That(f.Session.Readiness, Is.EqualTo(AnimationPlaygroundReadiness.DependenciesChecked), f.Session.UnavailableReason);
            Assert.That(f.Driver.LaboratoryForwardReviewConfigured, Is.True);
            Assert.That(f.Session.Bindings, Is.SameAs(binding));
            Assert.That(f.Driver.LocomotionProfile, Is.SameAs(binding.locomotion));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void MissingRequiredPrivateReviewProfileIsVisibleInsteadOfSilentlyFallingBack()
        {
            using var f = new Fixture(false); f.Session.ConfigureForwardReview(f.Policy);
            typeof(TopDown3DAnimationPlaygroundSession).GetField("experimentalForwardReview", Private).SetValue(f.Session, null);
            Assert.That(f.Session.CheckDependencies(), Is.False);
            Assert.That(f.Session.UnavailableReason, Does.Contain("forward review profile is missing"));
            Assert.That(f.Session.ForwardReviewStatus, Does.Contain("unavailable"));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void AuthoredAP02DeckProvidesAStaticFlatReviewSupport()
        {
            using var f = new Fixture(false); Physics.SyncTransforms();
            var ray = new Ray(f.Station.transform.position + Vector3.up * 10, Vector3.down);
            Collider support = null; var nearest = float.PositiveInfinity; RaycastHit contact = default;
            foreach (var collider in f.Station.GetComponentsInChildren<Collider>())
            {
                if (collider.gameObject == f.Ground || collider.isTrigger || collider.attachedRigidbody != null
                    || collider.GetComponentInParent<TopDown3DGroundSurface>() == null || !collider.Raycast(ray, out var hit, 30)
                    || hit.distance >= nearest) continue;
                support = collider; nearest = hit.distance; contact = hit;
            }
            Assert.That(support, Is.Not.Null, "The real deck must supply the authored ground marker and static collider.");
            Assert.That(Vector3.Angle(contact.normal, Vector3.up), Is.LessThan(.25f));
            f.Actor.transform.position = contact.point + Vector3.up * .91f; f.Player.position = f.Actor.transform.position;
            f.Driver.ConfigureLaboratoryForwardReview(f.Policy, f.Station); f.Call("Awake"); f.Actor.SetActive(true);
            void Set(string name, object value) => typeof(TopDown3DPlayerMotor).GetField(name, Private).SetValue(f.Motor, value);
            Set("observedGroundSupport", support); Set("laboratoryGroundSupport", support);
            Set("observedSupportMatrix", support.transform.localToWorldMatrix);
            Set("completedLaboratoryGroundPlane", new Plane(contact.normal, contact.point));
            Assert.That(f.TryPresent(), Is.True); Assert.That(f.Driver.LaboratoryForwardReviewActive, Is.True);
        }

        [Test, Category("PrivateAnimationQualification")]
        public void ReviewKeepsTheBaselineAndQualificationFlagsAndUsesOneOutput()
        {
            using var f = new Fixture();
            Assert.That(f.Driver.LocomotionProfile, Is.SameAs(f.Session.Bindings.locomotion));
            Assert.That(f.Policy.qualified || f.Policy.terrainQualified, Is.False);
            Assert.That(f.Driver.LaboratoryForwardReady, Is.True, f.Driver.LaboratoryForwardFailure);
            Assert.That(f.Graph.GetOutputCount(), Is.EqualTo(1));
            Assert.That(f.Get<AnimationPlaygroundTerrainPose>("forwardTerrainPose"), Is.Null);
            f.Frame(2.7f, .2); Assert.That(f.Driver.LaboratoryForwardReviewActive, Is.True);
            Assert.That(f.Mixer.GetInputWeight(24), Is.EqualTo(1));
            Assert.That(f.Mixer.GetInput(24).GetInputCount(), Is.EqualTo(4));
            for (var i = 0; i < 22; i++) Assert.That(f.Mixer.GetInputWeight(i), Is.Zero);
            var review = (AnimationMixerPlayable)f.Mixer.GetInput(24);
            for (var i = 1; i < 4; i++) Assert.That(((AnimationClipPlayable)review.GetInput(i)).GetAnimationClip(), Is.SameAs(f.Policy.gaits[i - 1].sourceClip));
            Assert.That(f.Driver.TryCapturePoseBlend(f, .18f), Is.True, "Existing action capture must traverse the nested review mixer.");
            Assert.That(f.Driver.LaboratoryForwardReviewActive, Is.False);
            Assert.That(f.Driver.EndPoseBlend(f), Is.True);
        }

        [TestCase(0f)]
        [TestCase(.7f)]
        [TestCase(1.4f)]
        [TestCase(2.05f)]
        [TestCase(2.7f)]
        [TestCase(3.25f)]
        [TestCase(3.8f)]
        [Category("PrivateAnimationQualification")]
        public void NestedReviewSamplesClearTheFlatFloorWithoutMovingThePlayer(float speed)
        {
            using var f = new Fixture(); var position = f.Player.position; var minimum = float.PositiveInfinity;
            for (var i = 0; i <= 64; i++)
            {
                f.Frame(speed, i / 64d);
                var sampler = f.Get<AnimationPlaygroundFootProjection.Sampler>("footProjectionSampler");
                minimum = Mathf.Min(minimum, sampler.MinimumWorldY() - (position.y - .91f));
                Assert.That(f.Driver.LaboratoryForwardLift, Is.InRange(-.03001f, .08001f));
            }
            Assert.That(minimum, Is.GreaterThanOrEqualTo(-.001f));
            Assert.That(f.Player.position, Is.EqualTo(position)); Assert.That(f.Graph.GetOutputCount(), Is.EqualTo(1));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void ReviewRejectsSlopesForeignSupportAndTheDeckEdgeAndRestoresBaselineWeights()
        {
            using var f = new Fixture(); f.Mixer.SetInputWeight(0, .25f); f.Mixer.SetInputWeight(2, .75f);
            f.Frame(2.7f, .2); f.Call("PrepareForwardGraphUpdate");
            Assert.That(f.Mixer.GetInputWeight(24), Is.Zero);
            Assert.That(f.Mixer.GetInputWeight(0), Is.EqualTo(.25f)); Assert.That(f.Mixer.GetInputWeight(2), Is.EqualTo(.75f));
            f.Support(1); Assert.That(f.TryPresent(), Is.False);
            f.Support(0); f.Ground.transform.SetParent(null, true); Assert.That(f.TryPresent(), Is.False);
            f.Ground.transform.SetParent(f.Station.transform, true); f.Support(0);
            f.Player.position += Vector3.right * f.Station.Footprint.x; f.Actor.transform.position = f.Player.position;
            Assert.That(f.TryPresent(), Is.False); Assert.That(f.Driver.LaboratoryForwardReviewActive, Is.False);
        }

        [TestCase(SkinWeights.OneBone)]
        [TestCase(SkinWeights.TwoBones)]
        [TestCase(SkinWeights.FourBones)]
        [Category("PrivateAnimationQualification")]
        public void ColdEquippedReviewClearsTheDeckAtSupportedSkinQualities(SkinWeights quality)
        {
            using var f = new Fixture(true, quality); var mesh = new Mesh();
            try
            {
                var minimum = float.PositiveInfinity; var maximum = float.NegativeInfinity;
                var foundCloak = false; var foundRigidKit = false;
                foreach (var speed in new[] { 0f, 1.4f, 2.05f, 2.7f, 3.25f, 3.8f })
                for (var i = 0; i <= 16; i++)
                {
                    f.Frame(speed, i / 16d);
                    foreach (var renderer in f.Driver.HumanoidAnimator.transform.parent.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                        Vector3[] vertices;
                        if (renderer is SkinnedMeshRenderer skin)
                        { skin.BakeMesh(mesh, true); vertices = mesh.vertices; foundCloak |= skin.name == "Cloak cloth skin"; }
                        else if (renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                        { vertices = filter.sharedMesh.vertices; foundRigidKit = true; }
                        else continue;
                        foreach (var vertex in vertices)
                        {
                            var height = renderer.transform.TransformPoint(vertex).y - (f.Player.position.y - .91f);
                            minimum = Mathf.Min(minimum, height); maximum = Mathf.Max(maximum, height);
                        }
                    }
                }
                Assert.That(foundCloak && foundRigidKit, Is.True, "Include skinned cloak and the carried rigid equipment.");
                Assert.That(minimum, Is.GreaterThanOrEqualTo(-.001f)); Assert.That(maximum, Is.LessThanOrEqualTo(1.821f));
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test, Category("PrivateAnimationQualification")]
        public void CompletedMotionDrivesTheActualReviewBranchAndActionOwnershipVetoesIt()
        {
            using var f = new Fixture();
            for (var i = 1; i <= 3; i++)
            {
                var sample = new AnimationPlaygroundGroundedMotion(i, 5, Time.fixedTimeAsDouble + i * .02, .02,
                    Vector3.forward * .054f, Vector3.up, true, i == 1);
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(f.Motor, sample);
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("AdvanceForwardMotion", Private).Invoke(f.Driver, new object[] { sample });
            }
            Assert.That(f.Driver.LaboratoryForwardSpeed, Is.EqualTo(2.7f).Within(.0001f));
            Assert.That(f.TryPresent(), Is.True); Assert.That(f.Driver.LaboratoryForwardReviewActive, Is.True);
            Assert.That(f.Motor.TryBeginActionConstraint(f), Is.True); f.Call("PrepareForwardGraphUpdate");
            Assert.That(f.TryPresent(), Is.False); Assert.That(f.Mixer.GetInputWeight(24), Is.Zero);
            f.Motor.EndActionConstraint(f);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [Category("PrivateAnimationQualification")]
        public void MissingOrDisabledReviewOwnersCannotTurnTheExperimentIntoGeneralLocomotion(int ownerChange)
        {
            using var f = new Fixture(); f.Frame(2.7f, .2);
            if (ownerChange == 0) f.Station.enabled = false;
            else if (ownerChange == 1) f.Session.enabled = false;
            else Object.DestroyImmediate(f.Station);
            f.Call("PrepareForwardGraphUpdate");
            Assert.That(f.Driver.LaboratoryForwardReviewConfigured, Is.True);
            Assert.That(f.TryPresent(), Is.False); Assert.That(f.Driver.LaboratoryForwardReviewActive, Is.False);
            Assert.That(f.Mixer.GetInputWeight(24), Is.Zero);
        }

        // Authored-scene wiring, explicit support and graph poses only. No physics simulation or Play Mode.
        private sealed class Fixture : IDisposable
        {
            public readonly Scene Scene;
            public readonly TopDown3DAnimationPlaygroundSession Session;
            public readonly TopDown3DAnimationPlaygroundStation Station;
            public readonly GameObject Actor, Ground;
            public readonly TopDown3DPlayerMotor Motor;
            public readonly Rigidbody Player;
            public readonly TopDown3DPlayerAnimationDriver Driver;
            public readonly AnimationPlaygroundForwardLocomotionProfile Policy;
            public readonly PlayableGraph Graph;
            public readonly AnimationMixerPlayable Mixer;
            private readonly SkinWeights quality = QualitySettings.skinWeights;
            public Fixture(bool initialize = true, SkinWeights influence = SkinWeights.FourBones)
            {
                try
                {
                QualitySettings.skinWeights = influence;
                Scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
                Session = TopDown3DAnimationPlaygroundSession.Find(Scene); Assert.That(Session, Is.Not.Null);
                Station = Session.Stations.Single(s => s.StationId == "AP02");
                Actor = Session.Player.gameObject; Actor.SetActive(false); Motor = Session.Player; Player = Actor.GetComponent<Rigidbody>();
                Actor.transform.position = Station.transform.position + Vector3.up * 2; Player.position = Actor.transform.position;
                Policy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(PolicyPath));
                Driver = Actor.GetComponent<TopDown3DPlayerAnimationDriver>();
                Driver.ConfigureLaboratory(Session.Bindings);
                if (initialize)
                {
                    Driver.ConfigureLaboratoryForwardReview(Policy, Station); Call("Awake"); Actor.SetActive(true);
                    Graph = Get<PlayableGraph>("playableGraph"); Mixer = Get<AnimationMixerPlayable>("mixer");
                    Driver.ConfigureCamp(TopDown3DCampSettings.Load()); Driver.SetCampPresentation(-1, 1, false, true);
                }
                Ground = new GameObject("Explicit review support"); SceneManager.MoveGameObjectToScene(Ground, Scene);
                Ground.transform.SetParent(Station.transform, true); Ground.AddComponent<BoxCollider>(); Support(0);
                }
                catch { Dispose(); throw; }
            }
            public T Get<T>(string name) => (T)typeof(TopDown3DPlayerAnimationDriver).GetField(name, Private).GetValue(Driver);
            public void Call(string name) => typeof(TopDown3DPlayerAnimationDriver).GetMethod(name, Private).Invoke(Driver, null);
            public void Frame(float speed, double phase)
            {
                Call("PrepareForwardGraphUpdate");
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardFrame", Private).Invoke(Driver, new object[] { speed, phase, .02f });
                Call("LateUpdate");
            }
            public bool TryPresent() => (bool)typeof(TopDown3DPlayerAnimationDriver).GetMethod("TryPresentForwardLocomotion", Private).Invoke(Driver, null);
            public void Support(float angle)
            {
                var normal = Quaternion.AngleAxis(angle, Vector3.right) * Vector3.up;
                var plane = new Plane(normal, Actor.transform.position + Vector3.down * .91f);
                void Set(string name, object value) => typeof(TopDown3DPlayerMotor).GetField(name, Private).SetValue(Motor, value);
                Set("hasCompletedLaboratoryGroundPlane", true); Set("completedLaboratoryGroundPlane", plane);
                Set("observedGroundSupport", Ground.GetComponent<Collider>()); Set("laboratoryGroundSupport", Ground.GetComponent<Collider>());
                Set("observedSupportMatrix", Ground.transform.localToWorldMatrix);
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(Motor, true);
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(Motor,
                    new AnimationPlaygroundGroundedMotion(1, 1, Time.fixedTimeAsDouble, .02, Vector3.zero, normal, true, false));
            }
            public void Dispose()
            {
                if (Driver != null) Call("OnDestroy");
                if (Policy != null) Object.DestroyImmediate(Policy);
                if (Scene.IsValid()) EditorSceneManager.CloseScene(Scene, true);
                QualitySettings.skinWeights = quality;
            }
        }
    }
}
