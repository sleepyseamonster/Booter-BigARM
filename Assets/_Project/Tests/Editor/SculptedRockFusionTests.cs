using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Linq;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Tests
{
    public sealed class SculptedRockFusionTests
    {
        [SetUp] public void PauseLiveOutsideItsTest() { SculptedRockLiveFusion.Suspended = true; SculptedRockLiveFusion.Clear(); }
        [TearDown] public void RestoreLive() { SculptedRockLiveFusion.Clear(); SculptedRockLiveFusion.Suspended = false; }
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
        public void DifferentBasaltTextureSetsRemainSeparateSlotsOnOneMesh()
        {
            var a=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SculptedBasaltAssetPipeline.PrefabPath));
            var b=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/TopDown3D/Environment/SculptedBasalt02.prefab"));
            try
            {
                b.transform.position=new Vector3(.8f,0,.3f);
                var result=Run(a,b,"mixed-basalt"); var mesh=SculptedRockFusionTool.MakeMesh(result);
                try { Assert.That(mesh.subMeshCount,Is.EqualTo(2)); Assert.That(mesh.GetIndexCount(0),Is.GreaterThan(0)); Assert.That(mesh.GetIndexCount(1),Is.GreaterThan(0)); }
                finally { UnityEngine.Object.DestroyImmediate(mesh); }
                Assert.That(result.report.interior_faces,Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
        }
        [UnityTest]
        public IEnumerator LiveFusionRebuildsAfterMovementAndRestoresSeparatedEditableSources()
        {
            var a=GameObject.CreatePrimitive(PrimitiveType.Cube); var b=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Material(a,b); a.AddComponent<SculptedRockFusionParticipant>(); b.AddComponent<SculptedRockFusionParticipant>();
                Assert.That(a.GetComponent<SculptedRockFusionParticipant>().LiveFusion,Is.False,"Manual fusion is the authoring default.");
                foreach(var root in new[]{a,b})
                { var settings=new SerializedObject(root.GetComponent<SculptedRockFusionParticipant>());settings.FindProperty("liveFusion").boolValue=true;settings.ApplyModifiedPropertiesWithoutUndo(); }
                b.transform.position=new Vector3(.5f,.2f,.1f); SculptedRockLiveFusion.Suspended=false;
                double deadline=EditorApplication.timeSinceStartup+20;
                while(SculptedRockLiveFusion.PreviewCount==0 && EditorApplication.timeSinceStartup<deadline) { SculptedRockLiveFusion.Tick(); yield return null; }
                Assert.That(SculptedRockLiveFusion.PreviewCount,Is.EqualTo(1));
                Assert.That(a.activeSelf && b.activeSelf,Is.True); Assert.That(a.GetComponent<MeshRenderer>().enabled,Is.True);
                Assert.That(a.GetComponent<MeshRenderer>().forceRenderingOff,Is.True);
                Directory.CreateDirectory("Logs/RockFusionTests");
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(a.scene,"Logs/RockFusionTests/live-source-save.unity",true);
                Assert.That(File.ReadAllText("Logs/RockFusionTests/live-source-save.unity"),Does.Not.Contain("m_Name: Live Rock Fusion"));
                b.transform.position=Vector3.right*3;
                deadline=EditorApplication.timeSinceStartup+2;
                while(SculptedRockLiveFusion.PreviewCount>0 && EditorApplication.timeSinceStartup<deadline) { SculptedRockLiveFusion.Tick(); yield return null; }
                Assert.That(SculptedRockLiveFusion.PreviewCount,Is.Zero); Assert.That(a.GetComponent<MeshRenderer>().forceRenderingOff,Is.False);
                b.transform.position=new Vector3(.25f,.25f,.25f);
                deadline=EditorApplication.timeSinceStartup+20;
                while(SculptedRockLiveFusion.PreviewCount==0 && EditorApplication.timeSinceStartup<deadline) { SculptedRockLiveFusion.Tick(); yield return null; }
                Assert.That(SculptedRockLiveFusion.PreviewCount,Is.EqualTo(1));
                SculptedRockLiveFusion.Clear(); Assert.That(a.GetComponent<MeshRenderer>().forceRenderingOff,Is.False);
            }
            finally { SculptedRockLiveFusion.Suspended=true; SculptedRockLiveFusion.Clear(); UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
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
        public static void CaptureBasaltFixture() => CaptureFixture("basalt");
        public static void CaptureMixedBasaltFixture() => CaptureFixture("mixed-basalt");
        public static void CaptureMixedGeometryFixture() => CaptureFixture("mixed-basalt",true);
        public static void CaptureMixedAlbedoFixture() => CaptureFixture("mixed-basalt",false,true);
        public static void CaptureMixedOriginalsFixture() => CaptureFixture("mixed-basalt",false,false,true);
        private static void CaptureFixture(string fixture,bool neutral=false,bool albedoOnly=false,bool originals=false)
        {
            string directory="Logs/RockFusionTests/"+fixture;
            var result=SculptedRockFusionTool.ReadResult(directory+"/output.json");
            var mesh=SculptedRockFusionTool.MakeMesh(result);
            File.WriteAllText(directory+"/unity-mesh.json",JsonUtility.ToJson(new SculptedRockFusionTool.InputMesh {vertices=mesh.vertices,normals=mesh.normals,tangents=mesh.tangents,uv=mesh.uv,triangles=mesh.triangles,materialIndices=Enumerable.Range(0,mesh.subMeshCount).SelectMany(slot=>Enumerable.Repeat(slot,(int)mesh.GetIndexCount(slot)/3)).ToArray()}));
            var rock=new GameObject("Fusion proof",typeof(MeshFilter),typeof(MeshRenderer));
            rock.GetComponent<MeshFilter>().sharedMesh=mesh;
            var input=JsonUtility.FromJson<SculptedRockFusionTool.Input>(File.ReadAllText(directory+"/input.json"));
            rock.GetComponent<MeshRenderer>().sharedMaterials=input.materialIds.Select(id=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
            var sourceObjects=new System.Collections.Generic.List<GameObject>(); var sourceMeshes=new System.Collections.Generic.List<Mesh>();
            if(originals)
            {
                rock.GetComponent<MeshRenderer>().enabled=false;
                foreach(var source in input.sources)
                {
                    var m=new Mesh {vertices=source.vertices,normals=source.normals,uv=source.uv,tangents=source.tangents,triangles=source.triangles};
                    var go=new GameObject("Original",typeof(MeshFilter),typeof(MeshRenderer)); go.GetComponent<MeshFilter>().sharedMesh=m;
                    go.GetComponent<MeshRenderer>().sharedMaterial=rock.GetComponent<MeshRenderer>().sharedMaterials[source.materialIndices[0]];
                    sourceObjects.Add(go);sourceMeshes.Add(m);
                }
            }
            Material plain=null;
            Material[] simplified=null;
            if(albedoOnly)
            {
                simplified=rock.GetComponent<MeshRenderer>().sharedMaterials.Select(m=>new Material(m)).ToArray();
                foreach(var m in simplified){m.DisableKeyword("_NORMALMAP");m.DisableKeyword("_OCCLUSIONMAP");m.DisableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",.2f);}
                rock.GetComponent<MeshRenderer>().sharedMaterials=simplified;
            }
            if(neutral)
            {
                plain=new Material(Shader.Find("Universal Render Pipeline/Lit")); plain.SetColor("_BaseColor",Color.gray);
                rock.GetComponent<MeshRenderer>().sharedMaterials=Enumerable.Repeat(plain,input.materialIds.Length).ToArray();
            }
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
                image.ReadPixels(new Rect(0,0,1000,1000),0,0); image.Apply(); File.WriteAllBytes(directory+(originals?"/originals-review.png":neutral?"/geometry-review.png":albedoOnly?"/albedo-review.png":"/review.png"),image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=null; camera.targetTexture=null; target.Release();
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(sun);
                UnityEngine.Object.DestroyImmediate(rock); UnityEngine.Object.DestroyImmediate(mesh);
                if(plain!=null)UnityEngine.Object.DestroyImmediate(plain);
                if(simplified!=null)foreach(var m in simplified)UnityEngine.Object.DestroyImmediate(m);
                foreach(var go in sourceObjects)UnityEngine.Object.DestroyImmediate(go); foreach(var m in sourceMeshes)UnityEngine.Object.DestroyImmediate(m);
            }
        }
    }
}
