using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHarvestPromptHud : MonoBehaviour
    {
        [SerializeField] private PrototypeHarvestInteractor interactor;
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, -80f);

        private GUIStyle labelStyle;
        private GUIStyle boxStyle;

        private void Awake()
        {
            if (interactor == null)
            {
                interactor = FindAnyObjectByType<PrototypeHarvestInteractor>();
            }
        }

        private void OnGUI()
        {
            if (interactor == null)
            {
                return;
            }

            EnsureStyles();

            var target = interactor.CurrentTarget;
            if (target == null)
            {
                return;
            }

            var progress = interactor.CurrentProgress01;
            var prompt = target.IsDepleted
                ? $"{target.DisplayName} is depleted"
                : $"Hold Interact: {target.DisplayName}";
            if (!target.IsDepleted && !string.IsNullOrWhiteSpace(target.RequiredToolId))
            {
                prompt += $"  (Tool: {target.RequiredToolId})";
            }

            var message = interactor.LastMessage;

            const float width = 420f;
            const float height = 72f;
            var rect = new Rect(
                (Screen.width - width) * 0.5f + screenOffset.x,
                (Screen.height - height) * 0.5f + screenOffset.y,
                width,
                height);

            GUILayout.BeginArea(rect, boxStyle);
            GUILayout.Label(prompt, labelStyle);
            DrawProgressBar(progress);
            if (!string.IsNullOrWhiteSpace(message))
            {
                GUILayout.Label(message, labelStyle);
            }
            GUILayout.EndArea();
        }

        private void DrawProgressBar(float progress01)
        {
            var barRect = GUILayoutUtility.GetRect(1f, 12f, GUILayout.ExpandWidth(true));
            GUI.Box(barRect, GUIContent.none);
            var fill = barRect;
            fill.width = Mathf.Clamp01(progress01) * barRect.width;
            GUI.color = new Color(0.95f, 0.66f, 0.28f, 0.9f);
            GUI.Box(fill, GUIContent.none);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                wordWrap = false
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperCenter
            };
        }
    }
}
