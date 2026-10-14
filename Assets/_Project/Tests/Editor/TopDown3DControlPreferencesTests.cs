using System;
using System.IO;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DControlPreferencesTests
    {
        private string directory;
        private InputActionAsset asset;
        private string FilePath => Path.Combine(directory, "controls-v1.json");

        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "BooterControlsTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            asset = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
        }
        [TearDown] public void Teardown()
        {
            UnityEngine.Object.DestroyImmediate(asset);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        private TopDown3DControlPreferences Draft(string actionPath, string path, string group = "Keyboard&Mouse")
        {
            var draft = new TopDown3DControlPreferences();
            var action = asset.FindAction(actionPath, true);
            foreach (var binding in action.bindings)
                if ((binding.groups ?? "").Contains(group))
                { draft.bindings.Add(new TopDown3DControlBindingOverride { actionPath = actionPath, bindingId = binding.id.ToString(), path = path }); return draft; }
            Assert.Fail("Missing scheme binding: " + actionPath); return null;
        }
        private string Effective(string actionPath, string group = "Keyboard&Mouse")
        {
            foreach (var binding in asset.FindAction(actionPath, true).bindings)
                if ((binding.groups ?? "").Contains(group)) return binding.effectivePath;
            return null;
        }
        [Test] public void MissingFileUsesDefaultsWithoutWriting()
        {
            var service = new TopDown3DControlPreferencesService(directory);
            Assert.That(service.CanSave, Is.True);
            Assert.That(service.Accepted.cargoHoldSeconds, Is.EqualTo(.35f));
            Assert.That(File.Exists(FilePath), Is.False);
            Assert.That(TopDown3DControlPreferencesService.TryValidate(service.Accepted, asset, out _), Is.True);
        }
        [Test] public void SingleInteractionOverridePersistsAndAppliesBothGestures()
        {
            var service = new TopDown3DControlPreferencesService(directory);
            var draft = Draft("Gameplay/Interact", "<Keyboard>/h"); draft.cargoHoldSeconds = .65f;
            Assert.That(service.TrySave(draft, asset, out string error), Is.True, error);
            Assert.That(Effective("Gameplay/Interact"), Is.EqualTo("<Keyboard>/h"));
            Assert.That(Effective("Gameplay/OpenCargo"), Is.EqualTo("<Keyboard>/h"));
            Assert.That(service.Accepted.bindings.Count, Is.EqualTo(2));
            Assert.That(asset.FindAction("Gameplay/Interact", true).GetParameterValue("tap:duration").Value.ToSingle(), Is.EqualTo(.65f));
            Assert.That(asset.FindAction("Gameplay/OpenCargo", true).GetParameterValue("hold:duration").Value.ToSingle(), Is.EqualTo(.65f));
            var restored = new TopDown3DControlPreferencesService(directory);
            Assert.That(restored.ValidateForAsset(asset), Is.True);
            Assert.That(restored.Accepted.cargoHoldSeconds, Is.EqualTo(.65f));
            var copy = restored.Accepted; copy.bindings.Clear();
            Assert.That(restored.Accepted.bindings.Count, Is.EqualTo(2));
        }
        [Test] public void ResetRestoresOwnedBindingsAndKeepsUnrelatedOverride()
        {
            var service = new TopDown3DControlPreferencesService(directory);
            asset.FindAction("Gameplay/Jump", true).ApplyBindingOverride(0, "<Keyboard>/j");
            Assert.That(service.TrySave(Draft("Gameplay/Interact", "<Keyboard>/h"), asset, out _), Is.True);
            Assert.That(service.TrySave(new TopDown3DControlPreferences(), asset, out _), Is.True);
            Assert.That(Effective("Gameplay/Interact"), Is.EqualTo("<Keyboard>/e"));
            Assert.That(asset.FindAction("Gameplay/Jump", true).bindings[0].overridePath, Is.EqualTo("<Keyboard>/j"));
            Assert.That(new TopDown3DControlPreferencesService(directory).Accepted.bindings, Is.Empty);
        }
        [Test] public void MovementOverridesRemainReservedWhenValidatingNewButtonBindings()
        {
            var move = asset.FindAction("Gameplay/Move", true);
            for (int i = 0; i < move.bindings.Count; i++)
                if (move.bindings[i].path == "<Keyboard>/w") move.ApplyBindingOverride(i, "<Keyboard>/h");
            Assert.That(TopDown3DControlPreferencesService.TryValidate(Draft("Gameplay/Sprint", "<Keyboard>/h"), asset, out var error), Is.False);
            Assert.That(error, Does.Contain("Movement"));
        }
        [TestCase("Gameplay/Interact")]
        [TestCase("Gameplay/OpenCargo")]
        [TestCase("Gameplay/Sprint")]
        [TestCase("Gameplay/PickupCanister")]
        [TestCase("System/ToggleInventory")]
        [TestCase("System/OpenRadial")]
        public void FreedDpadDirectionsCannotBypassCardinalArbitration(string actionPath)
        {
            var draft = Draft("Gameplay/TreatWithAlgae", "<Gamepad>/rightTrigger", "Gamepad");
            draft.bindings.Add(Draft(actionPath, "<Gamepad>/dpad/up", "Gamepad").bindings[0]);
            Assert.That(TopDown3DControlPreferencesService.TryValidate(draft, asset, out var error), Is.False);
            Assert.That(error, Does.Contain("D-pad directions"));
        }
        [TestCase("<Keyboard>/escape")]
        [TestCase("<Keyboard>/enter")]
        [TestCase("<Keyboard>/space")]
        [TestCase("<Keyboard>/w")]
        [TestCase("<Keyboard>/upArrow")]
        [TestCase("<Keyboard>/notAKey")]
        [TestCase("<Gamepad>/buttonWest")]
        [TestCase("<Keyboard>/*")]
        public void RejectsReservedUnknownAndWrongDeviceKeys(string path)
        { Assert.That(TopDown3DControlPreferencesService.TryValidate(Draft("Gameplay/Sprint", path), asset, out _), Is.False); }
        [TestCase("<Gamepad>/start")]
        [TestCase("<Gamepad>/select")]
        [TestCase("<Gamepad>/buttonSouth")]
        [TestCase("<Gamepad>/buttonEast")]
        [TestCase("<Gamepad>/dpad")]
        [TestCase("<Gamepad>/leftStick")]
        [TestCase("<Gamepad>/rightStickPress")]
        [TestCase("<Gamepad>/buttonNorth")]
        [TestCase("<Gamepad>/leftTrigger")]
        public void RejectsReservedAmbiguousAndCoactivePadControls(string path)
        { Assert.That(TopDown3DControlPreferencesService.TryValidate(Draft("Gameplay/Sprint", path, "Gamepad"), asset, out _), Is.False); }
        [Test] public void CrossMapConflictRejectedAndSimultaneousSwapAllowed()
        {
            var draft = Draft("Gameplay/PickupCanister", "<Keyboard>/q");
            Assert.That(TopDown3DControlPreferencesService.TryValidate(draft, asset, out _), Is.False);
            draft.bindings.Add(Draft("System/OpenRadial", "<Keyboard>/c").bindings[0]);
            Assert.That(TopDown3DControlPreferencesService.TryValidate(draft, asset, out _), Is.True);
        }
        [Test] public void KeyboardAndGamepadChangesAreIndependentAndInventoryNorthRemainsValid()
        {
            var draft = Draft("Gameplay/Interact", "<Keyboard>/h");
            draft.bindings.Add(Draft("Gameplay/Interact", "<Gamepad>/rightTrigger", "Gamepad").bindings[0]);
            draft.bindings.Add(Draft("System/ToggleInventory", "<Gamepad>/buttonNorth", "Gamepad").bindings[0]);
            Assert.That(TopDown3DControlPreferencesService.TryValidate(draft, asset, out string error), Is.True, error);
            TopDown3DControlPreferencesService.Apply(asset, draft);
            Assert.That(Effective("Gameplay/OpenCargo"), Is.EqualTo("<Keyboard>/h"));
            Assert.That(Effective("Gameplay/Interact", "Gamepad"), Is.EqualTo("<Gamepad>/rightTrigger"));
            Assert.That(Effective("Gameplay/OpenCargo", "Gamepad"), Is.Empty, "Legacy paired overrides cannot restore gamepad cargo opening.");
        }
        [Test] public void RejectsDuplicateWrongOwnerDormantAndDivergentPairs()
        {
            var draft = Draft("Gameplay/Interact", "<Keyboard>/h");
            draft.bindings.Add(draft.bindings[0]);
            Assert.That(TopDown3DControlPreferencesService.TryValidate(draft, asset, out _), Is.False);
            draft = Draft("Gameplay/Jump", "<Keyboard>/h");
            Assert.That(TopDown3DControlPreferencesService.TryValidate(draft, asset, out _), Is.False);
            draft = Draft("Gameplay/Interact", "<Keyboard>/h");
            draft.bindings[0].actionPath = "Gameplay/Sprint";
            Assert.That(TopDown3DControlPreferencesService.TryValidate(draft, asset, out _), Is.False);
            draft = Draft("Gameplay/Interact", "<Keyboard>/h");
            draft.bindings.Add(Draft("Gameplay/OpenCargo", "<Keyboard>/j").bindings[0]);
            Assert.That(TopDown3DControlPreferencesService.TryValidate(draft, asset, out _), Is.False);
        }
        [TestCase(.19f)] [TestCase(.81f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidTimingDoesNotMutateAssetOrAccepted(float timing)
        {
            var service = new TopDown3DControlPreferencesService(directory);
            var draft = Draft("Gameplay/Interact", "<Keyboard>/h"); draft.cargoHoldSeconds = timing;
            Assert.That(service.TrySave(draft, asset, out _), Is.False);
            Assert.That(service.Accepted.cargoHoldSeconds, Is.EqualTo(.35f));
            Assert.That(Effective("Gameplay/Interact"), Is.EqualTo("<Keyboard>/e"));
        }
        [TestCase("{\"version\":2,\"cargoHoldSeconds\":0.35,\"bindings\":[]}")]
        [TestCase("broken json")]
        [TestCase("{}")]
        [TestCase("{\"version\":0}")]
        [TestCase("{\"version\":1}")]
        public void UnknownOrCorruptFilePreservedAndBlocksWrites(string json)
        {
            File.WriteAllText(FilePath, json);
            var service = new TopDown3DControlPreferencesService(directory);
            Assert.That(service.CanSave, Is.False);
            Assert.That(service.TrySave(new TopDown3DControlPreferences(), asset, out _), Is.False);
            Assert.That(File.ReadAllText(FilePath), Is.EqualTo(json));
        }
        [Test] public void IncompatibleBindingFilePreservedAndDefaultsUsed()
        {
            var draft = Draft("Gameplay/Interact", "<Keyboard>/h"); draft.bindings[0].bindingId = Guid.NewGuid().ToString();
            string json = JsonUtility.ToJson(draft); File.WriteAllText(FilePath, json);
            var service = new TopDown3DControlPreferencesService(directory);
            Assert.That(service.ValidateForAsset(asset), Is.False);
            Assert.That(service.CanSave, Is.False);
            Assert.That(service.Accepted.bindings, Is.Empty);
            Assert.That(File.ReadAllText(FilePath), Is.EqualTo(json));
        }
        [Test] public void FailedWriteRetainsAcceptedAndRuntimeBindings()
        {
            string blockedDirectory = Path.Combine(directory, "blocked"); File.WriteAllText(blockedDirectory, "file blocks directory creation");
            var service = new TopDown3DControlPreferencesService(blockedDirectory);
            Assert.That(service.TrySave(Draft("Gameplay/Interact", "<Keyboard>/h"), asset, out _), Is.False);
            Assert.That(service.Accepted.bindings, Is.Empty);
            Assert.That(Effective("Gameplay/Interact"), Is.EqualTo("<Keyboard>/e"));
        }
    }
}
