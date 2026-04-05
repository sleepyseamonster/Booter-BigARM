using System.Collections.Generic;
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
        [SerializeField, Min(0.1f)] private float waypointTolerance = 0.2f;
        [SerializeField, Min(0.1f)] private float harvestRange = 1.45f;
        [SerializeField, Min(0.1f)] private float repathInterval = 0.35f;
        [SerializeField, Min(0.5f)] private float cellSize = 0.85f;
        [SerializeField, Min(4)] private int searchRadiusCells = 28;
        [SerializeField, Range(0.1f, 0.95f)] private float returnHomeSlotFraction = 0.7f;
        [SerializeField, Min(2f)] private float followPlayerDistance = 8f;
        [SerializeField, Min(4f)] private float taskSearchRadius = 70f;
        [SerializeField, Min(1f)] private float protectRadius = 12f;
        [SerializeField, Min(0.5f)] private float protectOffset = 2.2f;
        [SerializeField, Min(1f)] private float autoScoutInterval = 18f;
        [SerializeField, Min(1f)] private float hiddenAwayDuration = 6f;
        [SerializeField, Min(0.5f)] private float scoutAdvanceDistance = 10f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private Collider2D bodyCollider;
        private readonly List<Vector2> path = new List<Vector2>(32);
        private int pathIndex;
        private float repathTimer;
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

            repathTimer -= Time.deltaTime;
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
                currentDestination = homeAnchor != null ? homeAnchor.position : transform.position;
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
                currentDestination = GetProtectDestination(playerMotor != null ? playerMotor.transform.position : transform.position);
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

            if (!isHiddenAway && repathTimer <= 0f && taskPauseTimer <= 0f)
            {
                RebuildPath();
                repathTimer = repathInterval;
            }
        }

        private void FixedUpdate()
        {
            if (body == null || taskPauseTimer > 0f || isHiddenAway)
            {
                return;
            }

            if (path.Count == 0)
            {
                MoveTowards(currentDestination);
                return;
            }

            var target = pathIndex < path.Count ? path[pathIndex] : currentDestination;
            var distance = Vector2.Distance(body.position, target);
            if (distance <= waypointTolerance)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    path.Clear();
                    pathIndex = 0;
                    MoveTowards(currentDestination);
                    return;
                }

                target = path[pathIndex];
            }

            MoveTowards(target);
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
            currentDestination = activeNode.transform.position;

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
            if (currentTask == task && node == activeNode && Vector2.Distance(currentDestination, destination) < 0.05f)
            {
                return;
            }

            currentTask = task;
            activeNode = node;
            currentDestination = destination;
            CurrentTaskLabel = label;
            CurrentStatusMessage = label;
            path.Clear();
            pathIndex = 0;
            repathTimer = 0f;
        }

        private void EnterHiddenAwayState(string statusMessage, float duration)
        {
            currentTask = BigArmTask.HiddenAway;
            activeNode = null;
            hiddenAwayTimer = Mathf.Max(0f, duration);
            isHiddenAway = true;
            SetHiddenState(true);
            path.Clear();
            pathIndex = 0;
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

        private void RebuildPath()
        {
            if (currentTask == BigArmTask.Idle && Vector2.Distance(transform.position, currentDestination) <= waypointTolerance)
            {
                path.Clear();
                pathIndex = 0;
                return;
            }

            var start = (Vector2)transform.position;
            var destination = currentDestination;
            if (TryBuildPath(start, destination, out var newPath))
            {
                path.Clear();
                path.AddRange(newPath);
                pathIndex = 0;
            }
            else
            {
                path.Clear();
                pathIndex = 0;
            }
        }

        private bool TryBuildPath(Vector2 start, Vector2 destination, out List<Vector2> result)
        {
            result = new List<Vector2>(32);
            var startCell = WorldToCell(start);
            var goalCell = WorldToCell(destination);
            var resolvedGoal = FindWalkableCellNear(goalCell, destination);
            if (resolvedGoal == null)
            {
                return false;
            }

            var open = new List<Vector2Int> { startCell };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int> { [startCell] = 0 };
            var fScore = new Dictionary<Vector2Int, int> { [startCell] = Heuristic(startCell, resolvedGoal.Value) };
            var closed = new HashSet<Vector2Int>();
            var safetyBudget = Mathf.Max(64, searchRadiusCells * searchRadiusCells * 4);

            while (open.Count > 0 && safetyBudget-- > 0)
            {
                var currentIndex = GetLowestScoreIndex(open, fScore);
                var current = open[currentIndex];
                if (current == resolvedGoal.Value)
                {
                    result = ReconstructPath(cameFrom, current, startCell);
                    return true;
                }

                open.RemoveAt(currentIndex);
                closed.Add(current);

                var neighbors = GetNeighbors(current);
                for (var i = 0; i < neighbors.Length; i++)
                {
                    var neighbor = neighbors[i];
                    if (closed.Contains(neighbor) || IsCellBlocked(neighbor, destination))
                    {
                        continue;
                    }

                    var tentativeG = gScore[current] + 1;
                    if (gScore.TryGetValue(neighbor, out var knownG) && tentativeG >= knownG)
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, resolvedGoal.Value);
                    if (!open.Contains(neighbor))
                    {
                        open.Add(neighbor);
                    }
                }
            }

            return false;
        }

        private Vector2Int[] GetNeighbors(Vector2Int cell)
        {
            return new[]
            {
                new Vector2Int(cell.x + 1, cell.y),
                new Vector2Int(cell.x - 1, cell.y),
                new Vector2Int(cell.x, cell.y + 1),
                new Vector2Int(cell.x, cell.y - 1)
            };
        }

        private Vector2Int? FindWalkableCellNear(Vector2Int cell, Vector2 goal)
        {
            if (!IsCellBlocked(cell, goal))
            {
                return cell;
            }

            for (var radius = 1; radius <= searchRadiusCells; radius++)
            {
                for (var y = -radius; y <= radius; y++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        var candidate = new Vector2Int(cell.x + x, cell.y + y);
                        if (!IsCellBlocked(candidate, goal))
                        {
                            return candidate;
                        }
                    }
                }
            }

            return null;
        }

        private bool IsCellBlocked(Vector2Int cell, Vector2 goal)
        {
            var center = CellToWorld(cell);
            var size = new Vector2(cellSize * 0.8f, cellSize * 0.8f);
            var colliders = Physics2D.OverlapBoxAll(center, size, 0f, obstacleMask);
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                if (collider.transform == transform || collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (playerMotor != null && (collider.transform == playerMotor.transform || collider.transform.IsChildOf(playerMotor.transform)))
                {
                    continue;
                }

                if (activeNode != null && (collider.transform == activeNode.transform || collider.transform.IsChildOf(activeNode.transform)))
                {
                    continue;
                }

                var goalDistance = Vector2.Distance(center, goal);
                if (goalDistance <= cellSize * 0.6f)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private int GetLowestScoreIndex(List<Vector2Int> open, Dictionary<Vector2Int, int> fScore)
        {
            var bestIndex = 0;
            var bestScore = int.MaxValue;
            for (var i = 0; i < open.Count; i++)
            {
                var candidate = open[i];
                var score = fScore.TryGetValue(candidate, out var value) ? value : int.MaxValue;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private List<Vector2> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Vector2Int start)
        {
            var reversed = new List<Vector2>(32) { CellToWorld(current) };
            while (cameFrom.TryGetValue(current, out var previous))
            {
                current = previous;
                reversed.Add(CellToWorld(current));
                if (current == start)
                {
                    break;
                }
            }

            reversed.Reverse();
            return reversed;
        }

        private Vector2Int WorldToCell(Vector2 position)
        {
            var size = Mathf.Max(0.1f, cellSize);
            return new Vector2Int(Mathf.RoundToInt(position.x / size), Mathf.RoundToInt(position.y / size));
        }

        private Vector2 CellToWorld(Vector2Int cell)
        {
            return new Vector2(cell.x * cellSize, cell.y * cellSize);
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
            if (distance <= 0.0001f)
            {
                return;
            }

            var step = Mathf.Min(moveSpeed * Time.fixedDeltaTime, distance);
            body.MovePosition(current + direction.normalized * step);
        }

        private void Teleport(Vector2 position)
        {
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }

            transform.position = position;
            path.Clear();
            pathIndex = 0;
            currentDestination = position;
        }
    }
}
