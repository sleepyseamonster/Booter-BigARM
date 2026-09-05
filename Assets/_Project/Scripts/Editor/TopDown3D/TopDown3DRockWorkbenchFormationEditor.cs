using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DRockWorkbenchFormationAuthoring))]
    public sealed class TopDown3DRockWorkbenchFormationEditor : UnityEditor.Editor
    {
        private const string CreateMenuPath =
            "GameObject/Booter & BigARM/Top Down 3D/Create Formation From Selected Rocks";
        private bool showAdvanced;

        internal static bool CanCreateFromSelection => CollectSelectedWorkbenches().Count >= 2;

        public override void OnInspectorGUI()
        {
            var formation = (TopDown3DRockWorkbenchFormationAuthoring)target;
            EditorGUILayout.LabelField("Rock Formation Generator", EditorStyles.boldLabel);
            if (IsHiddenInScene(formation))
            {
                EditorGUILayout.HelpBox(
                    "This Rock Formation Workbench is hidden by Unity's Scene Visibility control. "
                    + "Its generated mesh still exists, so Unity may show only the selection outline and its shadow.",
                    MessageType.Warning);
                if (GUILayout.Button("Show Formation In Scene"))
                {
                    ShowInScene(formation);
                }
            }

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            var archetypeProperty = serializedObject.FindProperty("formationArchetype");
            EditorGUILayout.PropertyField(
                archetypeProperty,
                new GUIContent("Formation Type"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedOverallSize"),
                new GUIContent("Width", "Physical width and depth of the formation in meters."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedHeight"),
                new GUIContent("Height", "Physical formation height in meters, independent from width."));
            var usesEditableFractureCuts =
                (TopDown3DRockFormationArchetype)archetypeProperty.enumValueIndex
                == TopDown3DRockFormationArchetype.ScatteredRocks;
            using (new EditorGUI.DisabledScope(!usesEditableFractureCuts))
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("memberMajorFractures"),
                    new GUIContent(
                        "Major Fractures",
                        "Editable subtractive cuts for independent scattered rocks."));
            }
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("memberEdgeDamage"),
                new GUIContent("Edge Damage"));
            var generatorSettingsChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.HelpBox(GetArchetypeDescription(formation.FormationArchetype),
                MessageType.Info);
            if (!usesEditableFractureCuts)
            {
                EditorGUILayout.LabelField(
                    "Connected formations preserve structural joins; use Long Cracks for formation-scale fractures.",
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.LabelField(
                $"Will generate {formation.GeneratedRockCount} editable rocks; detail scales automatically",
                EditorStyles.miniLabel);
            if (GUILayout.Button("Generate New Formation", GUILayout.Height(36f)))
            {
                TopDown3DRockWorkbenchFormationGenerator.GenerateIntoFormation(
                    formation,
                    TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(
                        formation.FormationSeed));
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Update Current Formation With These Settings"))
            {
                TopDown3DRockWorkbenchFormationGenerator.GenerateIntoFormation(
                    formation,
                    formation.FormationSeed);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.LabelField(
                "Generation replaces the member rocks. Unity Undo restores the previous arrangement.",
                EditorStyles.miniLabel);

            if (generatorSettingsChanged)
            {
                TopDown3DRockWorkbenchFormationPreview.RequestRebuild(formation, false);
                Repaint();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Formation Look", EditorStyles.boldLabel);
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            if (formation.FormationArchetype == TopDown3DRockFormationArchetype.ScatteredRocks)
            {
                EditorGUILayout.LabelField("Rock Connections", "Separate Boulders");
            }
            else
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("joinStyle"),
                    new GUIContent("Rock Connections"));
            }
            var connectionsChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            DrawTessellationDetail(serializedObject.FindProperty("memberVoxelSize"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("memberFusionSmoothness"),
                new GUIContent(
                    "Rock Smoothing",
                    "Rounds the blends between source masses inside every member rock."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("memberSurfaceRelaxation"),
                new GUIContent(
                    "Surface Relaxation",
                    "Softens voxel-scale teeth and stair steps after the rock mesh is built while preserving volume and ground contact."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("memberSurfacePreset"),
                new GUIContent("Rock Material Family"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("memberColorVariation"),
                new GUIContent("Rock Color Variation"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("memberDustColor"),
                new GUIContent("Environmental Dust Color"));
            var memberMeshChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("longFractures"),
                new GUIContent("Long Cracks"));
            var fracturesChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (memberMeshChanged)
                TopDown3DRockWorkbenchFormationGenerator.ApplyMemberMeshSettings(formation);
            if (connectionsChanged || fracturesChanged)
            {
                TopDown3DRockWorkbenchFormationPreview.RequestRebuild(formation, false);
            }
            EditorGUILayout.LabelField(
                "Detail 5 is finest and slowest. Rock Smoothing blends source masses; Surface Relaxation cleans the finished mesh.",
                EditorStyles.miniLabel);

            var members = formation.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
            EditorGUILayout.HelpBox(
                $"Editable member rocks: {members.Length}\n{formation.PreviewStatus}",
                formation.PreviewStatus.IndexOf("could not", StringComparison.OrdinalIgnoreCase) >= 0
                    ? MessageType.Error
                    : members.Length < 2
                        ? MessageType.Warning
                        : MessageType.None);

            showAdvanced = EditorGUILayout.Foldout(
                showAdvanced,
                "Advanced",
                true,
                EditorStyles.foldoutHeader);
            if (!showAdvanced) return;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rockMaterial"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("formationSeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fractureSpacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRebuild"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateCollider"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fusedVoxelSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fusedJoinSoftness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("geologicalSeamWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("geologicalSeamStrength"));
            var advancedChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (advancedChanged)
                TopDown3DRockWorkbenchFormationPreview.RequestRebuild(formation, false);

            if (GUILayout.Button("Rebuild Formation Mesh"))
                TopDown3DRockWorkbenchFormationPreview.RequestRebuild(formation, true);
        }

        internal static bool IsHiddenInScene(
            TopDown3DRockWorkbenchFormationAuthoring formation)
        {
            return formation != null
                && SceneVisibilityManager.instance.IsHidden(formation.gameObject);
        }

        internal static void ShowInScene(
            TopDown3DRockWorkbenchFormationAuthoring formation)
        {
            if (formation == null) return;
            SceneVisibilityManager.instance.Show(formation.gameObject, true);
            SceneView.RepaintAll();
        }

        private static void DrawTessellationDetail(SerializedProperty voxelSize)
        {
            var detail = TopDown3DRockWorkbenchAuthoring.VoxelSizeToTessellationDetail(
                voxelSize.floatValue);
            EditorGUI.BeginChangeCheck();
            var adjustedDetail = EditorGUILayout.Slider(
                new GUIContent(
                    "Tessellation Detail",
                    "Controls surface sampling detail for every member rock. 1 rebuilds fastest; 5 produces the finest silhouette."),
                detail,
                TopDown3DRockWorkbenchAuthoring.MinimumTessellationDetail,
                TopDown3DRockWorkbenchAuthoring.MaximumTessellationDetail);
            if (EditorGUI.EndChangeCheck())
            {
                voxelSize.floatValue = TopDown3DRockWorkbenchAuthoring.TessellationDetailToVoxelSize(
                    adjustedDetail);
            }
        }

        private static string GetArchetypeDescription(
            TopDown3DRockFormationArchetype archetype)
        {
            return archetype switch
            {
                TopDown3DRockFormationArchetype.ScatteredRocks =>
                    "Generates a loose field of separate, similarly sized rocks with varied silhouettes, proportions, weathering, and uneven spacing. Every rock remains editable.",
                TopDown3DRockFormationArchetype.PileOfRocks =>
                    "Generates a compact, all-sided mound with broad base stones, an overlapping middle shelf, cap rocks, and retained crevices. Every rock remains editable.",
                TopDown3DRockFormationArchetype.SmallRidgePillars =>
                    "Generates a low, gently wandering stone ridge with several short upright pillar accents and attached edge talus. Every rock remains editable.",
                TopDown3DRockFormationArchetype.HoodooOutcrop =>
                    "Generates a broad, low eroded shelf carrying several isolated stone columns, selected capstones, and sparse perimeter talus. Every rock remains editable.",
                _ =>
                    "Generates one connected outcrop with a dominant anchor, structural rocks, and a readable crevice. Every member rock remains editable."
            };
        }

        [MenuItem(CreateMenuPath, false, 32)]
        internal static void CreateFromSelectedRocks()
        {
            var members = CollectSelectedWorkbenches();
            if (members.Count < 2)
            {
                EditorUtility.DisplayDialog(
                    "Create Rock Formation",
                    "Select at least two Rock Workbench objects (or their source cubes), then run this command again.",
                    "OK");
                return;
            }

            foreach (var member in members)
            {
                if (member.GetComponentInParent<TopDown3DRockWorkbenchFormationAuthoring>() != null)
                {
                    EditorUtility.DisplayDialog(
                        "Create Rock Formation",
                        $"'{member.name}' is already inside a Rock Formation Workbench.",
                        "OK");
                    return;
                }
            }

            var center = CalculateSelectionCenter(members);
            var root = new GameObject("Rock Formation Workbench");
            Undo.RegisterCreatedObjectUndo(root, "Create Rock Formation Workbench");
            root.transform.position = center;
            var formation = Undo.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>(root);
            formation.Configure(
                members[0].RockMaterial,
                Guid.NewGuid().GetHashCode());

            foreach (var member in members)
            {
                Undo.SetTransformParent(
                    member.transform,
                    root.transform,
                    "Group Rock Workbench Into Formation");
            }

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(formation);
            TopDown3DRockWorkbenchFormationPreview.RequestRebuild(formation, true);
        }

        [MenuItem(CreateMenuPath, true)]
        private static bool ValidateCreateFromSelectedRocks()
        {
            return CanCreateFromSelection;
        }

        private static List<TopDown3DRockWorkbenchAuthoring> CollectSelectedWorkbenches()
        {
            var members = new List<TopDown3DRockWorkbenchAuthoring>();
            foreach (var selected in Selection.gameObjects)
            {
                if (selected == null) continue;
                var workbench = selected.GetComponentInParent<TopDown3DRockWorkbenchAuthoring>();
                if (workbench != null && !members.Contains(workbench)) members.Add(workbench);
            }
            return members;
        }

        private static Vector3 CalculateSelectionCenter(
            IReadOnlyList<TopDown3DRockWorkbenchAuthoring> members)
        {
            var bounds = new Bounds(members[0].transform.position, Vector3.zero);
            foreach (var member in members)
            {
                var renderer = member.GetComponent<MeshRenderer>();
                var filter = member.GetComponent<MeshFilter>();
                if (renderer != null && filter != null && filter.sharedMesh != null)
                    bounds.Encapsulate(renderer.bounds);
                else bounds.Encapsulate(member.transform.position);
            }
            return bounds.center;
        }
    }

    [InitializeOnLoad]
    internal static class TopDown3DRockWorkbenchFormationPreview
    {
        private const double ScanIntervalSeconds = 0.08d;
        private const double RebuildDebounceSeconds = 0.15d;
        private static readonly Dictionary<int, PreviewState> States =
            new Dictionary<int, PreviewState>();
        private static double nextScanTime;

        static TopDown3DRockWorkbenchFormationPreview()
        {
            EditorApplication.update += Update;
        }

        internal static void RequestRebuild(
            TopDown3DRockWorkbenchFormationAuthoring formation,
            bool immediate)
        {
            if (!IsUsable(formation)) return;
            var state = GetOrCreateState(formation);
            state.Requested = true;
            state.PendingSignature = CalculateSignature(formation);
            state.DueTime = immediate
                ? 0d
                : EditorApplication.timeSinceStartup + RebuildDebounceSeconds;
        }

        internal static bool TryBuildNow(
            TopDown3DRockWorkbenchFormationAuthoring formation,
            out string error)
        {
            error = string.Empty;
            if (!IsUsable(formation))
            {
                error = "The formation is not in a loaded scene.";
                return false;
            }

            var state = GetOrCreateState(formation);
            return Rebuild(formation, state, CalculateSignature(formation), out error);
        }

        private static void Update()
        {
            if (EditorApplication.isCompiling
                || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.timeSinceStartup < nextScanTime)
            {
                return;
            }

            nextScanTime = EditorApplication.timeSinceStartup + ScanIntervalSeconds;
            var seen = new HashSet<int>();
            var rebuiltThisUpdate = false;
            var formations = Resources.FindObjectsOfTypeAll<TopDown3DRockWorkbenchFormationAuthoring>();
            foreach (var formation in formations)
            {
                if (!IsUsable(formation)) continue;
                var id = formation.GetInstanceID();
                seen.Add(id);
                var state = GetOrCreateState(formation);
                var signature = CalculateSignature(formation);
                if (formation.AutoRebuild && signature != state.PendingSignature)
                {
                    state.PendingSignature = signature;
                    state.Requested = true;
                    state.DueTime = EditorApplication.timeSinceStartup + RebuildDebounceSeconds;
                }

                if (state.Requested
                    && !rebuiltThisUpdate
                    && EditorApplication.timeSinceStartup >= state.DueTime
                    && signature == state.PendingSignature)
                {
                    Rebuild(formation, state, signature, out _);
                    rebuiltThisUpdate = true;
                }
            }

            if (seen.Count == States.Count) return;
            var stale = new List<int>();
            foreach (var pair in States)
            {
                if (seen.Contains(pair.Key)) continue;
                RestoreMembers(pair.Value.Members);
                stale.Add(pair.Key);
            }
            foreach (var id in stale) States.Remove(id);
        }

        private static bool Rebuild(
            TopDown3DRockWorkbenchFormationAuthoring formation,
            PreviewState state,
            int signature,
            out string error)
        {
            state.Requested = false;
            state.LastBuiltSignature = signature;
            state.PendingSignature = signature;
            error = string.Empty;

            var members = formation.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(false);
            state.Members.Clear();
            state.Members.AddRange(members);
            if (members.Length < 2)
            {
                ClearFormationMesh(formation);
                RestoreMembers(state.Members);
                error = "A formation needs at least two active Rock Workbench members.";
                formation.SetPreviewState(null, error);
                return false;
            }

            var formationBounds = CalculateBounds(members);
            var formationSize = formationBounds.size;
            formationSize.x = Mathf.Max(formationSize.x, 0.1f);
            formationSize.y = Mathf.Max(formationSize.y, 0.1f);
            formationSize.z = Mathf.Max(formationSize.z, 0.1f);

            var preserveSeparateRocks = formation.FormationArchetype
                == TopDown3DRockFormationArchetype.ScatteredRocks
                || formation.JoinStyle == TopDown3DRockFormationJoinStyle.PreserveNaturalSeams;
            if (preserveSeparateRocks)
            {
                ClearFormationMesh(formation);
                foreach (var member in members)
                {
                    var renderer = member.GetComponent<MeshRenderer>();
                    var collider = member.GetComponent<MeshCollider>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                        renderer.sharedMaterial = formation.RockMaterial != null
                            ? formation.RockMaterial
                            : member.RockMaterial;
                        TopDown3DRockWorkbenchPreview.ApplySurfaceProperties(
                            member,
                            renderer,
                            formationSize,
                            formation.SurfaceFieldSeed,
                            formation.SurfaceFieldOrigin,
                            formation.LongFractures,
                            formation.FractureSpacing);
                    }
                    if (collider != null) collider.enabled = member.UpdateCollider;
                }

                formation.SetPreviewState(
                    null,
                    formation.FormationArchetype == TopDown3DRockFormationArchetype.ScatteredRocks
                        ? $"SCATTERED ROCKS: {members.Length} separate editable rocks share one geological material field."
                        : $"NATURAL SEAMS: {members.Length} editable rock meshes share one geological pattern and long-fracture field.");
                RepaintViews();
                return true;
            }

            ClearFormationMesh(formation);
            RestoreMembers(state.Members);
            var boxes = new List<TopDown3DRockWorkbenchBox>();
            var groups = new List<TopDown3DRockWorkbenchFieldGroup>(members.Length);
            for (var memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                var member = members[memberIndex];
                var nodes = member.GetComponentsInChildren<TopDown3DRockVolumeNode>(false);
                var memberBoxes = new List<TopDown3DRockWorkbenchBox>(nodes.Length);
                foreach (var node in nodes)
                {
                    if (!node.ContributesToRock || !node.gameObject.activeInHierarchy) continue;
                    var scale = node.transform.lossyScale;
                    if (Mathf.Abs(scale.x) < 0.001f
                        || Mathf.Abs(scale.y) < 0.001f
                        || Mathf.Abs(scale.z) < 0.001f)
                    {
                        error = $"Source cube '{node.name}' has a zero-size axis.";
                        formation.SetPreviewState(null, $"Fused preview could not be built: {error}");
                        return false;
                    }
                    var box = new TopDown3DRockWorkbenchBox(
                        formation.transform,
                        node.transform,
                        node.SourceShape,
                        node.ShapeSeed,
                        node.Operation,
                        member.GeneratedEdgeDamage);
                    boxes.Add(box);
                    memberBoxes.Add(box);
                }

                if (memberBoxes.Count > 0)
                {
                    groups.Add(new TopDown3DRockWorkbenchFieldGroup(
                        memberIndex,
                        memberBoxes,
                        member.FusionSmoothness * GetMinimumRelativeScale(
                            formation.transform,
                            member.transform)));
                }
            }

            TopDown3DRockWorkbenchBuildResult result;
            var built = formation.JoinStyle == TopDown3DRockFormationJoinStyle.FusedGeologicalSeams
                ? TopDown3DRockWorkbenchMesher.TryBuildGrouped(
                    groups,
                    formation.FusedVoxelSize,
                    Mathf.Min(0.055f, formation.FusedJoinSoftness * 0.35f),
                    formation.GeologicalSeamWidth,
                    formation.MemberSurfaceRelaxation,
                    out result,
                    out error)
                : TopDown3DRockWorkbenchMesher.TryBuild(
                    boxes,
                    formation.FusedVoxelSize,
                    formation.FusedJoinSoftness,
                    formation.MemberSurfaceRelaxation,
                    out result,
                    out error);
            if (!built)
            {
                formation.SetPreviewState(null, $"Fused preview could not be built: {error}");
                RestoreMembers(state.Members);
                return false;
            }

            var mesh = result.CreateMesh($"{formation.name} Fused Preview");
            var filter = formation.GetComponent<MeshFilter>();
            var rendererRoot = formation.GetComponent<MeshRenderer>();
            var colliderRoot = formation.GetComponent<MeshCollider>();
            if (filter == null || rendererRoot == null || colliderRoot == null)
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                error = "Required mesh components are missing from the formation root.";
                formation.SetPreviewState(null, $"Fused preview could not be built: {error}");
                RestoreMembers(state.Members);
                return false;
            }

            DestroyGeneratedMesh(formation);
            filter.sharedMesh = mesh;
            rendererRoot.sharedMaterial = formation.RockMaterial != null
                ? formation.RockMaterial
                : members[0].RockMaterial;
            rendererRoot.enabled = true;
            TopDown3DRockWorkbenchPreview.ApplySurfaceProperties(
                members[0],
                rendererRoot,
                mesh.bounds.size,
                formation.SurfaceFieldSeed,
                formation.SurfaceFieldOrigin,
                formation.LongFractures,
                formation.FractureSpacing,
                formation.JoinStyle == TopDown3DRockFormationJoinStyle.FusedGeologicalSeams
                    ? formation.GeologicalSeamStrength
                    : 0f);
            colliderRoot.sharedMesh = null;
            colliderRoot.enabled = formation.UpdateCollider;
            if (formation.UpdateCollider) colliderRoot.sharedMesh = mesh;

            foreach (var member in members)
            {
                var renderer = member.GetComponent<MeshRenderer>();
                var collider = member.GetComponent<MeshCollider>();
                if (renderer != null) renderer.enabled = false;
                if (collider != null) collider.enabled = false;
            }

            var connection = result.ConnectedComponents == 1
                ? formation.JoinStyle == TopDown3DRockFormationJoinStyle.FusedGeologicalSeams
                    ? "ONE GEOLOGICAL SHELL"
                    : "ONE FUSED SURFACE"
                : $"{result.ConnectedComponents} DISCONNECTED SURFACES";
            var resolutionNote = result.EffectiveVoxelSize > formation.FusedVoxelSize + 0.0001f
                ? $" Effective voxel size was capped to {result.EffectiveVoxelSize:0.###}."
                : string.Empty;
            formation.SetPreviewState(
                mesh,
                $"{connection}: {members.Length} editable rocks, {boxes.Count} source cubes, "
                + $"{result.Topology.TriangleCount:N0} triangles.{resolutionNote}");
            RepaintViews();
            return true;
        }

        private static Bounds CalculateBounds(
            IReadOnlyList<TopDown3DRockWorkbenchAuthoring> members)
        {
            var bounds = new Bounds(members[0].transform.position, Vector3.zero);
            foreach (var member in members)
            {
                var renderer = member.GetComponent<MeshRenderer>();
                var filter = member.GetComponent<MeshFilter>();
                if (renderer != null && filter != null && filter.sharedMesh != null)
                    bounds.Encapsulate(renderer.bounds);
                else bounds.Encapsulate(member.transform.position);
            }
            return bounds;
        }

        private static float GetMinimumRelativeScale(Transform root, Transform child)
        {
            var childToRoot = root.worldToLocalMatrix * child.localToWorldMatrix;
            return Mathf.Max(
                0.001f,
                Mathf.Min(
                    childToRoot.MultiplyVector(Vector3.right).magnitude,
                    Mathf.Min(
                        childToRoot.MultiplyVector(Vector3.up).magnitude,
                        childToRoot.MultiplyVector(Vector3.forward).magnitude)));
        }

        private static void ClearFormationMesh(
            TopDown3DRockWorkbenchFormationAuthoring formation)
        {
            DestroyGeneratedMesh(formation);
            var filter = formation.GetComponent<MeshFilter>();
            var renderer = formation.GetComponent<MeshRenderer>();
            var collider = formation.GetComponent<MeshCollider>();
            if (filter != null) filter.sharedMesh = null;
            if (renderer != null) renderer.enabled = false;
            if (collider != null)
            {
                collider.sharedMesh = null;
                collider.enabled = false;
            }
        }

        private static void DestroyGeneratedMesh(
            TopDown3DRockWorkbenchFormationAuthoring formation)
        {
            var previous = formation.GeneratedMesh;
            if (previous != null
                && !EditorUtility.IsPersistent(previous)
                && (previous.hideFlags & HideFlags.DontSaveInEditor) != 0)
            {
                UnityEngine.Object.DestroyImmediate(previous);
            }
            formation.SetPreviewState(null, formation.PreviewStatus);
        }

        private static void RestoreMembers(
            IReadOnlyList<TopDown3DRockWorkbenchAuthoring> members)
        {
            foreach (var member in members)
            {
                if (member == null) continue;
                var renderer = member.GetComponent<MeshRenderer>();
                var collider = member.GetComponent<MeshCollider>();
                var filter = member.GetComponent<MeshFilter>();
                if (renderer != null)
                {
                    renderer.enabled = true;
                    renderer.sharedMaterial = member.RockMaterial;
                    var size = filter != null && filter.sharedMesh != null
                        ? filter.sharedMesh.bounds.size
                        : member.GeneratedOverallSize;
                    TopDown3DRockWorkbenchPreview.ApplySurfaceProperties(
                        member,
                        renderer,
                        size,
                        member.GenerationSeed,
                        Vector3.zero,
                        0f,
                        6f);
                }
                if (collider != null) collider.enabled = member.UpdateCollider;
            }
        }

        private static PreviewState GetOrCreateState(
            TopDown3DRockWorkbenchFormationAuthoring formation)
        {
            var id = formation.GetInstanceID();
            if (States.TryGetValue(id, out var state)) return state;
            state = new PreviewState
            {
                PendingSignature = int.MinValue,
                LastBuiltSignature = int.MinValue,
                Requested = true
            };
            States.Add(id, state);
            return state;
        }

        private static int CalculateSignature(
            TopDown3DRockWorkbenchFormationAuthoring formation)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + formation.FormationSeed;
                hash = hash * 31 + formation.SurfaceFieldSeed;
                hash = hash * 31 + formation.SurfaceFieldOrigin.GetHashCode();
                hash = hash * 31 + (int)formation.FormationArchetype;
                hash = hash * 31 + (int)formation.JoinStyle;
                hash = hash * 31 + formation.UpdateCollider.GetHashCode();
                hash = hash * 31 + formation.LongFractures.GetHashCode();
                hash = hash * 31 + formation.FractureSpacing.GetHashCode();
                hash = hash * 31 + formation.FusedVoxelSize.GetHashCode();
                hash = hash * 31 + formation.FusedJoinSoftness.GetHashCode();
                hash = hash * 31 + formation.MemberSurfaceRelaxation.GetHashCode();
                hash = hash * 31 + formation.GeologicalSeamWidth.GetHashCode();
                hash = hash * 31 + formation.GeologicalSeamStrength.GetHashCode();
                hash = hash * 31 + (formation.RockMaterial == null
                    ? 0
                    : formation.RockMaterial.GetInstanceID());
                HashMatrix(ref hash, formation.transform.localToWorldMatrix);
                var members = formation.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
                hash = hash * 31 + members.Length;
                foreach (var member in members)
                {
                    hash = hash * 31 + member.GetInstanceID();
                    hash = hash * 31 + (member.GeneratedMesh == null
                        ? 0
                        : member.GeneratedMesh.GetInstanceID());
                    hash = hash * 31 + member.GeologyScale.GetHashCode();
                    hash = hash * 31 + member.SurfaceVariation.GetHashCode();
                    hash = hash * 31 + member.CrackAmount.GetHashCode();
                    hash = hash * 31 + member.SideGrit.GetHashCode();
                    hash = hash * 31 + member.UndersideShale.GetHashCode();
                    hash = hash * 31 + member.SideShalePatches.GetHashCode();
                    hash = hash * 31 + member.TopShalePatches.GetHashCode();
                    hash = hash * 31 + member.WornShine.GetHashCode();
                    HashMatrix(ref hash, member.transform.localToWorldMatrix);
                    foreach (var node in member.GetComponentsInChildren<TopDown3DRockVolumeNode>(true))
                    {
                        hash = hash * 31 + node.GetInstanceID();
                        hash = hash * 31 + node.ContributesToRock.GetHashCode();
                        hash = hash * 31 + (int)node.SourceShape;
                        hash = hash * 31 + node.ShapeSeed;
                        hash = hash * 31 + node.gameObject.activeInHierarchy.GetHashCode();
                        HashMatrix(
                            ref hash,
                            formation.transform.worldToLocalMatrix * node.transform.localToWorldMatrix);
                    }
                }
                return hash;
            }
        }

        private static void HashMatrix(ref int hash, Matrix4x4 matrix)
        {
            unchecked
            {
                for (var row = 0; row < 4; row++)
                {
                    for (var column = 0; column < 4; column++)
                    {
                        hash = hash * 31 + matrix[row, column].GetHashCode();
                    }
                }
            }
        }

        private static bool IsUsable(TopDown3DRockWorkbenchFormationAuthoring formation)
        {
            return formation != null
                && formation.isActiveAndEnabled
                && !EditorUtility.IsPersistent(formation)
                && formation.gameObject.scene.IsValid()
                && formation.gameObject.scene.isLoaded;
        }

        private static void RepaintViews()
        {
            SceneView.RepaintAll();
            foreach (var editor in Resources.FindObjectsOfTypeAll<TopDown3DRockWorkbenchFormationEditor>())
            {
                editor.Repaint();
            }
        }

        private sealed class PreviewState
        {
            internal int PendingSignature;
            internal int LastBuiltSignature;
            internal double DueTime;
            internal bool Requested;
            internal readonly List<TopDown3DRockWorkbenchAuthoring> Members =
                new List<TopDown3DRockWorkbenchAuthoring>();
        }
    }
}
