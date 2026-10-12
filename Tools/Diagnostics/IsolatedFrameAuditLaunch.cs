using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Copy into an isolated candidate's Editor assembly, never the production Assets folder.
[InitializeOnLoad]
public static class IsolatedFrameAuditLaunch
{
    const string ActiveKey = "GreaterWasteland.IsolatedFrameAudit";
    const string StartedKey = "GreaterWasteland.IsolatedFrameAudit.Started";
    static bool started;
    static double deadline, nextRead;

    static IsolatedFrameAuditLaunch()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        // A reload during measurement invalidates the session; never resume hot-reloaded gameplay.
        if (SessionState.GetBool(StartedKey, false))
        {
            EditorApplication.delayCall += () => Exit(1);
            return;
        }
        deadline = EditorApplication.timeSinceStartup + 600;
        EditorApplication.update += Tick;
    }

    public static void Launch()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/BooterBigArmValidation/PerformanceAudit"))
            throw new InvalidOperationException("Use a task-owned isolated PerformanceAudit candidate.");
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(StartedKey, false);
        started = false;
        deadline = EditorApplication.timeSinceStartup + 600;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Production/GreaterWasteland.unity");
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (!started)
            {
                bool render = Array.IndexOf(Environment.GetCommandLineArgs(), "-auditRender") >= 0;
                if (render) GreaterWastelandFrameAudit.StartOffscreen();
                else GreaterWastelandFrameAudit.Start();
                started = true;
                SessionState.SetBool(StartedKey, true);
                deadline = EditorApplication.timeSinceStartup + 600;
            }
            EditorApplication.QueuePlayerLoopUpdate();
            // Poll infrequently so JSON parsing and file IO do not dominate the capture.
            if (EditorApplication.timeSinceStartup >= nextRead && File.Exists("Logs/greater-wasteland-frame-audit.json"))
            {
                nextRead = EditorApplication.timeSinceStartup + .5;
                var report = Newtonsoft.Json.JsonConvert.DeserializeObject<GreaterWastelandFrameAudit.Report>(
                    File.ReadAllText("Logs/greater-wasteland-frame-audit.json"));
                if (report.restored)
                {
                    Exit(string.IsNullOrEmpty(report.error) ? 0 : 1);
                    return;
                }
            }
            if (EditorApplication.timeSinceStartup > deadline)
            {
                GreaterWastelandFrameAudit.Cancel();
                Exit(2);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            GreaterWastelandFrameAudit.Cancel();
            Exit(1);
        }
    }

    static void Exit(int code)
    {
        SessionState.SetBool(ActiveKey, false);
        SessionState.SetBool(StartedKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
}
