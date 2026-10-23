using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BooterBigArm.Editor
{
    public static partial class CampExpeditionMenuCapture
    {
        // Native authoring has no assigned garment counts. These disposable records are explicitly
        // synthetic layout proof, never written back to native assets, player stock or SQLite.
        private static CampMenuCatalog InventoryCompletionCatalog(CampExpeditionMenuView view,string page,out CampExpeditionDefinition definition)
        {
            var data=JsonUtility.FromJson<CampMenuCatalogData>(Resources.Load<TextAsset>("CampMenuCatalog").text);
            var garment=data.items.First(item=>item.pocketSlot=="chest"&&item.category is "Clothing" or "Armor");garment.pockets=page switch{"pockets-unassigned"=>-1,"pockets-zero"=>0,"pockets-huge"=>CampMenuGarmentPockets.MaximumAuthoredCount,_=>9};
            definition=UnityEngine.Object.Instantiate(view.definition);definition.name="Disposable pocket render fixture";view.definition=definition;definition.items=definition.items.Concat(new[]{new CampExpeditionItem{id=garment.id,name=garment.name,purpose=garment.purpose,category=garment.category,tier=garment.tier,rarity=garment.rarity,equipmentAddress="chest"}}).ToArray();return new CampMenuCatalog(data);
        }
        private static void ShowInventoryCompletion(CampExpeditionMenuView view,CampMenuCatalog catalog,string page)
        {
            var garment=catalog.Items.First(item=>item.pocketSlot=="chest"&&item.category is "Clothing" or "Armor");
            var rows=new[]{new CampMenuStackView{owner="booter",key="0/2",itemId=garment.id,equipmentAddress="chest",instanceId="disposable-garment",quantity=1},new CampMenuStackView{owner="booter",key="0/8",itemId=CampExpeditionDefinition.ToolZeroId,equipmentAddress="tool",quantity=1}}.Concat(Enumerable.Range(0,12).Select(index=>new CampMenuStackView{owner="storage",key="2/"+index,itemId=index%2==0?"resource.ironstone_ore":CampExpeditionDefinition.ToolZeroId,quantity=index%2==0?index+1:1})).ToArray();
            view.Render(new CampMenuPresentation{canPlay=true,commons=50,storageCapacity=24,stacks=rows});if(page.StartsWith("storage-",StringComparison.Ordinal))view.ShowPage("storage");else view.ShowInventory("booter");
        }
        private static void FocusInventoryCompletion(CampExpeditionMenuView view,EventSystem events,string page)
        {
            CampMenuControl Control(string key)=>view.GetComponentsInChildren<CampMenuControl>(true).Single(control=>control.key==key);
            if(page is "storage-filter" or "storage-sort"){Control(page).Button.onClick.Invoke();view.SetStorageChoiceReady();}
            else if(page=="storage-no-results")view.storageSearch.text="no_such_owned_item_123";
            else
            {
                events.SetSelectedGameObject(Control("equip:chest").gameObject);typeof(CampExpeditionMenuView).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,null);
                if(page is "pockets-last" or "pockets-huge")view.SetPocketPage("chest",long.MaxValue);
                if(page=="pockets-last")events.SetSelectedGameObject(Control("pocket:empty:chest:0").gameObject);
            }
            Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();
        }
    }
}
