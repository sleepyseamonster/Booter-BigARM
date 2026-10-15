using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

public static class CampForwardTerrainStreamProbe
{
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, ".animation-playground-validation-project"))) throw new InvalidOperationException("Use the marked offline candidate.");
        var path = Path.Combine(root, "LocomotionImplementation20261014/terrain-stream-probe-v13.json");
        if (File.Exists(path)) throw new IOException("Preserve prior stream evidence.");
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundTerrainPoseTests+Fixture")).First(t => t != null);
        using var fixture = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { SkinWeights.FourBones }, null);
        var driver = (TopDown3DPlayerAnimationDriver)type.GetProperty("Driver").GetValue(fixture);
        var stage = (AnimationPlaygroundTerrainPose)type.GetProperty("Stage").GetValue(fixture);
        var support = (Action<float, float>)Delegate.CreateDelegate(typeof(Action<float, float>), fixture, type.GetMethod("Support"));
        var pose = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), driver,
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", BindingFlags.Instance | BindingFlags.NonPublic));
        var rows = new List<object>(); var refinements = new List<object>();
        foreach (var heading in new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f })
        {
            support(48, heading);
            var previous = Vector3.zero; var worst = 0f; var worstPhase = 0d;
            for (var frame = 0; frame <= 128; frame++)
            {
                string error = null;
                try { pose(2.05f, frame / 128d, 0); } catch (Exception exception) { error = exception.Message; }
                var data = stage.Sample;
                var knee = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
                if (frame > 0 && Vector3.Distance(knee, previous) > worst) { worst = Vector3.Distance(knee, previous); worstPhase = frame / 128d; }
                previous = knee;
                rows.Add(new { heading, phase = frame / 128d, data.sourceFootError, data.lowering, data.flatLift, data.leftReleased, data.rightReleased,
                    data.leftGap, data.rightGap, knee = new[] { knee.x, knee.y, knee.z },
                    expectedKnee = new[] { data.leftSolvedKnee.x, data.leftSolvedKnee.y, data.leftSolvedKnee.z },
                    nativeKnee = new[] { data.leftNativeKnee.x, data.leftNativeKnee.y, data.leftNativeKnee.z },
                    outputError = Vector3.Distance(knee, data.leftSolvedKnee), error });
            }
            var start = worstPhase - 1d / 128; var width = 1d / 128;
            for (var level = 0; level < 4; level++)
            {
                var maximum = 0f; var end = start;
                for (var frame = 0; frame <= 32; frame++)
                {
                    var phase = start + width * frame / 32;
                    pose(2.05f, phase, 0);
                    var knee = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
                    if (frame > 0 && Vector3.Distance(knee, previous) > maximum) { maximum = Vector3.Distance(knee, previous); end = phase; }
                    previous = knee;
                }
                refinements.Add(new { heading, level, start, width, maximum, end });
                width /= 32; start = end - width;
            }
        }
        File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(new { rows, refinements }, Newtonsoft.Json.Formatting.Indented));
        return path;
    }
}
