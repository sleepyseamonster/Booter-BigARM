using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PrototypeInventory))]
    public sealed class PrototypeBigArmAiController : MonoBehaviour
    {
        private enum BigArmTask
        {
            Idle,
            FollowPlayer,
            ProtectPlayer,
            Scout,
            HarvestNode,
            ReturnHome,
            HiddenAway
        }

        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private PrototypeInventory storageInventory;
        [SerializeField] private Transform homeAnchor;
        [SerializeField, Min(0.5f)] private float moveSpeed = 2.8f;
        [SerializeField, Min(0.1f)] private float acceleration = 18f;
        [SerializeField, Min(0.1f)] private float deceleration = 24f;
        [SerializeField, Min(0.1f)] private float slowdownDistance = 1f;
        [SerializeField, Min(0.1f)] private float destinationUpdateThreshold = 0.2f;
        [SerializeField, Min(0.1f)] private float waypointTolerance = 0.2f;
        [SerializeField, Min(0.1f)] private float harvestRange = 1.45f;
        [SerializeField, Range(0.1f, 0.95f)] private float returnHomeSlotFraction = 0.7f;
        [SerializeField, Min(2f)] private float followPlayerDistance = 8f;
        [SerializeField, Min(4f)] private float taskSearchRadius = 70f;
        [SerializeField, Min(1f)] private float protectRadius = 12f;
        [SerializeField, Min(0.5f)] private float protectOffset = 2.2f;
        [SerializeField, Min(1f)] private float autoScoutInterval = 18f;
        [SerializeField, Min(1f)] private float hiddenAwayDuration = 6f;
        [SerializeField, Min(0.5f)] private float scoutAdvanceDistance = 10f;

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private Collider2D bodyCollider;
        private Vector2 currentVelocity;
        private float taskPauseTimer;
        private float autoScoutTimer;
        private float hiddenAwayTimer;
        private bool recallRequested;
        private bool isHiddenAway;
        private BigArmTask currentTask = BigArmTask.Idle;
        private PrototypeHarvestNode activeNode;
        private Vector2 currentDestination;

        public string CurrentStatusMessage { get; private set; } = "Idle.";
        public string CurrentTaskLabel { get; private set; } = "Idle";
        public Vector3 CurrentDestination => currentDestination;
        public bool IsHiddenAway => isHiddenAway;

        public void Configure(
            PlayerMotor2D player,
            PrototypeInventory inventory,
            Transform home)
        {
            playerMotor = player;
            storageInventory = inventory;
            homeAnchor = home;
        }

        public void RequestRecall()
        {
            recallRequested = true;
            autoScoutTimer = autoScoutInterval;
            if (isHiddenAway)
            {
                RevealFromHiddenAway("Recalled by Booter.");
            }

            SetTask(BigArmTask.FollowPlayer, GetFollowDestination(), null, "Following Booter");
        }

        public void RequestScout()
        {
            recallRequested = false;
            autoScoutTimer = 0f;
            if (isHiddenAway)
            {
                RevealFromHiddenAway("Heading out to scout.");
            }

            BeginScout();
        }

        public PrototypeBigArmSaveData CaptureSaveData()
        {
            return PrototypeBigArmSaveData.FromPosition(transform.position);
        }

        public void ApplySaveData(PrototypeBigArmSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            Teleport(saveData.Position);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            bodyCollider = GetComponent<Collider2D>();
            storageInventory = storageInventory == null ? GetComponent<PrototypeInventory>() : storageInventory;
            homeAnchor = homeAnchor == null ? transform : homeAnchor;

            if (body != null)
            {
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.bodyType = RigidbodyType2D.Kinematic;
            }

            SetHiddenState(false);
            autoScoutTimer = autoScoutInterval;
        }

        private void Update()
        {
            if (taskPauseTimer > 0f)
            {
                taskPauseTimer -= Time.deltaTime;
            }

            if (!isHiddenAway)
            {
                autoScoutTimer -= Time.deltaTime;
            }

            if (playerMotor == null)
            {
                playerMotor = FindAnyObjectByType<PlayerMotor2D>();
            }

            if (storageInventory == null)
            {
                storageInventory = GetComponent<PrototypeInventory>();
            }

            if (homeAnchor == null)
            {
                homeAnchor = transform;
            }

            if (isHiddenAway)
            {
                hiddenAwayTimer -= Time.deltaTime;
                CurrentTaskLabel = "Away";
                CurrentStatusMessage = "BigARM is away.";
                if (hiddenAwayTimer <= 0f)
                {
                    RevealFromHiddenAway("Returning to Booter.");
                    SetTask(BigArmTask.FollowPlayer, GetFollowDestination(), null, "Following Booter");
                    autoScoutTimer = autoScoutInterval;
                }

                return;
            }

            if (TryFindThreatPosition(out var threatPosition))
            {
                recallRequested = false;
                SetTask(BigArmTask.ProtectPlayer, GetProtectDestination(threatPosition), null, "Protecting Booter");
            }
            else if (ShouldReturnHome())
            {
                var homePosition = homeAnchor.position;
                if (Vector2.Distance(transform.position, homePosition) <= waypointTolerance)
                {
                    SetTask(BigArmTask.Idle, homePosition, null, "Idle");
                    CurrentStatusMessage = "Waiting at BigARM.";
                }
                else
                {
                    SetTask(BigArmTask.ReturnHome, homePosition, null, "Returning home");
                }
            }
            else if (currentTask == BigArmTask.ProtectPlayer)
            {
                CurrentTaskLabel = "Protecting Booter";
                CurrentStatusMessage = "Staying close to Booter.";
                ApplyDestination(
                    GetProtectDestination(playerMotor != null ? playerMotor.transform.position : transform.position),
                    false);
            }
            else if (recallRequested)
            {
                SetTask(BigArmTask.FollowPlayer, GetFollowDestination(), null, "Returning to Booter");
                if (playerMotor != null &&
                    Vector2.Distance(transform.position, playerMotor.transform.position) <= Mathf.Max(1.25f, followPlayerDistance * 0.5f))
                {
                    recallRequested = false;
                }
            }
            else if (ShouldStartAutoScout())
            {
                BeginScout();
            }
            else
            {
                SetTask(BigArmTask.FollowPlayer, GetFollowDestination(), null, "Following Booter");
            }

            if (currentTask == BigArmTask.Scout || currentTask == BigArmTask.HarvestNode)
            {
                HandleMissionTask();
            }
        }

        private void FixedUpdate()
        {
            if (body == null || isHiddenAway)
            {
                currentVelocity = Vector2.zero;
                return;
            }

            if (taskPauseTimer > 0f)
            {
                currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
                return;
            }

            MoveTowards(currentDestination);
        }

        private void HandleMissionTask()
        {
            if (currentTask == BigArmTask.Scout && activeNode == null)
            {
                HandleScoutArrival();
                return;
            }

            if (activeNode == null || activeNode.IsDepleted)
            {
                if (currentTask == BigArmTask.Scout)
                {
                    HandleScoutArrival();
                }
                else
                {
                    SetTask(BigArmTask.FollowPlayer, GetFollowDestination(), null, "Following Booter");
                }

                return;
            }

            CurrentTaskLabel = currentTask == BigArmTask.HarvestNode
                ? $"Harvest {activeNode.DisplayName}"
                : $"Scout {activeNode.DisplayName}";
            CurrentStatusMessage = $"Moving to {activeNode.DisplayName}.";
            ApplyDestination(activeNode.transform.position, false);

            var nodeDistance = Vector2.Distance(transform.position, activeNode.transform.position);
            if (nodeDistance > harvestRange || taskPauseTimer > 0f)
            {
                return;
            }

            if (!activeNode.TryHarvest(storageInventory, string.Empty))
            {
                return;
            }

            CurrentStatusMessage = $"Harvested {activeNode.DisplayName}.";
            currentVelocity = Vector2.zero;
            taskPauseTimer = 0.35f;
            if (ShouldReturnHome())
            {
                SetTask(BigArmTask.ReturnHome, homeAnchor.position, null, "Returning home");
                return;
            }

            EnterHiddenAwayState("BigARM is scouting away.", hiddenAwayDuration);
        }

        private void HandleScoutArrival()
        {
            var distance = Vector2.Distance(transform.position, currentDestination);
            if (distance > waypointTolerance || taskPauseTimer > 0f)
            {
                return;
            }

            EnterHiddenAwayState("BigARM is scouting away.", hiddenAwayDuration);
        }

        private void SetTask(BigArmTask task, Vector2 destination, PrototypeHarvestNode node, string label)
        {
            var taskChanged = currentTask != task || node != activeNode;
            currentTask = task;
            activeNode = node;
            CurrentTaskLabel = label;
            CurrentStatusMessage = label;

            ApplyDestination(destination, taskChanged);
        }

        private void ApplyDestination(Vector2 destination, bool forceRetarget)
        {
            if (!forceRetarget && Vector2.Distance(currentDestination, destination) < destinationUpdateThreshold)
            {
                return;
            }

            currentDestination = destination;

            if (forceRetarget)
            {
                currentVelocity = Vector2.zero;
            }
        }

        private void EnterHiddenAwayState(string statusMessage, float duration)
        {
            currentTask = BigArmTask.HiddenAway;
            activeNode = null;
            hiddenAwayTimer = Mathf.Max(0f, duration);
            isHiddenAway = true;
            SetHiddenState(true);
            CurrentTaskLabel = "Away";
            CurrentStatusMessage = string.IsNullOrWhiteSpace(statusMessage) ? "BigARM is away." : statusMessage;
            currentDestination = homeAnchor != null ? (Vector2)homeAnchor.position : (Vector2)transform.position;
        }

        private void RevealFromHiddenAway(string statusMessage)
        {
            hiddenAwayTimer = 0f;
            isHiddenAway = false;
            SetHiddenState(false);
            CurrentTaskLabel = "Following Booter";
            CurrentStatusMessage = string.IsNullOrWhiteSpace(statusMessage) ? "Returning to Booter." : statusMessage;
        }

        private void SetHiddenState(bool hidden)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !hidden;
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = !hidden;
            }

            if (body != null)
            {
                body.simulated = !hidden;
            }

            if (hidden)
            {
                currentVelocity = Vector2.zero;
            }
        }

        private bool ShouldReturnHome()
        {
            if (storageInventory == null)
            {
                return false;
            }

            if (storageInventory.SlotCapacity <= 0)
            {
                return false;
            }

            var usedFraction = (float)storageInventory.SlotsUsed / Mathf.Max(1, storageInventory.SlotCapacity);
            return usedFraction >= returnHomeSlotFraction;
        }

        private bool ShouldStartAutoScout()
        {
            if (playerMotor == null)
            {
                return false;
            }

            if (recallRequested || autoScoutTimer > 0f)
            {
                return false;
            }

            var distance = Vector2.Distance(transform.position, playerMotor.transform.position);
            return distance <= followPlayerDistance;
        }

        private Vector2 GetFollowDestination()
        {
            if (playerMotor == null)
            {
                return transform.position;
            }

            var playerPosition = (Vector2)playerMotor.transform.position;
            var homePosition = homeAnchor != null ? (Vector2)homeAnchor.position : (Vector2)transform.position;
            var offset = (playerPosition - homePosition).normalized * 2.2f;
            if (offset.sqrMagnitude <= 0.0001f)
            {
                offset = Vector2.left * 2.2f;
            }

            return playerPosition - offset;
        }

        private Vector2 GetProtectDestination(Vector2 threatPosition)
        {
            if (playerMotor == null)
            {
                return threatPosition;
            }

            var playerPosition = (Vector2)playerMotor.transform.position;
            var awayFromThreat = (playerPosition - threatPosition).normalized;
            if (awayFromThreat.sqrMagnitude <= 0.0001f)
            {
                awayFromThreat = Vector2.left;
            }

            return playerPosition + awayFromThreat * protectOffset;
        }

        private Vector2 GetScoutDestination()
        {
            if (playerMotor == null)
            {
                return transform.position;
            }

            var playerPosition = (Vector2)playerMotor.transform.position;
            var forward = playerMotor.Velocity.sqrMagnitude > 0.01f ? playerMotor.Velocity.normalized : Vector2.right;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector2.right;
            }

            return playerPosition + forward * scoutAdvanceDistance;
        }

        private PrototypeHarvestNode FindTaskNode()
        {
            var nodes = FindObjectsByType<PrototypeHarvestNode>(FindObjectsInactive.Exclude);
            if (nodes == null || nodes.Length == 0)
            {
                return null;
            }

            PrototypeHarvestNode best = null;
            var bestDistance = float.PositiveInfinity;
            var origin = transform.position;
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node == null || node.IsDepleted)
                {
                    continue;
                }

                var distance = Vector2.Distance(origin, node.transform.position);
                if (distance > taskSearchRadius || distance >= bestDistance)
                {
                    continue;
                }

                best = node;
                bestDistance = distance;
            }

            return best;
        }

        private bool TryFindThreatPosition(out Vector2 threatPosition)
        {
            threatPosition = default;
            if (playerMotor == null)
            {
                return false;
            }

            var signals = FindObjectsByType<PrototypeBigArmThreatSignal>(FindObjectsInactive.Exclude);
            if (signals == null || signals.Length == 0)
            {
                return false;
            }

            var playerPosition = (Vector2)playerMotor.transform.position;
            var bestDistance = float.PositiveInfinity;
            var found = false;
            for (var i = 0; i < signals.Length; i++)
            {
                var signal = signals[i];
                if (signal == null)
                {
                    continue;
                }

                var distance = Vector2.Distance(playerPosition, signal.transform.position);
                if (distance > protectRadius || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                threatPosition = signal.transform.position;
                found = true;
            }

            return found;
        }

        private void BeginScout()
        {
            autoScoutTimer = autoScoutInterval;

            var scoutNode = FindTaskNode();
            if (scoutNode != null)
            {
                SetTask(BigArmTask.Scout, scoutNode.transform.position, scoutNode, $"Scouting {scoutNode.DisplayName}");
                return;
            }

            SetTask(BigArmTask.Scout, GetScoutDestination(), null, "Scouting ahead");
        }

        private void MoveTowards(Vector2 target)
        {
            if (body == null)
            {
                return;
            }

            var current = body.position;
            var direction = target - current;
            var distance = direction.magnitude;
            var desiredVelocity = Vector2.zero;
            if (distance > 0.0001f)
            {
                var desiredSpeed = moveSpeed;
                var arrivalSlowdownDistance = Mathf.Max(slowdownDistance, waypointTolerance);
                if (distance < arrivalSlowdownDistance)
                {
                    desiredSpeed *= distance / arrivalSlowdownDistance;
                }

                desiredVelocity = direction / distance * desiredSpeed;
            }

            var response = desiredVelocity.sqrMagnitude > currentVelocity.sqrMagnitude ? acceleration : deceleration;
            currentVelocity = Vector2.MoveTowards(currentVelocity, desiredVelocity, response * Time.fixedDeltaTime);
            body.MovePosition(current + currentVelocity * Time.fixedDeltaTime);
        }

        private void Teleport(Vector2 position)
        {
            currentVelocity = Vector2.zero;
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }

            transform.position = position;
            currentDestination = position;
        }
    }
}
