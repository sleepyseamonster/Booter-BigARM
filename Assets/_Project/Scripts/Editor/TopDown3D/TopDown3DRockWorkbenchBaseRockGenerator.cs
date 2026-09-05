using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    internal enum TopDown3DRockWorkbenchMassRole
    {
        Core,
        Support,
        Detail
    }

    internal readonly struct TopDown3DRockWorkbenchVolumeSpec
    {
        internal TopDown3DRockWorkbenchVolumeSpec(
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            TopDown3DRockWorkbenchMassRole role,
            TopDown3DRockSourceShape sourceShape =
                TopDown3DRockSourceShape.WeatheredBlock,
            TopDown3DRockVolumeOperation operation =
                TopDown3DRockVolumeOperation.Additive)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
            Role = role;
            SourceShape = sourceShape;
            Operation = operation;
        }

        internal Vector3 LocalPosition { get; }
        internal Quaternion LocalRotation { get; }
        internal Vector3 LocalScale { get; }
        internal TopDown3DRockWorkbenchMassRole Role { get; }
        internal TopDown3DRockSourceShape SourceShape { get; }
        internal TopDown3DRockVolumeOperation Operation { get; }
    }

    /// <summary>
    /// Editor-only base-rock planner. A seed produces a repeatable cluster of strongly
    /// overlapping rock-mass volumes whose transforms remain ordinary editable authoring controls.
    /// </summary>
    internal static class TopDown3DRockWorkbenchBaseRockGenerator
    {
        internal static IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> CreatePlan(
            int seed,
            int cubeCount,
            Vector3 overallSize,
            float verticality,
            float asymmetry,
            float overlap,
            TopDown3DRockSilhouetteProfile silhouetteProfile =
                TopDown3DRockSilhouetteProfile.Auto,
            float majorFractures = 0f)
        {
            cubeCount = Mathf.Clamp(cubeCount, 5, 10);
            overallSize = new Vector3(
                Mathf.Max(0.5f, overallSize.x),
                Mathf.Max(0.5f, overallSize.y),
                Mathf.Max(0.5f, overallSize.z));
            verticality = Mathf.Clamp01(verticality);
            asymmetry = Mathf.Clamp01(asymmetry);
            overlap = Mathf.Clamp01(overlap);
            silhouetteProfile = ResolveSilhouetteProfile(seed, silhouetteProfile);
            if (silhouetteProfile == TopDown3DRockSilhouetteProfile.Shard
                && overallSize.y / Mathf.Max(overallSize.x, overallSize.z) >= 5f)
            {
                // An already extreme physical aspect supplies the shard silhouette by itself.
                // Keeping the weathered profile here prevents tapered tips from becoming
                // thinner than the workbench voxel grid.
                silhouetteProfile = TopDown3DRockSilhouetteProfile.Boulder;
            }
            var profileVerticality = GetProfileVerticality(verticality, silhouetteProfile);

            var random = new System.Random(seed);
            var raw = new List<TopDown3DRockWorkbenchVolumeSpec>(cubeCount);
            var preferredAngle = NextRange(random, 0f, Mathf.PI * 2f);
            var preferredDirection = new Vector3(
                Mathf.Cos(preferredAngle),
                0f,
                Mathf.Sin(preferredAngle));
            var sharedRotation = CreateSharedRotation(random, asymmetry);

            var firstScale = CreateProfileCoreScale(
                random,
                verticality,
                asymmetry,
                silhouetteProfile);
            raw.Add(new TopDown3DRockWorkbenchVolumeSpec(
                Vector3.zero,
                CreateRoleRotation(random, sharedRotation, asymmetry, TopDown3DRockWorkbenchMassRole.Core),
                firstScale,
                TopDown3DRockWorkbenchMassRole.Core,
                GetProfileCoreShape(silhouetteProfile)));

            var supportCount = cubeCount <= 2
                ? 1
                : Mathf.Clamp(Mathf.CeilToInt((cubeCount - 1) * 0.6f), 1, cubeCount - 2);

            for (var index = 1; index < cubeCount; index++)
            {
                var role = index <= supportCount
                    ? TopDown3DRockWorkbenchMassRole.Support
                    : TopDown3DRockWorkbenchMassRole.Detail;
                var roleIndex = role == TopDown3DRockWorkbenchMassRole.Support
                    ? index - 1
                    : index - supportCount - 1;
                var roleCount = role == TopDown3DRockWorkbenchMassRole.Support
                    ? supportCount
                    : cubeCount - supportCount - 1;
                var parentIndex = ChooseParentIndex(
                    random,
                    index,
                    supportCount,
                    profileVerticality,
                    asymmetry,
                    role);
                var parent = raw[parentIndex];
                var childScale = CreateRoleScale(
                    random,
                    firstScale,
                    role,
                    roleIndex,
                    roleCount,
                    profileVerticality,
                    asymmetry);
                childScale = ApplyProfileScale(
                    random,
                    childScale,
                    firstScale,
                    index,
                    role,
                    silhouetteProfile);
                var childRotation = CreateRoleRotation(random, sharedRotation, asymmetry, role);
                var direction = CreateGrowthDirection(
                    random,
                    preferredDirection,
                    index,
                    profileVerticality,
                    asymmetry,
                    role);
                var sourceShape = ChooseSourceShape(
                    seed,
                    index,
                    role,
                    asymmetry,
                    silhouetteProfile);
                if (silhouetteProfile == TopDown3DRockSilhouetteProfile.Boulder
                    && cubeCount >= 3
                    && index == 1)
                {
                    sourceShape = TopDown3DRockSourceShape.Wedge;
                }
                else if (silhouetteProfile == TopDown3DRockSilhouetteProfile.Boulder
                         && cubeCount >= 5
                         && index == supportCount + 1)
                {
                    sourceShape = TopDown3DRockSourceShape.TaperedStone;
                }
                childRotation = OrientSourceShapeTowardGrowth(
                    childRotation,
                    direction,
                    sourceShape);

                var parentRadius = DirectionalRadius(parent.LocalScale, parent.LocalRotation, direction);
                var childRadius = DirectionalRadius(childScale, childRotation, direction);
                var overlapDepth = Mathf.Lerp(
                    0.18f,
                    0.8f,
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        GetProfileOverlap(overlap, silhouetteProfile)));
                if (silhouetteProfile != TopDown3DRockSilhouetteProfile.Boulder)
                {
                    // Wedges and tapered stones remove part of their nominal box. Preserve
                    // enough overlap for a connected surface without burying their entire
                    // silhouette inside the dominant core mass.
                    var safeLowCompactionOverlap = silhouetteProfile
                        == TopDown3DRockSilhouetteProfile.Slab
                        ? 0.76f
                        : 0.72f;
                    var minimumShapedOverlap = Mathf.Lerp(
                        safeLowCompactionOverlap,
                        0.64f,
                        Mathf.InverseLerp(0f, 0.5f, overlap));
                    overlapDepth = Mathf.Max(overlapDepth, minimumShapedOverlap);
                }
                overlapDepth = Mathf.Clamp01(
                    overlapDepth
                    + GetShapeOverlapAllowance(sourceShape)
                    + GetShapeOverlapAllowance(parent.SourceShape) * 0.65f);
                var centerDistance = (parentRadius + childRadius) * (1f - overlapDepth);
                var childPosition = parent.LocalPosition + direction * centerDistance;
                raw.Add(new TopDown3DRockWorkbenchVolumeSpec(
                    childPosition,
                    childRotation,
                    childScale,
                    role,
                    sourceShape));
            }

            var fitted = FitPlanToSizeAndGround(raw, overallSize);
            return AddFractureCuts(
                fitted,
                seed,
                overallSize,
                silhouetteProfile,
                majorFractures);
        }

        internal static int CreateNewSeed(int currentSeed)
        {
            var seed = Guid.NewGuid().GetHashCode();
            return seed == currentSeed ? unchecked(seed ^ (int)0x9E3779B9) : seed;
        }

        internal static int DeriveVolumeShapeSeed(int generationSeed, int volumeIndex)
        {
            unchecked
            {
                var value = (uint)generationSeed + 0x9E3779B9u * (uint)(volumeIndex + 1);
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (int)value;
            }
        }

        internal static void GenerateIntoWorkbench(
            TopDown3DRockWorkbenchAuthoring authoring,
            int seed)
        {
            if (authoring == null) return;

            const string undoName = "Generate Base Rock";
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                Undo.RecordObject(authoring, undoName);
                authoring.SetGenerationSeed(seed);

                var existing = authoring.GetComponentsInChildren<TopDown3DRockVolumeNode>(true);
                foreach (var node in existing)
                {
                    if (node != null) Undo.DestroyObjectImmediate(node.gameObject);
                }

                var silhouetteProfile = authoring.GeneratedSilhouetteProfile;
                if (silhouetteProfile == TopDown3DRockSilhouetteProfile.Auto
                    && authoring.SurfacePreset == TopDown3DRockSurfacePreset.DarkFracturedDesert)
                {
                    silhouetteProfile = ResolveDarkDesertSilhouetteProfile(seed);
                }
                var plan = CreatePlan(
                    seed,
                    authoring.GeneratedCubeCount,
                    authoring.GeneratedOverallSize,
                    authoring.GeneratedVerticality,
                    authoring.GeneratedAsymmetry,
                    authoring.GeneratedOverlap,
                    silhouetteProfile,
                    authoring.GeneratedMajorFractures);
                for (var index = 0; index < plan.Count; index++)
                {
                    var spec = plan[index];
                    var volumeObject = new GameObject(
                        $"{GetShapeLabel(spec.SourceShape)} Volume {index + 1}");
                    Undo.RegisterCreatedObjectUndo(volumeObject, undoName);
                    Undo.SetTransformParent(volumeObject.transform, authoring.transform, undoName);
                    volumeObject.transform.localPosition = spec.LocalPosition;
                    volumeObject.transform.localRotation = spec.LocalRotation;
                    volumeObject.transform.localScale = spec.LocalScale;
                    var node = Undo.AddComponent<TopDown3DRockVolumeNode>(volumeObject);
                    node.SetSourceShape(spec.SourceShape);
                    node.SetOperation(spec.Operation);
                    node.SetShapeSeed(DeriveVolumeShapeSeed(seed, index));
                    EditorUtility.SetDirty(node);
                }

                EditorUtility.SetDirty(authoring);
                Selection.activeGameObject = authoring.gameObject;
                TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, true);
                SceneView.RepaintAll();
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> AddFractureCuts(
            IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> stone,
            int seed,
            Vector3 overallSize,
            TopDown3DRockSilhouetteProfile silhouetteProfile,
            float majorFractures)
        {
            var fractureCount = TopDown3DRockWorkbenchAuthoring.CalculateFractureCount(
                majorFractures);
            if (fractureCount == 0) return stone;

            var result = new List<TopDown3DRockWorkbenchVolumeSpec>(
                stone.Count + fractureCount);
            result.AddRange(stone);
            var random = new System.Random(seed ^ unchecked((int)0x6A09E667));
            var horizontalSize = Mathf.Max(
                0.5f,
                Mathf.Min(overallSize.x, overallSize.z));
            var height = Mathf.Max(0.5f, overallSize.y);
            if (height / horizontalSize > 4f) return stone;
            var thickness = Mathf.Max(
                0.11f,
                horizontalSize * Mathf.Lerp(0.028f, 0.06f, Mathf.Clamp01(majorFractures)));

            for (var index = 0; index < fractureCount; index++)
            {
                var heading = NextRange(random, 0f, 360f);
                var radians = heading * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                var topCut = index == fractureCount - 1
                    && (fractureCount >= 3
                        || silhouetteProfile == TopDown3DRockSilhouetteProfile.BrokenSlab);
                Vector3 position;
                Vector3 scale;
                Quaternion rotation;
                if (topCut)
                {
                    position = outward * NextRange(
                        random,
                        -horizontalSize * 0.08f,
                        horizontalSize * 0.08f);
                    position.y = height * NextRange(random, 0.83f, 0.91f);
                    scale = new Vector3(
                        thickness * NextRange(random, 0.78f, 1.18f),
                        height * NextRange(random, 0.28f, 0.42f),
                        horizontalSize * NextRange(random, 0.46f, 0.68f));
                    rotation = Quaternion.Euler(
                        NextRange(random, -8f, 8f),
                        heading,
                        NextRange(random, -12f, 12f));
                }
                else
                {
                    position = outward * horizontalSize * NextRange(random, 0.31f, 0.38f);
                    position.y = height * NextRange(random, 0.44f, 0.58f);
                    scale = new Vector3(
                        thickness * NextRange(random, 0.82f, 1.22f),
                        height * NextRange(random, 0.58f, 0.88f),
                        horizontalSize * NextRange(random, 0.42f, 0.56f));
                    rotation = Quaternion.Euler(
                        NextRange(random, -7f, 7f),
                        heading,
                        NextRange(random, -20f, 20f));
                }

                result.Add(new TopDown3DRockWorkbenchVolumeSpec(
                    position,
                    rotation,
                    scale,
                    TopDown3DRockWorkbenchMassRole.Detail,
                    TopDown3DRockSourceShape.FractureCut,
                    TopDown3DRockVolumeOperation.Subtractive));
            }

            return result;
        }

        internal static Bounds CalculateBounds(IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> plan)
        {
            if (plan == null || plan.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);

            var firstIndex = -1;
            for (var index = 0; index < plan.Count; index++)
            {
                if (plan[index].Operation != TopDown3DRockVolumeOperation.Additive) continue;
                firstIndex = index;
                break;
            }
            if (firstIndex < 0) return new Bounds(Vector3.zero, Vector3.zero);

            var first = plan[firstIndex];
            var firstExtents = GetRotatedExtents(first.LocalRotation, first.LocalScale);
            var min = first.LocalPosition - firstExtents;
            var max = first.LocalPosition + firstExtents;
            for (var index = firstIndex + 1; index < plan.Count; index++)
            {
                var spec = plan[index];
                if (spec.Operation != TopDown3DRockVolumeOperation.Additive) continue;
                var extents = GetRotatedExtents(spec.LocalRotation, spec.LocalScale);
                min = Vector3.Min(min, spec.LocalPosition - extents);
                max = Vector3.Max(max, spec.LocalPosition + extents);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private static IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> FitPlanToSizeAndGround(
            IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> raw,
            Vector3 overallSize)
        {
            var rawBounds = CalculateBounds(raw);
            var fitted = new List<TopDown3DRockWorkbenchVolumeSpec>(raw.Count);
            var independentWidthAndHeight = Mathf.Abs(overallSize.x - overallSize.z) <= 0.001f;
            foreach (var spec in raw)
            {
                fitted.Add(new TopDown3DRockWorkbenchVolumeSpec(
                    spec.LocalPosition - rawBounds.center,
                    independentWidthAndHeight
                        ? KeepYawOnly(spec.LocalRotation)
                        : spec.LocalRotation,
                    spec.LocalScale,
                    spec.Role,
                    spec.SourceShape,
                    spec.Operation));
            }

            if (independentWidthAndHeight)
            {
                // Upright sources allow one exact horizontal/vertical affine scale without
                // shearing their editable transforms or weakening their overlaps.
                var bounds = CalculateBounds(fitted);
                ApplyAxisCorrection(fitted, new Vector3(
                    overallSize.x / Mathf.Max(bounds.size.x, 0.001f),
                    overallSize.y / Mathf.Max(bounds.size.y, 0.001f),
                    overallSize.z / Mathf.Max(bounds.size.z, 0.001f)));
            }
            else
            {
                // Compatibility path for earlier callers that supplied three unrelated axes.
                // Uniform fitting preserves their previous connected-volume behavior.
                var size = rawBounds.size;
                var fit = Mathf.Min(
                    overallSize.x / Mathf.Max(size.x, 0.001f),
                    overallSize.y / Mathf.Max(size.y, 0.001f),
                    overallSize.z / Mathf.Max(size.z, 0.001f));
                ApplyAxisCorrection(fitted, Vector3.one * fit);
            }

            var fittedMinimumY = CalculateBounds(fitted).min.y;
            for (var index = 0; index < fitted.Count; index++)
            {
                var spec = fitted[index];
                fitted[index] = new TopDown3DRockWorkbenchVolumeSpec(
                    spec.LocalPosition + Vector3.up * -fittedMinimumY,
                    spec.LocalRotation,
                    spec.LocalScale,
                    spec.Role,
                    spec.SourceShape,
                    spec.Operation);
            }
            return fitted;
        }

        private static void ApplyAxisCorrection(
            List<TopDown3DRockWorkbenchVolumeSpec> plan,
            Vector3 correction)
        {
            for (var index = 0; index < plan.Count; index++)
            {
                var spec = plan[index];
                plan[index] = new TopDown3DRockWorkbenchVolumeSpec(
                    Vector3.Scale(spec.LocalPosition, correction),
                    spec.LocalRotation,
                    Vector3.Scale(spec.LocalScale, correction),
                    spec.Role,
                    spec.SourceShape,
                    spec.Operation);
            }
        }

        private static Quaternion KeepYawOnly(Quaternion rotation)
        {
            var forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude <= 0.000001f) forward = Vector3.forward;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private static TopDown3DRockSourceShape ChooseSourceShape(
            int seed,
            int index,
            TopDown3DRockWorkbenchMassRole role,
            float asymmetry,
            TopDown3DRockSilhouetteProfile silhouetteProfile)
        {
            if (role == TopDown3DRockWorkbenchMassRole.Core)
                return GetProfileCoreShape(silhouetteProfile);

            switch (silhouetteProfile)
            {
                case TopDown3DRockSilhouetteProfile.Slab:
                    return index % 3 == 2
                        ? TopDown3DRockSourceShape.WeatheredBlock
                        : TopDown3DRockSourceShape.Wedge;
                case TopDown3DRockSilhouetteProfile.AngularChunk:
                    return index % 2 == 0
                        ? TopDown3DRockSourceShape.Wedge
                        : TopDown3DRockSourceShape.TaperedStone;
                case TopDown3DRockSilhouetteProfile.SplitLobe:
                    if (index == 1) return TopDown3DRockSourceShape.WeatheredBlock;
                    return index % 2 == 0
                        ? TopDown3DRockSourceShape.TaperedStone
                        : TopDown3DRockSourceShape.Wedge;
                case TopDown3DRockSilhouetteProfile.Shard:
                    return role == TopDown3DRockWorkbenchMassRole.Support
                        ? TopDown3DRockSourceShape.TaperedStone
                        : TopDown3DRockSourceShape.Wedge;
                case TopDown3DRockSilhouetteProfile.FracturedBoulder:
                    return index % 3 == 0
                        ? TopDown3DRockSourceShape.WeatheredBlock
                        : index % 2 == 0
                            ? TopDown3DRockSourceShape.Wedge
                            : TopDown3DRockSourceShape.TaperedStone;
                case TopDown3DRockSilhouetteProfile.BlockyMonolith:
                    return index % 3 == 2
                        ? TopDown3DRockSourceShape.Wedge
                        : TopDown3DRockSourceShape.WeatheredBlock;
                case TopDown3DRockSilhouetteProfile.BrokenSlab:
                    return index % 3 == 1
                        ? TopDown3DRockSourceShape.WeatheredBlock
                        : TopDown3DRockSourceShape.Wedge;
            }

            var shapeSeed = DeriveVolumeShapeSeed(seed ^ unchecked((int)0x5F356495), index);
            var roll = HashToUnitFloat(shapeSeed);
            var wedgeChance = role == TopDown3DRockWorkbenchMassRole.Support
                ? Mathf.Lerp(0.3f, 0.44f, asymmetry)
                : Mathf.Lerp(0.42f, 0.56f, asymmetry);
            var taperedChance = role == TopDown3DRockWorkbenchMassRole.Support
                ? Mathf.Lerp(0.12f, 0.2f, asymmetry)
                : Mathf.Lerp(0.18f, 0.26f, asymmetry);
            if (roll < wedgeChance) return TopDown3DRockSourceShape.Wedge;
            if (roll < wedgeChance + taperedChance)
                return TopDown3DRockSourceShape.TaperedStone;
            return TopDown3DRockSourceShape.WeatheredBlock;
        }

        internal static TopDown3DRockSilhouetteProfile ResolveSilhouetteProfile(
            int seed,
            TopDown3DRockSilhouetteProfile requested)
        {
            if (requested != TopDown3DRockSilhouetteProfile.Auto) return requested;

            var profileSeed = DeriveVolumeShapeSeed(
                seed ^ unchecked((int)0x2C1B3C6D),
                0);
            return (TopDown3DRockSilhouetteProfile)(1
                + unchecked((uint)profileSeed) % 5u);
        }

        internal static TopDown3DRockSilhouetteProfile ResolveDarkDesertSilhouetteProfile(
            int seed)
        {
            var profileSeed = DeriveVolumeShapeSeed(
                seed ^ unchecked((int)0x510E527F),
                0);
            return (TopDown3DRockSilhouetteProfile)(1
                + unchecked((uint)profileSeed) % 8u);
        }

        private static float GetProfileVerticality(
            float verticality,
            TopDown3DRockSilhouetteProfile silhouetteProfile)
        {
            switch (silhouetteProfile)
            {
                case TopDown3DRockSilhouetteProfile.Slab:
                    return verticality * 0.28f;
                case TopDown3DRockSilhouetteProfile.AngularChunk:
                    return Mathf.Lerp(verticality, 0.58f, 0.25f);
                case TopDown3DRockSilhouetteProfile.SplitLobe:
                    return Mathf.Lerp(verticality, 0.3f, 0.5f);
                case TopDown3DRockSilhouetteProfile.Shard:
                    return Mathf.Lerp(0.35f, 1f, verticality);
                case TopDown3DRockSilhouetteProfile.FracturedBoulder:
                    return Mathf.Lerp(verticality, 0.48f, 0.32f);
                case TopDown3DRockSilhouetteProfile.BlockyMonolith:
                    return Mathf.Lerp(0.62f, 1f, verticality);
                case TopDown3DRockSilhouetteProfile.BrokenSlab:
                    return verticality * 0.22f;
                default:
                    return verticality;
            }
        }

        private static Vector3 CreateProfileCoreScale(
            System.Random random,
            float verticality,
            float asymmetry,
            TopDown3DRockSilhouetteProfile silhouetteProfile)
        {
            Vector3 center;
            switch (silhouetteProfile)
            {
                case TopDown3DRockSilhouetteProfile.Slab:
                    center = new Vector3(1.85f, Mathf.Lerp(0.48f, 0.78f, verticality), 1.16f);
                    break;
                case TopDown3DRockSilhouetteProfile.AngularChunk:
                    center = new Vector3(1.18f, Mathf.Lerp(0.82f, 1.42f, verticality), 1.02f);
                    break;
                case TopDown3DRockSilhouetteProfile.SplitLobe:
                    center = new Vector3(1.12f, Mathf.Lerp(0.72f, 1.22f, verticality), 1.04f);
                    break;
                case TopDown3DRockSilhouetteProfile.Shard:
                    center = new Vector3(0.86f, Mathf.Lerp(0.72f, 1.95f, verticality), 0.76f);
                    break;
                case TopDown3DRockSilhouetteProfile.FracturedBoulder:
                    center = new Vector3(1.5f, Mathf.Lerp(0.9f, 1.48f, verticality), 1.34f);
                    break;
                case TopDown3DRockSilhouetteProfile.BlockyMonolith:
                    center = new Vector3(1.04f, Mathf.Lerp(1.38f, 2.12f, verticality), 0.96f);
                    break;
                case TopDown3DRockSilhouetteProfile.BrokenSlab:
                    center = new Vector3(1.92f, Mathf.Lerp(0.42f, 0.68f, verticality), 1.24f);
                    break;
                default:
                    center = new Vector3(1.42f, Mathf.Lerp(0.72f, 1.58f, verticality), 1.28f);
                    break;
            }

            return new Vector3(
                VariedFraction(random, center.x, center.x * 0.1f, asymmetry),
                VariedFraction(random, center.y, center.y * 0.08f, asymmetry),
                VariedFraction(random, center.z, center.z * 0.1f, asymmetry));
        }

        private static TopDown3DRockSourceShape GetProfileCoreShape(
            TopDown3DRockSilhouetteProfile silhouetteProfile)
        {
            switch (silhouetteProfile)
            {
                case TopDown3DRockSilhouetteProfile.Slab:
                    return TopDown3DRockSourceShape.Wedge;
                case TopDown3DRockSilhouetteProfile.AngularChunk:
                case TopDown3DRockSilhouetteProfile.Shard:
                    return TopDown3DRockSourceShape.TaperedStone;
                case TopDown3DRockSilhouetteProfile.BrokenSlab:
                    return TopDown3DRockSourceShape.Wedge;
                case TopDown3DRockSilhouetteProfile.FracturedBoulder:
                case TopDown3DRockSilhouetteProfile.BlockyMonolith:
                    return TopDown3DRockSourceShape.WeatheredBlock;
                default:
                    return TopDown3DRockSourceShape.WeatheredBlock;
            }
        }

        private static Vector3 ApplyProfileScale(
            System.Random random,
            Vector3 childScale,
            Vector3 coreScale,
            int index,
            TopDown3DRockWorkbenchMassRole role,
            TopDown3DRockSilhouetteProfile silhouetteProfile)
        {
            var support = role == TopDown3DRockWorkbenchMassRole.Support;
            switch (silhouetteProfile)
            {
                case TopDown3DRockSilhouetteProfile.Slab:
                    return Vector3.Scale(childScale, new Vector3(
                        support ? NextRange(random, 1.05f, 1.35f) : 0.9f,
                        support ? 0.62f : 0.48f,
                        support ? NextRange(random, 0.82f, 1.18f) : 0.72f));
                case TopDown3DRockSilhouetteProfile.AngularChunk:
                    return Vector3.Scale(childScale, new Vector3(
                        support ? 1.2f : 0.88f,
                        support ? 1.08f : 0.9f,
                        support ? 1.08f : 0.78f));
                case TopDown3DRockSilhouetteProfile.SplitLobe:
                    if (index == 1)
                    {
                        return Vector3.Scale(coreScale, new Vector3(
                            NextRange(random, 0.82f, 1.02f),
                            NextRange(random, 0.72f, 0.94f),
                            NextRange(random, 0.8f, 1f)));
                    }
                    return Vector3.Scale(childScale, support
                        ? new Vector3(1.15f, 0.92f, 1.08f)
                        : new Vector3(0.9f, 0.82f, 0.84f));
                case TopDown3DRockSilhouetteProfile.Shard:
                    return Vector3.Scale(childScale, new Vector3(
                        support ? 0.9f : 0.78f,
                        support ? 1.28f : 1.04f,
                        support ? 0.86f : 0.74f));
                case TopDown3DRockSilhouetteProfile.FracturedBoulder:
                    return Vector3.Scale(childScale, support
                        ? new Vector3(1.16f, 1.02f, 1.12f)
                        : new Vector3(0.82f, 0.78f, 0.8f));
                case TopDown3DRockSilhouetteProfile.BlockyMonolith:
                    return Vector3.Scale(childScale, new Vector3(
                        support ? 0.92f : 0.72f,
                        support ? 1.3f : 0.92f,
                        support ? 0.88f : 0.7f));
                case TopDown3DRockSilhouetteProfile.BrokenSlab:
                    return Vector3.Scale(childScale, new Vector3(
                        support ? NextRange(random, 1.16f, 1.48f) : 0.84f,
                        support ? 0.52f : 0.4f,
                        support ? NextRange(random, 0.9f, 1.24f) : 0.7f));
                default:
                    return Vector3.Scale(childScale, support
                        ? new Vector3(1.08f, 1f, 1.04f)
                        : Vector3.one);
            }
        }

        private static float GetProfileOverlap(
            float overlap,
            TopDown3DRockSilhouetteProfile silhouetteProfile)
        {
            switch (silhouetteProfile)
            {
                case TopDown3DRockSilhouetteProfile.Slab:
                    return Mathf.Lerp(overlap, 0.82f, 0.75f);
                case TopDown3DRockSilhouetteProfile.AngularChunk:
                    return Mathf.Lerp(overlap, 0.58f, 0.35f);
                case TopDown3DRockSilhouetteProfile.SplitLobe:
                    return Mathf.Lerp(overlap, 0.52f, 0.35f);
                case TopDown3DRockSilhouetteProfile.Shard:
                    return Mathf.Lerp(overlap, 0.68f, 0.55f);
                case TopDown3DRockSilhouetteProfile.FracturedBoulder:
                    return Mathf.Lerp(overlap, 0.56f, 0.4f);
                case TopDown3DRockSilhouetteProfile.BlockyMonolith:
                    return Mathf.Lerp(overlap, 0.64f, 0.42f);
                case TopDown3DRockSilhouetteProfile.BrokenSlab:
                    return Mathf.Lerp(overlap, 0.76f, 0.62f);
                default:
                    return overlap;
            }
        }

        private static float GetShapeOverlapAllowance(TopDown3DRockSourceShape sourceShape)
        {
            switch (sourceShape)
            {
                case TopDown3DRockSourceShape.Wedge:
                    return 0.09f;
                case TopDown3DRockSourceShape.TaperedStone:
                    return 0.07f;
                default:
                    return 0f;
            }
        }

        private static Quaternion OrientSourceShapeTowardGrowth(
            Quaternion rotation,
            Vector3 growthDirection,
            TopDown3DRockSourceShape sourceShape)
        {
            if (sourceShape != TopDown3DRockSourceShape.Wedge) return rotation;

            var localUp = rotation * Vector3.up;
            var currentSlopeHeading = Vector3.ProjectOnPlane(
                rotation * Vector3.right,
                localUp);
            var desiredSlopeHeading = Vector3.ProjectOnPlane(
                growthDirection,
                localUp);
            if (currentSlopeHeading.sqrMagnitude <= 0.000001f
                || desiredSlopeHeading.sqrMagnitude <= 0.000001f)
            {
                return rotation;
            }

            return Quaternion.FromToRotation(
                       currentSlopeHeading.normalized,
                       desiredSlopeHeading.normalized)
                   * rotation;
        }

        private static float HashToUnitFloat(int seed)
        {
            return (unchecked((uint)seed) & 0x00FFFFFFu) / 16777215f;
        }

        private static string GetShapeLabel(TopDown3DRockSourceShape sourceShape)
        {
            return sourceShape switch
            {
                TopDown3DRockSourceShape.Wedge => "Wedge",
                TopDown3DRockSourceShape.TaperedStone => "Tapered Stone",
                TopDown3DRockSourceShape.FractureCut => "Fracture Cut",
                _ => "Weathered Block"
            };
        }

        private static int ChooseParentIndex(
            System.Random random,
            int index,
            int supportCount,
            float verticality,
            float asymmetry,
            TopDown3DRockWorkbenchMassRole role)
        {
            if (index == 1) return 0;

            if (role == TopDown3DRockWorkbenchMassRole.Detail)
            {
                return random.Next(0, supportCount + 1);
            }

            var continueSpineChance = Mathf.Lerp(0.18f, 0.82f, verticality);
            continueSpineChance = Mathf.Lerp(
                continueSpineChance,
                0.92f,
                Mathf.SmoothStep(0f, 1f, asymmetry) * 0.72f);
            if (random.NextDouble() < continueSpineChance) return index - 1;
            return random.NextDouble() < 0.72 ? 0 : random.Next(1, index);
        }

        private static Vector3 CreateRoleScale(
            System.Random random,
            Vector3 coreScale,
            TopDown3DRockWorkbenchMassRole role,
            int roleIndex,
            int roleCount,
            float verticality,
            float asymmetry)
        {
            var progress = roleCount <= 1 ? 0f : roleIndex / (float)(roleCount - 1);
            var ratio = role == TopDown3DRockWorkbenchMassRole.Support
                ? Mathf.Lerp(0.72f, 0.48f, progress)
                : Mathf.Lerp(0.4f, 0.25f, progress);
            var horizontalVariation = Mathf.Lerp(0.025f, 0.28f, asymmetry);
            var verticalVariation = Mathf.Lerp(0.02f, 0.22f, asymmetry);
            var verticalRatio = ratio * (role == TopDown3DRockWorkbenchMassRole.Support
                ? Mathf.Lerp(0.78f, 1.1f, verticality)
                : Mathf.Lerp(0.7f, 0.92f, verticality));
            return new Vector3(
                coreScale.x * VariedFraction(random, ratio, horizontalVariation, 1f),
                coreScale.y * VariedFraction(random, verticalRatio, verticalVariation, 1f),
                coreScale.z * VariedFraction(random, ratio, horizontalVariation, 1f));
        }

        private static Vector3 CreateGrowthDirection(
            System.Random random,
            Vector3 preferredDirection,
            int index,
            float verticality,
            float asymmetry,
            TopDown3DRockWorkbenchMassRole role)
        {
            const float goldenAngle = 2.39996323f;
            var preferredAngle = Mathf.Atan2(preferredDirection.z, preferredDirection.x);
            var balancedAngle = preferredAngle + index * goldenAngle;
            var balancedDirection = new Vector3(
                Mathf.Cos(balancedAngle),
                0f,
                Mathf.Sin(balancedAngle));
            var randomAngle = NextRange(random, 0f, Mathf.PI * 2f);
            var randomDirection = new Vector3(
                Mathf.Cos(randomAngle),
                0f,
                Mathf.Sin(randomAngle));
            var irregularity = Mathf.SmoothStep(0f, 1f, asymmetry) * 0.35f;
            var horizontal = Vector3.Slerp(balancedDirection, randomDirection, irregularity);
            var directionalBias = Mathf.SmoothStep(0f, 1f, asymmetry) * 0.88f;
            horizontal = Vector3.Slerp(horizontal, preferredDirection, directionalBias).normalized;

            var detail = role == TopDown3DRockWorkbenchMassRole.Detail;
            var minimumY = detail
                ? Mathf.Lerp(-0.14f, 0.06f, verticality)
                : Mathf.Lerp(-0.08f, 0.38f, verticality);
            var maximumY = detail
                ? Mathf.Lerp(0.18f, 0.62f, verticality)
                : Mathf.Lerp(0.14f, 1.12f, verticality);
            var horizontalWeight = detail
                ? Mathf.Lerp(1f, 0.58f, verticality)
                : Mathf.Lerp(1f, 0.34f, verticality);
            return (horizontal * horizontalWeight
                + Vector3.up * NextRange(random, minimumY, maximumY)).normalized;
        }

        private static Quaternion CreateSharedRotation(System.Random random, float asymmetry)
        {
            var baseTilt = Mathf.Lerp(1.5f, 14f, asymmetry);
            return Quaternion.Euler(
                NextRange(random, -baseTilt, baseTilt),
                NextRange(random, 0f, 360f),
                NextRange(random, -baseTilt, baseTilt));
        }

        private static Quaternion CreateRoleRotation(
            System.Random random,
            Quaternion sharedRotation,
            float asymmetry,
            TopDown3DRockWorkbenchMassRole role)
        {
            var roleVariation = role == TopDown3DRockWorkbenchMassRole.Core
                ? 0.45f
                : role == TopDown3DRockWorkbenchMassRole.Support ? 0.75f : 1f;
            var maximumTilt = Mathf.Lerp(2f, 26f, asymmetry) * roleVariation;
            var maximumYaw = Mathf.Lerp(4f, 42f, asymmetry) * roleVariation;
            return sharedRotation * Quaternion.Euler(
                NextRange(random, -maximumTilt, maximumTilt),
                NextRange(random, -maximumYaw, maximumYaw),
                NextRange(random, -maximumTilt, maximumTilt));
        }

        private static float DirectionalRadius(
            Vector3 scale,
            Quaternion rotation,
            Vector3 worldDirection)
        {
            var direction = Quaternion.Inverse(rotation) * worldDirection.normalized;
            var half = scale * 0.5f;
            var radius = float.PositiveInfinity;
            if (Mathf.Abs(direction.x) > 0.0001f)
            {
                radius = Mathf.Min(radius, half.x / Mathf.Abs(direction.x));
            }
            if (Mathf.Abs(direction.y) > 0.0001f)
            {
                radius = Mathf.Min(radius, half.y / Mathf.Abs(direction.y));
            }
            if (Mathf.Abs(direction.z) > 0.0001f)
            {
                radius = Mathf.Min(radius, half.z / Mathf.Abs(direction.z));
            }
            return radius;
        }

        private static Vector3 GetRotatedExtents(Quaternion rotation, Vector3 scale)
        {
            var half = scale * 0.5f;
            var right = rotation * Vector3.right;
            var up = rotation * Vector3.up;
            var forward = rotation * Vector3.forward;
            return new Vector3(
                Mathf.Abs(right.x) * half.x + Mathf.Abs(up.x) * half.y + Mathf.Abs(forward.x) * half.z,
                Mathf.Abs(right.y) * half.x + Mathf.Abs(up.y) * half.y + Mathf.Abs(forward.y) * half.z,
                Mathf.Abs(right.z) * half.x + Mathf.Abs(up.z) * half.y + Mathf.Abs(forward.z) * half.z);
        }

        private static float VariedFraction(
            System.Random random,
            float center,
            float halfRange,
            float amount)
        {
            return Mathf.Max(0.15f, center + NextRange(random, -halfRange, halfRange) * amount);
        }

        private static float NextRange(System.Random random, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }
    }
}
