using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class CampForwardLocomotionQualification
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, Flags).GetValue(o);
    private static void Call(object o, string name) => o.GetType().GetMethod(name, Flags).Invoke(o, null);
    private sealed class Row
    {
        public float speed, minimumSole = float.PositiveInfinity, minimumBody = float.PositiveInfinity;
        public float maximumHeight, maximumLift, minimumLift, maximumPatchExcursion, maximumPatchTravel, maximumPatchGap;
        public float maximumNearestSoleGap;
        public double idleSeconds;
        public float maximumProjectionError;
        public double poseMicroseconds, poseAllocatedBytes;
        public int samples;
        public readonly List<object> patches = new();
        public readonly List<object> plants = new();
    }

    public static string Run() => RunMatrix(SkinWeights.FourBones, true, "ForwardDistancePoseV1");
    public static string RunOne() => RunMatrix(SkinWeights.OneBone, true, "ForwardDistancePoseOneV1");
    public static string RunTwo() => RunMatrix(SkinWeights.TwoBones, true, "ForwardDistancePoseTwoV1");
    public static string RunCold() => RunMatrix(SkinWeights.FourBones, false, "ForwardDistancePoseColdV1");
    public static string RunDense() => RunMatrix(SkinWeights.FourBones, true, "ForwardDensePoseV1", true);
    public static string RunTunedReview() => RunMatrix(SkinWeights.FourBones, true, "GaitPaceTuningContact20261015", false,
        "LocomotionImplementation20261014/GaitPaceTuning20261015/combined-calibration.json",
        new[] { .2f, .7f, 1.4f, 1.48f, 1.6f, 1.8f, 2f, 2.12f, 2.2f, 2.48f, 2.9f, 3.6f, 4.3f, 4.72f, 5f });
    public static string RunExpandedReview() => RunMatrix(SkinWeights.FourBones, true, "FullSprintContact20261015", false,
        "LocomotionImplementation20261014/FullSprintPreparation20261015/combined-calibration.json",
        new[] { 3f, 3.25f, 3.5f, 3.75f, 4f, 4.25f, 4.5f, 4.75f, 5f, 5.25f, 5.5f, 5.75f, 6f },
        "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset");

    public static string BenchmarkClock()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate.");
        var destination = Path.Combine(root, "LocomotionImplementation20261014/forward-clock-performance-v1.json");
        if (File.Exists(destination)) throw new IOException("Preserve existing measurements.");
        var profile = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
        var clock = new AnimationPlaygroundGaitClock();
        void Step(int tick)
        {
            var speed = 1.4f + 2.4f * (tick % 1000) / 999f;
            var sample = new AnimationPlaygroundGroundedMotion(tick, 1, tick * .02, .02,
                Vector3.forward * (speed * .02f), Vector3.up, true, false);
            clock.Advance(sample, profile); clock.RenderCycles(.5f, speed, profile);
        }
        for (var i = 0; i < 1000; i++) Step(i);
        var timer = new Stopwatch(); var before = GC.GetAllocatedBytesForCurrentThread();
        timer.Start(); for (var i = 1000; i < 101000; i++) Step(i); timer.Stop();
        var report = new { samples = 100000, microseconds = timer.Elapsed.TotalMilliseconds * 1000 / 100000,
            recurringBytes = GC.GetAllocatedBytesForCurrentThread() - before,
            scope = "completed sample advance plus physical-distance render interpolation; no physics or mesh baking" };
        File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return destination;
    }

    private static string RunMatrix(SkinWeights skinWeights, bool normalizeEquipment, string name, bool dense = false,
        string selectedRecipe = null, float[] selectedSpeeds = null, string selectedProfile = null)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate.");
        var directory = Path.Combine(root, "LocomotionImplementation20261014", name);
        if (Directory.Exists(directory)) throw new IOException("Preserve prior pose evidence.");
        Directory.CreateDirectory(directory);
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
            selectedProfile ?? "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
        if (!policy.Validate(policy.targetModel, policy.locomotion, out var error, false)) throw new InvalidDataException(error);
        var recipePath = selectedRecipe ?? "LocomotionImplementation20261014/native-calibration-v6.json";
        var recipe = JObject.Parse(File.ReadAllText(Path.Combine(root, recipePath)));
        var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
        var preview = EditorSceneManager.NewPreviewScene(); GameObject actor = null;
        var bindings = Object.Instantiate(TopDown3DAnimationPlaygroundSession.Find(scene).Bindings);
        var quality = QualitySettings.skinWeights; Mesh baked = null;
        try
        {
            QualitySettings.skinWeights = skinWeights;
            bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = null;
            actor = new GameObject("Offline actual forward driver"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, preview);
            actor.AddComponent<TopDown3DPlayerMotor>(); var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
            var shape = actor.GetComponent<CapsuleCollider>(); shape.height = bindings.capsuleHeight; shape.radius = bindings.capsuleRadius;
            EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
            driver.ConfigureLaboratory(bindings); Call(driver, "Awake"); actor.SetActive(true);
            typeof(TopDown3DPlayerAnimationDriver).GetField("laboratoryForwardProfile", Flags).SetValue(driver, policy);
            if (policy.ExpandedReview)
            {
                // Pose-only fixture. Authored AP02 ownership/support is exercised
                // separately by the real-scene six-channel EditMode tests.
                typeof(TopDown3DPlayerAnimationDriver).GetField("forwardReviewConfigured", Flags).SetValue(driver, true);
                Call(driver, "InitializeForwardReviewInputs");
            }
            var render = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), driver,
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", Flags));
            render(0, 0, 0);
            if (!driver.ConfigureCamp(TopDown3DCampSettings.Load())) throw new InvalidDataException("Hydrate existing camp equipment.");
            driver.SetCampPresentation(-1, 1, false, true); Call(driver, "LateUpdate");
            if (normalizeEquipment) Call(driver, "NormalizeCampSkinWeights");
            var visual = Field<GameObject>(driver, "visualInstance");
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.quality = SkinQuality.Auto;
            using var cpu = new AnimationPlaygroundMotionSampler.SoleProbe(visual,
                driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot),
                driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot));
            var body = visual.GetComponentsInChildren<SkinnedMeshRenderer>().First(s => s.transform.name == "skull");
            var raw = JObject.Parse(File.ReadAllText(Path.Combine(root, "LocomotionImplementation20261014/SoleTracksV2/MCU_am_Loco_Walk_Fwd_NoRM.json")));
            var footIds = new[]
            {
                ((JArray)raw["ids"]).Where(x => (int)x["side"] == 0).Select(x => (int)x["vertex"]).ToArray(),
                ((JArray)raw["ids"]).Where(x => (int)x["side"] == 1).Select(x => (int)x["vertex"]).ToArray()
            };
            baked = new Mesh(); var vertices = new List<Vector3>();
            var rows = new List<Row>(); var floor = -bindings.capsuleHeight * .5f;
            var projection = Field<AnimationPlaygroundFootProjection.Sampler>(driver, "footProjectionSampler");
            var phases = dense ? DensePhases(policy) : Enumerable.Range(0, 129).Select(i => i / 128d).ToArray();
            var kit = Field<GameObject>(driver, "packedCamp");
            var kitMinimum = float.PositiveInfinity; var kitMaximum = float.NegativeInfinity; var kitVertices = 0;
            foreach (var filter in kit.GetComponentsInChildren<MeshFilter>(true))
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    var world = filter.transform.TransformPoint(vertex);
                    kitMinimum = Mathf.Min(kitMinimum, world.y); kitMaximum = Mathf.Max(kitMaximum, world.y); kitVertices++;
                }
            foreach (var speed in selectedSpeeds ?? new[] { .2f, .7f, 1.4f, 1.53f, 1.725f, 2.05f, 2.375f, 2.57f, 2.7f, 2.81f, 2.975f, 3.25f, 3.525f, 3.69f, 3.8f })
            foreach (var idle in dense && speed < 1.4f ? new[] { 0d, .25, .5, .75 } : new[] { 0d })
            {
                var idleSeconds = idle * policy.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip.length;
                var row = new Row { speed = speed, idleSeconds = idleSeconds }; var timer = new Stopwatch();
                // Warm the graph before measuring recurring pose cost/allocation.
                for (var i = 0; i < 32; i++) render(speed, i / 32.0, idleSeconds);
                var allocated = GC.GetAllocatedBytesForCurrentThread();
                timer.Start(); for (var i = 0; i < 256; i++) render(speed, i / 256.0, idleSeconds); timer.Stop();
                row.poseAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                row.poseMicroseconds = timer.Elapsed.TotalMilliseconds * 1000 / 256;
                foreach (var phase in phases)
                {
                    render(speed, phase, idleSeconds);
                    cpu.Measure(out var left, out var right, out var minimum, out var maximum);
                    row.minimumSole = Mathf.Min(row.minimumSole, left.y, right.y); row.minimumBody = Mathf.Min(row.minimumBody, minimum.y);
                    row.maximumHeight = Mathf.Max(row.maximumHeight, maximum.y - floor);
                    row.maximumNearestSoleGap = Mathf.Max(row.maximumNearestSoleGap, Mathf.Min(left.y, right.y) - floor);
                    row.maximumProjectionError = Mathf.Max(row.maximumProjectionError, Mathf.Abs(projection.MinimumWorldY() - Mathf.Min(left.y, right.y)));
                    if (Math.Abs(phase * 4 - Math.Round(phase * 4)) < .0000001)
                        row.plants.Add(new { plant = Math.Round(phase * 4), leftGap = left.y - floor, rightGap = right.y - floor });
                    row.maximumLift = Mathf.Max(row.maximumLift, driver.LaboratoryForwardLift);
                    row.minimumLift = Mathf.Min(row.minimumLift, driver.LaboratoryForwardLift); row.samples++;
                }
                var weights = policy.PoseWeights(speed);
                for (var plant = 0; plant < 4; plant++)
                {
                    var from = double.NegativeInfinity; var to = double.PositiveInfinity;
                    for (var gait = 0; gait < policy.MovingGaitCount; gait++)
                    {
                        if (weights[gait + 1] < .09999f) continue;
                        var patch = recipe["gaits"][gait]["patches"][plant]; var entry = policy.gaits[gait];
                        var start = Semantic(entry, (float)patch["start"] / entry.sourceClip.length);
                        var end = Semantic(entry, (float)patch["end"] / entry.sourceClip.length);
                        var center = plant * .25;
                        while (start - center > .5) start -= 1;
                        while (end - center > .5) end -= 1;
                        from = Math.Max(from, start); to = Math.Min(to, end);
                    }
                    if (!double.IsFinite(from) || to <= from) continue;
                    var ids = footIds[plant % 2 == 0 ? 1 : 0]; var tracks = new Vector3[ids.Length][];
                    for (var i = 0; i < tracks.Length; i++) tracks[i] = new Vector3[33];
                    for (var frame = 0; frame <= 32; frame++)
                    {
                        var phase = from + (to - from) * frame / 32;
                        render(speed, phase, idleSeconds); body.BakeMesh(baked, true); baked.GetVertices(vertices);
                        for (var i = 0; i < ids.Length; i++)
                            tracks[i][frame] = body.transform.TransformPoint(vertices[ids[i]]) + Vector3.forward * (float)policy.DistanceAtCycles(speed, phase);
                    }
                    var best = -1; var bestExcursion = float.PositiveInfinity; var bestTravel = 0f; var bestGap = 0f;
                    var fitDistance = 0f; var fittedExcursion = 0f; var lateralExcursion = 0f;
                    var maximumTargetGap = 0f;
                    for (var frame = 0; frame <= 32; frame++)
                    {
                        var lowest = float.PositiveInfinity;
                        for (var i = 0; i < tracks.Length; i++) lowest = Mathf.Min(lowest, tracks[i][frame].y);
                        maximumTargetGap = Mathf.Max(maximumTargetGap, lowest - floor);
                    }
                    for (var i = 0; i < tracks.Length; i++)
                    {
                        var excursion = 0f; var travel = 0f; var gap = 0f;
                        for (var frame = 0; frame < tracks[i].Length; frame++)
                        {
                            var delta = Vector3.ProjectOnPlane(tracks[i][frame] - tracks[i][0], Vector3.up);
                            excursion = Mathf.Max(excursion, delta.magnitude); gap = Mathf.Max(gap, tracks[i][frame].y - floor);
                            if (frame > 0) travel += Vector3.ProjectOnPlane(tracks[i][frame] - tracks[i][frame - 1], Vector3.up).magnitude;
                        }
                        if (gap > .012f) continue;
                        if (excursion < bestExcursion)
                        {
                            best = ids[i]; bestExcursion = excursion; bestTravel = travel; bestGap = gap;
                            var numerator = 0.0; var denominator = 0.0;
                            for (var frame = 0; frame <= 32; frame++)
                            {
                                var u = (to - from) * (frame / 32.0 - .5);
                                var rawZ = tracks[i][frame].z - policy.DistanceAtCycles(speed, from + (to - from) * frame / 32);
                                numerator += u * rawZ; denominator += u * u;
                            }
                            fitDistance = (float)(-numerator / denominator);
                            fittedExcursion = lateralExcursion = 0;
                            for (var frame = 0; frame <= 32; frame++)
                            {
                                var delta = tracks[i][frame] - tracks[i][0];
                                delta.z += (float)(fitDistance * (to - from) * frame / 32 - (policy.DistanceAtCycles(speed, from + (to - from) * frame / 32) - policy.DistanceAtCycles(speed, from)));
                                delta.y = 0; fittedExcursion = Mathf.Max(fittedExcursion, delta.magnitude);
                                lateralExcursion = Mathf.Max(lateralExcursion, Mathf.Abs(delta.x));
                            }
                        }
                    }
                    row.maximumPatchExcursion = Mathf.Max(row.maximumPatchExcursion, bestExcursion);
                    row.maximumPatchTravel = Mathf.Max(row.maximumPatchTravel, bestTravel); row.maximumPatchGap = Mathf.Max(row.maximumPatchGap, bestGap);
                    row.patches.Add(new { plant, vertex = best, start = from, end = to,
                        duration = (policy.DistanceAtCycles(speed, to) - policy.DistanceAtCycles(speed, from)) / speed,
                        anchorExcursion = bestExcursion, cumulativeTravel = bestTravel, maximumGroundGap = bestGap,
                        contactQualified = best >= 0, fittedCycleDistance = fitDistance, fittedExcursion, lateralExcursion, maximumTargetGap,
                        supportedTraces = SupportedTraces(tracks, ids, from, to, policy, speed, floor, plant) });
                }
                rows.Add(row);
                File.WriteAllText(Path.Combine(directory, "progress.json"), Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
            }
            var path = Path.Combine(directory, "summary.json");
            File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                state = "actual-forward-driver-coarse-review", sourcePolicyQualified = policy.qualified,
                globalSkinWeights = skinWeights.ToString(), normalizeEquipment,
                dense, phaseSamplesPerCase = phases.Length,
                staticPackedKit = new { vertices = kitVertices, minimum = kitMinimum, maximumHeight = kitMaximum - floor },
                sourcePolicy = AssetDatabase.GetAssetPath(policy), recipe = recipePath,
                rows, limits = new { floor = .001f, patch = .02f, lift = .08f, lower = .03f },
                coverage = "skinned body/cloak/backpack; static packed kit vertex bounds. Boots have no separate equipped renderer."
            }, Newtonsoft.Json.Formatting.Indented));
            return path;
        }
        finally
        {
            QualitySettings.skinWeights = quality;
            if (baked != null) Object.DestroyImmediate(baked);
            if (actor != null) Call(actor.GetComponent<TopDown3DPlayerAnimationDriver>(), "OnDestroy");
            if (actor != null) Object.DestroyImmediate(actor); Object.DestroyImmediate(bindings);
            EditorSceneManager.ClosePreviewScene(preview); EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static double[] DensePhases(AnimationPlaygroundForwardLocomotionProfile policy)
    {
        var phases = new SortedSet<double>(Enumerable.Range(0, 129).Select(i => i / 128d));
        foreach (var gait in policy.gaits)
        {
            foreach (var point in gait.phaseMap)
                foreach (var offset in new[] { -.00005, 0, .00005 }) phases.Add(Math.Clamp(point.semantic + offset, 0, 1));
            foreach (var key in gait.signedHeight.keys) phases.Add(Math.Clamp(Semantic(gait, key.time), 0, 1));
            foreach (var binding in AnimationUtility.GetCurveBindings(gait.sourceClip))
            {
                var curve = AnimationUtility.GetEditorCurve(gait.sourceClip, binding);
                if (curve == null) continue;
                foreach (var key in curve.keys) phases.Add(Math.Clamp(Semantic(gait, key.time / gait.sourceClip.length), 0, 1));
            }
        }
        foreach (var point in new[] { .4, .45, .55, .6 })
            foreach (var offset in new[] { -.00005, 0, .00005 }) phases.Add(point + offset);
        return phases.ToArray();
    }

    private static object[] SupportedTraces(Vector3[][] tracks, int[] ids, double from, double to, AnimationPlaygroundForwardLocomotionProfile policy, float speed, float floor, int plant)
    {
        var support = new bool[33];
        for (var frame = 0; frame < 33; frame++)
            support[frame] = tracks.Min(track => track[frame].y) - floor <= .012f;
        var center = Mathf.Clamp(Mathf.RoundToInt((float)((plant * .25 - from) / (to - from) * 32)), 0, 32);
        if (!support[center]) return Array.Empty<object>();
        var first = center; var last = center;
        while (first > 0 && support[first - 1]) first--;
        while (last < 32 && support[last + 1]) last++;
        if (last - first < 8) return Array.Empty<object>();

        object Select(int start, int stop, bool rolling)
        {
            var best = -1; var bestScore = float.PositiveInfinity; var fit = 0f; var maximumGap = 0f;
            for (var vertex = 0; vertex < tracks.Length; vertex++)
            {
                var gap = 0f; var forwardJump = false; var numerator = 0.0; var denominator = 0.0;
                for (var frame = start; frame <= stop; frame++)
                {
                    gap = Mathf.Max(gap, tracks[vertex][frame].y - floor);
                    var q = from + (to - from) * frame / 32;
                    var centered = (to - from) * (frame - (start + stop) * .5) / 32;
                    var z = tracks[vertex][frame].z - policy.DistanceAtCycles(speed, q);
                    numerator += centered * z; denominator += centered * centered;
                    if (frame > start && tracks[vertex][frame].z - tracks[vertex][frame - 1].z
                        - (policy.DistanceAtCycles(speed, q) - policy.DistanceAtCycles(speed, q - (to - from) / 32)) > .0005) forwardJump = true;
                }
                if (gap > .012 || forwardJump) continue;
                var derivative = (float)(-numerator / denominator);
                if (derivative <= .05) continue;
                var excursion = 0f; var cumulative = 0f; var previous = Vector3.zero;
                for (var frame = start; frame <= stop; frame++)
                {
                    var delta = Vector3.ProjectOnPlane(tracks[vertex][frame] - tracks[vertex][start], Vector3.up);
                    delta.z += (float)(derivative * (to - from) * (frame - start) / 32 - (policy.DistanceAtCycles(speed, from + (to - from) * frame / 32) - policy.DistanceAtCycles(speed, from + (to - from) * start / 32)));
                    excursion = Mathf.Max(excursion, delta.magnitude);
                    if (frame > start) cumulative += (delta - previous).magnitude;
                    previous = delta;
                }
                var score = excursion + cumulative * .2f + gap * .1f;
                if (score < bestScore) { best = vertex; bestScore = score; fit = derivative; maximumGap = gap; }
            }
            if (best < 0) return null;
            var samples = new List<double[]>();
            var anchorExcursion = 0f; var cumulativeTravel = 0f;
            for (var frame = start; frame <= stop; frame++)
            {
                var q = from + (to - from) * frame / 32;
                var p = tracks[best][frame];
                samples.Add(new[] { q, p.x, p.y, p.z - policy.DistanceAtCycles(speed, q) });
                anchorExcursion = Mathf.Max(anchorExcursion, Vector3.ProjectOnPlane(p - tracks[best][start], Vector3.up).magnitude);
                if (frame > start) cumulativeTravel += Vector3.ProjectOnPlane(p - tracks[best][frame - 1], Vector3.up).magnitude;
            }
            return new { vertex = ids[best], rolling, maximumGap, anchorExcursion, cumulativeTravel, fittedDerivative = fit, samples };
        }
        var whole = Select(first, last, false);
        if (whole != null) return new[] { whole };
        // A heel-to-toe roll may replace the physical support patch. Keep two
        // persistent identities with an overlap; never differentiate a switching minimum.
        var a = Select(first, first + (last - first) * 3 / 5, true);
        var b = Select(first + (last - first) * 2 / 5, last, true);
        return a != null && b != null ? new[] { a, b } : Array.Empty<object>();
    }

    private static double Semantic(AnimationPlaygroundForwardGait gait, float native)
    {
        if (native < gait.phaseMap[0].native) native += 1;
        var map = gait.phaseMap;
        for (var i = 1; i < map.Length; i++)
            if (native <= map[i].native)
                return map[i - 1].semantic + (native - map[i - 1].native) / (map[i].native - map[i - 1].native)
                    * (map[i].semantic - map[i - 1].semantic);
        return 1;
    }
}
