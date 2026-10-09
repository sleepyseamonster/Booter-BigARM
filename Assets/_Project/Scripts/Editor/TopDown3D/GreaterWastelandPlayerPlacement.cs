using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Edit-mode placement of the existing authored player; no runtime spawn owner.</summary>
    [InitializeOnLoad]
    public static class GreaterWastelandPlayerPlacement
    {
        public const float GroundClearance = .12f;
        public static bool ClickPlacement { get; private set; }

        static GreaterWastelandPlayerPlacement()
        {
            SceneView.duringSceneGui += SceneGUI;
            EditorApplication.playModeStateChanged += _ => ClickPlacement = false;
            EditorSceneManager.sceneClosing += (_, _) => ClickPlacement = false;
        }

        public static TopDown3DPlayerMotor FindPlayer(Scene scene)
        {
            if (scene.path != GreaterWastelandTerrainWorkspace.ScenePath) return null;
            var players = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TopDown3DPlayerMotor>(true)).ToArray();
            return players.Length == 1 ? players[0] : null;
        }

        private static TopDown3DPlayerMotor RequirePlayer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Place Booter in Edit Mode so the position can be saved.");
            return FindPlayer(SceneManager.GetActiveScene())
                ?? throw new InvalidOperationException("Open Greater Wasteland with exactly one Booter player.");
        }

        [MenuItem("Booter & BigARM/Player Placement/Select Booter")]
        public static void SelectBooter()
        {
            var player = RequirePlayer();
            Selection.activeGameObject = player.gameObject;
            SceneVisibilityManager.instance.Show(player.gameObject, true);
            Tools.current = Tool.Move;
        }

        [MenuItem("Booter & BigARM/Player Placement/Find Booter In Scene View")]
        public static void FrameBooter()
        {
            SelectBooter();
            var player = RequirePlayer();
            if (GreaterWastelandTerrainWorkspace.LocalView)
                GreaterWastelandTerrainWorkspace.ApplyLocalView(player.transform.position);
            // Frame an existing view without opening or focusing any window.
            SceneView.lastActiveSceneView?.Frame(new Bounds(player.transform.position, Vector3.one * 12f), true);
        }

        [MenuItem("Booter & BigARM/Player Placement/Mark Booter At Top Of Hierarchy")]
        public static void MarkHierarchy()
        {
            var player = RequirePlayer();
            Undo.RegisterFullObjectHierarchyUndo(player.gameObject, "Mark player in hierarchy");
            player.transform.SetSiblingIndex(0);
            var icon = EditorGUIUtility.IconContent("sv_label_1").image as Texture2D;
            if (icon != null) EditorGUIUtility.SetIconForObject(player.gameObject, icon);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            SelectBooter();
        }

        public static bool TryGroundPoint(Scene scene, Vector3 position, out Vector3 ground)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
            {
                if (terrain.terrainData == null || !terrain.gameObject.activeInHierarchy) continue;
                Vector3 origin = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (position.x < origin.x || position.x > origin.x + size.x
                    || position.z < origin.z || position.z > origin.z + size.z) continue;
                var collider = terrain.GetComponent<TerrainCollider>();
                if (collider == null || !collider.enabled) continue;
                // Collider raycast respects terrain holes and returns the native collision surface.
                if (!collider.Raycast(new Ray(new Vector3(position.x, origin.y + size.y + 10f, position.z), Vector3.down),
                    out RaycastHit hit, size.y + 20f)) continue;
                ground = hit.point;
                return true;
            }
            ground = default;
            return false;
        }

        public static Vector3 PositionAboveGround(Vector3 ground, float pivotToCapsuleBottom) =>
            ground + Vector3.up * (pivotToCapsuleBottom + GroundClearance);

        public static void PlaceAt(Vector3 horizontalPosition)
        {
            var player = RequirePlayer();
            Physics.SyncTransforms();
            if (!TryGroundPoint(player.gameObject.scene, horizontalPosition, out Vector3 ground))
                throw new InvalidOperationException("No authored terrain collider at that position. Booter was not moved.");
            var capsule = player.GetComponent<CapsuleCollider>();
            if (capsule == null || !capsule.enabled)
                throw new InvalidOperationException("Booter's capsule collider must be enabled for placement.");
            float bottomOffset = player.transform.position.y - capsule.bounds.min.y;
            Undo.RecordObject(player.transform, "Place Booter on terrain");
            player.transform.position = PositionAboveGround(ground, bottomOffset);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            if (GreaterWastelandTerrainWorkspace.LocalView && !GreaterWastelandTerrainWorkspace.FollowSceneView)
                GreaterWastelandTerrainWorkspace.ApplyLocalView(player.transform.position);
            SelectBooter();
        }

        [MenuItem("Booter & BigARM/Player Placement/Snap Booter To Ground")]
        public static void SnapToGround() => PlaceAt(RequirePlayer().transform.position);

        [MenuItem("Booter & BigARM/Player Placement/Place Booter At Scene View Centre")]
        public static void PlaceAtViewCentre()
        {
            RequirePlayer();
            if (SceneView.lastActiveSceneView == null)
                throw new InvalidOperationException("An existing Scene view is required.");
            PlaceAt(SceneView.lastActiveSceneView.pivot);
        }

        [MenuItem("Booter & BigARM/Player Placement/Click Terrain To Place Booter")]
        public static void ToggleClickPlacement()
        {
            RequirePlayer();
            ClickPlacement = !ClickPlacement;
            SceneView.RepaintAll();
        }

        private static void SceneGUI(SceneView view)
        {
            if (!ClickPlacement || EditorApplication.isPlayingOrWillChangePlaymode
                || SceneManager.GetActiveScene().path != GreaterWastelandTerrainWorkspace.ScenePath) return;
            Event e = Event.current;
            Handles.BeginGUI();
            GUI.Label(new Rect(12, 12, 440, 30), "Place Booter: click terrain. Escape cancels. Alt navigates.", EditorStyles.helpBox);
            Handles.EndGUI();
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            { ClickPlacement = false; e.Use(); view.Repaint(); return; }
            if (e.alt) return;
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (e.type != EventType.MouseDown || e.button != 0) return;
            Physics.SyncTransforms();
            var hits = Physics.RaycastAll(HandleUtility.GUIPointToWorldRay(e.mousePosition), Mathf.Infinity,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits.OrderBy(h => h.distance))
            {
                if (hit.collider is not TerrainCollider || hit.collider.gameObject.scene != SceneManager.GetActiveScene()
                    || (SceneVisibilityManager.instance.IsHidden(hit.collider.gameObject)
                        && !GreaterWastelandTerrainOverview.Enabled)) continue;
                PlaceAt(hit.point);
                ClickPlacement = false;
                e.Use();
                view.Repaint();
                return;
            }
            e.Use();
            view.ShowNotification(new GUIContent("Click a visible terrain surface to place Booter."));
        }
    }

    [CustomEditor(typeof(TopDown3DPlayerMotor))]
    public sealed class GreaterWastelandPlayerPlacementInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var player = (TopDown3DPlayerMotor)target;
            if (player.gameObject.scene.path == GreaterWastelandTerrainWorkspace.ScenePath)
            {
                EditorGUILayout.LabelField("BOOTER — PLAYER CHARACTER", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This is the scene's player. Use the Transform / Move tool to position Booter in Edit Mode, then Snap To Ground and save the scene. Play starts at that saved position. Legger keeps his own placement.", MessageType.Info);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("Find Booter in Scene view")) GreaterWastelandPlayerPlacement.FrameBooter();
                    if (GUILayout.Button("Snap Booter to ground")) GreaterWastelandPlayerPlacement.SnapToGround();
                    if (GUILayout.Button("Place at Scene view centre")) GreaterWastelandPlayerPlacement.PlaceAtViewCentre();
                    if (GUILayout.Button(GreaterWastelandPlayerPlacement.ClickPlacement ? "Cancel click placement" : "Click terrain to place Booter"))
                        GreaterWastelandPlayerPlacement.ToggleClickPlacement();
                }
                EditorGUILayout.Space();
            }
            DrawDefaultInspector();
        }
    }
}
