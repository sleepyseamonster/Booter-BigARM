// Pipeline eval_file. Update only the existing paving asset, preserving GUIDs and user scenes.
var root=System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
var folder="Assets/_Project/Art/Environment/Ground/FlatStoneGroundPatch01";
var path="Assets/_Project/Prefabs/TopDown3D/Environment/FlatStoneGroundPatch01.prefab";
if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Requires Edit Mode.");
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var dirty=active.isDirty;
var receipt=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(root,"SourceArt/Blender/Studies/BooterGroundSetup/FlatStoneGroundPatch01/repair01.json")));
foreach(var suffix in new[]{"BaseColor","Normal","MetallicSmoothness"})
{
    var texturePath=folder+"/FlatStoneGroundPatch01_Limestone_"+suffix+".png";
    UnityEditor.AssetDatabase.ImportAsset(texturePath,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var t=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(texturePath);
    var type=suffix=="Normal"?UnityEditor.TextureImporterType.NormalMap:UnityEditor.TextureImporterType.Default;
    var alpha=suffix=="MetallicSmoothness"?UnityEditor.TextureImporterAlphaSource.FromInput:UnityEditor.TextureImporterAlphaSource.None;
    if(t.textureType==type && t.sRGBTexture==(suffix=="BaseColor") && t.maxTextureSize==2048 &&
       t.npotScale==UnityEditor.TextureImporterNPOTScale.ToLarger && t.wrapMode==UnityEngine.TextureWrapMode.Repeat &&
       t.alphaSource==alpha && t.textureCompression==UnityEditor.TextureImporterCompression.CompressedHQ)continue;
    t.textureType=type;t.sRGBTexture=suffix=="BaseColor";t.maxTextureSize=2048;t.npotScale=UnityEditor.TextureImporterNPOTScale.ToLarger;
    t.mipmapEnabled=true;t.filterMode=UnityEngine.FilterMode.Trilinear;t.anisoLevel=4;t.wrapMode=UnityEngine.TextureWrapMode.Repeat;
    t.alphaSource=alpha;t.alphaIsTransparency=false;t.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
}
UnityEditor.AssetDatabase.ImportAsset(folder+"/FlatStoneGroundPatch01.fbx",UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/FlatStoneGroundPatch01.fbx");
var filter=model.GetComponentsInChildren<UnityEngine.MeshFilter>().Single();var transform=filter.transform.localToWorldMatrix;
var temporary=UnityEngine.Object.Instantiate(filter.sharedMesh);temporary.name="FlatStoneGroundPatch01_Runtime";
var v=temporary.vertices;var n=temporary.normals;var tspace=temporary.tangents;var normalTransform=transform.inverse.transpose;
for(var i=0;i<v.Length;i++)
{
    v[i]=transform.MultiplyPoint3x4(v[i]);n[i]=normalTransform.MultiplyVector(n[i]).normalized;
    var t=transform.MultiplyVector(new UnityEngine.Vector3(tspace[i].x,tspace[i].y,tspace[i].z)).normalized;
    tspace[i]=new UnityEngine.Vector4(t.x,t.y,t.z,tspace[i].w*UnityEngine.Mathf.Sign(transform.determinant));
}
temporary.vertices=v;temporary.normals=n;temporary.tangents=tspace;temporary.RecalculateBounds();
if(temporary.subMeshCount!=2 || temporary.triangles.Length/3!=8000 ||
   UnityEngine.Mathf.Abs(temporary.bounds.min.y+.025f)>.001f)throw new System.InvalidOperationException("Repaired import contract failed.");
var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(folder+"/FlatStoneGroundPatch01_Runtime.asset");
mesh.Clear();mesh.indexFormat=temporary.indexFormat;
mesh.vertices=temporary.vertices;mesh.normals=temporary.normals;mesh.tangents=temporary.tangents;
mesh.uv=temporary.uv;mesh.uv2=temporary.uv2;mesh.subMeshCount=temporary.subMeshCount;
for(var submesh=0;submesh<temporary.subMeshCount;submesh++)mesh.SetTriangles(temporary.GetTriangles(submesh),submesh,false);
mesh.RecalculateBounds();UnityEditor.EditorUtility.SetDirty(mesh);UnityEngine.Object.DestroyImmediate(temporary);
if(mesh.triangles.Length/3!=8000 || mesh.subMeshCount!=2)throw new System.InvalidOperationException("Runtime mesh update did not retain repaired geometry.");
var shader=UnityEngine.Shader.Find("Universal Render Pipeline/Lit");
var ground=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(folder+"/FlatStoneGroundPatch01.mat");
ground.name="FlatStoneGroundPatch01 - base";
foreach(var property in new[]{"_BaseMap","_BumpMap","_OcclusionMap","_MetallicGlossMap"})ground.SetTexture(property,null);
foreach(var keyword in new[]{"_NORMALMAP","_OCCLUSIONMAP","_METALLICSPECGLOSSMAP"})ground.DisableKeyword(keyword);
var c=receipt["ground_color_linear"].ToObject<float[]>();ground.SetColor("_BaseColor",new UnityEngine.Color(c[0],c[1],c[2],c[3]).gamma);
ground.SetFloat("_Smoothness",1-(float)receipt["ground_roughness"]);ground.SetFloat("_Metallic",0);UnityEditor.EditorUtility.SetDirty(ground);
var stonePath=folder+"/FlatStoneGroundPatch01_Limestone.mat";
var stone=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(stonePath);
if(stone==null){stone=new UnityEngine.Material(shader){name="FlatStoneGroundPatch01 - approved white limestone"};UnityEditor.AssetDatabase.CreateAsset(stone,stonePath);}
stone.SetColor("_BaseColor",UnityEngine.Color.white);stone.SetFloat("_Metallic",0);stone.SetFloat("_Smoothness",1);
stone.SetFloat("_SmoothnessTextureChannel",0);stone.SetFloat("_BumpScale",1);
stone.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/FlatStoneGroundPatch01_Limestone_BaseColor.png"));
stone.SetTexture("_BumpMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/FlatStoneGroundPatch01_Limestone_Normal.png"));
stone.SetTexture("_MetallicGlossMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/FlatStoneGroundPatch01_Limestone_MetallicSmoothness.png"));
stone.SetTexture("_OcclusionMap",null);stone.EnableKeyword("_NORMALMAP");stone.EnableKeyword("_METALLICSPECGLOSSMAP");stone.DisableKeyword("_OCCLUSIONMAP");
UnityEditor.EditorUtility.SetDirty(stone);UnityEditor.AssetDatabase.SaveAssetIfDirty(mesh);UnityEditor.AssetDatabase.SaveAssetIfDirty(ground);UnityEditor.AssetDatabase.SaveAssetIfDirty(stone);
var contents=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    contents.GetComponent<UnityEngine.MeshFilter>().sharedMesh=mesh;
    contents.GetComponent<UnityEngine.MeshRenderer>().sharedMaterials=new[]{ground,stone};
    contents.GetComponent<UnityEngine.MeshCollider>().sharedMesh=mesh;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents,path);
}
finally{UnityEditor.PrefabUtility.UnloadPrefabContents(contents);}
if(active!=UnityEngine.SceneManagement.SceneManager.GetActiveScene() || active.isDirty!=dirty)throw new System.InvalidOperationException("Active scene changed.");
var result=new{triangles=mesh.triangles.Length/3,vertices=mesh.vertexCount,materials=2,rootUnchanged=true,
    prefabGuid=UnityEditor.AssetDatabase.AssetPathToGUID(path),meshGuid=UnityEditor.AssetDatabase.AssetPathToGUID(folder+"/FlatStoneGroundPatch01_Runtime.asset"),
    activeSceneUnchanged=true,productionScenePlaced=false,gameplaySmokeTest=false};
var json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText(System.IO.Path.Combine(root,"Logs/FlatStoneGroundPatch01/Repair01/unity-update.json"),json+"\n");return json;
