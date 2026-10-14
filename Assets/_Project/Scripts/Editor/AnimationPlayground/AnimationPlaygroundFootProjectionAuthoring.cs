using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundFootProjectionAuthoring
    {
        public const string Path="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/FootProjection.asset";
        public static AnimationPlaygroundFootProjection Build(GameObject asset,bool wholeBody=false)
        {
            var scene=EditorSceneManager.NewPreviewScene();var model=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);
            try{return BuildFixture(asset,model,wholeBody);}
            finally{Object.DestroyImmediate(model);EditorSceneManager.ClosePreviewScene(scene);}
        }
        public static AnimationPlaygroundFootProjection BuildFixture(GameObject asset,GameObject model,bool wholeBody,bool excludeSoftGarments=false)
        {
            if(asset==null || model==null)throw new ArgumentException("A paired model fixture is required.");
            var result=ScriptableObject.CreateInstance<AnimationPlaygroundFootProjection>();
            try
            {
                var animator=model.GetComponentInChildren<Animator>();var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                var bones=new List<Transform>();var vertices=new List<AnimationPlaygroundFootVertex>();
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if(excludeSoftGarments && TopDown3DPlayerAnimationDriver.IsSoftGarment(skin))continue;
                    var mesh=skin.sharedMesh;var positions=mesh.vertices;var bindposes=mesh.bindposes;
                    using var counts=mesh.GetBonesPerVertex();using var weights=mesh.GetAllBoneWeights();var cursor=0;
                    for(var i=0;i<positions.Length;i++)
                    {
                        var leftWeight=0f;var rightWeight=0f;
                        for(var j=0;j<counts[i];j++)
                        {
                            var influence=weights[cursor+j];var bone=skin.bones[influence.boneIndex];
                            if(bone.IsChildOf(left))leftWeight+=influence.weight;
                            if(bone.IsChildOf(right))rightWeight+=influence.weight;
                        }
                        if(wholeBody || leftWeight>=.6f || rightWeight>=.6f)
                        {
                            var points=new List<AnimationPlaygroundSkinPoint>();
                            for(var j=0;j<counts[i];j++)
                            {
                                var influence=weights[cursor+j];if(influence.weight<=0)continue;
                                var bone=skin.bones[influence.boneIndex];var index=bones.IndexOf(bone);
                                if(index<0){index=bones.Count;bones.Add(bone);}
                                points.Add(new AnimationPlaygroundSkinPoint{bone=index,weight=influence.weight,position=bindposes[influence.boneIndex].MultiplyPoint3x4(positions[i])});
                            }
                            vertices.Add(new AnimationPlaygroundFootVertex{points=points.ToArray()});
                        }
                        cursor+=counts[i];
                    }
                }
                var paths=new string[bones.Count];for(var i=0;i<paths.Length;i++)paths[i]=AnimationUtility.CalculateTransformPath(bones[i],model.transform);
                result.model=asset;result.bonePaths=paths;result.vertices=vertices.ToArray();
                if(!result.Validate(asset,out var reason))throw new InvalidDataException(reason);return result;
            }
            catch{Object.DestroyImmediate(result);throw;}
        }
        public static void BuildFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Build private foot data only in the marked candidate.");
            var directory=System.IO.Path.GetFullPath("MobilityQualification/FootProjection");Directory.CreateDirectory(directory);
            try
            {
                if(File.Exists(Path))throw new IOException("Preserve an existing projection and its GUID.");
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var data=Build(bindings.targetModel);AssetDatabase.CreateAsset(data,Path);bindings.footProjection=data;EditorUtility.SetDirty(bindings);AssetDatabase.SaveAssets();
                File.WriteAllText(System.IO.Path.Combine(directory,"completed.json"),"{\"state\":\"projection-built-not-yet-qualified\",\"vertices\":"+data.vertices.Length+",\"bones\":"+data.bonePaths.Length+"}\n");
                EditorApplication.Exit(0);
            }
            catch(Exception error){File.WriteAllText(System.IO.Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }
    }
}
