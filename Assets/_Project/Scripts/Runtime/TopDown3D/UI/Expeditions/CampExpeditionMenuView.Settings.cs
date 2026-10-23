using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        private sealed class SettingsColumn
        {public string panel,paneKey;public RectTransform root;public ScrollRect scroll;public readonly List<RectTransform> entries=new();}
        private readonly Dictionary<string,GameObject> settingsPanels=new(StringComparer.Ordinal);
        private readonly Dictionary<string,Text> settingsLabels=new(StringComparer.Ordinal);
        private readonly Dictionary<string,GameObject> settingsObjects=new(StringComparer.Ordinal);
        private readonly Dictionary<string,bool> settingsEnabled=new(StringComparer.Ordinal);
        private readonly Dictionary<Selectable,bool> beforeSettings=new();
        private readonly Dictionary<ScrollRect,bool> beforeSettingsScroll=new();
        private readonly List<SettingsColumn> settingsColumns=new();
        private readonly List<GameObject> settingsAdvanced=new();
        private readonly Dictionary<string,GameObject> settingsChecks=new();
        private readonly Dictionary<string,string> settingsPanelFocus=new(),settingsBodyFocus=new();
        private SettingsColumn settingsScrollColumn;
        private Text settingsRevealedNotice;
        internal Text RevealedSettingsNotice=>SettingsPresentationOpen&&settingsRevealedNotice!=null&&settingsRevealedNotice.gameObject.activeInHierarchy&&events?.currentSelectedGameObject==settingsRevealedNotice.GetComponentInParent<ScrollRect>()?.gameObject?settingsRevealedNotice:null;
        private Action<string> settingsCommand,settingsRename;
        private Action settingsBack;
        private InputField settingsPageName;
        private RectTransform settingsSlotGrid,settingsFooter;
        private GameObject settingsDialog;
        private Text settingsHeading,settingsIntroduction,settingsRadialSummary;
        private string settingsPanel,settingsFocus,settingsFooterApply,settingsFooterBack;
        private int settingsSelectedSector,settingsSlotCount=4;
        private bool settingsReady,settingsScrollAwaitNeutral;
        internal bool SettingsPresentationOpen=>settingsPanel!=null;
        internal string SettingsPanel=>settingsPanel;

        internal void BindSettingsPresentation(Action<string> command,Action<string> rename,Action back)
        {
            settingsCommand=command;settingsRename=rename;settingsBack=back;
            if(settingsPanels.Count>0)return;
            var root=ResourceRoot("settings","Make it yours.","Adjust the interface, arrange your shortcuts, and return to the expedition.");
            settingsHeading=root.GetComponentsInChildren<Text>(true).Single(t=>t.text=="Make it yours.");settingsIntroduction=root.GetComponentsInChildren<Text>(true).Single(t=>t.text.StartsWith("Adjust the interface",StringComparison.Ordinal));
            foreach(string panel in new[]{"Settings","Controls","Setup","Camera"})
            {var body=new GameObject(panel+" presentation",typeof(RectTransform)).GetComponent<RectTransform>();body.SetParent(root,false);body.anchorMin=Vector2.zero;body.anchorMax=Vector2.one;body.offsetMin=new Vector2(0,76);body.offsetMax=new Vector2(0,-110);settingsPanels.Add(panel,body.gameObject);}
            BuildSettingsHome();BuildControlSettings();BuildRadialSettings();BuildCameraSettings();BuildSettingsDialog(root);
            settingsFooter=new GameObject("Settings actions",typeof(RectTransform)).GetComponent<RectTransform>();settingsFooter.SetParent(root,false);settingsFooter.anchorMin=Vector2.zero;settingsFooter.anchorMax=new Vector2(1,0);settingsFooter.pivot=Vector2.zero;settingsFooter.offsetMin=Vector2.zero;settingsFooter.offsetMax=new Vector2(-12,62);
            foreach(var pair in new[]{("SettingsApply","Apply settings"),("SettingsBack","Back"),("ControlsApply","Apply controls"),("ControlsBack","Back"),("SetupApply","Apply shortcuts"),("SetupBack","Back"),("CameraApply","Apply camera"),("CameraBack","Back")})SettingsButton(settingsFooter,pair.Item1,pair.Item2);
            LinkMenuDirection(controls["resource-back:settings"].gameObject,data=>{if(settingsReady&&data.moveDir is MoveDirection.Down or MoveDirection.Right)FocusSettingsFirst();data.Use();});
            foreach(var panel in settingsPanels.Values)panel.SetActive(false);settingsDialog.SetActive(false);SetSettingsAdvanced(false);
        }
        private SettingsColumn SettingsPane(string panel,string title,float left,float right)
        {
            var root=ProjectPanel(settingsPanels[panel].transform,title,left,right);root.offsetMin=Vector2.zero;root.offsetMax=new Vector2(-12,0);
            string paneKey="pane:"+settingsColumns.Count;var scroll=CreateMenuScroll(root,title+" scroll","settings:"+paneKey);scroll.viewport.offsetMin=new Vector2(18,18);scroll.viewport.offsetMax=new Vector2(-18,-18);settingsObjects.Add(paneKey,scroll.gameObject);settingsEnabled.Add(paneKey,true);
            var column=new SettingsColumn{panel=panel,paneKey=paneKey,root=root,scroll=scroll};settingsColumns.Add(column);LinkMenuDirection(scroll.gameObject,data=>MoveSettingsPane(column,data));return column;
        }
        private Text SettingsLabel(SettingsColumn column,string key,string value,int size=18)
        {var text=CatalogText(column.scroll.content,value,size);text.name=key;text.supportRichText=false;settingsLabels.Add(key,text);settingsObjects.Add(key,text.gameObject);column.entries.Add(text.rectTransform);return text;}
        private CampMenuControl SettingsButton(Transform parent,string key,string label)
        {
            var control=ProjectButton(parent,"settings:"+key,label);control.title.supportRichText=false;settingsObjects.Add(key,control.gameObject);settingsEnabled.Add(key,true);
            LinkMenuDirection(control.gameObject,data=>MoveSettingsControl(key,data));return control;
        }
        private CampMenuControl SettingsButton(SettingsColumn column,string key,string label)
        {var control=SettingsButton(column.scroll.content,key,label);column.entries.Add((RectTransform)control.transform);return control;}
        private void SettingsToggle(SettingsColumn column,string key,string label)
        {
            var button=SettingsButton(column,key,label);button.title.rectTransform.offsetMin=new Vector2(48,4);
            var frame=new GameObject("Checkbox",typeof(RectTransform),typeof(CampMenuPanelGraphic)).GetComponent<RectTransform>();frame.SetParent(button.transform,false);frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(0,.5f);frame.anchoredPosition=new Vector2(12,0);frame.sizeDelta=new Vector2(22,22);frame.GetComponent<CampMenuPanelGraphic>().SetFrame(Color.clear,style.ink,1);
            var mark=new GameObject("Checked",typeof(RectTransform)).GetComponent<RectTransform>();mark.SetParent(frame,false);mark.anchorMin=Vector2.zero;mark.anchorMax=Vector2.one;mark.offsetMin=mark.offsetMax=Vector2.zero;settingsChecks[key]=mark.gameObject;
            TickPart(new Vector2(-4.5f,-2.5f),7.1f,45);TickPart(new Vector2(2.5f,.5f),14.2f,-39);
            void TickPart(Vector2 position,float height,float rotation){var line=new GameObject("Check stroke",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();line.SetParent(mark,false);line.anchorMin=line.anchorMax=line.pivot=new Vector2(.5f,.5f);line.anchoredPosition=position;line.sizeDelta=new Vector2(2.5f,height);line.localEulerAngles=new Vector3(0,0,rotation);line.GetComponent<Image>().color=style.ink;line.GetComponent<Image>().raycastTarget=false;}
        }
        private void BuildSettingsHome()
        {
            var read=SettingsPane("Settings","Readability",0,.33f);SettingsLabel(read,"ReadabilityEyebrow","INTERFACE / ACCESSIBILITY",14).color=style.accent;SettingsLabel(read,"ReadabilityTitle","Readability",26);SettingsToggle(read,"TextSize","Larger interface text");SettingsToggle(read,"Motion","Reduce interface motion");SettingsLabel(read,"ReadabilityHelp","Preview your preferences here. Apply saves them; Back protects unapplied changes.");SettingsLabel(read,"SettingsHints","D-pad / left stick: navigate\nSouth: select\nEast: back\nRight stick: scroll",16);
            var radial=SettingsPane("Settings","Radial shortcuts",.34f,.67f);SettingsLabel(radial,"RadialEyebrow","CONTROLS / QUICK ACTIONS",14).color=style.accent;SettingsLabel(radial,"RadialTitle","Radial shortcuts",26);SettingsButton(radial,"RadialSetup","Customize shortcuts");settingsRadialSummary=SettingsLabel(radial,"RadialSummary","",18);SettingsLabel(radial,"RadialHelp","Choose a direction and assign an available action. Shortcut changes have their own Apply button.",16);SettingsLabel(radial,"RadialNotice","",16);
            var system=SettingsPane("Settings","Controls and view",.68f,1);SettingsLabel(system,"SystemEyebrow","CONTROLS / VIEW",14).color=style.accent;SettingsLabel(system,"SystemTitle","Controls and view",26);SettingsButton(system,"Controls","Rebind controls");SettingsButton(system,"CameraOptions","Camera options");SettingsButton(system,"ViewDistance","Maximum view distance");SettingsLabel(system,"SettingsNotice","Changes are saved with Apply.",16);
        }
        private void BuildControlSettings()
        {
            var left=SettingsPane("Controls","Control binding",0,.57f);SettingsLabel(left,"ControlsTitle","Input bindings",26);SettingsButton(left,"ControlDevice","Device");SettingsButton(left,"ControlPrevious","Previous action");SettingsButton(left,"ControlNext","Next action");SettingsLabel(left,"ControlActionName","Action",22);SettingsButton(left,"ControlBinding","Change binding");
            var right=SettingsPane("Controls","Control behavior",.58f,1);SettingsLabel(right,"ControlsHelp","Choose the device and action, then change its binding. Listening ends after ten seconds. Back cancels listening.",18);SettingsButton(right,"ControlHold","Keyboard cargo hold");SettingsButton(right,"ControlReset","Reset controls");SettingsLabel(right,"ControlNotice","",18);
        }
        private void BuildRadialSettings()
        {
            var left=SettingsPane("Setup","Shortcut directions",0,.35f);SettingsLabel(left,"PageNumber","Field",22);SettingsButton(left,"PreviousPage","Previous page");SettingsButton(left,"NextPage","Next page");
            settingsSlotGrid=new GameObject("Shortcut directions",typeof(RectTransform)).GetComponent<RectTransform>();settingsSlotGrid.SetParent(left.scroll.content,false);left.entries.Add(settingsSlotGrid);for(int i=0;i<8;i++)SettingsButton(settingsSlotGrid,"Slot"+i,"");
            var choices=SettingsPane("Setup","Shortcut assignments",.36f,.68f);SettingsLabel(choices,"SelectedDirection","Selected direction",22);for(int i=0;i<5;i++)SettingsButton(choices,"Assign"+i,i==0?"Clear slot":TopDown3DRadialCanvas.Name((TopDown3DRadialCommand)i));SettingsLabel(choices,"SetupNotice","",16);
            var advanced=SettingsPane("Setup","Shortcut page options",.69f,1);SettingsButton(advanced,"More","More options");SettingsLabel(advanced,"SetupHelp","Manage pages, directions and wheel behavior.",16);int first=advanced.entries.Count;
            SettingsLabel(advanced,"NameLabel","Page name",16);settingsPageName=ResourceSearch(advanced.scroll.content,"Shortcut page name","Page name");settingsPageName.characterLimit=24;settingsPageName.onEndEdit.AddListener(value=>settingsRename?.Invoke(value));advanced.entries.Add((RectTransform)settingsPageName.transform);settingsObjects.Add("PageName",settingsPageName.gameObject);LinkMenuDirection(settingsPageName.gameObject,data=>{if(!settingsPageName.isFocused)MoveSettingsControl("PageName",data);});
            foreach(var pair in new[]{("AddPage","Add page"),("DeletePage","Delete page"),("DefaultPage","Make default page"),("SectorCount","4 directions"),("Style","Hold + Stick Release"),("WheelScale","Wheel scale")})SettingsButton(advanced,pair.Item1,pair.Item2);
            settingsAdvanced.AddRange(advanced.entries.Skip(first).Select(rect=>rect.gameObject));
        }
        private void BuildCameraSettings()
        {
            var left=SettingsPane("Camera","Camera view",0,.49f);SettingsLabel(left,"CameraTitle","Camera preferences",26);foreach(string key in new[]{"CameraView","CameraRecenter","CameraInvert"})SettingsButton(left,key,key);
            var right=SettingsPane("Camera","Camera response",.50f,1);foreach(string key in new[]{"CameraSensX","CameraSensY","CameraFov"})SettingsButton(right,key,key);SettingsLabel(right,"CameraNotice","",18);
        }
        private void BuildSettingsDialog(RectTransform parent)
        {
            settingsDialog=new GameObject("Settings confirmation",typeof(RectTransform),typeof(Image));settingsDialog.transform.SetParent(parent,false);var overlay=(RectTransform)settingsDialog.transform;overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;settingsDialog.GetComponent<Image>().color=new Color(0,0,0,.75f);
            var box=ProjectPanel(overlay,"Confirmation",.18f,.82f);box.anchorMin=new Vector2(.18f,.12f);box.anchorMax=new Vector2(.82f,.9f);box.offsetMin=box.offsetMax=Vector2.zero;var opaque=style.surface;opaque.a=1;box.GetComponent<CampMenuPanelGraphic>().SetFrame(opaque,style.line,1);
            var scroll=CreateMenuScroll(box,"Confirmation scroll");scroll.viewport.offsetMin=new Vector2(24,24);scroll.viewport.offsetMax=new Vector2(-24,-24);var column=new SettingsColumn{panel="Dialog",root=box,scroll=scroll};settingsColumns.Add(column);SettingsLabel(column,"DialogTitle","Confirm",28);SettingsLabel(column,"DialogMessage","",20);SettingsButton(column,"DialogCancel","Keep editing");SettingsButton(column,"DialogAccept","Discard");
        }

        void ITopDown3DMenuPresentation.Show(string panel,string focus)
        {
            settingsRevealedNotice=null;
            if(panel==null){settingsPanel=null;settingsReady=false;RestoreSettingsPresentation();settingsDialog.SetActive(false);if(page=="settings")ShowPage("start");return;}
            if(panel=="Root")panel="Settings";
            bool changed=settingsPanel!=panel;settingsPanel=panel;settingsFocus=focus;settingsReady=false;if(changed){settingsScrollAwaitNeutral=true;settingsScrollColumn=settingsColumns.FirstOrDefault(column=>column.panel==panel);}
            if(page!="settings")ShowPage("settings");SetVisible(true);
            if(panel!="Dialog")foreach(var pair in settingsPanels)pair.Value.SetActive(pair.Key==panel);
            settingsDialog.SetActive(panel=="Dialog");settingsDialog.transform.SetAsLastSibling();
            settingsHeading.text=panel=="Controls"?"Make the controls yours.":panel=="Setup"?"Arrange your shortcuts.":panel=="Camera"?"Choose your view.":"Make it yours.";
            settingsIntroduction.text=panel=="Setup"?"Choose a direction, assign an action, and Apply to save your layout.":panel=="Controls"?"Choose a device and action, then assign its button.":panel=="Camera"?"Preview camera preferences. Apply saves your choice.":"Adjust the interface, arrange your shortcuts, and return to the expedition.";
            settingsFooterApply=panel switch{"Controls"=>"ControlsApply","Setup"=>"SetupApply","Camera"=>"CameraApply",_=>"SettingsApply"};settingsFooterBack=panel switch{"Controls"=>"ControlsBack","Setup"=>"SetupBack","Camera"=>"CameraBack",_=>"SettingsBack"};
            foreach(var control in settingsFooter.GetComponentsInChildren<CampMenuControl>(true))control.gameObject.SetActive(panel!="Dialog"&&(control.key=="settings:"+settingsFooterApply||control.key=="settings:"+settingsFooterBack));
            controls["resource-back:settings"].SetContent(panel=="Settings"?"← Start menu":"← Settings");GateSettingsPresentation();ReflowSettingsPresentation();FocusSettings(focus,false);
        }
        void ITopDown3DMenuPresentation.SetInputReady(bool ready){settingsReady=ready;GateSettingsPresentation();if(ready)RepairSettingsFocus();}
        void ITopDown3DMenuPresentation.SetText(string key,string value)
        {
            if(settingsLabels.TryGetValue(key,out var label))label.text=value;
            else if(controls.TryGetValue("settings:"+key,out var control))
            {
                if(key is "TextSize" or "Motion"){bool check=key=="TextSize"?value.EndsWith("Large",StringComparison.Ordinal):value.EndsWith("Reduced",StringComparison.Ordinal);settingsChecks[key].SetActive(check);control.SetContent(key=="TextSize"?"Larger interface text":"Reduce interface motion");}
                else control.SetContent(value);
            }
        }
        void ITopDown3DMenuPresentation.SetButton(string key,bool enabled){if(settingsEnabled.ContainsKey(key))settingsEnabled[key]=enabled;GateSettingsPresentation();}
        void ITopDown3DMenuPresentation.SetActive(string key,bool enabled)
        {if(key=="Advanced")SetSettingsAdvanced(enabled);else if(settingsObjects.TryGetValue(key,out var target))target.SetActive(enabled);}
        private void SetSettingsAdvanced(bool enabled){foreach(var target in settingsAdvanced)target.SetActive(enabled);if(controls.TryGetValue("settings:More",out var more))more.SetContent(enabled?"Fewer options":"More options");}
        void ITopDown3DMenuPresentation.SetPageName(string value){if(!settingsPageName.isFocused)settingsPageName.SetTextWithoutNotify(value);}
        void ITopDown3DMenuPresentation.EndTextEdit(){settingsPageName.DeactivateInputField();FocusSettings("SetupBack");}
        string ITopDown3DMenuPresentation.FocusKey=>events?.currentSelectedGameObject?.GetComponent<CampMenuControl>()?.key is string key&&key.StartsWith("settings:",StringComparison.Ordinal)?key.Substring(9):events?.currentSelectedGameObject==settingsPageName.gameObject?"PageName":settingsPanelFocus.GetValueOrDefault(settingsPanel??"")??settingsFocus;
        bool ITopDown3DMenuPresentation.IsEditingText=>settingsPageName!=null&&settingsPageName.isFocused;
        void ITopDown3DMenuPresentation.ApplySettings(TopDown3DMenuSettings preferences){SetLargeText(preferences.largeText);SetReducedMotion(preferences.reducedMotion);if(SettingsPresentationOpen)GateSettingsPresentation();}
        void ITopDown3DMenuPresentation.DrawSlots(TopDown3DRadialPage radial,int selected)
        {
            settingsSelectedSector=selected;settingsSlotCount=radial.slots.Length;var directions=radial.slots.Length==8?new[]{"N","NE","E","SE","S","SW","W","NW"}:new[]{"N","E","S","W"};
            for(int i=0;i<8;i++){var control=controls["settings:Slot"+i];control.gameObject.SetActive(i<radial.slots.Length);control.SetContent(i>=radial.slots.Length?"":directions[i]+"\n"+TopDown3DRadialCanvas.Name(radial.slots[i]));control.SetSelected(i==selected);}ReflowSettingsPresentation();
        }
        void ITopDown3DMenuPresentation.DrawRadialSummary(TopDown3DRadialPreferences preferences)
        {var radial=preferences.pages[preferences.defaultPage];var directions=radial.slots.Length==8?new[]{"↑","↗","→","↘","↓","↙","←","↖"}:new[]{"↑","→","↓","←"};settingsRadialSummary.text="Default page: "+radial.name+"\n\n"+string.Join("\n",radial.slots.Select((command,index)=>directions[index]+"   "+TopDown3DRadialCanvas.Name(command)));}
        void ITopDown3DMenuPresentation.RefreshNavigation(){ReflowSettingsPresentation();GateSettingsPresentation();RepairSettingsFocus();}
        void ITopDown3DMenuPresentation.RevealNotice(string key)
        {
            if(!SettingsPresentationOpen||!settingsLabels.TryGetValue(key,out var label))return;
            var column=settingsColumns.FirstOrDefault(candidate=>label.transform.IsChildOf(candidate.scroll.content));if(column?.paneKey==null)return;
            settingsScrollAwaitNeutral=true;settingsScrollColumn=column;settingsRevealedNotice=label;ReflowSettingsPresentation();FocusSettings(column.paneKey,false);Canvas.ForceUpdateCanvases();RevealMenuScrollTarget(label.transform);
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(column.scroll.viewport,label.transform);var viewport=column.scroll.viewport.rect;
            if(bounds.size.y<=viewport.height-24){float shift=bounds.min.y<viewport.yMin+12?viewport.yMin+12-bounds.min.y:bounds.max.y>viewport.yMax-12?viewport.yMax-12-bounds.max.y:0;column.scroll.content.anchoredPosition+=new Vector2(0,shift);}
        }

        private bool HandleSettingsActivation(string key)
        {
            if(!SettingsPresentationOpen)return false;
            if(key=="hint:select")return false;
            if(key.StartsWith("settings:pane:",StringComparison.Ordinal))return true;
            if(key is "hint:back" or "page:start" or "resource-back:settings"){settingsBack?.Invoke();return true;}
            if(settingsReady&&key.StartsWith("settings:",StringComparison.Ordinal))settingsCommand?.Invoke(key.Substring(9));
            return true;
        }
        private void GateSettingsPresentation()
        {
            if(!SettingsPresentationOpen)return;
            RefreshSettingsPrompts();
            foreach(var selectable in GetComponentsInChildren<Selectable>(true))
            {
                var control=selectable.GetComponent<CampMenuControl>();string key=control?.key;
                if(key?.StartsWith("settings:",StringComparison.Ordinal)==true)
                    selectable.interactable=settingsReady&&settingsEnabled.GetValueOrDefault(key.Substring(9))&&(settingsPanel!="Dialog"||selectable.transform.IsChildOf(settingsDialog.transform));
                else if(selectable==settingsPageName)selectable.interactable=settingsReady&&settingsPanel=="Setup";
                else
                {
                    if(!beforeSettings.ContainsKey(selectable))beforeSettings[selectable]=selectable.interactable;
                    selectable.interactable=beforeSettings[selectable]&&(key is "hint:select" or "hint:back" or "page:start"||key=="resource-back:settings"&&settingsPanel!="Dialog");
                }
                control?.RefreshAvailability();
            }
            foreach(var scroll in GetComponentsInChildren<ScrollRect>(true))
            {if(!beforeSettingsScroll.ContainsKey(scroll))beforeSettingsScroll[scroll]=scroll.enabled;scroll.enabled=beforeSettingsScroll[scroll]&&scroll.transform.IsChildOf(roots["settings"].transform)&&(settingsPanel!="Dialog"||scroll.transform.IsChildOf(settingsDialog.transform));}
        }
        private void RefreshSettingsPrompts()
        {
            if(!SettingsPresentationOpen)return;
            foreach(var key in new[]{"page:start","page:inventory","hint:tabs","hint:subtabs","hint:west","hint:pan"})if(controls.TryGetValue(key,out var control))control.gameObject.SetActive(false);
            foreach(var key in new[]{"hint:select","hint:navigate","hint:scroll"})if(controls.TryGetValue(key,out var control))control.gameObject.SetActive(settingsReady);
        }
        private void RestoreSettingsPresentation()
        {foreach(var pair in beforeSettings)if(pair.Key!=null)pair.Key.interactable=pair.Value;beforeSettings.Clear();RestoreScrollContext(beforeSettingsScroll);}
        private void FocusSettings(string key,bool requireReady=true)
        {
            if(events==null||requireReady&&!settingsReady)return;
            var target=key=="PageName"?settingsPageName.gameObject:controls.GetValueOrDefault("settings:"+key)?.gameObject;
            if(target!=null&&target.activeInHierarchy&&(!requireReady||target.GetComponent<Selectable>().IsInteractable()))events.SetSelectedGameObject(target);
        }
        private void FocusSettingsFirst()
        {var first=ActiveSettingsSelectables().FirstOrDefault();if(first!=null)events?.SetSelectedGameObject(first.gameObject);}
        private IEnumerable<Selectable> ActiveSettingsSelectables()=>settingsColumns.Where(column=>column.panel==settingsPanel).SelectMany(column=>column.scroll.content.GetComponentsInChildren<Selectable>()).Where(selectable=>selectable.gameObject.activeInHierarchy&&selectable.IsInteractable());
        private void RepairSettingsFocus()
        {
            if(!SettingsPresentationOpen||!settingsReady||events==null)return;var selected=events.currentSelectedGameObject?.GetComponent<Selectable>();
            if(selected!=null&&selected.gameObject.activeInHierarchy&&selected.IsInteractable()&&(selected==settingsPageName||selected.GetComponent<CampMenuControl>()?.key.StartsWith("settings:",StringComparison.Ordinal)==true)){RememberSettingsFocus(selected.gameObject);RevealMenuScrollTarget(selected.transform);return;}
            FocusSettings(settingsFocus);selected=events.currentSelectedGameObject?.GetComponent<Selectable>();if(selected==null||!selected.gameObject.activeInHierarchy||!selected.IsInteractable())FocusSettingsFirst();
        }
        internal void AdvanceSettingsScroll(Vector2 axis,float seconds)
        {
            if(axis.sqrMagnitude<.04f)settingsScrollAwaitNeutral=false;if(!SettingsPresentationOpen||!settingsReady||settingsScrollAwaitNeutral||IsEditingText)return;
            var scroll=events?.currentSelectedGameObject?.GetComponentInParent<ScrollRect>();if(scroll==null||!scroll.transform.IsChildOf(roots["settings"].transform)||!scroll.gameObject.activeInHierarchy)scroll=settingsScrollColumn?.scroll;
            if(scroll!=null&&scroll.gameObject.activeInHierarchy)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+axis.y*seconds);
        }
        private void RememberSettingsFocus(GameObject target)
        {
            if(!SettingsPresentationOpen||!settingsReady||target==null)return;
            string key=target==settingsPageName.gameObject?"PageName":target.GetComponent<CampMenuControl>()?.key;if(key?.StartsWith("settings:",StringComparison.Ordinal)==true)key=key.Substring(9);else if(key!="PageName")return;
            settingsPanelFocus[settingsPanel]=key;var column=settingsColumns.FirstOrDefault(candidate=>target.transform.IsChildOf(candidate.scroll.transform));if(column!=null){settingsScrollColumn=column;settingsBodyFocus[settingsPanel]=key;}
        }
        private void MoveSettingsPane(SettingsColumn column,AxisEventData data)
        {
            if(!settingsReady){data.Use();return;}
            if(data.moveDir is MoveDirection.Up or MoveDirection.Down)
            {if(data.moveDir==MoveDirection.Down&&column.scroll.verticalNormalizedPosition<=.001f)FocusSettings(settingsEnabled.GetValueOrDefault(settingsFooterApply)?settingsFooterApply:settingsFooterBack);else ScrollInspector(data.moveDir==MoveDirection.Up?1:-1,.18f);}
            else {var columns=settingsColumns.Where(candidate=>candidate.panel==settingsPanel).ToArray();int index=Array.IndexOf(columns,column)+(data.moveDir==MoveDirection.Right?1:-1);if(index>=0&&index<columns.Length){var next=columns[index].scroll.content.GetComponentsInChildren<Selectable>().FirstOrDefault(selectable=>selectable.IsInteractable());if(next!=null)events?.SetSelectedGameObject(next.gameObject);}else FocusSettings(settingsFooterBack);}
            data.Use();
        }
        private void MoveSettingsControl(string key,AxisEventData data)
        {
            if(!settingsReady){data.Use();return;}
            if(key.StartsWith("Slot",StringComparison.Ordinal))
            {
                int index=int.Parse(key.Substring(4));if(data.moveDir==MoveDirection.Right)FocusSettings(index%2==0&&index+1<settingsSlotCount?"Slot"+(index+1):"Assign0");else if(data.moveDir==MoveDirection.Left)FocusSettings(index%2==1?"Slot"+(index-1):"PreviousPage");else if(data.moveDir==MoveDirection.Up)FocusSettings(index>=2?"Slot"+(index-2):"NextPage");else if(data.moveDir==MoveDirection.Down)FocusSettings(index+2<settingsSlotCount?"Slot"+(index+2):settingsFooterApply);data.Use();return;
            }
            if(key==settingsFooterApply||key==settingsFooterBack)
            {if(data.moveDir==MoveDirection.Up){if(settingsBodyFocus.TryGetValue(settingsPanel,out var previous))FocusSettings(previous);else FocusSettingsFirst();}else if(data.moveDir==MoveDirection.Left)FocusSettings(settingsFooterApply);else if(data.moveDir==MoveDirection.Right)FocusSettings(settingsFooterBack);data.Use();return;}
            if(key.StartsWith("Assign",StringComparison.Ordinal)&&data.moveDir==MoveDirection.Left){FocusSettings("Slot"+settingsSelectedSector);data.Use();return;}
            var target=key=="PageName"?settingsPageName.gameObject:settingsObjects.GetValueOrDefault(key);var column=settingsColumns.FirstOrDefault(c=>target!=null&&target.transform.IsChildOf(c.scroll.content));if(column==null){data.Use();return;}
            var entries=column.scroll.content.GetComponentsInChildren<Selectable>().Where(s=>s.IsInteractable()&&s.gameObject.activeInHierarchy).ToArray();int at=Array.FindIndex(entries,s=>s.gameObject==target);
            if(data.moveDir is MoveDirection.Up or MoveDirection.Down)
            {int next=at+(data.moveDir==MoveDirection.Down?1:-1);if(next>=0&&next<entries.Length)events?.SetSelectedGameObject(entries[next].gameObject);else if(data.moveDir==MoveDirection.Down)FocusSettings(settingsFooterApply);else if(controls["resource-back:settings"].Button.IsInteractable())events?.SetSelectedGameObject(controls["resource-back:settings"].gameObject);}
            else
            {var columns=settingsColumns.Where(c=>c.panel==settingsPanel).ToArray();int next=Array.IndexOf(columns,column)+(data.moveDir==MoveDirection.Right?1:-1);if(next>=0&&next<columns.Length){var neighbor=columns[next].scroll.content.GetComponentsInChildren<Selectable>().FirstOrDefault(s=>s.IsInteractable()&&s.gameObject.activeInHierarchy);if(neighbor!=null)events?.SetSelectedGameObject(neighbor.gameObject);}}
            data.Use();
        }
        private void ReflowSettingsPresentation()
        {
            if(settingsPanels.Count==0)return;ApplyTypography();
            RefreshSettingsPrompts();
            foreach(var column in settingsColumns)
            {
                float width=column.scroll.viewport.rect.width,y=0;
                foreach(var entry in column.entries)
                {
                    if(!entry.gameObject.activeSelf)continue;float height;
                    if(entry==settingsSlotGrid)
                    {
                        float cellWidth=(width-10)/2,cellHeight=0;for(int i=0;i<settingsSlotCount;i++)cellHeight=Mathf.Max(cellHeight,ProjectTextHeight(controls["settings:Slot"+i].title,cellWidth-20,60)+16);height=Mathf.Ceil(settingsSlotCount/2f)*(cellHeight+10);
                        for(int i=0;i<settingsSlotCount;i++){var rect=(RectTransform)controls["settings:Slot"+i].transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(i%2*(cellWidth+10),-(i/2)*(cellHeight+10));rect.sizeDelta=new Vector2(cellWidth,cellHeight);}
                    }
                    else if(entry.GetComponent<CampMenuControl>() is CampMenuControl control)height=Mathf.Max(52,ProjectTextHeight(control.title,width-(settingsChecks.ContainsKey(control.key.Substring(9))?58:20))+16);
                    else if(entry.GetComponent<Text>() is Text text)height=ProjectTextHeight(text,width);
                    else height=52;
                    TopRegion(entry,y,height);y+=height+16;
                }
                column.scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,y);
            }
            if(settingsFooter!=null&&settingsFooterApply!=null)
            {float width=settingsFooter.rect.width;foreach(var pair in new[]{(settingsFooterApply,0f,.49f),(settingsFooterBack,.51f,1f)}){var rect=(RectTransform)controls["settings:"+pair.Item1].transform;rect.anchorMin=new Vector2(pair.Item2,0);rect.anchorMax=new Vector2(pair.Item3,1);rect.offsetMin=rect.offsetMax=Vector2.zero;}}
        }
    }
}
