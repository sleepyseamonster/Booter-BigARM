using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    /// <summary>
    /// Debounces author-driven transform edits before requesting a Boolean mesh union.
    /// Fusion is deliberately editor-only and occurs only for formations whose author
    /// explicitly enabled automatic fusion.
    /// </summary>
    [InitializeOnLoad]
    internal static class TopDown3DRockFormationAutoFusion
    {
        private const double IdleDelaySeconds = 0.45d;
        private static readonly Dictionary<TopDown3DRockFormationAuthoring, WatchState> WatchStates =
            new Dictionary<TopDown3DRockFormationAuthoring, WatchState>();
        private static bool sceneWatchInitialized;
        private static int sceneTransformHash;
        private static double sceneLastChangedTime;

        static TopDown3DRockFormationAutoFusion()
        {
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            var formations = Object.FindObjectsByType<TopDown3DRockFormationAuthoring>();
            var activeFormations = new HashSet<TopDown3DRockFormationAuthoring>();
            var now = EditorApplication.timeSinceStartup;
            UpdateSceneFusion(formations, now);
            for (var index = 0; index < formations.Length; index++)
            {
                var formation = formations[index];
                if (formation == null || formation.GeneratedMembersRoot == null)
                {
                    continue;
                }

                if (TopDown3DRockFormationFusionUtility.HasFusedMembers(formation))
                {
                    formation.GeneratedMembersRoot.name = "Fused Rock Members (Source Editable)";
                    continue;
                }

                if (!formation.AutoFuseOverlappingMembers) continue;

                activeFormations.Add(formation);
                var hash = TopDown3DRockFormationFusionUtility.GetEditableMembersTransformHash(formation);
                if (!WatchStates.TryGetValue(formation, out var state) || state.Hash != hash)
                {
                    WatchStates[formation] = new WatchState(hash, now);
                    continue;
                }

                if (state.LastFailedHash == hash || now - state.LastChangedTime < IdleDelaySeconds)
                {
                    continue;
                }

                if (!TopDown3DRockFormationFusionUtility.HasOverlappingEditableMembers(formation))
                {
                    continue;
                }

                if (!TopDown3DRockFormationFusionUtility.TryFuseOverlappingMembers(
                        formation,
                        out _,
                        out var fusionError))
                {
                    state.LastFailedHash = hash;
                    WatchStates[formation] = state;
                    if (!string.IsNullOrEmpty(fusionError))
                    {
                        Debug.LogWarning($"[Rock Authoring] {fusionError}", formation);
                    }
                }
            }

            var staleFormations = new List<TopDown3DRockFormationAuthoring>();
            foreach (var pair in WatchStates)
            {
                if (!activeFormations.Contains(pair.Key)) staleFormations.Add(pair.Key);
            }

            for (var index = 0; index < staleFormations.Count; index++)
            {
                WatchStates.Remove(staleFormations[index]);
            }
        }

        private static void UpdateSceneFusion(
            IReadOnlyList<TopDown3DRockFormationAuthoring> formations,
            double now)
        {
            unchecked
            {
                var hash = 17;
                for (var index = 0; index < formations.Count; index++)
                {
                    var formation = formations[index];
                    if (formation == null || !formation.AutoFuseOverlappingMembers) continue;
                    hash = hash * 31 + formation.GetInstanceID();
                    hash = hash * 31 + TopDown3DRockFormationFusionUtility.GetEditableMembersTransformHash(formation);
                }

                if (!sceneWatchInitialized || sceneTransformHash != hash)
                {
                    sceneWatchInitialized = true;
                    sceneTransformHash = hash;
                    sceneLastChangedTime = now;
                    return;
                }
            }

            if (now - sceneLastChangedTime < IdleDelaySeconds) return;
            if (TopDown3DRockFormationFusionUtility.TryFuseAcrossFormations(
                    formations,
                    out _,
                    out var sceneFusionError))
            {
                sceneWatchInitialized = false;
            }
            else if (!string.IsNullOrEmpty(sceneFusionError))
            {
                Debug.LogWarning($"[Rock Authoring] {sceneFusionError}");
                sceneLastChangedTime = now;
            }
        }

        private struct WatchState
        {
            internal WatchState(int hash, double lastChangedTime)
            {
                Hash = hash;
                LastChangedTime = lastChangedTime;
                LastFailedHash = int.MinValue;
            }

            internal int Hash;
            internal double LastChangedTime;
            internal int LastFailedHash;
        }
    }
}
