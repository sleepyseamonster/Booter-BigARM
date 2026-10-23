using System;
using System.Collections.Generic;
using System.Linq;

namespace BooterBigArm.TopDown3D
{
    public sealed class CampMenuProjectView
    {
        public string rootId;
        public int remaining;
        public bool qualified;
        public CampExpeditionIngredient[] inputs=Array.Empty<CampExpeditionIngredient>();
    }
    public sealed class CampMenuRequirementView
    {
        public string itemId;
        public int required,eligible,knownOwned,missing;
    }
    public sealed class CampMenuPlanningView
    {
        public CampMenuProjectView[] projects=Array.Empty<CampMenuProjectView>();
        public CampMenuRequirementView[] requirements=Array.Empty<CampMenuRequirementView>();
        public string[] unqualified=Array.Empty<string>();
        public bool materialsAccounted,leggerAccessible=true,storageAccessible=true;
    }
    /// <summary>Projects request additional outputs. This projection cannot reserve stock or authorize crafting.</summary>
    public static class CampMenuProjects
    {
        public static CampMenuPlanningView Read(CampExpeditionDefinition definition,TopDown3DInventoryState booter,TopDown3DInventoryState legger,TopDown3DInventoryState storage,
            CampProjectRecord[] records,bool leggerAccessible=true,bool storageAccessible=true)
        {
            var demand=CampExpeditionProject.Calculate(definition,booter,legger,storage,records,leggerAccessible,storageAccessible);
            return new CampMenuPlanningView
            {
                projects=records.Select(record=>
                {
                    var item=definition.Find(record.rootId);bool qualified=item!=null&&item.inputs.Length>0;
                    return new CampMenuProjectView{rootId=record.rootId,remaining=record.remaining,qualified=qualified,
                        inputs=qualified?item.inputs.Select(input=>new CampExpeditionIngredient{itemId=input.itemId,quantity=checked(input.quantity*record.remaining)}).ToArray():Array.Empty<CampExpeditionIngredient>()};
                }).ToArray(),
                requirements=demand.total.OrderBy(row=>row.Key,StringComparer.Ordinal).Select(row=>new CampMenuRequirementView{itemId=row.Key,required=row.Value,
                    eligible=demand.eligible.GetValueOrDefault(row.Key),knownOwned=checked(demand.banked.GetValueOrDefault(row.Key)+demand.carried.GetValueOrDefault(row.Key)),missing=demand.missing.GetValueOrDefault(row.Key)}).ToArray(),
                unqualified=demand.unqualified.OrderBy(id=>id,StringComparer.Ordinal).ToArray(),materialsAccounted=records.Length>0&&demand.Ready,
                leggerAccessible=leggerAccessible,storageAccessible=storageAccessible
            };
        }
    }
}
