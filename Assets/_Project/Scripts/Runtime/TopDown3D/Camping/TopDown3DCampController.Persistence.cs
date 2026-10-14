using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DCampController
    {
        internal TopDown3DPreparedWorldMutation PrepareTrialRestore(TopDown3DCampRecord record, bool deployed)
        {
            // Loadout trials begin with the portable kit packed. Authored Starting Camp is a different owner.
            if (deployed || !CanRestoreCamp(record, false)) return null;
            var previous = Camp; var previousRecord = Record;
            return new TopDown3DPreparedWorldMutation(
                () => { if (committing || Phase != TopDown3DCampPhase.Idle || Camp != previous || Record != previousRecord) return false; committing = true; return true; },
                () => { Record = null; Camp = null; },
                () => { if (previous != null) DestroyOwned(previous.gameObject); }, () => committing = false);
        }
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        { if (Record != null && string.IsNullOrEmpty(Record.id)) Record = null; }
        internal bool CanRestoreCamp(TopDown3DCampRecord record, bool deployed)
        {
            return IsConfigured && Phase == TopDown3DCampPhase.Idle && !committing
                && (!deployed || record != null && Guid.TryParseExact(record.id, "N", out _)
                    && GreaterWastelandInventorySnapshot.Finite(record.position)
                    && GreaterWastelandInventorySnapshot.ValidRotation(record.rotation)
                    && TopDown3DCampPlacement.Validate(record.position, record.rotation, settings,
                        transform, Camp != null ? Camp.transform : null, out _, out _));
        }
        internal bool RestoreCamp(TopDown3DCampRecord record, bool deployed)
        {
            if (!CanRestoreCamp(record, deployed)) return false;
            GameObject candidate = null;
            if (deployed)
            {
                candidate = Instantiate(settings.deployedPrefab, worldRoot);
                candidate.SetActive(false);
                candidate.transform.SetPositionAndRotation(record.position, record.rotation);
                candidate.GetComponent<TopDown3DCampView>().Configure(this, settings, record);
            }
            var previous = Camp;
            Record = deployed ? record : null;
            Camp = candidate != null ? candidate.GetComponent<TopDown3DCampView>() : null;
            if (previous != null) DestroyOwned(previous.gameObject);
            if (candidate != null) candidate.SetActive(true);
            return true;
        }
    }
}
