using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeCameraTargetController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private PlayerInputAdapter inputAdapter;
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float returnSpeed = 24f;
        [SerializeField] private float deadZone = 0.18f;
        [SerializeField] private float maxRadius = 4f;

        private Vector2 offset;

        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }

        public void SetInputAdapter(PlayerInputAdapter adapter)
        {
            inputAdapter = adapter;
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                return;
            }

            var lookInput = inputAdapter != null ? inputAdapter.LookValue : Vector2.zero;
            var lookMagnitude = lookInput.magnitude;

            if (lookMagnitude > deadZone)
            {
                var normalizedLook = lookInput / Mathf.Max(lookMagnitude, 0.0001f);
                var intensity = Mathf.InverseLerp(deadZone, 1f, lookMagnitude);
                offset += normalizedLook * (moveSpeed * intensity * Time.deltaTime);
                offset = Vector2.ClampMagnitude(offset, maxRadius);
            }
            else
            {
                offset = Vector2.MoveTowards(offset, Vector2.zero, returnSpeed * Time.deltaTime);
            }

            transform.position = player.position + new Vector3(offset.x, offset.y, 0f);
        }
    }
}
