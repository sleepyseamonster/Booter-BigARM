using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class NineAreaRockStudy
    {
        public const string ScenePath = "Assets/_Project/Scenes/Reference/FixedTerrainDressingStudy.unity";
        public const string Folder = "Assets/_Project/Art/Environment/Ground/Studies/NineAreaRockStudy";
        public const string SourceLayer = "Assets/_Project/Art/Terrain/BadwaterFourSlices/TerrainLayers/r11_c08.terrainlayer";
        public const string SourceMaterial = "Assets/_Project/Art/Terrain/BadwaterFourSlices/BadwaterTerrainURP.mat";
        public const float AreaSize = 16, Spacing = 20;

        // Run in an isolated copy. The existing current study is explicitly replaced by user direction;
        // the earlier comparison is preserved separately, and this scene retains its original .meta GUID.
        public static void BuildFromCli()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Build in an isolated background batchmode project.");
            if (Directory.Exists(Folder)) throw new InvalidOperationException("Preserve existing work areas; builder is first-run only.");
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(SourceLayer);
            var material = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterial);
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(TopDown3DRockWorkbenchAuthoringEditor.WorkbenchMaterialPath);
            if (layer == null || material == null || rockMaterial == null) throw new InvalidOperationException("Missing retained native texture or rock workbench material.");
            EnsureFolder(Folder);
            var baseLayer = UnityEngine.Object.Instantiate(layer); baseLayer.name = "GreaterWasteland_r11_c08_StudyBase";
            // A real-scale pad-sized sample, not the whole 256 m tile shrunk onto the pad.
            baseLayer.tileSize = layer.tileSize;
            baseLayer.tileOffset = layer.tileOffset + Vector2.one * ((256f - AreaSize) / 2f);
            AssetDatabase.CreateAsset(baseLayer, Folder + "/StudyBase.terrainlayer");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (int i = 0; i < 9; i++)
            {
                var station = new GameObject($"Area {i + 1:00}");
                station.transform.position = new Vector3((i % 3 - 1) * Spacing, 0, (i / 3 - 1) * Spacing);
                var data = new TerrainData { name = $"StudyArea_{i + 1:00}", heightmapResolution = 65, alphamapResolution = 128,
                    size = new Vector3(AreaSize, 4, AreaSize), terrainLayers = new[] { baseLayer } };
                data.SetHeights(0, 0, new float[65, 65]);
                var weights = new float[128, 128, 1];
                for (int z = 0; z < 128; z++) for (int x = 0; x < 128; x++) weights[z, x, 0] = 1;
                data.SetAlphamaps(0, 0, weights); AssetDatabase.CreateAsset(data, $"{Folder}/Area_{i + 1:00}.asset");
                var ground = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>(); ground.name = "Flat terrain";
                ground.transform.SetParent(station.transform, false); ground.transform.localPosition = new Vector3(-AreaSize / 2, 0, -AreaSize / 2);
                ground.materialTemplate = material; ground.drawTreesAndFoliage = false; ground.heightmapPixelError = 5;
                var rockObject = new GameObject("Rock Workbench"); rockObject.transform.SetParent(station.transform, false);
                var workbench = rockObject.AddComponent<TopDown3DRockWorkbenchAuthoring>();
                workbench.Configure(rockMaterial); workbench.ConfigureGoldenRockDefaults();
                TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(workbench, 20261009 + i * 7919);
                if (!TopDown3DRockWorkbenchPreview.TryBuildNow(workbench, out var error)) throw new InvalidOperationException(error);
            }
            var sunlight = new GameObject("Study sunlight", typeof(Light)); sunlight.transform.rotation = Quaternion.Euler(30, -35, 0);
            var sun = sunlight.GetComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.Soft;
            var cameraObject = new GameObject("Study overview camera", typeof(Camera));
            cameraObject.transform.position = new Vector3(0, 66, -48); cameraObject.transform.LookAt(Vector3.zero);
            var camera = cameraObject.GetComponent<Camera>(); camera.fieldOfView = 48; camera.farClipPlane = 200;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f,.08f,.06f);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Study could not be saved.");
            NineAreaLightingAndGround.Apply();
        }

        public static void OpenAuthoringPanels()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit Mode for this study.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the rock study first.");
            var terrain = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Terrain>())
                .FirstOrDefault();
            if (terrain == null) throw new InvalidOperationException("The study has no active terrain.");
            // focus=false: expose editor panels without focusing an Editor window or raising Unity.
            var textures = EditorWindow.GetWindow<GroundTextureTool>(false, "Ground Textures", false);
            textures.PrepareForTerrainPaint();
            var rocks = EditorWindow.GetWindow<RockStudyWorkbenchPanel>(false, "Rock Workbench", false);
            var project = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w => w.GetType().FullName == "UnityEditor.ProjectBrowser");
            if (project != null)
            {
                var space = project.position; float half = space.width / 2;
                textures.position = new Rect(space.x + 8, space.y + 20, half - 16, Mathf.Max(320, space.height - 28));
                rocks.position = new Rect(space.x + half + 8, space.y + 20, half - 16, Mathf.Max(320, space.height - 28));
            }
            var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspector != null) EditorWindow.GetWindow(inspector, false, "Inspector", false);
            Selection.activeGameObject = terrain.gameObject;
            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.in2DMode = false; view.rotation = Quaternion.Euler(55, 0, 0);
                view.Frame(new Bounds(terrain.transform.position + terrain.terrainData.size / 2, terrain.terrainData.size), true);
            }
            EditorApplication.delayCall += () =>
            {
                if (terrain == null) return;
                ActiveEditorTracker.sharedTracker.isLocked = false;
                ActiveEditorTracker.sharedTracker.ForceRebuild();
                // Pinned 6000.4 TerrainInspector method, inspected from the installed Editor.
                // This changes the authoring tool selection only, never paints a stroke.
                var tool = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.TerrainTools.PaintTextureTool")).FirstOrDefault(t => t != null);
                foreach (var editor in Resources.FindObjectsOfTypeAll<UnityEditor.Editor>())
                {
                    if (editor.target != terrain || editor.GetType().FullName != "UnityEditor.TerrainInspector") continue;
                    var method = editor.GetType().GetMethod("SelectPaintTool", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static, null, new[] { typeof(Type) }, null);
                    if (method != null && tool != null) method.Invoke(method.IsStatic ? null : editor, new object[] { tool });
                }
            };
        }
        [MenuItem("Booter & BigARM/Geology Studies/Open Nine-Area Authoring Panels")]
        private static void OpenFromMenu() => OpenAuthoringPanels();
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    public sealed class RockStudyWorkbenchPanel : EditorWindow
    {
        private int area;
        private UnityEditor.Editor embedded;
        private Vector2 scroll;
        private void OnDisable() { if (embedded != null) DestroyImmediate(embedded); }
        private void OnGUI()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != NineAreaRockStudy.ScenePath)
            { EditorGUILayout.HelpBox("Open the rock study to edit its existing workbenches.", MessageType.Info); return; }
            var stations = scene.GetRootGameObjects().Where(r => r.name.StartsWith("Area ", StringComparison.Ordinal) && r.GetComponentInChildren<Terrain>() != null).OrderBy(r => r.name).ToArray();
            if (stations.Length == 0) return;
            area = Mathf.Clamp(EditorGUILayout.Popup("Work area", area, stations.Select(s => s.name).ToArray()), 0, stations.Length - 1);
            var workbench = stations[area].GetComponentInChildren<TopDown3DRockWorkbenchAuthoring>();
            if (workbench == null) return;
            if (embedded == null || embedded.target != workbench)
            { if (embedded != null) DestroyImmediate(embedded); embedded = UnityEditor.Editor.CreateEditor(workbench); }
            if (GUILayout.Button("Select this area's terrain")) Selection.activeGameObject = stations[area].GetComponentInChildren<Terrain>().gameObject;
            if (GUILayout.Button("Frame this work area")) SceneView.lastActiveSceneView?.Frame(new Bounds(stations[area].transform.position, new Vector3(18, 4, 18)), true);
            scroll = EditorGUILayout.BeginScrollView(scroll); embedded.OnInspectorGUI(); EditorGUILayout.EndScrollView();
        }
    }
}
