using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private PrototypeWorldGenerator worldGenerator;
        [SerializeField] private PrototypeSaveLoadController saveLoadController;

        private float smoothedFps;

        public void Configure(PlayerMotor2D motor, PrototypeWorldGenerator generator, PrototypeSaveLoadController controller)
        {
            playerMotor = motor;
            worldGenerator = generator;
            saveLoadController = controller;
        }

        private void Awake()
        {
            smoothedFps = 60f;
        }

        private void Update()
        {
            var currentFps = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f;
            smoothedFps = Mathf.Lerp(smoothedFps, currentFps, Time.unscaledDeltaTime * 6f);
        }

        private void OnGUI()
        {
            const int width = 360;
            const int height = 220;

            GUILayout.BeginArea(new Rect(12f, 12f, width, height), GUI.skin.box);
            GUILayout.Label("Prototype");
            GUILayout.Label($"FPS: {smoothedFps:0}");

            if (playerMotor != null)
            {
                var position = playerMotor.transform.position;
                GUILayout.Label($"Player: {position.x:0.00}, {position.y:0.00}");
                GUILayout.Label($"Velocity: {playerMotor.Velocity.x:0.00}, {playerMotor.Velocity.y:0.00}");
            }

            if (worldGenerator != null)
            {
                GUILayout.Label($"Seed: {worldGenerator.Seed}");
                GUILayout.Label($"Chunk: {worldGenerator.CurrentCenterChunk.x}, {worldGenerator.CurrentCenterChunk.y}");
                GUILayout.Label($"Loaded chunks: {worldGenerator.VisibleChunkCount}");
                GUILayout.Label($"Pending loads: {worldGenerator.PendingLoadChunkCount}");
                GUILayout.Label($"Pending unloads: {worldGenerator.PendingUnloadChunkCount}");
            }

            if (saveLoadController != null)
            {
                GUILayout.Label($"Save: {saveLoadController.CurrentSavePath}");
                GUILayout.Label(saveLoadController.LastStatusMessage);

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save") && saveLoadController != null)
                    {
                        saveLoadController.SaveCurrentState();
                    }

                    if (GUILayout.Button("Load") && saveLoadController != null)
                    {
                        saveLoadController.LoadLatestState();
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Rebuild") && saveLoadController != null)
                    {
                        saveLoadController.RebuildCurrentWorld();
                    }

                    if (GUILayout.Button("Next Seed") && saveLoadController != null)
                    {
                        saveLoadController.AdvanceWorldSeed();
                    }
                }
            }

            GUILayout.EndArea();
        }
    }
}
