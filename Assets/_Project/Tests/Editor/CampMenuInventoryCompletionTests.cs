using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    // Disposable display records/read models only. No profile, world, live item database or Play Mode.
    public sealed class CampMenuInventoryCompletionTests
    {
        private GameObject root,eventRoot,cameraRoot;
        private EventSystem events,previousEvents;
        private CampExpeditionMenuView view;
        private RenderTexture target;
        private CampMenuCatalogData data;
        private CampMenuCatalogItem garment;
        private readonly List<string> commands=new();
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private CampMenuControl Control(string key)=>root.GetComponentsInChildren<CampMenuControl>(true).Single(control=>control.key==key);
        private void Click(string key)=>Control(key).Button.onClick.Invoke();
        private void Focus(string key){events.SetSelectedGameObject(Control(key).gameObject);typeof(CampExpeditionMenuView).GetMethod("LateUpdate",Private).Invoke(view,null);Canvas.ForceUpdateCanvases();}
        private CampMenuStackView Owned(string instance="garment-one")=>new(){key="0/2",owner="booter",itemId=garment.id,equipmentAddress="chest",quantity=1,instanceId=instance};
        [SetUp] public void Setup()
        {
            previousEvents=EventSystem.current;eventRoot=new GameObject("Inventory completion fixture",typeof(EventSystem));events=eventRoot.GetComponent<EventSystem>();typeof(EventSystem).GetMethod("OnEnable",Private).Invoke(events,null);EventSystem.current=events;
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab"));view=root.GetComponent<CampExpeditionMenuView>();
            data=JsonUtility.FromJson<CampMenuCatalogData>(Resources.Load<TextAsset>("CampMenuCatalog").text);garment=data.items.First(item=>item.pocketSlot=="chest"&&item.category is "Clothing" or "Armor");garment.pockets=9;
            cameraRoot=new GameObject("Inventory layout camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.enabled=false;target=new RenderTexture(1280,720,0);camera.targetTexture=target;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;
            view.Initialize(events,new CampMenuCatalog(data));view.Command+=commands.Add;view.SetVisible(true);view.Render(new CampMenuPresentation{canPlay=true});
        }
        [TearDown] public void Cleanup()
        {
            root.SetActive(false);Object.DestroyImmediate(root);typeof(EventSystem).GetMethod("OnDisable",Private).Invoke(events,null);Object.DestroyImmediate(eventRoot);if(previousEvents!=null)EventSystem.current=previousEvents;Object.DestroyImmediate(cameraRoot);Object.DestroyImmediate(target);commands.Clear();
        }
        [TestCase(-1,0)][TestCase(0,0)][TestCase(1,1)][TestCase(9,9)]
        public void AuthoredPocketsRequireAnOwnedEligibleExactSlot(long count,long expected)
        {
            garment.pockets=count;var owned=Owned();Assert.That(CampMenuGarmentPockets.Count(garment,owned,"chest"),Is.EqualTo(expected));owned.owner="storage";Assert.That(CampMenuGarmentPockets.Count(garment,owned,"chest"),Is.Zero);owned.owner="booter";owned.equipmentAddress="legs";Assert.That(CampMenuGarmentPockets.Count(garment,owned,"legs"),Is.Zero);owned.equipmentAddress="chest";garment.category="Tools";Assert.That(CampMenuGarmentPockets.Count(garment,owned,"chest"),Is.Zero);
        }
        [Test] public void LargeAuthoringCountsRemainExactAndAllocateAtMostEightTiles()
        {
            long count=CampMenuGarmentPockets.MaximumAuthoredCount;var page=CampMenuGarmentPockets.ClampPage(count,long.MaxValue);var numbers=CampMenuGarmentPockets.Numbers(count,page);Assert.That(numbers.Length,Is.InRange(1,8));Assert.That(numbers[^1],Is.EqualTo(count));Assert.That(CampMenuGarmentPockets.Numbers(9,1),Is.EqualTo(new long[]{9}));Assert.That(CampMenuGarmentPockets.Numbers(-1,0),Is.Empty);
            garment.pockets=count;view.Render(new CampMenuPresentation{stacks=new[]{Owned()}});view.ShowInventory("booter");Focus("equip:chest");view.SetPocketPage("chest",long.MaxValue);Assert.That(root.GetComponentsInChildren<CampMenuControl>(true).Count(control=>control.key.StartsWith("pocket:empty:chest:",StringComparison.Ordinal)),Is.EqualTo(8));Assert.That(Control("pocket:empty:chest:0").gameObject.activeInHierarchy,Is.True);
        }
        [Test] public void MalformedPocketCountsAndSlotMappingsFailClosed()
        {
            garment.pockets=-2;Assert.Throws<ArgumentException>(()=>new CampMenuCatalog(data));garment.pockets=CampMenuGarmentPockets.MaximumAuthoredCount+1;Assert.Throws<ArgumentException>(()=>new CampMenuCatalog(data));garment.pockets=2;garment.pocketSlot="weapon";Assert.Throws<ArgumentException>(()=>new CampMenuCatalog(data));
        }
        [Test] public void SelectionAloneDoesNotRevealPocketsAndPagingPreservesOwnedIdentity()
        {
            var owned=Owned();view.Render(new CampMenuPresentation{stacks=new[]{owned}});view.ShowInventory("booter");Focus("equip:chest");Assert.That(Control("pocket:empty:chest:0").gameObject.activeInHierarchy,Is.True);Click("pocket:next:chest");Assert.That(view.PocketPage("chest"),Is.EqualTo(1));Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("equip:chest").gameObject));Assert.That(Control("pocket:empty:chest:0").title.text,Does.Contain("9"));Focus("equip:head");Assert.That(Control("pocket:empty:chest:0").gameObject.activeInHierarchy,Is.False);Focus("equip:chest");Assert.That(view.PocketPage("chest"),Is.EqualTo(1));
            view.Render(new CampMenuPresentation{stacks=new[]{Owned("different-owned-copy")}});Focus("equip:chest");Assert.That(view.PocketPage("chest"),Is.Zero);Assert.That(commands.All(command=>command.StartsWith("inspect:",StringComparison.Ordinal)),Is.True);
        }
        [Test] public void EmptyPocketBrowsingNeverFitsTransfersOrOpensAPicker()
        {
            view.Render(new CampMenuPresentation{stacks=new[]{Owned()}});view.ShowInventory("booter");Focus("equip:chest");var link=Control("equip:chest").GetComponent<CampMenuDirectionalLink>();link.OnMove(new AxisEventData(events){moveDir=MoveDirection.Down});Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("pocket:empty:chest:0").gameObject));Click("pocket:empty:chest:0");Assert.That(view.SelectedStack,Is.Null);Assert.That(view.PickerTarget,Is.Null);Assert.That(view.inspectorBody.text,Does.Contain("preview only"));Assert.That(commands.All(command=>command.StartsWith("inspect:",StringComparison.Ordinal)),Is.True);
            view.ShowQuote("Save waiting");Click("pocket:next:chest");Assert.That(view.PocketPage("chest"),Is.Zero);
        }
        [Test] public void RemovedGarmentRepairsFocusAndRecycledLocationsDoNotInheritPageState()
        {
            var owned=Owned();owned.instanceId=null;owned.storeRevision=1;view.Render(new CampMenuPresentation{stacks=new[]{owned}});view.ShowInventory("booter");Focus("equip:chest");Click("pocket:next:chest");Focus("pocket:empty:chest:0");string identity=CampMenuGarmentPockets.Identity(owned);owned.storeRevision=2;Assert.That(CampMenuGarmentPockets.Identity(owned),Is.Not.EqualTo(identity));view.Render(new CampMenuPresentation{stacks=new[]{owned}});Assert.That(view.PocketPage("chest"),Is.Zero);Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("equip:chest").gameObject));view.Render(new CampMenuPresentation());Assert.That(events.currentSelectedGameObject.activeInHierarchy,Is.True);Assert.That(Control("pocket:empty:chest:0").gameObject.activeInHierarchy,Is.False);
        }
        [Test] public void FilterAndSortOpenWithoutChangingResultsAndBackReturnsToTheirOwner()
        {
            view.Render(new CampMenuPresentation{stacks=new[]{new CampMenuStackView{owner="storage",itemId=CampExpeditionDefinition.ToolZeroId,key="2/0",quantity=1}}});view.ShowPage("storage");Click("storage-filter");Assert.That(view.StorageChoiceOpen,Is.True);Assert.That(Control("storage-choice:0").Button.IsInteractable(),Is.False);view.SetStorageChoiceReady();var unavailable=root.GetComponentsInChildren<CampMenuControl>(true).Where(control=>control.key.StartsWith("storage-choice:",StringComparison.Ordinal)&&control.title.text.Contains("unavailable")).ToArray();Assert.That(unavailable.Length,Is.EqualTo(2));Assert.That(unavailable.All(control=>!control.Button.IsInteractable()),Is.True);Assert.That(Control("cargo:0").Button.IsInteractable(),Is.False);Click("primary:crafting");Assert.That(view.Page,Is.EqualTo("storage"));view.Back();Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("storage-filter").gameObject));Assert.That(Control("cargo:0").Button.IsInteractable(),Is.True);
            Click("storage-sort");view.SetStorageChoiceReady();Click("storage-choice:1");Assert.That(Control("storage-sort").title.text,Does.StartWith("Quantity"));Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("storage-sort").gameObject));Assert.That(commands.All(command=>command.StartsWith("inspect:",StringComparison.Ordinal)),Is.True);
        }
        [Test] public void FilterStockChangesRepairSelectionAndQuotesPreemptThePopup()
        {
            var model=new CampMenuPresentation{stacks=new[]{new CampMenuStackView{owner="storage",itemId=CampExpeditionDefinition.ToolZeroId,key="2/0",quantity=1}}};view.Render(model);view.ShowPage("storage");Click("storage-filter");view.SetStorageChoiceReady();Click("storage-choice:1");Assert.That(Control("storage-filter").title.text,Does.Not.StartWith("All categories"));Click("storage-filter");view.SetStorageChoiceReady();view.Render(new CampMenuPresentation());Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("storage-choice:0").gameObject));Assert.That(Control("storage-choice:0").Button.IsInteractable(),Is.False);view.SetStorageChoiceReady();Assert.That(events.currentSelectedGameObject.GetComponent<Selectable>().IsInteractable(),Is.True);Assert.That(Control("storage-filter").title.text,Does.StartWith("All categories"));view.ShowQuote("Save waiting",allowCancel:false);Assert.That(view.StorageChoiceOpen,Is.False);Assert.That(Control("storage-filter").Button.IsInteractable(),Is.False);view.HideQuote();Assert.That(Control("storage-filter").Button.IsInteractable(),Is.True);
        }
        [Test] public void SearchShowsAnExplicitEmptyStateAndRestoresCapacityTilesWhenCleared()
        {
            view.Render(new CampMenuPresentation{stacks=new[]{new CampMenuStackView{owner="storage",itemId=CampExpeditionDefinition.ToolZeroId,key="2/0",quantity=1}}});view.ShowPage("storage");Focus("cargo:0");view.storageSearch.text="no_owned_match_123";Assert.That(Control("cargo:0").gameObject.activeSelf,Is.False);Assert.That(root.GetComponentsInChildren<Text>().Any(text=>text.text=="No matching items."),Is.True);Assert.That(events.currentSelectedGameObject.activeInHierarchy,Is.True);view.storageSearch.text="";Assert.That(Control("cargo:23").gameObject.activeSelf,Is.True);
        }
        [Test] public void TheClickableSelectAndBackLegendsTargetTheTopmostStorageChoice()
        {
            view.ShowPage("storage");Click("storage-sort");view.SetStorageChoiceReady();Focus("storage-choice:1");Click("hint:select");Assert.That(view.StorageChoiceOpen,Is.False);Assert.That(Control("storage-sort").title.text,Does.StartWith("Quantity"));Click("storage-filter");view.SetStorageChoiceReady();Click("hint:back");Assert.That(view.StorageChoiceOpen,Is.False);Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("storage-filter").gameObject));
        }
        [TestCase(9,false)][TestCase(9,true)][TestCase(CampMenuGarmentPockets.MaximumAuthoredCount,true)]
        public void PocketLabelsReflowAfterTextSizeChangesWithoutShrinkingBelowTheSecondaryFloor(long count,bool large)
        {
            garment.pockets=count;view.Render(new CampMenuPresentation{stacks=new[]{Owned()}});view.ShowInventory("booter");Focus("equip:chest");view.SetLargeText(large);view.SetPocketPage("chest",long.MaxValue);Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();
            foreach(var control in root.GetComponentsInChildren<CampMenuControl>().Where(control=>control.key.StartsWith("pocket:empty:",StringComparison.Ordinal))){Assert.That(control.title.fontSize,Is.GreaterThanOrEqualTo(14));Assert.That(control.title.preferredHeight,Is.LessThanOrEqualTo(control.title.rectTransform.rect.height+1));}
        }
        [Test] public void ReopeningAFilterRevealsTheChosenCategoryInsideThePopupViewport()
        {
            var ids=data.items.Where(item=>!string.IsNullOrEmpty(item.category)).GroupBy(item=>item.category).Select(group=>group.First()).ToArray();view.Render(new CampMenuPresentation{stacks=ids.Select((item,index)=>new CampMenuStackView{owner="storage",itemId=item.id,key="2/"+index,quantity=1}).ToArray()});view.ShowPage("storage");Click("storage-filter");view.SetStorageChoiceReady();int index=ids.Length;Click("storage-choice:"+index);Click("storage-filter");view.SetStorageChoiceReady();Canvas.ForceUpdateCanvases();var selected=events.currentSelectedGameObject;var scroll=selected.GetComponentInParent<ScrollRect>();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,selected.transform);Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-1));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+1));
        }
        [Test] public void ReadyOptionsUseEnabledColoursAndRemovedCategoriesCannotRepurposeFocusedRows()
        {
            var ids=data.items.Where(item=>!string.IsNullOrEmpty(item.category)).GroupBy(item=>item.category).Select(group=>group.First()).ToArray();CampMenuPresentation Model(CampMenuCatalogItem[] items)=>new(){stacks=items.Select((item,index)=>new CampMenuStackView{owner="storage",itemId=item.id,key="2/"+index,quantity=1}).ToArray()};view.Render(Model(ids));view.ShowPage("storage");Click("storage-filter");view.SetStorageChoiceReady();Focus("storage-choice:1");Assert.That(Control("storage-choice:1").title.color,Is.EqualTo(view.style.ink));var category=Control("storage-choice:1").title.text;view.Render(Model(ids.Where(item=>item.category!=category).ToArray()));Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("storage-choice:0").gameObject));Assert.That(Control("storage-choice:0").Button.IsInteractable(),Is.False);view.SetStorageChoiceReady();Assert.That(Control("storage-choice:0").Button.IsInteractable(),Is.True);
        }
        [TestCase(-1)][TestCase(0)] public void EquipmentHeadingsReflowInLargeTextWithoutAnOpenPocketPreview(long count)
        {
            garment.pockets=count;view.Render(new CampMenuPresentation{stacks=new[]{Owned()}});view.ShowInventory("booter");view.SetLargeText(true);Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();foreach(var control in root.GetComponentsInChildren<CampMenuControl>().Where(control=>control.key.StartsWith("equip:",StringComparison.Ordinal)&&control.cornerLabel!=null))Assert.That(control.cornerLabel.preferredHeight,Is.LessThanOrEqualTo(control.cornerLabel.rectTransform.rect.height+1),control.key);
        }
    }
}
