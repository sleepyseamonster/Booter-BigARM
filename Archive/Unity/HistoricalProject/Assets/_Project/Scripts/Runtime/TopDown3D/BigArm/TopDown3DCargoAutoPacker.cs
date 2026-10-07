using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DCargoAutoPacker
    {
        private sealed class StackRecord { public string Id; public int Quantity; public float Mass; public TopDown3DPackingPreference Preference; public int Source; }

        public static bool TryPack(TopDown3DInventoryState state, TopDown3DPackingSettings settings, out TopDown3DInventorySnapshot snapshot)
        {
            snapshot = null;
            if (state == null || settings == null || !settings.TryValidate(out _)) return false;
            var stacks = new List<StackRecord>();
            for (var i = 0; i < state.Slots.Count; i++)
            {
                var slot = state.Slots[i]; if (slot.IsEmpty) continue;
                var definition = state.ItemCatalog.GetRequiredDefinition(slot.ItemId);
                stacks.Add(new StackRecord { Id = slot.ItemId, Quantity = slot.Quantity, Mass = definition.Mass * slot.Quantity, Preference = definition.PackingPreference, Source = i });
            }
            stacks.Sort((a, b) => { var c = b.Mass.CompareTo(a.Mass); if (c != 0) return c; c = PreferenceRank(a.Preference).CompareTo(PreferenceRank(b.Preference)); if (c != 0) return c; c = string.CompareOrdinal(a.Id, b.Id); return c != 0 ? c : a.Source.CompareTo(b.Source); });
            var records = new List<TopDown3DInventorySlotSnapshot>();
            for (var i = 0; i < state.Capacity; i++) records.Add(TopDown3DInventorySlotSnapshot.Create(null, 0));
            var left = 0f; var right = 0f;
            foreach (var stack in stacks)
            {
                var best = -1; var bestScore = float.PositiveInfinity;
                for (var i = 0; i < settings.Mounts.Count && i < records.Count; i++)
                {
                    if (records[i].Quantity > 0) continue;
                    var mount = settings.Mounts[i];
                    var sideMass = mount.Side == TopDown3DMountSide.Left ? left : right;
                    var otherMass = mount.Side == TopDown3DMountSide.Left ? right : left;
                    var score = mount.Height * stack.Mass + Math.Abs((sideMass + stack.Mass) - otherMass) * 0.15f;
                    if (stack.Preference == TopDown3DPackingPreference.QuickAccess && mount.Zone != TopDown3DMountZone.Side) score += 8f;
                    if (stack.Preference == TopDown3DPackingPreference.Protected && mount.Zone != TopDown3DMountZone.Core) score += 5f;
                    if (stack.Preference == TopDown3DPackingPreference.UpperAllowed && mount.Zone == TopDown3DMountZone.Upper) score -= 1f;
                    if (score < bestScore - 0.0001f) { bestScore = score; best = i; }
                }
                if (best < 0) return false;
                records[best] = TopDown3DInventorySlotSnapshot.Create(stack.Id, stack.Quantity);
                if (settings.Mounts[best].Side == TopDown3DMountSide.Left) left += stack.Mass; else right += stack.Mass;
            }
            snapshot = TopDown3DInventorySnapshot.Create(TopDown3DInventoryState.CurrentSnapshotVersion, records);
            return true;
        }

        private static int PreferenceRank(TopDown3DPackingPreference preference)
        { return preference == TopDown3DPackingPreference.Protected ? 0 : preference == TopDown3DPackingPreference.QuickAccess ? 1 : 2; }
    }
}
