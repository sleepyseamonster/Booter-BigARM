using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundLocomotionAuthoring
    {
        public const string ProfilePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/CalibratedLocomotion.asset";
        private const string ModelPath="Assets/_Project/Art/Characters/AnimationPlayground/Qualification/MaleBaseMeshOriginalRig.fbx";
        [Serializable] private sealed class RoleReport
        {
            public string role,path;
            public bool finite,loop;
            public float maximumCorrection,seamGap,rootDrift;
            public float[] leftContacts,rightContacts;
        }
        [Serializable] private sealed class DraftReport{public string state,error;public RoleReport[] roles;}
        [Serializable] private sealed class ContactReview
        {
            public string role;
            public bool finite,groundClear;
            public float minimumSole,maximumContactGap,seamGap;
        }
        [Serializable] private sealed class ReviewSet{public ContactReview[] roles;}
        [Serializable] private sealed class RoleRecipe
        {
            public string role,path,guid;
            public long clipLocalId;
            public float nominalForward,nominalLateral,maximumHeightCorrection,plantTime,releaseTime;
            public bool looping;
            public float[] leftContacts,rightContacts;
        }
        [Serializable] private sealed class RecipeSet{public string state="fixture-qualified-draft";public float standingHeight=1.8f;public RoleRecipe[] roles;}
        public static void WriteCurrentRecipe(string outputPath)
        {
            var profile=AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(ProfilePath);
            var reports=new List<RoleRecipe>();
            for(var i=0;i<12;i++)
            {
                var entry=profile.GetRequiredEntry((TopDown3DLocomotionClipRole)i);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(entry.Clip,out string guid,out long id);
                var maximum=0f;for(var frame=0;frame<=600;frame++)maximum=Mathf.Max(maximum,entry.HeightCorrection(frame/600f));
                reports.Add(new RoleRecipe{role=entry.Role.ToString(),path=AssetDatabase.GetAssetPath(entry.Clip),guid=guid,clipLocalId=id,
                    nominalForward=entry.NominalForwardSpeed,nominalLateral=entry.NominalLateralSpeed,looping=entry.Looping,
                    leftContacts=entry.LeftContactPhases,rightContacts=entry.RightContactPhases,plantTime=entry.PlantTime,releaseTime=entry.ReleaseTime,
                    maximumHeightCorrection=maximum});
            }
            File.WriteAllText(outputPath,JsonUtility.ToJson(new RecipeSet{roles=reports.ToArray()},true)+"\n");
        }
        public static void ReviewDraft(string evidenceDirectory)
        {
            var profile=AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(ProfilePath);
            if(profile==null || !profile.TryValidate(out _))throw new InvalidDataException("Restore a structurally valid private draft first.");
            var serialized=new SerializedObject(profile);var properties=serialized.FindProperty("clips");var reports=new List<ContactReview>();
            for(var i=0;i<12;i++)
            {
                var entry=profile.GetRequiredEntry((TopDown3DLocomotionClipRole)i);
                var curve=properties.GetArrayElementAtIndex(i).FindPropertyRelative("visualHeightCorrection").animationCurveValue;
                var aligned=AnimationPlaygroundMotionSampler.Measure(ModelPath,AssetDatabase.GetAssetPath(entry.Clip),.0898994803f,new Vector3(0,-.894405782f,0),60,false,curve);
                var minimum=float.PositiveInfinity;foreach(var frame in aligned.frames)minimum=Mathf.Min(minimum,frame.leftSole.y,frame.rightSole.y);
                var gap=0f;
                if(i>0 && entry.Looping)
                {
                    foreach(var phase in entry.LeftContactPhases)gap=Mathf.Max(gap,ContactGap(aligned,phase,true));
                    foreach(var phase in entry.RightContactPhases)gap=Mathf.Max(gap,ContactGap(aligned,phase,false));
                }
                else if(i>0)gap=ContactGap(aligned,entry.PlantTime,entry.PlantedFoot==TopDown3DFootSide.Left);
                reports.Add(new ContactReview{role=entry.Role.ToString(),finite=aligned.finite,minimumSole=minimum,
                    groundClear=minimum>=-.905f,maximumContactGap=gap,seamGap=aligned.seamFootGap});
                File.WriteAllText(Path.Combine(evidenceDirectory,entry.Role+"-aligned.json"),JsonUtility.ToJson(aligned,true)+"\n");
            }
            File.WriteAllText(Path.Combine(evidenceDirectory,"contact-review.json"),JsonUtility.ToJson(new ReviewSet{roles=reports.ToArray()},true)+"\n");
        }
        private static float ContactGap(AnimationPlaygroundMotionSampler.MotionMeasurement report,float phase,bool left)
        {
            var index=Mathf.Clamp(Mathf.RoundToInt(phase*(report.frames.Length-1)),0,report.frames.Length-1);
            return Mathf.Abs((left ? report.frames[index].leftSole.y : report.frames[index].rightSole.y)+.9f);
        }
        public static void RecalibrateDirectionalContacts(string evidenceDirectory)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            var profile=AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(ProfilePath);
            var serialized=new SerializedObject(profile);var entries=serialized.FindProperty("clips");
            foreach(var role in new[]{TopDown3DLocomotionClipRole.DirectionalLeft,TopDown3DLocomotionClipRole.DirectionalRight})
            {
                var measured=JsonUtility.FromJson<AnimationPlaygroundMotionSampler.MotionMeasurement>(File.ReadAllText(Path.Combine(evidenceDirectory,role+"-aligned.json")));
                var property=entries.GetArrayElementAtIndex((int)role);
                foreach(var side in new[]{"leftContactPhases","rightContactPhases"})
                {
                    var array=property.FindPropertyRelative(side);
                    for(var marker=0;marker<array.arraySize;marker++)
                    {
                        var seed=array.GetArrayElementAtIndex(marker).floatValue;var best=seed;var score=float.PositiveInfinity;
                        for(var i=0;i<measured.frames.Length-1;i++)
                        {
                            var phase=(float)i/(measured.frames.Length-1);var distance=Mathf.Abs(Mathf.DeltaAngle(seed*360,phase*360))/360;
                            if(distance>.1f)continue;
                            var gap=ContactGap(measured,phase,side=="leftContactPhases");var candidate=gap+distance*.001f;
                            if(candidate<score){score=candidate;best=phase;}
                        }
                        array.GetArrayElementAtIndex(marker).floatValue=best;
                    }
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if(!profile.TryValidate(out var error))throw new InvalidDataException(error);
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        }
        public static void BindReviewedStrafeAlternatives()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            var profile=AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(ProfilePath);
            var serialized=new SerializedObject(profile);var entries=serialized.FindProperty("clips");
            foreach(var index in new[]{4,5})
            {
                var name=index==4 ? "DirectionalStrafeLeftTwoCycles" : "DirectionalStrafeRightTwoCycles";
                var clip=AnimationPlaygroundClipDerivatives.LoadClip(AnimationPlaygroundClipDerivatives.Folder+"/"+name+".anim");
                var measured=AnimationPlaygroundMotionSampler.Measure(ModelPath,AssetDatabase.GetAssetPath(clip),.0898994803f,new Vector3(0,-.894405782f,0));
                var property=entries.GetArrayElementAtIndex(index);
                property.FindPropertyRelative("clip").objectReferenceValue=clip;property.FindPropertyRelative("looping").boolValue=clip.isLooping;
                property.FindPropertyRelative("visualHeightCorrection").animationCurveValue=AnimationPlaygroundClipDerivatives.GroundCorrection(measured,-.9f);
                Contacts(property,"rightContactPhases",0,.5f);
                if(index==4)Contacts(property,"leftContactPhases",.3625f,.8625f);else Contacts(property,"leftContactPhases",.1375f,.6375f);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if(!profile.TryValidate(out var error))throw new InvalidDataException(error);
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        }
        public static void CreateDraft(string evidenceDirectory)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Author profiles only in Edit Mode outside compilation.");
            if(File.Exists(ProfilePath) || File.Exists(ProfilePath+".meta"))throw new IOException("Preserve the existing profile; updates need its recorded recipe.");
            var baseline=AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>("Assets/_Project/Settings/Player/TopDown3DLocomotionClipProfile.asset");
            var profile=Object.Instantiate(baseline);profile.name="Calibrated Camp Loop locomotion draft";
            var serialized=new SerializedObject(profile);var entries=serialized.FindProperty("clips");var reports=new List<RoleReport>();
            Directory.CreateDirectory(evidenceDirectory);
            try
            {
                var loops=new[]{"IdleBreathingLoop","WalkTwoCycles","RunTwoCycles","SprintTwoCycles"};
                for(var i=0;i<12;i++)
                {
                    var property=entries.GetArrayElementAtIndex(i);
                    var clip=i<4 ? AnimationPlaygroundClipDerivatives.LoadClip(AnimationPlaygroundClipDerivatives.Folder+"/"+loops[i]+".anim") : baseline.GetRequiredEntry((TopDown3DLocomotionClipRole)i).Clip;
                    var path=AssetDatabase.GetAssetPath(clip);
                    var measured=AnimationPlaygroundMotionSampler.Measure(ModelPath,path,.0898994803f,new Vector3(0,-.894405782f,0));
                    var correction=AnimationPlaygroundClipDerivatives.GroundCorrection(measured,-.9f);
                    File.WriteAllText(Path.Combine(evidenceDirectory,((TopDown3DLocomotionClipRole)i)+"-unadjusted.json"),JsonUtility.ToJson(measured,true)+"\n");
                    property.FindPropertyRelative("clip").objectReferenceValue=clip;
                    property.FindPropertyRelative("looping").boolValue=clip.isLooping;
                    property.FindPropertyRelative("visualHeightCorrection").animationCurveValue=correction;
                    if(i==1){Contacts(property,"leftContactPhases",.246478873f,.746478873f);Contacts(property,"rightContactPhases",0,.5f);}
                    if(i==2){Contacts(property,"leftContactPhases",.25f,.75f);Contacts(property,"rightContactPhases",0,.5f);}
                    if(i==3){Contacts(property,"leftContactPhases",.125f,.75f);Contacts(property,"rightContactPhases",0,.484375f);}
                    if(i==1)property.FindPropertyRelative("nominalForwardSpeed").floatValue=1.4f;
                    if(i==2 || i==4 || i==5)property.FindPropertyRelative("nominalForwardSpeed").floatValue=2.7f;
                    if(i==3)property.FindPropertyRelative("nominalForwardSpeed").floatValue=3.8f;
                    if(i==4 || i==5)property.FindPropertyRelative("nominalLateralSpeed").floatValue=(i==4 ? -1 : 1)*1.54f;
                    var maximum=0f;foreach(var key in correction.keys)maximum=Mathf.Max(maximum,key.value);
                    reports.Add(new RoleReport{role=((TopDown3DLocomotionClipRole)i).ToString(),path=path,finite=measured.finite,loop=clip.isLooping,
                        maximumCorrection=maximum,seamGap=measured.seamFootGap,rootDrift=measured.rootDrift,
                        leftContacts=Read(property.FindPropertyRelative("leftContactPhases")),rightContacts=Read(property.FindPropertyRelative("rightContactPhases"))});
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if(!profile.TryValidate(out var error))throw new InvalidDataException(error);
                AssetDatabase.CreateAsset(profile,ProfilePath);AssetDatabase.SaveAssetIfDirty(profile);
                File.WriteAllText(Path.Combine(evidenceDirectory,"draft-summary.json"),JsonUtility.ToJson(new DraftReport{state="structural-draft-needs-contact-review",roles=reports.ToArray()},true)+"\n");
            }
            catch(Exception error)
            {
                File.WriteAllText(Path.Combine(evidenceDirectory,"draft-summary.json"),JsonUtility.ToJson(new DraftReport{state="failed",error=error.ToString(),roles=reports.ToArray()},true)+"\n");
                if(!AssetDatabase.Contains(profile))Object.DestroyImmediate(profile);throw;
            }
        }
        private static void Contacts(SerializedProperty property,string name,float first,float second)
        {var array=property.FindPropertyRelative(name);array.arraySize=2;array.GetArrayElementAtIndex(0).floatValue=first;array.GetArrayElementAtIndex(1).floatValue=second;}
        private static float[] Read(SerializedProperty array)
        {var values=new float[array.arraySize];for(var i=0;i<values.Length;i++)values[i]=array.GetArrayElementAtIndex(i).floatValue;return values;}
    }
}
