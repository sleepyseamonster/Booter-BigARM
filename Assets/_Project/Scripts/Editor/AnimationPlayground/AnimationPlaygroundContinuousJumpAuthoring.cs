using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    // Candidate-only private authoring. Never saves or reloads the author's scene.
    public static class AnimationPlaygroundContinuousJumpAuthoring
    {
        [Serializable] private sealed class Gauge {public string source;public float worldUnitsPerCurveUnit;}
        [Serializable] private sealed class Gauges {public Gauge[] gauges;}
        public static string Prepare(string revision)
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".animation-playground-validation-project")
                || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || string.IsNullOrEmpty(revision) || revision.Any(c=>!char.IsLetterOrDigit(c)))
                throw new InvalidOperationException("Author a named continuous jump draft only in the marked stable background candidate.");
            var receipt="JumpRepairQualification/continuous-source-"+revision+".json";
            if(File.Exists(receipt))throw new IOException("Preserve the previous source receipt.");
            Directory.CreateDirectory("JumpRepairQualification");
            var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
            var profile=bindings.airborne;profile.qualified=false;EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
            var gauges=JsonUtility.FromJson<Gauges>(File.ReadAllText("AirborneQualification/Gauge/summary.json"));
            var model=AssetDatabase.GetAssetPath(bindings.targetModel);
            var rows=new System.Collections.Generic.List<object>();
            void Create(AnimationPlaygroundJumpVariant variant,float lift,float apex,float contact,float end)
            {
                var path="Assets/_Project/Art/Animations/Mixamo/Qualification/AirbornePolicies/"+variant.sourceId+"OriginalHeightBaked.fbx";
                var factor=gauges.gauges.Single(g=>g.source==path).worldUnitsPerCurveUnit;
                if(!float.IsFinite(factor) || factor<.9f || factor>1.1f)throw new InvalidDataException("Retain the measured root-height gauge.");
                var start=lift-AnimationPlaygroundJumpTiming.AnticipationSeconds;
                var clip=AnimationPlaygroundClipDerivatives.CreateRange(path,variant.sourceId+"ContinuousPhysics"+revision,start,end);
                var clipPath=AssetDatabase.GetAssetPath(clip);
                var measured=AnimationPlaygroundMotionSampler.Measure(model,clipPath,bindings.visualScale,bindings.visualOffset,120);
                var localLift=lift-start;var localApex=apex-start;var localContact=contact-start;
                float Sole(float t)
                {
                    var frames=measured.frames;var at=Mathf.Clamp(t/measured.seconds*(frames.Length-1),0,frames.Length-1);
                    var i=Mathf.Min(frames.Length-2,Mathf.FloorToInt(at));var fraction=at-i;
                    return Mathf.Lerp(Mathf.Min(frames[i].leftSole.y,frames[i].rightSole.y),Mathf.Min(frames[i+1].leftSole.y,frames[i+1].rightSole.y),fraction);
                }
                float Ground(float t)=>-.908f-Sole(t);
                var from=Ground(localLift);var to=Ground(localContact);
                var interval=localContact-localLift;
                var entering=(from-Ground(localLift-1f/120))*120;
                var leaving=(Ground(localContact+1f/120)-to)*120;
                var liftHip=measured.frames.OrderBy(f=>Mathf.Abs(f.seconds-localLift)).First().hips.y;
                var contactHip=measured.frames.OrderBy(f=>Mathf.Abs(f.seconds-localContact)).First().hips.y;
                var amplitude=measured.frames.Where(f=>f.seconds>=localLift && f.seconds<=localContact)
                    .Max(f=>f.hips.y-Mathf.Lerp(liftHip,contactHip,(f.seconds-localLift)/interval));
                var rootBinding=AnimationUtility.GetCurveBindings(clip).Single(b=>b.propertyName=="RootT.y");
                var source=AnimationUtility.GetEditorCurve(clip,rootBinding);
                float Correction(float t)
                {
                    if(t<=localLift || t>=localContact)return Ground(t);
                    var u=(t-localLift)/interval;var u2=u*u;var u3=u2*u;
                    var baseline=(2*u3-3*u2+1)*from+(u3-2*u2+u)*entering*interval
                        +(-2*u3+3*u2)*to+(u3-u2)*leaving*interval;
                    var arc=Mathf.Sin(Mathf.PI*u);return baseline-amplitude*arc*arc;
                }
                var times=measured.frames.Select(f=>f.seconds).Concat(new[]{localLift,localApex,localContact}).Distinct().OrderBy(t=>t).ToArray();
                var keys=new Keyframe[times.Length];
                for(var i=0;i<times.Length;i++)
                {
                    var t=times[i];var lo=Mathf.Max(0,t-.001f);var hi=Mathf.Min(clip.length,t+.001f);
                    var value=source.Evaluate(t)+Correction(t)/factor;
                    var tangent=(source.Evaluate(hi)+Correction(hi)/factor-source.Evaluate(lo)-Correction(lo)/factor)/(hi-lo);
                    keys[i]=new Keyframe(t,value,tangent,tangent);
                }
                AnimationUtility.SetEditorCurve(clip,rootBinding,new AnimationCurve(keys));EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
                var result=AnimationPlaygroundMotionSampler.Measure(model,clipPath,bindings.visualScale,bindings.visualOffset,120);
                if(!result.finite)throw new InvalidDataException("The continuous jump contains a non-finite pose.");
                var grounded=result.frames.Where(f=>f.seconds<=localLift || f.seconds>=localContact).ToArray();
                if(grounded.Min(f=>Mathf.Min(f.leftSole.y,f.rightSole.y))<-.912f
                    || measured.frames.Where(f=>f.seconds<=localLift || f.seconds>=localContact).Any(f=>Mathf.Abs(Ground(f.seconds))>.18f))
                    throw new InvalidDataException("The source exceeds its existing grounded sole/correction envelope.");
                variant.continuous=clip;variant.liftTime=localLift;variant.apexTime=localApex;variant.contactTime=localContact;
                File.WriteAllText("JumpRepairQualification/"+clip.name+".json",JsonUtility.ToJson(result,true)+"\n");
                rows.Add(new {variant.sourceId,clipPath,start,end,variant.liftTime,variant.apexTime,variant.contactTime,amplitude,
                    groundedMinimum=grounded.Min(f=>Mathf.Min(f.leftSole.y,f.rightSole.y)),result.finite});
            }
            Create(profile.standing,.6333333f,.9666667f,1.2166667f,2.1666667f);
            Create(profile.running,.098f,.376f,.605f,.9f);
            Create(profile.unarmed,.8333333f,1.1666667f,1.4666667f,2.3333333f);
            profile.schema=2;
            if(!profile.Validate(out var reason,false))throw new InvalidDataException(reason);
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
            File.WriteAllText(receipt,Newtonsoft.Json.JsonConvert.SerializeObject(new {revision,scope="Candidate draft only; qualification remains false.",rows},Newtonsoft.Json.Formatting.Indented)+"\n");
            return receipt;
        }
    }
}
