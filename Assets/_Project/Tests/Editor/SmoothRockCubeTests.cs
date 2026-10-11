using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class SmoothRockCubeTests
    {
        private static SmoothRockBox Box(Vector3 center) => new SmoothRockBox(center,Quaternion.identity,Vector3.one);

        [Test]
        public void PlainBoxAndRotatedContactAreGeometric()
        {
            var a=Box(Vector3.zero);
            Assert.That(a.Distance(Vector3.zero),Is.EqualTo(-.5f).Within(.00001));
            Assert.That(a.Distance(new Vector3(.5f,.1f,.1f)),Is.EqualTo(0f).Within(.00001));
            Assert.That(a.Distance(Vector3.one),Is.EqualTo(Mathf.Sqrt(.75f)).Within(.00001));
            Assert.That(SmoothRockBox.Touches(a,Box(Vector3.one)),Is.True);
            Assert.That(SmoothRockBox.Touches(a,Box(new Vector3(1.01f,0,0))),Is.False);
            var rotation=Quaternion.Euler(0,45,0);
            var longA=new SmoothRockBox(Vector3.zero,rotation,new Vector3(2,.2f,.2f));
            var longB=new SmoothRockBox(new Vector3(.3f,0,.3f),rotation,new Vector3(2,.2f,.2f));
            Assert.That(longA.Bounds.Intersects(longB.Bounds),Is.True);
            Assert.That(SmoothRockBox.Touches(longA,longB),Is.False);
        }

        [TestCase(1,0,0,0f)]
        [TestCase(1,1,0,0f)]
        [TestCase(1,1,1,0f)]
        [TestCase(1,0,0,.013f)]
        [TestCase(1,1,0,.013f)]
        [TestCase(1,1,1,.013f)]
        [TestCase(1,1,1,.027f)]
        public void FaceEdgeAndCornerContactsProduceOneClosedExterior(int x,int y,int z,float phase)
        {
            var shift=new Vector3(phase,phase*.7f,phase*.3f);
            var boxes=new[]{Box(shift),Box(shift+new Vector3(x,y,z))};
            var field=SmoothRockCubeGeometry.BuildField(boxes);
            var mesh=Extract(field);
            Assert.That(mesh.Topology.IsValid,Is.True,mesh.Topology.Error);
            Assert.That(mesh.ConnectedComponents,Is.EqualTo(1));
            Assert.That(mesh.Topology.SignedVolume,Is.GreaterThan(1.85).And.LessThan(2.25));
            Assert.That(field.Sample(shift+new Vector3(x,y,z)*.5f),Is.LessThan(-.035f),"The contact neck must extend inside the field, not merely meet at a point.");
            // Isosurface vertices must represent the sampled field's boundary.
            foreach(var vertex in mesh.MeshData.Vertices.Where((_,i)=>i%23==0))
                Assert.That(Mathf.Abs(field.Sample(vertex+field.Origin)),Is.LessThan(.045f));
            TestContext.WriteLine($"contact {x},{y},{z} phase {phase}: {mesh.Topology.VertexCount} vertices, {mesh.Topology.TriangleCount} triangles, volume {mesh.Topology.SignedVolume:R}");
        }

        [Test]
        public void DuplicatesAndInputOrderDoNotChangeTheField()
        {
            var boxes=new[]{Box(Vector3.zero),Box(new Vector3(1,0,0)),Box(new Vector3(1,1,0))};
            var first=SmoothRockCubeGeometry.BuildField(boxes);
            var reversed=SmoothRockCubeGeometry.BuildField(boxes.Reverse().ToArray());
            var duplicated=SmoothRockCubeGeometry.BuildField(new[]{boxes[0],boxes[1],boxes[0],boxes[2]});
            Assert.That(reversed.Origin,Is.EqualTo(first.Origin));
            Assert.That(reversed.Values,Is.EqualTo(first.Values));
            Assert.That(duplicated.Values,Is.EqualTo(first.Values));
            var single=SmoothRockCubeGeometry.BuildField(new[]{boxes[0]});
            var nearby=SmoothRockCubeGeometry.BuildField(new[]{boxes[0],Box(new Vector3(.0001f,0,0))});
            foreach(var point in new[]{new Vector3(.5f,0,0),new Vector3(.5f,.5f,.5f),new Vector3(.45f,.5f,0)})
                Assert.That(Mathf.Abs(single.Sample(point)-nearby.Sample(point)),Is.LessThan(.0003f));
        }

        [Test]
        public void ThreeMeterChainAndUOpeningStayResolvedAtFixedSpacing()
        {
            var chain=new[]{Box(Vector3.zero),Box(Vector3.right),Box(Vector3.right*2)};
            var stopwatch=Stopwatch.StartNew();
            var field=SmoothRockCubeGeometry.BuildField(chain);
            var mesh=Extract(field);
            Assert.That(field.Spacing,Is.EqualTo(.02f));
            Assert.That(mesh.ConnectedComponents,Is.EqualTo(1));
            Assert.That(mesh.Topology.SignedVolume,Is.InRange(2.85,3.2));
            TestContext.WriteLine($"3m chain {field.Cells}: {mesh.Topology.TriangleCount} triangles, total {stopwatch.Elapsed.TotalMilliseconds:F1} ms");
            var u=SmoothRockCubeGeometry.BuildField(new[]{Box(new Vector3(-1,0,0)),Box(Vector3.zero),Box(Vector3.right),Box(new Vector3(-1,1,0)),Box(new Vector3(1,1,0))});
            Assert.That(u.Sample(new Vector3(0,1,0)),Is.GreaterThan(.15f));
            Assert.That(Extract(u).ConnectedComponents,Is.EqualTo(1));
        }

        [Test]
        public void AuthorsOffsetCubeJoinHasSmoothContourInsteadOfTerraces()
        {
            var boxes=new[]{Box(new Vector3(-3,.5f,0)),Box(new Vector3(-3.43f,1.041f,.735f))};
            var raw=SmoothRockCubeGeometry.BuildField(boxes,default,false);
            var smooth=SmoothRockCubeGeometry.BuildField(boxes);
            float Roughness(SmoothRockField field)
            {
                float previousHeight=0,previousSlope=0,variation=0;
                for(int i=0;i<=75;i++)
                {
                    float z=-.05f+i*.003f;
                    float low=.8f,high=1.4f;
                    for(int j=0;j<20;j++)
                    {
                        float mid=(low+high)*.5f;
                        if(field.Sample(new Vector3(-3.2f,mid,z))<=0) low=mid; else high=mid;
                    }
                    float height=(low+high)*.5f;
                    float slope=(height-previousHeight)/.003f;
                    if(i>1) variation+=Mathf.Abs(slope-previousSlope);
                    previousHeight=height; previousSlope=slope;
                }
                return variation;
            }
            float before=Roughness(raw),after=Roughness(smooth);
            TestContext.WriteLine($"Authored join slope variation: raw={before:R}, smooth={after:R}");
            Assert.That(after,Is.LessThan(before*.75f),"The actual authored offset needs a smooth contour, not simply a closed mesh.");
            Assert.That(Extract(smooth).ConnectedComponents,Is.EqualTo(1));
        }

        [TestCase(1,0,0)]
        [TestCase(1,1,0)]
        [TestCase(1,1,1)]
        public void RotatedScaledContactsAndFieldBoundarySidesRemainValid(int x,int y,int z)
        {
            var rotation=Quaternion.Euler(13,35,7);
            var size=new Vector3(1,.8f,1.2f);
            var shift=new Vector3(20.013f,.017f,-20.021f);
            var delta=rotation*Vector3.Scale(size,new Vector3(x,y,z));
            var boxes=new[]{new SmoothRockBox(shift,rotation,size),new SmoothRockBox(shift+delta,rotation,size)};
            Assert.That(SmoothRockBox.Touches(boxes[0],boxes[1]),Is.True);
            var field=SmoothRockCubeGeometry.BuildField(boxes);
            var result=Extract(field);
            Assert.That(result.ConnectedComponents,Is.EqualTo(1));
            Assert.That(field.Sample(shift+delta*.5f),Is.LessThan(-.025f));
            var v=result.MeshData.Vertices; var triangles=result.MeshData.Triangles;
            for(int i=0;i<triangles.Length;i+=3*97)
            {
                var a=v[triangles[i]]; var b=v[triangles[i+1]]; var c=v[triangles[i+2]];
                var normal=Vector3.Cross(b-a,c-a).normalized;
                var point=(a+b+c)/3f+field.Origin;
                Assert.That(Mathf.Abs(field.Sample(point)),Is.LessThan(.04f));
                Assert.That(field.Sample(point+normal*.04f),Is.GreaterThan(-.025f));
                Assert.That(field.Sample(point-normal*.04f),Is.LessThan(.025f));
            }
        }

        [Test]
        public void CancellationAndOversizeDoNotReturnAnExterior()
        {
            using(var cancellation=new CancellationTokenSource())
            {
                cancellation.Cancel();
                Assert.Throws<OperationCanceledException>(()=>SmoothRockCubeGeometry.BuildField(new[]{Box(Vector3.zero)},cancellation.Token));
            }
            Assert.Throws<InvalidOperationException>(()=>SmoothRockCubeGeometry.BuildField(new[]{Box(Vector3.zero),Box(Vector3.one*100)}));
        }

        [UnityTest]
        public IEnumerator AutomaticDuplicationSeparationUndoAndDisableRestoreSources()
        {
            SmoothRockCubePreview.Clear(); Undo.ClearAll();
            var a=SmoothRockCubePreview.CreateCube(Vector3.zero);
            var b=SmoothRockCubePreview.CreateCube(Vector3.right);
            GameObject duplicate=null;
            try
            {
                yield return WaitFor(()=>SmoothRockCubePreview.PreviewCount==1);
                var rendererA=a.GetComponent<MeshRenderer>(); var rendererB=b.GetComponent<MeshRenderer>();
                Assert.That(rendererA.forceRenderingOff,Is.True);
                Assert.That(rendererB.forceRenderingOff,Is.True);
                var firstVertices=SmoothRockCubePreview.PreviewMesh.vertices;
                duplicate=Object.Instantiate(a);
                Undo.RegisterCreatedObjectUndo(duplicate,"Duplicate cube fixture");
                yield return WaitFor(()=>SmoothRockCubePreview.PreviewCount==1);
                Assert.That(SmoothRockCubePreview.PreviewMesh.vertices,Is.EqualTo(firstVertices));
                Undo.IncrementCurrentGroup();
                Undo.RecordObject(b.transform,"Move cube fixture");
                b.transform.position=new Vector3(4,0,0);
                Undo.FlushUndoRecordObjects();
                SmoothRockCubePreview.Tick(true);
                Assert.That(SmoothRockCubePreview.PreviewCount,Is.Zero,"Old exterior must disappear immediately after movement.");
                Assert.That(rendererB.forceRenderingOff,Is.False);
                Undo.PerformUndo();
                yield return WaitFor(()=>SmoothRockCubePreview.PreviewCount==1);
                Assert.That(b.transform.position,Is.EqualTo(Vector3.right));
                duplicate.transform.position=new Vector3(0,0,3);
                yield return WaitFor(()=>SmoothRockCubePreview.PreviewCount==1);
                Assert.That(duplicate.GetComponent<MeshRenderer>().forceRenderingOff,Is.False,"A separated copy must not inherit temporary hiding.");
                b.GetComponent<SmoothRockCube>().enabled=false;
                SmoothRockCubePreview.Tick(true);
                Assert.That(SmoothRockCubePreview.PreviewCount,Is.Zero);
                Assert.That(rendererA.forceRenderingOff,Is.False);
                Assert.That(rendererB.forceRenderingOff,Is.False);
                Assert.That(a.GetComponents<Collider>(),Is.Empty);
                Assert.That(b.GetComponents<Collider>(),Is.Empty);
                TestContext.WriteLine($"worker {SmoothRockCubePreview.LastBuildMilliseconds:F1} ms; main-thread apply {SmoothRockCubePreview.LastApplyMilliseconds:F1} ms");
            }
            finally
            {
                SmoothRockCubePreview.Clear();
                if(duplicate!=null) Object.DestroyImmediate(duplicate);
                Object.DestroyImmediate(a); Object.DestroyImmediate(b); Undo.ClearAll();
            }
        }

        [UnityTest]
        public IEnumerator SaveReloadAndSceneIsolationPreserveOnlyTheRecipe()
        {
            const string path="Assets/_Project/Tests/SmoothCubeSaveFixture.unity";
            SmoothRockCubePreview.Clear();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SmoothRockCubePreview.CreateCube(Vector3.zero);
            SmoothRockCubePreview.CreateCube(Vector3.right);
            try
            {
                yield return WaitFor(()=>SmoothRockCubePreview.PreviewCount==1);
                Assert.That(EditorSceneManager.SaveScene(scene,path),Is.True);
                var text=System.IO.File.ReadAllText(path);
                Assert.That(text,Does.Not.Contain("Smooth cube rock exterior"));
                Assert.That(scene.GetRootGameObjects().Where(g=>g.GetComponent<SmoothRockCube>()!=null).All(g=>!g.GetComponent<MeshRenderer>().forceRenderingOff),Is.True);
                scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var sources=scene.GetRootGameObjects().Where(g=>g.GetComponent<SmoothRockCube>()!=null).ToArray();
                Assert.That(sources.Length,Is.EqualTo(2));
                Assert.That(sources.Select(g=>g.transform.position),Does.Contain(Vector3.right));
                yield return WaitFor(()=>SmoothRockCubePreview.PreviewCount==1);
                var other=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                var lone=SmoothRockCubePreview.CreateCube(Vector3.zero);
                SceneManager.MoveGameObjectToScene(lone,other);
                yield return WaitFor(()=>SmoothRockCubePreview.IsIdle);
                Assert.That(SmoothRockCubePreview.PreviewCount,Is.EqualTo(1));
                Assert.That(lone.GetComponent<MeshRenderer>().forceRenderingOff,Is.False);
                SmoothRockCubePreview.Clear();
                Assert.That(sources.All(g=>!g.GetComponent<MeshRenderer>().forceRenderingOff),Is.True);
                EditorSceneManager.CloseScene(other,true);
            }
            finally
            {
                SmoothRockCubePreview.Clear();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [UnityTest]
        public IEnumerator MovingDuringWorkerRejectsTheOldResult()
        {
            SmoothRockCubePreview.Clear();
            var a=SmoothRockCubePreview.CreateCube(Vector3.zero);
            var b=SmoothRockCubePreview.CreateCube(Vector3.one);
            try
            {
                SmoothRockCubePreview.Tick(true);
                var due=EditorApplication.timeSinceStartup+.2;
                while(EditorApplication.timeSinceStartup<due) yield return null;
                SmoothRockCubePreview.Tick(true);
                b.transform.position=Vector3.one*4;
                SmoothRockCubePreview.Tick(true);
                yield return WaitFor(()=>SmoothRockCubePreview.IsIdle);
                Assert.That(SmoothRockCubePreview.PreviewCount,Is.Zero);
                Assert.That(a.GetComponent<MeshRenderer>().forceRenderingOff,Is.False);
                Assert.That(b.GetComponent<MeshRenderer>().forceRenderingOff,Is.False);
            }
            finally { SmoothRockCubePreview.Clear(); Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }

        [Test]
        public void MarkerHasNoSettingsAndPickingUsesOrientedBoxes()
        {
            Assert.That(typeof(SmoothRockCube).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly),Is.Empty);
            var rotated=new SmoothRockBox(Vector3.zero,Quaternion.Euler(0,45,0),new Vector3(2,.2f,.2f));
            Assert.That(rotated.Raycast(new Ray(new Vector3(0,0,-5),Vector3.forward),out var hit),Is.True);
            Assert.That(hit,Is.GreaterThan(4.7f));
        }

        [Test]
        public void StarterPrefabIsOnePlainTexturedCubeWithoutCollidersOrHiddenFlags()
        {
            SmoothRockCubePreview.CreateStarterPrefab();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SmoothRockCubePreview.PrefabPath);
            Assert.That(prefab,Is.Not.Null);
            Assert.That(prefab.GetComponent<SmoothRockCube>(),Is.Not.Null);
            Assert.That(prefab.transform.childCount,Is.Zero);
            Assert.That(prefab.GetComponents<Collider>(),Is.Empty);
            Assert.That(prefab.GetComponent<Rigidbody>(),Is.Null);
            Assert.That((prefab.hideFlags&HideFlags.DontSave),Is.EqualTo(HideFlags.None));
            Assert.That(prefab.GetComponent<MeshRenderer>().sharedMaterial,Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>(SmoothRockCubePreview.MaterialPath)));
            Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh.bounds.size,Is.EqualTo(Vector3.one));
        }

        [Test]
        public void VolcanicMapsRepeatWithCorrectColorAndNormalImportAndNoGenericLayers()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(SmoothRockCubePreview.MaterialPath);
            Assert.That(material,Is.Not.Null);
            foreach(var slot in new[]{"_BaseMap","_TopBaseMap","_BottomBaseMap"})
                Assert.That(AssetDatabase.GetAssetPath(material.GetTexture(slot)),Does.EndWith("SmoothCubeVolcanic/VolcanicBlackBasalt_Albedo.png"));
            var albedo=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")));
            var normal=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetTexture("_NormalMap")));
            var surface=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetTexture("_SurfaceMap")));
            Assert.That(albedo.sRGBTexture,Is.True); Assert.That(albedo.wrapMode,Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(albedo.textureShape,Is.EqualTo(TextureImporterShape.Texture2D));
            Assert.That(normal.textureType,Is.EqualTo(TextureImporterType.NormalMap)); Assert.That(normal.sRGBTexture,Is.False);
            Assert.That(surface.sRGBTexture,Is.False); Assert.That(surface.wrapMode,Is.EqualTo(TextureWrapMode.Repeat));
            var cube=SmoothRockCubePreview.CreateCube(Vector3.zero);
            try
            {
                var properties=new MaterialPropertyBlock(); cube.GetComponent<MeshRenderer>().GetPropertyBlock(properties);
                Assert.That(properties.GetColor("_BaseColor"),Is.EqualTo(Color.white));
                Assert.That(properties.GetFloat("_RockMetersPerTile"),Is.EqualTo(1.25f));
                Assert.That(properties.GetFloat("_CrackAmount"),Is.Zero);
                Assert.That(properties.GetFloat("_SideGritAmount"),Is.Zero);
            }
            finally { Object.DestroyImmediate(cube); }
        }

        [Test]
        public void SampledExtractionRejectsInvalidInputs()
        {
            var values=new float[27];
            Assert.That(TopDown3DRockWorkbenchMesher.TryBuildSampledField(new Bounds(Vector3.zero,Vector3.one),new Vector3Int(2,2,2),values,float.NaN,default,out _,out _),Is.False);
            values[0]=float.PositiveInfinity;
            Assert.That(TopDown3DRockWorkbenchMesher.TryBuildSampledField(new Bounds(Vector3.zero,Vector3.one),new Vector3Int(2,2,2),values,.5f,default,out _,out _),Is.False);
        }

        [UnityTest]
        public IEnumerator EmptyScenePlayTransitionRestoresSourcesAndEditModeRebuilds()
        {
            SmoothRockCubePreview.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SmoothRockCubePreview.CreateCube(Vector3.zero).name="Cube play transition A";
            SmoothRockCubePreview.CreateCube(Vector3.right).name="Cube play transition B";
            yield return WaitFor(()=>SmoothRockCubePreview.PreviewCount==1);
            yield return new EnterPlayMode();
            Assert.That(SmoothRockCubePreview.PreviewCount,Is.Zero);
            var sources=Object.FindObjectsByType<SmoothRockCube>(FindObjectsSortMode.None);
            Assert.That(sources.Length,Is.EqualTo(2));
            Assert.That(sources.All(s=>!s.GetComponent<MeshRenderer>().forceRenderingOff),Is.True);
            yield return new ExitPlayMode();
            try { yield return WaitFor(()=>SmoothRockCubePreview.PreviewCount==1); }
            finally
            {
                SmoothRockCubePreview.Clear();
                foreach(var source in Object.FindObjectsByType<SmoothRockCube>(FindObjectsSortMode.None)) Object.DestroyImmediate(source.gameObject);
            }
        }

        // Isolated background capture entry point only; never invoke in the artist's live Editor.
        public static void CaptureReview() => CaptureReview(false);
        public static void CaptureJointReview() => CaptureReview(true);
        private static void CaptureReview(bool closeJoint)
        {
            SmoothRockCubePreview.Clear(); SmoothRockCubePreview.Suspended=true;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var directory=closeJoint?"Logs/SmoothCubeJointReview":"Logs/SmoothCubeRockReview";
            System.IO.Directory.CreateDirectory(directory);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.36f,.38f,.42f);
            RenderSettings.ambientEquatorColor=new Color(.24f,.24f,.24f);
            RenderSettings.ambientGroundColor=new Color(.1f,.1f,.1f);
            var sun=new GameObject("Review sun",typeof(Light));
            sun.GetComponent<Light>().type=LightType.Directional; sun.GetComponent<Light>().intensity=1.4f;
            sun.transform.rotation=Quaternion.Euler(45,-35,0);
            var boxes=closeJoint?new[]{Box(new Vector3(-3,.5f,0)),Box(new Vector3(-3.43f,1.041f,.735f))}
                :new[]{Box(new Vector3(-.5f,0,0)),Box(new Vector3(.5f,0,0)),Box(new Vector3(.5f,1,0))};
            var sources=boxes.Select(b=>SmoothRockCubePreview.CreateCube(b.Center)).ToArray();
            var field=SmoothRockCubeGeometry.BuildField(boxes);
            var result=Extract(field); var mesh=result.CreateMesh("Cube rock review exterior");
            var beforeMesh=closeJoint?Extract(SmoothRockCubeGeometry.BuildField(boxes,default,false)).CreateMesh("Unfiltered joint review"):null;
            var exterior=new GameObject("Review exterior",typeof(MeshFilter),typeof(MeshRenderer));
            exterior.transform.position=field.Origin;
            exterior.GetComponent<MeshFilter>().sharedMesh=mesh;
            exterior.GetComponent<MeshRenderer>().sharedMaterial=sources[0].GetComponent<MeshRenderer>().sharedMaterial;
            SmoothRockCubePreview.ApplyMaterial(exterior.GetComponent<MeshRenderer>());
            var cameraObject=new GameObject("Review camera",typeof(Camera)); var camera=cameraObject.GetComponent<Camera>();
            cameraObject.transform.position=new Vector3(4,3,-6); cameraObject.transform.LookAt(new Vector3(0,.5f,0));
            if(closeJoint)
            { cameraObject.transform.position=new Vector3(-1.5f,1.8f,-2.4f); cameraObject.transform.LookAt(new Vector3(-3.2f,1.05f,.2f)); }
            camera.orthographic=true; camera.orthographicSize=closeJoint?.45f:2f; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.14f,.14f,.15f);
            var target=new RenderTexture(1000,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);
            var oldTarget=RenderTexture.active;
            try
            {
                foreach(bool fused in new[]{false,true})
                {
                    exterior.SetActive(fused || closeJoint);
                    exterior.GetComponent<MeshFilter>().sharedMesh=fused || !closeJoint?mesh:beforeMesh;
                    foreach(var source in sources) source.GetComponent<MeshRenderer>().forceRenderingOff=fused || closeJoint;
                    camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,1000,1000),0,0); image.Apply();
                    System.IO.File.WriteAllBytes(directory+(closeJoint?(fused?"/after.png":"/before.png"):(fused?"/fused.png":"/sources.png")),image.EncodeToPNG());
                }
            }
            finally
            {
                camera.targetTexture=null; RenderTexture.active=oldTarget; target.Release();
                Object.DestroyImmediate(image); Object.DestroyImmediate(target); Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(sun); Object.DestroyImmediate(exterior); Object.DestroyImmediate(mesh);
                if(beforeMesh!=null) Object.DestroyImmediate(beforeMesh);
                foreach(var source in sources) Object.DestroyImmediate(source);
                SmoothRockCubePreview.Suspended=false;
            }
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            double timeout=EditorApplication.timeSinceStartup+20;
            do
            {
                SmoothRockCubePreview.Tick(true);
                if(condition()) yield break;
                yield return null;
            } while(EditorApplication.timeSinceStartup<timeout);
            Assert.Fail("Cube editing condition timed out.");
        }

        private static TopDown3DRockWorkbenchBuildResult Extract(SmoothRockField field)
        {
            Assert.That(TopDown3DRockWorkbenchMesher.TryBuildSampledField(field.Bounds,field.Cells,field.Values,field.Spacing,default,out var result,out var error,SmoothRockCubeGeometry.MaximumFineSamples),Is.True,error);
            return result;
        }
    }
}
