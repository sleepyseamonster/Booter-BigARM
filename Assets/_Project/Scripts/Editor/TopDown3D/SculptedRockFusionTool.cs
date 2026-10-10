using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    /// <summary>Editor bake only. Exact union runs in a task-owned background Blender process.</summary>
    public sealed class SculptedRockFusionTool : EditorWindow
    {
        public const string BlenderPreference = "BooterBigArm.SculptedRockFusion.Blender";
        public const string OutputFolder = "Assets/_Project/Art/Environment/Rocks/Fused";
        private Process process;
        private Snapshot snapshot;
        private string output, message, executable;
        private double started;
        [MenuItem("Booter & BigARM/Geology Studies/Fuse Selected Sculpted Rocks")]
        private static void Open() => GetWindow<SculptedRockFusionTool>(false, "Fuse Sculpted Rocks", false);
        private void OnEnable()
        { minSize = new Vector2(420, 230); executable = EditorPrefs.GetString(BlenderPreference, Environment.GetEnvironmentVariable("BOOTER_BLENDER_EDITOR") ?? ""); }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Marked sculpted rocks fuse automatically after moving in Edit Mode. Originals stay editable. This button creates a saved, final fused asset from the selected roots. LOD0 only; URP Lit texture sets are retained as material slots.", MessageType.Info);
            EditorGUI.BeginChangeCheck(); executable = EditorGUILayout.TextField("Blender executable", executable ?? "");
            if (EditorGUI.EndChangeCheck()) EditorPrefs.SetString(BlenderPreference, executable);
            if (GUILayout.Button("Browse Blender executable"))
            {
                var chosen = EditorUtility.OpenFilePanel("Blender executable", Path.GetDirectoryName(executable ?? "") ?? "", "exe");
                if (!string.IsNullOrEmpty(chosen)) { executable = chosen; EditorPrefs.SetString(BlenderPreference, chosen); }
            }
            using (new EditorGUI.DisabledScope(process != null || EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Fuse Selected Rocks")) Begin();
            if (process != null && GUILayout.Button("Cancel bake")) Stop();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.None);
        }
        private void Begin()
        {
            try
            {
                if (!File.Exists(executable)) throw new InvalidOperationException("Choose the installed Blender executable.");
                snapshot = Capture(Selection.gameObjects);
                string directory = Path.GetFullPath("Logs/RockFusion/" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
                string input = Path.Combine(directory, "input.json"); output = Path.Combine(directory, "output.json");
                File.WriteAllText(input, JsonUtility.ToJson(snapshot.input));
                process = StartBackend(executable, input, output);
                started = EditorApplication.timeSinceStartup; message = "Fusing in background… original rocks remain editable.";
                EditorApplication.update += Poll;
            }
            catch (Exception e) { message = e.Message; Stop(); }
        }
        public static Process StartBackend(string executable, string input, string output)
        {
            string script = Path.GetFullPath("Tools/Art/Blender/fuse_rock_meshes.py");
            var process = new Process { StartInfo = new ProcessStartInfo
            {
                FileName = executable, Arguments = "--background --factory-startup --disable-autoexec --python \"" + script + "\" -- \"" + input + "\" \"" + output + "\"",
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
            } };
            process.Start(); return process;
        }
        private void Poll()
        {
            if (process == null) return;
            if (EditorApplication.timeSinceStartup - started > 120) { message = "Bake timed out. Originals preserved."; Stop(); return; }
            if (!process.HasExited) return;
            try
            {
                var result = ReadResult(output);
                if (process.ExitCode != 0) throw new InvalidOperationException("Blender did not complete the bake.");
                if (snapshot.roots.Any(r => r == null || !r.activeInHierarchy) || snapshot.roots.Any(r => r.scene != snapshot.scene))
                    throw new InvalidOperationException("Sources changed or their scene closed. Originals preserved; bake discarded.");
                var current = Capture(snapshot.roots);
                if (current.origin != snapshot.origin || !current.materials.SequenceEqual(snapshot.materials) || JsonUtility.ToJson(current.input) != JsonUtility.ToJson(snapshot.input))
                    throw new InvalidOperationException("Source geometry, material or transforms changed during the bake. Bake discarded.");
                var mesh = MakeMesh(result); string key = Digest(JsonUtility.ToJson(snapshot.input) + result.blender);
                EnsureFolder(OutputFolder); string stem = OutputFolder + "/RockFusion_" + key;
                var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(stem + ".asset");
                if (savedMesh == null) { AssetDatabase.CreateAsset(mesh, stem + ".asset"); savedMesh = mesh; }
                else Object.DestroyImmediate(mesh);
                var baked = new GameObject("Fused Sculpted Rocks", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                baked.GetComponent<MeshFilter>().sharedMesh = savedMesh; baked.GetComponent<MeshRenderer>().sharedMaterials = snapshot.materials;
                baked.AddComponent<BooterBigArm.TopDown3D.SculptedRockFusionParticipant>();
                // Static concave collision follows the fused outer surface; do not attach a dynamic Rigidbody.
                baked.GetComponent<MeshCollider>().sharedMesh = savedMesh;
                string prefabPath = stem + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) PrefabUtility.SaveAsPrefabAsset(baked, prefabPath);
                Object.DestroyImmediate(baked);
                if (!File.Exists(stem + ".fusion.json"))
                {
                    File.WriteAllText(stem + ".fusion.json", JsonUtility.ToJson(result.report, true));
                    AssetDatabase.ImportAsset(stem + ".fusion.json");
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), snapshot.scene);
                Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Fuse sculpted rocks");
                Undo.RegisterCreatedObjectUndo(instance, "Place fused rock");
                instance.transform.position = snapshot.origin;
                var parent = snapshot.roots[0].transform.parent;
                if (snapshot.roots.All(r => r.transform.parent == parent)) instance.transform.SetParent(parent, true);
                foreach (var root in snapshot.roots) { Undo.RecordObject(root, "Hide preserved fusion source"); root.SetActive(false); }
                Undo.CollapseUndoOperations(undo); Selection.activeGameObject = instance; SceneView.RepaintAll();
                message = $"Fused {snapshot.roots.Length} rocks: {result.report.triangles} triangles, {result.report.seam_edges} join edges, no buried faces. Save the scene to keep this arrangement. Undo restores the originals.";
            }
            catch (Exception e) { message = e.Message; }
            finally { Stop(); Repaint(); }
        }
        private void OnDisable() => Stop();
        private void Stop()
        {
            EditorApplication.update -= Poll;
            if (process != null) { if (!process.HasExited) process.Kill(); process.Dispose(); process = null; }
        }
        public static Snapshot Capture(GameObject[] selection)
        {
            var roots = selection.Where(r => r != null && r.activeInHierarchy && r.scene.IsValid())
                .Where(r => !selection.Any(other => other != null && other != r && r.transform.IsChildOf(other.transform)))
                .Distinct().OrderBy(r => GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(), StringComparer.Ordinal).ToArray();
            if (roots.Length < 2 || roots.Length > 16 || roots.Any(r => r.scene != roots[0].scene))
                throw new InvalidOperationException("Select 2–16 separate rock roots in one loaded scene.");
            var renderers = new List<MeshRenderer>();
            foreach (var root in roots)
            {
                var lod = root.GetComponent<LODGroup>();
                var choices = lod != null ? lod.GetLODs()[0].renderers.OfType<MeshRenderer>().ToArray() : root.GetComponentsInChildren<MeshRenderer>();
                if (choices.Length != 1) throw new InvalidOperationException("Each selected root must contain exactly one visible mesh, or one LOD0 renderer.");
                renderers.Add(choices[0]);
            }
            if (renderers.Any(r => r.sharedMaterials.Any(m => m == null || !EditorUtility.IsPersistent(m) || m.shader.name != "Universal Render Pipeline/Lit")))
                throw new InvalidOperationException("Source texture sets need saved URP Lit materials.");
            var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            Bounds bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            Vector3 origin = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var sources = new List<InputMesh>(); int total = 0;
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null || mesh.subMeshCount != renderer.sharedMaterials.Length || mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount)
                    throw new InvalidOperationException("Each rock needs triangle submeshes with matching materials, UVs and normals.");
                var matrix = Matrix4x4.Translate(-origin) * renderer.transform.localToWorldMatrix;
                if (matrix.determinant <= 0) throw new InvalidOperationException("Apply mirrored/zero scales before fusing.");
                var normalMatrix = matrix.inverse.transpose;
                Vector4[] tangents = mesh.tangents;
                if (tangents.Length != mesh.vertexCount)
                {
                    var copy = Object.Instantiate(mesh);
                    try { copy.RecalculateTangents(); tangents = copy.tangents; }
                    finally { Object.DestroyImmediate(copy); }
                }
                total += mesh.triangles.Length / 3;
                var slots = new List<int>();
                for (int slot = 0; slot < mesh.subMeshCount; slot++)
                    slots.AddRange(Enumerable.Repeat(Array.IndexOf(materials, renderer.sharedMaterials[slot]), mesh.GetTriangles(slot).Length / 3));
                sources.Add(new InputMesh { materialIndices = slots.ToArray(), vertices = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray(),
                    normals = mesh.normals.Select(n => normalMatrix.MultiplyVector(n).normalized).ToArray(),
                    tangents = tangents.Select(t => { var direction = matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized; return new Vector4(direction.x,direction.y,direction.z,t.w); }).ToArray(),
                    uv = mesh.uv, triangles = mesh.triangles });
            }
            if (total > 100000) throw new InvalidOperationException("Reduce source meshes first: this authoring tool is bounded to 100,000 input triangles.");
            return new Snapshot { roots = roots, materials = materials, origin = origin, scene = roots[0].scene,
                input = new Input { sources = sources.ToArray(), materialIds = materials.Select(m => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m))).ToArray() } };
        }
        public static Result ReadResult(string path)
        {
            if (!File.Exists(path)) throw new InvalidOperationException("Blender did not produce a result. Check the installed executable.");
            var result = JsonUtility.FromJson<Result>(File.ReadAllText(path));
            if (!string.IsNullOrEmpty(result.error)) throw new InvalidOperationException(result.error);
            if (result.version != 2 || result.report == null || result.report.nonmanifold != 0 || result.report.degenerate != 0 || result.report.loose != 0
                || result.report.volume <= 0 || result.report.interior_faces != 0 || result.report.components != 1)
                throw new InvalidOperationException("Invalid fusion result.");
            return result;
        }
        public static Mesh MakeMesh(Result result)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var tangents = new List<Vector4>();
            var lookup = new Dictionary<(Vector3, Vector3, Vector4, Vector2), int>(); var indices = new int[result.triangles.Length];
            for (int i = 0; i < result.triangles.Length; i++)
            {
                int corner = result.triangles[i];
                var p = new Vector3(result.vertices[corner * 3], result.vertices[corner * 3 + 1], result.vertices[corner * 3 + 2]);
                var n = new Vector3(result.normals[corner * 3], result.normals[corner * 3 + 1], result.normals[corner * 3 + 2]);
                var u = new Vector2(result.uv[corner * 2], result.uv[corner * 2 + 1]);
                var t = new Vector4(result.tangents[corner*4],result.tangents[corner*4+1],result.tangents[corner*4+2],result.tangents[corner*4+3]);
                var key = (p, n, t, u);
                if (!lookup.TryGetValue(key, out int index)) { index = vertices.Count; lookup.Add(key, index); vertices.Add(p); normals.Add(n); tangents.Add(t); uv.Add(u); }
                indices[i] = index;
            }
            var mesh = new Mesh { name = "Fused Sculpted Rock", indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.subMeshCount = result.materialCount;
            for (int slot = 0; slot < result.materialCount; slot++)
            {
                var submesh = new List<int>();
                for (int tri = 0; tri < result.materialIndices.Length; tri++)
                    if (result.materialIndices[tri] == slot) { submesh.Add(indices[tri * 3]); submesh.Add(indices[tri * 3 + 1]); submesh.Add(indices[tri * 3 + 2]); }
                mesh.SetTriangles(submesh, slot);
            }
            mesh.SetTangents(tangents); mesh.RecalculateBounds(); return mesh;
        }
        private static string Digest(string value)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").Substring(0, 16); }
        private static void EnsureFolder(string path)
        { if (!AssetDatabase.IsValidFolder(path)) { EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path)); } }
        public sealed class Snapshot { public GameObject[] roots; public Material[] materials; public Vector3 origin; public Scene scene; public Input input; }
        [Serializable] public sealed class Input { public int version = 2; public string[] materialIds; public InputMesh[] sources; }
        [Serializable] public sealed class InputMesh { public Vector3[] vertices, normals; public Vector4[] tangents; public Vector2[] uv; public int[] triangles, materialIndices; }
        [Serializable] public sealed class Result { public int version, materialCount; public string blender, error; public float[] vertices, normals, tangents, uv; public int[] triangles, materialIndices; public Report report; }
        [Serializable] public sealed class Report { public int nonmanifold, degenerate, loose, components, seam_edges, interior_faces, triangles, input_triangles; public double volume, input_volume; }
    }
}
