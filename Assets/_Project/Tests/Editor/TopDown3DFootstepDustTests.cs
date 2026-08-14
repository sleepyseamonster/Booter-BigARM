using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DFootstepDustTests
    {
        [Test]
        public void DistanceCadence_EmitsOnlyAfterCrossingAStepBoundary()
        {
            var accumulated = 0f;

            Assert.That(
                TopDown3DFootstepDust.ConsumeStepDistance(ref accumulated, 0.7f, 1.2f),
                Is.Zero);
            Assert.That(accumulated, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(
                TopDown3DFootstepDust.ConsumeStepDistance(ref accumulated, 0.65f, 1.2f),
                Is.EqualTo(1));
            Assert.That(accumulated, Is.EqualTo(0.15f).Within(0.0001f));
        }

        [Test]
        public void DistanceCadence_CapsLowFrameRateBurstsAndRetainsSafeRemainder()
        {
            var accumulated = 0f;

            var steps = TopDown3DFootstepDust.ConsumeStepDistance(
                ref accumulated,
                8.4f,
                1.2f,
                TopDown3DFootstepDust.MaximumStepsPerFrame);

            Assert.That(steps, Is.EqualTo(TopDown3DFootstepDust.MaximumStepsPerFrame));
            Assert.That(accumulated, Is.GreaterThanOrEqualTo(0f));
            Assert.That(accumulated, Is.LessThan(1.2f));
        }

        [Test]
        public void Sprinting_IncreasesCadenceWithoutUsingAnimationEvents()
        {
            const float walkSpeed = 4.2f;
            const float sprintSpeed = 7.4f;
            var walkStride = TopDown3DFootstepDust.EvaluateStepDistance(walkSpeed, false);
            var sprintStride = TopDown3DFootstepDust.EvaluateStepDistance(sprintSpeed, true);

            Assert.That(walkStride, Is.GreaterThan(0f));
            Assert.That(sprintStride, Is.GreaterThan(walkStride));
            Assert.That(sprintSpeed / sprintStride, Is.GreaterThan(walkSpeed / walkStride));
        }

        [Test]
        public void BurstCount_RespondsToSpeedAndRegionalDustWithoutExceedingBudget()
        {
            var clearWalk = TopDown3DFootstepDust.EvaluateBurstCount(4.2f, 0f);
            var dustySprint = TopDown3DFootstepDust.EvaluateBurstCount(
                7.4f,
                TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity);

            Assert.That(clearWalk, Is.InRange(
                TopDown3DFootstepDust.DefaultMinimumParticlesPerStep,
                TopDown3DFootstepDust.DefaultMaximumParticlesPerStep));
            Assert.That(
                dustySprint,
                Is.EqualTo(TopDown3DFootstepDust.DefaultMaximumParticlesPerStep));
            Assert.That(dustySprint, Is.GreaterThan(clearWalk));
        }

        [Test]
        public void FootstepDust_RemainsStrongInClearAirAndStillIncreasesInsidePockets()
        {
            var clearStrength = TopDown3DFootstepDust.EvaluateDustStrength(0f);
            var pocketStrength = TopDown3DFootstepDust.EvaluateDustStrength(
                TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity);

            Assert.That(clearStrength, Is.EqualTo(
                TopDown3DFootstepDust.DefaultClearAirDustStrength).Within(0.0001f));
            Assert.That(clearStrength, Is.GreaterThanOrEqualTo(0.8f));
            Assert.That(pocketStrength, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(pocketStrength, Is.GreaterThan(clearStrength));
        }

        [Test]
        public void Component_RequiresTheCanonicalPlayerMotorAndCapsule()
        {
            var player = new GameObject("Footstep Dust Test Player");
            try
            {
                player.AddComponent<TopDown3DFootstepDust>();

                Assert.That(player.GetComponent<TopDown3DPlayerMotor>(), Is.Not.Null);
                Assert.That(player.GetComponent<CapsuleCollider>(), Is.Not.Null);
                Assert.That(player.GetComponents<TopDown3DFootstepDust>(), Has.Length.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
