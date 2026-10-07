using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public sealed class WorldRepresentationCache : IDisposable
    {
        private readonly Dictionary<WorldRepresentationKey, CacheEntry> entries =
            new Dictionary<WorldRepresentationKey, CacheEntry>();
        private readonly LinkedList<WorldRepresentationKey> recency =
            new LinkedList<WorldRepresentationKey>();

        public WorldRepresentationCache(int maximumEntries, long maximumBytes)
        {
            if (maximumEntries < 1) throw new ArgumentOutOfRangeException(nameof(maximumEntries));
            if (maximumBytes < 1) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            MaximumEntries = maximumEntries;
            MaximumBytes = maximumBytes;
        }

        public int MaximumEntries { get; }
        public long MaximumBytes { get; }
        public int Count => entries.Count;
        public long CurrentBytes { get; private set; }
        public long HitCount { get; private set; }
        public long MissCount { get; private set; }
        public long EvictionCount { get; private set; }

        public bool TryGet(WorldRepresentationKey key, out WorldRepresentationBuildResult result)
        {
            if (!entries.TryGetValue(key, out var entry))
            {
                result = null;
                MissCount++;
                return false;
            }

            recency.Remove(entry.Node);
            recency.AddLast(entry.Node);
            HitCount++;
            result = entry.Result;
            return true;
        }

        public bool Store(WorldRepresentationBuildResult result, out int evicted)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.IsDisposed) throw new ObjectDisposedException(nameof(result));
            evicted = 0;
            if (result.EstimatedBytes > MaximumBytes)
            {
                result.Dispose();
                return false;
            }

            if (entries.TryGetValue(result.Key, out var existing))
            {
                Remove(existing, false);
            }

            var node = recency.AddLast(result.Key);
            entries.Add(result.Key, new CacheEntry(result, node));
            CurrentBytes += result.EstimatedBytes;
            while (entries.Count > MaximumEntries || CurrentBytes > MaximumBytes)
            {
                var oldest = recency.First;
                if (oldest == null) break;
                var entry = entries[oldest.Value];
                Remove(entry, true);
                evicted++;
            }

            return entries.ContainsKey(result.Key);
        }

        public IReadOnlyList<WorldRepresentationBuildResult> Snapshot()
        {
            var results = new List<WorldRepresentationBuildResult>(entries.Count);
            var node = recency.First;
            while (node != null)
            {
                results.Add(entries[node.Value].Result);
                node = node.Next;
            }

            return results.AsReadOnly();
        }

        public bool Remove(WorldRepresentationKey key)
        {
            if (!entries.TryGetValue(key, out var entry)) return false;
            Remove(entry, false);
            return true;
        }

        public void Clear()
        {
            var snapshot = Snapshot();
            entries.Clear();
            recency.Clear();
            CurrentBytes = 0;
            for (var i = 0; i < snapshot.Count; i++) snapshot[i].Dispose();
        }

        public void Dispose() => Clear();

        private void Remove(CacheEntry entry, bool eviction)
        {
            entries.Remove(entry.Result.Key);
            recency.Remove(entry.Node);
            CurrentBytes -= entry.Result.EstimatedBytes;
            entry.Result.Dispose();
            if (eviction) EvictionCount++;
        }

        private sealed class CacheEntry
        {
            public CacheEntry(WorldRepresentationBuildResult result, LinkedListNode<WorldRepresentationKey> node)
            {
                Result = result;
                Node = node;
            }

            public WorldRepresentationBuildResult Result { get; }
            public LinkedListNode<WorldRepresentationKey> Node { get; }
        }
    }
}
