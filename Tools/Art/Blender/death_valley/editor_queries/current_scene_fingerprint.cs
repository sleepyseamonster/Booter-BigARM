var scenes = new System.Collections.Generic.List<object>();
for (int i=0; i<UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    var records = new System.Collections.Generic.List<string>();
    foreach (var root in scene.GetRootGameObjects()) foreach (var transform in root.GetComponentsInChildren<UnityEngine.Transform>(true)) {
        var go=transform.gameObject;
        records.Add(go.GetInstanceID()+":"+UnityEditor.EditorJsonUtility.ToJson(go));
        foreach (var component in go.GetComponents<UnityEngine.Component>()) if (component != null) records.Add(component.GetInstanceID()+":"+UnityEditor.EditorJsonUtility.ToJson(component));
    }
    records.Sort(System.StringComparer.Ordinal);
    string hash;
    using(var sha=System.Security.Cryptography.SHA256.Create()) hash=System.BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",records)))).Replace("-","").ToLowerInvariant();
    scenes.Add(new {path=scene.path, dirty=scene.isDirty, roots=scene.rootCount, content_sha256=hash});
}
return scenes;
