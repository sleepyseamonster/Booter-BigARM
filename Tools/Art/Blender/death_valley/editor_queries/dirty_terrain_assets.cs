var dirty = new System.Collections.Generic.List<string>();
foreach (var asset in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Object>()) {
    if (!UnityEditor.EditorUtility.IsPersistent(asset) || !UnityEditor.EditorUtility.IsDirty(asset)) continue;
    var path = UnityEditor.AssetDatabase.GetAssetPath(asset);
    if (path.StartsWith("Assets/_Project/Art/Terrain/", System.StringComparison.Ordinal)) dirty.Add(path);
}
return dirty;
