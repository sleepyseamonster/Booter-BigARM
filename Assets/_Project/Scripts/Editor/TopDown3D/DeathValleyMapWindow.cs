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
        private Mesh relief, overviewLines, chunkLines, studyLines, candidateLines, selectedLines;
        private Vector3[] pickVertices;
        private int[] pickIndices;
        private string error;
        private string chunkQuery="r12_c01";
        [SerializeField] private string selectedKey="badwater";
        [SerializeField] private float exaggeration=3, pitch=55, yaw=-25, viewSize=145;
        [SerializeField] private Vector3 target;
        [SerializeField] private bool showOverview=true, showChunks=true, showStudies=false, showCandidates=true;
        private Vector2 scroll;
        private DeathValleyMapRecord[] pointMatches=Array.Empty<DeathValleyMapRecord>();
        private double[] pickedPoint;
        private bool dragging;
        private int dragButton;

        [MenuItem("Booter & BigARM/Death Valley/Developer Map")]
        public static void Open() => GetWindow<DeathValleyMapWindow>("Death Valley Map");

        private void OnEnable()
        {
            minSize=new Vector2(850,550);
            Reload();
        }
        private void OnDisable() => Release();
        private void Release()
        {
            preview?.Cleanup();preview=null;
            foreach(var item in new UnityEngine.Object[]{material,relief,overviewLines,chunkLines,studyLines,candidateLines,selectedLines})
                if(item!=null) DestroyImmediate(item);
            material=null;relief=overviewLines=chunkLines=studyLines=candidateLines=selectedLines=null;
        }
        private void Reload()
        {
            Release();error=null;data=null;pointMatches=Array.Empty<DeathValleyMapRecord>();pickedPoint=null;
            try
            {
                data=new DeathValleyMapData(Path.GetDirectoryName(Application.dataPath));
                preview=new PreviewRenderUtility();
                var shader=Shader.Find("Hidden/Internal-Colored");
                if(shader==null) throw new InvalidOperationException("Editor vertex-color preview shader is unavailable.");
                material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
                material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.Zero);
                material.SetInt("_Cull",(int)CullMode.Off);material.SetInt("_ZWrite",1);material.SetInt("_ZTest",(int)CompareFunction.LessEqual);
                Rebuild();
            }
            catch(Exception exception){error=exception.Message;Release();data=null;}
        }
        private DeathValleyMapRecord Selected => data?.AllRecords.FirstOrDefault(r=>r.Key==selectedKey);
        private void Rebuild()
        {
            foreach(var mesh in new[]{relief,overviewLines,chunkLines,studyLines,candidateLines,selectedLines})
                if(mesh!=null) DestroyImmediate(mesh);
            relief=data.BuildRelief(exaggeration);pickVertices=relief.vertices;pickIndices=relief.triangles;
            overviewLines=Lines(data.Catalog.regional_source_tiles,_=>new Color(.48f,.57f,.58f));
            chunkLines=Lines(data.Catalog.unity_tiles,r=>r.source_spacing_m==1 ? new Color(1,.82f,.17f) : new Color(.1f,.88f,.98f));
            studyLines=Lines(data.Catalog.regions.Where(r=>r.id!="expanded_region" && r.id!="original_region"),_=>new Color(.52f,.9f,.42f));
            candidateLines=Lines(data.Catalog.expansion_candidates,_=>new Color(1,.48f,.15f));
            selectedLines=Lines(Selected==null ? Array.Empty<DeathValleyMapRecord>() : new[]{Selected},_=>Color.white);
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
                    Vector3[] points=data.EdgeVertices(ax,az,bx,bz,exaggeration);
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
        private void Select(DeathValleyMapRecord record)
        {
            selectedKey=record.Key;
            if(selectedLines!=null)DestroyImmediate(selectedLines);
            selectedLines=Lines(new[]{record},_=>Color.white);Repaint();
        }
        private void Fit(double[] b)
        {
            double east=(b[0]+b[2])/2,north=(b[1]+b[3])/2;
            target=data.Project(east,north,data.SurfaceHeight(east,north),exaggeration);
            viewSize=Mathf.Max(.25f,(float)Math.Max(b[2]-b[0],b[3]-b[1])/1000*.7f);Repaint();
        }
        private void OnGUI()
        {
            using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if(GUILayout.Button("Reload inventory",EditorStyles.toolbarButton))Reload();
                if(data!=null)
                {
                    if(GUILayout.Button("Entire region",EditorStyles.toolbarButton))Fit(data.Manifest.bounds_m);
                    if(GUILayout.Button("Greater Wasteland",EditorStyles.toolbarButton))Fit(data.Catalog.regions.Single(r=>r.id=="badwater").bounds_m);
                    if(GUILayout.Button("Top view",EditorStyles.toolbarButton)){pitch=90;yaw=0;}
                    if(GUILayout.Button("Oblique",EditorStyles.toolbarButton)){pitch=55;yaw=-25;}
                }
                GUILayout.FlexibleSpace();GUILayout.Label("Development only",EditorStyles.miniLabel);
            }
            if(data==null){EditorGUILayout.HelpBox(error??"Map unavailable",MessageType.Error);return;}
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Min(320,position.width*.36f))))DrawDetails();
                using(new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("Death Valley and surrounding ranges",EditorStyles.boldLabel);
                    Rect rect=GUILayoutUtility.GetRect(100,100,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
                    ConfigureCamera(rect);HandleMapInput(rect);
                    if(Event.current.type==EventType.Repaint)
                    {
                        GUI.DrawTexture(rect,Render(rect),ScaleMode.StretchToFill,false);
                        DrawCompass(rect);
                    }
                    GUILayout.Label("Left click: inspect   Right drag: orbit   Middle drag: pan   Wheel: zoom",EditorStyles.miniLabel);
                    GUILayout.Label("North = +Z · East = +X · EPSG:26911 · preview coordinates scaled to kilometres",EditorStyles.miniLabel);
                }
            }
        }
        private void DrawDetails()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("Coverage layers",EditorStyles.boldLabel);
            showOverview=EditorGUILayout.ToggleLeft("Grey: 32 km overview footprints",showOverview);
            showChunks=EditorGUILayout.ToggleLeft("Cyan: built 256 m chunks; yellow: 1 m sources",showChunks);
            showCandidates=EditorGUILayout.ToggleLeft("Orange: proposed adjoining batches",showCandidates);
            showStudies=EditorGUILayout.ToggleLeft("Green: Blender study footprints",showStudies);
            EditorGUI.BeginChangeCheck();float scale=EditorGUILayout.Slider("Relief scale",exaggeration,1,10);
            if(EditorGUI.EndChangeCheck()){exaggeration=scale;Rebuild();}
            GUILayout.Label("Display relief: 800 m mesh / 200 m samples",EditorStyles.miniLabel);
            EditorGUILayout.Space();GUILayout.Label("Find a segment",EditorStyles.boldLabel);
            var choices=data.Catalog.regions.Concat(data.Catalog.expansion_candidates).ToArray();
            int current=Array.FindIndex(choices,r=>r.Key==selectedKey);
            int choice=EditorGUILayout.Popup(current+1,new[]{"Choose a footprint..."}.Concat(choices.Select(r=>r.Label)).ToArray());
            if(choice!=current+1 && choice>0){Select(choices[choice-1]);Fit(choices[choice-1].bounds_m);}
            chunkQuery=EditorGUILayout.TextField("Chunk ID",chunkQuery);
            if(GUILayout.Button("Find built chunk"))
            {
                var chunk=data.Catalog.unity_tiles.FirstOrDefault(r=>string.Equals(r.Key,chunkQuery.Trim(),StringComparison.OrdinalIgnoreCase));
                if(chunk==null)ShowNotification(new GUIContent("Use an inventory chunk ID, for example r12_c01."));
                else{Select(chunk);Fit(chunk.bounds_m);}
            }
            var selected=Selected;
            if(selected!=null)
            {
                EditorGUILayout.Space();GUILayout.Label(selected.Label,EditorStyles.boldLabel);
                double[] b=selected.bounds_m;
                EditorGUILayout.SelectableLabel($"E {b[0]:F0} to {b[2]:F0}\nN {b[1]:F0} to {b[3]:F0}",GUILayout.Height(36));
                GUILayout.Label($"Extent: {b[2]-b[0]:N0} × {b[3]-b[1]:N0} m");
                float spacing=selected.source_spacing_m>0?selected.source_spacing_m:selected.spacing_m;
                if(spacing>0)GUILayout.Label($"Recorded source spacing: {spacing} m");
                bool built=!string.IsNullOrEmpty(selected.legacy_id);
                if(built)GUILayout.Label("Quarter: "+selected.quarter);
                string state=built ? "Built Unity chunk (retained export inventory)" : selected.Key.StartsWith("candidate_") ? "Proposed; not selected or imported" : "Configured study footprint; not a built game slice";
                EditorGUILayout.HelpBox(state,MessageType.Info);
                if(!string.IsNullOrEmpty(selected.geographic_key))EditorGUILayout.SelectableLabel(selected.geographic_key,GUILayout.Height(36));
                Vector2 local=data.UnityPosition((b[0]+b[2])/2,(b[1]+b[3])/2);
                GUILayout.Label($"Unity centre X {local.x:F0}, Z {local.y:F0} m");
                if(GUILayout.Button("Zoom to selection"))Fit(b);
                if(GUILayout.Button("Copy coordinates and identity"))EditorGUIUtility.systemCopyBuffer=$"{selected.Key}\n{selected.geographic_key}\nEPSG:26911 [{string.Join(", ",b)}]\nUnity centre X={local.x}, Z={local.y}";
                using(new EditorGUI.DisabledScope(!built||EditorApplication.isPlaying))
                    if(GUILayout.Button("Frame selected terrain in loaded scene"))FrameTerrain(selected);
            }
            if(pickedPoint!=null)
            {
                EditorGUILayout.Space();GUILayout.Label("Clicked map position",EditorStyles.boldLabel);
                Vector2 local=data.UnityPosition(pickedPoint[0],pickedPoint[1]);
                GUILayout.Label($"E {pickedPoint[0]:F1} · N {pickedPoint[1]:F1}");
                GUILayout.Label($"Unity X {local.x:F1} · Z {local.y:F1}");
                GUILayout.Label("All footprints at this position",EditorStyles.miniBoldLabel);
                foreach(var record in pointMatches)
                    if(GUILayout.Button(record.Label,EditorStyles.miniButton))Select(record);
            }
            EditorGUILayout.Space();EditorGUILayout.HelpBox("New coarse USGS relief snapshot at the Blender overview bounds. It shows context, not detailed game terrain or collision. Reload after inventory refresh or file moves.",MessageType.None);
            GUILayout.Label("Source acquired: "+data.Manifest.retrieved_utc,EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label("Coverage audited: "+data.Catalog.audited_utc,EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }
        private void ConfigureCamera(Rect rect)
        {
            var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=viewSize;
            camera.aspect=Mathf.Max(.1f,rect.width/Mathf.Max(1,rect.height));camera.nearClipPlane=.01f;camera.farClipPlane=3000;
            camera.allowHDR=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.09f,.11f);
            camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);
            camera.transform.position=target-camera.transform.forward*Mathf.Max(50,viewSize*4);
        }
        private void DrawCompass(Rect rect)
        {
            Vector2 centre=new Vector2(rect.xMax-55,rect.y+55);
            Handles.BeginGUI();
            try
            {
                foreach(var axis in new[]{(Vector3.forward,"N",Color.white),(Vector3.right,"E",new Color(.3f,.9f,1))})
                {
                    Vector3 direction=preview.camera.transform.InverseTransformDirection(axis.Item1);
                    Vector2 end=centre+new Vector2(direction.x,-direction.y)*32;
                    Handles.color=axis.Item3;Handles.DrawAAPolyLine(2,new Vector3(centre.x,centre.y),new Vector3(end.x,end.y));
                    GUI.Label(new Rect(end.x-5,end.y-18,25,20),axis.Item2,EditorStyles.whiteMiniLabel);
                }
            }
            finally{Handles.EndGUI();}
        }
        private Texture Render(Rect rect)
        {
            preview.BeginPreview(rect,GUIStyle.none);
            Texture result=null;
            try
            {
                preview.DrawMesh(relief,Matrix4x4.identity,material,0);
                if(showOverview)preview.DrawMesh(overviewLines,Matrix4x4.identity,material,0);
                if(showStudies)preview.DrawMesh(studyLines,Matrix4x4.identity,material,0);
                if(showCandidates)preview.DrawMesh(candidateLines,Matrix4x4.identity,material,0);
                if(showChunks)preview.DrawMesh(chunkLines,Matrix4x4.identity,material,0);
                preview.DrawMesh(selectedLines,Matrix4x4.identity,material,0);
                preview.Render(false);
            }
            finally { result=preview.EndPreview(); }
            return result;
        }
        private void HandleMapInput(Rect rect)
        {
            Event e=Event.current;
            int control=GUIUtility.GetControlID(FocusType.Passive);
            if(e.type==EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                if(e.button==0)
                {
                    Vector2 p=e.mousePosition-rect.position;
                    Ray ray=preview.camera.ViewportPointToRay(new Vector3(p.x/rect.width,1-p.y/rect.height,0));
                    if(DeathValleyMapData.Pick(ray,pickVertices,pickIndices,out Vector3 point))
                    {
                        pickedPoint=data.Unproject(point);
                        var hits=data.AllRecords.Where(r=>r.Contains(pickedPoint[0],pickedPoint[1])).ToArray();
                        pointMatches=hits.OrderBy(r=>(r.bounds_m[2]-r.bounds_m[0])*(r.bounds_m[3]-r.bounds_m[1])).ToArray();
                        var preferred=(showChunks?data.Catalog.unity_tiles:Array.Empty<DeathValleyMapRecord>())
                            .Concat(showCandidates?data.Catalog.expansion_candidates:Array.Empty<DeathValleyMapRecord>())
                            .Concat(showOverview?data.Catalog.regional_source_tiles:Array.Empty<DeathValleyMapRecord>())
                            .Concat(showStudies?data.Catalog.regions:Array.Empty<DeathValleyMapRecord>())
                            .FirstOrDefault(r=>r.Contains(pickedPoint[0],pickedPoint[1]));
                        if(preferred!=null){Select(preferred);if(e.clickCount==2)Fit(preferred.bounds_m);}
                    }
                }
                else if(e.button==1||e.button==2){dragging=true;dragButton=e.button;GUIUtility.hotControl=control;}
                e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag && dragging && GUIUtility.hotControl==control)
            {
                if(dragButton==1){yaw+=e.delta.x*.4f;pitch=Mathf.Clamp(pitch+e.delta.y*.4f,10,90);}
                else target+=(-preview.camera.transform.right*e.delta.x+preview.camera.transform.up*e.delta.y)*(viewSize*2/Mathf.Max(1,rect.height));
                e.Use();Repaint();
            }
            if(e.type==EventType.MouseUp && dragging){dragging=false;GUIUtility.hotControl=0;e.Use();}
            if(e.type==EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {viewSize=Mathf.Clamp(viewSize*Mathf.Exp(e.delta.y*.08f),.1f,500);e.Use();Repaint();}
        }
        private void FrameTerrain(DeathValleyMapRecord record)
        {
            string scenePath=AssetDatabase.GUIDToAssetPath(data.Manifest.scene_guid);
            if(string.IsNullOrEmpty(scenePath)){ShowNotification(new GUIContent("The catalog scene GUID is unavailable. Refresh the inventory."));return;}
            var matches=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(t=>t.gameObject.scene.path==scenePath && t.name.EndsWith("_"+record.legacy_id,StringComparison.Ordinal)).ToArray();
            if(matches.Length!=1){ShowNotification(new GUIContent("Load Greater Wasteland; expected exactly one matching terrain."));return;}
            Vector2 lower=data.UnityPosition(record.bounds_m[0],record.bounds_m[1]);
            if(Mathf.Abs(matches[0].transform.position.x-lower.x)>.01f || Mathf.Abs(matches[0].transform.position.z-lower.y)>.01f)
            {ShowNotification(new GUIContent("Terrain position disagrees with catalog. Refresh and review coordinates."));return;}
            Selection.activeGameObject=matches[0].gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
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
                window.pitch=90;window.yaw=0;
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
            try{RenderTexture.active=(RenderTexture)texture;copy.ReadPixels(rect,0,0);copy.Apply();File.WriteAllBytes(path,copy.EncodeToPNG());}
            finally{RenderTexture.active=old;DestroyImmediate(copy);}
        }
    }
}
