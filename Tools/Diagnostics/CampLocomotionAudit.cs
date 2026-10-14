using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using BooterBigArm.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Inert outside Assets. Offline pose/clock measurements only; never runs the motor,
// enters Play Mode, changes a clip/profile, or saves a scene.
public static class CampLocomotionAudit
{
    [Serializable] private sealed class Cadence
    {
        public float speed, requested, clamped, nativeMinimum, nativeMaximum;
        public string dominant;
    }
    [Serializable] private sealed class Envelope
    {
        public string name;
        public float speed, minimumSole = float.PositiveInfinity, maximumContactHeight;
        public float medianStanceSlip, maximumStanceSlip, maximumStep;
        public int stanceSamples;
    }
    [Serializable] private sealed class Plant
    { public string role, side; public float phase, soleHeight; }
    [Serializable] private sealed class Handoff
    { public string name; public float minimumFeetGap = float.PositiveInfinity, maximumFeetGap, hipsGap; }
    [Serializable] private sealed class Report
    {
        public string state = "audit-only-offline-steady-blends-no-gameplay";
        public float feetPlane;
        public Cadence[] cadence;
        public Envelope[] envelopes;
        public Plant[] plants;
        public Handoff[] handoffs;
        public float sprintHalfPeriodError;
    }
    private sealed class Pose
    { public Vector3 left, right, hips; public float minimum, leftSoleY, rightSoleY; }
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
    private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Flags).Invoke(target, args);
    private static float Median(List<float> values)
    { if (values.Count == 0) return 0; values.Sort(); return values[values.Count / 2]; }

    public static string RunSourceAudit()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the isolated background candidate.");
        var output = Path.Combine(root, "LocomotionAudit20261014", "IndependentSources");
        if (Directory.Exists(output)) throw new IOException("Preserve prior source measurements.");
        Directory.CreateDirectory(output);
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        var rows = new List<object>();
        foreach (var role in new[] { TopDown3DLocomotionClipRole.Walk, TopDown3DLocomotionClipRole.Run, TopDown3DLocomotionClipRole.Sprint })
        {
            var entry = bindings.locomotion.GetRequiredEntry(role);
            var curve = Field<AnimationCurve>(entry, "visualHeightCorrection");
            var measured = AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),
                AssetDatabase.GetAssetPath(entry.Clip), bindings.visualScale, bindings.visualOffset, 90, false, curve);
            File.WriteAllText(Path.Combine(output, role + ".json"), JsonUtility.ToJson(measured, true) + "\n");
            var a = measured.frames[Mathf.RoundToInt((measured.frames.Length - 1) * .125f)];
            var b = measured.frames[Mathf.RoundToInt((measured.frames.Length - 1) * .625f)];
            var slips = new List<float>();
            for (var i = 1; i < measured.frames.Length - 1; i++)
            {
                var previous = measured.frames[i - 1]; var frame = measured.frames[i]; var next = measured.frames[i + 1];
                foreach (var side in new[] { 0, 1 })
                {
                    var height = side == 0 ? frame.leftSole.y : frame.rightSole.y;
                    var velocity = ((side == 0 ? next.leftFoot : next.rightFoot) - (side == 0 ? previous.leftFoot : previous.rightFoot))
                        / (next.seconds - previous.seconds);
                    if (height + bindings.capsuleHeight * .5f > .02f || velocity.z >= -.05f) continue;
                    slips.Add(Vector3.ProjectOnPlane(velocity + Vector3.forward * entry.NominalForwardSpeed, Vector3.up).magnitude);
                }
            }
            rows.Add(new { role = role.ToString(), samples = measured.frames.Length, finite = measured.finite,
                halfPeriodFeetGap = Mathf.Max(Vector3.Distance(a.leftFoot, b.leftFoot), Vector3.Distance(a.rightFoot, b.rightFoot)),
                halfPeriodRelativeFeetGap = Mathf.Max(Vector3.Distance(a.leftFoot - a.hips, b.leftFoot - b.hips), Vector3.Distance(a.rightFoot - a.hips, b.rightFoot - b.hips)),
                halfPeriodHipsGap = Vector3.Distance(a.hips, b.hips), nativePlaybackMedianStanceDrift = Median(slips),
                stanceSamples = slips.Count, minimumSole = measured.frames.Min(x => Mathf.Min(x.leftSole.y, x.rightSole.y)) });
        }
        var path = Path.Combine(output, "summary.json");
        File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented) + "\n");
        return path;
    }

    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked isolated background candidate in Edit Mode.");
        var output = Path.Combine(root, "LocomotionAudit20261014", "ProbeV3");
        if (Directory.Exists(output)) throw new IOException("Preserve existing locomotion audit evidence.");
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
        var preview = EditorSceneManager.NewPreviewScene();
        GameObject actor = null; TopDown3DPlayerAnimationDriver driver = null;
        AnimationPlaygroundMotionSampler.SoleProbe soles = null;
        try
        {
            var session = TopDown3DAnimationPlaygroundSession.Find(scene);
            var bindings = session.Bindings; var profile = bindings.locomotion;
            actor = new GameObject("Offline gait audit"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, preview);
            actor.AddComponent<TopDown3DPlayerMotor>(); driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
            EditorUtility.CopySerialized(session.Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
            driver.ConfigureLaboratory(bindings); Call(driver, "Awake"); actor.SetActive(true);
            var graph = Field<PlayableGraph>(driver, "playableGraph"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var mixer = Field<AnimationMixerPlayable>(driver, "mixer");
            var clips = Field<AnimationClipPlayable[]>(driver, "clipPlayables");
            var visual = Field<GameObject>(driver, "visualInstance");
            var animator = driver.HumanoidAnimator; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var leftBone = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightBone = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            soles = new AnimationPlaygroundMotionSampler.SoleProbe(visual,
                animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot));
            for (var i = 0; i < clips.Length; i++) if (clips[i].IsValid()) clips[i].SetSpeed(0);
            var floor = -bindings.capsuleHeight * .5f;
            void Weights(float[] weights)
            {
                for (var i = 0; i < mixer.GetInputCount(); i++) mixer.SetInputWeight(i, i < weights.Length ? weights[i] : 0);
            }
            Pose Read()
            {
                graph.Evaluate(1f / 240);
                soles.Measure(out var left, out var right, out var minimum, out _);
                return new Pose { left = leftBone.position, right = rightBone.position, hips = hips.position,
                    minimum = minimum.y, leftSoleY = left.y, rightSoleY = right.y };
            }
            Pose Pure(int role, float phase)
            {
                var weights = new float[12]; weights[role] = 1; Weights(weights);
                var entry = profile.GetRequiredEntry((TopDown3DLocomotionClipRole)role);
                clips[role].SetTime(phase * entry.Clip.length);
                Call(driver, "ApplyProfileHeightCorrection", role);
                return Read();
            }
            Cadence Rate(float speed, float[] weights)
            {
                var walk = profile.GetRequiredEntry(TopDown3DLocomotionClipRole.Walk);
                var run = profile.GetRequiredEntry(TopDown3DLocomotionClipRole.Run);
                var sprint = profile.GetRequiredEntry(TopDown3DLocomotionClipRole.Sprint);
                var dominant = TopDown3DAnimationMath.SelectDominantGaitRole(weights[1], weights[2], weights[3]);
                var entry = profile.GetRequiredEntry(dominant);
                var requested = TopDown3DAnimationMath.CalculateGaitCyclesPerSecond(speed, walk.NominalForwardSpeed, run.NominalForwardSpeed,
                    sprint.NominalForwardSpeed, 1, walk.Clip.length, run.Clip.length, sprint.Clip.length, weights[1], weights[2], weights[3]);
                var clamped = TopDown3DAnimationMath.ClampCadenceToPlaybackBounds(requested, entry.Clip.length, entry.MinimumPlaybackRate, entry.MaximumPlaybackRate);
                var phases = new[] { entry.RightContactPhases[0], entry.LeftContactPhases[0], entry.RightContactPhases[1], entry.LeftContactPhases[1], entry.RightContactPhases[0] + 1 };
                var rates = Enumerable.Range(0, 4).Select(i => clamped * entry.Clip.length * 4 * (phases[i + 1] - phases[i])).ToArray();
                return new Cadence { speed = speed, requested = requested, clamped = clamped, dominant = dominant.ToString(),
                    nativeMinimum = rates.Min(), nativeMaximum = rates.Max() };
            }
            var cadence = new List<Cadence>();
            foreach (var speed in new[] { .15f, .35f, 1.2f, 1.4f, 2.0499f, 2.05f, 2.0501f, 2.7f, 3.2499f, 3.25f, 3.2501f, 3.8f })
            {
                TopDown3DAnimationMath.CalculateGaitWeights(speed, bindings.slowWalkSpeed, bindings.runSpeed, bindings.sprintSpeed, out var w, out var r, out var s);
                cadence.Add(Rate(speed, new[] { 0, w, r, s }));
            }
            var envelopes = new List<Envelope>();
            void Blend(string name, float speed, float[] weights, float cadenceSpeed = -1)
            {
                Weights(weights);
                var forward = new float[4];
                TopDown3DAnimationMath.CalculateGaitWeights(cadenceSpeed < 0 ? speed : cadenceSpeed,
                    bindings.slowWalkSpeed, bindings.runSpeed, bindings.sprintSpeed, out forward[1], out forward[2], out forward[3]);
                var rate = Rate(speed, forward).clamped;
                var poses = new List<Pose>(); var row = new Envelope { name = name, speed = speed };
                const int intervals = 240;
                for (var i = 0; i <= intervals; i++)
                {
                    Call(driver, "SetPhaseDrivenGaitTimes", (float)i / intervals);
                    Call(driver, "ApplyProfileHeightCorrection", -1);
                    var pose = Read(); poses.Add(pose); row.minimumSole = Mathf.Min(row.minimumSole, pose.leftSoleY, pose.rightSoleY);
                    if (i % 60 == 0) row.maximumContactHeight = Mathf.Max(row.maximumContactHeight,
                        (i % 120 == 0 ? pose.rightSoleY : pose.leftSoleY) - floor);
                }
                var slips = new List<float>(); var dt = 1 / (rate * intervals);
                for (var i = 1; i < intervals; i++)
                {
                    foreach (var side in new[] { 0, 1 })
                    {
                        var point = side == 0 ? poses[i].left : poses[i].right;
                        var previous = side == 0 ? poses[i - 1].left : poses[i - 1].right;
                        var next = side == 0 ? poses[i + 1].left : poses[i + 1].right;
                        row.maximumStep = Mathf.Max(row.maximumStep, Vector3.Distance(point, previous));
                        var velocity = (next - previous) / (2 * dt);
                        if ((side == 0 ? poses[i].leftSoleY : poses[i].rightSoleY) - floor > .02f || velocity.z >= -.05f) continue;
                        var slip = Vector3.ProjectOnPlane(velocity + Vector3.forward * speed, Vector3.up).magnitude;
                        slips.Add(slip); row.maximumStanceSlip = Mathf.Max(row.maximumStanceSlip, slip);
                    }
                }
                row.stanceSamples = slips.Count; row.medianStanceSlip = Median(slips); envelopes.Add(row);
            }
            Blend("walk", 1.4f, new[] { 0f, 1f, 0f, 0f });
            Blend("run", 2.7f, new[] { 0f, 0f, 1f, 0f });
            Blend("sprint", 3.8f, new[] { 0f, 0f, 0f, 1f });
            Blend("walk-run-half", 2.05f, new[] { 0f, .5f, .5f, 0f });
            Blend("run-sprint-half", 3.25f, new[] { 0f, 0f, .5f, .5f });
            Blend("walk-left-turn", 1.4f, new[] { 0f, .45f, 0f, 0f, .55f, 0f });
            Blend("walk-right-turn", 1.4f, new[] { 0f, .45f, 0f, 0f, 0f, .55f });
            Blend("run-left-turn", 2.7f, new[] { 0f, 0f, .45f, 0f, .55f, 0f });
            Blend("run-right-turn", 2.7f, new[] { 0f, 0f, .45f, 0f, 0f, .55f });
            var plants = new List<Plant>();
            foreach (var role in new[] { 1, 2, 3 })
            {
                var entry = profile.GetRequiredEntry((TopDown3DLocomotionClipRole)role);
                foreach (var phase in entry.LeftContactPhases) plants.Add(new Plant { role = entry.Role.ToString(), side = "Left", phase = phase, soleHeight = Pure(role, phase).leftSoleY - floor });
                foreach (var phase in entry.RightContactPhases) plants.Add(new Plant { role = entry.Role.ToString(), side = "Right", phase = phase, soleHeight = Pure(role, phase).rightSoleY - floor });
            }
            var firstHalf = Pure(3, .125f); var secondHalf = Pure(3, .625f);
            var periodError = Mathf.Max(Vector3.Distance(firstHalf.left, secondHalf.left), Vector3.Distance(firstHalf.right, secondHalf.right));
            var handoffs = new List<Handoff>();
            foreach (var start in new[] { 6, 7, 8, 9 })
            {
                var entry = profile.GetRequiredEntry((TopDown3DLocomotionClipRole)start);
                var source = Pure(start, start < 8 ? entry.ReleaseTime : entry.PlantTime);
                var row = new Handoff { name = entry.Role + (start < 8 ? "-to-sustained" : "-to-idle") };
                foreach (var destination in start < 8 ? new[] { 1, 2, 3 } : new[] { 0 })
                    foreach (var phase in destination == 0 ? new[] { 0f } : new[] { 0f, .25f, .5f, .75f })
                    {
                        var target = Pure(destination, phase);
                        var gap = Mathf.Max(Vector3.Distance(source.left, target.left), Vector3.Distance(source.right, target.right));
                        row.minimumFeetGap = Mathf.Min(row.minimumFeetGap, gap); row.maximumFeetGap = Mathf.Max(row.maximumFeetGap, gap);
                        row.hipsGap = Mathf.Max(row.hipsGap, Vector3.Distance(source.hips, target.hips));
                    }
                handoffs.Add(row);
            }
            var report = new Report { feetPlane = floor, cadence = cadence.ToArray(), envelopes = envelopes.ToArray(),
                plants = plants.ToArray(), handoffs = handoffs.ToArray(), sprintHalfPeriodError = periodError };
            var path = Path.Combine(output, "summary.json");
            File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented) + "\n");
            return path;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(output, "failure.txt"), error.ToString()); throw; }
        finally
        {
            soles?.Dispose();
            if (actor != null) Object.DestroyImmediate(actor);
            EditorSceneManager.ClosePreviewScene(preview); EditorSceneManager.CloseScene(scene, true);
        }
    }
}
