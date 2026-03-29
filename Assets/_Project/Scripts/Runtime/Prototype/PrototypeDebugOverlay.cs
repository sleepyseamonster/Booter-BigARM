using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private PrototypeWorldGenerator worldGenerator;

        private float smoothedFps;

        public void Configure(PlayerMotor2D motor, PrototypeWorldGenerator generator)
        {
            playerMotor = motor;
            worldGenerator = generator;
        }

        private void Awake()
        {
            smoothedFps = 60f;
        }

        private void Update()
        {
            var currentFps = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f;
            smoothedFps = Mathf.Lerp(smoothedFps, currentFps, Time.unscaledDeltaTime * 6f);

            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame && worldGenerator != null)
            {
                worldGenerator.ResetWorld(worldGenerator.Seed + 1);
            }

            if (Keyboard.current.rKey.wasPressedThisFrame && worldGenerator != null)
            {
                worldGenerator.ResetWorld(worldGenerator.Seed);
            }
        }

        private void OnGUI()
        {
            const int width = 320;
            const int height = 150;

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

            GUILayout.Label("F5: next seed");
            GUILayout.Label("R: rebuild current seed");
            GUILayout.EndArea();
        }
    }
}
