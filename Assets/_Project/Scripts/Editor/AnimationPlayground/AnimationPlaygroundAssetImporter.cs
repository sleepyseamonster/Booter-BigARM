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
        [Serializable] private sealed class TransferAsset { public string destination,asset_id,planned_clip_name; }
        [Serializable] private sealed class RigCandidate { public string planned_qualification_destination; }
        public const string TransferPlanPath="Docs/Design/Gameplay/AnimationPlayground20261013/asset-import-plan.json";
        public static readonly string[] RigPaths =
        {
            "Assets/_Project/Art/Characters/AnimationPlayground/MaleBaseMeshMixamo.fbx",
            "Assets/_Project/Art/Characters/AnimationPlayground/Qualification/MaleBaseMeshOriginalRig.fbx",
            "Assets/_Project/Art/Characters/AnimationPlayground/Qualification/MaleBaseMeshBlenderRig.fbx"
        };

        public static void ImportRig(string path)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Import rigs only in Edit Mode outside compilation.");
            if(Array.IndexOf(RigPaths,path)<0 || !File.Exists(path))throw new InvalidDataException("Expected a staged planned rig candidate.");
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer==null)throw new InvalidDataException("Rig has no model importer.");
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=false;
            importer.optimizeGameObjects=false;
            importer.isReadable=true;
            importer.globalScale=1;
            importer.useFileScale=true;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.animationCompression=ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();
            if(path!=RigPaths[0])
            {
                var original=AssetImporter.GetAtPath(RigPaths[1]) as ModelImporter;
                if(original==null)throw new InvalidDataException("Import the original deform rig before target alternatives.");
                var definition=importer.humanDescription;
                var mapped=new List<HumanBone>();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var names=new HashSet<string>();foreach(var bone in model.GetComponentsInChildren<Transform>(true))names.Add(bone.name);
                foreach(var bone in original.humanDescription.human)
                {
                    // Rigify's head-tip was auto-mapped as an eye; it has no eye deformation role.
                    if(bone.humanName=="LeftEye" || bone.humanName=="RightEye" || bone.humanName=="Jaw")continue;
                    if(!names.Contains(bone.boneName))throw new InvalidDataException("Target lacks deform bone: "+bone.boneName);
                    mapped.Add(bone);
                }
                definition.human=mapped.ToArray();importer.humanDescription=definition;
                importer.SaveAndReimport();
            }
        }

        public static void ImportMotion(string path)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Import qualification motions only in Edit Mode outside compilation.");
            var plan=JsonUtility.FromJson<TransferPlan>(File.ReadAllText(TransferPlanPath));
            TransferAsset recipe=null;
            foreach(var item in plan.assets)if(item.destination==path)recipe=item;
            if(recipe==null || !path.StartsWith("Assets/_Project/Art/Animations/Mixamo/",StringComparison.Ordinal) || !File.Exists(path))
                throw new InvalidDataException("Use a staged canonical motion destination.");
            Avatar source=null;
            foreach(var item in AssetDatabase.LoadAllAssetsAtPath(RigPaths[0]))if(item is Avatar avatar && avatar.isValid && avatar.isHuman)source=avatar;
            if(source==null)throw new InvalidDataException("Qualify the matching Mixamo source Avatar first.");
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;importer.sourceAvatar=source;
            importer.importAnimation=true;importer.optimizeGameObjects=false;
            importer.animationCompression=ModelImporterAnimationCompression.Off;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            var clips=importer.defaultClipAnimations;
            if(clips.Length!=1)throw new InvalidDataException("Qualification expects exactly one source clip per motion.");
            clips[0].name=recipe.planned_clip_name;clips[0].loopTime=false;clips[0].loopPose=false;
            importer.clipAnimations=clips;importer.SaveAndReimport();
        }
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
