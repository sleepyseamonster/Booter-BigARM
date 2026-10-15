using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

// Numerical command model only. Does not create actors, step physics or enter Play Mode.
// The alternative is deliberately confined to this diagnostic, outside Assets.
public static class CampForwardSteeringAudit
{
    private sealed class Settings
    {
        public float walk, run, sprint, acceleration, deceleration, stop, reversal, turn, sprintTurn;
    }

    private struct State { public Vector3 velocity; public Quaternion rotation; }
    private sealed class Measurement
    {
        public string policy, scenario;
        public float speed, angle, timestep, requestedYawRate;
        public int samples, infeasible, projectionLimited;
        public float maximumFacingError, misalignedSeconds, backwardsSeconds, backwardsDistance;
        public float maximumBackwardsSpeed, maximumAcceleration, maximumYawRate, targetArrivalSeconds = -1;
        public float travelDistance, pathX, pathZ, maximumSpeed, finalSpeed, finalHeadingError;
    }

    public static string Run(string name = "ForwardSteeringCommandV3")
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the compiled, marked, non-playing candidate only.");
        if (!Regex.IsMatch(name, @"^[A-Za-z0-9]+$")) throw new ArgumentException("Use a simple immutable run name.");
        var output = Path.Combine(root, "LocomotionImplementation20261014", name);
        if (Directory.Exists(output)) throw new IOException("Preserve earlier measurements.");
        const string motor = "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.cs";
        const string lab = "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerMotor.AnimationPlayground.cs";
        const string session = "Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/TopDown3DAnimationPlaygroundSession.Player.cs";
        const string scene = "Assets/_Project/Scenes/Reference/CampLoopTesting.unity";
        var sceneText = File.ReadAllText(Path.Combine(root, scene));
        var motorGuid = Regex.Match(File.ReadAllText(Path.Combine(root, motor + ".meta")), @"guid: (\w+)").Groups[1].Value;
        var motorRecord = Regex.Split(sceneText, @"(?m)^--- !u!").Single(s => s.Contains("guid: " + motorGuid + ","));
        var bindingGuid = Regex.Match(sceneText, @"(?m)^  bindings: \{fileID: 11400000, guid: (\w+),").Groups[1].Value;
        var bindingPath = AssetDatabase.GUIDToAssetPath(bindingGuid);
        var binding = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(bindingPath));
        float Config(string key) => float.Parse(Regex.Match(motorRecord, @"(?m)^  " + key + @": ([\d.]+)").Groups[1].Value, CultureInfo.InvariantCulture);
        var settings = new Settings {
            walk = binding.FindProperty("slowWalkSpeed").floatValue,
            run = binding.FindProperty("runSpeed").floatValue,
            sprint = binding.FindProperty("sprintSpeed").floatValue,
            acceleration = Config("acceleration"), deceleration = Config("deceleration"),
            stop = Config("stopDeceleration"), reversal = Config("directionChangeAcceleration"),
            turn = Config("turnSpeedDegrees"), sprintTurn = Config("sprintTurnSpeedDegrees")
        };
        var paths = new[] { motor, motor + ".meta", lab, session, scene, bindingPath, bindingPath + ".meta" };
        var sources = paths.ToDictionary(p => p, p => Hash(File.ReadAllBytes(Path.Combine(root, p))));
        var all = new List<Measurement>();
        foreach (var dt in new[] { 1f / 30, 1f / 50, 1f / 60, 1f / 120 })
        foreach (var speed in new[] { settings.walk, settings.run, settings.sprint })
        foreach (var policy in new[] { "existing", "forward-constrained-prototype" })
        {
            foreach (var angle in new[] { -180f, -135, -90, -45, 0, 45, 90, 135, 180 })
            {
                all.Add(Trace(settings, dt, speed, angle, false, false, policy, null));
                all.Add(Trace(settings, dt, speed, angle, true, false, policy, null));
            }
            all.Add(Trace(settings, dt, speed, 0, false, true, policy, null));
            foreach (var yawRate in new[] { -360f, -90, -45, 45, 90, 360 })
                all.Add(Trace(settings, dt, speed, 0, false, false, policy, null, yawRate));
        }
        // Test the prototype's acceleration-ball projection independently of command traces.
        var projectionChecks = 0;
        foreach (var speed in new[] { 0f, .01f, .1f, 1.4f, 2.7f, 3.8f })
        foreach (var degrees in new[] { 0f, 3f, 15f, 45f, 90f, 135f, 180f })
        foreach (var radius in new[] { .01f, .1f, .27f, 1f, 8f })
        {
            var current = Vector3.forward * speed;
            var direction = Quaternion.Euler(0, degrees, 0) * Vector3.forward;
            if (ProjectForward(current, direction, direction * 3.8f, radius, out var projected))
            {
                if ((projected - current).magnitude > radius + .00002f || Vector3.Dot(projected, direction) < -.00001f
                    || Vector3.Cross(projected, direction).magnitude > .00002f)
                    throw new InvalidOperationException("Projection violated its mathematical contract.");
            }
            else
            {
                var closest = direction * Mathf.Max(0, Vector3.Dot(current, direction));
                if ((closest - current).magnitude < radius - .00002f)
                    throw new InvalidOperationException("Projection rejected an available nonnegative ray solution.");
            }
            projectionChecks++;
        }
        var representativeTrace = new List<object>();
        foreach (var policy in new[] { "existing", "forward-constrained-prototype" })
            Trace(settings, .02f, settings.sprint, 180, false, false, policy, representativeTrace);
        foreach (var p in paths)
            if (Hash(File.ReadAllBytes(Path.Combine(root, p))) != sources[p]) throw new InvalidOperationException("Source changed during audit.");
        if (EditorApplication.isCompiling) throw new InvalidOperationException("Assembly changed during audit.");
        Directory.CreateDirectory(output);
        var encoding = new UTF8Encoding(false);
        void Write(string name, object value) => File.WriteAllText(Path.Combine(output, name),
            Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented) + "\n", encoding);
        Write("summary.json", new {
            state = "offline-command-prototype-not-installed-or-qualified", settings, sources,
            moduleId = typeof(TopDown3DLocomotionMath).Module.ModuleVersionId,
            loadedMath = new[] { "SelectAccelerationRate", "SelectTurnSpeed" }.ToDictionary(n => n,
                n => Hash(typeof(TopDown3DLocomotionMath).GetMethod(n).GetMethodBody().GetILAsByteArray())),
            projectionChecks, cases = all.Count, measurements = all,
            scope = "Flat standing command integration. Uses loaded runtime rate helpers and saved motor/lab settings. Assumes issued rotation completes each tick. No actors, physics, collision, animation, contact, render interpolation or gameplay testing.",
            prototype = "Existing provisional velocity and body yaw, then nearest nonnegative forward-ray velocity within the same acceleration ball. If requested yaw is just outside that ball's feasible cone, reduce yaw by bounded bisection. Explicit unavailable fallback retains provisional velocity when even the current body heading is infeasible. No production policy changed."
        });
        Write("sprint-reversal-trace.json", representativeTrace);
        return Path.Combine(output, "summary.json");
    }

    private static Measurement Trace(Settings c, float dt, float speed, float angle, bool start, bool stop,
        string policy, List<object> trace, float yawRate = 0)
    {
        var state = new State { velocity = start ? Vector3.zero : Vector3.forward * speed, rotation = Quaternion.identity };
        var desired = stop ? Vector3.zero : Quaternion.Euler(0, angle, 0) * Vector3.forward;
        var target = desired * speed;
        var result = new Measurement { policy = policy, scenario = yawRate != 0 ? "orbit" : stop ? "stop" : start ? "start" : "turn", speed = speed, angle = angle, timestep = dt, requestedYawRate = yawRate };
        var position = Vector3.zero;
        for (var i = 0; i < Mathf.CeilToInt(3 / dt); i++)
        {
            if (yawRate != 0)
            {
                desired = Quaternion.Euler(0, yawRate * (i + 1) * dt, 0) * Vector3.forward;
                target = desired * speed;
            }
            var before = state;
            var rate = TopDown3DLocomotionMath.SelectAccelerationRate(before.velocity, target,
                c.acceleration, c.deceleration, c.stop, c.reversal);
            var provisional = Vector3.MoveTowards(before.velocity, target, rate * dt);
            var facing = provisional.sqrMagnitude <= .01f ? desired : provisional;
            var turnRate = TopDown3DLocomotionMath.SelectTurnSpeed(provisional.magnitude, c.run, c.sprint, c.turn, c.sprintTurn);
            var rotation = facing.sqrMagnitude > .0001f ? Quaternion.RotateTowards(before.rotation,
                Quaternion.LookRotation(facing.normalized, Vector3.up), turnRate * dt) : before.rotation;
            var next = provisional;
            if (policy != "existing" && !ProjectForward(before.velocity, rotation * Vector3.forward, provisional, rate * dt, out next))
            {
                if (ProjectForward(before.velocity, before.rotation * Vector3.forward, provisional, rate * dt, out next))
                {
                    var low = 0f; var high = 1f;
                    var wantedRotation = rotation;
                    var availableRotation = before.rotation;
                    for (var iteration = 0; iteration < 18; iteration++)
                    {
                        var fraction = (low + high) * .5f;
                        var trial = Quaternion.Slerp(before.rotation, wantedRotation, fraction);
                        if (ProjectForward(before.velocity, trial * Vector3.forward, provisional, rate * dt, out var velocity))
                        { low = fraction; availableRotation = trial; next = velocity; }
                        else high = fraction;
                    }
                    rotation = availableRotation; result.projectionLimited++;
                }
                else { result.infeasible++; next = provisional; }
            }
            state = new State { velocity = next, rotation = rotation };
            var heading = rotation * Vector3.forward;
            var facingError = next.magnitude >= .18f ? Vector3.Angle(heading, next) : 0;
            var backwards = Mathf.Max(0, -Vector3.Dot(next, heading));
            var previousHeading = before.rotation * Vector3.forward;
            var yawStep = (float)(Math.Atan2(Math.Abs((double)previousHeading.x * heading.z - (double)previousHeading.z * heading.x),
                (double)previousHeading.x * heading.x + (double)previousHeading.z * heading.z) * 180 / Math.PI);
            if ((next - before.velocity).magnitude > rate * dt + .00002f
                || yawStep > turnRate * dt + .01f
                || next.magnitude > speed + .00002f)
                throw new InvalidOperationException($"Command limit: {policy} {result.scenario} speed={speed} angle={angle} yaw={yawRate} dt={dt} tick={i}; dv={(next-before.velocity).magnitude}/{rate*dt}; yaw={Quaternion.Angle(before.rotation,rotation)}/{turnRate*dt}; speed={next.magnitude}/{speed}.");
            position += next * dt;
            result.samples++;
            result.maximumFacingError = Mathf.Max(result.maximumFacingError, facingError);
            if (facingError > 20) result.misalignedSeconds += dt;
            if (backwards > .01f) result.backwardsSeconds += dt;
            result.backwardsDistance += backwards * dt;
            result.maximumBackwardsSpeed = Mathf.Max(result.maximumBackwardsSpeed, backwards);
            result.maximumAcceleration = Mathf.Max(result.maximumAcceleration, (next - before.velocity).magnitude / dt);
            result.maximumYawRate = Mathf.Max(result.maximumYawRate, yawStep / dt);
            result.maximumSpeed = Mathf.Max(result.maximumSpeed, next.magnitude);
            result.travelDistance += next.magnitude * dt;
            if (result.targetArrivalSeconds < 0 && (next - target).magnitude < .02f
                && (stop || Vector3.Angle(heading, desired) < 1)) result.targetArrivalSeconds = (i + 1) * dt;
            if (trace != null) trace.Add(new { policy, time = (i + 1) * dt, x = position.x, z = position.z,
                vx = next.x, vz = next.z, yaw = rotation.eulerAngles.y, facingError, backwards,
                acceleration = (next - before.velocity).magnitude / dt });
        }
        result.pathX = position.x; result.pathZ = position.z; result.finalSpeed = state.velocity.magnitude;
        result.finalHeadingError = stop ? 0 : Vector3.Angle(state.rotation * Vector3.forward, desired);
        return result;
    }

    private static bool ProjectForward(Vector3 current, Vector3 forward, Vector3 provisional, float radius, out Vector3 projected)
    {
        forward.Normalize();
        // Cross-product distance avoids cancellation when current and forward
        // are almost parallel at the edge of a small acceleration ball.
        var length = Math.Sqrt((double)forward.x * forward.x + (double)forward.z * forward.z);
        var along = ((double)current.x * forward.x + (double)current.z * forward.z) / length;
        var cross = ((double)current.x * forward.z - (double)current.z * forward.x) / length;
        var available = (double)radius * radius - cross * cross;
        projected = default;
        if (available < -1e-12) return false;
        var extent = Math.Sqrt(Math.Max(0, available));
        var low = Math.Max(0, along - extent);
        var high = along + extent;
        if (high < low) return false;
        var wanted = ((double)provisional.x * forward.x + (double)provisional.z * forward.z) / length;
        projected = forward * (float)(Math.Max(low, Math.Min(high, wanted)) / length);
        return true;
    }

    private static string Hash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    public static string RunPoses(string name = "ForwardSteeringPoseV1")
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the marked non-playing candidate.");
        if (!Regex.IsMatch(name, @"^[A-Za-z0-9]+$")) throw new ArgumentException("Use a simple immutable run name.");
        var directory = Path.Combine(root, "LocomotionImplementation20261014", name);
        if (Directory.Exists(directory)) throw new IOException("Preserve prior measurements.");
        var commands = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root,
            "LocomotionImplementation20261014/ForwardSteeringCommandV3/summary.json")));
        var settings = commands["settings"].ToObject<Settings>();
        var recipePath = "LocomotionImplementation20261014/native-calibration-v6.json";
        var recipe = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root, recipePath)));
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t => t != null);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        using var fixture = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { true, SkinWeights.FourBones }, null);
        var driver = (TopDown3DPlayerAnimationDriver)type.GetField("Driver").GetValue(fixture);
        var actor = (GameObject)type.GetField("Actor").GetValue(fixture);
        var rigidbody = (Rigidbody)type.GetField("Player").GetValue(fixture);
        var profile = (AnimationPlaygroundForwardLocomotionProfile)type.GetField("Policy").GetValue(fixture);
        var graph = (PlayableGraph)type.GetField("Graph").GetValue(fixture);
        var visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", flags).GetValue(driver);
        var skin = visual.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s => s.name == "skull");
        var prepare = (Action)Delegate.CreateDelegate(typeof(Action), driver, typeof(TopDown3DPlayerAnimationDriver).GetMethod("PrepareForwardGraphUpdate", flags));
        var render = (Action<float, double, float>)Delegate.CreateDelegate(typeof(Action<float, double, float>), driver, typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardFrame", flags));
        var late = (Action)Delegate.CreateDelegate(typeof(Action), driver, typeof(TopDown3DPlayerAnimationDriver).GetMethod("LateUpdate", flags));
        var origin = actor.transform.position;
        var floor = origin.y - .91f;
        var baked = new Mesh(); var vertices = new List<Vector3>(); var rows = new List<object>();
        var paths = commands["sources"].ToObject<Dictionary<string, string>>().Keys.Concat(new[] {
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.ForwardReview.cs",
            "Assets/_Project/Scripts/Runtime/TopDown3D/TopDown3DPlayerAnimationDriver.LaboratoryLocomotion.cs",
            "Assets/_Project/Tests/Editor/AnimationPlaygroundForwardReviewTests.cs",
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset",
            recipePath }).Distinct().ToArray();
        var sources = paths.ToDictionary(p => p, p => Hash(File.ReadAllBytes(Path.Combine(root, p))));
        foreach (var p in commands["sources"].ToObject<Dictionary<string, string>>())
            if (sources[p.Key] != p.Value) throw new InvalidOperationException("Command-model sources have changed.");
        try
        {
            for (var gait = 0; gait < 3; gait++)
            foreach (var policy in new[] { "existing", "forward-constrained-prototype" })
            foreach (var angle in new[] { 0f, 180f })
            foreach (var initialPhase in new[] { 0d, .125, .25, .375 })
            {
                var speed = new[] { settings.walk, settings.run, settings.sprint }[gait];
                var trace = new List<object>(); Trace(settings, .02f, speed, angle, false, false, policy, trace);
                var phase = initialPhase; var lastPlant = long.MinValue; var anchor = Vector3.zero; var previous = Vector3.zero;
                var sampleCount = 0; var contactSamples = 0; var segments = 0;
                var maximumExcursion = 0f; var maximumTravel = 0f; var travel = 0f;
                var maximumGap = float.NegativeInfinity; var minimumPatchGap = float.PositiveInfinity;
                var maximumPatchSpeed = 0f; var maximumLift = float.NegativeInfinity; var minimumLift = float.PositiveInfinity;
                var nearPlant = long.MinValue; var nearAnchor = Vector3.zero; var nearPrevious = Vector3.zero;
                var nearSamples = 0; var nearWindowSamples = 0; var nearSegments = 0; var nearTravel = 0f; var nearWindowExcursion = 0f;
                var nearMaximumExcursion = 0f; var nearMaximumTravel = 0f;
                // Stabilize the direct pose envelope at this nominal speed first.
                actor.transform.SetPositionAndRotation(origin, Quaternion.identity); rigidbody.position = origin; rigidbody.rotation = Quaternion.identity;
                for (var warm = 0; warm < 16; warm++) { prepare(); render(speed, phase, .02f); late(); }
                foreach (var item in trace)
                {
                    var tick = Newtonsoft.Json.Linq.JObject.FromObject(item);
                    var velocity = new Vector3((float)tick["vx"], 0, (float)tick["vz"]);
                    var actualSpeed = velocity.magnitude;
                    phase = profile.AdvanceCycles(actualSpeed, phase, actualSpeed * .02);
                    var position = origin + new Vector3((float)tick["x"], 0, (float)tick["z"]);
                    var rotation = Quaternion.Euler(0, (float)tick["yaw"], 0);
                    actor.transform.SetPositionAndRotation(position, rotation); rigidbody.position = position; rigidbody.rotation = rotation;
                    prepare(); render(actualSpeed, phase, .02f); late();
                    skin.BakeMesh(baked, true); baked.GetVertices(vertices); sampleCount++;
                    maximumLift = Mathf.Max(maximumLift, driver.LaboratoryForwardLift); minimumLift = Mathf.Min(minimumLift, driver.LaboratoryForwardLift);
                    var weights = profile.Weights(actualSpeed); var plantFound = false;
                    for (var plant = 0; plant < 4; plant++)
                    {
                        var from = double.NegativeInfinity; var to = double.PositiveInfinity;
                        for (var contributor = 0; contributor < 3; contributor++)
                        {
                            if (weights[contributor + 1] < .09999f) continue;
                            var patch = recipe["gaits"][contributor]["patches"][plant];
                            var entry = profile.gaits[contributor];
                            var start = Semantic(entry, (float)patch["start"] / entry.sourceClip.length);
                            var end = Semantic(entry, (float)patch["end"] / entry.sourceClip.length);
                            while (start - plant * .25 > .5) start -= 1;
                            while (end - plant * .25 > .5) end -= 1;
                            from = Math.Max(from, start); to = Math.Min(to, end);
                        }
                        var cycle = Math.Floor(phase - (from + to) * .5 + .5);
                        if (!double.IsFinite(from) || to <= from || phase < from + cycle || phase > to + cycle || actualSpeed < .18f) continue;
                        plantFound = true;
                        var vertex = (int)recipe["gaits"][gait]["patches"][plant]["identity"]["vertex"];
                        var point = skin.transform.TransformPoint(vertices[vertex]);
                        var key = (long)cycle * 4 + plant;
                        if (lastPlant != key) { lastPlant = key; anchor = point; previous = point; travel = 0; segments++; }
                        var step = Vector3.ProjectOnPlane(point - previous, Vector3.up).magnitude;
                        travel += step; previous = point; contactSamples++;
                        maximumExcursion = Mathf.Max(maximumExcursion, Vector3.ProjectOnPlane(point - anchor, Vector3.up).magnitude);
                        maximumTravel = Mathf.Max(maximumTravel, travel); maximumPatchSpeed = Mathf.Max(maximumPatchSpeed, step / .02f);
                        maximumGap = Mathf.Max(maximumGap, point.y - floor); minimumPatchGap = Mathf.Min(minimumPatchGap, point.y - floor);
                        // Report the broad proposed stance separately from contiguous
                        // samples of the same vertex within [-1mm,+8mm] of the floor.
                        // Low height is a proxy; it cannot prove that a point is planted.
                        if (point.y - floor >= -.001f && point.y - floor <= .008f)
                        {
                            if (nearPlant != key) { nearPlant = key; nearAnchor = point; nearPrevious = point; nearTravel = 0; nearWindowSamples = 0; nearWindowExcursion = 0; }
                            nearTravel += Vector3.ProjectOnPlane(point - nearPrevious, Vector3.up).magnitude;
                            nearWindowExcursion = Mathf.Max(nearWindowExcursion, Vector3.ProjectOnPlane(point - nearAnchor, Vector3.up).magnitude);
                            nearPrevious = point; nearSamples++; nearWindowSamples++;
                            if (nearWindowSamples == 3) nearSegments++;
                            if (nearWindowSamples >= 3)
                            {
                                nearMaximumExcursion = Mathf.Max(nearMaximumExcursion, nearWindowExcursion);
                                nearMaximumTravel = Mathf.Max(nearMaximumTravel, nearTravel);
                            }
                        }
                        else nearPlant = long.MinValue;
                        break;
                    }
                    if (!plantFound) { lastPlant = long.MinValue; nearPlant = long.MinValue; }
                }
                rows.Add(new { policy, speed, angle, initialPhase, sampleCount, contactSamples, segments,
                    maximumExcursion, maximumTravel, maximumPatchSpeed, maximumGap, minimumPatchGap, maximumLift, minimumLift,
                    nearSamples, nearSegments, nearMaximumExcursion, nearMaximumTravel });
            }
            foreach (var p in paths)
                if (Hash(File.ReadAllBytes(Path.Combine(root, p))) != sources[p]) throw new InvalidOperationException("Pose source changed.");
            if (EditorApplication.isCompiling || graph.GetOutputCount() != 1) throw new InvalidOperationException("Pose graph changed.");
            Directory.CreateDirectory(directory);
            var destination = Path.Combine(directory, "summary.json");
            File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(new {
                state = "offline-equipped-pose-observation-not-gameplay-or-contact-qualification", sources,
                moduleId = typeof(TopDown3DPlayerAnimationDriver).Module.ModuleVersionId, outputs = graph.GetOutputCount(), rows,
                scope = "Actual flat review graph with cold authored four-influence equipment; manually placed command-model roots, 50Hz direct poses, distance phase advance, fixed nominal-gait sole vertex per plant and intersection of significant contributor stance proposals. Broad windows can contain elevated points. Separate near metrics require the same vertex within [-1mm,+8mm] for at least three contiguous samples; height alone does not prove support. No physics/gameplay simulation or full-mesh floor proof. Sparse samples, changing blend windows and rolling affect travel interpretation."
            }, Newtonsoft.Json.Formatting.Indented) + "\n", new UTF8Encoding(false));
            return destination;
        }
        finally { UnityEngine.Object.DestroyImmediate(baked); }
    }

    private static double Semantic(AnimationPlaygroundForwardGait gait, float native)
    {
        if (native < gait.phaseMap[0].native) native += 1;
        for (var i = 1; i < gait.phaseMap.Length; i++)
            if (native <= gait.phaseMap[i].native)
                return gait.phaseMap[i - 1].semantic + (native - gait.phaseMap[i - 1].native)
                    / (gait.phaseMap[i].native - gait.phaseMap[i - 1].native) * (gait.phaseMap[i].semantic - gait.phaseMap[i - 1].semantic);
        return 1;
    }
}
