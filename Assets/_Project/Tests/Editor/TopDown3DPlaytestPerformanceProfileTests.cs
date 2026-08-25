using BooterBigArm.TopDown3D;
using NUnit.Framework;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DPlaytestPerformanceProfileTests
    {
        [Test]
        public void FullContentProfile_RequiresTheExactCommandLineFlag()
        {
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.IsFullContentProfileRequested(
                    new[] { "BooterBigArm", TopDown3DPlaytestPerformanceProfile.FullContentProfileArgument }),
                Is.True);
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.IsFullContentProfileRequested(
                    new[] { "BooterBigArm", "-topDown3DFullContentProfileExtra" }),
                Is.False);
        }

        [Test]
        public void FullContentProfile_FlagMatchingIsCaseInsensitive()
        {
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.IsFullContentProfileRequested(
                    new[] { "-TOPDOWN3DFULLCONTENTPROFILE" }),
                Is.True);
        }

        [Test]
        public void FullContentProfile_DefaultsToStressModeWhenFlagIsAbsent()
        {
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.IsFullContentProfileRequested(null),
                Is.False);
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.IsFullContentProfileRequested(new string[0]),
                Is.False);
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.IsFullContentProfileRequested(
                    new[] { "BooterBigArm", "-batchmode" }),
                Is.False);
        }
    }
}
