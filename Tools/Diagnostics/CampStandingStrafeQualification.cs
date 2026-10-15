using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

// Actual driver poses with a virtual constant-speed trajectory. No motor or physics stepping.
public static class CampStandingStrafeQualification
{
    public static string Run() => RunMatrix(SkinWeights.FourBones, "StandingStrafeDriverContacts20261015.json");
    public static string RunOne() => RunMatrix(SkinWeights.OneBone, "StandingStrafeDriverContactsOne20261015.json");
    public static string RunTwo() => RunMatrix(SkinWeights.TwoBones, "StandingStrafeDriverContactsTwo20261015.json");
    public static string RunFinal(string suffix = "") => RunMatrix(SkinWeights.FourBones, "StandingStrafeDriverContactsFinal" + SafeSuffix(suffix) + "20261015.json");
    public static string RunFinalOne(string suffix = "") => RunMatrix(SkinWeights.OneBone, "StandingStrafeDriverContactsFinalOne" + SafeSuffix(suffix) + "20261015.json");
    public static string RunFinalTwo(string suffix = "") => RunMatrix(SkinWeights.TwoBones, "StandingStrafeDriverContactsFinalTwo" + SafeSuffix(suffix) + "20261015.json");
    private static string SafeSuffix(string value) => value != null && System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z0-9]{0,24}$")
        ? value : throw new ArgumentException("Use a bounded simple immutable measurement suffix.");
    private static string RunMatrix(SkinWeights quality, string name)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the compiled marked offline candidate.");
        var destination = Path.Combine(root, "LocomotionImplementation20261014/" + name);
        if (File.Exists(destination)) throw new IOException("Preserve previous contact evidence.");
        var calibration = JObject.Parse(File.ReadAllText(Path.Combine(root, "LocomotionImplementation20261014/StrafeWalkLoops20261015/calibration.json")));
        var profile = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingStrafeReview.asset");
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t => t != null);
        using var fixture = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { false, quality, "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset" }, null);
        T Field<T>(string name) => (T)type.GetField(name).GetValue(fixture);
        var driver = Field<TopDown3DPlayerAnimationDriver>("Driver"); var actor = Field<GameObject>("Actor");
        var forward = Field<AnimationPlaygroundForwardLocomotionProfile>("Policy");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Call(string name) => typeof(TopDown3DPlayerAnimationDriver).GetMethod(name, flags).Invoke(driver, null);
        driver.ConfigureLaboratoryForwardReview(forward, Field<TopDown3DAnimationPlaygroundStation>("Station"));
        driver.ConfigureLaboratoryStandingStrafeReview(profile); Call("Awake"); actor.SetActive(true);
        driver.ConfigureCamp(TopDown3DCampSettings.Load()); driver.SetCampPresentation(-1, 1, false, true);
        var visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", flags).GetValue(driver);
        var projection = profile.footProjection;
        var bones = projection.bonePaths.Select(p => p.Length == 0 ? visual.transform : visual.transform.Find(p)).ToArray();
        Vector3 Point(int vertex)
        {
            var point = Vector3.zero; var weight = 0f;
            foreach (var influence in projection.vertices[vertex].points.Select((p, index) => new { p, index })
                .OrderByDescending(v => v.p.weight).ThenBy(v => v.index).Take((int)quality))
            { point += bones[influence.p.bone].TransformPoint(influence.p.position) * influence.p.weight; weight += influence.p.weight; }
            return point / weight;
        }
        var rows = new List<object>();
        foreach (var side in new[] { AnimationPlaygroundStandingTravel.Left, AnimationPlaygroundStandingTravel.Right })
        {
            var gait = side == AnimationPlaygroundStandingTravel.Left ? profile.left : profile.right;
            var recipe = calibration["gaits"].Single(g => (string)g["role"] == (side == AnimationPlaygroundStandingTravel.Left ? "Left" : "Right"));
            typeof(TopDown3DPlayerAnimationDriver).GetField("standingStrafeSide", flags).SetValue(driver, side);
            foreach (var patch in recipe["patches"])
            {
                var vertex = (int)patch["vertex"]; var start = (double)patch["start"]; var end = (double)patch["end"];
                var first = Vector3.zero; var previous = Vector3.zero; var excursion = 0f; var travel = 0f;
                var gap = 0f; var minimum = float.PositiveInfinity; var maximum = float.NegativeInfinity;
                for (var i = 0; i <= 24; i++)
                {
                    var phase = (start + (end - start) * i / 24) / gait.sourceClip.length;
                    Call("PrepareForwardGraphUpdate");
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentStandingStrafeFrame", flags).Invoke(driver,
                        new object[] { profile.maximumSpeed, phase, .15f });
                    Call("LateUpdate");
                    var sole = Point(vertex);
                    var point = sole + Vector3.right * (side == AnimationPlaygroundStandingTravel.Left ? -1 : 1) * (float)(phase * gait.metresPerCycle);
                    if (i == 0) first = point; else travel += Vector3.ProjectOnPlane(point - previous, Vector3.up).magnitude;
                    excursion = Mathf.Max(excursion, Vector3.ProjectOnPlane(point - first, Vector3.up).magnitude); previous = point;
                    var floor = actor.transform.position.y - .91f;
                    gap = Mathf.Max(gap, sole.y - floor); minimum = Mathf.Min(minimum, sole.y - floor); maximum = Mathf.Max(maximum, sole.y - floor);
                }
                rows.Add(new { side = side.ToString(), vertex, nativeStart = start, nativeEnd = end,
                    sourceRate = profile.maximumSpeed * gait.sourceClip.length / gait.metresPerCycle,
                    persistentPointExcursion = excursion, cumulativeTravel = travel, maximumGroundGap = gap,
                    minimumSoleHeight = minimum, maximumSoleHeight = maximum });
            }
        }
        var graph = (PlayableGraph)typeof(TopDown3DPlayerAnimationDriver).GetField("playableGraph", flags).GetValue(driver);
        var result = new { state = "unqualified-slow-lateral-review", quality = quality.ToString(), graphOutputs = graph.GetOutputCount(), rows,
            runtimeModule = typeof(TopDown3DPlayerAnimationDriver).Module.ModuleVersionId,
            scope = "Eight native fixed sole windows on the actual driver at the recorded skin quality. Virtual constant-speed lateral path; no collision, physics, terrain, diagonal, backward, input-feel or performance proof." };
        File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return destination;
    }
}
