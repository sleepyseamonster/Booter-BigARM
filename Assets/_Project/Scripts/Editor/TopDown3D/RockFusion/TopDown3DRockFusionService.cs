using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace BooterBigArm.TopDown3D
{
    internal sealed class TopDown3DRockFusionService : IDisposable
    {
        private readonly object workGate = new object();
        private readonly object completionGate = new object();
        private readonly Queue<WorkItem> pendingWork = new Queue<WorkItem>();
        private readonly Queue<Completion> completions = new Queue<Completion>();
        private readonly Dictionary<long, MainThreadContext> contexts =
            new Dictionary<long, MainThreadContext>();
        private readonly HashSet<long> cancelledRequests = new HashSet<long>();
        private readonly AutoResetEvent workAvailable = new AutoResetEvent(false);
        private readonly Thread worker;
        private long nextRequestId;
        private bool stopping;
        private bool disposed;

        internal TopDown3DRockFusionService()
        {
            if (!TopDown3DManifoldNative.IsSupportedPlatform)
            {
                throw new PlatformNotSupportedException(
                    "Rock formation fusion currently requires the macOS Manifold plugin.");
            }

            worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "TopDown3D Rock Fusion"
            };
            worker.Start();
        }

        internal int PendingCount
        {
            get
            {
                lock (workGate)
                {
                    return pendingWork.Count;
                }
            }
        }

        internal int OutstandingCount => contexts.Count;

        internal long Enqueue(
            string formationStableId,
            int formationOrder,
            int chunkX,
            int chunkY,
            IReadOnlyList<TopDown3DIndexedMeshData> solids,
            long chunkGenerationToken,
            Func<bool> isStillValid,
            Action<TopDown3DRockFormationMeshData, double> onSuccess,
            Action<string> onFailure)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(TopDown3DRockFusionService));
            }

            if (string.IsNullOrEmpty(formationStableId))
            {
                throw new ArgumentException("A formation stable ID is required.", nameof(formationStableId));
            }

            if (solids == null || solids.Count < 2)
            {
                throw new ArgumentException("Fusion work requires at least two solids.", nameof(solids));
            }

            if (isStillValid == null)
            {
                throw new ArgumentNullException(nameof(isStillValid));
            }

            if (onSuccess == null)
            {
                throw new ArgumentNullException(nameof(onSuccess));
            }

            if (onFailure == null)
            {
                throw new ArgumentNullException(nameof(onFailure));
            }

            var requestId = Interlocked.Increment(ref nextRequestId);
            contexts.Add(
                requestId,
                new MainThreadContext(
                    chunkGenerationToken,
                    isStillValid,
                    onSuccess,
                    onFailure));
            lock (workGate)
            {
                pendingWork.Enqueue(new WorkItem(
                    requestId,
                    formationStableId,
                    formationOrder,
                    chunkX,
                    chunkY,
                    solids));
            }

            workAvailable.Set();
            return requestId;
        }

        internal void CancelChunk(long chunkGenerationToken)
        {
            if (disposed)
            {
                return;
            }

            var requestIds = new List<long>();
            foreach (var pair in contexts)
            {
                if (pair.Value.ChunkGenerationToken == chunkGenerationToken)
                {
                    requestIds.Add(pair.Key);
                }
            }

            if (requestIds.Count == 0)
            {
                return;
            }

            lock (workGate)
            {
                for (var i = 0; i < requestIds.Count; i++)
                {
                    cancelledRequests.Add(requestIds[i]);
                    contexts.Remove(requestIds[i]);
                }
            }
        }

        internal bool TryApplyOneCompletion()
        {
            Completion completion;
            lock (completionGate)
            {
                if (completions.Count == 0)
                {
                    return false;
                }

                completion = completions.Dequeue();
            }

            if (!contexts.TryGetValue(completion.RequestId, out var context))
            {
                lock (workGate)
                {
                    cancelledRequests.Remove(completion.RequestId);
                }

                return true;
            }

            contexts.Remove(completion.RequestId);
            if (!context.IsStillValid())
            {
                return true;
            }

            if (completion.Result != null)
            {
                context.OnSuccess(completion.Result, completion.ElapsedMilliseconds);
            }
            else
            {
                context.OnFailure(completion.Error);
            }

            return true;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            lock (workGate)
            {
                stopping = true;
                pendingWork.Clear();
                cancelledRequests.Clear();
            }

            contexts.Clear();
            workAvailable.Set();
            if (worker.IsAlive && Thread.CurrentThread != worker)
            {
                worker.Join();
            }

            lock (completionGate)
            {
                completions.Clear();
            }

            workAvailable.Dispose();
        }

        private void WorkerLoop()
        {
            while (true)
            {
                WorkItem work = default;
                var hasWork = false;
                lock (workGate)
                {
                    if (stopping)
                    {
                        return;
                    }

                    while (pendingWork.Count > 0)
                    {
                        var candidate = pendingWork.Dequeue();
                        if (cancelledRequests.Remove(candidate.RequestId))
                        {
                            continue;
                        }

                        work = candidate;
                        hasWork = true;
                        break;
                    }
                }

                if (!hasWork)
                {
                    workAvailable.WaitOne();
                    continue;
                }

                var stopwatch = Stopwatch.StartNew();
                TopDown3DRockFormationMeshData result = null;
                string error;
                try
                {
                    TopDown3DRockFormationMeshBuilder.TryBuildUnion(
                        work.Solids,
                        out result,
                        out error);
                }
                catch (Exception exception)
                {
                    error = $"Unhandled fusion worker failure for {work.FormationStableId} " +
                        $"(formation {work.FormationOrder}, chunk {work.ChunkX},{work.ChunkY}): " +
                        $"{exception.GetType().Name}: {exception.Message}";
                }

                stopwatch.Stop();
                lock (workGate)
                {
                    if (stopping || cancelledRequests.Remove(work.RequestId))
                    {
                        continue;
                    }
                }

                lock (completionGate)
                {
                    completions.Enqueue(new Completion(
                        work.RequestId,
                        result,
                        error,
                        stopwatch.Elapsed.TotalMilliseconds));
                }
            }
        }

        private readonly struct WorkItem
        {
            public WorkItem(
                long requestId,
                string formationStableId,
                int formationOrder,
                int chunkX,
                int chunkY,
                IReadOnlyList<TopDown3DIndexedMeshData> solids)
            {
                RequestId = requestId;
                FormationStableId = formationStableId;
                FormationOrder = formationOrder;
                ChunkX = chunkX;
                ChunkY = chunkY;
                Solids = solids;
            }

            public long RequestId { get; }
            public string FormationStableId { get; }
            public int FormationOrder { get; }
            public int ChunkX { get; }
            public int ChunkY { get; }
            public IReadOnlyList<TopDown3DIndexedMeshData> Solids { get; }
        }

        private readonly struct Completion
        {
            public Completion(
                long requestId,
                TopDown3DRockFormationMeshData result,
                string error,
                double elapsedMilliseconds)
            {
                RequestId = requestId;
                Result = result;
                Error = error;
                ElapsedMilliseconds = elapsedMilliseconds;
            }

            public long RequestId { get; }
            public TopDown3DRockFormationMeshData Result { get; }
            public string Error { get; }
            public double ElapsedMilliseconds { get; }
        }

        private readonly struct MainThreadContext
        {
            public MainThreadContext(
                long chunkGenerationToken,
                Func<bool> isStillValid,
                Action<TopDown3DRockFormationMeshData, double> onSuccess,
                Action<string> onFailure)
            {
                ChunkGenerationToken = chunkGenerationToken;
                IsStillValid = isStillValid;
                OnSuccess = onSuccess;
                OnFailure = onFailure;
            }

            public long ChunkGenerationToken { get; }
            public Func<bool> IsStillValid { get; }
            public Action<TopDown3DRockFormationMeshData, double> OnSuccess { get; }
            public Action<string> OnFailure { get; }
        }
    }
}
