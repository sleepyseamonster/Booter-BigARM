using System;
using System.Collections.Generic;
using System.Linq;

namespace BooterBigArm.TopDown3D
{
    public sealed class CampProjectDemand
    {
        public readonly Dictionary<string,int> total=new(StringComparer.Ordinal),missing=new(StringComparer.Ordinal);
        public readonly Dictionary<string,int> banked=new(StringComparer.Ordinal),carried=new(StringComparer.Ordinal);
        public readonly Dictionary<string,int> eligible=new(StringComparer.Ordinal);
        public readonly HashSet<string> unqualified=new(StringComparer.Ordinal);
        public bool Ready=>unqualified.Count==0&&missing.Values.All(q=>q==0);
    }
    public static class CampExpeditionProject
    {
        public static CampProjectDemand Calculate(CampExpeditionDefinition definition,TopDown3DInventoryState booter,TopDown3DInventoryState legger,TopDown3DInventoryState storage)
        {
            var result=new CampProjectDemand();Count(storage,result.banked);Count(booter,result.carried);Count(legger,result.carried);
            var owned=new Dictionary<string,int>(result.banked,StringComparer.Ordinal);
            foreach(var row in result.carried)Add(owned,row.Key,row.Value);
            Expand(definition,definition.rootId,1,new Dictionary<string,int>(),result.total,new HashSet<string>());
            Expand(definition,definition.rootId,1,owned,result.missing,new HashSet<string>());
            return result;
        }
        // Tracked quantities request ADDITIONAL outputs. Existing finished items cannot
        // satisfy them, while prepared intermediate components still replace their branches.
        public static CampProjectDemand Calculate(CampExpeditionDefinition definition,TopDown3DInventoryState booter,TopDown3DInventoryState legger,TopDown3DInventoryState storage,
            IEnumerable<CampProjectRecord> projects,bool leggerAccessible=true,bool storageAccessible=true)
        {
            var result=new CampProjectDemand();Count(storage,result.banked);Count(booter,result.carried);Count(legger,result.carried);
            Count(booter,result.eligible);if(leggerAccessible)Count(legger,result.eligible);if(storageAccessible)Count(storage,result.eligible);
            var available=new Dictionary<string,int>(result.eligible,StringComparer.Ordinal);
            foreach(var project in projects.OrderBy(p=>p.rootId,StringComparer.Ordinal))
            {
                if(project.remaining<1||project.remaining>99)throw new ArgumentOutOfRangeException(nameof(projects));
                var root=definition.Find(project.rootId);
                if(root==null||root.inputs.Length==0){result.unqualified.Add(project.rootId);continue;}
                foreach(var input in root.inputs)
                {
                    int quantity=checked(input.quantity*project.remaining);
                    Expand(definition,input.itemId,quantity,new Dictionary<string,int>(),result.total,new HashSet<string>());
                    Expand(definition,input.itemId,quantity,available,result.missing,new HashSet<string>());
                }
            }
            return result;
        }
        private static void Expand(CampExpeditionDefinition definition,string id,int quantity,Dictionary<string,int> owned,Dictionary<string,int> missing,HashSet<string> visiting)
        {
            int have=owned.TryGetValue(id,out var count)?count:0;int used=Math.Min(have,quantity);owned[id]=have-used;quantity-=used;if(quantity==0)return;
            var item=definition.Find(id)??throw new InvalidOperationException("Missing scoped project definition: "+id);
            if(!visiting.Add(id))throw new InvalidOperationException("Scoped project contains a recipe cycle.");
            if(item.inputs.Length==0)Add(missing,id,quantity);
            else foreach(var input in item.inputs)Expand(definition,input.itemId,checked(quantity*input.quantity),owned,missing,visiting);
            visiting.Remove(id);
        }
        private static void Count(TopDown3DInventoryState store,Dictionary<string,int> output)
        {if(store==null)return;foreach(var slot in store.Slots)if(!slot.IsEmpty)Add(output,slot.ItemId,slot.Quantity);}
        private static void Add(Dictionary<string,int> map,string key,int quantity)=>map[key]=checked((map.TryGetValue(key,out var current)?current:0)+quantity);
    }
}
