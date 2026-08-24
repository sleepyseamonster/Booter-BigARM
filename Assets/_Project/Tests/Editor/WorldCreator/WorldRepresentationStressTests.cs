using BooterBigArm.Editor.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldRepresentationStressTests
    {
        [Test]
        public void StressTransectStaysInsideProvisionalBudgetsAndStabilizesPoolAllocations()
        {
            var proof = WorldRepresentationStressTransect.RunProof(9);
            Assert.That(proof.Text, Does.Contain("NON-CANON WORLD CREATOR BATCH 5"));
            Assert.That(proof.Text, Does.Contain("hardware-specific frame and memory acceptance remains"));
            Assert.That(proof.AllocationsAfterSecondPass, Is.EqualTo(proof.AllocationsAfterFirstPass));
            Assert.That(proof.Reuses, Is.GreaterThan(0));
            Assert.That(proof.OutstandingAfterDispose, Is.Zero);
            Assert.That(proof.PeakCacheBytes, Is.LessThanOrEqualTo(512 * 1024L));
            Assert.That(proof.Metrics.FailedBuilds, Is.Zero);
            Assert.That(proof.Metrics.CacheRejected, Is.Zero);
            Assert.That(proof.Metrics.PeakQueued, Is.LessThanOrEqualTo(1));
        }
    }
}
