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
        private const int InnerExclusionRadius = 1;

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
            var anchorSpan = settings.ChunkSize * 4d;
            var nextA = checked((long)Math.Floor(absolute.HorizontalA / anchorSpan));
            var nextB = checked((long)Math.Floor(absolute.HorizontalB / anchorSpan));
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
            while (requestsToStart-- > 0 && pending.Count > 0)
            {
                var key = pending[0];
                pending.RemoveAt(0);
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
                if (outcome.State == WorldRepresentationRequestState.Failed)
                {
                    Debug.LogError($"World Creator far representation failed: {outcome.Error}");
                    removalBuffer.Add(pair.Key);
                    continue;
                }

                if (!required.Contains(pair.Key))
                {
                    removalBuffer.Add(pair.Key);
                    continue;
                }

                if (!runtime.TryGetIntegrated(pair.Key, out var result))
                {
                    continue;
                }

                using (IntegrateMarker.Auto())
                {
                    CreateRepresentation(result);
                }

                removalBuffer.Add(pair.Key);
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
                    if (Math.Max(Math.Abs(offsetA), Math.Abs(offsetB)) <= InnerExclusionRadius)
                    {
                        continue;
                    }

                    required.Add(runtime.CreateRepresentationKey(
                        tier,
                        checked(centerA + offsetA),
                        checked(centerB + offsetB),
                        span));
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
