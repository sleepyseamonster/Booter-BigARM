using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Deterministic run dressing on fixed, always-loaded terrain; never generates terrain/chunks.</summary>
    public static class CampExpeditionPopulation
    {
        public const int Version=1;
        public delegate bool SupportProbe(Vector3 proposed,out Vector3 supported);
        public static bool TryPrepare(CampExpeditionDefinition definition,string runId,int seed,Vector3 origin,
            SupportProbe support,Func<Vector3,Vector3,bool> route,out CampExpeditionNodeRecord[] nodes)
            =>TryPrepare(definition,runId,seed,origin,support,route,out nodes,out _);
        public static bool TryPrepare(CampExpeditionDefinition definition,string runId,int seed,Vector3 origin,
            SupportProbe support,Func<Vector3,Vector3,bool> route,out CampExpeditionNodeRecord[] nodes,out string failure)
        {
            nodes=null;failure=null;if(definition==null || support==null || route==null || string.IsNullOrEmpty(runId)){failure="Missing population inputs.";return false;}
            var materialIds=definition.items.Where(i=>i.visibleInTree&&i.inputs.Length==0).OrderBy(i=>i.id,StringComparer.Ordinal).Select(i=>i.id).ToArray();
            if(materialIds.Length!=4){failure="Expected four qualified materials, found "+materialIds.Length;return false;}
            var result=new List<CampExpeditionNodeRecord>();var random=new System.Random(seed ^ 0x43c55a);
            for(int m=0;m<materialIds.Length;m++)
                for(int copy=0;copy<3;copy++)
                {
                    float radius=24+m*12+copy*9;
                    if(!Place(materialIds[m],$"material-{m}-{copy}",radius,1,materialIds[m]=="resource.ironstone_ore"||copy==2)){failure="No supported approach for "+materialIds[m]+" at "+radius+"m after "+result.Count+" nodes.";return false;}
                }
            var prepared=definition.Find(definition.rootId).inputs.OrderBy(i=>i.itemId,StringComparer.Ordinal).ToArray();
            if(prepared.Length>0 && !Place(prepared[random.Next(prepared.Length)].itemId,"prepared-part-0",90,1,false)){failure="No supported prepared-part approach.";return false;}
            nodes=result.ToArray();return true;

            bool Place(string id,string key,float distance,int quantity,bool tool)
            {
                for(int attempt=0;attempt<64;attempt++)
                {
                    double angle=random.NextDouble()*Math.PI*2;
                    var proposal=origin+new Vector3((float)Math.Cos(angle),0,(float)Math.Sin(angle))*distance;
                    if(!support(proposal,out var point)||!route(origin,point)||result.Any(n=>Vector3.Distance(n.position,point)<4))continue;
                    result.Add(new CampExpeditionNodeRecord{key=runId+"/population-v1/"+key,itemId=id,remaining=quantity,position=point,requiresTool=tool});return true;
                }
                return false;
            }
        }
    }
}
