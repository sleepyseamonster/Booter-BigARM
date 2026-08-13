using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DDebugOverlay : MonoBehaviour
    {
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private TopDown3DPlayerMotor player;
        [SerializeField] private TopDown3DProceduralWorld world;
        [SerializeField] private TopDown3DBigArmFollower bigArm;
        [SerializeField] private TopDown3DCameraRig cameraRig;

        public void Configure(
            TopDown3DInputRouter inputRouter,
            TopDown3DPlayerMotor playerMotor,
            TopDown3DProceduralWorld proceduralWorld,
            TopDown3DBigArmFollower bigArmFollower,
            TopDown3DCameraRig rig)
        {
            input = inputRouter;
            player = playerMotor;
            world = proceduralWorld;
            bigArm = bigArmFollower;
            cameraRig = rig;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 390f, 215f), GUI.skin.box);
            GUILayout.Label("PERSPECTIVE TOP-DOWN 3D FOUNDATION");
            GUILayout.Label("Move: Left Stick / WASD    Sprint: RB / Left Shift");
            GUILayout.Label("Rotate camera: Right Stick (horizontal orbit + limited pitch)");
            GUILayout.Label("Recall BigARM: LB / F1");
            if (input != null)
            {
                GUILayout.Label($"Input device: {input.LastInputDevice}");
            }

            if (player != null)
            {
                var position = player.Position;
                GUILayout.Label($"Booter: {position.x:0.0}, {position.y:0.0}, {position.z:0.0}  Grounded: {player.IsGrounded}");
            }

            if (world != null)
            {
                GUILayout.Label(
                    $"Seed: {world.WorldSeed}  Chunk: {world.CurrentCenterChunk}  "
                    + $"Loaded: {world.LoadedChunkCount}  Pending: {world.PendingChunkCount}");
            }

            if (bigArm != null)
            {
                GUILayout.Label($"BigARM state: {bigArm.State}");
            }

            if (cameraRig != null)
            {
                GUILayout.Label($"Perspective camera: pitch {cameraRig.PitchDegrees:0}  yaw {cameraRig.YawDegrees:0}  distance {cameraRig.Distance:0.0}");
            }

            GUILayout.EndArea();
        }
    }
}
