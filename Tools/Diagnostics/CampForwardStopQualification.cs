using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class CampForwardStopQualification
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private sealed class Row
    {
        public float initialSpeed, phase, minimumSole = float.PositiveInfinity, minimumBody = float.PositiveInfinity;
        public float maximumHeight, maximumLift, minimumLift, maximumAnkleFrameDelta, finalPoseSpeed;
        public float maximumRepeatHeightDifference, maximumRepeatAnkleDifference;
        public int renderRate, samples, footEffects;
        public bool interrupted;
    }
    public static string Run() => RunMatrix(false, "ForwardStopsV4");
    public static string RunNativeIdle() => RunMatrix(true, "ForwardStopsNativeIdleV3");
    public static string RunDeterminism() => RunMatrix(false, "ForwardStopDeterminismV3", true);
    public static string RunWorstRestart() => RunMatrix(false, "ForwardWorstRestartV4", false, true);
    private static string RunMatrix(bool nativeIdle, string outputName, bool determinism = false, bool worst = false)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate.");
        var folder = Path.Combine(root, "LocomotionImplementation20261014", outputName);
        if (Directory.Exists(folder)) throw new IOException("Preserve prior stop evidence.");
        Directory.CreateDirectory(folder);
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
        var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
        var preview = EditorSceneManager.NewPreviewScene(); GameObject actor = null;
        var bindings = Object.Instantiate(TopDown3DAnimationPlaygroundSession.Find(scene).Bindings);
        TopDown3DLocomotionClipProfile idleVariant = null;
        var quality = QualitySettings.skinWeights;
        try
        {
            QualitySettings.skinWeights = SkinWeights.FourBones;
            bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = null;
            if (nativeIdle)
            {
                idleVariant = Object.Instantiate(policy.locomotion);
                var original = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(
                    "Assets/_Project/Settings/Player/TopDown3DLocomotionClipProfile.asset");
                var serialized = new SerializedObject(idleVariant);
                var entry = serialized.FindProperty("clips").GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("clip").objectReferenceValue = original.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip;
                entry.FindPropertyRelative("visualHeightCorrection").animationCurveValue = AnimationCurve.Linear(0, 0, 1, 0);
                serialized.ApplyModifiedPropertiesWithoutUndo(); bindings.locomotion = idleVariant;
            }
            actor = new GameObject("Offline stop envelope"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, preview);
            actor.AddComponent<TopDown3DPlayerMotor>(); var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
            EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
            driver.ConfigureLaboratory(bindings);
            void Call(string method) => typeof(TopDown3DPlayerAnimationDriver).GetMethod(method, Private).Invoke(driver, null);
            Call("Awake"); actor.SetActive(true);
            if (!driver.HumanoidAnimator.isActiveAndEnabled) throw new InvalidDataException("The pose probe requires an active Humanoid Animator.");
            typeof(TopDown3DPlayerAnimationDriver).GetField("laboratoryForwardProfile", Private).SetValue(driver, policy);
            var render = (Action<float, double, float>)Delegate.CreateDelegate(typeof(Action<float, double, float>), driver,
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardFrame", Private));
            render(0, 0, 0);
            if (!driver.ConfigureCamp(TopDown3DCampSettings.Load())) throw new InvalidDataException("Existing camp equipment is required.");
            driver.SetCampPresentation(-1, 1, false, true); Call("LateUpdate"); Call("NormalizeCampSkinWeights");
            var visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", Private).GetValue(driver);
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.quality = SkinQuality.Auto;
            var left = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var right = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            using var cpu = new AnimationPlaygroundMotionSampler.SoleProbe(visual, left, right);
            var effects = 0; driver.FootContact += _ => effects++;
            var rows = new List<Row>();
            foreach (var speed in worst ? new[] {3.8f} : determinism ? new[] { 1.4f } : new[] { 1.4f, 2.7f, 3.8f })
            foreach (var rate in worst ? new[] {30} : determinism ? new[] { 144 } : new[] { 30, 60, 144 })
            foreach (var interrupted in determinism || worst ? new[] { true } : new[] { false, true })
            {
                for (var start = worst ? 11 : determinism ? 1 : 0; start < (worst ? 12 : determinism ? 2 : 16); start++)
                {
                    Call("UnbindForwardMotion");
                    typeof(TopDown3DPlayerAnimationDriver).GetField("forwardIdleSeconds", Private).SetValue(driver, 0d);
                    var phase = start / 16d; render(speed, phase, 0);
                    var oldLeft = left.position; var oldRight = right.position;
                    var row = new Row { initialSpeed = speed, phase = (float)phase, renderRate = rate, interrupted = interrupted };
                    var before = effects;
                    var duration = interrupted ? .3 : .2;
                    for (var frame = 1; frame <= Math.Ceiling(duration * rate); frame++)
                    {
                        var elapsed = frame / (double)rate;
                        var actual = interrupted && elapsed > .04 ? Mathf.Min(1.4f, (float)(elapsed - .04) * 8.5f) : 0;
                        if (actual > 0) phase = policy.AdvanceCycles(actual, phase, actual / rate);
                        render(actual, phase, 1f / rate);
                        cpu.Measure(out var soleLeft, out var soleRight, out var minimum, out var maximum);
                        row.minimumSole = Mathf.Min(row.minimumSole, soleLeft.y, soleRight.y);
                        row.minimumBody = Mathf.Min(row.minimumBody, minimum.y);
                        row.maximumHeight = Mathf.Max(row.maximumHeight, maximum.y + bindings.capsuleHeight * .5f);
                        row.maximumLift = Mathf.Max(row.maximumLift, driver.LaboratoryForwardLift);
                        row.minimumLift = Mathf.Min(row.minimumLift, driver.LaboratoryForwardLift);
                        row.maximumAnkleFrameDelta = Mathf.Max(row.maximumAnkleFrameDelta,
                            Vector3.Distance(left.position, oldLeft), Vector3.Distance(right.position, oldRight));
                        if (determinism)
                        {
                            var firstLeft = left.position; var firstRight = right.position;
                            var height = maximum.y;
                            render(actual, phase, 0);
                            cpu.Measure(out _, out _, out _, out var again);
                            row.maximumRepeatHeightDifference = Mathf.Max(row.maximumRepeatHeightDifference, Mathf.Abs(again.y - height));
                            row.maximumRepeatAnkleDifference = Mathf.Max(row.maximumRepeatAnkleDifference,
                                Vector3.Distance(left.position, firstLeft), Vector3.Distance(right.position, firstRight));
                        }
                        oldLeft = left.position; oldRight = right.position; row.samples++;
                    }
                    row.footEffects = effects - before; row.finalPoseSpeed = driver.LaboratoryForwardPoseSpeed; rows.Add(row);
                }
                File.WriteAllText(Path.Combine(folder, "progress.json"), Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
            }
            var path = Path.Combine(folder, "summary.json");
            File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                state = "equipped-stop-and-interrupted-resume-pose-matrix", rows,
                nativeIdle,
                poseAnimatorActive = driver.HumanoidAnimator.isActiveAndEnabled,
                settleSeconds = AnimationPlaygroundForwardPoseEnvelope.SettleSeconds,
                resumeSeconds = AnimationPlaygroundForwardPoseEnvelope.ResumeSeconds,
                coverage = "four-bone normalized body/cloak/backpack, fixed completed phase on stop, physical-distance advance on resume; offline poses without controller or physics simulation"
            }, Newtonsoft.Json.Formatting.Indented));
            return path;
        }
        finally
        {
            QualitySettings.skinWeights = quality;
            if (actor != null) typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDestroy", Private).Invoke(actor.GetComponent<TopDown3DPlayerAnimationDriver>(), null);
            if (actor != null) Object.DestroyImmediate(actor); Object.DestroyImmediate(bindings);
            if (idleVariant != null) Object.DestroyImmediate(idleVariant);
            EditorSceneManager.ClosePreviewScene(preview); EditorSceneManager.CloseScene(scene, true);
        }
    }
}
