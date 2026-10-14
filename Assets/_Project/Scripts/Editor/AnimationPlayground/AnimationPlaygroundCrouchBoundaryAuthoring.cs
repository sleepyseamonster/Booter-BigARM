using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundCrouchBoundaryAuthoring
    {
        [Serializable] public sealed class Boundary
        {
            public string name;
            public float feet,hips,head,hands;
        }
        [Serializable] private sealed class Report { public string state="measured-boundary-gaps"; public Boundary[] boundaries; }
        public static void MeasureFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Measure boundaries only in the marked validation copy.");
            var directory=Path.GetFullPath("MobilityQualification/Boundaries");Directory.CreateDirectory(directory);
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var model=AssetDatabase.GetAssetPath(bindings.targetModel);
                var standing=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
                var standingFrames=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(standing.Clip),bindings.visualScale,bindings.visualOffset,60,
                    false,StandingCorrection(standing)).frames;
                var profile=bindings.crouch;
                var idle=Read(model,profile.Get(AnimationPlaygroundCrouchRole.Idle),bindings);
                var enter=Read(model,profile.Get(AnimationPlaygroundCrouchRole.Enter),bindings);
                var exit=Read(model,profile.Get(AnimationPlaygroundCrouchRole.Exit),bindings);
                var rows=new List<Boundary>();
                for(var i=0;i<4;i++)
                {
                    var currentIdle=idle[Mathf.RoundToInt((idle.Length-1)*i/4f)];
                    var currentStanding=standingFrames[Mathf.RoundToInt((standingFrames.Length-1)*i/4f)];
                    rows.Add(Gap("Standing"+i+"ToEnter",currentStanding,enter[0]));
                    rows.Add(Gap("EnterToIdle"+i,enter[^1],currentIdle));
                    rows.Add(Gap("Idle"+i+"ToExit",currentIdle,exit[0]));
                    rows.Add(Gap("ExitToStanding"+i,exit[^1],currentStanding));
                }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{boundaries=rows.ToArray()},true)+"\n");EditorApplication.Exit(0);
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }
        private static AnimationPlaygroundMotionSampler.PoseFrame[] Read(string model,AnimationPlaygroundCrouchEntry entry,TopDown3DAnimationPlaygroundBindings bindings)
            =>AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(entry.clip),bindings.visualScale,bindings.visualOffset,60,false,entry.heightCorrection).frames;
        private static AnimationCurve StandingCorrection(TopDown3DLocomotionClipEntry entry)
        {
            var keys=new Keyframe[129];for(var i=0;i<keys.Length;i++)keys[i]=new Keyframe(i/128f,entry.HeightCorrection(i/128f));
            return new AnimationCurve(keys);
        }
        private static Boundary Gap(string name,AnimationPlaygroundMotionSampler.PoseFrame a,AnimationPlaygroundMotionSampler.PoseFrame b)
            =>new(){name=name,feet=Mathf.Max(Vector3.Distance(a.leftFoot,b.leftFoot),Vector3.Distance(a.rightFoot,b.rightFoot)),
                hips=Vector3.Distance(a.hips,b.hips),head=Vector3.Distance(a.head,b.head),
                hands=Mathf.Max(Vector3.Distance(a.leftHand,b.leftHand),Vector3.Distance(a.rightHand,b.rightHand))};
    }
}
