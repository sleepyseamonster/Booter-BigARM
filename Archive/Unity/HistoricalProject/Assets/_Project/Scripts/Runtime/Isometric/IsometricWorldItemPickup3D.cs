using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class IsometricWorldItemPickup3D : MonoBehaviour
    {
        [SerializeField] private string itemId = "scrap_metal";
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField, Min(0f)] private float pickupDelay = 0.35f;

        private float age;

        public void Configure(string pickupItemId, int pickupAmount, float delay)
        {
            itemId = pickupItemId ?? string.Empty;
            amount = Mathf.Max(1, pickupAmount);
            pickupDelay = Mathf.Max(0f, delay);
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.Rotate(Vector3.up, 80f * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (age < pickupDelay || other == null)
            {
                return;
            }

            var inventory = other.GetComponentInParent<PrototypeInventory>();
            if (inventory == null || !inventory.TryAddItems(new[] { new PrototypeItemAmount(itemId, amount) }))
            {
                return;
            }

            Destroy(gameObject);
        }
    }
}
