using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public interface ICampDurableGatherTarget
    { bool TryCommitHarvest(TopDown3DInventoryState destination,out string failure);string Failure{get;} }
    public sealed class CampExpeditionSource:MonoBehaviour,ITopDown3DInteractable,ITopDown3DTargetColliderOwner,ITopDown3DTargetEpoch,ICampDurableGatherTarget
    {
        private CampExpeditionSession session;private string nodeKey;private Collider workFace;private uint epoch;
        private CampExpeditionSourceView view;
        private CampExpeditionNodeRecord Record=>session?.Node(nodeKey);
        public Object UnityObject=>this;
        public string StableId=>nodeKey;
        public uint Epoch=>epoch;
        public string Prompt=>(view!=null?view.Verb(Record?.requiresTool==true):"Gather")+" "+session.Configuration.definition.Find(Record?.itemId)?.DisplayName
            +(view!=null?" · "+view.SourceName:"")+(Record?.requiresTool==true?" · fitted multi-tool":"");
        public Vector3 InteractionPoint=>view!=null?view.WorkAnchor.position:transform.position+Vector3.up*.5f;
        public float InteractionRange=>2.2f;
        public float ActionDuration=>1.3f;
        public TopDown3DItemAmount Reward=>new(Record?.itemId,1);
        public bool IsAvailable=>isActiveAndEnabled && Record?.remaining>0 && session.CanSimulateField && (Record.requiresTool==false||session.HasDiggingTool);
        public string Failure{get;private set;}
        public bool OwnsCollider(Collider collider)=>view!=null?view.Owns(collider):collider==workFace;
        public bool TryConsume(out int remainingUses){remainingUses=Record?.remaining??0;return false;}
        public void Configure(CampExpeditionSession owner,string key,Collider collider){session=owner;nodeKey=key;workFace=collider;}
        public void Configure(CampExpeditionSession owner,string key,CampExpeditionSourceView presentation)
        {session=owner;nodeKey=key;view=presentation;}
        public void RefreshPresentation()=>view?.Present(Record);
        public bool TryCommitHarvest(TopDown3DInventoryState destination,out string failure)
        {
            bool success=session.TryHarvest(nodeKey,destination,out failure);Failure=failure;if(success)epoch++;return success;
        }
    }
}
