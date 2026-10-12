using System;
using System.Diagnostics;

// Candidate-only local clocks. Inert outside Assets; never copy into production.
public static class PopulationWorkTimings
{
    public enum Unit { Demand, PlannerBegin, PlannerStep, BaselineCommit, Queue, Acquire, Activation, Retire, PhysicsSync, Release, Initialize }
    [Serializable] public sealed class Summary
    {
        public string unit;
        public int phase;
        public long count;
        public double totalMs, maximumMs, maximumAtSeconds;
    }
    private static readonly Summary[] summaries = Create();
    private static long epoch;
    private static int phase;
    private static Summary[] Create()
    {
        var values = new Summary[44];
        for (int p = 0; p < 4; p++) for (int u = 0; u < 11; u++)
            values[p * 11 + u] = new Summary { phase = p, unit = ((Unit)u).ToString() };
        return values;
    }
    public static void Reset()
    {
        foreach (var value in summaries) { value.count = 0; value.totalMs = value.maximumMs = value.maximumAtSeconds = 0; }
        phase = 0; epoch = Stopwatch.GetTimestamp();
    }
    public static void SetPhase(int value) => phase = value;
    public static Summary[] Capture() => summaries;
    public static Scope Measure(Unit unit) => new Scope(summaries[phase * 11 + (int)unit]);
    public readonly struct Scope : IDisposable
    {
        private readonly Summary target;
        private readonly long start;
        public Scope(Summary target) { this.target = target; start = Stopwatch.GetTimestamp(); }
        public void Dispose()
        {
            long end = Stopwatch.GetTimestamp(); double ms = (end - start) * 1000d / Stopwatch.Frequency;
            target.count++; target.totalMs += ms;
            if (ms > target.maximumMs)
            { target.maximumMs = ms; target.maximumAtSeconds = (start - epoch) / (double)Stopwatch.Frequency; }
        }
    }
}
