using BooterBigArm.Editor.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class HybridTerrainPanelTests
    {
        [Test]
        public void ComparisonPanelAndReportStayExplicitlyNonCanonAndSampled()
        {
            var svg = HybridTerrainComparisonPanelExporter.BuildProofPanelSvg(16);
            var report = HybridTerrainComparisonPanelExporter.BuildProofReport();
            Assert.That(svg, Does.Contain("NON-CANON WORLD CREATOR BATCH 4"));
            Assert.That(svg, Does.Contain("synthetic history"));
            Assert.That(svg, Does.Contain("semantic query"));
            Assert.That(svg, Does.Contain("<rect"));
            Assert.That(report, Does.Contain("production terrain"));
            Assert.That(report, Does.Contain("persisted deltas: not applicable"));
            Assert.That(report, Does.Contain("boundedLandformRequests="));
        }
    }
}
