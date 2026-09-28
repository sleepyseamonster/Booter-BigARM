using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Feeds the existing terrain pebble shader from streamed authored formations.</summary>
    internal static class TopDown3DFormationTerrainShader
    {
        private const int MaskResolution = 64;

        internal readonly struct Influence
        {
            internal Influence(TopDown3DRockFormationPlan formation, Vector2 localShift)
            {
                Formation = formation;
                LocalShift = localShift;
            }

            internal TopDown3DRockFormationPlan Formation { get; }
            internal Vector2 LocalShift { get; }
        }

        internal static void Apply(TopDown3DGeneratedChunk chunk, TopDown3DWorldSettings settings,
            IReadOnlyList<Influence> nearby)
        {
            var filter = chunk != null ? chunk.GetComponent<MeshFilter>() : null;
            var renderer = chunk != null ? chunk.GetComponent<MeshRenderer>() : null;
            if (filter == null || renderer == null || filter.sharedMesh == null
                || renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty("_PebbleDetail")) return;

            var mesh = filter.sharedMesh;
            var relevant = new List<Influence>();
            var chunkCenter = new Vector2(chunk.transform.position.x + settings.ChunkSize * 0.5f,
                chunk.transform.position.z + settings.ChunkSize * 0.5f);
            var chunkReach = settings.ChunkSize * 0.7072f;
            for (var i = 0; i < nearby.Count; i++)
            {
                var formation = nearby[i].Formation;
                if (formation.AuthoredTemplate == null || formation.AuthoredTemplate.SurfaceTreatment == null) continue;
                var reach = chunkReach + formation.EnvelopeRadius + 3f;
                if ((formation.EnvelopeCenter + nearby[i].LocalShift - chunkCenter).sqrMagnitude <= reach * reach)
                    relevant.Add(nearby[i]);
            }
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            if (relevant.Count == 0)
            {
                chunk.SetFormationGroundMask(null);
                properties.SetFloat("_FormationGroundMaskEnabled", 0f);
                properties.SetFloat("_PebbleDetail", 0f);
                properties.SetFloat("_NearRockPebbleDensity", 0f);
                renderer.SetPropertyBlock(properties);
                chunk.SetFormationGroundStones(null, null);
                return;
            }

            var depth = 0f;
            Material sourceMaterial = null;
            var sandTint = relevant[0].Formation.AuthoredTemplate.SurfaceTreatment.BandColor;
            for (var f = 0; f < relevant.Count; f++)
            {
                var formation = relevant[f].Formation;
                depth = Mathf.Max(depth, formation.AuthoredTemplate.SurfaceTreatment.PebbleDepth);
                if (sourceMaterial == null && formation.Members.Count > 0)
                    sourceMaterial = formation.Members[0].AuthoredMaterial;
            }

            var pixels = new Color32[MaskResolution * MaskResolution];
            var chunkOrigin = chunk.transform.position;
            var metersPerPixel = settings.ChunkSize / MaskResolution;
            for (var z = 0; z < MaskResolution; z++)
            for (var x = 0; x < MaskResolution; x++)
            {
                var position = new Vector2(chunkOrigin.x + (x + 0.5f) * metersPerPixel,
                    chunkOrigin.z + (z + 0.5f) * metersPerPixel);
                var absoluteX = (double)chunk.Coordinate.x * settings.ChunkSize + (x + 0.5f) * metersPerPixel;
                var absoluteZ = (double)chunk.Coordinate.y * settings.ChunkSize + (z + 0.5f) * metersPerPixel;
                var variation = Mathf.PerlinNoise((float)absoluteX * 0.65f + settings.WorldSeed % 1024,
                    (float)absoluteZ * 0.65f - settings.WorldSeed % 1024);
                var edgeScale = Mathf.Lerp(0.8f, 1.2f, variation);
                var coverage = 0f;
                var sand = 0f;
                for (var f = 0; f < relevant.Count; f++)
                {
                    var formation = relevant[f].Formation;
                    var shift = relevant[f].LocalShift;
                    var treatment = formation.AuthoredTemplate.SurfaceTreatment;
                    var strength = treatment.GroundClutter;
                    var formationDistance = Mathf.Max(0f,
                        Vector2.Distance(position, formation.EnvelopeCenter + shift) - formation.EnvelopeRadius);
                    coverage = Mathf.Max(coverage, 0.65f * (1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(formationDistance / (2.5f * edgeScale)))) * strength);
                    for (var m = 0; m < formation.Members.Count; m++)
                    {
                        var member = formation.Members[m];
                        if (member.WorldBounds.min.y > member.GroundHeight + 0.2f) continue;
                        var bounds = member.WorldBounds;
                        bounds.center += new Vector3(shift.x, 0f, shift.y);
                        var dx = (position.x - bounds.center.x) / Mathf.Max(0.1f, bounds.extents.x);
                        var dz = (position.y - bounds.center.z) / Mathf.Max(0.1f, bounds.extents.z);
                        var distance = Mathf.Max(0f, Mathf.Sqrt(dx * dx + dz * dz) - 1f)
                            * Mathf.Max(0.1f, Mathf.Min(bounds.extents.x, bounds.extents.z));
                        coverage = Mathf.Max(coverage, (1f - Mathf.SmoothStep(0f, 1f,
                            Mathf.Clamp01(distance / (1.8f * edgeScale)))) * strength);
                        sand = Mathf.Max(sand, (1f - Mathf.SmoothStep(0f, 1f,
                            Mathf.Clamp01(distance / (0.75f * edgeScale)))) * treatment.BandOpacity);
                    }
                }
                pixels[z * MaskResolution + x] = new Color32(
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(coverage) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(sand) * 255f), 0, 255);
            }

            // A fine mask avoids the terrain vertex grid and leaves UV2.y's existing sand
            // deposition information intact. The terrain mesh and collider are untouched.
            var mask = new Texture2D(MaskResolution, MaskResolution, TextureFormat.RGBA32, false, true)
            {
                name = $"Formation ground {chunk.Coordinate.x},{chunk.Coordinate.y}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            mask.SetPixels32(pixels);
            mask.Apply(false, false);
            chunk.SetFormationGroundMask(mask);
            properties.SetTexture("_FormationGroundMask", mask);
            properties.SetVector("_FormationGroundMaskOriginScale", new Vector4(chunkOrigin.x,
                chunkOrigin.z, 1f / settings.ChunkSize, 1f / settings.ChunkSize));
            properties.SetFloat("_FormationGroundMaskEnabled", 1f);
            properties.SetColor("_FormationSandTint", sandTint);
            properties.SetFloat("_PebbleDetail", 1f);
            properties.SetFloat("_NearRockPebbleDepth", depth);
            properties.SetFloat("_NearRockPebbleDensity", 0f);
            properties.SetTexture("_PebbleAlbedoMap", settings.MixedGroundPebbleAlbedo);
            properties.SetTexture("_PebbleHeightMap", settings.MixedGroundPebbleHeight);
            properties.SetTexture("_NearRockPebbleAlbedoMap", settings.NearRockPebbleAlbedo);
            properties.SetTexture("_NearRockPebbleHeightMap", settings.NearRockPebbleHeight);
            if (sourceMaterial != null && sourceMaterial.HasProperty("_BaseMap"))
                properties.SetTexture("_RockPebbleColorMap", sourceMaterial.GetTexture("_BaseMap"));
            renderer.SetPropertyBlock(properties);
            TopDown3DFormationSurfaceStones.Apply(chunk, settings, relevant);
        }

        internal static void RepositionMask(TopDown3DGeneratedChunk chunk, TopDown3DWorldSettings settings)
        {
            var renderer = chunk != null ? chunk.GetComponent<MeshRenderer>() : null;
            if (renderer == null || settings == null) return;
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            if (properties.GetFloat("_FormationGroundMaskEnabled") < 0.5f) return;
            var origin = chunk.transform.position;
            properties.SetVector("_FormationGroundMaskOriginScale", new Vector4(origin.x, origin.z,
                1f / settings.ChunkSize, 1f / settings.ChunkSize));
            renderer.SetPropertyBlock(properties);
        }
    }

    /// <summary>Small workbench-style stone pockets owned by the streamed terrain chunk.</summary>
    internal static class TopDown3DFormationSurfaceStones
    {
        private const int MaximumStonesPerChunk = 96;

        internal static void Apply(TopDown3DGeneratedChunk chunk, TopDown3DWorldSettings settings,
            IReadOnlyList<TopDown3DFormationTerrainShader.Influence> formations)
        {
            var terrain = chunk.GetComponent<MeshFilter>()?.sharedMesh;
            var catalog = settings.NaturalObjectCatalog;
            if (terrain == null || catalog == null)
            {
                chunk.SetFormationGroundStones(null, null);
                return;
            }
            var vertices = terrain.vertices;
            var normals = terrain.normals;
            var triangles = terrain.triangles;
            var instances = new List<CombineInstance>();
            var placed = new List<Vector3>();
            var sizes = new List<float>();
            Material material = null;
            foreach (var influence in formations)
            {
                var formation = influence.Formation;
                if (formation.AuthoredTemplate.SurfaceTreatment.GroundClutter <= 0f) continue;
                for (var memberIndex = 0; memberIndex < formation.Members.Count; memberIndex++)
                {
                    if (instances.Count >= MaximumStonesPerChunk) break;
                    var member = formation.Members[memberIndex];
                    if (member.WorldBounds.min.y > member.GroundHeight + 0.2f) continue;
                    var bounds = member.WorldBounds;
                    bounds.center += new Vector3(influence.LocalShift.x, 0f, influence.LocalShift.y);
                    for (var attempt = 0; attempt < 9 && instances.Count < MaximumStonesPerChunk; attempt++)
                    {
                        var seed = Hash(formation.StableId + ":stone:" + member.StableId + ":" + attempt);
                        if (Unit(seed ^ 7171) > formation.AuthoredTemplate.SurfaceTreatment.GroundClutter * 0.85f)
                            continue;
                        var angle = Unit(seed ^ 4141) * Mathf.PI * 2f;
                        var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        var edge = 1f / Mathf.Sqrt(
                            direction.x * direction.x / Mathf.Max(0.01f, bounds.extents.x * bounds.extents.x)
                            + direction.y * direction.y / Mathf.Max(0.01f, bounds.extents.z * bounds.extents.z));
                        var reach = edge + Mathf.Lerp(0.1f, 0.8f, Unit(seed ^ 8383));
                        var point = new Vector2(bounds.center.x, bounds.center.z) + direction * reach;
                        var local = chunk.transform.InverseTransformPoint(new Vector3(point.x, 0f, point.y));
                        if (local.x < 0f || local.z < 0f
                            || local.x >= settings.ChunkSize || local.z >= settings.ChunkSize) continue;
                        if (!TrySampleTerrain(vertices, normals, triangles, local.x, local.z,
                            out var groundHeight, out var groundNormal) || groundNormal.y < 0.85f) continue;
                        var sizeRoll = Unit(seed ^ 1515);
                        var size = sizeRoll < 0.55f ? Mathf.Lerp(0.07f, 0.16f, Unit(seed ^ 1919))
                            : sizeRoll < 0.85f ? Mathf.Lerp(0.16f, 0.32f, Unit(seed ^ 2919))
                            : Mathf.Lerp(0.30f, 0.52f, Unit(seed ^ 3919));
                        var occupied = false;
                        foreach (var other in formation.Members)
                        {
                            var otherBounds = other.WorldBounds;
                            otherBounds.center += new Vector3(influence.LocalShift.x, 0f, influence.LocalShift.y);
                            var dx = (point.x - otherBounds.center.x) / Mathf.Max(0.1f, otherBounds.extents.x);
                            var dz = (point.y - otherBounds.center.z) / Mathf.Max(0.1f, otherBounds.extents.z);
                            if (dx * dx + dz * dz < 0.78f) { occupied = true; break; }
                        }
                        for (var i = 0; !occupied && i < placed.Count; i++)
                        {
                            var separation = (size + sizes[i]) * 0.45f;
                            var delta = point - new Vector2(placed[i].x, placed[i].z);
                            if (delta.sqrMagnitude < separation * separation) occupied = true;
                        }
                        if (occupied || !catalog.TryGetMeshFamily(TopDown3DNaturalObjectShape.Nodule,
                            Mathf.Min(2, (int)(Unit(seed ^ 3131) * 3f)), out var family)) continue;
                        var source = family.Lod2;
                        if (source == null) continue;
                        var dimensions = source.bounds.size;
                        var horizontalScale = size / Mathf.Max(0.001f, Mathf.Max(dimensions.x, dimensions.z));
                        var verticalScale = size * Mathf.Lerp(0.35f, 0.85f, Unit(seed ^ 5151))
                            / Mathf.Max(0.001f, dimensions.y);
                        var rotation = Quaternion.FromToRotation(Vector3.up, groundNormal)
                            * Quaternion.Euler(0f, Unit(seed ^ 6161) * 360f, 0f);
                        var shape = Matrix4x4.TRS(Vector3.zero, rotation,
                            new Vector3(horizontalScale, verticalScale, horizontalScale))
                            * Matrix4x4.Translate(-source.bounds.center);
                        var bottom = float.PositiveInfinity;
                        var extent = source.bounds.extents;
                        for (var corner = 0; corner < 8; corner++)
                        {
                            var vertex = source.bounds.center + Vector3.Scale(extent, new Vector3(
                                (corner & 1) == 0 ? -1f : 1f,
                                (corner & 2) == 0 ? -1f : 1f,
                                (corner & 4) == 0 ? -1f : 1f));
                            bottom = Mathf.Min(bottom, shape.MultiplyPoint3x4(vertex).y);
                        }
                        var worldGround = chunk.transform.TransformPoint(new Vector3(local.x, groundHeight, local.z));
                        var pose = Matrix4x4.Translate(worldGround + Vector3.up * (-bottom - size * 0.24f))
                            * shape;
                        instances.Add(new CombineInstance
                        {
                            mesh = source,
                            transform = chunk.transform.worldToLocalMatrix * pose
                        });
                        placed.Add(worldGround);
                        sizes.Add(size);
                        if (material == null) material = member.AuthoredMaterial;
                    }
                }
            }
            if (instances.Count == 0 || material == null)
            {
                chunk.SetFormationGroundStones(null, null);
                return;
            }
            var combined = new Mesh
            {
                name = $"Formation stones {chunk.Coordinate.x},{chunk.Coordinate.y}",
                indexFormat = IndexFormat.UInt32
            };
            combined.CombineMeshes(instances.ToArray(), true, true);
            chunk.SetFormationGroundStones(combined, material);
            chunk.RefreshDecorationCounts();
        }

        private static bool TrySampleTerrain(Vector3[] vertices, Vector3[] normals, int[] triangles,
            float x, float z, out float height, out Vector3 normal)
        {
            height = float.NegativeInfinity;
            normal = Vector3.up;
            var found = false;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                if (x < Mathf.Min(a.x, Mathf.Min(b.x, c.x)) || x > Mathf.Max(a.x, Mathf.Max(b.x, c.x))
                    || z < Mathf.Min(a.z, Mathf.Min(b.z, c.z)) || z > Mathf.Max(a.z, Mathf.Max(b.z, c.z)))
                    continue;
                var denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(denominator) < 0.000001f) continue;
                var u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / denominator;
                var v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / denominator;
                var w = 1f - u - v;
                if (u < -0.0001f || v < -0.0001f || w < -0.0001f) continue;
                var candidateHeight = u * a.y + v * b.y + w * c.y;
                if (found && candidateHeight <= height) continue;
                height = candidateHeight;
                normal = (u * normals[triangles[i]] + v * normals[triangles[i + 1]]
                    + w * normals[triangles[i + 2]]).normalized;
                found = true;
            }
            return found;
        }

        private static int Hash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in value) hash = (hash ^ character) * 16777619;
                return (int)hash;
            }
        }

        private static float Unit(int seed)
        {
            unchecked
            {
                var value = (uint)seed;
                value = (value ^ (value >> 16)) * 0x7FEB352Du;
                value = (value ^ (value >> 15)) * 0x846CA68Bu;
                return ((value ^ (value >> 16)) & 0xFFFFFF) / 16777215f;
            }
        }
    }
}
