using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable] public sealed class CampMenuCatalogItem
    {
        public string id,name,purpose,category,family,tier,rarity,search,acquisition,processing,limitations,artReference,artResource;
        public string type,mass,stackLimit;
        public long pockets=-1;
        public string pocketSlot;
    }
    [Serializable] public sealed class CampMenuTreeNode
    { public string id; public float x,y; public bool shared; public string[] inputs=Array.Empty<string>(); }
    [Serializable] public sealed class CampMenuTreeEdge {public string source,target,kind;}
    [Serializable] public sealed class CampMenuTree
    { public string rootId,category,family,tier; public float width,height; public int unavailableDefinitions,omittedCycles;public CampMenuTreeNode[] nodes=Array.Empty<CampMenuTreeNode>();public CampMenuTreeEdge[] edges=Array.Empty<CampMenuTreeEdge>(); }
    [Serializable] public sealed class CampMenuGuideSection {public string heading,text;}
    [Serializable] public sealed class CampMenuGuideTopic
    {public string id,title,category;public CampMenuGuideSection[] sections=Array.Empty<CampMenuGuideSection>();}
    [Serializable] public sealed class CampMenuCatalogData
    {
        public int version=1;
        public string catalogRevision,sourceHash,projectionHash,almanacSourceHash,pocketSourceHash;
        public CampMenuCatalogItem[] items=Array.Empty<CampMenuCatalogItem>();
        public CampMenuTree[] trees=Array.Empty<CampMenuTree>();
        public CampMenuGuideTopic[] guideTopics=Array.Empty<CampMenuGuideTopic>();
    }

    /// <summary>Read-only presentation index. It cannot grant items, define runtime recipes or change run identity.</summary>
    public sealed class CampMenuCatalog
    {
        private readonly Dictionary<string,CampMenuCatalogItem> items;
        private readonly CampMenuCatalogItem[] orderedItems;
        private readonly Dictionary<string,CampMenuTree> trees;
        private readonly Dictionary<string,HashSet<string>> uses=new(StringComparer.Ordinal);
        private readonly Dictionary<string,Sprite> artwork=new(StringComparer.Ordinal);
        private static CampMenuCatalog cached;
        private static string cachedText;
        public string Revision {get;}
        public IEnumerable<CampMenuCatalogItem> Items=>orderedItems;
        public IEnumerable<CampMenuTree> Trees=>trees.Values;
        public IReadOnlyList<CampMenuGuideTopic> GuideTopics {get;}
        public CampMenuCatalog(CampMenuCatalogData data)
        {
            if(data==null||data.version!=1||string.IsNullOrEmpty(data.catalogRevision)||data.items==null||data.trees==null)
                throw new ArgumentException("Menu catalog requires a supported revision-bound export.");
            Revision=data.catalogRevision;GuideTopics=data.guideTopics??Array.Empty<CampMenuGuideTopic>();items=new(StringComparer.Ordinal);trees=new(StringComparer.Ordinal);
            foreach(var item in data.items)
            {
                if(item==null||string.IsNullOrWhiteSpace(item.id)||string.IsNullOrWhiteSpace(item.name)||!items.TryAdd(item.id,item))
                    throw new ArgumentException("Menu item identity is missing or duplicated.");
                if(item.pockets< -1||item.pockets>CampMenuGarmentPockets.MaximumAuthoredCount||item.pocketSlot is not (null or "" or "chest" or "legs"))
                    throw new ArgumentException("Menu pocket metadata is invalid for "+item.id+".");
            }
            orderedItems=(CampMenuCatalogItem[])data.items.Clone();
            foreach(var tree in data.trees)
            {
                if(tree==null||!items.ContainsKey(tree.rootId)||!trees.TryAdd(tree.rootId,tree)||tree.nodes==null||!float.IsFinite(tree.width)||!float.IsFinite(tree.height)||tree.width<=0||tree.height<=0)
                    throw new ArgumentException("Menu tree identity or dimensions are invalid.");
                var ids=new HashSet<string>(StringComparer.Ordinal);
                foreach(var node in tree.nodes)
                    if(node==null||!items.ContainsKey(node.id)||!ids.Add(node.id)||node.inputs==null||!float.IsFinite(node.x)||!float.IsFinite(node.y))
                        throw new ArgumentException("Menu tree nodes require stable known identities.");
                if(!ids.Contains(tree.rootId))throw new ArgumentException("Menu tree has no finished item.");
                foreach(var node in tree.nodes)
                {
                    if(node.inputs.Any(id=>!ids.Contains(id)))throw new ArgumentException("Menu tree has an unresolved displayed input.");
                }
                if(tree.edges==null||tree.edges.Any(edge=>edge==null||!ids.Contains(edge.source)||!ids.Contains(edge.target)||!tree.nodes.First(node=>node.id==edge.source).inputs.Contains(edge.target)))
                    throw new ArgumentException("Menu edges must reference the inputs of this tree.");
                // Retained detached branches are visible for browsing, but are not inputs of the finished item.
                var byId=tree.nodes.ToDictionary(node=>node.id,StringComparer.Ordinal);
                var connected=new HashSet<string>(StringComparer.Ordinal);var pending=new Stack<string>();pending.Push(tree.rootId);
                while(pending.Count>0){var id=pending.Pop();if(connected.Add(id))foreach(var child in byId[id].inputs)pending.Push(child);}
                connected.Remove(tree.rootId);
                foreach(var id in connected)
                {
                    if(!uses.TryGetValue(id,out var roots))uses[id]=roots=new(StringComparer.Ordinal);
                    roots.Add(tree.rootId);
                }
            }
        }
        public CampMenuCatalogItem Find(string id)=>id!=null&&items.TryGetValue(id,out var item)?item:null;
        public CampMenuTree Tree(string rootId)=>rootId!=null&&trees.TryGetValue(rootId,out var tree)?tree:null;
        public Sprite Art(string id)
        {
            var item=Find(id);if(string.IsNullOrEmpty(item?.artResource))return null;
            if(!artwork.TryGetValue(item.artResource,out var sprite))artwork[item.artResource]=sprite=Resources.Load<Sprite>(item.artResource);
            return sprite;
        }
        public HashSet<string> Inputs(string rootId,string nodeId)
        {
            var tree=Tree(rootId);var found=new HashSet<string>(StringComparer.Ordinal);if(tree==null||nodeId==null)return found;
            var byId=tree.nodes.ToDictionary(node=>node.id,StringComparer.Ordinal);if(!byId.ContainsKey(nodeId))return found;
            var pending=new Stack<string>(byId[nodeId].inputs);
            while(pending.Count>0){var id=pending.Pop();if(id!=nodeId&&found.Add(id))foreach(var child in byId[id].inputs)pending.Push(child);}
            return found;
        }
        public HashSet<string> Outputs(string rootId,string nodeId)=>new(Tree(rootId)?.nodes.Where(node=>node.inputs.Contains(nodeId)).Select(node=>node.id)??Array.Empty<string>(),StringComparer.Ordinal);
        public IEnumerable<CampMenuCatalogItem> Search(string query,string category=null)
        {
            var terms=(query??"").Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
            return items.Values.Where(i=>(string.IsNullOrEmpty(category)||i.category==category)&&terms.All(term=>
                (i.name+" "+i.category+" "+i.family+" "+i.search+" "+i.id).IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0));
        }
        public string[] ResultsUsing(IEnumerable<string> tracked)=>tracked.Where(id=>id!=null&&uses.ContainsKey(id))
            .SelectMany(id=>uses[id]).Distinct(StringComparer.Ordinal).OrderBy(id=>Find(id)?.name,StringComparer.OrdinalIgnoreCase).ThenBy(id=>id,StringComparer.Ordinal).ToArray();
        public static CampMenuCatalog Load()
        {
            var source=Resources.Load<TextAsset>("CampMenuCatalog");
            if(source==null)return null;
            var text=source.text;if(cached==null||cachedText!=text){cached=new CampMenuCatalog(JsonUtility.FromJson<CampMenuCatalogData>(text));cachedText=text;}
            return cached;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetCache(){cached=null;cachedText=null;}
    }
}
