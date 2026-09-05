using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator.GroundedGeology
{
    internal readonly struct GroundedGeologyReviewCameraPreset : IEquatable<GroundedGeologyReviewCameraPreset>
    {
        public GroundedGeologyReviewCameraPreset(
            string name,
            float azimuthDegrees,
            float elevationDegrees,
            float distanceMultiplier)
        {
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentException("Camera name is required.", nameof(name)) : name;
            AzimuthDegrees = azimuthDegrees;
            ElevationDegrees = elevationDegrees;
            DistanceMultiplier = distanceMultiplier;
        }

        public string Name { get; }
        public float AzimuthDegrees { get; }
        public float ElevationDegrees { get; }
        public float DistanceMultiplier { get; }

        public bool Equals(GroundedGeologyReviewCameraPreset other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal)
                && AzimuthDegrees.Equals(other.AzimuthDegrees)
                && ElevationDegrees.Equals(other.ElevationDegrees)
                && DistanceMultiplier.Equals(other.DistanceMultiplier);
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyReviewCameraPreset other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(StringComparer.Ordinal.GetHashCode(Name) * 397 ^ AzimuthDegrees.GetHashCode());
        }
    }

    internal static class GroundedGeologyReviewEnvironment
    {
        private static readonly GroundedGeologyReviewCameraPreset[] CameraValues =
        {
            new GroundedGeologyReviewCameraPreset("Gameplay Baseline", 315f, 34f, 2.4f),
            new GroundedGeologyReviewCameraPreset("North", 0f, 30f, 2.4f),
            new GroundedGeologyReviewCameraPreset("North-East", 45f, 30f, 2.4f),
            new GroundedGeologyReviewCameraPreset("East", 90f, 30f, 2.4f),
            new GroundedGeologyReviewCameraPreset("South-East", 135f, 30f, 2.4f),
            new GroundedGeologyReviewCameraPreset("South", 180f, 30f, 2.4f),
            new GroundedGeologyReviewCameraPreset("South-West", 225f, 30f, 2.4f),
            new GroundedGeologyReviewCameraPreset("West", 270f, 30f, 2.4f),
            new GroundedGeologyReviewCameraPreset("North-West", 315f, 30f, 2.4f)
        };

        public static IReadOnlyList<GroundedGeologyReviewCameraPreset> Cameras => Array.AsReadOnly(CameraValues);
        public static Vector3 LockedLightEuler => new Vector3(42f, -34f, 0f);
        public static float LockedLightIntensity => 1f;
    }

    internal sealed class GroundedGeologyWorkbenchSeedState
    {
        public GroundedGeologyWorkbenchSeedState(int seed, bool locked)
        {
            Seed = seed;
            Locked = locked;
        }

        public int Seed { get; private set; }
        public bool Locked { get; set; }

        public bool TryAdvance()
        {
            if (Locked) return false;
            Seed = unchecked(Seed * 1103515245 + 12345);
            return true;
        }
    }

    internal sealed class GroundedGeologyTemporaryPreviewMarker : MonoBehaviour
    {
        [SerializeField] private List<Mesh> ownedMeshes = new List<Mesh>();

        public IReadOnlyList<Mesh> OwnedMeshes => ownedMeshes;

        public void Track(Mesh mesh)
        {
            if (mesh != null && !ownedMeshes.Contains(mesh)) ownedMeshes.Add(mesh);
        }

        public void DestroyOwnedMeshesImmediately()
        {
            for (var i = 0; i < ownedMeshes.Count; i++)
            {
                var mesh = ownedMeshes[i];
                if (mesh != null && !AssetDatabase.Contains(mesh))
                    UnityEngine.Object.DestroyImmediate(mesh);
            }
            ownedMeshes.Clear();
        }
    }

    internal readonly struct GroundedGeologyGeometryMetrics
    {
        public GroundedGeologyGeometryMetrics(
            Bounds renderBounds,
            Bounds colliderBounds,
            Vector3[] renderLandmarks,
            Vector3[] colliderLandmarks,
            WorldFeatureId sampleSignature)
        {
            RenderBounds = renderBounds;
            ColliderBounds = colliderBounds;
            RenderLandmarks = Array.AsReadOnly(renderLandmarks ?? Array.Empty<Vector3>());
            ColliderLandmarks = Array.AsReadOnly(colliderLandmarks ?? Array.Empty<Vector3>());
            SampleSignature = sampleSignature;
        }

        public Bounds RenderBounds { get; }
        public Bounds ColliderBounds { get; }
        public IReadOnlyList<Vector3> RenderLandmarks { get; }
        public IReadOnlyList<Vector3> ColliderLandmarks { get; }
        public WorldFeatureId SampleSignature { get; }
    }

    internal readonly struct GroundedGeologyABMetrics
    {
        public GroundedGeologyABMetrics(
            GroundedGeologyGeometryMetrics referenceA,
            GroundedGeologyGeometryMetrics targetB,
            float maximumRenderSampleDelta,
            float maximumColliderSampleDelta)
        {
            ReferenceA = referenceA;
            TargetB = targetB;
            MaximumRenderSampleDelta = maximumRenderSampleDelta;
            MaximumColliderSampleDelta = maximumColliderSampleDelta;
        }

        public GroundedGeologyGeometryMetrics ReferenceA { get; }
        public GroundedGeologyGeometryMetrics TargetB { get; }
        public float MaximumRenderSampleDelta { get; }
        public float MaximumColliderSampleDelta { get; }

        public bool IsEquivalent(float toleranceMeters)
        {
            return MaximumRenderSampleDelta <= toleranceMeters
                && MaximumColliderSampleDelta <= toleranceMeters
                && Vector3.Distance(ReferenceA.RenderBounds.size, TargetB.RenderBounds.size) <= toleranceMeters
                && Vector3.Distance(ReferenceA.ColliderBounds.size, TargetB.ColliderBounds.size) <= toleranceMeters;
        }
    }

    internal sealed class GroundedGeologyComparisonPreview
    {
        public GroundedGeologyComparisonPreview(
            GameObject root,
            GameObject referenceA,
            GameObject targetB,
            GroundedGeologyABMetrics metrics)
        {
            Root = root;
            ReferenceA = referenceA;
            TargetB = targetB;
            Metrics = metrics;
        }

        public GameObject Root { get; }
        public GameObject ReferenceA { get; }
        public GameObject TargetB { get; }
        public GroundedGeologyABMetrics Metrics { get; }
        public bool IsAlive => Root != null && ReferenceA != null && TargetB != null;
    }

    /// <summary>
    /// Builds non-saving A/B scene representations. A carries the fixture's reference root scale;
    /// B has a unit root with that factor baked into mesh vertices and spatial child positions.
    /// </summary>
    internal static class GroundedGeologyComparisonBuilder
    {
        internal const string TemporaryRootName = "Grounded Geology Preview (Temporary - Not Saved)";

        public static bool TryBuild(
            GameObject source,
            float physicalBakeFactor,
            GameObject previousRoot,
            out GroundedGeologyComparisonPreview preview,
            out string error)
        {
            preview = null;
            error = null;
            if (source == null)
            {
                error = "A source fixture is required.";
                return false;
            }
            if (!(physicalBakeFactor > 0f) || float.IsNaN(physicalBakeFactor) || float.IsInfinity(physicalBakeFactor))
            {
                error = "Physical bake factor must be finite and positive.";
                return false;
            }

            var sourceFilters = source.GetComponentsInChildren<MeshFilter>(true);
            var hasGeneratedMesh = false;
            for (var i = 0; i < sourceFilters.Length; i++)
            {
                if (sourceFilters[i].sharedMesh == null) continue;
                hasGeneratedMesh = true;
                break;
            }
            if (!hasGeneratedMesh)
            {
                error = "The fixture has no generated mesh to compare. Rebuild its existing workbench preview first.";
                return false;
            }

            var newRoot = new GameObject(TemporaryRootName)
            {
                hideFlags = HideFlags.DontSaveInEditor
            };
            var marker = newRoot.AddComponent<GroundedGeologyTemporaryPreviewMarker>();
            try
            {
                var referenceA = UnityEngine.Object.Instantiate(source, newRoot.transform, false);
                referenceA.name = "A - Original Rock (Reference)";
                PrepareClone(referenceA);
                referenceA.transform.localPosition = Vector3.zero;
                referenceA.transform.localRotation = Quaternion.identity;
                referenceA.transform.localScale = Vector3.one * physicalBakeFactor;

                var targetB = UnityEngine.Object.Instantiate(source, newRoot.transform, false);
                targetB.name = "B - Grounded Geology (Unit Scale)";
                PrepareClone(targetB);
                targetB.transform.localPosition = Vector3.zero;
                targetB.transform.localRotation = Quaternion.identity;
                targetB.transform.localScale = Vector3.one;
                BakeSpatialGeometry(targetB, physicalBakeFactor, marker);

                var referenceMetrics = Measure(referenceA);
                var targetMetrics = Measure(targetB);
                var metrics = new GroundedGeologyABMetrics(
                    referenceMetrics,
                    targetMetrics,
                    MaximumDelta(referenceMetrics.RenderLandmarks, targetMetrics.RenderLandmarks),
                    MaximumDelta(referenceMetrics.ColliderLandmarks, targetMetrics.ColliderLandmarks));
                if (!metrics.IsEquivalent(0.00001f))
                {
                    error = "The temporary B representation did not match reference A within the meter-space tolerance.";
                    marker.DestroyOwnedMeshesImmediately();
                    UnityEngine.Object.DestroyImmediate(newRoot);
                    return false;
                }

                var separation = Mathf.Max(0.5f, referenceMetrics.RenderBounds.size.x * 1.35f);
                referenceA.transform.localPosition = Vector3.left * separation * 0.5f;
                targetB.transform.localPosition = Vector3.right * separation * 0.5f;
                SetHideFlagsRecursively(newRoot);
                Undo.IncrementCurrentGroup();
                var undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Create Grounded Geology A/B Comparison");
                for (var i = 0; i < marker.OwnedMeshes.Count; i++)
                    Undo.RegisterCreatedObjectUndo(marker.OwnedMeshes[i], "Create Grounded Geology A/B Comparison");
                Undo.RegisterCreatedObjectUndo(newRoot, "Create Grounded Geology A/B Comparison");
                if (previousRoot != null)
                {
                    RestoreSelectionBeforeDestroy(previousRoot, source);
                    DestroyWithUndo(previousRoot);
                }
                Undo.CollapseUndoOperations(undoGroup);
                preview = new GroundedGeologyComparisonPreview(newRoot, referenceA, targetB, metrics);
                return true;
            }
            catch (Exception exception)
            {
                if (newRoot != null)
                {
                    marker.DestroyOwnedMeshesImmediately();
                    UnityEngine.Object.DestroyImmediate(newRoot);
                }
                error = "Could not build the temporary comparison: " + exception.Message;
                return false;
            }
        }

        public static void Cancel(GameObject root, GameObject selectionFallback = null)
        {
            if (root != null && IsTemporary(root))
            {
                RestoreSelectionBeforeDestroy(root, selectionFallback);
                Undo.IncrementCurrentGroup();
                var undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Cancel Grounded Geology A/B Comparison");
                DestroyWithUndo(root);
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static void RestoreSelectionBeforeDestroy(GameObject root, GameObject fallback)
        {
            var selected = Selection.activeGameObject;
            if (selected == null || (selected != root && !selected.transform.IsChildOf(root.transform))) return;
            Selection.activeGameObject = fallback;
        }

        public static bool IsTemporary(GameObject root)
        {
            return root != null
                && string.Equals(root.name, TemporaryRootName, StringComparison.Ordinal)
                && root.GetComponent<GroundedGeologyTemporaryPreviewMarker>() != null
                && (root.hideFlags & HideFlags.DontSaveInEditor) != 0;
        }

        public static GroundedGeologyGeometryMetrics Measure(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var renderPoints = CaptureMeshLandmarks(root, false);
            var colliderPoints = CaptureMeshLandmarks(root, true);
            var renderBounds = CalculateBounds(renderPoints);
            var colliderBounds = colliderPoints.Count > 0
                ? CalculateBounds(colliderPoints)
                : renderBounds;
            var hash = new WorldStableHashBuilder("grounded-geology-comparison-samples-v1");
            AppendLandmarks(renderPoints, ref hash);
            AppendLandmarks(colliderPoints, ref hash);
            hash.Finish128(out var high, out var low);
            return new GroundedGeologyGeometryMetrics(
                renderBounds,
                colliderBounds,
                renderPoints.ToArray(),
                colliderPoints.ToArray(),
                new WorldFeatureId(high, low));
        }

        private static void PrepareClone(GameObject clone)
        {
            foreach (var formation in clone.GetComponentsInChildren<TopDown3DRockWorkbenchFormationAuthoring>(true))
                UnityEngine.Object.DestroyImmediate(formation);
            foreach (var workbench in clone.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true))
                UnityEngine.Object.DestroyImmediate(workbench);
            foreach (var node in clone.GetComponentsInChildren<TopDown3DRockVolumeNode>(true))
                UnityEngine.Object.DestroyImmediate(node);
        }

        private static void BakeSpatialGeometry(
            GameObject target,
            float factor,
            GroundedGeologyTemporaryPreviewMarker marker)
        {
            var transforms = target.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != target.transform)
                    transforms[i].localPosition *= factor;
            }

            var filters = target.GetComponentsInChildren<MeshFilter>(true);
            var replacementBySource = new Dictionary<Mesh, Mesh>();
            for (var i = 0; i < filters.Length; i++)
            {
                var sourceMesh = filters[i].sharedMesh;
                if (sourceMesh == null) continue;
                if (!replacementBySource.TryGetValue(sourceMesh, out var replacement))
                {
                    replacement = UnityEngine.Object.Instantiate(sourceMesh);
                    replacement.name = sourceMesh.name + " (Temporary Meter Bake)";
                    replacement.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
                    var vertices = replacement.vertices;
                    for (var vertex = 0; vertex < vertices.Length; vertex++) vertices[vertex] *= factor;
                    replacement.vertices = vertices;
                    replacement.RecalculateBounds();
                    replacementBySource.Add(sourceMesh, replacement);
                    marker.Track(replacement);
                }
                filters[i].sharedMesh = replacement;
            }

            var colliders = target.GetComponentsInChildren<MeshCollider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var sourceMesh = colliders[i].sharedMesh;
                if (sourceMesh != null && replacementBySource.TryGetValue(sourceMesh, out var replacement))
                    colliders[i].sharedMesh = replacement;
            }
        }

        private static List<Vector3> CaptureMeshLandmarks(GameObject root, bool colliders)
        {
            var points = new List<Vector3>();
            if (colliders)
            {
                var items = root.GetComponentsInChildren<MeshCollider>(true);
                Array.Sort(items, (left, right) => string.CompareOrdinal(
                    GetRelativePath(root.transform, left.transform),
                    GetRelativePath(root.transform, right.transform)));
                for (var i = 0; i < items.Length; i++)
                    AppendMeshVertices(root.transform, items[i].transform, items[i].sharedMesh, points);
            }
            else
            {
                var items = root.GetComponentsInChildren<MeshFilter>(true);
                Array.Sort(items, (left, right) => string.CompareOrdinal(
                    GetRelativePath(root.transform, left.transform),
                    GetRelativePath(root.transform, right.transform)));
                for (var i = 0; i < items.Length; i++)
                    AppendMeshVertices(root.transform, items[i].transform, items[i].sharedMesh, points);
            }
            return points;
        }

        private static void AppendMeshVertices(
            Transform root,
            Transform item,
            Mesh mesh,
            ICollection<Vector3> output)
        {
            if (mesh == null) return;
            var vertices = mesh.vertices;
            var inverseRootRotation = Quaternion.Inverse(root.rotation);
            for (var i = 0; i < vertices.Length; i++)
            {
                var world = item.TransformPoint(vertices[i]);
                output.Add(inverseRootRotation * (world - root.position));
            }
        }

        private static Bounds CalculateBounds(IReadOnlyList<Vector3> points)
        {
            if (points.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);
            var bounds = new Bounds(points[0], Vector3.zero);
            for (var i = 1; i < points.Count; i++) bounds.Encapsulate(points[i]);
            return bounds;
        }

        private static float MaximumDelta(IReadOnlyList<Vector3> first, IReadOnlyList<Vector3> second)
        {
            if (first.Count != second.Count) return float.PositiveInfinity;
            var maximum = 0f;
            for (var i = 0; i < first.Count; i++)
                maximum = Mathf.Max(maximum, Vector3.Distance(first[i], second[i]));
            return maximum;
        }

        private static void AppendLandmarks(
            IReadOnlyList<Vector3> points,
            ref WorldStableHashBuilder hash)
        {
            hash.Append(points.Count);
            for (var i = 0; i < points.Count; i++)
            {
                hash.Append(BitConverter.SingleToInt32Bits(points[i].x == 0f ? 0f : points[i].x));
                hash.Append(BitConverter.SingleToInt32Bits(points[i].y == 0f ? 0f : points[i].y));
                hash.Append(BitConverter.SingleToInt32Bits(points[i].z == 0f ? 0f : points[i].z));
            }
        }

        private static string GetRelativePath(Transform root, Transform item)
        {
            if (item == root) return string.Empty;
            var names = new Stack<string>();
            var current = item;
            while (current != null && current != root)
            {
                names.Push(current.name + "[" + current.GetSiblingIndex() + "]");
                current = current.parent;
            }
            return string.Join("/", names.ToArray());
        }

        private static void SetHideFlagsRecursively(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                transform.gameObject.hideFlags |= HideFlags.DontSaveInEditor;
        }

        private static void DestroyWithUndo(GameObject root)
        {
            var marker = root.GetComponent<GroundedGeologyTemporaryPreviewMarker>();
            if (marker != null)
            {
                var meshes = new Mesh[marker.OwnedMeshes.Count];
                for (var i = 0; i < meshes.Length; i++) meshes[i] = marker.OwnedMeshes[i];
                for (var i = 0; i < meshes.Length; i++)
                {
                    if (meshes[i] != null && !AssetDatabase.Contains(meshes[i]))
                        Undo.DestroyObjectImmediate(meshes[i]);
                }
            }
            Undo.DestroyObjectImmediate(root);
        }
    }
}
