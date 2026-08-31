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
        private readonly List<TopDown3DRockFormationPlan> formations =
            new List<TopDown3DRockFormationPlan>();
        private string generationError;

        [MenuItem("GameObject/Booter & BigARM/Top Down 3D/Rock Formation Authoring", false, 20)]
        private static void CreateAuthoringRoot(MenuCommand command)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DPrototypeBuilder.RockMaterialPath);
            var root = new GameObject("Rock Formation Authoring");
            Undo.RegisterCreatedObjectUndo(root, "Create Rock Formation Authoring");
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
            var authoring = root.AddComponent<TopDown3DRockFormationAuthoring>();
            authoring.Configure(settings, rockMaterial, Vector2.zero);
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
                "Search selects a deterministic formation from the production geological planner. "
                + "Generate creates normal child GameObjects in this scene; move their pieces freely, then make this root a prefab when it is ready.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(authoring.WorldSettings == null))
            {
                if (GUILayout.Button("Search Source Area"))
                {
                    Search(authoring);
                }
            }

            DrawFormationSelection(authoring);

            using (new EditorGUI.DisabledScope(formations.Count == 0))
            {
                if (GUILayout.Button("Generate Selected Formation in Scene"))
                {
                    Generate(authoring);
                }

                if (GUILayout.Button("Add Selected Formation as New Root"))
                {
                    AddSelectedFormation(authoring);
                }
            }

            using (new EditorGUI.DisabledScope(authoring.WorldSettings == null))
            {
                if (GUILayout.Button("Add Random Formation From Source Area"))
                {
                    AddRandomFormation(authoring);
                }
            }

            using (new EditorGUI.DisabledScope(authoring.GeneratedMembersRoot == null))
            {
                if (GUILayout.Button("Clear Generated Members"))
                {
                    ClearGeneratedMembers(authoring);
                }
            }

            if (!string.IsNullOrEmpty(generationError))
            {
                EditorGUILayout.HelpBox(generationError, MessageType.Error);
            }
        }

        private void Search(TopDown3DRockFormationAuthoring authoring)
        {
            formations.Clear();
            generationError = null;
            try
            {
                var settings = CreateSettingsWithAuthoringSeed(authoring);
                try
                {
                    formations.AddRange(TopDown3DRockFormationGenerationService.BuildAround(
                        settings,
                        authoring.SourceWorldPosition,
                        authoring.SearchRadiusInChunks));
                }
                finally
                {
                    DestroyImmediate(settings);
                }

                if (formations.Count == 0)
                {
                    generationError = "No formations were admitted in this source area. Try another position or a wider search radius.";
                    return;
                }

                var selected = Mathf.Clamp(authoring.SelectedFormationIndex, 0, formations.Count - 1);
                SetSelection(authoring, selected);
            }
            catch (Exception exception)
            {
                generationError = exception.Message;
            }
        }

        private void DrawFormationSelection(TopDown3DRockFormationAuthoring authoring)
        {
            if (formations.Count == 0)
            {
                EditorGUILayout.HelpBox("Search a source area to choose a formation.", MessageType.None);
                return;
            }

            var labels = new string[formations.Count];
            for (var i = 0; i < formations.Count; i++)
            {
                var formation = formations[i];
                labels[i] = $"{i + 1}. {formation.Members.Count} members — {formation.StableId}";
            }

            var selected = Mathf.Clamp(authoring.SelectedFormationIndex, 0, formations.Count - 1);
            var next = EditorGUILayout.Popup("Formation", selected, labels);
            if (next != selected) SetSelection(authoring, next);

            var plan = formations[Mathf.Clamp(authoring.SelectedFormationIndex, 0, formations.Count - 1)];
            EditorGUILayout.LabelField("Surface", plan.Surface.ToString());
            EditorGUILayout.LabelField("Members", plan.Members.Count.ToString());
        }

        private void SetSelection(TopDown3DRockFormationAuthoring authoring, int index)
        {
            Undo.RecordObject(authoring, "Select Rock Formation");
            var plan = formations[Mathf.Clamp(index, 0, formations.Count - 1)];
            authoring.SetSelectedFormation(index, plan.StableId);
            EditorUtility.SetDirty(authoring);
        }

        private void Generate(TopDown3DRockFormationAuthoring authoring)
        {
            if (formations.Count == 0) Search(authoring);
            if (formations.Count == 0) return;

            var index = Mathf.Clamp(authoring.SelectedFormationIndex, 0, formations.Count - 1);
            var plan = formations[index];
            Undo.SetCurrentGroupName("Generate Rock Formation");
            var undoGroup = Undo.GetCurrentGroup();
            ClearGeneratedMembers(authoring, false);

            try
            {
                CreateGeneratedMembers(authoring, plan, "Generate Rock Formation");

                Undo.RecordObject(authoring, "Generate Rock Formation");
                authoring.SetSelectedFormation(index, plan.StableId);
                EditorUtility.SetDirty(authoring);
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

        private void AddSelectedFormation(TopDown3DRockFormationAuthoring authoring)
        {
            if (formations.Count == 0) Search(authoring);
            if (formations.Count == 0) return;

            var index = Mathf.Clamp(authoring.SelectedFormationIndex, 0, formations.Count - 1);
            var plan = formations[index];
            Undo.SetCurrentGroupName("Add Rock Formation");
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                var suffixLength = Mathf.Min(8, plan.StableId.Length);
                var root = new GameObject($"Rock Formation — {plan.StableId.Substring(0, suffixLength)}");
                Undo.RegisterCreatedObjectUndo(root, "Add Rock Formation");
                root.transform.SetParent(authoring.transform.parent, true);
                root.transform.position = GetSuggestedPlacement(authoring, plan);
                root.transform.rotation = authoring.transform.rotation;
                root.transform.localScale = Vector3.one;

                var addedFormation = Undo.AddComponent<TopDown3DRockFormationAuthoring>(root);
                addedFormation.Configure(
                    authoring.WorldSettings,
                    authoring.RegularRockMaterial,
                    authoring.SourceWorldPosition);
                addedFormation.SetSourceWorldSeed(authoring.SourceWorldSeed);
                addedFormation.SetSelectedFormation(index, plan.StableId);
                CreateGeneratedMembers(addedFormation, plan, "Add Rock Formation");
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

        private void AddRandomFormation(TopDown3DRockFormationAuthoring authoring)
        {
            if (formations.Count == 0) Search(authoring);
            if (formations.Count == 0) return;

            var random = new System.Random(ComputeRandomSeed(authoring));
            var index = random.Next(formations.Count);
            if (formations.Count > 1
                && formations[index].StableId == authoring.SelectedFormationStableId)
            {
                index = (index + 1) % formations.Count;
            }
            SetSelection(authoring, index);
            AddSelectedFormation(authoring);
            Undo.RecordObject(authoring, "Advance Random Rock Formation");
            authoring.AdvanceRandomSelection();
            EditorUtility.SetDirty(authoring);
        }

        private static int ComputeRandomSeed(TopDown3DRockFormationAuthoring authoring)
        {
            unchecked
            {
                return authoring.SourceWorldSeed * 486187739
                    + authoring.RandomSelectionIndex * 16777619
                    + authoring.SearchRadiusInChunks * 31;
            }
        }

        private static Vector3 GetSuggestedPlacement(
            TopDown3DRockFormationAuthoring source,
            TopDown3DRockFormationPlan plan)
        {
            var spacing = Mathf.Max(4f, plan.EnvelopeRadius * 2f + 2f);
            return source.transform.position + source.transform.right * spacing;
        }

        private static void CreateGeneratedMembers(
            TopDown3DRockFormationAuthoring authoring,
            TopDown3DRockFormationPlan plan,
            string undoName)
        {
            var generatedRoot = new GameObject("Generated Rock Members");
            Undo.RegisterCreatedObjectUndo(generatedRoot, undoName);
            generatedRoot.transform.SetParent(authoring.transform, false);
            generatedRoot.transform.localPosition = Vector3.zero;
            generatedRoot.transform.localRotation = Quaternion.identity;
            generatedRoot.transform.localScale = Vector3.one;

            var center = new Vector3(plan.EnvelopeCenter.x, 0f, plan.EnvelopeCenter.y);
            var material = ResolveMaterial(authoring, plan.Surface);
            for (var memberIndex = 0; memberIndex < plan.Members.Count; memberIndex++)
            {
                var member = plan.Members[memberIndex];
                var family = authoring.WorldSettings.NaturalObjectCatalog.GetRequiredMeshFamily(
                    member.Shape,
                    member.Variant);
                var memberObject = new GameObject($"Rock {member.MemberIndex} — {member.StableId}");
                Undo.RegisterCreatedObjectUndo(memberObject, undoName);
                memberObject.transform.SetParent(generatedRoot.transform, false);
                memberObject.transform.localPosition = member.Position - center;
                memberObject.transform.localRotation = member.Rotation;
                memberObject.transform.localScale = member.Scale;
                memberObject.AddComponent<MeshFilter>().sharedMesh = family.Lod0;
                memberObject.AddComponent<MeshRenderer>().sharedMaterial = material;
                var collider = memberObject.AddComponent<BoxCollider>();
                collider.center = family.ColliderCenter;
                collider.size = family.ColliderSize;
            }

            authoring.SetGeneratedMembersRoot(generatedRoot.transform);
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

        private static TopDown3DWorldSettings CreateSettingsWithAuthoringSeed(
            TopDown3DRockFormationAuthoring authoring)
        {
            if (authoring.WorldSettings == null)
                throw new InvalidOperationException("Choose a World Settings asset before searching formations.");

            var clone = Instantiate(authoring.WorldSettings);
            clone.hideFlags = HideFlags.HideAndDontSave;
            var serializedClone = new SerializedObject(clone);
            serializedClone.FindProperty("worldSeed").intValue = authoring.SourceWorldSeed;
            serializedClone.ApplyModifiedPropertiesWithoutUndo();
            return clone;
        }

        private static Material ResolveMaterial(
            TopDown3DRockFormationAuthoring authoring,
            TopDown3DRockSurface surface)
        {
            if (surface == TopDown3DRockSurface.Dark && authoring.WorldSettings.DarkRockMaterial != null)
                return authoring.WorldSettings.DarkRockMaterial;
            if (surface == TopDown3DRockSurface.Teal && authoring.WorldSettings.TealRockMaterial != null)
                return authoring.WorldSettings.TealRockMaterial;
            if (authoring.RegularRockMaterial != null) return authoring.RegularRockMaterial;
            throw new InvalidOperationException("Choose the production regular rock material before generating a formation.");
        }
    }
}
