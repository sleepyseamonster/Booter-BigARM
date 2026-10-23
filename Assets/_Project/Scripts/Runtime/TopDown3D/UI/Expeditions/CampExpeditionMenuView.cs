using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [Serializable] public sealed class CampMenuStackView
    {
        public string key, itemId, owner, equipmentAddress;
        public string instanceId;
        public string contentsSummary;
        public int slotIndex=-1;
        public long storeRevision;
        public int quantity;
    }

    /// <summary>Read model supplied by the session; this is never a second inventory authority.</summary>
    public sealed class CampMenuPresentation
    {
        public CampMenuStackView[] stacks = Array.Empty<CampMenuStackView>();
        public string[] tracked = Array.Empty<string>();
        public string[] discovered = Array.Empty<string>();
        public CampMenuPlanningView planning=new();
        public int storageCapacity = 24;
        public int leggerCapacity=12;
        public float leggerMass,leggerMaximumMass;
        public bool leggerAccessible=true;
        public int attachmentMounts=2,occupiedMounts=1;
        public long commons;
        public bool canPlay,cargoReview;
        public string playLabel = "PLAY", status = "", storageSummary = "";
        public string projectOverview="Gather T1 tool materials.";
        public bool townAccess=true;
        public bool recoveryAvailable;
    }

    [Serializable] public struct CampMenuPageBinding { public string key; public GameObject root; }

    /// <summary>Authored browser-derived presentation. Commands are delegated to the single session/controller.</summary>
    public sealed partial class CampExpeditionMenuView : MonoBehaviour, ICancelHandler,ITopDown3DMenuPresentation
    {
        public CampExpeditionMenuStyle style;
        public CampExpeditionDefinition definition;
        public CampMenuPageBinding[] pages = Array.Empty<CampMenuPageBinding>();
        public RectTransform safeArea, content, footer, inventoryGrid, inventoryAttachments;
        public Text inspectorTitle, inspectorBody, inspectorCount, projectBody, balance, status;
        public Text quoteBody;
        public Text quoteSummary;
        public RectTransform quoteViewport;
        public Text projectChecklist;
        public Text cargoSummary,stationTitle,stationBody;
        public RectTransform cargoGrid,storageToolbar,inspectorBodyViewport;
        public InputField storageSearch;
        private string storageCategory="";
        private bool sortByQuantity;
        public Image inspectorArt;
        private readonly Dictionary<string, CampMenuControl> controls = new(StringComparer.Ordinal);
        private readonly Dictionary<string, GameObject> roots = new(StringComparer.Ordinal);
        private readonly Dictionary<Text, int> fontSizes = new();
        private readonly Dictionary<string,string> pageFocus=new(StringComparer.Ordinal);
        private CampMenuPresentation presentation = new();
        private CampMenuCatalog menuCatalog;
        private EventSystem events;
        private string page = "play", owner = "booter", inspected, pickerTarget;
        private string selectedTree;
        private string[] visibleBookmarks=Array.Empty<string>();
        private bool initialized, layingOut, large;
        private bool quoting;
        private int lastPlaySubmitFrame=-1,lastReviewSubmitFrame=-1;
        private readonly Dictionary<Selectable,bool> beforeQuote=new();
        private CampMenuStackView selectedStack;
        public event Action<string> Command;
        public event Action<string> PageChanged;
        public event Action InputContextChanged;
        public string Page => page;
        public string Owner => owner;
        public string InspectedItem => inspected;
        public CampMenuStackView SelectedStack=>selectedStack;
        public string PickerTarget=>pickerTarget;
        internal bool IsEditingText=>events?.currentSelectedGameObject?.GetComponent<InputField>() is InputField field&&field.transform.IsChildOf(transform)&&field.isFocused;

        public void Initialize(EventSystem eventSystem = null,CampMenuCatalog displayCatalog=null)
        {
            if (initialized) { events = eventSystem ?? events;TreeCanvas?.SetEventSystem(events); return; }
            string reason = null;
            if (style == null || definition == null || !definition.ValidatePresentation(out reason))
                throw new InvalidOperationException("Camp menu requires qualified presentation assets. " + (definition == null ? "" : reason));
            events = eventSystem;
            selectedTree=definition.rootId;
            menuCatalog=displayCatalog??CampMenuCatalog.Load();
            foreach (var binding in pages) roots.Add(binding.key, binding.root);
            foreach (var control in GetComponentsInChildren<CampMenuControl>(true))
            {
                if (!controls.TryAdd(control.key, control)) throw new InvalidOperationException("Duplicate menu control: " + control.key);
                control.Wire(); control.Activated += Activate; control.Browsed += Browse;
                // Bumpers/triggers own tab navigation; keep tabs clickable but outside directional focus.
                if(control.tab)control.Button.navigation=new Navigation{mode=Navigation.Mode.None};
                if(control.prompt)
                {
                    control.Button.navigation=new Navigation{mode=Navigation.Mode.None};
                    control.Button.interactable=control.key is "page:start" or "page:inventory" or "hint:select" or "hint:west" or "hint:back" or "hint:tabs" or "hint:subtabs";
                }
            }
            if(controls.TryGetValue("page:inventory",out var inventoryPrompt))ConfigureInventoryPrompt(inventoryPrompt,style);
            // Play is the page's submit action. Shoulders and pointer clicks still switch tabs.
            if(controls.TryGetValue("play",out var playAction))playAction.Button.navigation=new Navigation{mode=Navigation.Mode.None};
            ConfigurePickerViewport();
            ConfigureEquipmentScroll();
            ConfigureGarmentPreviews();
            ConfigureStorageChoices();
            ConfigureInspectorTitle();
            ConfigureTreeCanvas();
            ConfigureTrackedBrowser();
            ConfigureProjects();
            ConfigureWorkshopPlans();
            ConfigureResources();
            var font=style.ResolveFont();
            foreach (var text in GetComponentsInChildren<Text>(true)){text.font=font;fontSizes[text] = text.fontSize;}
            if(storageSearch!=null)storageSearch.onValueChanged.AddListener(_=>{RefreshInventory();RefreshLayout();EnsureUsableFocus();RefreshFocusedDetails();});
            initialized = true; ShowPage("play"); RefreshLayout();
        }

        // Inventory is bound to North/Y. The frozen browser's View icon belongs to
        // a different runtime action, so replace only this prompt's presentation.
        internal static void ConfigureInventoryPrompt(CampMenuControl control,CampExpeditionMenuStyle menuStyle)
        {
            var oldView=control.transform.Find("Prompt view");if(oldView!=null)oldView.gameObject.SetActive(false);
            var glyph=control.transform.Find("Inventory button glyph") as RectTransform;
            if(glyph==null){glyph=new GameObject("Inventory button glyph",typeof(RectTransform)).GetComponent<RectTransform>();glyph.SetParent(control.transform,false);}
            glyph.anchorMin=glyph.anchorMax=new Vector2(0,.5f);glyph.pivot=new Vector2(0,.5f);glyph.anchoredPosition=Vector2.zero;glyph.sizeDelta=new Vector2(32,32);
            var font=menuStyle.ResolveFont();
            AddGlyphText("Ring","○",32);
            AddGlyphText("North button","Y",16);

            void AddGlyphText(string name,string value,int size)
            {
                var rect=glyph.Find(name) as RectTransform;
                if(rect==null){rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(glyph,false);}
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var text=rect.GetComponent<Text>()??rect.gameObject.AddComponent<Text>();text.text=value;text.font=font;text.fontSize=size;text.color=menuStyle.ink;
                text.alignment=TextAnchor.MiddleCenter;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;
            }
        }

        public void Render(CampMenuPresentation model)
        {
            if (!initialized) Initialize();
            presentation = model ?? throw new ArgumentNullException(nameof(model));
            if(selectedStack!=null)selectedStack=model.stacks.FirstOrDefault(s=>s.key==selectedStack.key&&s.itemId==selectedStack.itemId);
            if (balance != null) balance.text = model.commons + " Commons";
            foreach(var recoveryKey in new[]{"recover-pending","recover-backup"})if(controls.TryGetValue(recoveryKey,out var recovery))recovery.gameObject.SetActive(model.recoveryAvailable);
            foreach(var control in controls.Values.Where(control=>control.tab||control.key is "guide" or "map" or "settings" or "back:resources"))SetProjectEnabled(control.Button,!model.recoveryAvailable);
            var attachmentTitle=inventoryAttachments!=null?inventoryAttachments.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.gameObject.name=="Attachment heading"):null;
            if(attachmentTitle!=null)attachmentTitle.text=$"ATTACHMENTS                     {model.occupiedMounts} / {model.attachmentMounts}";
            if (status != null) { status.text = model.status ?? ""; status.gameObject.SetActive(!string.IsNullOrEmpty(model.status)); }
            if (projectBody != null) projectBody.text = model.projectOverview ?? "";
            if (controls.TryGetValue("play", out var play)) { play.Button.interactable = model.canPlay;play.SetContent(model.playLabel); }
            RefreshInventory(); if(page!="tracked")Inspect(inspected); ApplyTypography();LayoutEquipmentLabels();
            RefreshTracked();RefreshProjects();RefreshWorkshopPlans();RefreshTreeRelations();if(resourcesBuilt){RefreshAlmanac();ReflowResources();}
            if(pickerTarget!=null)GatePicker();if(quoting)GateQuote();
            if(StorageChoiceOpen)GateStorageChoice();
            if(SettingsPresentationOpen)GateSettingsPresentation();
            if(model.cargoReview&&!model.recoveryAvailable&&page!="review"&&!quoting)ShowPage("review");
            if(model.recoveryAvailable&&page!="start"&&!quoting&&pickerTarget==null&&!StorageChoiceOpen)ShowPage("start");
            EnsureUsableFocus();RefreshFocusedDetails();
        }

        public void ShowPage(string key)
        {
            if (!initialized) Initialize();
            if(presentation.recoveryAvailable&&key!="start")return;
            if(presentation.cargoReview&&!presentation.recoveryAvailable&&key!="review")return;
            if(quoting||pickerTarget!=null||StorageChoiceOpen)return;
            if(key=="start"&&page is not ("start" or "guide" or "almanac" or "map" or "settings"))startReturnPage=page;
            if(key is "storage" or "jobs")lastOutpostPage=key;
            if(!presentation.townAccess && key is "storage" or "review" or "jobs" or "guild" or "mess")
            {Command?.Invoke("restricted:town");return;}
            if (!roots.ContainsKey(key)) throw new ArgumentException("Unknown menu page: " + key);
            RememberBrowsingState();
            if(page=="tracked"&&key!="tracked"&&controls.TryGetValue("hint:west",out var restoredWest))restoredWest.Button.interactable=true;
            page = key; pickerTarget = null;selectedStack=null;
            shownContext=FocusContext;
            foreach (var root in roots) root.Value.SetActive(root.Key == key);
            var inventory = key == "inventory";
            var crafting = key is "tree" or "tracked" or "projects" or "workstations" or "legger-equipment" or "crafting-equipment";
            SetObject("InventoryTabs", inventory);
            SetObject("CraftingTabs", crafting);
            SetObject("OutpostTabs", key is "storage" or "jobs");
            foreach (var control in controls)
            {
                if (control.Key.StartsWith("page:", StringComparison.Ordinal)) control.Value.SetSelected(control.Key == "page:" + key);
                if (control.Key == "primary:play") control.Value.SetSelected(key == "play");
                if (control.Key == "primary:outpost") control.Value.SetSelected(key is "storage" or "jobs");
                if (control.Key == "primary:crafting") control.Value.SetSelected(crafting);
                if(control.Key.StartsWith("tree-node:",StringComparison.Ordinal))control.Value.SetSelected(control.Key=="tree-node:"+selectedTree);
            }
            SetObject("ItemInspector", key is "inventory" or "storage" or "review" or "crafting-equipment" or "tree" or "tracked");
            SetObject("EquipmentPicker", false); RefreshInventory();if(key=="tracked")RefreshTracked();if(key=="projects")RefreshProjects();RefreshLayout();
            if(key=="tree")Inspect(selectedTree);
            if(key=="almanac"&&resourcesBuilt){RefreshAlmanac();ReflowResources();}
            if(cargoGrid!=null&&cargoGrid.gameObject.activeInHierarchy)
            {var scroll=cargoGrid.GetComponent<ScrollRect>();scroll.StopMovement();scroll.content.anchoredPosition=scrollMemory.GetValueOrDefault(FocusContext);}
            if(equipmentScroll!=null&&equipmentScroll.gameObject.activeInHierarchy){equipmentScroll.StopMovement();equipmentScroll.content.anchoredPosition=scrollMemory.GetValueOrDefault(FocusContext);}
            if(events!=null)
            {
                if(key=="tracked"){FocusTrackedPage();}
                else
                {
                CampMenuControl focus=null;
                if(key=="review"&&controls.TryGetValue("send-all",out var sendAll))focus=sendAll;
                else if(key=="play"&&controls.TryGetValue("play",out var play)&&play.Button.IsInteractable())focus=play;
                else if(pageFocus.TryGetValue(FocusContext,out var saved)&&!(key=="tree"&&saved.StartsWith("tree-node:",StringComparison.Ordinal)))controls.TryGetValue(saved,out focus);
                else if(key=="start")controls.TryGetValue("guide",out focus);
                if(focus==null||focus.tab||!focus.gameObject.activeInHierarchy||!focus.Button.IsInteractable())
                    focus=controls.Values.FirstOrDefault(c=>c.gameObject.activeInHierarchy&&c.Button.interactable&& (c.key=="play"||c.key=="equip:frame"||c.key=="cargo:0"||c.key=="bookmark:0"||c.key=="node:"+definition.rootId||c.key=="settings"));
                if(focus!=null)events.SetSelectedGameObject(focus.gameObject);
                }
            }
            EnsureUsableFocus();RefreshFocusedDetails();
            PageChanged?.Invoke(key);
        }

        public void ShowInventory(string carrier)
        {if(quoting||pickerTarget!=null)return;owner = carrier == "legger" ? "legger" : "booter"; ShowPage("inventory"); }

        public void ShowPicker(string target, CampMenuStackView[] candidates)
        {
            if(quoting)return;
            RestorePickerGate();pickerRows=candidates;
            pickerTarget = target; SetObject("ItemInspector", false); SetObject("EquipmentPicker", true);
            EnsurePickerControls(candidates.Length);
            for (var i = 0; i < controls.Keys.Count(k=>k.StartsWith("candidate:",StringComparison.Ordinal)); i++)
            {
                if (!controls.TryGetValue("candidate:" + i, out var control)) continue;
                control.gameObject.SetActive(i < candidates.Length);
                if (i >= candidates.Length) continue;
                var item = definition.Find(candidates[i].itemId);
                control.SetContent(item?.DisplayName ?? candidates[i].itemId, item?.art);
                int copies=candidates.Count(c=>c.owner==candidates[i].owner&&c.itemId==candidates[i].itemId);
                int copy=Array.FindIndex(candidates.Where(c=>c.owner==candidates[i].owner&&c.itemId==candidates[i].itemId).ToArray(),c=>c.key==candidates[i].key)+1;
                control.subtitle.text = candidates[i].owner+(copies>1?" · Copy "+copy:"")+" · ×"+candidates[i].quantity;
                control.SetSelected(candidates[i].equipmentAddress == target);
                control.name = "Candidate " + candidates[i].key;
            }
            ConfigurePickerPreview();GatePicker();RefreshLayout();Canvas.ForceUpdateCanvases();ReflowPicker();Canvas.ForceUpdateCanvases();
            if(pickerList!=null){pickerViewport.GetComponent<ScrollRect>().StopMovement();pickerList.anchoredPosition=Vector2.zero;}
            if(events!=null)
            {var first=controls.Values.FirstOrDefault(c=>c.key.StartsWith("candidate:",StringComparison.Ordinal)&&c.gameObject.activeInHierarchy&&c.Button.interactable);if(pickerMemory.TryGetValue(target,out var remembered)){int index=Array.FindIndex(candidates,c=>c.key==remembered);if(index>=0)controls.TryGetValue("candidate:"+index,out first);}if(first!=null)events.SetSelectedGameObject(first.gameObject);else if(pickerViewport!=null)pickerPreview.text="No compatible owned equipment is accessible.";}
            RefreshLayout();InputContextChanged?.Invoke();
        }

        public void SetLargeText(bool value)
        {
            bool changed=large!=value;large=value;ApplyTypography();RefreshLayout();
            if(changed&&events?.currentSelectedGameObject is GameObject selected&&selected.transform.IsChildOf(transform))
            {Canvas.ForceUpdateCanvases();RefreshLayout();RevealMenuScrollTarget(selected.transform);}
        }
        public void ShowQuote(string message,string summary="",string title="REVIEW TRANSFER",string confirmLabel="Confirm transfer",bool allowCancel=true)
        {
            if(StorageChoiceOpen)CloseStorageChoice();
            if(!quoting)beforeQuote.Clear();
            quoting=true;if(quoteBody!=null)quoteBody.text=message;if(quoteSummary!=null)quoteSummary.text=summary;SetObject("TransactionQuote",true);
            var quoteRoot=transform.Find("Safe Area/Screen Host/TransactionQuote");
            if(quoteRoot!=null){var heading=quoteRoot.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.text=="REVIEW TRANSFER"||t.name=="Quote heading");if(heading!=null){heading.name="Quote heading";heading.text=title;}}
            if(controls.TryGetValue("quote-confirm",out var confirm))confirm.SetContent(confirmLabel);
            if(controls.TryGetValue("quote-cancel",out var cancelControl))cancelControl.gameObject.SetActive(allowCancel);
            GateQuote();
            if(events!=null&&controls.TryGetValue(allowCancel?"quote-cancel":"quote-confirm",out var initial))events.SetSelectedGameObject(initial.gameObject);
            InputContextChanged?.Invoke();
        }
        private void GateQuote()
        {
            GateScrollContext(transform.Find("Safe Area/Screen Host/TransactionQuote"),beforeQuoteScroll);
            foreach(var selectable in GetComponentsInChildren<Selectable>(true))
            {
                if(!beforeQuote.ContainsKey(selectable))beforeQuote[selectable]=selectable.interactable;
                selectable.interactable=selectable.GetComponent<CampMenuControl>()?.key is "quote-confirm" or "quote-cancel";
            }
        }
        public void HideQuote(){quoting=false;SetObject("TransactionQuote",false);foreach(var row in beforeQuote)if(row.Key!=null)row.Key.interactable=row.Value;beforeQuote.Clear();RestoreScrollContext(beforeQuoteScroll);Render(presentation);InputContextChanged?.Invoke();}
        public void SetVisible(bool value)
        {
            var group = GetComponent<CanvasGroup>();
            group.alpha = value ? 1 : 0; group.interactable = value; group.blocksRaycasts = value;
            if (!value && events != null && events.currentSelectedGameObject != null
                && events.currentSelectedGameObject.transform.IsChildOf(transform)) events.SetSelectedGameObject(null);
        }

        private void Activate(CampMenuControl control)
        {
            var key = control.key;
            if(!control.Button.IsInteractable()||quoting&&key is not ("quote-confirm" or "quote-cancel")||pickerTarget!=null&&!key.StartsWith("candidate:",StringComparison.Ordinal)&&key is not ("back" or "hint:back" or "hint:select"))return;
            if(HandleSettingsActivation(key))return;
            if(HandleStorageChoiceAction(key))return;
            if(StorageChoiceOpen)return;
            if(HandlePocketAction(key))return;
            if(key=="play")
            {
                if(page!="play"||quoting||pickerTarget!=null||!control.gameObject.activeInHierarchy||!control.Button.IsInteractable()||lastPlaySubmitFrame==Time.frameCount)return;
                // Direct action dispatch and the uGUI module can observe the same press.
                lastPlaySubmitFrame=Time.frameCount;
            }
            if(key=="send-all"&&page=="review")
            {
                if(!control.gameObject.activeInHierarchy||lastReviewSubmitFrame==Time.frameCount)return;
                lastReviewSubmitFrame=Time.frameCount;
            }
            if(key=="hint:select")
            {
                FocusPlayAction();FocusReviewAction();FocusTreeAction();var target=events?.currentSelectedGameObject;var selected=target?.GetComponent<Selectable>();
                if(target!=null&&target.transform.IsChildOf(transform)&&target.GetComponent<CampMenuControl>()?.prompt!=true&&(selected!=null?selected.IsInteractable():target==TreeCanvas?.gameObject&&TreeCanvas.CanInteract))
                    ExecuteEvents.Execute(target,new BaseEventData(events),ExecuteEvents.submitHandler);
                return;
            }
            if(key=="hint:back"){Back();return;}
            if(key=="inspector-title")return;
            if(HandleResourceAction(key))return;
            if(HandleWorkshopPlanAction(key))return;
            if(HandleProjectAction(key))return;
            if(key=="hint:west"){Command?.Invoke("shortcut:west");return;}
            if(key=="hint:tabs"){Command?.Invoke("shortcut:primary");return;}
            if(key=="hint:subtabs"){Command?.Invoke("shortcut:context");return;}
            if (key.StartsWith("page:", StringComparison.Ordinal)) { ShowPage(key.Substring(5)); return; }
            if (key == "primary:play") { ShowPage("play"); return; }
            if (key == "primary:outpost") { ShowPage(lastOutpostPage); return; }
            if (key == "primary:crafting") { ShowPage("workstations"); return; }
            if (key == "carrier:booter" || key == "carrier:legger") { ShowInventory(key.Substring(8)); return; }
            if(key.StartsWith("tree-group:",StringComparison.Ordinal)){ToggleTreeGroup(key);return;}
            if (key.StartsWith("node:", StringComparison.Ordinal)) {if(treeStage!=null)OpenDisplayTree(key.Substring(5),true);else Inspect(key.Substring(5)); return; }
            if (key.StartsWith("tree-node:", StringComparison.Ordinal))
            {selectedTree=key.Substring(10);foreach(var node in controls.Where(c=>c.Key.StartsWith("tree-node:",StringComparison.Ordinal)))node.Value.SetSelected(node.Key==key);RefreshTreeRelations();Inspect(selectedTree);return;}
            if(key.StartsWith("tracked-filter:",StringComparison.Ordinal)){SelectTrackedFilter(key=="tracked-filter:all"?"":key.Substring(15));return;}
            if(key=="tracked-grid"){FocusTrackedSelection();return;}
            if(key.StartsWith("bookmark:",StringComparison.Ordinal)){PreviewTracked(int.Parse(key.Substring(9)));return;}
            if(key.StartsWith("carried:",StringComparison.Ordinal)){Command?.Invoke("equip:carried-"+key.Substring(8));return;}
            if(key is "tree-zoom-in" or "tree-zoom-out" or "tree-fit")
            {if(key=="tree-fit")TreeCanvas?.Fit();else TreeCanvas?.Zoom(key=="tree-zoom-in"?1.15f:1/1.15f);return;}
            if (key.StartsWith("cargo:", StringComparison.Ordinal)) { Browse(control);if(selectedStack!=null)confirmedSelections[SelectionContext]=SelectionIdentity(selectedStack);RefreshConfirmedSelection();return; }
            if (key == "back") { Back(); return; }
            Command?.Invoke(key);
        }

        private void Browse(CampMenuControl control)
        {
            if(control.prompt||control.tab||!control.Button.IsInteractable())return;
            if(control.key.StartsWith("candidate:",StringComparison.Ordinal)){PreviewCandidate(control);return;}
            if(pickerTarget!=null)return;
            if(page=="tree"&&control.HasNavigationFocus)TreeCanvas?.ReleaseFocus();
            pageFocus[FocusContext]=control.key;
            if(SettingsPresentationOpen&&control.HasNavigationFocus)RememberSettingsFocus(control.gameObject);
            RevealFocusedControl(control);
            if(BrowsePocket(control))return;
            if(page=="almanac"&&control.key.StartsWith("almanac-entry:",StringComparison.Ordinal))SelectAlmanacCard(int.Parse(control.key.Substring(14)));
            if(control.key.StartsWith("equip:",StringComparison.Ordinal)||control.key.StartsWith("cargo:",StringComparison.Ordinal))selectedStack=null;
            if (control.key.StartsWith("node:", StringComparison.Ordinal)){if(treeStage==null)Inspect(control.key.Substring(5));}
            else if (control.key.StartsWith("tree-node:", StringComparison.Ordinal))
            {
                if(control.HasNavigationFocus)Inspect(control.key.Substring(10));
            }
            else if(control.key.StartsWith("bookmark:",StringComparison.Ordinal))PreviewTracked(int.Parse(control.key.Substring(9)));
            else if (control.key.StartsWith("equip:", StringComparison.Ordinal))
            {
                foreach (var stack in presentation.stacks)
                    if (stack.owner == "booter" && stack.equipmentAddress == control.key.Substring(6)) { selectedStack=stack;Inspect(stack.itemId); return; }
                Inspect(null);
            }
            else if (control.key.StartsWith("cargo:", StringComparison.Ordinal))
            {
                var rows = CargoRows();
                if (int.TryParse(control.key.Substring(6), out var i) && i < rows.Count){selectedStack=rows[i];Inspect(rows[i].itemId);}else Inspect(null);
            }
            else if(control.key.StartsWith("carried:",StringComparison.Ordinal))
            {int slot=CampExpeditionLayout.BodyCount+CampExpeditionLayout.MountCount+int.Parse(control.key.Substring(8));selectedStack=presentation.stacks.FirstOrDefault(s=>s.owner=="booter"&&s.slotIndex==slot);Inspect(selectedStack?.itemId);}
            if(page=="tracked")RefreshTrackedUntrackAvailability();
            if(EquipmentContext)RefreshGarmentPreviews();
        }

        private static void RevealFocusedControl(CampMenuControl control)
        {
            if(!control.HasNavigationFocus)return;
            RevealMenuScrollTarget(control.transform);
        }
        private static void RevealMenuScrollTarget(Transform target)
        {
            var scroll=target.GetComponentInParent<ScrollRect>();if(scroll==null||!scroll.isActiveAndEnabled||scroll.viewport==null||scroll.content==null)return;
            if(target==scroll.viewport||target==scroll.transform)return;
            Canvas.ForceUpdateCanvases();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,target);var viewport=scroll.viewport.rect;var shift=Vector2.zero;
            if(scroll.horizontal){if(bounds.min.x<viewport.xMin)shift.x=viewport.xMin-bounds.min.x;if(bounds.max.x>viewport.xMax)shift.x=viewport.xMax-bounds.max.x;}
            if(scroll.vertical){if(bounds.size.y>viewport.height)shift.y=viewport.yMax-bounds.max.y;else{if(bounds.min.y<viewport.yMin)shift.y=viewport.yMin-bounds.min.y;if(bounds.max.y>viewport.yMax)shift.y=viewport.yMax-bounds.max.y;}}
            scroll.StopMovement();scroll.content.anchoredPosition+=shift;
        }

        internal void RefreshFocusedDetails()
        {
            var control=events?.currentSelectedGameObject?.GetComponent<CampMenuControl>();
            if(control==null||!control.transform.IsChildOf(transform)||!control.gameObject.activeInHierarchy||!control.Button.IsInteractable())return;
            if(new[]{"equip:","cargo:","carried:","node:","tree-node:","bookmark:"}.Any(prefix=>control.key.StartsWith(prefix,StringComparison.Ordinal)))Browse(control);
        }

        private void EnsureUsableFocus()
        {
            if(events==null)return;
            if(!GetComponent<CanvasGroup>().interactable){if(events.currentSelectedGameObject!=null&&events.currentSelectedGameObject.transform.IsChildOf(transform))events.SetSelectedGameObject(null);return;}
            if(SettingsPresentationOpen){RepairSettingsFocus();return;}
            if(StorageChoiceOpen){RepairStorageChoiceFocus();return;}
            if(TreeCameraOwnsFocus&&events.currentSelectedGameObject==TreeCanvas.gameObject&&TreeCanvas.CanInteract)return;
            var selected=events.currentSelectedGameObject?.GetComponent<Selectable>();
            if(selected!=null&&selected.GetComponent<CampMenuControl>()?.tab!=true&&selected.transform.IsChildOf(transform)&&selected.gameObject.activeInHierarchy&&selected.IsInteractable())return;
            if(page=="tree"&&treeSearch!=null)
            {var first=libraryControls.GetValueOrDefault("tree-group:equipment");events.SetSelectedGameObject(first!=null&&first.gameObject.activeInHierarchy?first.gameObject:treeSearch.gameObject);return;}
            if(!quoting&&pickerTarget==null&&page=="tracked"&&trackedBuilt){FocusTrackedPage();return;}
            if(!quoting&&pickerTarget==null&&page=="projects"&&projectsBuilt){FocusProjectsPage();return;}
            if(!quoting&&pickerTarget==null&&resourcesBuilt&&page is "guide" or "almanac" or "map"){FocusResourcePage();return;}
            string key=quoting?"quote-cancel":pickerTarget!=null?"back":page=="review"?"send-all":page=="play"?"play"
                :(page=="inventory"&&owner=="booter")||page=="crafting-equipment"?"equip:frame"
                :(page is "storage" or "review")||(page=="inventory"&&owner=="legger")?"cargo:0"
                :page=="tree"?"node:"+definition.rootId
                :page=="workstations"?"station:"+definition.stations.FirstOrDefault()?.id
                :page=="tracked"?"tracked-filter:all":page=="start"?(presentation.recoveryAvailable?"recover-pending":"guide"):"";
            if(!controls.TryGetValue(key,out var fallback)||fallback.tab||!fallback.gameObject.activeInHierarchy||!fallback.Button.IsInteractable())fallback=controls.Values.FirstOrDefault(c=>!c.prompt&&!c.tab&&c.gameObject.activeInHierarchy&&c.Button.IsInteractable());
            events.SetSelectedGameObject(fallback!=null?fallback.gameObject:null);
        }

        private List<CampMenuStackView> CargoRows()
        {
            string carrier = page == "storage" ? "storage" : owner;
            var rows = new List<CampMenuStackView>();
            foreach (var stack in presentation.stacks)
                if (page=="review"?stack.owner is "booter" or "legger":stack.owner == carrier && string.IsNullOrEmpty(stack.equipmentAddress)) rows.Add(stack);
            if(page=="storage")
            {
                string search=storageSearch?.text??"";
                rows=rows.Where(s=>StorageMatches(s,search)).ToList();
                rows=rows.OrderBy(s=>sortByQuantity?-s.quantity:0).ThenBy(s=>ItemName(s.itemId),StringComparer.OrdinalIgnoreCase).ThenBy(s=>s.key,StringComparer.Ordinal).ToList();
            }
            return rows;
        }

        private void RefreshInventory()
        {
            if (!initialized) return;
            RefreshStorageChoiceModel();
            bool equipment = (page == "inventory" && owner == "booter") || page == "crafting-equipment";
            SetObject("BooterLayout", equipment);
            SetObject("CargoLayout", page == "storage" || page == "review" || page == "inventory" && owner == "legger");
            if(storageToolbar!=null)storageToolbar.gameObject.SetActive(page=="storage");
            if(cargoGrid!=null)cargoGrid.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,page=="storage"?184:page=="review"?90:38,Mathf.Max(180,content.rect.height-(page=="storage"?184:page=="review"?90:38)));
            if(cargoSummary!=null)
            {
                int occupied=presentation.stacks.Count(s=>s.owner==(page=="storage"?"storage":"legger"));
                cargoSummary.text=page=="storage"?$"Storage\n\nCAMP\n\n▤\n\nCamp chest\n\n{occupied} / {presentation.storageCapacity}\ncargo spaces used":page=="review"?"Recovered cargo\n\nReview Booter and Legger belongings.\n\nSelect sends all belongings to storage and completes your return. Browse items to inspect them; Transfer sends an individual stack.":$"COMPANION\n\n♜\n\nThe Legger\n\n{occupied} / {presentation.leggerCapacity}\ncargo spaces used\n\nCargo mass\n{presentation.leggerMass:0.0} / {presentation.leggerMaximumMass:0.0}\n\n{(presentation.leggerAccessible?"Available":"Out of reach")}";
            }
            foreach (var slot in new[] { "back", "frame", "chest", "legs", "head", "hands", "feet", "weapon", "tool", "attachment-0", "attachment-1" })
            {
                if (!controls.TryGetValue("equip:" + slot, out var control)) continue;
                CampMenuStackView found = null;
                foreach (var stack in presentation.stacks) if (stack.owner == "booter" && stack.equipmentAddress == slot) { found = stack; break; }
                var item = found == null ? null : definition.Find(found.itemId);
                bool compact=((RectTransform)control.transform).rect.width<110;
                string label=item?.DisplayName??"Empty";
                if(compact&&found?.itemId==CampExpeditionDefinition.ToolZeroId)label="Bone tool";
                control.SetContent(label, item?.art ?? style.FindGlyph(slot));
                if(slot.StartsWith("attachment-",StringComparison.Ordinal)){int mount=int.Parse(slot.Substring(11));control.gameObject.SetActive(mount<presentation.attachmentMounts);}
                if(control.art!=null){control.art.color=item==null?new Color(style.ink.r,style.ink.g,style.ink.b,.24f):Color.white;control.art.rectTransform.localScale=Vector3.one*(item==null?.42f:1);}
            }
            var rows = CargoRows();
            int count=CargoDisplayCount();if(storageEmpty!=null)storageEmpty.gameObject.SetActive(StorageFiltered&&rows.Count==0);
            EnsureCargoControls(count);
            int existing=controls.Keys.Count(k=>k.StartsWith("cargo:",StringComparison.Ordinal));
            for (var i = 0; i < existing; i++)
                if (controls.TryGetValue("cargo:" + i, out var control))
                {
                    var stack = i < rows.Count ? rows[i] : null;
                    var item = stack == null ? null : definition.Find(stack.itemId);
                    control.SetContent(item?.DisplayName ?? "+", item?.art, stack == null ? "" : "×" + stack.quantity+(page=="review"?" · "+stack.owner:""));
                    control.gameObject.SetActive(i < count);
                }
            RefreshConfirmedSelection();
            RefreshCarriedEquipment();
            RefreshGarmentPreviews();
        }

        public void Inspect(string itemId)
        {
            bool changed=inspected!=itemId;inspected = itemId; var item = definition.Find(itemId);var display=menuCatalog?.Find(itemId);bool known=item!=null||display!=null;
            if (inspectorTitle != null) inspectorTitle.text = known?ItemName(itemId):"";
            string purpose=display?.purpose??item?.purpose;if(string.IsNullOrWhiteSpace(purpose))purpose=known?"No description recorded.":page=="tree"?"Choose a part to inspect it.":"Select an item.";
            if (inspectorBody != null) inspectorBody.text = InventoryDescription(itemId,purpose);
            LayoutInspector(known);
            SetItemArtwork(inspectorArt,known?itemId:null);
            if (inspectorCount != null) inspectorCount.text = !known?"":(display?.tier??item?.tier)+"   "+(display?.rarity??item?.rarity);
            if(controls.TryGetValue("track",out var tracking)){tracking.gameObject.SetActive(known);tracking.SetContent(presentation.tracked.Contains(itemId)?"Untrack item":"Track item");}
            if(controls.TryGetValue("transfer",out var transfer))transfer.gameObject.SetActive(item!=null&&page is "inventory" or "storage" or "review");
            if(changed)foreach(var viewport in new[]{inspectorTitleViewport,inspectorBodyViewport})if(viewport!=null){var scroll=viewport.GetComponent<ScrollRect>();if(scroll!=null){scroll.StopMovement();scroll.verticalNormalizedPosition=1;}}
            Command?.Invoke("inspect:" + (itemId ?? ""));
        }

        private void LayoutInspector(bool hasItem)
        {
            if(content==null)return;
            float actionHeight=45;
            if(controls.TryGetValue("track",out var action))
            {actionHeight=Mathf.Clamp(action.title.preferredHeight+8,45,100);((RectTransform)action.transform).SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom,60,actionHeight);}
            float titleHeight=inspectorTitle==null?28:Mathf.Clamp(inspectorTitle.preferredHeight+4,28,Mathf.Max(60,content.rect.height*.35f));
            if(inspectorTitleViewport!=null){inspectorTitleViewport.gameObject.SetActive(hasItem);inspectorTitleViewport.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,55,titleHeight);}
            float top=Mathf.Min(Mathf.Max(200,content.rect.height-168),Mathf.Max(Mathf.Min(415,content.rect.height*.58f),55+titleHeight+120));
            if(inspectorArt!=null)inspectorArt.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,65+titleHeight,Mathf.Max(20,top-125-titleHeight));
            if(inspectorCount!=null)inspectorCount.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,top-50,32);
            if(inspectorBodyViewport!=null)inspectorBodyViewport.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,hasItem?top:55,hasItem?Mathf.Max(48,content.rect.height-top-Mathf.Max(120,actionHeight+75)):100);
        }

        public void Back()
        {
            if(StorageChoiceOpen){CloseStorageChoice();return;}
            if(SettingsPresentationOpen){settingsBack?.Invoke();return;}
            if(quoting){Command?.Invoke("quote-cancel");return;}
            if(IsEditingText){events.currentSelectedGameObject.GetComponent<InputField>().DeactivateInputField();InputContextChanged?.Invoke();return;}
            if (pickerTarget != null) { var target = pickerTarget;pickerTarget = null;RestorePickerGate(); SetObject("EquipmentPicker", false); SetObject("ItemInspector", true);
                if (events != null && controls.TryGetValue("equip:" + target, out var control)) events.SetSelectedGameObject(control.gameObject);RefreshLayout();InputContextChanged?.Invoke();return; }
            if(page=="review"){FocusReviewAction();return;}
            if(ReturnFromResource())return;
            Command?.Invoke("back:" + page);
        }
        public void OnCancel(BaseEventData data) { Back(); data.Use(); }
        private void SetObject(string name, bool value)
        {
            var target = transform.Find("Safe Area/" + name);
            if (target == null) target = transform.Find("Safe Area/Screen Host/" + name);
            if (target != null && target.gameObject.activeSelf != value) target.gameObject.SetActive(value);
        }
        public void RefreshLayout()
        {
            if (!initialized || layingOut) return; layingOut = true;
            if (safeArea != null)
            {
                var safe = Screen.safeArea;
                if (Screen.width > 0 && Screen.height > 0) { safeArea.anchorMin = safe.min / new Vector2(Screen.width, Screen.height); safeArea.anchorMax = safe.max / new Vector2(Screen.width, Screen.height); }
            }
            var backdrop=transform.Find("Outpost Backdrop") as RectTransform;
            if(backdrop!=null&&style.backdrop!=null)
            {var bounds=((RectTransform)transform).rect;float ratio=style.backdrop.width/(float)style.backdrop.height;float width=Mathf.Max(bounds.width,bounds.height*ratio)*1.08f;backdrop.anchorMin=backdrop.anchorMax=backdrop.pivot=new Vector2(.5f,.5f);backdrop.anchoredPosition=Vector2.zero;backdrop.sizeDelta=new Vector2(width,width/ratio);}
            LayoutEquipmentScroll();
            LayoutStorageChoices();
            LayoutStorageToolbar();
            if(inspectorTitle!=null)
            {
                var inspector=(inspectorTitleViewport!=null?inspectorTitleViewport.parent:inspectorTitle.transform.parent) as RectTransform;
                inspector.anchorMin=new Vector2(page=="tree"?.786f:page=="tracked"?.825f:page=="storage"||owner=="legger"?.715f:.727f,0);inspector.anchorMax=Vector2.one;inspector.offsetMin=Vector2.zero;inspector.offsetMax=Vector2.zero;
            }
            if(cargoGrid!=null)
            {
                var grid=cargoGrid.GetComponentInChildren<GridLayoutGroup>(true);
                if(grid!=null)
                {
                    int columns=cargoGrid.rect.width<750?3:4;float height=large?195:175;grid.constraintCount=columns;grid.cellSize=new Vector2(Mathf.Max(76,(cargoGrid.rect.width-(columns-1)*12)/columns),height);
                    grid.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Ceil(CargoDisplayCount()/(float)columns)*(height+12));
                    foreach(var card in controls.Values.Where(c=>c.key.StartsWith("cargo:",StringComparison.Ordinal)))
                    {card.title.rectTransform.anchorMax=new Vector2(1,.5f);card.title.rectTransform.offsetMin=new Vector2(8,28);card.art.rectTransform.anchorMin=new Vector2(0,.52f);card.art.rectTransform.anchorMax=new Vector2(1,.83f);}
                }
            }
            ApplyTypography(); layingOut = false;
            LayoutInspector(KnownItem(inspected)||page=="tracked"&&trackedSelected!=null);
            ReflowCraftingTabs();
            var outpost=transform.Find("Safe Area/OutpostTabs") as RectTransform;
            if(outpost!=null)
            {
                float x=65;
                foreach(var key in new[]{"page:storage","page:jobs"})if(controls.TryGetValue(key,out var tab))
                {float width=tab.title.preferredWidth+20;((RectTransform)tab.transform).SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left,x,width);x+=width+8;}
                var trigger=outpost.Find("Prompt rt") as RectTransform;if(trigger!=null)trigger.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left,x+10,32);
            }
            float primaryX=125;
            foreach(var key in new[]{"primary:play","primary:outpost","primary:crafting"})if(controls.TryGetValue(key,out var primary))
            {float width=primary.title.preferredWidth+20;((RectTransform)primary.transform).SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left,primaryX,width);primaryX+=width+10;}
            var rightBumper=transform.Find("Safe Area/Primary/Prompt rb") as RectTransform;if(rightBumper!=null)rightBumper.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left,primaryX+5,40);
            RefreshInventory();
            LayoutEquipmentLabels();
            ReflowPicker();
            if(footer!=null)
            {
                float width=footer.rect.width;
                var prompt=footer.Find("Text")?.GetComponent<Text>();
                if(prompt!=null){prompt.text=width<1500?"Select   Back   LB / RB Tabs   LT / RT Sub-tabs":"Select     Unequip     Back     Navigate     Scroll     LB / RB Primary tabs     LT / RT Sub-tabs";prompt.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Max(300,width-390));}
                if(controls.TryGetValue("hint:west",out var west)){west.gameObject.SetActive(pickerTarget==null&&(page is "tree" or "tracked" or "crafting-equipment" or "inventory" or "storage" or "review"));west.SetContent(page is "tree" or "tracked"?"Track / Untrack":page=="crafting-equipment"||page=="inventory"&&owner=="booter"?"Unequip":"Transfer");}
                if(controls.TryGetValue("hint:subtabs",out var sub))sub.gameObject.SetActive(page is not ("play" or "start" or "guide" or "almanac" or "map" or "settings"));
                if(controls.TryGetValue("hint:pan",out var pan))pan.gameObject.SetActive(page is "tree" or "tracked");
                if(controls.TryGetValue("hint:scroll",out var scrollHint))scrollHint.SetContent(page=="tree"?"Zoom":"Scroll");
                if(controls.TryGetValue("hint:navigate",out var navigate)){var glyph=navigate.transform.Find("Navigation glyph")?.GetComponent<Text>();if(glyph!=null)glyph.text=page is "tree" or "tracked"?"+":"+ / LS";}
                if(controls.TryGetValue("hint:tabs",out var tabs))tabs.SetContent(width<1500?"Tabs":"Primary tabs");
                bool compact=width<1400;
                foreach(var key in new[]{"page:start","page:inventory"})if(controls.TryGetValue(key,out var shortcut))shortcut.SetContent(compact?(key=="page:start"?"Start":"Kit"):(key=="page:start"?"Start menu":"Inventory"));
                foreach(var key in new[]{"page:start","page:inventory","hint:tabs"})if(controls.TryGetValue(key,out var coveredPrompt))coveredPrompt.gameObject.SetActive(!SettingsPresentationOpen);
                if(!SettingsPresentationOpen)foreach(var key in new[]{"hint:select","hint:navigate","hint:scroll"})if(controls.TryGetValue(key,out var restoredPrompt))restoredPrompt.gameObject.SetActive(true);
                if(controls.TryGetValue("hint:navigate",out var navHint))navHint.SetContent(compact?"Move":"Navigate");
                if(controls.TryGetValue("hint:pan",out var panHint))panHint.SetContent(compact?"Pan":"Pan canvas");
                if(controls.TryGetValue("hint:west",out var westHint)&&compact&&page is "tree" or "tracked")westHint.SetContent("Track");
                if(page=="tracked")
                {if(controls.TryGetValue("hint:pan",out var resultsHint))resultsHint.SetContent("Results");if(controls.TryGetValue("hint:navigate",out var gridHint))gridHint.SetContent("Grid");if(controls.TryGetValue("hint:scroll",out var inspectorHint))inspectorHint.SetContent(compact?"Inspect":"Inspect / scroll");if(controls.TryGetValue("hint:west",out var removeHint))removeHint.SetContent("Untrack");}
                var prompts=footer.Find("Prompts");
                if(prompts!=null){prompts.GetComponent<HorizontalLayoutGroup>().spacing=compact?2:12;((RectTransform)prompts).anchoredPosition=new Vector2(compact?-20:-52,0);}
                if(prompts!=null)foreach(var control in prompts.GetComponentsInChildren<CampMenuControl>(true))
                {var element=control.GetComponent<LayoutElement>();if(element!=null)element.preferredWidth=control.title.rectTransform.offsetMin.x+control.title.preferredWidth+8;}
            }
            SetObject("Primary",page!="review");
            foreach(var key in new[]{"page:start","page:inventory","hint:tabs","hint:subtabs","hint:back"})
                if(controls.TryGetValue(key,out var reviewHidden)&&page=="review")reviewHidden.gameObject.SetActive(false);
            if(controls.TryGetValue("hint:back",out var backHint)&&page!="review")backHint.gameObject.SetActive(true);
            if(controls.TryGetValue("hint:select",out var selectHint))selectHint.SetContent(page=="review"?"Send all to storage":"Select");
            ApplyBindingPrompts();
            ReflowTracked();
            ReflowProjects();
            ReflowWorkshopPlans();
            ReflowResources();
            ReflowSettingsPresentation();
            ReflowGarmentPreviews();
            RefreshTreeLibrary();
            ReflowCatalogTree();
            TreeCanvas?.RefreshCamera();
            LayoutEquipmentLabels();
        }
        private void ReflowCraftingTabs()
        {
            var bar=transform.Find("Safe Area/CraftingTabs") as RectTransform;if(bar==null)return;
            string[] keys={"workstations","tree","crafting-equipment","tracked","legger-equipment","projects"};
            string[] labels=bar.rect.width<1400?new[]{"Workshops","Crafting tree","Booter","Tracked items","Legger","Projects"}:new[]{"Workshops","Crafting tree","Booter equipment","Tracked items","Legger equipment","Tracked projects"};
            float x=65;for(int i=0;i<keys.Length;i++)if(controls.TryGetValue("page:"+keys[i],out var tab))
            {tab.SetContent(labels[i]);float width=tab.title.preferredWidth+20;var rect=(RectTransform)tab.transform;rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left,x,width);x+=width+8;}
            var trigger=bar.Find("Prompt rt") as RectTransform;if(trigger!=null)trigger.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left,x+10,32);
        }
        private void ApplyTypography()
        {
            foreach (var entry in fontSizes)
                if (entry.Key != null) entry.Key.fontSize = Mathf.RoundToInt(entry.Value * (large ? 1.16f : 1));
        }
        private void EnsureCargoControls(int count)
        {
            if(!controls.TryGetValue("cargo:0",out var template))return;
            for(int i=48;i<count;i++)
            {
                string key="cargo:"+i;if(controls.ContainsKey(key))continue;
                var clone=Instantiate(template,template.transform.parent);clone.name=key;clone.key=key;clone.Wire();clone.Activated+=Activate;clone.Browsed+=Browse;controls.Add(key,clone);
                clone.Button.interactable=beforeStorageChoice.TryGetValue(template.Button,out var templateEnabled)?templateEnabled:beforeQuote.TryGetValue(template.Button,out templateEnabled)?templateEnabled:template.Button.interactable;
                if(clone.cornerLabel!=null)clone.cornerLabel.text=(i+1).ToString("00");
                RememberClonedTypography(template,clone);
            }
        }
        public void ScrollInspector(float vertical,float seconds)
        {
            bool details=pickerTarget!=null&&events?.currentSelectedGameObject==pickerDetailsViewport?.gameObject;
            var viewport=quoting?quoteViewport:pickerTarget!=null?details?pickerDetailsViewport:pickerViewport:null;
            var scroll=viewport!=null?viewport.GetComponent<ScrollRect>():events?.currentSelectedGameObject?.GetComponentInParent<ScrollRect>();
            if(viewport==null&&scroll!=null&&scroll.viewport==inspectorTitleViewport&&scroll.content.rect.height<=scroll.viewport.rect.height+1)scroll=null;
            if(scroll==null||!scroll.transform.IsChildOf(transform)||!scroll.gameObject.activeInHierarchy)scroll=inspectorBodyViewport!=null?inspectorBodyViewport.GetComponent<ScrollRect>():null;
            if(scroll!=null)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+vertical*seconds);
        }
        public void MoveTree(Vector2 pan,float zoom,float seconds)
        {
            TreeCanvas?.MoveSticks(pan,zoom,seconds);
        }
        private void OnRectTransformDimensionsChange() { if (initialized) RefreshLayout(); }
    }
}
