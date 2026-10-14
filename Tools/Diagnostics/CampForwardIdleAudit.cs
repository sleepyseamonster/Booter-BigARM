using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

public static class CampForwardIdleAudit
{
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || Directory.Exists(Path.Combine(root, ".git")))
            throw new InvalidOperationException("Use the offline candidate.");
        var destination = Path.Combine(root, "LocomotionImplementation20261014/idle-selection-v1.json");
        if (File.Exists(destination)) throw new IOException("Preserve prior measurements.");
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        var baseline = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>("Assets/_Project/Settings/Player/TopDown3DLocomotionClipProfile.asset");
        var before = QualitySettings.skinWeights; var rows = new List<object>();
        try
        {
            QualitySettings.skinWeights = SkinWeights.FourBones;
            foreach (var clip in new[] { bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip,
                baseline.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip })
            {
                var data = AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),
                    AssetDatabase.GetAssetPath(clip), bindings.visualScale, bindings.visualOffset, 120);
                rows.Add(new { source = AssetDatabase.GetAssetPath(clip), samples = data.frames.Length,
                    maximumSoleHeightDifference = data.frames.Max(x => Mathf.Abs(x.leftSole.y - x.rightSole.y)),
                    meanSoleHeightDifference = data.frames.Average(x => Mathf.Abs(x.leftSole.y - x.rightSole.y)),
                    feetSeparation = Vector3.Distance(data.frames[0].leftFoot, data.frames[0].rightFoot) });
            }
            File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
            return destination;
        }
        finally { QualitySettings.skinWeights = before; }
    }
}
