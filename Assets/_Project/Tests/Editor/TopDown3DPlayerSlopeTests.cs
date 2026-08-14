using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DPlayerSlopeTests
    {
        [Test]
        public void WalkableSlope_AcceptsConfiguredLimitAndRejectsSteeperSurface()
        {
            var atLimit = Quaternion.AngleAxis(48f, Vector3.forward) * Vector3.up;
            var tooSteep = Quaternion.AngleAxis(49f, Vector3.forward) * Vector3.up;

            Assert.That(TopDown3DSlopeMath.IsWalkable(atLimit, 48f), Is.True);
            Assert.That(TopDown3DSlopeMath.IsWalkable(tooSteep, 48f), Is.False);
        }

        [Test]
        public void WalkableSlope_ProjectsMovementAlongSurfaceWithoutChangingMagnitude()
        {
            var normal = Quaternion.AngleAxis(35f, Vector3.forward) * Vector3.up;
            var projected = TopDown3DSlopeMath.ProjectDirectionOnSlope(
                Vector3.right,
                normal,
                48f);

            Assert.That(projected.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Vector3.Dot(projected, normal), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(projected.y, Is.GreaterThan(0f));
        }

        [Test]
        public void SteepSlope_ProducesNoUphillDrive()
        {
            var normal = Quaternion.AngleAxis(55f, Vector3.forward) * Vector3.up;

            Assert.That(
                TopDown3DSlopeMath.ProjectDirectionOnSlope(Vector3.right, normal, 48f),
                Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void SteepSlope_RemovesOnlyUphillMovement()
        {
            var normal = Quaternion.AngleAxis(55f, Vector3.forward) * Vector3.up;

            var uphill = TopDown3DSlopeMath.RemoveSteepUphillComponent(
                Vector3.right,
                normal,
                48f);
            var downhill = TopDown3DSlopeMath.RemoveSteepUphillComponent(
                Vector3.left,
                normal,
                48f);
            var lateral = TopDown3DSlopeMath.RemoveSteepUphillComponent(
                Vector3.forward,
                normal,
                48f);

            Assert.That(uphill.magnitude, Is.LessThan(0.0001f));
            Assert.That(downhill.x, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(lateral.z, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GroundNormalSmoothing_FiltersTriangleChangesWithoutFlatteningThem()
        {
            var measured = Quaternion.AngleAxis(40f, Vector3.forward) * Vector3.up;

            var smoothed = TopDown3DSlopeMath.SmoothNormal(
                Vector3.up,
                measured,
                12f,
                0.02f);
            var smoothedAngle = Vector3.Angle(Vector3.up, smoothed);

            Assert.That(smoothedAngle, Is.GreaterThan(0f));
            Assert.That(smoothedAngle, Is.LessThan(40f));
            Assert.That(smoothed.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GroundNormalSmoothing_IsStableAcrossPhysicsStepSizes()
        {
            var measured = Quaternion.AngleAxis(40f, Vector3.forward) * Vector3.up;
            var oneLargeStep = TopDown3DSlopeMath.SmoothNormal(
                Vector3.up,
                measured,
                12f,
                0.1f);
            var fiveSmallSteps = Vector3.up;
            for (var step = 0; step < 5; step++)
            {
                fiveSmallSteps = TopDown3DSlopeMath.SmoothNormal(
                    fiveSmallSteps,
                    measured,
                    12f,
                    0.02f);
            }

            Assert.That(
                Vector3.Angle(oneLargeStep, fiveSmallSteps),
                Is.LessThan(0.001f));
        }
    }
}
