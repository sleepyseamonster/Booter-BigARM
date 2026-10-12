using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInventoryState
    {
        private bool transactionLocked;
        public long Revision { get; private set; }
        public bool IsCommitting => transactionLocked;
        public bool IsInitialized => catalog != null && slots != null && policy.Capacity > 0;

        public TopDown3DInventoryResult CheckAdd(TopDown3DItemAmount amount)
        {
            return SimulateAdd(new[] { amount }, out _, out var result) ? Success() : result;
        }

        public int GetMaxAddable(string itemId, int requestedQuantity)
        {
            var low = 0;
            var high = Math.Max(0, requestedQuantity);
            while (low < high)
            {
                var candidate = low + (high - low + 1) / 2;
                if (CanAdd(new TopDown3DItemAmount(itemId, candidate))) low = candidate;
                else high = candidate - 1;
            }
            return low;
        }

        internal TopDown3DInventoryState CreateDraft()
        {
            var draft = new TopDown3DInventoryState(catalog, policy);
            var result = draft.ApplySnapshot(CaptureSnapshot());
            if (!result.Succeeded) throw new InvalidOperationException(result.Message);
            return draft;
        }

        internal void InstallDraft(TopDown3DInventoryState draft)
        {
            for (var i = 0; i < slots.Count; i++) slots[i].Set(draft.slots[i].ItemId, draft.slots[i].Quantity);
            Revision++;
        }

        internal void PublishDraft()
        {
            // A committed reward cannot be reported as failed because a presentation observer threw.
            var observers = Changed;
            if (observers == null) return;
            var alreadyLocked = transactionLocked;
            transactionLocked = true;
            try
            {
                foreach (Action observer in observers.GetInvocationList())
                {
                    try { observer(); }
                    catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
                }
            }
            finally { transactionLocked = alreadyLocked; }
        }
        internal void RetainForRecovery() => transactionLocked = true;
        internal TopDown3DInventoryResult RemoveFromSlot(int index, int quantity)
        {
            if (index < 0 || index >= slots.Count || quantity <= 0 || slots[index].Quantity < quantity)
                return new TopDown3DInventoryResult(TopDown3DInventoryResultCode.InvalidSlot, "Selected stack changed.");
            slots[index].Set(slots[index].ItemId, slots[index].Quantity - quantity);
            Revision++;
            return Success();
        }
        internal bool TryLock(long expectedRevision)
        {
            if (transactionLocked || Revision != expectedRevision) return false;
            transactionLocked = true;
            return true;
        }
        internal void Unlock() => transactionLocked = false;
    }

    /// <summary>Prepared inventories become visible together before any notification is published.</summary>
    public static class TopDown3DInventoryTransaction
    {
        internal sealed class Mutation
        {
            internal readonly TopDown3DInventoryState State;
            internal readonly TopDown3DInventoryState Draft;
            internal readonly long Revision;
            internal Mutation(TopDown3DInventoryState state)
            { State = state; Revision = state.Revision; Draft = state.CreateDraft(); }
        }

        internal static TopDown3DInventoryResult Commit(IReadOnlyList<Mutation> mutations, Func<bool> condition = null)
        {
            var locked = new List<TopDown3DInventoryState>();
            try
            {
                foreach (var mutation in mutations)
                {
                    if (locked.Contains(mutation.State) || !mutation.State.TryLock(mutation.Revision))
                        return new TopDown3DInventoryResult(TopDown3DInventoryResultCode.MutationBusy, "Inventory changed; retry the operation.");
                    locked.Add(mutation.State);
                }
                if (condition != null && !condition())
                    return new TopDown3DInventoryResult(TopDown3DInventoryResultCode.CommitConditionRejected, "The world operation is no longer available.");
                foreach (var mutation in mutations) mutation.State.InstallDraft(mutation.Draft);
                foreach (var mutation in mutations) mutation.State.PublishDraft();
                return new TopDown3DInventoryResult(TopDown3DInventoryResultCode.Success);
            }
            finally { foreach (var state in locked) state.Unlock(); }
        }

        public static TopDown3DInventoryResult AddRewards(TopDown3DInventoryState first,
            IReadOnlyList<TopDown3DItemAmount> firstRewards, TopDown3DInventoryState second,
            IReadOnlyList<TopDown3DItemAmount> secondRewards, Func<bool> condition = null)
        {
            if (first == null || (secondRewards != null && secondRewards.Count > 0 && second == null))
                return new TopDown3DInventoryResult(TopDown3DInventoryResultCode.DestinationUnavailable, "The required carrier is unavailable.");
            var mutations = new List<Mutation>();
            var a = new Mutation(first);
            var result = a.Draft.TryAdd(firstRewards);
            if (!result.Succeeded) return result;
            mutations.Add(a);
            if (secondRewards != null && secondRewards.Count > 0)
            {
                var b = first == second ? a : new Mutation(second);
                result = b.Draft.TryAdd(secondRewards);
                if (!result.Succeeded) return result;
                if (b != a) mutations.Add(b);
            }
            return Commit(mutations, condition);
        }
    }
}
