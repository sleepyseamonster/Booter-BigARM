using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

// Source selection only. Two zero-time evaluations intentionally fit the native
// flat pose before screening terrain reach. This is not runtime/performance proof.
public static class CampForwardTerrainSourceScreen
{
    private sealed class Row
    {
        public string source;
        public float slope, heading, maximumNativeFlexion, maximumAdaptedFlexion, maximumKneeStep;
        public float maximumIncompatibleReach, maximumFlightLowering, maximumContactRelease;
        public int poses, unavailable;
    }
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, ".animation-playground-validation-project"))) throw new InvalidOperationException("Use the marked offline candidate.");
        var revision = (int)typeof(AnimationPlaygroundTerrainPose).GetField("ImplementationRevision").GetRawConstantValue();
        if (revision != 13 || EditorApplication.isCompiling) throw new InvalidOperationException("Wait for the paired runtime revision.");
        var directory = Path.Combine(root, "LocomotionImplementation20261014/TerrainSourceScreenV2");
        if (Directory.Exists(directory)) throw new IOException("Preserve earlier source evidence.");
        Directory.CreateDirectory(directory);
        var runtimeSource = Path.Combine(root, "Assets/_Project/Scripts/Runtime/TopDown3D/AnimationPlayground/AnimationPlaygroundTerrainPose.cs");
        string Hash(string path)
        {
            using var algorithm = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        var runtimeSourceSha256 = Hash(runtimeSource);
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundTerrainPoseTests+Fixture")).First(t => t != null);
        using var fixture = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { SkinWeights.FourBones }, null);
        T Property<T>(string name) => (T)type.GetProperty(name).GetValue(fixture);
        var driver = Property<TopDown3DPlayerAnimationDriver>("Driver"); var stage = Property<AnimationPlaygroundTerrainPose>("Stage");
        var graph = Property<PlayableGraph>("Graph");
        var support = (Action<float, float>)Delegate.CreateDelegate(typeof(Action<float, float>), fixture, type.GetMethod("Support"));
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var motor = Property<TopDown3DPlayerMotor>("Motor");
        var visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", flags).GetValue(driver);
        var basePosition = (Vector3)typeof(TopDown3DPlayerAnimationDriver).GetField("visualLocalPosition", flags).GetValue(driver);
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
        var sampler = policy.footProjection.Bind(visual.transform);
        var sources = policy.gaits.Select(g => (name: "MCU " + g.role, clip: g.sourceClip)).ToList();
        foreach (var name in new[] { "StandardWalk", "StandardRun" })
        {
            var path = "Assets/_Project/Art/Animations/Mixamo/Locomotion/" + name + ".fbx";
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            sources.Add((name, clip));
        }
        var retained = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/CalibratedLocomotion.asset");
        foreach (var role in new[] { TopDown3DLocomotionClipRole.Run, TopDown3DLocomotionClipRole.Sprint })
            sources.Add(("Retained " + role, retained.GetRequiredEntry(role).Clip));
        var inputs = sources.Select(s => new { s.name, path = AssetDatabase.GetAssetPath(s.clip), s.clip.length,
            sha256 = Hash(Path.Combine(root, AssetDatabase.GetAssetPath(s.clip))) }).ToArray();
        var animator = driver.HumanoidAnimator;
        var lh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var rh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        var lk = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg); var rk = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        var lf = animator.GetBoneTransform(HumanBodyBones.LeftFoot); var rf = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        float Flexion() => Mathf.Max(180 - Vector3.Angle(lh.position - lk.position, lf.position - lk.position),
            180 - Vector3.Angle(rh.position - rk.position, rf.position - rk.position));
        var job = graph.GetOutput(0).GetSourcePlayable(); graph.Disconnect(job, 0);
        var rows = new List<Row>();
        foreach (var source in sources)
        {
            var clip = Object.Instantiate(source.clip);
            try
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    if (binding.propertyName == "RootT.x" || binding.propertyName == "RootT.z")
                        AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, clip.length, 0));
                var playable = AnimationClipPlayable.Create(graph, clip); playable.SetApplyFootIK(false); playable.SetSpeed(0);
                graph.Connect(playable, 0, job, 0);
                foreach (var slope in new[] { 0f, 30f, 40f, 48f })
                foreach (var heading in slope == 0 ? new[] { 0f } : new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f })
                {
                    support(slope, heading); if (!motor.TryGetLaboratorySupportPlane(out var plane)) throw new InvalidOperationException("Missing fixture support.");
                    var row = new Row { source = source.name, slope = slope, heading = heading };
                    var previousLeft = Vector3.zero; var previousRight = Vector3.zero;
                    for (var frame = 0; frame <= 64; frame++)
                    {
                        visual.transform.localPosition = basePosition; stage.Disable(); playable.SetTime(clip.length * frame / 64d); graph.Evaluate(0);
                        row.maximumNativeFlexion = Mathf.Max(row.maximumNativeFlexion, Flexion());
                        visual.transform.localPosition += Vector3.up * (-.91f - sampler.MinimumWorldY());
                        stage.Configure(plane, -.91f); graph.Evaluate(0); var data = stage.Sample;
                        row.maximumAdaptedFlexion = Mathf.Max(row.maximumAdaptedFlexion, Flexion());
                        row.maximumIncompatibleReach = Mathf.Max(row.maximumIncompatibleReach, data.incompatibleReach);
                        row.maximumFlightLowering = Mathf.Max(row.maximumFlightLowering, Mathf.Max(data.leftFlightLowering, data.rightFlightLowering));
                        row.maximumContactRelease = Mathf.Max(row.maximumContactRelease, Mathf.Max(data.leftReleased, data.rightReleased));
                        if (data.incompatibleReach > .001f) row.unavailable++;
                        if (frame > 0) row.maximumKneeStep = Mathf.Max(row.maximumKneeStep,
                            Mathf.Max(Vector3.Distance(previousLeft, lk.position), Vector3.Distance(previousRight, rk.position)));
                        previousLeft = lk.position; previousRight = rk.position; row.poses++;
                    }
                    rows.Add(row); File.WriteAllText(Path.Combine(directory, "progress.json"), Newtonsoft.Json.JsonConvert.SerializeObject(rows));
                }
                graph.Disconnect(job, 0); graph.DestroyPlayable(playable);
            }
            finally { Object.DestroyImmediate(clip); }
        }
        var output = Path.Combine(directory, "summary.json");
        if (EditorApplication.isCompiling || Hash(runtimeSource) != runtimeSourceSha256) throw new InvalidOperationException("Runtime source changed while screening.");
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { revision, runtimeSourceSha256, inputs,
            moduleId = typeof(AnimationPlaygroundTerrainPose).Module.ModuleVersionId,
            state = "bare-source-feasibility-screen-not-qualification", rows,
            recipe = "In-memory horizontal RootT curves zeroed; native per-pose flat fit then terrain job; no saved assets, garment proof, motion clock or physics simulation." }, Newtonsoft.Json.Formatting.Indented));
        return output;
    }
}
