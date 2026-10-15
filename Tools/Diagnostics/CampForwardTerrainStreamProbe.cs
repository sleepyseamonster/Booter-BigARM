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
        var path = Path.Combine(root, "LocomotionImplementation20261014/terrain-stream-probe-v25.json");
        if (File.Exists(path)) throw new IOException("Preserve prior stream evidence.");
        var revision = (int)typeof(AnimationPlaygroundTerrainPose).GetField("ImplementationRevision").GetRawConstantValue();
        if (revision != 13 || EditorApplication.isCompiling) throw new InvalidOperationException("Wait for the paired runtime revision before sampling.");
        var sourcePath = Path.Combine(root, "Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundTerrainPose.cs");
        string SourceHash()
        {
            using var algorithm = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(sourcePath))).Replace("-", "").ToLowerInvariant();
        }
        var sourceSha256 = SourceHash();
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundTerrainPoseTests+Fixture")).First(t => t != null);
        using var fixture = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { SkinWeights.FourBones }, null);
        var driver = (TopDown3DPlayerAnimationDriver)type.GetProperty("Driver").GetValue(fixture);
        var stage = (AnimationPlaygroundTerrainPose)type.GetProperty("Stage").GetValue(fixture);
        var support = (Action<float, float>)Delegate.CreateDelegate(typeof(Action<float, float>), fixture, type.GetMethod("Support"));
        var pose = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), driver,
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", BindingFlags.Instance | BindingFlags.NonPublic));
        var rows = new List<object>(); var refinements = new List<object>();
        float[] Bone(HumanBodyBones role)
        {
            var p = driver.HumanoidAnimator.GetBoneTransform(role).position;
            return new[] { p.x, p.y, p.z };
        }
        foreach (var speed in new[] { 1.4f, 2.05f, 2.7f, 3.25f, 3.8f })
        foreach (var heading in new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f })
        {
            support(48, heading);
            var previous = Vector3.zero; var previousRight = Vector3.zero; var worst = 0f; var worstPhase = 0d;
            var caseErrors = 0;
            for (var frame = 0; frame <= 128; frame++)
            {
                string error = null;
                try { pose(speed, frame / 128d, 0); } catch (Exception exception) { error = exception.Message; caseErrors++; }
                var data = stage.Sample;
                var sourceFoot = data.matrices[data.leftFootCacheBone].GetColumn(3);
                var knee = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
                var rightKnee = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position;
                var step = Mathf.Max(Vector3.Distance(knee, previous), Vector3.Distance(rightKnee, previousRight));
                if (frame > 0 && step > worst) { worst = step; worstPhase = frame / 128d; }
                previous = knee; previousRight = rightKnee;
                rows.Add(new { speed, heading, phase = frame / 128d, data.sourceFootError, data.lowering, data.flatLift, data.leftReleased, data.rightReleased,
                    data.leftGap, data.rightGap, knee = new[] { knee.x, knee.y, knee.z },
                    rightKnee = new[] { rightKnee.x, rightKnee.y, rightKnee.z },
                    leftHip = Bone(HumanBodyBones.LeftUpperLeg), rightHip = Bone(HumanBodyBones.RightUpperLeg),
                    leftFoot = Bone(HumanBodyBones.LeftFoot), rightFoot = Bone(HumanBodyBones.RightFoot),
                    data.leftWeight, data.rightWeight,
                    data.leftFlightLowering, data.rightFlightLowering,
                    sourceFoot = new[] { sourceFoot.x, sourceFoot.y, sourceFoot.z },
                    leftTarget = new[] { data.leftTarget.x, data.leftTarget.y, data.leftTarget.z },
                    rightTarget = new[] { data.rightTarget.x, data.rightTarget.y, data.rightTarget.z },
                    expectedKnee = new[] { data.leftSolvedKnee.x, data.leftSolvedKnee.y, data.leftSolvedKnee.z },
                    nativeKnee = new[] { data.leftNativeKnee.x, data.leftNativeKnee.y, data.leftNativeKnee.z },
                    outputError = Vector3.Distance(knee, data.leftSolvedKnee), error });
            }
            if (caseErrors > 0) { refinements.Add(new { speed, heading, caseErrors, level = -1 }); continue; }
            var start = worstPhase - 1d / 128; var width = 1d / 128;
            for (var level = 0; level < 4; level++)
            {
                var maximum = 0f; var end = start; string error = null;
                for (var frame = 0; frame <= 32; frame++)
                {
                    var phase = start + width * frame / 32;
                    try { pose(speed, phase, 0); } catch (Exception exception) { error = exception.Message; break; }
                    var knee = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
                    var rightKnee = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position;
                    var step = Mathf.Max(Vector3.Distance(knee, previous), Vector3.Distance(rightKnee, previousRight));
                    if (frame > 0 && step > maximum) { maximum = step; end = phase; }
                    previous = knee; previousRight = rightKnee;
                }
                refinements.Add(new { speed, heading, coarseMaximum = worst, level, start, width, maximum, end, error });
                if (error != null) break;
                width /= 32; start = end - width;
            }
        }
        if (EditorApplication.isCompiling || SourceHash() != sourceSha256) throw new InvalidOperationException("Runtime source changed while sampling.");
        File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(new { revision, sourceSha256,
            moduleId = typeof(AnimationPlaygroundTerrainPose).Module.ModuleVersionId, rows, refinements }, Newtonsoft.Json.Formatting.Indented));
        return path;
    }
}
