using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PrototypeHarvestNode : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string nodeId = Guid.NewGuid().ToString("N");
        [SerializeField] private string displayName = "Salvage";
        [SerializeField] private PrototypeHarvestNodeKind kind = PrototypeHarvestNodeKind.Salvage;

        [Header("Interaction")]
        [SerializeField, Min(0.05f)] private float harvestSeconds = 0.75f;
        [SerializeField] private bool repeatWhileHeld;
        [SerializeField] private string requiredToolId;

        [Header("Depletion")]
        [SerializeField, Min(1)] private int maxUses = 1;
        [SerializeField, Min(0f)] private float respawnSeconds;

        [Header("Yield")]
        [SerializeField] private List<PrototypeHarvestYieldEntry> yields = new List<PrototypeHarvestYieldEntry>();
        [SerializeField] private Sprite pickupSprite;

        private int usesRemaining;
        private float respawnTimer;
        private SpriteRenderer spriteRenderer;
        private Color baseColor = Color.white;

        public string NodeId => string.IsNullOrWhiteSpace(nodeId) ? gameObject.name : nodeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? kind.ToString() : displayName;
        public PrototypeHarvestNodeKind Kind => kind;
        public float HarvestSeconds => Mathf.Max(0.05f, harvestSeconds);
        public bool RepeatWhileHeld => repeatWhileHeld;
        public bool IsDepleted => usesRemaining <= 0;
        public string RequiredToolId => requiredToolId ?? string.Empty;

        public int RemainingUses => usesRemaining;

        public void Configure(
            string id,
            string name,
            PrototypeHarvestNodeKind nodeKind,
            float seconds,
            bool repeat,
            string toolId,
            int uses,
            float respawn,
            List<PrototypeHarvestYieldEntry> nodeYields,
            Sprite dropSprite)
        {
            nodeId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            displayName = string.IsNullOrWhiteSpace(name) ? "Salvage" : name;
            kind = nodeKind;
            harvestSeconds = Mathf.Max(0.05f, seconds);
            repeatWhileHeld = repeat;
            requiredToolId = toolId ?? string.Empty;
            maxUses = Mathf.Max(1, uses);
            respawnSeconds = Mathf.Max(0f, respawn);
            yields = nodeYields ?? new List<PrototypeHarvestYieldEntry>();
            pickupSprite = dropSprite;
            usesRemaining = maxUses;
            respawnTimer = 0f;
        }

        public PrototypeHarvestNodeSaveData CaptureSaveData()
        {
            return PrototypeHarvestNodeSaveData.Create(NodeId, usesRemaining);
        }

        public void ApplySaveData(PrototypeHarvestNodeSaveData saveData)
        {
            if (saveData == null || !string.Equals(saveData.NodeId, NodeId, System.StringComparison.Ordinal))
            {
                return;
            }

            usesRemaining = Mathf.Clamp(saveData.RemainingUses, 0, maxUses);
            if (usesRemaining > 0)
            {
                respawnTimer = 0f;
                ApplyDepletedVisual(false);
            }
            else
            {
                ApplyDepletedVisual(true);
                respawnTimer = respawnSeconds > 0f ? respawnSeconds : 0f;
            }
        }

        public bool CanHarvest(string equippedToolId)
        {
            if (IsDepleted)
            {
                return false;
            }

            var required = requiredToolId;
            if (string.IsNullOrWhiteSpace(required))
            {
                return true;
            }

            return string.Equals(required.Trim(), equippedToolId ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
        }

        public bool TryHarvest(IPrototypeItemReceiver receiver, string equippedToolId)
        {
            if (!CanHarvest(equippedToolId))
            {
                return false;
            }

            var rolledItems = RollYield();
            if (rolledItems.Count == 0)
            {
                ConsumeUse();
                return true;
            }

            SpawnDrops(rolledItems);
            ConsumeUse();
            return true;
        }

        private void Awake()
        {
            usesRemaining = Mathf.Max(1, maxUses);
            respawnTimer = 0f;

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }
        }

        private void Update()
        {
            if (usesRemaining > 0 || respawnSeconds <= 0f)
            {
                return;
            }

            respawnTimer -= Time.deltaTime;
            if (respawnTimer > 0f)
            {
                return;
            }

            usesRemaining = Mathf.Max(1, maxUses);
            ApplyDepletedVisual(false);
        }

        private List<PrototypeItemAmount> RollYield()
        {
            var results = new List<PrototypeItemAmount>(yields != null ? yields.Count : 0);
            if (yields == null || yields.Count == 0)
            {
                return results;
            }

            // Local RNG to avoid perturbing UnityEngine.Random global state.
            var rng = new System.Random(GetStableRollSeed());

            for (var i = 0; i < yields.Count; i++)
            {
                var entry = yields[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    continue;
                }

                var rolls = Mathf.Max(1, entry.Rolls);
                var total = 0;
                for (var r = 0; r < rolls; r++)
                {
                    total += entry.RollAmount(rng);
                }

                if (total > 0)
                {
                    results.Add(new PrototypeItemAmount(entry.ItemId.Trim(), total));
                }
            }

            return results;
        }

        private int GetStableRollSeed()
        {
            unchecked
            {
                // Deterministic per-node baseline. Later we can mix world seed + chunk identity into this.
                var hash = 17;
                hash = hash * 31 + Mathf.RoundToInt(transform.position.x * 10f);
                hash = hash * 31 + Mathf.RoundToInt(transform.position.y * 10f);
                hash = hash * 31 + usesRemaining;
                return hash;
            }
        }

        private void ConsumeUse()
        {
            usesRemaining = Mathf.Max(0, usesRemaining - 1);
            if (usesRemaining > 0)
            {
                return;
            }

            ApplyDepletedVisual(true);
            if (respawnSeconds > 0f)
            {
                respawnTimer = respawnSeconds;
            }
        }

        private void SpawnDrops(List<PrototypeItemAmount> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var dropTexture = pickupSprite != null ? pickupSprite : spriteRenderer != null ? spriteRenderer.sprite : null;
            var rng = new System.Random(GetStableRollSeed() ^ 0x51A7BEEF);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Amount <= 0 || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                var drop = new GameObject($"{DisplayName} Drop");
                drop.transform.SetParent(transform.parent, true);

                var offsetX = (float)(rng.NextDouble() - 0.5) * 0.45f;
                var offsetY = 0.15f + (float)rng.NextDouble() * 0.2f;
                drop.transform.position = transform.position + new Vector3(offsetX, offsetY, 0f);

                var pickup = drop.AddComponent<PrototypeWorldItemPickup>();
                pickup.Configure(item.ItemId, item.Amount, dropTexture);
            }
        }

        private void ApplyDepletedVisual(bool depleted)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = depleted ? baseColor * new Color(0.6f, 0.6f, 0.6f, 1f) : baseColor;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.66f, 0.28f, 0.35f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.75f);
        }
    }
}
