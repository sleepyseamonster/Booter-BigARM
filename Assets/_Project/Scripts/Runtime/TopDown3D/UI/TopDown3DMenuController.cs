using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DMenuController : MonoBehaviour
    {
        private TopDown3DInputRouter input;
        private TopDown3DInventoryUiController inventory;
        private TopDown3DRadialMenuController radial;
        private TopDown3DMenuView view;
        private InputSystemUIInputModule module;
        private InputActionReference cancelReference, submitReference, clickReference;
        private TopDown3DMenuSettingsService settings;
        private BadwaterCameraRange cameraRange;
        private TopDown3DMenuSettings settingsDraft;
        private TopDown3DRadialPreferences radialDraft;
        private bool returnInventory, directSetup, more, waitingRelease, configured;
        private int page, sector;
        private string panel, dialogParent, dialogFocus;
        private Action confirmed;
        public bool IsOpen => panel != null;
        public string CurrentPanel => panel;
        public Action ExitRequested;
        public bool IsConfigured => configured;

        public void Configure(TopDown3DInputRouter router, TopDown3DInventoryUiController kit,
            TopDown3DRadialMenuController wheel, TopDown3DMenuView menuView,
            InputSystemUIInputModule uiModule, string preferenceDirectory = null, BadwaterCameraRange range = null)
        {
            input = router; inventory = kit; radial = wheel; view = menuView; module = uiModule;
            settings = new TopDown3DMenuSettingsService(preferenceDirectory ?? Application.persistentDataPath);
            cameraRange = range;
            cameraRange?.SetPreset(settings.Accepted.viewDistancePreset);
            view.Initialize(kit.EventSystem); view.ApplySettings(settings.Accepted);
            view.Command += OnCommand; view.Rename += Rename;
            input.MenuToggleRequested += Toggle; input.UiCancelRequested += Back;
            input.PromptDeviceChanged += PromptChanged;
            input.ModeChanged += InputModeChanged;
            radial.CustomizationRequested = OpenSetupFromInventory;
            ExitRequested = () => Application.Quit();
            configured = true;
        }

        public void Toggle()
        {
            if (this == null || !configured || input == null || view == null || module == null) return;
            if (!IsOpen) { Open(); return; }
            if (panel == "Dialog") return;
            if (panel == "Root") { Close(); return; }
            if (view.IsEditingText) view.EndTextEdit();
            LeaveChild(() => { directSetup = false; Show("Root", "RadialSetup"); });
        }

        public void Open()
        {
            if (IsOpen || input.Mode == TopDown3DInputMode.Disabled) return;
            returnInventory = inventory.IsOpen;
            if (returnInventory) inventory.SuspendForMenu();
            radial.CancelForSystemMenu();
            cancelReference = module.cancel; submitReference = module.submit; clickReference = module.leftClick;
            module.cancel = null;
            input.EnterSystemMenuMode();
            input.InputActions.FindAction("UI/Cancel", true).Enable();
            Show("Root", "Resume");
        }

        public void OpenSetupFromInventory()
        {
            if (!IsOpen) Open();
            directSetup = true;
            BeginSetup();
        }

        private void Show(string next, string focus)
        {
            panel = next;
            view.Show(next, focus);
            waitingRelease = true;
            module.submit = null; module.leftClick = null;
            input.InputActions.FindAction("UI/Submit", true).Enable();
            input.InputActions.FindAction("UI/Click", true).Enable();
            view.SetInputReady(false);
            Refresh();
        }

        public void Back()
        {
            if (!IsOpen) return;
            if (view.IsEditingText) { view.EndTextEdit(); return; }
            if (panel == "Dialog")
            {
                confirmed = null; Show(dialogParent, dialogFocus); return;
            }
            if (panel == "Root") Close();
            else LeaveChild(ReturnFromChild);
        }

        private void ReturnFromChild()
        {
            if (directSetup) Close();
            else Show("Root", panel == "Settings" ? "Settings" : "RadialSetup");
        }

        private bool Dirty => panel == "Setup" ? JsonUtility.ToJson(radialDraft) != JsonUtility.ToJson(radial.Preferences)
            : panel == "Settings" && JsonUtility.ToJson(settingsDraft) != JsonUtility.ToJson(settings.Accepted);

        private void LeaveChild(Action destination)
        {
            if (!Dirty) { radial.EndSetup(); view.ApplySettings(settings.Accepted); destination(); return; }
            Dialog("Discard changes?", "Your unapplied changes will be discarded.", "Keep Editing", "Discard", () =>
            { radial.EndSetup(); view.ApplySettings(settings.Accepted); destination(); });
        }

        private void Dialog(string title, string message, string safeLabel, string acceptLabel, Action action)
        {
            dialogParent = panel; dialogFocus = view.FocusKey; confirmed = action;
            Show("Dialog", "DialogCancel");
            view.SetText("DialogTitle", title); view.SetText("DialogMessage", message);
            view.SetText("DialogCancel", safeLabel); view.SetText("DialogAccept", acceptLabel);
        }

        public void Close()
        {
            if (!IsOpen) return;
            if (radial != null) radial.EndSetup();
            if (view != null) view.ApplySettings(settings.Accepted);
            panel = null; confirmed = null;
            if (view != null) view.Show(null);
            if (module != null) { module.cancel = cancelReference; module.submit = submitReference; module.leftClick = clickReference; }
            if (returnInventory && inventory != null && inventory.IsSuspended) inventory.ResumeFromMenu();
            else if (input != null && input.Mode == TopDown3DInputMode.SystemMenu) input.EnterGameplayMode();
            directSetup = false;
        }

        private void BeginSetup()
        {
            radialDraft = radial.BeginSetup(); page = radialDraft.defaultPage; sector = 0; more = false;
            Show("Setup", "Slot0");
        }

        private void OnCommand(string command)
        {
            if (waitingRelease || !IsOpen) return;
            if (command == "DialogCancel") { Back(); return; }
            if (command == "DialogAccept") { var action = confirmed; confirmed = null; Show(dialogParent, dialogFocus); action?.Invoke(); return; }
            if (command == "Resume") { Close(); return; }
            if (command == "RadialSetup") { directSetup = false; BeginSetup(); return; }
            if (command == "Settings") { settingsDraft = settings.Accepted.Clone(); Show("Settings", "TextSize"); return; }
            if (command == "Exit")
            { Dialog("Exit game?", "Current session progress will not be saved.", "Keep Playing", "Exit Game", () => ExitRequested?.Invoke()); return; }
            if (command == "SettingsBack" || command == "SetupBack") { Back(); return; }
            if (command == "TextSize") settingsDraft.largeText = !settingsDraft.largeText;
            else if (command == "Motion") settingsDraft.reducedMotion = !settingsDraft.reducedMotion;
            else if (command == "ViewDistance")
            {
                settingsDraft.viewDistancePreset = settingsDraft.viewDistancePreset switch
                { "Low" => "Medium", "Medium" => "High", "High" => "Maximum", _ => "Low" };
            }
            else if (command == "SettingsApply")
            {
                if (!settings.TrySave(settingsDraft, out var error)) { view.SetText("SettingsNotice", error); return; }
                cameraRange?.SetPreset(settings.Accepted.viewDistancePreset);
                view.ApplySettings(settings.Accepted); ReturnFromChild(); return;
            }
            else if (command == "SetupApply")
            {
                if (!radial.TryApplySetup(radialDraft, out var error)) { view.SetText("SetupNotice", error); return; }
                radial.EndSetup(); ReturnFromChild(); return;
            }
            else if (command.StartsWith("Slot", StringComparison.Ordinal)) sector = int.Parse(command.Substring(4));
            else if (command.StartsWith("Assign", StringComparison.Ordinal)) radialDraft.pages[page].slots[sector] = (TopDown3DRadialCommand)int.Parse(command.Substring(6));
            else if (command == "PreviousPage" || command == "NextPage")
            { page = (page + (command == "NextPage" ? 1 : radialDraft.pages.Length - 1)) % radialDraft.pages.Length; sector = 0; }
            else if (command == "More") more = !more;
            else if (command == "AddPage")
            {
                if (radialDraft.pages.Length >= 4) return;
                var pages = new List<TopDown3DRadialPage>(radialDraft.pages);
                var added = new TopDown3DRadialPage { name = "Custom " + pages.Count };
                Array.Clear(added.slots, 0, added.slots.Length); pages.Add(added);
                radialDraft.pages = pages.ToArray(); page = pages.Count - 1; sector = 0;
            }
            else if (command == "DeletePage")
            {
                if (radialDraft.pages.Length == 1) return;
                Dialog("Delete this page?", "This changes your draft. Apply will save the layout.", "Keep Page", "Delete", () =>
                {
                    var defaultId = radialDraft.pages[radialDraft.defaultPage].id;
                    var pages = new List<TopDown3DRadialPage>(radialDraft.pages); pages.RemoveAt(page);
                    radialDraft.pages = pages.ToArray(); page = Math.Min(page, pages.Count - 1); sector = 0;
                    var retainedDefault = pages.FindIndex(p => p.id == defaultId);
                    radialDraft.defaultPage = retainedDefault >= 0 ? retainedDefault : page;
                    Show("Setup", "More");
                }); return;
            }
            else if (command == "SectorCount")
            {
                Dialog("Change directions?", "Cardinal assignments are retained. Diagonal assignments are cleared when reducing to four.", "Keep Layout", "Change", () =>
                {
                    var old = radialDraft.pages[page].slots; var slots = new TopDown3DRadialCommand[old.Length == 4 ? 8 : 4];
                    for (var i = 0; i < 4; i++) slots[slots.Length == 8 ? i * 2 : i] = old[old.Length == 8 ? i * 2 : i];
                    radialDraft.pages[page].slots = slots; sector = 0; Show("Setup", "Slot0");
                }); return;
            }
            else if (command == "Style")
            { if (radialDraft.toggleOpen) { radialDraft.toggleOpen = false; radialDraft.explicitConfirm = false; }
                else if (radialDraft.explicitConfirm) radialDraft.toggleOpen = true; else radialDraft.explicitConfirm = true; }
            else if (command == "WheelScale") radialDraft.scale = radialDraft.scale >= 1.30f ? 0.85f : Mathf.Min(1.35f, radialDraft.scale + 0.15f);
            else if (command == "DefaultPage") radialDraft.defaultPage = page;
            Refresh();
        }

        private void Rename(string name)
        {
            if (radialDraft == null || panel != "Setup") return;
            radialDraft.pages[page].name = string.IsNullOrWhiteSpace(name) ? "Field" : name.Trim(); Refresh();
        }

        private void Refresh()
        {
            if (panel == "Settings")
            {
                view.SetText("TextSize", "Menu text: " + (settingsDraft.largeText ? "Large" : "Normal"));
                view.SetText("Motion", "Menu motion: " + (settingsDraft.reducedMotion ? "Reduced" : "Normal"));
                view.SetText("ViewDistance", "Maximum view distance\n" + settingsDraft.viewDistancePreset + " - "
                    + BadwaterCameraRange.GetDistance(settingsDraft.viewDistancePreset).ToString("N0") + " m");
                view.SetText("SettingsNotice", settings.Notice ?? "Changes are saved with Apply.");
                view.SetButton("SettingsApply", settings.CanSave); view.ApplySettings(settingsDraft);
            }
            if (panel == "Setup")
            {
                view.SetPageName(radialDraft.pages[page].name);
                view.SetText("PageNumber", $"{radialDraft.pages[page].name}\n{page + 1} / {radialDraft.pages.Length}");
                view.DrawSlots(radialDraft.pages[page], sector);
                view.SetText("SetupNotice", "Choose a direction, then an action. Apply saves your layout.");
                view.SetText("SelectedDirection", "Selected: " + TopDown3DRadialCanvas.Name(radialDraft.pages[page].slots[sector]));
                view.SetActive("Advanced", more);
                view.SetButton("AddPage", radialDraft.pages.Length < 4); view.SetButton("DeletePage", radialDraft.pages.Length > 1);
                view.SetText("SectorCount", radialDraft.pages[page].slots.Length + " directions");
                view.SetText("Style", radialDraft.toggleOpen ? "Toggle + Confirm" : radialDraft.explicitConfirm ? "Hold + Confirm" : "Hold + Stick Release");
                view.SetText("WheelScale", "Wheel scale: " + radialDraft.scale.ToString("0.00"));
                view.SetText("DefaultPage", radialDraft.defaultPage == page ? "Default page" : "Make default page");
            }
            view.SetText("Hints", "[" + input.GetBindingDisplayName("UI/Submit", "Select") + "] Select     ["
                + input.GetBindingDisplayName("UI/Cancel", "Back") + "] Back");
            view.RefreshNavigation();
        }

        private void Update()
        {
            if (!IsOpen || !waitingRelease) return;
            var submitHeld = false;
            var submit = input.InputActions.FindAction("UI/Submit", false);
            if (submit != null) foreach (var control in submit.controls)
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed) submitHeld = true;
            var clickHeld = Mouse.current?.leftButton.isPressed == true;
            if (submitHeld || clickHeld) return;
            waitingRelease = false; module.submit = submitReference; module.leftClick = clickReference;
            view.SetInputReady(true);
        }
        private void PromptChanged(TopDown3DPromptDevice device) { if (this != null && view != null && IsOpen) Refresh(); }
        private void InputModeChanged(TopDown3DInputMode mode)
        {
            if (mode != TopDown3DInputMode.Disabled || !IsOpen) return;
            returnInventory = false;
            if (inventory != null) inventory.EndSuspensionWithoutResume();
            Close();
        }
        private void OnDisable() { if (configured) Close(); }
        private void OnDestroy()
        {
            if (!configured) return;
            if (input != null) { input.MenuToggleRequested -= Toggle; input.UiCancelRequested -= Back; input.PromptDeviceChanged -= PromptChanged; input.ModeChanged -= InputModeChanged; }
            if (view != null) { view.Command -= OnCommand; view.Rename -= Rename; }
            if (radial != null) radial.CustomizationRequested = null;
            Close();
        }
    }
}
