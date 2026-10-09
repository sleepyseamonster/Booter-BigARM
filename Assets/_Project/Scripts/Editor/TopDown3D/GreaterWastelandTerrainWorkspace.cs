using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Editor-only organization and working visibility for the fixed authored landscape.</summary>
    [InitializeOnLoad]
    public static class GreaterWastelandTerrainWorkspace
    {
        public const string ScenePath = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        public const string SectorPrefix = "Terrain Sector | ";
        public const float SectorSize = 1024f;
        private const string ActiveKey = "GreaterWasteland.Terrain.LocalView";
        private const string FollowKey = "GreaterWasteland.Terrain.Follow";
        private const string PlayerFollowKey = "GreaterWasteland.Terrain.FollowPlayer";
        private static readonly Dictionary<GameObject, bool> originalVisibility = new();
        private static readonly List<GameObject> sectors = new();
        private static readonly Dictionary<GameObject, Bounds> sectorBounds = new();
        private static double nextUpdate;
        private static Vector3 center;
        private static Vector2Int lastCell;
        private static Transform followTarget;
        private static bool refresh = true;
        public static bool LocalView => SessionState.GetBool(ActiveKey, true);
        public static bool FollowSceneView
        {
            get => SessionState.GetBool(FollowKey, false);
            set { if (FollowSceneView == value) return; SessionState.SetBool(FollowKey, value); if (value) FollowBooter = false; refresh = true; }
        }
        public static bool FollowBooter
        {
            get => SessionState.GetBool(PlayerFollowKey, true);
            set { if (FollowBooter == value) return; SessionState.SetBool(PlayerFollowKey, value); if (value) FollowSceneView = false; refresh = true; }
        }
        public static int VisibleSectors { get; private set; }
        public static int TotalSectors => sectors.Count;

        static GreaterWastelandTerrainWorkspace()
        {
            EditorApplication.update += Update;
            EditorApplication.hierarchyChanged += () => { RestoreVisibility(); sectors.Clear(); sectorBounds.Clear(); followTarget = null; refresh = true; };
            AssemblyReloadEvents.beforeAssemblyReload += RestoreVisibility;
            EditorApplication.quitting += RestoreVisibility;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) RestoreVisibility();
                if (state == PlayModeStateChange.EnteredEditMode) refresh = true;
            };
            EditorSceneManager.sceneClosing += (scene, removing) => RestoreVisibility();
            center = new Vector3(SessionState.GetFloat("GreaterWasteland.Terrain.X", 0), 0,
                SessionState.GetFloat("GreaterWasteland.Terrain.Z", 0));
            if (FollowBooter) SessionState.SetBool(FollowKey, false);
        }

        public static Vector2Int SectorCell(Vector3 position, Vector3 sectionOrigin) => new(
            Mathf.FloorToInt((position.x - sectionOrigin.x) / SectorSize),
            Mathf.FloorToInt((position.z - sectionOrigin.z) / SectorSize));

        public static Vector2Int WorldSectorCell(Vector3 position) => new(
            Mathf.FloorToInt(position.x / SectorSize), Mathf.FloorToInt(position.z / SectorSize));

        public static bool InNeighbourhood(Vector2Int candidate, Vector2Int centre) =>
            Mathf.Abs(candidate.x - centre.x) <= 1 && Mathf.Abs(candidate.y - centre.y) <= 1;

        public static Bounds TerrainBounds(IEnumerable<Terrain> tiles)
        {
            bool first = true;
            Bounds result = default;
            foreach (Terrain tile in tiles)
            {
                if (tile == null || tile.terrainData == null) continue;
                Bounds bounds = new(tile.transform.position + tile.terrainData.size * .5f, tile.terrainData.size);
                if (first) { result = bounds; first = false; }
                else result.Encapsulate(bounds);
            }
            return result;
        }

        [MenuItem("Booter & BigARM/Greater Wasteland Terrain/Organize Into Sectors")]
        public static void OrganizeCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open Greater Wasteland in Edit Mode to organize terrain.");
            var owners = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BadwaterTerrainConnectivity>(true)).ToArray();
            if (owners.Length != 1) throw new InvalidOperationException("Expected one authored terrain owner.");
            Organize(owners[0].transform);
        }

        // Does not save automatically: the caller owns the scene and the save decision.
        public static void Organize(Transform owner)
        {
            Terrain[] tiles = owner.GetComponentsInChildren<Terrain>(true);
            if (tiles.Any(t => PrefabUtility.IsPartOfPrefabInstance(t)))
                throw new InvalidOperationException("Unpack must be explicitly reviewed before organizing prefab terrain.");
            RestoreVisibility();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Organize Greater Wasteland terrain sectors");
            var loose = tiles.Where(t => t.transform.parent == owner).ToArray();
            if (loose.Length > 0)
            {
                Transform core = owner.Find("badwater_core");
                if (core == null) core = CreateGroup("badwater_core", owner);
                foreach (Terrain tile in loose) Undo.SetTransformParent(tile.transform, core, "Group core terrain");
            }
            // Geographic section names and the common connectivity owner remain stable.
            foreach (Transform section in owner.Cast<Transform>().ToArray())
            {
                Terrain[] sectionTiles = section.GetComponentsInChildren<Terrain>(true);
                if (sectionTiles.Length == 0) continue;
                Vector3 origin = new(sectionTiles.Min(t => t.transform.position.x), 0,
                    sectionTiles.Min(t => t.transform.position.z));
                foreach (var cell in sectionTiles.GroupBy(t => SectorCell(t.transform.position, origin))
                    .OrderBy(g => g.Key.y).ThenBy(g => g.Key.x))
                {
                    string name = $"{SectorPrefix}z{cell.Key.y:00}_x{cell.Key.x:00} | 1 km";
                    Transform sector = section.Find(name);
                    if (sector == null) sector = CreateGroup(name, section);
                    foreach (Terrain tile in cell.OrderBy(t => t.name))
                        if (tile.transform.parent != sector) Undo.SetTransformParent(tile.transform, sector, "Group terrain tile");
                }
            }
            Undo.CollapseUndoOperations(undoGroup);
            sectors.Clear();
            sectorBounds.Clear();
            refresh = true;
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(go, "Create terrain group");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        [MenuItem("Booter & BigARM/Greater Wasteland Terrain/Local View Around Booter")]
        public static void AroundBooter()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject player = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Booter Perspective 3D Controller");
            if (scene.path != ScenePath || player == null) return;
            FollowSceneView = false;
            FollowBooter = true;
            followTarget = player.transform;
            ApplyLocalView(player.transform.position);
        }

        [MenuItem("Booter & BigARM/Greater Wasteland Terrain/Local View Around Selection")]
        public static void AroundSelection()
        {
            if (Selection.activeTransform == null || Selection.activeTransform.gameObject.scene.path != ScenePath) return;
            FollowSceneView = false;
            FollowBooter = false;
            Terrain[] selectedTiles = Selection.activeTransform.GetComponentsInChildren<Terrain>(true);
            ApplyLocalView(selectedTiles.Length > 0 ? TerrainBounds(selectedTiles).center : Selection.activeTransform.position);
        }

        public static void ApplyLocalView(Vector3 focus)
        {
            if (SceneManager.GetActiveScene().path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode) return;
            center = focus;
            SessionState.SetFloat("GreaterWasteland.Terrain.X", center.x);
            SessionState.SetFloat("GreaterWasteland.Terrain.Z", center.z);
            SessionState.SetBool(ActiveKey, true);
            if (sectors.Count == 0)
            {
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                    sectors.AddRange(root.GetComponentsInChildren<Transform>(true)
                        .Where(t => t.name.StartsWith(SectorPrefix, StringComparison.Ordinal)).Select(t => t.gameObject));
            }
            if (sectors.Count == 0) { SessionState.SetBool(ActiveKey, false); return; }
            var visibility = SceneVisibilityManager.instance;
            VisibleSectors = 0;
            foreach (GameObject sector in sectors)
            {
                if (sector == null) continue;
                if (!originalVisibility.ContainsKey(sector))
                    foreach (Transform node in sector.GetComponentsInChildren<Transform>(true))
                        originalVisibility.Add(node.gameObject, visibility.IsHidden(node.gameObject));
                if (!sectorBounds.TryGetValue(sector, out Bounds bounds))
                {
                    bounds = TerrainBounds(sector.GetComponentsInChildren<Terrain>(true));
                    sectorBounds.Add(sector, bounds);
                }
                bool visible = InNeighbourhood(WorldSectorCell(bounds.min), WorldSectorCell(center));
                if (visible) { visibility.Show(sector, true); VisibleSectors++; }
                else visibility.Hide(sector, true);
            }
            lastCell = WorldSectorCell(center);
            refresh = false;
            SceneView.RepaintAll();
        }

        [MenuItem("Booter & BigARM/Greater Wasteland Terrain/Show All Terrain")]
        public static void ShowAll()
        {
            StopLocalView();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Terrain tile in root.GetComponentsInChildren<Terrain>(true))
                    if (tile.gameObject.scene.path == ScenePath)
                    {
                        // Clear terrain ancestor visibility too, without showing unrelated scene roots.
                        for (Transform t = tile.transform; t != null; t = t.parent)
                            SceneVisibilityManager.instance.Show(t.gameObject, false);
                    }
            VisibleSectors = sectors.Count;
            SceneView.RepaintAll();
        }

        [MenuItem("Booter & BigARM/Greater Wasteland Terrain/Stop Local View And Restore Visibility")]
        public static void StopLocalView()
        {
            SessionState.SetBool(ActiveKey, false);
            RestoreVisibility();
            SceneView.RepaintAll();
        }

        private static void RestoreVisibility()
        {
            var visibility = SceneVisibilityManager.instance;
            foreach (var pair in originalVisibility)
                if (pair.Key != null) visibility.Show(pair.Key, false);
            foreach (var pair in originalVisibility)
                if (pair.Key != null && pair.Value) visibility.Hide(pair.Key, false);
            originalVisibility.Clear();
            refresh = true;
        }

        private static void Update()
        {
            if (!LocalView || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || EditorApplication.timeSinceStartup < nextUpdate || SceneManager.GetActiveScene().path != ScenePath) return;
            nextUpdate = EditorApplication.timeSinceStartup + .2;
            if (FollowBooter)
            {
                if (followTarget == null) followTarget = GreaterWastelandPlayerPlacement.FindPlayer(SceneManager.GetActiveScene())?.transform;
                if (followTarget != null) center = followTarget.position;
            }
            if (FollowSceneView && SceneView.lastActiveSceneView != null) center = SceneView.lastActiveSceneView.pivot;
            if (refresh || WorldSectorCell(center) != lastCell) ApplyLocalView(center);
        }
    }

    public sealed class GreaterWastelandTerrainWorkspaceWindow : EditorWindow
    {
        [MenuItem("Booter & BigARM/Greater Wasteland Terrain/Terrain Workspace")]
        public static void Open() => GetWindow<GreaterWastelandTerrainWorkspaceWindow>("Terrain Workspace");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Greater Wasteland terrain", EditorStyles.boldLabel);
            GreaterWastelandTerrainOverview.Enabled = EditorGUILayout.Toggle("Lightweight overview", GreaterWastelandTerrainOverview.Enabled);
            if (GUILayout.Button("Frame entire landscape")) GreaterWastelandTerrainOverview.FrameLandscape();
            EditorGUILayout.HelpBox("Local view hides distant terrain only in the Scene view. Terrain stays loaded; colliders and gameplay remain active. Use hierarchy eye icons for individual sectors.", MessageType.Info);
            using (new EditorGUI.DisabledScope(SceneManager.GetActiveScene().path != GreaterWastelandTerrainWorkspace.ScenePath
                || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.LabelField("Detailed terrain: 3 × 3 sectors (1,024 m each)");
                if (GUILayout.Button("Follow Booter — centre plus 8 neighbours")) GreaterWastelandTerrainWorkspace.AroundBooter();
                using (new EditorGUI.DisabledScope(Selection.activeTransform == null))
                    if (GUILayout.Button("Work around selection")) GreaterWastelandTerrainWorkspace.AroundSelection();
                if (GUILayout.Button("Work around Scene view"))
                {
                    if (SceneView.lastActiveSceneView != null)
                    {
                        GreaterWastelandTerrainWorkspace.FollowBooter = false;
                        GreaterWastelandTerrainWorkspace.ApplyLocalView(SceneView.lastActiveSceneView.pivot);
                    }
                }
                GreaterWastelandTerrainWorkspace.FollowBooter = EditorGUILayout.Toggle("Follow Booter", GreaterWastelandTerrainWorkspace.FollowBooter);
                GreaterWastelandTerrainWorkspace.FollowSceneView = EditorGUILayout.Toggle("Follow Scene view", GreaterWastelandTerrainWorkspace.FollowSceneView);
                if (GUILayout.Button("Show all terrain")) GreaterWastelandTerrainWorkspace.ShowAll();
                if (GUILayout.Button("Stop and restore previous visibility")) GreaterWastelandTerrainWorkspace.StopLocalView();
            }
            EditorGUILayout.LabelField(GreaterWastelandTerrainWorkspace.LocalView
                ? $"Local view: {GreaterWastelandTerrainWorkspace.VisibleSectors}/{GreaterWastelandTerrainWorkspace.TotalSectors} sectors shown"
                : "Local view is off");
        }
    }
}
