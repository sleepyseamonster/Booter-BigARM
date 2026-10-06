using System;
using System.IO;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor;

namespace BooterBigArm.Tests
{
    public sealed class BadwaterDevelopmentBuildTests
    {
        private string temporaryRoot;

        [SetUp]
        public void SetUp()
        {
            temporaryRoot = Path.Combine(Path.GetTempPath(), "BadwaterBuildTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
        }

        [TestCase(BuildTarget.StandaloneWindows64, "FreshWindows/Badwater.exe")]
        [TestCase(BuildTarget.StandaloneOSX, "Badwater.app")]
        public void SupportedTarget_AcceptsFreshOutputWithoutWriting(BuildTarget target, string suffix)
        {
            string output = Path.Combine(temporaryRoot, suffix);
            string result = BadwaterDevelopmentBuild.ValidateOutputPath(target, output, Path.Combine(temporaryRoot, "Assets"));
            Assert.That(result, Is.EqualTo(Path.GetFullPath(output)));
            Assert.That(File.Exists(output) || Directory.Exists(output), Is.False);
        }

        [Test]
        public void Windows_RejectsExistingParentToProtectSiblingBuildFiles()
        {
            string parent = Path.Combine(temporaryRoot, "ExistingWindows");
            Directory.CreateDirectory(parent);
            string sentinel = Path.Combine(parent, "UnityPlayer.dll");
            File.WriteAllText(sentinel, "preserve");
            Assert.Throws<IOException>(() => BadwaterDevelopmentBuild.ValidateOutputPath(
                BuildTarget.StandaloneWindows64, Path.Combine(parent, "Badwater.exe"), Path.Combine(temporaryRoot, "Assets")));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("preserve"));
        }

        [TestCase(BuildTarget.StandaloneWindows64, "Badwater.app")]
        [TestCase(BuildTarget.StandaloneOSX, "Badwater.exe")]
        public void Target_RejectsWrongExtension(BuildTarget target, string suffix)
        {
            Assert.Throws<ArgumentException>(() => BadwaterDevelopmentBuild.ValidateOutputPath(
                target, Path.Combine(temporaryRoot, suffix), Path.Combine(temporaryRoot, "Assets")));
        }

        [Test]
        public void Output_RejectsAssetsAndNormalizedTraversalIntoAssets()
        {
            string assets = Path.Combine(temporaryRoot, "Assets");
            Assert.Throws<IOException>(() => BadwaterDevelopmentBuild.ValidateOutputPath(
                BuildTarget.StandaloneWindows64, Path.Combine(assets, "Build", "Badwater.exe"), assets));
            Assert.Throws<IOException>(() => BadwaterDevelopmentBuild.ValidateOutputPath(
                BuildTarget.StandaloneWindows64, Path.Combine(temporaryRoot, "Build", "..", "Assets", "Build", "Badwater.exe"), assets));
        }

        [Test]
        public void Output_RejectsExistingMacAppAndUnsupportedTarget()
        {
            string app = Path.Combine(temporaryRoot, "Badwater.app");
            Directory.CreateDirectory(app);
            Assert.Throws<IOException>(() => BadwaterDevelopmentBuild.ValidateOutputPath(
                BuildTarget.StandaloneOSX, app, Path.Combine(temporaryRoot, "Assets")));
            Assert.Throws<InvalidOperationException>(() => BadwaterDevelopmentBuild.ValidateOutputPath(
                BuildTarget.Android, Path.Combine(temporaryRoot, "Badwater.apk"), Path.Combine(temporaryRoot, "Assets")));
        }
    }
}
