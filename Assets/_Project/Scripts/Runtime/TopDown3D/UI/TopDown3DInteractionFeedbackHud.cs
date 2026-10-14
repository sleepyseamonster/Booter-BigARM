using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
        private string feedbackMessage;
        private float promptRefreshRemaining;
        private bool subscribed;
        private bool promptDirty = true;
        private ITopDown3DInteractionTarget cachedTarget;
        private string cachedTargetPrompt, cachedFeedback, interactBinding, cargoBinding;
        private bool cachedCargoAvailable;
        private int cachedHoldPercent = -1;

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
            InvalidateBindings();
            EnsureVisualTree();
            Subscribe();
            RefreshPrompt(interaction != null ? interaction.SelectedTarget : null);
        }

        public void ShowFeedback(string message)
        {
            if (this == null) { Unsubscribe(); return; }
            EnsureVisualTree();
            feedbackRemaining = FeedbackDuration;
            feedbackMessage = message ?? string.Empty;
            RefreshPrompt(interaction != null ? interaction.SelectedTarget : null);
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
            InvalidateBindings();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }
        private void OnDestroy() => Unsubscribe();

        private void Update()
        {
            feedbackRemaining = Mathf.Max(0f, feedbackRemaining - Time.unscaledDeltaTime);
            promptRefreshRemaining -= Time.unscaledDeltaTime;
            if (promptRefreshRemaining <= 0f)
            {
                promptRefreshRemaining = input != null && input.IsCargoHoldActive ? 0.05f : 0.1f;
                RefreshPrompt(interaction != null ? interaction.SelectedTarget : null);
            }
        }

        private void RefreshPrompt(ITopDown3DInteractionTarget target)
        {
            // A manually configured edit-mode view can outlive native teardown callbacks.
            if (this == null) { Unsubscribe(); return; }
            EnsureVisualTree();
            if (input != null && input.Mode != TopDown3DInputMode.Gameplay)
            {
                promptDirty = true; SetLabel(string.Empty); return;
            }
            string text;
            if (actionController != null && actionController.IsPlacingHarvester)
            {
                promptDirty = true; text = actionController.PlacementPrompt;
            }
            else if (actionController != null && actionController.IsGathering)
            { promptDirty = true; text = actionController.BusyPrompt; }
            else
            {
                var targetPrompt = target != null && target.IsAvailable ? target.Prompt : null;
                var cargoAvailable = actionController != null && actionController.CanOpenCargo
                    && (input == null || input.PromptDevice != TopDown3DPromptDevice.Gamepad);
                var holdPercent = cargoAvailable && input != null && input.IsCargoHoldActive ? Mathf.RoundToInt(input.CargoHoldProgress * 100f) : -1;
                var feedback = feedbackRemaining > 0f ? feedbackMessage : null;
                // Availability and LOS stay fresh; only unchanged presentation is reused.
                if (!promptDirty && ReferenceEquals(cachedTarget, target) && cachedTargetPrompt == targetPrompt
                    && cachedCargoAvailable == cargoAvailable && cachedHoldPercent == holdPercent && cachedFeedback == feedback) return;
                cachedTarget = target; cachedTargetPrompt = targetPrompt; cachedCargoAvailable = cargoAvailable;
                cachedHoldPercent = holdPercent; cachedFeedback = feedback; promptDirty = false;
                interactBinding ??= input != null ? input.GetBindingDisplayName("Gameplay/Interact", "E") : "E";
                cargoBinding ??= input != null ? input.GetBindingDisplayName("Gameplay/OpenCargo", "E") : "E";
                var gesture = input != null && input.PromptDevice == TopDown3DPromptDevice.Gamepad ? string.Empty : "Tap ";
                text = targetPrompt != null ? $"[{gesture}{interactBinding}] {targetPrompt}" : string.Empty;
                if (cargoAvailable)
                {
                    var progress = holdPercent >= 0 ? $" · {holdPercent}%" : string.Empty;
                    text += (text.Length > 0 ? "\n" : string.Empty) + $"[Hold {cargoBinding}] Open Legger cargo{progress}";
                }
            }
            if (feedbackRemaining > 0f && !string.IsNullOrEmpty(feedbackMessage))
                text = feedbackMessage + (text.Length > 0 ? "\n" + text : string.Empty);
            SetLabel(text);
        }

        private void SetLabel(string text)
        {
            if (label.text != text) label.text = text;
            label.enabled = !string.IsNullOrEmpty(text);
        }
        private void InvalidateBindings() { interactBinding = cargoBinding = null; promptDirty = true; }

        private void EnsureVisualTree()
        {
            if (label != null) return;
            promptDirty = true;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 116f);
            rect.sizeDelta = new Vector2(760f, 96f);
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
                interaction.SelectionChanged += RefreshPrompt;
            }

            if (actionController != null)
            {
                actionController.FeedbackRequested += ShowFeedback;
                actionController.PlacementChanged += HandlePlacementChanged;
            }

            if (input != null)
            {
                input.PromptDeviceChanged += HandlePromptDeviceChanged;
                input.ModeChanged += HandleModeChanged;
            }

            subscribed = true;
            InputSystem.onDeviceChange += HandleDeviceChange;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (!ReferenceEquals(interaction, null))
            {
                interaction.SelectionChanged -= RefreshPrompt;
            }

            if (!ReferenceEquals(actionController, null))
            {
                actionController.FeedbackRequested -= ShowFeedback;
                actionController.PlacementChanged -= HandlePlacementChanged;
            }

            if (!ReferenceEquals(input, null))
            {
                input.PromptDeviceChanged -= HandlePromptDeviceChanged;
                input.ModeChanged -= HandleModeChanged;
            }

            subscribed = false;
            InputSystem.onDeviceChange -= HandleDeviceChange;
        }

        private void HandlePromptDeviceChanged(TopDown3DPromptDevice _)
        {
            InvalidateBindings();
            RefreshPrompt(interaction != null ? interaction.SelectedTarget : null);
        }
        private void HandleModeChanged(TopDown3DInputMode _) { promptDirty = true; RefreshPrompt(interaction != null ? interaction.SelectedTarget : null); }
        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (this == null) { Unsubscribe(); return; }
            if (device is Gamepad || device is Keyboard) InvalidateBindings();
        }

        private void HandlePlacementChanged()
        {
            RefreshPrompt(interaction != null ? interaction.SelectedTarget : null);
        }
    }
}
