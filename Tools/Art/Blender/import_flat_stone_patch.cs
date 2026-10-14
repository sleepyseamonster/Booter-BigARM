// Unity Pipeline eval_file: create only the task-owned static ground-patch assets.
// No production scene placement, save, Play Mode, settings change or window activation.
// Import settings are idempotent so a partial compression/import can resume.
// Completion/errors are recorded in Logs before any retry.
var taskRoot = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
try
{
var root = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
var progressPath = System.IO.Path.Combine(root, "Logs/FlatStoneGroundPatch01/unity-import-progress.txt");
System.IO.File.WriteAllText(progressPath, "Started scoped import\n");
var folder = "Assets/_Project/Art/Environment/Ground/FlatStoneGroundPatch01";
var prefabPath = "Assets/_Project/Prefabs/TopDown3D/Environment/FlatStoneGroundPatch01.prefab";
var materialPath = folder + "/FlatStoneGroundPatch01.mat";
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Requires Edit Mode.");
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath) != null || UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(materialPath) != null)
    throw new System.InvalidOperationException("Task assets already exist; inspect their receipt instead of overwriting.");
var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var wasDirty = active.isDirty;
foreach (var suffix in new[] { ".fbx", "_BaseColor.png", "_Normal.png", "_Occlusion.png", "_MetallicSmoothness.png" })
    if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(folder + "/FlatStoneGroundPatch01" + suffix) == null)
        UnityEditor.AssetDatabase.ImportAsset(folder + "/FlatStoneGroundPatch01" + suffix, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(folder + "/FlatStoneGroundPatch01.fbx");
var needsModelImport = importer.globalScale != 1f || !importer.bakeAxisConversion || importer.importAnimation || importer.importBlendShapes ||
    importer.importCameras || importer.importLights || importer.materialImportMode != UnityEditor.ModelImporterMaterialImportMode.None ||
    importer.importNormals != UnityEditor.ModelImporterNormals.Import || importer.importTangents != UnityEditor.ModelImporterTangents.CalculateMikk ||
    !importer.isReadable || !importer.generateSecondaryUV;
if (needsModelImport)
{
importer.globalScale = 1f;
importer.bakeAxisConversion = true;
importer.importAnimation = false;
importer.importBlendShapes = false;
importer.importCameras = false;
importer.importLights = false;
importer.materialImportMode = UnityEditor.ModelImporterMaterialImportMode.None;
importer.importNormals = UnityEditor.ModelImporterNormals.Import;
importer.importTangents = UnityEditor.ModelImporterTangents.CalculateMikk;
importer.isReadable = true; // Explicit structural readback and collider cooking; bounded static patch.
importer.generateSecondaryUV = true;
importer.SaveAndReimport();
}
foreach (var suffix in new[] { "BaseColor", "Normal", "Occlusion", "MetallicSmoothness" })
{
    var ti = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(folder + "/FlatStoneGroundPatch01_" + suffix + ".png");
    var needsTextureImport = ti.textureType != (suffix == "Normal" ? UnityEditor.TextureImporterType.NormalMap : UnityEditor.TextureImporterType.Default) ||
        ti.sRGBTexture != (suffix == "BaseColor") || ti.maxTextureSize != 4096 || !ti.mipmapEnabled || ti.wrapMode != UnityEngine.TextureWrapMode.Clamp ||
        ti.filterMode != UnityEngine.FilterMode.Trilinear || ti.anisoLevel != 4 || ti.alphaIsTransparency ||
        ti.alphaSource != (suffix == "MetallicSmoothness" ? UnityEditor.TextureImporterAlphaSource.FromInput : UnityEditor.TextureImporterAlphaSource.None) ||
        ti.textureCompression != UnityEditor.TextureImporterCompression.CompressedHQ;
    if (!needsTextureImport) continue;
    ti.textureType = suffix == "Normal" ? UnityEditor.TextureImporterType.NormalMap : UnityEditor.TextureImporterType.Default;
    ti.sRGBTexture = suffix == "BaseColor";
    ti.maxTextureSize = 4096;
    ti.mipmapEnabled = true;
    ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    ti.filterMode = UnityEngine.FilterMode.Trilinear;
    ti.anisoLevel = 4;
    ti.alphaSource = suffix == "MetallicSmoothness" ? UnityEditor.TextureImporterAlphaSource.FromInput : UnityEditor.TextureImporterAlphaSource.None;
    ti.alphaIsTransparency = false;
    ti.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
    ti.SaveAndReimport();
}
System.IO.File.WriteAllText(progressPath, "Import settings complete; normalize mesh\n");
var meshes = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(folder + "/FlatStoneGroundPatch01.fbx").OfType<UnityEngine.Mesh>().ToArray();
if (meshes.Length != 1 || meshes[0].subMeshCount != 1) throw new System.InvalidOperationException("Expected one mesh with one material slot.");
var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder + "/FlatStoneGroundPatch01.fbx");
var modelFilter = model.GetComponentsInChildren<UnityEngine.MeshFilter>().Single();
var sourceTransform = modelFilter.transform.localToWorldMatrix;
var mesh = UnityEngine.Object.Instantiate(meshes[0]);
mesh.name = "FlatStoneGroundPatch01_Runtime";
var positions = mesh.vertices;
var normals = mesh.normals;
var tangents = mesh.tangents;
var normalTransform = sourceTransform.inverse.transpose;
for (var i = 0; i < positions.Length; ++i)
{
    positions[i] = sourceTransform.MultiplyPoint3x4(positions[i]);
    normals[i] = normalTransform.MultiplyVector(normals[i]).normalized;
    var t = sourceTransform.MultiplyVector(new UnityEngine.Vector3(tangents[i].x, tangents[i].y, tangents[i].z)).normalized;
    tangents[i] = new UnityEngine.Vector4(t.x, t.y, t.z, tangents[i].w * UnityEngine.Mathf.Sign(sourceTransform.determinant));
}
mesh.vertices = positions; mesh.normals = normals; mesh.tangents = tangents;
if (sourceTransform.determinant < 0)
{
    var triangles = mesh.triangles;
    for (var i = 0; i < triangles.Length; i += 3) { var v = triangles[i]; triangles[i] = triangles[i+1]; triangles[i+1] = v; }
    mesh.triangles = triangles;
}
mesh.RecalculateBounds();
System.IO.File.WriteAllText(progressPath, "Normalized bounds: " + mesh.bounds.ToString() + "\n");
if (UnityEngine.Mathf.Abs(mesh.bounds.size.x - 9.051995f) > .001f || UnityEngine.Mathf.Abs(mesh.bounds.size.z - 7.370174f) > .001f || UnityEngine.Mathf.Abs(mesh.bounds.min.y + .025f) > .001f)
    throw new System.InvalidOperationException("Import scale, orientation or ground pivot does not match the authoring patch.");
var shader = UnityEngine.Shader.Find("Universal Render Pipeline/Lit");
if (shader == null) throw new System.InvalidOperationException("Production URP Lit shader unavailable.");
UnityEditor.AssetDatabase.CreateAsset(mesh, folder + "/FlatStoneGroundPatch01_Runtime.asset");
var material = new UnityEngine.Material(shader) { name = "FlatStoneGroundPatch01 - limestone paving atlas" };
material.SetFloat("_WorkflowMode", 1f);
material.SetColor("_BaseColor", UnityEngine.Color.white);
material.SetFloat("_Metallic", 0f);
material.SetFloat("_Smoothness", 1f);
material.SetFloat("_SmoothnessTextureChannel", 0f);
material.SetFloat("_BumpScale", 1f);
material.SetFloat("_OcclusionStrength", 1f);
material.SetTexture("_BaseMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder + "/FlatStoneGroundPatch01_BaseColor.png"));
material.SetTexture("_BumpMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder + "/FlatStoneGroundPatch01_Normal.png"));
material.SetTexture("_OcclusionMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder + "/FlatStoneGroundPatch01_Occlusion.png"));
material.SetTexture("_MetallicGlossMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder + "/FlatStoneGroundPatch01_MetallicSmoothness.png"));
material.EnableKeyword("_NORMALMAP");
material.EnableKeyword("_OCCLUSIONMAP");
material.EnableKeyword("_METALLICSPECGLOSSMAP");
UnityEditor.AssetDatabase.CreateAsset(material, materialPath);
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
UnityEngine.GameObject instance = null;
try
{
    instance = new UnityEngine.GameObject("FlatStoneGroundPatch01");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, preview);
    instance.transform.SetPositionAndRotation(UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
    instance.transform.localScale = UnityEngine.Vector3.one;
    instance.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
    instance.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial = material;
    var collider = instance.AddComponent<UnityEngine.MeshCollider>();
    collider.convex = false;
    collider.sharedMesh = mesh;
    instance.isStatic = true;
    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(root, "Assets/_Project/Prefabs/TopDown3D/Environment"));
    UnityEditor.AssetDatabase.ImportAsset("Assets/_Project/Prefabs/TopDown3D/Environment", UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    bool success;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out success);
    if (!success) throw new System.InvalidOperationException("Prefab save failed.");
    // Structural collider readback only: no simulation or gameplay test.
    UnityEngine.RaycastHit hit;
    if (!collider.Raycast(new UnityEngine.Ray(new UnityEngine.Vector3(0, 2, 0), UnityEngine.Vector3.down), out hit, 3f))
        throw new System.InvalidOperationException("Static patch collider did not cook correctly.");
    var receipt = new {
        editor = UnityEngine.Application.unityVersion, prefab = prefabPath, material = materialPath,
        meshVertices = mesh.vertexCount, meshTriangles = mesh.triangles.Length / 3, subMeshes = mesh.subMeshCount,
        boundsMin = new[] { mesh.bounds.min.x, mesh.bounds.min.y, mesh.bounds.min.z },
        boundsSize = new[] { mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z },
        collider = "Static non-convex MeshCollider; no Rigidbody", colliderRayHeight = hit.point.y,
        prefabGuid = UnityEditor.AssetDatabase.AssetPathToGUID(prefabPath), modelGuid = UnityEditor.AssetDatabase.AssetPathToGUID(folder + "/FlatStoneGroundPatch01.fbx"),
        activeSceneUnchanged = active == UnityEngine.SceneManagement.SceneManager.GetActiveScene() && active.isDirty == wasDirty,
        productionScenePlaced = false, gameplaySmokeTest = false
    };
    var json = Newtonsoft.Json.JsonConvert.SerializeObject(receipt, Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText(System.IO.Path.Combine(root, "Logs/FlatStoneGroundPatch01/unity-import.json"), json + "\n");
    System.IO.File.WriteAllText(progressPath, "Completed\n");
    UnityEngine.Debug.Log("FlatStoneGroundPatch01 import completed; proof: Logs/FlatStoneGroundPatch01/unity-import.json");
    return json;
}
finally
{
    if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
    if (active != UnityEngine.SceneManagement.SceneManager.GetActiveScene() || active.isDirty != wasDirty)
        throw new System.InvalidOperationException("The active user scene changed during asset preparation.");
}
}
catch (System.Exception error)
{
    System.IO.File.WriteAllText(System.IO.Path.Combine(taskRoot, "Logs/FlatStoneGroundPatch01/unity-import-error.txt"), error.ToString());
    UnityEngine.Debug.LogError("FlatStoneGroundPatch01 import failed: " + error.Message);
    throw;
}
