using BooterBigArm.TopDown3D;
using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundInputTests
    {
        [Test]
        public void OwnedAssetPreservesShippedIdsAndDeterministicLabBindings()
        {
            var source=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            var before=source.ToJson();
            var first=AnimationPlaygroundInputFactory.Create(source); var second=AnimationPlaygroundInputFactory.Create(source);
            try
            {
                Assert.That(first,Is.Not.SameAs(source));Assert.That(source.ToJson(),Is.EqualTo(before));
                foreach(var map in source.actionMaps) foreach(var action in map.actions)
                {
                    var cloned=first.FindAction(map.name+"/"+action.name,true);
                    Assert.That(cloned.id,Is.EqualTo(action.id));
                    foreach(var binding in action.bindings)
                        Assert.That(System.Linq.Enumerable.Any(cloned.bindings,b=>b.id == binding.id),Is.True);
                }
                foreach(var map in first.actionMaps) foreach(var action in map.actions)
                {
                    var repeated=second.FindAction(map.name+"/"+action.name,true);
                    Assert.That(action.id,Is.EqualTo(repeated.id));
                    for(var i=0;i<action.bindings.Count;i++) Assert.That(action.bindings[i].id,Is.EqualTo(repeated.bindings[i].id));
                }
                Assert.That(AnimationPlaygroundInputFactory.IsLabAsset(first),Is.True);
                Assert.That(AnimationPlaygroundInputFactory.IsLabAsset(source),Is.False);
            }
            finally{Object.DestroyImmediate(first);Object.DestroyImmediate(second);}
        }
        [Test]
        public void CompatibleOverridesAreCopiedWithoutMutatingTheSource()
        {
            var shipped=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            var source=InputActionAsset.FromJson(shipped.ToJson());InputActionAsset owned=null;
            try
            {
                var action=source.FindAction("Gameplay/RecallBigArm",true);
                var index=0;action.ApplyBindingOverride(index,"<Keyboard>/f4");
                var before=source.SaveBindingOverridesAsJson();owned=AnimationPlaygroundInputFactory.Create(source);
                Assert.That(owned.FindAction("Gameplay/RecallBigArm",true).bindings[index].effectivePath,Is.EqualTo("<Keyboard>/f4"));
                Assert.That(source.SaveBindingOverridesAsJson(),Is.EqualTo(before));
            }
            finally{if(owned!=null)Object.DestroyImmediate(owned);Object.DestroyImmediate(source);}
        }
        [Test]
        public void LabCoactiveBindingsRejectNewActionConflictsWithoutChangingProductionPolicy()
        {
            var shipped=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            var owned=AnimationPlaygroundInputFactory.Create(shipped);
            try
            {
                Assert.That(TopDown3DControlPreferencesService.TryValidate(new TopDown3DControlPreferences(),owned,out var error),Is.True,error);
                var recall=owned.FindAction("Gameplay/RecallBigArm",true);
                var draft=new TopDown3DControlPreferences();
                draft.bindings.Add(new TopDown3DControlBindingOverride{actionPath="Gameplay/RecallBigArm",bindingId=recall.bindings[0].id.ToString(),path="<Keyboard>/v"});
                Assert.That(TopDown3DControlPreferencesService.TryValidate(draft,owned,out error),Is.False);
                Assert.That(error,Does.Contain("Crouch"));
                Assert.That(TopDown3DControlPreferencesService.TryValidate(draft,shipped,out error),Is.True,error);
            }
            finally{Object.DestroyImmediate(owned);}
        }
        [Test]
        public void HeldJumpAndDodgeCannotLeakThroughModalReturnAndViewPicksUpCanister()
        {
            var oldSettings=InputSystem.settings;var settings=Object.Instantiate(oldSettings);
            settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);InputSystem.settings=settings;
            var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
            var shipped=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            var actions=AnimationPlaygroundInputFactory.Create(shipped);
            var root=new GameObject("Lab router fixture");root.SetActive(false);
            try
            {
                var router=root.AddComponent<TopDown3DInputRouter>();router.Configure(actions);Assert.That(router.Initialize(),Is.True);
                var jumps=0;var cancels=0;var pickups=0;var dodges=0;
                router.AnimationJumpRequested+=()=>jumps++;router.AnimationCancelRequested+=()=>cancels++;router.PickupCanisterRequested+=()=>pickups++;router.AnimationDodgeRequested+=()=>dodges++;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();Assert.That(jumps,Is.EqualTo(1));
                router.EnterSystemMenuMode();router.EnterGameplayMode();InputSystem.Update();Assert.That(jumps,Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();Assert.That(jumps,Is.EqualTo(2));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                router.AnimationActionOwned=()=>true;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));InputSystem.Update();
                Assert.That(dodges,Is.EqualTo(1));Assert.That(cancels,Is.Zero);Assert.That(pickups,Is.Zero);
                InputSystem.Update();Assert.That(dodges,Is.EqualTo(1),"Holding East must not repeat.");
                router.EnterSystemMenuMode();router.EnterGameplayMode();InputSystem.Update();Assert.That(dodges,Is.EqualTo(1));
                InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));InputSystem.Update();
                Assert.That(dodges,Is.EqualTo(2));Assert.That(pickups,Is.Zero);
                InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.Select));InputSystem.Update();
                Assert.That(pickups,Is.EqualTo(1));
                InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));InputSystem.Update();
                Assert.That(dodges,Is.EqualTo(3)); InputSystem.RemoveDevice(pad);
                pad=InputSystem.AddDevice<Gamepad>();
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));InputSystem.Update();
                Assert.That(dodges,Is.EqualTo(3),"A replacement device must return to neutral before dodging.");
                InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));InputSystem.Update();
                Assert.That(dodges,Is.EqualTo(4));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Backspace));InputSystem.Update();Assert.That(cancels,Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);Object.DestroyImmediate(actions);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);
                InputSystem.settings=oldSettings;Object.DestroyImmediate(settings);
            }
        }
        [TestCase("Facing", false)]
        [TestCase("Walk", false)]
        [TestCase("Jump", false)]
        [TestCase("Dodge", false)]
        [TestCase("Facing", true)]
        [TestCase("Walk", true)]
        public void FreshCommandsRearmIndependentlyOfHeldButtonsAfterInterruption(string held, bool focusChange)
        {
            var oldSettings=InputSystem.settings;var settings=Object.Instantiate(oldSettings);
            settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);InputSystem.settings=settings;
            var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
            var shipped=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            var actions=AnimationPlaygroundInputFactory.Create(shipped);
            var root=new GameObject("Independent command rearming");root.SetActive(false);
            try
            {
                var router=root.AddComponent<TopDown3DInputRouter>();router.Configure(actions);Assert.That(router.Initialize(),Is.True);
                var jumps=0;var dodges=0;router.AnimationJumpRequested+=()=>jumps++;router.AnimationDodgeRequested+=()=>dodges++;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
                var heldPad=held=="Facing" ? new GamepadState {leftTrigger=1} : held=="Dodge" ? new GamepadState().WithButton(GamepadButton.East) : new GamepadState();
                var heldKey=held=="Walk" ? Key.LeftAlt : held=="Jump" ? Key.Space : Key.None;
                InputSystem.QueueStateEvent(keyboard,heldKey==Key.None ? new KeyboardState() : new KeyboardState(heldKey));
                InputSystem.QueueStateEvent(pad,heldPad);InputSystem.Update();
                if(focusChange)
                {
                    var method=typeof(TopDown3DInputRouter).GetMethod("OnApplicationFocus",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    method.Invoke(router,new object[]{false});method.Invoke(router,new object[]{true});
                }
                else {router.EnterSystemMenuMode();router.EnterGameplayMode();}
                InputSystem.Update();var beforeJump=jumps;var beforeDodge=dodges;
                if(held!="Jump")
                {
                    InputSystem.QueueStateEvent(keyboard,heldKey==Key.None ? new KeyboardState(Key.Space) : new KeyboardState(heldKey,Key.Space));InputSystem.Update();
                    Assert.That(jumps,Is.EqualTo(beforeJump+1),"Fresh Jump must work while another control remains held.");
                    InputSystem.QueueStateEvent(keyboard,heldKey==Key.None ? new KeyboardState() : new KeyboardState(heldKey));InputSystem.Update();
                }
                if(held!="Dodge")
                {
                    InputSystem.QueueStateEvent(pad,heldPad.WithButton(GamepadButton.East));InputSystem.Update();
                    Assert.That(dodges,Is.EqualTo(beforeDodge+1),"Fresh Dodge must work while another control remains held.");
                }
                Assert.That(held=="Jump" ? jumps : held=="Dodge" ? dodges : 0,Is.EqualTo(held=="Jump" ? beforeJump : held=="Dodge" ? beforeDodge : 0),"A held action must not replay.");
            }
            finally
            {
                Object.DestroyImmediate(root);Object.DestroyImmediate(actions);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);
                InputSystem.settings=oldSettings;Object.DestroyImmediate(settings);
            }
        }
        [Test]
        public void OldEastPickupPreferenceAdaptsInMemoryAndTheSourceRemainsUnchanged()
        {
            var shipped=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            var owned=AnimationPlaygroundInputFactory.Create(shipped);
            try
            {
                var pickup=owned.FindAction("Gameplay/PickupCanister",true);var binding=System.Linq.Enumerable.Single(pickup.bindings,b=>b.groups=="Gamepad");
                Assert.That(binding.path,Is.EqualTo("<Gamepad>/select"));
                var old=new TopDown3DControlPreferences();old.bindings.Add(new TopDown3DControlBindingOverride{actionPath="Gameplay/PickupCanister",bindingId=binding.id.ToString(),path="<Gamepad>/buttonEast"});
                var directory=Path.Combine(Path.GetTempPath(),"AnimationDodgePreferences",Guid.NewGuid().ToString("N"));
                var service=new TopDown3DControlPreferencesService(directory);Assert.That(service.TryAdoptInitial(old,owned,out var reason),Is.True,reason);
                Assert.That(old.bindings[0].path,Is.EqualTo("<Gamepad>/buttonEast"));Assert.That(Directory.Exists(directory),Is.False);
                Assert.That(TopDown3DControlPreferencesService.TryValidate(service.Accepted,owned,out reason),Is.True,reason);
                Assert.That(owned.FindAction("Gameplay/AnimationDodge",true).bindings[0].path,Is.EqualTo("<Gamepad>/buttonEast"));
            }
            finally{Object.DestroyImmediate(owned);}
        }

        [Test]
        public void InitialPreferenceInheritanceDoesNotWriteEitherFileAndExistingLabDataWins()
        {
            var directory=Path.Combine(Path.GetTempPath(),"AnimationControlFixtures",Guid.NewGuid().ToString("N"));
            var shipped=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            var owned=AnimationPlaygroundInputFactory.Create(shipped);
            try
            {
                var service=new TopDown3DControlPreferencesService(directory);var source=new TopDown3DControlPreferences();
                var recall=owned.FindAction("Gameplay/RecallBigArm",true);
                source.bindings.Add(new TopDown3DControlBindingOverride{actionPath="Gameplay/RecallBigArm",bindingId=recall.bindings[0].id.ToString(),path="<Keyboard>/f4"});
                Assert.That(service.TryAdoptInitial(source,owned,out var error),Is.True,error);
                Assert.That(Directory.Exists(directory),Is.False,"Inheritance is read-only and must not create preferences.");
                Assert.That(service.Accepted.bindings[0].path,Is.EqualTo("<Keyboard>/f4"));
                Assert.That(service.TrySave(service.Accepted,owned,out error),Is.True,error);
                var path=Path.Combine(directory,"controls-v1.json");var before=File.ReadAllBytes(path);
                Assert.That(service.TryAdoptInitial(new TopDown3DControlPreferences(),owned,out error),Is.False);
                Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(owned);
                var parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"AnimationControlFixtures"))+Path.DirectorySeparatorChar;
                var target=Path.GetFullPath(directory);
                if(!target.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Fixture cleanup escaped its parent.");
                if(Directory.Exists(target))Directory.Delete(target,true);
            }
        }

        [Test]
        public void KeyboardDodgeAndMouseFacingWorkAndRearmAfterMenuReturn()
        {
            var original=InputSystem.settings;var settings=Object.Instantiate(original);
            settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);InputSystem.settings=settings;
            var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
            var actions=AnimationPlaygroundInputFactory.Create(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            var root=new GameObject("Keyboard locomotion controls");root.SetActive(false);
            try
            {
                var router=root.AddComponent<TopDown3DInputRouter>();router.Configure(actions);Assert.That(router.Initialize(),Is.True);
                var dodges=0;router.AnimationDodgeRequested+=()=>dodges++;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.Update();
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));InputSystem.Update();
                Assert.That(router.LaboratoryStandingFacingHeld,Is.True,"Mouse facing uses the same directional movement route as LT.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl));InputSystem.Update();Assert.That(dodges,Is.EqualTo(1));
                router.EnterSystemMenuMode();router.EnterGameplayMode();InputSystem.Update();
                Assert.That(router.LaboratoryStandingFacingHeld,Is.False);Assert.That(dodges,Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl));InputSystem.Update();
                Assert.That(dodges,Is.EqualTo(2),"Held mouse after a menu must not suppress a fresh dodge.");
                InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.Update();
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));InputSystem.Update();
                Assert.That(router.LaboratoryStandingFacingHeld,Is.True);
                InputSystem.RemoveDevice(mouse);Assert.That(router.LaboratoryStandingFacingHeld,Is.False);
                Assert.That(TopDown3DControlPreferencesService.TryValidate(new TopDown3DControlPreferences(),actions,out var error),Is.True,error);
            }
            finally
            {
                Object.DestroyImmediate(root);Object.DestroyImmediate(actions);InputSystem.RemoveDevice(keyboard);
                if(mouse.added)InputSystem.RemoveDevice(mouse);InputSystem.settings=original;Object.DestroyImmediate(settings);
            }
        }

        [TestCase(TopDown3DInputMode.Inventory)]
        [TestCase(TopDown3DInputMode.CampActions)]
        [TestCase(TopDown3DInputMode.SystemMenu)]
        public void CancelLeavesItsCallbackContextIntactForLaterUiListeners(TopDown3DInputMode mode)
        {
            var original=InputSystem.settings;var settings=Object.Instantiate(original);
            settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);InputSystem.settings=settings;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            var actions=InputActionAsset.FromJson(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions").ToJson());
            var root=new GameObject("Cancel callback ordering");root.SetActive(false);
            GameObject uiRoot=null;InputActionReference cancelReference=null;
            try
            {
                var router=root.AddComponent<TopDown3DInputRouter>();router.Configure(actions);Assert.That(router.Initialize(),Is.True);
                uiRoot=new GameObject("Cancel UI listener",typeof(EventSystem),typeof(InputSystemUIInputModule));
                var module=uiRoot.GetComponent<InputSystemUIInputModule>();module.actionsAsset=actions;
                cancelReference=InputActionReference.Create(actions.FindAction("UI/Cancel",true));module.cancel=cancelReference;
                typeof(InputSystemUIInputModule).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(module,null);
                if(mode==TopDown3DInputMode.Inventory)router.EnterInventoryMode();
                else if(mode==TopDown3DInputMode.SystemMenu)router.EnterSystemMenuMode();else router.EnterCampMode(mode);
                var callbacks=0;var observedMode=TopDown3DInputMode.Disabled;InputControl observedControl=null;
                router.UiCancelRequested+=()=>{callbacks++;router.EnterGameplayMode();};
                // Unity's UI module reads context.control after the router's
                // listener. Re-resolving maps inside that listener invalidates it.
                actions.FindAction("UI/Cancel",true).performed+=context=>{observedMode=router.Mode;observedControl=context.control;};
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));InputSystem.Update();
                Assert.That(observedMode,Is.EqualTo(mode),"Map changes must wait until all action listeners have read the event.");
                Assert.That(observedControl,Is.SameAs(keyboard.escapeKey));Assert.That(callbacks,Is.EqualTo(1));
                Assert.That(router.Mode,Is.EqualTo(TopDown3DInputMode.Gameplay));InputSystem.Update();Assert.That(callbacks,Is.EqualTo(1));
            }
            finally
            {
                if(uiRoot!=null)Object.DestroyImmediate(uiRoot);if(cancelReference!=null)Object.DestroyImmediate(cancelReference);
                Object.DestroyImmediate(root);Object.DestroyImmediate(actions);InputSystem.RemoveDevice(keyboard);
                InputSystem.settings=original;Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void DestroyedPlayerStatusCanBeReadDuringStationTeardown()
        {
            var root=new GameObject("Presentation teardown status");root.SetActive(false);
            var driver=root.AddComponent<TopDown3DPlayerAnimationDriver>();
            var continuation=ScriptableObject.CreateInstance<AnimationPlaygroundContinuationProfile>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(TopDown3DPlayerAnimationDriver).GetField("forwardReviewConfigured",flags).SetValue(driver,true);
            typeof(TopDown3DPlayerAnimationDriver).GetField("continuationReview",flags).SetValue(driver,continuation);
            try
            {
                Object.DestroyImmediate(root);
                Assert.DoesNotThrow(()=>{var status=driver.LaboratoryForwardReviewStatus;});
            }
            finally { if(root!=null)Object.DestroyImmediate(root);Object.DestroyImmediate(continuation); }
        }

        [Test]
        public void MouseFacingAndKeyboardDodgeCanBeReboundWithCoactiveConflictsProtected()
        {
            var actions=AnimationPlaygroundInputFactory.Create(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            try
            {
                var facing=actions.FindAction("Gameplay/CameraLookAhead",true);var dodge=actions.FindAction("Gameplay/AnimationDodge",true);
                var facingBinding=System.Linq.Enumerable.Single(facing.bindings,b=>b.groups=="Keyboard&Mouse");
                var dodgeBinding=System.Linq.Enumerable.Single(dodge.bindings,b=>b.groups=="Keyboard&Mouse");
                var draft=new TopDown3DControlPreferences();
                draft.bindings.Add(new TopDown3DControlBindingOverride{actionPath="Gameplay/CameraLookAhead",bindingId=facingBinding.id.ToString(),path="<Mouse>/middleButton"});
                draft.bindings.Add(new TopDown3DControlBindingOverride{actionPath="Gameplay/AnimationDodge",bindingId=dodgeBinding.id.ToString(),path="<Keyboard>/z"});
                Assert.That(TopDown3DControlPreferencesService.TryValidate(draft,actions,out var reason),Is.True,reason);
                TopDown3DControlPreferencesService.Apply(actions,draft);
                Assert.That(System.Linq.Enumerable.Single(facing.bindings,b=>b.id==facingBinding.id).effectivePath,Is.EqualTo("<Mouse>/middleButton"));
                Assert.That(System.Linq.Enumerable.Single(dodge.bindings,b=>b.id==dodgeBinding.id).effectivePath,Is.EqualTo("<Keyboard>/z"));
                var resetFacing=new TopDown3DControlPreferences();
                resetFacing.bindings.Add(new TopDown3DControlBindingOverride{actionPath="Gameplay/AnimationDodge",bindingId=dodgeBinding.id.ToString(),path="<Mouse>/rightButton"});
                Assert.That(TopDown3DControlPreferencesService.TryValidate(resetFacing,actions,out reason),Is.False,
                    "Removing the facing override restores right mouse, even while the currently applied binding uses middle mouse.");
                draft.bindings[0].path="<Mouse>/leftButton";
                Assert.That(TopDown3DControlPreferencesService.TryValidate(draft,actions,out reason),Is.False,"Attack and held facing cannot share a button.");
                draft.bindings[0].path="<Mouse>/delta";
                Assert.That(TopDown3DControlPreferencesService.TryValidate(draft,actions,out reason),Is.False,"An axis is not a facing button.");
                TopDown3DControlPreferencesService.Apply(actions,new TopDown3DControlPreferences());
                Assert.That(System.Linq.Enumerable.Single(facing.bindings,b=>b.id==facingBinding.id).effectivePath,Is.EqualTo("<Mouse>/rightButton"));
                Assert.That(System.Linq.Enumerable.Single(dodge.bindings,b=>b.id==dodgeBinding.id).effectivePath,Is.EqualTo("<Keyboard>/leftCtrl"));
            }
            finally{Object.DestroyImmediate(actions);}
        }

        [Test]
        public void KeyboardFacingFeedsTheExistingDirectionalMotorAndReleasesNormally()
        {
            using var f=new AnimationPlaygroundStandingStrafeTests.InputFixture();
            var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                var motor=f.Actor.AddComponent<TopDown3DPlayerMotor>();
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(TopDown3DPlayerMotor).GetField("input",flags).SetValue(motor,f.Router);
                typeof(TopDown3DPlayerMotor).GetField("cameraBasis",flags).SetValue(motor,f.Actor.transform);
                var binding=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
                Assert.That(binding,Is.Not.Null);Assert.That(binding.standingStrafeReview.AllDirections,Is.True);
                var owner=new object();motor.BindAnimationPostureOwner(owner);
                motor.ConfigureLaboratoryStandingStrafe(owner,binding.standingStrafeReview,()=>true);
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,true);
                void Apply() => typeof(TopDown3DPlayerMotor).GetMethod("ApplyStandingStrafeIntent",flags).Invoke(motor,new object[]{motor.LaboratoryMoveDirection});
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.Update();Apply();
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A));InputSystem.Update();Apply();
                Assert.That(motor.LaboratoryStandingFacingLocked,Is.True);Assert.That(motor.LaboratoryStandingTravel,Is.EqualTo(AnimationPlaygroundStandingTravel.Left));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));InputSystem.Update();Apply();
                Assert.That(motor.LaboratoryStandingTravel,Is.EqualTo(AnimationPlaygroundStandingTravel.Backward));
                InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.Update();Apply();
                Assert.That(motor.LaboratoryStandingFacingLocked,Is.False);
            }
            finally {InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);}
        }
    }
}
