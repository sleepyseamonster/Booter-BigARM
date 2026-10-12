using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using BooterBigArm.TopDown3D;

public static class AlgaeCanisterVerification
{
    public static string RunFocused()
    {
        if(EditorApplication.isPlaying)throw new Exception("Use Edit Mode; do not stop the user's session.");
        var assembly=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="BooterBigArm.Editor.Tests");
        var reports=new List<object>();var passed=0;var failed=0;
        var loggedErrors=new List<string>();
        Application.LogCallback captureErrors=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)loggedErrors.Add(message);};
        Application.logMessageReceived+=captureErrors;
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var sandbox=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(sandbox);
        try
        {
        foreach(var fixtureName in new[]{"TopDown3DAlgaeCanisterTests","TopDown3DAlgaeInputTests","TopDown3DAlgaeControllerTests","TopDown3DSurvivalTests","TopDown3DCampInputTests","TopDown3DSleepTests","TopDown3DRadialTests","TopDown3DMenuTests"})
        {
            var fixture=assembly.GetType("BooterBigArm.Tests."+fixtureName,true);
            var setup=fixture.GetMethods().Where(m=>m.GetCustomAttributes(false).Any(a=>a.GetType().Name=="SetUpAttribute")).ToArray();
            var teardown=fixture.GetMethods().Where(m=>m.GetCustomAttributes(false).Any(a=>a.GetType().Name=="TearDownAttribute")).ToArray();
            foreach(var method in fixture.GetMethods())
            {
                // This legacy method replaces the active scene; never invoke it in the user's Editor.
                if(fixtureName=="TopDown3DSurvivalTests" && method.Name=="TryInstallForScene_AttachesVitalsToPlayerAndIsIdempotent")continue;
                var attributes=method.GetCustomAttributes(false);
                var cases=attributes.Where(a=>a.GetType().Name=="TestCaseAttribute").Select(a=>(object[])a.GetType().GetProperty("Arguments").GetValue(a)).ToList();
                if(cases.Count==0 && attributes.Any(a=>a.GetType().Name=="TestAttribute"))cases.Add(Array.Empty<object>());
                foreach(var args in cases)
                {
                    var instance=Activator.CreateInstance(fixture);string error=null;var errorCount=loggedErrors.Count;
                    try{foreach(var s in setup)s.Invoke(instance,null);method.Invoke(instance,args);}
                    catch(Exception e){error=(e is TargetInvocationException t?t.InnerException:e).ToString();}
                    finally
                    {
                        // EditMode fixtures bind routers explicitly; dehook them before their roots are destroyed.
                        foreach(var root in sandbox.GetRootGameObjects())
                            foreach(var router in root.GetComponentsInChildren<TopDown3DInputRouter>(true))
                                typeof(TopDown3DInputRouter).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(router,null);
                        try{foreach(var t in teardown)t.Invoke(instance,null);}catch(Exception e){error=(error??"")+e;}
                    }
                    if(loggedErrors.Count>errorCount)error=(error??"")+string.Join("\n",loggedErrors.Skip(errorCount));
                    if(error==null)passed++;else failed++;
                    reports.Add(new{name=fixtureName+"."+method.Name+"("+string.Join(",",args)+")",passed=error==null,error});
                }
            }
        }
        }
        finally
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(sandbox,true);
            Application.logMessageReceived-=captureErrors;
        }
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,failed,tests=reports},Newtonsoft.Json.Formatting.Indented);
        Directory.CreateDirectory("Logs/AlgaeCanister");File.WriteAllText("Logs/AlgaeCanister/focused-tests.json",json);
        return $"{passed} passed, {failed} failed. Details: Logs/AlgaeCanister/focused-tests.json";
    }
}
