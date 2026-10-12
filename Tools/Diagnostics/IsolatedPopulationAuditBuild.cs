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
        PlayerSettings.enableFrameTimingStats = true;
        string output = Path.Combine(root, "BuildsPopulation/PopulationSectorAudit.exe");
        if (File.Exists(output)) throw new InvalidOperationException("Use a fresh audit output.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { FixedTerrainPopulationSectorBuilder.ActorScenePath },
            target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.Development });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Population diagnostic build failed.");
        Debug.Log("POPULATION_AUDIT_BUILD_SUCCEEDED");
    }
}
