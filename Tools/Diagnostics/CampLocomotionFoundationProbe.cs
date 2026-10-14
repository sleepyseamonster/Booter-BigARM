using System;
using System.Diagnostics;
using System.IO;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

public static class CampLocomotionFoundationProbe
{
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate.");
        var destination = Path.Combine(root, "LocomotionImplementation20261014", "foundation-performance.json");
        if (File.Exists(destination)) throw new IOException("Preserve prior evidence.");
        var observer = new AnimationPlaygroundGroundedMotionTracker();
        var clock = new AnimationPlaygroundGaitClock();
        void Step(int i)
        {
            var sample = observer.Observe(Vector3.forward * (i * .03125f), i * .02, true, 1, Vector3.up);
            var stride = AnimationPlaygroundGaitClock.BlendedCycleDistance(sample.Speed,
                new Vector3(1.4f, 2.7f, 3.8f), new Vector3(3.17f, 4.37f, 5.37f));
            clock.Advance(sample, stride);
            clock.RenderCycles(.5f);
        }
        for (var i = 0; i < 4000; i++) Step(i);
        observer.Reset(); clock.Reset();
        var timer = new Stopwatch();
        var before = GC.GetAllocatedBytesForCurrentThread();
        timer.Start();
        for (var i = 0; i < 100000; i++) Step(i);
        timer.Stop();
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        var report = new
        {
            state = "offline-pure-foundation-microbenchmark-not-player-profile",
            samples = 100000, milliseconds = timer.Elapsed.TotalMilliseconds,
            microsecondsPerStep = timer.Elapsed.TotalMilliseconds * .01,
            recurringManagedBytes = bytes, graphEvaluations = 0, addedPhysicsQueries = 0,
            budgets = new { fixedObservationAndClockMilliseconds = .2, recurringManagedBytes = 0,
                projectionAndExplicitGraphMilliseconds = .5, projectionStatus = "awaiting-qualified-family-measurement" },
            clockCycles = clock.Cycles, countedContacts = clock.ContactCount
        };
        File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return destination;
    }
}
