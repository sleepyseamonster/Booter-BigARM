using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    internal enum TopDown3DRockLandmarkFormationRole
    {
        Crown,
        Shoulder,
        Apron,
        Outlier
    }

    internal readonly struct TopDown3DRockLandmarkFormationPlan
    {
        internal TopDown3DRockLandmarkFormationPlan(
            int index,
            TopDown3DRockLandmarkFormationRole role,
            TopDown3DRockFormationArchetype archetype,
            Vector3 localPosition,
            Quaternion localRotation,
            float width,
            float height,
            int seed)
        {
            Index = index;
            Role = role;
            Archetype = archetype;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            Width = width;
            Height = height;
            Seed = seed;
        }

        internal int Index { get; }
        internal TopDown3DRockLandmarkFormationRole Role { get; }
        internal TopDown3DRockFormationArchetype Archetype { get; }
        internal Vector3 LocalPosition { get; }
        internal Quaternion LocalRotation { get; }
        internal float Width { get; }
        internal float Height { get; }
        internal int Seed { get; }

        internal TopDown3DRockLandmarkFormationPlan WithLocalPosition(Vector3 position)
        {
            return new TopDown3DRockLandmarkFormationPlan(
                Index,
                Role,
                Archetype,
                position,
                LocalRotation,
                Width,
                Height,
                Seed);
        }
    }

    internal static class TopDown3DRockLandmarkGenerator
    {
        private const string CreateMenuPath =
            "GameObject/Booter & BigARM/Top Down 3D/New Random Rock Landmark";
        private const float GoldenAngleRadians = 2.39996323f;

        [MenuItem(CreateMenuPath, false, 22)]
        private static void CreateRandomLandmark(MenuCommand command)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                TopDown3DRockWorkbenchAuthoringEditor.WorkbenchMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException(
                    "The Rock Workbench PBR material could not be found.");
            }

            const string undoName = "Create Random Rock Landmark";
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                var root = new GameObject("Rock Landmark Workbench");
                Undo.RegisterCreatedObjectUndo(root, undoName);
                GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
                var landmark = Undo.AddComponent<TopDown3DRockLandmarkAuthoring>(root);
                var seed = TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(
                    landmark.LandmarkSeed);
                landmark.Configure(material, seed);
                GenerateIntoLandmark(landmark, seed);
                Selection.activeGameObject = root;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static IReadOnlyList<TopDown3DRockLandmarkFormationPlan> CreatePlan(
            TopDown3DRockLandmarkArchetype archetype,
            int seed,
            int formationCount,
            float width,
            float height,
            float asymmetry,
            float approachOpening)
        {
            return archetype switch
            {
                _ => CreateSpireComplexPlan(
                    seed,
                    formationCount,
                    width,
                    height,
                    asymmetry,
                    approachOpening)
            };
        }

        private static IReadOnlyList<TopDown3DRockLandmarkFormationPlan> CreateSpireComplexPlan(
            int seed,
            int formationCount,
            float width,
            float height,
            float asymmetry,
            float approachOpening)
        {
            formationCount = Mathf.Clamp(formationCount, 12, 29);
            width = Mathf.Clamp(width, 12f, 60f);
            height = Mathf.Clamp(height, 4f, 30f);
            asymmetry = Mathf.Clamp01(asymmetry);
            approachOpening = Mathf.Clamp01(approachOpening);

            var random = new System.Random(seed);
            var structuralHeading = NextRange(random, 0f, Mathf.PI * 2f);
            var approachHeading = structuralHeading
                + Mathf.PI
                + NextRange(random, -0.42f, 0.42f);
            var asymmetryDirection = new Vector2(
                Mathf.Cos(structuralHeading),
                Mathf.Sin(structuralHeading));
            var crownCount = formationCount >= 20 ? 3 : 2;
            var outlierCount = Mathf.Clamp(Mathf.RoundToInt(formationCount * 0.1f), 1, 3);
            var shoulderCount = Mathf.Clamp(
                Mathf.RoundToInt(formationCount * 0.23f),
                3,
                7);
            var apronCount = formationCount - crownCount - shoulderCount - outlierCount;
            var output = new List<TopDown3DRockLandmarkFormationPlan>(formationCount);
            var usedSeeds = new HashSet<int>();
            var index = 0;

            for (var crown = 0; crown < crownCount; crown++, index++)
            {
                var normalizedRank = crownCount <= 1 ? 0f : crown / (float)(crownCount - 1);
                var angle = structuralHeading
                    + crown * (Mathf.PI * 2f / crownCount)
                    + NextRange(random, -0.26f, 0.26f);
                var radius = crown == 0
                    ? 0f
                    : width * NextRange(random, 0.075f, 0.14f);
                var horizontal = Direction(angle) * radius;
                horizontal += asymmetryDirection * width * asymmetry
                    * (crown == 0 ? 0.045f : 0.075f);
                var formationWidth = Mathf.Clamp(
                    width * NextRange(random, 0.34f, 0.46f),
                    6f,
                    30f);
                var formationHeight = crown == 0
                    ? height
                    : height * Mathf.Lerp(0.9f, 0.66f, normalizedRank)
                        * NextRange(random, 0.92f, 1.05f);
                output.Add(CreateMember(
                    index,
                    TopDown3DRockLandmarkFormationRole.Crown,
                    TopDown3DRockFormationArchetype.PileOfRocks,
                    horizontal,
                    structuralHeading + NextRange(random, -0.45f, 0.45f),
                    formationWidth,
                    Mathf.Clamp(formationHeight, 4f, 30f),
                    seed,
                    usedSeeds));
            }

            for (var shoulder = 0; shoulder < shoulderCount; shoulder++, index++)
            {
                var angle = AvoidApproachSector(
                    structuralHeading + GoldenAngleRadians * (shoulder + 0.42f)
                        + NextRange(random, -0.18f, 0.18f),
                    approachHeading,
                    approachOpening,
                    shoulder);
                var radius = width * NextRange(random, 0.14f, 0.28f);
                var horizontal = Direction(angle) * radius;
                horizontal += asymmetryDirection * width * asymmetry
                    * NextRange(random, 0.025f, 0.075f);
                var archetype = shoulder % 3 == 1
                    ? TopDown3DRockFormationArchetype.PileOfRocks
                    : TopDown3DRockFormationArchetype.ScatteredRocks;
                var formationWidth = width * NextRange(random, 0.19f, 0.34f);
                var formationHeight = height * NextRange(random, 0.24f, 0.53f);
                output.Add(CreateMember(
                    index,
                    TopDown3DRockLandmarkFormationRole.Shoulder,
                    archetype,
                    horizontal,
                    angle + NextRange(random, -0.55f, 0.55f),
                    Mathf.Clamp(formationWidth, 4f, 24f),
                    Mathf.Clamp(formationHeight, 1.5f, 18f),
                    seed,
                    usedSeeds));
            }

            for (var apron = 0; apron < apronCount; apron++, index++)
            {
                var angle = AvoidApproachSector(
                    structuralHeading + GoldenAngleRadians * (apron + 0.17f)
                        + NextRange(random, -0.16f, 0.16f),
                    approachHeading,
                    approachOpening,
                    apron + shoulderCount);
                var radius = width * NextRange(random, 0.25f, 0.46f);
                var horizontal = Direction(angle) * radius;
                horizontal += asymmetryDirection * width * asymmetry
                    * NextRange(random, 0.01f, 0.055f);
                var usePile = apron % 2 == 0 || random.NextDouble() < 0.34d;
                var archetype = usePile
                    ? TopDown3DRockFormationArchetype.PileOfRocks
                    : TopDown3DRockFormationArchetype.ScatteredRocks;
                var widthRatio = usePile
                    ? NextRange(random, 0.3f, 0.64f)
                    : NextRange(random, 0.15f, 0.31f);
                var formationHeight = height * (usePile
                    ? NextRange(random, 0.055f, 0.13f)
                    : NextRange(random, 0.07f, 0.17f));
                output.Add(CreateMember(
                    index,
                    TopDown3DRockLandmarkFormationRole.Apron,
                    archetype,
                    horizontal,
                    angle + NextRange(random, -0.75f, 0.75f),
                    Mathf.Clamp(width * widthRatio, 4f, 30f),
                    Mathf.Clamp(formationHeight, 1f, 5.5f),
                    seed,
                    usedSeeds));
            }

            for (var outlier = 0; outlier < outlierCount; outlier++, index++)
            {
                var angle = AvoidApproachSector(
                    structuralHeading + GoldenAngleRadians * (outlier + 0.73f)
                        + Mathf.PI * 0.5f,
                    approachHeading,
                    approachOpening * 0.75f,
                    outlier + formationCount);
                var radius = width * NextRange(random, 0.46f, 0.58f);
                var horizontal = Direction(angle) * radius;
                output.Add(CreateMember(
                    index,
                    TopDown3DRockLandmarkFormationRole.Outlier,
                    TopDown3DRockFormationArchetype.ScatteredRocks,
                    horizontal,
                    angle + NextRange(random, -0.9f, 0.9f),
                    Mathf.Clamp(width * NextRange(random, 0.12f, 0.22f), 4f, 12f),
                    Mathf.Clamp(height * NextRange(random, 0.065f, 0.15f), 1f, 4.5f),
                    seed,
                    usedSeeds));
            }

            return output;
        }

        internal static void GenerateIntoLandmark(
            TopDown3DRockLandmarkAuthoring landmark,
            int seed)
        {
            if (landmark == null) return;

            const string undoName = "Generate Rock Landmark";
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                Undo.RecordObject(landmark, undoName);
                landmark.SetLandmarkSeed(seed);
                var existing = landmark.GetComponentsInChildren<
                    TopDown3DRockWorkbenchFormationAuthoring>(true);
                foreach (var formation in existing)
                {
                    if (formation != null
                        && formation.GetComponentInParent<TopDown3DRockLandmarkAuthoring>()
                            == landmark)
                    {
                        Undo.DestroyObjectImmediate(formation.gameObject);
                    }
                }

                var material = landmark.RockMaterial != null
                    ? landmark.RockMaterial
                    : AssetDatabase.LoadAssetAtPath<Material>(
                        TopDown3DRockWorkbenchAuthoringEditor.WorkbenchMaterialPath);
                if (material == null)
                {
                    throw new InvalidOperationException(
                        "The Rock Workbench PBR material could not be found.");
                }

                var plan = CreatePlan(
                    landmark.LandmarkArchetype,
                    seed,
                    landmark.GeneratedFormationCount,
                    landmark.GeneratedWidth,
                    landmark.GeneratedHeight,
                    landmark.GeneratedAsymmetry,
                    landmark.ApproachOpening);
                if (landmark.ConformToTerrain)
                {
                    plan = ConformPlanToTerrain(landmark, plan);
                }

                var generated = new List<TopDown3DRockWorkbenchFormationAuthoring>(plan.Count);
                for (var index = 0; index < plan.Count; index++)
                {
                    var spec = plan[index];
                    var root = new GameObject($"{spec.Role} Formation {index + 1}");
                    Undo.RegisterCreatedObjectUndo(root, undoName);
                    Undo.SetTransformParent(root.transform, landmark.transform, undoName);
                    root.transform.localPosition = spec.LocalPosition;
                    root.transform.localRotation = spec.LocalRotation;
                    root.transform.localScale = Vector3.one;

                    var formation = Undo.AddComponent<
                        TopDown3DRockWorkbenchFormationAuthoring>(root);
                    formation.Configure(material, spec.Seed);
                    ConfigureFormation(formation, spec, landmark);
                    TopDown3DRockWorkbenchFormationGenerator.GenerateIntoFormation(
                        formation,
                        spec.Seed);
                    generated.Add(formation);
                }

                landmark.SetPreviewStatus(
                    $"SPIRE COMPLEX: {generated.Count} editable formations share one landmark seed."
                    + (landmark.ShareGeologicalField
                        ? " Geological surface fields are unified."
                        : string.Empty));
                EditorUtility.SetDirty(landmark);
                Selection.activeGameObject = landmark.gameObject;
                foreach (var formation in generated)
                {
                    TopDown3DRockWorkbenchFormationPreview.RequestRebuild(formation, false);
                }
                SceneView.RepaintAll();
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static void ConfigureFormation(
            TopDown3DRockWorkbenchFormationAuthoring formation,
            TopDown3DRockLandmarkFormationPlan spec,
            TopDown3DRockLandmarkAuthoring landmark)
        {
            var serialized = new SerializedObject(formation);
            serialized.FindProperty("formationArchetype").enumValueIndex =
                (int)spec.Archetype;
            serialized.FindProperty("generatedOverallSize").floatValue = spec.Width;
            serialized.FindProperty("generatedHeight").floatValue = spec.Height;
            serialized.FindProperty("memberVoxelSize").floatValue = landmark.MemberVoxelSize;
            serialized.FindProperty("memberFusionSmoothness").floatValue =
                landmark.MemberFusionSmoothness;
            serialized.FindProperty("memberSurfaceRelaxation").floatValue =
                landmark.MemberSurfaceRelaxation;
            serialized.FindProperty("autoRebuild").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(formation);
        }

        private static IReadOnlyList<TopDown3DRockLandmarkFormationPlan> ConformPlanToTerrain(
            TopDown3DRockLandmarkAuthoring landmark,
            IReadOnlyList<TopDown3DRockLandmarkFormationPlan> source)
        {
            var sandbox = UnityEngine.Object.FindAnyObjectByType<
                TopDown3DLandscapeAuthoringSandbox>();
            if (sandbox == null
                || sandbox.WorldSettings == null
                || sandbox.gameObject.scene != landmark.gameObject.scene)
            {
                return source;
            }

            try
            {
                var generator = new TopDown3DWorldGenerator(sandbox.WorldSettings);
                var output = new List<TopDown3DRockLandmarkFormationPlan>(source.Count);
                foreach (var member in source)
                {
                    var supportRadius = Mathf.Clamp(member.Width * 0.22f, 0.75f, 5f);
                    var samples = new List<float>(5);
                    SampleTerrainHeight(landmark, sandbox, generator, member.LocalPosition, samples);
                    SampleTerrainHeight(landmark, sandbox, generator,
                        member.LocalPosition + Vector3.right * supportRadius, samples);
                    SampleTerrainHeight(landmark, sandbox, generator,
                        member.LocalPosition - Vector3.right * supportRadius, samples);
                    SampleTerrainHeight(landmark, sandbox, generator,
                        member.LocalPosition + Vector3.forward * supportRadius, samples);
                    SampleTerrainHeight(landmark, sandbox, generator,
                        member.LocalPosition - Vector3.forward * supportRadius, samples);
                    samples.Sort();
                    var localPosition = member.LocalPosition;
                    localPosition.y = samples[Mathf.Min(1, samples.Count - 1)];
                    output.Add(member.WithLocalPosition(localPosition));
                }
                return output;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Rock landmark formations could not sample the authoring terrain and used the landmark plane instead: {exception.Message}",
                    landmark);
                return source;
            }
        }

        private static void SampleTerrainHeight(
            TopDown3DRockLandmarkAuthoring landmark,
            TopDown3DLandscapeAuthoringSandbox sandbox,
            TopDown3DWorldGenerator generator,
            Vector3 landmarkLocalPosition,
            ICollection<float> output)
        {
            var world = landmark.transform.TransformPoint(new Vector3(
                landmarkLocalPosition.x,
                0f,
                landmarkLocalPosition.z));
            var sandboxLocal = sandbox.transform.InverseTransformPoint(world);
            var surfaceHeight = generator.SampleHeight(sandboxLocal.x, sandboxLocal.z);
            var surfaceWorld = sandbox.transform.TransformPoint(new Vector3(
                sandboxLocal.x,
                surfaceHeight,
                sandboxLocal.z));
            output.Add(landmark.transform.InverseTransformPoint(surfaceWorld).y);
        }

        private static TopDown3DRockLandmarkFormationPlan CreateMember(
            int index,
            TopDown3DRockLandmarkFormationRole role,
            TopDown3DRockFormationArchetype archetype,
            Vector2 horizontal,
            float yawRadians,
            float width,
            float height,
            int landmarkSeed,
            ISet<int> usedSeeds)
        {
            var salt = 0;
            int seed;
            do
            {
                seed = DeriveFormationSeed(landmarkSeed, index, salt++);
            }
            while (!usedSeeds.Add(seed));

            return new TopDown3DRockLandmarkFormationPlan(
                index,
                role,
                archetype,
                new Vector3(horizontal.x, 0f, horizontal.y),
                Quaternion.Euler(0f, yawRadians * Mathf.Rad2Deg, 0f),
                Mathf.Clamp(width, 4f, 30f),
                Mathf.Clamp(height, 1f, 30f),
                seed);
        }

        internal static int DeriveFormationSeed(int landmarkSeed, int index, int salt = 0)
        {
            unchecked
            {
                var value = (uint)landmarkSeed
                    + 0x9E3779B9u * (uint)(index + 1)
                    + 0x85EBCA6Bu * (uint)salt;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (int)value;
            }
        }

        private static Vector2 Direction(float angle)
        {
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private static float AvoidApproachSector(
            float angle,
            float approachHeading,
            float opening,
            int stableSide)
        {
            var halfAngle = Mathf.Lerp(0.08f, 0.62f, Mathf.Clamp01(opening));
            var delta = Mathf.DeltaAngle(
                approachHeading * Mathf.Rad2Deg,
                angle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (Mathf.Abs(delta) >= halfAngle) return angle;
            var side = Mathf.Abs(delta) > 0.0001f
                ? Mathf.Sign(delta)
                : stableSide % 2 == 0 ? 1f : -1f;
            // Push redirected members slightly beyond the boundary so several rejected
            // candidates do not form an artificial straight line along the opening edge.
            var boundaryVariation = 0.035f * (1 + Mathf.Abs(stableSide) % 3);
            return approachHeading + side * (halfAngle + boundaryVariation);
        }

        private static float NextRange(System.Random random, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }
    }
}
