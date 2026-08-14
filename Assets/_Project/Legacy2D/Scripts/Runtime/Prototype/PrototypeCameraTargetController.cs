using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeCameraTargetController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private PlayerInputAdapter inputAdapter;
        [SerializeField] private float deadZone = 0.18f;
        [SerializeField] private float maxRadius = 10f;
        [SerializeField] private float stickFollowSpeed = 4f;
        [SerializeField] private float returnSpeed = 28f;

        private Vector2 currentOffset;

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
                var desiredOffset = Vector2.ClampMagnitude(normalizedLook * (maxRadius * intensity), maxRadius);
                var t = 1f - Mathf.Exp(-stickFollowSpeed * Time.deltaTime);
                currentOffset = Vector2.Lerp(currentOffset, desiredOffset, t);
            }
            else
            {
                currentOffset = Vector2.MoveTowards(currentOffset, Vector2.zero, returnSpeed * Time.deltaTime);
            }

            transform.position = player.position + new Vector3(currentOffset.x, currentOffset.y, 0f);
        }
    }
}
