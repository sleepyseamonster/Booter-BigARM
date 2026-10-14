using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundAssetImporter
    {
        [Serializable] private sealed class TransferPlan { public int schema_version; public TransferAsset[] assets; public RigCandidate[] target_rig_candidates; }
        [Serializable] private sealed class TransferAsset { public string destination; }
        [Serializable] private sealed class RigCandidate { public string planned_qualification_destination; }
        public const string TransferPlanPath="Docs/Design/Gameplay/AnimationPlayground20261013/asset-import-plan.json";
        public static int CreateFolderScaffolding()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Prepare playground folders only in Edit Mode outside compilation.");
            var plan=JsonUtility.FromJson<TransferPlan>(File.ReadAllText(TransferPlanPath));
            if(plan == null || plan.schema_version != 2) throw new InvalidDataException("Unsupported animation transfer plan.");
            var folders=new HashSet<string>(StringComparer.Ordinal);
            foreach(var asset in plan.assets) if(asset.destination.StartsWith("Assets/",StringComparison.Ordinal))
                folders.Add(Path.GetDirectoryName(asset.destination).Replace('\\','/'));
            foreach(var rig in plan.target_rig_candidates) folders.Add(Path.GetDirectoryName(rig.planned_qualification_destination).Replace('\\','/'));
            folders.Add("Assets/_Project/Settings/AnimationPlayground/PrivateBindings");
            var created=0;
            foreach(var folder in folders)
            {
                created+=EnsureFolder(folder);
                var placeholder=Path.Combine(folder,".gitkeep");
                if(!File.Exists(placeholder)) File.WriteAllText(placeholder,string.Empty);
            }
            return created;
        }
        private static int EnsureFolder(string folder)
        {
            if(AssetDatabase.IsValidFolder(folder)) return 0;
            if(!folder.StartsWith("Assets/_Project/",StringComparison.Ordinal) || folder.Contains(".."))
                throw new InvalidDataException("Folder is outside project-owned assets.");
            var parent=Path.GetDirectoryName(folder).Replace('\\','/');
            var count=EnsureFolder(parent);
            if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,Path.GetFileName(folder))))
                throw new IOException("Could not create Unity folder: "+folder);
            return count+1;
        }
    }
}
