using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Read-only support/exclusion/approach qualification on authored, always-loaded collision terrain.</summary>
    public sealed class CampExpeditionTerrain
    {
        private readonly Terrain[] terrains;
        private readonly Transform booter,legger;
        private readonly Vector3 footprint;
        public int Queries{get;private set;}
        public CampExpeditionTerrain(Terrain[] sources,Transform player,Transform companion)
        {
            terrains=sources.Where(t=>t!=null&&t.isActiveAndEnabled&&t.terrainData!=null).OrderBy(t=>t.transform.position.x).ThenBy(t=>t.transform.position.z).ThenBy(t=>t.name,System.StringComparer.Ordinal).ToArray();
            booter=player;legger=companion;var shape=companion.GetComponent<Collider>().bounds;
            footprint=new Vector3(Mathf.Max(.8f,shape.extents.x),Mathf.Max(1,shape.extents.y),Mathf.Max(.8f,shape.extents.z));
        }
        public bool Support(Vector3 proposed,out Vector3 result)
        {
            result=default;
            foreach(var terrain in terrains)
            {
                var o=terrain.transform.position;var size=terrain.terrainData.size;if(proposed.x<o.x+2||proposed.z<o.z+2||proposed.x>o.x+size.x-2||proposed.z>o.z+size.z-2)continue;
                var collider=terrain.GetComponent<TerrainCollider>();Queries++;
                if(collider==null||!collider.enabled||collider.terrainData!=terrain.terrainData||!collider.Raycast(new Ray(new Vector3(proposed.x,o.y+size.y+3,proposed.z),Vector3.down),out var hit,size.y+6)||Vector3.Angle(hit.normal,Vector3.up)>20)continue;
                Queries++;bool clear=true;foreach(var solid in Physics.OverlapBox(hit.point+Vector3.up*(footprint.y+.15f),footprint,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                    if(solid is not TerrainCollider&&!solid.transform.IsChildOf(booter)&&!solid.transform.IsChildOf(legger)){clear=false;break;}
                if(clear){result=hit.point;return true;}
            }
            return false;
        }
        public bool Route(Vector3 a,Vector3 b)
        {
            var previous=a;int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/2);
            for(int i=1;i<=steps;i++)
            {if(!Support(Vector3.Lerp(a,b,i/(float)steps),out var step)||Mathf.Abs(step.y-previous.y)>1.1f)return false;previous=step;}
            return true;
        }
    }
}
