using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Public record addresses are safe to display before discovery. Private facts are absent.</summary>
    public sealed class CampAlmanacEntry
    {
        public int slot,ordinal;
        public string category;
        public CampMenuCatalogItem item;
        public bool Known=>item!=null;
    }
    public sealed class CampAlmanacCategory {public string name;public int total,known;}
    public sealed class CampAlmanacIndex
    {
        public CampAlmanacEntry[] entries;
        public CampAlmanacCategory[] categories;
        public int total,known,matches,page,pages,start;
    }
    public static class CampMenuResources
    {
        public static CampMenuGuideTopic[] NativeTopics(CampMenuCatalog catalog)=>catalog.GuideTopics.Select(topic=>new CampMenuGuideTopic
        {
            id=topic.id,title=topic.title,category=topic.id=="expeditions"?"Expeditions":topic.category,
            sections=topic.sections.Select((section,index)=>new CampMenuGuideSection{heading=topic.id=="expeditions"&&index==0?"Your gathering loop":section.heading,text=(topic.id,index) switch
            {
                ("navigation",2)=>"This schematic shows illustrative Booter, Legger and camp markers. A waypoint is a local visual aid. It does not provide live terrain, distance measurements or movement commands.",
                ("equipment",0)=>"Open Inventory to inspect Booter’s equipment and the Legger’s cargo. Possession and equipped status are distinct. Inspect compatible equipment in the picker before confirming a choice.",
                ("equipment",2)=>"The Legger is a physically present companion and finite cargo carrier. He has a true world position and must traverse the world to regroup. Bring him close to swap carried gear in the field.",
                ("workshops",2)=>"Track items and projects to organize gathering. Workshop recipe plans show required inputs and eligible stock. A visible tree is a design record; it does not mean every recipe can be made yet.",
                ("expeditions",0)=>"Prepare your loadout in town. The current gathering goal is to bring materials back to storage over several expeditions and plan the tier-one multi-tool.",
                ("expeditions",1)=>"Leave town through Play, gather in the field, then return to your departure beacon to extract. Review recovered cargo and send selected items or all items to storage, or keep them on their current carrier.",
                ("expeditions",2)=>"Travel farther from the beacon to find more demanding gathering opportunities. Prepare the right tools and leave enough cargo space for the return journey.",
                ("expeditions",3)=>"Stored goods and Commons remain in town between expeditions. Save and quit preserves the current world instance. The expedition and its timers remain frozen while the game is closed.",
                ("controls",1)=>"The north face button opens Inventory. Start / Menu opens the Start page. Use the displayed keyboard shortcuts for the current bindings. The bottom prompts are also clickable.",
                ("controls",2)=>"The east face button or the displayed Back shortcut backs out. Leave text editing or close an item picker first. The almanac returns to its opener; Guide and Map return to Start. Start restores its originating page. Ordinary menu pages return to Play.",
                _=>section.text
            }}).ToArray()
        }).ToArray();
        public static CampMenuGuideTopic[] FindTopics(CampMenuCatalog catalog,string query)
        {
            var terms=(query??"").Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
            return NativeTopics(catalog).Where(topic=>terms.All(term=>
                (topic.title+" "+topic.category+" "+string.Join(" ",topic.sections.Select(s=>s.heading+" "+s.text))).IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
        }
        public static CampAlmanacIndex Almanac(CampMenuCatalog catalog,IEnumerable<string> discoveries,string category="",string status="all",string query="",bool catalogOrder=false,int page=0,int pageSize=36,int locateSlot=-1)
        {
            var discovered=new HashSet<string>(discoveries??Array.Empty<string>(),StringComparer.Ordinal);
            var categories=new Dictionary<string,CampAlmanacCategory>(StringComparer.Ordinal);
            var entries=new List<CampAlmanacEntry>();int slot=0;
            foreach(var item in catalog.Items)
            {
                string family=string.IsNullOrEmpty(item.category)?"Uncategorized":item.category;
                if(!categories.TryGetValue(family,out var count))categories.Add(family,count=new CampAlmanacCategory{name=family});
                count.total++;bool known=discovered.Contains(item.id);if(known)count.known++;
                entries.Add(new CampAlmanacEntry{slot=slot++,ordinal=count.total,category=family,item=known?item:null});
            }
            string text=(query??"").Trim();
            var matches=entries.Where(e=>(string.IsNullOrEmpty(category)||e.category==category)&&(status=="all"||(status=="discovered"?e.Known:!e.Known))&&
                (text.Length==0||e.Known&&(e.item.name+" "+e.category+" "+e.item.purpose+" "+e.item.search).IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0));
            var sorted=(catalogOrder?matches.OrderBy(e=>e.category,StringComparer.OrdinalIgnoreCase).ThenBy(e=>e.ordinal):
                matches.OrderByDescending(e=>e.Known).ThenBy(e=>e.Known?e.item.name:e.category,StringComparer.OrdinalIgnoreCase).ThenBy(e=>e.ordinal)).ToArray();
            int size=pageSize>0?Math.Min(100,pageSize):36,pages=Math.Max(1,(sorted.Length+size-1)/size),located=Array.FindIndex(sorted,e=>e.slot==locateSlot);
            int current=Math.Clamp(located>=0?located/size:page,0,pages-1);
            return new CampAlmanacIndex{categories=categories.Values.OrderBy(c=>c.name,StringComparer.OrdinalIgnoreCase).ToArray(),total=entries.Count,known=entries.Count(e=>e.Known),matches=sorted.Length,pages=pages,page=current,start=current*size,entries=sorted.Skip(current*size).Take(size).ToArray()};
        }
        // The browser's preview has no terrain identity, world coordinates or movement authority.
        public static float MapZoom(float current,int direction)=>Mathf.Clamp(current+Math.Sign(direction)*.25f,1,2);
    }
}
