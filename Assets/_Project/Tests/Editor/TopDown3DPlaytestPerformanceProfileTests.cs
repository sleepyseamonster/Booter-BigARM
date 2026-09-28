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
        public void FullContentProfile_IsTheDefaultWhenStressIsNotRequested()
        {
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.ShouldUseFullContentProfile(null, false),
                Is.True);
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.ShouldUseFullContentProfile(new string[0], false),
                Is.True);
            Assert.That(
                TopDown3DPlaytestPerformanceProfile.ShouldUseFullContentProfile(
                    new[] { "BooterBigArm", "-batchmode" }, false),
                Is.True);
        }

        [Test]
        public void ReducedStressProfile_RequiresAnExplicitFlagOrEditorSelection()
        {
            Assert.That(TopDown3DPlaytestPerformanceProfile.ShouldUseFullContentProfile(
                new[] { "BooterBigArm", TopDown3DPlaytestPerformanceProfile.StressProfileArgument }, false),
                Is.False);
            Assert.That(TopDown3DPlaytestPerformanceProfile.ShouldUseFullContentProfile(
                new[] { "BooterBigArm" }, true), Is.False);
            Assert.That(TopDown3DPlaytestPerformanceProfile.IsStressProfileRequested(
                new[] { "BooterBigArm", "-topDown3DStressProfileExtra" }), Is.False);
            Assert.That(TopDown3DPlaytestPerformanceProfile.ShouldUseFullContentProfile(
                new[] { TopDown3DPlaytestPerformanceProfile.FullContentProfileArgument,
                    TopDown3DPlaytestPerformanceProfile.StressProfileArgument }, true), Is.True);
        }
    }
}
