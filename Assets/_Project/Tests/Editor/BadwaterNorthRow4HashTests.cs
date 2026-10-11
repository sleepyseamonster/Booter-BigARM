using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using BooterBigArm.Editor;
using NUnit.Framework;

namespace BooterBigArm.Tests
{
    public sealed class BadwaterNorthRow4HashTests
    {
        [TestCase("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [TestCase("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
        public void KnownSha256VectorsMatch(string value, string expected)
        {
            string file = Path.GetTempFileName();
            try { File.WriteAllBytes(file, Encoding.UTF8.GetBytes(value)); Assert.That(BadwaterNorthRow4FileHash.Compute(file), Is.EqualTo(expected)); }
            finally { File.Delete(file); }
        }

        [TestCase(1)] [TestCase(65537)] [TestCase(131072)] [TestCase(524291)]
        public void StreamBoundariesMatchIndependentManagedShaAndDetectMutation(int count)
        {
            string file = Path.GetTempFileName();
            try
            {
                var bytes = new byte[count]; new Random(26911).NextBytes(bytes); File.WriteAllBytes(file, bytes);
                string expected = BadwaterTerrainExpansionSource.Hash(file);
                Assert.That(BadwaterNorthRow4FileHash.Compute(file), Is.EqualTo(expected));
                bytes[count - 1] ^= 1; File.WriteAllBytes(file, bytes);
                Assert.That(BadwaterNorthRow4FileHash.Compute(file), Is.EqualTo(BadwaterTerrainExpansionSource.Hash(file)).And.Not.EqualTo(expected));
            }
            finally { File.Delete(file); }
        }

        [Test] public void AcceptedSourceManifestMatchesLegacyProviderWithMeasuredTiming()
        {
            var config = BadwaterNorthRow4Source.Selected;
            string path = BadwaterNorthRow4Source.ProjectFile(config.ManifestPath);
            var timer = Stopwatch.StartNew();
            string expected = BadwaterTerrainExpansionSource.Hash(path);
            long legacyMs = timer.ElapsedMilliseconds;
            timer.Restart();
            string actual = BadwaterNorthRow4FileHash.Compute(path);
            long nativeMs = timer.ElapsedMilliseconds;
            Assert.That(actual, Is.EqualTo(expected));
            UnityEngine.Debug.Log($"ROW4_SHA256_BENCHMARK: {new FileInfo(path).Length} bytes, legacy={legacyMs}ms, row4={nativeMs}ms; exact digest agreement.");
        }
    }
}
