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
        Talus,
        Boulder,
        Slab,
        Fragment,
        PileBase,
        PileMiddle,
        PileCap
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
        private const float StableSupportQuantile = 0.16f;
        private static readonly Vector3[] GroundSupportPattern =
        {
            new Vector3(0f, -0.5f, 0f),
            new Vector3(-0.24f, -0.5f, 0f),
            new Vector3(0.24f, -0.5f, 0f),
            new Vector3(0f, -0.5f, -0.24f),
            new Vector3(0f, -0.5f, 0.24f),
            new Vector3(-0.2f, -0.5f, -0.2f),
            new Vector3(0.2f, -0.5f, -0.2f),
            new Vector3(0.2f, -0.5f, 0.2f),
            new Vector3(-0.2f, -0.5f, 0.2f)
        };

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
            return CreatePlan(
                TopDown3DRockFormationArchetype.ConnectedOutcrop,
                seed,
                rockCount,
                overallSize,
                complexity,
                verticality);
        }

        internal static IReadOnlyList<TopDown3DRockFormationMemberPlan> CreatePlan(
            TopDown3DRockFormationArchetype archetype,
            int seed,
            int rockCount,
            float overallSize,
            float complexity,
            float verticality)
        {
            return archetype switch
            {
                TopDown3DRockFormationArchetype.ScatteredRocks =>
                    CreateScatteredPlan(seed, rockCount, overallSize, complexity, verticality),
                TopDown3DRockFormationArchetype.PileOfRocks =>
                    CreateRockPilePlan(seed, rockCount, overallSize, complexity, verticality),
                _ => CreateConnectedOutcropPlan(
                    seed,
                    rockCount,
                    overallSize,
                    complexity,
                    verticality)
            };
        }

        internal static IReadOnlyList<TopDown3DRockFormationMemberPlan> CreateDimensionedPlan(
            TopDown3DRockFormationArchetype archetype,
            int seed,
            int rockCount,
            float width,
            float height,
            float complexity)
        {
            width = Mathf.Clamp(width, 4f, 30f);
            height = Mathf.Clamp(height, 1f, 30f);
            var verticality = TopDown3DRockWorkbenchAuthoring.CalculateAspectVerticality(
                width,
                height);
            var plan = CreatePlan(
                archetype,
                seed,
                rockCount,
                width,
                complexity,
                verticality);
            return FitPlanToHeight(KeepMemberYawOnly(plan), height);
        }

        private static IReadOnlyList<TopDown3DRockFormationMemberPlan> CreateConnectedOutcropPlan(
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
            var creviceHeading = NextRange(random, 0f, Mathf.PI * 2f);
            var creviceHalfAngle = Mathf.Lerp(0.44f, 0.66f, complexity);
            var baseRockSize = Mathf.Clamp(
                overallSize * Mathf.Lerp(0.34f, 0.27f, complexity),
                1.25f,
                8.5f);
            var plan = new List<TopDown3DRockFormationMemberPlan>(rockCount);

            var coreSize = Mathf.Clamp(
                baseRockSize * NextRange(random, 1.16f, 1.34f),
                1.4f,
                10f);
            var coreScale = new Vector3(
                NextRange(random, 1.16f, 1.44f),
                Mathf.Lerp(0.86f, 1.48f, verticality) * NextRange(random, 0.94f, 1.08f),
                NextRange(random, 1.16f, 1.44f));
            var coreBurial = coreSize * coreScale.y * Mathf.Lerp(0.035f, 0.065f, complexity);
            var coreMember = new TopDown3DRockFormationMemberPlan(
                Vector3.down * coreBurial,
                Quaternion.Euler(
                    NextRange(random, -4f, 4f) * complexity,
                    creviceHeading * Mathf.Rad2Deg,
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
                ? Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.Lerp(2f, 4f, complexity)),
                    2,
                    Mathf.Min(4, rockCount - 4))
                : 1;
            var crownCount = rockCount >= 9 && complexity >= 0.42f ? 1 : 0;
            var structuralSlots = remaining - talusCount - crownCount;
            var pillarCount = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(2f, 4f, complexity)),
                2,
                Mathf.Max(2, structuralSlots - 1));
            var buttressCount = structuralSlots - pillarCount;
            if (rockCount >= 8 && structuralSlots >= 4 && buttressCount < 2)
            {
                var transfer = Mathf.Min(2 - buttressCount, pillarCount - 2);
                pillarCount -= transfer;
                buttressCount += transfer;
            }

            var memberIndex = 1;
            var structuralRoles = CreateStructuralRoles(pillarCount, buttressCount);
            var structuralMembers = new List<TopDown3DRockFormationMemberPlan>(structuralSlots);
            for (var slot = 0; slot < structuralRoles.Count; slot++, memberIndex++)
            {
                var role = structuralRoles[slot];
                var angle = CreateReservedArcAngle(
                    random,
                    creviceHeading,
                    creviceHalfAngle,
                    slot,
                    structuralSlots);
                var direction = new Vector3(
                    Mathf.Cos(angle),
                    role == TopDown3DRockFormationMemberRole.Pillar ? -0.06f : -0.12f,
                    Mathf.Sin(angle)).normalized;
                TopDown3DRockFormationMemberPlan member;
                float attachmentRatio;
                if (role == TopDown3DRockFormationMemberRole.Pillar)
                {
                    var scale = new Vector3(
                        NextRange(random, 0.64f, 0.92f),
                        Mathf.Lerp(1.08f, 2.08f, verticality) * NextRange(random, 0.9f, 1.12f),
                        NextRange(random, 0.64f, 0.92f));
                    var size = Mathf.Clamp(
                        baseRockSize * NextRange(random, 0.68f, 0.94f),
                        0.9f,
                        9f);
                    member = new TopDown3DRockFormationMemberPlan(
                        Vector3.zero,
                        Quaternion.Euler(
                            NextRange(random, -7f, 7f) * complexity,
                            angle * Mathf.Rad2Deg + NextRange(random, -18f, 18f),
                            NextRange(random, -7f, 7f) * complexity),
                        scale,
                        size,
                        Mathf.Clamp01(verticality + NextRange(random, 0.08f, 0.26f)),
                        Mathf.Clamp01(Mathf.Lerp(0.56f, 0.82f, complexity)
                            + NextRange(random, -0.1f, 0.1f)),
                        Mathf.Clamp01(Mathf.Lerp(0.62f, 0.8f, complexity)),
                        DeriveMemberSeed(seed, memberIndex),
                        role);
                    attachmentRatio = 0.76f;
                }
                else
                {
                    var scale = new Vector3(
                        NextRange(random, 1.04f, 1.48f),
                        Mathf.Lerp(0.66f, 1.18f, verticality) * NextRange(random, 0.9f, 1.08f),
                        NextRange(random, 0.62f, 0.94f));
                    var size = Mathf.Clamp(
                        baseRockSize * NextRange(random, 0.62f, 0.88f),
                        0.8f,
                        8.5f);
                    member = new TopDown3DRockFormationMemberPlan(
                        Vector3.zero,
                        Quaternion.Euler(
                            NextRange(random, -10f, 10f) * complexity,
                            angle * Mathf.Rad2Deg + NextRange(random, -14f, 14f),
                            NextRange(random, -10f, 10f) * complexity),
                        scale,
                        size,
                        Mathf.Clamp01(verticality + NextRange(random, -0.16f, 0.08f)),
                        Mathf.Clamp01(Mathf.Lerp(0.62f, 0.88f, complexity)),
                        Mathf.Clamp01(Mathf.Lerp(0.68f, 0.84f, complexity)),
                        DeriveMemberSeed(seed, memberIndex),
                        role);
                    attachmentRatio = 0.8f;
                }

                var attached = AttachToCore(coreContact, member, direction, attachmentRatio);
                structuralMembers.Add(attached);
                plan.Add(attached);
            }

            for (var crown = 0; crown < crownCount; crown++, memberIndex++)
            {
                var angle = creviceHeading + Mathf.PI + NextRange(random, -0.7f, 0.7f);
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
                var angle = KeepOutsideCrevice(
                    creviceHeading
                        + 2.39996323f * (talus + 1f)
                        + NextRange(random, -0.16f, 0.16f),
                    creviceHeading,
                    creviceHalfAngle + 0.14f,
                    talus);
                var scale = new Vector3(
                    NextRange(random, 1.0f, 1.48f),
                    NextRange(random, 0.34f, 0.58f),
                    NextRange(random, 0.9f, 1.4f));
                var size = Mathf.Clamp(
                    baseRockSize * NextRange(random, 0.3f, 0.52f),
                    0.6f,
                    5f);
                var direction = new Vector3(Mathf.Cos(angle), -0.72f, Mathf.Sin(angle)).normalized;
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
                var host = FindClosestStructuralHost(structuralMembers, angle, coreMember);
                plan.Add(AttachToContacts(
                    CreateLowestContact(host),
                    CreateHighestContact(member),
                    member,
                    direction,
                    0.5f));
            }

            return plan;
        }

        private static IReadOnlyList<TopDown3DRockFormationMemberPlan> CreateScatteredPlan(
            int seed,
            int rockCount,
            float overallSize,
            float complexity,
            float verticality)
        {
            rockCount = Mathf.Clamp(rockCount, 5, 20);
            overallSize = Mathf.Clamp(overallSize, 4f, 30f);
            complexity = Mathf.Clamp01(complexity);
            verticality = Mathf.Clamp01(verticality);

            var random = new System.Random(seed);
            var baseRockSize = Mathf.Clamp(
                overallSize * Mathf.Lerp(0.09f, 0.115f, complexity),
                0.48f,
                3.45f);
            var halfWidth = Mathf.Max(
                overallSize * 0.46f,
                baseRockSize * Mathf.Sqrt(rockCount) * 1.08f);
            var halfDepth = halfWidth * NextRange(random, 0.68f, 0.92f);
            var fieldHeading = NextRange(random, 0f, Mathf.PI * 2f);
            var clearance = Mathf.Max(
                0.16f,
                overallSize * Mathf.Lerp(0.035f, 0.022f, complexity));
            var dominantCount = rockCount >= 12 ? 2 : 1;
            var slabCount = Mathf.Clamp(
                Mathf.RoundToInt((rockCount - dominantCount) * 0.38f),
                2,
                5);
            var positions = new List<Vector2>(rockCount);
            var footprintRadii = new List<float>(rockCount);
            var plan = new List<TopDown3DRockFormationMemberPlan>(rockCount);

            for (var index = 0; index < rockCount; index++)
            {
                TopDown3DRockFormationMemberRole role;
                float rockSize;
                Vector3 scale;
                float memberVerticality;
                if (index < dominantCount)
                {
                    role = TopDown3DRockFormationMemberRole.Boulder;
                    rockSize = baseRockSize * (index == 0
                        ? NextRange(random, 1.34f, 1.68f)
                        : NextRange(random, 1.08f, 1.34f));
                    scale = new Vector3(
                        NextRange(random, 1.08f, 1.44f),
                        Mathf.Lerp(0.58f, 0.98f, verticality)
                            * NextRange(random, 0.9f, 1.1f),
                        NextRange(random, 0.96f, 1.36f));
                    memberVerticality = Mathf.Clamp01(
                        0.16f + verticality * 0.42f + NextRange(random, -0.08f, 0.12f));
                }
                else if (index < dominantCount + slabCount)
                {
                    role = TopDown3DRockFormationMemberRole.Slab;
                    rockSize = baseRockSize * NextRange(random, 0.82f, 1.16f);
                    scale = new Vector3(
                        NextRange(random, 1.16f, 1.72f),
                        Mathf.Lerp(0.38f, 0.7f, verticality)
                            * NextRange(random, 0.86f, 1.06f),
                        NextRange(random, 0.78f, 1.22f));
                    memberVerticality = Mathf.Clamp01(
                        0.08f + verticality * 0.24f + NextRange(random, -0.04f, 0.1f));
                }
                else
                {
                    role = TopDown3DRockFormationMemberRole.Fragment;
                    rockSize = baseRockSize * NextRange(random, 0.58f, 0.9f);
                    scale = new Vector3(
                        NextRange(random, 0.84f, 1.26f),
                        Mathf.Lerp(0.42f, 0.76f, verticality)
                            * NextRange(random, 0.86f, 1.06f),
                        NextRange(random, 0.8f, 1.24f));
                    memberVerticality = Mathf.Clamp01(
                        0.12f + verticality * 0.3f + NextRange(random, -0.06f, 0.12f));
                }

                rockSize = Mathf.Clamp(rockSize, 0.45f, 5.8f);
                var footprintRadius = rockSize * Mathf.Max(scale.x, scale.z) * 0.43f;
                var horizontal = FindScatteredPosition(
                    random,
                    positions,
                    footprintRadii,
                    footprintRadius,
                    halfWidth,
                    halfDepth,
                    fieldHeading,
                    clearance);
                positions.Add(horizontal);
                footprintRadii.Add(footprintRadius);

                var member = new TopDown3DRockFormationMemberPlan(
                    new Vector3(horizontal.x, 0f, horizontal.y),
                    Quaternion.Euler(0f, NextRange(random, 0f, 360f), 0f),
                    scale,
                    rockSize,
                    memberVerticality,
                    Mathf.Clamp01(NextRange(random, 0.56f, 0.9f)),
                    Mathf.Clamp01(Mathf.Lerp(0.7f, 0.88f, complexity)
                        + NextRange(random, -0.06f, 0.06f)),
                    DeriveMemberSeed(seed, index),
                    role);
                plan.Add(GroundScatteredMember(
                    member,
                    NextRange(random, 0.02f, 0.06f)));
            }

            return plan;
        }

        private static IReadOnlyList<TopDown3DRockFormationMemberPlan> CreateRockPilePlan(
            int seed,
            int rockCount,
            float overallSize,
            float complexity,
            float verticality)
        {
            rockCount = Mathf.Clamp(rockCount, 5, 20);
            overallSize = Mathf.Clamp(overallSize, 4f, 30f);
            complexity = Mathf.Clamp01(complexity);
            verticality = Mathf.Clamp01(verticality);

            var random = new System.Random(seed);
            var plan = new List<TopDown3DRockFormationMemberPlan>(rockCount);
            var baseMembers = new List<TopDown3DRockFormationMemberPlan>();
            var middleMembers = new List<TopDown3DRockFormationMemberPlan>();
            var baseRockSize = Mathf.Clamp(
                overallSize * Mathf.Lerp(0.22f, 0.16f, complexity),
                0.8f,
                5.8f);
            var capCount = rockCount >= 15 ? 2 : 1;
            var baseCount = Mathf.Clamp(
                Mathf.CeilToInt(rockCount * 0.46f),
                3,
                rockCount - capCount - 1);
            var middleCount = rockCount - baseCount - capCount;
            var heading = NextRange(random, 0f, Mathf.PI * 2f);

            var coreSize = Mathf.Clamp(
                baseRockSize * NextRange(random, 1.04f, 1.18f),
                0.9f,
                6.4f);
            var core = new TopDown3DRockFormationMemberPlan(
                Vector3.zero,
                Quaternion.Euler(0f, heading * Mathf.Rad2Deg, 0f),
                new Vector3(
                    NextRange(random, 1.35f, 1.62f),
                    NextRange(random, 0.48f, 0.66f),
                    NextRange(random, 1.22f, 1.5f)),
                coreSize,
                Mathf.Clamp01(0.2f + verticality * 0.22f),
                Mathf.Clamp01(NextRange(random, 0.58f, 0.78f)),
                Mathf.Clamp01(NextRange(random, 0.76f, 0.9f)),
                DeriveMemberSeed(seed, 0),
                TopDown3DRockFormationMemberRole.PileBase);
            core = GroundScatteredMember(core, NextRange(random, 0.035f, 0.065f));
            plan.Add(core);
            baseMembers.Add(core);

            const float goldenAngle = 2.39996323f;
            for (var index = 1; index < baseCount; index++)
            {
                var angle = heading
                    + goldenAngle * index
                    + NextRange(random, -0.16f, 0.16f);
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var scale = new Vector3(
                    NextRange(random, 1.16f, 1.62f),
                    NextRange(random, 0.32f, 0.56f),
                    NextRange(random, 0.92f, 1.38f));
                var size = Mathf.Clamp(
                    baseRockSize * NextRange(random, 0.62f, 0.9f),
                    0.55f,
                    5.2f);
                var member = new TopDown3DRockFormationMemberPlan(
                    direction * baseRockSize * NextRange(random, 0.62f, 0.78f),
                    Quaternion.Euler(
                        0f,
                        angle * Mathf.Rad2Deg + NextRange(random, -28f, 28f),
                        0f),
                    scale,
                    size,
                    Mathf.Clamp01(0.12f + verticality * 0.18f),
                    Mathf.Clamp01(NextRange(random, 0.58f, 0.86f)),
                    Mathf.Clamp01(NextRange(random, 0.78f, 0.92f)),
                    DeriveMemberSeed(seed, index),
                    TopDown3DRockFormationMemberRole.PileBase);
                member = GroundScatteredMember(
                    member,
                    NextRange(random, 0.035f, 0.075f));
                plan.Add(member);
                baseMembers.Add(member);
            }

            for (var middle = 0; middle < middleCount; middle++)
            {
                var memberIndex = baseCount + middle;
                var angle = heading
                    + goldenAngle * (middle + 0.5f)
                    + NextRange(random, -0.2f, 0.2f);
                var direction = new Vector3(
                    Mathf.Cos(angle) * NextRange(random, 0.24f, 0.42f),
                    1f,
                    Mathf.Sin(angle) * NextRange(random, 0.24f, 0.42f)).normalized;
                var scale = new Vector3(
                    NextRange(random, 0.92f, 1.32f),
                    NextRange(random, 0.58f, 0.94f),
                    NextRange(random, 0.84f, 1.24f));
                var size = Mathf.Clamp(
                    baseRockSize * NextRange(random, 0.62f, 0.92f),
                    0.55f,
                    5.4f);
                var member = new TopDown3DRockFormationMemberPlan(
                    Vector3.zero,
                    Quaternion.Euler(
                        0f,
                        angle * Mathf.Rad2Deg + NextRange(random, -35f, 35f),
                        0f),
                    scale,
                    size,
                    Mathf.Clamp01(0.32f + verticality * 0.38f),
                    Mathf.Clamp01(NextRange(random, 0.56f, 0.84f)),
                    Mathf.Clamp01(NextRange(random, 0.76f, 0.9f)),
                    DeriveMemberSeed(seed, memberIndex),
                    TopDown3DRockFormationMemberRole.PileMiddle);
                var host = baseMembers[middle % baseMembers.Count];
                member = AttachToContacts(
                    CreateHighestContact(host),
                    CreateLowestContact(member),
                    member,
                    direction,
                    NextRange(random, 0.52f, 0.68f));
                plan.Add(member);
                middleMembers.Add(member);
            }

            for (var cap = 0; cap < capCount; cap++)
            {
                var memberIndex = baseCount + middleCount + cap;
                var angle = heading + Mathf.PI * 0.75f * cap + NextRange(random, -0.4f, 0.4f);
                var scale = new Vector3(
                    NextRange(random, 1.02f, 1.42f),
                    NextRange(random, 0.4f, 0.68f),
                    NextRange(random, 0.94f, 1.34f));
                var size = Mathf.Clamp(
                    baseRockSize * NextRange(random, 0.66f, 0.94f),
                    0.6f,
                    5.4f);
                var member = new TopDown3DRockFormationMemberPlan(
                    Vector3.zero,
                    Quaternion.Euler(
                        0f,
                        angle * Mathf.Rad2Deg + NextRange(random, -42f, 42f),
                        0f),
                    scale,
                    size,
                    Mathf.Clamp01(0.24f + verticality * 0.3f),
                    Mathf.Clamp01(NextRange(random, 0.5f, 0.78f)),
                    Mathf.Clamp01(NextRange(random, 0.78f, 0.92f)),
                    DeriveMemberSeed(seed, memberIndex),
                    TopDown3DRockFormationMemberRole.PileCap);
                var host = middleMembers.Count > 0
                    ? middleMembers[(middleMembers.Count - 1 - cap + middleMembers.Count)
                        % middleMembers.Count]
                    : core;
                var direction = new Vector3(
                    Mathf.Cos(angle) * 0.18f,
                    1f,
                    Mathf.Sin(angle) * 0.18f).normalized;
                plan.Add(AttachToContacts(
                    CreateHighestContact(host),
                    CreateLowestContact(member),
                    member,
                    direction,
                    NextRange(random, 0.48f, 0.62f)));
            }

            return plan;
        }

        private static Vector2 FindScatteredPosition(
            System.Random random,
            IReadOnlyList<Vector2> positions,
            IReadOnlyList<float> footprintRadii,
            float footprintRadius,
            float halfWidth,
            float halfDepth,
            float heading,
            float clearance)
        {
            var bestPosition = Vector2.zero;
            var bestGap = float.NegativeInfinity;
            for (var attempt = 0; attempt < 64; attempt++)
            {
                var radius = Mathf.Sqrt((float)random.NextDouble());
                var angle = NextRange(random, 0f, Mathf.PI * 2f);
                var local = new Vector2(
                    Mathf.Cos(angle) * radius * halfWidth,
                    Mathf.Sin(angle) * radius * halfDepth);
                var cosine = Mathf.Cos(heading);
                var sine = Mathf.Sin(heading);
                var candidate = new Vector2(
                    local.x * cosine - local.y * sine,
                    local.x * sine + local.y * cosine);

                var minimumGap = float.PositiveInfinity;
                for (var index = 0; index < positions.Count; index++)
                {
                    var gap = Vector2.Distance(candidate, positions[index])
                        - footprintRadius
                        - footprintRadii[index];
                    minimumGap = Mathf.Min(minimumGap, gap);
                }

                if (positions.Count == 0 || minimumGap >= clearance) return candidate;
                if (minimumGap <= bestGap) continue;
                bestGap = minimumGap;
                bestPosition = candidate;
            }

            return bestPosition;
        }

        private static TopDown3DRockFormationMemberPlan GroundScatteredMember(
            TopDown3DRockFormationMemberPlan member,
            float burialRatio)
        {
            var burialDepth = member.RockSize * member.LocalScale.y
                * Mathf.Clamp(burialRatio, 0.015f, 0.12f);
            var surfacePoint = new Vector3(
                member.LocalPosition.x,
                0f,
                member.LocalPosition.z);
            return SeatScatteredMember(
                member,
                surfacePoint,
                Vector3.up,
                burialDepth);
        }

        internal static float CalculateGroundSupportCoverage(
            TopDown3DRockFormationMemberPlan member,
            Vector3 surfacePoint,
            Vector3 surfaceNormal)
        {
            surfaceNormal = NormalizeSurfaceNormal(surfaceNormal);
            var distances = CollectGroundSupportDistances(member, surfaceNormal);
            var rootDistance = Vector3.Dot(
                member.LocalPosition - surfacePoint,
                surfaceNormal);
            var supported = 0;
            for (var index = 0; index < distances.Count; index++)
            {
                if (rootDistance + distances[index] <= 0.0001f) supported++;
            }

            return distances.Count > 0 ? supported / (float)distances.Count : 0f;
        }

        internal static TopDown3DRockFormationMemberPlan SeatScatteredMember(
            TopDown3DRockFormationMemberPlan member,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            float burialDepth)
        {
            surfaceNormal = NormalizeSurfaceNormal(surfaceNormal);
            var forward = Vector3.ProjectOnPlane(
                member.LocalRotation * Vector3.forward,
                surfaceNormal);
            if (forward.sqrMagnitude <= 0.000001f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
            }
            if (forward.sqrMagnitude <= 0.000001f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.right, surfaceNormal);
            }

            var aligned = WithLocalPose(
                member,
                member.LocalPosition,
                Quaternion.LookRotation(forward.normalized, surfaceNormal));
            var supportDistance = CalculateStableSupportDistance(aligned, surfaceNormal);
            var groundedPosition = surfacePoint
                - surfaceNormal * (supportDistance + Mathf.Max(0f, burialDepth));
            return WithLocalPose(aligned, groundedPosition, aligned.LocalRotation);
        }

        private static float CalculateStableSupportDistance(
            TopDown3DRockFormationMemberPlan member,
            Vector3 surfaceNormal)
        {
            var distances = CollectGroundSupportDistances(member, surfaceNormal);
            if (distances.Count == 0) return 0f;

            distances.Sort();
            var index = Mathf.Clamp(
                Mathf.CeilToInt(distances.Count * StableSupportQuantile) - 1,
                0,
                distances.Count - 1);
            return distances[index];
        }

        private static List<float> CollectGroundSupportDistances(
            TopDown3DRockFormationMemberPlan member,
            Vector3 surfaceNormal)
        {
            var rockPlan = CreateMemberRockPlan(member);
            var memberMatrix = Matrix4x4.TRS(
                Vector3.zero,
                member.LocalRotation,
                GetMemberRootScale(member));
            var distances = new List<float>(rockPlan.Count * GroundSupportPattern.Length);
            for (var volumeIndex = 0; volumeIndex < rockPlan.Count; volumeIndex++)
            {
                var source = rockPlan[volumeIndex];
                var volumeMatrix = memberMatrix * Matrix4x4.TRS(
                    source.LocalPosition,
                    source.LocalRotation,
                    source.LocalScale);
                for (var sampleIndex = 0; sampleIndex < GroundSupportPattern.Length; sampleIndex++)
                {
                    distances.Add(Vector3.Dot(
                        volumeMatrix.MultiplyPoint3x4(GroundSupportPattern[sampleIndex]),
                        surfaceNormal));
                }
            }

            return distances;
        }

        private static Vector3 NormalizeSurfaceNormal(Vector3 surfaceNormal)
        {
            return surfaceNormal.sqrMagnitude > 0.000001f
                ? surfaceNormal.normalized
                : Vector3.up;
        }

        private static TopDown3DRockFormationMemberPlan WithLocalPosition(
            TopDown3DRockFormationMemberPlan member,
            Vector3 localPosition)
        {
            return WithLocalPose(member, localPosition, member.LocalRotation);
        }

        private static TopDown3DRockFormationMemberPlan WithLocalPose(
            TopDown3DRockFormationMemberPlan member,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            return WithLocalPoseAndScale(
                member,
                localPosition,
                localRotation,
                member.LocalScale);
        }

        private static TopDown3DRockFormationMemberPlan WithLocalPoseAndScale(
            TopDown3DRockFormationMemberPlan member,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            return new TopDown3DRockFormationMemberPlan(
                localPosition,
                localRotation,
                localScale,
                member.RockSize,
                member.Verticality,
                member.Lopsidedness,
                member.Compaction,
                member.Seed,
                member.Role);
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
                IReadOnlyList<TopDown3DRockFormationMemberPlan> plan = CreateDimensionedPlan(
                    formation.FormationArchetype,
                    seed,
                    formation.GeneratedRockCount,
                    formation.GeneratedWidth,
                    formation.GeneratedHeight,
                    formation.GeneratedComplexity);
                if (formation.FormationArchetype == TopDown3DRockFormationArchetype.ScatteredRocks)
                {
                    plan = ConformScatteredPlanToTerrain(formation, plan);
                }
                for (var index = 0; index < plan.Count; index++)
                {
                    var spec = plan[index];
                    var memberObject = new GameObject($"{spec.Role} Rock {index + 1}");
                    Undo.RegisterCreatedObjectUndo(memberObject, undoName);
                    Undo.SetTransformParent(memberObject.transform, formation.transform, undoName);
                    memberObject.transform.localPosition = spec.LocalPosition;
                    memberObject.transform.localRotation = spec.LocalRotation;
                    memberObject.transform.localScale = GetMemberRootScale(spec);
                    var member = Undo.AddComponent<TopDown3DRockWorkbenchAuthoring>(memberObject);
                    member.Configure(material);
                    memberObject.GetComponent<MeshRenderer>().sharedMaterial = material;

                    var serializedMember = new SerializedObject(member);
                    if (formation.FormationArchetype == TopDown3DRockFormationArchetype.ScatteredRocks)
                    {
                        serializedMember.FindProperty("voxelSize").floatValue = Mathf.Clamp(
                            spec.RockSize * 0.055f,
                            0.055f,
                            0.16f);
                        serializedMember.FindProperty("fusionSmoothness").floatValue = Mathf.Clamp(
                            spec.RockSize * 0.028f,
                            0.025f,
                            0.11f);
                    }
                    serializedMember.FindProperty("generatedOverallScale").floatValue = spec.RockSize;
                    serializedMember.FindProperty("generatedHeight").floatValue =
                        spec.RockSize * Mathf.Max(0.01f, spec.LocalScale.y);
                    serializedMember.FindProperty("generatedAsymmetry").floatValue = spec.Lopsidedness;
                    serializedMember.FindProperty("generatedOverlap").floatValue = spec.Compaction;
                    serializedMember.FindProperty("generatedSilhouetteProfile").enumValueIndex =
                        (int)ChooseMemberSilhouetteProfile(spec);
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

        private static IReadOnlyList<TopDown3DRockFormationMemberPlan> ConformScatteredPlanToTerrain(
            TopDown3DRockWorkbenchFormationAuthoring formation,
            IReadOnlyList<TopDown3DRockFormationMemberPlan> plan)
        {
            var sandbox = UnityEngine.Object.FindAnyObjectByType<TopDown3DLandscapeAuthoringSandbox>();
            if (sandbox == null
                || sandbox.WorldSettings == null
                || sandbox.gameObject.scene != formation.gameObject.scene)
            {
                return plan;
            }

            try
            {
                var generator = new TopDown3DWorldGenerator(sandbox.WorldSettings);
                var conformed = new List<TopDown3DRockFormationMemberPlan>(plan.Count);
                for (var index = 0; index < plan.Count; index++)
                {
                    var member = plan[index];
                    var flatSupportDistance = CalculateStableSupportDistance(member, Vector3.up);
                    var burialDepth = Mathf.Max(
                        0f,
                        -(member.LocalPosition.y + flatSupportDistance));
                    var targetWorld = formation.transform.TransformPoint(new Vector3(
                        member.LocalPosition.x,
                        0f,
                        member.LocalPosition.z));
                    var targetInSandbox = sandbox.transform.InverseTransformPoint(targetWorld);
                    var surfaceHeight = generator.SampleHeight(
                        targetInSandbox.x,
                        targetInSandbox.z);
                    var surfaceNormalInSandbox = generator.SampleNormal(
                        targetInSandbox.x,
                        targetInSandbox.z);
                    var surfacePointWorld = sandbox.transform.TransformPoint(new Vector3(
                        targetInSandbox.x,
                        surfaceHeight,
                        targetInSandbox.z));
                    var surfaceNormalWorld = sandbox.transform.TransformDirection(
                        surfaceNormalInSandbox).normalized;
                    var surfacePointLocal = formation.transform.InverseTransformPoint(surfacePointWorld);
                    var surfaceNormalLocal = formation.transform.InverseTransformDirection(
                        surfaceNormalWorld).normalized;
                    conformed.Add(SeatScatteredMember(
                        member,
                        surfacePointLocal,
                        surfaceNormalLocal,
                        burialDepth));
                }

                return conformed;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Scattered rocks could not sample the authoring terrain and used the formation ground plane instead: {exception.Message}",
                    formation);
                return plan;
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

        private static IReadOnlyList<TopDown3DRockFormationMemberRole> CreateStructuralRoles(
            int pillarCount,
            int buttressCount)
        {
            var total = pillarCount + buttressCount;
            var roles = new List<TopDown3DRockFormationMemberRole>(total);
            var remainingPillars = pillarCount;
            var remainingButtresses = buttressCount;
            for (var slot = 0; slot < total; slot++)
            {
                var edge = slot == 0 || slot == total - 1;
                var placeButtress = remainingButtresses > 0
                    && (edge
                        || remainingPillars == 0
                        || remainingButtresses * (total - slot)
                            > remainingPillars * 2);
                if (placeButtress)
                {
                    roles.Add(TopDown3DRockFormationMemberRole.Buttress);
                    remainingButtresses--;
                }
                else
                {
                    roles.Add(TopDown3DRockFormationMemberRole.Pillar);
                    remainingPillars--;
                }
            }

            return roles;
        }

        private static float CreateReservedArcAngle(
            System.Random random,
            float creviceHeading,
            float creviceHalfAngle,
            int slot,
            int slotCount)
        {
            if (slotCount <= 1) return creviceHeading + Mathf.PI;

            var usableArc = Mathf.PI * 2f - creviceHalfAngle * 2f;
            var step = usableArc / (slotCount - 1);
            var jitter = Mathf.Min(0.11f, step * 0.16f);
            var angleAlongArc = Mathf.Clamp(
                slot * step + NextRange(random, -jitter, jitter),
                0f,
                usableArc);
            return creviceHeading + creviceHalfAngle + angleAlongArc;
        }

        private static float KeepOutsideCrevice(
            float angle,
            float creviceHeading,
            float clearance,
            int stableSide)
        {
            var signedDelta = Mathf.DeltaAngle(
                creviceHeading * Mathf.Rad2Deg,
                angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (Mathf.Abs(signedDelta) >= clearance) return angle;

            var side = Mathf.Abs(signedDelta) > 0.0001f
                ? Mathf.Sign(signedDelta)
                : stableSide % 2 == 0 ? 1f : -1f;
            return creviceHeading + side * clearance;
        }

        private static TopDown3DRockFormationMemberPlan FindClosestStructuralHost(
            IReadOnlyList<TopDown3DRockFormationMemberPlan> structuralMembers,
            float angle,
            TopDown3DRockFormationMemberPlan fallback)
        {
            if (structuralMembers == null || structuralMembers.Count == 0) return fallback;

            var desired = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var best = fallback;
            var bestDot = float.NegativeInfinity;
            for (var index = 0; index < structuralMembers.Count; index++)
            {
                var candidate = structuralMembers[index];
                var horizontal = new Vector2(
                    candidate.LocalPosition.x,
                    candidate.LocalPosition.z);
                if (horizontal.sqrMagnitude <= 0.000001f) continue;

                var dot = Vector2.Dot(desired, horizontal.normalized);
                if (dot <= bestDot) continue;
                bestDot = dot;
                best = candidate;
            }

            return best;
        }

        private static TopDown3DRockFormationMemberPlan AttachToContacts(
            CoreContact host,
            CoreContact memberContact,
            TopDown3DRockFormationMemberPlan member,
            Vector3 direction,
            float centerDistanceRatio)
        {
            direction = direction.sqrMagnitude <= 0.000001f
                ? Vector3.right
                : direction.normalized;
            var centerDistance = (host.Radius + memberContact.Radius)
                * Mathf.Clamp(centerDistanceRatio, 0.1f, 0.9f);
            var targetContactCenter = host.Center + direction * centerDistance;
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

        private static TopDown3DRockFormationMemberPlan AttachToCore(
            CoreContact core,
            TopDown3DRockFormationMemberPlan member,
            Vector3 direction,
            float centerDistanceRatio)
        {
            var memberContact = CreateCoreContact(member);
            return AttachToContacts(core, memberContact, member, direction, centerDistanceRatio);
        }

        private static CoreContact CreateCoreContact(
            TopDown3DRockFormationMemberPlan member)
        {
            return CreateContact(member, ContactSelection.Core);
        }

        private static CoreContact CreateLowestContact(
            TopDown3DRockFormationMemberPlan member)
        {
            return CreateContact(member, ContactSelection.Lowest);
        }

        private static CoreContact CreateHighestContact(
            TopDown3DRockFormationMemberPlan member)
        {
            return CreateContact(member, ContactSelection.Highest);
        }

        private static CoreContact CreateContact(
            TopDown3DRockFormationMemberPlan member,
            ContactSelection selection)
        {
            var rockPlan = CreateMemberRockPlan(member);
            var memberMatrix = Matrix4x4.TRS(
                member.LocalPosition,
                member.LocalRotation,
                GetMemberRootScale(member));
            var selected = CreateVolumeContact(memberMatrix, rockPlan[0]);
            if (selection == ContactSelection.Core) return selected;

            for (var index = 1; index < rockPlan.Count; index++)
            {
                var candidate = CreateVolumeContact(memberMatrix, rockPlan[index]);
                var replace = selection == ContactSelection.Lowest
                    ? candidate.Bottom < selected.Bottom
                    : candidate.Top > selected.Top;
                if (!replace) continue;
                selected = candidate;
            }

            var selectedSurface = selection == ContactSelection.Lowest
                ? selected.Bottom + selected.Radius
                : selected.Top - selected.Radius;
            return new CoreContact(
                new Vector3(
                    selected.Center.x,
                    selectedSurface,
                    selected.Center.z),
                selected.Radius,
                selected.Radius);
        }

        private static CoreContact CreateVolumeContact(
            Matrix4x4 memberMatrix,
            TopDown3DRockWorkbenchVolumeSpec source)
        {
            var volumeMatrix = memberMatrix * Matrix4x4.TRS(
                source.LocalPosition,
                source.LocalRotation,
                source.LocalScale);
            var minimumAxis = Mathf.Min(
                volumeMatrix.MultiplyVector(Vector3.right).magnitude,
                Mathf.Min(
                    volumeMatrix.MultiplyVector(Vector3.up).magnitude,
                    volumeMatrix.MultiplyVector(Vector3.forward).magnitude));
            var verticalExtent = 0.5f * (
                Mathf.Abs(volumeMatrix.MultiplyVector(Vector3.right).y)
                + Mathf.Abs(volumeMatrix.MultiplyVector(Vector3.up).y)
                + Mathf.Abs(volumeMatrix.MultiplyVector(Vector3.forward).y));
            return new CoreContact(
                volumeMatrix.MultiplyPoint3x4(Vector3.zero),
                Mathf.Max(0.05f, minimumAxis * 0.26f),
                verticalExtent);
        }

        private static IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> CreateMemberRockPlan(
            TopDown3DRockFormationMemberPlan member)
        {
            var sourceSize = GetMemberSourceSize(member);
            var cubeCount = TopDown3DRockWorkbenchAuthoring.CalculateSourceMassCount(
                sourceSize.x,
                sourceSize.y);
            return TopDown3DRockWorkbenchBaseRockGenerator.CreatePlan(
                member.Seed,
                cubeCount,
                sourceSize,
                member.Verticality,
                member.Lopsidedness,
                member.Compaction,
                ChooseMemberSilhouetteProfile(member));
        }

        internal static TopDown3DRockSilhouetteProfile ChooseMemberSilhouetteProfile(
            TopDown3DRockFormationMemberPlan member)
        {
            var variation = unchecked((uint)DeriveMemberSeed(member.Seed, 0)) % 3u;
            switch (member.Role)
            {
                case TopDown3DRockFormationMemberRole.Core:
                case TopDown3DRockFormationMemberRole.Pillar:
                case TopDown3DRockFormationMemberRole.Buttress:
                case TopDown3DRockFormationMemberRole.Crown:
                case TopDown3DRockFormationMemberRole.Talus:
                    // Preserve the accepted connected-outcrop composition while the new
                    // silhouette vocabulary is evaluated in loose and piled formations.
                    return TopDown3DRockSilhouetteProfile.Boulder;
                case TopDown3DRockFormationMemberRole.Boulder:
                    if (variation == 0u) return TopDown3DRockSilhouetteProfile.Boulder;
                    return variation == 1u
                        ? TopDown3DRockSilhouetteProfile.SplitLobe
                        : TopDown3DRockSilhouetteProfile.AngularChunk;
                case TopDown3DRockFormationMemberRole.Slab:
                    return TopDown3DRockSilhouetteProfile.Slab;
                case TopDown3DRockFormationMemberRole.Fragment:
                    return variation == 0u
                        ? TopDown3DRockSilhouetteProfile.Shard
                        : TopDown3DRockSilhouetteProfile.AngularChunk;
                case TopDown3DRockFormationMemberRole.PileBase:
                    return variation == 0u
                        ? TopDown3DRockSilhouetteProfile.SplitLobe
                        : TopDown3DRockSilhouetteProfile.Slab;
                case TopDown3DRockFormationMemberRole.PileMiddle:
                    if (variation == 0u) return TopDown3DRockSilhouetteProfile.Boulder;
                    return variation == 1u
                        ? TopDown3DRockSilhouetteProfile.AngularChunk
                        : TopDown3DRockSilhouetteProfile.SplitLobe;
                case TopDown3DRockFormationMemberRole.PileCap:
                    return variation == 0u
                        ? TopDown3DRockSilhouetteProfile.AngularChunk
                        : TopDown3DRockSilhouetteProfile.Slab;
                default:
                    return TopDown3DRockSilhouetteProfile.Boulder;
            }
        }

        private static Vector3 GetMemberSourceSize(
            TopDown3DRockFormationMemberPlan member)
        {
            return new Vector3(
                member.RockSize,
                member.RockSize * Mathf.Max(0.01f, member.LocalScale.y),
                member.RockSize);
        }

        private static Vector3 GetMemberRootScale(
            TopDown3DRockFormationMemberPlan member)
        {
            return new Vector3(member.LocalScale.x, 1f, member.LocalScale.z);
        }

        private static IReadOnlyList<TopDown3DRockFormationMemberPlan> FitPlanToHeight(
            IReadOnlyList<TopDown3DRockFormationMemberPlan> source,
            float targetHeight)
        {
            var fitted = new List<TopDown3DRockFormationMemberPlan>(source);
            for (var iteration = 0; iteration < 6; iteration++)
            {
                var currentHeight = CalculatePlanHeight(fitted);
                var correction = targetHeight / Mathf.Max(0.001f, currentHeight);
                for (var index = 0; index < fitted.Count; index++)
                {
                    var member = fitted[index];
                    var scale = member.LocalScale;
                    scale.y *= correction;
                    var position = member.LocalPosition;
                    position.y *= correction;
                    fitted[index] = WithLocalPoseAndScale(
                        member,
                        position,
                        member.LocalRotation,
                        scale);
                }
            }

            return fitted;
        }

        private static IReadOnlyList<TopDown3DRockFormationMemberPlan> KeepMemberYawOnly(
            IReadOnlyList<TopDown3DRockFormationMemberPlan> source)
        {
            var upright = new List<TopDown3DRockFormationMemberPlan>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                var member = source[index];
                var forward = Vector3.ProjectOnPlane(
                    member.LocalRotation * Vector3.forward,
                    Vector3.up);
                if (forward.sqrMagnitude <= 0.000001f) forward = Vector3.forward;
                upright.Add(WithLocalPose(
                    member,
                    member.LocalPosition,
                    Quaternion.LookRotation(forward.normalized, Vector3.up)));
            }

            return upright;
        }

        internal static float CalculatePlanHeight(
            IReadOnlyList<TopDown3DRockFormationMemberPlan> plan)
        {
            var minimum = float.PositiveInfinity;
            var maximum = float.NegativeInfinity;
            for (var memberIndex = 0; memberIndex < plan.Count; memberIndex++)
            {
                var member = plan[memberIndex];
                var rockPlan = CreateMemberRockPlan(member);
                var memberMatrix = Matrix4x4.TRS(
                    member.LocalPosition,
                    member.LocalRotation,
                    GetMemberRootScale(member));
                for (var volumeIndex = 0; volumeIndex < rockPlan.Count; volumeIndex++)
                {
                    var contact = CreateVolumeContact(memberMatrix, rockPlan[volumeIndex]);
                    minimum = Mathf.Min(minimum, contact.Bottom);
                    maximum = Mathf.Max(maximum, contact.Top);
                }
            }

            return plan.Count == 0 ? 0f : maximum - minimum;
        }

        private static float NextRange(System.Random random, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }

        private readonly struct CoreContact
        {
            internal CoreContact(Vector3 center, float radius, float verticalExtent)
            {
                Center = center;
                Radius = radius;
                VerticalExtent = verticalExtent;
            }

            internal Vector3 Center { get; }
            internal float Radius { get; }
            internal float VerticalExtent { get; }
            internal float Bottom => Center.y - VerticalExtent;
            internal float Top => Center.y + VerticalExtent;
        }

        private enum ContactSelection
        {
            Core,
            Lowest,
            Highest
        }
    }
}
