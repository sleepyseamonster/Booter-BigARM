using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

// Explicit Edit Mode query/velocity planning. Never advances physics or Play Mode.
public static class CampUphillGaitQualification
{
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the compiled, marked offline candidate.");
        var directory = Path.Combine(root, "LocomotionImplementation20261014/UphillGaitV1");
        if (Directory.Exists(directory)) throw new IOException("Preserve earlier measurements.");
        var paths = new[] {
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.AnimationPlayground.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.LaboratoryLocomotion.cs",
            "Assets/_Project/Tests/Editor/AnimationPlaygroundLocomotionRepairTests.cs" };
        string Hash(string path)
        {
            using var algorithm = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(Path.Combine(root, path)))).Replace("-", "").ToLowerInvariant();
        }
        var sources = paths.ToDictionary(p => p, Hash);
        var test = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundLocomotionRepairTests")).First(t => t != null);
        if (test.GetMethod("SupportedSteepUphillTravelUsesWalkingMetresPerSecondAlongTheSurface") == null)
            throw new InvalidOperationException("Compile the paired uphill tests first.");
        var type = test.GetNestedType("UphillFixture", BindingFlags.NonPublic);
        using var fixture = (IDisposable)Activator.CreateInstance(type, true);
        type.GetMethod("Input").Invoke(fixture, new object[] { 1f, false });
        var motor = (TopDown3DPlayerMotor)type.GetField("Motor").GetValue(fixture);
        var body = (Rigidbody)type.GetField("Body").GetValue(fixture);
        var tick = (Action)Delegate.CreateDelegate(typeof(Action), motor,
            typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic));
        for (var i = 0; i < 128; i++) tick();
        const int samples = 8192;
        var watch = new Stopwatch(); var before = GC.GetAllocatedBytesForCurrentThread(); watch.Start();
        for (var i = 0; i < samples; i++) tick();
        watch.Stop(); var recurringBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        if (!motor.IsGrounded || Mathf.Abs(body.linearVelocity.z - 1.4f) > .0001f)
            throw new InvalidOperationException("The benchmark must retain its actual upcoming-ramp speed cap.");
        var curve = Enumerable.Range(0, 49).Select(grade => new { grade,
            speed = AnimationPlaygroundUphillGait.SpeedLimit(3.8f, 1.4f, Vector3.forward,
                Quaternion.AngleAxis(-grade, Vector3.right) * Vector3.up, 48) }).ToArray();
        foreach (var p in paths) if (Hash(p) != sources[p]) throw new InvalidOperationException("Source changed during measurement.");
        if (EditorApplication.isCompiling) throw new InvalidOperationException("Compilation began during measurement.");
        Directory.CreateDirectory(directory); var output = Path.Combine(directory, "summary.json");
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { state = "edit-mode-static-uphill-query-and-velocity-planning",
            sources, moduleId = typeof(TopDown3DPlayerMotor).Module.ModuleVersionId,
            beginGrade = AnimationPlaygroundUphillGait.BeginGrade, walkingGrade = AnimationPlaygroundUphillGait.WalkingGrade,
            curve, performance = new { samples, microseconds = watch.Elapsed.TotalMilliseconds * 1000 / samples, recurringBytes,
                scope = "warm existing motor FixedUpdate and observer, including optional incline ray; no physics simulation or graph presentation" } }, Newtonsoft.Json.Formatting.Indented));
        return output;
    }
}
