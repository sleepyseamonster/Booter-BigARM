using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DControlBindingOverride
    {
        public string actionPath;
        public string bindingId;
        public string path;
    }

    [Serializable]
    public sealed class TopDown3DControlPreferences
    {
        public int version = 1;
        public float cargoHoldSeconds = .35f;
        public List<TopDown3DControlBindingOverride> bindings = new();
        public TopDown3DControlPreferences Clone() => JsonUtility.FromJson<TopDown3DControlPreferences>(JsonUtility.ToJson(this));
    }

    /// <summary>Local device preferences; never part of world identity or saved runtime deltas.</summary>
    public sealed class TopDown3DControlPreferencesService
    {
        private readonly string filePath;
        private TopDown3DControlPreferences accepted = new();
        public TopDown3DControlPreferences Accepted => accepted.Clone();
        public bool CanSave { get; private set; } = true;
        public string Notice { get; private set; }
        private static readonly HashSet<string> Active = new(StringComparer.Ordinal)
        {
            "Gameplay/Interact", "Gameplay/OpenCargo", "Gameplay/Sprint", "Gameplay/RecallBigArm",
            "Gameplay/DeployCanister", "Gameplay/PickupCanister", "Gameplay/DrinkAlgae", "Gameplay/EatAlgae",
            "Gameplay/TreatWithAlgae", "Gameplay/CancelItemAction", "System/ToggleInventory", "System/OpenRadial"
        };
        private static readonly HashSet<string> PadButtons = new(StringComparer.OrdinalIgnoreCase)
        { "buttonWest", "buttonEast", "buttonNorth", "leftShoulder", "rightShoulder", "leftTrigger", "rightTrigger", "dpad/up", "dpad/down", "dpad/left", "dpad/right" };
        public static readonly string[] LabActions = { "Gameplay/Jump", "Gameplay/Attack", "Gameplay/Crouch",
            "Gameplay/AnimationSlowWalk", "Gameplay/AnimationCancel", "Gameplay/AnimationDodge", AnimationPlaygroundInputFactory.PalettePath };
        private static bool IsActive(InputActionAsset asset,string path) => Active.Contains(path)
            || AnimationPlaygroundInputFactory.IsLabAsset(asset) && Array.IndexOf(LabActions,path)>=0;
        private static readonly HashSet<string> CardinalActions = new(StringComparer.Ordinal)
        { "Gameplay/DeployCanister", "Gameplay/DrinkAlgae", "Gameplay/EatAlgae", "Gameplay/TreatWithAlgae" };
        private static readonly HashSet<string> Keys = new(StringComparer.OrdinalIgnoreCase)
        {
            "tab", "backspace", "delete", "insert", "home", "end", "pageUp", "pageDown",
            "leftShift", "rightShift", "leftCtrl", "rightCtrl", "leftAlt", "rightAlt", "capsLock",
            "leftBracket", "rightBracket", "semicolon", "quote", "comma", "period", "slash", "backslash", "minus", "equals", "backquote"
        };

        public TopDown3DControlPreferencesService(string directory)
        {
            filePath = Path.Combine(directory, "controls-v1.json");
            try
            {
                if (!File.Exists(filePath)) return;
                string json = File.ReadAllText(filePath);
                // Require a version field: omitted fields must never silently become an accepted schema.
                if (!System.Text.RegularExpressions.Regex.IsMatch(json, "\"version\"\\s*:")) throw new ArgumentException();
                var saved = JsonUtility.FromJson<TopDown3DControlPreferences>(json);
                if (saved != null && saved.version > 1)
                { CanSave = false; Notice = "Controls are from a newer version. The original file is preserved."; return; }
                if (!System.Text.RegularExpressions.Regex.IsMatch(json, "\"cargoHoldSeconds\"\\s*:")
                    || !System.Text.RegularExpressions.Regex.IsMatch(json, "\"bindings\"\\s*:")) throw new ArgumentException();
                if (!ValidStructure(saved, out _)) throw new ArgumentException();
                accepted = saved;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { CanSave = false; Notice = "Controls could not be read safely. Defaults are in use; the original file is preserved."; }
        }

        public bool TrySave(TopDown3DControlPreferences draft, InputActionAsset asset, out string error)
        {
            error = null;
            if (!CanSave) { error = Notice; return false; }
            if (!TryValidate(draft, asset, out error)) return false;
            var normalized = Normalize(draft, asset);
            string temporary = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(temporary, JsonUtility.ToJson(normalized, true));
                if (File.Exists(filePath)) File.Replace(temporary, filePath, null);
                else File.Move(temporary, filePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                error = "Could not save controls. Your edits are retained; try Apply again."; return false;
            }
            Apply(asset, normalized);
            accepted = normalized; Notice = null;
            return true;
        }

        public bool ValidateForAsset(InputActionAsset asset)
        {
            accepted = AdaptDodgePreferences(accepted, asset);
            if (TryValidate(accepted, asset, out string error)) return CanSave;
            RetainForRecovery("Saved controls are incompatible with this version. Defaults are in use; the original file is preserved. " + error);
            accepted = new TopDown3DControlPreferences();
            return false;
        }

        // Adapt only the superseded lab defaults in memory. The original preference file remains recoverable.
        private static TopDown3DControlPreferences AdaptDodgePreferences(TopDown3DControlPreferences source, InputActionAsset asset)
        {
            if (source == null || !AnimationPlaygroundInputFactory.IsLabAsset(asset)
                || asset.FindAction("Gameplay/AnimationDodge", false) == null) return source;
            var result = source.Clone();
            result.bindings.RemoveAll(entry => (entry.actionPath == AnimationPlaygroundInputFactory.PalettePath
                    && entry.path == "<Gamepad>/select" || entry.actionPath == "Gameplay/AnimationCancel"
                    && entry.path == "<Gamepad>/buttonEast")
                && FindBinding(asset.FindAction(entry.actionPath, false), entry.bindingId) < 0);
            foreach (var entry in result.bindings)
                if (entry.actionPath == "Gameplay/PickupCanister" && entry.path == "<Gamepad>/buttonEast")
                    entry.path = "<Gamepad>/select";
            return result;
        }

        public void RetainForRecovery(string notice)
        { CanSave = false; Notice = notice; }

        public bool TryAdoptInitial(TopDown3DControlPreferences source,InputActionAsset asset,out string error)
        {
            if(File.Exists(filePath) || !CanSave){error="Preserve the existing lab controls file.";return false;}
            source = AdaptDodgePreferences(source, asset);
            if(!TryValidate(source,asset,out error)) return false;
            accepted=Normalize(source,asset);return true;
        }

        public static string ActionDisplayName(string path) => path switch
        {
            "Gameplay/Interact" or "Gameplay/OpenCargo" => "Interact",
            "Gameplay/Sprint" => "Sprint",
            "Gameplay/RecallBigArm" => "Call Legger over",
            "Gameplay/DeployCanister" => "Deploy canister",
            "Gameplay/PickupCanister" => "Pick up canister / cancel",
            "Gameplay/DrinkAlgae" => "Drink algae",
            "Gameplay/EatAlgae" => "Eat algae",
            "Gameplay/TreatWithAlgae" => "Treat with algae",
            "Gameplay/CancelItemAction" => "Cancel item action",
            "System/ToggleInventory" => "Open inventory",
            "System/OpenRadial" => "Open radial",
            "Gameplay/Move" => "Movement",
            "Gameplay/CameraLookAhead" => "Camera look-ahead",
            "Gameplay/Jump" => "Jump",
            "Gameplay/Attack" => "Selected animation action",
            "Gameplay/Crouch" => "Crouch / slide",
            "Gameplay/AnimationSlowWalk" => "Slow walk",
            "Gameplay/AnimationCancel" => "Cancel animation action",
            "System/AnimationPalette" => "Animation palette",
            "Gameplay/AnimationDodge" => "Dodge roll",
            _ => "another action"
        };

        public static bool TryValidate(TopDown3DControlPreferences draft, InputActionAsset asset, out string error)
        {
            if (!ValidStructure(draft, out error)) return false;
            if (asset == null) { error = "Controls are unavailable."; return false; }
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in draft.bindings)
            {
                var action = asset.FindAction(entry.actionPath, false);
                int index = FindBinding(action, entry.bindingId);
                if (!IsActive(asset,entry.actionPath) || index < 0 || !Editable(action.bindings[index]))
                { error = "This control cannot be edited."; return false; }
                if (!ids.Add(entry.bindingId)) { error = "A control was edited more than once."; return false; }
                var binding = action.bindings[index];
                bool labPath=AnimationPlaygroundInputFactory.IsLabAsset(asset) &&
                    (Group(binding)=="Keyboard&Mouse" && entry.path == "<Keyboard>/space"
                    || Group(binding)=="Gamepad" && (entry.path == "<Gamepad>/buttonSouth"
                    || (entry.actionPath == AnimationPlaygroundInputFactory.PalettePath || entry.actionPath == "Gameplay/PickupCanister") && entry.path == "<Gamepad>/select"
                    || entry.actionPath == "Gameplay/Crouch" && entry.path == "<Gamepad>/leftStickPress"));
                if (!AllowedPath(entry.path, Group(binding)) && !labPath) { error = "Choose a supported button or keyboard key. Menu and movement controls are reserved."; return false; }
                if (entry.path.StartsWith("<Gamepad>/dpad/", StringComparison.OrdinalIgnoreCase) && !CardinalActions.Contains(entry.actionPath))
                { error = "D-pad directions are reserved for algae and canister placement."; return false; }
                // System inventory remains enabled while UI is active; its opener cannot
                // share a fixed transfer/panel/navigation command in that captured mode.
                if (entry.actionPath == "System/ToggleInventory")
                {
                    var ui = asset.FindActionMap("UI", false);
                    if (ui != null) foreach (var uiBinding in ui.bindings)
                        if (!uiBinding.isComposite && string.Equals(entry.path, uiBinding.effectivePath, StringComparison.OrdinalIgnoreCase))
                        { error = "That button is reserved for inventory navigation or transfer."; return false; }
                }
                if (string.Equals(entry.path, "<Gamepad>/buttonEast", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(binding.path, entry.path, StringComparison.OrdinalIgnoreCase))
                { error = AnimationPlaygroundInputFactory.IsLabAsset(asset) ? "East is reserved for Back and dodge." : "East is reserved for Back and canister pickup."; return false; }
            }
            foreach (string group in new[] { "Gamepad", "Keyboard&Mouse" })
            {
                string pairedPath = null;
                foreach (var entry in draft.bindings)
                {
                    if (entry.actionPath != "Gameplay/Interact" && entry.actionPath != "Gameplay/OpenCargo") continue;
                    var action = asset.FindAction(entry.actionPath, false);
                    if (Group(action.bindings[FindBinding(action, entry.bindingId)]) != group) continue;
                    if (pairedPath != null && !string.Equals(pairedPath, entry.path, StringComparison.OrdinalIgnoreCase))
                    { error = "Tap interaction and cargo hold must use the same button."; return false; }
                    pairedPath = entry.path;
                }
            }
            var normalized = Normalize(draft, asset);
            var used = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var map in asset.actionMaps)
            foreach (var action in map.actions)
            {
                string actionPath = map.name + "/" + action.name;
                bool reservation = actionPath == "Gameplay/Move" || actionPath == "Gameplay/CameraLookAhead";
                if (!IsActive(asset,actionPath) && !reservation) continue;
                foreach (var binding in action.bindings)
                {
                    if (binding.isComposite || string.IsNullOrEmpty(Group(binding))) continue;
                    string path = reservation ? binding.effectivePath : binding.path;
                    foreach (var entry in normalized.bindings) if (entry.bindingId == binding.id.ToString()) path = entry.path;
                    string key = Group(binding) + ":" + path;
                    string canonicalAction = actionPath == "Gameplay/OpenCargo" ? "Gameplay/Interact" : actionPath;
                    if(actionPath == "Gameplay/AnimationCancel")
                        canonicalAction = path == "<Gamepad>/buttonEast" ? "Gameplay/PickupCanister"
                            : path == "<Keyboard>/backspace" ? "Gameplay/CancelItemAction" : actionPath;
                    if (used.TryGetValue(key, out string previous) && previous != canonicalAction)
                    {
                        // Report the other action when the earlier entry is the control being edited.
                        var conflict=previous;
                        foreach(var entry in normalized.bindings) if(entry.actionPath == previous){conflict=canonicalAction;break;}
                        error = "That button is already used by " + ActionDisplayName(conflict) + "."; return false;
                    }
                    used[key] = canonicalAction;
                }
            }
            error = null; return true;
        }

        public static void Apply(InputActionAsset asset, TopDown3DControlPreferences preferences)
        {
            if (!TryValidate(preferences, asset, out string error)) throw new ArgumentException(error);
            var normalized = Normalize(preferences, asset);
            foreach (var map in asset.actionMaps)
            foreach (var action in map.actions)
            {
                if (!IsActive(asset,map.name + "/" + action.name)) continue;
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (!Editable(action.bindings[i])) continue;
                    action.RemoveBindingOverride(i);
                    string path = null;
                    foreach (var entry in normalized.bindings) if (entry.bindingId == action.bindings[i].id.ToString()) path = entry.path;
                    if (path != null) action.ApplyBindingOverride(i, path);
                }
                // Parameter overrides change the existing action interaction. Adding binding interactions
                // would append a second Tap/Hold beside the shipped action interaction.
                if (action.name == "Interact") action.ApplyParameterOverride("tap:duration", normalized.cargoHoldSeconds);
                if (action.name == "OpenCargo") action.ApplyParameterOverride("hold:duration", normalized.cargoHoldSeconds);
            }
            DisableGamepadCargoShortcut(asset);
        }

        // Preserve existing preference binding IDs and the keyboard gesture while retiring
        // the gamepad cargo shortcut, including overrides from the old paired control row.
        public static void DisableGamepadCargoShortcut(InputActionAsset asset)
        {
            var cargo = asset?.FindAction("Gameplay/OpenCargo", false);
            if (cargo == null) return;
            for (var i = 0; i < cargo.bindings.Count; i++)
                if (Group(cargo.bindings[i]) == "Gamepad") cargo.ApplyBindingOverride(i, string.Empty);
        }

        public static TopDown3DControlPreferences Normalize(TopDown3DControlPreferences draft, InputActionAsset asset)
        {
            var result = draft.Clone();
            foreach (var entry in result.bindings) entry.bindingId = Guid.Parse(entry.bindingId).ToString();
            foreach (string group in new[] { "Gamepad", "Keyboard&Mouse" })
            {
                TopDown3DControlBindingOverride source = null;
                foreach (var entry in result.bindings)
                {
                    if (entry.actionPath != "Gameplay/Interact" && entry.actionPath != "Gameplay/OpenCargo") continue;
                    var action = asset.FindAction(entry.actionPath, false);
                    if (Group(action.bindings[FindBinding(action, entry.bindingId)]) == group)
                    { source = entry; if (entry.actionPath == "Gameplay/Interact") break; }
                }
                if (source == null) continue;
                foreach (string actionPath in new[] { "Gameplay/Interact", "Gameplay/OpenCargo" })
                {
                    var action = asset.FindAction(actionPath, false);
                    if (action == null) continue;
                    foreach (var binding in action.bindings)
                    {
                        if (!Editable(binding) || Group(binding) != group) continue;
                        var target = result.bindings.Find(e => e.bindingId == binding.id.ToString());
                        if (target == null) result.bindings.Add(new TopDown3DControlBindingOverride { actionPath = actionPath, bindingId = binding.id.ToString(), path = source.path });
                        else target.path = source.path;
                    }
                }
            }
            return result;
        }

        private static bool ValidStructure(TopDown3DControlPreferences draft, out string error)
        {
            error = "Controls preferences are invalid.";
            if (draft == null || draft.version != 1 || float.IsNaN(draft.cargoHoldSeconds) || float.IsInfinity(draft.cargoHoldSeconds)
                || draft.cargoHoldSeconds < .2f || draft.cargoHoldSeconds > .8f || draft.bindings == null) return false;
            foreach (var entry in draft.bindings)
                if (entry == null || string.IsNullOrEmpty(entry.actionPath) || !Guid.TryParse(entry.bindingId, out _) || string.IsNullOrEmpty(entry.path)) return false;
            error = null; return true;
        }
        private static int FindBinding(InputAction action, string id)
        {
            if (action == null) return -1;
            for (int i = 0; i < action.bindings.Count; i++) if (string.Equals(action.bindings[i].id.ToString(), id, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
        private static bool Editable(InputBinding binding) => !binding.isComposite && !binding.isPartOfComposite && Group(binding) != null;
        private static string Group(InputBinding binding)
        {
            foreach (string group in (binding.groups ?? "").Split(';')) if (group == "Gamepad" || group == "Keyboard&Mouse") return group;
            return null;
        }
        private static bool AllowedPath(string path, string group)
        {
            if (group == "Gamepad") return path.StartsWith("<Gamepad>/", StringComparison.OrdinalIgnoreCase) && PadButtons.Contains(path.Substring(10));
            if (!path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase)) return false;
            string key = path.Substring(11);
            if (Keys.Contains(key)) return true;
            if (key.Length == 1) return (key[0] >= 'a' && key[0] <= 'z') || (key[0] >= '0' && key[0] <= '9');
            return key.StartsWith("f", StringComparison.OrdinalIgnoreCase) && int.TryParse(key.Substring(1), out int number) && number >= 1 && number <= 12;
        }
    }
}
