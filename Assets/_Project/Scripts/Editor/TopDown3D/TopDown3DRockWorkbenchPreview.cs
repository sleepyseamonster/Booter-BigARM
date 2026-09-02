using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    [InitializeOnLoad]
    internal static class TopDown3DRockWorkbenchPreview
    {
        private const double ScanIntervalSeconds = 0.08d;
        private const double RebuildDebounceSeconds = 0.12d;
        private static readonly int RockSeedId = Shader.PropertyToID("_RockSeed01");
        private static readonly int RockSizeId = Shader.PropertyToID("_RockSize");
        private static readonly int GeologyScaleId = Shader.PropertyToID("_RockMetersPerTile");
        private static readonly int SurfaceVariationId = Shader.PropertyToID("_SurfacePatchStrength");
        private static readonly int CrackAmountId = Shader.PropertyToID("_CrackAmount");
        private static readonly int SideGritId = Shader.PropertyToID("_SideGritAmount");
        private static readonly int WornShineId = Shader.PropertyToID("_WornSmoothnessBoost");
        private static readonly Dictionary<int, PreviewState> States = new Dictionary<int, PreviewState>();
        private static double nextScanTime;

        static TopDown3DRockWorkbenchPreview()
        {
            EditorApplication.update += Update;
        }

        internal static void RequestRebuild(TopDown3DRockWorkbenchAuthoring authoring, bool immediate)
        {
            if (!IsUsable(authoring)) return;
            var state = GetOrCreateState(authoring);
            state.Requested = true;
            state.PendingSignature = CalculateSignature(authoring);
            state.DueTime = immediate ? 0d : EditorApplication.timeSinceStartup + RebuildDebounceSeconds;
        }

        internal static void ClearPreview(TopDown3DRockWorkbenchAuthoring authoring, string status)
        {
            if (authoring == null) return;
            DestroyGeneratedMesh(authoring);

            var filter = authoring.GetComponent<MeshFilter>();
            if (filter != null) filter.sharedMesh = null;
            var collider = authoring.GetComponent<MeshCollider>();
            if (collider != null) collider.sharedMesh = null;
            authoring.SetPreviewState(null, status);

            if (IsUsable(authoring))
            {
                var state = GetOrCreateState(authoring);
                state.LastBuiltSignature = CalculateSignature(authoring);
                state.PendingSignature = state.LastBuiltSignature;
                state.Requested = false;
            }

            SceneView.RepaintAll();
        }

        private static void Update()
        {
            if (EditorApplication.isCompiling
                || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.timeSinceStartup < nextScanTime)
            {
                return;
            }

            nextScanTime = EditorApplication.timeSinceStartup + ScanIntervalSeconds;
            var seen = new HashSet<int>();
            var rebuiltThisUpdate = false;
            var workbenches = Resources.FindObjectsOfTypeAll<TopDown3DRockWorkbenchAuthoring>();
            foreach (var authoring in workbenches)
            {
                if (!IsUsable(authoring)) continue;
                var id = authoring.GetInstanceID();
                seen.Add(id);
                var state = GetOrCreateState(authoring);
                var signature = CalculateSignature(authoring);
                if (authoring.AutoRebuild && signature != state.PendingSignature)
                {
                    state.PendingSignature = signature;
                    state.Requested = true;
                    state.DueTime = EditorApplication.timeSinceStartup + RebuildDebounceSeconds;
                }

                if (authoring.AutoRebuild
                    && authoring.GeneratedMesh == null
                    && signature != state.LastBuiltSignature)
                {
                    state.PendingSignature = signature;
                    state.Requested = true;
                }

                if (state.Requested
                    && !rebuiltThisUpdate
                    && EditorApplication.timeSinceStartup >= state.DueTime
                    && signature == state.PendingSignature)
                {
                    Rebuild(authoring, state, signature);
                    rebuiltThisUpdate = true;
                }
            }

            if (seen.Count == States.Count) return;
            var stale = new List<int>();
            foreach (var pair in States)
            {
                if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
            }
            foreach (var id in stale) States.Remove(id);
        }

        private static void Rebuild(
            TopDown3DRockWorkbenchAuthoring authoring,
            PreviewState state,
            int signature)
        {
            state.Requested = false;
            state.LastBuiltSignature = signature;

            var nodes = authoring.GetComponentsInChildren<TopDown3DRockVolumeNode>(false);
            var boxes = new List<TopDown3DRockWorkbenchBox>(nodes.Length);
            foreach (var node in nodes)
            {
                if (!node.ContributesToRock || !node.gameObject.activeInHierarchy) continue;
                var scale = node.transform.lossyScale;
                if (Mathf.Abs(scale.x) < 0.001f
                    || Mathf.Abs(scale.y) < 0.001f
                    || Mathf.Abs(scale.z) < 0.001f)
                {
                    ClearPreview(authoring, $"Cube volume '{node.name}' has a zero-size axis and could not be meshed.");
                    return;
                }
                boxes.Add(new TopDown3DRockWorkbenchBox(
                    authoring.transform,
                    node.transform,
                    node.ShapeSeed));
            }

            if (!TopDown3DRockWorkbenchMesher.TryBuild(
                    boxes,
                    authoring.VoxelSize,
                    authoring.FusionSmoothness,
                    out var result,
                    out var error))
            {
                ClearPreview(authoring, $"Preview could not be built: {error}");
                return;
            }

            var mesh = result.CreateMesh($"{authoring.name} Preview");
            var filter = authoring.GetComponent<MeshFilter>();
            var renderer = authoring.GetComponent<MeshRenderer>();
            var collider = authoring.GetComponent<MeshCollider>();
            if (filter == null || renderer == null || collider == null)
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                ClearPreview(authoring, "Preview could not be built because a required mesh component is missing.");
                return;
            }

            DestroyGeneratedMesh(authoring);
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = authoring.RockMaterial;
            var materialProperties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(materialProperties);
            materialProperties.SetFloat(RockSeedId, SeedToUnitFloat(authoring.GenerationSeed));
            materialProperties.SetFloat(GeologyScaleId, authoring.GeologyScale);
            materialProperties.SetFloat(SurfaceVariationId, authoring.SurfaceVariation);
            materialProperties.SetFloat(CrackAmountId, authoring.CrackAmount);
            materialProperties.SetFloat(SideGritId, authoring.SideGrit);
            materialProperties.SetFloat(WornShineId, authoring.WornShine);
            var rockSize = mesh.bounds.size;
            materialProperties.SetVector(
                RockSizeId,
                new Vector4(rockSize.x, rockSize.y, rockSize.z, 0f));
            renderer.SetPropertyBlock(materialProperties);
            collider.sharedMesh = null;
            if (authoring.UpdateCollider) collider.sharedMesh = mesh;

            var connection = boxes.Count == 1
                ? "ONE SOURCE: one connected surface"
                : result.ConnectedComponents == 1
                    ? "FUSED: one connected surface"
                : $"NOT FUSED: {result.ConnectedComponents} disconnected surfaces";
            var resolutionNote = result.EffectiveVoxelSize > authoring.VoxelSize + 0.0001f
                ? $" Effective voxel size was capped to {result.EffectiveVoxelSize:0.###}."
                : string.Empty;
            authoring.SetPreviewState(
                mesh,
                $"{connection}. {result.Topology.VertexCount:N0} vertices, "
                + $"{result.Topology.TriangleCount:N0} triangles, closed-manifold topology. "
                + $"Grid {result.GridCells.x} x {result.GridCells.y} x {result.GridCells.z}.{resolutionNote}");
            SceneView.RepaintAll();
            InternalEditorUtilityRepaintAllViews();
        }

        private static void DestroyGeneratedMesh(TopDown3DRockWorkbenchAuthoring authoring)
        {
            var previous = authoring.GeneratedMesh;
            if (previous != null
                && !EditorUtility.IsPersistent(previous)
                && (previous.hideFlags & HideFlags.DontSaveInEditor) != 0)
            {
                UnityEngine.Object.DestroyImmediate(previous);
            }
        }

        private static PreviewState GetOrCreateState(TopDown3DRockWorkbenchAuthoring authoring)
        {
            var id = authoring.GetInstanceID();
            if (States.TryGetValue(id, out var state)) return state;
            state = new PreviewState
            {
                PendingSignature = int.MinValue,
                LastBuiltSignature = int.MinValue,
                DueTime = 0d,
                Requested = authoring.AutoRebuild
            };
            States.Add(id, state);
            return state;
        }

        private static int CalculateSignature(TopDown3DRockWorkbenchAuthoring authoring)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + authoring.VoxelSize.GetHashCode();
                hash = hash * 31 + authoring.FusionSmoothness.GetHashCode();
                hash = hash * 31 + authoring.UpdateCollider.GetHashCode();
                hash = hash * 31 + authoring.GeologyScale.GetHashCode();
                hash = hash * 31 + authoring.SurfaceVariation.GetHashCode();
                hash = hash * 31 + authoring.CrackAmount.GetHashCode();
                hash = hash * 31 + authoring.SideGrit.GetHashCode();
                hash = hash * 31 + authoring.WornShine.GetHashCode();
                hash = hash * 31 + authoring.GenerationSeed;
                hash = hash * 31 + (authoring.RockMaterial == null ? 0 : authoring.RockMaterial.GetInstanceID());
                var nodes = authoring.GetComponentsInChildren<TopDown3DRockVolumeNode>(true);
                hash = hash * 31 + nodes.Length;
                foreach (var node in nodes)
                {
                    hash = hash * 31 + node.GetInstanceID();
                    hash = hash * 31 + node.ContributesToRock.GetHashCode();
                    hash = hash * 31 + node.ShapeSeed;
                    hash = hash * 31 + node.gameObject.activeInHierarchy.GetHashCode();
                    var matrix = authoring.transform.worldToLocalMatrix * node.transform.localToWorldMatrix;
                    for (var row = 0; row < 4; row++)
                    {
                        for (var column = 0; column < 4; column++)
                        {
                            hash = hash * 31 + matrix[row, column].GetHashCode();
                        }
                    }
                }
                return hash;
            }
        }

        private static bool IsUsable(TopDown3DRockWorkbenchAuthoring authoring)
        {
            return authoring != null
                && !EditorUtility.IsPersistent(authoring)
                && authoring.gameObject.scene.IsValid()
                && authoring.gameObject.scene.isLoaded;
        }

        internal static float SeedToUnitFloat(int seed)
        {
            unchecked
            {
                var value = (uint)seed;
                value = (value ^ (value >> 16)) * 0x7FEB352Du;
                value = (value ^ (value >> 15)) * 0x846CA68Bu;
                value ^= value >> 16;
                return (value & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static void InternalEditorUtilityRepaintAllViews()
        {
            foreach (var editor in Resources.FindObjectsOfTypeAll<TopDown3DRockWorkbenchAuthoringEditor>())
            {
                editor.Repaint();
            }
        }

        private sealed class PreviewState
        {
            internal int PendingSignature;
            internal int LastBuiltSignature;
            internal double DueTime;
            internal bool Requested;
        }
    }
}
