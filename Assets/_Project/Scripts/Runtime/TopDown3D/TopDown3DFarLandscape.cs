using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BooterBigArm.TopDown3D.WorldCreator;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Streams non-colliding middle and far tiles from the same canonical representation
    /// scheduler used by near terrain. It owns presentation objects, never geography.
    /// </summary>
    internal sealed class TopDown3DFarLandscape
    {
        private const int MidRadius = 3;
        private const int FarRadius = 3;

        private static readonly ProfilerMarker IntegrateMarker =
            new ProfilerMarker("TopDown3D.World.IntegrateFarRepresentation");

        private readonly Transform parent;
        private readonly TopDown3DWorldSettings settings;
        private readonly TopDown3DWorldGenerator generator;
        private readonly WorldCreatorProductionRuntime runtime;
        private readonly Material material;
        private readonly HashSet<WorldRepresentationKey> required =
            new HashSet<WorldRepresentationKey>();
        private readonly List<WorldRepresentationKey> pending =
            new List<WorldRepresentationKey>();
        private readonly Dictionary<WorldRepresentationKey, Task<WorldRepresentationRequestOutcome>> requests =
            new Dictionary<WorldRepresentationKey, Task<WorldRepresentationRequestOutcome>>();
        private readonly Dictionary<WorldRepresentationKey, double> retryDue =
            new Dictionary<WorldRepresentationKey, double>();
        private readonly Dictionary<WorldRepresentationKey, int> retryCounts =
            new Dictionary<WorldRepresentationKey, int>();
        private readonly Dictionary<WorldRepresentationKey, double> missingIntegrationSince =
            new Dictionary<WorldRepresentationKey, double>();
        private readonly Dictionary<WorldRepresentationKey, GameObject> loaded =
            new Dictionary<WorldRepresentationKey, GameObject>();
        private readonly List<WorldRepresentationKey> removalBuffer =
            new List<WorldRepresentationKey>();
        private long anchorA = long.MinValue;
        private long anchorB = long.MinValue;

        internal TopDown3DFarLandscape(
            Transform parent,
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            WorldCreatorProductionRuntime runtime,
            Material material)
        {
            this.parent = parent;
            this.settings = settings;
            this.generator = generator;
            this.runtime = runtime;
            this.material = material;
        }

        internal int LoadedRepresentationCount => loaded.Count;
        internal int PendingRepresentationCount => pending.Count + requests.Count;

        internal void Refresh(Vector3 targetPosition, bool force)
        {
            if (parent == null || settings == null || generator == null || runtime == null || material == null)
            {
                return;
            }

            var absolute = generator.ToAbsolute(targetPosition.x, targetPosition.y, targetPosition.z);
            var nextA = checked((long)Math.Floor(absolute.HorizontalA / settings.ChunkSize));
            var nextB = checked((long)Math.Floor(absolute.HorizontalB / settings.ChunkSize));
            if (!force && nextA == anchorA && nextB == anchorB)
            {
                return;
            }

            anchorA = nextA;
            anchorB = nextB;
            required.Clear();
            pending.Clear();
            AddTier(WorldRepresentationTier.Mid, settings.ChunkSize * 4d, MidRadius, absolute);
            AddTier(WorldRepresentationTier.Far, settings.ChunkSize * 16d, FarRadius, absolute);
            RemoveStaleObjects();
            foreach (var key in required)
            {
                if (!loaded.ContainsKey(key) && !requests.ContainsKey(key))
                {
                    pending.Add(key);
                }
            }

            pending.Sort((left, right) => left.CompareTo(right));
        }

        internal int ProcessReady(int requestBudget, int integrationBudget)
        {
            var requestsToStart = Mathf.Max(0, requestBudget);
            while (requestsToStart-- > 0 && TryTakeReadyPending(out var key))
            {
                if (!required.Contains(key) || loaded.ContainsKey(key) || requests.ContainsKey(key))
                {
                    continue;
                }

                requests.Add(key, runtime.RequestAsync(key));
            }

            var integrated = 0;
            removalBuffer.Clear();
            foreach (var pair in requests)
            {
                if (integrated >= integrationBudget || !pair.Value.IsCompleted)
                {
                    continue;
                }

                var outcome = pair.Value.GetAwaiter().GetResult();
                if (outcome.State == WorldRepresentationRequestState.Failed
                    || outcome.State == WorldRepresentationRequestState.Cancelled)
                {
                    Debug.LogError($"World Creator {pair.Key.Tier} representation {pair.Key.TileA},{pair.Key.TileB} {outcome.State}: {outcome.Error}");
                    removalBuffer.Add(pair.Key);
                    ScheduleRetry(pair.Key);
                    continue;
                }

                if (!required.Contains(pair.Key))
                {
                    removalBuffer.Add(pair.Key);
                    missingIntegrationSince.Remove(pair.Key);
                    continue;
                }

                if (!runtime.TryGetIntegrated(pair.Key, out var result))
                {
                    var now = Time.realtimeSinceStartupAsDouble;
                    if (!missingIntegrationSince.TryGetValue(pair.Key, out var since))
                    {
                        missingIntegrationSince.Add(pair.Key, now);
                        since = now;
                    }
                    if (outcome.State == WorldRepresentationRequestState.QueuedForIntegration
                        && runtime.CaptureMetrics().Queued > 0 && now - since < 5d)
                        continue;
                    removalBuffer.Add(pair.Key);
                    missingIntegrationSince.Remove(pair.Key);
                    ScheduleRetry(pair.Key);
                    continue;
                }

                using (IntegrateMarker.Auto())
                {
                    CreateRepresentation(result);
                }

                removalBuffer.Add(pair.Key);
                retryDue.Remove(pair.Key);
                retryCounts.Remove(pair.Key);
                missingIntegrationSince.Remove(pair.Key);
                integrated++;
            }

            for (var i = 0; i < removalBuffer.Count; i++)
            {
                requests.Remove(removalBuffer[i]);
            }

            return integrated;
        }

        internal void RepositionForCurrentFrame()
        {
            foreach (var pair in loaded)
            {
                if (pair.Value != null
                    && runtime.TryToLocal(pair.Key.Minimum, out var local))
                {
                    pair.Value.transform.localPosition = new Vector3(local.X, local.Y, local.Z);
                }
            }
        }

        internal void Dispose()
        {
            foreach (var pair in loaded)
            {
                DestroyOwnedObject(pair.Value);
            }

            loaded.Clear();
            requests.Clear();
            pending.Clear();
            required.Clear();
            retryDue.Clear();
            retryCounts.Clear();
            missingIntegrationSince.Clear();
        }

        private bool TryTakeReadyPending(out WorldRepresentationKey key)
        {
            var now = Time.realtimeSinceStartupAsDouble;
            for (var i = 0; i < pending.Count; i++)
            {
                var candidate = pending[i];
                if (!required.Contains(candidate) || loaded.ContainsKey(candidate)
                    || requests.ContainsKey(candidate))
                {
                    pending.RemoveAt(i--);
                    continue;
                }
                if (retryDue.TryGetValue(candidate, out var due) && due > now) continue;
                pending.RemoveAt(i);
                retryDue.Remove(candidate);
                key = candidate;
                return true;
            }
            key = default;
            return false;
        }

        private void ScheduleRetry(WorldRepresentationKey key)
        {
            if (!required.Contains(key)) return;
            var attempts = retryCounts.TryGetValue(key, out var previous)
                ? Math.Min(7, previous + 1) : 1;
            retryCounts[key] = attempts;
            retryDue[key] = Time.realtimeSinceStartupAsDouble
                + Math.Min(30d, 0.5d * (1 << (attempts - 1)));
            if (!pending.Contains(key)) pending.Add(key);
        }

        private void AddTier(
            WorldRepresentationTier tier,
            double span,
            int radius,
            AbsoluteWorldPosition target)
        {
            var centerA = checked((long)Math.Floor(target.HorizontalA / span));
            var centerB = checked((long)Math.Floor(target.HorizontalB / span));
            for (var offsetB = -radius; offsetB <= radius; offsetB++)
            {
                for (var offsetA = -radius; offsetA <= radius; offsetA++)
                {
                    var key = runtime.CreateRepresentationKey(
                        tier,
                        checked(centerA + offsetA),
                        checked(centerB + offsetB),
                        span);
                    // Skip a coarse tile only when the finer tier covers its entire
                    // rectangle. The rings have different alignment and the near radius
                    // can shrink in the diagnostic profile, so fixed inner holes are unsafe.
                    if (!IsFullyCoveredByFinerTier(key, target)) required.Add(key);
                }
            }
        }

        private bool IsFullyCoveredByFinerTier(
            WorldRepresentationKey key,
            AbsoluteWorldPosition target)
        {
            double finerMinA, finerMinB, finerMaxA, finerMaxB;
            if (key.Tier == WorldRepresentationTier.Mid)
            {
                var nearSpan = (double)settings.ChunkSize;
                var radius = TopDown3DPlaytestPerformanceProfile.StreamingRadiusOverride > 0
                    ? Math.Min(settings.StreamingRadius,
                        TopDown3DPlaytestPerformanceProfile.StreamingRadiusOverride)
                    : settings.StreamingRadius;
                finerMinA = (Math.Floor(target.HorizontalA / nearSpan) - radius) * nearSpan;
                finerMinB = (Math.Floor(target.HorizontalB / nearSpan) - radius) * nearSpan;
                finerMaxA = finerMinA + (radius * 2 + 1) * nearSpan;
                finerMaxB = finerMinB + (radius * 2 + 1) * nearSpan;
            }
            else
            {
                var midSpan = settings.ChunkSize * 4d;
                finerMinA = (Math.Floor(target.HorizontalA / midSpan) - MidRadius) * midSpan;
                finerMinB = (Math.Floor(target.HorizontalB / midSpan) - MidRadius) * midSpan;
                finerMaxA = finerMinA + (MidRadius * 2 + 1) * midSpan;
                finerMaxB = finerMinB + (MidRadius * 2 + 1) * midSpan;
            }
            var minimum = key.Minimum;
            return minimum.HorizontalA >= finerMinA && minimum.HorizontalB >= finerMinB
                && minimum.HorizontalA + key.TileSpan <= finerMaxA
                && minimum.HorizontalB + key.TileSpan <= finerMaxB;
        }

        private void RemoveStaleObjects()
        {
            removalBuffer.Clear();
            foreach (var pair in loaded)
            {
                if (!required.Contains(pair.Key))
                {
                    DestroyOwnedObject(pair.Value);
                    removalBuffer.Add(pair.Key);
                }
            }

            for (var i = 0; i < removalBuffer.Count; i++)
            {
                loaded.Remove(removalBuffer[i]);
            }

            removalBuffer.Clear();
            foreach (var pair in requests)
            {
                if (!required.Contains(pair.Key))
                {
                    removalBuffer.Add(pair.Key);
                }
            }

            for (var i = 0; i < removalBuffer.Count; i++)
            {
                requests.Remove(removalBuffer[i]);
                retryDue.Remove(removalBuffer[i]);
                retryCounts.Remove(removalBuffer[i]);
                missingIntegrationSince.Remove(removalBuffer[i]);
            }

            removalBuffer.Clear();
            foreach (var pair in retryCounts)
                if (!required.Contains(pair.Key)) removalBuffer.Add(pair.Key);
            for (var i = 0; i < removalBuffer.Count; i++)
            {
                retryDue.Remove(removalBuffer[i]);
                retryCounts.Remove(removalBuffer[i]);
            }
        }

        private void CreateRepresentation(WorldRepresentationBuildResult result)
        {
            if (loaded.ContainsKey(result.Key)
                || !runtime.TryToLocal(result.Key.Minimum, out var local))
            {
                return;
            }

            var name = $"World Creator {result.Key.Tier} {result.Key.TileA},{result.Key.TileB}";
            var mesh = TopDown3DChunkMeshBuilder.BuildMesh(result, name);
            if (Application.isPlaying)
            {
                mesh.UploadMeshData(true);
            }

            var representation = new GameObject(name);
            representation.transform.SetParent(parent, false);
            representation.transform.localPosition = new Vector3(local.X, local.Y, local.Z);
            representation.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = representation.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            loaded.Add(result.Key, representation);
        }

        private static void DestroyOwnedObject(GameObject ownedObject)
        {
            if (ownedObject == null)
            {
                return;
            }

            var filter = ownedObject.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(filter.sharedMesh);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
                }
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(ownedObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(ownedObject);
            }
        }
    }
}
