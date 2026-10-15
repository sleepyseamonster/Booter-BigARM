using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

// Author feedback: relaxed jog and a faster sprint with stronger upper-body drive.
// Original sources, physics, actions and qualification flags are preserved.
public static class CampGaitPaceTuning
{
    public const string PolicyPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset";
    public const string SprintPath = AnimationPlaygroundClipDerivatives.Folder + "/SprintArmDriveReview.anim";
    public const string Evidence = "LocomotionImplementation20261014/GaitPaceTuning20261015";
    private static void Candidate()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the stable marked offline candidate.");
    }
    private static string Hash(string path)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    public static string Prepare()
    {
        Candidate();
        if (Directory.Exists(Evidence) || File.Exists(SprintPath) || File.Exists(SprintPath + ".meta"))
            throw new IOException("Preserve existing candidates and evidence.");
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(PolicyPath);
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        if (!policy.Validate(policy.targetModel, policy.locomotion, out var error, false) || !bindings.ValidatePlayer(out error))
            throw new InvalidDataException(error);
        var clips = policy.locomotion;
        var paths = new[] { PolicyPath, AssetDatabase.GetAssetPath(clips), AssetDatabase.GetAssetPath(bindings) };
        Directory.CreateDirectory(Evidence);
        var before = paths.SelectMany(p => new[] { p, p + ".meta" }).Select(p => new { path = p, sha256 = Hash(p) }).ToArray();
        foreach (var item in before)
        {
            var backup = Path.Combine(Evidence, "Before", item.path); Directory.CreateDirectory(Path.GetDirectoryName(backup));
            File.Copy(item.path, backup, false);
        }
        var oldPolicy = Object.Instantiate(policy); var oldClips = Object.Instantiate(clips); var oldBindings = Object.Instantiate(bindings);
        var sprint = Object.Instantiate(policy.gaits[2].sourceClip); sprint.name = "Sprint arm drive review"; sprint.hideFlags = HideFlags.None;
        var source = AssetDatabase.GetAssetPath(policy.gaits[2].sourceClip); var sourceHash = Hash(source); var sourceMeta = Hash(source + ".meta");
        var changed = new List<object>();
        try
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(sprint))
            {
                var gain = binding.propertyName switch {
                    "Left Arm Front-Back" or "Right Arm Front-Back" => 1.2f,
                    "Left Arm Down-Up" or "Right Arm Down-Up" => 1.15f,
                    "Chest Front-Back" => 1.15f,
                    "UpperChest Front-Back" => 1.1f,
                    _ => 1f
                };
                if (binding.path != "" || gain == 1) continue;
                var curve = AnimationUtility.GetEditorCurve(sprint, binding); var keys = curve.keys;
                var centre = binding.propertyName.Contains("Arm") ? (keys.Min(k => k.value) + keys.Max(k => k.value)) * .5f : 0;
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i].value = centre + (keys[i].value - centre) * gain;
                    keys[i].inTangent *= gain; keys[i].outTangent *= gain;
                    if (Mathf.Abs(keys[i].value) > 1) throw new InvalidDataException("Muscle amplification exceeds its normalized range.");
                }
                curve.keys = keys; AnimationUtility.SetEditorCurve(sprint, binding, curve);
                changed.Add(new { muscle = binding.propertyName, gain, centre });
            }
            if (changed.Count != 6 || !sprint.isLooping) throw new InvalidDataException("Expected six bounded upper-body curves and the original full loop.");
            AssetDatabase.CreateAsset(sprint, SprintPath); AssetDatabase.SaveAssetIfDirty(sprint);
            var oldWalk = policy.gaits[0].nominalSpeed; var oldRun = policy.gaits[1].nominalSpeed; var oldSprint = policy.gaits[2].nominalSpeed;
            bindings.runSpeed = 2.2f; bindings.sprintSpeed = 5;
            foreach (var point in policy.secondRightDistanceAdjustment.Select((p, i) => (p, i)).ToArray())
            {
                var speed = point.p.x <= oldWalk ? point.p.x : point.p.x <= oldRun
                    ? Mathf.Lerp(oldWalk, 2.2f, Mathf.InverseLerp(oldWalk, oldRun, point.p.x))
                    : Mathf.Lerp(2.2f, 5, Mathf.InverseLerp(oldRun, oldSprint, point.p.x));
                policy.secondRightDistanceAdjustment[point.i] = new Vector2(speed, point.p.y);
            }
            policy.gaits[1].nominalSpeed = 2.2f; policy.gaits[2].nominalSpeed = 5; policy.gaits[2].sourceClip = sprint;
            // Rates describe the requested wider speed spread. Hard policy ceilings remain unchanged.
            policy.gaits[1].blendMaximumRate = 1.4f;
            policy.gaits[2].pureMaximumRate = 1.25f; policy.gaits[2].blendMinimumRate = .45f; policy.gaits[2].blendMaximumRate = 1.25f;
            var serialized = new SerializedObject(clips); var entries = serialized.FindProperty("clips");
            entries.GetArrayElementAtIndex(2).FindPropertyRelative("nominalForwardSpeed").floatValue = 2.2f;
            entries.GetArrayElementAtIndex(3).FindPropertyRelative("nominalForwardSpeed").floatValue = 5;
            entries.GetArrayElementAtIndex(3).FindPropertyRelative("clip").objectReferenceValue = sprint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!policy.Validate(policy.targetModel, clips, out error, false) || !bindings.ValidatePlayer(out error)) throw new InvalidDataException(error);
            if (policy.qualified || policy.terrainQualified) throw new InvalidDataException("Experimental review must remain unqualified.");
            foreach (var asset in new Object[] { clips, policy, bindings }) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
            if (Hash(source) != sourceHash || Hash(source + ".meta") != sourceMeta) throw new InvalidDataException("Original animation changed.");
            var after = paths.Append(SprintPath).SelectMany(p => new[] { p, p + ".meta" }).Select(p => new { path = p, sha256 = Hash(p) }).ToArray();
            File.WriteAllText(Path.Combine(Evidence, "recipe.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new {
                state = "candidate-prepared-not-author-installed", before, after, source, sourceHash, sourceMeta, changed,
                speeds = new[] { 1.4f, 2.2f, 5f }, lowerBody = "Original leg, foot, root and timing curves retained",
                mixedDistance = "Existing walk/jog distance calibration remapped by blend weight; measured distances unchanged",
                policy.qualified, policy.terrainQualified }, Newtonsoft.Json.Formatting.Indented));
            return Path.Combine(Evidence, "recipe.json");
        }
        catch
        {
            EditorUtility.CopySerialized(oldPolicy, policy); EditorUtility.CopySerialized(oldClips, clips); EditorUtility.CopySerialized(oldBindings, bindings);
            foreach (var asset in new Object[] { clips, policy, bindings }) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
            throw;
        }
        finally { Object.DestroyImmediate(oldPolicy); Object.DestroyImmediate(oldClips); Object.DestroyImmediate(oldBindings); if (!AssetDatabase.Contains(sprint)) Object.DestroyImmediate(sprint); }
    }

    public static string Measure()
    {
        Candidate();
        var output = Path.Combine(Evidence, "pose-review.json"); if (File.Exists(output)) throw new IOException("Preserve prior measurements.");
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(PolicyPath);
        if (!policy.Validate(policy.targetModel, policy.locomotion, out var error, false)) throw new InvalidDataException(error);
        var model = AssetDatabase.GetAssetPath(policy.targetModel);
        var recipe = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(Evidence, "recipe.json")));
        var original = AnimationPlaygroundMotionSampler.Measure(model, (string)recipe["source"], .08989948f, new Vector3(0, -.90440583f, 0), 120);
        var amplified = AnimationPlaygroundMotionSampler.Measure(model, SprintPath, .08989948f, new Vector3(0, -.90440583f, 0), 120);
        var footDifference = 0f; var relativeFootDifference = 0f;
        for (var i = 0; i < original.frames.Length; i++)
        {
            footDifference = Mathf.Max(footDifference, Vector3.Distance(original.frames[i].leftFoot, amplified.frames[i].leftFoot),
                Vector3.Distance(original.frames[i].rightFoot, amplified.frames[i].rightFoot));
            relativeFootDifference = Mathf.Max(relativeFootDifference,
                Vector3.Distance(original.frames[i].leftFoot - original.frames[i].hips, amplified.frames[i].leftFoot - amplified.frames[i].hips),
                Vector3.Distance(original.frames[i].rightFoot - original.frames[i].hips, amplified.frames[i].rightFoot - amplified.frames[i].hips));
        }
        // Humanoid retargeting also changes the feet. The sprint's contacts, stride,
        // time map and height are remeasured by ApplyCalibration before this review.
        if (!File.Exists(Path.Combine(Evidence, "calibration-applied.json"))) throw new InvalidDataException("Recalibrate the authored source first.");
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t => t != null);
        var rows = new List<object>();
        foreach (var quality in new[] { SkinWeights.OneBone, SkinWeights.TwoBones, SkinWeights.FourBones })
        {
            using var fixture = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { true, quality }, null);
            var driver = (TopDown3DPlayerAnimationDriver)type.GetField("Driver").GetValue(fixture);
            var graph = (PlayableGraph)type.GetField("Graph").GetValue(fixture);
            var body = (Rigidbody)type.GetField("Player").GetValue(fixture); var floor = body.position.y - .91f;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", flags).GetValue(driver);
            var prepare = (Action)Delegate.CreateDelegate(typeof(Action), driver, typeof(TopDown3DPlayerAnimationDriver).GetMethod("PrepareForwardGraphUpdate", flags));
            var pose = (Action<float, double, float>)Delegate.CreateDelegate(typeof(Action<float, double, float>), driver, typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardFrame", flags));
            var late = (Action)Delegate.CreateDelegate(typeof(Action), driver, typeof(TopDown3DPlayerAnimationDriver).GetMethod("LateUpdate", flags));
            using var soles = new AnimationPlaygroundMotionSampler.SoleProbe(visual,
                driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot), driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot));
            foreach (var speed in new[] { 0f, 1.4f, 1.6f, 1.8f, 2f, 2.2f, 2.48f, 2.9f, 3.6f, 4.3f, 4.72f, 5f })
            {
                var minimum = float.PositiveInfinity; var height = float.NegativeInfinity;
                for (var frame = 0; frame <= 128; frame++)
                {
                    prepare(); pose(speed, frame / 128d, 1 / 60f); late();
                    soles.Measure(out var left, out var right, out var bodyMinimum, out var bodyMaximum);
                    minimum = Mathf.Min(minimum, left.y - floor, right.y - floor, bodyMinimum.y - floor); height = Mathf.Max(height, bodyMaximum.y - floor);
                }
                if (minimum < -.001f || height > 1.82f || graph.GetOutputCount() != 1 || !driver.LaboratoryForwardReviewActive)
                    throw new InvalidDataException("Tuned review exceeds the existing floor/height/output envelope at " + speed + ": " + minimum + "/" + height);
                rows.Add(new { quality = quality.ToString(), speed, poses = 129, minimum, height, outputs = graph.GetOutputCount() });
            }
        }
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new {
            state = "equipped-flat-review-envelope-passed-not-full-motion-qualification", rows,
            maximumNativeFootDifference = footDifference, maximumFootRelativeToHipsDifference = relativeFootDifference,
            scope = "One manual output; actual driver poses and equipped mesh. No Play Mode, input, physics simulation or slope acceptance."
        }, Newtonsoft.Json.Formatting.Indented));
        return output;
    }

    public static string ApplyCalibration()
    {
        Candidate();
        var output = Path.Combine(Evidence, "calibration-applied.json"); if (File.Exists(output)) throw new IOException("Preserve prior calibration.");
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(PolicyPath);
        var report = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(Evidence, "sprint-calibration.json")));
        var measured = report["gaits"][0]; var gait = policy.gaits[2];
        if ((string)measured["source"] != SprintPath || (float)measured["nominalSpeed"] != 5) throw new InvalidDataException("Unexpected source recipe.");
        gait.metresPerCycle = (float)measured["metresPerCycle"]; gait.nativeContacts = measured["nativeContacts"].ToObject<float[]>();
        gait.phaseMap = ((Newtonsoft.Json.Linq.JArray)measured["sourceTimeMap"]).Select(p => new AnimationPlaygroundNativePhase((float)p[0], (float)p[1])).ToArray();
        gait.signedHeight = new AnimationCurve(((Newtonsoft.Json.Linq.JArray)measured["correction"]).Select(p => new Keyframe((float)p[0], (float)p[1])).ToArray());
        for (var i = 0; i < gait.signedHeight.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(gait.signedHeight, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(gait.signedHeight, i, AnimationUtility.TangentMode.Linear);
        }
        var serialized = new SerializedObject(policy.locomotion); var entry = serialized.FindProperty("clips").GetArrayElementAtIndex(3);
        foreach (var side in new[] { "leftContactPhases", "rightContactPhases" })
        {
            var contacts = entry.FindPropertyRelative(side); contacts.arraySize = 2;
            contacts.GetArrayElementAtIndex(0).floatValue = gait.nativeContacts[side[0] == 'l' ? 1 : 0];
            contacts.GetArrayElementAtIndex(1).floatValue = gait.nativeContacts[side[0] == 'l' ? 3 : 2];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (!policy.Validate(policy.targetModel, policy.locomotion, out var error, false)) throw new InvalidDataException(error);
        var rates = new List<object>();
        for (var i = 0; i < 3; i++)
        {
            var source = policy.gaits[i]; var minimum = float.PositiveInfinity; var maximum = 0f;
            for (var step = 18; step <= 500; step++)
            {
                var speed = step / 100f; if (policy.Weights(speed)[i + 1] < .1f) continue;
                for (var k = 1; k < source.phaseMap.Length; k++)
                {
                    var a = source.phaseMap[k - 1]; var b = source.phaseMap[k];
                    var rate = (float)((b.native - a.native) / (b.semantic - a.semantic) * source.sourceClip.length * speed / policy.DistanceDerivative(speed, (a.semantic + b.semantic) * .5));
                    minimum = Mathf.Min(minimum, rate); maximum = Mathf.Max(maximum, rate);
                    if (rate < source.blendMinimumRate - .001f || rate > source.blendMaximumRate + .001f) throw new InvalidDataException("Dense contributor rate check failed.");
                }
            }
            rates.Add(new { role = source.role.ToString(), minimum, maximum });
        }
        EditorUtility.SetDirty(policy); AssetDatabase.SaveAssetIfDirty(policy);
        EditorUtility.SetDirty(policy.locomotion); AssetDatabase.SaveAssetIfDirty(policy.locomotion);
        var bindings = AnimationPlaygroundPlayerAuthoring.BindingPath;
        var paths = new[] { PolicyPath, AssetDatabase.GetAssetPath(policy.locomotion), bindings, SprintPath };
        var after = paths.SelectMany(p => new[] { p, p + ".meta" }).Select(p => new { path = p, sha256 = Hash(p) }).ToArray();
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new {
            state = "authored-sprint-recalibrated-not-fully-qualified", after, rates,
            calibrationSha256 = Hash(Path.Combine(Evidence, "sprint-calibration.json")),
            gait.nominalSpeed, gait.metresPerCycle, gait.nativeContacts,
            policy.qualified, policy.terrainQualified
        }, Newtonsoft.Json.Formatting.Indented));
        return output;
    }

    public static string InstallAuthor(string archive)
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || !Directory.Exists(".git") || UnityEngine.SceneManagement.SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Use the author checkout in stable Edit Mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(0);
        if (!scene.isLoaded || scene.path != TopDown3DAnimationPlaygroundSession.ScenePath)
            throw new InvalidOperationException("Keep the author's existing Camp Loop scene open.");
        var sceneHash = Hash(scene.path); var dirty = scene.isDirty;
        var manifest = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(archive, "manifest.json")));
        var before = (Newtonsoft.Json.Linq.JArray)manifest["before"]; var after = (Newtonsoft.Json.Linq.JArray)manifest["after"];
        var expected = new HashSet<string> { PolicyPath, AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(PolicyPath).locomotion), AnimationPlaygroundPlayerAuthoring.BindingPath, SprintPath };
        var expectedAfter = new HashSet<string>(expected.SelectMany(p => new[] { p, p + ".meta" }));
        var expectedBefore = new HashSet<string>(expected.Where(p => p != SprintPath).SelectMany(p => new[] { p, p + ".meta" }));
        if (after.Count != 8 || !expectedAfter.SetEquals(after.Select(p => (string)p["path"]))
            || before.Count != 6 || !expectedBefore.SetEquals(before.Select(p => (string)p["path"])))
            throw new InvalidDataException("Unexpected private asset manifest.");
        foreach (var item in before)
        {
            var path = (string)item["path"];
            if (Hash(path) != (string)item["sha256"] || Hash(Path.Combine(archive, "Before", path)) != (string)item["sha256"])
                throw new InvalidDataException("Author calibration changed since candidate preparation: " + path);
        }
        foreach (var item in after)
            if (Hash(Path.Combine(archive, "After", (string)item["path"])) != (string)item["sha256"])
                throw new InvalidDataException("Private recovery payload changed.");
        if (File.Exists(SprintPath) || File.Exists(SprintPath + ".meta")) throw new IOException("Preserve an existing authored source and GUID.");
        try
        {
            foreach (var item in after)
            {
                var path = (string)item["path"]; Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.Copy(Path.Combine(archive, "After", path), path, true);
            }
            foreach (var path in new[] { SprintPath, expected.Single(p => p != PolicyPath && p != SprintPath && p != AnimationPlaygroundPlayerAuthoring.BindingPath), PolicyPath, AnimationPlaygroundPlayerAuthoring.BindingPath })
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(PolicyPath);
            var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
            var session = TopDown3DAnimationPlaygroundSession.Find(scene);
            if (session == null || session.Bindings != bindings || session.ExperimentalForwardReview != policy
                || !bindings.ValidatePlayer(out var error) || !policy.Validate(policy.targetModel, policy.locomotion, out error, false)
                || bindings.runSpeed != 2.2f || bindings.sprintSpeed != 5 || policy.gaits[2].sourceClip != AnimationPlaygroundClipDerivatives.LoadClip(SprintPath)
                || policy.qualified || policy.terrainQualified || scene.isDirty != dirty || Hash(scene.path) != sceneHash)
                throw new InvalidDataException("Author import/readiness or unchanged-scene checks failed.");
            foreach (var item in after)
                if (Hash((string)item["path"]) != (string)item["sha256"]) throw new InvalidDataException("Imported bytes differ from verified candidate.");
            var output = Path.GetFullPath("Logs/gait-pace-author-install.json");
            File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new {
                state = "installed-experimental-flat-deck-review-awaiting-author-play", speeds = new[] { bindings.slowWalkSpeed, bindings.runSpeed, bindings.sprintSpeed },
                scene = scene.path, sceneDirty = dirty, sceneHash, policy.qualified, policy.terrainQualified,
                animation = policy.gaits[2].sourceClip.name, sources = after,
                sceneSaved = false, playModeEntered = false, foregroundActivated = false
            }, Newtonsoft.Json.Formatting.Indented));
            return output;
        }
        catch
        {
            // Restore only the three changed private pairs; keep the new source preserved and unbound.
            foreach (var item in before) File.Copy(Path.Combine(archive, "Before", (string)item["path"]), (string)item["path"], true);
            foreach (var path in expected.Where(p => p != SprintPath)) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            throw;
        }
    }

    public static string TryJogPace()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || Application.isBatchMode) throw new InvalidOperationException("Use stable author-owned Play Mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var session = TopDown3DAnimationPlaygroundSession.Find(scene);
        if (session == null || !session.PreviewReady || session.IsBusy) throw new InvalidOperationException("The existing player must be ready and standing.");
        var driver = session.Player.GetComponent<TopDown3DPlayerAnimationDriver>();
        if (!driver.LaboratoryForwardReviewActive) throw new InvalidOperationException("Review jogging on AP02 first.");
        var field = typeof(TopDown3DPlayerAnimationDriver).GetField("laboratoryForwardProfile", BindingFlags.Instance | BindingFlags.NonPublic);
        var original = (AnimationPlaygroundForwardLocomotionProfile)field.GetValue(driver);
        if (original.gaits[1].nominalSpeed != 2.2f || original.gaits[2].nominalSpeed != 5) throw new InvalidOperationException("Unexpected current tuning; preserve it.");
        var trial = Object.Instantiate(original); trial.name = "Temporary 2.5mps jog review"; trial.hideFlags = HideFlags.DontSave;
        foreach (var point in trial.secondRightDistanceAdjustment.Select((p, i) => (p, i)).ToArray())
        {
            var speed = point.p.x <= 1.4f ? point.p.x : point.p.x <= 2.2f
                ? Mathf.Lerp(1.4f, 2.5f, Mathf.InverseLerp(1.4f, 2.2f, point.p.x))
                : Mathf.Lerp(2.5f, 5, Mathf.InverseLerp(2.2f, 5, point.p.x));
            trial.secondRightDistanceAdjustment[point.i] = new Vector2(speed, point.p.y);
        }
        trial.gaits[1].nominalSpeed = 2.5f;
        if (!trial.Validate(trial.targetModel, trial.locomotion, out var error, false)) { Object.DestroyImmediate(trial); throw new InvalidDataException(error); }
        var before = session.Player.Position;
        field.SetValue(driver, trial); session.Player.ConfigureLaboratorySpeeds(session.Coordinator, 1.4f, 2.5f, 5);
        AssemblyReloadEvents.AssemblyReloadCallback cleanup = null; Action<PlayModeStateChange> playChange = null;
        cleanup = () => {
            EditorApplication.playModeStateChanged -= playChange; AssemblyReloadEvents.beforeAssemblyReload -= cleanup;
            if (driver != null && ReferenceEquals(field.GetValue(driver), trial)) field.SetValue(driver, original);
            if (session != null && session.Coordinator != null)
            { try { session.Player.ConfigureLaboratorySpeeds(session.Coordinator, session.Bindings.slowWalkSpeed, session.Bindings.runSpeed, session.Bindings.sprintSpeed); } catch (ArgumentException) { } }
            if (trial != null) Object.DestroyImmediate(trial);
        };
        playChange = state => { if (state == PlayModeStateChange.ExitingPlayMode) cleanup(); };
        EditorApplication.playModeStateChanged += playChange; AssemblyReloadEvents.beforeAssemblyReload += cleanup;
        var output = Path.GetFullPath("Logs/gait-pace-jog-2p5-trial.json");
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new {
            state = "temporary-current-play-session-only", speeds = new[] { 1.4f, 2.5f, 5f },
            originalBindingSpeeds = new[] { session.Bindings.slowWalkSpeed, session.Bindings.runSpeed, session.Bindings.sprintSpeed },
            preservesSprintSource = trial.gaits[2].sourceClip == original.gaits[2].sourceClip,
            preservesPlayerPosition = session.Player.Position == before, assetsSaved = false,
            cleanup = "Restore original profile and binding speeds; destroy transient clone on Play exit or assembly reload"
        }, Newtonsoft.Json.Formatting.Indented));
        return output;
    }

    public static string PrepareFasterJog()
    {
        Candidate();
        var directory = Evidence + "/Jog3mps"; if (Directory.Exists(directory)) throw new IOException("Preserve previous tuning.");
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(PolicyPath);
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        if (bindings.runSpeed != 2.2f || policy.gaits[1].nominalSpeed != 2.2f || bindings.sprintSpeed != 5) throw new InvalidOperationException("Unexpected prior tuning.");
        var paths = new[] { PolicyPath, AssetDatabase.GetAssetPath(policy.locomotion), AnimationPlaygroundPlayerAuthoring.BindingPath };
        var before = paths.SelectMany(p => new[] { p, p + ".meta" }).Select(p => new { path = p, sha256 = Hash(p) }).ToArray();
        foreach (var item in before)
        {
            var target = Path.Combine(directory, "Before", item.path); Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(item.path, target, false);
        }
        foreach (var point in policy.secondRightDistanceAdjustment.Select((p, i) => (p, i)).ToArray())
        {
            var speed = point.p.x <= 1.4f ? point.p.x : point.p.x <= 2.2f
                ? Mathf.Lerp(1.4f, 2.7f, Mathf.InverseLerp(1.4f, 2.2f, point.p.x)) : point.p.x;
            policy.secondRightDistanceAdjustment[point.i] = new Vector2(speed, point.p.y);
        }
        policy.walkToJogBlendEndSpeed = 2.7f; policy.gaits[1].nominalSpeed = 3; bindings.runSpeed = 3;
        var serialized = new SerializedObject(policy.locomotion); serialized.FindProperty("clips").GetArrayElementAtIndex(2).FindPropertyRelative("nominalForwardSpeed").floatValue = 3;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (!policy.Validate(policy.targetModel, policy.locomotion, out var error, false) || !bindings.ValidatePlayer(out error)) throw new InvalidDataException(error);
        foreach (var asset in new Object[] { policy, policy.locomotion, bindings }) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
        var after = paths.SelectMany(p => new[] { p, p + ".meta" }).Select(p => new { path = p, sha256 = Hash(p) }).ToArray();
        foreach (var item in after)
        {
            var target = Path.Combine(directory, "After", item.path); Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(item.path, target, false);
        }
        var output = Path.Combine(directory, "manifest.json");
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { before, after,
            speeds = new[] { 1.4f, 3f, 5f }, policy.walkToJogBlendEndSpeed,
            sprintSha256 = Hash(SprintPath), sprintMetaSha256 = Hash(SprintPath + ".meta"),
            state = "prepared-faster-jog-keeps-accepted-sprint" }, Newtonsoft.Json.Formatting.Indented));
        return output;
    }

    public static string InstallFasterJog(string archive)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Application.isBatchMode || !Directory.Exists(".git"))
            throw new InvalidOperationException("Use stable author Edit Mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); var session = TopDown3DAnimationPlaygroundSession.Find(scene);
        if (session == null || scene.path != TopDown3DAnimationPlaygroundSession.ScenePath) throw new InvalidOperationException("Keep Camp Loop open.");
        var sceneHash = Hash(scene.path); var dirty = scene.isDirty;
        var manifest = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(archive, "manifest.json")));
        var before = (Newtonsoft.Json.Linq.JArray)manifest["before"]; var after = (Newtonsoft.Json.Linq.JArray)manifest["after"];
        var expected = new HashSet<string>(new[] { PolicyPath, AssetDatabase.GetAssetPath(session.ExperimentalForwardReview.locomotion), AnimationPlaygroundPlayerAuthoring.BindingPath }.SelectMany(p => new[] { p, p + ".meta" }));
        if (before.Count != 6 || after.Count != 6 || !expected.SetEquals(before.Select(p => (string)p["path"])) || !expected.SetEquals(after.Select(p => (string)p["path"])))
            throw new InvalidDataException("Unexpected pace-only manifest.");
        if (Hash(SprintPath) != (string)manifest["sprintSha256"] || Hash(SprintPath + ".meta") != (string)manifest["sprintMetaSha256"])
            throw new InvalidDataException("Preserve the accepted sprint.");
        foreach (var item in before)
            if (Hash((string)item["path"]) != (string)item["sha256"] || Hash(Path.Combine(archive, "Before", (string)item["path"])) != (string)item["sha256"])
                throw new InvalidDataException("Prior author calibration differs.");
        foreach (var item in after)
            if (Hash(Path.Combine(archive, "After", (string)item["path"])) != (string)item["sha256"]) throw new InvalidDataException("Recovery bytes differ.");
        try
        {
            foreach (var item in after) File.Copy(Path.Combine(archive, "After", (string)item["path"]), (string)item["path"], true);
            foreach (var path in expected.Where(p => !p.EndsWith(".meta"))) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var policy = session.ExperimentalForwardReview; var bindings = session.Bindings;
            if (!policy.Validate(policy.targetModel, policy.locomotion, out var error, false) || !bindings.ValidatePlayer(out error)
                || policy.gaits[1].nominalSpeed != 3 || bindings.runSpeed != 3 || bindings.sprintSpeed != 5 || policy.walkToJogBlendEndSpeed != 2.7f
                || scene.isDirty != dirty || Hash(scene.path) != sceneHash || policy.qualified || policy.terrainQualified)
                throw new InvalidDataException("Pace import or unchanged-scene validation failed.");
            foreach (var item in after) if (Hash((string)item["path"]) != (string)item["sha256"]) throw new InvalidDataException("Imported bytes differ.");
            var output = Path.GetFullPath("Logs/gait-pace-jog-3mps-installed.json");
            File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { state = "persistent-faster-jog-installed-awaiting-author-comparison", speeds = new[] { bindings.slowWalkSpeed, bindings.runSpeed, bindings.sprintSpeed },
                policy.walkToJogBlendEndSpeed, sceneHash, sceneDirty = dirty, acceptedSprintUnchanged = true,
                sceneSaved = false, playModeEntered = false, sources = after }, Newtonsoft.Json.Formatting.Indented));
            return output;
        }
        catch
        {
            foreach (var item in before) File.Copy(Path.Combine(archive, "Before", (string)item["path"]), (string)item["path"], true);
            foreach (var path in expected.Where(p => !p.EndsWith(".meta"))) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            throw;
        }
    }
}
