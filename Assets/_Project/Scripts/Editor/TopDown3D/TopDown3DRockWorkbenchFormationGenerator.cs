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
            var memberMatrix = Matrix4x4.TRS(
                member.LocalPosition,
                member.LocalRotation,
                member.LocalScale);
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
