using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 28f;
        [SerializeField] private float deceleration = 34f;

        private Rigidbody2D body;
        private PlayerInputAdapter inputAdapter;
        private Vector2 currentVelocity;

        public Vector2 Velocity => currentVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            inputAdapter = GetComponent<PlayerInputAdapter>();

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void FixedUpdate()
        {
            var input = inputAdapter != null ? inputAdapter.MoveValue : Vector2.zero;
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            var desiredVelocity = input * moveSpeed;
            var response = desiredVelocity.sqrMagnitude > currentVelocity.sqrMagnitude ? acceleration : deceleration;
            currentVelocity = Vector2.MoveTowards(currentVelocity, desiredVelocity, response * Time.fixedDeltaTime);

            body.MovePosition(body.position + currentVelocity * Time.fixedDeltaTime);
        }
    }
}
