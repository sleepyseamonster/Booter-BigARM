using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        private sealed class ProjectRow
        {
            public RectTransform root;
            public Text title,state,inputs,make,more;
            public InputField quantity;
            public CampMenuControl minus,plus,recipe,remove;
            public string id;
            public int remaining;
        }
        private readonly List<ProjectRow> projectRows=new();
        private ScrollRect projectScroll,requirementScroll;
        private Text projectHeading,projectIntroduction,projectEmpty,projectValidation,requirementEyebrow,requirementHeading;
        private CampMenuControl projectBrowse;
        private bool projectsBuilt,rebindingProjects;
        private string focusedProject;
        private string projectValidationOwner;
        internal int ProjectCount=>presentation.planning?.projects.Length??0;
        private bool ProjectsInteractive=>page=="projects"&&!quoting&&pickerTarget==null&&GetComponent<CanvasGroup>().interactable;

        private void ConfigureProjects()
        {
            if(!roots.TryGetValue("projects",out var root))return;
            foreach(Transform child in root.transform)child.gameObject.SetActive(false);
            projectHeading=CatalogText(root.transform,"Tracked projects",24);
            projectIntroduction=CatalogText(root.transform,"Plan additional outputs to make. Tracking never reserves or consumes stock.",16);
            var left=ProjectPanel(root.transform,"Project plans",0,.615f);var right=ProjectPanel(root.transform,"Combined gathering list",.63f,1);
            projectScroll=CreateMenuScroll(left,"Project list");projectScroll.viewport.offsetMin=new Vector2(12,12);projectScroll.viewport.offsetMax=new Vector2(-12,-12);
            requirementScroll=CreateMenuScroll(right,"Project requirements","project-requirements");requirementScroll.viewport.offsetMin=new Vector2(18,18);requirementScroll.viewport.offsetMax=new Vector2(-18,-18);
            projectChecklist=CatalogText(requirementScroll.content,"",16);
            requirementEyebrow=CatalogText(requirementScroll.content,"COMBINED REQUIREMENTS",14);requirementEyebrow.color=style.accent;requirementHeading=CatalogText(requirementScroll.content,"Gathering list",24);
            projectEmpty=CatalogText(projectScroll.content,"No tracked projects yet. Open Workshops and choose Track project to build a gathering list.",18);
            projectBrowse=ProjectButton(projectScroll.content,"project-browse","Open Workshops");
            LinkMenuDirection(projectBrowse.gameObject,data=>{if(ProjectsInteractive&&data.moveDir==MoveDirection.Right){events?.SetSelectedGameObject(requirementScroll.gameObject);data.Use();}});
            projectValidation=CatalogText(projectScroll.content,"",16);projectValidation.color=style.accent;projectValidation.gameObject.SetActive(false);
            LinkMenuDirection(requirementScroll.gameObject,data=>
            {
                if(!ProjectsInteractive)return;
                if(data.moveDir==MoveDirection.Left){var row=projectRows.FirstOrDefault(row=>row.id==focusedProject&&row.root.gameObject.activeSelf)??projectRows.FirstOrDefault(row=>row.root.gameObject.activeSelf);events?.SetSelectedGameObject(row?.remove.gameObject??projectBrowse.gameObject);}
                else if(data.moveDir is MoveDirection.Up or MoveDirection.Down)ScrollProjects(data.moveDir==MoveDirection.Up?1:-1,.15f);
                data.Use();
            });
            projectsBuilt=true;
        }
        private RectTransform ProjectPanel(Transform parent,string name,float min,float max)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(CampMenuPanelGraphic));root.transform.SetParent(parent,false);root.GetComponent<CampMenuPanelGraphic>().SetFrame(style.surface,style.line,1);
            var rect=(RectTransform)root.transform;rect.anchorMin=new Vector2(min,0);rect.anchorMax=new Vector2(max,1);rect.offsetMin=Vector2.zero;rect.offsetMax=new Vector2(0,-100);return rect;
        }
        private CampMenuControl ProjectButton(Transform parent,string key,string label)
        {
            var root=new GameObject(key,typeof(RectTransform),typeof(CampMenuControl));root.transform.SetParent(parent,false);var control=root.GetComponent<CampMenuControl>();control.Configure(style,key,style.surface);control.title=CatalogText(root.transform,label,16);control.title.alignment=TextAnchor.MiddleCenter;
            control.title.rectTransform.anchorMin=Vector2.zero;control.title.rectTransform.anchorMax=Vector2.one;control.title.rectTransform.offsetMin=new Vector2(10,4);control.title.rectTransform.offsetMax=new Vector2(-10,-4);
            control.Wire();control.Activated+=Activate;control.Browsed+=Browse;control.Button.navigation=new Navigation{mode=Navigation.Mode.None};controls.Add(key,control);return control;
        }
        private ProjectRow CreateProjectRow(int index)
        {
            var row=new ProjectRow{root=ProjectPanel(projectScroll.content,"Project row "+index,0,1)};
            row.title=CatalogText(row.root,"",22);row.state=CatalogText(row.root,"",14);row.inputs=CatalogText(row.root,"",16);row.make=CatalogText(row.root,"Make",16);row.more=CatalogText(row.root,"more",16);
            row.minus=ProjectButton(row.root,"project-pool:"+index+":minus","−");row.plus=ProjectButton(row.root,"project-pool:"+index+":plus","+");row.recipe=ProjectButton(row.root,"project-pool:"+index+":view","View recipe");row.remove=ProjectButton(row.root,"project-pool:"+index+":remove","Untrack");
            row.quantity=Instantiate(storageSearch,row.root);row.quantity.name="Project quantity";row.quantity.onValueChanged=new InputField.OnChangeEvent();row.quantity.onEndEdit=new InputField.EndEditEvent();row.quantity.contentType=InputField.ContentType.IntegerNumber;row.quantity.characterLimit=2;row.quantity.lineType=InputField.LineType.SingleLine;((Text)row.quantity.placeholder).text="1–99";row.quantity.textComponent.alignment=TextAnchor.MiddleCenter;row.quantity.navigation=new Navigation{mode=Navigation.Mode.None};
            row.quantity.shouldActivateOnSelect=false;
            fontSizes[row.quantity.textComponent]=16;fontSizes[(Text)row.quantity.placeholder]=16;row.quantity.onEndEdit.AddListener(value=>RequestProjectQuantity(row,value));
            Selectable[] fields={row.minus.Button,row.quantity,row.plus.Button,row.recipe.Button,row.remove.Button};
            for(int i=0;i<fields.Length;i++){int column=i;LinkMenuDirection(fields[i].gameObject,data=>{if(!row.quantity.isFocused)MoveProjectFocus(row,column,data);});}
            return row;
        }
        private void RefreshProjects()
        {
            if(!projectsBuilt)return;var plans=presentation.planning?.projects??Array.Empty<CampMenuProjectView>();
            if(!plans.Any(plan=>plan.rootId==projectValidationOwner))projectValidation.gameObject.SetActive(false);
            string selectedKey=events?.currentSelectedGameObject?.GetComponent<CampMenuControl>()?.key;
            rebindingProjects=true;try
            {
                while(projectRows.Count<plans.Length)projectRows.Add(CreateProjectRow(projectRows.Count));
                for(int i=0;i<projectRows.Count;i++)
                {var pooled=projectRows[i];int part=0;foreach(var control in new[]{pooled.minus,pooled.plus,pooled.recipe,pooled.remove}){controls.Remove(control.key);control.key="project-pool:"+i+":"+part++;}}
                for(int i=0;i<projectRows.Count;i++)
                {
                    var row=projectRows[i];if(i>=plans.Length){row.root.gameObject.SetActive(false);continue;}
                    var plan=plans[i];if(row.id!=plan.rootId&&row.quantity.isFocused)row.quantity.DeactivateInputField();row.id=plan.rootId;row.remaining=plan.remaining;row.root.name="Project "+row.id;row.quantity.name="Project quantity "+row.id;row.root.gameObject.SetActive(true);row.title.text=ItemName(row.id);
                    row.state.text=plan.qualified?"TRACKED · RECIPE PLAN":"TRACKED · RECIPE QUANTITIES UNAVAILABLE";row.state.color=plan.qualified?style.muted:style.accent;
                    row.inputs.text=plan.qualified?"Direct requirements\n"+string.Join("\n",plan.inputs.Select(input=>input.quantity+" × "+ItemName(input.itemId))):"Gathering counts exclude this project until its recipe quantities are available.";
                    if(!row.quantity.isFocused)row.quantity.SetTextWithoutNotify(plan.remaining.ToString(CultureInfo.InvariantCulture));
                    BindProjectControl(row.minus,"project:minus:"+row.id);BindProjectControl(row.plus,"project:plus:"+row.id);BindProjectControl(row.recipe,"project:view:"+row.id);BindProjectControl(row.remove,"project:remove:"+row.id);
                    SetProjectEnabled(row.minus.Button,plan.remaining>1);SetProjectEnabled(row.plus.Button,plan.remaining<99);SetProjectEnabled(row.recipe.Button,menuCatalog?.Tree(row.id)!=null);SetProjectEnabled(row.remove.Button,true);SetProjectEnabled(row.quantity,true);
                }
                bool empty=plans.Length==0;projectEmpty.gameObject.SetActive(empty);projectBrowse.gameObject.SetActive(empty);
                var data=presentation.planning??new CampMenuPlanningView();
                string requirements=string.Join("\n\n",data.requirements.OrderBy(row=>ItemName(row.itemId),StringComparer.OrdinalIgnoreCase).Select(row=>ItemName(row.itemId)+$"\n{row.eligible} eligible · {row.knownOwned} known owned\n{row.missing} missing · {row.required} base required"));
                string unresolved=data.unqualified.Length==0?"":"\n\nRecipe quantities unavailable — excluded from counts\n"+string.Join("\n",data.unqualified.Select(ItemName));
                projectChecklist.text=(empty?"Track a project to see gathering requirements.":data.materialsAccounted?"Materials accounted for. Crafting checks still apply.":data.unqualified.Length>0?"Some recipe quantities are unavailable. Readiness cannot be confirmed.":"More eligible materials are needed.")+"\n\n"+requirements+unresolved+
                    "\n\nPrepared parts replace their ingredient branches. Base requirements assume no prepared parts. Eligible stock is shared once; planned outputs do not count as stock.\n\n"+
                    (data.storageAccessible?"Town storage is eligible.":"Town storage is known but unavailable here.")+" "+(data.leggerAccessible?"Legger is accessible.":"Legger's cargo is known but out of reach.")+"\n\nMaking an item reduces its remaining project quantity.";
            }
            finally{rebindingProjects=false;}
            ReflowProjects();
            if(page=="projects"&&selectedKey?.StartsWith("project:",StringComparison.Ordinal)==true&&events!=null)
            {if(controls.TryGetValue(selectedKey,out var saved)&&saved.gameObject.activeInHierarchy&&saved.Button.IsInteractable())events.SetSelectedGameObject(saved.gameObject);else events.SetSelectedGameObject(null);}
        }
        private void BindProjectControl(CampMenuControl control,string key)
        {controls.Remove(control.key);control.key=control.name=key;controls[key]=control;}
        private void SetProjectEnabled(Selectable control,bool enabled)
        {
            if(StorageChoiceOpen)beforeStorageChoice[control]=enabled;
            if(pickerTarget!=null)beforePicker[control]=enabled;
            if(quoting)beforeQuote[control]=pickerTarget==null&&enabled;
            control.interactable=!quoting&&pickerTarget==null&&(!StorageChoiceOpen||IsStorageChoiceControl(control)&&storageChoiceReady)&&enabled;
            control.GetComponent<CampMenuControl>()?.RefreshAvailability();
        }
        private float ProjectTextHeight(Text text,float width,float minimum=24)=>Mathf.Max(minimum,text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text,text.GetGenerationSettings(new Vector2(Mathf.Max(20,width),0)))/text.pixelsPerUnit);
        private void ReflowProjects()
        {
            if(!projectsBuilt)return;
            float width=((RectTransform)roots["projects"].transform).rect.width;
            TopRegion(projectHeading.rectTransform,0,40);float introduction=ProjectTextHeight(projectIntroduction,width);TopRegion(projectIntroduction.rectTransform,44,introduction);
            foreach(var scroll in new[]{projectScroll,requirementScroll})((RectTransform)scroll.transform.parent).offsetMax=new Vector2(0,-56-introduction);
            float left=projectScroll.viewport.rect.width,top=0;
            foreach(var row in projectRows.Where(row=>row.root.gameObject.activeSelf))
            {
                float title=ProjectTextHeight(row.title,left-32,30),state=ProjectTextHeight(row.state,left-32),inputs=ProjectTextHeight(row.inputs,left-32);
                float inputTop=16+title+8+state+16,quantityTop=inputTop+inputs+16,actionsTop=quantityTop+58;
                if(projectValidation.gameObject.activeSelf&&projectValidationOwner==row.id)
                {projectValidation.transform.SetParent(row.root,false);float validation=ProjectTextHeight(projectValidation,left-32);TopRegion(projectValidation.rectTransform,actionsTop,validation,16,16);actionsTop+=validation+12;}
                float actionWidth=(left-40)*.5f,actionHeight=Mathf.Max(48,Mathf.Max(ProjectTextHeight(row.recipe.title,actionWidth-20),ProjectTextHeight(row.remove.title,actionWidth-20))+12);
                TopRegion(row.root,top,actionsTop+actionHeight+16);TopRegion(row.title.rectTransform,16,title,16,16);TopRegion(row.state.rectTransform,24+title,state,16,16);TopRegion(row.inputs.rectTransform,inputTop,inputs,16,16);
                TopRegion(row.make.rectTransform,quantityTop,44,16,left-74);TopRegion((RectTransform)row.minus.transform,quantityTop,44,80,left-124);TopRegion((RectTransform)row.quantity.transform,quantityTop,44,132,left-208);TopRegion((RectTransform)row.plus.transform,quantityTop,44,216,left-260);TopRegion(row.more.rectTransform,quantityTop,44,272,16);
                TopRegion((RectTransform)row.recipe.transform,actionsTop,actionHeight,16,24+actionWidth);TopRegion((RectTransform)row.remove.transform,actionsTop,actionHeight,24+actionWidth,16);top+=actionsTop+actionHeight+28;
            }
            float empty=ProjectTextHeight(projectEmpty,left-24,80);TopRegion(projectEmpty.rectTransform,12,empty,12,12);TopRegion((RectTransform)projectBrowse.transform,empty+28,52,12,12);
            if(ProjectCount==0)top=empty+92;
            projectScroll.content.sizeDelta=new Vector2(0,Mathf.Max(top,projectScroll.viewport.rect.height));
            float rightWidth=requirementScroll.viewport.rect.width-8,eyebrow=ProjectTextHeight(requirementEyebrow,rightWidth),heading=ProjectTextHeight(requirementHeading,rightWidth,32),bodyTop=eyebrow+heading+20;
            TopRegion(requirementEyebrow.rectTransform,0,eyebrow,4,4);TopRegion(requirementHeading.rectTransform,eyebrow+8,heading,4,4);float requirementHeight=ProjectTextHeight(projectChecklist,rightWidth);TopRegion(projectChecklist.rectTransform,bodyTop,requirementHeight,4,4);requirementScroll.content.sizeDelta=new Vector2(0,Mathf.Max(bodyTop+requirementHeight,requirementScroll.viewport.rect.height));
            foreach(var scroll in new[]{projectScroll,requirementScroll}){float maximum=Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height);scroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(scroll.content.anchoredPosition.y,0,maximum));}
        }
        private bool HandleProjectAction(string key)
        {
            if(key=="project-requirements")return true;
            if(key=="project-browse"){ShowPage("workstations");return true;}
            string[] actions={"minus","plus","view","remove"};string action=actions.FirstOrDefault(value=>key.StartsWith("project:"+value+":",StringComparison.Ordinal));if(action==null)return false;
            if(!ProjectsInteractive)return true;
            string id=key.Substring(("project:"+action+":").Length);var row=projectRows.FirstOrDefault(row=>row.id==id&&row.root.gameObject.activeSelf);if(row==null)return true;
            if(action=="view"){if(menuCatalog?.Tree(id)!=null)OpenWorkshopRecipe(id);return true;}
            if(action=="remove")Command?.Invoke("project:remove:"+id);
            else RequestProjectQuantity(row,(row.remaining+(action=="plus"?1:-1)).ToString(CultureInfo.InvariantCulture));return true;
        }
        private void RequestProjectQuantity(ProjectRow row,string value)
        {
            if(rebindingProjects||!ProjectsInteractive||!row.root.gameObject.activeInHierarchy)return;
            if(!int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out int quantity)||quantity<1||quantity>99)
            {row.quantity.SetTextWithoutNotify(row.remaining.ToString(CultureInfo.InvariantCulture));projectValidationOwner=row.id;projectValidation.text="Choose a project quantity from 1 to 99.";projectValidation.gameObject.SetActive(true);ReflowProjects();RevealMenuScrollTarget(projectValidation.transform);return;}
            projectValidation.gameObject.SetActive(false);ReflowProjects();if(quantity!=row.remaining)Command?.Invoke("project:set:"+quantity+":"+row.id);
        }
        private void FocusProjectsPage()
        {
            if(events==null||!ProjectsInteractive)return;
            if(pageFocus.TryGetValue("projects",out var key)&&controls.TryGetValue(key,out var saved)&&saved.gameObject.activeInHierarchy&&saved.Button.IsInteractable()){events.SetSelectedGameObject(saved.gameObject);return;}
            var row=projectRows.FirstOrDefault(row=>row.root.gameObject.activeSelf);events.SetSelectedGameObject(row==null?projectBrowse.gameObject:row.recipe.Button.IsInteractable()?row.recipe.gameObject:row.remove.gameObject);
        }
        private void MoveProjectFocus(ProjectRow row,int column,AxisEventData data)
        {
            if(!ProjectsInteractive||events==null)return;int at=projectRows.IndexOf(row),next=column;
            if(data.moveDir==MoveDirection.Right){if(column==4){focusedProject=row.id;events.SetSelectedGameObject(requirementScroll.gameObject);data.Use();return;}next++;}
            else if(data.moveDir==MoveDirection.Left)next=Mathf.Max(0,column-1);
            else if(data.moveDir is MoveDirection.Up or MoveDirection.Down)at=Mathf.Clamp(at+(data.moveDir==MoveDirection.Up?-1:1),0,ProjectCount-1);
            var target=projectRows[at];Selectable[] fields={target.minus.Button,target.quantity,target.plus.Button,target.recipe.Button,target.remove.Button};
            int step=next<column?-1:1;while(next>=0&&next<fields.Length&&!fields[next].IsInteractable())next+=step;
            if(next>=0&&next<fields.Length){focusedProject=target.id;events.SetSelectedGameObject(fields[next].gameObject);RevealMenuScrollTarget(fields[next].transform);}
            data.Use();
        }
        internal void ScrollProjects(float axis,float seconds)
        {
            if(!ProjectsInteractive)return;var scroll=events?.currentSelectedGameObject?.GetComponentInParent<ScrollRect>();if(scroll!=projectScroll&&scroll!=requirementScroll)scroll=projectScroll;
            scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+axis*seconds);
        }
    }
}
