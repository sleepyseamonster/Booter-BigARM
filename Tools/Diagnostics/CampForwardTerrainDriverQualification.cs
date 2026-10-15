using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

// Uses the source-sealed private fixture already exercised by Edit Mode tests.
// Reflection avoids a production dependency on the Editor test assembly.
public static class CampForwardTerrainDriverQualification
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private sealed class Row
    {
        public float speed, slope, heading, minimumBody = float.PositiveInfinity, maximumHeight;
        public float maximumLowering, maximumNativeLift, maximumHostCorrection, maximumGarmentCorrection;
        public float maximumKneeStep, maximumHipStep, worstFrom, worstTo;
        public float maximumKneeFlexion, minimumLegDistance = float.PositiveInfinity;
        public float maximumGoalError, maximumFlightLowering, maximumContactRelease;
        public float maximumSupportedRelease;
        public int poses, bodyPoses;
        public int garmentBoundFailures;
        public int unavailable, validPoses;
        public string firstFailure;
    }
    public static string Run() => RunMatrix(false);
    public static string Worst() => RunMatrix(true);
    private static string RunMatrix(bool narrow)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, ".animation-playground-validation-project"))) throw new InvalidOperationException("Use the marked offline candidate.");
        var revision = (int)typeof(AnimationPlaygroundTerrainPose).GetField("ImplementationRevision").GetRawConstantValue();
        if (revision != 13 || EditorApplication.isCompiling) throw new InvalidOperationException("Wait for the paired runtime revision before sampling.");
        var directory = Path.Combine(root, "LocomotionImplementation20261014/" + (narrow ? "TerrainDriverV6Worst" : "TerrainDriverV6"));
        if (Directory.Exists(directory)) throw new IOException("Preserve earlier terrain-driver evidence.");
        Directory.CreateDirectory(directory);
        var sourcePaths = new[] {
            "Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundTerrainPose.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundForwardLocomotionProfile.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.LaboratoryLocomotion.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.GarmentFloor.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.LaboratoryLocomotion.cs",
            "Assets/_Project/Tests/Editor/AnimationPlaygroundTerrainPoseTests.cs" };
        string Hash(string relative)
        {
            using var algorithm = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(Path.Combine(root, relative)))).Replace("-", "").ToLowerInvariant();
        }
        var sources = sourcePaths.ToDictionary(p => p, Hash);
        try
        {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundTerrainPoseTests+Fixture")).FirstOrDefault(t => t != null);
        if (type == null) throw new InvalidOperationException("Compile the paired terrain tests first.");
        using var fixture = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { SkinWeights.FourBones }, null);
        T Property<T>(string name) => (T)type.GetProperty(name).GetValue(fixture);
        var driver = Property<TopDown3DPlayerAnimationDriver>("Driver");
        var visualHost = Property<Transform>("VisualHost");
        var stage = Property<AnimationPlaygroundTerrainPose>("Stage");
        var garment = Property<Action>("Garment");
        var support = (Action<float, float>)Delegate.CreateDelegate(typeof(Action<float, float>), fixture, type.GetMethod("Support"));
        var prepare = (Action)Delegate.CreateDelegate(typeof(Action), driver, typeof(TopDown3DPlayerAnimationDriver).GetMethod("PrepareForwardGraphUpdate", Private));
        var pose = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), driver,
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", Private));
        var minimum = type.GetMethod("MinimumPlaneAndHeight");
        var policy = (AnimationPlaygroundForwardLocomotionProfile)typeof(TopDown3DPlayerAnimationDriver).GetField("laboratoryForwardProfile", Private).GetValue(driver);
        var idleLength = policy.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip.length;
        void Render(float speed, double cycles) { pose(speed, cycles, speed == 0 ? cycles * idleLength : 0); garment(); }
        var left = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        var right = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        var leftHip = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        var rightHip = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        var leftFoot = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var rightFoot = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
        var hips = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.Hips);
        var rows = new List<Row>(); var worst = new Row();
        foreach (var speed in narrow ? new[] { 1.4f } : new[] { 0f, .2f, .7f, 1.4f, 2.05f, 2.7f, 3.25f, 3.8f })
        foreach (var slope in narrow ? new[] { 48f } : new[] { 0f, 15f, 30f, 40f, 48f })
        foreach (var heading in narrow ? new[] { 45f } : slope == 0 ? new[] { 0f } : new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f })
        {
            support(slope, heading); var row = new Row { speed = speed, slope = slope, heading = heading };
            var previousLeft = Vector3.zero; var previousRight = Vector3.zero; var previousHips = Vector3.zero; var previousValid = false;
            var track = new List<float[]>();
            for (var frame = 0; frame <= 128; frame++)
            {
                var phase = frame / 128d; var before = driver.LaboratoryTerrainEvaluations; row.poses++;
                try { Render(speed, phase); }
                catch (InvalidOperationException exception)
                {
                    row.unavailable++; row.firstFailure ??= $"phase {phase}: {exception.Message}";
                    previousValid = false; continue;
                }
                row.validPoses++;
                if (driver.LaboratoryTerrainEvaluations != before + 1)
                    throw new InvalidDataException($"Terrain bound at {speed}m/s {slope}/{heading} phase {phase}: evaluations {before}->{driver.LaboratoryTerrainEvaluations}; garment {driver.LastGarmentFloorDisplacement}, exceeded={driver.GarmentFloorExceeded}.");
                if (driver.GarmentFloorExceeded) row.garmentBoundFailures++;
                var sample = stage.Sample;
                row.maximumLowering = Mathf.Max(row.maximumLowering, sample.lowering);
                row.maximumNativeLift = Mathf.Max(row.maximumNativeLift, sample.flatLift);
                row.maximumHostCorrection = Mathf.Max(row.maximumHostCorrection, driver.LaboratoryForwardLift);
                row.maximumGarmentCorrection = Mathf.Max(row.maximumGarmentCorrection, driver.LastGarmentFloorDisplacement);
                row.maximumKneeFlexion = Mathf.Max(row.maximumKneeFlexion,
                    Mathf.Max(180 - Vector3.Angle(leftHip.position - left.position, leftFoot.position - left.position),
                        180 - Vector3.Angle(rightHip.position - right.position, rightFoot.position - right.position)));
                row.minimumLegDistance = Mathf.Min(row.minimumLegDistance,
                    Mathf.Min(Vector3.Distance(leftHip.position, leftFoot.position), Vector3.Distance(rightHip.position, rightFoot.position)));
                row.maximumFlightLowering = Mathf.Max(row.maximumFlightLowering, Mathf.Max(sample.leftFlightLowering, sample.rightFlightLowering));
                row.maximumContactRelease = Mathf.Max(row.maximumContactRelease, Mathf.Max(sample.leftReleased, sample.rightReleased));
                if (sample.leftWeight >= .99f) row.maximumSupportedRelease = Mathf.Max(row.maximumSupportedRelease, sample.leftReleased);
                if (sample.rightWeight >= .99f) row.maximumSupportedRelease = Mathf.Max(row.maximumSupportedRelease, sample.rightReleased);
                if (slope > 0)
                    row.maximumGoalError = Mathf.Max(row.maximumGoalError,
                        Mathf.Max(Vector3.Distance(leftFoot.position - (visualHost.position - sample.visualHostPosition), sample.leftTarget),
                            Vector3.Distance(rightFoot.position - (visualHost.position - sample.visualHostPosition), sample.rightTarget)));
                if (frame > 0 && previousValid)
                {
                    var step = Mathf.Max(Vector3.Distance(previousLeft, left.position), Vector3.Distance(previousRight, right.position));
                    if (step > row.maximumKneeStep) { row.maximumKneeStep = step; row.worstFrom = (frame - 1) / 128f; row.worstTo = frame / 128f; }
                    row.maximumHipStep = Mathf.Max(row.maximumHipStep, Vector3.Distance(previousHips, hips.position));
                }
                previousLeft = left.position; previousRight = right.position; previousHips = hips.position;
                previousValid = true;
                track.Add(new[] { (float)phase, left.position.x, left.position.y, left.position.z, right.position.x, right.position.y, right.position.z, hips.position.y });
                if (frame % 8 == 0)
                {
                    var arguments = new object[] { 0f }; var min = (float)minimum.Invoke(fixture, arguments);
                    row.minimumBody = Mathf.Min(row.minimumBody, min); row.maximumHeight = Mathf.Max(row.maximumHeight, (float)arguments[0]); row.bodyPoses++;
                }
            }
            if (row.maximumKneeStep > worst.maximumKneeStep) worst = row;
            rows.Add(row);
            File.WriteAllText(Path.Combine(directory, $"track-{rows.Count:D3}.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new { row, track }));
            File.WriteAllText(Path.Combine(directory, "progress.json"), Newtonsoft.Json.JsonConvert.SerializeObject(rows));
        }
        var refinements = new List<object>(); var from = (double)worst.worstFrom; var to = (double)worst.worstTo;
        support(worst.slope, worst.heading);
        for (var level = 0; level < 3; level++)
        {
            var maximum = 0f; var nextFrom = from; var nextTo = to; var previousLeft = Vector3.zero; var previousRight = Vector3.zero; string error = null;
            for (var frame = 0; frame <= 32; frame++)
            {
                var phase = from + (to - from) * frame / 32;
                try { Render(worst.speed, phase); } catch (InvalidOperationException exception) { error = exception.Message; break; }
                if (frame > 0)
                {
                    var delta = Mathf.Max(Vector3.Distance(previousLeft, left.position), Vector3.Distance(previousRight, right.position));
                    if (delta > maximum) { maximum = delta; nextFrom = from + (to - from) * (frame - 1) / 32; nextTo = phase; }
                }
                previousLeft = left.position; previousRight = right.position;
            }
            refinements.Add(new { level, from, to, maximumKneeStep = maximum, peakFrom = nextFrom, peakTo = nextTo, error }); from = nextFrom; to = nextTo;
            if (error != null) break;
        }
        support(30, 0);
        for (var i = 0; i < 128; i++) { prepare(); Render(2.7f, i / 128d); }
        var stopwatch = new Stopwatch(); var allocated = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Start(); for (var i = 0; i < 1024; i++) { prepare(); Render(2.7f, i / 1024d); } stopwatch.Stop();
        var recurring = GC.GetAllocatedBytesForCurrentThread() - allocated;
        if (EditorApplication.isCompiling || sources.Any(pair => Hash(pair.Key) != pair.Value)) throw new InvalidOperationException("Sources changed during terrain measurement.");
        var summary = new { state = "actual-driver-terrain-candidate-uninstalled", revision, sources, rows, worstKnee = worst, refinements,
            performance = new { samples = 1024, microseconds = stopwatch.Elapsed.TotalMilliseconds * 1000 / 1024, recurringBytes = recurring,
                scope = "prepare plus actual one-evaluation core pose and garment floor, excluding CPU mesh baking and diagnostic serialization" },
            coverage = "four bone influences; 129 sole/pose phases and 17 full equipped body phases per case; no physics simulation or Play Mode" };
        var destination = Path.Combine(directory, "summary.json"); File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(summary, Newtonsoft.Json.Formatting.Indented));
        return destination;
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(directory, "failure.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new { error = error.ToString() }, Newtonsoft.Json.Formatting.Indented));
            throw;
        }
    }
}
