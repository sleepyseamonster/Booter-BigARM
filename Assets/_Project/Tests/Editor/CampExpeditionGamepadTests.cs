using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    // Runs action updates and the real uGUI module across queued EditMode rendering frames. The
    // release test invokes only Session.Update's menu fence (ledger is null). Session.Start,
    // gameplay simulation, physics, profiles and Play Mode remain outside this fixture.
    public sealed class CampExpeditionGamepadTests
    {
        private static readonly BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        private InputSettings previousSettings,testSettings;
        private Gamepad pad,previousPad;
        private Keyboard keyboard,previousKeyboard;
        private EventSystem events,previousEvents;
        private InputSystemUIInputModule module;
        private InputActionAsset actions;
        private TopDown3DInputRouter router;
        private CampExpeditionSession session;
        private CampExpeditionMenuView view;
        private GameObject routerObject,eventObject,sessionObject,viewObject;
        private readonly List<InputActionReference> references=new();
        private readonly List<string> commands=new();
        private bool moduleEnabled,eventsEnabled;
        private float previousTimeScale;

        [SetUp] public void Setup()
        {
            previousTimeScale=Time.timeScale;previousEvents=EventSystem.current;
            previousPad=Gamepad.current;previousKeyboard=Keyboard.current;
            previousSettings=InputSystem.settings;testSettings=Object.Instantiate(previousSettings);
            testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);
            // The pinned UI module tests WasPerformedThisDynamicUpdate, whose frame
            // accounting differs in Manual mode. Use the production Dynamic mode and
            // explicit Dynamic action updates, with Editor yields between submit frames.
            testSettings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings=testSettings;
            pad=InputSystem.AddDevice<Gamepad>();keyboard=InputSystem.AddDevice<Keyboard>();
            var authored=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            Assert.That(authored,Is.Not.Null);actions=AnimationPlaygroundInputFactory.Create(authored);
            actions.devices=new InputDevice[]{pad,keyboard};
            routerObject=new GameObject("Native menu input fixture");routerObject.SetActive(false);
            router=routerObject.AddComponent<TopDown3DInputRouter>();router.Configure(actions);
            // Supply the normal runtime factory's bookkeeping without invoking Play Mode.
            Set(router,"ownedAnimationActions",actions);Set(router,"authoredAnimationActions",authored);
            Assert.That(router.Initialize(),Is.True);

            eventObject=new GameObject("Native menu fixture EventSystem");eventObject.SetActive(false);
            events=eventObject.AddComponent<EventSystem>();events.sendNavigationEvents=true;
            // EditMode does not run EventSystem's registration lifecycle. Register the
            // synthetic system before selecting it; assigning an unregistered (or null)
            // EventSystem.current logs an error in this uGUI package.
            module=eventObject.AddComponent<InputSystemUIInputModule>();module.actionsAsset=actions;
            module.move=Reference("UI/Navigate");module.submit=Reference("UI/Submit");module.cancel=null;
            module.point=null;module.leftClick=null;module.rightClick=null;module.middleClick=null;module.scrollWheel=null;
            module.trackedDevicePosition=null;module.trackedDeviceOrientation=null;
            eventObject.SetActive(true);Call(events,"OnEnable");eventsEnabled=true;EventSystem.current=events;
            Call(module,"OnEnable");moduleEnabled=true;

            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab");
            Assert.That(prefab,Is.Not.Null);viewObject=Object.Instantiate(prefab);
            view=viewObject.GetComponent<CampExpeditionMenuView>();view.Initialize(events);
            view.Render(new CampMenuPresentation{canPlay=true,townAccess=true});view.SetVisible(true);view.ShowPage("play");
            Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();

            // Bind the real session's menu-only Tab/SubTab handlers to this real view.
            // This inactive component never starts, reads a profile, or creates a world.
            sessionObject=new GameObject("Native menu command fixture");sessionObject.SetActive(false);
            session=sessionObject.AddComponent<CampExpeditionSession>();
            var settings=sessionObject.AddComponent<TopDown3DMenuController>();
            Set(session,"input",router);Set(session,"module",module);Set(session,"view",view);Set(session,"events",events);
            Set(session,"settings",settings);Set(session,"snapshot",new CampExpeditionSnapshot{phase=CampExpeditionPhase.Town});
            Set(session,"menuOpen",true);Set(session,"waitingRelease",false);
            Call(session,"BindMenuCommands");router.EnterInventoryMode();module.cancel=null;
            Set(session,"submit",module.submit);Set(session,"click",Reference("UI/Click"));
            view.Command+=commands.Add;
            Send(new GamepadState());Send(new KeyboardState());commands.Clear();
        }

        [TearDown] public void Cleanup()
        {
            if(session!=null){Call(session,"UnbindMenuCommands");Set(session,"view",null);}
            if(moduleEnabled&&module!=null){Call(module,"OnDisable");moduleEnabled=false;}
            if(eventsEnabled&&events!=null){Call(events,"OnDisable");eventsEnabled=false;}
            if(router!=null)Call(router,"OnDisable");
            if(viewObject!=null)Object.DestroyImmediate(viewObject);
            if(sessionObject!=null)Object.DestroyImmediate(sessionObject);
            if(eventObject!=null)Object.DestroyImmediate(eventObject);
            if(routerObject!=null)Object.DestroyImmediate(routerObject);
            foreach(var reference in references)if(reference!=null)Object.DestroyImmediate(reference);
            references.Clear();commands.Clear();if(actions!=null)Object.DestroyImmediate(actions);
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(previousPad!=null&&previousPad.added)previousPad.MakeCurrent();
            if(previousKeyboard!=null&&previousKeyboard.added)previousKeyboard.MakeCurrent();
            if(previousEvents!=null)EventSystem.current=previousEvents;
            InputSystem.settings=previousSettings;
            if(testSettings!=null)Object.DestroyImmediate(testSettings);Time.timeScale=previousTimeScale;
        }

        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,PrivateInstance).SetValue(target,value);
        private static void Call(object target,string name)
        {var method=target.GetType().GetMethod(name,PrivateInstance);method.Invoke(target,method.GetParameters().Select(p=>p.DefaultValue).ToArray());}
        private InputActionReference Reference(string path)
        {var reference=InputActionReference.Create(actions.FindAction(path,true));references.Add(reference);return reference;}
        private CampMenuControl Control(string key)=>viewObject.GetComponentsInChildren<CampMenuControl>(true).Single(control=>control.key==key);
        private void UpdateActions(){InputSystem.Update();}
        private void Send(GamepadState state){InputSystem.QueueStateEvent(pad,state);UpdateActions();module.Process();}
        private void Send(KeyboardState state){InputSystem.QueueStateEvent(keyboard,state);UpdateActions();module.Process();}
        private static IEnumerator NextEditorRenderingFrame()
        {
            // A plain EditMode yield may leave Time.frameCount unchanged. The pinned
            // module consumes Submit once per rendering frame, so queue an Editor update
            // and prove its frame counter advanced before polling the held action again.
            int previous=Time.frameCount;
            for(int attempt=0;attempt<3&&Time.frameCount==previous;attempt++)
            {EditorApplication.QueuePlayerLoopUpdate();yield return null;}
            Assert.That(Time.frameCount,Is.GreaterThan(previous),
                "The candidate must advance an EditMode rendering frame to verify uGUI held-submit timing without entering Play Mode.");
        }

        [Test] public void ReturnReviewLocksPagesAndAllMenuOpeners()
        {
            Set(session,"snapshot",new CampExpeditionSnapshot{phase=CampExpeditionPhase.CargoReview});
            view.Render(new CampMenuPresentation{cargoReview=true,townAccess=true});
            commands.Clear();
            foreach(var page in new[]{"play","start","inventory","storage","workstations","settings"})
            {view.ShowPage(page);Assert.That(view.Page,Is.EqualTo("review"));}
            Call(session,"ToggleStart");Call(session,"ToggleInventory");Call(session,"OpenCargo");view.Back();
            foreach(var state in new[]{new GamepadState().WithButton(GamepadButton.LeftShoulder),new GamepadState().WithButton(GamepadButton.RightShoulder),new GamepadState{leftTrigger=1},new GamepadState{rightTrigger=1}})
            {Send(new GamepadState());Send(state);Assert.That(view.Page,Is.EqualTo("review"));}
            Assert.That(viewObject.transform.Find("Safe Area/Primary").gameObject.activeSelf,Is.False);
            foreach(var key in new[]{"page:start","page:inventory","hint:tabs","hint:subtabs","hint:back"})Assert.That(Control(key).gameObject.activeInHierarchy,Is.False,key);
            Assert.That(commands,Is.Empty);
        }
        [TestCase(false)][TestCase(true)] public void ReturnSubmitFocusesAndSendsAllFromAnItemOrMissingSelection(bool keyboardSubmit)
        {
            var row=new CampMenuStackView{key="1/0",owner="legger",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1};
            view.Render(new CampMenuPresentation{cargoReview=true,stacks=new[]{row}});
            Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("send-all").gameObject));
            events.SetSelectedGameObject(Control("cargo:0").gameObject);view.RefreshFocusedDetails();
            Assert.That(view.SelectedStack,Is.SameAs(row));
            if(keyboardSubmit)events.SetSelectedGameObject(null);
            if(keyboardSubmit)Send(new KeyboardState(Key.Enter));else Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("send-all").gameObject));
            Assert.That(commands.Count(c=>c=="send-all"),Is.EqualTo(1));
            module.Process();Assert.That(commands.Count(c=>c=="send-all"),Is.EqualTo(1));
        }
        [Test] public void ReturnItemsRemainReachableWithDirectionalInput()
        {
            var rows=Enumerable.Range(0,6).Select(i=>new CampMenuStackView{key="1/"+i,owner="legger",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}).ToArray();
            view.Render(new CampMenuPresentation{cargoReview=true,stacks=rows});Canvas.ForceUpdateCanvases();
            bool reached=false;
            foreach(var button in new[]{GamepadButton.DpadDown,GamepadButton.DpadRight,GamepadButton.DpadLeft,GamepadButton.DpadUp})
            {
                Send(new GamepadState());events.SetSelectedGameObject(Control("send-all").gameObject);
                Send(new GamepadState().WithButton(button));
                if(events.currentSelectedGameObject?.GetComponent<CampMenuControl>()?.key.StartsWith("cargo:")==true)reached=true;
                Assert.That(view.Page,Is.EqualTo("review"));
            }
            Assert.That(reached,Is.True,"The default return action must connect to the browsable cargo grid.");
        }
        private sealed class ReturnMemoryFile:ICampExpeditionFile
        {
            public CampExpeditionSnapshot saved;
            public CampExpeditionSnapshot Read()=>saved?.Clone();
            public void Write(CampExpeditionSnapshot value)=>saved=value.Clone();
        }
        [TestCase(false)][TestCase(true)] public void DefaultReturnSavesCargoAndTownBeforeOpeningPlay(bool hasCargo)
        {
            var config=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");var catalog=config.definition.itemCatalog;
            var booter=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,CampExpeditionLayout.Capacity,expeditionLayout:true));
            var legger=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12));
            var storage=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,24,campStorage:true));
            if(hasCargo)Assert.That(booter.TryAdd(new TopDown3DItemAmount(CampExpeditionDefinition.ToolZeroId,1)).Succeeded,Is.True);
            var state=new CampExpeditionSnapshot{sequence=1,profileId="return-fixture",contentFingerprint="fixture",phase=CampExpeditionPhase.CargoReview,storageCapacity=24,booter=booter.CaptureSnapshot(),legger=legger.CaptureSnapshot(),storage=storage.CaptureSnapshot()};
            var file=new ReturnMemoryFile();var ledger=new CampExpeditionLedger(file,state,booter,legger,storage);
            Set(session,"ledger",ledger);Set(session,"snapshot",state);Set(session,"<Configuration>k__BackingField",config);
            Set(session,"vitals",sessionObject.AddComponent<TopDown3DSurvivalVitals>());
            ledger.Changed+=()=>Call(session,"Committed");
            view.Render(new CampMenuPresentation{cargoReview=true});
            Action<string> dispatch=key=>typeof(CampExpeditionSession).GetMethod("Command",PrivateInstance).Invoke(session,new object[]{key});
            view.Command+=dispatch;
            try
            {
                Send(new GamepadState().WithButton(GamepadButton.South));
                Assert.That(file.saved,Is.Not.Null);Assert.That(file.saved.phase,Is.EqualTo(CampExpeditionPhase.Town));
                Assert.That(ledger.Booter.OccupiedSlotCount,Is.Zero);Assert.That(ledger.Storage.OccupiedSlotCount,Is.EqualTo(hasCargo?1:0));
                Assert.That(view.Page,Is.EqualTo("play"));Assert.That(module.submit,Is.Null,"The return press must release before another expedition can start.");
                Assert.That(ledger.State.receipts.Length,Is.EqualTo(1));
            }
            finally{view.Command-=dispatch;Set(session,"ledger",null);}
        }
        [Test] public void ReturnQuoteKeepsItsOwnSubmitAndBackTarget()
        {
            view.Render(new CampMenuPresentation{cargoReview=true});view.ShowQuote("Storage costs Commons");Set(session,"pending",new CampPreparedOperation());commands.Clear();
            Send(new GamepadState());Call(session,"Update");Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(commands,Is.EqualTo(new[]{"quote-cancel"}));Assert.That(view.Page,Is.EqualTo("review"));
        }

        [UnityTest] public IEnumerator PlayPageDefaultsToPlayAndSouthDispatchesThatSelectedControl()
        {
            Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("play").gameObject));
            Assert.That(commands,Does.Not.Contain("play"));
            Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(commands.Count(command=>command=="play"),Is.EqualTo(1));
            yield return NextEditorRenderingFrame();
            Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(commands.Count(command=>command=="play"),Is.EqualTo(1),"A held South button cannot submit again.");
        }

        [Test] public void SouthOnPlayDispatchesOnceBeforeTheUiModulePollsSelection()
        {
            events.SetSelectedGameObject(null);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));UpdateActions();
            Assert.That(commands.Count(command=>command=="play"),Is.EqualTo(1));
            Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("play").gameObject));
            module.Process();Assert.That(commands.Count(command=>command=="play"),Is.EqualTo(1),"uGUI must not dispatch the same expedition command again.");
        }

        [Test] public void PlayDispatchWaitsUntilInputCallbacksFinish()
        {
            int downstreamCallbacks=0;var action=actions.FindAction("UI/Submit",true);
            Action<InputAction.CallbackContext> observe=context=>
            {
                Assert.That(context.action,Is.SameAs(action));
                Assert.That(commands.Count(command=>command=="play"),Is.Zero,"Later input listeners must finish before the expedition command changes maps.");
                downstreamCallbacks++;
            };
            Action<string> close=command=>{if(command=="play")router.EnterGameplayMode();};
            action.performed+=observe;view.Command+=close;
            try
            {
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));UpdateActions();
                Assert.That(downstreamCallbacks,Is.EqualTo(1));
                Assert.That(commands.Count(command=>command=="play"),Is.EqualTo(1));
                Assert.That(router.Mode,Is.EqualTo(TopDown3DInputMode.Gameplay));
                module.Process();Assert.That(commands.Count(command=>command=="play"),Is.EqualTo(1));
            }
            finally{action.performed-=observe;view.Command-=close;}
        }

        [UnityTest] public IEnumerator GamepadNavigationChangesSelectionAndSouthSubmitsTheNewControl()
            =>NavigateAndSubmit(false);
        [UnityTest] public IEnumerator KeyboardNavigationChangesSelectionAndSouthSubmitsTheNewControl()
            =>NavigateAndSubmit(true);
        [TestCase(false)]
        [TestCase(true)]
        public void DirectionalInputCannotReachPrimaryOrSecondaryTabs(bool useStick)
        {
            var directions=new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right};
            var buttons=new[]{GamepadButton.DpadUp,GamepadButton.DpadDown,GamepadButton.DpadLeft,GamepadButton.DpadRight};
            foreach(var page in new[]{"play","storage","jobs","workstations","tree","crafting-equipment","tracked","legger-equipment","projects","inventory"})
            {
                view.ShowPage(page);Canvas.ForceUpdateCanvases();
                Assert.That(events.currentSelectedGameObject?.GetComponent<CampMenuControl>()?.tab,Is.Not.True,page+" must not open with tab focus.");
                var contents=viewObject.GetComponentsInChildren<CampMenuControl>().Where(c=>!c.tab&&!c.prompt&&c.Button.IsInteractable()&&c.Button.navigation.mode!=Navigation.Mode.None).ToArray();
                foreach(var control in contents)
                for(int i=0;i<directions.Length;i++)
                {
                    Send(new GamepadState());events.SetSelectedGameObject(control.gameObject);
                    Send(useStick?new GamepadState{leftStick=directions[i]}:new GamepadState().WithButton(buttons[i]));
                    var selected=events.currentSelectedGameObject?.GetComponent<CampMenuControl>();
                    Assert.That(selected?.tab,Is.Not.True,$"{page}: {control.key} / {directions[i]} / stick={useStick}");
                    Assert.That(view.Page,Is.EqualTo(page));
                }
            }
        }
        [TestCase("primary:outpost")]
        [TestCase("primary:crafting")]
        [TestCase("page:start")]
        public void PlayPageSouthTargetsPlayRegardlessOfThePreviouslySelectedControl(string key)
        {
            events.SetSelectedGameObject(Control(key).gameObject);Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(commands.Count(c=>c=="play"),Is.EqualTo(1));Assert.That(view.Page,Is.EqualTo("play"));
            Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("play").gameObject));
        }
        [Test] public void DirectionAndSouthTogetherStillSelectPlayOnThePlayPage()
        {
            events.SetSelectedGameObject(Control("primary:outpost").gameObject);
            Send(new GamepadState().WithButton(GamepadButton.DpadUp).WithButton(GamepadButton.South));
            Assert.That(commands.Count(c=>c=="play"),Is.EqualTo(1));Assert.That(view.Page,Is.EqualTo("play"));
        }
        [Test] public void DisabledPlayDoesNotSubmitAnUnrelatedSelectedTab()
        {
            view.Render(new CampMenuPresentation{canPlay=false});events.SetSelectedGameObject(Control("primary:outpost").gameObject);
            commands.Clear();Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(commands,Is.Empty);Assert.That(view.Page,Is.EqualTo("play"));
        }
        [Test] public void ConfirmationOnThePlayPageKeepsItsOwnSubmitTarget()
        {
            Set(session,"pending",new CampPreparedOperation());view.ShowQuote("Confirm transfer");
            Assert.That(module.submit,Is.Null);Send(new GamepadState());Call(session,"Update");
            Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(commands.Count(c=>c=="quote-cancel"),Is.EqualTo(1));Assert.That(commands,Does.Not.Contain("play"));
        }
        [TestCase("inventory",CampExpeditionPhase.Town)]
        [TestCase("start",CampExpeditionPhase.Town)]
        [TestCase("tree",CampExpeditionPhase.Town)]
        [TestCase("workstations",CampExpeditionPhase.Town)]
        [TestCase("inventory",CampExpeditionPhase.Exploring)]
        [TestCase("start",CampExpeditionPhase.Exploring)]
        [TestCase("tree",CampExpeditionPhase.Exploring)]
        [TestCase("workstations",CampExpeditionPhase.Exploring)]
        [TestCase("review",CampExpeditionPhase.CargoReview)]
        [TestCase("inventory",CampExpeditionPhase.CargoReview)]
        [TestCase("storage",CampExpeditionPhase.CargoReview)]
        [TestCase("tree",CampExpeditionPhase.CargoReview)]
        [TestCase("workstations",CampExpeditionPhase.CargoReview)]
        [TestCase("tracked",CampExpeditionPhase.CargoReview)]
        [TestCase("projects",CampExpeditionPhase.CargoReview)]
        public void BackReturnsToPlayAndKeepsTheFieldMenuOpen(string page,CampExpeditionPhase phase)
        {
            Set(session,"snapshot",new CampExpeditionSnapshot{phase=phase});Set(session,"frozen",true);
            if(phase==CampExpeditionPhase.CargoReview)view.Render(new CampMenuPresentation{cargoReview=true});else view.ShowPage(page);
            Action<string> command=key=>typeof(CampExpeditionSession).GetMethod("Command",PrivateInstance).Invoke(session,new object[]{key});
            Action back=()=>Call(session,"Back");view.Command+=command;router.UiCancelRequested+=back;
            try
            {
                Send(new GamepadState().WithButton(GamepadButton.East));
                Assert.That(view.Page,Is.EqualTo(phase==CampExpeditionPhase.CargoReview?"review":"play"));Assert.That(events.currentSelectedGameObject,Is.SameAs(Control(phase==CampExpeditionPhase.CargoReview?"send-all":"play").gameObject));
                Assert.That(typeof(CampExpeditionSession).GetField("menuOpen",PrivateInstance).GetValue(session),Is.EqualTo(true));
                Assert.That(typeof(CampExpeditionSession).GetField("frozen",PrivateInstance).GetValue(session),Is.EqualTo(true));
                Assert.That(router.Mode,Is.EqualTo(TopDown3DInputMode.Inventory));Assert.That(commands,Does.Not.Contain("play"));
            }
            finally{view.Command-=command;router.UiCancelRequested-=back;}
        }
        [UnityTest] public IEnumerator BackWithSouthLandsOnPlayAndRequiresAFreshSouthToStart()
        {
            view.ShowPage("tree");Action back=()=>Call(session,"Back");
            Action<string> route=key=>{if(key.StartsWith("back:",StringComparison.Ordinal))typeof(CampExpeditionSession).GetMethod("Command",PrivateInstance).Invoke(session,new object[]{key});};
            view.Command+=route;router.UiCancelRequested+=back;
            try
            {
                Send(new GamepadState().WithButton(GamepadButton.East).WithButton(GamepadButton.South));
                Assert.That(view.Page,Is.EqualTo("play"));Assert.That(commands,Does.Not.Contain("play"));Assert.That(module.submit,Is.Null);
                yield return NextEditorRenderingFrame();Send(new GamepadState());Call(session,"Update");
                yield return NextEditorRenderingFrame();Send(new GamepadState().WithButton(GamepadButton.South));
                Assert.That(commands.Count(c=>c=="play"),Is.EqualTo(1));
            }
            finally{view.Command-=route;router.UiCancelRequested-=back;}
        }
        [UnityTest] public IEnumerator KeyboardEnterAlsoSubmitsPlayOncePerFreshPress()
        {
            Send(new KeyboardState(Key.Enter));Assert.That(commands.Count(c=>c=="play"),Is.EqualTo(1));
            yield return NextEditorRenderingFrame();Send(new KeyboardState(Key.Enter));Assert.That(commands.Count(c=>c=="play"),Is.EqualTo(1));
            yield return NextEditorRenderingFrame();Send(new KeyboardState());
            yield return NextEditorRenderingFrame();Send(new KeyboardState(Key.Enter));Assert.That(commands.Count(c=>c=="play"),Is.EqualTo(2));
        }
        [Test] public void TreeLeftStickRoutesToPanWhileDpadOwnsFocusNavigation()
        {
            view.ShowPage("tree");var selected=events.currentSelectedGameObject;
            Send(new GamepadState{leftStick=new Vector2(1,0)});
            Assert.That(module.move.action.name,Is.EqualTo("ExpeditionTreeNavigate"));
            Assert.That((Vector2)typeof(CampExpeditionSession).GetField("panAxis",PrivateInstance).GetValue(session),Is.EqualTo(new Vector2(1,0)));
            Assert.That(events.currentSelectedGameObject,Is.SameAs(selected));
            view.ShowPage("storage");Assert.That(module.move.action.name,Is.EqualTo("Navigate"));
        }
        [Test] public void TrackedRawSticksUseSeparatePanesWithoutMovingTheGridThroughTheModule()
        {
            var ids=new[]{"author.bio.stone_bone",CampExpeditionDefinition.ToolZeroId};view.Render(new CampMenuPresentation{tracked=ids});view.ShowPage("tracked");Call(session,"AdvanceMenuScroll");
            var selected=events.currentSelectedGameObject;Send(new GamepadState{leftStick=new Vector2(0,-.56f),rightStick=new Vector2(.56f,0)});
            Assert.That(module.move.action.name,Is.EqualTo("ExpeditionTreeNavigate"));Assert.That(events.currentSelectedGameObject,Is.SameAs(selected));
            Assert.That(((Vector2)typeof(CampExpeditionSession).GetField("trackedLeftAxis",PrivateInstance).GetValue(session)).y,Is.EqualTo(-.56f).Within(.001f));
            Call(session,"AdvanceMenuScroll");Assert.That(view.TrackedFilter,Is.EqualTo(CampMenuCatalog.Load().ResultsUsing(ids)[0]));Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("track").gameObject));
            Send(new GamepadState().WithButton(GamepadButton.DpadLeft));Assert.That(events.currentSelectedGameObject.GetComponent<CampMenuControl>().key,Does.StartWith("bookmark:"));
            view.ShowPage("storage");Assert.That(module.move.action.name,Is.EqualTo("Navigate"));
        }
        [Test] public void TrackedPaneSticksStayGatedThroughConfirmationUntilReleased()
        {
            view.Render(new CampMenuPresentation{tracked=new[]{"author.bio.stone_bone"}});view.ShowPage("tracked");Call(session,"AdvanceMenuScroll");
            Set(session,"pending",new CampPreparedOperation());view.ShowQuote("Waiting for save");Call(session,"ArmRelease");Send(new GamepadState{leftStick=Vector2.down,rightStick=Vector2.right});Call(session,"AdvanceMenuScroll");
            Assert.That(view.TrackedFilter,Is.Empty);Set(session,"pending",null);view.HideQuote();Call(session,"AdvanceMenuScroll");Assert.That(view.TrackedFilter,Is.Empty);
            Send(new GamepadState());Call(session,"Update");Call(session,"AdvanceMenuScroll");Send(new GamepadState{leftStick=Vector2.down});Call(session,"AdvanceMenuScroll");Assert.That(view.TrackedFilter,Is.Not.Empty);
        }
        [UnityTest] public IEnumerator ProjectSouthRequestsOneQuantityChangePerFreshPress()
        {
            string id=CampExpeditionDefinition.ToolOneId;view.Render(new CampMenuPresentation{planning=new CampMenuPlanningView{projects=new[]{new CampMenuProjectView{rootId=id,remaining=1}}}});view.ShowPage("projects");events.SetSelectedGameObject(Control("project:plus:"+id).gameObject);commands.Clear();
            Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(commands.Count(command=>command=="project:set:2:"+id),Is.EqualTo(1));yield return NextEditorRenderingFrame();Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(commands.Count(command=>command=="project:set:2:"+id),Is.EqualTo(1));yield return NextEditorRenderingFrame();Send(new GamepadState());yield return NextEditorRenderingFrame();Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(commands.Count(command=>command=="project:set:2:"+id),Is.EqualTo(2));Assert.That(commands,Does.Not.Contain("play"));
        }
        [Test] public void ProjectQuantityFocusRequiresSubmitToEditAndBackExitsBeforePageNavigation()
        {
            view.Render(new CampMenuPresentation{planning=new CampMenuPlanningView{projects=new[]{new CampMenuProjectView{rootId=CampExpeditionDefinition.ToolOneId,remaining=1}}}});view.ShowPage("projects");var quantity=viewObject.GetComponentsInChildren<InputField>(true).Single(field=>field.name=="Project quantity "+CampExpeditionDefinition.ToolOneId);events.SetSelectedGameObject(quantity.gameObject);
            Assert.That(quantity.shouldActivateOnSelect,Is.False);Assert.That(quantity.isFocused,Is.False);quantity.OnSubmit(new BaseEventData(events));Call(quantity,"LateUpdate");Assert.That(quantity.isFocused,Is.True);Send(new GamepadState().WithButton(GamepadButton.RightShoulder));Assert.That(view.Page,Is.EqualTo("projects"));view.Back();Assert.That(quantity.isFocused,Is.False);Assert.That(view.Page,Is.EqualTo("projects"));
            Send(new GamepadState());Call(session,"Update");Send(new GamepadState().WithButton(GamepadButton.DpadRight));Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("project:plus:"+CampExpeditionDefinition.ToolOneId).gameObject));
        }
        [UnityTest] public IEnumerator TreeCameraReadsOneDeadzoneAndSouthSelectsItsCenterBeforeDpadExits()
        {
            view.ShowPage("tree");Call(session,"AdvanceMenuScroll");var camera=view.TreeCanvas;camera.RefreshCamera();
            Send(new GamepadState{leftStick=new Vector2(.61f,.61f),rightStick=new Vector2(0,.61f)});
            var pan=(Vector2)typeof(CampExpeditionSession).GetField("panAxis",PrivateInstance).GetValue(session);
            Assert.That(pan.x,Is.EqualTo(.5f).Within(.0001f));Assert.That(pan.y,Is.EqualTo(.5f).Within(.0001f));
            Assert.That((float)typeof(CampExpeditionSession).GetField("treeZoomAxis",PrivateInstance).GetValue(session),Is.EqualTo(.5f).Within(.0001f));
            yield return NextEditorRenderingFrame();var before=camera.Offset;float scale=camera.Scale;Call(session,"AdvanceMenuScroll");
            Assert.That(camera.Offset,Is.Not.EqualTo(before));Assert.That(camera.Scale,Is.GreaterThan(scale));Assert.That(events.currentSelectedGameObject,Is.SameAs(camera.gameObject));
            Send(new GamepadState());var node=view.definition.items.First(i=>i.visibleInTree);var viewport=(RectTransform)camera.transform;
            var center=(viewport.rect.size*.5f-camera.Offset)/camera.Scale;
            camera.Pan((center-new Vector2(node.treeX+72.5f,node.treeY+119))*camera.Scale);
            yield return NextEditorRenderingFrame();Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(view.InspectedItem,Is.EqualTo(node.id));Assert.That(view.TreeTrackingTarget,Is.EqualTo(node.id));Assert.That(commands,Does.Not.Contain("play"));
            yield return NextEditorRenderingFrame();Send(new GamepadState().WithButton(GamepadButton.DpadRight));
            Assert.That(camera.OwnsFocus,Is.False);Assert.That(events.currentSelectedGameObject,Is.Not.SameAs(camera.gameObject));
            Assert.That(events.currentSelectedGameObject.GetComponent<CampMenuControl>().key,Does.Not.StartWith("tree-node:"));
        }
        [Test] public void TreeKeyboardZoomAndHomeUseTheRealActionsOnlyWhileCanvasOwnsFocus()
        {
            view.ShowPage("tree");var camera=view.TreeCanvas;camera.RefreshCamera();float fit=camera.Scale;
            Send(new KeyboardState(Key.Equals));Assert.That(camera.Scale,Is.EqualTo(fit),"Sidebar focus does not own camera shortcuts.");Send(new KeyboardState());
            camera.Pan(Vector2.zero);Send(new KeyboardState(Key.Equals));Assert.That(camera.Scale,Is.EqualTo(fit*1.15f).Within(.0001f));
            Send(new KeyboardState());Send(new KeyboardState(Key.Home));Assert.That(camera.Scale,Is.EqualTo(fit).Within(.0001f));
            Send(new KeyboardState());var offset=camera.Offset;Send(new KeyboardState(Key.RightArrow));Assert.That(camera.Offset.x,Is.EqualTo(offset.x-60).Within(.001f));
            Send(new KeyboardState());Send(new KeyboardState(Key.NumpadMinus));Assert.That(camera.Scale,Is.EqualTo(Mathf.Max(.1f,fit/1.15f)).Within(.0001f));
        }
        [UnityTest] public IEnumerator TreeConfirmationRequiresNeutralSticksBeforeCanvasCanResume()
        {
            view.ShowPage("tree");Call(session,"AdvanceMenuScroll");var camera=view.TreeCanvas;camera.RefreshCamera();
            Set(session,"pending",new CampPreparedOperation());view.ShowQuote("Confirm transaction");Call(session,"ArmRelease");
            var before=camera.Offset;float scale=camera.Scale;
            Send(new GamepadState{leftStick=Vector2.one,rightStick=Vector2.up});Call(session,"AdvanceMenuScroll");
            Assert.That(camera.Offset,Is.EqualTo(before));Assert.That(camera.Scale,Is.EqualTo(scale));
            Set(session,"pending",null);view.HideQuote();Call(session,"AdvanceMenuScroll");
            Assert.That(camera.Offset,Is.EqualTo(before));Assert.That(camera.Scale,Is.EqualTo(scale),"Held sticks cannot resume through the dialog release fence.");
            Send(new GamepadState());Call(session,"Update");Call(session,"AdvanceMenuScroll");Send(new GamepadState{leftStick=Vector2.right});
            yield return NextEditorRenderingFrame();Call(session,"AdvanceMenuScroll");Assert.That(camera.Offset.x,Is.LessThan(before.x));
        }
        private IEnumerator NavigateAndSubmit(bool useKeyboard)
        {
            view.ShowInventory("booter");
            var initial=events.currentSelectedGameObject;Assert.That(initial,Is.SameAs(Control("equip:frame").gameObject));
            if(useKeyboard)Send(new KeyboardState(Key.UpArrow));
            else Send(new GamepadState().WithButton(GamepadButton.DpadUp));
            var selected=events.currentSelectedGameObject;
            Assert.That(selected,Is.Not.Null);Assert.That(selected,Is.Not.SameAs(initial));
            var control=selected.GetComponent<CampMenuControl>();Assert.That(control,Is.Not.Null);
            Assert.That(control.gameObject.activeInHierarchy&&control.Button.interactable,Is.True);
            Assert.That(commands,Does.Not.Contain("play"));
            int activations=0;control.Activated+=_=>activations++;
            yield return NextEditorRenderingFrame();
            if(useKeyboard)Send(new KeyboardState());else Send(new GamepadState());
            yield return NextEditorRenderingFrame();
            Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(activations,Is.EqualTo(1));Assert.That(commands,Does.Not.Contain("play"));
        }

        [UnityTest] public IEnumerator SessionReleaseFenceRequiresReleaseThenOneFreshSouthPress()
        {
            var originalSubmit=module.submit;
            // Prime the opener before the menu owns input. Once open, South is now
            // dispatched directly and does not need the UI module to be polled.
            Set(session,"menuOpen",false);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));UpdateActions();
            Assert.That(actions.FindAction("UI/Submit",true).IsPressed(),Is.True);
            Assert.That(commands,Does.Not.Contain("play"));Set(session,"menuOpen",true);
            Call(session,"ArmRelease");Assert.That(module.submit,Is.Null);
            yield return NextEditorRenderingFrame();
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));UpdateActions();
            Call(session,"Update");module.Process();
            Assert.That(module.submit,Is.Null,"Held South must keep the menu release fence armed.");
            Assert.That(commands,Does.Not.Contain("play"));
            yield return NextEditorRenderingFrame();
            InputSystem.QueueStateEvent(pad,new GamepadState());UpdateActions();Call(session,"Update");module.Process();
            Assert.That(module.submit,Is.SameAs(originalSubmit));Assert.That(commands,Does.Not.Contain("play"));
            yield return NextEditorRenderingFrame();
            Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(commands.Count(command=>command=="play"),Is.EqualTo(1));
            yield return NextEditorRenderingFrame();
            Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(commands.Count(command=>command=="play"),Is.EqualTo(1));
        }

        [Test] public void RouterOwnedCancelBacksOutOfPickerExactlyOnce()
        {
            view.ShowInventory("booter");view.ShowPicker("tool",Array.Empty<CampMenuStackView>());
            int backs=0;Action back=()=>{backs++;view.Back();};router.UiCancelRequested+=back;
            try
            {
                Assert.That(module.cancel,Is.Null);
                Send(new GamepadState().WithButton(GamepadButton.East));
                Assert.That(backs,Is.EqualTo(1));Assert.That(view.PickerTarget,Is.Null);Assert.That(view.Page,Is.EqualTo("inventory"));
                Send(new GamepadState().WithButton(GamepadButton.East));
                Assert.That(backs,Is.EqualTo(1));Assert.That(view.Page,Is.EqualTo("inventory"));
                Assert.That(commands.Any(command=>command.StartsWith("back:",StringComparison.Ordinal)),Is.False);
            }
            finally{router.UiCancelRequested-=back;}
        }
        [UnityTest] public IEnumerator TabSwitchWithSouthCannotSubmitTheNewDestinationUntilRelease()
        {
            int activations=0;Control("cargo:0").Activated+=_=>activations++;
            Send(new GamepadState().WithButton(GamepadButton.RightShoulder).WithButton(GamepadButton.South));
            Assert.That(view.Page,Is.EqualTo("storage"));Assert.That(activations,Is.Zero,"A tab-changing press cannot activate the new page.");
            yield return NextEditorRenderingFrame();Send(new GamepadState());Call(session,"Update");
            yield return NextEditorRenderingFrame();Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(activations,Is.EqualTo(1));
        }
        [Test] public void BackWithSouthCannotReactivateTheUnderlyingEquipmentSlot()
        {
            view.ShowInventory("booter");view.ShowPicker("tool",Array.Empty<CampMenuStackView>());
            Action back=()=>Call(session,"Back");router.UiCancelRequested+=back;
            try
            {
                Send(new GamepadState().WithButton(GamepadButton.East).WithButton(GamepadButton.South));
                Assert.That(view.PickerTarget,Is.Null);Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("equip:tool").gameObject));
                Assert.That(commands,Does.Not.Contain("equip:tool"));Assert.That(module.submit,Is.Null);
            }
            finally{router.UiCancelRequested-=back;}
        }
        [TestCase(GamepadButton.DpadUp)]
        [TestCase(GamepadButton.DpadDown)]
        [TestCase(GamepadButton.DpadLeft)]
        [TestCase(GamepadButton.DpadRight)]
        public void QuoteNavigationCannotReachTheSearchFieldBehindTheModal(GamepadButton direction)
        {
            view.ShowPage("storage");view.ShowQuote("Confirm transfer");Send(new GamepadState().WithButton(direction));
            Assert.That(events.currentSelectedGameObject,Is.Not.Null);
            Assert.That(events.currentSelectedGameObject.GetComponent<CampMenuControl>()?.key,Is.EqualTo("quote-cancel").Or.EqualTo("quote-confirm"));
            Assert.That(view.storageSearch.IsInteractable(),Is.False);
        }
        [Test] public void EditingSearchSuppressesTabShortcutsAndBackExitsEditingFirst()
        {
            view.ShowPage("storage");events.SetSelectedGameObject(view.storageSearch.gameObject);
            typeof(InputField).GetMethod("LateUpdate",PrivateInstance).Invoke(view.storageSearch,null);
            Assert.That(view.storageSearch.isFocused,Is.True,"The real text editor must be active for this check.");
            Send(new KeyboardState(Key.E));Assert.That(view.Page,Is.EqualTo("storage"));
            view.Back();Assert.That(view.storageSearch.isFocused,Is.False);Assert.That(commands,Does.Not.Contain("back:storage"));
            view.Back();Assert.That(commands.Count(c=>c=="back:storage"),Is.EqualTo(1));
        }
        [Test] public void WestWhileChoosingEquipmentCannotUnequipTheUnderlyingSlot()
        {
            var tool=new CampMenuStackView{key="0/8",owner="booter",itemId=CampExpeditionDefinition.ToolZeroId,equipmentAddress="tool",quantity=1};
            view.Render(new CampMenuPresentation{stacks=new[]{tool}});view.ShowInventory("booter");events.SetSelectedGameObject(Control("equip:tool").gameObject);
            view.ShowPicker("tool",new[]{tool});Assert.That(Control("hint:west").gameObject.activeInHierarchy,Is.False);
            Send(new GamepadState().WithButton(GamepadButton.West));
            Assert.That(view.PickerTarget,Is.EqualTo("tool"));Assert.That(view.SelectedStack,Is.SameAs(tool));
            Assert.That(typeof(CampExpeditionSession).GetField("message",PrivateInstance).GetValue(session),Is.Null);
        }
        [Test] public void FieldMenuButtonsSwitchDestinationsBeforeTogglingTheCurrentDestinationClosed()
        {
            // A usable field menu needs a valid ledger. A ledgerless session now correctly
            // renders stopped-profile recovery instead of allowing inert destinations.
            var configuration=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");var catalog=configuration.definition.itemCatalog;
            var booter=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,CampExpeditionLayout.Capacity,expeditionLayout:true));var cargo=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12));var storage=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,24,campStorage:true));
            var state=new CampExpeditionSnapshot{phase=CampExpeditionPhase.Exploring,storageCapacity=24,booter=booter.CaptureSnapshot(),legger=cargo.CaptureSnapshot(),storage=storage.CaptureSnapshot()};
            typeof(CampExpeditionSession).GetProperty("Configuration").SetValue(session,configuration);Set(session,"ledger",new CampExpeditionLedger(new NoWriteMenuFile(),state,booter,cargo,storage));Set(session,"snapshot",state);Set(session,"vitals",sessionObject.AddComponent<TopDown3DSurvivalVitals>());Set(session,"kit",routerObject.AddComponent<TopDown3DPlayerInventory>());view.ShowInventory("booter");
            Action start=()=>Call(session,"ToggleStart"),inventory=()=>Call(session,"ToggleInventory");router.MenuToggleRequested+=start;router.InventoryToggleRequested+=inventory;
            try
            {
                Send(new GamepadState().WithButton(GamepadButton.Start));Assert.That(view.Page,Is.EqualTo("start"));Assert.That(router.Mode,Is.EqualTo(TopDown3DInputMode.Inventory));
                Send(new GamepadState());Set(session,"frozen",true);Call(session,"Update");Send(new GamepadState().WithButton(GamepadButton.North));Assert.That(view.Page,Is.EqualTo("inventory"));Assert.That(router.Mode,Is.EqualTo(TopDown3DInputMode.Inventory));
                Send(new GamepadState());Set(session,"frozen",true);Call(session,"Update");Send(new GamepadState().WithButton(GamepadButton.North));Assert.That(router.Mode,Is.EqualTo(TopDown3DInputMode.Gameplay));
            }
            finally{router.MenuToggleRequested-=start;router.InventoryToggleRequested-=inventory;}
        }
        private sealed class NoWriteMenuFile:ICampExpeditionFile
        {public CampExpeditionSnapshot Read()=>null;public void Write(CampExpeditionSnapshot value)=>throw new AssertionException("Menu-only hotkeys cannot write a profile.");}
        [Test] public void MenuOpenersCannotChangeThePageBehindAPendingQuote()
        {
            view.ShowPage("storage");view.ShowQuote("Confirm storage");Set(session,"pending",new CampPreparedOperation());
            Call(session,"ToggleStart");Call(session,"ToggleInventory");Assert.That(view.Page,Is.EqualTo("storage"));Assert.That(Control("quote-confirm").gameObject.activeInHierarchy,Is.True);
        }
        [Test] public void FooterBindingsFollowKeyboardGamepadAndOwnedBindingOverrides()
        {
            Text Binding(string key)=>Control(key).transform.Find("Current binding").GetComponent<Text>();
            Send(new KeyboardState(Key.UpArrow));Assert.That(router.PromptDevice,Is.EqualTo(TopDown3DPromptDevice.KeyboardMouse));
            Assert.That(Binding("hint:select").text,Does.Contain(router.GetBindingDisplayName("UI/Submit","Select")));
            Assert.That(Binding("hint:subtabs").text,Is.EqualTo("Click"));
            Assert.That(Control("hint:select").transform.Find("Binding glyph").GetComponent<CampMenuBindingGlyph>().Kind,Is.EqualTo(CampMenuBindingGlyph.Shape.Key));
            var action=actions.FindAction("System/ToggleInventory",true);int index=Enumerable.Range(0,action.bindings.Count).First(i=>action.bindings[i].path=="<Gamepad>/buttonNorth");action.ApplyBindingOverride(index,"<Gamepad>/select");
            Send(new GamepadState().WithButton(GamepadButton.LeftShoulder));Assert.That(router.PromptDevice,Is.EqualTo(TopDown3DPromptDevice.Gamepad));
            Assert.That(Binding("page:inventory").text,Does.Contain(router.GetBindingDisplayName("System/ToggleInventory","Inventory")));
            Assert.That(Control("page:inventory").transform.Find("Binding glyph").GetComponent<CampMenuBindingGlyph>().Kind,Is.EqualTo(CampMenuBindingGlyph.Shape.View),"The glyph must follow the actual override, not the action's default North button.");
            Assert.That(Binding("hint:navigate").text,Is.EqualTo(pad.dpad.shortDisplayName+" / "+pad.leftStick.shortDisplayName));
            Assert.That(Control("hint:select").transform.Find("Binding glyph").GetComponent<CampMenuBindingGlyph>().Kind,Is.EqualTo(CampMenuBindingGlyph.Shape.South));
            Assert.That(Control("hint:tabs").transform.Find("Binding glyph left").GetComponent<CampMenuBindingGlyph>().Kind,Is.EqualTo(CampMenuBindingGlyph.Shape.LeftBumper));
            Assert.That(Control("hint:subtabs").transform.Find("Binding glyph right").GetComponent<CampMenuBindingGlyph>().Kind,Is.EqualTo(CampMenuBindingGlyph.Shape.RightTrigger));
            Assert.That(actions.FindAction("UI/PreviousExpeditionSubTab",true).bindings[0].groups,Is.EqualTo("Gamepad"));
        }
        [Test] public void ChangingControllersRefreshesBindingNamesWithinTheGamepadScheme()
        {
            var dual=(Gamepad)InputSystem.AddDevice("DualSenseGamepadHID");
            try
            {
                actions.devices=new InputDevice[]{pad,keyboard,dual};
                // Establish input ownership after expanding this fixture's restricted device set.
                Send(new KeyboardState(Key.UpArrow));
                // The native HID layout requires its own report, not generic GamepadState.
                InputSystem.QueueStateEvent(dual,new UnityEngine.InputSystem.DualShock.LowLevel.DualSenseHIDInputReport{leftStickX=127,leftStickY=127,rightStickX=127,rightStickY=127,buttons0=2});UpdateActions();module.Process();
                Assert.That(router.PromptDevice,Is.EqualTo(TopDown3DPromptDevice.Gamepad));
                Assert.That(Control("hint:select").transform.Find("Current binding").GetComponent<Text>().text,Is.EqualTo("Cross"));
                Send(new GamepadState().WithButton(GamepadButton.DpadLeft));
                Assert.That(router.PromptDevice,Is.EqualTo(TopDown3DPromptDevice.Gamepad));
                Assert.That(Control("hint:select").transform.Find("Current binding").GetComponent<Text>().text,Is.EqualTo(pad.buttonSouth.shortDisplayName));
                Send(new KeyboardState());Send(new KeyboardState(Key.UpArrow));InputSystem.RemoveDevice(dual);
                Assert.That(router.PromptDevice,Is.EqualTo(TopDown3DPromptDevice.KeyboardMouse),"Removing an unused controller must preserve keyboard prompts.");
            }
            finally{if(dual.added)InputSystem.RemoveDevice(dual);pad.MakeCurrent();}
        }
        private sealed class NoWriteFile:ICampExpeditionFile
        {
            public int writes;
            public CampExpeditionSnapshot Read()=>null;
            public void Write(CampExpeditionSnapshot value){writes++;throw new InvalidOperationException("This quote fixture cannot commit.");}
        }
        [Test] public void ReviewWestQuotesStorageForEquippedGearWithoutUnequippingOrCommittingIt()
        {
            var catalog=view.definition.itemCatalog;
            var booter=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,CampExpeditionLayout.Capacity,expeditionLayout:true));
            var legger=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12));
            var storage=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,24,campStorage:true));
            Assert.That(booter.TryAdd(new TopDown3DItemAmount(CampExpeditionDefinition.ToolZeroId,1)).Succeeded,Is.True);
            var snapshot=new CampExpeditionSnapshot{sequence=1,profileId="gui-review",contentFingerprint="fixture",phase=CampExpeditionPhase.CargoReview,storageCapacity=24,booter=booter.CaptureSnapshot(),legger=legger.CaptureSnapshot(),storage=storage.CaptureSnapshot()};
            var file=new NoWriteFile();var ledger=new CampExpeditionLedger(file,snapshot,booter,legger,storage);
            Set(session,"ledger",ledger);Set(session,"snapshot",snapshot);
            Set(session,"<Configuration>k__BackingField",Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration"));
            view.Render(new CampMenuPresentation{stacks=new[]{new CampMenuStackView{key="0/8",owner="booter",itemId=CampExpeditionDefinition.ToolZeroId,equipmentAddress="tool",quantity=1,instanceId=ledger.Booter.Slots[8].InstanceId,storeRevision=ledger.Booter.Revision}}});view.ShowPage("review");events.SetSelectedGameObject(Control("cargo:0").gameObject);
            Send(new GamepadState().WithButton(GamepadButton.West));
            var proposal=(CampPreparedOperation)typeof(CampExpeditionSession).GetField("pending",PrivateInstance).GetValue(session);
            Assert.That(proposal,Is.Not.Null);Assert.That(proposal.drafts[2].Slots.Any(s=>s.ItemId==CampExpeditionDefinition.ToolZeroId),Is.True);
            Assert.That(ledger.Booter.Slots[8].ItemId,Is.EqualTo(CampExpeditionDefinition.ToolZeroId));Assert.That(ledger.Legger.OccupiedSlotCount,Is.Zero);Assert.That(file.writes,Is.Zero);
            Assert.That(Control("quote-confirm").Button.IsInteractable(),Is.True);
        }

        [TestCase("guide")][TestCase("almanac")][TestCase("map")]
        public void ResourcePagesIgnoreContextTriggersRatherThanJumpingIntoCrafting(string page)
        {
            view.ShowPage(page);Send(new GamepadState{rightTrigger=1});Assert.That(view.Page,Is.EqualTo(page));Send(new GamepadState());Send(new GamepadState{leftTrigger=1});Assert.That(view.Page,Is.EqualTo(page));Assert.That(commands,Does.Not.Contain("play"));
        }
        [Test] public void SouthOpensGuideFromStartWithoutDispatchingPlay()
        {
            view.ShowPage("start");Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("guide").gameObject));Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(view.Page,Is.EqualTo("guide"));Assert.That(commands,Does.Not.Contain("play"));
        }
        [Test] public void SouthOnTheGuideArticleDoesNotDispatchAReservedServiceCommand()
        {
            view.ShowPage("guide");events.SetSelectedGameObject(Control("guide-article").gameObject);commands.Clear();Send(new GamepadState().WithButton(GamepadButton.South));Assert.That(commands,Is.Empty);Assert.That(view.Page,Is.EqualTo("guide"));
        }
        [TestCase("LB","play","workstations","PreviousInventory")]
        [TestCase("RB","play","storage","NextInventory")]
        [TestCase("LT","tree","workstations","PreviousExpeditionSubTab")]
        [TestCase("LT","workstations","projects","PreviousExpeditionSubTab")]
        [TestCase("RT","workstations","tree","NextExpeditionSubTab")]
        [TestCase("RT","projects","workstations","NextExpeditionSubTab")]
        [TestCase("RT","tree","crafting-equipment","NextExpeditionSubTab")]
        public void ShoulderAndTriggerActionsReachTheirRealSessionTabHandlers(string input,string start,string expected,string actionName)
        {
            view.ShowPage(start);var action=actions.FindAction("UI/"+actionName,true);int performed=0;action.performed+=_=>performed++;
            var state=input=="LB"?new GamepadState().WithButton(GamepadButton.LeftShoulder)
                :input=="RB"?new GamepadState().WithButton(GamepadButton.RightShoulder)
                :input=="LT"?new GamepadState{leftTrigger=1}:new GamepadState{rightTrigger=1};
            Send(state);Assert.That(performed,Is.EqualTo(1));Assert.That(view.Page,Is.EqualTo(expected));
            var selected=events.currentSelectedGameObject;
            if(expected=="projects")Assert.That(selected,Is.SameAs(Control("project-browse").gameObject));
            else
            {
                Assert.That(selected,Is.Not.Null);
                var control=selected.GetComponent<CampMenuControl>();Assert.That(control,Is.Not.Null);
                Assert.That(control.gameObject.activeInHierarchy&&control.Button.IsInteractable(),Is.True);
            }
            Send(state);Assert.That(performed,Is.EqualTo(1));Assert.That(view.Page,Is.EqualTo(expected));
            Assert.That(commands,Does.Not.Contain("play"));
        }
    }
}
