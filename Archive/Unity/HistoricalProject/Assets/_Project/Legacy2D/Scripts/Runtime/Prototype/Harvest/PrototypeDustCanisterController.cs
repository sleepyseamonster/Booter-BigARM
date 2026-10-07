using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDustCanisterController : MonoBehaviour
    {
        [SerializeField] private PlayerInputAdapter inputAdapter;
        [SerializeField] private PrototypeInventory inventory;
        [SerializeField] private string dustCanisterItemId = "dust_canister";
        [SerializeField] private string dustItemId = "dust";
        [SerializeField] private Sprite canisterSprite;
        [SerializeField] private Sprite dustSprite;
        [SerializeField, Min(0.25f)] private float deployDistance = 0.8f;
        [SerializeField, Min(0.25f)] private float pickupDistance = 1.1f;

        private PrototypeDustCanister activeCanister;
        private bool inputBound;

        public PrototypeDustCanister ActiveCanister => activeCanister;
        public string LastStatusMessage { get; private set; } = "Ready.";

        public void Configure(PlayerInputAdapter input, PrototypeInventory playerInventory, Sprite bodySprite, Sprite fillSprite)
        {
            inputAdapter = input;
            inventory = playerInventory;
            canisterSprite = bodySprite;
            dustSprite = fillSprite;
            RefreshInputBindings();
        }

        public PrototypeDustCanisterSaveData CaptureSaveData()
        {
            if (activeCanister == null)
            {
                return PrototypeDustCanisterSaveData.Create(false, Vector3.zero, 0f);
            }

            return activeCanister.CaptureSaveData();
        }

        public void ApplySaveData(PrototypeDustCanisterSaveData saveData)
        {
            ClearActiveCanister();

            if (saveData == null || !saveData.IsDeployed)
            {
                LastStatusMessage = "Dust canister stowed.";
                return;
            }

            SpawnCanister(saveData.Position, saveData.StoredDust);
            LastStatusMessage = "Dust canister restored.";
        }

        private void Awake()
        {
            if (inputAdapter == null)
            {
                inputAdapter = GetComponent<PlayerInputAdapter>();
            }

            if (inventory == null)
            {
                inventory = GetComponent<PrototypeInventory>();
            }
        }

        private void OnEnable()
        {
            RefreshInputBindings();
        }

        private void OnDisable()
        {
            UnbindInput();
        }

        private void HandleDeployCanister()
        {
            if (inventory == null)
            {
                LastStatusMessage = "No inventory available.";
                return;
            }

            if (activeCanister != null)
            {
                LastStatusMessage = "Dust canister already deployed.";
                return;
            }

            if (!inventory.TryRemove(dustCanisterItemId, 1, out var removed) || removed <= 0)
            {
                LastStatusMessage = "No dust canister available.";
                return;
            }

            var direction = GetPlacementDirection();
            var spawnPosition = (Vector2)transform.position + (direction * deployDistance);
            SpawnCanister(spawnPosition, 0f);
            LastStatusMessage = "Dust canister deployed.";
        }

        private void HandlePickupCanister()
        {
            if (activeCanister == null)
            {
                LastStatusMessage = "No dust canister deployed.";
                return;
            }

            var distance = Vector2.Distance(transform.position, activeCanister.transform.position);
            if (distance > pickupDistance)
            {
                LastStatusMessage = "Move closer to pick up the canister.";
                return;
            }

            if (inventory == null || !inventory.CanStore(dustCanisterItemId, 1))
            {
                LastStatusMessage = "No room to reclaim the canister.";
                return;
            }

            var dustAmount = Mathf.RoundToInt(activeCanister.CollectAllDust());
            if (dustAmount > 0)
            {
                inventory.TryAdd(dustItemId, dustAmount, out _);
            }

            inventory.TryAdd(dustCanisterItemId, 1, out _);
            ClearActiveCanister();
            LastStatusMessage = dustAmount > 0
                ? $"Picked up canister with {dustAmount} dust."
                : "Picked up empty canister.";
        }

        private void SpawnCanister(Vector3 position, float storedDust)
        {
            ClearActiveCanister();

            var canisterObject = new GameObject("Dust Canister");
            canisterObject.transform.position = position;

            var spriteRenderer = canisterObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = canisterSprite;
            spriteRenderer.sortingOrder = 8;

            var collider = canisterObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.36f;

            activeCanister = canisterObject.AddComponent<PrototypeDustCanister>();
            activeCanister.Initialize(canisterSprite, dustSprite, storedDust);
        }

        private void RefreshInputBindings()
        {
            UnbindInput();
            if (inputAdapter == null)
            {
                return;
            }

            inputAdapter.DeployCanisterPressed += HandleDeployCanister;
            inputAdapter.PickupCanisterPressed += HandlePickupCanister;
            inputBound = true;
        }

        private void UnbindInput()
        {
            if (!inputBound || inputAdapter == null)
            {
                inputBound = false;
                return;
            }

            inputAdapter.DeployCanisterPressed -= HandleDeployCanister;
            inputAdapter.PickupCanisterPressed -= HandlePickupCanister;
            inputBound = false;
        }

        private void ClearActiveCanister()
        {
            if (activeCanister != null)
            {
                Destroy(activeCanister.gameObject);
                activeCanister = null;
            }
        }

        private Vector2 GetPlacementDirection()
        {
            var direction = inputAdapter != null ? inputAdapter.FacingValue : Vector2.up;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.up;
            }

            return direction.normalized;
        }
    }
}
