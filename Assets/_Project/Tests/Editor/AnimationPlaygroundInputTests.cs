using BooterBigArm.TopDown3D;
using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
    }
}
