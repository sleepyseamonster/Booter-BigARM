using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DBaseRockAuthoring))]
    public sealed class TopDown3DBaseRockAuthoringEditor : UnityEditor.Editor
    {
        [MenuItem("GameObject/Booter & BigARM/Top Down 3D/Subdivided Cube Rock", false, 21)]
        private static void CreateBaseRock(MenuCommand command)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.RockMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException("The project regular rock material could not be found.");
            }

            var rockObject = new GameObject("Cube Rock");
            Undo.RegisterCreatedObjectUndo(rockObject, "Create Cube Rock");
            GameObjectUtility.SetParentAndAlign(rockObject, command.context as GameObject);
            var authoring = rockObject.AddComponent<TopDown3DBaseRockAuthoring>();
            authoring.Configure(material);
            authoring.SetSeed(CreateNewSeed());
            Build(authoring, "Generate Cube Rock");
            Selection.activeGameObject = rockObject;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "generatedMesh");
            serializedObject.ApplyModifiedProperties();

            var authoring = (TopDown3DBaseRockAuthoring)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This starts as a visibly cubic, closed, subdivided mesh. Add Cube Rock creates another live cube beside this one; move, rotate, and scale the cubes normally in the Scene view. Roughness controls stay at zero until you are ready to begin shaping rocks.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(authoring.RockMaterial == null))
            {
                if (GUILayout.Button("Add Cube Rock to Scene"))
                {
                    AddCubeRock(authoring);
                }

                if (GUILayout.Button("Regenerate This Cube"))
                {
                    Build(authoring, "Regenerate Cube Rock");
                }
            }
        }

        private static void AddCubeRock(TopDown3DBaseRockAuthoring source)
        {
            const string undoName = "Add Cube Rock";
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                var rockObject = new GameObject("Cube Rock");
                Undo.RegisterCreatedObjectUndo(rockObject, undoName);
                rockObject.transform.SetParent(source.transform.parent, true);
                rockObject.transform.position = source.transform.position
                    + source.transform.right * (source.Size.x + 1f);
                rockObject.transform.rotation = source.transform.rotation;

                var authoring = Undo.AddComponent<TopDown3DBaseRockAuthoring>(rockObject);
                authoring.CopyControlsFrom(source);
                authoring.SetSeed(CreateNewSeed());
                Build(authoring, undoName);
                Selection.activeGameObject = rockObject;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static void Build(TopDown3DBaseRockAuthoring authoring, string undoName)
        {
            if (authoring == null) throw new ArgumentNullException(nameof(authoring));
            if (authoring.RockMaterial == null)
                throw new InvalidOperationException("Assign a rock material before generating a base rock.");

            var mesh = BuildMesh(authoring);
            var filter = authoring.GetComponent<MeshFilter>();
            if (filter == null) filter = Undo.AddComponent<MeshFilter>(authoring.gameObject);
            var renderer = authoring.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = Undo.AddComponent<MeshRenderer>(authoring.gameObject);
            var collider = authoring.GetComponent<MeshCollider>();
            if (collider == null) collider = Undo.AddComponent<MeshCollider>(authoring.gameObject);

            var previousMesh = filter.sharedMesh;
            Undo.RegisterCreatedObjectUndo(mesh, undoName);
            Undo.RecordObject(filter, undoName);
            Undo.RecordObject(collider, undoName);
            Undo.RecordObject(authoring, undoName);
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = authoring.RockMaterial;
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
            authoring.SetGeneratedMesh(mesh);
            EditorUtility.SetDirty(authoring);
            EditorSceneManager.MarkSceneDirty(authoring.gameObject.scene);

            if (previousMesh != null && (previousMesh.hideFlags & HideFlags.DontSaveInEditor) != 0)
            {
                UnityEngine.Object.DestroyImmediate(previousMesh);
            }
        }

        private static Mesh BuildMesh(TopDown3DBaseRockAuthoring authoring)
        {
            var resolution = authoring.Subdivisions;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            AppendFace(resolution, new Vector3Int(resolution, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, 0, 1), new Dictionary<Vector3Int, int>(), vertices, triangles, authoring);
            AppendFace(resolution, Vector3Int.zero, new Vector3Int(0, 0, 1), new Vector3Int(0, 1, 0), new Dictionary<Vector3Int, int>(), vertices, triangles, authoring);
            AppendFace(resolution, new Vector3Int(0, resolution, 0), new Vector3Int(0, 0, 1), new Vector3Int(1, 0, 0), new Dictionary<Vector3Int, int>(), vertices, triangles, authoring);
            AppendFace(resolution, Vector3Int.zero, new Vector3Int(1, 0, 0), new Vector3Int(0, 0, 1), new Dictionary<Vector3Int, int>(), vertices, triangles, authoring);
            AppendFace(resolution, new Vector3Int(0, 0, resolution), new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Dictionary<Vector3Int, int>(), vertices, triangles, authoring);
            AppendFace(resolution, Vector3Int.zero, new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0), new Dictionary<Vector3Int, int>(), vertices, triangles, authoring);

            var solid = TopDown3DRockMeshTopology.Normalize(vertices, triangles);
            var report = TopDown3DRockMeshTopology.Validate(solid);
            if (!report.IsValid || report.SignedVolume <= 0d)
            {
                throw new InvalidOperationException($"Generated cube rock is not a valid closed solid: {report.Error}");
            }

            var mesh = new Mesh { name = $"Cube Rock {authoring.Seed}" };
            mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AppendFace(
            int resolution,
            Vector3Int origin,
            Vector3Int axisU,
            Vector3Int axisV,
            IDictionary<Vector3Int, int> vertexByCoordinate,
            IList<Vector3> vertices,
            IList<int> triangles,
            TopDown3DBaseRockAuthoring authoring)
        {
            for (var v = 0; v < resolution; v++)
            {
                for (var u = 0; u < resolution; u++)
                {
                    var lowerLeft = GetVertex(origin + axisU * u + axisV * v, resolution, vertexByCoordinate, vertices, authoring);
                    var lowerRight = GetVertex(origin + axisU * (u + 1) + axisV * v, resolution, vertexByCoordinate, vertices, authoring);
                    var upperRight = GetVertex(origin + axisU * (u + 1) + axisV * (v + 1), resolution, vertexByCoordinate, vertices, authoring);
                    var upperLeft = GetVertex(origin + axisU * u + axisV * (v + 1), resolution, vertexByCoordinate, vertices, authoring);
                    triangles.Add(lowerLeft);
                    triangles.Add(lowerRight);
                    triangles.Add(upperRight);
                    triangles.Add(lowerLeft);
                    triangles.Add(upperRight);
                    triangles.Add(upperLeft);
                }
            }
        }

        private static int GetVertex(
            Vector3Int coordinate,
            int resolution,
            IDictionary<Vector3Int, int> vertexByCoordinate,
            IList<Vector3> vertices,
            TopDown3DBaseRockAuthoring authoring)
        {
            if (vertexByCoordinate.TryGetValue(coordinate, out var index)) return index;

            var unitPosition = new Vector3(
                coordinate.x / (float)resolution - 0.5f,
                coordinate.y / (float)resolution - 0.5f,
                coordinate.z / (float)resolution - 0.5f);
            var axisNoise = new Vector3(
                1f + SignedNoise(authoring.Seed, coordinate.x, coordinate.y, coordinate.z, 13) * authoring.AxisVariation,
                1f + SignedNoise(authoring.Seed, coordinate.x, coordinate.y, coordinate.z, 37) * authoring.AxisVariation,
                1f + SignedNoise(authoring.Seed, coordinate.x, coordinate.y, coordinate.z, 71) * authoring.AxisVariation);
            var position = Vector3.Scale(Vector3.Scale(unitPosition, authoring.Size), axisNoise);
            var roughness = 1f + SignedNoise(authoring.Seed, coordinate.x, coordinate.y, coordinate.z, 101) * authoring.SurfaceRoughness;
            position *= roughness;

            index = vertices.Count;
            vertices.Add(position);
            vertexByCoordinate.Add(coordinate, index);
            return index;
        }

        private static float SignedNoise(int seed, int x, int y, int z, int salt)
        {
            unchecked
            {
                var hash = seed;
                hash = hash * 486187739 + x * 16777619;
                hash = hash * 486187739 + y * 374761393;
                hash = hash * 486187739 + z * 668265263;
                hash = hash * 486187739 + salt;
                hash ^= hash >> 13;
                hash *= 1274126177;
                return ((hash & 0x7fffffff) / (float)int.MaxValue) * 2f - 1f;
            }
        }

        private static int CreateNewSeed()
        {
            return Guid.NewGuid().GetHashCode();
        }
    }
}
