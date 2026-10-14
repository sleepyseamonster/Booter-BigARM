// Read-only fresh asset/reference checks through Unity Pipeline eval_file.
var root = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
var folder = "Assets/_Project/Art/Environment/Ground/FlatStoneGroundPatch01";
var path = "Assets/_Project/Prefabs/TopDown3D/Environment/FlatStoneGroundPatch01.prefab";
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
if (prefab == null || prefab.transform.childCount != 0 || prefab.transform.localPosition != UnityEngine.Vector3.zero || prefab.transform.localRotation != UnityEngine.Quaternion.identity || prefab.transform.localScale != UnityEngine.Vector3.one)
    throw new System.InvalidOperationException("Expected one root with normalized placement transforms.");
var filter = prefab.GetComponent<UnityEngine.MeshFilter>();
var renderer = prefab.GetComponent<UnityEngine.MeshRenderer>();
var collider = prefab.GetComponent<UnityEngine.MeshCollider>();
if (filter == null || renderer == null || collider == null || collider.convex || collider.sharedMesh != filter.sharedMesh || prefab.GetComponent<UnityEngine.Rigidbody>() != null || !prefab.isStatic)
    throw new System.InvalidOperationException("Prefab component or static collision contract failed.");
var mesh = filter.sharedMesh;
if (mesh.subMeshCount != 1 || renderer.sharedMaterials.Length != 1 || mesh.triangles.Length / 3 != 5500 || mesh.uv.Length != mesh.vertexCount || mesh.uv2.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount || mesh.tangents.Length != mesh.vertexCount)
    throw new System.InvalidOperationException("Runtime mesh geometry/UV/shading contract failed.");
var material = renderer.sharedMaterial;
if (material == null || material.shader.name != "Universal Render Pipeline/Lit" || !material.IsKeywordEnabled("_NORMALMAP") || !material.IsKeywordEnabled("_METALLICSPECGLOSSMAP") || !material.IsKeywordEnabled("_OCCLUSIONMAP"))
    throw new System.InvalidOperationException("URP material or shader keywords missing.");
foreach (var entry in new[] { new[]{"BaseColor","_BaseMap"}, new[]{"Normal","_BumpMap"}, new[]{"Occlusion","_OcclusionMap"}, new[]{"MetallicSmoothness","_MetallicGlossMap"} })
{
    var texturePath = folder + "/FlatStoneGroundPatch01_" + entry[0] + ".png";
    var texture = material.GetTexture(entry[1]);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(texturePath);
    if (texture == null || UnityEditor.AssetDatabase.GetAssetPath(texture) != texturePath || texture.width != 4096 || texture.height != 4096 || importer.sRGBTexture != (entry[0]=="BaseColor") || !importer.mipmapEnabled || importer.wrapMode != UnityEngine.TextureWrapMode.Clamp)
        throw new System.InvalidOperationException("Atlas reference/import contract failed: " + entry[0]);
    if (entry[0]=="Normal" && importer.textureType != UnityEditor.TextureImporterType.NormalMap)
        throw new System.InvalidOperationException("Normal texture has wrong import type.");
}
var shaderErrors = UnityEditor.ShaderUtil.GetShaderMessages(material.shader).Where(m=>m.severity.ToString()=="Error").ToArray();
if (shaderErrors.Length != 0) throw new System.InvalidOperationException("URP shader has import/compiler errors.");
var dependencies = UnityEditor.AssetDatabase.GetDependencies(path, true);
var ownedPaths = dependencies.Where(p=>p.StartsWith(folder + "/") || p == path).ToArray();
var guids = ownedPaths.Select(p=>UnityEditor.AssetDatabase.AssetPathToGUID(p)).ToArray();
if (guids.Any(g=>string.IsNullOrEmpty(g)) || guids.Distinct().Count()!=guids.Length)
    throw new System.InvalidOperationException("Missing or duplicate task asset GUIDs.");
var report = new { prefab=path, editor=UnityEngine.Application.unityVersion, rendererCount=1, materials=1,
    meshPath=UnityEditor.AssetDatabase.GetAssetPath(mesh), vertices=mesh.vertexCount, triangles=mesh.triangles.Length/3,
    unitRoot=true, staticCollider=true, lightmapUV=true, shaderErrors=0, dependencies=ownedPaths,
    gameplaySmokeTest=false, activeScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };
var json = Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText(System.IO.Path.Combine(root,"Logs/FlatStoneGroundPatch01/unity-verification.json"),json+"\n");
return json;
