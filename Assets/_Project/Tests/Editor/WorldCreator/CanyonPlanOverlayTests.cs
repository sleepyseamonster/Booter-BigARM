using BooterBigArm.Editor.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class CanyonPlanOverlayTests
    {
        [Test]
        public void OverlayIsDeterministicAndClearlyNonCanon()
        {
            var first = CanyonPlanOverlayExporter.BuildProofOverlaySvg();
            var second = CanyonPlanOverlayExporter.BuildProofOverlaySvg();

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.Contain("NON-CANON WORLD CREATOR BATCH 3 CANYON GRAPH OVERLAY"));
            Assert.That(first, Does.Contain("not globe, coordinate, region, terrain, or lore canon"));
            Assert.That(first, Does.Contain("Booter-only"));
            Assert.That(first, Does.Contain("BigARM-compatible"));
        }

        [Test]
        public void ReportProvesBoundedPlansAndSharedSeams()
        {
            var report = CanyonPlanOverlayExporter.BuildProofReport();

            Assert.That(report, Does.StartWith("NON-CANON WORLD CREATOR BATCH 3 CANYON GRAPH REPORT"));
            Assert.That(report, Does.Contain("plans=9"));
            Assert.That(report, Does.Contain("validatedSeams=12"));
            Assert.That(report, Does.Contain("contexts=5"));
            Assert.That(report, Does.Contain("boundaries=4"));
        }
    }
}
