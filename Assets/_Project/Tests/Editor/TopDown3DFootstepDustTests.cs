using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DFootstepDustTests
    {
        [Test]
        public void FootContact_ClampsImpactAndPreservesCanonicalContactData()
        {
            var contact = new TopDown3DFootContact(
                TopDown3DFootSide.Left,
                new Vector3(1f, 2f, 3f),
                4.2f,
                2f);

            Assert.That(contact.Side, Is.EqualTo(TopDown3DFootSide.Left));
            Assert.That(contact.WorldPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(contact.PlanarSpeed, Is.EqualTo(4.2f));
            Assert.That(contact.NormalizedImpact, Is.EqualTo(1f));
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

    }
}
