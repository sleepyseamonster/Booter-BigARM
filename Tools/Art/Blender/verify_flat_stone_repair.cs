// Read-only prefab/reference checks plus an isolated collider-solid check.
var root=System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
var path="Assets/_Project/Prefabs/TopDown3D/Environment/FlatStoneGroundPatch01.prefab";
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
var mesh=prefab.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
var renderer=prefab.GetComponent<UnityEngine.MeshRenderer>();
if(mesh.triangles.Length/3!=8000 || mesh.subMeshCount!=2 || renderer.sharedMaterials.Length!=2 ||
   mesh.uv.Length!=mesh.vertexCount || mesh.uv2.Length!=mesh.vertexCount || mesh.normals.Length!=mesh.vertexCount || mesh.tangents.Length!=mesh.vertexCount ||
   prefab.transform.childCount!=0 || prefab.transform.localPosition!=UnityEngine.Vector3.zero ||
   prefab.transform.localScale!=UnityEngine.Vector3.one || prefab.transform.localRotation!=UnityEngine.Quaternion.identity)
    throw new System.InvalidOperationException("Repaired prefab structure failed.");
foreach(var material in renderer.sharedMaterials)
{
    if(material==null || material.shader.name!="Universal Render Pipeline/Lit" ||
       UnityEditor.ShaderUtil.GetShaderMessages(material.shader).Any(m=>m.severity.ToString()=="Error"))
       throw new System.InvalidOperationException("Material/shader verification failed.");
}
var stone=renderer.sharedMaterials[1];
if(!stone.IsKeywordEnabled("_NORMALMAP") || !stone.IsKeywordEnabled("_METALLICSPECGLOSSMAP"))
    throw new System.InvalidOperationException("Limestone shader keywords missing.");
foreach(var entry in new[]{new[]{"BaseColor","_BaseMap"},new[]{"Normal","_BumpMap"},new[]{"MetallicSmoothness","_MetallicGlossMap"}})
{
    var texturePath="Assets/_Project/Art/Environment/Ground/FlatStoneGroundPatch01/FlatStoneGroundPatch01_Limestone_"+entry[0]+".png";
    var texture=stone.GetTexture(entry[1]);var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(texturePath);
    if(texture==null || UnityEditor.AssetDatabase.GetAssetPath(texture)!=texturePath || texture.width!=2048 || texture.height!=2048 ||
       importer.sRGBTexture!=(entry[0]=="BaseColor") || !importer.mipmapEnabled || importer.wrapMode!=UnityEngine.TextureWrapMode.Repeat ||
       (entry[0]=="Normal" && importer.textureType!=UnityEditor.TextureImporterType.NormalMap))
        throw new System.InvalidOperationException("Limestone texture/import contract failed: "+entry[0]);
}
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var dirty=active.isDirty;
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var instance=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance,scene);
try
{
    var collider=instance.GetComponent<UnityEngine.MeshCollider>();
    if(collider.convex || collider.sharedMesh!=mesh || instance.GetComponent<UnityEngine.Rigidbody>()!=null)throw new System.InvalidOperationException("Static collider contract failed.");
    var misses=0;
    for(var y=0;y<24;y++)for(var x=0;x<32;x++)
    {
        var p=new UnityEngine.Vector3(UnityEngine.Mathf.Lerp(mesh.bounds.min.x,mesh.bounds.max.x,(x+.5f)/32),1,
            UnityEngine.Mathf.Lerp(mesh.bounds.min.z,mesh.bounds.max.z,(y+.5f)/24));
        UnityEngine.RaycastHit hit;if(!collider.Raycast(new UnityEngine.Ray(p,UnityEngine.Vector3.down),out hit,2))misses++;
    }
    if(misses!=0)throw new System.InvalidOperationException("Collider coverage misses: "+misses);
    var result=new{triangles=8000,nativeVertices=4002,runtimeVertices=mesh.vertexCount,materials=2,colliderSamples=768,colliderMisses=0,
        prefabGuid=UnityEditor.AssetDatabase.AssetPathToGUID(path),meshGuid=UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(mesh)),
        originalPrefabGuidPreserved=UnityEditor.AssetDatabase.AssetPathToGUID(path)=="31bbd4ca1edca0347abcda68670c0bd4",
        rootNormalized=true,shaderErrors=0,gameplaySmokeTest=false};
    if(!result.originalPrefabGuidPreserved || result.meshGuid!="9d880fa03e73ac84f98e9214ef5903cc")throw new System.InvalidOperationException("Prefab/mesh identity changed.");
    var json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText(System.IO.Path.Combine(root,"Logs/FlatStoneGroundPatch01/Repair01/unity-verification.json"),json+"\n");return json;
}
finally
{
    UnityEngine.Object.DestroyImmediate(instance);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
    if(active!=UnityEngine.SceneManagement.SceneManager.GetActiveScene() || active.isDirty!=dirty)throw new System.InvalidOperationException("User scene changed.");
}
