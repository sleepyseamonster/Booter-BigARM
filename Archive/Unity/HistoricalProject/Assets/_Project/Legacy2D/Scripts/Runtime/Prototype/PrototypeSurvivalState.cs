using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSurvivalState : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private Transform homeAnchor;
        [SerializeField] private Vector3 homeAnchorPosition;
        [SerializeField, Min(1f)] private float maxAlgaeReserve = 100f;
        [SerializeField, Min(0f)] private float algaeReserve = 100f;
        [SerializeField, Min(0f)] private float travelDrainPerSecond = 0.9f;
        [SerializeField, Min(0f)] private float sprintDrainPerSecond = 1.4f;
        [SerializeField, Min(0f)] private float homeRegenPerSecond = 4f;
        [SerializeField, Min(0f)] private float idleRegenPerSecond = 0.4f;
        [SerializeField, Min(0f)] private float safeZoneRadius = 8f;
        [SerializeField, Range(0.5f, 1f)] private float lowReserveSpeedFloor = 0.72f;

        public float AlgaeReserve => algaeReserve;
        public float MaxAlgaeReserve => maxAlgaeReserve;
        public bool IsAtHome => Vector3.Distance(transform.position, GetHomeAnchorPosition()) <= safeZoneRadius;
        public float MovementMultiplier => Mathf.Lerp(lowReserveSpeedFloor, 1f, Mathf.Clamp01(algaeReserve / Mathf.Max(1f, maxAlgaeReserve)));

        public void Configure(PlayerMotor2D motor, Transform anchor)
        {
            playerMotor = motor;
            homeAnchor = anchor;
            if (anchor != null)
            {
                homeAnchorPosition = anchor.position;
            }
        }

        public void SetAlgaeReserve(float reserve)
        {
            algaeReserve = Mathf.Clamp(reserve, 0f, maxAlgaeReserve);
        }

        public PrototypeSurvivalSaveData CaptureSaveData()
        {
            return PrototypeSurvivalSaveData.FromReserve(algaeReserve);
        }

        public void ApplySaveData(PrototypeSurvivalSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            SetAlgaeReserve(saveData.AlgaeReserve);
        }

        private void Awake()
        {
            if (playerMotor == null)
            {
                playerMotor = GetComponent<PlayerMotor2D>();
            }

            algaeReserve = Mathf.Clamp(algaeReserve, 0f, maxAlgaeReserve);
            CacheHomeAnchorPosition();
        }

        private void Update()
        {
            var drain = 0f;
            if (playerMotor != null && playerMotor.Velocity.sqrMagnitude > 0.01f)
            {
                drain += travelDrainPerSecond;
                if (playerMotor.SprintHeld)
                {
                    drain += sprintDrainPerSecond;
                }
            }
            else
            {
                drain += idleRegenPerSecond;
            }

            if (IsAtHome)
            {
                algaeReserve += homeRegenPerSecond * Time.deltaTime;
            }
            else
            {
                algaeReserve -= drain * Time.deltaTime;
            }

            algaeReserve = Mathf.Clamp(algaeReserve, 0f, maxAlgaeReserve);
        }

        private void OnValidate()
        {
            CacheHomeAnchorPosition();
        }

        private void CacheHomeAnchorPosition()
        {
            homeAnchorPosition = GetHomeAnchorPosition();
        }

        private Vector3 GetHomeAnchorPosition()
        {
            if (homeAnchor == null)
            {
                return homeAnchorPosition;
            }

            try
            {
                homeAnchorPosition = homeAnchor.position;
            }
            catch (MissingReferenceException)
            {
                homeAnchor = null;
            }

            return homeAnchorPosition;
        }
    }
}
