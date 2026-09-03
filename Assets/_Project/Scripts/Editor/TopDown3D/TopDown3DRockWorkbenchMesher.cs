using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    internal readonly struct TopDown3DRockWorkbenchBox
    {
        private readonly Matrix4x4 rootToBox;
        private readonly float distanceScale;
        private readonly float roundRadius;
        private readonly float taperX;
        private readonly float taperZ;
        private readonly Vector4 cornerCut0;
        private readonly Vector4 cornerCut1;
        private readonly Vector4 cornerCut2;
        private readonly Vector4 cornerCut3;

        internal TopDown3DRockWorkbenchBox(Transform root, Transform box)
            : this(root, box, 0)
        {
        }

        internal TopDown3DRockWorkbenchBox(Transform root, Transform box, int shapeSeed)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (box == null) throw new ArgumentNullException(nameof(box));

            var boxToRoot = root.worldToLocalMatrix * box.localToWorldMatrix;
            rootToBox = boxToRoot.inverse;
            distanceScale = Mathf.Max(
                0.0001f,
                Mathf.Min(
                    boxToRoot.MultiplyVector(Vector3.right).magnitude,
                    Mathf.Min(
                        boxToRoot.MultiplyVector(Vector3.up).magnitude,
                        boxToRoot.MultiplyVector(Vector3.forward).magnitude)));

            var firstCorner = boxToRoot.MultiplyPoint3x4(new Vector3(-0.5f, -0.5f, -0.5f));
            var bounds = new Bounds(firstCorner, Vector3.zero);
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        bounds.Encapsulate(boxToRoot.MultiplyPoint3x4(
                            new Vector3(x * 0.5f, y * 0.5f, z * 0.5f)));
                    }
                }
            }

            Bounds = bounds;

            var randomState = unchecked((uint)shapeSeed) ^ 0xA511E9B3u;
            roundRadius = Mathf.Lerp(0.07f, 0.11f, Next01(ref randomState));
            taperX = Mathf.Lerp(0.035f, 0.09f, Next01(ref randomState));
            taperZ = Mathf.Lerp(0.035f, 0.09f, Next01(ref randomState));

            var startingCorner = (int)(NextUInt(ref randomState) & 7u);
            var cornerStride = ((int)(NextUInt(ref randomState) & 3u) * 2) + 1;
            cornerCut0 = CreateCornerCut(ref randomState, startingCorner);
            cornerCut1 = CreateCornerCut(ref randomState, (startingCorner + cornerStride) & 7);
            cornerCut2 = CreateCornerCut(ref randomState, (startingCorner + cornerStride * 2) & 7);
            cornerCut3 = CreateCornerCut(ref randomState, (startingCorner + cornerStride * 3) & 7);
        }

        internal Bounds Bounds { get; }

        internal float Evaluate(Vector3 rootLocalPoint)
        {
            var localPoint = rootToBox.MultiplyPoint3x4(rootLocalPoint);
            var height = Mathf.Clamp01(localPoint.y + 0.5f);
            var upperTaper = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 1f, height));
            var halfExtents = new Vector3(
                0.5f - taperX * upperTaper,
                0.5f,
                0.5f - taperZ * upperTaper);
            var innerHalfExtents = halfExtents - Vector3.one * roundRadius;
            var q = new Vector3(
                Mathf.Abs(localPoint.x) - innerHalfExtents.x,
                Mathf.Abs(localPoint.y) - innerHalfExtents.y,
                Mathf.Abs(localPoint.z) - innerHalfExtents.z);
            var outside = new Vector3(
                Mathf.Max(q.x, 0f),
                Mathf.Max(q.y, 0f),
                Mathf.Max(q.z, 0f));
            var inside = Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f);
            var roundedDistance = outside.magnitude + inside - roundRadius;
            var rockDistance = Mathf.Max(roundedDistance, EvaluateCut(cornerCut0, localPoint));
            rockDistance = Mathf.Max(rockDistance, EvaluateCut(cornerCut1, localPoint));
            rockDistance = Mathf.Max(rockDistance, EvaluateCut(cornerCut2, localPoint));
            rockDistance = Mathf.Max(rockDistance, EvaluateCut(cornerCut3, localPoint));
            return rockDistance * distanceScale;
        }

        private static Vector4 CreateCornerCut(ref uint randomState, int corner)
        {
            var normal = new Vector3(
                ((corner & 1) == 0 ? -1f : 1f) * Mathf.Lerp(0.72f, 1f, Next01(ref randomState)),
                ((corner & 2) == 0 ? -1f : 1f) * Mathf.Lerp(0.72f, 1f, Next01(ref randomState)),
                ((corner & 4) == 0 ? -1f : 1f) * Mathf.Lerp(0.72f, 1f, Next01(ref randomState)));
            normal.Normalize();
            return new Vector4(
                normal.x,
                normal.y,
                normal.z,
                Mathf.Lerp(0.56f, 0.66f, Next01(ref randomState)));
        }

        private static float EvaluateCut(Vector4 cut, Vector3 point)
        {
            return cut.x * point.x + cut.y * point.y + cut.z * point.z - cut.w;
        }

        private static uint NextUInt(ref uint state)
        {
            state += 0x9E3779B9u;
            var value = state;
            value = (value ^ (value >> 16)) * 0x7FEB352Du;
            value = (value ^ (value >> 15)) * 0x846CA68Bu;
            return value ^ (value >> 16);
        }

        private static float Next01(ref uint state)
        {
            return (NextUInt(ref state) & 0x00FFFFFFu) / 16777215f;
        }
    }

    /// <summary>
    /// Preserves the authoring identity of one completed Rock Workbench while several
    /// workbenches are sampled into a single formation surface.
    /// </summary>
    internal sealed class TopDown3DRockWorkbenchFieldGroup
    {
        internal TopDown3DRockWorkbenchFieldGroup(
            int id,
            IReadOnlyList<TopDown3DRockWorkbenchBox> boxes,
            float smoothness)
        {
            if (boxes == null) throw new ArgumentNullException(nameof(boxes));
            if (boxes.Count == 0) throw new ArgumentException(
                "A rock field group needs at least one source volume.",
                nameof(boxes));

            Id = id;
            var copiedBoxes = new TopDown3DRockWorkbenchBox[boxes.Count];
            for (var index = 0; index < boxes.Count; index++) copiedBoxes[index] = boxes[index];
            Boxes = copiedBoxes;
            Smoothness = Mathf.Max(0f, smoothness);
        }

        internal int Id { get; }
        internal IReadOnlyList<TopDown3DRockWorkbenchBox> Boxes { get; }
        internal float Smoothness { get; }
    }

    internal sealed class TopDown3DRockWorkbenchBuildResult
    {
        internal TopDown3DRockWorkbenchBuildResult(
            TopDown3DIndexedMeshData meshData,
            TopDown3DMeshTopologyReport topology,
            int connectedComponents,
            Vector3Int gridCells,
            float effectiveVoxelSize,
            Color32[] vertexColors = null)
        {
            MeshData = meshData;
            Topology = topology;
            ConnectedComponents = connectedComponents;
            GridCells = gridCells;
            EffectiveVoxelSize = effectiveVoxelSize;
            VertexColors = vertexColors;
        }

        internal TopDown3DIndexedMeshData MeshData { get; }
        internal TopDown3DMeshTopologyReport Topology { get; }
        internal int ConnectedComponents { get; }
        internal Vector3Int GridCells { get; }
        internal float EffectiveVoxelSize { get; }
        internal Color32[] VertexColors { get; }

        internal Mesh CreateMesh(string name)
        {
            var mesh = new Mesh
            {
                name = name,
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset,
                indexFormat = MeshData.Vertices.Length > ushort.MaxValue
                    ? IndexFormat.UInt32
                    : IndexFormat.UInt16
            };
            mesh.vertices = MeshData.Vertices;
            mesh.triangles = MeshData.Triangles;
            if (VertexColors != null && VertexColors.Length == MeshData.Vertices.Length)
                mesh.colors32 = VertexColors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>
    /// Replaceable editor-preview mesher. Marching tetrahedra keeps this first workbench
    /// fully project-owned C# while presenting a stable scalar-field input seam.
    /// </summary>
    internal static class TopDown3DRockWorkbenchMesher
    {
        private const int MaximumCellsPerAxis = 96;
        private static readonly int[,] Tetrahedra =
        {
            { 0, 5, 1, 6 },
            { 0, 1, 2, 6 },
            { 0, 2, 3, 6 },
            { 0, 3, 7, 6 },
            { 0, 7, 4, 6 },
            { 0, 4, 5, 6 }
        };

        private static readonly Vector3Int[] CubeCorners =
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 1),
            new Vector3Int(1, 1, 1),
            new Vector3Int(0, 1, 1)
        };

        internal static bool TryBuild(
            IReadOnlyList<TopDown3DRockWorkbenchBox> boxes,
            float requestedVoxelSize,
            float smoothness,
            out TopDown3DRockWorkbenchBuildResult result,
            out string error)
        {
            return TryBuildInternal(
                boxes,
                requestedVoxelSize,
                Mathf.Max(0f, smoothness),
                point => Evaluate(boxes, point, smoothness),
                null,
                0f,
                out result,
                out error);
        }

        internal static bool TryBuildGrouped(
            IReadOnlyList<TopDown3DRockWorkbenchFieldGroup> groups,
            float requestedVoxelSize,
            float interGroupSmoothness,
            float seamWidth,
            out TopDown3DRockWorkbenchBuildResult result,
            out string error)
        {
            result = null;
            error = null;
            if (groups == null || groups.Count == 0)
            {
                error = "Add at least one completed rock group.";
                return false;
            }

            var boxes = new List<TopDown3DRockWorkbenchBox>();
            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var group = groups[groupIndex];
                if (group == null || group.Boxes == null || group.Boxes.Count == 0)
                {
                    error = $"Rock group {groupIndex + 1} has no enabled cube volumes.";
                    return false;
                }

                for (var boxIndex = 0; boxIndex < group.Boxes.Count; boxIndex++)
                    boxes.Add(group.Boxes[boxIndex]);
            }

            interGroupSmoothness = Mathf.Max(0f, interGroupSmoothness);
            seamWidth = Mathf.Max(0.001f, seamWidth);
            var paddingSmoothness = interGroupSmoothness;
            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                paddingSmoothness = Mathf.Max(paddingSmoothness, groups[groupIndex].Smoothness);
            return TryBuildInternal(
                boxes,
                requestedVoxelSize,
                paddingSmoothness,
                point => EvaluateGroups(groups, point, interGroupSmoothness),
                groups,
                seamWidth,
                out result,
                out error);
        }

        private static bool TryBuildInternal(
            IReadOnlyList<TopDown3DRockWorkbenchBox> boxes,
            float requestedVoxelSize,
            float paddingSmoothness,
            Func<Vector3, float> evaluateField,
            IReadOnlyList<TopDown3DRockWorkbenchFieldGroup> seamGroups,
            float seamWidth,
            out TopDown3DRockWorkbenchBuildResult result,
            out string error)
        {
            result = null;
            error = null;
            if (boxes == null || boxes.Count == 0)
            {
                error = "Add at least one enabled cube volume.";
                return false;
            }

            var bounds = boxes[0].Bounds;
            for (var i = 1; i < boxes.Count; i++) bounds.Encapsulate(boxes[i].Bounds);

            requestedVoxelSize = Mathf.Max(0.04f, requestedVoxelSize);
            paddingSmoothness = Mathf.Max(0f, paddingSmoothness);
            var padding = Mathf.Max(
                requestedVoxelSize * 1.75f,
                paddingSmoothness + requestedVoxelSize * 1.25f);
            bounds.Expand(padding * 2f);

            var largestDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            var voxelSize = Mathf.Max(requestedVoxelSize, largestDimension / MaximumCellsPerAxis);
            var cells = new Vector3Int(
                Mathf.Max(2, Mathf.CeilToInt(bounds.size.x / voxelSize)),
                Mathf.Max(2, Mathf.CeilToInt(bounds.size.y / voxelSize)),
                Mathf.Max(2, Mathf.CeilToInt(bounds.size.z / voxelSize)));
            var points = cells + Vector3Int.one;
            var pointCount = points.x * points.y * points.z;
            if (pointCount <= 0 || pointCount > 1000000)
            {
                error = $"The preview grid would require {pointCount:N0} samples. Increase Voxel Size or use a smaller arrangement.";
                return false;
            }

            var step = new Vector3(
                bounds.size.x / cells.x,
                bounds.size.y / cells.y,
                bounds.size.z / cells.z);
            var fieldValues = new float[pointCount];
            var zeroNudge = voxelSize * 0.00001f;
            for (var z = 0; z < points.z; z++)
            {
                for (var y = 0; y < points.y; y++)
                {
                    for (var x = 0; x < points.x; x++)
                    {
                        var position = bounds.min + Vector3.Scale(new Vector3(x, y, z), step);
                        var value = evaluateField(position);
                        if (Mathf.Abs(value) < zeroNudge) value = zeroNudge;
                        fieldValues[PointIndex(x, y, z, points)] = value;
                    }
                }
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var vertexByEdge = new Dictionary<GridEdge, int>();
            var cornerIds = new int[8];
            var cornerPositions = new Vector3[8];
            var cornerValues = new float[8];

            for (var z = 0; z < cells.z; z++)
            {
                for (var y = 0; y < cells.y; y++)
                {
                    for (var x = 0; x < cells.x; x++)
                    {
                        for (var corner = 0; corner < 8; corner++)
                        {
                            var coordinate = new Vector3Int(x, y, z) + CubeCorners[corner];
                            var id = PointIndex(coordinate.x, coordinate.y, coordinate.z, points);
                            cornerIds[corner] = id;
                            cornerPositions[corner] = bounds.min + Vector3.Scale((Vector3)coordinate, step);
                            cornerValues[corner] = fieldValues[id];
                        }

                        for (var tetrahedron = 0; tetrahedron < 6; tetrahedron++)
                        {
                            PolygonizeTetrahedron(
                                Tetrahedra[tetrahedron, 0],
                                Tetrahedra[tetrahedron, 1],
                                Tetrahedra[tetrahedron, 2],
                                Tetrahedra[tetrahedron, 3],
                                cornerIds,
                                cornerPositions,
                                cornerValues,
                                vertexByEdge,
                                vertices,
                                triangles);
                        }
                    }
                }
            }

            if (triangles.Count == 0)
            {
                error = "The scalar field did not produce a surface. Try a smaller Voxel Size.";
                return false;
            }

            if (!TryOrientConsistently(vertices, triangles, out error))
            {
                error = $"The preview surface could not be oriented consistently: {error}";
                return false;
            }

            var meshData = TopDown3DRockMeshTopology.Normalize(vertices, triangles);
            var topology = TopDown3DRockMeshTopology.Validate(meshData);
            if (!topology.IsValid)
            {
                error = $"The preview mesher produced invalid topology: {topology.Error}";
                return false;
            }

            result = new TopDown3DRockWorkbenchBuildResult(
                meshData,
                topology,
                CountConnectedComponents(meshData),
                cells,
                voxelSize,
                seamGroups == null
                    ? null
                    : BuildGeologicalSeamColors(meshData.Vertices, seamGroups, seamWidth));
            return true;
        }

        private static void PolygonizeTetrahedron(
            int a,
            int b,
            int c,
            int d,
            IReadOnlyList<int> cornerIds,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<float> values,
            IDictionary<GridEdge, int> vertexByEdge,
            IList<Vector3> vertices,
            IList<int> triangles)
        {
            var inside0 = -1;
            var inside1 = -1;
            var inside2 = -1;
            var inside3 = -1;
            var outside0 = -1;
            var outside1 = -1;
            var outside2 = -1;
            var outside3 = -1;
            var insideCount = 0;
            var outsideCount = 0;
            ClassifyCorner(a, values[a] < 0f, ref insideCount, ref inside0, ref inside1, ref inside2, ref inside3, ref outsideCount, ref outside0, ref outside1, ref outside2, ref outside3);
            ClassifyCorner(b, values[b] < 0f, ref insideCount, ref inside0, ref inside1, ref inside2, ref inside3, ref outsideCount, ref outside0, ref outside1, ref outside2, ref outside3);
            ClassifyCorner(c, values[c] < 0f, ref insideCount, ref inside0, ref inside1, ref inside2, ref inside3, ref outsideCount, ref outside0, ref outside1, ref outside2, ref outside3);
            ClassifyCorner(d, values[d] < 0f, ref insideCount, ref inside0, ref inside1, ref inside2, ref inside3, ref outsideCount, ref outside0, ref outside1, ref outside2, ref outside3);

            if (insideCount == 0 || insideCount == 4) return;

            if (insideCount == 1 || insideCount == 3)
            {
                var isolated = insideCount == 1 ? inside0 : outside0;
                var other0 = insideCount == 1 ? outside0 : inside0;
                var other1 = insideCount == 1 ? outside1 : inside1;
                var other2 = insideCount == 1 ? outside2 : inside2;
                var first = GetEdgeVertex(isolated, other0, cornerIds, positions, values, vertexByEdge, vertices);
                var second = GetEdgeVertex(isolated, other1, cornerIds, positions, values, vertexByEdge, vertices);
                var third = GetEdgeVertex(isolated, other2, cornerIds, positions, values, vertexByEdge, vertices);
                AddTriangle(first, second, third, vertices, triangles);
                return;
            }

            var ac = GetEdgeVertex(inside0, outside0, cornerIds, positions, values, vertexByEdge, vertices);
            var ad = GetEdgeVertex(inside0, outside1, cornerIds, positions, values, vertexByEdge, vertices);
            var bc = GetEdgeVertex(inside1, outside0, cornerIds, positions, values, vertexByEdge, vertices);
            var bd = GetEdgeVertex(inside1, outside1, cornerIds, positions, values, vertexByEdge, vertices);
            AddTriangle(ac, ad, bd, vertices, triangles);
            AddTriangle(ac, bd, bc, vertices, triangles);
        }

        private static void ClassifyCorner(
            int corner,
            bool isInside,
            ref int insideCount,
            ref int inside0,
            ref int inside1,
            ref int inside2,
            ref int inside3,
            ref int outsideCount,
            ref int outside0,
            ref int outside1,
            ref int outside2,
            ref int outside3)
        {
            if (isInside)
            {
                switch (insideCount++)
                {
                    case 0: inside0 = corner; break;
                    case 1: inside1 = corner; break;
                    case 2: inside2 = corner; break;
                    default: inside3 = corner; break;
                }
                return;
            }

            switch (outsideCount++)
            {
                case 0: outside0 = corner; break;
                case 1: outside1 = corner; break;
                case 2: outside2 = corner; break;
                default: outside3 = corner; break;
            }
        }

        private static int GetEdgeVertex(
            int firstCorner,
            int secondCorner,
            IReadOnlyList<int> cornerIds,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<float> values,
            IDictionary<GridEdge, int> vertexByEdge,
            IList<Vector3> vertices)
        {
            var edge = new GridEdge(cornerIds[firstCorner], cornerIds[secondCorner]);
            if (vertexByEdge.TryGetValue(edge, out var existing)) return existing;

            var firstValue = values[firstCorner];
            var secondValue = values[secondCorner];
            var denominator = firstValue - secondValue;
            var t = Mathf.Abs(denominator) < 0.0000001f
                ? 0.5f
                : Mathf.Clamp01(firstValue / denominator);
            var vertex = Vector3.LerpUnclamped(positions[firstCorner], positions[secondCorner], t);
            var index = vertices.Count;
            vertices.Add(vertex);
            vertexByEdge.Add(edge, index);
            return index;
        }

        private static void AddTriangle(
            int a,
            int b,
            int c,
            IList<Vector3> vertices,
            IList<int> triangles)
        {
            if (a == b || b == c || c == a) return;
            var first = vertices[a];
            var second = vertices[b];
            var third = vertices[c];
            var normal = Vector3.Cross(second - first, third - first);
            // Very narrow but non-zero triangles are still required to close the surface.
            // The shared topology validator rejects only true degeneracy in double precision.
            if (normal.sqrMagnitude <= 0f) return;

            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        private static float Evaluate(
            IReadOnlyList<TopDown3DRockWorkbenchBox> boxes,
            Vector3 point,
            float smoothness)
        {
            var distance = boxes[0].Evaluate(point);
            for (var i = 1; i < boxes.Count; i++)
            {
                var next = boxes[i].Evaluate(point);
                distance = SmoothMinimum(distance, next, smoothness);
            }

            return distance;
        }

        private static float EvaluateGroups(
            IReadOnlyList<TopDown3DRockWorkbenchFieldGroup> groups,
            Vector3 point,
            float interGroupSmoothness)
        {
            var distance = EvaluateGroup(groups[0], point);
            for (var groupIndex = 1; groupIndex < groups.Count; groupIndex++)
            {
                distance = SmoothMinimum(
                    distance,
                    EvaluateGroup(groups[groupIndex], point),
                    interGroupSmoothness);
            }

            return distance;
        }

        private static float EvaluateGroup(
            TopDown3DRockWorkbenchFieldGroup group,
            Vector3 point)
        {
            var distance = group.Boxes[0].Evaluate(point);
            for (var boxIndex = 1; boxIndex < group.Boxes.Count; boxIndex++)
            {
                distance = SmoothMinimum(
                    distance,
                    group.Boxes[boxIndex].Evaluate(point),
                    group.Smoothness);
            }

            return distance;
        }

        private static Color32[] BuildGeologicalSeamColors(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<TopDown3DRockWorkbenchFieldGroup> groups,
            float seamWidth)
        {
            var colors = new Color32[vertices.Count];
            if (groups.Count < 2) return colors;

            var contactWidth = Mathf.Max(0.001f, seamWidth * 0.8f);
            for (var vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                var closest = float.PositiveInfinity;
                var secondClosest = float.PositiveInfinity;
                for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    var distance = EvaluateGroup(groups[groupIndex], vertices[vertexIndex]);
                    if (distance < closest)
                    {
                        secondClosest = closest;
                        closest = distance;
                    }
                    else if (distance < secondClosest)
                    {
                        secondClosest = distance;
                    }
                }

                var ownershipDifference = Mathf.Max(0f, secondClosest - closest);
                var ownershipBoundary = 1f - Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(
                        seamWidth * 0.08f,
                        seamWidth,
                        ownershipDifference));
                var contactDistance = Mathf.Max(0f, secondClosest);
                var contact = 1f - Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(
                        contactWidth * 0.2f,
                        contactWidth,
                        contactDistance));
                var seam = Mathf.Pow(Mathf.Clamp01(ownershipBoundary * contact), 0.72f);
                colors[vertexIndex] = new Color32(
                    (byte)Mathf.RoundToInt(seam * byte.MaxValue),
                    0,
                    0,
                    byte.MaxValue);
            }

            return colors;
        }

        private static float SmoothMinimum(float first, float second, float smoothness)
        {
            if (smoothness <= 0.00001f) return Mathf.Min(first, second);
            var blend = Mathf.Clamp01(0.5f + 0.5f * (second - first) / smoothness);
            return Mathf.Lerp(second, first, blend) - smoothness * blend * (1f - blend);
        }

        private static int CountConnectedComponents(TopDown3DIndexedMeshData mesh)
        {
            var parents = new int[mesh.Vertices.Length];
            var used = new bool[mesh.Vertices.Length];
            for (var i = 0; i < parents.Length; i++) parents[i] = i;

            for (var triangle = 0; triangle < mesh.Triangles.Length; triangle += 3)
            {
                var a = mesh.Triangles[triangle];
                var b = mesh.Triangles[triangle + 1];
                var c = mesh.Triangles[triangle + 2];
                used[a] = used[b] = used[c] = true;
                Union(parents, a, b);
                Union(parents, b, c);
            }

            var roots = new HashSet<int>();
            for (var i = 0; i < parents.Length; i++)
            {
                if (used[i]) roots.Add(Find(parents, i));
            }

            return roots.Count;
        }

        private static bool TryOrientConsistently(
            IReadOnlyList<Vector3> vertices,
            IList<int> triangles,
            out string error)
        {
            error = null;
            var triangleCount = triangles.Count / 3;
            var edges = new Dictionary<GridEdge, List<TriangleEdgeReference>>();
            for (var triangle = 0; triangle < triangleCount; triangle++)
            {
                var offset = triangle * 3;
                AddEdgeReference(edges, triangle, triangles[offset], triangles[offset + 1]);
                AddEdgeReference(edges, triangle, triangles[offset + 1], triangles[offset + 2]);
                AddEdgeReference(edges, triangle, triangles[offset + 2], triangles[offset]);
            }

            var adjacency = new List<OrientationConstraint>[triangleCount];
            for (var triangle = 0; triangle < triangleCount; triangle++)
            {
                adjacency[triangle] = new List<OrientationConstraint>();
            }

            foreach (var pair in edges)
            {
                var uses = pair.Value;
                if (uses.Count != 2)
                {
                    error = $"edge {pair.Key} is used by {uses.Count} triangles instead of two";
                    return false;
                }

                var first = uses[0];
                var second = uses[1];
                var sameDirection = first.From == second.From && first.To == second.To;
                adjacency[first.Triangle].Add(new OrientationConstraint(second.Triangle, sameDirection));
                adjacency[second.Triangle].Add(new OrientationConstraint(first.Triangle, sameDirection));
            }

            var assigned = new bool[triangleCount];
            var flipped = new bool[triangleCount];
            var componentByTriangle = new int[triangleCount];
            var componentCount = 0;
            var queue = new Queue<int>();
            for (var start = 0; start < triangleCount; start++)
            {
                if (assigned[start]) continue;
                assigned[start] = true;
                componentByTriangle[start] = componentCount;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var constraint in adjacency[current])
                    {
                        var requiredFlip = flipped[current] ^ constraint.RequiresOppositeFlip;
                        if (!assigned[constraint.Triangle])
                        {
                            assigned[constraint.Triangle] = true;
                            flipped[constraint.Triangle] = requiredFlip;
                            componentByTriangle[constraint.Triangle] = componentCount;
                            queue.Enqueue(constraint.Triangle);
                        }
                        else if (flipped[constraint.Triangle] != requiredFlip)
                        {
                            error = "triangle adjacency contains a contradictory orientation cycle";
                            return false;
                        }
                    }
                }
                componentCount++;
            }

            for (var triangle = 0; triangle < triangleCount; triangle++)
            {
                if (flipped[triangle]) FlipTriangle(triangles, triangle);
            }

            var volumes = new double[componentCount];
            for (var triangle = 0; triangle < triangleCount; triangle++)
            {
                var offset = triangle * 3;
                var a = vertices[triangles[offset]];
                var b = vertices[triangles[offset + 1]];
                var c = vertices[triangles[offset + 2]];
                volumes[componentByTriangle[triangle]] += SignedTetrahedronVolume(a, b, c);
            }

            for (var component = 0; component < componentCount; component++)
            {
                if (Math.Abs(volumes[component]) <= 1e-12)
                {
                    error = $"surface component {component + 1} has zero signed volume";
                    return false;
                }
                if (volumes[component] >= 0d) continue;
                for (var triangle = 0; triangle < triangleCount; triangle++)
                {
                    if (componentByTriangle[triangle] == component) FlipTriangle(triangles, triangle);
                }
            }

            return true;
        }

        private static void AddEdgeReference(
            IDictionary<GridEdge, List<TriangleEdgeReference>> edges,
            int triangle,
            int from,
            int to)
        {
            var edge = new GridEdge(from, to);
            if (!edges.TryGetValue(edge, out var uses))
            {
                uses = new List<TriangleEdgeReference>(2);
                edges.Add(edge, uses);
            }
            uses.Add(new TriangleEdgeReference(triangle, from, to));
        }

        private static void FlipTriangle(IList<int> triangles, int triangle)
        {
            var offset = triangle * 3;
            var temporary = triangles[offset + 1];
            triangles[offset + 1] = triangles[offset + 2];
            triangles[offset + 2] = temporary;
        }

        private static double SignedTetrahedronVolume(Vector3 a, Vector3 b, Vector3 c)
        {
            var crossX = (double)b.y * c.z - (double)b.z * c.y;
            var crossY = (double)b.z * c.x - (double)b.x * c.z;
            var crossZ = (double)b.x * c.y - (double)b.y * c.x;
            return ((double)a.x * crossX + (double)a.y * crossY + (double)a.z * crossZ) / 6.0;
        }

        private static void Union(IList<int> parents, int first, int second)
        {
            var firstRoot = Find(parents, first);
            var secondRoot = Find(parents, second);
            if (firstRoot != secondRoot) parents[secondRoot] = firstRoot;
        }

        private static int Find(IList<int> parents, int index)
        {
            while (parents[index] != index)
            {
                parents[index] = parents[parents[index]];
                index = parents[index];
            }

            return index;
        }

        private static int PointIndex(int x, int y, int z, Vector3Int points)
        {
            return x + points.x * (y + points.y * z);
        }

        private readonly struct GridEdge : IEquatable<GridEdge>
        {
            internal GridEdge(int first, int second)
            {
                Min = Math.Min(first, second);
                Max = Math.Max(first, second);
            }

            private int Min { get; }
            private int Max { get; }

            public override string ToString()
            {
                return $"{Min}-{Max}";
            }

            public bool Equals(GridEdge other)
            {
                return Min == other.Min && Max == other.Max;
            }

            public override bool Equals(object obj)
            {
                return obj is GridEdge other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Min * 397) ^ Max;
                }
            }
        }

        private readonly struct TriangleEdgeReference
        {
            internal TriangleEdgeReference(int triangle, int from, int to)
            {
                Triangle = triangle;
                From = from;
                To = to;
            }

            internal int Triangle { get; }
            internal int From { get; }
            internal int To { get; }
        }

        private readonly struct OrientationConstraint
        {
            internal OrientationConstraint(int triangle, bool requiresOppositeFlip)
            {
                Triangle = triangle;
                RequiresOppositeFlip = requiresOppositeFlip;
            }

            internal int Triangle { get; }
            internal bool RequiresOppositeFlip { get; }
        }
    }
}
