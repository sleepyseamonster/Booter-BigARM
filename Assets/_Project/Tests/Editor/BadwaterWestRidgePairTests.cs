using System;
using System.IO;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Tests
{
    /// <summary>Read-only asset contracts and focused native candidate verification; no gameplay or saved asset mutations.</summary>
    public sealed class BadwaterWestRidgePairTests
    {
        [Test]
        public void SavedCandidatePreservesRetainedReadbackAndHasCompleteNativeTopology()
        {
            var scene = EditorSceneManager.OpenScene(BadwaterWestRidgePairSource.CandidateScene);
            var report = BadwaterWestRidgePairValidator.ValidateScene(scene, true);
            Assert.That(report.terrains, Is.EqualTo(3072));
            Assert.That(report.retained, Is.EqualTo(2560));
            Assert.That(report.new_terrains, Is.EqualTo(512));
            Assert.That(report.edges, Is.EqualTo(6016));
            Assert.That(report.outer_edges, Is.EqualTo(256));
            Assert.That(report.collider_samples, Is.EqualTo(76800));
        }

        [TestCase("omit")]
        [TestCase("duplicate")]
        [TestCase("unowned")]
        [TestCase("hash")]
        public void InMemoryProtectionReceiptCannotOmitOrReplaceAcceptedFiles(string mutation)
        {
            var manifest = BadwaterWestRidgePairSource.ReadManifest();
            // Modify only the deserialized receipt. Accepted manifests and payloads stay untouched.
            if (mutation == "omit")
                Array.Resize(ref manifest.protected_files, manifest.protected_files.Length - 1);
            else if (mutation == "duplicate")
                manifest.protected_files[1] = manifest.protected_files[0];
            else if (mutation == "unowned")
                manifest.protected_files[0].path = BadwaterWestRidgePairSource.ManifestPath;
            else
                manifest.protected_files[0].sha256 = new string('0', 64);
            Assert.Throws<InvalidDataException>(() => BadwaterWestRidgePairSource.VerifyProtectedFiles(manifest));
        }

        [TestCase("../../../../Packages/manifest.json")]
        [TestCase("../manifest.json")]
        [TestCase("D:/outside-owner.bytes")]
        [TestCase("")]
        public void SourceOwnershipRejectsEscapingOrUnspecifiedPaths(string path)
        {
            Assert.Throws<InvalidDataException>(() => BadwaterWestRidgePairSource.SourceFile(path));
        }

        [Test]
        public void ChangedSourceHashIsRejectedBeforeNativeHeightsAreRead()
        {
            var manifest = BadwaterWestRidgePairSource.ReadManifest();
            var tile = manifest.new_tiles[0];
            tile.render_sha256 = new string('0', 64);
            Assert.Throws<InvalidDataException>(() => BadwaterWestRidgePairSource.ReadNormalized(tile));
        }

        [Test]
        public void EarlierManifestFootprintsRemainStrictAndUnmodified()
        {
            string expansionPath = BadwaterTerrainExpansionSource.ProjectFile(BadwaterTerrainExpansionSource.ManifestPath);
            string pairPath = BadwaterWestPairSource.ProjectFile(BadwaterWestPairSource.ManifestPath);
            string nextPath = BadwaterNextWestPairSource.ProjectFile(BadwaterNextWestPairSource.ManifestPath);
            string farPath = BadwaterFarWestPairSource.ProjectFile(BadwaterFarWestPairSource.ManifestPath);
            string expansionHash = BadwaterTerrainExpansionSource.Hash(expansionPath);
            string pairHash = BadwaterWestPairSource.Hash(pairPath);
            string nextHash = BadwaterNextWestPairSource.Hash(nextPath);
            string farHash = BadwaterFarWestPairSource.Hash(farPath);
            Assert.That(BadwaterTerrainExpansionSource.ReadManifest().tile_count, Is.EqualTo(1024));
            Assert.That(BadwaterWestPairSource.ReadManifest().tile_count, Is.EqualTo(1536));
            Assert.That(BadwaterNextWestPairSource.ReadManifest().tile_count, Is.EqualTo(2048));
            Assert.That(BadwaterFarWestPairSource.ReadManifest().tile_count, Is.EqualTo(2560));
            Assert.That(BadwaterWestRidgePairSource.ReadManifest().tile_count, Is.EqualTo(3072));
            Assert.That(BadwaterTerrainExpansionSource.Hash(expansionPath), Is.EqualTo(expansionHash));
            Assert.That(BadwaterWestPairSource.Hash(pairPath), Is.EqualTo(pairHash));
            Assert.That(BadwaterNextWestPairSource.Hash(nextPath), Is.EqualTo(nextHash));
            Assert.That(BadwaterFarWestPairSource.Hash(farPath), Is.EqualTo(farHash));
        }

        [TestCase("missing")]
        [TestCase("duplicate")]
        [TestCase("shifted")]
        [TestCase("wrong_footprint")]
        public void DeveloperMapRejectsIncompleteOrMisplacedNextPair(string mutation)
        {
            var grid = RectangularGrid();
            Assert.That(DeathValleyMapData.GetPlayableBounds(grid), Is.EqualTo(new double[] {499920, 4006200, 524496, 4014392}));
            if (mutation == "missing") Array.Resize(ref grid, grid.Length - 1);
            else if (mutation == "duplicate") grid[1] = grid[0];
            else if (mutation == "shifted") grid[0].bounds_m[0] += 1;
            else foreach (var tile in grid) { tile.bounds_m[0] += 256; tile.bounds_m[2] += 256; }
            Assert.Throws<InvalidDataException>(() => DeathValleyMapData.GetPlayableBounds(grid));
        }

        private static DeathValleyMapRecord[] RectangularGrid()
        {
            var grid = new DeathValleyMapRecord[3072];
            for (int row = 0; row < 32; row++) for (int col = 0; col < 96; col++)
            {
                int east = 499920 + col * 256, north = 4006200 + row * 256;
                grid[row * 96 + col] = new DeathValleyMapRecord
                {
                    geographic_key = $"epsg26911/e{east}/n{north}/size256",
                    bounds_m = new double[] {east, north, east + 256, north + 256}
                };
            }
            return grid;
        }
    }
}
