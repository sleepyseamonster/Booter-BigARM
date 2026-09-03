using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    internal enum TopDown3DRockFormationMemberRole
    {
        Core,
        Pillar,
        Buttress,
        Crown,
        Talus
    }

    internal readonly struct TopDown3DRockFormationMemberPlan
    {
        internal TopDown3DRockFormationMemberPlan(
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            float rockSize,
            float verticality,
            float lopsidedness,
            float compaction,
            int seed,
            TopDown3DRockFormationMemberRole role)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
            RockSize = rockSize;
            Verticality = verticality;
            Lopsidedness = lopsidedness;
            Compaction = compaction;
            Seed = seed;
            Role = role;
        }

        internal Vector3 LocalPosition { get; }
        internal Quaternion LocalRotation { get; }
        internal Vector3 LocalScale { get; }
        internal float RockSize { get; }
        internal float Verticality { get; }
        internal float Lopsidedness { get; }
        internal float Compaction { get; }
        internal int Seed { get; }
        internal TopDown3DRockFormationMemberRole Role { get; }
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
            rockCount = Mathf.Clamp(rockCount, 5, 14);
            overallSize = Mathf.Clamp(overallSize, 4f, 30f);
            complexity = Mathf.Clamp01(complexity);
            verticality = Mathf.Clamp01(verticality);

            var random = new System.Random(seed);
            var phase = NextRange(random, 0f, Mathf.PI * 2f);
            var baseRockSize = Mathf.Clamp(
                overallSize * Mathf.Lerp(0.34f, 0.27f, complexity),
                1.25f,
                8.5f);
            var plan = new List<TopDown3DRockFormationMemberPlan>(rockCount);

            var coreSize = Mathf.Clamp(
                baseRockSize * NextRange(random, 1.04f, 1.2f),
                1.4f,
                10f);
            var coreScale = new Vector3(
                NextRange(random, 1.02f, 1.28f),
                Mathf.Lerp(0.82f, 1.5f, verticality) * NextRange(random, 0.92f, 1.08f),
                NextRange(random, 1.02f, 1.28f));
            var coreMember = new TopDown3DRockFormationMemberPlan(
                Vector3.zero,
                Quaternion.Euler(
                    NextRange(random, -4f, 4f) * complexity,
                    phase * Mathf.Rad2Deg,
                    NextRange(random, -4f, 4f) * complexity),
                coreScale,
                coreSize,
                Mathf.Clamp01(verticality + NextRange(random, -0.08f, 0.12f)),
                Mathf.Clamp01(Mathf.Lerp(0.42f, 0.68f, complexity)),
                Mathf.Clamp01(Mathf.Lerp(0.72f, 0.88f, complexity)),
                DeriveMemberSeed(seed, 0),
                TopDown3DRockFormationMemberRole.Core);
            plan.Add(coreMember);
            var coreContact = CreateCoreContact(coreMember);

            var remaining = rockCount - 1;
            var talusCount = rockCount >= 8
                ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 3f, complexity)), 1, 3)
                : 1;
            var crownCount = rockCount >= 9 && complexity >= 0.42f ? 1 : 0;
            var structuralSlots = remaining - talusCount - crownCount;
            var pillarCount = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(3f, 4f, complexity)),
                3,
                Mathf.Max(3, structuralSlots));
            var buttressCount = structuralSlots - pillarCount;
            if (rockCount >= 7 && buttressCount < 1)
            {
                pillarCount--;
                buttressCount++;
            }

            var memberIndex = 1;
            for (var pillar = 0; pillar < pillarCount; pillar++, memberIndex++)
            {
                var angle = phase
                    + Mathf.PI * 2f * pillar / pillarCount
                    + NextRange(random, -0.16f, 0.16f);
                var scale = new Vector3(
                    NextRange(random, 0.72f, 1.02f),
                    Mathf.Lerp(1.08f, 2.05f, verticality) * NextRange(random, 0.9f, 1.12f),
                    NextRange(random, 0.72f, 1.02f));
                var size = Mathf.Clamp(
                    baseRockSize * NextRange(random, 0.78f, 1.08f),
                    0.9f,
                    10f);
                var direction = new Vector3(Mathf.Cos(angle), -0.04f, Mathf.Sin(angle)).normalized;
                var member = new TopDown3DRockFormationMemberPlan(
                    Vector3.zero,
                    Quaternion.Euler(
                        NextRange(random, -7f, 7f) * complexity,
                        angle * Mathf.Rad2Deg + NextRange(random, -22f, 22f),
                        NextRange(random, -7f, 7f) * complexity),
                    scale,
                    size,
                    Mathf.Clamp01(verticality + NextRange(random, 0.08f, 0.26f)),
                    Mathf.Clamp01(Mathf.Lerp(0.56f, 0.82f, complexity)
                        + NextRange(random, -0.1f, 0.1f)),
                    Mathf.Clamp01(Mathf.Lerp(0.62f, 0.8f, complexity)),
                    DeriveMemberSeed(seed, memberIndex),
                    TopDown3DRockFormationMemberRole.Pillar);
                plan.Add(AttachToCore(coreContact, member, direction, 0.76f));
            }

            for (var buttress = 0; buttress < buttressCount; buttress++, memberIndex++)
            {
                var angle = phase
                    + Mathf.PI / Mathf.Max(3, pillarCount)
                    + Mathf.PI * 2f * buttress / Mathf.Max(1, buttressCount)
                    + NextRange(random, -0.22f, 0.22f);
                var scale = new Vector3(
                    NextRange(random, 1.08f, 1.58f),
                    Mathf.Lerp(0.72f, 1.28f, verticality) * NextRange(random, 0.9f, 1.08f),
                    NextRange(random, 0.68f, 1.0f));
                var size = Mathf.Clamp(
                    baseRockSize * NextRange(random, 0.68f, 0.96f),
                    0.8f,
                    9f);
                var direction = new Vector3(Mathf.Cos(angle), -0.08f, Mathf.Sin(angle)).normalized;
                var member = new TopDown3DRockFormationMemberPlan(
                    Vector3.zero,
                    Quaternion.Euler(
                        NextRange(random, -10f, 10f) * complexity,
                        angle * Mathf.Rad2Deg + NextRange(random, -16f, 16f),
                        NextRange(random, -10f, 10f) * complexity),
                    scale,
                    size,
                    Mathf.Clamp01(verticality + NextRange(random, -0.14f, 0.1f)),
                    Mathf.Clamp01(Mathf.Lerp(0.62f, 0.88f, complexity)),
                    Mathf.Clamp01(Mathf.Lerp(0.68f, 0.84f, complexity)),
                    DeriveMemberSeed(seed, memberIndex),
                    TopDown3DRockFormationMemberRole.Buttress);
                plan.Add(AttachToCore(coreContact, member, direction, 0.8f));
            }

            for (var crown = 0; crown < crownCount; crown++, memberIndex++)
            {
                var angle = phase + NextRange(random, 0f, Mathf.PI * 2f);
                var scale = new Vector3(
                    NextRange(random, 0.82f, 1.2f),
                    NextRange(random, 0.78f, 1.2f),
                    NextRange(random, 0.82f, 1.2f));
                var size = Mathf.Clamp(
                    baseRockSize * NextRange(random, 0.62f, 0.84f),
                    0.75f,
                    7.5f);
                var direction = new Vector3(
                    Mathf.Cos(angle) * 0.24f,
                    1f,
                    Mathf.Sin(angle) * 0.24f).normalized;
                var member = new TopDown3DRockFormationMemberPlan(
                    Vector3.zero,
                    Quaternion.Euler(
                        NextRange(random, -9f, 9f),
                        angle * Mathf.Rad2Deg + NextRange(random, -35f, 35f),
                        NextRange(random, -9f, 9f)),
                    scale,
                    size,
                    Mathf.Clamp01(verticality + NextRange(random, -0.18f, 0.16f)),
                    Mathf.Clamp01(Mathf.Lerp(0.5f, 0.76f, complexity)),
                    Mathf.Clamp01(Mathf.Lerp(0.7f, 0.86f, complexity)),
                    DeriveMemberSeed(seed, memberIndex),
                    TopDown3DRockFormationMemberRole.Crown);
                plan.Add(AttachToCore(coreContact, member, direction, 0.7f));
            }

            for (var talus = 0; talus < talusCount; talus++, memberIndex++)
            {
                var angle = phase
                    + 2.39996323f * (talus + buttressCount * 0.5f)
                    + NextRange(random, -0.2f, 0.2f);
                var scale = new Vector3(
                    NextRange(random, 1.0f, 1.48f),
                    NextRange(random, 0.44f, 0.72f),
                    NextRange(random, 0.9f, 1.4f));
                var size = Mathf.Clamp(
                    baseRockSize * NextRange(random, 0.42f, 0.68f),
                    0.7f,
                    6f);
                var direction = new Vector3(Mathf.Cos(angle), -0.18f, Mathf.Sin(angle)).normalized;
                var member = new TopDown3DRockFormationMemberPlan(
                    Vector3.zero,
                    Quaternion.Euler(
                        NextRange(random, -12f, 12f),
                        angle * Mathf.Rad2Deg + NextRange(random, -30f, 30f),
                        NextRange(random, -12f, 12f)),
                    scale,
                    size,
                    Mathf.Clamp01(verticality * 0.45f + NextRange(random, 0f, 0.16f)),
                    Mathf.Clamp01(Mathf.Lerp(0.44f, 0.72f, complexity)),
                    Mathf.Clamp01(Mathf.Lerp(0.72f, 0.9f, complexity)),
                    DeriveMemberSeed(seed, memberIndex),
                    TopDown3DRockFormationMemberRole.Talus);
                plan.Add(AttachToCore(coreContact, member, direction, 0.78f));
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
                    var memberObject = new GameObject($"{spec.Role} Rock {index + 1}");
                    Undo.RegisterCreatedObjectUndo(memberObject, undoName);
                    Undo.SetTransformParent(memberObject.transform, formation.transform, undoName);
                    memberObject.transform.localPosition = spec.LocalPosition;
                    memberObject.transform.localRotation = spec.LocalRotation;
                    memberObject.transform.localScale = spec.LocalScale;
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

        private static TopDown3DRockFormationMemberPlan AttachToCore(
            CoreContact core,
            TopDown3DRockFormationMemberPlan member,
            Vector3 direction,
            float centerDistanceRatio)
        {
            var memberContact = CreateCoreContact(member);
            direction = direction.sqrMagnitude <= 0.000001f
                ? Vector3.right
                : direction.normalized;
            var centerDistance = (core.Radius + memberContact.Radius)
                * Mathf.Clamp(centerDistanceRatio, 0.1f, 0.9f);
            var targetContactCenter = core.Center + direction * centerDistance;
            var localPosition = member.LocalPosition
                + targetContactCenter
                - memberContact.Center;
            return new TopDown3DRockFormationMemberPlan(
                localPosition,
                member.LocalRotation,
                member.LocalScale,
                member.RockSize,
                member.Verticality,
                member.Lopsidedness,
                member.Compaction,
                member.Seed,
                member.Role);
        }

        private static CoreContact CreateCoreContact(
            TopDown3DRockFormationMemberPlan member)
        {
            var cubeCount = Mathf.Clamp(
                Mathf.CeilToInt(member.RockSize * 0.9f) + 1,
                2,
                10);
            var rockPlan = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                member.Seed,
                cubeCount,
                Vector3.one * member.RockSize,
                member.Verticality,
                member.Lopsidedness,
                member.Compaction);
            var sourceCore = rockPlan[0];
            var memberMatrix = Matrix4x4.TRS(
                member.LocalPosition,
                member.LocalRotation,
                member.LocalScale);
            var coreMatrix = memberMatrix * Matrix4x4.TRS(
                sourceCore.LocalPosition,
                sourceCore.LocalRotation,
                sourceCore.LocalScale);
            var minimumAxis = Mathf.Min(
                coreMatrix.MultiplyVector(Vector3.right).magnitude,
                Mathf.Min(
                    coreMatrix.MultiplyVector(Vector3.up).magnitude,
                    coreMatrix.MultiplyVector(Vector3.forward).magnitude));
            return new CoreContact(
                coreMatrix.MultiplyPoint3x4(Vector3.zero),
                Mathf.Max(0.05f, minimumAxis * 0.26f));
        }

        private static float NextRange(System.Random random, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }

        private readonly struct CoreContact
        {
            internal CoreContact(Vector3 center, float radius)
            {
                Center = center;
                Radius = radius;
            }

            internal Vector3 Center { get; }
            internal float Radius { get; }
        }
    }
}
