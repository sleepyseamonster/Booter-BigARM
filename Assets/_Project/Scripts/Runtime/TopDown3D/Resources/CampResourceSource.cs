using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class CampResourceSource : MonoBehaviour, ITopDown3DInteractable, ITopDown3DTargetEpoch, ITopDown3DTargetColliderOwner
    {
        [SerializeField] private CampResourceStudy study;
        [SerializeField] private int index;
        [SerializeField] private Collider targetCollider;
        [SerializeField] private Transform anchor;
        [SerializeField] private GameObject availableVisual, spentVisual;
        private static readonly string[] Verbs = { "Mine ironstone", "Scrape salts", "Gather bristles", "Recover cable", "Scrape husk" };
        public int Index => index;
        public Object UnityObject => this;
        public string StableId => index >= 0 && index < 5 ? CampResourceLedger.SourceId(index) : string.Empty;
        public uint Epoch => study != null ? study.Ledger.Epoch : 0;
        public string Prompt => Verbs[Mathf.Clamp(index, 0, 4)] + " — " + Reward.Quantity + " "
            + (study != null && study.ItemCatalog != null && study.ItemCatalog.TryGetDefinition(Reward.ItemId, out var item) ? item.DisplayName : "material");
        public Vector3 InteractionPoint => anchor != null ? anchor.position : transform.position;
        public float InteractionRange => 2.2f;
        public float ActionDuration => index == 0 ? 2.2f : index == 3 ? 2.5f : 1.6f;
        public TopDown3DItemAmount Reward => index >= 0 && index < 5 ? new(CampResourceLedger.Items[index], CampResourceLedger.Quantities[index]) : default;
        public bool IsAvailable => isActiveAndEnabled && study != null && study.Ready && study.Ledger.CanHarvest(index);
        public bool OwnsCollider(Collider value) => value != null && value == targetCollider;
        public bool TryConsume(out int remainingUses)
        { remainingUses = 0; return IsAvailable && study.TryConsume(index); }
        public void Configure(CampResourceStudy owner, int key, Collider volume, Transform point, GameObject available, GameObject spent)
        { study = owner; index = key; targetCollider = volume; anchor = point; availableVisual = available; spentVisual = spent; }
        public void Refresh()
        {
            if (study == null) return;
            bool used = study.Ledger.Used(index), visible = !used && (index != 4 || study.Ledger.Dead);
            if (availableVisual != null) availableVisual.SetActive(visible);
            if (spentVisual != null) spentVisual.SetActive(used);
        }
        private void LateUpdate() => Refresh();
    }
}
