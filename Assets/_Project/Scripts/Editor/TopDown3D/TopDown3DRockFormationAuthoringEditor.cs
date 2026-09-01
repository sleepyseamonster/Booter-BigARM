using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DRockFormationAuthoring))]
    public sealed class TopDown3DRockFormationAuthoringEditor : UnityEditor.Editor
    {
        private static readonly TopDown3DNaturalObjectShape[] SmallShapes =
        {
            TopDown3DNaturalObjectShape.Pebble,
            TopDown3DNaturalObjectShape.Shard,
            TopDown3DNaturalObjectShape.Slab,
            TopDown3DNaturalObjectShape.Nodule,
            TopDown3DNaturalObjectShape.Talus
        };

        private static readonly TopDown3DNaturalObjectShape[] LargeShapes =
        {
            TopDown3DNaturalObjectShape.Boulder,
            TopDown3DNaturalObjectShape.Outcrop,
            TopDown3DNaturalObjectShape.Cliff,
            TopDown3DNaturalObjectShape.HeroSpire
        };

        private string generationError;

        [MenuItem("GameObject/Booter & BigARM/Top Down 3D/Rock Formation Authoring", false, 20)]
        private static void CreateAuthoringRoot(MenuCommand command)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.RockMaterialPath);
            if (settings == null || settings.NaturalObjectCatalog == null || rockMaterial == null)
            {
                throw new InvalidOperationException(
                    "Rock Formation Authoring needs the project's rock mesh catalog and regular rock material.");
            }

            var root = new GameObject("Rock Formation Authoring");
            Undo.RegisterCreatedObjectUndo(root, "Create Rock Formation Authoring");
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
            var authoring = root.AddComponent<TopDown3DRockFormationAuthoring>();
            authoring.Configure(settings.NaturalObjectCatalog, rockMaterial);
            authoring.SetAuthoringSeed(CreateNewSeed());
            Selection.activeGameObject = root;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "generatedMembersRoot");
            serializedObject.ApplyModifiedProperties();

            var authoring = (TopDown3DRockFormationAuthoring)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This is a standalone rock workshop. It uses the project rock mesh library, "
                + "but it does not search, sample, or reproduce the streamed world generator. "
                + "Generate New Random Formation creates a fresh composition every time. Keep a seed only when you find a result you want to revisit.",
                MessageType.Info);

            DrawLibrarySetup(authoring);
            DrawFusionStatus(authoring);

            using (new EditorGUI.DisabledScope(!HasUsableLibrary(authoring)))
            {
                if (GUILayout.Button("Generate New Random Formation"))
                {
                    SetNewSeed(authoring, "Generate New Random Rock Formation");
                    Generate(authoring, "Generate New Random Rock Formation");
                }

                if (GUILayout.Button("Regenerate Current Seed"))
                {
                    Generate(authoring, "Regenerate Rock Formation");
                }

                if (GUILayout.Button("Add New Random Formation"))
                {
                    AddRandomFormation(authoring);
                }

                if (GUILayout.Button("Fuse Overlapping Formations in Scene"))
                {
                    var formations = UnityEngine.Object.FindObjectsByType<TopDown3DRockFormationAuthoring>();
                    if (TopDown3DRockFormationFusionUtility.TryFuseAcrossFormations(
                            formations,
                            out var clusterCount,
                            out var fusionError))
                    {
                        generationError = $"Fused {clusterCount} cross-formation rock cluster(s).";
                    }
                    else
                    {
                        generationError = string.IsNullOrEmpty(fusionError)
                            ? "No overlapping rocks from separate formation roots were found."
                            : fusionError;
                    }
                }

                if (TopDown3DRockFormationFusionUtility.HasSceneFusedMembers(authoring.gameObject.scene)
                    && GUILayout.Button("Restore All Scene-Fused Rocks"))
                {
                    TopDown3DRockFormationFusionUtility.RestoreSceneFusedMembers(authoring.gameObject.scene);
                    generationError = null;
                }
            }

            using (new EditorGUI.DisabledScope(authoring.GeneratedMembersRoot == null))
            {
                if (GUILayout.Button("Fuse Overlapping Members"))
                {
                    if (TopDown3DRockFormationFusionUtility.TryFuseOverlappingMembers(
                            authoring,
                            out var clusterCount,
                            out var fusionError))
                    {
                        generationError = clusterCount > 0
                            ? $"Fused {clusterCount} overlapping rock cluster(s)."
                            : "No members overlap enough to fuse yet.";
                    }
                    else
                    {
                        generationError = fusionError;
                    }
                }

                if (TopDown3DRockFormationFusionUtility.HasFusedMembers(authoring)
                    && GUILayout.Button("Restore Individual Members"))
                {
                    TopDown3DRockFormationFusionUtility.RestoreIndividualMembers(authoring, true);
                    generationError = null;
                }

                if (GUILayout.Button("Clear Generated Members"))
                {
                    ClearGeneratedMembers(authoring);
                }
            }

            if (!string.IsNullOrEmpty(generationError))
            {
                EditorGUILayout.HelpBox(generationError, MessageType.Warning);
            }
        }

        private static void DrawLibrarySetup(TopDown3DRockFormationAuthoring authoring)
        {
            if (authoring.RockMeshCatalog != null && authoring.RockMaterial != null) return;

            EditorGUILayout.HelpBox(
                "This older formation needs its standalone rock-library references assigned once.",
                MessageType.Warning);
            if (!GUILayout.Button("Use Project Rock Mesh Library")) return;

            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.RockMaterialPath);
            if (settings == null || settings.NaturalObjectCatalog == null || material == null)
            {
                throw new InvalidOperationException(
                    "The project rock mesh catalog or regular rock material could not be found.");
            }

            Undo.RecordObject(authoring, "Assign Rock Workshop Library");
            authoring.Configure(settings.NaturalObjectCatalog, material);
            if (authoring.AuthoringSeed == 0) authoring.SetAuthoringSeed(CreateNewSeed());
            EditorUtility.SetDirty(authoring);
            EditorSceneManager.MarkSceneDirty(authoring.gameObject.scene);
        }

        private static void DrawFusionStatus(TopDown3DRockFormationAuthoring authoring)
        {
            if (authoring.GeneratedMembersRoot == null)
            {
                EditorGUILayout.HelpBox("Fusion status: no generated members yet.", MessageType.None);
                DrawLastFusionReadout(authoring);
                return;
            }

            if (TopDown3DRockFormationFusionUtility.HasFusedMembers(authoring))
            {
                EditorGUILayout.HelpBox(
                    "Fusion status: fused mesh active. The editable source rocks are disabled under Source Rock Members.",
                    MessageType.Info);
                DrawLastFusionReadout(authoring);
                return;
            }

            EditorGUILayout.HelpBox(
                authoring.AutoFuseOverlappingMembers
                    ? "Fusion status: automatic fusion is armed. Move rocks slightly into one another, then release the handle."
                    : "Fusion status: individual meshes. Enable Auto Fuse Overlapping Members or use Fuse Overlapping Members.",
                MessageType.None);
            DrawLastFusionReadout(authoring);
        }

        private static void DrawLastFusionReadout(TopDown3DRockFormationAuthoring authoring)
        {
            if (string.IsNullOrWhiteSpace(authoring.FusionStatus)) return;
            EditorGUILayout.HelpBox($"Last Boolean fusion result: {authoring.FusionStatus}", MessageType.None);
        }

        private void Generate(TopDown3DRockFormationAuthoring authoring, string undoName)
        {
            generationError = null;
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                ClearGeneratedMembers(authoring, false);
                CreateGeneratedMembers(authoring, BuildDraft(authoring), undoName);
                EditorSceneManager.MarkSceneDirty(authoring.gameObject.scene);
            }
            catch (Exception exception)
            {
                generationError = exception.Message;
                Undo.RevertAllDownToGroup(undoGroup);
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private void AddRandomFormation(TopDown3DRockFormationAuthoring authoring)
        {
            generationError = null;
            const string undoName = "Add New Random Rock Formation";
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                var root = new GameObject("Rock Formation");
                Undo.RegisterCreatedObjectUndo(root, undoName);
                root.transform.SetParent(authoring.transform.parent, true);
                root.transform.position = GetSuggestedPlacement(authoring);
                root.transform.rotation = authoring.transform.rotation;

                var addedFormation = Undo.AddComponent<TopDown3DRockFormationAuthoring>(root);
                addedFormation.CopyGeneratorSettingsFrom(authoring);
                addedFormation.SetAuthoringSeed(CreateNewSeed());
                CreateGeneratedMembers(addedFormation, BuildDraft(addedFormation), undoName);
                EditorUtility.SetDirty(addedFormation);
                EditorSceneManager.MarkSceneDirty(root.scene);
                Selection.activeGameObject = root;
            }
            catch (Exception exception)
            {
                generationError = exception.Message;
                Undo.RevertAllDownToGroup(undoGroup);
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static RockFormationDraft BuildDraft(TopDown3DRockFormationAuthoring authoring)
        {
            if (!HasUsableLibrary(authoring))
            {
                throw new InvalidOperationException(
                    "Assign a Rock Mesh Catalog and Rock Material before generating a formation.");
            }

            var random = new System.Random(authoring.AuthoringSeed);
            var memberCount = random.Next(authoring.MinimumMemberCount, authoring.MaximumMemberCount + 1);
            var draft = new RockFormationDraft();
            for (var index = 0; index < memberCount; index++)
            {
                var shape = PickAvailableShape(authoring.RockMeshCatalog, random, index == 0 || random.NextDouble() < authoring.LargeRockFrequency);
                var variant = PickAvailableVariant(authoring.RockMeshCatalog, shape, random);
                var scale = GetScale(authoring, random, index, shape);
                var position = GetPosition(authoring, random, index, memberCount);
                var rotation = GetRotation(authoring, random, shape);
                draft.Members.Add(new RockDraftMember(shape, variant, position, rotation, scale));
            }

            return draft;
        }

        private static TopDown3DNaturalObjectShape PickAvailableShape(
            TopDown3DNaturalObjectCatalog catalog,
            System.Random random,
            bool preferLarge)
        {
            var preferred = preferLarge ? LargeShapes : SmallShapes;
            if (TryPickShape(catalog, preferred, random, out var shape)) return shape;
            var fallback = preferLarge ? SmallShapes : LargeShapes;
            if (TryPickShape(catalog, fallback, random, out shape)) return shape;
            throw new InvalidOperationException("The assigned rock mesh catalog has no complete rock mesh families.");
        }

        private static bool TryPickShape(
            TopDown3DNaturalObjectCatalog catalog,
            IReadOnlyList<TopDown3DNaturalObjectShape> candidates,
            System.Random random,
            out TopDown3DNaturalObjectShape shape)
        {
            var available = new List<TopDown3DNaturalObjectShape>();
            for (var i = 0; i < candidates.Count; i++)
            {
                for (var variant = 0; variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape; variant++)
                {
                    if (!catalog.TryGetMeshFamily(candidates[i], variant, out var family) || !family.IsComplete) continue;
                    available.Add(candidates[i]);
                    break;
                }
            }

            if (available.Count == 0)
            {
                shape = default;
                return false;
            }

            shape = available[random.Next(available.Count)];
            return true;
        }

        private static int PickAvailableVariant(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DNaturalObjectShape shape,
            System.Random random)
        {
            var available = new List<int>();
            for (var variant = 0; variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape; variant++)
            {
                if (catalog.TryGetMeshFamily(shape, variant, out var family) && family.IsComplete)
                {
                    available.Add(variant);
                }
            }

            if (available.Count == 0)
                throw new InvalidOperationException($"No complete mesh variants are available for {shape}.");
            return available[random.Next(available.Count)];
        }

        private static Vector3 GetScale(
            TopDown3DRockFormationAuthoring authoring,
            System.Random random,
            int index,
            TopDown3DNaturalObjectShape shape)
        {
            var baseScale = index == 0
                ? RandomRange(random, 1.45f, 2.3f)
                : RandomRange(random, 0.55f, 1.45f);
            if (shape == TopDown3DNaturalObjectShape.HeroSpire || shape == TopDown3DNaturalObjectShape.Cliff)
            {
                baseScale *= RandomRange(random, 1.1f, 1.45f);
            }

            var heightMultiplier = Mathf.Lerp(0.82f, 1.9f, authoring.Verticality);
            return new Vector3(
                baseScale * RandomRange(random, 0.78f, 1.2f),
                baseScale * heightMultiplier,
                baseScale * RandomRange(random, 0.78f, 1.2f));
        }

        private static Vector3 GetPosition(
            TopDown3DRockFormationAuthoring authoring,
            System.Random random,
            int index,
            int memberCount)
        {
            if (index == 0) return Vector3.zero;

            var compactRadius = Mathf.Lerp(1.15f, authoring.FormationRadius, 1f - authoring.Compactness);
            var angle = RandomRange(random, 0f, Mathf.PI * 2f);
            float x;
            float z;
            switch (authoring.Style)
            {
                case TopDown3DRockFormationStyle.Ridge:
                    x = Mathf.Lerp(-authoring.FormationRadius, authoring.FormationRadius, (float)random.NextDouble());
                    z = RandomRange(random, -compactRadius * 0.24f, compactRadius * 0.24f);
                    break;
                case TopDown3DRockFormationStyle.SpireCluster:
                    var spireDistance = RandomRange(random, 0.25f, compactRadius * 0.65f);
                    x = Mathf.Cos(angle) * spireDistance;
                    z = Mathf.Sin(angle) * spireDistance;
                    break;
                case TopDown3DRockFormationStyle.Scatter:
                    var scatterDistance = Mathf.Sqrt((float)random.NextDouble()) * authoring.FormationRadius;
                    x = Mathf.Cos(angle) * scatterDistance;
                    z = Mathf.Sin(angle) * scatterDistance;
                    break;
                default:
                    var clusterDistance = Mathf.Sqrt((float)random.NextDouble()) * compactRadius;
                    x = Mathf.Cos(angle) * clusterDistance;
                    z = Mathf.Sin(angle) * clusterDistance;
                    break;
            }

            var lift = authoring.Style == TopDown3DRockFormationStyle.SpireCluster
                ? RandomRange(random, 0f, authoring.Verticality * 1.6f)
                : RandomRange(random, -0.12f, authoring.Verticality * 0.35f);
            return new Vector3(x, lift, z);
        }

        private static Quaternion GetRotation(
            TopDown3DRockFormationAuthoring authoring,
            System.Random random,
            TopDown3DNaturalObjectShape shape)
        {
            var maxTilt = Mathf.Lerp(8f, 34f, authoring.Verticality);
            if (shape == TopDown3DNaturalObjectShape.HeroSpire) maxTilt *= 0.35f;
            return Quaternion.Euler(
                RandomRange(random, -maxTilt, maxTilt),
                RandomRange(random, 0f, 360f),
                RandomRange(random, -maxTilt, maxTilt));
        }

        private static void CreateGeneratedMembers(
            TopDown3DRockFormationAuthoring authoring,
            RockFormationDraft draft,
            string undoName)
        {
            var generatedRoot = new GameObject("Generated Rock Members");
            Undo.RegisterCreatedObjectUndo(generatedRoot, undoName);
            generatedRoot.transform.SetParent(authoring.transform, false);

            for (var index = 0; index < draft.Members.Count; index++)
            {
                var member = draft.Members[index];
                var family = authoring.RockMeshCatalog.GetRequiredMeshFamily(member.Shape, member.Variant);
                var memberObject = new GameObject($"Rock {index} — {member.Shape} {member.Variant}");
                Undo.RegisterCreatedObjectUndo(memberObject, undoName);
                memberObject.transform.SetParent(generatedRoot.transform, false);
                memberObject.transform.localPosition = member.Position;
                memberObject.transform.localRotation = member.Rotation;
                memberObject.transform.localScale = member.Scale;
                memberObject.AddComponent<MeshFilter>().sharedMesh = family.Lod0;
                memberObject.AddComponent<MeshRenderer>().sharedMaterial = authoring.RockMaterial;
                var collider = memberObject.AddComponent<BoxCollider>();
                collider.center = family.ColliderCenter;
                collider.size = family.ColliderSize;
            }

            authoring.SetGeneratedMembersRoot(generatedRoot.transform);
            authoring.SetFusionStatus(
                authoring.AutoFuseOverlappingMembers
                    ? "Generated editable rocks. Automatic fusion will run when their collision volumes overlap."
                    : "Generated editable rocks. Automatic fusion is disabled.");
            EditorUtility.SetDirty(authoring);
        }

        private void SetNewSeed(TopDown3DRockFormationAuthoring authoring, string undoName)
        {
            Undo.RecordObject(authoring, undoName);
            authoring.SetAuthoringSeed(CreateNewSeed());
            EditorUtility.SetDirty(authoring);
        }

        private static int CreateNewSeed()
        {
            return Guid.NewGuid().GetHashCode();
        }

        private static float RandomRange(System.Random random, float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }

        private static bool HasUsableLibrary(TopDown3DRockFormationAuthoring authoring)
        {
            return authoring.RockMeshCatalog != null && authoring.RockMaterial != null;
        }

        private static Vector3 GetSuggestedPlacement(TopDown3DRockFormationAuthoring source)
        {
            var spacing = Mathf.Max(7f, source.FormationRadius * 2.5f);
            return source.transform.position + source.transform.right * spacing;
        }

        private void ClearGeneratedMembers(TopDown3DRockFormationAuthoring authoring)
        {
            ClearGeneratedMembers(authoring, true);
        }

        private static void ClearGeneratedMembers(TopDown3DRockFormationAuthoring authoring, bool registerUndo)
        {
            var generatedRoot = authoring.GeneratedMembersRoot;
            if (generatedRoot == null) return;

            if (registerUndo)
            {
                Undo.SetCurrentGroupName("Clear Generated Rock Members");
                Undo.DestroyObjectImmediate(generatedRoot.gameObject);
                Undo.RecordObject(authoring, "Clear Generated Rock Members");
                authoring.SetGeneratedMembersRoot(null);
                EditorUtility.SetDirty(authoring);
                EditorSceneManager.MarkSceneDirty(authoring.gameObject.scene);
            }
            else
            {
                Undo.DestroyObjectImmediate(generatedRoot.gameObject);
                authoring.SetGeneratedMembersRoot(null);
            }
        }

        private sealed class RockFormationDraft
        {
            public readonly List<RockDraftMember> Members = new List<RockDraftMember>();
        }

        private readonly struct RockDraftMember
        {
            public RockDraftMember(
                TopDown3DNaturalObjectShape shape,
                int variant,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                Shape = shape;
                Variant = variant;
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public TopDown3DNaturalObjectShape Shape { get; }
            public int Variant { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }
        }
    }
}
