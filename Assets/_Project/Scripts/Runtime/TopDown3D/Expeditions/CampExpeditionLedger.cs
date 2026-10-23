using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed class CampPreparedOperation
    {
        internal CampExpeditionSnapshot candidate;
        internal TopDown3DInventoryState[] drafts;
        internal long[] revisions;
        internal long expectedSequence;
        internal CampExpeditionLedger owner;
        public string Id=>candidate.receipts[candidate.receipts.Length-1].id;
        public long CommonsDebit { get; internal set; }
        public int AdditionalSlots { get; internal set; }
        public string Description { get; internal set; }
    }

    /// <summary>Composite local authority. Views propose commands; existing inventory rules prepare their drafts.</summary>
    public sealed class CampExpeditionLedger
    {
        private CampExpeditionSnapshot committed;
        private readonly ICampExpeditionFile file;
        private readonly TopDown3DInventoryState[] stores;
        private bool busy;
        public TopDown3DInventoryState Booter=>stores[0];
        public TopDown3DInventoryState Legger=>stores[1];
        public TopDown3DInventoryState Storage=>stores[2];
        public CampExpeditionSnapshot State=>Capture();
        public bool Busy=>busy;
        public bool SelectionMatches(int owner,int slot,string itemId,int quantity,string instanceId,long revision)
        {
            if(owner<0||owner>=stores.Length||slot<0||slot>=stores[owner].Capacity||stores[owner].Revision!=revision)return false;
            var current=stores[owner].Slots[slot];return !current.IsEmpty&&current.ItemId==itemId&&current.Quantity==quantity&&current.InstanceId==instanceId;
        }
        public bool LastCommitRequiresRecovery{get;private set;}
        public event Action Changed;
        public Action<CampExpeditionSnapshot> CaptureRuntime;
        public CampExpeditionLedger(ICampExpeditionFile saveFile,CampExpeditionSnapshot snapshot,TopDown3DInventoryState booter,TopDown3DInventoryState legger,TopDown3DInventoryState storage)
        {
            file=saveFile??throw new ArgumentNullException(nameof(saveFile));stores=new[]{booter,legger,storage};
            if(stores.Any(s=>s==null))throw new ArgumentNullException(nameof(booter));
            committed=snapshot?.Clone()??throw new ArgumentNullException(nameof(snapshot));
            var drafts=ValidateStores(committed);
            for(int i=0;i<3;i++)stores[i].InstallExpeditionDraft(drafts[i]);
        }
        public CampPreparedOperation Prepare(string kind,Action<CampExpeditionSnapshot,TopDown3DInventoryState[]> edit,string operationId=null)
        {
            if(busy)throw new InvalidOperationException("Another session command is committing.");
            var id=operationId??Guid.NewGuid().ToString("N");
            if(committed.HasReceipt(id))throw new InvalidOperationException("This operation is already committed.");
            var next=Capture();var revisions=stores.Select(s=>s.Revision).ToArray();var drafts=stores.Select(s=>s.CreateDraft()).ToArray();
            edit(next,drafts);
            next.discovered=next.discovered.Concat(drafts.SelectMany(store=>store.Slots).Where(slot=>!slot.IsEmpty).Select(slot=>slot.ItemId)).Distinct(StringComparer.Ordinal).ToArray();
            next.booter=drafts[0].CaptureSnapshot();next.legger=drafts[1].CaptureSnapshot();next.storage=drafts[2].CaptureSnapshot();next.storageCapacity=drafts[2].Capacity;
            next.sequence=checked(committed.sequence+1);
            next.receipts=next.receipts.Concat(new[]{new CampOperationReceipt{id=id,kind=kind,sequence=next.sequence}}).ToArray();
            ValidateStores(next);
            return new CampPreparedOperation{owner=this,candidate=next.Clone(),drafts=drafts,revisions=revisions,expectedSequence=committed.sequence,Description=kind};
        }
        public bool Commit(CampPreparedOperation operation,out string failure)
        {
            failure=null;
            LastCommitRequiresRecovery=false;
            if(operation==null || operation.owner!=this){failure="Invalid prepared operation.";return false;}
            if(committed.HasReceipt(operation.Id))return true;
            if(busy || operation.expectedSequence!=committed.sequence){failure="Session changed; refresh this action.";return false;}
            var locked=new List<TopDown3DInventoryState>();busy=true;
            try
            {
                for(int i=0;i<3;i++)
                {if(!stores[i].TryLock(operation.revisions[i])){failure="Inventory changed; refresh this action.";return false;}locked.Add(stores[i]);}
                try {file.Write(operation.candidate);}
                catch(Exception writeError)
                {
                    // A replacement may have succeeded before an acknowledgement failed. Reconcile the SAME receipt.
                    CampExpeditionSnapshot recovered=null;
                    try{recovered=file.Read();}catch(Exception){/* Preserve the candidate and every recovery file. */}
                    if(recovered==null || recovered.sequence!=operation.candidate.sequence || !recovered.HasReceipt(operation.Id)
                        || JsonUtility.ToJson(recovered)!=JsonUtility.ToJson(operation.candidate))
                    {LastCommitRequiresRecovery=true;failure="Save failed; ownership and funds were preserved. "+writeError.Message;return false;}
                }
                // All state is installed before any observer sees the new phase/account/stores.
                for(int i=0;i<3;i++)stores[i].InstallExpeditionDraft(operation.drafts[i]);
                committed=operation.candidate.Clone();
                for(int i=0;i<3;i++)stores[i].PublishDraft();
                Publish();return true;
            }
            finally{foreach(var state in locked)state.Unlock();busy=false;}
        }
        public CampPreparedOperation PrepareStorage(IReadOnlyList<(int owner,int slot)> manifest,long feePerSlot=1,bool completeReview=false)
        {
            if(committed.phase==CampExpeditionPhase.Exploring || manifest==null || manifest.Count==0 || feePerSlot<0)
                throw new InvalidOperationException("Choose town cargo to send to storage.");
            var explicitManifest=manifest.ToArray();
            if(explicitManifest.Distinct().Count()!=explicitManifest.Length)throw new InvalidOperationException("The manifest contains duplicate stacks.");
            var description=new List<string>();int additional=0;long debit=0;
            var operation=Prepare(completeReview?"review.store-all":"storage.transfer",(next,drafts)=>
            {
                var expanded=ResizeStorage(drafts[2],checked(drafts[2].Capacity+explicitManifest.Length));
                // Detach mounts before their frame. No unrelated/filter-hidden stacks are inferred here.
                foreach(var row in explicitManifest.OrderBy(r=>r.owner==0&&r.slot==1?1:0))
                {
                    if(row.owner<0 || row.owner>1 || row.slot<0 || row.slot>=drafts[row.owner].Capacity || drafts[row.owner].Slots[row.slot].IsEmpty)
                        throw new InvalidOperationException("The selected cargo changed.");
                    var slot=drafts[row.owner].Slots[row.slot];int expected=slot.Quantity;
                    description.Add($"{slot.Quantity} × {drafts[row.owner].ItemCatalog.GetRequiredDefinition(slot.ItemId).DisplayName} ({(row.owner==0?"Booter":"Legger")})");
                    var result=TopDown3DInventoryTransferService.TransferSlot(drafts[row.owner],expanded,row.slot);
                    if(!result.Succeeded || result.Quantity!=expected)throw new InvalidOperationException(result.Message??"The full stack does not fit.");
                }
                int used=expanded.Capacity;while(used>drafts[2].Capacity&&expanded.Slots[used-1].IsEmpty)used--;
                additional=used-drafts[2].Capacity;debit=checked(additional*feePerSlot);
                drafts[2]=ResizeStorage(expanded,used);next.commons=checked(next.commons-debit);
                if(completeReview)
                {if(next.phase!=CampExpeditionPhase.CargoReview)throw new InvalidOperationException("No expedition return is pending.");next.phase=CampExpeditionPhase.Town;}
            });
            operation.AdditionalSlots=additional;operation.CommonsDebit=debit;
            operation.Description=string.Join("\n",description)+$"\n\n{additional} permanent slots · {debit} Commons · balance {operation.candidate.commons}";
            return operation;
        }
        public CampPreparedOperation PrepareProject(string rootId,int remaining)
        {
            if(string.IsNullOrWhiteSpace(rootId)||remaining<0||remaining>99)throw new ArgumentException("Choose a project quantity from 1 to 99, or remove the project.");
            return Prepare("project.plan",(next,_)=>
            {
                var existing=next.projects.FirstOrDefault(p=>p.rootId==rootId);
                if(remaining==0)next.projects=next.projects.Where(p=>p.rootId!=rootId).ToArray();
                else if(existing!=null)existing.remaining=remaining;
                else next.projects=next.projects.Concat(new[]{new CampProjectRecord{rootId=rootId,remaining=remaining}}).ToArray();
            });
        }
        // Called inside the SAME prepared consumption/output command, never after a separate commit.
        public static void CompleteProjectOutputs(CampExpeditionSnapshot next,string rootId,int outputs)
        {
            if(outputs<1)throw new ArgumentOutOfRangeException(nameof(outputs));
            foreach(var project in next.projects)if(project.rootId==rootId)project.remaining=Math.Max(0,project.remaining-outputs);
            next.projects=next.projects.Where(p=>p.remaining>0).ToArray();
        }
        public IReadOnlyList<(int owner,int slot)> AllCarriedManifest()
        {
            var manifest=new List<(int,int)>();
            for(int o=0;o<2;o++)for(int s=0;s<stores[o].Capacity;s++)if(!stores[o].Slots[s].IsEmpty)manifest.Add((o,s));
            return manifest;
        }
        public CampPreparedOperation PrepareDeath(float respawnHealth=100)
        {
            if(committed.phase!=CampExpeditionPhase.Exploring||!float.IsFinite(respawnHealth)||respawnHealth<=0)throw new InvalidOperationException("An active expedition and valid full-health respawn are required.");
            return Prepare("expedition.death",(next,drafts)=>
            {
                var bundle=new CampRecoveryBundle{id=Guid.NewGuid().ToString("N"),ownerProfileId=next.profileId,lostRunId=next.run.id,
                    terrainId=next.run.terrainId,lossPosition=next.run.leggerPosition,lostRunSeed=next.run.seed,
                    state=CampRecoveryState.Sealed,booter=drafts[0].CaptureSnapshot(),legger=drafts[1].CaptureSnapshot(),cargoFootprint=drafts[0].OccupiedSlotCount+drafts[1].OccupiedSlotCount+1};
                var lostIds=new HashSet<string>(drafts[0].Slots.Concat(drafts[1].Slots).Select(s=>s.InstanceId).Where(s=>!string.IsNullOrEmpty(s)));
                bundle.equipmentStates=next.equipmentStates.Where(s=>lostIds.Contains(s.instanceId)).ToArray();next.equipmentStates=next.equipmentStates.Where(s=>!lostIds.Contains(s.instanceId)).ToArray();
                next.losses=next.losses.Concat(new[]{bundle}).ToArray();
                drafts[0]=new TopDown3DInventoryState(Booter.ItemCatalog,Booter.Policy);drafts[1]=new TopDown3DInventoryState(Legger.ItemCatalog,Legger.Policy);
                next.phase=CampExpeditionPhase.Town;next.run=null; // Commons/storage remain protected. No free replacement kit.
                if(next.vitals!=null)next.vitals=TopDown3DSurvivalSnapshot.Create(respawnHealth,next.vitals.Hunger,next.vitals.Thirst,next.vitals.Oxygen,next.vitals.Reserve);
            });
        }
        private CampExpeditionSnapshot Capture()
        {var copy=committed.Clone();copy.booter=Booter.CaptureSnapshot();copy.legger=Legger.CaptureSnapshot();copy.storage=Storage.CaptureSnapshot();CaptureRuntime?.Invoke(copy);return copy;}
        private TopDown3DInventoryState[] ValidateStores(CampExpeditionSnapshot snapshot)
        {
            if(snapshot.layoutVersion!=CampExpeditionLayout.Version || snapshot.booter==null || snapshot.legger==null || snapshot.storage==null
                || snapshot.storageCapacity!=snapshot.storage.Capacity || snapshot.storageCapacity<Storage.Capacity
                || snapshot.phase==CampExpeditionPhase.Exploring && snapshot.run==null)throw new InvalidOperationException("Composite snapshot is incomplete.");
            var policies=new[]{Booter.Policy,Legger.Policy,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,snapshot.storageCapacity,campStorage:true,canisterContents:Storage.Policy.SupportsCanisterContents)};
            var records=new[]{snapshot.booter,snapshot.legger,snapshot.storage};var drafts=new TopDown3DInventoryState[3];var identities=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<3;i++)
            {
                drafts[i]=new TopDown3DInventoryState(stores[i].ItemCatalog,policies[i]);var result=drafts[i].ApplySnapshot(records[i]);
                if(!result.Succeeded)throw new InvalidOperationException(result.Message);
                foreach(var slot in drafts[i].Slots)if(!string.IsNullOrEmpty(slot.InstanceId)&&!identities.Add(slot.InstanceId))throw new InvalidOperationException("Duplicate item ownership across stores.");
            }
            foreach(var loss in snapshot.losses)
            {
                if(loss.state==CampRecoveryState.Returned)continue;
                foreach(var lost in new[]{loss.booter,loss.legger})foreach(var slot in lost.Slots)
                    if(!string.IsNullOrEmpty(slot.InstanceId)&&!identities.Add(slot.InstanceId))throw new InvalidOperationException("A sealed recovery instance also has another owner.");
            }
            return drafts;
        }
        private static TopDown3DInventoryState ResizeStorage(TopDown3DInventoryState source,int capacity)
        {
            var rows=source.CaptureSnapshot().Slots.Take(capacity).ToList();
            while(rows.Count<capacity)rows.Add(TopDown3DInventorySlotSnapshot.Create(null,0));
            var resized=new TopDown3DInventoryState(source.ItemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,capacity,campStorage:true,canisterContents:source.Policy.SupportsCanisterContents));
            var result=resized.ApplySnapshot(TopDown3DInventorySnapshot.Create(source.CaptureSnapshot().Version,rows));
            if(!result.Succeeded)throw new InvalidOperationException(result.Message);return resized;
        }
        private void Publish(){if(Changed==null)return;foreach(Action observer in Changed.GetInvocationList())try{observer();}catch(Exception e){Debug.LogException(e);}}
    }
}
