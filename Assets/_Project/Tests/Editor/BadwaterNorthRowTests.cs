using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class BadwaterNorthRowTests
    {
        [TestCase(1,504016,3584,7024,89600)]
        [TestCase(2,508112,3840,7536,96000)]
        [TestCase(3,512208,4096,8048,102400)]
        [TestCase(4,516304,4352,8560,108800)]
        [TestCase(5,520400,4608,9072,115200)]
        public void OnlySealedGeographicStepsHaveTheApprovedCounts(int step,int east,int total,int edges,int samples)
        {
            var c=BadwaterNorthRowSource.ForStep(step);
            Assert.That(c.East,Is.EqualTo(east)); Assert.That(c.TotalCount,Is.EqualTo(total));
            Assert.That(c.RetainedCount,Is.EqualTo(total-256)); Assert.That(c.EdgeCount,Is.EqualTo(edges));
            Assert.That(c.ColliderSamples,Is.EqualTo(samples));
            Assert.That(c.Section,Is.EqualTo("north_row_e"+east));
            Assert.That(c.Batch,Is.EqualTo("NorthRowE"+east+"2026-10-09"));
        }

        [TestCase(0)] [TestCase(6)] [TestCase(-1)]
        public void UnplannedStepsAreRejected(int step) => Assert.Throws<InvalidDataException>(()=>BadwaterNorthRowSource.ForStep(step));

        [Test] public void BatchCannotSelectALaterStepOrDifferentDate()
        {
            Assert.Throws<InvalidDataException>(()=>BadwaterNorthRowSource.ForStep(1,"NorthRowE5081122026-10-09"));
            Assert.Throws<InvalidDataException>(()=>BadwaterNorthRowSource.ForStep(1,"NorthRowE5040162026-10-10"));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void EachMapAcceptsExactlyTheAccumulatedNorthernFootprint(int step)
        {
            var grid=MapGrid(step);
            Assert.That(DeathValleyMapData.GetPlayableBounds(grid),Is.EqualTo(new double[]{499920,4006200,524496,4018488}));
            var duplicate=(DeathValleyMapRecord[])grid.Clone(); duplicate[1]=duplicate[0];
            Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.GetPlayableBounds(duplicate));
            Array.Resize(ref grid,grid.Length-1);
            Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.GetPlayableBounds(grid));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void AabbCannotHideHoleOrPrematureEastwardFill(int step)
        {
            var grid=MapGrid(step);
            int east=499920+4096*(step+1);
            grid[10].bounds_m=new double[]{east,4014648,east+256,4014904};
            Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.GetPlayableBounds(grid));
        }

        [TestCase("step")] [TestCase("section")] [TestCase("retained_omission")]
        [TestCase("duplicate")] [TestCase("bounds")] [TestCase("owner")] [TestCase("metadata")] [TestCase("predecessor")]
        public void DeserializedReceiptRejectsWrongOrderingOwnershipOrIncompleteData(string mutation)
        {
            var config=Available(); var m=BadwaterNorthRowSource.ReadManifest(config);
            if(mutation=="step") m.row_step=config.Step==5 ? 4 : config.Step+1;
            else if(mutation=="section") m.new_tiles[0].section="north";
            else if(mutation=="retained_omission") Array.Resize(ref m.retained_tiles,m.retained_tiles.Length-1);
            else if(mutation=="duplicate") m.new_tiles[1]=m.new_tiles[0];
            else if(mutation=="bounds") m.new_tiles[0].bounds_m[1]-=256;
            else if(mutation=="owner") m.retained_tiles[0].data_path=config.AssetRoot+"/TerrainData/unowned.asset";
            else if(mutation=="metadata") m.new_tiles[0].source_version="";
            else m.predecessor_manifest_sha256="";
            Assert.Throws<InvalidDataException>(()=>BadwaterNorthRowSource.ValidateManifest(config,m));
        }

        [Test] public void ProtectionReceiptCannotOmitThePrecedingAcceptedSection()
        {
            var c=Available(); var m=BadwaterNorthRowSource.ReadManifest(c);
            string previous=c.Step==1 ? BadwaterNorthRidgeSource.AssetRoot : BadwaterNorthRowSource.ForStep(c.Step-1).AssetRoot;
            m.protected_files=m.protected_files.Where(f=>!f.path.StartsWith(previous+"/",StringComparison.Ordinal)).ToArray();
            Assert.Throws<InvalidDataException>(()=>BadwaterNorthRowSource.VerifyProtectedFiles(c,m));
        }

        [Test] public void ExportedNormalizedSouthAndWestBordersMatchRetainedNativeReadbackExactly()
        {
            var c=Available(); var m=BadwaterNorthRowSource.ReadManifest(c,true);
            var retained=m.retained_tiles.ToDictionary(t=>t.geographic_key);
            int joins=0;
            foreach(var tile in m.new_tiles)
            {
                int e=tile.bounds_m[0],n=tile.bounds_m[1];
                var h=BadwaterNorthRowSource.ReadNormalized(c,tile);
                if(n==4014392)
                {
                    var south=BadwaterNorthRowSource.ReadNormalized(c,retained[$"epsg26911/e{e}/n{n-256}/size256"]);
                    for(int i=0;i<257;i++) Assert.That(h[0,i],Is.EqualTo(south[256,i]));
                    joins++;
                }
                if(e==c.East)
                {
                    var west=BadwaterNorthRowSource.ReadNormalized(c,retained[$"epsg26911/e{e-256}/n{n}/size256"]);
                    for(int i=0;i<257;i++) Assert.That(h[i,0],Is.EqualTo(west[i,256]));
                    joins++;
                }
            }
            Assert.That(joins,Is.EqualTo(32));
        }

        [Test] public void SavedCandidatePreservesEveryRetainedAssetAndSparseNeighborContract()
        {
            var c=Available(); var scene=EditorSceneManager.OpenScene(c.CandidateScene);
            var report=BadwaterNorthRowValidator.ValidateScene(scene,true);
            Assert.That(report.terrains,Is.EqualTo(c.TotalCount)); Assert.That(report.retained,Is.EqualTo(c.RetainedCount));
            Assert.That(report.edges,Is.EqualTo(c.EdgeCount)); Assert.That(report.outer_edges,Is.EqualTo(288));
            Assert.That(report.collider_samples,Is.EqualTo(c.ColliderSamples));
        }

        private static BadwaterNorthRowSource.Config Available()
        {
            for(int step=5;step>=1;step--)
            {
                var c=BadwaterNorthRowSource.ForStep(step);
                if(File.Exists(BadwaterNorthRowSource.ProjectFile(c.ManifestPath))) return c;
            }
            Assert.Ignore("Requires a freshly exported northern-row candidate in the isolated validation copy.");
            return null;
        }
        private static DeathValleyMapRecord[] MapGrid(int step)
        {
            var result=new List<DeathValleyMapRecord>();
            for(int row=0;row<48;row++) for(int col=0;col<(row<32 ? 96 : 16*(step+1));col++)
            {
                int e=499920+col*256,n=4006200+row*256;
                result.Add(new DeathValleyMapRecord{geographic_key=$"epsg26911/e{e}/n{n}/size256",bounds_m=new double[]{e,n,e+256,n+256}});
            }
            return result.ToArray();
        }
    }
}
