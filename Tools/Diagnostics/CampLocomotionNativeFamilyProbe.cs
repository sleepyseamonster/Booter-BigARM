using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Initial equipped candidate screening, not source/grounding acceptance.
public static class CampLocomotionNativeFamilyProbe
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object o, string n) => (T)o.GetType().GetField(n, Flags).GetValue(o);
    private static void Call(object o, string n) => o.GetType().GetMethod(n, Flags).Invoke(o, null);
    private sealed class Envelope
    {
        public string name;
        public int samples;
        public float minimumSole = float.PositiveInfinity, minimumEquippedBody = float.PositiveInfinity;
        public float maximumHeight, maximumCorrection, maximumPredictionError;
        public double meanProjectionGraphMicroseconds;
    }

    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate.");
        var directory = Path.Combine(root, "LocomotionImplementation20261014", "NativeFamilyScreenV3");
        if (Directory.Exists(directory)) throw new IOException("Preserve prior candidate evidence.");
        Directory.CreateDirectory(directory);
        var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
        var preview = EditorSceneManager.NewPreviewScene();
        var rows = new List<Envelope>();
        var bindings = Object.Instantiate(TopDown3DAnimationPlaygroundSession.Find(scene).Bindings);
        bindings.locomotion = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>("Assets/_Project/Settings/Player/TopDown3DLocomotionClipProfile.asset");
        GameObject actor = null;
        var qualityBefore = QualitySettings.skinWeights;
        try
        {
            foreach (var quality in new[] { SkinQuality.Bone1, SkinQuality.Bone2, SkinQuality.Bone4 })
            {
                QualitySettings.skinWeights = (SkinWeights)(int)quality;
                actor = new GameObject("Offline equipped native family"); actor.SetActive(false);
                SceneManager.MoveGameObjectToScene(actor, preview);
                actor.AddComponent<TopDown3DPlayerMotor>();
                var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
                driver.ConfigureLaboratory(bindings); Call(driver, "Awake"); actor.SetActive(true);
                driver.SetLaboratorySustainedDiagnostic(true);
                var graph = Field<PlayableGraph>(driver, "playableGraph"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var mixer = Field<AnimationMixerPlayable>(driver, "mixer");
                var clips = Field<AnimationClipPlayable[]>(driver, "clipPlayables");
                var visual = Field<GameObject>(driver, "visualInstance");
                for (var i = 0; i < clips.Length; i++) if (clips[i].IsValid()) clips[i].SetSpeed(0);
                for (var i = 0; i < mixer.GetInputCount(); i++) mixer.SetInputWeight(i, i == 0 ? 1 : 0);
                clips[0].SetTime(0); graph.Evaluate(0);
                if (!driver.ConfigureCamp(TopDown3DCampSettings.Load())) throw new InvalidDataException("Hydrate existing equipment.");
                driver.SetCampPresentation(-1, 1, false, true); Call(driver, "LateUpdate");
                foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.quality = SkinQuality.Auto;
                var sampler = bindings.footProjection.Bind(visual.transform);
                using var cpu = new AnimationPlaygroundMotionSampler.SoleProbe(visual,
                    driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot),
                    driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot));
                foreach (var warm in new[] { false, true })
                {
                    if (warm) Call(driver, "NormalizeCampSkinWeights");
                    var cases = new[]
                    {
                        new Vector3(1,0,0), new Vector3(0,1,0), new Vector3(0,0,1),
                        new Vector3(.75f,.25f,0), new Vector3(.5f,.5f,0), new Vector3(.25f,.75f,0),
                        new Vector3(0,.75f,.25f), new Vector3(0,.5f,.5f), new Vector3(0,.25f,.75f)
                    };
                    foreach (var weights in cases)
                    {
                        var row = new Envelope { name = quality + "." + (warm ? "warm" : "cold") + "." + weights };
                        for (var i = 0; i < mixer.GetInputCount(); i++) mixer.SetInputWeight(i, i == 1 ? weights.x : i == 2 ? weights.y : i == 3 ? weights.z : 0);
                        var timer = new Stopwatch();
                        for (var frame = 0; frame <= 32; frame++)
                        {
                            visual.transform.localPosition = bindings.visualOffset;
                            for (var role = 1; role <= 3; role++)
                            {
                                var entry = bindings.locomotion.GetRequiredEntry((TopDown3DLocomotionClipRole)role);
                                var phase = TopDown3DAnimationMath.MapSemanticPhaseToSource(frame / 32f,
                                    entry.LeftContactPhases, entry.RightContactPhases);
                                clips[role].SetTime(phase * entry.Clip.length);
                            }
                            timer.Start();
                            graph.Evaluate(0);
                            var predicted = sampler.MinimumWorldY();
                            var correction = Mathf.Max(0, -bindings.capsuleHeight * .5f - predicted);
                            if (correction > .18f) throw new InvalidDataException("Native family exceeds projection envelope.");
                            visual.transform.localPosition += Vector3.up * correction;
                            timer.Stop();
                            cpu.Measure(out var left, out var right, out var minimum, out var maximum);
                            row.minimumSole = Mathf.Min(row.minimumSole, left.y, right.y);
                            row.minimumEquippedBody = Mathf.Min(row.minimumEquippedBody, minimum.y);
                            row.maximumHeight = Mathf.Max(row.maximumHeight, maximum.y + bindings.capsuleHeight * .5f);
                            row.maximumCorrection = Mathf.Max(row.maximumCorrection, correction);
                            row.maximumPredictionError = Mathf.Max(row.maximumPredictionError, Mathf.Abs(predicted + correction - Mathf.Min(left.y, right.y)));
                            row.samples++;
                        }
                        row.meanProjectionGraphMicroseconds = timer.Elapsed.TotalMilliseconds * 1000 / row.samples;
                        rows.Add(row);
                        File.WriteAllText(Path.Combine(directory, "progress.json"), Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
                    }
                }
                Object.DestroyImmediate(actor); actor = null;
            }
            var summary = Path.Combine(directory, "summary.json");
            File.WriteAllText(summary, Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                state = "native-family-screen-not-qualification",
                floor = -bindings.capsuleHeight * .5f, sourceProfile = AssetDatabase.GetAssetPath(bindings.locomotion),
                envelopes = rows, equipment = "skinned body/cloak/backpack; actor-root packed kit is outside this mesh probe; boots have no separate renderer in current loadout owner",
                graphOrder = "one zero-delta pose evaluation, then visual-host projection, no second time/pose evaluation",
                skinning = "candidate global quality explicitly matched per envelope; renderer Auto; original global quality restored",
                limitations = "33 phases x nine weights; refine keys/time-map breaks, stance drift and handoffs before acceptance"
            }, Newtonsoft.Json.Formatting.Indented));
            return summary;
        }
        finally
        {
            QualitySettings.skinWeights = qualityBefore;
            if (actor != null) Object.DestroyImmediate(actor);
            Object.DestroyImmediate(bindings);
            EditorSceneManager.ClosePreviewScene(preview);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
