using System;
using System.IO;
using BooterBigArm.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Inert outside Assets; copy only into the owned diagnostic Editor assembly.
public static class IsolatedPopulationAuditBuild
{
    public static void Build()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        if (!Application.isBatchMode || !root.StartsWith("D:\\BooterBigArmValidation\\PerformancePlayer", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Owned background Player copy required.");
        var scene = EditorSceneManager.OpenScene(FixedTerrainPopulationSectorBuilder.ActorScenePath);
        FixedTerrainPopulationSectorBuilder.ValidateActorScene(scene);
        PlayerSettings.companyName = "BooterBigArmDiagnostics";
        PlayerSettings.productName = "PopulationSectorAudit20261012";
        // This observer uses local wall clocks and manual URP submission, not the Profiler connection.
        PlayerSettings.enableFrameTimingStats = false;
        string output = Path.Combine(root, "BuildsPopulationLocal/PopulationSectorAudit.exe");
        if (File.Exists(output)) throw new InvalidOperationException("Use a fresh audit output.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { FixedTerrainPopulationSectorBuilder.ActorScenePath },
            target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.None });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Population diagnostic build failed.");
        if ((report.summary.options & (BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging)) != 0)
            throw new InvalidOperationException("Local-clock diagnostic must exclude development/profiler/debugging build flags.");
        Debug.Log("POPULATION_LOCAL_CLOCK_BUILD_SUCCEEDED options=" + report.summary.options);
    }
}
