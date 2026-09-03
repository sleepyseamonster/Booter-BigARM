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
                TopDown3DRockSourceShape.WeatheredBlock)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
            Role = role;
            SourceShape = sourceShape;
        }

        internal Vector3 LocalPosition { get; }
        internal Quaternion LocalRotation { get; }
        internal Vector3 LocalScale { get; }
        internal TopDown3DRockWorkbenchMassRole Role { get; }
        internal TopDown3DRockSourceShape SourceShape { get; }
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
            float overlap)
        {
            cubeCount = Mathf.Clamp(cubeCount, 2, 10);
            overallSize = new Vector3(
                Mathf.Max(0.5f, overallSize.x),
                Mathf.Max(0.5f, overallSize.y),
                Mathf.Max(0.5f, overallSize.z));
            verticality = Mathf.Clamp01(verticality);
            asymmetry = Mathf.Clamp01(asymmetry);
            overlap = Mathf.Clamp01(overlap);

            var random = new System.Random(seed);
            var raw = new List<TopDown3DRockWorkbenchVolumeSpec>(cubeCount);
            var preferredAngle = NextRange(random, 0f, Mathf.PI * 2f);
            var preferredDirection = new Vector3(
                Mathf.Cos(preferredAngle),
                0f,
                Mathf.Sin(preferredAngle));
            var sharedRotation = CreateSharedRotation(random, asymmetry);

            var firstScale = new Vector3(
                VariedFraction(random, 1.6f, 0.12f, asymmetry),
                VariedFraction(random, Mathf.Lerp(0.72f, 1.65f, verticality), 0.1f, asymmetry),
                VariedFraction(random, 1.45f, 0.12f, asymmetry));
            raw.Add(new TopDown3DRockWorkbenchVolumeSpec(
                Vector3.zero,
                CreateRoleRotation(random, sharedRotation, asymmetry, TopDown3DRockWorkbenchMassRole.Core),
                firstScale,
                TopDown3DRockWorkbenchMassRole.Core));

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
                    verticality,
                    asymmetry,
                    role);
                var parent = raw[parentIndex];
                var childScale = CreateRoleScale(
                    random,
                    firstScale,
                    role,
                    roleIndex,
                    roleCount,
                    verticality,
                    asymmetry);
                var childRotation = CreateRoleRotation(random, sharedRotation, asymmetry, role);
                var direction = CreateGrowthDirection(
                    random,
                    preferredDirection,
                    index,
                    verticality,
                    asymmetry,
                    role);
                var sourceShape = ChooseSourceShape(seed, index, role, asymmetry);
                if (cubeCount >= 3 && index == 1)
                {
                    sourceShape = TopDown3DRockSourceShape.Wedge;
                }
                else if (cubeCount >= 5 && index == supportCount + 1)
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
                    Mathf.SmoothStep(0f, 1f, overlap));
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

            return FitPlanToSizeAndGround(raw, overallSize);
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

                var plan = CreatePlan(
                    seed,
                    authoring.GeneratedCubeCount,
                    authoring.GeneratedOverallSize,
                    authoring.GeneratedVerticality,
                    authoring.GeneratedAsymmetry,
                    authoring.GeneratedOverlap);
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

        internal static Bounds CalculateBounds(IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> plan)
        {
            if (plan == null || plan.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);

            var first = plan[0];
            var firstExtents = GetRotatedExtents(first.LocalRotation, first.LocalScale);
            var min = first.LocalPosition - firstExtents;
            var max = first.LocalPosition + firstExtents;
            for (var index = 1; index < plan.Count; index++)
            {
                var spec = plan[index];
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
                    spec.SourceShape));
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
                    spec.SourceShape);
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
                    spec.SourceShape);
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
            float asymmetry)
        {
            if (role == TopDown3DRockWorkbenchMassRole.Core)
                return TopDown3DRockSourceShape.WeatheredBlock;

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
