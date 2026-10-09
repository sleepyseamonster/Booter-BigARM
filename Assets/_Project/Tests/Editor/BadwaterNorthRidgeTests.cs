using System;
using System.IO;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Tests
{
    /// <summary>Read-only asset contracts and focused native candidate verification; no gameplay or saved asset mutations.</summary>
    public sealed class BadwaterNorthRidgeTests
    {
        [Test]
        public void SavedCandidatePreservesRetainedReadbackAndHasCompleteNativeTopology()
        {
            var scene = EditorSceneManager.OpenScene(BadwaterNorthRidgeSource.CandidateScene);
            var report = BadwaterNorthRidgeValidator.ValidateScene(scene, true);
            Assert.That(report.terrains, Is.EqualTo(3328));
            Assert.That(report.retained, Is.EqualTo(3072));
            Assert.That(report.new_terrains, Is.EqualTo(256));
            Assert.That(report.edges, Is.EqualTo(6512));
            Assert.That(report.outer_edges, Is.EqualTo(288));
            Assert.That(report.collider_samples, Is.EqualTo(83200));
        }

        [TestCase("omit")]
        [TestCase("duplicate")]
        [TestCase("unowned")]
        [TestCase("hash")]
        public void InMemoryProtectionReceiptCannotOmitOrReplaceAcceptedFiles(string mutation)
        {
            var manifest = BadwaterNorthRidgeSource.ReadManifest();
            // Modify only the deserialized receipt. Accepted manifests and payloads stay untouched.
            if (mutation == "omit")
                Array.Resize(ref manifest.protected_files, manifest.protected_files.Length - 1);
            else if (mutation == "duplicate")
                manifest.protected_files[1] = manifest.protected_files[0];
            else if (mutation == "unowned")
                manifest.protected_files[0].path = BadwaterNorthRidgeSource.ManifestPath;
            else
                manifest.protected_files[0].sha256 = new string('0', 64);
            Assert.Throws<InvalidDataException>(() => BadwaterNorthRidgeSource.VerifyProtectedFiles(manifest));
        }

        [TestCase("../../../../Packages/manifest.json")]
        [TestCase("../manifest.json")]
        [TestCase("D:/outside-owner.bytes")]
        [TestCase("")]
        public void SourceOwnershipRejectsEscapingOrUnspecifiedPaths(string path)
        {
            Assert.Throws<InvalidDataException>(() => BadwaterNorthRidgeSource.SourceFile(path));
        }

        [Test]
        public void ChangedSourceHashIsRejectedBeforeNativeHeightsAreRead()
        {
            var manifest = BadwaterNorthRidgeSource.ReadManifest();
            var tile = manifest.new_tiles[0];
            tile.render_sha256 = new string('0', 64);
            Assert.Throws<InvalidDataException>(() => BadwaterNorthRidgeSource.ReadNormalized(tile));
        }

        [Test]
        public void EarlierManifestFootprintsRemainStrictAndUnmodified()
        {
            string expansionPath = BadwaterTerrainExpansionSource.ProjectFile(BadwaterTerrainExpansionSource.ManifestPath);
            string pairPath = BadwaterWestPairSource.ProjectFile(BadwaterWestPairSource.ManifestPath);
            string nextPath = BadwaterNextWestPairSource.ProjectFile(BadwaterNextWestPairSource.ManifestPath);
            string farPath = BadwaterFarWestPairSource.ProjectFile(BadwaterFarWestPairSource.ManifestPath);
            string ridgePath = BadwaterWestRidgePairSource.ProjectFile(BadwaterWestRidgePairSource.ManifestPath);
            string expansionHash = BadwaterTerrainExpansionSource.Hash(expansionPath);
            string pairHash = BadwaterWestPairSource.Hash(pairPath);
            string nextHash = BadwaterNextWestPairSource.Hash(nextPath);
            string farHash = BadwaterFarWestPairSource.Hash(farPath);
            string ridgeHash = BadwaterWestRidgePairSource.Hash(ridgePath);
            Assert.That(BadwaterTerrainExpansionSource.ReadManifest().tile_count, Is.EqualTo(1024));
            Assert.That(BadwaterWestPairSource.ReadManifest().tile_count, Is.EqualTo(1536));
            Assert.That(BadwaterNextWestPairSource.ReadManifest().tile_count, Is.EqualTo(2048));
            Assert.That(BadwaterFarWestPairSource.ReadManifest().tile_count, Is.EqualTo(2560));
            Assert.That(BadwaterWestRidgePairSource.ReadManifest().tile_count, Is.EqualTo(3072));
            Assert.That(BadwaterNorthRidgeSource.ReadManifest().tile_count, Is.EqualTo(3328));
            Assert.That(BadwaterTerrainExpansionSource.Hash(expansionPath), Is.EqualTo(expansionHash));
            Assert.That(BadwaterWestPairSource.Hash(pairPath), Is.EqualTo(pairHash));
            Assert.That(BadwaterNextWestPairSource.Hash(nextPath), Is.EqualTo(nextHash));
            Assert.That(BadwaterFarWestPairSource.Hash(farPath), Is.EqualTo(farHash));
            Assert.That(BadwaterWestRidgePairSource.Hash(ridgePath), Is.EqualTo(ridgeHash));
        }

        [TestCase("missing")]
        [TestCase("duplicate")]
        [TestCase("shifted")]
        [TestCase("wrong_footprint")]
        [TestCase("hole_replaced_in_aabb")]
        [TestCase("shifted_cap")]
        public void DeveloperMapRejectsIncompleteOrMisplacedNextPair(string mutation)
        {
            var grid = NorthernCapGrid();
            Assert.That(DeathValleyMapData.GetPlayableBounds(grid), Is.EqualTo(new double[] {499920, 4006200, 524496, 4018488}));
            if (mutation == "missing") Array.Resize(ref grid, grid.Length - 1);
            else if (mutation == "duplicate") grid[1] = grid[0];
            else if (mutation == "shifted") grid[0].bounds_m[0] += 1;
            else if (mutation == "hole_replaced_in_aabb")
                grid[10].bounds_m = new double[] {508112, 4014648, 508368, 4014904};
            else if (mutation == "shifted_cap")
                for (int i = 3072; i < grid.Length; i++) { grid[i].bounds_m[0] += 256; grid[i].bounds_m[2] += 256; }
            else foreach (var tile in grid) { tile.bounds_m[0] += 256; tile.bounds_m[2] += 256; }
            Assert.Throws<InvalidDataException>(() => DeathValleyMapData.GetPlayableBounds(grid));
        }

        [TestCase("count")]
        [TestCase("section")]
        [TestCase("bounds")]
        [TestCase("duplicate")]
        [TestCase("retained_bounds")]
        public void DeserializedManifestRejectsMalformedNorthernCapWithoutWritingAssets(string mutation)
        {
            var manifest = BadwaterNorthRidgeSource.ReadManifest();
            if (mutation == "count") manifest.new_tile_count = 512;
            else if (mutation == "section") manifest.new_tiles[0].section = "northwest_ridge";
            else if (mutation == "bounds") manifest.new_tiles[0].bounds_m[1] -= 256;
            else if (mutation == "duplicate") manifest.new_tiles[1] = manifest.new_tiles[0];
            else manifest.retained_tiles[0].bounds_m[0] -= 256;
            Assert.Throws<InvalidDataException>(() => BadwaterNorthRidgeSource.ValidateManifest(manifest));
        }

        private static DeathValleyMapRecord[] NorthernCapGrid()
        {
            var grid = new DeathValleyMapRecord[3328];
            int index = 0;
            for (int row = 0; row < 48; row++) for (int col = 0; col < (row < 32 ? 96 : 16); col++)
            {
                int east = 499920 + col * 256, north = 4006200 + row * 256;
                grid[index++] = new DeathValleyMapRecord
                {
                    geographic_key = $"epsg26911/e{east}/n{north}/size256",
                    bounds_m = new double[] {east, north, east + 256, north + 256}
                };
            }
            return grid;
        }
    }
}
