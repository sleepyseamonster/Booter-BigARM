using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DMenuController
    {
        private TopDown3DControlPreferencesService controls;
        private TopDown3DControlPreferences controlsDraft;
        private bool controlsCompatible;
        private string controlGroup = "Gamepad", controlNotice;
        private int controlIndex;
        private InputAction captureControl;
        private InputActionRebindingExtensions.RebindingOperation controlRebind;
        private static readonly string[] ControlActions = { "Gameplay/Interact", "Gameplay/Sprint", "Gameplay/RecallBigArm",
            "Gameplay/DeployCanister", "Gameplay/PickupCanister", "Gameplay/DrinkAlgae", "Gameplay/EatAlgae",
            "Gameplay/TreatWithAlgae", "Gameplay/CancelItemAction", "System/ToggleInventory", "System/OpenRadial" };
        public bool IsRebindingControls => controlRebind != null;
        private bool ControlsDirty => controlsDraft != null && JsonUtility.ToJson(controlsDraft) != JsonUtility.ToJson(controls.Accepted);

        private void ConfigureControls(string directory)
        {
            InputSystem.onDeviceChange += ControlDeviceChanged;
            controls = new TopDown3DControlPreferencesService(directory);
            var inheritedNotice=string.Empty;
            if(AnimationPlaygroundInputFactory.IsLabAsset(input.InputActions)
                && !System.IO.File.Exists(System.IO.Path.Combine(directory,"controls-v1.json"))
                && directory != Application.persistentDataPath
                && System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath,"controls-v1.json")))
            {
                var production=new TopDown3DControlPreferencesService(Application.persistentDataPath);
                if(!production.CanSave || !controls.TryAdoptInitial(production.Accepted,input.InputActions,out var inheritError))
                    inheritedNotice="Existing controls need separate lab rebinding; the original file is preserved. "+(production.Notice ?? "A binding conflicts with a lab action.");
            }
            controlsCompatible = controls.ValidateForAsset(input.InputActions);
            controlNotice = string.IsNullOrEmpty(inheritedNotice) ? controls.Notice : inheritedNotice;
            if (controlsCompatible)
            {
                TopDown3DControlPreferencesService.Apply(input.InputActions, controls.Accepted);
                input.RefreshControlPrompts();
            }
        }
        private List<(InputAction action, int index)> ControlChoices()
        {
            var choices = new List<(InputAction, int)>();
            var paths=new List<string>(ControlActions);
            if(AnimationPlaygroundInputFactory.IsLabAsset(input.InputActions)) paths.AddRange(TopDown3DControlPreferencesService.LabActions);
            foreach (var path in paths)
            {
                var action = input.InputActions.FindAction(path, false);
                if (action == null) continue;
                for (var i = 0; i < action.bindings.Count; i++)
                    if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite
                        && Array.IndexOf((action.bindings[i].groups ?? "").Split(';'), controlGroup) >= 0)
                        choices.Add((action, i));
            }
            return choices;
        }
        private bool HandleControlCommand(string command)
        {
            if (command == "Controls")
            {
                controlsDraft = controls.Accepted.Clone(); controlIndex = 0;
                controlGroup = input.PromptDevice == TopDown3DPromptDevice.Gamepad ? "Gamepad" : "Keyboard&Mouse";
                controlNotice = controls.Notice; Show("Controls", "ControlDevice"); return true;
            }
            if (panel != "Controls") return false;
            if (command == "ControlsBack") { Back(); return true; }
            if (command == "ControlDevice") { controlGroup = controlGroup == "Gamepad" ? "Keyboard&Mouse" : "Gamepad"; controlIndex = 0; }
            else if (command == "ControlNext" || command == "ControlPrevious")
            { var count = ControlChoices().Count; controlIndex = (controlIndex + (command == "ControlNext" ? 1 : count - 1)) % Math.Max(1, count); }
            else if (command == "ControlHold")
            {
                var times = new[] { .2f, .35f, .5f, .65f, .8f };
                var next = Array.Find(times, value => value > controlsDraft.cargoHoldSeconds + .001f);
                controlsDraft.cargoHoldSeconds = next > 0 ? next : times[0];
            }
            else if (command == "ControlReset")
            {
                Dialog("Reset controls?", "Defaults replace your draft. Apply saves them.", "Keep controls", "Reset", () =>
                { controlsDraft = new TopDown3DControlPreferences(); controlNotice = null; Show("Controls", "ControlReset"); }); return true;
            }
            else if (command == "ControlBinding") { BeginControlRebind(); return true; }
            else if (command == "ControlsApply")
            {
                if (!controlsCompatible || !controls.TrySave(controlsDraft, input.InputActions, out controlNotice))
                { Refresh(); return true; }
                input.RefreshControlPrompts(); Show("Settings", "Controls"); return true;
            }
            Refresh(); return true;
        }
        private void RefreshControls()
        {
            var choices = ControlChoices(); controlIndex = Mathf.Clamp(controlIndex, 0, Math.Max(0, choices.Count - 1));
            view.SetText("ControlDevice", "Device: " + (controlGroup == "Gamepad" ? "Gamepad" : "Keyboard"));
            if (choices.Count > 0)
            {
                var choice = choices[controlIndex]; var binding = choice.action.bindings[choice.index];
                var entry = controlsDraft.bindings.Find(value => value.bindingId == binding.id.ToString());
                var name = TopDown3DControlPreferencesService.ActionDisplayName(choice.action.actionMap.name + "/" + choice.action.name);
                view.SetText("ControlActionName", name + $"  ({controlIndex + 1}/{choices.Count})");
                var displayPath = entry != null ? entry.path : binding.path;
                var deviceControl = InputSystem.FindControl(displayPath);
                var display = deviceControl != null ? deviceControl.shortDisplayName ?? deviceControl.displayName
                    : InputControlPath.ToHumanReadableString(displayPath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                view.SetText("ControlBinding", "Change: " + display);
            }
            view.SetText("ControlHold", "Cargo hold: " + controlsDraft.cargoHoldSeconds.ToString("0.00") + " seconds");
            view.SetText("ControlNotice", controlNotice ?? controls.Notice ?? "Choose an action and change its button. Apply saves; Back protects unapplied edits.");
            foreach (var key in new[] { "ControlBinding", "ControlHold", "ControlReset", "ControlsApply" })
                view.SetButton(key, controls.CanSave && controlsCompatible && !IsRebindingControls);
        }
        private void BeginControlRebind()
        {
            if (!controls.CanSave || !controlsCompatible || IsRebindingControls) return;
            var choices = ControlChoices(); if (choices.Count == 0) return;
            var choice = choices[controlIndex]; var path = choice.action.actionMap.name + "/" + choice.action.name;
            var id = choice.action.bindings[choice.index].id.ToString();
            captureControl = new InputAction("ControlCapture", InputActionType.Button, choice.action.bindings[choice.index].path);
            controlRebind = captureControl.PerformInteractiveRebinding(0).WithExpectedControlType("Button")
                .WithControlsHavingToMatchPath(controlGroup == "Gamepad" ? "<Gamepad>" : "<Keyboard>")
                .WithControlsExcluding("<Gamepad>/start").WithControlsExcluding("<Gamepad>/buttonSouth").WithControlsExcluding("<Gamepad>/buttonEast")
                .WithControlsExcluding("<Keyboard>/enter").WithCancelingThrough("<Keyboard>/escape").WithTimeout(10f).OnMatchWaitForAnother(0f)
                .OnGeneratePath(control => (control.device is Gamepad ? "<Gamepad>/" : "<Keyboard>/")
                    + control.path.Substring(control.device.path.Length + 1))
                .OnApplyBinding((operation, newPath) =>
                {
                    var draft = controlsDraft.Clone();
                    draft.bindings.RemoveAll(value => value.bindingId == id);
                    // Paired cargo override must follow interaction when editing an already saved pair.
                    if (path == "Gameplay/Interact")
                    {
                        var pairIds = new HashSet<string>();
                        foreach (var binding in input.InputActions.FindAction("Gameplay/OpenCargo", true).bindings)
                            if (InputBinding.MaskByGroup(controlGroup).Matches(binding)) pairIds.Add(binding.id.ToString());
                        draft.bindings.RemoveAll(value => value.actionPath == "Gameplay/OpenCargo" && pairIds.Contains(value.bindingId));
                    }
                    draft.bindings.Add(new TopDown3DControlBindingOverride { actionPath = path, bindingId = id, path = newPath });
                    if (TopDown3DControlPreferencesService.TryValidate(draft, input.InputActions, out var error))
                    { controlsDraft = draft; controlNotice = "Binding changed in your draft. Apply to save."; }
                    else controlNotice = error;
                })
                .OnComplete(operation => EndControlRebind())
                .OnCancel(operation => { controlNotice = "Binding change cancelled."; EndControlRebind(); });
            controlNotice = "Press a button for this device. Back, Escape or Start cancels; listening ends after 10 seconds.";
            module.submit = null; module.leftClick = null; view.SetInputReady(false);
            Refresh(); controlRebind.Start();
        }
        private void CancelControlRebind() => controlRebind?.Cancel();
        private void EndControlRebind()
        {
            DisposeControlRebind();
            if (IsOpen && panel == "Controls") Show("Controls", "ControlBinding");
        }
        private void DisposeControlRebind()
        {
            var operation = controlRebind; controlRebind = null; operation?.Dispose();
            captureControl?.Dispose(); captureControl = null;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelControlRebind(); }
        private void ControlDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (this == null) { InputSystem.onDeviceChange -= ControlDeviceChanged; return; }
            if (!IsRebindingControls || change != InputDeviceChange.Disconnected && change != InputDeviceChange.Removed
                && change != InputDeviceChange.Disabled) return;
            if (controlGroup == "Gamepad" && device is Gamepad || controlGroup == "Keyboard&Mouse" && device is Keyboard)
                CancelControlRebind();
        }
    }
}
