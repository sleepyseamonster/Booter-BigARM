using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum CampExpeditionPhase { Town, Exploring, CargoReview }
    [Serializable] public sealed class CampExpeditionNodeRecord
    { public string key,itemId; public Vector3 position; public int remaining; public bool requiresTool; }
    [Serializable] public sealed class CampExpeditionRunRecord
    {
        public string id,terrainId;
        public int seed,generationVersion=1;
        public Vector3 origin,playerPosition,leggerPosition;
        public Quaternion playerRotation=Quaternion.identity,leggerRotation=Quaternion.identity;
        public bool riding;
        public CampExpeditionNodeRecord[] nodes=Array.Empty<CampExpeditionNodeRecord>();
    }
    [Serializable] public sealed class CampOperationReceipt
    { public string id,kind; public long sequence; }
    [Serializable] public sealed class CampEquipmentStateRecord
    {public string instanceId;public TopDown3DAlgaeCanisterSnapshot phagia;}
    [Serializable] public sealed class CampProjectRecord
    { public string rootId; public int remaining=1; }
    [Serializable] public sealed class CampExpeditionSnapshot
    {
        [Serializable] private sealed class VersionHeader {public int version;}
        public const int CurrentVersion=2;
        public int version=CurrentVersion,layoutVersion=CampExpeditionLayout.Version,storageCapacity=24;
        public string profileId,contentFingerprint;
        public long sequence,commons=50;
        public CampExpeditionPhase phase;
        public double inGameSeconds;
        public TopDown3DInventorySnapshot booter,legger,storage;
        public TopDown3DSurvivalSnapshot vitals;
        public CampExpeditionRunRecord run;
        public string[] tracked=Array.Empty<string>();
        public string[] discovered=Array.Empty<string>();
        public CampProjectRecord[] projects=Array.Empty<CampProjectRecord>();
        public CampOperationReceipt[] receipts=Array.Empty<CampOperationReceipt>();
        public CampRecoveryBundle[] losses=Array.Empty<CampRecoveryBundle>();
        public CampMailboxDelivery[] mailbox=Array.Empty<CampMailboxDelivery>();
        public CampEquipmentStateRecord[] equipmentStates=Array.Empty<CampEquipmentStateRecord>();
        // Unity inline serialization materializes null class fields. Treat only its empty settled-run
        // sentinel as absent; never erase a populated or active run during recovery validation.
        public static CampExpeditionSnapshot FromJson(string json)
        {
            var value=JsonUtility.FromJson<CampExpeditionSnapshot>(json);
            if(value!=null)value.version=JsonUtility.FromJson<VersionHeader>(json)?.version??0;
            if(value!=null&&value.version==1)
            {
                // Additive, in-memory migration only. Reading never rewrites the player's file.
                // The next ordinary durable command retains the original as the normal backup.
                value.projects=Array.Empty<CampProjectRecord>();value.discovered=Array.Empty<string>();
                value.version=CurrentVersion;
            }
            var run=value?.run;
            if(value!=null&&value.phase!=CampExpeditionPhase.Exploring&&run!=null&&string.IsNullOrEmpty(run.id)&&string.IsNullOrEmpty(run.terrainId)&&run.seed==0&&run.origin==Vector3.zero&&run.playerPosition==Vector3.zero&&run.leggerPosition==Vector3.zero&&!run.riding&&(run.nodes==null||run.nodes.Length==0))value.run=null;
            return value;
        }
        public CampExpeditionSnapshot Clone()=>FromJson(JsonUtility.ToJson(this));
        public bool HasReceipt(string id){foreach(var receipt in receipts)if(receipt.id==id)return true;return false;}
    }
}
