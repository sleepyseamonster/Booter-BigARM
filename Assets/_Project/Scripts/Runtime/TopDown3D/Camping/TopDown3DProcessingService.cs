using System;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DProcessingService
    {
        public sealed class Quote
        {
            private readonly TopDown3DInventoryTransaction.Mutation source, tray;
            private readonly TopDown3DPreparedWorldMutation operation;
            private readonly TopDown3DIronstoneProcessor processor;
            private readonly string fingerprint;
            private readonly Func<bool> access;
            private bool spent;
            internal Quote(TopDown3DIronstoneProcessor station, TopDown3DInventoryTransaction.Mutation input,
                TopDown3DInventoryTransaction.Mutation output, TopDown3DPreparedWorldMutation next, string signature, Func<bool> canAccess)
            { processor = station; source = input; tray = output; operation = next; fingerprint = signature; access = canAccess; }
            public TopDown3DInventoryResult Commit()
            {
                if (spent) return TopDown3DCraftingService.Failure("Process quote was already used. Prepare a new batch.");
                spent = true;
                return TopDown3DInventoryTransaction.Commit(new[] { source, tray },
                    () => processor != null && processor.IsAvailable && ReferenceEquals(processor.Output.State, tray.State)
                        && access() && processor.Recipe.TryFingerprint(source.State.ItemCatalog, out var now, out _)
                        && now == fingerprint, new[] { operation });
            }
        }
        public static TopDown3DInventoryResult Prepare(TopDown3DIronstoneProcessor processor, TopDown3DInventoryState source,
            Func<bool> access, out Quote quote)
        {
            quote = null;
            if (processor == null || !processor.IsAvailable || source == null || source == processor.Output.State
                || source.IsCommitting || processor.Output.State.IsCommitting || source.Revision == long.MaxValue
                || processor.Output.State.Revision == long.MaxValue || source.ItemCatalog != processor.Output.State.ItemCatalog
                || access == null || !access()) return TopDown3DCraftingService.Failure("Processor or selected material source is unavailable.");
            if (!processor.Recipe.TryFingerprint(source.ItemCatalog, out var signature, out var error)) return TopDown3DCraftingService.Failure(error);
            var input = new TopDown3DInventoryTransaction.Mutation(source);
            var output = new TopDown3DInventoryTransaction.Mutation(processor.Output.State);
            foreach (var ingredient in processor.Recipe.Inputs)
            { var result = input.Draft.TryRemove(ingredient.itemId, ingredient.quantity); if (!result.Succeeded) return result; }
            foreach (var product in processor.Recipe.Outputs)
            { var result = output.Draft.TryAdd(new TopDown3DItemAmount(product.itemId, product.quantity)); if (!result.Succeeded) return result; }
            var operation = processor.PrepareAdvance();
            if (operation == null) return TopDown3DCraftingService.Failure("Processor revision is unavailable.");
            quote = new Quote(processor, input, output, operation, signature, access);
            return new TopDown3DInventoryResult(TopDown3DInventoryResultCode.Success);
        }
    }
}
