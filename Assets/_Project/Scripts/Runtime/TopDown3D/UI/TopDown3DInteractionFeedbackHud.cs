using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopDown3DInteractionFeedbackHud : MonoBehaviour
    {
        private const float FeedbackDuration = 2f;

        [SerializeField] private TopDown3DInteractionController interaction;
        [SerializeField] private TopDown3DPlayerActionController actionController;
        [SerializeField] private TopDown3DInputRouter input;

        private Text label;
        private float feedbackRemaining;
        private bool subscribed;

        public string DisplayedText => label != null ? label.text : string.Empty;

        public void Configure(
            TopDown3DInteractionController interactionController,
            TopDown3DPlayerActionController playerActionController,
            TopDown3DInputRouter inputRouter)
        {
            Unsubscribe();
            interaction = interactionController;
            actionController = playerActionController;
            input = inputRouter;
            EnsureVisualTree();
            Subscribe();
            RefreshPrompt(interaction != null ? interaction.CurrentTarget : null);
        }

        public void ShowFeedback(string message)
        {
            EnsureVisualTree();
            feedbackRemaining = FeedbackDuration;
            label.text = message ?? string.Empty;
            label.enabled = !string.IsNullOrWhiteSpace(label.text);
        }

        public static TopDown3DInteractionFeedbackHud TryInstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var interactionController = TopDown3DGameHudCanvas.FindInScene<TopDown3DInteractionController>(scene);
            var playerAction = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerActionController>(scene);
            var inputRouter = TopDown3DGameHudCanvas.FindInScene<TopDown3DInputRouter>(scene);
            var gameHud = TopDown3DGameHudCanvas.TryInstallForScene(scene);
            if (interactionController == null || playerAction == null || inputRouter == null || gameHud == null)
            {
                return null;
            }

            var existing = TopDown3DGameHudCanvas.FindInScene<TopDown3DInteractionFeedbackHud>(scene);
            if (existing == null)
            {
                var hudObject = new GameObject("Interaction Feedback HUD", typeof(RectTransform));
                hudObject.transform.SetParent(gameHud.transform, false);
                existing = hudObject.AddComponent<TopDown3DInteractionFeedbackHud>();
            }
            else if (existing.transform.parent != gameHud.transform)
            {
                existing.transform.SetParent(gameHud.transform, false);
            }

            existing.Configure(interactionController, playerAction, inputRouter);
            return existing;
        }

        private void Awake()
        {
            EnsureVisualTree();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (feedbackRemaining <= 0f)
            {
                return;
            }

            feedbackRemaining = Mathf.Max(0f, feedbackRemaining - Time.unscaledDeltaTime);
            if (feedbackRemaining <= 0f)
            {
                RefreshPrompt(interaction != null ? interaction.CurrentTarget : null);
            }
        }

        private void RefreshPrompt(ITopDown3DInteractable target)
        {
            if (feedbackRemaining > 0f)
            {
                return;
            }

            EnsureVisualTree();
            var binding = input != null
                ? input.GetBindingDisplayName("Gameplay/Interact", "E")
                : "E";
            label.text = target != null && target.IsAvailable
                ? $"[{binding}] {target.Prompt}"
                : string.Empty;
            label.enabled = !string.IsNullOrEmpty(label.text);
        }

        private void EnsureVisualTree()
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 116f);
            rect.sizeDelta = new Vector2(620f, 56f);
            if (label == null)
            {
                label = GetComponent<Text>();
            }

            if (label == null)
            {
                label = gameObject.AddComponent<Text>();
            }

            if (label.font == null)
            {
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 22;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color32(239, 224, 192, 255);
            label.raycastTarget = false;
        }

        private void Subscribe()
        {
            if (subscribed || !isActiveAndEnabled)
            {
                return;
            }

            if (interaction != null)
            {
                interaction.TargetChanged += RefreshPrompt;
            }

            if (actionController != null)
            {
                actionController.FeedbackRequested += ShowFeedback;
            }

            if (input != null)
            {
                input.PromptDeviceChanged += HandlePromptDeviceChanged;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (interaction != null)
            {
                interaction.TargetChanged -= RefreshPrompt;
            }

            if (actionController != null)
            {
                actionController.FeedbackRequested -= ShowFeedback;
            }

            if (input != null)
            {
                input.PromptDeviceChanged -= HandlePromptDeviceChanged;
            }

            subscribed = false;
        }

        private void HandlePromptDeviceChanged(TopDown3DPromptDevice _)
        {
            RefreshPrompt(interaction != null ? interaction.CurrentTarget : null);
        }
    }
}
