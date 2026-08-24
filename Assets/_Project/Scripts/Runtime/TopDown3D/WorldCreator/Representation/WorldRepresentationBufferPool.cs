using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public sealed class WorldRepresentationBufferPool
    {
        private readonly object gate = new object();
        private readonly Dictionary<int, Stack<WorldRepresentationBuffers>> retained =
            new Dictionary<int, Stack<WorldRepresentationBuffers>>();
        private readonly int maximumRetainedPerResolution;

        public WorldRepresentationBufferPool(int maximumRetainedPerResolution = 4)
        {
            if (maximumRetainedPerResolution < 1 || maximumRetainedPerResolution > 64)
                throw new ArgumentOutOfRangeException(nameof(maximumRetainedPerResolution));
            this.maximumRetainedPerResolution = maximumRetainedPerResolution;
        }

        public int TotalSetAllocations { get; private set; }
        public int TotalSetReuses { get; private set; }
        public int OutstandingLeases { get; private set; }
        public int RetainedSets { get; private set; }

        internal WorldRepresentationBufferLease Rent(int resolution)
        {
            lock (gate)
            {
                WorldRepresentationBuffers buffers;
                if (retained.TryGetValue(resolution, out var available) && available.Count > 0)
                {
                    buffers = available.Pop();
                    RetainedSets--;
                    TotalSetReuses++;
                }
                else
                {
                    buffers = new WorldRepresentationBuffers(resolution);
                    TotalSetAllocations++;
                }

                OutstandingLeases++;
                return new WorldRepresentationBufferLease(this, buffers);
            }
        }

        internal void Return(WorldRepresentationBuffers buffers)
        {
            lock (gate)
            {
                if (OutstandingLeases <= 0) throw new InvalidOperationException("Representation buffer lease accounting underflow.");
                OutstandingLeases--;
                if (!retained.TryGetValue(buffers.Resolution, out var available))
                {
                    available = new Stack<WorldRepresentationBuffers>();
                    retained.Add(buffers.Resolution, available);
                }

                if (available.Count < maximumRetainedPerResolution)
                {
                    buffers.ClearIdentities();
                    available.Push(buffers);
                    RetainedSets++;
                }
            }
        }
    }

    internal sealed class WorldRepresentationBuffers
    {
        public WorldRepresentationBuffers(int resolution)
        {
            Resolution = resolution;
            var vertexCount = checked(resolution * resolution);
            var indexCount = checked((resolution - 1) * (resolution - 1) * 6);
            AbsoluteA = new double[vertexCount];
            AbsoluteB = new double[vertexCount];
            Height = new float[vertexCount];
            NormalA = new float[vertexCount];
            NormalVertical = new float[vertexCount];
            NormalB = new float[vertexCount];
            LocalX = new float[vertexCount];
            LocalY = new float[vertexCount];
            LocalZ = new float[vertexCount];
            FeatureIds = new WorldFeatureId[vertexCount];
            Semantics = new WorldSurfaceSemantic[vertexCount];
            Indices = new int[indexCount];
        }

        public int Resolution { get; }
        public double[] AbsoluteA { get; }
        public double[] AbsoluteB { get; }
        public float[] Height { get; }
        public float[] NormalA { get; }
        public float[] NormalVertical { get; }
        public float[] NormalB { get; }
        public float[] LocalX { get; }
        public float[] LocalY { get; }
        public float[] LocalZ { get; }
        public WorldFeatureId[] FeatureIds { get; }
        public WorldSurfaceSemantic[] Semantics { get; }
        public int[] Indices { get; }

        public long EstimatedBytes =>
            (long)AbsoluteA.Length * sizeof(double) * 2L
            + (long)Height.Length * sizeof(float) * 7L
            + (long)FeatureIds.Length * sizeof(ulong) * 2L
            + (long)Semantics.Length * sizeof(ushort)
            + (long)Indices.Length * sizeof(int);

        public void ClearIdentities()
        {
            Array.Clear(FeatureIds, 0, FeatureIds.Length);
            Array.Clear(Semantics, 0, Semantics.Length);
        }
    }

    internal sealed class WorldRepresentationBufferLease : IDisposable
    {
        private WorldRepresentationBufferPool owner;

        public WorldRepresentationBufferLease(WorldRepresentationBufferPool owner, WorldRepresentationBuffers buffers)
        {
            this.owner = owner;
            Buffers = buffers;
        }

        public WorldRepresentationBuffers Buffers { get; private set; }

        public void Dispose()
        {
            if (owner == null) return;
            var returning = Buffers;
            Buffers = null;
            var pool = owner;
            owner = null;
            pool.Return(returning);
        }
    }
}
