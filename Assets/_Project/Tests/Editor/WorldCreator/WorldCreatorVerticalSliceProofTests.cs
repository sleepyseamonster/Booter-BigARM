using BooterBigArm.Editor.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldCreatorVerticalSliceProofTests
    {
        [Test]
        public void ProductionVerticalSlice_ExecutesNineDeclaredTransectsWithoutHardViolations()
        {
            var report = WorldCreatorVerticalSliceProof.BuildReport();

            Assert.That(report, Does.Contain("transects=9"));
            Assert.That(report, Does.Contain("uniqueArrangementSignatures=9"));
            Assert.That(report, Does.Contain("uniqueSeedSources=3"));
            Assert.That(report, Does.Contain("hardViolations=0"));
            Assert.That(report, Does.Contain("Not automated:"));
        }
    }
}
