using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampMenuSettingsTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private TopDown3DMenuTests fixture;
        private GameObject root;
        private GameObject cameraRoot,cameraTarget;
        private GameObject uiCameraRoot;
        private RenderTexture uiTarget;
        private EventSystem previousEvents;
        private bool eventsEnabled;
        private CampExpeditionMenuView view;
        private TopDown3DMenuController Menu=>fixture.Menu;
        private T Read<T>(string field)=>(T)typeof(TopDown3DMenuController).GetField(field,Private).GetValue(Menu);
        private T Fixture<T>(string field)=>(T)typeof(TopDown3DMenuTests).GetField(field,Private).GetValue(fixture);
        private void Tick()=>typeof(TopDown3DMenuController).GetMethod("Update",Private).Invoke(Menu,null);
        private CampMenuControl Control(string key)=>root.GetComponentsInChildren<CampMenuControl>(true).Single(control=>control.key==key);
        private void Click(string key)=>Control("settings:"+key).Button.onClick.Invoke();
        private void Ready(){fixture.ProcessUiState(new GamepadState());Tick();}
        private void Open(){Menu.OpenExpeditionSettings();Ready();}
        [SetUp] public void Setup()
        {
            previousEvents=EventSystem.current;fixture=new TopDown3DMenuTests();fixture.SetUp();typeof(EventSystem).GetMethod("OnEnable",Private).Invoke(fixture.Events,null);eventsEnabled=true;EventSystem.current=fixture.Events;
            uiCameraRoot=new GameObject("Settings layout camera",typeof(Camera));var camera=uiCameraRoot.GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=360;uiTarget=new RenderTexture(1280,720,0);camera.targetTexture=uiTarget;
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab"));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();view=root.GetComponent<CampExpeditionMenuView>();view.Initialize(fixture.Events);view.Render(new CampMenuPresentation{canPlay=true});view.BindInputRouter(Fixture<TopDown3DInputRouter>("router"));Menu.UseExpeditionHost();Menu.UseExpeditionPresentation(view);Fixture<TopDown3DInputRouter>("router").UiCancelRequested+=Menu.Back;
        }
        [TearDown] public void Cleanup()
        {
            if(fixture!=null){Fixture<TopDown3DInputRouter>("router").UiCancelRequested-=Menu.Back;Menu.Close();Menu.UseExpeditionPresentation(null);}if(root!=null){root.SetActive(false);Object.DestroyImmediate(root);}if(eventsEnabled&&fixture!=null){typeof(EventSystem).GetMethod("OnDisable",Private).Invoke(fixture.Events,null);eventsEnabled=false;}fixture?.TearDown();fixture=null;if(previousEvents!=null)EventSystem.current=previousEvents;if(cameraRoot!=null)Object.DestroyImmediate(cameraRoot);if(cameraTarget!=null)Object.DestroyImmediate(cameraTarget);if(uiCameraRoot!=null)Object.DestroyImmediate(uiCameraRoot);if(uiTarget!=null)Object.DestroyImmediate(uiTarget);
        }
        [Test] public void NativeSettingsOwnsPresentationWhileTheLegacyCanvasStaysHidden()
        {
            Open();Assert.That(view.Page,Is.EqualTo("settings"));Assert.That(view.SettingsPanel,Is.EqualTo("Settings"));Assert.That(fixture.MenuRoot.GetComponent<CanvasGroup>().alpha,Is.Zero);Assert.That(fixture.MenuRoot.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);Assert.That(root.GetComponent<CanvasGroup>().alpha,Is.EqualTo(1));Assert.That(Control("primary:play").Button.IsInteractable(),Is.False);Assert.That(Control("page:inventory").Button.IsInteractable(),Is.False);Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(Control("settings:TextSize").gameObject));
            Assert.That(Control("hint:tabs").gameObject.activeInHierarchy,Is.False);Assert.That(Control("page:inventory").gameObject.activeInHierarchy,Is.False);Menu.Close();Assert.That(view.Page,Is.EqualTo("start"));Assert.That(Control("primary:play").Button.IsInteractable(),Is.True);Assert.That(Control("page:inventory").Button.IsInteractable(),Is.True);Assert.That(Control("page:inventory").gameObject.activeInHierarchy,Is.True);Assert.That(Control("hint:tabs").gameObject.activeInHierarchy,Is.True);
        }
        [Test] public void ReadabilityPreviewsUseTheExistingDraftAndOnlyApplySaves()
        {
            Open();var file=Path.Combine(Fixture<string>("directory"),"menu-settings-v1.json");Click("TextSize");Click("Motion");Assert.That(Read<TopDown3DMenuSettingsService>("settings").Accepted.largeText,Is.False);Assert.That(File.Exists(file),Is.False);Assert.That(Control("settings:TextSize").transform.Find("Checkbox/Checked").gameObject.activeSelf,Is.True);Click("SettingsApply");Assert.That(Menu.IsOpen,Is.False);Assert.That(Menu.LargeText&&Menu.ReducedMotion,Is.True);Assert.That(File.Exists(file),Is.True);Assert.That(JsonUtility.FromJson<TopDown3DMenuSettings>(File.ReadAllText(file)).largeText,Is.True);
        }
        [Test] public void DirtyBackShowsNativeSafeDialogAndRestoresTheExactParentControl()
        {
            Open();fixture.Events.SetSelectedGameObject(Control("settings:Motion").gameObject);Click("Motion");view.Back();Assert.That(view.SettingsPanel,Is.EqualTo("Dialog"));Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(Control("settings:DialogCancel").gameObject));Assert.That(Control("settings:TextSize").Button.IsInteractable(),Is.False);Ready();Click("DialogCancel");Ready();Assert.That(view.SettingsPanel,Is.EqualTo("Settings"));Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(Control("settings:Motion").gameObject));view.Back();Ready();Click("DialogAccept");Assert.That(Menu.IsOpen,Is.False);Assert.That(Menu.ReducedMotion,Is.False);Assert.That(File.Exists(Path.Combine(Fixture<string>("directory"),"menu-settings-v1.json")),Is.False);
        }
        [Test] public void NativeSettingsStillRequiresReleaseAfterOpeningAndChangingPanels()
        {
            fixture.ProcessUiState(new GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South));Menu.OpenExpeditionSettings();Tick();Click("TextSize");Assert.That(Read<TopDown3DMenuSettings>("settingsDraft").largeText,Is.False);Assert.That(fixture.Module.submit,Is.Null);Ready();Click("Controls");Assert.That(Control("settings:ControlBinding").Button.IsInteractable(),Is.False);Assert.That(fixture.Module.submit,Is.Null);Ready();Assert.That(Control("settings:ControlBinding").Button.IsInteractable(),Is.True);
        }
        [Test] public void RadialEditingUsesTheExistingDraftAndReturnsToSettingsAfterApply()
        {
            Open();Click("TextSize");Click("RadialSetup");Ready();Click("Slot0");Click("Assign2");var radial=Fixture<TopDown3DRadialMenuController>("radial");Assert.That(radial.Preferences.pages[0].slots[0],Is.Not.EqualTo(TopDown3DRadialCommand.CallLegger));Assert.That(Read<TopDown3DRadialPreferences>("radialDraft").pages[0].slots[0],Is.EqualTo(TopDown3DRadialCommand.CallLegger));Click("SetupApply");Ready();Assert.That(view.SettingsPanel,Is.EqualTo("Settings"));Assert.That(radial.Preferences.pages[0].slots[0],Is.EqualTo(TopDown3DRadialCommand.CallLegger));Assert.That(Read<TopDown3DMenuSettings>("settingsDraft").largeText,Is.True);Assert.That(Menu.LargeText,Is.False);Assert.That(File.Exists(Path.Combine(Fixture<string>("directory"),"radial-layout-v1.json")),Is.True);
        }
        [Test] public void AdvancedRadialPagesDirectionDialogsAndRenameStayInTheExistingService()
        {
            Open();Click("RadialSetup");Ready();Click("More");var field=root.GetComponentsInChildren<InputField>().Single(input=>input.name=="Shortcut page name");field.onEndEdit.Invoke("Survey");Assert.That(Read<TopDown3DRadialPreferences>("radialDraft").pages[0].name,Is.EqualTo("Survey"));Click("SectorCount");Ready();Assert.That(view.SettingsPanel,Is.EqualTo("Dialog"));Click("DialogAccept");Ready();Assert.That(Read<TopDown3DRadialPreferences>("radialDraft").pages[0].slots.Length,Is.EqualTo(8));Assert.That(Control("settings:Slot7").gameObject.activeInHierarchy,Is.True);Click("AddPage");Assert.That(Read<TopDown3DRadialPreferences>("radialDraft").pages.Length,Is.EqualTo(2));Click("SetupBack");Ready();Click("DialogAccept");Ready();Assert.That(view.SettingsPanel,Is.EqualTo("Settings"));Assert.That(Fixture<TopDown3DRadialMenuController>("radial").Preferences.pages.Length,Is.EqualTo(1));
        }
        [Test] public void InteractiveRebindingUsesTheExistingCaptureAndBackDisposesIt()
        {
            Open();Click("Controls");Ready();Click("ControlBinding");Assert.That(Menu.IsRebindingControls,Is.True);Assert.That(Control("settings:ControlBinding").Button.IsInteractable(),Is.False);Assert.That(fixture.Module.submit,Is.Null);Assert.That(Control("hint:select").gameObject.activeInHierarchy,Is.False);Assert.That(Control("hint:scroll").gameObject.activeInHierarchy,Is.False);fixture.ProcessUiState(new GamepadState().WithButton(GamepadButton.East));Ready();Assert.That(Menu.IsRebindingControls,Is.False);Assert.That(view.SettingsPanel,Is.EqualTo("Controls"));Assert.That(Control("settings:ControlBinding").Button.IsInteractable(),Is.True);Assert.That(Control("hint:select").gameObject.activeInHierarchy,Is.True);Assert.That(File.Exists(Path.Combine(Fixture<string>("directory"),"controls-v1.json")),Is.False);
        }
        [Test] public void AReboundKeyIsSavedAndAppliedThroughTheNativeControlsPanel()
        {
            Open();Click("Controls");Ready();if(Control("settings:ControlDevice").title.text.Contains("Gamepad"))Click("ControlDevice");Click("ControlBinding");InputSystem.QueueStateEvent(Fixture<Keyboard>("keyboard"),new KeyboardState(Key.H));InputSystem.Update();Tick();Assert.That(Menu.IsRebindingControls,Is.False);Assert.That(Read<TopDown3DControlPreferences>("controlsDraft").bindings.Any(binding=>binding.actionPath=="Gameplay/Interact"&&binding.path=="<Keyboard>/h"),Is.True);InputSystem.QueueStateEvent(Fixture<Keyboard>("keyboard"),new KeyboardState());InputSystem.Update();Tick();Click("ControlsApply");Ready();Assert.That(view.SettingsPanel,Is.EqualTo("Settings"));Assert.That(File.ReadAllText(Path.Combine(Fixture<string>("directory"),"controls-v1.json")),Does.Contain("<Keyboard>/h"));Assert.That(Read<TopDown3DControlPreferencesService>("controls").Accepted.bindings.Any(binding=>binding.path=="<Keyboard>/h"),Is.True);
        }
        [Test] public void RejectedBindingsStayOutOfTheDraftAndRevealTheirReason()
        {
            Open();Click("Controls");Ready();if(Control("settings:ControlDevice").title.text.Contains("Gamepad"))Click("ControlDevice");Click("ControlBinding");InputSystem.QueueStateEvent(Fixture<Keyboard>("keyboard"),new KeyboardState(Key.W));InputSystem.Update();Tick();Assert.That(Menu.IsRebindingControls,Is.False);InputSystem.QueueStateEvent(Fixture<Keyboard>("keyboard"),new KeyboardState());InputSystem.Update();Tick();Assert.That(Read<TopDown3DControlPreferences>("controlsDraft").bindings,Is.Empty);Assert.That(view.RevealedSettingsNotice,Is.Not.Null);Assert.That(view.RevealedSettingsNotice.name,Is.EqualTo("ControlNotice"));Assert.That(File.Exists(Path.Combine(Fixture<string>("directory"),"controls-v1.json")),Is.False);
        }
        [Test] public void HeldNavigationMustReturnToNeutralBeforeANewSettingsPanelAcceptsInput()
        {
            Menu.OpenExpeditionSettings();fixture.ProcessUiState(new GamepadState{leftStick=Vector2.right});Assert.That(Control("settings:TextSize").Button.IsInteractable(),Is.False);Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(Control("settings:TextSize").gameObject));Ready();Assert.That(Control("settings:TextSize").Button.IsInteractable(),Is.True);
        }
        [TestCase(false)][TestCase(true)] public void NativeCameraOptionsRetainTheDraftOnBackAndApplyThroughTheExistingService(bool cameraApply)
        {
            cameraRoot=new GameObject("Settings camera fixture",typeof(Camera));cameraTarget=new GameObject("Settings camera target");var rig=cameraRoot.AddComponent<TopDown3DCameraRig>();rig.Configure(cameraTarget.transform);rig.EnableCameraOptions();cameraRoot.GetComponent<Camera>().enabled=false;typeof(TopDown3DMenuController).GetMethod("ConfigureCameraOptions",Private).Invoke(Menu,null);Open();Assert.That(Control("settings:CameraOptions").gameObject.activeInHierarchy,Is.True);Click("CameraOptions");Ready();Click("CameraInvert");Click("CameraBack");Ready();Assert.That(view.SettingsPanel,Is.EqualTo("Settings"));Assert.That(Read<TopDown3DMenuSettings>("settingsDraft").cameraInvertY,Is.True);Assert.That(Read<TopDown3DMenuSettingsService>("settings").Accepted.cameraInvertY,Is.False);if(cameraApply){Click("CameraOptions");Ready();Click("CameraApply");Ready();Assert.That(view.SettingsPanel,Is.EqualTo("Settings"));}else Click("SettingsApply");Assert.That(Read<TopDown3DMenuSettingsService>("settings").Accepted.cameraInvertY,Is.True);
        }
        [TestCase("{\"version\":99}")][TestCase("{broken")]
        public void UnsupportedRadialPreferencesCannotBeOverwrittenByTheNativeSetup(string original)
        {
            string file=Path.Combine(Fixture<string>("directory"),"radial-layout-v1.json");Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file,original);var radial=Fixture<TopDown3DRadialMenuController>("radial");typeof(TopDown3DRadialMenuController).GetMethod("LoadPreferences",Private).Invoke(radial,null);Open();Click("RadialSetup");Ready();Assert.That(Control("settings:SetupApply").Button.IsInteractable(),Is.False);Assert.That(radial.TryApplySetup(radial.Preferences.Clone(),out var error),Is.False);Assert.That(error,Does.Contain("preserved"));Assert.That(File.ReadAllText(file),Is.EqualTo(original));
        }
        [Test] public void FutureSettingsRemainProtectedAndFailedSaveStaysVisible()
        {
            var path=Path.Combine(Fixture<string>("directory"),"menu-settings-v1.json");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,"{\"version\":99}");typeof(TopDown3DMenuController).GetField("settings",Private).SetValue(Menu,new TopDown3DMenuSettingsService(Fixture<string>("directory")));Open();Assert.That(Control("settings:SettingsApply").Button.IsInteractable(),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo("{\"version\":99}"));Menu.Close();File.Delete(path);typeof(TopDown3DMenuController).GetField("settings",Private).SetValue(Menu,new TopDown3DMenuSettingsService(Fixture<string>("directory")));Directory.CreateDirectory(path);Open();Click("TextSize");Click("SettingsApply");Assert.That(Menu.IsOpen,Is.True);var notice=root.GetComponentsInChildren<Text>().Single(text=>text.text.StartsWith("Could not save settings.",StringComparison.Ordinal));var scroll=notice.GetComponentInParent<ScrollRect>();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,notice.transform);Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin+10));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax-10));Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(scroll.gameObject));Assert.That(Read<TopDown3DMenuSettingsService>("settings").Accepted.largeText,Is.False);
        }
        [Test] public void BackLeavesPageNameEditingBeforeOpeningADiscardDialog()
        {
            Open();Click("RadialSetup");Ready();Click("More");var field=root.GetComponentsInChildren<InputField>().Single(input=>input.name=="Shortcut page name");fixture.Events.SetSelectedGameObject(field.gameObject);field.OnSubmit(new BaseEventData(fixture.Events));typeof(InputField).GetMethod("LateUpdate",Private).Invoke(field,null);Assert.That(field.isFocused,Is.True);field.text="Survey";view.Back();Assert.That(field.isFocused,Is.False);Assert.That(view.SettingsPanel,Is.EqualTo("Setup"));Assert.That(Read<TopDown3DRadialPreferences>("radialDraft").pages[0].name,Is.EqualTo("Survey"));view.Back();Assert.That(view.SettingsPanel,Is.EqualTo("Dialog"));
        }
        [Test] public void ReadingPaneScrollRequiresNeutralOnEachSettingsTransition()
        {
            Open();var scroll=Control("settings:ViewDistance").GetComponentInParent<ScrollRect>();scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,3000);fixture.Events.SetSelectedGameObject(Control("settings:ViewDistance").gameObject);scroll.verticalNormalizedPosition=1;view.AdvanceSettingsScroll(new Vector2(0,-1),.3f);Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(1));view.AdvanceSettingsScroll(Vector2.zero,.1f);view.AdvanceSettingsScroll(new Vector2(0,-1),.3f);Assert.That(scroll.verticalNormalizedPosition,Is.LessThan(1));
        }
        [Test] public void FooterBackKeepsTheBodyFocusAndFooterScrollingUsesTheLastPane()
        {
            Open();fixture.Events.SetSelectedGameObject(Control("settings:Motion").gameObject);Click("Motion");fixture.Events.SetSelectedGameObject(Control("hint:back").gameObject);Control("hint:back").Button.onClick.Invoke();Ready();Click("DialogCancel");Ready();Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(Control("settings:Motion").gameObject));var scroll=Control("settings:Motion").GetComponentInParent<ScrollRect>();scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,3000);scroll.verticalNormalizedPosition=1;fixture.Events.SetSelectedGameObject(Control("settings:SettingsApply").gameObject);view.AdvanceSettingsScroll(Vector2.zero,.1f);view.AdvanceSettingsScroll(new Vector2(0,-1),.25f);Assert.That(scroll.verticalNormalizedPosition,Is.LessThan(1));Control("settings:SettingsApply").GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(fixture.Events){moveDir=MoveDirection.Up});Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(Control("settings:Motion").gameObject));
        }
        [Test] public void SettingsColumnsAndAssignmentDirectionsHaveControllerRoutes()
        {
            Open();Control("settings:Motion").GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(fixture.Events){moveDir=MoveDirection.Right});Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(Control("settings:RadialSetup").gameObject));Click("RadialSetup");Ready();var scroll=Control("settings:Slot0").GetComponentInParent<ScrollRect>();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,Control("settings:Slot0").transform);Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-.5f));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+.5f));Control("settings:Slot0").GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(fixture.Events){moveDir=MoveDirection.Right});Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(Control("settings:Slot1").gameObject));Control("settings:Slot1").GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(fixture.Events){moveDir=MoveDirection.Right});Assert.That(fixture.Events.currentSelectedGameObject,Is.SameAs(Control("settings:Assign0").gameObject));
        }
        [Test] public void ExpeditionRadialInventoryRoutesToTheNativeOwnerAndDetachesCleanly()
        {
            int opened=0;Menu.UseExpeditionPresentation(view,()=>opened++);var radial=Fixture<TopDown3DRadialMenuController>("radial");typeof(TopDown3DRadialMenuController).GetField("open",Private).SetValue(radial,true);typeof(TopDown3DRadialMenuController).GetField("selected",Private).SetValue(radial,0);typeof(TopDown3DRadialMenuController).GetMethod("Close",Private).Invoke(radial,new object[]{true});Assert.That(opened,Is.EqualTo(1));Assert.That(Fixture<TopDown3DInventoryUiController>("inventory").IsOpen,Is.False);Menu.UseExpeditionPresentation(null);Assert.That(radial.InventoryRequested,Is.Null);
        }
    }
    public sealed class CampMenuSettingsInputOwnershipTests
    {
        [Test] public void SessionReleaseFenceCannotRestoreSubmitWhileTheSettingsControllerOwnsIt()
        {
            const BindingFlags fields=BindingFlags.Instance|BindingFlags.NonPublic;var fixture=new CampExpeditionGamepadTests();fixture.Setup();try
            {
                var session=(CampExpeditionSession)typeof(CampExpeditionGamepadTests).GetField("session",fields).GetValue(fixture);var settings=(TopDown3DMenuController)typeof(CampExpeditionSession).GetField("settings",fields).GetValue(session);var module=(UnityEngine.InputSystem.UI.InputSystemUIInputModule)typeof(CampExpeditionGamepadTests).GetField("module",fields).GetValue(fixture);typeof(TopDown3DMenuController).GetField("panel",fields).SetValue(settings,"Controls");typeof(CampExpeditionSession).GetField("waitingRelease",fields).SetValue(session,true);module.submit=null;module.leftClick=null;typeof(CampExpeditionSession).GetMethod("Update",fields).Invoke(session,null);Assert.That(module.submit,Is.Null);Assert.That(module.leftClick,Is.Null);typeof(TopDown3DMenuController).GetField("panel",fields).SetValue(settings,null);
            }finally{fixture.Cleanup();}
        }
    }
}
