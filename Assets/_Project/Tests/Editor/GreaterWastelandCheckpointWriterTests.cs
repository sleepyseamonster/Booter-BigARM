using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BooterBigArm.TopDown3D;
using NUnit.Framework;

namespace BooterBigArm.Tests
{
    public sealed class GreaterWastelandCheckpointWriterTests
    {
        [Test]
        public void WriterSerializesWritesCoalescesPendingAndAcknowledgesCapturedRevisions()
        {
            using var started = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var writes = new List<string>();
            int concurrent = 0, maximumConcurrent = 0;
            var writer = new GreaterWastelandCheckpointWriter("unused", (path, text) =>
            {
                int current = Interlocked.Increment(ref concurrent);
                maximumConcurrent = Math.Max(maximumConcurrent, current);
                if (text == "first") { started.Set(); if (!release.Wait(2000)) throw new TimeoutException("Fixture worker was not released."); }
                writes.Add(text); Interlocked.Decrement(ref concurrent);
            });
            try
            {
                Assert.That(writer.Queue(Request("first", 1)), Is.True);
                Assert.That(started.Wait(2000), Is.True);
                Assert.That(writer.Queue(Request("superseded", 2)), Is.True);
                Assert.That(writer.Queue(Request("latest", 3)), Is.True);
                Assert.That(writer.Poll(out _, out _), Is.False, "Queued is not durably completed.");
                release.Set();
                Assert.That(writer.Drain(out var completed, out var error), Is.True);
                Assert.That(error, Is.Null); Assert.That(completed.Text, Is.EqualTo("latest"));
                Assert.That(completed.KitRevision, Is.EqualTo(3));
                Assert.That(completed.CargoRevision, Is.EqualTo(13)); Assert.That(completed.CacheRevision, Is.EqualTo(23));
                Assert.That(writes, Is.EqualTo(new[] { "first", "latest" }));
                Assert.That(maximumConcurrent, Is.EqualTo(1)); Assert.That(writer.Busy, Is.False);
            }
            finally { release.Set(); writer.Drain(out _, out _); }
        }

        [Test]
        public void WorkerFailureStopsPendingWritesAndReportsFailure()
        {
            int attempts = 0;
            var writer = new GreaterWastelandCheckpointWriter("unused", (path, text) =>
            { Interlocked.Increment(ref attempts); throw new IOException("Synthetic disk failure."); });
            writer.Queue(Request("first", 1)); writer.Queue(Request("pending", 2));
            Assert.That(writer.Drain(out _, out var error), Is.False);
            Assert.That(error, Is.TypeOf<IOException>());
            Assert.That(attempts, Is.EqualTo(1)); Assert.That(writer.Failed, Is.True);
            Assert.That(writer.Queue(Request("must not retry", 3)), Is.False);
        }

        [Test]
        public void DetachedEncodingPreservesFileFormatBackupAndInterruptedWriteProtection()
        {
            var path = Path.Combine(Path.GetTempPath(), "GreaterWastelandWriterTests", Guid.NewGuid().ToString("N"), "checkpoint.json");
            var snapshot = new GreaterWastelandInventorySnapshot { version = 1, worldId = GreaterWastelandInventorySnapshot.WorldIdentity };
            var first = GreaterWastelandInventoryFile.Encode(snapshot);
            var writer = new GreaterWastelandCheckpointWriter(path);
            writer.Queue(Request(first, 1));
            snapshot.worldId = "changed after capture";
            Assert.That(writer.Drain(out _, out _), Is.True);
            Assert.That(GreaterWastelandInventoryFile.Read(path, out var loaded), Is.True);
            Assert.That(loaded.worldId, Is.EqualTo(GreaterWastelandInventorySnapshot.WorldIdentity));
            var second = GreaterWastelandInventoryFile.Encode(snapshot);
            writer.Queue(Request(second, 2)); Assert.That(writer.Drain(out _, out _), Is.True);
            Assert.That(File.ReadAllText(path + ".backup"), Is.EqualTo(first));
            File.WriteAllText(path + ".pending", "retained interrupted checkpoint");
            writer.Queue(Request(first, 3)); Assert.That(writer.Drain(out _, out var error), Is.False);
            Assert.That(error, Is.TypeOf<IOException>());
            Assert.That(File.ReadAllText(path), Is.EqualTo(second));
            Assert.That(File.ReadAllText(path + ".pending"), Is.EqualTo("retained interrupted checkpoint"));
            // Small fixture artifacts are retained; the production save directory is never accessed.
        }

        private static GreaterWastelandCheckpointWriter.Checkpoint Request(string text, long revision) => new(text, revision, revision + 10, revision + 20);
    }
}
