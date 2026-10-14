using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundAssetValidator
    {
        [Serializable] public sealed class RigReport
        {
            public string path,guid,avatarName;
            public bool avatarValid,avatarHuman,finiteWeights;
            public int renderers,vertices,triangles,unweightedVertices,invalidBoneIndices,mappedFingers;
            public float importedHeight,visualScale;
            public Vector3 importedBoundsCenter,importedBoundsSize,rendererBoundsSize,visualOffset;
            public string[] mappedBones,hierarchy;
        }
        [Serializable] public sealed class MotionReport
        {
            public string path,guid,clipName,sourceAvatarGuid;
            public long clipLocalId;
            public float seconds,fps,firstFrame,lastFrame;
            public bool humanMotion,compressionOff,loopTime,rootRotationBaked,rootYBaked,rootXZBaked;
        }

        public static void WriteMotionReports(string[] paths,string outputPath)
        {
            var reports=new List<MotionReport>();
            foreach(var path in paths)
            {
                if(!path.StartsWith("Assets/_Project/Art/Animations/Mixamo/",StringComparison.Ordinal))throw new ArgumentException("Use canonical motion paths.");
                var importer=AssetImporter.GetAtPath(path) as ModelImporter;
                if(importer==null || importer.clipAnimations.Length!=1)throw new InvalidDataException("Expected an imported single-clip motion.");
                AnimationClip clip=null;
                foreach(var item in AssetDatabase.LoadAllAssetsAtPath(path))if(item is AnimationClip candidate && !candidate.name.StartsWith("__preview__"))clip=candidate;
                if(clip==null)throw new InvalidDataException("Imported motion has no clip.");
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip,out string guid,out long id);
                var policy=importer.clipAnimations[0];
                reports.Add(new MotionReport{path=path,guid=guid,clipLocalId=id,clipName=clip.name,seconds=clip.length,fps=clip.frameRate,
                    humanMotion=clip.isHumanMotion,sourceAvatarGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(importer.sourceAvatar)),
                    compressionOff=importer.animationCompression==ModelImporterAnimationCompression.Off,loopTime=policy.loopTime,
                    firstFrame=policy.firstFrame,lastFrame=policy.lastFrame,rootRotationBaked=policy.lockRootRotation,rootYBaked=policy.lockRootHeightY,rootXZBaked=policy.lockRootPositionXZ});
            }
            var absolute=Path.GetFullPath(outputPath);Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute,JsonUtility.ToJson(new MotionReportSet{reports=reports.ToArray()},true)+"\n");
        }

        public static RigReport MeasureRig(string path,float bodyHeight=1.8f,float bodyBottom=-.9f)
        {
            if(Array.IndexOf(AnimationPlaygroundAssetImporter.RigPaths,path)<0 || !float.IsFinite(bodyHeight) || bodyHeight<=0)
                throw new ArgumentException("Use a planned rig path and finite character height.");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(model==null || importer==null)throw new InvalidDataException("Rig candidate is not imported.");
            var scene=EditorSceneManager.NewPreviewScene();GameObject instance=null;
            try
            {
                instance=PrefabUtility.InstantiatePrefab(model,scene) as GameObject;
                instance.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                instance.transform.localScale=Vector3.one;
                var report=new RigReport{path=path,guid=AssetDatabase.AssetPathToGUID(path),finiteWeights=true};
                var animator=instance.GetComponentInChildren<Animator>();
                var avatar=animator!=null ? animator.avatar : null;
                report.avatarName=avatar!=null ? avatar.name : null;
                report.avatarValid=avatar!=null && avatar.isValid;report.avatarHuman=avatar!=null && avatar.isHuman;
                var mapped=new List<string>();
                foreach(var bone in importer.humanDescription.human)
                {
                    mapped.Add(bone.humanName+"="+bone.boneName);
                    if(bone.humanName.Contains("Thumb") || bone.humanName.Contains("Index") || bone.humanName.Contains("Middle")
                        || bone.humanName.Contains("Ring") || bone.humanName.Contains("Little"))report.mappedFingers++;
                }
                report.mappedBones=mapped.ToArray();
                var hierarchy=new List<string>();foreach(var item in instance.GetComponentsInChildren<Transform>(true))hierarchy.Add(item.name);
                report.hierarchy=hierarchy.ToArray();
                var bounds=new Bounds();var rendererBounds=new Bounds();var hasBounds=false;var hasRendererBounds=false;
                foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh=renderer.sharedMesh;if(mesh==null)continue;
                    report.renderers++;report.vertices+=mesh.vertexCount;report.triangles+=mesh.triangles.Length/3;
                    if(!hasRendererBounds){rendererBounds=renderer.bounds;hasRendererBounds=true;}else rendererBounds.Encapsulate(renderer.bounds);
                    var baked=new Mesh();
                    try
                    {
                        // Compensate for renderer scale; TransformPoint applies it exactly once.
                        renderer.BakeMesh(baked,true);
                        foreach(var vertex in baked.vertices)
                        {
                            var world=renderer.transform.TransformPoint(vertex);
                            if(!float.IsFinite(world.x) || !float.IsFinite(world.y) || !float.IsFinite(world.z))throw new InvalidDataException("Rig contains a non-finite deformed vertex.");
                            if(!hasBounds){bounds=new Bounds(world,Vector3.zero);hasBounds=true;}else bounds.Encapsulate(world);
                        }
                    }
                    finally{Object.DestroyImmediate(baked);}
                    using var perVertex=mesh.GetBonesPerVertex();using var weights=mesh.GetAllBoneWeights();var cursor=0;
                    foreach(var count in perVertex)
                    {
                        var total=0f;
                        for(var i=0;i<count;i++)
                        {
                            var weight=weights[cursor++];total+=weight.weight;
                            if(!float.IsFinite(weight.weight) || weight.weight<0)report.finiteWeights=false;
                            if(weight.boneIndex<0 || weight.boneIndex>=renderer.bones.Length || renderer.bones[weight.boneIndex]==null)report.invalidBoneIndices++;
                        }
                        if(count==0 || total<=.0001f)report.unweightedVertices++;
                        else if(Mathf.Abs(total-1)>.01f)report.finiteWeights=false;
                    }
                }
                if(!hasBounds || bounds.size.y<=.01f)throw new InvalidDataException("Rig has no measurable skinned geometry.");
                report.importedHeight=bounds.size.y;report.importedBoundsCenter=bounds.center;report.importedBoundsSize=bounds.size;
                report.rendererBoundsSize=rendererBounds.size;
                report.visualScale=bodyHeight/bounds.size.y;
                report.visualOffset=Vector3.up*(bodyBottom-bounds.min.y*report.visualScale);
                return report;
            }
            finally{if(instance!=null)Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(scene);}
        }

        public static void WriteRigReports(string outputPath,float bodyHeight=1.8f,float bodyBottom=-.9f)
        {
            var absolute=Path.GetFullPath(outputPath);
            if(absolute.StartsWith(Path.GetFullPath("Assets")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Qualification reports belong outside imported assets.");
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            var reports=new RigReport[AnimationPlaygroundAssetImporter.RigPaths.Length];
            for(var i=0;i<reports.Length;i++)reports[i]=MeasureRig(AnimationPlaygroundAssetImporter.RigPaths[i],bodyHeight,bodyBottom);
            File.WriteAllText(absolute,JsonUtility.ToJson(new RigReportSet{reports=reports},true)+"\n");
        }

        public static void RenderRigPreview(string path,string outputPath,float bodyHeight=1.8f,float bodyBottom=-.9f,
            string clipPath=null,float samplePhase=0)
        {
            var report=MeasureRig(path,bodyHeight,bodyBottom);
            var preview=new PreviewRenderUtility();Material material=null;Texture2D image=null;
            PlayableGraph graph=default;
            try
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.62f,.55f,.43f);
                var instance=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),preview.camera.gameObject.scene) as GameObject;preview.AddSingleGO(instance);
                instance.transform.localScale=Vector3.one*report.visualScale;instance.transform.localPosition=report.visualOffset;
                foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {renderer.sharedMaterials=new[]{material};renderer.updateWhenOffscreen=true;}
                if(clipPath!=null)
                {
                    if(!clipPath.StartsWith("Assets/_Project/Art/Animations/Mixamo/",StringComparison.Ordinal))throw new ArgumentException("Use a canonical qualification clip.");
                    AnimationClip clip=null;
                    foreach(var item in AssetDatabase.LoadAllAssetsAtPath(clipPath))if(item is AnimationClip candidate && !candidate.name.StartsWith("__preview__"))clip=candidate;
                    var animator=instance.GetComponentInChildren<Animator>();
                    if(clip==null || !clip.isHumanMotion || !animator.isHuman)throw new InvalidDataException("Retarget preview needs a Humanoid clip and its own Humanoid target.");
                    animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    graph=PlayableGraph.Create("Camp Loop rig qualification");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable=AnimationClipPlayable.Create(graph,clip);playable.SetSpeed(0);playable.SetTime(Mathf.Clamp01(samplePhase)*clip.length);
                    playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);
                    var output=AnimationPlayableOutput.Create(graph,"Qualification pose",animator);output.SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
                }
                var gauge=GameObject.CreatePrimitive(PrimitiveType.Cube);preview.AddSingleGO(gauge);
                gauge.transform.position=new Vector3(-1.6f,bodyBottom+.5f,0);gauge.transform.localScale=new Vector3(.08f,1,.08f);
                gauge.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(gauge.GetComponent<Collider>());
                var framing=gauge.GetComponent<Renderer>().bounds;
                foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var baked=new Mesh();
                    try{renderer.BakeMesh(baked,true);foreach(var vertex in baked.vertices)framing.Encapsulate(renderer.transform.TransformPoint(vertex));}
                    finally{Object.DestroyImmediate(baked);}
                }
                preview.camera.orthographic=true;preview.camera.orthographicSize=Mathf.Max(framing.size.x,framing.size.y)*.6f;
                preview.camera.transform.position=framing.center+Vector3.forward*6;preview.camera.transform.LookAt(framing.center);
                preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=20;
                preview.camera.clearFlags=CameraClearFlags.Color;preview.camera.backgroundColor=new Color(.08f,.09f,.1f);
                preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(35,35,0);
                preview.lights[1].intensity=.6f;preview.lights[1].transform.rotation=Quaternion.Euler(320,210,0);
                preview.ambientColor=new Color(.22f,.22f,.22f);
                preview.BeginStaticPreview(new Rect(0,0,768,768));preview.Render(true,true);image=preview.EndStaticPreview();
                var absolute=Path.GetFullPath(outputPath);Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                File.WriteAllBytes(absolute,image.EncodeToPNG());
            }
            finally{if(graph.IsValid())graph.Destroy();if(image!=null)Object.DestroyImmediate(image);preview.Cleanup();if(material!=null)Object.DestroyImmediate(material);}
        }
        [Serializable] private sealed class RigReportSet{public RigReport[] reports;}
        [Serializable] private sealed class MotionReportSet{public MotionReport[] reports;}
    }
}
