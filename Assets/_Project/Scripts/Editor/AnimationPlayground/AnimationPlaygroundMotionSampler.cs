using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundMotionSampler
    {
        [Serializable] public sealed class PoseFrame
        {
            public float seconds;
            public Vector3 root,hips,head,leftFoot,rightFoot,leftToes,rightToes,leftHand,rightHand,leftSole,rightSole;
            public Vector3 bodyMinimum,bodyMaximum;
            public Quaternion rootRotation,hipsRotation,leftFootRotation,rightFootRotation;
        }
        [Serializable] public sealed class ContactWindow
        {
            public float start,end,phase,meanPlanarSpeed;
        }
        [Serializable] public sealed class MotionMeasurement
        {
            public string modelPath,clipPath,clipName;
            public float visualScale,seconds,sampleRate,rootDrift,rootRotationDrift,hipsPlanarDrift,hipsHeightDrift,seamFootGap,seamHipsAngle;
            public bool finite,footIK;
            public PoseFrame[] frames;
            public ContactWindow[] leftCandidates,rightCandidates;
        }
        public static MotionMeasurement Measure(string modelPath,string clipPath,float scale,Vector3 offset,int samplesPerSecond=60,bool footIK=false,AnimationCurve heightCorrection=null)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            AnimationClip clip=null;
            foreach(var item in AssetDatabase.LoadAllAssetsAtPath(clipPath))
                if(item is AnimationClip candidate && !candidate.name.StartsWith("__preview__"))clip=candidate;
            if(model==null || clip==null || !clip.isHumanMotion || clip.length<=0 || !float.IsFinite(scale) || scale<=0 || samplesPerSecond<30 || samplesPerSecond>120)
                throw new ArgumentException("Sampling requires an imported Humanoid motion, target model and measured calibration.");
            var scene=EditorSceneManager.NewPreviewScene();GameObject instance=null;PlayableGraph graph=default;
            try
            {
                instance=PrefabUtility.InstantiatePrefab(model,scene) as GameObject;
                instance.transform.SetPositionAndRotation(offset,Quaternion.identity);instance.transform.localScale=Vector3.one*scale;
                var animator=instance.GetComponentInChildren<Animator>();
                if(animator==null || !animator.isHuman || !animator.avatar.isValid)throw new InvalidDataException("Target must expose its own valid Humanoid Avatar.");
                animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                animator.stabilizeFeet=false;
                var bones=new Transform[9];
                var roles=new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,
                    HumanBodyBones.LeftToes,HumanBodyBones.RightToes,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.Chest};
                for(var i=0;i<roles.Length;i++){bones[i]=animator.GetBoneTransform(roles[i]);if(bones[i]==null)throw new InvalidDataException("Target is missing "+roles[i]);}
                using var soles=new SoleProbe(instance,bones[2],bones[3]);
                graph=PlayableGraph.Create("Calibrated Camp Loop motion sampler");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(footIK);playable.SetApplyPlayableIK(false);playable.SetSpeed(0);
                var output=AnimationPlayableOutput.Create(graph,"Measured target pose",animator);output.SetSourcePlayable(playable);graph.Play();
                var intervals=Mathf.CeilToInt(clip.length*samplesPerSecond);var frames=new PoseFrame[intervals+1];
                var result=new MotionMeasurement{modelPath=modelPath,clipPath=clipPath,clipName=clip.name,visualScale=scale,
                    seconds=clip.length,sampleRate=intervals/clip.length,finite=true,footIK=footIK,frames=frames};
                for(var i=0;i<frames.Length;i++)
                {
                    var seconds=clip.length*i/intervals;
                    instance.transform.position=offset+Vector3.up*(heightCorrection?.Evaluate((float)i/intervals) ?? 0);
                    playable.SetTime(seconds);graph.Evaluate(i==0 ? 0 : clip.length/intervals);
                    var frame=new PoseFrame{seconds=seconds,root=instance.transform.position,rootRotation=instance.transform.rotation,
                        hips=bones[0].position,head=bones[1].position,leftFoot=bones[2].position,rightFoot=bones[3].position,
                        leftToes=bones[4].position,rightToes=bones[5].position,leftHand=bones[6].position,rightHand=bones[7].position,
                        hipsRotation=bones[0].rotation,leftFootRotation=bones[2].rotation,rightFootRotation=bones[3].rotation};
                    soles.Measure(out frame.leftSole,out frame.rightSole,out frame.bodyMinimum,out frame.bodyMaximum);
                    foreach(var bone in bones)if(!Finite(bone.position) || !Finite(bone.rotation))result.finite=false;
                    if(!Finite(frame.root) || !Finite(frame.rootRotation) || !Finite(frame.bodyMinimum) || !Finite(frame.bodyMaximum))result.finite=false;
                    frames[i]=frame;
                }
                var first=frames[0];var last=frames[^1];
                result.rootDrift=Vector3.Distance(first.root,last.root);result.rootRotationDrift=Quaternion.Angle(first.rootRotation,last.rootRotation);
                result.hipsPlanarDrift=Vector3.ProjectOnPlane(last.hips-first.hips,Vector3.up).magnitude;
                result.hipsHeightDrift=last.hips.y-first.hips.y;
                result.seamFootGap=Mathf.Max(Vector3.Distance(first.leftFoot,last.leftFoot),Vector3.Distance(first.rightFoot,last.rightFoot));
                result.seamHipsAngle=Quaternion.Angle(first.hipsRotation,last.hipsRotation);
                result.leftCandidates=FindContactWindows(frames,true);result.rightCandidates=FindContactWindows(frames,false);
                return result;
            }
            finally{if(graph.IsValid())graph.Destroy();if(instance!=null)Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(scene);}
        }

        // Height/vertical-velocity windows are proposals for review, never profile acceptance by themselves.
        public static ContactWindow[] FindContactWindows(PoseFrame[] frames,bool left,float heightAllowance=.02f,float verticalSpeedLimit=.35f)
        {
            if(frames==null || frames.Length<3)throw new ArgumentException("Contact measurement requires a complete sampled timeline.");
            var minimum=float.PositiveInfinity;var toeMinimum=float.PositiveInfinity;
            foreach(var frame in frames){minimum=Mathf.Min(minimum,(left ? frame.leftFoot : frame.rightFoot).y);toeMinimum=Mathf.Min(toeMinimum,(left ? frame.leftToes : frame.rightToes).y);}
            var candidates=new bool[frames.Length];var speeds=new float[frames.Length];
            for(var i=1;i<frames.Length-1;i++)
            {
                var position=left ? frames[i].leftFoot : frames[i].rightFoot;var toe=left ? frames[i].leftToes : frames[i].rightToes;
                var previous=left ? frames[i-1].leftFoot : frames[i-1].rightFoot;var next=left ? frames[i+1].leftFoot : frames[i+1].rightFoot;
                var dt=frames[i+1].seconds-frames[i-1].seconds;var velocity=(next-previous)/Mathf.Max(.00001f,dt);
                candidates[i]=position.y<=minimum+heightAllowance && toe.y<=toeMinimum+heightAllowance && Mathf.Abs(velocity.y)<=verticalSpeedLimit;
                speeds[i]=Vector3.ProjectOnPlane(velocity,Vector3.up).magnitude;
            }
            var windows=new List<ContactWindow>();
            for(var i=1;i<frames.Length-1;i++)
            {
                if(!candidates[i])continue;var start=i;var speed=0f;
                while(i<frames.Length-1 && candidates[i]){speed+=speeds[i];i++;}
                var end=i-1;
                if(frames[end].seconds-frames[start].seconds<.025f)continue;
                windows.Add(new ContactWindow{start=frames[start].seconds,end=frames[end].seconds,
                    phase=(frames[start].seconds+frames[end].seconds)*.5f/frames[^1].seconds,meanPlanarSpeed=speed/(end-start+1)});
            }
            return windows.ToArray();
        }
        public static void Write(string modelPath,string[] clipPaths,float scale,Vector3 offset,string outputDirectory,int samplesPerSecond=60,bool footIK=false)
        {
            Directory.CreateDirectory(outputDirectory);
            foreach(var path in clipPaths)
            {
                var report=Measure(modelPath,path,scale,offset,samplesPerSecond,footIK);
                File.WriteAllText(Path.Combine(outputDirectory,Path.GetFileNameWithoutExtension(path)+".json"),JsonUtility.ToJson(report,true)+"\n");
            }
        }
        private static bool Finite(Vector3 value)=>float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        private static bool Finite(Quaternion value)=>float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);

        private sealed class SoleProbe : IDisposable
        {
            private sealed class Region{public SkinnedMeshRenderer renderer;public int[] left,right;}
            private readonly List<Region> regions=new();
            private readonly Mesh baked=new();
            private readonly List<Vector3> vertices=new();
            public SoleProbe(GameObject model,Transform leftFoot,Transform rightFoot)
            {
                foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    using var counts=renderer.sharedMesh.GetBonesPerVertex();using var weights=renderer.sharedMesh.GetAllBoneWeights();
                    var left=new List<int>();var right=new List<int>();var cursor=0;
                    for(var vertex=0;vertex<counts.Length;vertex++)
                    {
                        var leftWeight=0f;var rightWeight=0f;
                        for(var i=0;i<counts[vertex];i++)
                        {
                            var weight=weights[cursor++];var bone=renderer.bones[weight.boneIndex];
                            if(bone.IsChildOf(leftFoot))leftWeight+=weight.weight;
                            if(bone.IsChildOf(rightFoot))rightWeight+=weight.weight;
                        }
                        if(leftWeight>=.6f)left.Add(vertex);if(rightWeight>=.6f)right.Add(vertex);
                    }
                    if(left.Count>0 || right.Count>0)regions.Add(new Region{renderer=renderer,left=left.ToArray(),right=right.ToArray()});
                }
            }
            public void Measure(out Vector3 left,out Vector3 right,out Vector3 bodyMinimum,out Vector3 bodyMaximum)
            {
                left=right=new Vector3(0,float.PositiveInfinity,0);
                bodyMinimum=Vector3.one*float.PositiveInfinity;bodyMaximum=Vector3.one*float.NegativeInfinity;
                foreach(var region in regions)
                {
                    region.renderer.BakeMesh(baked,true);baked.GetVertices(vertices);
                    foreach(var vertex in vertices)
                    {
                        var world=region.renderer.transform.TransformPoint(vertex);
                        bodyMinimum=Vector3.Min(bodyMinimum,world);bodyMaximum=Vector3.Max(bodyMaximum,world);
                    }
                    foreach(var index in region.left){var world=region.renderer.transform.TransformPoint(vertices[index]);if(world.y<left.y)left=world;}
                    foreach(var index in region.right){var world=region.renderer.transform.TransformPoint(vertices[index]);if(world.y<right.y)right=world;}
                }
                if(!Finite(left) || !Finite(right))throw new InvalidDataException("Target has no finite deformed foot regions.");
            }
            public void Dispose()=>Object.DestroyImmediate(baked);
        }
    }
}
