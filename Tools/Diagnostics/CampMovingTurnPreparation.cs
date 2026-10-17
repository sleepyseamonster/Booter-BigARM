using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

// Candidate-only source extraction. No gameplay, physics stepping or scene writes.
public static class CampMovingTurnPreparation
{
    const string Folder="Assets/_Project/Art/Animations/Mixamo/Qualification/MovingTurn20261016";
    const string Private="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/";
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));
    static void Guard()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(Root,".git")) || !File.Exists(Path.Combine(Root,".animation-playground-validation-project")))
            throw new InvalidOperationException("Require the marked stable offline candidate.");
    }
    static void Stage(string expected,string next)
    {
        var marker=Path.Combine(Root,".moving-turn-preparation-stage");
        if((File.Exists(marker)?File.ReadAllText(marker):"")!=expected)throw new InvalidOperationException("Moving-turn preparation stage changed; preserve existing results.");
        File.WriteAllText(marker,next);
    }
    public static string Prepare(string receipt)
    {
        Guard();if(Directory.Exists(Path.Combine(Root,Folder)) || File.Exists(receipt))throw new IOException("Fresh moving-turn destinations required.");
        Stage("","prepared");
        Directory.CreateDirectory(Path.Combine(Root,Folder));AssetDatabase.Refresh();
        var binding=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(Private+"PlayerBindings.asset");
        var records=new List<object>();
        foreach(var spec in new[]{("Walk","WalkingLeftTurnSource",true,2),("Jog","RunningRightTurnSource",false,2),
            ("Run","RunningRightTurnSource",false,2),("FastRun","SprintTurnSource",false,1),("Sprint","SprintTurnSource",false,1)})
        foreach(var right in new[]{false,true})
        {
            var name="MovingTurn"+spec.Item1+(right?"Right":"Left");var import=Folder+"/"+name+".fbx";
            File.Copy(Path.Combine(Root,"Assets/_Project/Art/Animations/Mixamo/Locomotion/MovingTurns/"+spec.Item2+".fbx"),Path.Combine(Root,import));
            AssetDatabase.ImportAsset(import,ImportAssetOptions.ForceSynchronousImport);
            var im=(ModelImporter)AssetImporter.GetAtPath(import);im.animationType=ModelImporterAnimationType.Human;
            im.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;im.sourceAvatar=binding.sourceAvatar;im.importAnimation=true;
            im.materialImportMode=ModelImporterMaterialImportMode.None;im.animationCompression=ModelImporterAnimationCompression.Off;
            var take=im.defaultClipAnimations.Single();take.name=name;take.mirror=right==spec.Item3;take.loop=false;take.loopTime=false;take.loopPose=false;
            take.lockRootHeightY=false;take.keepOriginalPositionY=true;take.heightFromFeet=false;
            take.lockRootRotation=false;take.keepOriginalOrientation=true;take.lockRootPositionXZ=false;take.keepOriginalPositionXZ=true;
            im.clipAnimations=new[]{take};im.SaveAndReimport();
            var source=AnimationPlaygroundClipDerivatives.LoadClip(import);var clip=Object.Instantiate(source);clip.name=name;clip.hideFlags=HideFlags.None;
            var bindings=AnimationUtility.GetCurveBindings(source);var curves=bindings.ToDictionary(b=>b.propertyName,b=>AnimationUtility.GetEditorCurve(source,b));
            Quaternion Q(float t)=>new Quaternion(curves["RootQ.x"].Evaluate(t),curves["RootQ.y"].Evaluate(t),curves["RootQ.z"].Evaluate(t),curves["RootQ.w"].Evaluate(t)).normalized;
            float Yaw(Quaternion q){var f=q*Vector3.forward;return Mathf.Atan2(f.x,f.z)*Mathf.Rad2Deg;}
            var startYaw=Yaw(Q(0));var yaw=Mathf.DeltaAngle(startYaw,Yaw(Q(source.length)));
            var length=source.length*spec.Item4;var count=Mathf.CeilToInt(length*120);
            var maxMuscleSeam=bindings.Where(b=>!b.propertyName.StartsWith("Root")).Select(b=>Mathf.Abs(curves[b.propertyName].Evaluate(source.length)-curves[b.propertyName].Evaluate(0))).DefaultIfEmpty().Max();
            foreach(var b in bindings)
            {
                var original=curves[b.propertyName];var keys=new Keyframe[count+1];
                for(var i=0;i<=count;i++)
                {
                    var at=length*i/count;var phase=Mathf.Repeat(at/source.length,1);var t=phase*source.length;float value;
                    if(b.propertyName=="RootT.x" || b.propertyName=="RootT.z")value=0;
                    else if(b.propertyName.StartsWith("RootQ."))
                    {
                        var q=Quaternion.AngleAxis(-startYaw-yaw*phase,Vector3.up)*Q(t);
                        var first=Quaternion.AngleAxis(-startYaw,Vector3.up)*Q(0);
                        var end=Quaternion.AngleAxis(-startYaw-yaw,Vector3.up)*Q(source.length);
                        // Preserve lean/sway, removing only accumulated heading plus small seam residual.
                        q=Quaternion.Slerp(Quaternion.identity,first*Quaternion.Inverse(end),phase)*q;
                        value=b.propertyName=="RootQ.x"?q.x:b.propertyName=="RootQ.y"?q.y:b.propertyName=="RootQ.z"?q.z:q.w;
                    }
                    else value=original.Evaluate(t)-(original.Evaluate(source.length)-original.Evaluate(0))*phase;
                    keys[i]=new Keyframe(at,value);
                }
                var curve=new AnimationCurve(keys);for(var i=0;i<=count;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
                AnimationUtility.SetEditorCurve(clip,b,curve);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=length;settings.loopTime=true;settings.loopBlend=false;
            settings.keepOriginalOrientation=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;
            AnimationUtility.SetAnimationClipSettings(clip,settings);clip.frameRate=120;
            var destination=AnimationPlaygroundClipDerivatives.Folder+"/"+name+".anim";
            if(File.Exists(destination))throw new IOException("Preserve existing derivatives.");
            AssetDatabase.CreateAsset(clip,destination);AssetDatabase.SaveAssetIfDirty(clip);
            records.Add(new{name,source=import,original=spec.Item2,mirror=take.mirror,repetitions=spec.Item4,source.length,derivativeLength=length,removedYaw=yaw,maxMuscleSeam,destination});
        }
        File.WriteAllText(receipt,Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));return receipt;
    }
    public static string ShiftMirrors()
    {
        Guard();Stage("prepared","phased");
        foreach(var name in new[]{"MovingTurnWalkRight","MovingTurnJogLeft","MovingTurnRunLeft","MovingTurnFastRunLeft","MovingTurnSprintLeft"})
        {
            var clip=AnimationPlaygroundClipDerivatives.LoadClip(AnimationPlaygroundClipDerivatives.Folder+"/"+name+".anim");
            foreach(var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var old=AnimationUtility.GetEditorCurve(clip,binding);var count=Mathf.CeilToInt(clip.length*120);var keys=new Keyframe[count+1];
                for(var i=0;i<=count;i++){var t=clip.length*i/count;keys[i]=new Keyframe(t,old.Evaluate(Mathf.Repeat(t/clip.length+.25f,1)*clip.length));}
                var curve=new AnimationCurve(keys);for(var i=0;i<=count;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
                AnimationUtility.SetEditorCurve(clip,binding,curve);
            }
            EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        }
        return "Mirrored derivatives shifted by one semantic contact; originals retained.";
    }
    public static string ReviseFastRun()
    {
        Guard();Stage("phased","fast-set");foreach(var side in new[]{"Left","Right"})
        {
            var source=AnimationPlaygroundClipDerivatives.LoadClip(AnimationPlaygroundClipDerivatives.Folder+"/MovingTurnSprint"+side+".anim");
            var target=AnimationPlaygroundClipDerivatives.LoadClip(AnimationPlaygroundClipDerivatives.Folder+"/MovingTurnFastRun"+side+".anim");
            EditorUtility.CopySerialized(source,target);target.name="MovingTurnFastRun"+side;EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);
        }
        return "Fast-run uses its own sprint-turn derivatives at the unchanged accepted cadence.";
    }
    public static string CorrectMirrorReference()
    {
        Guard();Stage("fast-set","complete");foreach(var name in new[]{"MovingTurnSprintLeft","MovingTurnFastRunLeft"})
        {
            var clip=AnimationPlaygroundClipDerivatives.LoadClip(AnimationPlaygroundClipDerivatives.Folder+"/"+name+".anim");
            var bs=AnimationUtility.GetCurveBindings(clip).Where(b=>b.propertyName.StartsWith("RootQ.")).OrderBy(b=>b.propertyName).ToArray();
            var cs=bs.Select(b=>AnimationUtility.GetEditorCurve(clip,b)).ToArray();var count=cs[0].length;var values=new Keyframe[4][];for(var j=0;j<4;j++)values[j]=new Keyframe[count];
            for(var i=0;i<count;i++)
            {
                var t=cs[0].keys[i].time;var q=Quaternion.AngleAxis(18.7411f,Vector3.up)*new Quaternion(cs[1].Evaluate(t),cs[2].Evaluate(t),cs[3].Evaluate(t),cs[0].Evaluate(t));
                var v=new[]{q.w,q.x,q.y,q.z};for(var j=0;j<4;j++)values[j][i]=new Keyframe(t,v[j]);
            }
            for(var j=0;j<4;j++){var curve=new AnimationCurve(values[j]);for(var i=0;i<count;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(clip,bs[j],curve);}
            EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        }
        return "Measured mirrored sprint reference heading corrected; source yaw remains removed.";
    }
    public static string ExtractRemainingHeading()
    {
        Guard();Stage("complete","heading-extracted");
        foreach(var path in Directory.GetFiles(Path.Combine(Root,AnimationPlaygroundClipDerivatives.Folder),"MovingTurn*.anim").Select(p=>p.Substring(Root.Length+1).Replace('\\','/')))
        {
            var clip=AnimationPlaygroundClipDerivatives.LoadClip(path);var bs=AnimationUtility.GetCurveBindings(clip).Where(b=>b.propertyName.StartsWith("RootQ.")).OrderBy(b=>b.propertyName).ToArray();
            var cs=bs.Select(b=>AnimationUtility.GetEditorCurve(clip,b)).ToArray();var count=cs[0].length;var values=new Keyframe[4][];for(var j=0;j<4;j++)values[j]=new Keyframe[count];
            for(var i=0;i<count;i++)
            {
                var t=cs[0].keys[i].time;var q=new Quaternion(cs[1].Evaluate(t),cs[2].Evaluate(t),cs[3].Evaluate(t),cs[0].Evaluate(t));
                var f=q*Vector3.forward;var yaw=Mathf.Atan2(f.x,f.z)*Mathf.Rad2Deg;q=Quaternion.AngleAxis(-yaw,Vector3.up)*q;
                var v=new[]{q.w,q.x,q.y,q.z};for(var j=0;j<4;j++)values[j][i]=new Keyframe(t,v[j]);
            }
            for(var j=0;j<4;j++){var curve=new AnimationCurve(values[j]);for(var i=0;i<count;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(clip,bs[j],curve);}
            EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        }
        return "Full source root heading extracted; pitch/roll and muscle motion retained.";
    }
    public static string Export(string destination)
    {
        Guard();if(Directory.Exists(destination))throw new IOException("Fresh tape destination required.");Directory.CreateDirectory(destination);
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t=>t!=null);
        using var f=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,
            new object[]{true,SkinWeights.FourBones,Private+"ExpandedForwardGaitReview.asset"},null);
        T Field<T>(string n)=>(T)type.GetField(n).GetValue(f);
        var driver=Field<TopDown3DPlayerAnimationDriver>("Driver");var graph=Field<PlayableGraph>("Graph");var output=(AnimationPlayableOutput)graph.GetOutput(0);var original=output.GetSourcePlayable();
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var visual=(GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance",flags).GetValue(driver);
        var origin=(Vector3)typeof(TopDown3DPlayerAnimationDriver).GetField("visualLocalPosition",flags).GetValue(driver);
        var animator=visual.GetComponentInChildren<Animator>();var projection=Field<AnimationPlaygroundForwardLocomotionProfile>("Policy").footProjection;
        var bones=projection.bonePaths.Select(p=>p.Length==0?visual.transform:visual.transform.Find(p)).ToArray();
        Vector3 Point(AnimationPlaygroundFootVertex v){var p=Vector3.zero;foreach(var b in v.points)p+=bones[b.bone].TransformPoint(b.position)*b.weight;return driver.transform.InverseTransformPoint(p);}
        float[] C(Vector3 p)=>new[]{p.x,p.y,p.z};
        foreach(var path in Directory.GetFiles(Path.Combine(Root,AnimationPlaygroundClipDerivatives.Folder),"MovingTurn*.anim").Select(p=>p.Substring(Root.Length+1).Replace('\\','/')).OrderBy(p=>p))
        {
            var clip=AnimationPlaygroundClipDerivatives.LoadClip(path);var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);playable.SetSpeed(0);output.SetSourcePlayable(playable);
            var frames=new List<object>();var count=Mathf.CeilToInt(clip.length*120);
            for(var i=0;i<=count;i++)
            {
                visual.transform.localPosition=origin;playable.SetTime(i*(double)clip.length/count);graph.Evaluate(0);
                Vector3 Bone(HumanBodyBones bone)=>driver.transform.InverseTransformPoint(animator.GetBoneTransform(bone).position);
                frames.Add(new{seconds=i*(double)clip.length/count,hips=C(Bone(HumanBodyBones.Hips)),left=C(Bone(HumanBodyBones.LeftFoot)),right=C(Bone(HumanBodyBones.RightFoot)),vertices=projection.vertices.Select(v=>C(Point(v))).ToArray()});
            }
            var data=new{path,globalSkinWeights="FourBones",floor=-.91f,ids=projection.vertices.Select(v=>new{side=projection.bonePaths[v.points[0].bone].Contains(".L")?0:1}).ToArray(),frames};
            File.WriteAllText(Path.Combine(destination,clip.name+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(data));output.SetSourcePlayable(original);graph.DestroyPlayable(playable);
        }
        return destination;
    }
}
