using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DClimbSurfaceMathTests
    {
        [TestCase(20f, TopDown3DClimbMode.Ground)]
        [TestCase(40f, TopDown3DClimbMode.Incline)]
        [TestCase(60f, TopDown3DClimbMode.Scramble)]
        [TestCase(90f, TopDown3DClimbMode.Wall)]
        [TestCase(120f, TopDown3DClimbMode.Overhang)]
        [TestCase(170f, TopDown3DClimbMode.None)]
        public void SelectMode_UsesMeasuredContactAngle(float angle, TopDown3DClimbMode expected)
        {
            var normal = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;

            Assert.That(
                TopDown3DClimbSurfaceMath.SelectMode(normal, TopDown3DClimbMode.None),
                Is.EqualTo(expected));
        }

        [Test]
        public void SelectMode_UsesHysteresisAtScrambleBoundary()
        {
            var nearBoundary = Quaternion.AngleAxis(56f, Vector3.forward) * Vector3.up;
            Assert.That(
                TopDown3DClimbSurfaceMath.SelectMode(nearBoundary, TopDown3DClimbMode.Incline),
                Is.EqualTo(TopDown3DClimbMode.Incline));
            Assert.That(
                TopDown3DClimbSurfaceMath.SelectMode(nearBoundary, TopDown3DClimbMode.Scramble),
                Is.EqualTo(TopDown3DClimbMode.Scramble));
        }

        [Test]
        public void SurfaceUp_RemainsTangentOnOverhangAndUsesPreviousFrameOnCeiling()
        {
            var overhangNormal = Quaternion.AngleAxis(120f, Vector3.forward) * Vector3.up;
            var overhangUp = TopDown3DClimbSurfaceMath.SurfaceUp(overhangNormal, Vector3.up);
            var ceilingUp = TopDown3DClimbSurfaceMath.SurfaceUp(Vector3.down, overhangUp);

            Assert.That(Vector3.Dot(overhangUp, overhangNormal), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(overhangUp.y, Is.GreaterThan(0f));
            Assert.That(ceilingUp.sqrMagnitude, Is.GreaterThan(0.99f));
            Assert.That(Vector3.Dot(ceilingUp, Vector3.down), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void MissingContact_HasNoTraversalModeOrSurfaceFrame()
        {
            Assert.That(
                TopDown3DClimbSurfaceMath.SelectMode(Vector3.zero, TopDown3DClimbMode.Wall),
                Is.EqualTo(TopDown3DClimbMode.None));
            Assert.That(
                TopDown3DClimbSurfaceMath.SurfaceUp(Vector3.zero, Vector3.up),
                Is.EqualTo(Vector3.zero));
        }
    }
}
