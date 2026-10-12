using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DAlgaeInputTests
    {
        private InputSettings original,manual;
        private InputActionAsset asset;
        private GameObject player;
        private TopDown3DInputRouter router;
        private Gamepad pad;
        private Keyboard keyboard;
        private readonly List<TopDown3DAlgaeUse> requests=new();
        private int deploymentRequests;
        [SetUp] public void Setup()
        {
            original=InputSystem.settings;manual=Object.Instantiate(original);
            manual.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);
            manual.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
            manual.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=manual;
            pad=InputSystem.AddDevice<Gamepad>();keyboard=InputSystem.AddDevice<Keyboard>();
            asset=Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            player=new GameObject("Algae input test");player.SetActive(false);router=player.AddComponent<TopDown3DInputRouter>();
            router.Configure(asset);player.SetActive(true);router.Initialize();router.AlgaeUseRequested+=requests.Add;
            router.DeployCanisterRequested+=()=>deploymentRequests++;
            deploymentRequests=0;requests.Clear();Send(new GamepadState());
        }
        [TearDown] public void Cleanup()
        {
            typeof(TopDown3DInputRouter).GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(router,null);
            Object.DestroyImmediate(player);Object.DestroyImmediate(asset);InputSystem.RemoveDevice(pad);InputSystem.RemoveDevice(keyboard);
            InputSystem.settings=original;Object.DestroyImmediate(manual);
        }
        private void Send(GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();}
        [Test] public void CardinalsUseOnceAndRequireNeutral()
        {
            Send(new GamepadState().WithButton(GamepadButton.DpadLeft));Send(new GamepadState().WithButton(GamepadButton.DpadLeft));
            Send(new GamepadState().WithButton(GamepadButton.DpadUp));Assert.That(requests,Is.EqualTo(new[]{TopDown3DAlgaeUse.Drink}));
            Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.DpadRight));
            Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.DpadUp));
            Assert.That(requests,Is.EqualTo(new[]{TopDown3DAlgaeUse.Drink,TopDown3DAlgaeUse.Eat,TopDown3DAlgaeUse.Treat}));
        }
        [Test] public void DiagonalsAndSimultaneousKeysDoNotConsume()
        {
            Send(new GamepadState().WithButton(GamepadButton.DpadLeft).WithButton(GamepadButton.DpadUp));
            Assert.That(requests,Is.Empty);Send(new GamepadState());
            Send(new GamepadState().WithButton(GamepadButton.DpadLeft).WithButton(GamepadButton.DpadDown));
            Assert.That(requests,Is.Empty);Send(new GamepadState());
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F5));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F5,Key.F6));InputSystem.Update();Assert.That(requests,Is.Empty);
        }
        [Test] public void MenuWinsSameUpdateAndHeldReturnCannotConsume()
        {
            router.MenuToggleRequested+=router.EnterSystemMenuMode;
            Send(new GamepadState().WithButton(GamepadButton.Start).WithButton(GamepadButton.DpadUp));
            Assert.That(requests,Is.Empty);Assert.That(router.Mode,Is.EqualTo(TopDown3DInputMode.SystemMenu));
            router.EnterGameplayMode();Send(new GamepadState().WithButton(GamepadButton.DpadUp));Assert.That(requests,Is.Empty);
            Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.DpadUp));Assert.That(requests.Count,Is.EqualTo(1));
        }
        [Test] public void CampOwnsDpadAndReboundButtonWorksWithoutDpad()
        {
            router.EnterCampMode(TopDown3DInputMode.CampPlacement);
            Send(new GamepadState().WithButton(GamepadButton.DpadLeft));Assert.That(requests,Is.Empty);
            Send(new GamepadState());router.EnterGameplayMode();Send(new GamepadState());
            var action=asset.FindAction("Gameplay/DrinkAlgae");
            for(var i=0;i<action.bindings.Count;i++)if(action.bindings[i].path=="<Gamepad>/dpad/left")action.ApplyBindingOverride(i,"<Gamepad>/rightShoulder");
            Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.RightShoulder));
            Assert.That(requests,Is.EqualTo(new[]{TopDown3DAlgaeUse.Drink}));
        }
        [Test] public void FocusLossRequiresReleaseAndDustDownDoesNotConsume()
        {
            Send(new GamepadState().WithButton(GamepadButton.DpadDown));Assert.That(requests,Is.Empty);Send(new GamepadState());
            typeof(TopDown3DInputRouter).GetMethod("OnApplicationFocus",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(router,new object[]{false});
            Send(new GamepadState().WithButton(GamepadButton.DpadLeft));Assert.That(requests,Is.Empty);
            Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.DpadLeft));Assert.That(requests.Count,Is.EqualTo(1));
        }
        [Test] public void DeviceRemovalDoesNotReadDetachedDpad()
        {
            Send(new GamepadState().WithButton(GamepadButton.DpadLeft));
            InputSystem.RemoveDevice(pad);InputSystem.Update();
            InputSystem.AddDevice(pad);Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.DpadUp));
            Assert.That(requests,Is.EqualTo(new[]{TopDown3DAlgaeUse.Drink,TopDown3DAlgaeUse.Treat}));
        }
        [Test] public void RapidReleasedDiagonalStillRejectsAndFreshTapRearms()
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadLeft).WithButton(GamepadButton.DpadDown));
            InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();Assert.That(requests,Is.Empty);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadUp));
            InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
            Assert.That(requests,Is.EqualTo(new[]{TopDown3DAlgaeUse.Treat}));
        }
        [Test] public void DustDownRequiresNeutralBeforePlacementConfirmation()
        {
            Send(new GamepadState().WithButton(GamepadButton.DpadDown));
            Send(new GamepadState().WithButton(GamepadButton.DpadDown));
            Send(new GamepadState().WithButton(GamepadButton.DpadLeft));
            Assert.That(deploymentRequests,Is.EqualTo(1));Assert.That(requests,Is.Empty);
            Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.DpadDown));
            Assert.That(deploymentRequests,Is.EqualTo(2));
        }
        [Test] public void DownDiagonalsRejectDeploymentEvenWhenReleasedInSameUpdate()
        {
            foreach(var adjacent in new[]{GamepadButton.DpadLeft,GamepadButton.DpadRight})
            {
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadDown).WithButton(adjacent));
                InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
            }
            Assert.That(deploymentRequests,Is.Zero);Assert.That(requests,Is.Empty);
            Send(new GamepadState().WithButton(GamepadButton.DpadDown));Assert.That(deploymentRequests,Is.EqualTo(1));
        }
        [Test] public void MenuAndHeldReturnCannotDeploy()
        {
            router.MenuToggleRequested+=router.EnterSystemMenuMode;
            Send(new GamepadState().WithButton(GamepadButton.Start).WithButton(GamepadButton.DpadDown));
            Assert.That(deploymentRequests,Is.Zero);
            router.EnterGameplayMode();Send(new GamepadState().WithButton(GamepadButton.DpadDown));
            Assert.That(deploymentRequests,Is.Zero);
            Send(new GamepadState());Send(new GamepadState().WithButton(GamepadButton.DpadDown));
            Assert.That(deploymentRequests,Is.EqualTo(1));
        }
        [Test] public void SimultaneousKeyboardDoseAndDeployRejectBoth()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.G,Key.F5));InputSystem.Update();
            Assert.That(deploymentRequests,Is.Zero);Assert.That(requests,Is.Empty);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.G));InputSystem.Update();
            Assert.That(deploymentRequests,Is.EqualTo(1));
        }
    }
}
