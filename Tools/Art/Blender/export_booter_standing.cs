// Unity Pipeline eval_file script. Exports only a disposable preview instance.
// Does not open, save, or modify any production scene or asset.
var root = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
var output = System.IO.Path.Combine(root, "Logs", "BooterGroundSetup", "booter_standing.json");
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before exporting.");
if (System.IO.File.Exists(output)) throw new System.InvalidOperationException("Snapshot already exists; preserve it before a new export.");
var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var wasDirty = active.isDirty;
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
UnityEngine.GameObject instance = null;
var graph = UnityEngine.Playables.PlayableGraph.Create("Booter Blender standing snapshot");
try
{
    var path = "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/defaultmale_rig.fbx";
    var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    instance = UnityEngine.Object.Instantiate(model);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, preview);
    instance.transform.SetPositionAndRotation(UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
    instance.transform.localScale = UnityEngine.Vector3.one * 1.4f;
    var animator = instance.GetComponentInChildren<UnityEngine.Animator>();
    animator.applyRootMotion = false;
    animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<BooterBigArm.TopDown3D.TopDown3DLocomotionClipProfile>("Assets/_Project/Settings/Player/TopDown3DLocomotionClipProfile.asset");
    var clip = profile.GetRequiredEntry(BooterBigArm.TopDown3D.TopDown3DLocomotionClipRole.Idle).Clip;
    graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
    var playable = UnityEngine.Animations.AnimationClipPlayable.Create(graph, clip);
    playable.SetApplyFootIK(false);
    var animationOutput = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "Standing", animator);
    UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(animationOutput, playable);
    graph.Play();
    graph.Evaluate(0.1f);
    var camp = UnityEditor.AssetDatabase.LoadAssetAtPath<BooterBigArm.TopDown3D.TopDown3DCampSettings>("Assets/_Project/Settings/Camping/Resources/TopDown3DCampSettings.asset");
    var cloakProperty = new UnityEditor.SerializedObject(camp).FindProperty("wornCloakPrefab");
    var cloak = UnityEngine.Object.Instantiate((UnityEngine.GameObject)cloakProperty.objectReferenceValue, instance.transform, false);
    var meshes = new System.Collections.Generic.List<object>();
    foreach (var renderer in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
    {
        var mesh = new UnityEngine.Mesh();
        try
        {
            // Compensate renderer scale before TransformPoint applies it once.
            // false double-applies the 1.4 character scale in this hierarchy.
            renderer.BakeMesh(mesh, true);
            var vertices = mesh.vertices.Select(v => renderer.transform.TransformPoint(v)).Select(v => new[] { v.x, v.y, v.z }).ToArray();
            var parts = new System.Collections.Generic.List<object>();
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var material = renderer.sharedMaterials[System.Math.Min(i, renderer.sharedMaterials.Length - 1)];
                var color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
                parts.Add(new { triangles = mesh.GetTriangles(i), material = material.name, color = new[] { color.r, color.g, color.b, color.a } });
            }
            meshes.Add(new { name = renderer.name, vertices, uv = mesh.uv.Select(v => new[] { v.x, v.y }).ToArray(), parts });
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
    }
    foreach (var filter in cloak.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
    {
        var mesh = filter.sharedMesh;
        var renderer = filter.GetComponent<UnityEngine.MeshRenderer>();
        var vertices = mesh.vertices.Select(v => filter.transform.TransformPoint(v)).Select(v => new[] { v.x, v.y, v.z }).ToArray();
        var parts = new System.Collections.Generic.List<object>();
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            var material = renderer.sharedMaterials[System.Math.Min(i, renderer.sharedMaterials.Length - 1)];
            var color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            parts.Add(new { triangles = mesh.GetTriangles(i), material = material.name, color = new[] { color.r, color.g, color.b, color.a } });
        }
        meshes.Add(new { name = filter.name, vertices, uv = mesh.uv.Select(v => new[] { v.x, v.y }).ToArray(), parts });
    }
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
    System.IO.File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { source = path, unityVersion = UnityEngine.Application.unityVersion, scale = 1.4f, idle = clip.name, meshes }, Newtonsoft.Json.Formatting.Indented));
    return output;
}
finally
{
    if (graph.IsValid()) graph.Destroy();
    if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
    if (active.isDirty != wasDirty) throw new System.InvalidOperationException("Active scene dirty state changed unexpectedly.");
}
