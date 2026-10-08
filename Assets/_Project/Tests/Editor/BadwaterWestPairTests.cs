using System;
using System.IO;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class BadwaterWestPairTests
    {
        private byte[] original;
        private string path;
        [SetUp] public void Capture()
        {
            path = BadwaterWestPairSource.ProjectFile(BadwaterWestPairSource.ManifestPath);
            original = File.ReadAllBytes(path);
        }
        [TearDown] public void Restore() => File.WriteAllBytes(path, original);

        [Test] public void SourcesAndCompleteRetainedBaselineRemainVerified()
        {
            var m = BadwaterWestPairSource.ReadManifest(true);
            BadwaterWestPairSource.VerifyProtectedFiles(m);
            Assert.That(m.new_tiles.Length, Is.EqualTo(512));
            Assert.That(m.retained_tiles.Length, Is.EqualTo(1024));
        }

        [TestCase("origin")]
        [TestCase("duplicate")]
        [TestCase("bounds")]
        [TestCase("retained")]
        [TestCase("escape")]
        public void UnsupportedOrShiftedContractsAreRejected(string mutation)
        {
            var m = BadwaterWestPairSource.ReadManifest();
            if (mutation == "origin") m.unity_origin_m[0] += 256;
            if (mutation == "duplicate") m.new_tiles[1] = m.new_tiles[0];
            if (mutation == "bounds") m.new_tiles[0].bounds_m[0] += 1;
            if (mutation == "retained") m.retained_tiles[0].position[0] -= 1;
            if (mutation == "escape") m.new_tiles[0].render_file = "../../../../Packages/manifest.json";
            File.WriteAllText(path, JsonUtility.ToJson(m));
            Assert.Throws<InvalidDataException>(() => BadwaterWestPairSource.ReadManifest(true));
        }

        [Test] public void SavedCandidateRestoresAllSeamsNeighborsAndColliders()
        {
            var scene = EditorSceneManager.OpenScene(BadwaterWestPairSource.CandidateScene);
            var report = BadwaterWestPairValidator.ValidateScene(scene, true);
            Assert.That(report.retained, Is.EqualTo(1024));
            Assert.That(report.edges, Is.EqualTo(2992));
            Assert.That(report.outer_edges, Is.EqualTo(160));
            Assert.That(report.collider_samples, Is.EqualTo(38400));
        }

        [Test] public void DifferentRetainedTerrainPayloadCannotMasqueradeAsTheProtectedBaseline()
        {
            string root=Path.GetDirectoryName(Application.dataPath);
            if (Directory.Exists(Path.Combine(root,".git"))) Assert.Ignore("Native serialization swap is restricted to the isolated validation copy.");
            const string asset="Assets/_Project/Art/Terrain/WestNorthNorthwest2026-10-07/TerrainData/north/r12_c05.asset";
            string file=BadwaterWestPairSource.ProjectFile(asset);
            byte[] saved=File.ReadAllBytes(file);
            var m=BadwaterWestPairSource.ReadManifest(true);
            var tile=Array.Find(m.retained_tiles,t=>t.data_path==asset);
            var expected=BadwaterWestPairSource.ReadNormalized(tile);
            try
            {
                foreach (string variant in new[] {"before","current"})
                {
                    File.WriteAllBytes(file,File.ReadAllBytes(BadwaterWestPairSource.SourceFile("SerializationVariants/north_r12_c05_"+variant+".bytes")));
                    UnityEditor.AssetDatabase.ImportAsset(asset,UnityEditor.ImportAssetOptions.ForceUpdate);
                    var data=UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>(asset);
                    var actual=data.GetHeights(0,0,257,257);
                    bool canonical=BadwaterWestPairSource.Hash(file)==tile.data_sha256;
                    Assert.That(BadwaterWestPairSource.HashMatches(asset,tile.data_sha256),Is.EqualTo(canonical));
                    if (canonical)
                        for(int r=0;r<257;r++) for(int c=0;c<257;c++) Assert.That(actual[r,c],Is.EqualTo(expected[r,c]));
                    else
                        Assert.Throws<InvalidDataException>(()=>BadwaterWestPairSource.VerifyProtectedFiles(m));
                }
            }
            finally
            {
                File.WriteAllBytes(file,saved);
                UnityEditor.AssetDatabase.ImportAsset(asset,UnityEditor.ImportAssetOptions.ForceUpdate);
            }
        }

        [Test] public void RectangularMapUsesAllSixSectionsAndRejectsMissingOrShiftedCells()
        {
            var grid = new DeathValleyMapRecord[1536];
            for (int r=0; r<32; r++) for (int c=0; c<48; c++)
            {
                int e=512208+c*256, n=4006200+r*256;
                grid[r*48+c]=new DeathValleyMapRecord { geographic_key=$"epsg26911/e{e}/n{n}/size256", bounds_m=new double[] {e,n,e+256,n+256} };
            }
            Assert.That(DeathValleyMapData.GetPlayableBounds(grid), Is.EqualTo(new double[] {512208,4006200,524496,4014392}));
            var copy=(DeathValleyMapRecord[])grid.Clone(); copy[1]=copy[0];
            Assert.Throws<InvalidDataException>(() => DeathValleyMapData.GetPlayableBounds(copy));
            grid[0].bounds_m[0]+=1;
            Assert.Throws<InvalidDataException>(() => DeathValleyMapData.GetPlayableBounds(grid));
        }
    }
}
