using System;
using System.IO;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Tests
{
    /// <summary>Read-only asset contracts and focused native candidate verification; no gameplay or saved asset mutations.</summary>
    public sealed class BadwaterNextWestPairTests
    {
        [Test]
        public void SavedCandidatePreservesRetainedReadbackAndHasCompleteNativeTopology()
        {
            var scene = EditorSceneManager.OpenScene(BadwaterNextWestPairSource.CandidateScene);
            var report = BadwaterNextWestPairValidator.ValidateScene(scene, true);
            Assert.That(report.terrains, Is.EqualTo(2048));
            Assert.That(report.retained, Is.EqualTo(1536));
            Assert.That(report.new_terrains, Is.EqualTo(512));
            Assert.That(report.edges, Is.EqualTo(4000));
            Assert.That(report.outer_edges, Is.EqualTo(192));
            Assert.That(report.collider_samples, Is.EqualTo(51200));
        }

        [TestCase("omit")]
        [TestCase("duplicate")]
        [TestCase("unowned")]
        [TestCase("hash")]
        public void InMemoryProtectionReceiptCannotOmitOrReplaceAcceptedFiles(string mutation)
        {
            var manifest = BadwaterNextWestPairSource.ReadManifest();
            // Modify only the deserialized receipt. Accepted manifests and payloads stay untouched.
            if (mutation == "omit")
                Array.Resize(ref manifest.protected_files, manifest.protected_files.Length - 1);
            else if (mutation == "duplicate")
                manifest.protected_files[1] = manifest.protected_files[0];
            else if (mutation == "unowned")
                manifest.protected_files[0].path = BadwaterNextWestPairSource.ManifestPath;
            else
                manifest.protected_files[0].sha256 = new string('0', 64);
            Assert.Throws<InvalidDataException>(() => BadwaterNextWestPairSource.VerifyProtectedFiles(manifest));
        }

        [TestCase("../../../../Packages/manifest.json")]
        [TestCase("../manifest.json")]
        [TestCase("D:/outside-owner.bytes")]
        [TestCase("")]
        public void SourceOwnershipRejectsEscapingOrUnspecifiedPaths(string path)
        {
            Assert.Throws<InvalidDataException>(() => BadwaterNextWestPairSource.SourceFile(path));
        }

        [Test]
        public void ChangedSourceHashIsRejectedBeforeNativeHeightsAreRead()
        {
            var manifest = BadwaterNextWestPairSource.ReadManifest();
            var tile = manifest.new_tiles[0];
            tile.render_sha256 = new string('0', 64);
            Assert.Throws<InvalidDataException>(() => BadwaterNextWestPairSource.ReadNormalized(tile));
        }

        [Test]
        public void EarlierManifestFootprintsRemainStrictAndUnmodified()
        {
            string expansionPath = BadwaterTerrainExpansionSource.ProjectFile(BadwaterTerrainExpansionSource.ManifestPath);
            string pairPath = BadwaterWestPairSource.ProjectFile(BadwaterWestPairSource.ManifestPath);
            string expansionHash = BadwaterTerrainExpansionSource.Hash(expansionPath);
            string pairHash = BadwaterWestPairSource.Hash(pairPath);
            Assert.That(BadwaterTerrainExpansionSource.ReadManifest().tile_count, Is.EqualTo(1024));
            Assert.That(BadwaterWestPairSource.ReadManifest().tile_count, Is.EqualTo(1536));
            Assert.That(BadwaterNextWestPairSource.ReadManifest().tile_count, Is.EqualTo(2048));
            Assert.That(BadwaterTerrainExpansionSource.Hash(expansionPath), Is.EqualTo(expansionHash));
            Assert.That(BadwaterWestPairSource.Hash(pairPath), Is.EqualTo(pairHash));
        }

        [TestCase("missing")]
        [TestCase("duplicate")]
        [TestCase("shifted")]
        [TestCase("wrong_footprint")]
        public void DeveloperMapRejectsIncompleteOrMisplacedNextPair(string mutation)
        {
            var grid = RectangularGrid();
            Assert.That(DeathValleyMapData.GetPlayableBounds(grid), Is.EqualTo(new double[] {508112, 4006200, 524496, 4014392}));
            if (mutation == "missing") Array.Resize(ref grid, grid.Length - 1);
            else if (mutation == "duplicate") grid[1] = grid[0];
            else if (mutation == "shifted") grid[0].bounds_m[0] += 1;
            else foreach (var tile in grid) { tile.bounds_m[0] += 256; tile.bounds_m[2] += 256; }
            Assert.Throws<InvalidDataException>(() => DeathValleyMapData.GetPlayableBounds(grid));
        }

        private static DeathValleyMapRecord[] RectangularGrid()
        {
            var grid = new DeathValleyMapRecord[2048];
            for (int row = 0; row < 32; row++) for (int col = 0; col < 64; col++)
            {
                int east = 508112 + col * 256, north = 4006200 + row * 256;
                grid[row * 64 + col] = new DeathValleyMapRecord
                {
                    geographic_key = $"epsg26911/e{east}/n{north}/size256",
                    bounds_m = new double[] {east, north, east + 256, north + 256}
                };
            }
            return grid;
        }
    }
}
