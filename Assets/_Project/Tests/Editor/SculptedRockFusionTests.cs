using System;
using System.IO;
using System.Collections;
using System.Reflection;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace BooterBigArm.Tests
{
    public sealed class SculptedRockFusionTests
    {
        private string Blender => EditorPrefs.GetString(SculptedRockFusionTool.BlenderPreference, "");
        private SculptedRockFusionTool.Result Run(GameObject a, GameObject b, string label)
        {
            Assert.That(File.Exists(Blender), Is.True, "Configure installed Blender for these integration tests.");
            var snapshot = SculptedRockFusionTool.Capture(new[] { a, b });
            string directory = Path.GetFullPath("Logs/RockFusionTests/" + label); Directory.CreateDirectory(directory);
            string input = Path.Combine(directory, "input.json"), output = Path.Combine(directory, "output.json");
            File.WriteAllText(input, JsonUtility.ToJson(snapshot.input));
            using (var process = SculptedRockFusionTool.StartBackend(Blender, input, output))
            {
                if (!process.WaitForExit(30000)) { process.Kill(); Assert.Fail("Blender fixture exceeded 30 seconds"); }
                return SculptedRockFusionTool.ReadResult(output);
            }
        }
        private static void Material(GameObject a, GameObject b)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SculptedBasaltAssetPipeline.MaterialPath);
            a.GetComponent<MeshRenderer>().sharedMaterial = material; b.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        [Test]
        public void OverlappingCubesRemoveBuriedFacesAndKeepJoinEdges()
        {
            var a = GameObject.CreatePrimitive(PrimitiveType.Cube); var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                b.transform.position = new Vector3(.5f, .2f, .1f); Material(a, b);
                var result = Run(a, b, "overlap");
                Assert.That(result.report.volume, Is.EqualTo(1.64).Within(.0001));
                Assert.That(result.report.seam_edges, Is.GreaterThan(0)); Assert.That(result.report.interior_faces, Is.Zero);
                var mesh = SculptedRockFusionTool.MakeMesh(result);
                try { Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount)); Assert.That(mesh.tangents.Length, Is.EqualTo(mesh.vertexCount)); }
                finally { UnityEngine.Object.DestroyImmediate(mesh); }
            }
            finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
        }
        [Test]
        public void DisjointRocksAreRejectedInsteadOfPretendingTheyAreOneSolid()
        {
            var a = GameObject.CreatePrimitive(PrimitiveType.Cube); var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                b.transform.position = Vector3.right * 2; Material(a,b);
                Assert.That(Assert.Throws<InvalidOperationException>(() => Run(a,b,"disjoint")).Message, Does.Contain("one closed solid"));
            }
            finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
        }
        [Test]
        public void ActualBasaltPrefabsFuseIntoOneClosedTexturedSurface()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SculptedBasaltAssetPipeline.PrefabPath);
            var a = UnityEngine.Object.Instantiate(prefab); var b = UnityEngine.Object.Instantiate(prefab);
            try
            {
                b.transform.position = new Vector3(1.3f,.4f,.5f); b.transform.rotation = Quaternion.Euler(0,27,0);
                var result=Run(a,b,"basalt");
                Assert.That(result.report.components, Is.EqualTo(1)); Assert.That(result.report.nonmanifold, Is.Zero);
                Assert.That(result.report.seam_edges, Is.GreaterThan(0)); Assert.That(result.report.volume, Is.LessThan(result.report.input_volume));
                Assert.That(a.activeSelf && b.activeSelf, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
        }
        [Test]
        public void OpenMeshesAreRejectedWithoutChangingTheOriginals()
        {
            var a = GameObject.CreatePrimitive(PrimitiveType.Cube); var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var opened = UnityEngine.Object.Instantiate(a.GetComponent<MeshFilter>().sharedMesh);
            try
            {
                var indices = opened.triangles; Array.Resize(ref indices, indices.Length - 3); opened.triangles = indices;
                a.GetComponent<MeshFilter>().sharedMesh = opened; b.transform.position=Vector3.right*.5f; Material(a,b);
                Assert.That(Assert.Throws<InvalidOperationException>(() => Run(a,b,"open")).Message, Does.Contain("closed"));
                Assert.That(a.activeSelf && b.activeSelf, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); UnityEngine.Object.DestroyImmediate(opened); }
        }
        [UnityTest]
        public IEnumerator WindowBakeCreatesLinkedPrefabAndUndoRestoresSources()
        {
            if (AssetDatabase.IsValidFolder(SculptedRockFusionTool.OutputFolder)) Assert.Ignore("Run roundtrip in an isolated project with no existing fused assets.");
            var a=GameObject.CreatePrimitive(PrimitiveType.Cube); var b=GameObject.CreatePrimitive(PrimitiveType.Cube);
            SculptedRockFusionTool window=null;
            try
            {
                b.transform.position=new Vector3(.5f,.2f,.1f); Material(a,b);
                Selection.objects=new UnityEngine.Object[]{a,b};
                // Exercise the actual window controller without creating a native view in headless batchmode.
                window=ScriptableObject.CreateInstance<SculptedRockFusionTool>();
                typeof(SculptedRockFusionTool).GetMethod("Begin",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);
                double deadline=EditorApplication.timeSinceStartup+30;
                while(a.activeSelf && EditorApplication.timeSinceStartup<deadline) yield return null;
                Assert.That(a.activeSelf || b.activeSelf,Is.False,"Window bake did not replace the visible sources.");
                var fused=Selection.activeGameObject;
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(fused),Is.True);
                Assert.That(fused.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(1));
                Assert.That(fused.GetComponent<MeshCollider>().sharedMesh,Is.SameAs(fused.GetComponent<MeshFilter>().sharedMesh));
                yield return null;
                Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
                Assert.That(a.activeSelf && b.activeSelf,Is.True); Assert.That(fused==null,Is.True);
            }
            finally
            {
                if(window!=null)UnityEngine.Object.DestroyImmediate(window); UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
                if(AssetDatabase.IsValidFolder(SculptedRockFusionTool.OutputFolder))AssetDatabase.DeleteAsset(SculptedRockFusionTool.OutputFolder);
            }
        }
        public static void CaptureBasaltFixture()
        {
            var result=SculptedRockFusionTool.ReadResult("Logs/RockFusionTests/basalt/output.json");
            var mesh=SculptedRockFusionTool.MakeMesh(result);
            var rock=new GameObject("Fusion proof",typeof(MeshFilter),typeof(MeshRenderer));
            rock.GetComponent<MeshFilter>().sharedMesh=mesh;
            rock.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(SculptedBasaltAssetPipeline.MaterialPath);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.36f,.38f,.42f); RenderSettings.ambientEquatorColor=new Color(.24f,.24f,.24f); RenderSettings.ambientGroundColor=new Color(.1f,.1f,.1f);
            var sun=new GameObject("Proof key",typeof(Light)); sun.GetComponent<Light>().type=LightType.Directional;
            sun.GetComponent<Light>().intensity=1.4f; sun.transform.rotation=Quaternion.Euler(45,-35,0);
            var cameraObject=new GameObject("Proof camera",typeof(Camera)); var camera=cameraObject.GetComponent<Camera>();
            cameraObject.transform.position=mesh.bounds.center+new Vector3(9,6,-12); cameraObject.transform.LookAt(mesh.bounds.center);
            camera.orthographic=true; camera.orthographicSize=4.1f; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.12f,.12f,.12f);
            var target=new RenderTexture(1000,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1000,1000),0,0); image.Apply(); File.WriteAllBytes("Logs/RockFusionTests/basalt/review.png",image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=null; camera.targetTexture=null; target.Release();
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(sun);
                UnityEngine.Object.DestroyImmediate(rock); UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
    }
}
