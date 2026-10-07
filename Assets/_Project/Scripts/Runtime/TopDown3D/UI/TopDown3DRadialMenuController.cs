using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DRadialMenuController : MonoBehaviour
    {
        private TopDown3DInputRouter router;
        private TopDown3DPlayerInventory inventory;
        private TopDown3DInventoryUiController inventoryUi;
        private TopDown3DPlayerActionController action;
        private TopDown3DBigArmFollower legger;
        private TopDown3DBigArmCargo cargo;
        private TopDown3DRadialCanvas view;
        private InputAction opener, stick, pointer, digital, confirm, cancel, next, previous;
        private InputAction nextEntry, previousEntry;
        private ButtonControl openingButton;
        private bool open, editing, neutralStick, waitRelease;
        private bool firstPointerSample, stickSelectionArmed;
        private bool neutralDigital;
        private bool watchCallOver, reportedBlocked;
        private float callWatchAfter;
        private Vector2 lastPointer, lastStick;
        private int page, selected = -1, editPage, editSector;
        private TopDown3DRadialPreferences preferences, draft;
        private Action pendingEdit;
        private string editorMessage = "Select a direction, then an assignment.";
        public bool IsOpen => open;
        public bool IsCustomizing => editing;
        public TopDown3DRadialPreferences Preferences => preferences;
        public int SelectedSector => selected;

        public void Configure(TopDown3DInputRouter input, TopDown3DPlayerInventory kit,
            TopDown3DInventoryUiController ui, TopDown3DPlayerActionController playerAction,
            TopDown3DBigArmFollower follower, TopDown3DBigArmCargo companionCargo, TopDown3DRadialCanvas canvas)
        {
            router = input; inventory = kit; inventoryUi = ui; action = playerAction;
            legger = follower; cargo = companionCargo; view = canvas;
            opener = input.InputActions.FindAction("System/OpenRadial", true);
            stick = input.InputActions.FindAction("Radial/Select", true);
            pointer = input.InputActions.FindAction("Radial/Point", true);
            digital = input.InputActions.FindAction("Radial/Digital", true);
            confirm = input.InputActions.FindAction("Radial/Confirm", true);
            cancel = input.InputActions.FindAction("Radial/Cancel", true);
            next = input.InputActions.FindAction("Radial/NextPage", true);
            previous = input.InputActions.FindAction("Radial/PreviousPage", true);
            nextEntry = input.InputActions.FindAction("Radial/NextEntry", false);
            previousEntry = input.InputActions.FindAction("Radial/PreviousEntry", false);
            preferences = LoadPreferences(); page = preferences.defaultPage;
            opener.performed += OnOpen;
            router.ModeChanged += OnModeChanged;
            router.UiCancelRequested += OnEditorCancel;
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnOpen(InputAction.CallbackContext context)
        {
            if (this == null || router == null || view == null || view.Wheel == null)
            { if (opener != null) opener.performed -= OnOpen; return; }
            if (Application.isPlaying && !Application.isFocused) return;
            if (editing || waitRelease) return;
            if (open && preferences.toggleOpen) { Close(false); return; }
            if (router.Mode != TopDown3DInputMode.Gameplay || open) return;
            openingButton = context.control as ButtonControl;
            selected = -1; open = true; stickSelectionArmed = false;
            neutralStick = context.control.device is Gamepad pad && pad.rightStick.ReadValue().sqrMagnitude > 0.09f;
            neutralDigital = !DigitalIsNeutral(context.control.device);
            lastStick = Vector2.zero;
            router.EnterRadialMode();
            view.SetVisible(true);
            Canvas.ForceUpdateCanvases();
            var screenCenter = RectTransformUtility.WorldToScreenPoint(null, view.Wheel.position);
            if (context.control.device is Keyboard && Mouse.current != null) Mouse.current.WarpCursorPosition(screenCenter);
            lastPointer = screenCenter;
            firstPointerSample = true;
            view.SetVisible(true); Refresh();
        }

        private void Update()
        {
            if (watchCallOver && Time.time >= callWatchAfter)
            {
                if (legger == null) watchCallOver = false;
                else if (legger.State == TopDown3DBigArmFollower.FollowState.WaitingForRoute && !reportedBlocked)
                { reportedBlocked = true; action?.ReportFeedback("Legger cannot reach you from here"); }
                else if (cargo != null && cargo.IsInAccessRange(inventory.transform.position)
                    && legger.State == TopDown3DBigArmFollower.FollowState.Idle)
                { watchCallOver = false; action?.ReportFeedback("Legger ready — interact to access cargo"); }
            }
            if (waitRelease && (openingButton == null || !openingButton.isPressed)) waitRelease = false;
            if (!open) return;
            if (cancel.WasPressedThisFrame()) { Close(false); return; }
            if (next.WasPressedThisFrame() || previous.WasPressedThisFrame())
            {
                page = (page + (previous.WasPressedThisFrame() ? preferences.pages.Length - 1 : 1)) % preferences.pages.Length;
                selected = -1; neutralStick = true; stickSelectionArmed = false; Refresh();
            }
            var direction = digital.ReadValue<Vector2>();
            if (neutralDigital && DigitalIsNeutral(openingButton?.device)) neutralDigital = false;
            if (!neutralDigital && digital.WasPerformedThisFrame() && direction.sqrMagnitude > 0.1f)
            { stickSelectionArmed = false; selected = TopDown3DRadialGeometry.Select(direction, preferences.pages[page].slots.Length); }
            var analog = stick.ReadValue<Vector2>();
            var openingPad = openingButton?.device as Gamepad ?? Gamepad.current;
            if (neutralStick && (openingPad == null || openingPad.rightStick.ReadValue().sqrMagnitude < 0.0484f)) neutralStick = false;
            if (!neutralStick && stickSelectionArmed && analog.sqrMagnitude < 0.0484f)
            {
                stickSelectionArmed = false;
                if (!preferences.toggleOpen && !preferences.explicitConfirm
                    && openingButton != null && openingButton.isPressed)
                { Close(true); return; }
            }
            if (!neutralStick && analog.sqrMagnitude >= (selected < 0 ? 0.09f : 0.0484f)
                && (analog - lastStick).sqrMagnitude > 0.0025f)
            {
                selected = TopDown3DRadialGeometry.Select(analog, preferences.pages[page].slots.Length, selected,
                    selected < 0 ? 0.30f : 0.22f);
                stickSelectionArmed = selected >= 0;
            }
            lastStick = analog;
            var pos = pointer.ReadValue<Vector2>();
            if (firstPointerSample) { lastPointer = pos; firstPointerSample = false; }
            else if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f
                && (pos - lastPointer).sqrMagnitude > 9f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Wheel, pos, null, out var local);
                stickSelectionArmed = false;
                selected = TopDown3DRadialGeometry.Select(local, preferences.pages[page].slots.Length, selected, 44f);
                lastPointer = pos;
            }
            if (nextEntry?.WasPressedThisFrame() == true || previousEntry?.WasPressedThisFrame() == true)
            {
                stickSelectionArmed = false;
                var count = preferences.pages[page].slots.Length;
                selected = selected < 0 ? (previousEntry?.WasPressedThisFrame() == true ? count - 1 : 0)
                    : (selected + (previousEntry?.WasPressedThisFrame() == true ? count - 1 : 1)) % count;
            }
            Refresh();
            if (confirm.WasPressedThisFrame()) { Close(true); return; }
            if (!preferences.toggleOpen && openingButton != null && !openingButton.isPressed)
                Close(!preferences.explicitConfirm && !(openingButton.device is Gamepad));
        }

        private void Refresh()
        {
            var p = preferences.pages[page];
            var description = selected < 0 ? "" : Reason(p.slots[selected]);
            var hint = (preferences.toggleOpen ? "Open " : "Hold ") + router.GetBindingDisplayName("System/OpenRadial", "Q") + "  ·  "
                + (preferences.toggleOpen || preferences.explicitConfirm ? "Confirm " + router.GetBindingDisplayName("Radial/Confirm", "Enter") : router.PromptDevice == TopDown3DPromptDevice.Gamepad ? "Release right stick" : "Release Q to enter")
                + "  ·  Cancel " + router.GetBindingDisplayName("Radial/Cancel", "Escape");
            if (preferences.pages.Length > 1) hint += "  ·  Page " + router.GetBindingDisplayName("Radial/NextPage", "PageDown");
            if (p.slots.Length == 8) hint += "  ·  Entry " + router.GetBindingDisplayName("Radial/NextEntry", "]");
            var count = 0;
            foreach (var slot in inventory.State.Slots) if (slot.ItemId == TopDown3DHarvesterSettings.CanisterItemId) count += slot.Quantity;
            view.Refresh(p, selected, description, hint, preferences.scale, preferences.toggleOpen || preferences.explicitConfirm, count);
        }

        private static bool DigitalIsNeutral(InputDevice device)
        {
            if (device is Gamepad pad) return pad.dpad.ReadValue().sqrMagnitude < 0.1f;
            if (device is Keyboard keyboard) return !keyboard.upArrowKey.isPressed && !keyboard.downArrowKey.isPressed
                && !keyboard.leftArrowKey.isPressed && !keyboard.rightArrowKey.isPressed;
            return true;
        }

        private string Reason(TopDown3DRadialCommand command)
        {
            if (!CanExecute(command)) return command switch {
                TopDown3DRadialCommand.DustCanister => "No carried canister, or placement is unavailable",
                TopDown3DRadialCommand.CallLegger => "Legger unavailable",
                TopDown3DRadialCommand.AutoPack => "Legger must be within reach",
                _ => "No action assigned" };
            return command switch {
                TopDown3DRadialCommand.PlayerInventory => "Player Inventory — open field kit",
                TopDown3DRadialCommand.CallLegger => "Call Legger Over — sprint to Booter",
                TopDown3DRadialCommand.DustCanister => "Dust Canister — choose a placement point",
                TopDown3DRadialCommand.AutoPack => "Auto-Pack Legger cargo",
                _ => "No action assigned" };
        }

        private bool CanExecute(TopDown3DRadialCommand c) => c switch {
            TopDown3DRadialCommand.PlayerInventory => inventoryUi != null,
            TopDown3DRadialCommand.CallLegger => legger != null && legger.isActiveAndEnabled,
            TopDown3DRadialCommand.DustCanister => action != null && action.CanSelectCanister,
            TopDown3DRadialCommand.AutoPack => cargo != null && cargo.IsInAccessRange(inventory.transform.position),
            _ => false };

        private void Close(bool commit)
        {
            if (!open) return;
            var command = selected < 0 ? TopDown3DRadialCommand.Empty : preferences.pages[page].slots[selected];
            open = false; selected = -1; waitRelease = openingButton != null && openingButton.isPressed;
            if (view != null) view.SetVisible(false);
            if (router.Mode == TopDown3DInputMode.Radial) router.EnterGameplayMode();
            if (!commit) return;
            if (!CanExecute(command)) { if (command != TopDown3DRadialCommand.Empty) action?.ReportFeedback(Reason(command)); return; }
            switch (command)
            {
                case TopDown3DRadialCommand.PlayerInventory: inventoryUi.Open(); break;
                case TopDown3DRadialCommand.CallLegger:
                    legger.RequestCallOver(); watchCallOver = true; reportedBlocked = false;
                    callWatchAfter = Time.time + 0.25f; action?.ReportFeedback("Legger approaching"); break;
                case TopDown3DRadialCommand.DustCanister: action.SelectCanister(); break;
                case TopDown3DRadialCommand.AutoPack: cargo.TryAutoPack(); break;
            }
        }

        public void OpenCustomization()
        {
            Close(false); inventoryUi.Close(); router.EnterInventoryMode(); editing = true;
            draft = preferences.Clone(); editPage = page; editSector = 0; pendingEdit = null; DrawEditor();
        }

        private void DrawEditor()
        {
            view.ShowEditor(draft, editPage, editSector,
                index => { editPage = index; editSector = 0; pendingEdit = null; DrawEditor(); },
                index => { editSector = index; pendingEdit = null; DrawEditor(); },
                command => { draft.pages[editPage].slots[editSector] = command; DrawEditor(); },
                () => { var list = new System.Collections.Generic.List<TopDown3DRadialPage>(draft.pages); var p = new TopDown3DRadialPage { name = "Custom " + list.Count }; Array.Clear(p.slots, 0, p.slots.Length); list.Add(p); draft.pages = list.ToArray(); editPage = list.Count - 1; editSector = 0; DrawEditor(); },
                () => ConfirmEdit("Delete this page? Press Apply to confirm.", () => { var list = new System.Collections.Generic.List<TopDown3DRadialPage>(draft.pages); list.RemoveAt(editPage); draft.pages = list.ToArray(); editPage = Math.Min(editPage, list.Count - 1); editSector = 0; }),
                () => ConfirmEdit("Change sector count? Press Apply to confirm.", ResizeDraft),
                ApplyEditor, CancelEditor,
                () => { if (draft.toggleOpen) { draft.toggleOpen = false; draft.explicitConfirm = false; } else if (draft.explicitConfirm) draft.toggleOpen = true; else draft.explicitConfirm = true; DrawEditor(); },
                () => { draft.scale = draft.scale >= 1.30f ? 0.85f : Mathf.Min(1.35f, draft.scale + 0.15f); DrawEditor(); },
                editorMessage);
        }

        private void ResizeDraft()
        {
            var p = draft.pages[editPage]; var slots = new TopDown3DRadialCommand[p.slots.Length == 4 ? 8 : 4];
            for (var i = 0; i < 4; i++) slots[slots.Length == 8 ? i * 2 : i] = p.slots[p.slots.Length == 8 ? i * 2 : i];
            p.slots = slots; editSector = 0;
        }
        private void ConfirmEdit(string message, Action mutation) { pendingEdit = mutation; editorMessage = message; DrawEditor(); }
        private void ApplyEditor()
        {
            if (pendingEdit != null) { var mutation = pendingEdit; pendingEdit = null; mutation(); if (!editing) return; editorMessage = "Draft changed. Apply layout to save."; DrawEditor(); return; }
            draft.defaultPage = Math.Min(draft.defaultPage, draft.pages.Length - 1);
            if (!draft.IsValid()) { editorMessage = "Layout is invalid; check names and pages."; DrawEditor(); return; }
            try
            {
                var path = PreferencePath; Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(draft, true));
                if (File.Exists(path)) File.Replace(path + ".tmp", path, null); else File.Move(path + ".tmp", path);
                var activeId = preferences.pages[page].id;
                preferences = draft.Clone();
                var retainedPage = Array.FindIndex(preferences.pages, p => p.id == activeId);
                page = retainedPage >= 0 ? retainedPage : preferences.defaultPage;
                FinishEditor();
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { editorMessage = "Could not save layout. Your draft is retained; try Apply again."; DrawEditor(); }
        }
        private void CancelEditor()
        {
            if (pendingEdit != null) { pendingEdit = null; editorMessage = "Operation canceled; draft retained."; DrawEditor(); return; }
            if (JsonUtility.ToJson(draft) == JsonUtility.ToJson(preferences)) { FinishEditor(); return; }
            ConfirmEdit("Discard edits? Press Apply to discard, or Cancel to keep editing.", FinishEditor);
        }
        private void FinishEditor() { editing = false; view.SetVisible(false); inventoryUi.Open(); }
        private void OnEditorCancel() { if (editing) CancelEditor(); }
        private static string PreferencePath => Path.Combine(Application.persistentDataPath, "radial-layout-v1.json");
        private static TopDown3DRadialPreferences LoadPreferences()
        {
            try { if (File.Exists(PreferencePath)) { var p = JsonUtility.FromJson<TopDown3DRadialPreferences>(File.ReadAllText(PreferencePath)); if (p != null && p.IsValid()) return p; } }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException) { Debug.LogWarning("Radial layout could not be loaded; using defaults."); }
            return new TopDown3DRadialPreferences();
        }
        private void OnModeChanged(TopDown3DInputMode mode) { if (open && mode != TopDown3DInputMode.Radial) Close(false); }
        private void OnApplicationFocus(bool focused) { if (!focused) Close(false); }
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (openingButton?.device == device && (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed || change == InputDeviceChange.Disabled))
            { Close(false); openingButton = null; waitRelease = false; }
        }
        private void OnDisable() { if (open) Close(false); }
        private void OnDestroy()
        {
            Close(false); if (opener != null) opener.performed -= OnOpen;
            if (router != null) { router.ModeChanged -= OnModeChanged; router.UiCancelRequested -= OnEditorCancel; }
            InputSystem.onDeviceChange -= OnDeviceChange;
        }
    }
}
