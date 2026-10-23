using System.Collections;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace BooterBigArm.Tests
{
    public sealed class CampMenuInventoryInputTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private CampExpeditionGamepadTests fixture;
        private Gamepad unpaired;
        private T Get<T>(string name)=>(T)typeof(CampExpeditionGamepadTests).GetField(name,Private).GetValue(fixture);
        private CampExpeditionMenuView View=>Get<CampExpeditionMenuView>("view");
        private CampExpeditionSession Session=>Get<CampExpeditionSession>("session");
        private CampMenuControl Control(string key)=>Get<GameObject>("viewObject").GetComponentsInChildren<CampMenuControl>(true).Single(control=>control.key==key);
        private void Send(GamepadState state)=>typeof(CampExpeditionGamepadTests).GetMethod("Send",Private,null,new[]{typeof(GamepadState)},null).Invoke(fixture,new object[]{state});
        private void Tick()=>typeof(CampExpeditionSession).GetMethod("Update",Private).Invoke(Session,null);
        private static IEnumerator Frame()=>(IEnumerator)typeof(CampExpeditionGamepadTests).GetMethod("NextEditorRenderingFrame",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        [SetUp] public void Setup(){fixture=new CampExpeditionGamepadTests();fixture.Setup();View.ShowPage("storage");}
        [TearDown] public void Cleanup(){if(unpaired!=null&&unpaired.added)InputSystem.RemoveDevice(unpaired);fixture.Cleanup();}
        [Test] public void OpeningPressAndHeldNavigationMustReleaseBeforeStorageChoicesCanSubmit()
        {
            Control("storage-sort").Button.onClick.Invoke();Send(new GamepadState().WithButton(GamepadButton.South));Tick();Assert.That(View.StorageChoiceOpen,Is.True);Assert.That(Get<InputSystemUIInputModule>("module").submit,Is.Null);Assert.That(Control("storage-choice:0").Button.IsInteractable(),Is.False);
            Send(new GamepadState().WithButton(GamepadButton.DpadDown));Tick();Assert.That(Control("storage-choice:0").Button.IsInteractable(),Is.False);Send(new GamepadState());Tick();Assert.That(Control("storage-choice:0").Button.IsInteractable(),Is.True);Assert.That(Get<InputSystemUIInputModule>("module").submit,Is.Not.Null);
        }
        [Test] public void ShoulderTriggerAndWestCannotActBehindStorageChoices()
        {
            Control("storage-filter").Button.onClick.Invoke();Send(new GamepadState());Tick();Send(new GamepadState().WithButton(GamepadButton.RightShoulder));Assert.That(View.Page,Is.EqualTo("storage"));Send(new GamepadState());Send(new GamepadState{rightTrigger=1});Assert.That(View.Page,Is.EqualTo("storage"));Send(new GamepadState().WithButton(GamepadButton.West));Assert.That(View.StorageChoiceOpen,Is.True);
            View.Back();Assert.That(View.StorageChoiceOpen,Is.False);Assert.That(Get<EventSystem>("events").currentSelectedGameObject,Is.SameAs(Control("storage-filter").gameObject));Assert.That(Get<InputSystemUIInputModule>("module").submit,Is.Null);Send(new GamepadState());Tick();Assert.That(Get<InputSystemUIInputModule>("module").submit,Is.Not.Null);
        }
        [UnityTest] public IEnumerator SouthOpensTheSortPopupThenRequiresAFreshPressToChoose()
        {
            IEnumerator Frame()=>(IEnumerator)typeof(CampExpeditionGamepadTests).GetMethod("NextEditorRenderingFrame",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            Get<EventSystem>("events").SetSelectedGameObject(Control("storage-sort").gameObject);yield return Frame();Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(View.StorageChoiceOpen,Is.True);Tick();Assert.That(Control("storage-choice:0").Button.IsInteractable(),Is.False);
            yield return Frame();Send(new GamepadState().WithButton(GamepadButton.South));Tick();Assert.That(View.StorageChoiceOpen,Is.True);yield return Frame();Send(new GamepadState());Tick();yield return Frame();Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(View.StorageChoiceOpen,Is.False);Assert.That(Get<EventSystem>("events").currentSelectedGameObject,Is.SameAs(Control("storage-sort").gameObject));yield return Frame();Send(new GamepadState().WithButton(GamepadButton.South));Tick();Assert.That(View.StorageChoiceOpen,Is.False);
        }
        [Test] public void AnUnpairedDeviceCannotHoldThePopupReleaseFence()
        {
            unpaired=InputSystem.AddDevice<Gamepad>();Control("storage-sort").Button.onClick.Invoke();InputSystem.QueueStateEvent(unpaired,new GamepadState().WithButton(GamepadButton.DpadDown));InputSystem.Update();Tick();Assert.That(Control("storage-choice:0").Button.IsInteractable(),Is.True);
        }
        [TestCase(true)][TestCase(false)] public void ClosingAStoragePopupKeepsNavigationSuspendedUntilNeutral(bool dpad)
        {
            var module=Get<InputSystemUIInputModule>("module");var move=module.move;Control("storage-filter").Button.onClick.Invoke();Send(new GamepadState());Tick();
            var held=dpad?new GamepadState().WithButton(GamepadButton.DpadDown):new GamepadState{leftStick=Vector2.down};Send(held);View.Back();Tick();
            Assert.That(module.move,Is.Null);Assert.That(module.submit,Is.Null);Assert.That(Get<EventSystem>("events").currentSelectedGameObject,Is.SameAs(Control("storage-filter").gameObject));Send(new GamepadState());Tick();Assert.That(module.move,Is.SameAs(move));Assert.That(module.submit,Is.Not.Null);
        }
        [UnityTest] public IEnumerator ClosingAPopupCannotMoveOrSubmitTheCoveredPageUntilAFreshPress()
        {
            var module=Get<InputSystemUIInputModule>("module");Control("storage-filter").Button.onClick.Invoke();Send(new GamepadState());Tick();Send(new GamepadState().WithButton(GamepadButton.DpadDown));View.Back();
            yield return Frame();Send(new GamepadState().WithButton(GamepadButton.DpadDown).WithButton(GamepadButton.South));Tick();Assert.That(View.StorageChoiceOpen,Is.False);Assert.That(module.move,Is.Null);Assert.That(module.submit,Is.Null);Assert.That(Get<EventSystem>("events").currentSelectedGameObject,Is.SameAs(Control("storage-filter").gameObject));
            yield return Frame();Send(new GamepadState().WithButton(GamepadButton.DpadDown));Tick();Assert.That(module.submit,Is.Null);yield return Frame();Send(new GamepadState());Tick();Assert.That(module.move,Is.Not.Null);yield return Frame();Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(View.StorageChoiceOpen,Is.True);
        }
        [Test] public void ClosingAPopupWaitsForBoundKeyboardDirectionsToRelease()
        {
            var module=Get<InputSystemUIInputModule>("module");var move=module.move;Control("storage-sort").Button.onClick.Invoke();Send(new GamepadState());Tick();
            void Keys(KeyboardState state)=>typeof(CampExpeditionGamepadTests).GetMethod("Send",Private,null,new[]{typeof(KeyboardState)},null).Invoke(fixture,new object[]{state});
            Keys(new KeyboardState(Key.DownArrow));View.Back();Tick();Assert.That(module.move,Is.Null);Assert.That(module.submit,Is.Null);Keys(new KeyboardState());Tick();Assert.That(module.move,Is.SameAs(move));
        }
        [Test] public void APageChangeDuringTheReleaseGateRestoresItsOwnNavigationAction()
        {
            var module=Get<InputSystemUIInputModule>("module");Control("storage-filter").Button.onClick.Invoke();Send(new GamepadState());Tick();Send(new GamepadState().WithButton(GamepadButton.DpadDown));View.Back();View.ShowPage("tree");Assert.That(module.move,Is.Null);Send(new GamepadState());Tick();Assert.That(module.move.action.name,Is.EqualTo("ExpeditionTreeNavigate"));
        }
        [Test] public void UnbindingRestoresTheSuspendedMoveAndRemovesTheContextSubscription()
        {
            var module=Get<InputSystemUIInputModule>("module");var move=module.move;Control("storage-sort").Button.onClick.Invoke();Assert.That(module.move,Is.Null);typeof(CampExpeditionSession).GetMethod("UnbindMenuCommands",Private).Invoke(Session,null);Assert.That(module.move,Is.SameAs(move));View.Back();Assert.That(module.move,Is.SameAs(move));
        }
        [Test] public void DisconnectingTheHeldDeviceReleasesThePopupGate()
        {
            var pad=Get<Gamepad>("pad");var module=Get<InputSystemUIInputModule>("module");Control("storage-filter").Button.onClick.Invoke();Send(new GamepadState());Tick();Send(new GamepadState().WithButton(GamepadButton.DpadDown));View.Back();Tick();Assert.That(module.move,Is.Null);InputSystem.RemoveDevice(pad);Tick();Assert.That(module.move,Is.Not.Null);Assert.That(module.submit,Is.Not.Null);
        }
        [Test] public void UnbindingDoesNotOverwriteNavigationTakenByAnotherOwner()
        {
            var module=Get<InputSystemUIInputModule>("module");var original=module.move;var other=InputActionReference.Create(Get<InputActionAsset>("actions").FindAction("UI/Point",true));
            try{Control("storage-sort").Button.onClick.Invoke();module.move=other;typeof(CampExpeditionSession).GetMethod("UnbindMenuCommands",Private).Invoke(Session,null);Assert.That(module.move,Is.SameAs(other));}
            finally{module.move=original;Object.DestroyImmediate(other);}
        }
        [TestCase("picker")][TestCase("quote")][TestCase("nested")]
        public void ClosingAModalContextRequiresNeutralBeforeTheExposedContextCanNavigate(string context)
        {
            var module=Get<InputSystemUIInputModule>("module");var move=module.move;View.ShowInventory("booter");
            if(context!="quote")View.ShowPicker("tool",System.Array.Empty<CampMenuStackView>());
            if(context!="picker")View.ShowQuote("Confirm transfer");
            System.Action<string> route=key=>{if(key=="quote-cancel")typeof(CampExpeditionSession).GetMethod("Command",Private).Invoke(Session,new object[]{key});};
            View.Command+=route;
            try
            {
                Send(new GamepadState());Tick();Send(new GamepadState().WithButton(GamepadButton.DpadDown));View.Back();var focused=Get<EventSystem>("events").currentSelectedGameObject;Tick();
                Assert.That((bool)typeof(CampExpeditionMenuView).GetField("quoting",Private).GetValue(View),Is.False);Assert.That(View.PickerTarget,Is.EqualTo(context=="nested"?"tool":null));
                Assert.That(module.move,Is.Null);Assert.That(module.submit,Is.Null);Send(new GamepadState().WithButton(GamepadButton.DpadDown).WithButton(GamepadButton.South));Tick();Assert.That(Get<EventSystem>("events").currentSelectedGameObject,Is.SameAs(focused));Assert.That(module.move,Is.Null);Assert.That(module.submit,Is.Null);
                Send(new GamepadState());Tick();Assert.That(module.move,Is.SameAs(move));Assert.That(module.submit,Is.Not.Null);
            }
            finally{View.Command-=route;}
        }
        [Test] public void BackOutOfTextEditingAlsoWaitsForHeldNavigationToRelease()
        {
            var module=Get<InputSystemUIInputModule>("module");var events=Get<EventSystem>("events");events.SetSelectedGameObject(View.storageSearch.gameObject);typeof(UnityEngine.UI.InputField).GetMethod("LateUpdate",Private).Invoke(View.storageSearch,null);Assert.That(View.IsEditingText,Is.True);
            Send(new GamepadState().WithButton(GamepadButton.DpadDown));View.Back();Tick();Assert.That(View.IsEditingText,Is.False);Assert.That(View.Page,Is.EqualTo("storage"));Assert.That(module.move,Is.Null);Assert.That(module.submit,Is.Null);Send(new GamepadState());Tick();Assert.That(module.move,Is.Not.Null);Assert.That(module.submit,Is.Not.Null);
        }
    }
}
