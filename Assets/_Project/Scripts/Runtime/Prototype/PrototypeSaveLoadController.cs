using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSaveLoadController : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private PrototypeWorldGenerator worldGenerator;
        [SerializeField] private PrototypeSurvivalState survivalState;
        [SerializeField] private PrototypeInventory inventoryState;
        [SerializeField] private string savePath;

        public string LastStatusMessage { get; private set; } = "Ready.";
        public string CurrentSavePath => string.IsNullOrWhiteSpace(savePath) ? PrototypeSaveService.GetDefaultSavePath() : savePath;

        public void Configure(PlayerMotor2D motor, PrototypeWorldGenerator generator)
        {
            playerMotor = motor;
            worldGenerator = generator;
        }

        public void SaveCurrentState()
        {
            if (!TryCaptureSaveData(out var saveData))
            {
                return;
            }

            PrototypeSaveService.Save(saveData, savePath);
            LastStatusMessage = $"Saved to {CurrentSavePath}";
            Debug.Log(LastStatusMessage, this);
        }

        public void LoadLatestState()
        {
            if (!PrototypeSaveService.TryLoad(out var saveData, out var error, savePath))
            {
                LastStatusMessage = error;
                Debug.LogWarning(error, this);
                return;
            }

            if (saveData.Version != PrototypeSaveSchema.CurrentSaveVersion)
            {
                Debug.LogWarning(
                    $"Loaded save version {saveData.Version}, but the current save version is {PrototypeSaveSchema.CurrentSaveVersion}.",
                    this);
            }

            if (saveData.WorldIdentity != null &&
                saveData.WorldIdentity.GenerationVersion != PrototypeWorldGenerator.GenerationVersion)
            {
                Debug.LogWarning(
                    $"Loaded world generation version {saveData.WorldIdentity.GenerationVersion}, but the current generation version is {PrototypeWorldGenerator.GenerationVersion}.",
                    this);
            }

            ApplySaveData(saveData);
            LastStatusMessage = $"Loaded from {CurrentSavePath}";
            Debug.Log(LastStatusMessage, this);
        }

        public void RebuildCurrentWorld()
        {
            if (worldGenerator == null)
            {
                LastStatusMessage = $"{nameof(PrototypeSaveLoadController)} has no world generator.";
                Debug.LogWarning(LastStatusMessage, this);
                return;
            }

            worldGenerator.ResetWorld(worldGenerator.Seed);
            LastStatusMessage = $"Rebuilt seed {worldGenerator.Seed}.";
            Debug.Log(LastStatusMessage, this);
        }

        public void AdvanceWorldSeed()
        {
            if (worldGenerator == null)
            {
                LastStatusMessage = $"{nameof(PrototypeSaveLoadController)} has no world generator.";
                Debug.LogWarning(LastStatusMessage, this);
                return;
            }

            worldGenerator.ResetWorld(worldGenerator.Seed + 1);
            LastStatusMessage = $"Advanced to seed {worldGenerator.Seed}.";
            Debug.Log(LastStatusMessage, this);
        }

        private bool TryCaptureSaveData(out PrototypeSaveData saveData)
        {
            if (worldGenerator == null)
            {
                saveData = null;
                LastStatusMessage = $"{nameof(PrototypeSaveLoadController)} has no world generator.";
                Debug.LogWarning(LastStatusMessage, this);
                return false;
            }

            var worldIdentity = worldGenerator.GetWorldIdentity();
            var playerState = playerMotor != null
                ? PrototypePlayerSaveData.FromPosition(playerMotor.transform.position)
                : PrototypePlayerSaveData.FromPosition(Vector3.zero);
            var survivalData = survivalState != null
                ? survivalState.CaptureSaveData()
                : PrototypeSurvivalSaveData.FromReserve(100f);
            var inventoryData = inventoryState != null
                ? inventoryState.CaptureSaveData()
                : PrototypeInventorySaveData.FromSnapshot(0, System.Array.Empty<PrototypeInventorySlot>());
            var harvestNodes = CaptureHarvestNodeStates();
            saveData = PrototypeSaveData.Create(worldIdentity, playerState, survivalData, inventoryData, harvestNodes);
            return true;
        }

        private void ApplySaveData(PrototypeSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            if (playerMotor != null)
            {
                playerMotor.Teleport(saveData.Player.Position);
            }

            if (survivalState != null)
            {
                survivalState.ApplySaveData(saveData.Survival);
            }

            if (inventoryState != null)
            {
                inventoryState.ApplySaveData(saveData.Inventory);
            }

            if (worldGenerator != null)
            {
                var seed = saveData.WorldIdentity != null ? saveData.WorldIdentity.Seed : worldGenerator.Seed;
                worldGenerator.ResetWorld(seed);
            }

            ApplyHarvestNodeStates(saveData.HarvestNodes);
        }

        private PrototypeHarvestNodeSaveData[] CaptureHarvestNodeStates()
        {
            var nodes = FindObjectsByType<PrototypeHarvestNode>(FindObjectsSortMode.None);
            if (nodes == null || nodes.Length == 0)
            {
                return System.Array.Empty<PrototypeHarvestNodeSaveData>();
            }

            var snapshot = new System.Collections.Generic.List<PrototypeHarvestNodeSaveData>(nodes.Length);
            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] == null)
                {
                    continue;
                }

                snapshot.Add(nodes[i].CaptureSaveData());
            }

            return snapshot.Count > 0 ? snapshot.ToArray() : System.Array.Empty<PrototypeHarvestNodeSaveData>();
        }

        private void ApplyHarvestNodeStates(PrototypeHarvestNodeSaveData[] nodeStates)
        {
            if (nodeStates == null || nodeStates.Length == 0)
            {
                return;
            }

            var nodes = FindObjectsByType<PrototypeHarvestNode>(FindObjectsSortMode.None);
            if (nodes == null || nodes.Length == 0)
            {
                return;
            }

            for (var i = 0; i < nodeStates.Length; i++)
            {
                var state = nodeStates[i];
                if (state == null || string.IsNullOrWhiteSpace(state.NodeId))
                {
                    continue;
                }

                for (var j = 0; j < nodes.Length; j++)
                {
                    var node = nodes[j];
                    if (node == null)
                    {
                        continue;
                    }

                    if (string.Equals(node.NodeId, state.NodeId, System.StringComparison.Ordinal))
                    {
                        node.ApplySaveData(state);
                        break;
                    }
                }
            }
        }
    }
}
