using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class IsometricHarvestNode3D : MonoBehaviour
    {
        [SerializeField] private string nodeId = "conversion.ironstone";
        [SerializeField] private string displayName = "Ironstone Outcrop";
        [SerializeField] private string itemId = "ironstone";
        [SerializeField, Min(1)] private int amount = 2;
        [SerializeField, Min(0.05f)] private float holdDuration = 0.75f;
        [SerializeField, Min(0f)] private float respawnDelay = 6f;
        [SerializeField] private Renderer[] visuals;

        private Collider nodeCollider;
        private float respawnTimer;

        public string NodeId => nodeId;
        public string DisplayName => displayName;
        public float HoldDuration => Mathf.Max(0.05f, holdDuration);
        public bool IsAvailable { get; private set; } = true;

        public void Configure(
            string id,
            string label,
            string harvestedItemId,
            int harvestedAmount,
            float secondsToHarvest,
            float secondsToRespawn,
            params Renderer[] nodeVisuals)
        {
            nodeId = id ?? string.Empty;
            displayName = label ?? "Resource";
            itemId = harvestedItemId ?? string.Empty;
            amount = Mathf.Max(1, harvestedAmount);
            holdDuration = Mathf.Max(0.05f, secondsToHarvest);
            respawnDelay = Mathf.Max(0f, secondsToRespawn);
            visuals = nodeVisuals;
        }

        public bool TryHarvest(IPrototypeItemReceiver receiver)
        {
            if (!IsAvailable || receiver == null || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var yield = new[] { new PrototypeItemAmount(itemId, amount) };
            if (!receiver.TryAddItems(yield))
            {
                return false;
            }

            SetAvailable(false);
            respawnTimer = respawnDelay;
            return true;
        }

        private void Awake()
        {
            nodeCollider = GetComponent<Collider>();
            if (visuals == null || visuals.Length == 0)
            {
                visuals = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void Update()
        {
            if (IsAvailable || respawnDelay <= 0f)
            {
                return;
            }

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                SetAvailable(true);
            }
        }

        private void SetAvailable(bool available)
        {
            IsAvailable = available;
            if (nodeCollider != null)
            {
                nodeCollider.enabled = available;
            }

            if (visuals == null)
            {
                return;
            }

            for (var i = 0; i < visuals.Length; i++)
            {
                if (visuals[i] != null)
                {
                    visuals[i].enabled = available;
                }
            }
        }
    }
}
