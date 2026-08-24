using System;
using System.Threading;
using System.Threading.Tasks;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public interface IWorldRepresentationCompiler
    {
        Task<WorldRepresentationBuildResult> BuildAsync(
            WorldRepresentationKey key,
            CancellationToken cancellationToken);
    }

    public sealed class WorldRepresentationCompiler : IWorldRepresentationCompiler
    {
        private readonly IWorldQueryService query;
        private readonly WorldRepresentationBufferPool pool;
        private readonly WorldRepresentationBuildProfile profile;
        private readonly WorldFeatureId sourceFingerprint;

        public WorldRepresentationCompiler(
            IWorldQueryService query,
            WorldRepresentationBufferPool pool,
            WorldRepresentationBuildProfile profile,
            WorldFeatureId sourceFingerprint)
        {
            this.query = query ?? throw new ArgumentNullException(nameof(query));
            this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
            this.profile = profile;
            if (sourceFingerprint.IsEmpty) throw new ArgumentException("Representations require a canonical source fingerprint.", nameof(sourceFingerprint));
            this.sourceFingerprint = sourceFingerprint;
        }

        public Task<WorldRepresentationBuildResult> BuildAsync(
            WorldRepresentationKey key,
            CancellationToken cancellationToken)
        {
            return Task.Run(() => Build(key, cancellationToken), cancellationToken);
        }

        private WorldRepresentationBuildResult Build(
            WorldRepresentationKey key,
            CancellationToken cancellationToken)
        {
            var resolution = profile.ResolutionFor(key.Tier);
            var lease = pool.Rent(resolution);
            try
            {
                var buffers = lease.Buffers;
                var step = key.TileSpan / (resolution - 1);
                for (var z = 0; z < resolution; z++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    for (var x = 0; x < resolution; x++)
                    {
                        var index = z * resolution + x;
                        var absoluteA = key.Minimum.HorizontalA + x * step;
                        var absoluteB = key.Minimum.HorizontalB + z * step;
                        if (!query.TrySampleSurface(
                                new AbsoluteWorldPosition(absoluteA, 0d, absoluteB),
                                out var sample,
                                out var error))
                        {
                            throw new InvalidOperationException(error);
                        }

                        buffers.AbsoluteA[index] = absoluteA;
                        buffers.AbsoluteB[index] = absoluteB;
                        buffers.Height[index] = checked((float)sample.Position.Vertical);
                        buffers.NormalA[index] = sample.NormalA;
                        buffers.NormalVertical[index] = sample.NormalVertical;
                        buffers.NormalB[index] = sample.NormalB;
                        buffers.FeatureIds[index] = sample.DominantFeatureId;
                        buffers.Semantics[index] = sample.Semantic;
                    }
                }

                var triangle = 0;
                for (var z = 0; z < resolution - 1; z++)
                {
                    for (var x = 0; x < resolution - 1; x++)
                    {
                        var bottomLeft = z * resolution + x;
                        var topLeft = bottomLeft + resolution;
                        buffers.Indices[triangle++] = bottomLeft;
                        buffers.Indices[triangle++] = topLeft;
                        buffers.Indices[triangle++] = bottomLeft + 1;
                        buffers.Indices[triangle++] = bottomLeft + 1;
                        buffers.Indices[triangle++] = topLeft;
                        buffers.Indices[triangle++] = topLeft + 1;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                return new WorldRepresentationBuildResult(key, sourceFingerprint, lease);
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }
    }

    public sealed class WorldRepresentationBuildResult : IDisposable
    {
        private WorldRepresentationBufferLease lease;

        internal WorldRepresentationBuildResult(
            WorldRepresentationKey key,
            WorldFeatureId sourceFingerprint,
            WorldRepresentationBufferLease lease)
        {
            Key = key;
            SourceFingerprint = sourceFingerprint;
            this.lease = lease ?? throw new ArgumentNullException(nameof(lease));
            if (!key.IncludesCollision && HasCollision)
                throw new InvalidOperationException("Only near representations may carry collision authority.");
        }

        public WorldRepresentationKey Key { get; }
        public WorldFeatureId SourceFingerprint { get; }
        public int Resolution => RequireBuffers().Resolution;
        public int VertexCount => RequireBuffers().Height.Length;
        public int IndexCount => RequireBuffers().Indices.Length;
        public long EstimatedBytes => RequireBuffers().EstimatedBytes;
        public bool HasCollision => Key.Tier == WorldRepresentationTier.Near;
        public bool HasLocalFrame { get; private set; }
        public LocalOriginFrame LocalFrame { get; private set; }
        public bool IsDisposed => lease == null;

        public float GetHeight(int vertexIndex) => RequireBuffers().Height[vertexIndex];
        public int GetIndex(int index) => RequireBuffers().Indices[index];
        public WorldSurfaceSemantic GetSemantic(int vertexIndex) => RequireBuffers().Semantics[vertexIndex];
        public void GetNormal(
            int vertexIndex,
            out float normalA,
            out float normalVertical,
            out float normalB)
        {
            var buffers = RequireBuffers();
            normalA = buffers.NormalA[vertexIndex];
            normalVertical = buffers.NormalVertical[vertexIndex];
            normalB = buffers.NormalB[vertexIndex];
        }
        public WorldFeatureId GetFeatureId(int vertexIndex) => RequireBuffers().FeatureIds[vertexIndex];
        public AbsoluteWorldPosition GetAbsolutePosition(int vertexIndex)
        {
            var buffers = RequireBuffers();
            return new AbsoluteWorldPosition(buffers.AbsoluteA[vertexIndex], buffers.Height[vertexIndex], buffers.AbsoluteB[vertexIndex]);
        }

        public LocalWorldPosition GetLocalPosition(int vertexIndex)
        {
            var buffers = RequireBuffers();
            if (!HasLocalFrame) throw new InvalidOperationException("The representation has not been integrated into a local origin frame.");
            return new LocalWorldPosition(buffers.LocalX[vertexIndex], buffers.LocalY[vertexIndex], buffers.LocalZ[vertexIndex]);
        }

        public bool CanRebase(LocalOriginFrame frame)
        {
            var buffers = RequireBuffers();
            for (var i = 0; i < buffers.Height.Length; i++)
            {
                if (!frame.TryToLocal(
                        new AbsoluteWorldPosition(buffers.AbsoluteA[i], buffers.Height[i], buffers.AbsoluteB[i]),
                        out _)) return false;
            }

            return true;
        }

        public bool TryRebase(LocalOriginFrame frame)
        {
            if (!CanRebase(frame)) return false;
            var buffers = RequireBuffers();
            for (var i = 0; i < buffers.Height.Length; i++)
            {
                var local = frame.ToLocal(new AbsoluteWorldPosition(buffers.AbsoluteA[i], buffers.Height[i], buffers.AbsoluteB[i]));
                buffers.LocalX[i] = local.X;
                buffers.LocalY[i] = local.Y;
                buffers.LocalZ[i] = local.Z;
            }

            LocalFrame = frame;
            HasLocalFrame = true;
            return true;
        }

        public void Dispose()
        {
            if (lease == null) return;
            lease.Dispose();
            lease = null;
            HasLocalFrame = false;
        }

        private WorldRepresentationBuffers RequireBuffers()
        {
            if (lease == null || lease.Buffers == null) throw new ObjectDisposedException(nameof(WorldRepresentationBuildResult));
            return lease.Buffers;
        }
    }
}
