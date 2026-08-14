using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DSmartTraversalTests
    {
        [Test]
        public void NearRightRockSide_SelectsSideStep()
        {
            var move = TopDown3DTraversalPlanner.SelectMove(1.1f, 0.8f, 0.64f, 0.8f, 0.76f);

            Assert.That(move, Is.EqualTo(TopDown3DTraversalMove.SideStep));
        }

        [Test]
        public void NearLeftRockSide_SelectsSideStep()
        {
            var move = TopDown3DTraversalPlanner.SelectMove(1.1f, 0.8f, -0.64f, 0.8f, 0.76f);

            Assert.That(move, Is.EqualTo(TopDown3DTraversalMove.SideStep));
        }

        [Test]
        public void CenteredLowRock_SelectsVault()
        {
            var move = TopDown3DTraversalPlanner.SelectMove(0.7f, 0.8f, 0.1f, 0.8f, 0.76f);

            Assert.That(move, Is.EqualTo(TopDown3DTraversalMove.Vault));
        }

        [Test]
        public void CenteredTallRock_RemainsBlocking()
        {
            var move = TopDown3DTraversalPlanner.SelectMove(1.1f, 0.8f, 0.1f, 0.8f, 0.76f);

            Assert.That(move, Is.EqualTo(TopDown3DTraversalMove.None));
        }

        [Test]
        public void TallRockAwayFromSideBand_RemainsBlocking()
        {
            var move = TopDown3DTraversalPlanner.SelectMove(1.1f, 0.8f, 0.5f, 0.8f, 0.76f);

            Assert.That(move, Is.EqualTo(TopDown3DTraversalMove.None));
        }

        [Test]
        public void VaultArc_PreservesEndpointsAndClearsApex()
        {
            var start = new Vector3(1f, 2f, 3f);
            var end = new Vector3(5f, 3f, 7f);
            var arcHeight = 1.25f;

            Assert.That(
                TopDown3DTraversalPlanner.CalculateVaultPoint(start, end, arcHeight, 0f),
                Is.EqualTo(start));
            Assert.That(
                TopDown3DTraversalPlanner.CalculateVaultPoint(start, end, arcHeight, 1f),
                Is.EqualTo(end));
            Assert.That(
                TopDown3DTraversalPlanner.CalculateVaultPoint(start, end, arcHeight, 0.5f).y,
                Is.EqualTo(3.75f).Within(0.0001f));
        }

        [Test]
        public void TraversalDuration_NeverRequiresMoreThanConfiguredSpeed()
        {
            Assert.That(
                TopDown3DTraversalPlanner.CalculateSpeedLimitedDuration(2.1f, 4.2f, 0.2f),
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(
                TopDown3DTraversalPlanner.CalculateSpeedLimitedDuration(1f, 4.2f, 0.68f),
                Is.EqualTo(0.68f).Within(0.0001f));
        }
    }
}
