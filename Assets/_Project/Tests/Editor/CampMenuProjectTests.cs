using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampMenuProjectTests
    {
        private GameObject root,eventRoot;
        private CampExpeditionMenuView view;
        private CampExpeditionDefinition definition;
        private TopDown3DInventoryState booter,legger,storage;
        private EventSystem events;
        private readonly List<string> commands=new();
        private CampMenuControl[] Controls=>root.GetComponentsInChildren<CampMenuControl>(true);
        private CampMenuControl Control(string key)=>Controls.Single(control=>control.key==key);
        [SetUp] public void Setup()
        {
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab"));eventRoot=new GameObject("Project fixture",typeof(EventSystem));events=eventRoot.GetComponent<EventSystem>();view=root.GetComponent<CampExpeditionMenuView>();view.Initialize(events);definition=view.definition;
            booter=new TopDown3DInventoryState(definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,CampExpeditionLayout.Capacity,expeditionLayout:true));
            legger=new TopDown3DInventoryState(definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12));storage=new TopDown3DInventoryState(definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,24,campStorage:true));view.Command+=commands.Add;
        }
        [TearDown] public void Cleanup(){root.SetActive(false);Object.DestroyImmediate(root);Object.DestroyImmediate(eventRoot);commands.Clear();}
        private CampProjectRecord Plan(string id=null,int quantity=1)=>new(){rootId=id??definition.rootId,remaining=quantity};
        private CampMenuPlanningView Read(CampProjectRecord[] records,bool cargo=true,bool town=true)=>CampMenuProjects.Read(definition,booter,legger,storage,records,cargo,town);
        private void Render(params CampProjectRecord[] records){view.Render(new CampMenuPresentation{planning=Read(records)});view.ShowPage("projects");Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();}
        private InputField Quantity(string id=null)=>root.GetComponentsInChildren<InputField>(true).Single(field=>field.name=="Project quantity "+(id??definition.rootId));

        [Test] public void EmptyProjectsHaveAReachableWorkshopRouteAndNoInventedDemand()
        {
            Render();Assert.That(view.ProjectCount,Is.Zero);Assert.That(Read(Array.Empty<CampProjectRecord>()).materialsAccounted,Is.False);Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("project-browse").gameObject));
            Control("project-browse").Button.onClick.Invoke();Assert.That(view.Page,Is.EqualTo("workstations"));Assert.That(booter.OccupiedSlotCount+legger.OccupiedSlotCount+storage.OccupiedSlotCount,Is.Zero);
        }
        [Test] public void AdditionalOutputsIgnoreFinishedStockAndReturnDetachedRequirementCopies()
        {
            var records=new[]{Plan(quantity:2)};var before=Read(records);Assert.That(storage.TryAdd(new TopDown3DItemAmount(definition.rootId,1)).Succeeded,Is.True);var after=Read(records);
            CollectionAssert.AreEquivalent(before.requirements.Select(row=>row.itemId+":"+row.missing),after.requirements.Select(row=>row.itemId+":"+row.missing));Assert.That(after.projects[0].inputs[0].quantity,Is.EqualTo(definition.Find(definition.rootId).inputs[0].quantity*2));
            after.projects[0].inputs[0].quantity=999;Assert.That(definition.Find(definition.rootId).inputs[0].quantity,Is.Not.EqualTo(999));Assert.That(records[0].remaining,Is.EqualTo(2));
        }
        [Test] public void KnownOwnedAndEligibleSuppliesRespectTownAndCargoAccess()
        {
            const string ore="resource.ironstone_ore";Assert.That(legger.TryAdd(new TopDown3DItemAmount(ore,1)).Succeeded,Is.True);Assert.That(storage.TryAdd(new TopDown3DItemAmount(ore,2)).Succeeded,Is.True);
            var unavailable=Read(new[]{Plan()},false,false).requirements.Single(row=>row.itemId==ore);var available=Read(new[]{Plan()}).requirements.Single(row=>row.itemId==ore);
            Assert.That(unavailable.knownOwned,Is.EqualTo(3));Assert.That(unavailable.eligible,Is.Zero);Assert.That(unavailable.missing,Is.GreaterThan(available.missing));Assert.That(available.eligible,Is.EqualTo(3));
        }
        [Test] public void PreparedPartsReplaceBranchesAndUnknownRecipesCannotClaimReadiness()
        {
            var initial=Read(new[]{Plan()});var part=definition.Find(definition.rootId).inputs.First(input=>definition.Find(input.itemId).inputs.Length>0);Assert.That(legger.TryAdd(new TopDown3DItemAmount(part.itemId,1)).Succeeded,Is.True);
            var prepared=Read(new[]{Plan()});Assert.That(prepared.requirements.Sum(row=>row.missing),Is.LessThan(initial.requirements.Sum(row=>row.missing)));Assert.That(prepared.requirements.Sum(row=>row.required),Is.EqualTo(initial.requirements.Sum(row=>row.required)));
            var unknown=Read(new[]{Plan("retired.saved.recipe",3)});Assert.That(unknown.unqualified,Is.EqualTo(new[]{"retired.saved.recipe"}));Assert.That(unknown.materialsAccounted,Is.False);Assert.That(unknown.projects[0].qualified,Is.False);Assert.That(unknown.projects[0].remaining,Is.EqualTo(3));
        }
        [Test] public void QuantityButtonsAndEntryOnlyRequestDurableCommandsAndValidateBounds()
        {
            Render(Plan());Assert.That(Control("project:minus:"+definition.rootId).Button.interactable,Is.False);Control("project:plus:"+definition.rootId).Button.onClick.Invoke();Assert.That(commands.Last(),Is.EqualTo("project:set:2:"+definition.rootId));Assert.That(Quantity().text,Is.EqualTo("1"));
            commands.Clear();Quantity().onEndEdit.Invoke("0");Assert.That(commands,Is.Empty);Assert.That(Quantity().text,Is.EqualTo("1"));var validation=root.GetComponentsInChildren<Text>().Single(text=>text.text.Contains("Choose a project quantity"));Assert.That(validation.transform.parent,Is.SameAs(Quantity().transform.parent));
            Quantity().onEndEdit.Invoke("17");Assert.That(commands.Single(),Is.EqualTo("project:set:17:"+definition.rootId));Render(Plan(quantity:99));Assert.That(Control("project:plus:"+definition.rootId).Button.interactable,Is.False);Assert.That(Control("project:plus:"+definition.rootId).title.color,Is.EqualTo(view.style.muted));
        }
        [Test] public void RecipeLinksOpenWorkshopPlansAndTheirTreeLinkClearsStaleSearch()
        {
            const string id="author.tool.arc_powered_rotary_percussion_mining_tool";root.GetComponentsInChildren<InputField>(true).Single(field=>field.name=="Tree search").text="missing_tree_query";Render(Plan(id,4));Control("project:view:"+id).Button.onClick.Invoke();
            Assert.That(view.Page,Is.EqualTo("workstations"));Assert.That(view.WorkshopRecipe,Is.EqualTo(id));Assert.That(Control("workshop:track").Button.interactable,Is.False);Control("workshop:tree").Button.onClick.Invoke();Assert.That(view.Page,Is.EqualTo("tree"));Assert.That(view.DisplayedTreeRoot,Is.EqualTo(id));Assert.That(root.GetComponentsInChildren<InputField>(true).Single(field=>field.name=="Tree search").text,Is.Empty);Assert.That(view.ProjectCount,Is.EqualTo(1));Assert.That(commands,Does.Not.Contain("craft"));
        }
        [Test] public void RemovingRowsDoesNotLeaveDuplicateAddressesOrStaleFocus()
        {
            var other=CampExpeditionDefinition.ToolZeroId;Render(Plan(),Plan(other,2));events.SetSelectedGameObject(Control("project:remove:"+definition.rootId).gameObject);
            view.Render(new CampMenuPresentation{planning=Read(new[]{Plan(other,2)})});Assert.That(Controls.Select(control=>control.key).Distinct().Count(),Is.EqualTo(Controls.Length));Assert.That(events.currentSelectedGameObject.activeInHierarchy,Is.True);Assert.That(Control("project:view:"+other).gameObject.activeInHierarchy,Is.True);
            view.Render(new CampMenuPresentation());Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("project-browse").gameObject));
        }
        [Test] public void ProjectAndGatheringPanesHaveIndependentControllerScrollAndDirectionRoutes()
        {
            Render(Plan(),Plan(CampExpeditionDefinition.ToolZeroId));events.SetSelectedGameObject(Control("project:remove:"+definition.rootId).gameObject);
            var left=events.currentSelectedGameObject.GetComponentInParent<ScrollRect>();var right=Control("project-requirements").GetComponent<ScrollRect>();left.verticalNormalizedPosition=1;right.verticalNormalizedPosition=1;view.ScrollProjects(-1,.2f);Assert.That(left.verticalNormalizedPosition,Is.LessThan(1));Assert.That(right.verticalNormalizedPosition,Is.EqualTo(1).Within(.001f));
            events.currentSelectedGameObject.GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(events){moveDir=MoveDirection.Right});Assert.That(events.currentSelectedGameObject,Is.SameAs(right.gameObject));var leftOffset=left.content.anchoredPosition;view.ScrollProjects(-1,.2f);Assert.That(right.verticalNormalizedPosition,Is.LessThan(1));Assert.That(left.content.anchoredPosition,Is.EqualTo(leftOffset));
            right.GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(events){moveDir=MoveDirection.Left});Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("project:remove:"+definition.rootId).gameObject));
        }
        [Test] public void SaveQuoteContainsPlanningControlsAndRestoresNewQuantityFieldsCorrectly()
        {
            Render(Plan());view.ShowQuote("Waiting for save");view.Render(new CampMenuPresentation{planning=Read(new[]{Plan(quantity:99),Plan(CampExpeditionDefinition.ToolZeroId)})});Assert.That(Quantity().IsInteractable(),Is.False);var focused=events.currentSelectedGameObject;commands.Clear();
            view.ScrollProjects(-1,1);Control("project:plus:"+CampExpeditionDefinition.ToolZeroId).Button.onClick.Invoke();Assert.That(commands,Is.Empty);Assert.That(events.currentSelectedGameObject,Is.SameAs(focused));view.HideQuote();Assert.That(Quantity(CampExpeditionDefinition.ToolZeroId).IsInteractable(),Is.True);Assert.That(Control("project:plus:"+definition.rootId).Button.interactable,Is.False);
        }
        [Test] public void UnknownSavedProjectsRemainEditableAndRemovableWithNoFalseRecipeLink()
        {
            const string id="retired.saved.recipe";Render(Plan(id,3));Assert.That(Control("project:view:"+id).Button.interactable,Is.False);Assert.That(view.projectChecklist.text,Does.Contain("Readiness cannot be confirmed"));Assert.That(view.projectChecklist.text,Does.Contain(id));Quantity(id).onEndEdit.Invoke("4");Assert.That(commands.Last(),Is.EqualTo("project:set:4:"+id));Control("project:remove:"+id).Button.onClick.Invoke();Assert.That(commands.Last(),Is.EqualTo("project:remove:"+id));
        }
        [Test] public void NewProjectFieldsStayContainedThroughNestedPickerAndQuoteRestoration()
        {
            view.ShowInventory("booter");view.ShowPicker("tool",Array.Empty<CampMenuStackView>());view.ShowQuote("Waiting for save");view.Render(new CampMenuPresentation{planning=Read(new[]{Plan(),Plan(CampExpeditionDefinition.ToolZeroId)})});view.HideQuote();Assert.That(Quantity().IsInteractable(),Is.False);view.Back();view.ShowPage("projects");Assert.That(Quantity().IsInteractable(),Is.True);Assert.That(Control("project:minus:"+definition.rootId).Button.interactable,Is.False);
        }
        [Test] public void MovingBetweenQuantityFieldsRevealsTheNewRowAndEmptyPagesReachBothPanes()
        {
            Render();Control("project-browse").GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(events){moveDir=MoveDirection.Right});Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("project-requirements").gameObject));
            Render(Plan(),Plan(CampExpeditionDefinition.ToolZeroId));events.SetSelectedGameObject(Quantity().gameObject);Quantity().GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(events){moveDir=MoveDirection.Down});var field=Quantity(CampExpeditionDefinition.ToolZeroId);Assert.That(events.currentSelectedGameObject,Is.SameAs(field.gameObject));var scroll=field.GetComponentInParent<ScrollRect>();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,field.transform);Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-1));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+1));
        }
        [Test] public void WorkbenchPlanningEntryTracksTheScopedT1WithoutEnablingCrafting()
        {
            view.Render(new CampMenuPresentation());view.ShowPage("workstations");Assert.That(view.WorkshopRecipe,Is.EqualTo(definition.rootId));Assert.That(Control("workshop:craft").Button.interactable,Is.False);Control("workshop:track").Button.onClick.Invoke();Assert.That(commands.Last(),Is.EqualTo("project:add:"+definition.rootId));Assert.That(view.ProjectCount,Is.Zero);
            view.Render(new CampMenuPresentation{planning=Read(new[]{Plan(quantity:4)})});Assert.That(Control("workshop:track").title.text,Is.EqualTo("Project tracked"));Assert.That(Control("workshop:track").Button.interactable,Is.False);
        }
        [Test] public void OtherStationsDoNotInventRecipesAndCatalogPlansDoNotInventAssignments()
        {
            view.ShowPage("workstations");Control("station:author.station.dust_refinery").Button.onClick.Invoke();Assert.That(view.WorkshopRecipe,Is.Null);Assert.That(Control("workshop:track").gameObject.activeSelf,Is.False);Assert.That(Control("workshop:craft").Button.interactable,Is.False);
            view.OpenWorkshopRecipe("author.tool.arc_powered_rotary_percussion_mining_tool");Assert.That(view.stationTitle.text,Is.EqualTo("Recipe plan"));Assert.That(view.stationBody.text,Does.Contain("have not been assigned"));Assert.That(Control("workshop:craft").Button.interactable,Is.False);
        }
        [Test] public void WorkshopDetailsStayScrollableAndPlanningStaysAvailableInTheField()
        {
            view.Render(new CampMenuPresentation{townAccess=false});view.ShowPage("workstations");var station=Control("station:author.station.workbench");events.SetSelectedGameObject(station.gameObject);station.GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(events){moveDir=MoveDirection.Right});Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("workshop:track").gameObject));Assert.That(Control("workshop:craft").Button.interactable,Is.False);
            var scroll=Control("workshop-details").GetComponent<ScrollRect>();events.SetSelectedGameObject(scroll.gameObject);scroll.verticalNormalizedPosition=1;view.ScrollInspector(-1,.2f);Assert.That(scroll.verticalNormalizedPosition,Is.LessThan(1));Control("workshop:track").Button.onClick.Invoke();Assert.That(commands.Last(),Is.EqualTo("project:add:"+definition.rootId));
        }
        [Test] public void LongWorkshopNamesReflowAndTheLastStationRemainsReachable()
        {
            view.ShowPage("workstations");view.SetLargeText(true);Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();
            foreach(var row in Controls.Where(control=>control.key.StartsWith("station:",StringComparison.Ordinal)))Assert.That(row.title.preferredHeight,Is.LessThanOrEqualTo(row.title.rectTransform.rect.height+1),row.key);
            var last=Control("station:"+definition.stations.Last().id);events.SetSelectedGameObject(last.gameObject);var scroll=last.GetComponentInParent<ScrollRect>();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,last.transform);Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-1));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+1));
        }
        [Test] public void FacilityRowsShowTheirDesignAndRecipeStateWithoutEnablingExecution()
        {
            view.Render(new CampMenuPresentation());view.ShowPage("workstations");view.SetLargeText(true);Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();
            foreach(var station in definition.stations)
            {
                var row=Control("station:"+station.id);Assert.That(row.GetComponentInChildren<CampMenuItemGlyphGraphic>(),Is.Not.Null);Assert.That(row.subtitle.text,Is.Not.Empty);Assert.That(row.subtitle.preferredHeight,Is.LessThanOrEqualTo(row.subtitle.rectTransform.rect.height+1),station.id);
                if(station.id.StartsWith("proposal.",StringComparison.Ordinal))Assert.That(row.subtitle.text,Is.EqualTo("Proposed facility"));
                commands.Clear();row.Button.onClick.Invoke();Assert.That(Control("workshop:craft").Button.interactable,Is.False);Assert.That(commands,Is.Empty);
                if(station.id!="author.station.workbench")Assert.That(Control("workshop:track").gameObject.activeSelf,Is.False);
            }
            Assert.That(root.GetComponentsInChildren<Text>(true).Any(text=>text.text=="STATIONS  "+definition.stations.Length),Is.True);
        }
        [Test] public void IncreasingTextSizeRevealsTheAlreadyFocusedFacilityAfterReflow()
        {
            view.ShowPage("workstations");Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();var last=Control("station:"+definition.stations.Last().id);events.SetSelectedGameObject(last.gameObject);
            view.SetLargeText(true);Canvas.ForceUpdateCanvases();var scroll=last.GetComponentInParent<ScrollRect>();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,last.transform);
            Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-1));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+1));Assert.That(events.currentSelectedGameObject,Is.SameAs(last.gameObject));
        }
        [Test] public void FocusingAScrollPanePreservesItsPositionInsteadOfRevealingItsEntireContent()
        {
            Render(Plan());var requirements=Control("project-requirements").GetComponent<ScrollRect>();requirements.verticalNormalizedPosition=.7f;var before=requirements.content.anchoredPosition;events.SetSelectedGameObject(requirements.gameObject);Assert.That(requirements.content.anchoredPosition,Is.EqualTo(before));
            view.OpenWorkshopRecipe(definition.rootId);var detail=Control("workshop-details").GetComponent<ScrollRect>();detail.verticalNormalizedPosition=1;events.SetSelectedGameObject(Control("station:author.station.workbench").gameObject);before=detail.content.anchoredPosition;events.SetSelectedGameObject(detail.gameObject);Assert.That(detail.content.anchoredPosition,Is.EqualTo(before));Assert.That(Control("workshop:craft").title.color,Is.EqualTo(view.style.muted));
        }
    }
}
