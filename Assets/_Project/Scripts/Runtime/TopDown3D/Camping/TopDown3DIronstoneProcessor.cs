using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent, RequireComponent(typeof(TopDown3DStationResourceStore))]
    public sealed class TopDown3DIronstoneProcessor : MonoBehaviour, ITopDown3DInteractable
    {
        [SerializeField] private string stationId = "camp-loop:ironstone-processor:v1";
        [SerializeField] private TopDown3DProcessRecipe recipe;
        private long revision;
        private bool locked;
        public long Revision => revision;
        public bool IsCommitting => locked;
        public TopDown3DProcessRecipe Recipe => recipe;
        public TopDown3DStationResourceStore Output => GetComponent<TopDown3DStationResourceStore>();
        public string StableId => stationId;
        public UnityEngine.Object UnityObject => this;
        public string Prompt => "Sort Ironstone";
        public Vector3 InteractionPoint => transform.position + Vector3.up * .8f;
        public float InteractionRange => 2.8f;
        public float ActionDuration => .01f;
        public TopDown3DItemAmount Reward => default;
        public bool IsAvailable => isActiveAndEnabled && recipe != null && Output.Ready && revision >= 0 && revision < long.MaxValue;
        public bool TryConsume(out int remainingUses) { remainingUses = 1; return false; }
        public bool CanAccess(Transform actor) => IsAvailable && actor != null && actor.gameObject.scene == gameObject.scene
            && Vector3.Distance(actor.position, InteractionPoint) <= InteractionRange && TopDown3DCargoAccess.HasLineOfSight(actor, transform);
        internal void Configure(TopDown3DProcessRecipe value) => recipe = value;
        internal void RestoreRevision(long value) => revision = value;
        internal TopDown3DPreparedWorldMutation PrepareAdvance()
        {
            if (locked || revision == long.MaxValue) return null;
            var expected = revision; var next = checked(expected + 1);
            return new TopDown3DPreparedWorldMutation(() => { if (locked || revision != expected) return false; locked = true; return true; },
                () => revision = next, () => { }, () => locked = false);
        }
    }
}
