using System;
using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class DeathValleyMapTests
    {
        private DeathValleyMapData data;
        [OneTimeSetUp] public void Load() => data=new DeathValleyMapData(Path.GetDirectoryName(Application.dataPath));

        [Test] public void PortableGridMatchesRecordedBoundsAndInventory()
        {
            Assert.That(data.Manifest.width,Is.EqualTo(961));Assert.That(data.Manifest.height,Is.EqualTo(1121));
            Assert.That(data.Catalog.unity_tiles.Length,Is.EqualTo(256));Assert.That(data.Catalog.regional_source_tiles.Length,Is.EqualTo(42));
            Assert.That(data.Heights.Min(),Is.InRange(-86f,-84f));Assert.That(data.Heights.Max(),Is.InRange(3600f,3700f));
        }
        [Test] public void ProjectedCoordinatesRoundTripAndUnityOriginRemainDistinct()
        {
            Vector3 p=data.Project(520700,4007100,100,3);
            double[] back=data.Unproject(p);
            Assert.That(back[0],Is.EqualTo(520700).Within(.01));Assert.That(back[1],Is.EqualTo(4007100).Within(.01));
            Assert.That(p.y,Is.EqualTo(.3f).Within(1e-5f));
            Vector2 unity=data.UnityPosition(520700,4007100);
            Assert.That(unity.x,Is.EqualTo(-1748));Assert.That(unity.y,Is.EqualTo(-1148));
        }
        [Test] public void FocusChunkAndSharedCornerUseAbsoluteGeography()
        {
            Assert.That(data.Catalog.unity_tiles.Where(t=>t.Contains(520700,4007100)).Single().Key,Is.EqualTo("r12_c01"));
            Assert.That(data.Catalog.unity_tiles.Count(t=>t.Contains(520656,4010040)),Is.EqualTo(4));
            Assert.That(data.Catalog.unity_tiles.Count(t=>t.Contains(519000,4007100)),Is.Zero);
            Assert.That(data.Catalog.expansion_candidates.Single(t=>t.Contains(519000,4007100)).Key,Is.EqualTo("candidate_west"));
        }
        [Test] public void MeshHasSimplifiedGeometryAndPickingReturnsClosestSurface()
        {
            Mesh mesh=data.BuildRelief(3);
            try
            {
                Assert.That(mesh.vertexCount,Is.EqualTo(241*281));Assert.That(mesh.triangles.Length/3,Is.EqualTo(240*280*2));
                Vector3 start=data.Project(520700,4007100,10000,3);
                Assert.That(DeathValleyMapData.Pick(new Ray(start,Vector3.down),mesh.vertices,mesh.triangles,out Vector3 hit),Is.True);
                double[] projected=data.Unproject(hit);
                Assert.That(projected[0],Is.EqualTo(520700).Within(.05));Assert.That(projected[1],Is.EqualTo(4007100).Within(.05));
                Assert.That(hit.y,Is.EqualTo(data.SurfaceHeight(520700,4007100)*3/1000).Within(.0001f));
                Assert.That(mesh.normals.All(n=>n.y>0),Is.True);
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        [Test] public void RayMissesAndBackfacesAreHandledWithoutFalseHits()
        {
            Vector3 a=new Vector3(-1,0,-1),b=new Vector3(1,0,-1),c=new Vector3(0,0,1);
            Assert.That(DeathValleyMapData.IntersectTriangle(new Ray(Vector3.up*5,Vector3.down),a,b,c,out float d),Is.True);
            Assert.That(d,Is.EqualTo(5));
            Assert.That(DeathValleyMapData.IntersectTriangle(new Ray(Vector3.down*5,Vector3.up),a,b,c,out _),Is.True);
            Assert.That(DeathValleyMapData.IntersectTriangle(new Ray(new Vector3(8,5,0),Vector3.down),a,b,c,out _),Is.False);
            Assert.That(DeathValleyMapData.IntersectTriangle(new Ray(Vector3.up*5,Vector3.up),a,b,c,out _),Is.False);
            Assert.That(DeathValleyMapData.IntersectTriangle(new Ray(Vector3.up,Vector3.right),a,b,c,out _),Is.False);
        }
        [Test] public void CoverageLinesFollowEveryTriangleBetweenEndpoints()
        {
            foreach(var segment in new[]{new[]{520400d,4008248,524496,4008248},new[]{522448d,4006200,522448,4010296}})
            {
                Vector3[] vertices=data.EdgeVertices(segment[0],segment[1],segment[2],segment[3],3);
                Assert.That(vertices.Length,Is.GreaterThan(5));
                for(int i=0;i<vertices.Length-1;i++)
                {
                    Vector3 middle=(vertices[i]+vertices[i+1])/2;double[] coordinate=data.Unproject(middle);
                    Assert.That(middle.y-.025f,Is.EqualTo(data.SurfaceHeight(coordinate[0],coordinate[1])*3/1000).Within(.0001f));
                }
            }
        }
        [Test] public void BoundsRejectNonfiniteReversedOrEmptyGeography()
        {
            foreach(var b in new[]{new[]{0d,0,0,1},new[]{1d,0,0,1},new[]{0d,0,double.NaN,1},new[]{0d,1,2,0}})
                Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.ValidateBounds(b));
        }
        [Test] public void HashChangeRefusesReliefWithoutTouchingProductionAssets()
        {
            string temporary=Path.Combine(Path.GetTempPath(),"DeathValleyMapTests-"+Guid.NewGuid().ToString("N"));
            string folder=Path.Combine(temporary,"Docs/DeathValley");Directory.CreateDirectory(folder);
            try
            {
                string original=Path.Combine(data.Root,"Docs/DeathValley");
                File.Copy(Path.Combine(original,"coverage_catalog.json"),Path.Combine(folder,"coverage_catalog.json"));
                string snapshot=Directory.GetFiles(original,"developer_map.json",SearchOption.AllDirectories).Single();
                File.Copy(snapshot,Path.Combine(folder,"developer_map.json"));
                string grid=Path.Combine(folder,data.Manifest.grid_file);Directory.CreateDirectory(Path.GetDirectoryName(grid));
                File.WriteAllBytes(grid,new byte[]{1,2,3,4});
                Assert.Throws<InvalidDataException>(()=>new DeathValleyMapData(temporary));
            }
            finally{Directory.Delete(temporary,true);}
        }
        [Test] public void PackedAerialImagesDecodeOnTheirOriginalGeographicGrids()
        {
            Assert.That(data.HasImagery,Is.True);Assert.That(data.Manifest.height_source,Is.EqualTo("saved_blender_meshes"));
            Assert.That(data.Manifest.textures.Select(t=>t.spacing_m),Is.EqualTo(new[]{200f,20f,10f,2f}));
            for(int i=0;i<4;i++)
            {
                var texture=new Texture2D(2,2);
                try
                {
                    Assert.That(ImageConversion.LoadImage(texture,data.ImageryBytes[i]),Is.True);
                    Assert.That(texture.width,Is.EqualTo(data.Manifest.textures[i].width));
                    Assert.That(texture.height,Is.EqualTo(data.Manifest.textures[i].height));
                }
                finally{UnityEngine.Object.DestroyImmediate(texture);}
            }
            Mesh mesh=data.BuildRelief(3,true);
            try
            {
                Assert.That(mesh.uv[0],Is.EqualTo(new Vector2(0,1))); // Northwest = upper left image.
                Assert.That(mesh.uv.Last(),Is.EqualTo(new Vector2(1,0)));
                Assert.That(mesh.colors.All(c=>Mathf.Abs(c.r-c.g)<1e-6f && Mathf.Abs(c.g-c.b)<1e-6f),Is.True);
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        [Test] public void ImageSourcesRejectEscapingPathsAndChangedBytes()
        {
            string temporary=Path.Combine(Path.GetTempPath(),"DeathValleyMapTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);
            try
            {
                File.WriteAllBytes(Path.Combine(temporary,"image.png"),new byte[]{1,2,3});
                Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.ReadVerifiedFile(temporary,"../image.png","ignored"));
                Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.ReadVerifiedFile(temporary,"image.png",new string('0',64)));
                Assert.Throws<InvalidDataException>(()=>DeathValleyMapData.ReadVerifiedFile(temporary,Path.Combine(temporary,"image.png"),"ignored"));
            }
            finally{Directory.Delete(temporary,true);}
        }
        [Test] public void PlanPanningUsesMetresPerPixelWithoutChangingAltitude()
        {
            Vector3 moved=DeathValleyMapWindow.PanTarget(new Vector3(5,7,9),new Vector2(100,-50),10,1000);
            Assert.That(moved.x,Is.EqualTo(3).Within(.0001f));
            Assert.That(moved.z,Is.EqualTo(8).Within(.0001f));
            Assert.That(moved.y,Is.EqualTo(7));
        }
        [Test] public void PlanZoomKeepsThePointUnderTheCursorFixed()
        {
            Vector3 centre=new Vector3(12,3,18);Vector2 cursor=new Vector2(200,-100);
            Vector3 before=centre+new Vector3(cursor.x,0,-cursor.y)*.02f;
            Vector3 moved=DeathValleyMapWindow.ZoomTarget(centre,cursor,10,5,1000);
            Vector3 after=moved+new Vector3(cursor.x,0,-cursor.y)*.01f;
            Assert.That(Vector3.Distance(before,after),Is.LessThan(.0001f));
        }
        [Test] public void EntireMapFitsBothPortraitAndLandscapeViewports()
        {
            foreach(float aspect in new[]{.4f,1f,2f})
            {
                double[] b=data.Manifest.bounds_m;float size=DeathValleyMapWindow.PlanViewSize(b,aspect);
                Assert.That(size*2,Is.GreaterThan((b[3]-b[1])/1000));
                Assert.That(size*2*aspect,Is.GreaterThan((b[2]-b[0])/1000));
            }
        }
        [Test] public void ChangedEncodingSceneIdentityOrOriginCannotInheritCoordinateProof()
        {
            string temporary=Path.Combine(Path.GetTempPath(),"DeathValleyMapTests-"+Guid.NewGuid().ToString("N"));
            string folder=Path.Combine(temporary,"Docs/DeathValley");Directory.CreateDirectory(folder);
            try
            {
                File.Copy(Path.Combine(data.Root,"Docs/DeathValley/coverage_catalog.json"),Path.Combine(folder,"coverage_catalog.json"));
                string json=JsonUtility.ToJson(data.Manifest);
                foreach(string mutation in new[]{"encoding","scene","origin"})
                {
                    var manifest=JsonUtility.FromJson<DeathValleyMapManifest>(json);
                    if(mutation=="encoding")manifest.encoding="south-to-north";
                    if(mutation=="scene")manifest.scene_guid="unrelated-scene";
                    if(mutation=="origin")manifest.unity_origin_m[0]+=256;
                    File.WriteAllText(Path.Combine(folder,"developer_map.json"),JsonUtility.ToJson(manifest));
                    Assert.Throws<InvalidDataException>(()=>new DeathValleyMapData(temporary));
                }
            }
            finally{Directory.Delete(temporary,true);}
        }
    }
}
