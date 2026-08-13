using BooterBigArm.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class IsometricMovementBasisTests
    {
        [Test]
        public void ZeroInput_ReturnsZero()
        {
            var result = IsometricMovementBasis.ToWorldDirection(
                Vector2.zero,
                new Vector3(1f, -1f, 1f),
                new Vector3(1f, 0f, -1f));

            Assert.That(result, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ForwardInput_UsesCameraForwardProjectedOntoTraversalPlane()
        {
            var cameraForward = new Vector3(1f, -1f, 1f).normalized;
            var result = IsometricMovementBasis.ToWorldDirection(
                Vector2.up,
                cameraForward,
                new Vector3(1f, 0f, -1f));

            var expected = new Vector3(1f, 0f, 1f).normalized;
            Assert.That(Vector3.Distance(result, expected), Is.LessThan(0.0001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void FullDiagonalInput_IsClampedToUnitMagnitude()
        {
            var result = IsometricMovementBasis.ToWorldDirection(
                new Vector2(1f, 1f),
                new Vector3(1f, -1f, 1f),
                new Vector3(1f, 0f, -1f));

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void VerticalCameraBasis_FallsBackWithoutProducingInvalidValues()
        {
            var result = IsometricMovementBasis.ToWorldDirection(
                Vector2.up,
                Vector3.down,
                Vector3.right);

            Assert.That(result, Is.EqualTo(Vector3.forward));
            Assert.That(float.IsNaN(result.x) || float.IsNaN(result.y) || float.IsNaN(result.z), Is.False);
        }
    }
}
