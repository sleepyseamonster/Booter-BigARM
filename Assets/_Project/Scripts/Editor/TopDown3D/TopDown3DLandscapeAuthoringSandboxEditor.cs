using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;

namespace BooterBigArm.Editor
{
    [InitializeOnLoad]
    [CustomEditor(typeof(TopDown3DLandscapeAuthoringSandbox))]
    public sealed class TopDown3DLandscapeAuthoringSandboxEditor : UnityEditor.Editor
    {
        private const string TerrainPreviewRootName = "__Generated Terrain Context";
        internal const float FormationStageSize = 40f;
        private const int FormationStageQuadsPerAxis = 20;
        internal const string MixedReferencePath =
            "Assets/_Project/Art/Environment/Rocks/Source/MixedPileScatterReference.prefab";
        private static List<FormationChoice> formationChoicesCache;

        static TopDown3DLandscapeAuthoringSandboxEditor()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall -= RestoreMissingTerrainContexts;
            EditorApplication.delayCall += RestoreMissingTerrainContexts;
            EditorApplication.projectChanged -= ClearFormationChoiceCache;
            EditorApplication.projectChanged += ClearFormationChoiceCache;
        }

        [MenuItem("Booter & BigARM/Top Down 3D/Open Landscape Authoring Sandbox")]
        public static void OpenOrCreateSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(LandscapeAuthoringSandboxBuilder.ScenePath);
            if (existing != null)
            {
                EditorSceneManager.OpenScene(LandscapeAuthoringSandboxBuilder.ScenePath, OpenSceneMode.Single);
                RebuildLoadedTerrainContexts();
                return;
            }

            LandscapeAuthoringSandboxBuilder.CreateScene();
        }

        private readonly struct FormationChoice
        {
            internal string DisplayName { get; }
            internal GameObject Prefab { get; }

            internal FormationChoice(string displayName, GameObject prefab)
            {
                DisplayName = displayName;
                Prefab = prefab;
            }
        }

        private static List<FormationChoice> LoadFormationChoices()
        {
            if (formationChoicesCache != null)
                return new List<FormationChoice>(formationChoicesCache);
            var choices = new List<FormationChoice>();
            var guids = AssetDatabase.FindAssets("t:Prefab",
                new[] { TopDown3DMixedFormationAssetBaker.SourceFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("Reference.prefab", StringComparison.Ordinal)) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                choices.Add(new FormationChoice(GetFormationDisplayName(prefab.name), prefab));
            }
            choices.Sort((left, right) =>
            {
                if (left.Prefab == right.Prefab) return 0;
                if (left.Prefab.name == "MixedPileScatterReference") return -1;
                if (right.Prefab.name == "MixedPileScatterReference") return 1;
                return string.CompareOrdinal(left.DisplayName, right.DisplayName);
            });
            formationChoicesCache = choices;
            return new List<FormationChoice>(formationChoicesCache);
        }

        private static void ClearFormationChoiceCache() => formationChoicesCache = null;

        private static string GetFormationDisplayName(string prefabName)
        {
            if (prefabName == "MixedPileScatterReference") return "Scatter";
            const string suffix = "Reference";
            if (prefabName.EndsWith(suffix, StringComparison.Ordinal))
                prefabName = prefabName.Substring(0, prefabName.Length - suffix.Length);
            return ObjectNames.NicifyVariableName(prefabName);
        }

        public override void OnInspectorGUI()
        {
            var sandbox = (TopDown3DLandscapeAuthoringSandbox)target;
            serializedObject.Update();
            if (sandbox.RockReference != null)
            {
                EditorGUILayout.LabelField("Formation Template Ground", EditorStyles.boldLabel);
                var referenceProperty = serializedObject.FindProperty("rockReference");
                var formationChoices = LoadFormationChoices();
                var currentChoice = formationChoices.FindIndex(choice => choice.Prefab == sandbox.RockReference);
                if (currentChoice < 0)
                {
                    formationChoices.Add(new FormationChoice(
                        GetFormationDisplayName(sandbox.RockReference.name), sandbox.RockReference));
                    currentChoice = formationChoices.Count - 1;
                }
                var nextChoice = EditorGUILayout.Popup(
                    new GUIContent("Formation Type", "Choose which saved formation template to test on this stage."),
                    currentChoice, formationChoices.Select(choice => choice.DisplayName).ToArray());
                var nextReference = formationChoices[nextChoice].Prefab;
                if (nextReference != sandbox.RockReference)
                {
                    TopDown3DMixedFormationAssetBaker.GetOutputPath(nextReference);
                    referenceProperty.objectReferenceValue = nextReference;
                    serializedObject.FindProperty("variationEnabled").boolValue = false;
                    serializedObject.FindProperty("variationSeed").intValue = 0;
                    serializedObject.ApplyModifiedProperties();
                    sandbox.ConfigureRockReference(nextReference);
                    BuildTerrainContext(sandbox);
                    serializedObject.Update();
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("centerChunk"),
                    new GUIContent("Terrain Location"));
                var shallowProperty = serializedObject.FindProperty("rockBurial");
                var deepProperty = serializedObject.FindProperty("maximumRockBurial");
                var shallow = sandbox.RockBurial;
                var deep = sandbox.MaximumRockBurial;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(new GUIContent("Burial Range (m)",
                    "Shallow to deep. Each rock samples a bell curve between the two handles; extremes are rare."),
                    ref shallow, ref deep, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    shallowProperty.floatValue = shallow;
                    deepProperty.floatValue = deep;
                }
                EditorGUILayout.LabelField($"Shallow {shallow:0.000} m  —  Deep {deep:0.000} m",
                    EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumRockTilt"),
                    new GUIContent("Maximum Ground Tilt"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sandBuildup"),
                    new GUIContent("Sand Buildup (m)", "Rock-hugging sand banks. Update Rocks & Ground applies changes without changing the variation seed. Zero removes buildup."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundClutter"),
                    new GUIContent("Formation Clutter", "Pebble relief and sparse protruding stones immediately around the mixed formation."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("landscapeClutter"),
                    new GUIContent("Landscape Rock Clutter", "Production-planned loose stones and ground texture pockets across the surrounding terrain."));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandWidth"),
                    new GUIContent("Rock Stain Height", "How far the sand-stained shader band climbs each grounded rock face."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandOpacity"),
                    new GUIContent("Rock Stain Opacity", "Controls only the textured color stain on the rocks. The raised sand border remains fully opaque."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandColor"),
                    new GUIContent("Rock Band Tint", "Opaque sand-stain color drawn directly on the lower rock surface."));
                EditorGUILayout.HelpBox(
                    "The raised sand border inherits its texture and color from the regular Terrain Material. Its body is opaque; only the adjustable outer base feather fades into the terrain.",
                    MessageType.None);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandFeather"),
                    new GUIContent("Rock Stain Feather (m)", "Vertical distance over which the opaque rock stain fades into the original rock surface."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandWaviness"),
                    new GUIContent("Waviness", "Breaks up the upper edge of the sand stain on the rock surface."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandNoiseScale"),
                    new GUIContent("Noise Scale", "World-space frequency of the shoreline-like band edge."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandDirectionalBuildup"),
                    new GUIContent("Directional Buildup", "Makes the band climb higher on the prevailing-wind side of each rock."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandTrimWidth"),
                    new GUIContent("Border Width", "How far the shaped sand lip extends outward from each grounded rock."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandTrimHeight"),
                    new GUIContent("Border Rise", "How high the concave sand bevel rises where it meets the rock. It leaves the outer terrain nearly flat, then steepens toward the rock; collision is unchanged."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandBaseFeather"),
                    new GUIContent("Sand Border Base Feather (m)", "Distance over which the terrain-facing bottom of the raised sand border fades from transparent into its opaque body. This does not affect the rock stain."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandTopFeather"),
                    new GUIContent("Sand Border Top Feather (m)", "Distance over which the rock-facing top of the raised sand border fades into the rock contact. The middle of the sand border remains opaque, and this does not affect the rock stain."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSandTrimCurve"),
                    new GUIContent("Border Curve", "Controls how strongly the raised sand stays flat near the terrain before curving upward into the rock. Higher values make the transition more concave."));
                using (new EditorGUI.DisabledScope(sandbox.VariationEnabled))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("blowingSand"),
                        new GUIContent("Blowing Sand", "Excluded from rock variations."));
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pebbleDepth"),
                        new GUIContent("Pebble Depth — Deferred", "The rejected textured-pebble experiment is off while we review the whole formation. Its settings are preserved."));
                EditorGUILayout.HelpBox(
                    "Each ground-contact rock receives stable bell-curve burial. Stacked rocks follow their supports. "
                    + "The saved reference and the original scene rocks stay intact. Broad raised sand is controlled "
                    + "separately on Landscape Authoring Sandbox. This authoring view clears in Play Mode.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Landscape Terrain Ground", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("centerChunk"),
                    new GUIContent("Terrain Location"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainRadiusInChunks"),
                    new GUIContent("Terrain Radius In Chunks"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("landscapeSand"),
                    new GUIContent("Landscape Sand", "Broad deterministic terrain-owned sand coverage."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("landscapeSandRelief"),
                    new GUIContent("Raised Sand Relief (m)", "Actual raised height of broad terrain sand, independent of every rock and formation generator."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("landscapeClutter"),
                    new GUIContent("Landscape Rock Clutter", "Production-planned loose stones and ground texture pockets across the surrounding terrain."));
                EditorGUILayout.HelpBox(
                    "This object owns the broad landscape sand and terrain clutter. Formation workbenches only preview the result.",
                    MessageType.Info);
                DrawPropertiesExcluding(serializedObject, "m_Script", "rockReference", "rockBurial", "maximumRockBurial", "maximumRockTilt", "burialRangeVersion", "sandBuildup", "sandBuildupVersion", "groundClutter", "landscapeSand", "landscapeSandRelief", "landscapeClutter", "contactSandWidth", "contactSandOpacity", "contactSandColor", "contactSandTrimColor", "contactSandFeather", "contactSandBaseFeather", "contactSandTopFeather", "contactSandWaviness", "contactSandNoiseScale", "contactSandDirectionalBuildup", "contactSandTrimWidth", "contactSandTrimHeight", "contactSandTrimCurve", "blowingSand", "pebbleDepth");
            }
            serializedObject.ApplyModifiedProperties();

            var validPlacement = HasValidPlacement(sandbox);
            if (!validPlacement)
            {
                EditorGUILayout.HelpBox("This ground preview uses world coordinates. Its workbench Transform "
                    + "has been moved, rotated or scaled. Reset the workbench below, then use Terrain Location "
                    + "to choose the ground area. Your saved rock template is not changed.", MessageType.Warning);
                var parent = sandbox.transform.parent;
                var canReset = parent == null || (parent.lossyScale - Vector3.one).sqrMagnitude < 0.000001f;
                using (new EditorGUI.DisabledScope(!canReset || EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("Reset Workbench Transform"))
                    {
                        Undo.RecordObject(sandbox.transform, "Reset Mixed Formation Ground Transform");
                        sandbox.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        sandbox.transform.localScale = Vector3.one;
                        EditorUtility.SetDirty(sandbox.transform);
                        validPlacement = HasValidPlacement(sandbox);
                    }
                if (!canReset) EditorGUILayout.HelpBox("The parent is scaled. Move this workbench to the scene root "
                    + "before resetting it; the tool will not change your parent or hierarchy automatically.", MessageType.Info);
            }

            if (sandbox.RockReference != null)
            {
                EditorGUILayout.LabelField(sandbox.VariationEnabled
                    ? $"Rock Variation {sandbox.VariationSeed}"
                    : "Original Arrangement", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode
                    || !validPlacement || sandbox.WorldSettings == null || sandbox.TerrainMaterial == null))
                {
                    if (GUILayout.Button("Generate New Variation"))
                    {
                        serializedObject.Update();
                        serializedObject.FindProperty("variationEnabled").boolValue = true;
                        serializedObject.FindProperty("variationSeed").intValue = unchecked(sandbox.VariationSeed + 1);
                        serializedObject.ApplyModifiedProperties();
                        BuildTerrainContext(sandbox);
                    }
                    if (sandbox.VariationEnabled && GUILayout.Button("Restore Original Arrangement"))
                    {
                        serializedObject.Update();
                        serializedObject.FindProperty("variationEnabled").boolValue = false;
                        serializedObject.ApplyModifiedProperties();
                        BuildTerrainContext(sandbox);
                    }
                }
            }

            EditorGUILayout.Space();
            if (sandbox.RockReference == null) EditorGUILayout.HelpBox(
                "Terrain Context is a temporary editor view built from the production terrain generator. "
                + "It provides scale, lighting, and a playable ground surface without becoming a second terrain asset.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!validPlacement || sandbox.WorldSettings == null || sandbox.TerrainMaterial == null))
            {
                if (GUILayout.Button(sandbox.RockReference != null ? "Update Rocks & Ground" : "Build Terrain Context"))
                {
                    BuildTerrainContext(sandbox);
                }
            }
            if (sandbox.RockReference != null
                && GUILayout.Button("Center & Consolidate Formation Stage"))
            {
                CenterFormationStage(sandbox);
            }

            if (GUILayout.Button("Clear Terrain Context"))
            {
                ClearTerrainContext(sandbox);
            }
            if (sandbox.RockReference != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Reusable capture preserves the saved reference rocks and surfaces. "
                    + "Terrain, sand and clutter are preview treatments, not part of this capture. "
                    + "A complete gameplay bake adds the formation to the world catalog for eligible geological reservations.", MessageType.Info);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("Save / Update Reusable Formation"))
                        TopDown3DMixedFormationAssetBaker.Bake(sandbox.RockReference);
                    if (GUILayout.Button("Build Gameplay Rock Meshes"))
                        TopDown3DMixedFormationAssetBaker.BakeGameplay(sandbox.RockReference);
                }
                EditorGUILayout.HelpBox("Build Gameplay Rock Meshes captures the saved reference and prepares "
                    + "three shape choices per rock with distance-detail meshes and updates the world catalog. "
                    + "It does not change this preview or guarantee placement at every reservation. "
                    + "Recapturing the reference requires rebuilding its gameplay meshes.", MessageType.Info);
            }
        }

        [MenuItem("Booter & BigARM/Create Mixed Formation Ground", false, 3)]
        public static void CreateMixedFormationGround()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Create the mixed ground setup outside Play Mode.");
            var reference = AssetDatabase.LoadAssetAtPath<GameObject>(MixedReferencePath);
            if (reference == null) throw new InvalidOperationException("The saved mixed formation reference is missing.");
            var scene = SceneManager.GetActiveScene();
            var context = scene.GetRootGameObjects()
                .Select(root => root.GetComponent<TopDown3DLandscapeAuthoringSandbox>())
                .FirstOrDefault(candidate => candidate != null && candidate.RockReference == reference);
            if (context == null)
            {
                var root = new GameObject("Mixed Formation Ground");
                Undo.RegisterCreatedObjectUndo(root, "Create Mixed Formation Ground");
                root.tag = "EditorOnly";
                context = Undo.AddComponent<TopDown3DLandscapeAuthoringSandbox>(root);
                context.Configure(
                    AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(TopDown3DPrototypeBuilder.WorldSettingsPath),
                    AssetDatabase.LoadAssetAtPath<Material>(TopDown3DPrototypeBuilder.TerrainMaterialPath),
                    null, Vector2Int.zero);
                context.ConfigureRockReference(reference);
                var serialized = new SerializedObject(context);
                serialized.FindProperty("terrainRadiusInChunks").intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            BuildTerrainContext(context);
            Selection.activeGameObject = context.gameObject;
        }

        [MenuItem("Booter & BigARM/Center Formation Sandbox Stage", false, 4)]
        public static void CenterFormationStageFromMenu()
        {
            var stage = FindStageAuthority();
            if (stage == null || stage.RockReference == null)
                throw new InvalidOperationException("Create Mixed Formation Ground before consolidating the sandbox stage.");

            CenterFormationStage(stage);
        }

        internal static void CenterFormationStage(TopDown3DLandscapeAuthoringSandbox stage)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (stage.RockReference == null)
                throw new InvalidOperationException("The formation stage requires the saved mixed formation reference.");

            Undo.RecordObject(stage, "Center Formation Sandbox Stage");
            var serialized = new SerializedObject(stage);
            serialized.FindProperty("centerChunk").vector2IntValue = Vector2Int.zero;
            serialized.FindProperty("terrainRadiusInChunks").intValue = 0;
            serialized.ApplyModifiedProperties();

            RebuildTerrainContexts(stage.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<TopDown3DLandscapeAuthoringSandbox>(true))
                .ToArray());
            ArrangeAuthoringWorkbenches(stage);
            Selection.activeGameObject = stage.gameObject;
            EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
        }

        private static bool HasValidPlacement(TopDown3DLandscapeAuthoringSandbox sandbox)
        {
            return sandbox.RockReference == null
                || (sandbox.transform.position.sqrMagnitude < 0.000001f
                    && Quaternion.Angle(sandbox.transform.rotation, Quaternion.identity) < 0.001f
                    && (sandbox.transform.lossyScale - Vector3.one).sqrMagnitude < 0.000001f);
        }

        internal static void BuildTerrainContext(TopDown3DLandscapeAuthoringSandbox sandbox)
        {
            if (sandbox == null) throw new ArgumentNullException(nameof(sandbox));
            if (sandbox.WorldSettings == null)
                throw new InvalidOperationException("Landscape authoring needs a World Settings asset.");
            if (sandbox.TerrainMaterial == null)
                throw new InvalidOperationException("Landscape authoring needs the production terrain material.");

            if (sandbox.RockReference != null && EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            // Automatic reloads must also preserve the existing preview on invalid placement.
            // The Inspector exposes an explicit undoable reset; never reset user transforms here.
            if (!HasValidPlacement(sandbox)) return;
            ClearTerrainContext(sandbox);
            var contextRoot = new GameObject(TerrainPreviewRootName)
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };
            contextRoot.transform.SetParent(sandbox.transform, false);

            try
            {
                var settings = sandbox.WorldSettings;
                var generator = new TopDown3DWorldGenerator(settings);
                if (sandbox.RockReference != null)
                {
                    BuildFlatFormationStage(sandbox, contextRoot.transform, generator);
                    BuildGroundedReference(sandbox, contextRoot.transform);
                    return;
                }

                var radius = sandbox.TerrainRadiusInChunks;
                var center = sandbox.CenterChunk;
                for (var z = -radius; z <= radius; z++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        var coordinate = new Vector2Int(center.x + x, center.y + z);
                        var chunk = new GameObject($"Terrain {coordinate.x}, {coordinate.y}")
                        {
                            hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
                        };
                        chunk.transform.SetParent(contextRoot.transform, false);
                        chunk.transform.localPosition = new Vector3(
                            coordinate.x * settings.ChunkSize,
                            0f,
                            coordinate.y * settings.ChunkSize);

                        var mesh = TopDown3DChunkMeshBuilder.BuildMesh(settings, generator, coordinate);
                        mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
                        chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                        chunk.AddComponent<MeshRenderer>().sharedMaterial = sandbox.TerrainMaterial;
                        chunk.AddComponent<TopDown3DGroundSurface>();
                        chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
                    }
                }
                var terrain = contextRoot.GetComponentsInChildren<MeshCollider>();
                TopDown3DLandscapeSandPreview.Apply(sandbox, terrain);
                TopDown3DLandscapeGroundClutterPreview.Apply(
                    sandbox,
                    contextRoot.transform,
                    terrain,
                    Array.Empty<TopDown3DRockWorkbenchAuthoring>());
            }
            catch
            {
                ClearTerrainContext(sandbox);
                throw;
            }
        }

        internal static TopDown3DLandscapeAuthoringSandbox ResolveLandscapeAuthority(
            TopDown3DLandscapeAuthoringSandbox sandbox)
        {
            if (sandbox == null || sandbox.RockReference == null) return sandbox;
            var roots = sandbox.gameObject.scene.GetRootGameObjects();
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var candidates = roots[rootIndex]
                    .GetComponentsInChildren<TopDown3DLandscapeAuthoringSandbox>(true);
                for (var candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    var candidate = candidates[candidateIndex];
                    if (candidate != null && candidate.RockReference == null) return candidate;
                }
            }

            // Older isolated formation scenes remain usable until a separate landscape
            // authority is added; the production sandbox always contains one.
            return sandbox;
        }

        private static void BuildFlatFormationStage(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            Transform contextRoot,
            TopDown3DWorldGenerator generator)
        {
            var stage = new GameObject("Compact Flat Formation Stage")
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };
            stage.transform.SetParent(contextRoot, false);

            var verticesPerAxis = FormationStageQuadsPerAxis + 1;
            var vertices = new Vector3[verticesPerAxis * verticesPerAxis];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[FormationStageQuadsPerAxis * FormationStageQuadsPerAxis * 6];
            var step = FormationStageSize / FormationStageQuadsPerAxis;
            var halfSize = FormationStageSize * 0.5f;
            var sampleOrigin = new Vector2(
                sandbox.CenterChunk.x * sandbox.WorldSettings.ChunkSize,
                sandbox.CenterChunk.y * sandbox.WorldSettings.ChunkSize);
            // Sand authoring expects each terrain tile's vertices to begin at its Transform
            // origin. Offset the tile itself so its world-space bounds remain centered at zero.
            // Terrain Location selects the production geology sampled under that centered stage;
            // it must not move the compact workbench away from the camera and rock formation.
            stage.transform.localPosition = new Vector3(-halfSize, 0f, -halfSize);

            for (var z = 0; z < verticesPerAxis; z++)
            {
                for (var x = 0; x < verticesPerAxis; x++)
                {
                    var index = z * verticesPerAxis + x;
                    var localX = x * step;
                    var localZ = z * step;
                    var worldX = localX - halfSize;
                    var worldZ = localZ - halfSize;
                    var sampleX = worldX + sampleOrigin.x;
                    var sampleZ = worldZ + sampleOrigin.y;
                    vertices[index] = new Vector3(localX, 0f, localZ);
                    normals[index] = Vector3.up;
                    uvs[index] = new Vector2(
                        sampleX / sandbox.WorldSettings.ChunkSize,
                        sampleZ / sandbox.WorldSettings.ChunkSize);
                    if (!generator.Authority.Materials.TrySample(
                            new AbsoluteWorldPosition(sampleX, 0d, sampleZ),
                            out var material,
                            out var materialError))
                        throw new InvalidOperationException(materialError);
                    colors[index] = BuildFormationStagePreviewColor(
                        WorldTerrainMaterialPackingAdapter.Pack(material),
                        sampleX,
                        sampleZ,
                        worldX,
                        worldZ,
                        sandbox.WorldSettings.WorldSeed);
                }
            }

            var triangleIndex = 0;
            for (var z = 0; z < FormationStageQuadsPerAxis; z++)
            {
                for (var x = 0; x < FormationStageQuadsPerAxis; x++)
                {
                    var bottomLeft = z * verticesPerAxis + x;
                    var topLeft = bottomLeft + verticesPerAxis;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomLeft + 1;
                    triangles[triangleIndex++] = bottomLeft + 1;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topLeft + 1;
                }
            }

            var mesh = new Mesh { name = "Compact Flat Formation Stage" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
            mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
            stage.AddComponent<MeshFilter>().sharedMesh = mesh;
            stage.AddComponent<MeshRenderer>().sharedMaterial = sandbox.TerrainMaterial;
            stage.AddComponent<TopDown3DGroundSurface>();
            stage.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static Color BuildFormationStagePreviewColor(
            Color productionWeights,
            float sampleX,
            float sampleZ,
            float stageX,
            float stageZ,
            int worldSeed)
        {
            // The compact flat pad covers far less area than a streamed production region.
            // Preserve the production material query, then lift broad deterministic patches so
            // sand, gravel and shale remain visible together while formations are authored.
            var seedOffset = (worldSeed & 0xffff) * 0.0137f;
            var shaleNoise = Mathf.PerlinNoise(
                (sampleX + seedOffset) * 0.055f,
                (sampleZ - seedOffset * 0.37f) * 0.055f);
            var sandNoise = Mathf.PerlinNoise(
                (sampleX - seedOffset * 0.61f) * 0.047f + 17.3f,
                (sampleZ + seedOffset * 0.29f) * 0.047f - 9.1f);
            var shalePatch = Mathf.Max(
                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.43f, 0.72f, shaleNoise)),
                1f - Mathf.SmoothStep(3f, 11f, Vector2.Distance(
                    new Vector2(stageX, stageZ), new Vector2(-9f, 6f))));
            var sandPatch = Mathf.Max(
                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.46f, 0.75f, sandNoise)),
                1f - Mathf.SmoothStep(3f, 12f, Vector2.Distance(
                    new Vector2(stageX, stageZ), new Vector2(9f, -7f))))
                * (1f - shalePatch * 0.72f);
            var gravelPatch = Mathf.Clamp01((1f - Mathf.Abs(shaleNoise - 0.5f) * 4f) * 0.68f);
            return new Color(
                Mathf.Max(productionWeights.r, sandPatch * 0.86f),
                Mathf.Max(productionWeights.g, gravelPatch),
                Mathf.Max(productionWeights.b, shalePatch * 0.9f),
                productionWeights.a);
        }

        private static void BuildGroundedReference(
            TopDown3DLandscapeAuthoringSandbox sandbox, Transform contextRoot)
        {
            // Sample the displayed collision triangles, not a finer continuous query that can
            // differ between mesh vertices. Only these terrain colliders can support the copy.
            var ground = contextRoot.GetComponentsInChildren<MeshCollider>();
            Physics.SyncTransforms();
            RaycastHit GroundAt(Vector3 point)
            {
                foreach (var collider in ground)
                {
                    var bounds = collider.bounds;
                    if (point.x < bounds.min.x || point.x > bounds.max.x
                        || point.z < bounds.min.z || point.z > bounds.max.z) continue;
                    var origin = new Vector3(point.x, bounds.max.y + 1f, point.z);
                    if (collider.Raycast(new Ray(origin, Vector3.down), out var hit, bounds.size.y + 2f))
                        return hit;
                }
                var terrainBounds = ground[0].bounds;
                for (var i = 1; i < ground.Length; i++) terrainBounds.Encapsulate(ground[i].bounds);
                throw new InvalidOperationException(
                    $"The mixed reference point ({point.x:0.###}, {point.z:0.###}) extends beyond "
                    + $"terrain X[{terrainBounds.min.x:0.###}, {terrainBounds.max.x:0.###}] "
                    + $"Z[{terrainBounds.min.z:0.###}, {terrainBounds.max.z:0.###}].");
            }
            var copy = UnityEngine.Object.Instantiate(sandbox.RockReference, contextRoot, false);
            copy.name = "Mixed Rocks — Grounded Copy";
            var rocks = copy.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
            var rejectUnsupportedDisplayedRocks = false;
            if (sandbox.VariationEnabled)
            {
                var template = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                    TopDown3DMixedFormationAssetBaker.GetOutputPath(sandbox.RockReference));
                if (template == null) throw new InvalidOperationException("Save / Update Reusable Mixed Formation first.");
                var sourcePath = AssetDatabase.GetAssetPath(sandbox.RockReference);
                if (template.SourceRevision != AssetDatabase.GetAssetDependencyHash(sourcePath).ToString())
                    throw new InvalidOperationException("The saved reference changed. Save / Update Reusable Mixed Formation first.");
                rejectUnsupportedDisplayedRocks = template.VariationProfile
                    == TopDown3DAuthoredFormationVariationProfile.HandbuiltSpire;
                var sourceRocks = sandbox.RockReference.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
                var layout = TopDown3DAuthoredFormationVariation.GenerateLayout(template, sandbox.VariationSeed);
                var instancesById = new Dictionary<string, TopDown3DRockWorkbenchAuthoring>();
                for (var i = 0; i < sourceRocks.Length; i++)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sourceRocks[i], out string guid, out long localId);
                    instancesById.Add(guid + ":" + localId, rocks[i]);
                }
                var selected = new List<TopDown3DRockWorkbenchAuthoring>(layout.Entries.Count);
                var usedOriginals = new HashSet<TopDown3DRockWorkbenchAuthoring>();
                for (var i = 0; i < layout.Entries.Count; i++)
                {
                    var entry = layout.Entries[i];
                    var original = instancesById[template.Members[entry.SourceIndex].SourceId];
                    var rock = usedOriginals.Add(original)
                        ? original
                        : UnityEngine.Object.Instantiate(original, original.transform.parent, false);
                    if (rock != original) rock.name = original.name + " (Variation Extra)";
                    var pose = copy.transform.localToWorldMatrix * entry.Transform;
                    rock.transform.SetPositionAndRotation(pose.GetColumn(3), pose.rotation);
                    // Captured workbench members have positive TRS transforms; divide out their parent scale.
                    var parentScale = rock.transform.parent.lossyScale;
                    var scale = pose.lossyScale;
                    rock.transform.localScale = new Vector3(scale.x / parentScale.x,
                        scale.y / parentScale.y, scale.z / parentScale.z);
                    selected.Add(rock);
                }
                for (var i = 0; i < rocks.Length; i++)
                    if (!usedOriginals.Contains(rocks[i])) UnityEngine.Object.DestroyImmediate(rocks[i].gameObject);
                rocks = selected.ToArray();
            }
            // Select the visible silhouette before testing support. Shape variation can make a
            // previously valid source contact visibly miss its neighbor even when the captured
            // authoring bounds were sound.
            for (var rockIndex = 0; rockIndex < rocks.Length; rockIndex++)
            {
                var rock = rocks[rockIndex];
                var filter = rock.GetComponent<MeshFilter>();
                var renderer = rock.GetComponent<MeshRenderer>();
                if (filter == null || filter.sharedMesh == null || renderer == null)
                    throw new InvalidOperationException($"{rock.name} has no captured reference mesh.");
                var serialized = new SerializedObject(rock);
                serialized.FindProperty("autoRebuild").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (sandbox.VariationEnabled)
                    TopDown3DMixedFormationShapeVariation.Apply(rock, unchecked(
                        rock.GenerationSeed ^ sandbox.VariationSeed * 486187739
                        ^ rockIndex * 16777619));
            }
            if (rejectUnsupportedDisplayedRocks)
            {
                var displayedBounds = new Bounds[rocks.Length];
                for (var i = 0; i < rocks.Length; i++)
                    displayedBounds[i] = rocks[i].GetComponent<MeshRenderer>().bounds;
                var supported = TopDown3DAuthoredFormationVariation.SupportedMask(displayedBounds);
                var grounded = new List<TopDown3DRockWorkbenchAuthoring>(rocks.Length);
                for (var i = 0; i < rocks.Length; i++)
                {
                    if (supported[i]) grounded.Add(rocks[i]);
                    else UnityEngine.Object.DestroyImmediate(rocks[i].gameObject);
                }
                rocks = grounded.ToArray();
            }
            var members = new List<TopDown3DRockGroundContact.Member>(rocks.Length);
            for (var rockIndex = 0; rockIndex < rocks.Length; rockIndex++)
            {
                var rock = rocks[rockIndex];
                var filter = rock.GetComponent<MeshFilter>();
                var renderer = rock.GetComponent<MeshRenderer>();
                var vertices = filter.sharedMesh.vertices;
                for (var i = 0; i < vertices.Length; i++)
                    vertices[i] = rock.transform.TransformPoint(vertices[i]);
                members.Add(new TopDown3DRockGroundContact.Member(
                    rock.transform.position, rock.transform.rotation, renderer.bounds, vertices,
                    unchecked(rock.GenerationSeed ^ (members.Count * 486187739)
                        ^ (sandbox.VariationEnabled ? sandbox.VariationSeed : 0))));
                TopDown3DRockWorkbenchPreview.ApplySurfaceProperties(
                    rock, renderer, filter.sharedMesh.bounds.size, rock.GenerationSeed, Vector3.zero, 0f, 6f);
                TopDown3DMixedFormationClutter.ApplyFormationReadability(renderer);
            }
            var poses = TopDown3DRockGroundContact.Fit(members,
                point => GroundAt(point).point.y,
                point => GroundAt(point).normal,
                sandbox.RockBurial, sandbox.MaximumRockBurial, sandbox.MaximumRockTilt);
            for (var i = 0; i < rocks.Length; i++)
                rocks[i].transform.SetPositionAndRotation(poses[i].Position, poses[i].Rotation);
            foreach (var child in copy.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable;
            var groundContacts = TopDown3DMixedFormationSand.CollectGroundContacts(sandbox, ground, rocks);
            TopDown3DLandscapeSandPreview.Apply(sandbox, ground, groundContacts);
            var contactRocks = new List<TopDown3DContactRockPreview>(rocks.Length + 32);
            foreach (var rock in rocks)
                contactRocks.Add(new TopDown3DContactRockPreview(rock.transform,
                    rock.GetComponent<MeshFilter>(), rock.GetComponent<MeshRenderer>(), rock.GenerationSeed));
            TopDown3DMixedFormationClutter.Apply(
                sandbox, contextRoot, ground, rocks, groundContacts, contactRocks);
            TopDown3DLandscapeGroundClutterPreview.Apply(
                sandbox, contextRoot, ground, rocks, contactRocks);
            var allContacts = TopDown3DMixedFormationSand.CollectGroundContacts(
                sandbox, ground, contactRocks);
            TopDown3DMixedFormationContactSand.Apply(
                sandbox, contextRoot, ground, contactRocks, allContacts);
            if (!sandbox.VariationEnabled)
                TopDown3DMixedFormationBlowingSand.Apply(sandbox, contextRoot, ground, rocks);
        }

        internal static void ClearTerrainContext(TopDown3DLandscapeAuthoringSandbox sandbox)
        {
            if (sandbox == null) return;
            TopDown3DMixedFormationBlowingSand.Clear(sandbox);
            var existing = sandbox.transform.Find(TerrainPreviewRootName);
            if (existing == null) return;

            var colliders = existing.GetComponentsInChildren<MeshCollider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].sharedMesh = null;
            }

            var filters = existing.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh != null && (mesh.hideFlags & HideFlags.DontSaveInEditor) != 0)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode
                && state != PlayModeStateChange.EnteredPlayMode
                && state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            RebuildLoadedTerrainContexts();
        }

        internal static void RebuildLoadedTerrainContexts()
        {
            var sandboxes = UnityEngine.Object.FindObjectsByType<TopDown3DLandscapeAuthoringSandbox>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var sceneSandboxes in sandboxes.GroupBy(sandbox => sandbox.gameObject.scene.handle))
            {
                RebuildTerrainContexts(sceneSandboxes.ToArray());
            }
        }

        private static void RestoreMissingTerrainContexts()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var sandboxes = UnityEngine.Object.FindObjectsByType<TopDown3DLandscapeAuthoringSandbox>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var sceneSandboxes in sandboxes.GroupBy(sandbox => sandbox.gameObject.scene.handle))
            {
                var candidates = sceneSandboxes.ToArray();
                var stage = FindStageAuthority(candidates);
                for (var i = 0; i < candidates.Length; i++)
                {
                    var sandbox = candidates[i];
                    if (sandbox == null || sandbox.gameObject.scene.path != LandscapeAuthoringSandboxBuilder.ScenePath)
                        continue;
                    if (sandbox == stage)
                    {
                        // Generated preview objects survive ordinary domain reloads. Rebuilding an intact
                        // stage here was repeatedly resampling the full sand field after every script edit.
                        if (sandbox.transform.Find(TerrainPreviewRootName) == null)
                            BuildTerrainContext(sandbox);
                    }
                    else
                    {
                        ClearTerrainContext(sandbox);
                    }
                }
            }
        }

        private static void RebuildTerrainContexts(TopDown3DLandscapeAuthoringSandbox[] candidates)
        {
            var stage = FindStageAuthority(candidates);
            for (var i = 0; i < candidates.Length; i++)
            {
                var sandbox = candidates[i];
                if (sandbox == null
                    || !sandbox.gameObject.scene.IsValid()
                    || sandbox.WorldSettings == null
                    || sandbox.TerrainMaterial == null)
                {
                    continue;
                }

                if (sandbox == stage) BuildTerrainContext(sandbox);
                else ClearTerrainContext(sandbox);
            }
        }

        private static TopDown3DLandscapeAuthoringSandbox FindStageAuthority(
            TopDown3DLandscapeAuthoringSandbox[] sandboxes = null)
        {
            sandboxes ??= SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<TopDown3DLandscapeAuthoringSandbox>(true))
                .ToArray();
            return sandboxes
                .Where(candidate => candidate != null
                    && candidate.gameObject.scene.IsValid()
                    && candidate.WorldSettings != null
                    && candidate.TerrainMaterial != null)
                .OrderByDescending(candidate => candidate.RockReference != null)
                .ThenBy(candidate => candidate.gameObject.name, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.transform.GetSiblingIndex())
                .FirstOrDefault();
        }

        private static void ArrangeAuthoringWorkbenches(TopDown3DLandscapeAuthoringSandbox stage)
        {
            var contextRoot = stage.transform.Find(TerrainPreviewRootName);
            if (contextRoot == null) return;
            var groundColliders = contextRoot.GetComponentsInChildren<TopDown3DGroundSurface>(true)
                .Select(surface => surface.GetComponent<MeshCollider>())
                .Where(collider => collider != null)
                .ToArray();
            if (groundColliders.Length == 0) return;
            var stageCenter = Vector2.zero;

            var sceneRoots = stage.gameObject.scene.GetRootGameObjects();
            var formations = sceneRoots
                .SelectMany(root => root.GetComponentsInChildren<TopDown3DRockWorkbenchFormationAuthoring>(true))
                .Where(candidate => (candidate.gameObject.hideFlags & HideFlags.DontSaveInEditor) == 0)
                .OrderBy(candidate => candidate.gameObject.name, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.transform.GetSiblingIndex())
                .ToArray();
            for (var i = 0; i < formations.Length; i++)
                PlaceAndGround(formations[i].transform,
                    stageCenter + new Vector2(0f, 9f + i * 4f), groundColliders);

            var standaloneRocks = sceneRoots
                .SelectMany(root => root.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true))
                .Where(candidate => candidate.GetComponentInParent<TopDown3DRockWorkbenchFormationAuthoring>() == null
                    && (candidate.gameObject.hideFlags & HideFlags.DontSaveInEditor) == 0)
                .OrderBy(candidate => candidate.gameObject.name, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.transform.GetSiblingIndex())
                .ToArray();
            for (var i = 0; i < standaloneRocks.Length; i++)
                PlaceAndGround(standaloneRocks[i].transform,
                    stageCenter + new Vector2(0f, -12f + i * 4f), groundColliders);
        }

        private static void PlaceAndGround(Transform root, Vector2 stagePosition, MeshCollider[] groundColliders)
        {
            Undo.RecordObject(root, "Arrange Formation Sandbox Workbench");
            root.position = new Vector3(stagePosition.x, root.position.y, stagePosition.y);
            Physics.SyncTransforms();

            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled
                    && (renderer.gameObject.hideFlags & HideFlags.DontSaveInEditor) == 0)
                .ToArray();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var rayOrigin = new Vector3(bounds.center.x, 256f, bounds.center.z);
            var groundY = float.NegativeInfinity;
            for (var i = 0; i < groundColliders.Length; i++)
            {
                if (groundColliders[i].Raycast(new Ray(rayOrigin, Vector3.down), out var hit, 512f))
                    groundY = Mathf.Max(groundY, hit.point.y);
            }
            if (float.IsNegativeInfinity(groundY)) return;

            root.position += Vector3.up * (groundY - bounds.min.y);
            EditorUtility.SetDirty(root);
        }
    }

    internal static class LandscapeAuthoringSandboxBuilder
    {
        internal const string ScenePath = "Assets/_Project/Scenes/TopDown3D/LandscapeAuthoringSandbox.unity";

        internal static void CreateScene()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.TerrainMaterialPath);
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.RockMaterialPath);
            if (settings == null || terrainMaterial == null || rockMaterial == null)
            {
                throw new InvalidOperationException(
                    "Landscape Authoring Sandbox requires the production world settings, terrain material, and rock material.");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LandscapeAuthoringSandbox";
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientLight = new Color(0.36f, 0.39f, 0.43f);

            var sandboxObject = new GameObject("Landscape Authoring Sandbox");
            var sandbox = sandboxObject.AddComponent<TopDown3DLandscapeAuthoringSandbox>();
            sandbox.Configure(settings, terrainMaterial, rockMaterial, Vector2Int.zero);

            CreateLighting();
            CreateCamera();

            var formationObject = new GameObject("Rock Formation Authoring");
            formationObject.transform.SetParent(sandboxObject.transform, false);
            var formation = formationObject.AddComponent<TopDown3DRockFormationAuthoring>();
            formation.Configure(settings.NaturalObjectCatalog, rockMaterial);

            if (!EditorSceneManager.SaveScene(scene, ScenePath, false))
            {
                throw new InvalidOperationException($"Unity did not save {ScenePath}.");
            }

            Selection.activeGameObject = formationObject;
            EditorApplication.delayCall += BuildInitialTerrainContext;
        }

        private static void BuildInitialTerrainContext()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath) return;

            var sandbox = UnityEngine.Object.FindAnyObjectByType<TopDown3DLandscapeAuthoringSandbox>();
            if (sandbox == null) return;

            try
            {
                TopDown3DLandscapeAuthoringSandboxEditor.BuildTerrainContext(sandbox);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, sandbox);
            }
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Authoring Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.82f, 0.64f);
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Authoring Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 50f;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            cameraObject.transform.position = new Vector3(24f, 26f, -24f);
            cameraObject.transform.LookAt(new Vector3(0f, 0f, 4f));
            cameraObject.AddComponent<TopDown3DCameraRig>();
        }
    }
}
