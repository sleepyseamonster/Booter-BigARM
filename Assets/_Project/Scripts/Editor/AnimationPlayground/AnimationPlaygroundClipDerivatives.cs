using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundClipDerivatives
    {
        public const string Folder="Assets/_Project/Art/Animations/Mixamo/Derived";
        public static AnimationClip CreateLoop(string sourcePath,string name,float originPhase,int repetitions=1)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Author private derivatives only in Edit Mode outside compilation.");
            if(!float.IsFinite(originPhase) || originPhase<0 || originPhase>=1 || repetitions<1 || repetitions>2
                || name!=Path.GetFileName(name) || string.IsNullOrWhiteSpace(name))throw new ArgumentException("Invalid loop recipe.");
            var source=LoadClip(sourcePath);
            if(!source.isHumanMotion || source.length<=0)throw new InvalidDataException("Derivative needs a Humanoid source.");
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Animations/Mixamo","Derived");
            var destination=Folder+"/"+name+".anim";
            if(File.Exists(destination) || File.Exists(destination+".meta"))throw new IOException("Derivative destination already exists; preserve its GUID and recipe.");
            var derivative=Object.Instantiate(source);derivative.name=name;derivative.hideFlags=HideFlags.None;
            try
            {
                var length=source.length*repetitions;var samples=Mathf.CeilToInt(length*Mathf.Max(60,source.frameRate*2));
                foreach(var binding in AnimationUtility.GetCurveBindings(source))
                {
                    var curve=AnimationUtility.GetEditorCurve(source,binding);if(curve==null)continue;
                    var keys=new Keyframe[samples+1];
                    for(var i=0;i<keys.Length;i++)
                    {
                        var at=length*i/samples;var phase=Mathf.Repeat(at/source.length+originPhase,1);
                        keys[i]=new Keyframe(at,curve.Evaluate(phase*source.length));
                    }
                    var shifted=new AnimationCurve(keys);
                    for(var i=0;i<keys.Length;i++)
                    {AnimationUtility.SetKeyLeftTangentMode(shifted,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(shifted,i,AnimationUtility.TangentMode.Linear);}
                    AnimationUtility.SetEditorCurve(derivative,binding,shifted);
                }
                var settings=AnimationUtility.GetAnimationClipSettings(derivative);
                settings.startTime=0;settings.stopTime=length;settings.loopTime=true;settings.loopBlend=false;
                AnimationUtility.SetAnimationClipSettings(derivative,settings);
                derivative.frameRate=Mathf.Max(60,source.frameRate*2);
                AssetDatabase.CreateAsset(derivative,destination);AssetDatabase.SaveAssetIfDirty(derivative);
                return derivative;
            }
            catch{if(!AssetDatabase.Contains(derivative))Object.DestroyImmediate(derivative);throw;}
        }
        public static AnimationClip LoadClip(string path)
        {
            AnimationClip clip=null;
            foreach(var item in AssetDatabase.LoadAllAssetsAtPath(path))
                if(item is AnimationClip candidate && !candidate.name.StartsWith("__preview__"))
                {if(clip!=null)throw new InvalidDataException("Expected a single motion clip.");clip=candidate;}
            return clip ?? throw new FileNotFoundException("Imported source clip is absent.",path);
        }
        public static AnimationCurve GroundCorrection(AnimationPlaygroundMotionSampler.MotionMeasurement measured,float feetPlane)
        {
            if(measured==null || !measured.finite || measured.frames.Length<2 || !float.IsFinite(feetPlane))
                throw new ArgumentException("Ground correction requires a complete calibrated sole envelope.");
            var keys=new Keyframe[measured.frames.Length];
            for(var i=0;i<keys.Length;i++)
            {
                var frame=measured.frames[i];var correction=Mathf.Max(0,feetPlane-Mathf.Min(frame.leftSole.y,frame.rightSole.y));
                if(!float.IsFinite(correction) || correction>.18f)throw new InvalidDataException("Retargeted ground correction exceeds the reviewed 18cm ceiling.");
                keys[i]=new Keyframe(frame.seconds/measured.seconds,correction);
            }
            var curve=new AnimationCurve(keys);
            for(var i=0;i<keys.Length;i++)
            {AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
            return curve;
        }
    }
}
