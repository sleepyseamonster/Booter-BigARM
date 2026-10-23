using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampExpeditionInputTests
    {
        private InputActionAsset source,owned;
        private GameObject root;
        private TopDown3DInputRouter router;

        [SetUp] public void Setup()
        {
            source=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            Assert.That(source,Is.Not.Null);
            owned=AnimationPlaygroundInputFactory.Create(source);
            root=new GameObject("Expedition owned input fixture");root.SetActive(false);
            router=root.AddComponent<TopDown3DInputRouter>();router.Configure(owned);
            // EditMode does not execute PrepareAnimationInput's Play Mode clone path. Supply
            // its exact ownership bookkeeping without starting gameplay or a scene bootstrap.
            SetRouterField("ownedAnimationActions",owned);
            SetRouterField("authoredAnimationActions",source);
        }

        [TearDown] public void Cleanup()
        {
            if(owned!=null)owned.Disable();
            if(root!=null)Object.DestroyImmediate(root);
            if(owned!=null)Object.DestroyImmediate(owned);
        }

        private void SetRouterField(string field,object value)
            =>typeof(TopDown3DInputRouter).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(router,value);
        private static Dictionary<InputAction,bool> EnabledStates(InputActionAsset asset)
            =>asset.actionMaps.SelectMany(map=>map.actions).ToDictionary(action=>action,action=>action.enabled);
        private static void AssertEnabledStates(Dictionary<InputAction,bool> states)
        {foreach(var pair in states)Assert.That(pair.Key.enabled,Is.EqualTo(pair.Value),pair.Key.actionMap.name+"/"+pair.Key.name);}

        [Test] public void PreparingMenuActionsWithGameplayAndUiEnabledDoesNotThrow()
        {
            owned.FindActionMap("Gameplay",true).Enable();owned.FindActionMap("UI",true).Enable();
            var states=EnabledStates(owned);var sourceJson=source.ToJson();
            Assert.DoesNotThrow(()=>router.PrepareExpeditionMenuActions());
            AssertEnabledStates(states);Assert.That(source.ToJson(),Is.EqualTo(sourceJson));
            Assert.That(owned.FindAction("UI/ExpeditionTreeNavigate",true).bindings.Any(binding=>binding.path=="<Gamepad>/dpad"),Is.True);
        }

        [Test] public void PreparingRestoresExactIndividualActionStatesAcrossAllMaps()
        {
            owned.FindAction("Gameplay/Move",true).Enable();
            owned.FindAction("System/AnimationPalette",true).Enable();
            owned.FindAction("UI/Navigate",true).Enable();
            var states=EnabledStates(owned);
            router.PrepareExpeditionMenuActions();
            AssertEnabledStates(states);
            foreach(var action in owned.actionMaps.SelectMany(map=>map.actions).Where(action=>!states.ContainsKey(action)))
                Assert.That(action.enabled,Is.False,"New actions are enabled by the later UI mode transition, not by changing prior map state.");
        }

        [Test] public void FailedPreparationRestoresPriorStatesAndDisablesNewActions()
        {
            owned.FindAction("Gameplay/Move",true).Enable();owned.FindAction("UI/Navigate",true).Enable();
            var states=EnabledStates(owned);InputAction introduced=null;
            Assert.Throws<InvalidOperationException>(()=>router.EditExpeditionMenuActions(map=>
            {
                Assert.That(owned.actionMaps.SelectMany(actionMap=>actionMap.actions).Any(action=>action.enabled),Is.False);
                introduced=map.AddAction("FailedPreparationFixture",InputActionType.Button,"<Gamepad>/buttonSouth");
                introduced.Enable();
                throw new InvalidOperationException("Injected preparation failure.");
            }));
            AssertEnabledStates(states);Assert.That(introduced.enabled,Is.False);
        }

        [Test] public void PreparingTwiceIsIdempotentAndPreservesExistingIdsAndBindings()
        {
            var existing=owned.actionMaps.SelectMany(map=>map.actions).ToDictionary(action=>action,
                action=>(id:action.id,bindings:action.bindings.Select(binding=>binding.id).ToArray(),paths:action.bindings.Select(binding=>binding.path).ToArray()));
            router.PrepareExpeditionMenuActions();var first=owned.ToJson();
            router.PrepareExpeditionMenuActions();Assert.That(owned.ToJson(),Is.EqualTo(first));
            foreach(var pair in existing)
            {
                Assert.That(pair.Key.id,Is.EqualTo(pair.Value.id));
                CollectionAssert.AreEqual(pair.Value.bindings,pair.Key.bindings.Select(binding=>binding.id));
                CollectionAssert.AreEqual(pair.Value.paths,pair.Key.bindings.Select(binding=>binding.path));
            }
            var ui=owned.FindActionMap("UI",true);
            foreach(var name in new[]{"NextExpeditionSubTab","PreviousExpeditionSubTab","ExpeditionInspectScroll","ExpeditionTreeNavigate","ExpeditionTreePan"})
                Assert.That(ui.actions.Count(action=>action.name==name),Is.EqualTo(1),name);
            Assert.That(ui.FindAction("ExpeditionTreeNavigate",true).bindings.Count,Is.EqualTo(6));
        }

        [Test] public void AuthoredAssetIsRejectedWithoutMutation()
        {
            router.Configure(source);var before=source.ToJson();var states=EnabledStates(source);
            Assert.Throws<InvalidOperationException>(()=>router.PrepareExpeditionMenuActions());
            Assert.That(source.ToJson(),Is.EqualTo(before));AssertEnabledStates(states);
        }

        [Test] public void LabMarkerWithoutRouterOwnershipIsRejectedWithoutMutation()
        {
            SetRouterField("ownedAnimationActions",null);
            owned.FindActionMap("Gameplay",true).Enable();owned.FindAction("UI/Navigate",true).Enable();
            var before=owned.ToJson();var states=EnabledStates(owned);
            Assert.That(AnimationPlaygroundInputFactory.IsLabAsset(owned),Is.True);
            Assert.Throws<InvalidOperationException>(()=>router.PrepareExpeditionMenuActions());
            Assert.That(owned.ToJson(),Is.EqualTo(before));AssertEnabledStates(states);
        }
    }
}
