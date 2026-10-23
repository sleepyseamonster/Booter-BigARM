using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public interface ICampExpeditionFile
    { CampExpeditionSnapshot Read(); void Write(CampExpeditionSnapshot candidate); }
    /// <summary>Single ordered writer. No autosave queue can coalesce or overtake a critical operation.</summary>
    public sealed class CampExpeditionFile : ICampExpeditionFile
    {
        [Serializable] private sealed class Envelope { public int version=1; public string payload,sha256; }
        private readonly string path,fingerprint;
        private readonly object gate=new();
        public CampExpeditionFile(string filePath,string contentFingerprint){path=filePath;fingerprint=contentFingerprint;}
        public static string Hash(string text){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
        public CampExpeditionSnapshot Read()
        {
            lock(gate)
            {
                if(!File.Exists(path))
                {
                    if(File.Exists(path+".pending") || File.Exists(path+".backup"))throw new IOException("Existing recovery files need review; a new profile was not created.");
                    return null;
                }
                var current=Decode(File.ReadAllText(path));
                if(File.Exists(path+".pending"))
                {
                    // Preserve every uncertain write. Only an exact already-committed candidate is harmless.
                    var pending=Decode(File.ReadAllText(path+".pending"));
                    if(pending.sequence>current.sequence || pending.sequence==current.sequence && JsonUtility.ToJson(pending)!=JsonUtility.ToJson(current))
                        throw new IOException("An uncertain pending save needs recovery; all files were preserved.");
                }
                return current;
            }
        }
        public void Write(CampExpeditionSnapshot candidate)
        {
            lock(gate)
            {
                Validate(candidate);var old=File.Exists(path)?Decode(File.ReadAllText(path)):null;
                if(old!=null)
                {
                    if(candidate.sequence==old.sequence && JsonUtility.ToJson(candidate)==JsonUtility.ToJson(old))return;
                    if(candidate.sequence!=checked(old.sequence+1))throw new InvalidOperationException("Save sequence is stale or out of order.");
                }
                else if(candidate.sequence!=1)throw new InvalidOperationException("The first durable sequence must be one.");
                var payload=JsonUtility.ToJson(candidate);var text=JsonUtility.ToJson(new Envelope{payload=payload,sha256=Hash(payload)});
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                bool reusePending=false;
                if(File.Exists(path+".pending"))
                {
                    var retained=Decode(File.ReadAllText(path+".pending"));
                    reusePending=JsonUtility.ToJson(retained)==payload;
                    if(!reusePending && retained.sequence> (old?.sequence??0))throw new IOException("Retained pending operation requires reconciliation.");
                    // A previous acknowledged duplicate is preserved before allocating a new pending file.
                    if(!reusePending)File.Move(path+".pending",path+".pending."+Guid.NewGuid().ToString("N")+".retained");
                }
                var bytes=Encoding.UTF8.GetBytes(text);
                if(!reusePending)
                    using(var stream=new FileStream(path+".pending",FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
                    {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                Decode(File.ReadAllText(path+".pending"));
                if(File.Exists(path))File.Replace(path+".pending",path,path+".backup",true);
                else File.Move(path+".pending",path);
                var committed=Decode(File.ReadAllText(path));
                if(committed.sequence!=candidate.sequence || Hash(JsonUtility.ToJson(committed))!=Hash(payload))throw new IOException("Durable acknowledgement did not match the candidate.");
            }
        }
        public bool HasPending=>File.Exists(path+".pending");
        // Explicit recovery choice: validate identity/ordering and preserve every original byte first.
        public CampExpeditionSnapshot FinishPending()
        {
            lock(gate)
            {
                if(!File.Exists(path+".pending"))throw new IOException("No pending checkpoint is available.");
                var candidate=Decode(File.ReadAllText(path+".pending"));var current=File.Exists(path)?Decode(File.ReadAllText(path)):null;
                if(current!=null && (candidate.profileId!=current.profileId || candidate.sequence!=current.sequence+1)
                    || current==null&&candidate.sequence!=1)throw new IOException("Pending checkpoint ordering requires manual recovery.");
                File.Copy(path+".pending",path+".pending."+Guid.NewGuid().ToString("N")+".recovered");
                if(current!=null)File.Replace(path+".pending",path,path+".backup",true);else File.Move(path+".pending",path);
                return Read();
            }
        }
        public CampExpeditionSnapshot RestoreBackup()
        {
            lock(gate)
            {
                if(!File.Exists(path+".backup"))throw new IOException("No backup checkpoint is available.");
                var backup=Decode(File.ReadAllText(path+".backup"));var suffix=".retained."+Guid.NewGuid().ToString("N");
                if(File.Exists(path))File.Copy(path,path+suffix);
                if(File.Exists(path+".pending"))File.Move(path+".pending",path+".pending"+suffix);
                // The validated backup is staged and flushed; its original remains available for inspection.
                string staging=path+".restore."+Guid.NewGuid().ToString("N");var bytes=File.ReadAllBytes(path+".backup");
                using(var stream=new FileStream(staging,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                Decode(File.ReadAllText(staging));if(File.Exists(path))File.Replace(staging,path,null,true);else File.Move(staging,path);return Read();
            }
        }
        private CampExpeditionSnapshot Decode(string text)
        {
            var envelope=JsonUtility.FromJson<Envelope>(text);
            if(envelope==null || envelope.version!=1 || string.IsNullOrEmpty(envelope.payload) || Hash(envelope.payload)!=envelope.sha256)
                throw new IOException("Save envelope is incompatible or damaged; recovery copies were preserved.");
            var snapshot=CampExpeditionSnapshot.FromJson(envelope.payload);Validate(snapshot);return snapshot;
        }
        private void Validate(CampExpeditionSnapshot snapshot)
        {
            if(snapshot==null || snapshot.version!=CampExpeditionSnapshot.CurrentVersion || snapshot.layoutVersion!=CampExpeditionLayout.Version || snapshot.contentFingerprint!=fingerprint
                || !Guid.TryParseExact(snapshot.profileId,"N",out _) || snapshot.sequence<1 || snapshot.storageCapacity<1
                || !double.IsFinite(snapshot.inGameSeconds) || snapshot.inGameSeconds<0 || !Enum.IsDefined(typeof(CampExpeditionPhase),snapshot.phase))
                throw new IOException("Save version/content requires recovery; it was not replaced or reseeded.");
            if(snapshot.booter==null||snapshot.legger==null||snapshot.storage==null||snapshot.receipts==null||snapshot.losses==null||snapshot.equipmentStates==null||snapshot.tracked==null||snapshot.mailbox==null||snapshot.projects==null||snapshot.discovered==null)
                throw new IOException("Composite save is incomplete; recovery files were preserved.");
            var projects=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach(var project in snapshot.projects)
                if(project==null||string.IsNullOrWhiteSpace(project.rootId)||!projects.Add(project.rootId)||project.remaining<1||project.remaining>99)
                    throw new IOException("Project quantities require recovery; no inventory was changed.");
            foreach(var ids in new[]{snapshot.tracked,snapshot.discovered})
            {
                var distinct=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach(var id in ids)if(string.IsNullOrWhiteSpace(id)||!distinct.Add(id))throw new IOException("Menu records require recovery; saved identities were preserved.");
            }
            var receipts=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);long previous=0;
            foreach(var receipt in snapshot.receipts)
            {if(receipt==null||string.IsNullOrEmpty(receipt.id)||string.IsNullOrEmpty(receipt.kind)||!receipts.Add(receipt.id)||receipt.sequence<=previous||receipt.sequence>snapshot.sequence)throw new IOException("Operation receipts require recovery.");previous=receipt.sequence;}
            if(snapshot.phase==CampExpeditionPhase.Exploring)
            {
                var run=snapshot.run;
                if(run==null||run.generationVersion!=CampExpeditionPopulation.Version||string.IsNullOrEmpty(run.id)||string.IsNullOrEmpty(run.terrainId)||run.nodes==null||!Finite(run.origin)||!Finite(run.playerPosition)||!Finite(run.leggerPosition))throw new IOException("Run identity/version/pose requires recovery.");
                var keys=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach(var node in run.nodes)if(node==null||string.IsNullOrEmpty(node.key)||!keys.Add(node.key)||string.IsNullOrEmpty(node.itemId)||!Finite(node.position)||node.remaining<0)throw new IOException("Population deltas require recovery.");
            }
            else if(snapshot.run!=null)throw new IOException("A settled town checkpoint cannot retain an active field run.");
            foreach(var state in snapshot.equipmentStates)
                if(state==null||!Guid.TryParseExact(state.instanceId,"N",out _)||state.phagia==null||state.phagia.version!=1||!double.IsFinite(state.phagia.remaining)||!float.IsFinite(state.phagia.capacity)||state.phagia.capacity<=0||state.phagia.remaining<0||state.phagia.remaining>state.phagia.capacity)throw new IOException("Instance-bound equipment contents require recovery.");
        }
        private static bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
    }
}
