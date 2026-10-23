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
        private readonly Dictionary<Selectable,bool> beforeStorageChoice=new();
        private readonly Dictionary<ScrollRect,bool> beforeStorageChoiceScroll=new();
        private readonly List<CampMenuControl> storageChoices=new();
        private RectTransform storageChoiceRoot,storageChoicePanel;
        private ScrollRect storageChoiceScroll;
        private Text storageChoiceTitle,storageChoiceHelp;
        private Text storageEmpty;
        private CampMenuControl storageChoiceCancel;
        private string storageChoiceKind;
        private string[] storageCategories=Array.Empty<string>(),storageChoiceValues=Array.Empty<string>();
        private bool storageChoiceReady;
        private int storageChoiceFocusIndex;
        internal bool StorageChoiceOpen=>storageChoiceKind!=null;
        private void ConfigureStorageChoices()
        {
            storageChoiceRoot=new GameObject("Storage choices",typeof(RectTransform),typeof(Image),typeof(Button)).GetComponent<RectTransform>();storageChoiceRoot.SetParent(safeArea,false);
            storageChoiceRoot.anchorMin=Vector2.zero;storageChoiceRoot.anchorMax=Vector2.one;storageChoiceRoot.offsetMin=storageChoiceRoot.offsetMax=Vector2.zero;
            storageChoiceRoot.GetComponent<Image>().color=new Color(0,0,0,.7f);var backdrop=storageChoiceRoot.GetComponent<Button>();backdrop.navigation=new Navigation{mode=Navigation.Mode.None};backdrop.onClick.AddListener(()=>{if(storageChoiceReady)CloseStorageChoice();});
            storageChoicePanel=new GameObject("Storage choice panel",typeof(RectTransform),typeof(Image),typeof(Button)).GetComponent<RectTransform>();storageChoicePanel.SetParent(storageChoiceRoot,false);storageChoicePanel.GetComponent<Image>().color=style.surface;storageChoicePanel.GetComponent<Button>().navigation=new Navigation{mode=Navigation.Mode.None};
            storageChoiceTitle=CatalogText(storageChoicePanel,"",24);storageChoiceHelp=CatalogText(storageChoicePanel,"",16);storageChoiceHelp.supportRichText=false;
            storageChoiceScroll=CreateMenuScroll(storageChoicePanel,"Storage options");storageChoiceScroll.viewport.offsetMin=new Vector2(20,158);storageChoiceScroll.viewport.offsetMax=new Vector2(-20,-62);
            storageChoiceCancel=ProjectButton(storageChoicePanel,"storage-choice:cancel","Back");LinkMenuDirection(storageChoiceCancel.gameObject,data=>{if(data.moveDir is MoveDirection.Up or MoveDirection.Down)FocusStorageChoice(data.moveDir==MoveDirection.Up?-1:1,-1);data.Use();});
            storageChoiceRoot.gameObject.SetActive(false);
            if(cargoGrid!=null){storageEmpty=CatalogText(cargoGrid,"No matching items.",18);TopRegion(storageEmpty.rectTransform,12,90,12,12);storageEmpty.gameObject.SetActive(false);}
        }
        private bool StorageFiltered=>page=="storage"&&(!string.IsNullOrEmpty(storageSearch?.text)||storageCategory.Length>0);
        private int CargoDisplayCount()=>StorageFiltered?CargoRows().Count:page=="storage"?presentation.storageCapacity:page=="review"?CampExpeditionLayout.Capacity+presentation.leggerCapacity:presentation.leggerCapacity;
        private void LayoutStorageToolbar()
        {
            if(storageToolbar==null||storageSearch==null||!controls.TryGetValue("storage-filter",out var filter)||!controls.TryGetValue("storage-sort",out var sort))return;
            float width=storageToolbar.rect.width;float filterWidth=Mathf.Min(width*.46f,Mathf.Max(140,filter.title.preferredWidth+24)),sortWidth=Mathf.Min(width*.3f,Mathf.Max(115,sort.title.preferredWidth+24));
            var filterRect=(RectTransform)filter.transform;var sortRect=(RectTransform)sort.transform;
            sortRect.anchorMin=new Vector2(1,0);sortRect.anchorMax=Vector2.one;sortRect.offsetMin=new Vector2(-sortWidth,0);sortRect.offsetMax=Vector2.zero;
            filterRect.anchorMin=new Vector2(1,0);filterRect.anchorMax=Vector2.one;filterRect.offsetMin=new Vector2(-sortWidth-filterWidth-8,0);filterRect.offsetMax=new Vector2(-sortWidth-8,0);
            var search=(RectTransform)storageSearch.transform;search.anchorMin=Vector2.zero;search.anchorMax=Vector2.one;search.offsetMin=Vector2.zero;search.offsetMax=new Vector2(-sortWidth-filterWidth-16,0);
            float height=Mathf.Max(large?64:48,Mathf.Max(ProjectTextHeight(filter.title,filterWidth-20),ProjectTextHeight(sort.title,sortWidth-20))+8);storageToolbar.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,89,height);
        }
        private string StorageCategory(CampMenuStackView stack)=>menuCatalog?.Find(stack.itemId)?.category??definition.Find(stack.itemId)?.category??"Other";
        private void RefreshStorageChoiceModel()
        {
            storageCategories=presentation.stacks.Where(stack=>stack.owner=="storage").Select(StorageCategory).Where(value=>!string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal).OrderBy(value=>value,StringComparer.OrdinalIgnoreCase).ToArray();
            if(storageCategory.Length>0&&!storageCategories.Contains(storageCategory))storageCategory="";
            if(controls.TryGetValue("storage-filter",out var filter))filter.SetContent(storageCategory.Length==0?"All categories ▾":storageCategory+" ▾");
            if(controls.TryGetValue("storage-sort",out var sort))sort.SetContent(sortByQuantity?"Quantity ▾":"Name A–Z ▾");
            if(StorageChoiceOpen)RefreshStorageChoices();
        }
        private bool HandleStorageChoiceAction(string key)
        {
            if(StorageChoiceOpen&&key is "hint:back" or "hint:select")
            {
                if(!storageChoiceReady)return true;
                if(key=="hint:back"){CloseStorageChoice();return true;}
                var selected=events?.currentSelectedGameObject;var control=selected?.GetComponent<CampMenuControl>();
                if(control!=null&&control.transform.IsChildOf(storageChoicePanel)&&control.Button.IsInteractable())ExecuteEvents.Execute(selected,new BaseEventData(events),ExecuteEvents.submitHandler);return true;
            }
            if(key is "storage-filter" or "storage-sort")
            {
                if(page!="storage"||StorageChoiceOpen||quoting||pickerTarget!=null||SettingsPresentationOpen)return true;
                storageChoiceKind=key;storageChoiceReady=false;storageChoiceRoot.gameObject.SetActive(true);storageChoiceRoot.SetAsLastSibling();RefreshStorageChoices();
                storageChoiceFocusIndex=Math.Max(0,Array.IndexOf(storageChoiceValues,key=="storage-sort"?(sortByQuantity?"quantity":"name"):storageCategory));GateStorageChoice();InputContextChanged?.Invoke();return true;
            }
            if(!key.StartsWith("storage-choice:",StringComparison.Ordinal))return false;
            if(!StorageChoiceOpen||!storageChoiceReady)return true;
            if(key=="storage-choice:cancel"){CloseStorageChoice();return true;}
            if(!int.TryParse(key.Substring(15),out int index)||index<0||index>=storageChoiceValues.Length)return true;
            string value=storageChoiceValues[index];if(value is "!favourites" or "!protected")return true;
            if(storageChoiceKind=="storage-sort")sortByQuantity=value=="quantity";else storageCategory=value;
            selectedStack=null;CloseStorageChoice();RefreshInventory();Inspect(null);RefreshFocusedDetails();return true;
        }
        private void RefreshStorageChoices()
        {
            string selectedKey=events?.currentSelectedGameObject?.GetComponent<CampMenuControl>()?.key;
            string oldValue=null;if(selectedKey!=null&&selectedKey.StartsWith("storage-choice:",StringComparison.Ordinal)&&int.TryParse(selectedKey.Substring(15),out int oldIndex)&&oldIndex<storageChoiceValues.Length)oldValue=storageChoiceValues[oldIndex];
            storageChoiceValues=storageChoiceKind=="storage-sort"?new[]{"name","quantity"}:new[]{""}.Concat(storageCategories).Concat(new[]{"!favourites","!protected"}).ToArray();
            storageChoiceTitle.text=storageChoiceKind=="storage-sort"?"Sort storage":"Filter storage";
            storageChoiceHelp.text=storageChoiceKind=="storage-sort"?"Choose an order. Back keeps the current order.":"Marking favourites and protecting items are not available in this prototype.";
            while(storageChoices.Count<storageChoiceValues.Length)
            {
                int index=storageChoices.Count;var control=ProjectButton(storageChoiceScroll.content,"storage-choice:"+index,"");control.title.supportRichText=false;storageChoices.Add(control);
                LinkMenuDirection(control.gameObject,data=>{if(data.moveDir is MoveDirection.Down or MoveDirection.Up)FocusStorageChoice(data.moveDir==MoveDirection.Down?1:-1,index);data.Use();});
            }
            for(int i=0;i<storageChoices.Count;i++)
            {
                var control=storageChoices[i];bool exists=i<storageChoiceValues.Length;control.gameObject.SetActive(exists);if(!exists)continue;
                string value=storageChoiceValues[i];bool available=!value.StartsWith("!",StringComparison.Ordinal);string label=value switch{""=>"All categories","name"=>"Name A–Z","quantity"=>"Quantity","!favourites"=>"Favourites · unavailable","!protected"=>"Protected · unavailable",_=>value};control.SetContent(label);
                bool current=storageChoiceKind=="storage-sort"?value==(sortByQuantity?"quantity":"name"):value==storageCategory;control.SetSelected(current);control.Button.interactable=storageChoiceReady&&available;beforeStorageChoice[control.Button]=available;
            }
            LayoutStorageChoices();
            if(oldValue!=null&&events!=null)
            {
                int retained=Array.IndexOf(storageChoiceValues,oldValue);storageChoiceFocusIndex=Math.Max(0,retained);events.SetSelectedGameObject(storageChoices[storageChoiceFocusIndex].gameObject);
                if(retained<0){storageChoiceReady=false;InputContextChanged?.Invoke();}
            }
        }
        private void LayoutStorageChoices()
        {
            if(storageChoicePanel==null)return;
            storageChoicePanel.anchorMin=new Vector2(.5f,.5f);storageChoicePanel.anchorMax=storageChoicePanel.anchorMin;storageChoicePanel.pivot=new Vector2(.5f,.5f);storageChoicePanel.anchoredPosition=Vector2.zero;storageChoicePanel.sizeDelta=new Vector2(Mathf.Min(620,Mathf.Max(280,safeArea.rect.width-40)),Mathf.Min(650,Mathf.Max(300,safeArea.rect.height-40)));
            TopRegion(storageChoiceTitle.rectTransform,18,40,20,20);float rowHeight=large?68:54;
            for(int i=0;i<storageChoices.Count;i++)TopRegion((RectTransform)storageChoices[i].transform,i*(rowHeight+6),rowHeight,4,4);
            storageChoiceScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,storageChoiceValues.Length*(rowHeight+6));
            TopRegion(storageChoiceHelp.rectTransform,storageChoicePanel.rect.height-148,84,24,24);TopRegion((RectTransform)storageChoiceCancel.transform,storageChoicePanel.rect.height-58,42,24,24);
        }
        internal void SetStorageChoiceReady()
        {if(!StorageChoiceOpen)return;storageChoiceReady=true;RefreshStorageChoices();GateStorageChoice();RepairStorageChoiceFocus();}
        private bool IsStorageChoiceControl(Selectable selectable)=>selectable.transform.IsChildOf(storageChoiceRoot)||selectable.GetComponent<CampMenuControl>()?.key is "hint:back" or "hint:select";
        private void GateStorageChoice()
        {
            GateScrollContext(storageChoiceRoot,beforeStorageChoiceScroll);
            foreach(var selectable in GetComponentsInChildren<Selectable>(true))
            {if(!beforeStorageChoice.ContainsKey(selectable))beforeStorageChoice[selectable]=selectable.interactable;selectable.interactable=beforeStorageChoice[selectable]&&IsStorageChoiceControl(selectable)&&storageChoiceReady;selectable.GetComponent<CampMenuControl>()?.RefreshAvailability();}
            // Selectable.interactable=false clears EventSystem selection. Restore the
            // pending visual focus after gating; it cannot activate until ready.
            if(!storageChoiceReady&&storageChoices.Count>0)events?.SetSelectedGameObject(storageChoices[Math.Clamp(storageChoiceFocusIndex,0,storageChoiceValues.Length-1)].gameObject);
        }
        private void CloseStorageChoice()
        {
            if(!StorageChoiceOpen)return;string returnKey=storageChoiceKind;storageChoiceKind=null;storageChoiceReady=false;storageChoiceRoot.gameObject.SetActive(false);
            foreach(var row in beforeStorageChoice)if(row.Key!=null){row.Key.interactable=row.Value;row.Key.GetComponent<CampMenuControl>()?.RefreshAvailability();}beforeStorageChoice.Clear();RestoreScrollContext(beforeStorageChoiceScroll);
            RefreshStorageChoiceModel();if(events!=null&&controls.TryGetValue(returnKey,out var control))events.SetSelectedGameObject(control.gameObject);InputContextChanged?.Invoke();
        }
        private void RepairStorageChoiceFocus()
        {
            if(!storageChoiceReady)return;var selected=events?.currentSelectedGameObject?.GetComponent<Selectable>();if(selected!=null&&selected.IsInteractable()&&selected.transform.IsChildOf(storageChoicePanel)){RevealMenuScrollTarget(selected.transform);return;}
            FocusStorageChoice(1,-1);
        }
        private void FocusStorageChoice(int direction,int index)
        {
            if(!storageChoiceReady||events==null)return;
            int count=storageChoiceValues.Length;int position=index<0?(direction<0?count: -1):index;
            for(int i=0;i<=count;i++){position=(position+direction+count+1)%(count+1);var control=position==count?storageChoiceCancel:storageChoices[position];if(control.gameObject.activeInHierarchy&&control.Button.IsInteractable()){events.SetSelectedGameObject(control.gameObject);RevealFocusedControl(control);return;}}
        }
    }
}
