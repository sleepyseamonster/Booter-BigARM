using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundTangentFootPoseTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(.003f)] [TestCase(.01f)] [TestCase(.03f)] [TestCase(.05f)]
        [TestCase(-.01f)] [TestCase(179.9f)] [TestCase(180f)]
        public void JointAlignmentRetainsSmallCorrectionsAndOppositeDirections(float degrees)
        {
            var from = Vector3.forward * .5f;
            var to = Quaternion.AngleAxis(degrees, Vector3.up) * from;
            var align = typeof(AnimationPlaygroundTangentFootPose.ContactJob).GetMethod("Align", BindingFlags.Static | BindingFlags.NonPublic);
            var rotation = (Quaternion)align.Invoke(null, new object[] { from, to });
            Assert.That(Vector3.Distance(rotation * from, to), Is.LessThan(.000001f));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void DisabledStagePassesTheExistingPoseThroughWithoutMovingPhysics()
        {
            using var f = new Fixture();
            f.Pose(); var before = f.Bones(); var root = f.Actor.transform.position;
            var source = f.Output.GetSourcePlayable();
            using (var stage = f.Stage(9))
            {
                f.Pose(); AssertSamePose(before, f.Bones());
                Assert.That(stage.Sample.evaluations, Is.Zero);
                Assert.That(f.Graph.GetOutputCount(), Is.EqualTo(1));
                Assert.That(f.Actor.transform.position, Is.EqualTo(root));
                Assert.That(f.Actor.GetComponent<Rigidbody>().position, Is.EqualTo(root));
            }
            Assert.That(f.Output.GetSourcePlayable(), Is.EqualTo(source));
        }

        [TestCase(SkinWeights.OneBone, 9)] [TestCase(SkinWeights.TwoBones, 9)] [TestCase(SkinWeights.FourBones, 9)]
        [TestCase(SkinWeights.OneBone, 300)] [TestCase(SkinWeights.TwoBones, 300)] [TestCase(SkinWeights.FourBones, 300)]
        [Category("PrivateAnimationQualification")]
        public void SmallTangentCorrectionPreservesFootHeightRotationAndLegLength(SkinWeights quality, int vertex)
        {
            using var f = new Fixture(quality);
            f.Pose(); var original = f.Bones(); var root = f.Actor.transform.position;
            var left = vertex == 9;
            var hip = f.Animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            var knee = f.Animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
            var foot = f.Animator.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
            var upperLength = Vector3.Distance(hip.position, knee.position); var lowerLength = Vector3.Distance(knee.position, foot.position);
            var footHeight = foot.position.y; var rotation = foot.rotation;
            var anchor = f.Point(vertex) + Vector3.ProjectOnPlane(hip.position - foot.position, Vector3.up).normalized * .005f;
            using var stage = f.Stage(vertex);
            stage.Configure(anchor + Vector3.up * 100, (int)quality); f.Pose();
            var sample = stage.Sample;
            Assert.That(sample.available, Is.True, $"Reason {sample.unavailableReason}, reach {sample.reachDeficit}, residual {sample.residual}");
            Assert.That(sample.evaluations, Is.EqualTo(1));
            Assert.That(Vector3.ProjectOnPlane(f.Point(vertex) - anchor, Vector3.up).magnitude, Is.LessThan(.001f));
            Assert.That(foot.position.y, Is.EqualTo(footHeight).Within(.001f));
            Assert.That(Quaternion.Angle(rotation, foot.rotation), Is.LessThan(.01f));
            Assert.That(Vector3.Distance(hip.position, knee.position), Is.EqualTo(upperLength).Within(.00001f));
            Assert.That(Vector3.Distance(knee.position, foot.position), Is.EqualTo(lowerLength).Within(.00001f));
            Assert.That(f.Actor.transform.position, Is.EqualTo(root));
            Assert.That(f.Graph.GetOutputCount(), Is.EqualTo(1));
            stage.Disable(); f.Pose(); AssertSamePose(original, f.Bones());
            Assert.That(stage.Sample.evaluations, Is.EqualTo(1));
            stage.Configure(anchor, (int)quality); f.Pose();
            Assert.That(stage.Sample.available, Is.True); Assert.That(stage.Sample.evaluations, Is.EqualTo(2));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void OverBudgetCorrectionRejectsWithoutWritingThePose()
        {
            using var f = new Fixture();
            f.Pose(); var original = f.Bones();
            using var stage = f.Stage(9);
            stage.Configure(f.Point(9) + Vector3.right * .04f, 4); f.Pose();
            Assert.That(stage.Sample.available, Is.False); Assert.That(stage.Sample.unavailableReason, Is.EqualTo(2));
            AssertSamePose(original, f.Bones());
        }

        [Test, Category("PrivateAnimationQualification")]
        public void InvalidConfigurationLeavesTheOutputAndSourceAssetsUntouched()
        {
            using var f = new Fixture(); f.Pose();
            var source = f.Output.GetSourcePlayable();
            Assert.Throws<ArgumentException>(() => f.Stage(-1));
            Assert.Throws<ArgumentException>(() => f.Stage(f.Projection.vertices.Length));
            var invalid = Object.Instantiate(f.Projection);
            try
            {
                invalid.bonePaths = new[] { "" };
                invalid.vertices = new[] { new AnimationPlaygroundFootVertex { points = new[] {
                    new AnimationPlaygroundSkinPoint { bone = 0, weight = 1, position = Vector3.zero } } } };
                Assert.Throws<ArgumentException>(() => new AnimationPlaygroundTangentFootPose(f.Animator, f.Visual.transform, invalid, 0, f.Graph));
            }
            finally { Object.DestroyImmediate(invalid); }
            Assert.That(f.Output.GetSourcePlayable(), Is.EqualTo(source));
            using var stage = f.Stage(9);
            Assert.Throws<ArgumentException>(() => stage.Configure(new Vector3(float.NaN, 0, 0), 4));
            Assert.Throws<ArgumentException>(() => stage.Configure(Vector3.zero, 3));
            Assert.That(stage.Sample.enabled, Is.False);
            Assert.That(f.Graph.GetOutputCount(), Is.EqualTo(1));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void DisposalRestoresTheExistingSourceAndHonorsNestedOwnership()
        {
            using var f = new Fixture();
            var source = f.Output.GetSourcePlayable();
            using var inner = f.Stage(9); var innerSource = f.Output.GetSourcePlayable();
            using (var outer = f.Stage(300))
            {
                Assert.Throws<InvalidOperationException>(() => inner.Dispose());
                Assert.That(inner.IsDisposed, Is.False);
            }
            Assert.That(f.Output.GetSourcePlayable(), Is.EqualTo(innerSource));
            inner.Dispose(); inner.Dispose(); Assert.That(inner.IsDisposed, Is.True);
            Assert.That(f.Output.GetSourcePlayable(), Is.EqualTo(source));
            Assert.Throws<ObjectDisposedException>(() => inner.Configure(Vector3.zero, 4));
            inner.Disable();
            var destroyedGraphStage = f.Stage(9); f.Graph.Destroy();
            destroyedGraphStage.Dispose(); destroyedGraphStage.Dispose();
            Assert.That(destroyedGraphStage.IsDisposed, Is.True);
        }

        [Test, Category("PrivateAnimationQualification")]
        public void WarmStageEvaluatesOncePerPoseWithoutManagedAllocation()
        {
            using var f = new Fixture(); f.Pose();
            var ankle = f.Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var hip = f.Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var anchor = f.Point(9) + Vector3.ProjectOnPlane(hip.position - ankle.position, Vector3.up).normalized * .005f;
            using var stage = f.Stage(9);
            for (var i = 0; i < 32; i++) { stage.Configure(anchor, 4); f.Pose(); }
            var evaluations = stage.Sample.evaluations;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 200; i++) { stage.Configure(anchor, 4); f.Pose(); }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(stage.Sample.evaluations, Is.EqualTo(evaluations + 200));
            Assert.That(stage.Sample.available, Is.True);
            Assert.That(stage.Sample.iterations, Is.GreaterThan(0), "Exercise the active solve, rather than its zero-correction shortcut.");
        }

        private static void AssertSamePose((Vector3 p, Quaternion q)[] a, (Vector3 p, Quaternion q)[] b)
        {
            Assert.That(b.Length, Is.EqualTo(a.Length));
            for (var i = 0; i < a.Length; i++)
            {
                Assert.That(Vector3.Distance(a[i].p, b[i].p), Is.LessThan(.00001f), $"Bone {i}");
                Assert.That(Quaternion.Angle(a[i].q, b[i].q), Is.LessThan(.01f), $"Bone {i}");
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly IDisposable shared;
            private readonly Action<float, double, double> pose;
            public readonly GameObject Actor, Visual;
            public readonly TopDown3DPlayerAnimationDriver Driver;
            public readonly PlayableGraph Graph;
            public readonly AnimationPlaygroundFootProjection Projection;
            public Animator Animator => Driver.HumanoidAnimator;
            public AnimationPlayableOutput Output => (AnimationPlayableOutput)Graph.GetOutput(0);
            public Fixture(SkinWeights quality = SkinWeights.FourBones)
            {
                var type = typeof(AnimationPlaygroundForwardReviewTests).GetNestedType("Fixture", BindingFlags.NonPublic);
                shared = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new object[] { true, quality }, null);
                try
                {
                    Actor = (GameObject)type.GetField("Actor").GetValue(shared);
                    // Joint-length assertions need origin precision, independently
                    // of the separate authored-world replay qualification.
                    Actor.transform.SetPositionAndRotation(Vector3.up * .91f, Quaternion.identity);
                    Actor.GetComponent<Rigidbody>().position = Actor.transform.position;
                    Actor.GetComponent<Rigidbody>().rotation = Quaternion.identity;
                    Driver = (TopDown3DPlayerAnimationDriver)type.GetField("Driver").GetValue(shared);
                    Graph = (PlayableGraph)type.GetField("Graph").GetValue(shared);
                    Visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", Private).GetValue(Driver);
                    Projection = ((AnimationPlaygroundForwardLocomotionProfile)type.GetField("Policy").GetValue(shared)).footProjection;
                    pose = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), Driver,
                        typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", Private));
                }
                catch { shared.Dispose(); throw; }
            }
            public void Pose() => pose(1.4f, .25, 0);
            public AnimationPlaygroundTangentFootPose Stage(int vertex) => new(Animator, Visual.transform, Projection, vertex, Graph);
            public (Vector3 p, Quaternion q)[] Bones() => Animator.GetComponentsInChildren<Transform>().Select(t => (t.position, t.rotation)).ToArray();
            public Vector3 Point(int vertex)
            {
                var limit = (int)QualitySettings.skinWeights; var point = Vector3.zero; var total = 0f;
                foreach (var p in Projection.vertices[vertex].points.OrderByDescending(p => p.weight).Take(limit))
                {
                    var path = Projection.bonePaths[p.bone]; var bone = path.Length == 0 ? Visual.transform : Visual.transform.Find(path);
                    point += bone.TransformPoint(p.position) * p.weight; total += p.weight;
                }
                return point / total;
            }
            public void Dispose() => shared.Dispose();
        }
    }
}
