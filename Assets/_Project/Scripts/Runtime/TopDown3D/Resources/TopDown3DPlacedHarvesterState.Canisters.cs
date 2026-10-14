using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlacedHarvesterState
    {
        // Placement identity has always used the production scene GUID; checkpoint identity is a separate contract.
        public const string AuthoredWorldIdentity = "4de5dd018ee194314a00fd369e2d3eeb";
        [SerializeField] private bool retainCanisterContents;
        public bool RetainsCanisterContents => retainCanisterContents && IsAuthored && TopDown3DAnimationPlaygroundSession.IsSessionOnly(gameObject.scene);
        internal void ConfigureCanisterContents(bool enabled)
        {
            if (enabled && (!IsAuthored || authoredWorldId != AuthoredWorldIdentity
                || !TopDown3DAnimationPlaygroundSession.IsSessionOnly(gameObject.scene)))
                throw new InvalidOperationException("Contents require the fixed authored-world owner.");
            retainCanisterContents = enabled;
        }

        public bool TryPlaceCanister(Vector3 position, TopDown3DInventoryState source, int index, Func<bool> access, out string placementId)
        {
            placementId = null;
            if (!RetainsCanisterContents || resetLocked || HasRetainedRecovery || settings == null || source == null || index < 0 || index >= source.Capacity
                || !source.Policy.SupportsCanisterContents || records.Count >= settings.MaximumDeployed || nextSequence == long.MaxValue
                || access == null || !access() || !GreaterWastelandInventorySnapshot.Finite(position)) return false;
            var selected = source.Slots[index]; var contents = selected.CanisterContents;
            if (selected.ItemId != TopDown3DHarvesterSettings.CanisterItemId || selected.Quantity != 1
                || !Guid.TryParseExact(selected.InstanceId, "N", out _) || !contents.IsValid(settings.Capacity, settings.SecondsPerUnit)) return false;
            var snapshot = CaptureSnapshot(); var next = new List<TopDown3DPlacedHarvesterRecord>();
            foreach (var record in snapshot.Records)
            {
                if (record.EquipmentInstanceId == selected.InstanceId) return false;
                next.Add(record);
            }
            var id = AuthoredPlacementId(nextSequence);
            // Carried time is dormant. Rebase to now and carry only the fractional collection remainder.
            next.Add(TopDown3DPlacedHarvesterRecord.Create(id, nextSequence,
                new AbsoluteWorldPosition(position.x, position.y, position.z), contents.dust, contents.fractionalSeconds,
                simulationSeconds, selected.InstanceId));
            var preparedSnapshot = TopDown3DPlacedHarvesterSnapshot.Create(0, simulationSeconds, nextSequence + 1, next, authoredWorldId);
            var worldMutation = PrepareTrialRestore(preparedSnapshot); if (worldMutation == null) return false;
            var inventory = new TopDown3DInventoryTransaction.Mutation(source);
            if (!inventory.Draft.RemoveFromSlot(index, 1).Succeeded) return false;
            var result = TopDown3DInventoryTransaction.Commit(new[] { inventory },
                () => RetainsCanisterContents && access(), new[] { worldMutation });
            if (!result.Succeeded) return false;
            placementId = id; return true;
        }

        private bool TryPickupContents(string id, TopDown3DInventoryState kit, TopDown3DInventoryState cargo,
            out int collectedDust, Func<bool> cargoAccess)
        {
            collectedDust = 0; LastPickupFailure = null;
            if (resetLocked || settings == null || !TryGet(id, out var record)) return false;
            var contents = TopDown3DCanisterContents.Create(record.Dust, record.FractionalSeconds);
            var amount = new TopDown3DItemAmount(TopDown3DHarvesterSettings.CanisterItemId, 1, record.EquipmentInstanceId, contents);
            var destination = contents.dust == 0 && kit != null && kit.CheckAdd(amount).Succeeded ? kit : cargo;
            var needsCargo = ReferenceEquals(destination, cargo);
            if (destination == null || needsCargo && (cargoAccess == null || !cargoAccess()))
            { LastPickupFailure = "Bring the Legger closer; the canister keeps its dust."; return false; }
            var inventory = new TopDown3DInventoryTransaction.Mutation(destination);
            var added = inventory.Draft.TryAdd(amount);
            if (!added.Succeeded) { LastPickupFailure = added.Message; return false; }
            var snapshot = CaptureSnapshot(); var next = new List<TopDown3DPlacedHarvesterRecord>();
            foreach (var existing in snapshot.Records) if (existing.StableId != id) next.Add(existing);
            var prepared = PrepareTrialRestore(TopDown3DPlacedHarvesterSnapshot.Create(0, simulationSeconds, nextSequence, next, authoredWorldId));
            if (prepared == null) return false;
            var result = TopDown3DInventoryTransaction.Commit(new[] { inventory },
                () => RetainsCanisterContents && (!needsCargo || cargoAccess()), new[] { prepared });
            if (!result.Succeeded) { LastPickupFailure = result.Message; return false; }
            collectedDust = contents.dust; return true;
        }
    }
}
