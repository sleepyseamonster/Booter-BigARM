using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class PrototypeWorldItemPickup : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField, Min(0f)] private float pickupDelaySeconds = 0.12f;
        [SerializeField, Min(0.05f)] private float triggerRadius = 0.45f;

        private SpriteRenderer spriteRenderer;
        private CircleCollider2D pickupCollider;
        private float age;
        private bool collected;

        public void Configure(string id, int stackAmount, Sprite sprite)
        {
            itemId = id ?? string.Empty;
            amount = Mathf.Max(1, stackAmount);

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 8;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            pickupCollider = GetComponent<CircleCollider2D>();
            pickupCollider.isTrigger = true;
            pickupCollider.radius = triggerRadius;
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            age += Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void TryCollect(Collider2D other)
        {
            if (collected || age < pickupDelaySeconds || other == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                Destroy(gameObject);
                return;
            }

            var receivers = other.GetComponentsInParent<MonoBehaviour>();
            for (var i = 0; i < receivers.Length; i++)
            {
                if (receivers[i] is not IPrototypeItemReceiver receiver)
                {
                    continue;
                }

                if (!receiver.TryAddItems(new[] { new PrototypeItemAmount(itemId, amount) }))
                {
                    continue;
                }

                collected = true;
                Destroy(gameObject);
                return;
            }
        }
    }
}
