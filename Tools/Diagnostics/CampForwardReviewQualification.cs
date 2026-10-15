using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

public static class CampForwardReviewQualification
{
    public static string Run()
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root,".git")) || !File.Exists(Path.Combine(root,".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the compiled marked candidate.");
        var directory=Path.Combine(root,"LocomotionImplementation20261014/ForwardReviewQualificationV3");
        if(Directory.Exists(directory))throw new IOException("Preserve prior measurements.");
        var paths=new[] {
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.ForwardReview.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.LaboratoryLocomotion.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.LaboratoryLocomotion.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.Player.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundActionCoordinator.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundStation.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/UI/TopDown3DMenuController.AnimationPlayground.cs",
            "Assets/_Project/Tests/Editor/AnimationPlaygroundForwardReviewTests.cs",
            "Assets/_Project/Tests/Editor/AnimationPlaygroundPlayerIntegrationTests.cs",
            "Assets/_Project/Prefabs/AnimationPlayground/Stations/AP02.prefab",
            "Assets/_Project/Scripts/Editor/AnimationPlayground/AnimationPlaygroundStationAuthoring.cs",
            TopDown3DAnimationPlaygroundSession.ScenePath };
        string Hash(string relative)
        {using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root,relative)))).Replace("-","").ToLowerInvariant();}
        var sources=paths.ToDictionary(p=>p,Hash);
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t=>t!=null);
        using var fixture=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public,null,new object[]{true,SkinWeights.FourBones},null);
        var driver=(TopDown3DPlayerAnimationDriver)type.GetField("Driver").GetValue(fixture);
        var graph=(PlayableGraph)type.GetField("Graph").GetValue(fixture);
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var prepare=(Action)Delegate.CreateDelegate(typeof(Action),driver,typeof(TopDown3DPlayerAnimationDriver).GetMethod("PrepareForwardGraphUpdate",flags));
        var pose=(Action<float,double,float>)Delegate.CreateDelegate(typeof(Action<float,double,float>),driver,typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardFrame",flags));
        var late=(Action)Delegate.CreateDelegate(typeof(Action),driver,typeof(TopDown3DPlayerAnimationDriver).GetMethod("LateUpdate",flags));
        void Frame(float speed,int i){prepare();pose(speed,(i%128)/128d,.016666667f);late();}
        var measurements=new List<object>();
        foreach(var speed in new[]{0f,1.4f,2.7f,3.8f})
        {
            for(var i=0;i<128;i++)Frame(speed,i);
            var watch=new Stopwatch();var before=GC.GetAllocatedBytesForCurrentThread();watch.Start();
            for(var i=0;i<1024;i++)Frame(speed,i);
            watch.Stop();var recurringBytes=GC.GetAllocatedBytesForCurrentThread()-before;
            measurements.Add(new{speed,samples=1024,microseconds=watch.Elapsed.TotalMilliseconds*1000/1024,recurringBytes});
        }
        foreach(var p in paths)if(Hash(p)!=sources[p])throw new InvalidOperationException("Source changed during measurement.");
        if(EditorApplication.isCompiling || graph.GetOutputCount()!=1 || !driver.LaboratoryForwardReviewActive)
            throw new InvalidOperationException("Review output or compilation changed during measurement.");
        Directory.CreateDirectory(directory);var output=Path.Combine(directory,"summary.json");
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{state="experimental-flat-deck-review-not-full-locomotion-acceptance",
            sources,moduleId=typeof(TopDown3DPlayerAnimationDriver).Module.ModuleVersionId,outputs=graph.GetOutputCount(),measurements,
            scope="actual prepare, nested clip mix, one zero-time graph evaluation and LateUpdate; no physics simulation, mesh baking, UI or author feel proof"},Newtonsoft.Json.Formatting.Indented));
        return output;
    }
}
