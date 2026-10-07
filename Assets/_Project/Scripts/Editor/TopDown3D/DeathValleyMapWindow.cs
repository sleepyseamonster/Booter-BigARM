using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    /// <summary>Developer-only preview. Never installs map objects in a gameplay scene.</summary>
    public sealed class DeathValleyMapWindow : EditorWindow
    {
        private DeathValleyMapData data;
        private PreviewRenderUtility preview;
        private Material material;
        private Material reliefMaterial;
        private Texture2D[] aerialTextures;
        private Mesh relief, chunkLines;
        private string error;
        [SerializeField] private float viewSize=145;
        [SerializeField] private Vector3 target;
        private double[] pendingFit;
        private bool dragging;
        private const float ReliefScale=1;

        [MenuItem("Booter & BigARM/Death Valley/Developer Map")]
        public static void Open() => GetWindow<DeathValleyMapWindow>("Death Valley Map");

        private void OnEnable()
        {
            minSize=new Vector2(320,220);
            Reload();
            if(data!=null)Fit(data.Manifest.bounds_m);
        }
        private void OnDisable() => Release();
        private void Release()
        {
            preview?.Cleanup();preview=null;
            foreach(var item in new UnityEngine.Object[]{material,reliefMaterial,relief,chunkLines})
                if(item!=null) DestroyImmediate(item);
            if(aerialTextures!=null)foreach(var texture in aerialTextures)if(texture!=null)DestroyImmediate(texture);
            aerialTextures=null;reliefMaterial=null;
            material=null;relief=chunkLines=null;
        }
        private void Reload()
        {
            Release();error=null;data=null;
            try
            {
                data=new DeathValleyMapData(Path.GetDirectoryName(Application.dataPath));
                preview=new PreviewRenderUtility();
                var shader=Shader.Find("Hidden/Internal-Colored");
                if(shader==null) throw new InvalidOperationException("Editor vertex-color preview shader is unavailable.");
                material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
                material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.Zero);
                material.SetInt("_Cull",(int)CullMode.Off);material.SetInt("_ZWrite",1);material.SetInt("_ZTest",(int)CompareFunction.LessEqual);
                Shader aerialShader=Shader.Find("Hidden/BooterBigArm/DeathValleyMapPreview");
                if(aerialShader==null)throw new InvalidOperationException("Developer aerial preview shader is unavailable.");
                reliefMaterial=new Material(aerialShader){hideFlags=HideFlags.HideAndDontSave};
                if(data.HasImagery)
                {
                    aerialTextures=new Texture2D[4];
                    string[] slots={"_MainTex","_CorridorTex","_PilotTex","_PatchTex"};
                    string[] boundsSlots={null,"_CorridorBounds","_PilotBounds","_PatchBounds"};
                    double[] full=data.Manifest.bounds_m;double width=full[2]-full[0],height=full[3]-full[1];
                    reliefMaterial.SetVector("_MapSize",new Vector4((float)width,(float)height,0,0));
                    for(int i=0;i<4;i++)
                    {
                        var record=data.Manifest.textures[i];
                        var texture=new Texture2D(2,2,TextureFormat.RGB24,false){name=record.id,hideFlags=HideFlags.HideAndDontSave,
                            wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
                        aerialTextures[i]=texture;
                        if(!ImageConversion.LoadImage(texture,data.ImageryBytes[i],true)||texture.width!=record.width||texture.height!=record.height)
                            throw new InvalidDataException("Packed Blender image dimensions or decoding changed.");
                        reliefMaterial.SetTexture(slots[i],texture);
                        if(i>0)
                        {
                            double[] b=record.bounds_m;
                            reliefMaterial.SetVector(boundsSlots[i],new Vector4((float)((b[0]-full[0])/width),(float)((b[1]-full[1])/height),
                                (float)((b[2]-full[0])/width),(float)((b[3]-full[1])/height)));
                        }
                    }
                }
                Rebuild();
            }
            catch(Exception exception){error=exception.Message;Release();data=null;}
        }
        private void Rebuild()
        {
            foreach(var mesh in new[]{relief,chunkLines})if(mesh!=null)DestroyImmediate(mesh);
            reliefMaterial.SetFloat("_UseImagery",data.HasImagery?1:0);
            relief=data.BuildRelief(ReliefScale,data.HasImagery);
            chunkLines=Lines(data.Catalog.unity_tiles,_=>new Color(.1f,.88f,.98f));
        }
        private Mesh Lines(IEnumerable<DeathValleyMapRecord> records,Func<DeathValleyMapRecord,Color> color)
        {
            var vertices=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            foreach(var record in records)
            {
                double[] b=record.bounds_m;
                double[,] corners={{b[0],b[1]},{b[2],b[1]},{b[2],b[3]},{b[0],b[3]},{b[0],b[1]}};
                for(int edge=0;edge<4;edge++)
                {
                    double ax=corners[edge,0],az=corners[edge,1],bx=corners[edge+1,0],bz=corners[edge+1,1];
                    Vector3[] points=data.EdgeVertices(ax,az,bx,bz,ReliefScale);
                    for(int step=0;step<points.Length-1;step++)
                        for(int end=0;end<2;end++)
                        {
                            vertices.Add(points[step+end]);
                            colors.Add(color(record));indices.Add(indices.Count);
                        }
                }
            }
            var mesh=new Mesh{name="Developer coverage outlines",hideFlags=HideFlags.HideAndDontSave,indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetIndices(indices.ToArray(),MeshTopology.Lines,0);mesh.RecalculateBounds();return mesh;
        }
        private void Fit(double[] bounds)
        {
            pendingFit=bounds;
            Repaint();
        }
        public static float PlanViewSize(double[] bounds,float aspect)
        {
            return Mathf.Max(.1f,(float)Math.Max(bounds[3]-bounds[1],(bounds[2]-bounds[0])/Mathf.Max(.1f,aspect))/1000*.53f);
        }
        public static Vector3 PanTarget(Vector3 current,Vector2 drag,float size,float viewportHeight)
        {
            return current+new Vector3(-drag.x,0,drag.y)*(size*2/Mathf.Max(1,viewportHeight));
        }
        public static Vector3 ZoomTarget(Vector3 current,Vector2 cursorFromCentre,float oldSize,float newSize,float viewportHeight)
        {
            return current+new Vector3(cursorFromCentre.x,0,-cursorFromCentre.y)*((oldSize-newSize)*2/Mathf.Max(1,viewportHeight));
        }
        private void OnGUI()
        {
            using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using(new EditorGUI.DisabledScope(data==null))
                {
                    if(GUILayout.Button("Entire Map",EditorStyles.toolbarButton))Fit(data.Manifest.bounds_m);
                    if(GUILayout.Button("Playable Area",EditorStyles.toolbarButton))Fit(data.Catalog.regions.Single(r=>r.id=="badwater").bounds_m);
                }
                GUILayout.FlexibleSpace();
            }
            if(data==null)
            {
                EditorGUILayout.HelpBox(error??"Map unavailable",MessageType.Error);
                return;
            }
            Rect rect=GUILayoutUtility.GetRect(100,100,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
            HandleMapInput(rect);
            ConfigureCamera(rect);
            if(Event.current.type==EventType.Repaint)GUI.DrawTexture(rect,Render(rect),ScaleMode.StretchToFill,false);
        }
        private void ConfigureCamera(Rect rect)
        {
            float aspect=Mathf.Max(.1f,rect.width/Mathf.Max(1,rect.height));
            if(pendingFit!=null)
            {
                double east=(pendingFit[0]+pendingFit[2])/2,north=(pendingFit[1]+pendingFit[3])/2;
                target=data.Project(east,north,data.SurfaceHeight(east,north),ReliefScale);
                viewSize=PlanViewSize(pendingFit,aspect);pendingFit=null;
            }
            var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=viewSize;
            camera.aspect=aspect;camera.nearClipPlane=.01f;camera.farClipPlane=3000;
            camera.allowHDR=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.09f,.11f);
            camera.transform.rotation=Quaternion.Euler(90,0,0);
            camera.transform.position=target-camera.transform.forward*Mathf.Max(50,viewSize*4);
        }
        private Texture Render(Rect rect)
        {
            preview.BeginPreview(rect,GUIStyle.none);
            Texture result=null;
            try
            {
                preview.DrawMesh(relief,Matrix4x4.identity,reliefMaterial,0);
                preview.DrawMesh(chunkLines,Matrix4x4.identity,material,0);
                preview.Render(false);
            }
            finally { result=preview.EndPreview(); }
            return result;
        }
        private void HandleMapInput(Rect rect)
        {
            Event e=Event.current;
            int control=GUIUtility.GetControlID(FocusType.Passive);
            if(e.type==EventType.MouseDown && rect.Contains(e.mousePosition) && e.button<=2)
            {
                dragging=true;GUIUtility.hotControl=control;e.Use();
            }
            if(e.type==EventType.MouseDrag && dragging && GUIUtility.hotControl==control)
            {
                target=PanTarget(target,e.delta,viewSize,rect.height);
                e.Use();Repaint();
            }
            if(e.type==EventType.MouseUp && dragging)
            {
                dragging=false;GUIUtility.hotControl=0;e.Use();
            }
            if(e.type==EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                float newSize=Mathf.Clamp(viewSize*Mathf.Exp(e.delta.y*.08f),.1f,500);
                target=ZoomTarget(target,e.mousePosition-rect.center,viewSize,newSize,rect.height);
                viewSize=newSize;e.Use();Repaint();
            }
        }

        // Background validation only: exports a preview; does not open/focus any window or scene.
        public static void RenderFromCli()
        {
            var window=CreateInstance<DeathValleyMapWindow>();
            try
            {
                if(window.data==null)throw new InvalidOperationException(window.error);
                string root=Path.GetDirectoryName(Application.dataPath);
                string output=Path.Combine(root,"Logs/DeathValleyVisualizer");Directory.CreateDirectory(output);
                window.Fit(window.data.Manifest.bounds_m);
                window.ExportPreview(Path.Combine(output,"unity-overview.png"));
                window.Fit(window.data.Catalog.regions.Single(r=>r.id=="badwater").bounds_m);
                window.ExportPreview(Path.Combine(output,"unity-chunks.png"));
                window.ExportPreview(Path.Combine(output,"unity-chunks-top.png"));
                Debug.Log("Death Valley developer map preview export passed.");
            }
            finally{DestroyImmediate(window);}
        }
        private void ExportPreview(string path)
        {
            var rect=new Rect(0,0,1200,850);ConfigureCamera(rect);
            Texture texture=Render(rect);
            RenderTexture old=RenderTexture.active;
            var copy=new Texture2D(1200,850,TextureFormat.RGB24,false);
            try
            {
                var rendered=(RenderTexture)texture;RenderTexture.active=rendered;
                copy.ReadPixels(rect,0,0);copy.Apply();
                // The preview's linear render target needs display encoding for PNG.
                // The Editor GUI handles this conversion when drawing the live preview.
                if(QualitySettings.activeColorSpace==ColorSpace.Linear && !rendered.sRGB)
                {
                    Color[] pixels=copy.GetPixels();
                    for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
                    copy.SetPixels(pixels);copy.Apply();
                }
                File.WriteAllBytes(path,copy.EncodeToPNG());
            }
            finally{RenderTexture.active=old;DestroyImmediate(copy);}
        }
    }
}
