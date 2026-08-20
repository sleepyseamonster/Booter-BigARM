using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DBigArmCargoAccess : MonoBehaviour, ITopDown3DInteractable
    {
        [SerializeField] private TopDown3DBigArmCargo cargo;
        public Object UnityObject => this;
        public string StableId => "bigarm.cargo";
        public string Prompt => "Open BigARM Packing";
        public Vector3 InteractionPoint => transform.position;
        public float InteractionRange => cargo != null ? cargo.AccessRange : 3.2f;
        public float ActionDuration => 0.01f;
        public TopDown3DItemAmount Reward => default;
        public bool IsAvailable => cargo != null && cargo.isActiveAndEnabled;
        public bool TryConsume(out int remainingUses) { remainingUses = 1; return false; }
        public TopDown3DBigArmCargo Cargo => cargo;
        private void Awake() { cargo ??= GetComponent<TopDown3DBigArmCargo>(); }
    }
}
