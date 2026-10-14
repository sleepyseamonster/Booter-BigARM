using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DLeggerMountTarget : MonoBehaviour, ITopDown3DInteractionTarget, ITopDown3DTargetEpoch
    {
        [SerializeField] private TopDown3DLeggerRidingController controller;
        public Object UnityObject => this;
        public string StableId => "legger.primary.ride";
        public uint Epoch => controller != null ? controller.State.Epoch : 0;
        public string Prompt => "Mount Legger";
        public Vector3 InteractionPoint => transform.position;
        public float InteractionRange => 2.4f;
        public bool IsAvailable => controller != null && controller.CanMount;
        internal bool HasConfiguredOwner => controller != null && controller.isActiveAndEnabled && controller.IsConfigured;
        public void Configure(TopDown3DLeggerRidingController owner) { controller = owner; }
        public bool TryMount() => IsAvailable && controller.RequestMount();
    }
}
