using System;
using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundTerrainPoseTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static AnimationPlaygroundForwardLocomotionProfile Policy()
        {
            var value = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
            if (value == null) Assert.Ignore("Restore the private measured forward candidate.");
            return value;
        }

        [Test, Category("PrivateAnimationQualification")]
        public void TerrainExtensionRequiresItsVersionAndBoundsWithoutEnablingOldAssets()
        {
            Assert.That(Policy().terrainQualified, Is.False);
            var policy = Object.Instantiate(Policy());
            try
            {
                policy.terrainQualified = true; policy.terrainSchema = 2;
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out _, false), Is.False);
                policy.terrainSchema = 1; policy.maximumTerrainBodyLowering = .451f;
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out _, false), Is.False);
                policy.maximumTerrainBodyLowering = .45f; policy.maximumTerrainGarmentDisplacement = float.NaN;
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out _, false), Is.False);
                policy.maximumTerrainGarmentDisplacement = .35f;
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out var reason, false), Is.True, reason);
            }
            finally { Object.DestroyImmediate(policy); }
        }

        [TestCase(SkinWeights.OneBone, 0f)] [TestCase(SkinWeights.TwoBones, 0f)] [TestCase(SkinWeights.FourBones, 0f)]
        [TestCase(SkinWeights.OneBone, 30f)] [TestCase(SkinWeights.TwoBones, 30f)] [TestCase(SkinWeights.FourBones, 30f)]
        [TestCase(SkinWeights.OneBone, 48f)] [TestCase(SkinWeights.TwoBones, 48f)] [TestCase(SkinWeights.FourBones, 48f)]
        [Category("PrivateAnimationQualification")]
        public void RealDriverKeepsOneEvaluationAndClearsEquippedPoseWithoutMovingPhysics(SkinWeights quality, float slope)
        {
            using var fixture = new Fixture(quality);
            fixture.Support(slope);
            foreach (var speed in new[] { 0f, 1.4f, 2.05f, 2.7f, 3.25f, 3.8f })
            foreach (var phase in new[] { 0d, .125, .25, .5, .875 })
            {
                var before = fixture.Driver.LaboratoryTerrainEvaluations;
                fixture.Frame(speed, phase, 0); fixture.Garment();
                Assert.That(fixture.Driver.LaboratoryTerrainEvaluations, Is.EqualTo(before + 1));
                Assert.That(fixture.Graph.GetOutputCount(), Is.EqualTo(1));
                Assert.That(fixture.Graph.GetTimeUpdateMode(), Is.EqualTo(DirectorUpdateMode.Manual));
                Assert.That(fixture.Actor.transform.position, Is.EqualTo(Vector3.zero));
                Assert.That(fixture.Actor.GetComponent<Rigidbody>().position, Is.EqualTo(Vector3.zero));
                Assert.That(fixture.MinimumPlaneAndHeight(out var height), Is.GreaterThanOrEqualTo(-.001f), $"{speed}m/s phase {phase}");
                Assert.That(height, Is.LessThanOrEqualTo(1.821f));
                Assert.That(fixture.Driver.GarmentFloorExceeded, Is.False);
                Assert.That(fixture.Driver.LaboratoryTerrainBodyLowering, Is.InRange(0, .45f));
            }
            fixture.Frame(2.7f, .25, 0); fixture.Garment();
            var foot = fixture.Driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var position = foot.position;
            fixture.Frame(2.7f, .75, 0); fixture.Garment();
            fixture.Frame(2.7f, .25, 0); fixture.Garment();
            Assert.That(Vector3.Distance(position, foot.position), Is.LessThan(.00002f));
            Assert.That(fixture.Driver.LaboratoryForwardContacts, Is.Zero, "Pose evaluation cannot invent completed travel contacts.");
            fixture.Call("PrepareForwardGraphUpdate");
            Assert.That(fixture.Stage.Sample.enabled, Is.False, "Other owners must receive an inert stage before their update.");
            fixture.Call("OnDisable"); Assert.That(fixture.Stage.Sample.enabled, Is.False);
            fixture.Call("OnEnable");
            var retained = fixture.Stage; fixture.Call("OnDestroy");
            Assert.That(retained.IsDisposed, Is.True); retained.Dispose();
        }

        [Test, Category("PrivateAnimationQualification")]
        public void NativeFlatPoseMatchesDisabledStageAndWarmTerrainHasNoManagedAllocation()
        {
            using var fixture = new Fixture(SkinWeights.FourBones);
            fixture.Support(0); fixture.Frame(3.8f, .37, 0); fixture.Garment();
            var bones = fixture.Driver.HumanoidAnimator.GetComponentsInChildren<Transform>();
            var positions = Array.ConvertAll(bones, b => b.position);
            typeof(TopDown3DPlayerMotor).GetField("hasCompletedLaboratoryGroundPlane", Private).SetValue(fixture.Motor, false);
            fixture.Frame(3.8f, .37, 0); fixture.Garment();
            for (var i = 0; i < bones.Length; i++) Assert.That(Vector3.Distance(positions[i], bones[i].position), Is.LessThan(.00001f));
            fixture.Support(30);
            for (var i = 0; i < 32; i++) { fixture.Frame(2.7f, i / 32d, 0); fixture.Garment(); }
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) { fixture.Frame(2.7f, i / 128d, 0); fixture.Garment(); }
            var recurring = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Assert.That(recurring, Is.Zero);
        }

        [Test, Category("PrivateAnimationQualification")]
        public void NearFlatSurfaceDoesNotIntroduceAPoseStep()
        {
            using var fixture = new Fixture(SkinWeights.FourBones);
            var roles = new[] { HumanBodyBones.Hips, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.Head };
            var bones = Array.ConvertAll(roles, r => fixture.Driver.HumanoidAnimator.GetBoneTransform(r));
            var positions = new Vector3[bones.Length]; var maximum = 0f;
            foreach (var speed in new[] { 1.4f, 2.7f, 3.8f })
            for (var phase = 0; phase < 32; phase++)
            {
                fixture.Support(.05f); fixture.Frame(speed, phase / 32d, 0); fixture.Garment();
                for (var i = 0; i < bones.Length; i++) positions[i] = bones[i].position;
                fixture.Support(.12f); fixture.Frame(speed, phase / 32d, 0); fixture.Garment();
                for (var i = 0; i < bones.Length; i++) maximum = Mathf.Max(maximum, Vector3.Distance(positions[i], bones[i].position));
            }
            Assert.That(maximum, Is.LessThan(.002f), "Tiny normal changes around level ground must not switch to a different leg solution.");
        }

        [Test, Category("PrivateAnimationQualification")]
        public void ActionOwnerUsesInertTerrainStageAndReturnsWithoutAnotherOutput()
        {
            using var fixture = new Fixture(SkinWeights.FourBones);
            fixture.Support(30); fixture.Frame(2.7f, .25, 0); fixture.Garment();
            var before = fixture.Driver.LaboratoryTerrainEvaluations; var stage = fixture.Stage; var owner = new object();
            Assert.That(fixture.Driver.TryBeginAnimationPresentation(owner, Policy().gaits[0].sourceClip), Is.True);
            fixture.Call("Update"); fixture.Graph.Evaluate(0);
            Assert.That(stage.Sample.enabled, Is.False);
            Assert.That(fixture.Driver.LaboratoryTerrainEvaluations, Is.EqualTo(before));
            Assert.That(fixture.Driver.EndAnimationPresentation(owner), Is.True);
            fixture.Call("Update"); fixture.Garment();
            Assert.That(stage.Sample.enabled, Is.True);
            Assert.That(fixture.Driver.LaboratoryTerrainEvaluations, Is.EqualTo(before + 1));
            Assert.That(fixture.Graph.GetOutputCount(), Is.EqualTo(1));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void ResetBetweenPoseAndGarmentCannotProjectAgainstAnOldPlane()
        {
            using var fixture = new Fixture(SkinWeights.FourBones);
            fixture.Support(30); fixture.Frame(2.7f, .25, 0); fixture.Garment();
            var evaluations = fixture.Driver.LaboratoryTerrainEvaluations;
            var owner = new object(); Assert.That(fixture.Motor.TryBeginActionConstraint(owner), Is.True);
            fixture.Garment();
            Assert.That(fixture.Stage.Sample.enabled, Is.False);
            Assert.That(fixture.Driver.LastGarmentFloorDisplacement, Is.Zero);
            Assert.That(fixture.Driver.LaboratoryTerrainEvaluations, Is.EqualTo(evaluations));
            fixture.Motor.EndActionConstraint(owner);
        }

        [TestCase(45f, .469049)] [TestCase(90f, .35190978)]
        [Category("PrivateAnimationQualification")]
        public void SteepCrossSlopeDoesNotSwitchKneeSolutionsWithinATinyPhaseChange(float heading, double phase)
        {
            using var fixture = new Fixture(SkinWeights.FourBones);
            fixture.Support(48, heading);
            var bones = new[] { fixture.Driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),
                fixture.Driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg) };
            fixture.Frame(2.05f, phase - .000001, 0);
            var before = Array.ConvertAll(bones, b => b.position);
            fixture.Frame(2.05f, phase + .000001, 0);
            for (var i = 0; i < bones.Length; i++)
                Assert.That(Vector3.Distance(before[i], bones[i].position), Is.LessThan(.001f));
        }

        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.FourBones)]
        [Category("PrivateAnimationQualification")]
        public void ColdGarmentUsesActualLateUpdateWithoutASecondGraphEvaluation(SkinWeights quality)
        {
            using var fixture = new Fixture(quality, true);
            fixture.Support(30);
            var before = fixture.Driver.LaboratoryTerrainEvaluations;
            fixture.Frame(2.7f, .25, 0); fixture.Garment();
            Assert.That(fixture.Driver.LaboratoryTerrainEvaluations, Is.EqualTo(before + 1));
            Assert.That(fixture.Driver.GarmentFloorExceeded, Is.False);
            Assert.That(fixture.MinimumPlaneAndHeight(out var height), Is.GreaterThanOrEqualTo(-.001f));
            Assert.That(height, Is.LessThanOrEqualTo(1.821f));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void AirborneFootKeepsTangentStrideWithinTheJointReachEnvelope()
        {
            using var fixture = new Fixture(SkinWeights.FourBones);
            var knee = fixture.Driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            var foot = fixture.Driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            fixture.Support(0); fixture.Frame(3.8f, .453125, 0);
            var nativeRotation = foot.rotation;
            fixture.Support(30); fixture.Frame(3.8f, .453125, 0);
            Assert.That(fixture.Stage.Sample.leftWeight, Is.Zero);
            var sample = fixture.Stage.Sample;
            var sourceFoot = (Vector3)sample.matrices[sample.leftFootCacheBone].GetColumn(3);
            Assert.That(fixture.Motor.TryGetLaboratorySupportPlane(out var plane), Is.True);
            var surfaceOrigin = plane.ClosestPointOnPlane(Vector3.up * (-.91f + .2f));
            var tangent = Vector3.ProjectOnPlane(Vector3.forward, plane.normal).normalized;
            Assert.That(sample.leftReleased, Is.LessThan(.0001f));
            Assert.That(sample.leftFlightLowering, Is.GreaterThan(.001f), "This fixture exercises the minimum-reach constraint.");
            Assert.That(plane.GetDistanceToPoint(sample.leftTarget),
                Is.EqualTo(sourceFoot.y + sample.flatLift + .91f - sample.leftFlightLowering + sample.leftReleased).Within(.0001f));
            Assert.That(Vector3.Dot(sample.leftTarget - surfaceOrigin, tangent), Is.EqualTo(sourceFoot.z).Within(.0001f));
            Assert.That(sample.leftTarget.x, Is.EqualTo(sourceFoot.x).Within(.0001f));
            var hostShift = fixture.VisualHost.position - sample.visualHostPosition;
            Assert.That(Vector3.Distance(foot.position - hostShift, sample.leftTarget), Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(foot.rotation, Quaternion.AngleAxis(30, Vector3.right) * nativeRotation), Is.LessThan(.01f));
            var hip = fixture.Driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Assert.That(180 - Vector3.Angle(hip.position - knee.position, foot.position - knee.position),
                Is.LessThanOrEqualTo(AnimationPlaygroundTerrainPose.MaximumKneeFlexion + .01f));
        }

        // Support observations are explicit pose fixtures, not simulated travel.
        private sealed class Fixture : IDisposable
        {
            public GameObject Actor { get; private set; }
            public TopDown3DPlayerAnimationDriver Driver { get; private set; }
            public TopDown3DPlayerMotor Motor { get; private set; }
            public PlayableGraph Graph { get; private set; }
            public AnimationPlaygroundTerrainPose Stage { get; private set; }
            public Transform VisualHost => visual.transform;
            public Action<float, double, float> Frame { get; private set; }
            public Action Garment { get; private set; }
            private readonly SkinWeights previousQuality = QualitySettings.skinWeights;
            private Scene scene, preview;
            private GameObject ground, visual;
            private AnimationPlaygroundForwardLocomotionProfile policy;
            private TopDown3DAnimationPlaygroundBindings bindings;
            private Plane plane;
            private Mesh baked;
            private readonly List<Vector3> vertices = new();
            public Fixture(SkinWeights quality) : this(quality, false) { }
            public Fixture(SkinWeights quality, bool cold)
            {
                try
                {
                    QualitySettings.skinWeights = quality;
                    policy = Object.Instantiate(Policy()); policy.qualified = policy.terrainQualified = true;
                    scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
                    preview = EditorSceneManager.NewPreviewScene();
                    var template = TopDown3DAnimationPlaygroundSession.Find(scene);
                    bindings = Object.Instantiate(template.Bindings); bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = policy;
                    // This fixture exercises the retained three-gait terrain policy,
                    // independently of the current five-band Camp Loop selection.
                    bindings.slowWalkSpeed = policy.gaits[0].nominalSpeed;
                    bindings.runSpeed = policy.gaits[1].nominalSpeed;
                    bindings.sprintSpeed = policy.gaits[2].nominalSpeed;
                    bindings.forwardReviewOverride = null; bindings.forwardReviewOverrideRequired = false;
                    bindings.movingTurns = null; bindings.movingTurnsRequired = false;
                    bindings.standingStrafeReview = null; bindings.standingStrafeReviewRequired = false;
                    bindings.continuationReview = null; bindings.continuationReviewRequired = false;
                    Actor = new GameObject("Terrain graph contract"); Actor.SetActive(false); SceneManager.MoveGameObjectToScene(Actor, preview);
                    Motor = Actor.AddComponent<TopDown3DPlayerMotor>(); Driver = Actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                    var shape = Actor.GetComponent<CapsuleCollider>(); shape.height = bindings.capsuleHeight; shape.radius = bindings.capsuleRadius;
                    EditorUtility.CopySerialized(template.Player.GetComponent<TopDown3DPlayerAnimationDriver>(), Driver);
                    Driver.ConfigureLaboratory(bindings); Call("Awake"); Actor.SetActive(true);
                    Assert.That(Driver.HumanoidAnimator.isActiveAndEnabled, Is.True);
                    Frame = (Action<float, double, float>)Delegate.CreateDelegate(typeof(Action<float, double, float>), Driver,
                        typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardFrame", Private));
                    Garment = (Action)Delegate.CreateDelegate(typeof(Action), Driver, typeof(TopDown3DPlayerAnimationDriver).GetMethod("LateUpdate", Private));
                    Graph = (PlayableGraph)typeof(TopDown3DPlayerAnimationDriver).GetField("playableGraph", Private).GetValue(Driver);
                    Stage = (AnimationPlaygroundTerrainPose)typeof(TopDown3DPlayerAnimationDriver).GetField("forwardTerrainPose", Private).GetValue(Driver);
                    Assert.That(Stage, Is.Not.Null);
                    Driver.ConfigureCamp(TopDown3DCampSettings.Load()); Driver.SetCampPresentation(-1, 1, false, true);
                    if (!cold) { Frame(0, 0, 0); Call("LateUpdate"); Call("NormalizeCampSkinWeights"); }
                    visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", Private).GetValue(Driver);
                    foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.quality = SkinQuality.Auto;
                    ground = new GameObject("Recorded static plane"); SceneManager.MoveGameObjectToScene(ground, preview);
                    ground.AddComponent<BoxCollider>().size = new Vector3(20, .1f, 20); ground.AddComponent<TopDown3DGroundSurface>();
                    baked = new Mesh();
                }
                catch { Dispose(); throw; }
            }
            public void Call(string name) => typeof(TopDown3DPlayerAnimationDriver).GetMethod(name, Private).Invoke(Driver, null);
            public void Support(float slope, float heading = 0)
            {
                var normal = Quaternion.Euler(0, heading, 0) * (Quaternion.AngleAxis(slope, Vector3.right) * Vector3.up);
                var point = Vector3.up * (-bindings.capsuleHeight * .5f - bindings.capsuleRadius * (1 / normal.y - 1));
                plane = new Plane(normal, point); ground.transform.SetPositionAndRotation(point - normal * .05f, Quaternion.FromToRotation(Vector3.up, normal));
                var collider = ground.GetComponent<Collider>();
                void Set(string name, object value) => typeof(TopDown3DPlayerMotor).GetField(name, Private).SetValue(Motor, value);
                Set("hasCompletedLaboratoryGroundPlane", true); Set("completedLaboratoryGroundPlane", plane);
                Set("observedGroundSupport", collider); Set("laboratoryGroundSupport", collider); Set("observedSupportMatrix", ground.transform.localToWorldMatrix);
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(Motor, true);
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(Motor,
                    new AnimationPlaygroundGroundedMotion(1, 1, 1, .02, Vector3.zero, normal, true, false));
            }
            public float MinimumPlaneAndHeight(out float height)
            {
                var minimum = float.PositiveInfinity; height = float.NegativeInfinity;
                foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.BakeMesh(baked, true); baked.GetVertices(vertices);
                    foreach (var vertex in vertices)
                    {
                        var p = skin.transform.TransformPoint(vertex); minimum = Mathf.Min(minimum, plane.GetDistanceToPoint(p));
                        height = Mathf.Max(height, p.y + bindings.capsuleHeight * .5f);
                    }
                }
                return minimum;
            }
            public void Dispose()
            {
                QualitySettings.skinWeights = previousQuality;
                if (Driver != null) Call("OnDestroy");
                if (Actor != null) Object.DestroyImmediate(Actor); if (ground != null) Object.DestroyImmediate(ground);
                if (baked != null) Object.DestroyImmediate(baked); if (bindings != null) Object.DestroyImmediate(bindings);
                if (policy != null) Object.DestroyImmediate(policy);
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
