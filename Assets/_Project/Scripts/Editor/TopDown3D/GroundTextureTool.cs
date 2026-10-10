using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public sealed class GroundTextureTool : EditorWindow
    {
        public const string LayerFolder = "Assets/_Project/Materials/Terrain/GroundPaintLayers";
        private static readonly string[] PresetNames = { "Sand", "Gravel", "Rocky ground", "Sand and dirt" };
        private static readonly string[] PresetFiles = { "BrokenWorldSweptSandAlbedo", "BrokenWorldGravelAlbedo", "BrokenWorldMixedRockyAlbedo", "BrokenWorldSandDirtAlbedo" };
        [SerializeField] private Texture2D texture;
        [SerializeField] private TerrainLayer layer;
        [SerializeField] private Vector2 stickerSize = new Vector2(8, 8);
        [SerializeField] private float yaw, brushRadius = 4, strength = .25f, softness = .5f;
        [SerializeField] private int mode;
        [SerializeField] private bool restoreBase;
        private bool armed;
        private int strokeGroup = -1;
        private Vector3 lastStrokePoint;
        private bool hasStrokePoint;
        private Terrain[] terrains = Array.Empty<Terrain>();
        private Collider[] surfaces = Array.Empty<Collider>();
        private readonly HashSet<TerrainData> strokeUndoTargets = new HashSet<TerrainData>();
        private readonly HashSet<TerrainData> paintedAssets = new HashSet<TerrainData>();
        private Scene terrainScene;
        private string status;

        [MenuItem("Booter & BigARM/Ground Textures/Texture Sticker and Paint Tool")]
        public static void Open() => GetWindow<GroundTextureTool>("Ground Textures");

        public static void InstallDefaults()
        {
            var shader = Shader.Find("BooterBigArm/Ground Texture Sticker")
                ?? throw new InvalidOperationException("Import the sticker shader first.");
            if (AssetDatabase.LoadAssetAtPath<Material>(GroundTextureAuthoring.MaterialPath) == null)
                AssetDatabase.CreateAsset(new Material(shader) { name = "GroundTextureSticker" }, GroundTextureAuthoring.MaterialPath);
            foreach (string file in PresetFiles)
            {
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Project/Art/Environment/Ground/SandDirt/{file}.png");
                if (albedo != null) CreateLayer(albedo);
            }
        }

        public static TerrainLayer CreateLayer(Texture2D albedo)
        {
            if (albedo == null) throw new ArgumentNullException(nameof(albedo));
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(albedo));
            if (string.IsNullOrEmpty(guid)) throw new ArgumentException("Use an imported project texture.");
            EnsureFolder(LayerFolder);
            string path = $"{LayerFolder}/{albedo.name}_{guid.Substring(0, 8)}.terrainlayer";
            var existing = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path); if (existing != null) return existing;
            var result = new TerrainLayer { name = albedo.name, diffuseTexture = albedo, tileSize = Vector2.one * 2, smoothness = .08f };
            string normalPath = AssetDatabase.GetAssetPath(albedo).Replace("Albedo.png", "Normal.png");
            if (normalPath != AssetDatabase.GetAssetPath(albedo)) result.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            AssetDatabase.CreateAsset(result, path); return result;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGui;
            Undo.undoRedoPerformed += UndoRedo;
            EditorApplication.hierarchyChanged += InvalidateSurfaces;
            if (texture == null) SelectPreset(0);
        }
        private void OnDisable() { SceneView.duringSceneGui -= DuringSceneGui; Undo.undoRedoPerformed -= UndoRedo; EditorApplication.hierarchyChanged -= InvalidateSurfaces; EndStroke(); armed = false; }
        private void InvalidateSurfaces() { surfaces = Array.Empty<Collider>(); terrains = Array.Empty<Terrain>(); }
        private void UndoRedo()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var sticker in root.GetComponentsInChildren<GroundTextureSticker>(true)) sticker.RebuildVisual();
            SceneView.RepaintAll();
        }
        private void SelectPreset(int preset)
        {
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Project/Art/Environment/Ground/SandDirt/{PresetFiles[preset]}.png");
            if (texture != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture));
                layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{LayerFolder}/{texture.name}_{guid.Substring(0, 8)}.terrainlayer");
            }
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Ground textures", EditorStyles.boldLabel);
            int next = GUILayout.Toolbar(mode, new[] { "Place stickers", "Paint terrain" });
            if (next != mode) { EndStroke(); armed = false; mode = next; }
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < PresetNames.Length; i++) if (GUILayout.Button(PresetNames[i])) SelectPreset(i);
            EditorGUILayout.EndHorizontal();
            texture = (Texture2D)EditorGUILayout.ObjectField("Texture", texture, typeof(Texture2D), false);
            if (mode == 0)
            {
                stickerSize = EditorGUILayout.Vector2Field("Size in metres", stickerSize);
                stickerSize = new Vector2(Mathf.Clamp(stickerSize.x, .25f, 64), Mathf.Clamp(stickerSize.y, .25f, 64));
                yaw = EditorGUILayout.Slider("Rotation", yaw, -180, 180);
                EditorGUILayout.HelpBox("Choose a texture, start placement, then click ground in Scene view. Select a placed sticker to adjust its soft edges, opacity, tiling, normal map and overlap order. Escape stops placement.", MessageType.Info);
            }
            else
            {
                layer = (TerrainLayer)EditorGUILayout.ObjectField("Paint layer", layer, typeof(TerrainLayer), false);
                restoreBase = EditorGUILayout.Toggle("Restore base texture", restoreBase);
                using (new EditorGUI.DisabledScope(texture == null))
                    if (GUILayout.Button("Create / use paint layer from texture")) layer = CreateLayer(texture);
                brushRadius = EditorGUILayout.Slider("Brush radius (m)", brushRadius, .25f, 32);
                strength = EditorGUILayout.Slider("Brush strength", strength, .01f, 1);
                softness = EditorGUILayout.Slider("Soft edge", softness, .01f, 1);
                EditorGUILayout.HelpBox("Drag on native Unity terrain to paint. Each stroke has Undo. Heights and holes are preserved; texture weights and layer assignments change in TerrainData assets. Save Project to keep those asset edits. Escape stops painting.", MessageType.Info);
                EditorGUILayout.HelpBox("Greater Wasteland currently paints at roughly 2 m per texture-weight pixel. Use stickers for finer marks. Layer tiling is edited on the TerrainLayer asset and affects every tile using that layer.", MessageType.None);
                using (new EditorGUI.DisabledScope(paintedAssets.Count == 0))
                    if (GUILayout.Button("Save painted terrain assets"))
                    { foreach (var asset in paintedAssets) if (asset != null) AssetDatabase.SaveAssetIfDirty(asset); status = "Painted TerrainData assets saved."; }
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || (mode == 0 ? texture == null : !restoreBase && layer == null)))
            {
                if (GUILayout.Button(armed ? "Stop" : mode == 0 ? "Start placing stickers" : "Start painting"))
                { EndStroke(); armed = !armed; InvalidateSurfaces(); SceneView.RepaintAll(); }
            }
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
        }
        private void DuringSceneGui(SceneView view)
        {
            if (!armed || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { EndStroke(); armed = false; e.Use(); Repaint(); return; }
            if (e.alt || e.button == 1 || e.button == 2) return;
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (e.type == EventType.MouseUp && e.button == 0) { EndStroke(); e.Use(); return; }
            var scene = SceneManager.GetActiveScene();
            if (terrainScene != scene || surfaces.Length == 0)
            { surfaces = GroundTextureAuthoring.Surfaces(scene); terrains = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Terrain>()).ToArray(); terrainScene = scene; }
            if (!GroundTextureAuthoring.TryHit(scene, HandleUtility.GUIPointToWorldRay(e.mousePosition), out var hit, surfaces)) return;
            Handles.color = new Color(1, .75f, .3f, .9f);
            Handles.DrawWireDisc(hit.point, hit.normal, mode == 0 ? Mathf.Max(stickerSize.x, stickerSize.y) / 2 : brushRadius);
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown) view.Repaint();
            if (e.button != 0 || (e.type != EventType.MouseDown && e.type != EventType.MouseDrag)) return;
            if (e.control || e.command || e.shift) return;
            if (e.type == EventType.MouseDown) EndStroke();
            try
            {
                if (mode == 0 && e.type == EventType.MouseDown)
                {
                    var sticker = GroundTextureAuthoring.Create(scene, hit.point, texture, stickerSize, yaw);
                    Selection.activeGameObject = sticker.gameObject; status = "Sticker placed. Save the scene to keep it.";
                }
                else if (mode == 1)
                {
                    if (!(hit.collider is TerrainCollider)) { status = "Terrain painting requires native Unity terrain. Use stickers on the study ground."; e.Use(); Repaint(); return; }
                    if (strokeGroup < 0) { Undo.IncrementCurrentGroup(); strokeGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Paint ground texture stroke"); }
                    if (terrainScene != scene || terrains.Length == 0)
                    { terrains = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Terrain>()).ToArray(); terrainScene = scene; }
                    float step = Mathf.Max(.05f, brushRadius * .15f);
                    if (!hasStrokePoint || Vector3.Distance(lastStrokePoint, hit.point) >= step)
                    {
                        int count = hasStrokePoint ? Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(lastStrokePoint, hit.point) / step), 1, 32) : 1;
                        for (int i = 1; i <= count; i++)
                        {
                            var point = hasStrokePoint ? Vector3.Lerp(lastStrokePoint, hit.point, (float)i / count) : hit.point;
                            foreach (var terrain in terrains)
                            {
                                var paintLayer = restoreBase ? terrain.terrainData.terrainLayers.FirstOrDefault() : layer;
                                if (GroundTextureAuthoring.PaintTerrain(terrain, paintLayer, point, brushRadius, strength, softness, strokeUndoTargets)) paintedAssets.Add(terrain.terrainData);
                            }
                        }
                        lastStrokePoint = hit.point; hasStrokePoint = true; status = "Painting terrain texture weights. Undo restores the stroke.";
                    }
                }
            }
            catch (Exception exception) { status = exception.Message; EndStroke(); armed = false; }
            e.Use(); Repaint();
        }
        private void EndStroke()
        { if (strokeGroup >= 0) Undo.CollapseUndoOperations(strokeGroup); strokeGroup = -1; hasStrokePoint = false; strokeUndoTargets.Clear(); }
    }

    [InitializeOnLoad]
    public static class GroundStickerIdentity
    {
        static GroundStickerIdentity() { EditorApplication.hierarchyChanged += RepairSceneIds; }
        public static void RepairSceneIds()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(scene)) continue;
                RepairIds(scene);
            }
        }
        public static void RepairIds(Scene scene)
        {
                if (!scene.IsValid() || !scene.isLoaded) return;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var root in scene.GetRootGameObjects())
                foreach (var sticker in root.GetComponentsInChildren<GroundTextureSticker>(true))
                {
                    if (!string.IsNullOrWhiteSpace(sticker.AuthoredId) && seen.Add(sticker.AuthoredId)) continue;
                    Undo.RecordObject(sticker, "Assign authored ground sticker identity");
                    string id = Guid.NewGuid().ToString("N"); sticker.SetAuthoredId(id); seen.Add(id);
                    EditorUtility.SetDirty(sticker); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }

    [CustomEditor(typeof(GroundTextureSticker))]
    public sealed class GroundTextureStickerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var sticker = (GroundTextureSticker)target;
            EditorGUI.BeginChangeCheck(); DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck()) GroundTextureAuthoring.Conform(sticker);
            if (GUILayout.Button("Conform to ground")) GroundTextureAuthoring.Conform(sticker);
            EditorGUILayout.HelpBox("Move or rotate with Unity's tools; the sticker follows ground when selected. Unsupported ground and holes are clipped. This is surface appearance, with no physical relief.", MessageType.Info);
        }
        private void OnSceneGUI()
        {
            var sticker = (GroundTextureSticker)target;
            if (sticker.NeedsConform && !EditorApplication.isPlayingOrWillChangePlaymode) GroundTextureAuthoring.Conform(sticker);
        }
    }
}
