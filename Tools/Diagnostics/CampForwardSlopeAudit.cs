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

// Measures the missing terrain adaptation without enabling an unqualified fix.
// Existing completed support observations remain the future plane authority.
public static class CampForwardSlopeAudit
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private sealed class Row
    {
        public float speed, slope, heading;
        public float maximumRequiredLift, maximumTotalCorrection, worstPhase;
        public int poses, overCurrentEnvelope;
    }
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate.");
        var destination = Path.Combine(root, "LocomotionImplementation20261014/forward-slope-audit-v2.json");
        if (File.Exists(destination)) throw new IOException("Preserve previous slope measurements.");
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
        var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
        var preview = EditorSceneManager.NewPreviewScene(); GameObject actor = null;
        var bindings = Object.Instantiate(TopDown3DAnimationPlaygroundSession.Find(scene).Bindings);
        var quality = QualitySettings.skinWeights;
        try
        {
            QualitySettings.skinWeights = SkinWeights.FourBones;
            bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = null;
            actor = new GameObject("Offline slope envelope"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, preview);
            actor.AddComponent<TopDown3DPlayerMotor>(); var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
            EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
            driver.ConfigureLaboratory(bindings);
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("Awake", Private).Invoke(driver, null);
            actor.SetActive(true);
            if (!driver.HumanoidAnimator.isActiveAndEnabled) throw new InvalidDataException("The slope probe requires an active Humanoid Animator.");
            typeof(TopDown3DPlayerAnimationDriver).GetField("laboratoryForwardProfile", Private).SetValue(driver, policy);
            var render = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), driver,
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", Private));
            var sampler = (AnimationPlaygroundFootProjection.Sampler)typeof(TopDown3DPlayerAnimationDriver)
                .GetField("footProjectionSampler", Private).GetValue(driver);
            var rows = new List<Row>();
            foreach (var speed in new[] { 0f, 1.4f, 2.375f, 2.7f, 3.25f, 3.8f })
            {
                var local = new List<Row>();
                foreach (var slope in new[] { 0f, 5f, 10f, 15f, 20f, 30f, 40f, 48f })
                foreach (var heading in slope == 0 ? new[] { 0f } : new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f })
                    local.Add(new Row { speed = speed, slope = slope, heading = heading });
                for (var frame = 0; frame <= 64; frame++)
                {
                    var phase = frame / 64d; render(speed, phase, 0);
                    foreach (var row in local)
                    {
                        var normal = Quaternion.Euler(0, row.heading, 0) * (Quaternion.AngleAxis(row.slope, Vector3.right) * Vector3.up);
                        // Upright capsule support: the lower hemisphere touches the
                        // plane away from its vertical bottom on an incline.
                        var planeY = -bindings.capsuleHeight * .5f - bindings.capsuleRadius * (1 / normal.y - 1);
                        var plane = new Plane(normal, new Vector3(0, planeY, 0));
                        var lift = Mathf.Max(0, -sampler.MinimumDistance(plane)) / normal.y;
                        if (lift > row.maximumRequiredLift) { row.maximumRequiredLift = lift; row.worstPhase = (float)phase; }
                        var total = driver.LaboratoryForwardLift + lift;
                        row.maximumTotalCorrection = Mathf.Max(row.maximumTotalCorrection, total);
                        if (total > policy.maximumLift) row.overCurrentEnvelope++;
                        row.poses++;
                    }
                }
                rows.AddRange(local);
            }
            File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                state = "upright-forward-terrain-envelope-audit", rows,
                coverage = "cached complete sole geometry, 65 phases per speed/plane; no terrain queries or controller simulation",
                correctionLimit = policy.maximumLift, currentPolicyQualified = policy.qualified
            }, Newtonsoft.Json.Formatting.Indented));
            return destination;
        }
        finally
        {
            QualitySettings.skinWeights = quality;
            if (actor != null) typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDestroy", Private).Invoke(actor.GetComponent<TopDown3DPlayerAnimationDriver>(), null);
            if (actor != null) Object.DestroyImmediate(actor); Object.DestroyImmediate(bindings);
            EditorSceneManager.ClosePreviewScene(preview); EditorSceneManager.CloseScene(scene, true);
        }
    }
}
