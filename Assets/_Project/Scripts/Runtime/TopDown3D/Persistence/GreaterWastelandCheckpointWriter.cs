using System;
using System.Threading.Tasks;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Main-thread queue/poll/drain, one filesystem worker and at most one latest pending checkpoint.</summary>
    internal sealed class GreaterWastelandCheckpointWriter
    {
        internal sealed class Checkpoint
        {
            public readonly string Text;
            public readonly long KitRevision, CargoRevision, CacheRevision;
            public Checkpoint(string text, long kit, long cargo, long cache)
            { Text = text ?? throw new ArgumentNullException(nameof(text)); KitRevision = kit; CargoRevision = cargo; CacheRevision = cache; }
        }

        private readonly string path;
        private readonly Action<string, string> write;
        private Task task;
        private Checkpoint active, pending;
        public bool Busy => task != null;
        public bool Failed { get; private set; }
        public GreaterWastelandCheckpointWriter(string path, Action<string, string> write = null)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.write = write ?? GreaterWastelandInventoryFile.WriteEncoded;
        }

        public bool Queue(Checkpoint checkpoint)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            if (Failed) return false;
            if (task == null) Start(checkpoint);
            else pending = checkpoint;
            return true;
        }

        public bool Poll(out Checkpoint completed, out Exception error)
        {
            completed = null; error = null;
            if (task == null || !task.IsCompleted) return false;
            completed = active;
            try { task.GetAwaiter().GetResult(); }
            catch (Exception exception) { error = exception; Failed = true; }
            task = null; active = null;
            if (Failed) pending = null;
            else if (pending != null)
            {
                var next = pending; pending = null; Start(next);
            }
            return true;
        }

        // Explicit saves/shutdown retain the old durable, synchronous completion contract.
        public bool Drain(out Checkpoint completed, out Exception error)
        {
            completed = null; error = null;
            while (task != null)
            {
                try { task.GetAwaiter().GetResult(); }
                catch (Exception) { /* Poll observes and returns the same failure on the main thread. */ }
                Poll(out var result, out error);
                if (error != null) return false;
                completed = result;
            }
            return !Failed;
        }

        private void Start(Checkpoint checkpoint)
        {
            active = checkpoint;
            // Capture locals: the worker never touches queue state, participants or Unity APIs.
            var text = checkpoint.Text; var destination = path; var action = write;
            task = Task.Run(() => action(destination, text));
        }
    }
}
