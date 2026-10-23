using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class CampFirstToolSource : MonoBehaviour, ITopDown3DInteractable, ITopDown3DTargetEpoch, ITopDown3DTargetColliderOwner
    {
        [SerializeField] private CampFirstToolScenario scenario;
        [SerializeField] private string sourceId, itemId, label;
        [SerializeField] private int index, quantity = 1;
        [SerializeField] private bool living;
        [SerializeField] private Transform anchor, breathingBody;
        [SerializeField] private Collider workFace;
        [SerializeField] private GameObject availableMarker, depletedMarker;
        public string StableId => sourceId;
        public string Label => label;
        public bool Harvested => scenario != null && !scenario.Stock.Available(index);
        public Object UnityObject => this;
        public uint Epoch => scenario != null ? scenario.Stock.Epoch : 0;
        public string Prompt => "Gather " + label + " · " + quantity + (living ? " · living critter" : " · rock formation");
        public Vector3 InteractionPoint => anchor != null ? anchor.position : transform.position;
        public float InteractionRange => 2.2f;
        public float ActionDuration => 1.3f;
        public TopDown3DItemAmount Reward => new(itemId, quantity);
        public bool IsAvailable => isActiveAndEnabled && scenario != null && scenario.Available(index);
        public bool IsLiving => living;
        public float Health => living ? 1f : 0f; // No damage/death owner is installed on these non-hostile study critters.
        public bool OwnsCollider(Collider collider) => collider != null && collider == workFace;
        public bool TryConsume(out int remainingUses) { remainingUses = 0; return IsAvailable && scenario.TryConsume(index); }
        public void Configure(CampFirstToolScenario owner, int key, string id, string item, string name, int count,
            bool live, Transform point, Collider collider, GameObject available, GameObject spent, Transform body)
        {
            scenario = owner; index = key; sourceId = id; itemId = item; label = name; quantity = count; living = live;
            anchor = point; workFace = collider; availableMarker = available; depletedMarker = spent; breathingBody = body;
        }
        public void Refresh()
        {
            var used = scenario != null && !scenario.Stock.Available(index);
            if (availableMarker != null) availableMarker.SetActive(!used);
            if (depletedMarker != null) depletedMarker.SetActive(used);
        }
        private void LateUpdate()
        {
            Refresh();
            if (living && breathingBody != null && Application.isPlaying)
                breathingBody.localScale = new Vector3(1, 1 + Mathf.Sin(Time.time * 2.3f + index) * .025f, 1);
        }
    }
}
