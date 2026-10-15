using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Offline source/contact measurement. No controller ticks, physics simulation,
// live root-motion installation or asset serialization.
public static class CampTurnContactQualification
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable] private sealed class Track
    {
        public string clip; public int quality; public int[] sides;
        public List<Frame> frames = new();
    }
    [Serializable] private sealed class Frame
    {
        public float seconds, lift; public Vector3[] points;
    }

    public static string Run(string name = "TurnContactV2")
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the marked, compiled, non-playing candidate.");
        if (!Regex.IsMatch(name, @"^[A-Za-z0-9]+$")) throw new ArgumentException("Use an immutable alphanumeric run name.");
        var output = Path.Combine(root, "LocomotionImplementation20261014", name);
        if (Directory.Exists(output)) throw new IOException("Preserve prior measurements.");
        const string source = "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/Animations/m@RapidTurningLeft.fbx";
        const string policyPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset";
        var profile = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(policyPath);
        var tapeRoot = "LocomotionImplementation20261014/TurnSourceContactBasisV1/";
        var clips = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).OrderBy(c => c.name).ToArray();
        if (clips.Length != 2 || clips.Any(c => !c.isHumanMotion)) throw new InvalidDataException("Require the two named imported Humanoid turns.");
        var inputPaths = AssetDatabase.GetDependencies(new[] { policyPath, source,
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset" }, true)
            .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && File.Exists(Path.Combine(root, p)))
            .SelectMany(p => File.Exists(Path.Combine(root, p + ".meta")) ? new[] { p, p + ".meta" } : new[] { p })
            .Concat(clips.Select(c => tapeRoot + c.name + "-private-applied-poses.json")).Distinct().ToArray();
        string Hash(string path) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        var inputs = inputPaths.ToDictionary(p => p, p => Hash(Path.Combine(root, p)));
        var fixtureType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t => t != null);
        var assemblies = new[] { typeof(TopDown3DPlayerAnimationDriver).Assembly, fixtureType.Assembly, typeof(AnimationPlaygroundMotionSampler).Assembly }
            .ToDictionary(a => a.GetName().Name, a => new { moduleId = a.ManifestModule.ModuleVersionId, sha256 = Hash(a.Location) });
        Directory.CreateDirectory(output); var rows = new List<object>();
        foreach (var quality in new[] { SkinWeights.OneBone, SkinWeights.TwoBones, SkinWeights.FourBones })
        foreach (var clip in clips)
        {
            var tape = JsonUtility.FromJson<AnimationPlaygroundMotionSampler.MotionMeasurement>(File.ReadAllText(Path.Combine(root, tapeRoot + clip.name + "-private-applied-poses.json")));
            if (tape.clipName != clip.name || tape.frames.Length < 180 || Math.Abs(tape.seconds - clip.length) > .00001f)
                throw new InvalidDataException("Require the named 120Hz evaluated source-root tape.");
            using var fixture = (IDisposable)Activator.CreateInstance(fixtureType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new object[] { true, quality }, null);
            T Field<T>(string field) => (T)fixtureType.GetField(field).GetValue(fixture);
            var driver = Field<TopDown3DPlayerAnimationDriver>("Driver");
            var actor = Field<GameObject>("Actor"); var body = Field<Rigidbody>("Player"); var graph = Field<PlayableGraph>("Graph");
            var mixer = Field<AnimationMixerPlayable>("Mixer");
            var visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", Private).GetValue(driver);
            var localPosition = (Vector3)typeof(TopDown3DPlayerAnimationDriver).GetField("visualLocalPosition", Private).GetValue(driver);
            var localEuler = (Vector3)typeof(TopDown3DPlayerAnimationDriver).GetField("visualLocalEulerAngles", Private).GetValue(driver);
            var sampler = profile.footProjection.Bind(visual.transform);
            var bones = profile.footProjection.bonePaths.Select(p => p.Length == 0 ? visual.transform : visual.transform.Find(p)).ToArray();
            var left = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var right = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            var sides = profile.footProjection.vertices.Select(v => {
                var l = v.points.Where(p => bones[p.bone].IsChildOf(left)).Sum(p => p.weight);
                var r = v.points.Where(p => bones[p.bone].IsChildOf(right)).Sum(p => p.weight);
                return l >= .5f ? 0 : r >= .5f ? 1 : -1;
            }).ToArray();
            if (sides.Any(s => s < 0)) throw new InvalidDataException("All cached sole points need an anatomical side.");
            var selected = profile.footProjection.vertices.Select(v => v.points.Select((p, i) => new { point = p, index = i })
                .OrderByDescending(p => p.point.weight).ThenBy(p => p.index).Take((int)quality).Select(p => p.point).ToArray()).ToArray();
            var origin = actor.transform.position; var floor = origin.y - .91f;
            var playable = AnimationClipPlayable.Create(graph, clip); playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); playable.SetSpeed(0);
            var slot = mixer.GetInputCount(); mixer.SetInputCount(slot + 1); graph.Connect(playable, 0, mixer, slot);
            for (var i = 0; i < mixer.GetInputCount(); i++) mixer.SetInputWeight(i, i == slot ? 1 : 0);
            var late = (Action)Delegate.CreateDelegate(typeof(Action), driver, typeof(TopDown3DPlayerAnimationDriver).GetMethod("LateUpdate", Private));
            var record = new Track { clip = clip.name, quality = (int)quality, sides = sides };
            var minMesh = float.PositiveInfinity; var maxHeight = float.NegativeInfinity; var maxLift = 0f; var maxPredictionError = 0f;
            var minSole = float.PositiveInfinity; var maxNearestGap = 0f; var foundCloak = false; var foundRigid = false;
            var mesh = new Mesh(); var vertices = new List<Vector3>(); var matrices = new Matrix4x4[bones.Length];
            AnimationPlaygroundMotionSampler.SoleProbe sole = null;
            try
            {
                foreach (var frame in tape.frames)
                {
                    actor.transform.SetPositionAndRotation(origin, frame.rootRotation); body.position = origin; body.rotation = frame.rootRotation;
                    visual.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(localEuler));
                    foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.quality = SkinQuality.Auto;
                    playable.SetTime(frame.seconds); graph.Evaluate(0);
                    var lift = Mathf.Max(0, floor - sampler.MinimumWorldY());
                    if (!float.IsFinite(lift) || lift > profile.maximumLift + .00001f) throw new InvalidOperationException("Turn exceeds existing host lift limit.");
                    visual.transform.localPosition = localPosition + Vector3.up * lift; maxLift = Mathf.Max(maxLift, lift);
                    late();
                    foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.quality = SkinQuality.Auto;
                    sole ??= new AnimationPlaygroundMotionSampler.SoleProbe(visual, left, right);
                    sole.MeasureSoles(out var actualLeft, out var actualRight);
                    maxPredictionError = Mathf.Max(maxPredictionError, Mathf.Abs(sampler.MinimumWorldY() - Mathf.Min(actualLeft.y, actualRight.y)));
                    minSole = Mathf.Min(minSole, actualLeft.y - floor, actualRight.y - floor);
                    maxNearestGap = Mathf.Max(maxNearestGap, Mathf.Min(actualLeft.y, actualRight.y) - floor);
                    for (var i = 0; i < bones.Length; i++) matrices[i] = bones[i].localToWorldMatrix;
                    var points = new Vector3[selected.Length];
                    for (var i = 0; i < selected.Length; i++)
                    {
                        var point = Vector3.zero; var total = 0f;
                        foreach (var influence in selected[i]) { point += matrices[influence.bone].MultiplyPoint3x4(influence.position) * influence.weight; total += influence.weight; }
                        points[i] = point / total - origin; points[i].y += .91f;
                        if (!float.IsFinite(points[i].sqrMagnitude)) throw new InvalidDataException("Nonfinite cached sole track.");
                    }
                    record.frames.Add(new Frame { seconds = frame.seconds, lift = lift, points = points });
                    foreach (var renderer in driver.HumanoidAnimator.transform.parent.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                        vertices.Clear();
                        if (renderer is SkinnedMeshRenderer skin)
                        { skin.BakeMesh(mesh, true); mesh.GetVertices(vertices); foundCloak |= skin.name == "Cloak cloth skin"; }
                        else if (renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                        { filter.sharedMesh.GetVertices(vertices); foundRigid = true; }
                        else continue;
                        foreach (var vertex in vertices)
                        {
                            var height = renderer.transform.TransformPoint(vertex).y - floor;
                            if (!float.IsFinite(height)) throw new InvalidDataException("Nonfinite full equipped turn mesh.");
                            minMesh = Mathf.Min(minMesh, height); maxHeight = Mathf.Max(maxHeight, height);
                        }
                    }
                }
                if (!foundCloak || !foundRigid || graph.GetOutputCount() != 1) throw new InvalidOperationException("Require cold cloak, rigid kit and one target output.");
                // JsonUtility omits the frame list from this dynamically compiled
                // diagnostic type. Serialize scalar DTOs and verify the saved track.
                var trackName = clip.name + "-" + (int)quality + "-private-tracks.json";
                var trackPath = Path.Combine(output, trackName);
                File.WriteAllText(trackPath, Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    record.clip, record.quality, record.sides,
                    frames = record.frames.Select(f => new { f.seconds, f.lift,
                        points = f.points.Select(p => new { p.x, p.y, p.z }).ToArray() }).ToArray()
                }), new UTF8Encoding(false));
                var saved = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(trackPath));
                var savedFrames = saved["frames"] as Newtonsoft.Json.Linq.JArray;
                if (savedFrames == null || savedFrames.Count != record.frames.Count
                    || savedFrames.Any(f => f["points"] is not Newtonsoft.Json.Linq.JArray values || values.Count != sides.Length))
                    throw new InvalidDataException("The saved private sole track is incomplete.");
                rows.Add(new { clip = clip.name, quality = (int)quality, samples = record.frames.Count, minMesh, maxHeight, maxLift, minSole,
                    maxNearestGap, maxPredictionError, foundCloak, foundRigid, targetOutputs = graph.GetOutputCount(),
                    privateTrack = new { file = trackName, sha256 = Hash(trackPath), frames = savedFrames.Count, pointsPerFrame = sides.Length },
                    supportWindows = new[] { .08f, .12f, .18f }.Select(seconds => Windows(record, seconds)).ToArray() });
            }
            finally { sole?.Dispose(); UnityEngine.Object.DestroyImmediate(mesh); }
        }
        foreach (var path in inputPaths) if (Hash(Path.Combine(root, path)) != inputs[path]) throw new InvalidOperationException("Input changed during measurement.");
        if (EditorApplication.isCompiling) throw new InvalidOperationException("Assembly changed during measurement.");
        var destination = Path.Combine(output, "summary.json");
        File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(new { state = "turn-contact-screen-not-qualified-or-installed", inputs, assemblies, rows,
            scope = "Cold equipped single-output review rig, named turn clips, manually supplied 120Hz evaluated source yaw, existing sole cache and bounded upward host projection. No motor, physics or gameplay simulation. Window test is optimistic: best fixed point per centered window within [-1mm,+8mm], >=80/120/180ms, excursion <=20mm. Changing selected points does not establish a persistent contact contract or rolling acceptance. Full-mesh finite samples are not continuous-pose proof."
        }, Newtonsoft.Json.Formatting.Indented) + "\n", new UTF8Encoding(false));
        return destination;
    }

    private static object Windows(Track track, float seconds)
    {
        var dt = track.frames[1].seconds - track.frames[0].seconds;
        var half = Mathf.CeilToInt(seconds / dt * .5f); var windows = 0; var covered = 0; var unavailable = 0;
        var worstBest = 0f; var switches = 0; var previousPoint = -1; var missingRun = 0; var maximumMissingRun = 0;
        var perSide = new int[2];
        for (var center = half; center < track.frames.Count - half; center++)
        {
            var best = float.PositiveInfinity; var bestPoint = -1; var sideBest = new[] { float.PositiveInfinity, float.PositiveInfinity };
            for (var vertex = 0; vertex < track.sides.Length; vertex++)
            {
                var anchor = track.frames[center - half].points[vertex]; var excursion = 0f; var valid = true;
                for (var frame = center - half; frame <= center + half; frame++)
                {
                    var point = track.frames[frame].points[vertex];
                    if (point.y < -.001f || point.y > .008f) { valid = false; break; }
                    excursion = Mathf.Max(excursion, Vector3.ProjectOnPlane(point - anchor, Vector3.up).magnitude);
                }
                if (!valid) continue;
                sideBest[track.sides[vertex]] = Mathf.Min(sideBest[track.sides[vertex]], excursion);
                if (excursion < best) { best = excursion; bestPoint = vertex; }
            }
            windows++;
            for (var side = 0; side < 2; side++) if (sideBest[side] <= .02f) perSide[side]++;
            if (float.IsFinite(best)) worstBest = Mathf.Max(worstBest, best); else unavailable++;
            if (best <= .02f) { covered++; missingRun = 0; if (previousPoint >= 0 && previousPoint != bestPoint) switches++; previousPoint = bestPoint; }
            else { missingRun++; maximumMissingRun = Mathf.Max(maximumMissingRun, missingRun); previousPoint = -1; }
        }
        return new { requestedSeconds = seconds, actualSeconds = 2 * half * dt, windows, covered, unavailable,
            coverage = covered / (float)windows, worstAvailableBestExcursion = worstBest, pointSwitches = switches,
            maximumUncoveredSeconds = maximumMissingRun * dt, leftCoverage = perSide[0] / (float)windows, rightCoverage = perSide[1] / (float)windows };
    }
}
