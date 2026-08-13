using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DSmartTraversalTests
    {
        [Test]
        public void NearRockSide_SelectsSpin()
        {
            var move = TopDown3DTraversalPlanner.SelectMove(1.4f, 1.15f, 0.55f, 0.8f, 0.48f);

            Assert.That(move, Is.EqualTo(TopDown3DTraversalMove.Spin));
        }

        [Test]
        public void CenteredLowRock_SelectsVault()
        {
            var move = TopDown3DTraversalPlanner.SelectMove(0.9f, 1.15f, 0.1f, 0.8f, 0.48f);

            Assert.That(move, Is.EqualTo(TopDown3DTraversalMove.Vault));
        }

        [Test]
        public void CenteredTallRock_RemainsBlocking()
        {
            var move = TopDown3DTraversalPlanner.SelectMove(1.4f, 1.15f, 0.1f, 0.8f, 0.48f);

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
    }
}
