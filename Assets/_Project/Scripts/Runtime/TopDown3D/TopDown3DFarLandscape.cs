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
        private readonly Func<long, long, bool> isNearLoaded;
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
        private readonly Dictionary<WorldRepresentationKey, LoadedTile> loaded =
            new Dictionary<WorldRepresentationKey, LoadedTile>();
        private readonly List<WorldRepresentationKey> removalBuffer =
            new List<WorldRepresentationKey>();
        private readonly HashSet<WorldRepresentationKey> coverageDirtyTiles =
            new HashSet<WorldRepresentationKey>();
        private long anchorA = long.MinValue;
        private long anchorB = long.MinValue;

        private sealed class LoadedTile
        {
            internal GameObject Object;
            internal Mesh Mesh;
            internal MeshRenderer Renderer;
            internal int Resolution;
            internal bool[] HiddenQuads;
        }

        internal TopDown3DFarLandscape(
            Transform parent,
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            WorldCreatorProductionRuntime runtime,
            Material material,
            Func<long, long, bool> isNearLoaded = null)
        {
            this.parent = parent;
            this.settings = settings;
            this.generator = generator;
            this.runtime = runtime;
            this.material = material;
            this.isNearLoaded = isNearLoaded;
        }

        internal int LoadedRepresentationCount => loaded.Count;
        internal int PendingRepresentationCount => pending.Count + requests.Count;

        internal void NotifyNearCoverageChanged(long nearA, long nearB)
        {
            var span = (double)settings.ChunkSize;
            MarkCoveringTiles((nearA + 0.5d) * span, (nearB + 0.5d) * span);
        }

        private void MarkCoveringTiles(double pointA, double pointB,
            WorldRepresentationTier? tier = null)
        {
            foreach (var key in loaded.Keys)
            {
                if (tier.HasValue && key.Tier != tier.Value) continue;
                var minimum = key.Minimum;
                if (pointA >= minimum.HorizontalA && pointA < minimum.HorizontalA + key.TileSpan
                    && pointB >= minimum.HorizontalB && pointB < minimum.HorizontalB + key.TileSpan)
                    coverageDirtyTiles.Add(key);
            }
        }

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

            RefreshCoverage();

            return integrated;
        }

        internal void RepositionForCurrentFrame()
        {
            foreach (var pair in loaded)
            {
                if (pair.Value.Object != null
                    && runtime.TryToLocal(pair.Key.Minimum, out var local))
                {
                    pair.Value.Object.transform.localPosition = new Vector3(local.X, local.Y, local.Z);
                }
            }
        }

        internal void Dispose()
        {
            foreach (var pair in loaded)
            {
                DestroyOwnedObject(pair.Value.Object);
            }

            loaded.Clear();
            requests.Clear();
            pending.Clear();
            required.Clear();
            retryDue.Clear();
            retryCounts.Clear();
            missingIntegrationSince.Clear();
            coverageDirtyTiles.Clear();
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
                    // Finer terrain can still be pending. Keep the coarse fallback and
                    // mask only the quads for finer tiles that have actually loaded.
                    required.Add(key);
                }
            }
        }

        private void RemoveStaleObjects()
        {
            removalBuffer.Clear();
            foreach (var pair in loaded)
            {
                if (!required.Contains(pair.Key))
                {
                    if (pair.Key.Tier == WorldRepresentationTier.Mid)
                        MarkCoveringTiles(pair.Key.Minimum.HorizontalA + pair.Key.TileSpan * 0.5d,
                            pair.Key.Minimum.HorizontalB + pair.Key.TileSpan * 0.5d,
                            WorldRepresentationTier.Far);
                    DestroyOwnedObject(pair.Value.Object);
                    removalBuffer.Add(pair.Key);
                    coverageDirtyTiles.Remove(pair.Key);
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

        private void RefreshCoverage()
        {
            if (coverageDirtyTiles.Count == 0) return;

            var midTiles = new HashSet<(long, long)>();
            foreach (var pair in loaded)
                if (pair.Key.Tier == WorldRepresentationTier.Mid)
                    midTiles.Add((pair.Key.TileA, pair.Key.TileB));

            foreach (var key in coverageDirtyTiles)
            {
                if (!loaded.TryGetValue(key, out var tile)) continue;
                var quads = tile.Resolution - 1;
                var hidden = ComputeHiddenQuads(
                    key.Tier, key.Minimum.HorizontalA, key.Minimum.HorizontalB,
                    key.TileSpan, tile.Resolution, settings.ChunkSize,
                    isNearLoaded, midTiles);
                var changed = tile.HiddenQuads == null;
                for (var index = 0; !changed && index < hidden.Length; index++)
                    if (hidden[index] != tile.HiddenQuads[index]) changed = true;

                if (!changed) continue;
                var triangles = new List<int>(quads * quads * 6);
                for (var z = 0; z < quads; z++)
                for (var x = 0; x < quads; x++)
                {
                    if (hidden[z * quads + x]) continue;
                    var bottomLeft = z * tile.Resolution + x;
                    var topLeft = bottomLeft + tile.Resolution;
                    triangles.Add(bottomLeft);
                    triangles.Add(topLeft);
                    triangles.Add(bottomLeft + 1);
                    triangles.Add(bottomLeft + 1);
                    triangles.Add(topLeft);
                    triangles.Add(topLeft + 1);
                }

                tile.Mesh.SetTriangles(triangles, 0, true);
                tile.Renderer.enabled = triangles.Count != 0;
                tile.HiddenQuads = hidden;
            }
            coverageDirtyTiles.Clear();
        }

        internal static bool[] ComputeHiddenQuads(
            WorldRepresentationTier tier,
            double minimumA,
            double minimumB,
            double tileSpan,
            int resolution,
            double nearSpan,
            Func<long, long, bool> nearLoaded,
            HashSet<(long, long)> midTiles)
        {
            var quads = resolution - 1;
            var step = tileSpan / quads;
            var midSpan = nearSpan * 4d;
            var hidden = new bool[quads * quads];
            for (var z = 0; z < quads; z++)
            for (var x = 0; x < quads; x++)
            {
                var a = minimumA + (x + 0.5d) * step;
                var b = minimumB + (z + 0.5d) * step;
                var nearA = checked((long)Math.Floor(a / nearSpan));
                var nearB = checked((long)Math.Floor(b / nearSpan));
                var quadMinA = minimumA + x * step;
                var quadMaxA = minimumA + (x + 1) * step;
                var quadMinB = minimumB + z * step;
                var quadMaxB = minimumB + (z + 1) * step;
                var covered = IsInsideCell(quadMinA, quadMaxA, nearSpan, nearA)
                    && IsInsideCell(quadMinB, quadMaxB, nearSpan, nearB)
                    && (nearLoaded?.Invoke(nearA, nearB) ?? false);
                if (!covered && tier == WorldRepresentationTier.Far)
                {
                    var midA = checked((long)Math.Floor(a / midSpan));
                    var midB = checked((long)Math.Floor(b / midSpan));
                    covered = IsInsideCell(quadMinA, quadMaxA, midSpan, midA)
                        && IsInsideCell(quadMinB, quadMaxB, midSpan, midB)
                        && midTiles.Contains((midA, midB));
                }
                hidden[z * quads + x] = covered;
            }

            return hidden;
        }

        private static bool IsInsideCell(double minimum, double maximum, double span, long cell)
        {
            var cellMinimum = cell * span;
            var tolerance = span * 1e-8d;
            return minimum >= cellMinimum - tolerance
                && maximum <= cellMinimum + span + tolerance;
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
            var representation = new GameObject(name);
            representation.transform.SetParent(parent, false);
            representation.transform.localPosition = new Vector3(local.X, local.Y, local.Z);
            representation.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = representation.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            loaded.Add(result.Key, new LoadedTile
            {
                Object = representation,
                Mesh = mesh,
                Renderer = renderer,
                Resolution = result.Resolution
            });
            coverageDirtyTiles.Add(result.Key);
            if (result.Key.Tier == WorldRepresentationTier.Mid)
                MarkCoveringTiles(result.Key.Minimum.HorizontalA + result.Key.TileSpan * 0.5d,
                    result.Key.Minimum.HorizontalB + result.Key.TileSpan * 0.5d,
                    WorldRepresentationTier.Far);
        }

        private static void DestroyOwnedObject(GameObject ownedObject)
        {
            if (ownedObject == null)
            {
                return;
            }

            // Destroy is deferred in Play Mode; stop drawing the retired tile now.
            ownedObject.SetActive(false);

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
