using System;
using System.IO;
using BooterBigArm.Editor;
using UnityEditor;
using UnityEngine;

// Copy into the isolated candidate's existing Editor assembly, never production Assets.
public static class IsolatedPlayerAuditBuild
{
    public static void Build()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        if (!Application.isBatchMode || !root.StartsWith("D:\\BooterBigArmValidation\\PerformancePlayer", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A task-owned background PerformancePlayer copy is required.");
        PlayerSettings.companyName = "BooterBigArmDiagnostics";
        PlayerSettings.productName = "GreaterWastelandAudit20261012";
        PlayerSettings.enableFrameTimingStats = true;
        BuildAutomation.BuildFromCli();
    }
}
