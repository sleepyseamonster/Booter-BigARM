using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    /// <summary>Transient Edit Mode exterior preview. Never moves, disables or saves source objects.</summary>
    [InitializeOnLoad]
    public static class SculptedRockLiveFusion
    {
        private sealed class Group
        {
            public GameObject[] roots;
            public string signature, error, output;
            public double changed;
            public bool attempted;
            public Process process;
            public SculptedRockFusionTool.Snapshot snapshot;
            public GameObject preview;
            public Mesh mesh;
            public readonly Dictionary<Renderer, bool> visibility = new Dictionary<Renderer, bool>();
        }
        private static readonly Dictionary<string, Group> groups = new Dictionary<string, Group>();
        private static double nextTick;
        public static bool Suspended;
        public static int PreviewCount => groups.Values.Count(g => g.preview != null);
        static SculptedRockLiveFusion()
        {
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.quitting += Clear;
            EditorApplication.playModeStateChanged += _ => Clear();
            Undo.undoRedoPerformed += Clear;
            EditorApplication.projectChanged += Clear;
            Selection.selectionChanged += RedirectSelection;
        }
        private static MeshRenderer RendererOf(GameObject root)
        {
            var lod = root.GetComponent<LODGroup>();
            return lod != null ? lod.GetLODs().FirstOrDefault().renderers?.OfType<MeshRenderer>().FirstOrDefault()
                : root.GetComponentInChildren<MeshRenderer>();
        }
        public static void Tick()
        {
            if (Suspended || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            { if (groups.Count > 0) Clear(); return; }
            double now = EditorApplication.timeSinceStartup;
            if (now < nextTick) return; nextTick = now + .15;
            var members = Resources.FindObjectsOfTypeAll<SculptedRockFusionParticipant>()
                .Where(p => p != null && p.LiveFusion && p.gameObject.activeInHierarchy && p.gameObject.scene.IsValid() && p.gameObject.scene.isLoaded
                    && !EditorUtility.IsPersistent(p) && !EditorSceneManager.IsPreviewScene(p.gameObject.scene)
                    && PrefabStageUtility.GetPrefabStage(p.gameObject) == null && RendererOf(p.gameObject) != null)
                .OrderBy(p => p.GetInstanceID()).ToArray();
            var pending = new HashSet<SculptedRockFusionParticipant>(members);
            var found = new HashSet<string>();
            while (pending.Count > 0)
            {
                var first = pending.First(); pending.Remove(first);
                var connected = new List<SculptedRockFusionParticipant> { first };
                for (int i = 0; i < connected.Count; i++)
                {
                    var a = connected[i]; var bounds = RendererOf(a.gameObject).bounds;
                    foreach (var b in pending.ToArray())
                    {
                        if (a.gameObject.scene == b.gameObject.scene && a.FusionSet == b.FusionSet
                            && bounds.Intersects(RendererOf(b.gameObject).bounds))
                        { connected.Add(b); pending.Remove(b); }
                    }
                }
                if (connected.Count < 2) continue;
                var roots = connected.OrderBy(p => p.GetInstanceID()).Select(p => p.gameObject).ToArray();
                string key = string.Join("_", roots.Select(r => r.GetInstanceID())); found.Add(key);
                string signature = string.Join("|", roots.Select(r =>
                {
                    var renderer = RendererOf(r); var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    return renderer.transform.localToWorldMatrix.ToString("R") + (mesh == null ? "missing" : mesh.GetInstanceID().ToString())
                        + string.Join(",", renderer.sharedMaterials.Select(m => m == null ? 0 : m.GetInstanceID()));
                }));
                if (!groups.TryGetValue(key, out var group))
                { group = new Group { roots = roots, signature = signature, changed = now }; groups.Add(key, group); }
                if (group.signature != signature)
                { Dispose(group); group.signature = signature; group.changed = now; group.attempted = false; group.error = null; }
                if (group.process != null) Complete(group, now);
                // One worker at a time, debounced after transforms settle. No automatic asset publication.
                if (!group.attempted && now - group.changed >= .35 && !groups.Values.Any(g => g.process != null)) Start(group);
            }
            foreach (var key in groups.Keys.Where(k => !found.Contains(k)).ToArray()) { Dispose(groups[key]); groups.Remove(key); }
        }
        private static void Start(Group group)
        {
            group.attempted = true;
            try
            {
                string executable = EditorPrefs.GetString(SculptedRockFusionTool.BlenderPreference, Environment.GetEnvironmentVariable("BOOTER_BLENDER_EDITOR") ?? "");
                if (!File.Exists(executable)) throw new InvalidOperationException("Configure Blender in the Fuse Sculpted Rocks panel.");
                group.snapshot = SculptedRockFusionTool.Capture(group.roots);
                string folder = Path.GetFullPath("Logs/RockFusion/Live_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
                string input = Path.Combine(folder, "input.json"); group.output = Path.Combine(folder, "output.json");
                File.WriteAllText(input, JsonUtility.ToJson(group.snapshot.input));
                group.process = SculptedRockFusionTool.StartBackend(executable, input, group.output);
                group.changed = EditorApplication.timeSinceStartup;
            }
            catch (Exception e) { group.error = e.Message; }
        }
        private static void Complete(Group group, double now)
        {
            if (now - group.changed > 120) { Dispose(group); group.error = "Live fusion timed out; source rocks remain visible."; return; }
            if (!group.process.HasExited) return;
            try
            {
                var result = SculptedRockFusionTool.ReadResult(group.output);
                if (group.process.ExitCode != 0) throw new InvalidOperationException("Live union failed; source rocks retained.");
                var current = SculptedRockFusionTool.Capture(group.roots);
                if (current.origin != group.snapshot.origin || JsonUtility.ToJson(current.input) != JsonUtility.ToJson(group.snapshot.input))
                    throw new InvalidOperationException("Source changed; move it again to rebuild.");
                group.mesh = SculptedRockFusionTool.MakeMesh(result); group.mesh.hideFlags = HideFlags.HideAndDontSave;
                group.preview = new GameObject("Live Rock Fusion", typeof(MeshFilter), typeof(MeshRenderer));
                group.preview.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(group.preview, group.snapshot.scene);
                group.preview.transform.position = group.snapshot.origin;
                group.preview.GetComponent<MeshFilter>().sharedMesh = group.mesh;
                group.preview.GetComponent<MeshRenderer>().sharedMaterials = group.snapshot.materials;
                foreach (var root in group.roots) foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                { group.visibility[renderer] = renderer.forceRenderingOff; renderer.forceRenderingOff = true; }
                SceneView.RepaintAll();
            }
            catch (Exception e) { Dispose(group); group.error = e.Message; }
            finally { if (group.process != null) { group.process.Dispose(); group.process = null; } }
        }
        private static void Dispose(Group group)
        {
            if (group.process != null) { if (!group.process.HasExited) group.process.Kill(); group.process.Dispose(); group.process = null; }
            foreach (var pair in group.visibility) if (pair.Key != null) pair.Key.forceRenderingOff = pair.Value;
            group.visibility.Clear();
            if (group.preview != null) Object.DestroyImmediate(group.preview); if (group.mesh != null) Object.DestroyImmediate(group.mesh);
            group.preview = null; group.mesh = null;
        }
        public static void Clear() { foreach (var group in groups.Values) Dispose(group); groups.Clear(); }
        public static GameObject[] SourcesFor(GameObject root) => groups.Values.FirstOrDefault(g => g.roots.Contains(root))?.roots ?? new[] { root };
        public static string StatusFor(GameObject root)
        {
            if (root.GetComponent<SculptedRockFusionParticipant>()?.LiveFusion != true)
                return "Manual fusion: select overlapping rock roots and click Fuse Selected Rocks in the fusion panel.";
            var group = groups.Values.FirstOrDefault(g => g.roots.Contains(root));
            return group == null ? "Ready: overlap another marked rock under the same parent to fuse."
                : group.error ?? (group.preview != null ? "Live fused exterior; every source transform remains editable."
                    : group.process != null ? "Rebuilding live fusion…" : "Waiting for transforms to settle…");
        }
        private static void RedirectSelection()
        {
            var group = groups.Values.FirstOrDefault(g => g.preview == Selection.activeGameObject);
            if (group != null) Selection.activeGameObject = group.roots.FirstOrDefault(r => r != null);
        }
    }

    [CustomEditor(typeof(SculptedRockFusionParticipant)), CanEditMultipleObjects]
    public sealed class SculptedRockFusionParticipantEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var participant = (SculptedRockFusionParticipant)target;
            EditorGUILayout.HelpBox(SculptedRockLiveFusion.StatusFor(participant.gameObject), MessageType.Info);
            foreach (var root in SculptedRockLiveFusion.SourcesFor(participant.gameObject))
                if (root != null && GUILayout.Button("Edit " + root.name)) Selection.activeGameObject = root;
            if (GUILayout.Button("Open Final Fusion Bake Panel"))
                EditorWindow.GetWindow<SculptedRockFusionTool>(false, "Fuse Sculpted Rocks", false);
        }
    }
}
