using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Transient Scene-view context for the existing landscape, independent of detail visibility.</summary>
    [InitializeOnLoad]
    public static class GreaterWastelandTerrainOverview
    {
        private const string EnabledKey = "GreaterWasteland.Terrain.Overview";
        private const float TileSize = 256f;
        private static Mesh mesh;
        private static Material material;
        private static Terrain[] tiles;
        private static Vector3[] perimeter = System.Array.Empty<Vector3>();
        private static Vector3[] grid = System.Array.Empty<Vector3>();
        private static readonly Dictionary<Vector2Int, Terrain> cells = new();
        private static TopDown3DPlayerMotor player;
        private static bool visibilityDirty = true;
        public static int TriangleCount => mesh == null ? 0 : mesh.GetIndexCount(0) > 0 ? (int)mesh.GetIndexCount(0) / 3 : 0;
        public static int TileCount => tiles?.Length ?? 0;
        public static bool Enabled
        {
            get => SessionState.GetBool(EnabledKey, true);
            set { SessionState.SetBool(EnabledKey, value); SceneView.RepaintAll(); }
        }

        static GreaterWastelandTerrainOverview()
        {
            SceneView.duringSceneGui += Draw;
            SceneVisibilityManager.visibilityChanged += () => visibilityDirty = true;
            EditorApplication.hierarchyChanged += Invalidate;
            Undo.undoRedoPerformed += Invalidate;
            TerrainCallbacks.heightmapChanged += (_, _, _) => Invalidate();
            EditorSceneManager.sceneClosed += _ => Invalidate();
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.quitting += Release;
        }

        [MenuItem("Booter & BigARM/Greater Wasteland Terrain/Show Lightweight Overview")]
        public static void Toggle() => Enabled = !Enabled;

        [MenuItem("Booter & BigARM/Greater Wasteland Terrain/Show Lightweight Overview", true)]
        private static bool ValidateToggle()
        {
            Menu.SetChecked("Booter & BigARM/Greater Wasteland Terrain/Show Lightweight Overview", Enabled);
            return SceneManager.GetActiveScene().path == GreaterWastelandTerrainWorkspace.ScenePath;
        }

        [MenuItem("Booter & BigARM/Greater Wasteland Terrain/Frame Entire Landscape")]
        public static void FrameLandscape()
        {
            if (SceneManager.GetActiveScene().path != GreaterWastelandTerrainWorkspace.ScenePath) return;
            Enabled = true;
            RebuildIfNeeded();
            if (mesh != null) SceneView.lastActiveSceneView?.Frame(mesh.bounds, true);
        }

        public static List<(Vector2Int cell, Vector2Int direction)> FootprintEdges(IEnumerable<Vector2Int> occupied)
        {
            var set = new HashSet<Vector2Int>(occupied);
            var result = new List<(Vector2Int, Vector2Int)>();
            foreach (Vector2Int cell in set)
                foreach (Vector2Int direction in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up })
                    if (!set.Contains(cell + direction)) result.Add((cell, direction));
            return result;
        }

        private static Vector3 Corner(Terrain tile, float x, float z) => tile.transform.position
            + new Vector3(x * tile.terrainData.size.x, tile.terrainData.GetInterpolatedHeight(x, z) + .5f,
                z * tile.terrainData.size.z);

        public static void RebuildIfNeeded()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != GreaterWastelandTerrainWorkspace.ScenePath || mesh != null) return;
            tiles = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>(true))
                .Where(t => t.terrainData != null && t.gameObject.activeInHierarchy).ToArray();
            if (tiles.Length == 0) return;
            var vertices = new Vector3[tiles.Length * 4];
            var colors = new Color[vertices.Length];
            var lines = new List<Vector3>();
            cells.Clear();
            for (int i = 0; i < tiles.Length; i++)
            {
                Terrain tile = tiles[i];
                var cell = new Vector2Int(Mathf.RoundToInt(tile.transform.position.x / TileSize),
                    Mathf.RoundToInt(tile.transform.position.z / TileSize));
                cells.Add(cell, tile);
                int v = i * 4;
                vertices[v] = Corner(tile, 0, 0); vertices[v + 1] = Corner(tile, 0, 1);
                vertices[v + 2] = Corner(tile, 1, 1); vertices[v + 3] = Corner(tile, 1, 0);
                for (int c = 0; c < 4; c++) colors[v + c] = Color.white;
                // One coarse guide every 1,024 m, sampled at native tile corners.
                if (cell.x % 4 == 0) { lines.Add(vertices[v]); lines.Add(vertices[v + 1]); }
                if (cell.y % 4 == 0) { lines.Add(vertices[v]); lines.Add(vertices[v + 3]); }
            }
            var outline = new List<Vector3>();
            foreach (var edge in FootprintEdges(cells.Keys))
            {
                Terrain tile = cells[edge.cell];
                Vector2Int d = edge.direction;
                if (d == Vector2Int.left) { outline.Add(Corner(tile, 0, 0)); outline.Add(Corner(tile, 0, 1)); }
                else if (d == Vector2Int.right) { outline.Add(Corner(tile, 1, 0)); outline.Add(Corner(tile, 1, 1)); }
                else if (d == Vector2Int.down) { outline.Add(Corner(tile, 0, 0)); outline.Add(Corner(tile, 1, 0)); }
                else { outline.Add(Corner(tile, 0, 1)); outline.Add(Corner(tile, 1, 1)); }
            }
            grid = lines.ToArray(); perimeter = outline.ToArray();
            mesh = new Mesh { name = "Greater Wasteland editor overview", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.colors = colors;
            mesh.RecalculateBounds();
            player = GreaterWastelandPlayerPlacement.FindPlayer(scene);
            visibilityDirty = true;
            UpdateHiddenTriangles();
        }

        private static void UpdateHiddenTriangles()
        {
            if (!visibilityDirty || mesh == null) return;
            var indices = new List<int>();
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null || !SceneVisibilityManager.instance.IsHidden(tiles[i].gameObject)) continue;
                int v = i * 4;
                indices.Add(v); indices.Add(v + 1); indices.Add(v + 2);
                indices.Add(v); indices.Add(v + 2); indices.Add(v + 3);
            }
            // Visible native terrain supplies the detailed surface; only hidden tiles need proxies.
            mesh.SetTriangles(indices, 0, false);
            visibilityDirty = false;
        }

        private static void Draw(SceneView view)
        {
            if (!Enabled || EditorApplication.isPlayingOrWillChangePlaymode
                || SceneManager.GetActiveScene().path != GreaterWastelandTerrainWorkspace.ScenePath
                || Event.current.type != EventType.Repaint) return;
            RebuildIfNeeded();
            if (mesh == null) return;
            UpdateHiddenTriangles();
            if (material == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                if (shader != null)
                {
                    material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_Cull", (int)CullMode.Off);
                    material.SetInt("_ZWrite", 0);
                    material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                    material.SetColor("_Color", new Color(.32f, .62f, .68f, .35f));
                }
            }
            if (material != null && TriangleCount > 0 && material.SetPass(0)) Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
            using (new Handles.DrawingScope(new Color(.35f, .7f, .8f, .3f))) Handles.DrawLines(grid);
            using (new Handles.DrawingScope(new Color(.3f, .9f, 1f, .9f))) Handles.DrawLines(perimeter);
            DrawPlayerContext();
            Handles.BeginGUI();
            GUI.Label(new Rect(12, view.position.height - 76, 440, 24),
                "Blue terrain: lightweight overview. Cyan outline: terrain boundary.", EditorStyles.helpBox);
            Handles.EndGUI();
        }

        private static void DrawPlayerContext()
        {
            if (player == null) return;
            Vector3 position = player.transform.position;
            var cell = new Vector2Int(Mathf.FloorToInt(position.x / TileSize), Mathf.FloorToInt(position.z / TileSize));
            using (new Handles.DrawingScope(Color.cyan))
            {
                Handles.DrawWireDisc(position, Vector3.up, HandleUtility.GetHandleSize(position) * .15f);
                if (!cells.TryGetValue(cell, out Terrain terrain))
                { Handles.Label(position, "Booter — outside terrain footprint"); return; }
                Vector3 origin = terrain.transform.position;
                float y = origin.y + terrain.terrainData.GetInterpolatedHeight((position.x - origin.x) / TileSize,
                    (position.z - origin.z) / TileSize);
                Vector3 ground = new(position.x, y, position.z);
                Handles.DrawDottedLine(position, ground, 4f);
                Handles.Label(position, "Booter — player (dotted line to terrain)");
            }
        }

        private static void Invalidate()
        {
            if (mesh != null) Object.DestroyImmediate(mesh);
            mesh = null; tiles = null; cells.Clear(); player = null;
            visibilityDirty = true;
            SceneView.RepaintAll();
        }

        private static void Release()
        {
            Invalidate();
            if (material != null) Object.DestroyImmediate(material);
            material = null;
        }
    }
}
