using System;
using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class BadwaterNorthRow4Tests
    {
        [Test] public void SixOrderedUnions_HaveExactTopologyAndSparseIntermediateFootprints()
        {
            for(int step=1;step<=6;step++)
            {
                var c=new BadwaterNorthRow4Source.Config(step,"2026-10-11"); var cells=BadwaterNorthRow4Source.Occupied(step);
                int edges=cells.Sum(p=>(cells.Contains(p+new Vector2Int(256,0))?1:0)+(cells.Contains(p+new Vector2Int(0,256))?1:0));
                Assert.That(cells.Count,Is.EqualTo(c.TotalCount));Assert.That(edges,Is.EqualTo(c.EdgeCount));Assert.That(4*cells.Count-2*edges,Is.EqualTo(384));
                Assert.That(cells.Except(BadwaterNorthRow4Source.Occupied(step-1)).Count(),Is.EqualTo(256));
                Assert.That(cells.Contains(new Vector2Int(524240,4030520)),Is.EqualTo(step==6));
            }
        }
        [TestCase(0)] [TestCase(7)] public void InvalidOrderedScope_IsRejected(int step) => Assert.Throws<InvalidDataException>(()=>new BadwaterNorthRow4Source.Config(step,"2026-10-11"));
        [Test] public void MissingOrInvalidExecutionDate_IsRejected() => Assert.Throws<InvalidDataException>(()=>new BadwaterNorthRow4Source.Config(1,"yesterday"));
        [Test] public void NativeReferences_RejectEscapeWrongGridAndMissingOwner()
        {
            var m=new BadwaterNorthRow4Source.Manifest { protected_roots=new[]{"Assets/_Project/Art/Terrain/Fixture"}, shared_snapshot_owner=BadwaterNorthRow4Source.SharedOwner, asset_root="Assets/_Project/Art/Terrain/NewFixture" };
            var r=new BadwaterNorthRow4Source.NativeReference {path="Assets/_Project/Art/Terrain/Fixture/Source/x.bytes",sha256=new string('a',64),orientation="south-first",samples=257};
            Assert.DoesNotThrow(()=>BadwaterNorthRow4Source.ValidateReference(m,r));
            r.path="../outside.bytes";Assert.Throws<InvalidDataException>(()=>BadwaterNorthRow4Source.ValidateReference(m,r));
            r.path="Assets/_Project/Art/Terrain/Unprotected/Source/x.bytes";Assert.Throws<InvalidDataException>(()=>BadwaterNorthRow4Source.ValidateReference(m,r));
            r.path="Assets/_Project/Art/Terrain/Fixture/Source/x.bytes";r.samples=129;Assert.Throws<InvalidDataException>(()=>BadwaterNorthRow4Source.ValidateReference(m,r));
        }
        [Test] public void ChangedNativeHash_IsRejectedBeforeDecode()
        {
            var c=BadwaterNorthRow4Source.Selected;
            var m=BadwaterNorthRow4Source.ReadManifest(c,false);var t=m.retained_tiles[0];
            var changed=new BadwaterNorthRow4Source.NativeReference {path=t.native_reference.path,sha256=new string('0',64),orientation=t.native_reference.orientation,samples=257};
            Assert.Throws<InvalidDataException>(()=>BadwaterNorthRow4Source.ReadReference(m,changed));
        }
        private static BadwaterNorthRow4Source.Manifest Fixture(BadwaterNorthRow4Source.Config c) => BadwaterNorthRow4Source.ReadManifest(c,false);
        [Test] public void DeveloperMap_RejectsMissingDuplicateShiftedAndPrematureCells()
        {
            var tiles=BadwaterNorthRow4Source.Occupied(1).OrderBy(p=>p.x).ThenBy(p=>p.y).Select(p=>new DeathValleyMapRecord{bounds_m=new double[]{p.x,p.y,p.x+256,p.y+256},geographic_key=$"epsg26911/e{p.x}/n{p.y}/size256"}).ToArray();
            Assert.That(DeathValleyMapData.GetPlayableBounds(tiles),Is.EqualTo(new double[]{499920,4006200,524496,4030776}));
            Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.GetPlayableBounds(tiles.Take(tiles.Length-1).ToArray()));
            var bad=(DeathValleyMapRecord[])tiles.Clone();bad[1]=bad[0];Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.GetPlayableBounds(bad));
            bad=(DeathValleyMapRecord[])tiles.Clone();bad[0]=new DeathValleyMapRecord{bounds_m=new double[]{524240,4030520,524496,4030776},geographic_key="epsg26911/e524240/n4030520/size256"};Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.GetPlayableBounds(bad));
        }
        [TestCase("hole")] [TestCase("duplicate")] [TestCase("shift")] [TestCase("premature")] [TestCase("missing-owner")] [TestCase("wrong-south")]
        public void ExactFootprint_RejectsBrokenNativeManifest(string issue)
        {
            var c=BadwaterNorthRow4Source.Selected;var m=Fixture(c);
            if(issue=="hole")m.new_tiles=m.new_tiles.Take(255).ToArray();
            if(issue=="duplicate")m.new_tiles[1]=m.new_tiles[0];
            if(issue=="shift")m.new_tiles[0].bounds_m[0]++;
            if(issue=="premature")m.new_tiles[0].bounds_m=new[]{524240,4030520,524496,4030776};
            if(issue=="missing-owner")m.protected_roots=m.protected_roots.Skip(1).ToArray();
            if(issue=="wrong-south")m.new_tiles[0].position[2]+=256;
            Assert.Throws<InvalidDataException>(()=>BadwaterNorthRow4Source.ValidateManifest(c,m,false));
        }
        [TestCase("predecessor")] [TestCase("west-anchor")] [TestCase("corner")] [TestCase("encoding")] [TestCase("source-proof")]
        public void OrderedSourceContract_RejectsChangedLineageAndFirstStepWestAnchor(string issue)
        {
            var c=BadwaterNorthRow4Source.Selected;var m=Fixture(c);
            if(issue=="predecessor")m.predecessor_manifest_sha256="";
            if(issue=="west-anchor")m.color_corner.west_anchor_present=!m.color_corner.west_anchor_present;
            if(issue=="corner")m.color_corner.west_exception_pixels=m.color_corner.west_exception_pixels==0?1:0;
            if(issue=="encoding")m.range_m=2400;
            if(issue=="source-proof")m.blender_source_sha256="";
            Assert.Throws<InvalidDataException>(()=>BadwaterNorthRow4Source.ValidateManifest(c,m,false));
        }
    }
}
