using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Persistent terrain target and native sculpt controls, independent of Inspector selection.</summary>
    public sealed class TerrainSculptControlsWindow : EditorWindow
    {
        [SerializeField] private Terrain terrain;
        [SerializeField] private Vector2 scroll;
        private UnityEditor.Editor terrainEditor;

        [MenuItem("Booter & BigARM/Geology Studies/Terrain Sculpting Controls")]
        public static void Open()
        {
            var window = GetWindow<TerrainSculptControlsWindow>(false, "Terrain Controls", false);
            var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Terrain>() : null;
            if (selected == null && window.terrain == null)
            {
                var center = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                    .FirstOrDefault(g => g.name == "Area 05");
                if (center != null) selected = center.GetComponentInChildren<Terrain>();
            }
            if (selected != null) window.Bind(selected);
        }

        private void OnEnable() => titleContent = new GUIContent("Terrain Controls");
        private void OnDisable() => ReleaseEditor();

        public void Bind(Terrain target)
        {
            if (target == null || target.terrainData == null) throw new ArgumentException("Choose a terrain with TerrainData.");
            if (terrain != target) ReleaseEditor();
            terrain = target;
            Activate("PaintHeightTool");
            Repaint();
        }

        private void ReleaseEditor()
        {
            if (terrainEditor != null) DestroyImmediate(terrainEditor);
            terrainEditor = null;
        }

        private void EnsureEditor()
        {
            if (terrainEditor != null && terrainEditor.target == terrain) return;
            ReleaseEditor();
            if (terrain != null) terrainEditor = UnityEditor.Editor.CreateEditor(terrain);
        }

        private void Activate(string toolName)
        {
            if (terrain == null || terrain.terrainData == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var toolType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.TerrainTools." + toolName)).FirstOrDefault(t => t != null);
            if (toolType == null || !typeof(EditorTool).IsAssignableFrom(toolType))
                throw new InvalidOperationException("The installed Editor does not expose this terrain tool: " + toolName);
            foreach (var textures in Resources.FindObjectsOfTypeAll<GroundTextureTool>()) textures.PrepareForTerrainPaint();
            Selection.activeGameObject = terrain.gameObject;
            EnsureEditor();
            var method = terrainEditor.GetType().GetMethod("SelectPaintTool",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { typeof(Type) }, null);
            method?.Invoke(terrainEditor, new object[] { toolType });
            ToolManager.SetActiveTool(toolType);
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Terrain sculpting", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var next = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
            if (EditorGUI.EndChangeCheck())
            {
                ReleaseEditor(); terrain = next;
                if (terrain != null && terrain.terrainData != null) Bind(terrain);
            }
            if (terrain == null || terrain.terrainData == null)
            {
                EditorGUILayout.HelpBox("Choose the terrain you want to sculpt. This window keeps that target when selection changes.", MessageType.Info);
                return;
            }
            if (terrain.transform.parent != null) EditorGUILayout.LabelField("Work area", terrain.transform.parent.name);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Raise / Lower")) Activate("PaintHeightTool");
                if (GUILayout.Button("Smooth")) Activate("SmoothHeightTool");
                if (GUILayout.Button("Set Height")) Activate("SetHeightTool");
            }
            EditorGUILayout.HelpBox("Drag in Scene view to sculpt. Shift lowers with Raise / Lower. These controls stay on this terrain; click a sculpt button to reactivate its brush.", MessageType.None);
            EnsureEditor();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            terrainEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }
    }
}
