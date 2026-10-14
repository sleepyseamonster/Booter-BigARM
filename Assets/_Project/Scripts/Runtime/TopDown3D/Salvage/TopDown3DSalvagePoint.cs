using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DSalvagePoint : MonoBehaviour, ITopDown3DInteractable, ITopDown3DTargetEpoch, ITopDown3DTargetColliderOwner
    {
        [SerializeField] private TopDown3DSalvageWreckState state;
        [SerializeField] private CampLoopSalvageSession session;
        [SerializeField] private int index;
        [SerializeField] private float duration = 2f;
        [SerializeField] private string prompt;
        [SerializeField] private Collider targetCollider;
        [SerializeField] private GameObject removable;
        public Object UnityObject => this;
        public string StableId => state != null ? state.StableId + ":" + new[] { "armour", "wiring", "drive" }[Mathf.Clamp(index, 0, 2)] : string.Empty;
        public uint Epoch => state != null ? state.Epoch : 0;
        public string Prompt => prompt;
        public Vector3 InteractionPoint => transform.position;
        public float InteractionRange => 2.2f;
        public float ActionDuration => duration;
        public TopDown3DItemAmount Reward => index >= 0 && index < 3 ? new(TopDown3DSalvageWreckState.ItemIds[index], TopDown3DSalvageWreckState.Quantities[index]) : default;
        public bool IsAvailable => isActiveAndEnabled && state != null && state.IsValid && !state.IsRemoved(index)
            && session != null && session.Ready;
        public bool OwnsCollider(Collider collider) => targetCollider != null && collider == targetCollider;
        public bool TryConsume(out int remainingUses)
        {
            remainingUses = 0;
            return IsAvailable && state.TryConsume(index);
        }
        public void Configure(TopDown3DSalvageWreckState owner, CampLoopSalvageSession lab, int part, float seconds, string text, Collider collider, GameObject geometry)
        { state = owner; session = lab; index = part; duration = seconds; prompt = text; targetCollider = collider; removable = geometry; }
        private void LateUpdate() => RefreshPresentation();
        public void RefreshPresentation()
        {
            if (state != null && removable != null && removable.activeSelf == state.IsRemoved(index))
                removable.SetActive(!state.IsRemoved(index));
        }
    }
}
