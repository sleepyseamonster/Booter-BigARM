using System;
using UnityEngine.InputSystem;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionSession
    {
        private InputAction nextTab,previousTab,nextSub,previousSub,quickTransfer,menuSubmit;
        private bool playSubmitRequested;
        private InputAction inspectScroll;
        private Vector2 scrollAxis;
        private bool awaitScrollNeutral=true;
        private InputAction treeNavigate,treePan;
        private InputActionReference originalMove,treeMove;
        private InputActionReference suspendedMenuMove;
        private bool waitingNavigationRelease;
        private Vector2 panAxis;
        private Vector2 trackedLeftAxis,trackedRightAxis;
        private bool awaitPanNeutral=true;
        private InputAction treeFitKey,treeZoomInKey,treeZoomOutKey;
        private float treeZoomAxis;
        private bool awaitTreeZoomNeutral=true;
        private void BindMenuCommands()
        {
            var map=input.PrepareExpeditionMenuActions();
            nextSub=map.FindAction("NextExpeditionSubTab",true);
            previousSub=map.FindAction("PreviousExpeditionSubTab",true);
            inspectScroll=map.FindAction("ExpeditionInspectScroll",true);
            treeNavigate=map.FindAction("ExpeditionTreeNavigate",true);
            treePan=map.FindAction("ExpeditionTreePan",true);
            originalMove=module.move;treeMove=InputActionReference.Create(treeNavigate);view.PageChanged+=ChangeNavigation;
            view.InputContextChanged+=MenuContextChanged;
            nextTab=map.FindAction("NextInventory",true);previousTab=map.FindAction("PreviousInventory",true);quickTransfer=map.FindAction("QuickTransfer",true);
            menuSubmit=map.FindAction("Submit",true);menuSubmit.performed+=FocusPlaySubmit;
            InputSystem.onAfterUpdate+=ResolvePlaySubmit;
            treeFitKey=map.FindAction("ExpeditionTreeFit",true);treeZoomInKey=map.FindAction("ExpeditionTreeZoomIn",true);treeZoomOutKey=map.FindAction("ExpeditionTreeZoomOut",true);
            treeFitKey.performed+=TreeShortcut;treeZoomInKey.performed+=TreeShortcut;treeZoomOutKey.performed+=TreeShortcut;
            nextTab.performed+=Tab;previousTab.performed+=Tab;nextSub.performed+=SubTab;previousSub.performed+=SubTab;quickTransfer.performed+=QuickTransfer;
            inspectScroll.performed+=Scroll;inspectScroll.canceled+=Scroll;
            treePan.performed+=Pan;treePan.canceled+=Pan;
            view.BindInputRouter(input);
            view.SetTreeInputGate(()=>MenuCommandAllowed);
        }
        private void UnbindMenuCommands(bool restoreShared=true)
        {
            if(restoreShared)RestoreMenuNavigation();
            InputSystem.onAfterUpdate-=ResolvePlaySubmit;playSubmitRequested=false;
            if(restoreShared&&view!=null)view.BindInputRouter(null);
            if(restoreShared&&view!=null)view.SetTreeInputGate(null);
            if(treeFitKey!=null)treeFitKey.performed-=TreeShortcut;if(treeZoomInKey!=null)treeZoomInKey.performed-=TreeShortcut;if(treeZoomOutKey!=null)treeZoomOutKey.performed-=TreeShortcut;
            if(menuSubmit!=null)menuSubmit.performed-=FocusPlaySubmit;
            if(nextTab!=null)nextTab.performed-=Tab;if(previousTab!=null)previousTab.performed-=Tab;if(nextSub!=null)nextSub.performed-=SubTab;if(previousSub!=null)previousSub.performed-=SubTab;if(quickTransfer!=null)quickTransfer.performed-=QuickTransfer;
            if(inspectScroll!=null){inspectScroll.performed-=Scroll;inspectScroll.canceled-=Scroll;}
            if(treePan!=null){treePan.performed-=Pan;treePan.canceled-=Pan;}
            if(view!=null){view.PageChanged-=ChangeNavigation;view.InputContextChanged-=MenuContextChanged;}if(restoreShared&&module!=null&&module.move==treeMove)module.move=originalMove;
            if(treeMove!=null&&(restoreShared||module==null||module.move!=treeMove)){if(Application.isPlaying)Destroy(treeMove);else DestroyImmediate(treeMove);}treeMove=null;
        }
        private bool MenuCommandAllowed=>menuOpen&&!waitingRelease&&!settings.IsOpen&&pending==null&&!view.StorageChoiceOpen&&!view.IsEditingText;
        private void MenuContextChanged()
        {
            if(!menuOpen||input==null||module==null||settings?.IsOpen==true)return;
            ArmRelease();
            if(!waitingNavigationRelease){suspendedMenuMove=module.move;waitingNavigationRelease=true;}
            module.move=null;suspendedMenuMove?.action?.Enable();
        }
        private void RestoreMenuNavigation()
        {
            if(!waitingNavigationRelease)return;
            // A different controller may have taken ownership while this context closed.
            if(module!=null&&module.move==null)module.move=suspendedMenuMove;
            suspendedMenuMove=null;waitingNavigationRelease=false;
        }
        private bool MenuNavigationNeutral()
        {
            if(MenuNavigationActionHeld(originalMove?.action)||MenuNavigationActionHeld(treeNavigate))return false;
            foreach(var pad in Gamepad.all)if(MenuNavigationDevice(pad)&&(pad.dpad.ReadValue().sqrMagnitude>.04f||pad.leftStick.ReadValue().sqrMagnitude>.04f||pad.rightStick.ReadValue().sqrMagnitude>.04f))return false;
            return true;
        }
        private bool MenuNavigationActionHeld(InputAction action)
        {
            if(action==null)return false;
            if(action.ReadValue<Vector2>().sqrMagnitude>.04f)return true;
            // Removing a module reference can reset action state; bound physical controls
            // still cover keyboard composites, D-pads, sticks and explicitly paired devices.
            foreach(var control in action.controls)
            {
                if(!MenuNavigationDevice(control.device))continue;
                if(control is UnityEngine.InputSystem.Controls.ButtonControl button&&button.isPressed)return true;
                if(control is UnityEngine.InputSystem.Controls.Vector2Control axis&&axis.ReadValue().sqrMagnitude>.04f)return true;
            }
            return false;
        }
        private bool MenuNavigationDevice(InputDevice device)
        {var devices=input.InputActions.devices;if(!devices.HasValue)return true;foreach(var allowed in devices.Value)if(ReferenceEquals(allowed,device))return true;return false;}
        private void FocusPlaySubmit(InputAction.CallbackContext context)
        {
            if(!MenuCommandAllowed)return;
            view.FocusPlayAction();view.FocusReviewAction();view.FocusTreeAction();
            if(view.Page is "play" or "review")playSubmitRequested=true;
        }
        private void ResolvePlaySubmit()
        {
            if(!playSubmitRequested)return;playSubmitRequested=false;
            // Run startup changes action maps. Dispatch only after every input callback,
            // including Cancel and tab changes, has finished.
            if(MenuCommandAllowed){if(view.Page=="play")view.SubmitPlayAction();else if(view.Page=="review")view.SubmitReviewAction();}
        }
        private void TreeShortcut(InputAction.CallbackContext context)
        {if(MenuCommandAllowed)view.TreeShortcut(context.action.name);}
        private bool MenuSubmissionHeld()
        {
            // Removing the module reference can reset Button action state. Its bound
            // physical controls still tell us whether the opening press was released.
            foreach(var path in new[]{"UI/Submit","UI/Click"})
            {
                var action=input.InputActions.FindAction(path,true);
                if(action.IsPressed())return true;
                foreach(var control in action.controls)
                    if(control is UnityEngine.InputSystem.Controls.ButtonControl button&&button.isPressed)return true;
            }
            return false;
        }
        private void Tab(InputAction.CallbackContext context)
            =>CyclePrimary(context.action==nextTab?1:-1);
        private void CyclePrimary(int direction)
        {
            if(!MenuCommandAllowed||snapshot.phase==CampExpeditionPhase.CargoReview||view.Page=="review"||view.PickerTarget!=null)return;var tabs=new[]{"play",view.OutpostPage,"workstations"};int index=view.Page=="play"?0:view.Page is "storage" or "jobs"?1:2;
            for(int i=0;i<3;i++){index=(index+direction+3)%3;if(index!=1||snapshot.phase!=CampExpeditionPhase.Exploring){view.ShowPage(tabs[index]);return;}}
        }
        private void SubTab(InputAction.CallbackContext context)
            =>CycleContext(context.action==nextSub?1:-1);
        private void CycleContext(int direction)
        {
            if(!MenuCommandAllowed||view.PickerTarget!=null||view.Page is "review" or "play" or "start" or "guide" or "almanac" or "map" or "settings")return;
            if(view.Page=="inventory"){view.ShowInventory(view.Owner=="booter"?"legger":"booter");return;}
            var tabs=view.Page is "storage" or "jobs"?new[]{"storage","jobs"}:new[]{"workstations","tree","crafting-equipment","tracked","legger-equipment","projects"};
            int index=Array.IndexOf(tabs,view.Page);view.ShowPage(tabs[(Math.Max(0,index)+direction+tabs.Length)%tabs.Length]);
        }
        private void QuickTransfer(InputAction.CallbackContext context)
            =>PerformQuickTransfer();
        private void PerformQuickTransfer()
        {
            if(!MenuCommandAllowed||view.PickerTarget!=null)return;
            view.RefreshFocusedDetails();
            if(view.Page=="tree"){Command("track-center");return;}
            if(view.Page=="tracked"){if(view.TrackedCanUntrack)Command("track");return;}
            if(view.Page is not ("inventory" or "storage" or "review" or "crafting-equipment"))return;
            var selected=view.SelectedStack;if(selected==null)return;
            if(selected.owner=="booter"&&!string.IsNullOrEmpty(selected.equipmentAddress)&&(view.Page is "inventory" or "crafting-equipment"))
            {
                try
                {
                    if(snapshot.phase==CampExpeditionPhase.Exploring&&!TopDown3DCargoAccess.CanAccess(cargo,kit.transform))throw new InvalidOperationException("Bring Legger closer to unequip gear.");
                    var row=ParseCurrent(selected);Apply(ledger.Prepare("equipment.unequip",(_,drafts)=>{var result=TopDown3DInventoryTransferService.TransferSlot(drafts[0],drafts[1],row.slot);if(!result.Succeeded)throw new InvalidOperationException(result.Message);}));
                }
                catch(Exception e){message=e.Message;Render();}
            }
            else Command("transfer");
        }
        private void Scroll(InputAction.CallbackContext context)
        {scrollAxis=context.ReadValue<Vector2>();var raw=context.control is UnityEngine.InputSystem.Controls.StickControl stick?stick.ReadUnprocessedValue():scrollAxis;trackedRightAxis=raw;treeZoomAxis=CampTreeCameraMath.Axis(raw.y);if(scrollAxis.sqrMagnitude<.04f)awaitScrollNeutral=false;if(treeZoomAxis==0)awaitTreeZoomNeutral=false;}
        private void Pan(InputAction.CallbackContext context)
        {var raw=context.control is UnityEngine.InputSystem.Controls.StickControl stick?stick.ReadUnprocessedValue():context.ReadValue<Vector2>();trackedLeftAxis=raw;panAxis=new Vector2(CampTreeCameraMath.Axis(raw.x),CampTreeCameraMath.Axis(raw.y));if(panAxis==Vector2.zero)awaitPanNeutral=false;}
        private void ChangeNavigation(string page){if(module==null)return;var next=page is "tree" or "tracked"?treeMove:originalMove;if(waitingNavigationRelease)suspendedMenuMove=next;else module.move=next;view.ResetTrackedInput();awaitPanNeutral=awaitScrollNeutral=awaitTreeZoomNeutral=true;if(menuOpen&&MenuSubmissionHeld())ArmRelease();}
        private void AdvanceMenuScroll()
        {
            if(scrollAxis.sqrMagnitude<.04f)awaitScrollNeutral=false;if(panAxis==Vector2.zero)awaitPanNeutral=false;if(treeZoomAxis==0)awaitTreeZoomNeutral=false;
            if(view.Page=="tree"&&pending!=null)awaitPanNeutral=awaitTreeZoomNeutral=true;
            if(!menuOpen)return;float seconds=Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(settings.IsOpen){if(settings.UsesExpeditionPresentation&&!settings.IsRebindingControls)view.AdvanceSettingsScroll(scrollAxis,seconds);return;}
            if(waitingRelease)return;
            if(view.Page=="tracked")
            {if(MenuCommandAllowed&&view.PickerTarget==null)view.AdvanceTrackedInput(awaitPanNeutral?Vector2.zero:trackedLeftAxis,awaitScrollNeutral?Vector2.zero:trackedRightAxis,seconds);else{view.ResetTrackedInput();if(!awaitScrollNeutral)view.ScrollInspector(scrollAxis.y,seconds);}return;}
            if(view.Page=="projects"&&MenuCommandAllowed){if(!awaitScrollNeutral)view.ScrollProjects(scrollAxis.y,seconds);return;}
            if(view.Page is "guide" or "almanac" or "map"&&MenuCommandAllowed){if(!awaitScrollNeutral)view.ScrollResource(scrollAxis,seconds);return;}
            if(view.Page=="tree"&&MenuCommandAllowed)view.MoveTree(awaitPanNeutral?Vector2.zero:panAxis,awaitTreeZoomNeutral?0:treeZoomAxis,seconds);
            else if(!awaitScrollNeutral)view.ScrollInspector(scrollAxis.y,seconds);
        }
    }

    public sealed partial class TopDown3DInputRouter
    {
        internal InputActionMap PrepareExpeditionMenuActions()
        {
            return EditExpeditionMenuActions(map=>
            {
                if(map.FindAction("NextExpeditionSubTab")==null)map.AddAction("NextExpeditionSubTab",InputActionType.Button,"<Gamepad>/rightTrigger",groups:"Gamepad");
                if(map.FindAction("PreviousExpeditionSubTab")==null)map.AddAction("PreviousExpeditionSubTab",InputActionType.Button,"<Gamepad>/leftTrigger",groups:"Gamepad");
                if(map.FindAction("ExpeditionInspectScroll")==null)map.AddAction("ExpeditionInspectScroll",InputActionType.Value,"<Gamepad>/rightStick",groups:"Gamepad",expectedControlLayout:"Vector2");
                if(map.FindAction("ExpeditionTreeNavigate")==null)
                {
                    var navigation=map.AddAction("ExpeditionTreeNavigate",InputActionType.Value,"<Gamepad>/dpad",groups:"Gamepad",expectedControlLayout:"Vector2");
                    navigation.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow","Keyboard&Mouse").With("Down","<Keyboard>/downArrow","Keyboard&Mouse").With("Left","<Keyboard>/leftArrow","Keyboard&Mouse").With("Right","<Keyboard>/rightArrow","Keyboard&Mouse");
                }
                if(map.FindAction("ExpeditionTreePan")==null)map.AddAction("ExpeditionTreePan",InputActionType.Value,"<Gamepad>/leftStick",groups:"Gamepad",expectedControlLayout:"Vector2");
                if(map.FindAction("ExpeditionTreeFit")==null)map.AddAction("ExpeditionTreeFit",InputActionType.Button,"<Keyboard>/home",groups:"Keyboard&Mouse");
                if(map.FindAction("ExpeditionTreeZoomIn")==null){var action=map.AddAction("ExpeditionTreeZoomIn",InputActionType.Button,"<Keyboard>/equals",groups:"Keyboard&Mouse");action.AddBinding("<Keyboard>/numpadPlus",groups:"Keyboard&Mouse");}
                if(map.FindAction("ExpeditionTreeZoomOut")==null){var action=map.AddAction("ExpeditionTreeZoomOut",InputActionType.Button,"<Keyboard>/minus",groups:"Keyboard&Mouse");action.AddBinding("<Keyboard>/numpadMinus",groups:"Keyboard&Mouse");}
            });
        }

        // Input System shares binding state across the asset. Disabling only UI is insufficient
        // while Gameplay or System actions remain enabled. Keep each prior action's state rather
        // than enabling whole maps, which would also enable previously inactive actions.
        internal InputActionMap EditExpeditionMenuActions(Action<InputActionMap> edit)
        {
            if(ownedAnimationActions==null||!ReferenceEquals(inputActions,ownedAnimationActions)
                ||!AnimationPlaygroundInputFactory.IsLabAsset(inputActions))
                throw new InvalidOperationException("Expedition UI requires the existing owned runtime input asset; authored bindings were preserved.");
            if(edit==null)throw new ArgumentNullException(nameof(edit));
            var map=inputActions.FindActionMap("UI",true);
            var enabled=new System.Collections.Generic.List<InputAction>();
            foreach(var actionMap in inputActions.actionMaps)
                foreach(var action in actionMap.actions)if(action.enabled)enabled.Add(action);
            try
            {
                inputActions.Disable();
                edit(map);
                return map;
            }
            finally
            {
                inputActions.Disable();
                foreach(var action in enabled)action.Enable();
            }
        }
    }
}
