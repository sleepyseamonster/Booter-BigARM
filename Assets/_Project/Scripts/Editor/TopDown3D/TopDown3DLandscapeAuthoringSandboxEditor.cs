using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;

namespace BooterBigArm.Editor
{
    [InitializeOnLoad]
    [CustomEditor(typeof(TopDown3DLandscapeAuthoringSandbox))]
    public sealed class TopDown3DLandscapeAuthoringSandboxEditor : UnityEditor.Editor
    {
        private const string TerrainPreviewRootName = "__Generated Terrain Context";
        internal const string MixedReferencePath =
            "Assets/_Project/Art/Environment/Rocks/Source/MixedPileScatterReference.prefab";

        static TopDown3DLandscapeAuthoringSandboxEditor()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall -= RebuildLoadedTerrainContexts;
            EditorApplication.delayCall += RebuildLoadedTerrainContexts;
        }

        [MenuItem("Booter & BigARM/Top Down 3D/Open Landscape Authoring Sandbox")]
        public static void OpenOrCreateSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(LandscapeAuthoringSandboxBuilder.ScenePath);
            if (existing != null)
            {
                EditorSceneManager.OpenScene(LandscapeAuthoringSandboxBuilder.ScenePath, OpenSceneMode.Single);
                var loadedSandbox = UnityEngine.Object.FindAnyObjectByType<TopDown3DLandscapeAuthoringSandbox>();
                if (loadedSandbox != null) BuildTerrainContext(loadedSandbox);
                return;
            }

            LandscapeAuthoringSandboxBuilder.CreateScene();
        }

        public override void OnInspectorGUI()
        {
            var sandbox = (TopDown3DLandscapeAuthoringSandbox)target;
            serializedObject.Update();
            if (sandbox.RockReference != null)
            {
                EditorGUILayout.LabelField("Mixed Formation Ground", EditorStyles.boldLabel);
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
                using (new EditorGUI.DisabledScope(sandbox.VariationEnabled))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sandBuildup"),
                        new GUIContent("Sand Buildup (m)", "Excluded from rock variations while the raised-sand issue is unresolved."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groundClutter"),
                    new GUIContent("Ground Clutter", "Pebble surface relief and sparse protruding stones. Sand hides the buried detail."));
                using (new EditorGUI.DisabledScope(sandbox.VariationEnabled))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("blowingSand"),
                        new GUIContent("Blowing Sand", "Excluded from rock variations."));
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pebbleDepth"),
                        new GUIContent("Pebble Depth — Deferred", "The rejected textured-pebble experiment is off while we review the whole formation. Its settings are preserved."));
                EditorGUILayout.HelpBox(
                    "Each ground-contact rock receives stable bell-curve burial. Stacked rocks follow their supports. "
                    + "The saved reference and the original scene rocks stay intact. This authoring view clears in Play Mode.",
                    MessageType.Info);
            }
            else
            {
                DrawPropertiesExcluding(serializedObject, "m_Script", "rockReference", "rockBurial", "maximumRockBurial", "maximumRockTilt", "burialRangeVersion", "sandBuildup", "sandBuildupVersion", "groundClutter", "blowingSand", "pebbleDepth");
            }
            serializedObject.ApplyModifiedProperties();

            if (sandbox.RockReference != null)
            {
                EditorGUILayout.LabelField(sandbox.VariationEnabled
                    ? $"Rock Variation {sandbox.VariationSeed} — sand excluded"
                    : "Original Arrangement", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode
                    || sandbox.WorldSettings == null || sandbox.TerrainMaterial == null))
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

            using (new EditorGUI.DisabledScope(sandbox.WorldSettings == null || sandbox.TerrainMaterial == null))
            {
                if (GUILayout.Button(sandbox.RockReference != null ? "Update Rocks & Ground" : "Build Terrain Context"))
                {
                    BuildTerrainContext(sandbox);
                }
            }

            if (GUILayout.Button("Clear Terrain Context"))
            {
                ClearTerrainContext(sandbox);
            }
            if (sandbox.RockReference != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Reusable capture preserves the saved reference rocks and surfaces. "
                    + "Sand remains unresolved and is excluded. World placement is not enabled yet.", MessageType.Info);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("Save / Update Reusable Mixed Formation"))
                        TopDown3DMixedFormationAssetBaker.Bake(sandbox.RockReference);
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
                    null, new Vector2Int(1, 0));
                context.ConfigureRockReference(reference);
            }
            BuildTerrainContext(context);
            Selection.activeGameObject = context.gameObject;
        }

        internal static void BuildTerrainContext(TopDown3DLandscapeAuthoringSandbox sandbox)
        {
            if (sandbox == null) throw new ArgumentNullException(nameof(sandbox));
            if (sandbox.WorldSettings == null)
                throw new InvalidOperationException("Landscape authoring needs a World Settings asset.");
            if (sandbox.TerrainMaterial == null)
                throw new InvalidOperationException("Landscape authoring needs the production terrain material.");

            ClearTerrainContext(sandbox);
            if (sandbox.RockReference != null && EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (sandbox.RockReference != null
                && (sandbox.transform.position != Vector3.zero
                    || sandbox.transform.rotation != Quaternion.identity
                    || sandbox.transform.lossyScale != Vector3.one))
                throw new InvalidOperationException(
                    "Keep Mixed Formation Ground at zero position/rotation and unit scale. Use Terrain Location to move it.");
            var contextRoot = new GameObject(TerrainPreviewRootName)
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };
            contextRoot.transform.SetParent(sandbox.transform, false);

            try
            {
                var settings = sandbox.WorldSettings;
                var generator = new TopDown3DWorldGenerator(settings);
                var authority = generator.Authority;
                var compiler = sandbox.RockReference == null ? null : new WorldRepresentationCompiler(
                    authority.Query, authority.Materials, new WorldRepresentationBufferPool(1),
                    authority.Profile.CreateRepresentationProfile(), authority.SourceFingerprint);
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

                        Mesh mesh;
                        if (compiler == null)
                        {
                            mesh = TopDown3DChunkMeshBuilder.BuildMesh(settings, generator, coordinate);
                        }
                        else
                        {
                            var key = new WorldRepresentationKey(authority.Identity, authority.CoordinateModel,
                                WorldRepresentationTier.Near, coordinate.x, coordinate.y, settings.ChunkSize);
                            using (var representation = compiler.BuildAsync(key, CancellationToken.None)
                                .GetAwaiter().GetResult())
                                mesh = TopDown3DChunkMeshBuilder.BuildMesh(representation, chunk.name);
                        }
                        mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
                        chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                        chunk.AddComponent<MeshRenderer>().sharedMaterial = sandbox.TerrainMaterial;
                        chunk.AddComponent<TopDown3DGroundSurface>();
                        chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
                    }
                }
                if (sandbox.RockReference != null)
                    BuildGroundedReference(sandbox, contextRoot.transform);
            }
            catch
            {
                ClearTerrainContext(sandbox);
                throw;
            }
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
                throw new InvalidOperationException("The mixed reference extends beyond its terrain context.");
            }
            var copy = UnityEngine.Object.Instantiate(sandbox.RockReference, contextRoot, false);
            copy.name = "Mixed Rocks — Grounded Copy";
            copy.transform.localPosition += new Vector3(
                sandbox.CenterChunk.x * sandbox.WorldSettings.ChunkSize, 0f,
                sandbox.CenterChunk.y * sandbox.WorldSettings.ChunkSize);
            var rocks = copy.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
            if (sandbox.VariationEnabled)
            {
                var template = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                    TopDown3DMixedFormationAssetBaker.OutputPath);
                if (template == null) throw new InvalidOperationException("Save / Update Reusable Mixed Formation first.");
                var sourcePath = AssetDatabase.GetAssetPath(sandbox.RockReference);
                if (template.SourceRevision != AssetDatabase.GetAssetDependencyHash(sourcePath).ToString())
                    throw new InvalidOperationException("The saved reference changed. Save / Update Reusable Mixed Formation first.");
                var sourceRocks = sandbox.RockReference.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
                var variations = TopDown3DAuthoredFormationVariation.Generate(template, sandbox.VariationSeed);
                var byId = new Dictionary<string, int>();
                for (var i = 0; i < template.Members.Count; i++) byId.Add(template.Members[i].SourceId, i);
                for (var i = 0; i < rocks.Length; i++)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sourceRocks[i], out string guid, out long localId);
                    var pose = copy.transform.localToWorldMatrix * variations[byId[guid + ":" + localId]];
                    rocks[i].transform.SetPositionAndRotation(pose.GetColumn(3), pose.rotation);
                    // Captured workbench members have positive TRS transforms; divide out their parent scale.
                    var parentScale = rocks[i].transform.parent.lossyScale;
                    var scale = pose.lossyScale;
                    rocks[i].transform.localScale = new Vector3(scale.x / parentScale.x,
                        scale.y / parentScale.y, scale.z / parentScale.z);
                }
            }
            var members = new List<TopDown3DRockGroundContact.Member>(rocks.Length);
            foreach (var rock in rocks)
            {
                var filter = rock.GetComponent<MeshFilter>();
                var renderer = rock.GetComponent<MeshRenderer>();
                if (filter == null || filter.sharedMesh == null || renderer == null)
                    throw new InvalidOperationException($"{rock.name} has no captured reference mesh.");
                var serialized = new SerializedObject(rock);
                serialized.FindProperty("autoRebuild").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
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
            var groundContacts = TopDown3DMixedFormationSand.Apply(sandbox, ground, rocks, !sandbox.VariationEnabled);
            TopDown3DMixedFormationClutter.Apply(sandbox, contextRoot, ground, rocks, groundContacts);
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

        private static void RebuildLoadedTerrainContexts()
        {
            var sandboxes = UnityEngine.Object.FindObjectsByType<TopDown3DLandscapeAuthoringSandbox>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < sandboxes.Length; i++)
            {
                var sandbox = sandboxes[i];
                if (sandbox == null
                    || !sandbox.gameObject.scene.IsValid()
                    || sandbox.WorldSettings == null
                    || sandbox.TerrainMaterial == null)
                {
                    continue;
                }

                BuildTerrainContext(sandbox);
            }
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
            cameraObject.transform.position = new Vector3(24f, 26f, -24f);
            cameraObject.transform.LookAt(new Vector3(0f, 0f, 4f));
        }
    }
}
