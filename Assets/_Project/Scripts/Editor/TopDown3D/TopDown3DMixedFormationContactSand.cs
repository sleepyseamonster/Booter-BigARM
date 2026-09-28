using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    internal sealed class TopDown3DContactRockPreview
    {
        internal TopDown3DContactRockPreview(
            Transform transform, MeshFilter filter, MeshRenderer renderer, int seed)
        {
            Transform = transform;
            Filter = filter;
            Renderer = renderer;
            Seed = seed;
        }

        internal Transform Transform { get; }
        internal MeshFilter Filter { get; }
        internal MeshRenderer Renderer { get; }
        internal int Seed { get; }
    }

    /// <summary>
    /// Draws the editor-only rock stain and a separate raised visual sand lip at each contact.
    /// Neither preview receives a collider or mutates the terrain or authored materials.
    /// </summary>
    internal static class TopDown3DMixedFormationContactSand
    {
        private const float ContactJoinTolerance = 0.08f;
        private const string RockBandShaderName = "BooterBigArm/TopDown3D/Rock Contact Sand Band";
        private const string TrimShaderName = "BooterBigArm/TopDown3D/Formation Contact Sand";
        private static readonly int GroundHeightId = Shader.PropertyToID("_GroundHeight");
        private static readonly int BandHeightId = Shader.PropertyToID("_BandHeight");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int FeatherHeightId = Shader.PropertyToID("_FeatherHeight");
        private static readonly int BaseFeatherDistanceId = Shader.PropertyToID("_BaseFeatherDistance");
        private static readonly int TopFeatherDistanceId = Shader.PropertyToID("_TopFeatherDistance");
        private static readonly int BorderWidthId = Shader.PropertyToID("_BorderWidth");
        private static readonly int WavinessId = Shader.PropertyToID("_Waviness");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int DirectionalBuildupId = Shader.PropertyToID("_DirectionalBuildup");
        private static readonly int WindDirectionId = Shader.PropertyToID("_WindDirection");
        private static readonly int RockCenterId = Shader.PropertyToID("_RockCenter");
        private static readonly int ColorId = Shader.PropertyToID("_BandColor");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");
        private static readonly int RockBaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int RockNormalMapId = Shader.PropertyToID("_NormalMap");
        private static readonly int RockMetersPerTileId = Shader.PropertyToID("_RockMetersPerTile");
        private static readonly int TriplanarSharpnessId = Shader.PropertyToID("_TriplanarSharpness");
        private static readonly int NormalStrengthId = Shader.PropertyToID("_NormalStrength");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int SourceBaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SandBorderColorId = Shader.PropertyToID("_SandBorderColor");
        private static readonly int MetersPerTileId = Shader.PropertyToID("_MetersPerTile");
        private static readonly int BaseMetersPerTileId = Shader.PropertyToID("_BaseMetersPerTile");
        private static Material rockBandMaterial;
        private static Material trimMaterial;

        private sealed class GroundedRock
        {
            internal TopDown3DContactRockPreview Rock;
            internal MeshFilter Filter;
            internal MeshRenderer Renderer;
            internal float GroundHeight;
        }

        internal static void Apply(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            Transform contextRoot,
            MeshCollider[] terrain,
            TopDown3DRockWorkbenchAuthoring[] rocks,
            IReadOnlyList<TopDown3DDustDepositionPlanner.AuthoredObstruction> contacts)
        {
            var previews = new List<TopDown3DContactRockPreview>(rocks.Length);
            foreach (var rock in rocks)
                previews.Add(new TopDown3DContactRockPreview(rock.transform,
                    rock.GetComponent<MeshFilter>(), rock.GetComponent<MeshRenderer>(), rock.GenerationSeed));
            Apply(sandbox, contextRoot, terrain, previews, contacts);
        }

        internal static void Apply(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            Transform contextRoot,
            MeshCollider[] terrain,
            IReadOnlyList<TopDown3DContactRockPreview> rocks,
            IReadOnlyList<TopDown3DDustDepositionPlanner.AuthoredObstruction> contacts)
        {
            var color = sandbox.ContactSandColor;
            var windRadians = sandbox.WorldSettings.PrevailingWindDegrees * Mathf.Deg2Rad;
            var wind = new Vector4(Mathf.Cos(windRadians), Mathf.Sin(windRadians), 0f, 0f);
            var material = sandbox.ContactSandOpacity > 0f ? GetRockBandMaterial() : null;

            var groundedRocks = new List<GroundedRock>(rocks.Count);
            foreach (var rock in rocks)
            {
                var sourceFilter = rock.Filter;
                var sourceRenderer = rock.Renderer;
                float groundHeight;
                if (sourceFilter == null || sourceFilter.sharedMesh == null || sourceRenderer == null
                    || !TrySampleGround(terrain, sourceRenderer.bounds.center, out groundHeight))
                    continue;
                if (sourceRenderer.bounds.min.y > groundHeight + 0.15f
                    || sourceRenderer.bounds.max.y <= groundHeight + 0.01f)
                    continue;

                groundedRocks.Add(new GroundedRock
                {
                    Rock = rock,
                    Filter = sourceFilter,
                    Renderer = sourceRenderer,
                    GroundHeight = groundHeight
                });
            }

            if (material != null)
            foreach (var group in BuildRockGroups(groundedRocks, contacts))
            {
                var componentBounds = groundedRocks[group[0]].Renderer.bounds;
                var componentGroundHeight = 0f;
                var componentSeed = 17;
                foreach (var memberIndex in group)
                {
                    var member = groundedRocks[memberIndex];
                    componentBounds.Encapsulate(member.Renderer.bounds);
                    componentGroundHeight += member.GroundHeight;
                    componentSeed = unchecked(componentSeed * 31 + member.Rock.Seed);
                }
                componentGroundHeight /= group.Count;
                var componentCenter = new Vector4(
                    componentBounds.center.x, componentBounds.center.z, 0f, 0f);
                var componentPhase = StablePhase(componentSeed);

                foreach (var memberIndex in group)
                {
                    var member = groundedRocks[memberIndex];
                    var rock = member.Rock;
                    var sourceFilter = member.Filter;
                    var sourceRenderer = member.Renderer;

                    var overlay = new GameObject("Rock Base Shader Band")
                    {
                        hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
                    };
                    overlay.transform.SetParent(rock.Transform, false);
                    overlay.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                    var renderer = overlay.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;

                    var properties = new MaterialPropertyBlock();
                    properties.SetFloat(GroundHeightId, componentGroundHeight);
                    properties.SetFloat(BandHeightId, sandbox.ContactSandWidth);
                    properties.SetFloat(OpacityId, sandbox.ContactSandOpacity);
                    properties.SetFloat(FeatherHeightId, sandbox.ContactSandFeather);
                    properties.SetFloat(WavinessId, sandbox.ContactSandWaviness);
                    properties.SetFloat(NoiseScaleId, sandbox.ContactSandNoiseScale);
                    properties.SetFloat(DirectionalBuildupId, sandbox.ContactSandDirectionalBuildup);
                    properties.SetVector(WindDirectionId, wind);
                    properties.SetVector(RockCenterId, componentCenter);
                    properties.SetColor(ColorId, color);
                    properties.SetFloat(PhaseId, componentPhase);
                    var sourceMaterial = sourceRenderer.sharedMaterial;
                    if (sourceMaterial != null)
                    {
                        if (sourceMaterial.HasProperty(RockBaseMapId))
                            properties.SetTexture(RockBaseMapId, sourceMaterial.GetTexture(RockBaseMapId));
                        if (sourceMaterial.HasProperty(RockNormalMapId))
                            properties.SetTexture(RockNormalMapId, sourceMaterial.GetTexture(RockNormalMapId));
                        if (sourceMaterial.HasProperty(RockMetersPerTileId))
                            properties.SetFloat(RockMetersPerTileId, sourceMaterial.GetFloat(RockMetersPerTileId));
                        if (sourceMaterial.HasProperty(TriplanarSharpnessId))
                            properties.SetFloat(TriplanarSharpnessId, sourceMaterial.GetFloat(TriplanarSharpnessId));
                        if (sourceMaterial.HasProperty(NormalStrengthId))
                            properties.SetFloat(NormalStrengthId, sourceMaterial.GetFloat(NormalStrengthId));
                    }
                    renderer.SetPropertyBlock(properties);
                }
            }

            BuildBorderTrim(sandbox, contextRoot, terrain, contacts, wind);
        }

        private static Material GetRockBandMaterial()
        {
            if (rockBandMaterial != null) return rockBandMaterial;
            var shader = Shader.Find(RockBandShaderName);
            if (shader == null)
            {
                Debug.LogError($"Missing shader: {RockBandShaderName}");
                return null;
            }
            rockBandMaterial = new Material(shader)
            {
                name = "Rock Contact Sand Band Preview",
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = true
            };
            return rockBandMaterial;
        }

        private static Material GetTrimMaterial(TopDown3DLandscapeAuthoringSandbox sandbox)
        {
            if (trimMaterial == null)
            {
                var shader = Shader.Find(TrimShaderName);
                if (shader == null)
                {
                    Debug.LogError($"Missing shader: {TrimShaderName}");
                    return null;
                }
                trimMaterial = new Material(shader)
                {
                    name = "Rock Contact Raised Sand Trim Preview",
                    hideFlags = HideFlags.HideAndDontSave,
                    enableInstancing = true
                };
            }
            // The raised bevel belongs visually to the ground, not to the rock stain.
            // Prefer the sandbox terrain material so its texture and tint remain the single
            // source of truth. Only the outer terrain-facing toe receives the authored fade;
            // deposited dust is a safe fallback when no terrain material is assigned.
            var ground = sandbox.TerrainMaterial != null
                ? sandbox.TerrainMaterial
                : sandbox.WorldSettings != null ? sandbox.WorldSettings.DepositedDustMaterial : null;
            if (ground != null)
            {
                if (ground.HasProperty(BaseMapId))
                    trimMaterial.SetTexture(BaseMapId, ground.GetTexture(BaseMapId));
                if (ground.HasProperty(SourceBaseColorId))
                    trimMaterial.SetColor(SandBorderColorId, ground.GetColor(SourceBaseColorId));
                if (ground.HasProperty(BaseMetersPerTileId))
                    trimMaterial.SetFloat(MetersPerTileId, ground.GetFloat(BaseMetersPerTileId));
                else if (ground.HasProperty(MetersPerTileId))
                    trimMaterial.SetFloat(MetersPerTileId, ground.GetFloat(MetersPerTileId));
            }
            return trimMaterial;
        }

        private static void BuildBorderTrim(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            Transform contextRoot,
            MeshCollider[] terrain,
            IReadOnlyList<TopDown3DDustDepositionPlanner.AuthoredObstruction> contacts,
            Vector4 wind)
        {
            if (contextRoot == null || contacts == null || contacts.Count == 0) return;
            var material = GetTrimMaterial(sandbox);
            if (material == null) return;

            var width = sandbox.ContactSandTrimWidth;
            var rise = sandbox.ContactSandTrimHeight;
            var curve = sandbox.ContactSandTrimCurve;
            // Keep small clutter contacts round at inspection distance without allowing large
            // formations to create unbounded preview meshes.
            var spacing = Mathf.Clamp(width / 12f, 0.018f, 0.04f);
            var innerTuck = Mathf.Clamp(width * 0.35f, 0.045f, 0.12f);
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var wind2 = new Vector2(wind.x, wind.y).normalized;

            foreach (var group in BuildContactGroups(contacts))
            {
                var reach = width * (1.35f + sandbox.ContactSandWaviness * 0.25f);
                var minimum = contacts[group[0]].Center - contacts[group[0]].HalfSize;
                var maximum = contacts[group[0]].Center + contacts[group[0]].HalfSize;
                var componentSeed = 127;
                foreach (var contactIndex in group)
                {
                    var contact = contacts[contactIndex];
                    minimum = Vector2.Min(minimum, contact.Center - contact.HalfSize);
                    maximum = Vector2.Max(maximum, contact.Center + contact.HalfSize);
                    componentSeed = unchecked(componentSeed * 31 + contactIndex * 486187739);
                }
                var componentCenter = (minimum + maximum) * 0.5f;
                var half = (maximum - minimum) * 0.5f + Vector2.one * reach;
                var xCount = Mathf.Clamp(Mathf.CeilToInt(half.x * 2f / spacing) + 1, 10, 256);
                var zCount = Mathf.Clamp(Mathf.CeilToInt(half.y * 2f / spacing) + 1, 10, 256);
                var baseVertex = vertices.Count;
                var phase = StablePhase(componentSeed);

                for (var z = 0; z < zCount; z++)
                for (var x = 0; x < xCount; x++)
                {
                    var local = new Vector2(
                        Mathf.Lerp(-half.x, half.x, x / (float)(xCount - 1)),
                        Mathf.Lerp(-half.y, half.y, z / (float)(zCount - 1)));
                    var point = componentCenter + local;
                    var signedDistance = float.PositiveInfinity;
                    foreach (var contactIndex in group)
                    {
                        var contact = contacts[contactIndex];
                        if (signedDistance >= 0f
                            && BoundsDistanceOutside(contact, point) > signedDistance) continue;
                        signedDistance = Mathf.Min(signedDistance, contact.SignedDistance(point));
                        if (signedDistance <= -innerTuck) break;
                    }
                    var outward = local.sqrMagnitude > 0.000001f ? local.normalized : Vector2.right;
                    var noise = Mathf.PerlinNoise(
                        point.x * sandbox.ContactSandNoiseScale + phase,
                        point.y * sandbox.ContactSandNoiseScale + phase * 0.37f) - 0.5f;
                    var directional = Vector2.Dot(outward, wind2);
                    var effectiveWidth = width * Mathf.Clamp(
                        1f + noise * sandbox.ContactSandWaviness * 0.45f
                        + directional * sandbox.ContactSandDirectionalBuildup * 0.28f,
                        0.48f, 1.55f);
                    var normalized = Mathf.Clamp01(signedDistance / Mathf.Max(0.01f, effectiveWidth));
                    // This is a concave-up bevel into the rock, not a freestanding ridge.
                    // It leaves the terrain nearly flat, then becomes progressively steeper
                    // until it reaches full height beneath the contact footprint.
                    var inwardProgress = 1f - normalized;
                    var smoothedProgress = Mathf.SmoothStep(0f, 1f, inwardProgress);
                    var profile = Mathf.Pow(smoothedProgress, curve);
                    var crest = rise * profile * (0.88f + (noise + 0.5f) * 0.18f);
                    RaycastHit hit;
                    if (!TrySampleGround(terrain, new Vector3(point.x, 0f, point.y), out hit))
                        hit = new RaycastHit { point = new Vector3(point.x, 0f, point.y), normal = Vector3.up };
                    vertices.Add(contextRoot.InverseTransformPoint(
                        hit.point + hit.normal * (Mathf.Lerp(0.0015f, 0.004f, profile) + crest)));
                    colors.Add(new Color(
                        signedDistance / Mathf.Max(0.01f, effectiveWidth),
                        phase / 31f,
                        profile,
                        signedDistance / innerTuck));
                    uvs.Add(new Vector2(local.x / Mathf.Max(half.x - reach, 0.05f),
                        local.y / Mathf.Max(half.y - reach, 0.05f)));
                }

                for (var z = 0; z < zCount - 1; z++)
                for (var x = 0; x < xCount - 1; x++)
                {
                    var a = baseVertex + z * xCount + x;
                    var b = a + 1;
                    var c = a + xCount;
                    var d = c + 1;
                    if (colors[a].b <= 0f && colors[b].b <= 0f && colors[c].b <= 0f && colors[d].b <= 0f)
                        continue;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }

            if (triangles.Count == 0) return;
            var mesh = new Mesh
            {
                name = "Rock Contact Raised Sand Trim Mesh",
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable,
                indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var trim = new GameObject("Raised Sand Contact Border")
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };
            trim.transform.SetParent(contextRoot, false);
            trim.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = trim.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            var properties = new MaterialPropertyBlock();
            properties.SetFloat(WavinessId, sandbox.ContactSandWaviness);
            properties.SetFloat(NoiseScaleId, sandbox.ContactSandNoiseScale);
            properties.SetFloat(DirectionalBuildupId, sandbox.ContactSandDirectionalBuildup);
            properties.SetVector(WindDirectionId, wind);
            properties.SetFloat(BorderWidthId, sandbox.ContactSandTrimWidth);
            properties.SetFloat(BaseFeatherDistanceId, sandbox.ContactSandBaseFeather);
            properties.SetFloat(TopFeatherDistanceId, sandbox.ContactSandTopFeather);
            renderer.SetPropertyBlock(properties);
        }

        private static List<List<int>> BuildRockGroups(
            IReadOnlyList<GroundedRock> rocks,
            IReadOnlyList<TopDown3DDustDepositionPlanner.AuthoredObstruction> contacts)
        {
            // Both lists are collected from the same ordered grounded-rock array. Reuse the
            // contour groups whenever possible so stain and border agree on what is connected.
            if (contacts != null && contacts.Count == rocks.Count)
                return BuildContactGroups(contacts);
            return BuildGroups(rocks.Count, (left, right) =>
            {
                var a = rocks[left].Renderer.bounds;
                var b = rocks[right].Renderer.bounds;
                return Mathf.Abs(a.center.x - b.center.x) <= a.extents.x + b.extents.x + ContactJoinTolerance
                    && Mathf.Abs(a.center.z - b.center.z) <= a.extents.z + b.extents.z + ContactJoinTolerance;
            });
        }

        private static List<List<int>> BuildContactGroups(
            IReadOnlyList<TopDown3DDustDepositionPlanner.AuthoredObstruction> contacts)
        {
            return BuildGroups(contacts.Count, (left, right) =>
            {
                var a = contacts[left];
                var b = contacts[right];
                return a.TouchesOrOverlaps(b, ContactJoinTolerance);
            });
        }

        private static float BoundsDistanceOutside(
            TopDown3DDustDepositionPlanner.AuthoredObstruction contact, Vector2 point)
        {
            var delta = new Vector2(
                Mathf.Max(Mathf.Abs(point.x - contact.Center.x) - contact.HalfSize.x, 0f),
                Mathf.Max(Mathf.Abs(point.y - contact.Center.y) - contact.HalfSize.y, 0f));
            return delta.magnitude;
        }

        private static List<List<int>> BuildGroups(int count, Func<int, int, bool> connected)
        {
            var parents = new int[count];
            for (var i = 0; i < count; i++) parents[i] = i;

            int Find(int value)
            {
                while (parents[value] != value)
                {
                    parents[value] = parents[parents[value]];
                    value = parents[value];
                }
                return value;
            }

            for (var left = 0; left < count; left++)
            for (var right = left + 1; right < count; right++)
            {
                if (!connected(left, right)) continue;
                var leftRoot = Find(left);
                var rightRoot = Find(right);
                if (leftRoot != rightRoot) parents[rightRoot] = leftRoot;
            }

            var byRoot = new Dictionary<int, List<int>>();
            for (var i = 0; i < count; i++)
            {
                var root = Find(i);
                if (!byRoot.TryGetValue(root, out var group))
                {
                    group = new List<int>();
                    byRoot.Add(root, group);
                }
                group.Add(i);
            }
            return new List<List<int>>(byRoot.Values);
        }

        private static float StablePhase(int seed)
        {
            unchecked
            {
                var value = (uint)seed;
                value = (value ^ (value >> 16)) * 0x7FEB352Du;
                value = (value ^ (value >> 15)) * 0x846CA68Bu;
                return ((value ^ (value >> 16)) & 0xFFFFFF) / 16777215f * 31f;
            }
        }

        private static bool TrySampleGround(
            MeshCollider[] terrain,
            Vector3 rockCenter,
            out float groundHeight)
        {
            RaycastHit hit;
            if (TrySampleGround(terrain, rockCenter, out hit))
            {
                groundHeight = hit.point.y;
                return true;
            }
            groundHeight = 0f;
            return false;
        }

        private static bool TrySampleGround(
            MeshCollider[] terrain,
            Vector3 sample,
            out RaycastHit hit)
        {
            var point = new Vector2(sample.x, sample.z);
            for (var i = 0; i < terrain.Length; i++)
            {
                var bounds = terrain[i].bounds;
                if (point.x < bounds.min.x || point.x > bounds.max.x
                    || point.y < bounds.min.z || point.y > bounds.max.z) continue;
                var origin = new Vector3(point.x, bounds.max.y + 1f, point.y);
                if (!terrain[i].Raycast(
                        new Ray(origin, Vector3.down),
                        out hit,
                        bounds.size.y + 2f)) continue;
                return true;
            }
            hit = default;
            return false;
        }
    }
}
