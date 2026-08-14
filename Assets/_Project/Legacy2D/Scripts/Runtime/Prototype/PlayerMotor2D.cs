using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 5.4f;
        [SerializeField] private float sprintSpeed = 7.2f;
        [SerializeField] private float acceleration = 28f;
        [SerializeField] private float deceleration = 34f;

        private Rigidbody2D body;
        private PlayerInputAdapter inputAdapter;
        private PrototypeSurvivalState survivalState;
        private Vector2 currentVelocity;

        public Vector2 Velocity => currentVelocity;
        public bool SprintHeld => inputAdapter != null && inputAdapter.SprintHeld;

        public void Teleport(Vector2 position)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            currentVelocity = Vector2.zero;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = position;
            }

            transform.position = position;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            inputAdapter = GetComponent<PlayerInputAdapter>();
            survivalState = GetComponent<PrototypeSurvivalState>();
            EnsureSortingGroup();

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

            var maxSpeed = inputAdapter != null && inputAdapter.SprintHeld ? sprintSpeed : walkSpeed;
            if (survivalState != null)
            {
                maxSpeed *= survivalState.MovementMultiplier;
            }

            var desiredVelocity = input * maxSpeed;
            var response = desiredVelocity.sqrMagnitude > currentVelocity.sqrMagnitude ? acceleration : deceleration;
            currentVelocity = Vector2.MoveTowards(currentVelocity, desiredVelocity, response * Time.fixedDeltaTime);

            body.MovePosition(body.position + currentVelocity * Time.fixedDeltaTime);
        }

        private void EnsureSortingGroup()
        {
            var legacySorter = GetComponent<PrototypeSpriteDepthSorter>();
            if (legacySorter != null)
            {
                Destroy(legacySorter);
            }

            var sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup == null)
            {
                sortingGroup = gameObject.AddComponent<SortingGroup>();
            }

            sortingGroup.sortingLayerID = 0;
            sortingGroup.sortingOrder = 100;
        }
    }
}
