using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    internal readonly struct TopDown3DRockFormationMemberPlan
    {
        internal TopDown3DRockFormationMemberPlan(
            Vector3 localPosition,
            Quaternion localRotation,
            float rockSize,
            float verticality,
            float lopsidedness,
            float compaction,
            int seed)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            RockSize = rockSize;
            Verticality = verticality;
            Lopsidedness = lopsidedness;
            Compaction = compaction;
            Seed = seed;
        }

        internal Vector3 LocalPosition { get; }
        internal Quaternion LocalRotation { get; }
        internal float RockSize { get; }
        internal float Verticality { get; }
        internal float Lopsidedness { get; }
        internal float Compaction { get; }
        internal int Seed { get; }
    }

    internal static class TopDown3DRockWorkbenchFormationGenerator
    {
        private const string CreateMenuPath =
            "GameObject/Booter & BigARM/Top Down 3D/New Random Rock Formation";

        [MenuItem(CreateMenuPath, false, 21)]
        private static void CreateRandomFormation(MenuCommand command)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DRockWorkbenchAuthoringEditor.WorkbenchMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException(
                    "The Rock Workbench PBR material could not be found.");
            }

            const string undoName = "Create Random Rock Formation";
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                var root = new GameObject("Rock Formation Workbench");
                Undo.RegisterCreatedObjectUndo(root, undoName);
                GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
                var formation = Undo.AddComponent<TopDown3DRockWorkbenchFormationAuthoring>(root);
                var seed = TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(
                    formation.FormationSeed);
                formation.Configure(material, seed);
                GenerateIntoFormation(formation, seed);
                Selection.activeGameObject = root;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static IReadOnlyList<TopDown3DRockFormationMemberPlan> CreatePlan(
            int seed,
            int rockCount,
            float overallSize,
            float complexity,
            float verticality)
        {
            rockCount = Mathf.Clamp(rockCount, 3, 9);
            overallSize = Mathf.Clamp(overallSize, 4f, 30f);
            complexity = Mathf.Clamp01(complexity);
            verticality = Mathf.Clamp01(verticality);

            var random = new System.Random(seed);
            var angle = NextRange(random, 0f, Mathf.PI * 2f);
            var mainDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var sideDirection = new Vector3(-mainDirection.z, 0f, mainDirection.x);
            var span = overallSize * Mathf.Lerp(0.58f, 0.76f, 1f - verticality);
            var baseRockSize = Mathf.Clamp(
                overallSize * Mathf.Lerp(0.36f, 0.29f, complexity),
                1.2f,
                10f);
            var plan = new List<TopDown3DRockFormationMemberPlan>(rockCount);

            for (var index = 0; index < rockCount; index++)
            {
                var t = rockCount == 1
                    ? 0f
                    : index / (float)(rockCount - 1) - 0.5f;
                var centerWeight = 1f - Mathf.Abs(t) * 0.48f;
                var size = baseRockSize
                    * centerWeight
                    * NextRange(random, 0.86f, 1.16f);
                var sideJitter = NextRange(random, -0.32f, 0.32f)
                    * baseRockSize
                    * Mathf.Lerp(0.7f, 1.25f, complexity);
                var alongJitter = NextRange(random, -0.08f, 0.08f) * span;
                var position = mainDirection * (t * span + alongJitter)
                    + sideDirection * sideJitter;
                var yaw = angle * Mathf.Rad2Deg + NextRange(random, -28f, 28f);
                var memberVerticality = Mathf.Clamp01(
                    verticality + NextRange(random, -0.2f, 0.22f));
                var lopsidedness = Mathf.Clamp01(
                    Mathf.Lerp(0.48f, 0.82f, complexity)
                    + NextRange(random, -0.14f, 0.14f));
                var compaction = Mathf.Clamp01(
                    Mathf.Lerp(0.58f, 0.76f, complexity)
                    + NextRange(random, -0.1f, 0.1f));
                plan.Add(new TopDown3DRockFormationMemberPlan(
                    position,
                    Quaternion.Euler(0f, yaw, 0f),
                    Mathf.Clamp(size, 0.75f, 10f),
                    memberVerticality,
                    lopsidedness,
                    compaction,
                    DeriveMemberSeed(seed, index)));
            }

            return plan;
        }

        internal static void GenerateIntoFormation(
            TopDown3DRockWorkbenchFormationAuthoring formation,
            int seed)
        {
            if (formation == null) return;

            const string undoName = "Generate Rock Formation";
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                Undo.RecordObject(formation, undoName);
                formation.SetFormationSeed(seed);
                foreach (var member in formation.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true))
                {
                    if (member != null) Undo.DestroyObjectImmediate(member.gameObject);
                }

                var material = formation.RockMaterial != null
                    ? formation.RockMaterial
                    : AssetDatabase.LoadAssetAtPath<Material>(
                        TopDown3DRockWorkbenchAuthoringEditor.WorkbenchMaterialPath);
                var plan = CreatePlan(
                    seed,
                    formation.GeneratedRockCount,
                    formation.GeneratedOverallSize,
                    formation.GeneratedComplexity,
                    formation.GeneratedVerticality);
                for (var index = 0; index < plan.Count; index++)
                {
                    var spec = plan[index];
                    var memberObject = new GameObject($"Formation Rock {index + 1}");
                    Undo.RegisterCreatedObjectUndo(memberObject, undoName);
                    Undo.SetTransformParent(memberObject.transform, formation.transform, undoName);
                    memberObject.transform.localPosition = spec.LocalPosition;
                    memberObject.transform.localRotation = spec.LocalRotation;
                    var member = Undo.AddComponent<TopDown3DRockWorkbenchAuthoring>(memberObject);
                    member.Configure(material);
                    memberObject.GetComponent<MeshRenderer>().sharedMaterial = material;

                    var serializedMember = new SerializedObject(member);
                    serializedMember.FindProperty("generatedOverallScale").floatValue = spec.RockSize;
                    serializedMember.FindProperty("generatedVerticality").floatValue = spec.Verticality;
                    serializedMember.FindProperty("generatedAsymmetry").floatValue = spec.Lopsidedness;
                    serializedMember.FindProperty("generatedOverlap").floatValue = spec.Compaction;
                    serializedMember.FindProperty("showSourceVolumes").boolValue = false;
                    serializedMember.ApplyModifiedPropertiesWithoutUndo();
                    TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(member, spec.Seed);
                }

                EditorUtility.SetDirty(formation);
                Selection.activeGameObject = formation.gameObject;
                TopDown3DRockWorkbenchFormationPreview.RequestRebuild(formation, true);
                SceneView.RepaintAll();
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static int DeriveMemberSeed(int formationSeed, int index)
        {
            unchecked
            {
                var value = (uint)formationSeed + 0x9E3779B9u * (uint)(index + 1);
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (int)value;
            }
        }

        private static float NextRange(System.Random random, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }
    }
}
