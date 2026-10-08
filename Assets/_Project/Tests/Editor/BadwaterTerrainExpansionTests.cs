using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class BadwaterTerrainExpansionTests
    {
        private byte[] original;
        private string path;
        [SetUp] public void SaveManifest()
        {
            path = global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.ProjectFile(global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.ManifestPath);
            original = File.ReadAllBytes(path);
        }
        [TearDown] public void RestoreManifest() => File.WriteAllBytes(path, original);

        [Test] public void Manifest_VerifiesAllSourcesAndGeographicKeys()
        {
            var m = global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.ReadManifest(true);
            Assert.That(m.new_tiles.Length, Is.EqualTo(768));
            Assert.That(m.retained_tiles.Length, Is.EqualTo(256));
            global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.VerifyProtectedFiles(m);
        }

        [TestCase("origin")]
        [TestCase("duplicate")]
        [TestCase("bounds")]
        [TestCase("range")]
        [TestCase("section")]
        public void Manifest_RejectsShiftedDuplicatedOrUnsupportedContracts(string mutation)
        {
            var m = global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.ReadManifest();
            if (mutation == "origin") m.unity_origin_m[0] += 256;
            if (mutation == "duplicate") m.new_tiles[1] = m.new_tiles[0];
            if (mutation == "bounds") m.new_tiles[0].bounds_m[0] += 1;
            if (mutation == "range") m.range_m = 1900;
            if (mutation == "section") m.new_tiles[0].section = "east";
            File.WriteAllText(path, JsonUtility.ToJson(m));
            Assert.Throws<InvalidDataException>(() => global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.ReadManifest());
        }

        [Test] public void SourcePaths_RejectTraversalOutsideOwnedFolder()
        {
            Assert.Throws<InvalidDataException>(() => global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.SourceFile("../../../../Packages/manifest.json"));
            Assert.Throws<InvalidDataException>(() => global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.ProjectFile("../outside.json"));
        }

        [Test] public void RetainedReadback_DecodesSouthPositiveZWithoutInheritingFocusRules()
        {
            var m = global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.ReadManifest();
            var t = m.retained_tiles[0];
            byte[] bytes = File.ReadAllBytes(global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.SourceFile(t.render_file));
            var h = global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.ReadNormalized(t);
            Assert.That(h[256, 0], Is.EqualTo(BitConverter.ToSingle(bytes, 0)));
            Assert.That(h[0, 256], Is.EqualTo(BitConverter.ToSingle(bytes, bytes.Length - 4)));
        }

        [Test] public void SavedCandidate_ReloadRestoresFullGridWithoutManualNeighborRepair()
        {
            var scene = EditorSceneManager.OpenScene(global::BooterBigArm.Editor.BadwaterTerrainExpansionSource.CandidateScene);
            var report = global::BooterBigArm.Editor.BadwaterTerrainExpansionValidator.ValidateScene(scene, true);
            Assert.That(report.edges, Is.EqualTo(1984));
            Assert.That(report.outer_edges, Is.EqualTo(128));
            Assert.That(report.collider_samples, Is.EqualTo(25600));
        }
    }
}
