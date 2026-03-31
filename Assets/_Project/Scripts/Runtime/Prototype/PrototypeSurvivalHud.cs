using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSurvivalHud : MonoBehaviour
    {
        [SerializeField] private PrototypeSurvivalState survivalState;

        public void Configure(PrototypeSurvivalState state)
        {
            survivalState = state;
        }

        private void OnGUI()
        {
            if (survivalState == null)
            {
                return;
            }

            const int width = 240;
            const int height = 96;

            GUILayout.BeginArea(new Rect(12f, 240f, width, height), GUI.skin.box);
            GUILayout.Label("Survival");
            GUILayout.Label($"Algae: {survivalState.AlgaeReserve:0}/{survivalState.MaxAlgaeReserve:0}");
            GUILayout.Label(survivalState.IsAtHome ? "Status: Home" : "Status: Away");
            GUILayout.Label($"Move Mult: {survivalState.MovementMultiplier:0.00}x");
            GUILayout.EndArea();
        }
    }
}
