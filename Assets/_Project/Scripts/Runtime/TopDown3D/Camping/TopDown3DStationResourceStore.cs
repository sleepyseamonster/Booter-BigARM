using System;
using System.Globalization;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DStationResourceStore : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable] private sealed class Envelope
        {
            public int version;
            public bool present;
            public string world, site, member;
            public string epoch, operationRevision;
            public TopDown3DInventorySnapshot inventory;
        }
        [SerializeField] private string memberId = "camp-loop:ironstone-output:v1";
        [SerializeField, HideInInspector] private string retainedJson;
        [NonSerialized] private TopDown3DInventoryState state;
        [NonSerialized] private TopDown3DIronstoneProcessor processor;
        [NonSerialized] private TopDown3DCampLoopEconomySession session;
        [NonSerialized] private bool recovery;
        public string StableId => memberId;
        public TopDown3DInventoryState State => state;
        public bool Ready => state != null && !recovery && isActiveAndEnabled;
        public string FailureReason { get; private set; }
        internal bool WasFresh { get; private set; }
        internal bool Bind(TopDown3DCampLoopEconomySession owner, TopDown3DIronstoneProcessor station, string world, string site)
        {
            if (recovery) return false;
            if (state != null) return ReferenceEquals(session, owner) && ReferenceEquals(processor, station);
            session = owner; processor = station;
            var prepared = new TopDown3DInventoryState(owner.Kit.ItemCatalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.StationResources, 8));
            WasFresh = string.IsNullOrEmpty(retainedJson);
            if (!WasFresh)
            {
                try
                {
                    var record = JsonUtility.FromJson<Envelope>(retainedJson);
                    if (record == null || record.version != 1 || !record.present || record.world != world || record.site != site || record.member != memberId
                        || !uint.TryParse(record.epoch, NumberStyles.None, CultureInfo.InvariantCulture, out var savedEpoch) || savedEpoch != owner.Epoch
                        || !long.TryParse(record.operationRevision, NumberStyles.None, CultureInfo.InvariantCulture, out var savedRevision) || record.inventory == null
                        || !prepared.ApplySnapshot(record.inventory).Succeeded) return Fail("Retained processor tray is incompatible; its data was preserved.");
                    processor.RestoreRevision(savedRevision);
                }
                catch (Exception error) { return Fail("Retained processor tray could not load: " + error.Message); }
            }
            state = prepared; return true;
        }
        private bool Fail(string reason) { recovery = true; FailureReason = reason; return false; }
        internal TopDown3DInventorySnapshot EmptyBaseline() => new TopDown3DInventoryState(state.ItemCatalog, state.Policy).CaptureSnapshot();
        public void OnBeforeSerialize()
        {
            if (Application.isPlaying) CaptureRetained();
        }
        internal string CaptureRetained()
        {
            if (!Ready || processor == null || session == null || session.Site == null
                || state.IsCommitting || processor.IsCommitting || session.IsCommitting) return retainedJson;
            retainedJson = JsonUtility.ToJson(new Envelope { version = 1, present = true, world = session.Site.WorldId, site = session.Site.SiteId,
                member = memberId, epoch = session.Epoch.ToString(CultureInfo.InvariantCulture),
                operationRevision = processor.Revision.ToString(CultureInfo.InvariantCulture), inventory = state.CaptureSnapshot() });
            return retainedJson;
        }
        public void OnAfterDeserialize() { state = null; processor = null; session = null; recovery = false; }
    }
}
