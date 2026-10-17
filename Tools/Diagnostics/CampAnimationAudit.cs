using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Reflection;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

// Read-only asset/loaded-scene inventory. Pose and scene-opening checks belong in the offline candidate.
public static class CampAnimationAudit
{
    static void Offline(string receipt)
    {
        if (!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".animation-playground-validation-project")
            || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || File.Exists(receipt))
            throw new InvalidOperationException("Use a fresh receipt in the marked offline candidate.");
    }
    static string Hash(string path)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    public static string Inventory(string receipt, string candidate)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating
            || File.Exists(receipt)) throw new InvalidOperationException("Stable Edit Mode and a fresh receipt are required.");
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var binding = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        if (binding == null) throw new InvalidDataException("Restore the private player binding.");
        var valid = binding.ValidatePlayer(out var reason);
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/_Project/", StringComparison.Ordinal)).ToArray();
        var clips = paths.Where(p => p.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .Select(c => new { path = p, c.name, c.length, c.frameRate, c.isHumanMotion, c.isLooping,
                    legacy = c.legacy, events = AnimationUtility.GetAnimationEvents(c).Select(e => new { e.time, e.functionName }).ToArray(),
                    sha256 = Hash(p) })).ToArray();
        var forward = binding.forwardReviewOverride;
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(i => {
            var s = SceneManager.GetSceneAt(i);
            return new { s.path, s.isDirty, diskHash = File.Exists(s.path) ? Hash(s.path) : null,
                roots = s.GetRootGameObjects().Select(r => new { r.name, id = r.GetInstanceID() }).ToArray() };
        }).ToArray();
        var dependencies = AssetDatabase.GetDependencies(new[] {
            "Assets/_Project/Scenes/Reference/CampLoopTesting.unity",
            AssetDatabase.GetAssetPath(binding) }, true)
            .Concat(paths.Where(p => p.StartsWith("Assets/_Project/Scripts/") || p.StartsWith("Assets/_Project/Tests/")
                || p.StartsWith("Assets/_Project/Art/Animations/") || p.StartsWith("Assets/_Project/Art/Characters/")
                || p.StartsWith("Assets/_Project/Settings/AnimationPlayground/")))
            .Where(p => p.StartsWith("Assets/") && File.Exists(p))
            .SelectMany(p => File.Exists(p + ".meta") ? new[] { p, p + ".meta" } : new[] { p })
            .Distinct().OrderBy(p => p).ToArray();
        var files = dependencies.Select(p => {
            var sourceHash = Hash(p); var target = Path.Combine(candidate, p);
            var before = File.Exists(target) ? Hash(target) : null;
            return new { path = p, sha256 = sourceHash, candidateBefore = before, transfer = sourceHash != before };
        }).ToArray();
        var result = new { scope = "Read-only imported clip and loaded scene audit; no Play Mode or physics simulation", valid, reason,
            scenes, clips, clipCount = clips.Length,
            motions = binding.motions.Select(m => new { m.sourceId, readiness = m.readiness.ToString(), m.CanPreview, m.CanPerform,
                m.restrictedReference, m.loop, clip = AssetDatabase.GetAssetPath(m.clip), actualLoop = m.clip != null && m.clip.isLooping }).ToArray(),
            gaits = forward.gaits.Select(g => new { source = AssetDatabase.GetAssetPath(g.sourceClip), g.nominalSpeed, g.metresPerCycle,
                seconds = g.sourceClip.length, nativeRate = g.nominalSpeed * g.sourceClip.length / g.metresPerCycle }).ToArray(),
            profiles = new { crouch = binding.crouch != null, airborne = binding.airborne != null,
                lowMotion = binding.lowMotion != null, dodge = binding.dodge != null, movingTurns = binding.movingTurns != null },
            hydration = new { state = "read-only-current-saved-scene-hydration-plan", repository = root, candidate, files } };
        File.WriteAllText(receipt, JsonConvert.SerializeObject(result, Formatting.Indented));
        return receipt;
    }

    public static string SampleAll(string inventory, string receipt)
    {
        Offline(receipt);
        var input = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(inventory));
        var binding = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject actor = null; PlayableGraph graph = default; var rows = new List<object>();
        try
        {
            actor = PrefabUtility.InstantiatePrefab(binding.targetModel, scene) as GameObject;
            actor.transform.localScale = Vector3.one * binding.visualScale;
            actor.transform.position = binding.visualOffset;
            var animator = actor.GetComponentInChildren<Animator>(); animator.avatar = binding.targetAvatar;
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var bones = actor.GetComponentsInChildren<Transform>(true);
            graph = PlayableGraph.Create("Offline complete animation audit"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, "Audit", animator); graph.Play();
            foreach (var item in input["clips"])
            {
                var path = (string)item["path"]; var name = (string)item["name"];
                if (Hash(path) != (string)item["sha256"]) throw new InvalidDataException("Source changed since inventory: " + path);
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => c.name == name);
                var playable = AnimationClipPlayable.Create(graph, clip); playable.SetSpeed(0);
                playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); output.SetSourcePlayable(playable);
                var finite = true; var maximumBoneStep = 0f; Vector3[] previous = null;
                var samples = Math.Max(33, Mathf.CeilToInt(clip.length * 30) + 1);
                for (var i = 0; i < samples; i++)
                {
                    playable.SetTime(clip.length * i / (samples - 1)); graph.Evaluate(0);
                    var positions = bones.Select(b => b.position).ToArray();
                    for (var k = 0; k < bones.Length; k++)
                    {
                        var p = positions[k]; var q = bones[k].localRotation;
                        finite &= float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z)
                            && float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) && float.IsFinite(q.w)
                            && Quaternion.Dot(q, q) > .000001f;
                        if (previous != null) maximumBoneStep = Mathf.Max(maximumBoneStep, Vector3.Distance(previous[k], p));
                    }
                    previous = positions;
                }
                rows.Add(new { path, name, samples, finite, maximumBoneStep, clip.isLooping,
                    note = "Raw retargeted pose screen; action arcs and source floor height are intentional until fitted by their runtime profile." });
                graph.DestroyPlayable(playable);
            }
            File.WriteAllText(receipt, JsonConvert.SerializeObject(new { clips = rows.Count,
                scope = "30Hz minimum whole-range finite Humanoid retarget screen, no physics or Play Mode; no contact/feel acceptance", rows }, Formatting.Indented));
            return receipt;
        }
        finally
        {
            if (graph.IsValid()) graph.Destroy();
            if (actor != null) UnityEngine.Object.DestroyImmediate(actor);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    public static string ApplySprintTrial(string recipe, string receipt)
    {
        Offline(receipt);
        const string path = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset";
        var input = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(recipe));
        var profile = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(path);
        var gait = profile.gaits[4]; var trial = input["selected"];
        if (Math.Abs(gait.metresPerCycle - (double)input["baseline"]["distance"]) > .00001
            || (double)trial["maximumExcursion"] >= (double)input["baseline"]["maximumExcursion"]
            || (double)trial["maximumRate"] > gait.pureMaximumRate)
            throw new InvalidDataException("The trial must improve the sealed baseline within its existing rate budget.");
        var before = File.ReadAllBytes(path); var metadata = Hash(path + ".meta");
        var maps = gait.phaseMap; var distance = gait.metresPerCycle;
        var backup = receipt + ".before.asset"; File.WriteAllBytes(backup, before);
        try
        {
            gait.metresPerCycle = (float)trial["distance"];
            gait.phaseMap = trial["mapping"].Select(p => new AnimationPlaygroundNativePhase((float)p[0], (float)p[1])).ToArray();
            if (!profile.Validate(profile.targetModel, profile.locomotion, out var reason, false)) throw new InvalidDataException(reason);
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
            if (Hash(path + ".meta") != metadata) throw new InvalidDataException("Metadata changed.");
            File.WriteAllText(receipt, JsonConvert.SerializeObject(new { path, before = Hash(backup), after = Hash(path), metadata,
                scope = "Candidate-only sprint phase-map trial; original clips, contacts, height curve and other gaits retained" }, Formatting.Indented));
            return receipt;
        }
        catch
        {
            gait.phaseMap = maps; gait.metresPerCycle = distance;
            File.WriteAllBytes(path, before); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            throw;
        }
    }

    public static string SprintContacts(string receipt)
    {
        Offline(receipt);
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(
            "BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t => t != null);
        using var fixture = (IDisposable)Activator.CreateInstance(type, new object[] { true, SkinWeights.FourBones,
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset" });
        var driver = (TopDown3DPlayerAnimationDriver)type.GetField("Driver").GetValue(fixture);
        var policy = (AnimationPlaygroundForwardLocomotionProfile)type.GetField("Policy").GetValue(fixture);
        var body = (Rigidbody)type.GetField("Player").GetValue(fixture);
        var visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(driver);
        var projection = policy.footProjection;
        var bones = projection.bonePaths.Select(p => p.Length == 0 ? visual.transform : visual.transform.Find(p)).ToArray();
        var sides = projection.vertices.Select(v => projection.bonePaths[v.points[0].bone].Contains(".L") ? 0 : 1).ToArray();
        Vector3 Point(AnimationPlaygroundFootVertex v)
        { var p = Vector3.zero; foreach (var b in v.points) p += bones[b.bone].TransformPoint(b.position) * b.weight; return p; }
        var origin = body.position; var floor = origin.y - .91f; var rows = new List<object>();
        foreach (var speed in new[] { 4.9f, 5.2f, 5.5f, 5.8f, 6.15f })
        for (var plant = 0; plant < 4; plant++)
        {
            var centerPhase = 1 + plant * .25; var center = policy.DistanceAtCycles(speed, centerPhase);
            var points = new List<Vector3[]>();
            for (var frame = 0; frame < 13; frame++)
            {
                var distance = (frame / 12d - .5) * .0334 * speed;
                var phase = policy.AdvanceCycles(speed, centerPhase, distance);
                body.position = origin + Vector3.forward * (float)(policy.DistanceAtCycles(speed, phase) - center);
                body.transform.position = body.position;
                type.GetMethod("Frame").Invoke(fixture, new object[] { speed, phase });
                points.Add(projection.vertices.Select(Point).ToArray());
            }
            var side = plant % 2 == 0 ? 1 : 0; var best = float.PositiveInfinity; var bestGap = float.PositiveInfinity;
            for (var vertex = 0; vertex < projection.vertices.Length; vertex++)
            {
                if (sides[vertex] != side) continue;
                var gap = points.Max(p => p[vertex].y - floor); if (gap > .008f) continue;
                var first = points[0][vertex]; var drift = points.Max(p => Vector2.Distance(
                    new Vector2(first.x, first.z), new Vector2(p[vertex].x, p[vertex].z)));
                if (drift < best) { best = drift; bestGap = gap; }
            }
            rows.Add(new { speed, plant, excursion = best, gap = bestGap, supported = float.IsFinite(best), passes20mm = best <= .02f });
        }
        File.WriteAllText(receipt, JsonConvert.SerializeObject(new { scope = "Actual equipped graph on virtual flat paths, fixed sole identities over 33.4ms; no physics or Play Mode", rows }, Formatting.Indented));
        return receipt;
    }
}
