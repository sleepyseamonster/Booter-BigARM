using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    internal readonly struct TopDown3DRockWorkbenchVolumeSpec
    {
        internal TopDown3DRockWorkbenchVolumeSpec(
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
        }

        internal Vector3 LocalPosition { get; }
        internal Quaternion LocalRotation { get; }
        internal Vector3 LocalScale { get; }
    }

    /// <summary>
    /// Editor-only base-rock planner. A seed produces a repeatable cluster of strongly
    /// overlapping cube volumes; the created child transforms remain ordinary authoring controls.
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

            var firstScale = new Vector3(
                overallSize.x * VariedFraction(random, 0.56f, 0.08f, asymmetry),
                overallSize.y * VariedFraction(
                    random,
                    Mathf.Lerp(0.46f, 0.68f, verticality),
                    0.08f,
                    asymmetry),
                overallSize.z * VariedFraction(random, 0.56f, 0.08f, asymmetry));
            raw.Add(new TopDown3DRockWorkbenchVolumeSpec(
                Vector3.zero,
                CreateRotation(random, asymmetry),
                firstScale));

            for (var index = 1; index < cubeCount; index++)
            {
                var parentLimit = Mathf.Min(index, 3);
                var parentIndex = index <= 2 ? 0 : random.Next(0, parentLimit);
                var parent = raw[parentIndex];
                var childScale = new Vector3(
                    overallSize.x * VariedFraction(random, 0.38f, 0.14f, asymmetry),
                    overallSize.y * VariedFraction(
                        random,
                        Mathf.Lerp(0.34f, 0.52f, verticality),
                        0.13f,
                        asymmetry),
                    overallSize.z * VariedFraction(random, 0.38f, 0.14f, asymmetry));

                var angle = NextRange(random, 0f, Mathf.PI * 2f);
                var direction = new Vector3(
                    Mathf.Cos(angle),
                    NextRange(
                        random,
                        -Mathf.Lerp(0.08f, 0.3f, verticality),
                        Mathf.Lerp(0.12f, 0.85f, verticality)),
                    Mathf.Sin(angle));
                direction += preferredDirection * (asymmetry * 0.35f);
                direction.Normalize();

                var parentRadius = ProjectedRadius(parent.LocalScale, direction);
                var childRadius = ProjectedRadius(childScale, direction);
                var overlapDepth = Mathf.Lerp(0.3f, 0.62f, overlap);
                var centerDistance = (parentRadius + childRadius) * (1f - overlapDepth);
                var childPosition = parent.LocalPosition + direction * centerDistance;
                raw.Add(new TopDown3DRockWorkbenchVolumeSpec(
                    childPosition,
                    CreateRotation(random, asymmetry),
                    childScale));
            }

            return FitPlanToSizeAndGround(raw, overallSize);
        }

        internal static int CreateNewSeed(int currentSeed)
        {
            var seed = Guid.NewGuid().GetHashCode();
            return seed == currentSeed ? unchecked(seed ^ (int)0x9E3779B9) : seed;
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
                    var volumeObject = new GameObject($"Cube Volume {index + 1}");
                    Undo.RegisterCreatedObjectUndo(volumeObject, undoName);
                    Undo.SetTransformParent(volumeObject.transform, authoring.transform, undoName);
                    volumeObject.transform.localPosition = spec.LocalPosition;
                    volumeObject.transform.localRotation = spec.LocalRotation;
                    volumeObject.transform.localScale = spec.LocalScale;
                    Undo.AddComponent<TopDown3DRockVolumeNode>(volumeObject);
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

        private static IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> FitPlanToSizeAndGround(
            IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> raw,
            Vector3 overallSize)
        {
            var first = raw[0];
            var min = first.LocalPosition - first.LocalScale * 0.5f;
            var max = first.LocalPosition + first.LocalScale * 0.5f;
            for (var index = 1; index < raw.Count; index++)
            {
                var spec = raw[index];
                min = Vector3.Min(min, spec.LocalPosition - spec.LocalScale * 0.5f);
                max = Vector3.Max(max, spec.LocalPosition + spec.LocalScale * 0.5f);
            }

            var size = max - min;
            var fit = new Vector3(
                overallSize.x / Mathf.Max(size.x, 0.001f),
                overallSize.y / Mathf.Max(size.y, 0.001f),
                overallSize.z / Mathf.Max(size.z, 0.001f));
            var center = (min + max) * 0.5f;
            var fitted = new List<TopDown3DRockWorkbenchVolumeSpec>(raw.Count);
            var fittedMinimumY = float.PositiveInfinity;
            foreach (var spec in raw)
            {
                var position = Vector3.Scale(spec.LocalPosition - center, fit);
                var scale = Vector3.Scale(spec.LocalScale, fit);
                fittedMinimumY = Mathf.Min(fittedMinimumY, position.y - scale.y * 0.5f);
                fitted.Add(new TopDown3DRockWorkbenchVolumeSpec(
                    position,
                    spec.LocalRotation,
                    scale));
            }

            for (var index = 0; index < fitted.Count; index++)
            {
                var spec = fitted[index];
                fitted[index] = new TopDown3DRockWorkbenchVolumeSpec(
                    spec.LocalPosition + Vector3.up * -fittedMinimumY,
                    spec.LocalRotation,
                    spec.LocalScale);
            }
            return fitted;
        }

        private static Quaternion CreateRotation(System.Random random, float asymmetry)
        {
            var maximumTilt = Mathf.Lerp(4f, 22f, asymmetry);
            return Quaternion.Euler(
                NextRange(random, -maximumTilt, maximumTilt),
                NextRange(random, 0f, 360f),
                NextRange(random, -maximumTilt, maximumTilt));
        }

        private static float ProjectedRadius(Vector3 scale, Vector3 direction)
        {
            return 0.5f * (
                Mathf.Abs(direction.x) * scale.x
                + Mathf.Abs(direction.y) * scale.y
                + Mathf.Abs(direction.z) * scale.z);
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
