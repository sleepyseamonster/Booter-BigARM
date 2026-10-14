using System;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundLowMotionBindingAuthoring
    {
        public static void BindDraftFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Bind a low-motion draft only in the marked candidate.");
            var directory=Path.GetFullPath("LowMotionQualification/Binding");Directory.CreateDirectory(directory);var exitCode=0;
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundLowMotionProfile>(AnimationPlaygroundLowMotionAuthoring.ProfilePath);
                if(bindings.lowMotion!=null || profile==null || profile.qualified || !profile.Validate(out _,false))
                    throw new InvalidDataException("Preserve an existing binding or qualified profile.");
                bindings.lowMotion=profile;
                if(!bindings.ValidatePlayer(out var reason))throw new InvalidDataException(reason);
                EditorUtility.SetDirty(bindings);AssetDatabase.SaveAssetIfDirty(bindings);
                File.WriteAllText(Path.Combine(directory,"completed.json"),"{\"state\":\"low-controller-bound-with-physical-gate-closed\"}\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            EditorApplication.Exit(exitCode);
        }
    }
}
