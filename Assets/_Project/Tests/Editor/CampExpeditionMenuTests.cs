using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampExpeditionMenuTests
    {
        private GameObject instance,eventObject;
        private CampExpeditionMenuView view;
        [SetUp] public void Setup()
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab");Assert.That(asset,Is.Not.Null);
            instance=Object.Instantiate(asset);eventObject=new GameObject("Menu fixture EventSystem",typeof(EventSystem));view=instance.GetComponent<CampExpeditionMenuView>();view.Initialize(eventObject.GetComponent<EventSystem>());
        }
        [TearDown] public void Cleanup(){instance.SetActive(false);Object.DestroyImmediate(instance);Object.DestroyImmediate(eventObject);}
        private CampMenuControl Control(string key)=>instance.GetComponentsInChildren<CampMenuControl>(true).Single(c=>c.key==key);
        [Test] public void LongInspectorTitlesHaveTheirOwnControllerReachableScrollRegion()
        {
            view.ShowInventory("booter");view.Inspect(CampExpeditionDefinition.ToolZeroId);view.SetLargeText(true);view.inspectorTitle.text=string.Join(" ",Enumerable.Repeat("Very long inspected item name",20));view.RefreshLayout();Canvas.ForceUpdateCanvases();
            var control=Control("inspector-title");var scroll=control.GetComponent<ScrollRect>();Assert.That(control.gameObject.activeInHierarchy,Is.True);Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(control.gameObject);scroll.verticalNormalizedPosition=1;view.ScrollInspector(-1,1);Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(0).Within(.0001f));
        }
        [Test] public void CarriedGearHasFourReachablePositionsAndUsesTheExistingOwnedSlot()
        {
            var stack=new CampMenuStackView{key="0/14",slotIndex=14,owner="booter",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1};
            view.Render(new CampMenuPresentation{stacks=new[]{stack}});view.ShowInventory("booter");
            var carried=Control("carried:3");Assert.That(carried.title.text,Is.EqualTo(view.definition.Find(stack.itemId).DisplayName));
            var events=eventObject.GetComponent<EventSystem>();events.SetSelectedGameObject(carried.gameObject);Canvas.ForceUpdateCanvases();
            Assert.That(view.SelectedStack,Is.SameAs(stack));var scroll=carried.GetComponentInParent<ScrollRect>();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,carried.transform);
            Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-1));
            Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(scroll.viewport.rect.xMin-1));Assert.That(bounds.max.x,Is.LessThanOrEqualTo(scroll.viewport.rect.xMax+1));
            Assert.That(carried.title.preferredHeight,Is.LessThanOrEqualTo(carried.title.rectTransform.rect.height+1));
            string command=null;view.Command+=key=>command=key;carried.Button.onClick.Invoke();Assert.That(command,Is.EqualTo("equip:carried-3"));
            Assert.That(CampExpeditionLayout.Index("carried-3"),Is.EqualTo(14));Assert.That(CampExpeditionLayout.Index("carried-4"),Is.EqualTo(-1));Assert.That(CampExpeditionLayout.Address(14),Is.Null);
        }
        [Test] public void EachCarrierRestoresItsOwnFocusAndEquipmentScroll()
        {
            view.ShowInventory("booter");var events=eventObject.GetComponent<EventSystem>();events.SetSelectedGameObject(Control("carried:3").gameObject);
            var scroll=Control("carried:3").GetComponentInParent<ScrollRect>();var position=scroll.content.anchoredPosition;
            view.ShowInventory("legger");events.SetSelectedGameObject(Control("cargo:8").gameObject);view.ShowInventory("booter");
            Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("carried:3").gameObject));Assert.That(scroll.content.anchoredPosition.y,Is.EqualTo(position.y).Within(1));
            view.ShowInventory("legger");Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("cargo:8").gameObject));
        }
        [Test] public void PickerAndNestedQuoteContainMouseWheelScrollingAndRestoreThePriorStates()
        {
            view.ShowInventory("booter");var equipment=Control("carried:0").GetComponentInParent<ScrollRect>();Assert.That(equipment.enabled,Is.True);
            view.ShowPicker("tool",Array.Empty<CampMenuStackView>());Assert.That(equipment.enabled,Is.False);
            var candidates=Control("candidate:0").GetComponentInParent<ScrollRect>();Assert.That(candidates.enabled,Is.True);
            view.ShowQuote("Save waiting",allowCancel:false);Assert.That(candidates.enabled,Is.False);view.HideQuote();Assert.That(candidates.enabled,Is.True);Assert.That(equipment.enabled,Is.False);
            view.Back();Assert.That(equipment.enabled,Is.True);
        }
        [Test] public void StorageOffersPossessedCategoriesAndQuantitySortWithoutMutatingTheModel()
        {
            var rows=new[]{new CampMenuStackView{key="2/0",owner="storage",itemId="resource.ironstone_ore",quantity=2},new CampMenuStackView{key="2/1",owner="storage",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}};
            view.Render(new CampMenuPresentation{stacks=rows});view.ShowPage("storage");Control("storage-sort").Button.onClick.Invoke();
            view.SetStorageChoiceReady();Control("storage-choice:1").Button.onClick.Invoke();
            Assert.That(Control("storage-sort").title.text,Is.EqualTo("Quantity ▾"));Assert.That(Control("cargo:0").title.text,Is.EqualTo(view.definition.Find(rows[0].itemId).DisplayName));
            Control("storage-filter").Button.onClick.Invoke();view.SetStorageChoiceReady();Control("storage-choice:1").Button.onClick.Invoke();Assert.That(Control("storage-filter").title.text,Is.Not.EqualTo("All categories ▾"));Assert.That(Control("cargo:1").gameObject.activeSelf,Is.False);
            Assert.That(rows.Select(r=>r.quantity),Is.EqualTo(new[]{2,1}));
        }
        [Test] public void InspectorUsesTheSelectedInstancesContentsAndLeggerUsesTheReadModelsLimits()
        {
            var stack=new CampMenuStackView{key="1/0",owner="legger",itemId=CampExpeditionDefinition.PhagiaId,quantity=1,contentsSummary="Phagia: 14.5 / 80.0"};
            view.Render(new CampMenuPresentation{stacks=new[]{stack},leggerCapacity=16,leggerMass=17.5f,leggerMaximumMass=92,leggerAccessible=false});view.ShowInventory("legger");
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(Control("cargo:0").gameObject);
            Assert.That(view.inspectorBody.text,Does.Contain("14.5 / 80.0"));Assert.That(view.cargoSummary.text,Does.Contain("1 / 16").And.Contain("17.5 / 92.0").And.Contain("Out of reach"));Assert.That(Control("cargo:15").gameObject.activeSelf,Is.True);
        }
        [Test] public void PickerContainsPointerAndDirectionalControlsAndRestoresThemOnCancel()
        {
            view.ShowInventory("booter");var events=eventObject.GetComponent<EventSystem>();
            view.ShowPicker("tool",new[]{new CampMenuStackView{key="one",owner="legger",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}});
            Assert.That(Control("equip:frame").Button.IsInteractable(),Is.False);Assert.That(Control("primary:play").Button.IsInteractable(),Is.False);
            Assert.That(Control("candidate:0").Button.navigation.selectOnLeft,Is.Null);Assert.That(Control("candidate:0").Button.navigation.selectOnRight,Is.Null);
            Control("primary:play").Button.onClick.Invoke();view.ShowInventory("legger");Assert.That(view.Page,Is.EqualTo("inventory"));Assert.That(view.Owner,Is.EqualTo("booter"));
            view.Back();Assert.That(Control("equip:frame").Button.IsInteractable(),Is.True);Assert.That(Control("primary:play").Button.IsInteractable(),Is.True);
            Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("equip:tool").gameObject));
        }
        [Test] public void PickerPreviewsDistinctCopiesAndRemembersTheLastBrowsedOwnedCopy()
        {
            view.ShowInventory("booter");var rows=Enumerable.Range(0,2).Select(i=>new CampMenuStackView{key="copy-"+i,owner="legger",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}).ToArray();
            view.ShowPicker("tool",rows);Assert.That(Control("candidate:0").subtitle.text,Does.Contain("Copy 1"));Assert.That(Control("candidate:1").subtitle.text,Does.Contain("Copy 2"));
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(Control("candidate:1").gameObject);
            Assert.That(instance.GetComponentsInChildren<Text>().Single(t=>t.name=="Candidate preview").text,Does.Contain(view.definition.Find(rows[0].itemId).DisplayName));
            view.Back();view.ShowPicker("tool",rows);Assert.That(eventObject.GetComponent<EventSystem>().currentSelectedGameObject,Is.SameAs(Control("candidate:1").gameObject));
        }
        [Test] public void BrowsingCargoDoesNotCommitItsAmberSelection()
        {
            var rows=new[]{new CampMenuStackView{key="ore",owner="legger",itemId="resource.ironstone_ore",quantity=1},new CampMenuStackView{key="tool",owner="legger",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}};
            view.Render(new CampMenuPresentation{stacks=rows});view.ShowInventory("legger");var events=eventObject.GetComponent<EventSystem>();
            events.SetSelectedGameObject(Control("cargo:0").gameObject);Assert.That(Control("cargo:0").IsSelected,Is.False);
            Control("cargo:0").Button.onClick.Invoke();Assert.That(Control("cargo:0").IsSelected,Is.True);
            events.SetSelectedGameObject(Control("cargo:1").gameObject);Assert.That(Control("cargo:0").IsSelected,Is.True);Assert.That(Control("cargo:1").IsSelected,Is.False);
        }
        [Test] public void OutpostAndStartRetainTheirOwnNavigationContexts()
        {
            view.ShowPage("jobs");view.ShowPage("play");Control("primary:outpost").Button.onClick.Invoke();Assert.That(view.Page,Is.EqualTo("jobs"));
            view.ShowInventory("legger");view.ShowPage("start");view.Back();Assert.That(view.Page,Is.EqualTo("inventory"));Assert.That(view.Owner,Is.EqualTo("legger"));
        }
        [Test] public void SaveRetryDialogHasSpecificCopyAndNoMisleadingCancelAction()
        {
            view.ShowQuote("The write needs retry.","","SAVE WAITING","Retry save",false);
            Assert.That(Control("quote-confirm").title.text,Is.EqualTo("Retry save"));Assert.That(Control("quote-cancel").gameObject.activeSelf,Is.False);
            Assert.That(eventObject.GetComponent<EventSystem>().currentSelectedGameObject,Is.SameAs(Control("quote-confirm").gameObject));
            view.HideQuote();view.ShowQuote("Store equipment");Assert.That(Control("quote-confirm").title.text,Is.EqualTo("Confirm transfer"));Assert.That(Control("quote-cancel").gameObject.activeSelf,Is.True);
        }
        [Test] public void PickerDetailsStayAboveBackAndScrollFromTheirOwnControllerFocus()
        {
            view.ShowInventory("booter");view.ShowPicker("tool",new[]{new CampMenuStackView{key="one",owner="legger",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}});
            var details=Control("picker-details");var scroll=details.GetComponent<ScrollRect>();var text=scroll.content.GetComponent<Text>();
            text.text=string.Join("\n",Enumerable.Repeat("A long equipment description.",40));LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);Canvas.ForceUpdateCanvases();
            var parent=(RectTransform)details.transform.parent;
            var detailsCorners=new Vector3[4];var backCorners=new Vector3[4];
            ((RectTransform)details.transform).GetWorldCorners(detailsCorners);((RectTransform)Control("back").transform).GetWorldCorners(backCorners);
            // Measure the clipping viewport itself, not its intentionally scrollable child text.
            Assert.That(detailsCorners.Select(point=>parent.InverseTransformPoint(point).y).Min(),Is.GreaterThan(backCorners.Select(point=>parent.InverseTransformPoint(point).y).Max()));
            Assert.That(Control("candidate:0").Button.navigation.selectOnDown,Is.SameAs(details.Button));
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(details.gameObject);scroll.verticalNormalizedPosition=1;
            view.ScrollInspector(-1,.5f);Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.5f).Within(.01f));
        }
        [Test] public void ReusingACargoAddressForAnotherItemClearsItsConfirmedHighlight()
        {
            view.Render(new CampMenuPresentation{stacks=new[]{new CampMenuStackView{key="1/0",owner="legger",itemId="resource.ironstone_ore",quantity=1}}});view.ShowInventory("legger");
            Control("cargo:0").Button.onClick.Invoke();Assert.That(Control("cargo:0").IsSelected,Is.True);
            view.Render(new CampMenuPresentation{stacks=new[]{new CampMenuStackView{key="1/0",owner="legger",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}}});
            Assert.That(Control("cargo:0").IsSelected,Is.False);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void InventoryLegendShowsNorthYInsteadOfTheUnrelatedViewButton(bool large)
        {
            view.SetLargeText(large);
            var bindings=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions").FindAction("System/ToggleInventory",true).bindings;
            Assert.That(bindings.Any(binding=>binding.path=="<Gamepad>/buttonNorth"),Is.True);
            var prompt=Control("page:inventory");var glyph=prompt.transform.Find("Inventory button glyph");Assert.That(glyph,Is.Not.Null);Assert.That(glyph.gameObject.activeInHierarchy,Is.True);
            Assert.That(glyph.Find("North button").GetComponent<Text>().text,Is.EqualTo("Y"));
            Assert.That(glyph.Find("Ring").GetComponent<Text>().text,Is.EqualTo("○"));
            Assert.That(glyph.GetComponentsInChildren<Graphic>().All(graphic=>!graphic.raycastTarget),Is.True);
            Assert.That(prompt.GetComponentsInChildren<Image>().Any(image=>image.enabled&&image.sprite==view.style.FindGlyph("prompt-view")),Is.False,"The old View icon must not remain visible.");
        }
        [Test] public void NativeMenuHasTheExactPrimaryAndCraftingServiceOrder()
        {
            var primary=instance.transform.Find("Safe Area/Primary").GetComponentsInChildren<CampMenuControl>(true).Select(c=>c.title.text);CollectionAssert.AreEqual(new[]{"Play","Outpost","Crafting"},primary);
            var authored=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab");
            var crafting=authored.transform.Find("Safe Area/CraftingTabs").GetComponentsInChildren<CampMenuControl>(true).Select(c=>c.title.text);CollectionAssert.AreEqual(new[]{"Workshops","Crafting tree","Booter equipment","Tracked items","Legger equipment","Tracked projects"},crafting);
            var displayed=instance.transform.Find("Safe Area/CraftingTabs").GetComponentsInChildren<CampMenuControl>(true).OrderBy(c=>((RectTransform)c.transform).anchoredPosition.x).ToArray();
            CollectionAssert.AreEqual(new[]{"page:workstations","page:tree","page:crafting-equipment","page:tracked","page:legger-equipment","page:projects"},displayed.Select(c=>c.key));
            Assert.That(displayed[0].title.text,Is.EqualTo("Workshops"));
            Assert.That(instance.GetComponentsInChildren<EventSystem>(true),Is.Empty);Assert.That(instance.GetComponent<Canvas>().renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        }
        [Test] public void PrimaryAndSecondaryTabsStayClickableOutsideDirectionalNavigation()
        {
            var tabs=instance.GetComponentsInChildren<CampMenuControl>(true).Where(c=>c.tab).ToArray();
            Assert.That(tabs.Length,Is.GreaterThan(3),"Cover both primary and secondary tabs.");
            foreach(var tab in tabs)
            {
                Assert.That(tab.Button.navigation.mode,Is.EqualTo(Navigation.Mode.None),tab.key);
                Assert.That(tab.Button.interactable,Is.True,tab.key+" must remain clickable.");
            }
            Control("primary:crafting").Button.onClick.Invoke();Assert.That(view.Page,Is.EqualTo("workstations"));
            Control("page:tree").Button.onClick.Invoke();Assert.That(view.Page,Is.EqualTo("tree"));
            view.ShowInventory("booter");Control("carrier:legger").Button.onClick.Invoke();Assert.That(view.Owner,Is.EqualTo("legger"));
        }
        [Test] public void NoFrameHidesMountsAndStarterFixtureShowsOnlyOneOccupiedMount()
        {
            view.Render(new CampMenuPresentation{attachmentMounts=0,occupiedMounts=0});view.ShowInventory("booter");Assert.That(Control("equip:attachment-0").gameObject.activeSelf,Is.False);Assert.That(Control("equip:attachment-1").gameObject.activeSelf,Is.False);
            view.Render(new CampMenuPresentation{stacks=new[]{new CampMenuStackView{key="canister",owner="booter",equipmentAddress="attachment-0",itemId=CampExpeditionDefinition.PhagiaId,quantity=1}}});
            Assert.That(Control("equip:attachment-0").title.text,Is.EqualTo(view.definition.Find(CampExpeditionDefinition.PhagiaId).DisplayName));Assert.That(Control("equip:attachment-1").title.text,Is.EqualTo("Empty"));
        }
        [Test] public void FieldCannotNavigateToTownStorage()
        {
            var commands=new List<string>();view.Command+=commands.Add;view.Render(new CampMenuPresentation{townAccess=false});view.ShowPage("storage");Assert.That(view.Page,Is.EqualTo("play"));Assert.That(commands,Does.Contain("restricted:town"));
        }
        [Test] public void QuoteIsModalAndDefaultsToCancellation()
        {
            view.Render(new CampMenuPresentation{canPlay=true});view.ShowQuote("Two selected stacks","1 slot · 1 Commons");Assert.That(Control("play").Button.interactable,Is.False);Assert.That(Control("quote-confirm").Button.interactable,Is.True);
            Assert.That(eventObject.GetComponent<EventSystem>().currentSelectedGameObject,Is.EqualTo(Control("quote-cancel").gameObject));
            view.Render(new CampMenuPresentation{canPlay=true});Assert.That(Control("play").Button.interactable,Is.False);view.HideQuote();Assert.That(Control("play").Button.interactable,Is.True);
        }
        [Test] public void ClosingPickerRestoresItsEquipmentFocusWithoutACommand()
        {
            view.ShowInventory("booter");var commands=new List<string>();view.Command+=commands.Add;view.ShowPicker("tool",Array.Empty<CampMenuStackView>());view.Back();Assert.That(eventObject.GetComponent<EventSystem>().currentSelectedGameObject,Is.EqualTo(Control("equip:tool").gameObject));Assert.That(commands.All(s=>s.StartsWith("inspect:")),Is.True);
        }
        [TestCase(true)]
        [TestCase(false)]
        public void QuoteAlsoGatesAndRestoresTheStorageSearchField(bool searchWasEnabled)
        {
            view.ShowPage("storage");view.storageSearch.interactable=searchWasEnabled;view.ShowQuote("Confirm transfer");
            Assert.That(view.storageSearch.interactable,Is.False);view.Render(new CampMenuPresentation{canPlay=true});Assert.That(view.storageSearch.interactable,Is.False);
            view.HideQuote();Assert.That(view.storageSearch.interactable,Is.EqualTo(searchWasEnabled));
        }
        [Test] public void RenderPreservesTheSelectedSearchField()
        {
            view.ShowPage("storage");var events=eventObject.GetComponent<EventSystem>();events.SetSelectedGameObject(view.storageSearch.gameObject);
            view.Render(new CampMenuPresentation());Assert.That(events.currentSelectedGameObject,Is.SameAs(view.storageSearch.gameObject));
        }
        [Test] public void ReturningToPlayDefaultsToThePlayActionInsteadOfItsPreviouslySelectedTab()
        {
            view.Render(new CampMenuPresentation{canPlay=true});var events=eventObject.GetComponent<EventSystem>();
            events.SetSelectedGameObject(Control("primary:play").gameObject);view.ShowPage("storage");view.ShowPage("play");
            Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("play").gameObject));
        }
        [Test] public void SlotsAddedDuringAQuotedStorageUpgradeAreUsableAfterClosingTheQuote()
        {
            view.ShowPage("storage");view.ShowQuote("Additional storage");view.Render(new CampMenuPresentation{storageCapacity=64});
            Assert.That(Control("cargo:63").Button.interactable,Is.False);view.HideQuote();Assert.That(Control("cargo:63").Button.IsInteractable(),Is.True);
        }
        [Test] public void FocusedCargoRebindsAfterItsContentsChange()
        {
            var first=new CampMenuStackView{key="1/0",owner="legger",itemId="resource.ironstone_ore",quantity=1};
            var next=new CampMenuStackView{key="1/1",owner="legger",itemId="proposal.bio.shell",quantity=1};
            view.Render(new CampMenuPresentation{stacks=new[]{first,next}});view.ShowInventory("legger");eventObject.GetComponent<EventSystem>().SetSelectedGameObject(Control("cargo:0").gameObject);
            Assert.That(view.SelectedStack,Is.SameAs(first));view.Render(new CampMenuPresentation{stacks=new[]{next}});
            Assert.That(view.SelectedStack,Is.SameAs(next));Assert.That(view.InspectedItem,Is.EqualTo(next.itemId));
            view.Render(new CampMenuPresentation());Assert.That(view.SelectedStack,Is.Null);Assert.That(view.InspectedItem,Is.Null);
        }
        [TestCase("storage")]
        [TestCase("review")]
        [TestCase("inventory")]
        public void CargoPagesAdvertiseTheImplementedWestTransferAction(string page)
        {
            if(page=="inventory")view.ShowInventory("legger");else view.ShowPage(page);
            Assert.That(Control("hint:west").gameObject.activeInHierarchy,Is.True);Assert.That(Control("hint:west").title.text,Is.EqualTo("Transfer"));
        }
        [Test] public void StorageGrowthRemainsBrowsableAndReturningToCargoHidesExtraSlots()
        {
            view.SetLargeText(true);view.Render(new CampMenuPresentation{storageCapacity=64});view.ShowPage("storage");Assert.That(Control("cargo:63").title.fontSize,Is.EqualTo(Control("cargo:0").title.fontSize));Assert.That(Control("cargo:63").gameObject.activeSelf,Is.True);view.ShowInventory("legger");Assert.That(Control("cargo:63").gameObject.activeSelf,Is.False);Assert.That(Control("cargo:11").gameObject.activeSelf,Is.True);
        }
        [Test] public void StorageSearchFiltersOnlyPresentationAndDoesNotChangeTheReadModel()
        {
            var stacks=new[]{new CampMenuStackView{key="store-one",owner="storage",quantity=2,itemId="resource.ironstone_ore"},new CampMenuStackView{key="store-two",owner="storage",quantity=1,itemId="proposal.bio.shell"}};
            view.Render(new CampMenuPresentation{stacks=stacks});view.ShowPage("storage");view.storageSearch.text="Ironstone";Assert.That(Control("cargo:0").title.text,Is.EqualTo(view.definition.Find("resource.ironstone_ore").DisplayName));Assert.That(Control("cargo:1").title.text,Is.EqualTo("+"));Assert.That(stacks.Length,Is.EqualTo(2));Assert.That(stacks[1].quantity,Is.EqualTo(1));
        }
        [Test] public void TrackedBookmarksAndProjectDemandHaveSeparateBindings()
        {
            view.Render(new CampMenuPresentation{tracked=new[]{"author.bio.stone_bone"},planning=new CampMenuPlanningView{projects=new[]{new CampMenuProjectView{rootId=CampExpeditionDefinition.ToolOneId,remaining=2}}}});view.ShowPage("tracked");Assert.That(Control("bookmark:0").title.text,Is.EqualTo(view.definition.Find("author.bio.stone_bone").DisplayName));Assert.That(view.ProjectCount,Is.EqualTo(1));
            view.Inspect("author.bio.stone_bone");Assert.That(Control("track").title.text,Is.EqualTo("Untrack item"));view.Render(new CampMenuPresentation());Assert.That(Control("track").gameObject.activeSelf,Is.False);
        }
        [Test] public void FooterCommandsWorkWithoutJoiningDirectionalNavigation()
        {
            view.Render(new CampMenuPresentation{canPlay=true});view.ShowPage("play");var commands=new List<string>();view.Command+=commands.Add;
            var events=eventObject.GetComponent<EventSystem>();events.SetSelectedGameObject(Control("play").gameObject);
            foreach(var key in new[]{"hint:select","hint:west","hint:tabs","hint:subtabs","hint:back"})
            {Assert.That(Control(key).Button.interactable,Is.True);Assert.That(Control(key).Button.navigation.mode,Is.EqualTo(Navigation.Mode.None));}
            Control("hint:select").Button.onClick.Invoke();Assert.That(commands.Count(c=>c=="play"),Is.EqualTo(1));
            Control("hint:west").Button.onClick.Invoke();Control("hint:tabs").Button.onClick.Invoke();Control("hint:subtabs").Button.onClick.Invoke();
            CollectionAssert.IsSubsetOf(new[]{"shortcut:west","shortcut:primary","shortcut:context"},commands);
            view.ShowInventory("booter");view.ShowPicker("tool",Array.Empty<CampMenuStackView>());Control("hint:back").Button.onClick.Invoke();Assert.That(view.PickerTarget,Is.Null);
        }
        [Test] public void PickerFocusStartsOnAnAvailableCandidate()
        {
            view.ShowInventory("booter");view.ShowPicker("tool",new[]{new CampMenuStackView{key="tool",owner="legger",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}});
            Assert.That(eventObject.GetComponent<EventSystem>().currentSelectedGameObject,Is.SameAs(Control("candidate:0").gameObject));
        }
        [Test] public void PickerDisplaysAllCandidatesAndRevealsTheLastThenResetsForAShorterList()
        {
            var candidates=Enumerable.Range(0,40).Select(i=>new CampMenuStackView{key="gear-"+i,owner="storage",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}).ToArray();
            view.SetLargeText(true);view.ShowInventory("booter");view.ShowPicker("tool",candidates);Canvas.ForceUpdateCanvases();
            var last=Control("candidate:39");Assert.That(last.gameObject.activeInHierarchy,Is.True);
            Assert.That(last.title.fontSize,Is.EqualTo(Control("candidate:0").title.fontSize));
            var scroll=last.GetComponentInParent<ScrollRect>();Assert.That(scroll,Is.Not.Null);Assert.That(scroll.vertical&&!scroll.horizontal,Is.True);
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(last.gameObject);Canvas.ForceUpdateCanvases();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,last.transform);
            Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-1));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+1));
            string detail=$"viewport {scroll.viewport.rect}, content {scroll.content.rect} at {scroll.content.anchoredPosition}, card {((RectTransform)last.transform).rect}, bounds {bounds}, cell {scroll.content.GetComponent<GridLayoutGroup>().cellSize}";
            Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(scroll.viewport.rect.xMin-1),detail);Assert.That(bounds.max.x,Is.LessThanOrEqualTo(scroll.viewport.rect.xMax+1),detail);
            string selected=null;view.Command+=key=>selected=key;last.Button.onClick.Invoke();Assert.That(selected,Is.EqualTo("candidate:39"));
            view.ShowPicker("tool",candidates.Take(1).ToArray());Assert.That(last.gameObject.activeSelf,Is.False);Assert.That(eventObject.GetComponent<EventSystem>().currentSelectedGameObject,Is.SameAs(Control("candidate:0").gameObject));
        }
        [Test] public void InspectorScrollTargetsTheOpenPickerAndQuotesTakePriority()
        {
            view.ShowInventory("booter");view.ShowPicker("tool",Enumerable.Range(0,40).Select(i=>new CampMenuStackView{key="gear-"+i,owner="storage",itemId=CampExpeditionDefinition.ToolZeroId,quantity=1}).ToArray());
            var scroll=Control("candidate:0").GetComponentInParent<ScrollRect>();scroll.verticalNormalizedPosition=1;
            view.ScrollInspector(-1,.5f);Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.5f).Within(.01f));
            view.ShowQuote("Confirm transfer");view.ScrollInspector(-1,.5f);Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.5f).Within(.01f));
        }
        [TestCase("tree","tree-group:")]
        [TestCase("workstations","station:")]
        public void SwitchingPagesAlwaysLeavesUsableGamepadFocus(string page,string prefix)
        {
            view.Render(new CampMenuPresentation{canPlay=true});view.ShowPage("play");view.ShowPage(page);
            var selected=eventObject.GetComponent<EventSystem>().currentSelectedGameObject;Assert.That(selected,Is.Not.Null);
            var control=selected.GetComponent<CampMenuControl>();Assert.That(control,Is.Not.Null);Assert.That(selected.activeInHierarchy&&control.Button.IsInteractable(),Is.True);Assert.That(control.key,Does.StartWith(prefix));
        }
        [TestCase("jobs")]
        public void PagesWithoutContentControlsDoNotFallBackToTabFocus(string page)
        {
            view.Render(new CampMenuPresentation{canPlay=true});view.ShowPage(page);
            Assert.That(eventObject.GetComponent<EventSystem>().currentSelectedGameObject,Is.Null);
        }
        [Test] public void TabHoverCannotReplaceRememberedPageContentFocus()
        {
            view.ShowInventory("booter");var events=eventObject.GetComponent<EventSystem>();
            events.SetSelectedGameObject(Control("equip:tool").gameObject);
            Control("page:inventory").OnPointerEnter(new PointerEventData(events));
            view.ShowPage("workstations");view.ShowInventory("booter");
            Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("equip:tool").gameObject));
        }
        [Test] public void RemovingSelectedBookmarkRepairsFocusToAnAvailableControl()
        {
            view.Render(new CampMenuPresentation{tracked=new[]{CampExpeditionDefinition.ToolZeroId}});view.ShowPage("tracked");
            var events=eventObject.GetComponent<EventSystem>();events.SetSelectedGameObject(Control("bookmark:0").gameObject);
            view.Render(new CampMenuPresentation());var selected=events.currentSelectedGameObject;
            Assert.That(selected,Is.Not.Null);Assert.That(selected.activeInHierarchy,Is.True);Assert.That(selected.GetComponent<Button>().IsInteractable(),Is.True);
        }
        [Test] public void SelectingAnOffscreenStorageSlotScrollsItIntoView()
        {
            view.Render(new CampMenuPresentation{storageCapacity=64});view.ShowPage("storage");Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();
            var target=Control("cargo:63");var scroll=target.GetComponentInParent<ScrollRect>();Assert.That(scroll,Is.Not.Null);
            var first=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,Control("cargo:0").transform);
            Assert.That(first.max.y,Is.EqualTo(scroll.viewport.rect.yMax).Within(1),"Cargo must open at its first row, without an empty gap above it.");
            scroll.content.anchoredPosition=Vector2.zero;Canvas.ForceUpdateCanvases();
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(target.gameObject);Canvas.ForceUpdateCanvases();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,target.transform);var viewport=scroll.viewport.rect;
            Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(viewport.yMin-1));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(viewport.yMax+1));
        }
        [Test] public void ReverseUseFilterPreservesIndependentBookmarksAndProjectDemand()
        {
            var tracked=new[]{CampExpeditionDefinition.ToolZeroId,"author.bio.stone_bone"};
            view.Render(new CampMenuPresentation{tracked=tracked,planning=new CampMenuPlanningView{projects=new[]{new CampMenuProjectView{rootId=CampExpeditionDefinition.ToolOneId,remaining=2}}}});view.ShowPage("tracked");
            Control("tracked-filter:"+CampExpeditionDefinition.ToolZeroId).Button.onClick.Invoke();
            Assert.That(Control("bookmark:0").title.text,Is.EqualTo(view.definition.Find("author.bio.stone_bone").DisplayName));Assert.That(view.TrackedVisibleIds,Has.Length.EqualTo(1));
            Control("tracked-filter:all").Button.onClick.Invoke();Assert.That(Control("bookmark:1").gameObject.activeSelf,Is.True);Assert.That(tracked.Length,Is.EqualTo(2));Assert.That(view.ProjectCount,Is.EqualTo(1));
        }
    }
}
