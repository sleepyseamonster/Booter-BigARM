using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// A closed bedrock body between two terrain cross sections. Its ends use the
    /// same absolute samples as neighboring spans, so streamed spans meet cleanly.
    /// </summary>
    internal static class TopDown3DCliffFaceMeshBuilder
    {
        private const int RingSize = 12;

        internal static Mesh Build(
            Vector3 firstRim, Vector3 firstToe, Vector3 firstOutward,
            Vector3 lastRim, Vector3 lastToe, Vector3 lastOutward,
            int seed, bool near, float strataShift = 0f,
            float firstRhythm = 0f, float lastRhythm = 0f,
            Func<Vector3, float> renderedTerrainHeight = null)
        {
            var stations = near ? 5 : 2;
            var vertices = new List<Vector3>(stations * RingSize + 2);
            var seamWeights = new List<float>(stations * RingSize + 2);
            var triangles = new List<int>((stations - 1) * RingSize * 6 + RingSize * 6);
            for (var station = 0; station < stations; station++)
            {
                var t = station / (float)(stations - 1);
                var rim = Vector3.Lerp(firstRim, lastRim, t);
                var toe = Vector3.Lerp(firstToe, lastToe, t);
                var outward = Vector3.Lerp(firstOutward, lastOutward, t).normalized;
                var irregularity = station == 0 || station == stations - 1
                    ? 0f : (Hash01(seed, station) - 0.5f) * 0.24f;
                var drop = Mathf.Max(0.5f, rim.y - toe.y);
                var faceDepth = Mathf.Lerp(0.34f, 0.86f,
                    Mathf.Clamp01((drop - 1.5f) / 4f));
                var rhythm = Mathf.Lerp(firstRhythm, lastRhythm, t);
                faceDepth *= 1f + rhythm * 0.30f;
                var lowerBand = Mathf.Clamp(0.31f + strataShift + rhythm * 0.065f,
                    0.22f, 0.40f);
                var upperBand = Mathf.Clamp(0.59f + strataShift - rhythm * 0.055f,
                    0.48f, 0.70f);

                // The two ledges and cap share the same parent-level strata phase.
                // Endpoint stations carry no random displacement, so neighbors meet.
                vertices.Add(toe - outward * 0.55f + Vector3.down * 0.38f);
                vertices.Add(toe + outward * 0.16f + Vector3.down * 0.34f);
                vertices.Add(Vector3.Lerp(toe, rim, 0.13f)
                    + outward * (0.32f + irregularity * 0.4f));
                vertices.Add(Vector3.Lerp(toe, rim, lowerBand - 0.035f)
                    + outward * (faceDepth * 0.77f + irregularity));
                vertices.Add(Vector3.Lerp(toe, rim, lowerBand)
                    + outward * (faceDepth * 1.14f + irregularity));
                vertices.Add(Vector3.Lerp(toe, rim, lowerBand + 0.055f)
                    + outward * (faceDepth * 0.67f + irregularity * 0.6f));
                vertices.Add(Vector3.Lerp(toe, rim, upperBand - 0.035f)
                    + outward * (faceDepth * 0.73f + irregularity));
                vertices.Add(Vector3.Lerp(toe, rim, upperBand)
                    + outward * (faceDepth * 1.18f + irregularity));
                vertices.Add(Vector3.Lerp(toe, rim, upperBand + 0.055f)
                    + outward * (faceDepth * 0.63f + irregularity * 0.5f));
                vertices.Add(Vector3.Lerp(toe, rim, 0.88f)
                    + outward * (faceDepth * 0.88f + irregularity * 0.3f));
                vertices.Add(rim + outward * (0.18f + irregularity * 0.25f)
                    + Vector3.up * 0.10f);
                vertices.Add(rim - outward * 0.42f + Vector3.down * 0.24f);
                for (var side = 0; side < RingSize; side++)
                    seamWeights.Add(side == 4 || side == 7 || side == 10 ? 1f : 0f);
            }

            if (renderedTerrainHeight != null)
            {
                for (var index = 0; index < vertices.Count; index++)
                {
                    var vertex = vertices[index];
                    vertex.y = Mathf.Min(vertex.y,
                        renderedTerrainHeight(vertex) + 0.15f);
                    vertices[index] = vertex;
                }
            }

            for (var station = 0; station < stations - 1; station++)
            for (var side = 0; side < RingSize; side++)
            {
                var next = (side + 1) % RingSize;
                var a = station * RingSize + side;
                var b = (station + 1) * RingSize + side;
                var c = station * RingSize + next;
                var d = (station + 1) * RingSize + next;
                if (side == RingSize - 1 && renderedTerrainHeight != null)
                {
                    AddBuriedBackTriangle(vertices, seamWeights, triangles,
                        a, c, b, renderedTerrainHeight);
                    AddBuriedBackTriangle(vertices, seamWeights, triangles,
                        c, d, b, renderedTerrainHeight);
                }
                else
                {
                    AddTriangle(triangles, a, c, b);
                    AddTriangle(triangles, c, d, b);
                }
            }

            var firstCap = vertices.Count;
            vertices.Add(AverageRing(vertices, 0));
            seamWeights.Add(0f);
            var lastCap = vertices.Count;
            vertices.Add(AverageRing(vertices, (stations - 1) * RingSize));
            seamWeights.Add(0f);
            if (renderedTerrainHeight != null)
            {
                foreach (var cap in new[] { firstCap, lastCap })
                {
                    var vertex = vertices[cap];
                    vertex.y = Mathf.Min(vertex.y,
                        renderedTerrainHeight(vertex) - 2.0f);
                    vertices[cap] = vertex;
                }
            }
            for (var side = 0; side < RingSize; side++)
            {
                var next = (side + 1) % RingSize;
                AddTriangle(triangles, firstCap, next, side);
                var offset = (stations - 1) * RingSize;
                AddTriangle(triangles, lastCap, offset + side, offset + next);
            }

            // Every facet owns a normal; geological planes should not shade like
            // a rounded pipe. This also keeps the collider's visible silhouette.
            var flatVertices = new Vector3[triangles.Count];
            var flatTriangles = new int[triangles.Count];
            var normals = new Vector3[triangles.Count];
            var colors = new Color[triangles.Count];
            for (var index = 0; index < triangles.Count; index += 3)
            {
                var a = vertices[triangles[index]];
                var b = vertices[triangles[index + 1]];
                var c = vertices[triangles[index + 2]];
                var normal = Vector3.Cross(b - a, c - a).normalized;
                flatVertices[index] = a;
                flatVertices[index + 1] = b;
                flatVertices[index + 2] = c;
                for (var corner = 0; corner < 3; corner++)
                {
                    flatTriangles[index + corner] = index + corner;
                    normals[index + corner] = normal;
                    colors[index + corner] = new Color(
                        seamWeights[triangles[index + corner]], 0f, 0f, 1f);
                }
            }
            var mesh = new Mesh { name = near ? "Cliff Bedrock LOD0" : "Cliff Bedrock LOD1" };
            mesh.vertices = flatVertices;
            mesh.triangles = flatTriangles;
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 AverageRing(List<Vector3> vertices, int offset)
        {
            var total = Vector3.zero;
            for (var i = 0; i < RingSize; i++) total += vertices[offset + i];
            return total / RingSize;
        }

        private static void AddTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        private static void AddBuriedBackTriangle(
            List<Vector3> vertices, List<float> seamWeights,
            List<int> triangles, int a, int b, int c,
            Func<Vector3, float> renderedTerrainHeight)
        {
            var center = (vertices[a] + vertices[b] + vertices[c]) / 3f;
            center.y = Mathf.Min(center.y, renderedTerrainHeight(center) - 2f);
            var middle = vertices.Count;
            vertices.Add(center);
            seamWeights.Add(0f);
            AddTriangle(triangles, a, b, middle);
            AddTriangle(triangles, b, c, middle);
            AddTriangle(triangles, c, a, middle);
        }

        private static float Hash01(int seed, int station)
        {
            unchecked
            {
                var value = (uint)seed ^ (uint)station * 0x9E3779B9u;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (value & 0xFFFFFFu) / 16777215f;
            }
        }
    }
}
