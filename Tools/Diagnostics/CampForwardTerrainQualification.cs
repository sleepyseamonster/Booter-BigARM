using System;
using System.Collections.Generic;
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

// Offline prototype only. No scene/binding write or physics simulation.
public static class CampForwardTerrainQualification
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public struct ProbeJob : IAnimationJob
    {
        public Vector3 leftOffset, rightOffset;
        public Vector3 leftPose, rightPose, body;
        public float scale, leftHeight, rightHeight;
        public bool solve;
        public bool terrain;
        public bool rotateTrajectory;
        public Vector3 normal;
        public float planeY, flatY, bodyOffset;
        public Vector3 leftTarget, rightTarget;
        public void ProcessRootMotion(AnimationStream stream) { }
        public void ProcessAnimation(AnimationStream stream)
        {
            if (!stream.isHumanStream) return;
            var human = stream.AsHuman();
            leftPose = human.GetGoalPositionFromPose(AvatarIKGoal.LeftFoot);
            rightPose = human.GetGoalPositionFromPose(AvatarIKGoal.RightFoot);
            scale = human.humanScale; leftHeight = human.leftFootHeight; rightHeight = human.rightFootHeight;
            body = human.bodyPosition;
            if (!solve) return;
            leftTarget = terrain ? Target(leftPose) : leftPose + leftOffset;
            rightTarget = terrain ? Target(rightPose) : rightPose + rightOffset;
            human.bodyPosition += Vector3.up * bodyOffset;
            human.SetGoalWeightPosition(AvatarIKGoal.LeftHand, 0);
            human.SetGoalWeightPosition(AvatarIKGoal.RightHand, 0);
            human.SetGoalWeightRotation(AvatarIKGoal.LeftHand, 0);
            human.SetGoalWeightRotation(AvatarIKGoal.RightHand, 0);
            human.SetGoalPosition(AvatarIKGoal.LeftFoot, leftTarget);
            human.SetGoalPosition(AvatarIKGoal.RightFoot, rightTarget);
            human.SetGoalWeightPosition(AvatarIKGoal.LeftFoot, 1);
            human.SetGoalWeightPosition(AvatarIKGoal.RightFoot, 1);
            var rotation = terrain ? Quaternion.FromToRotation(Vector3.up, normal) : Quaternion.identity;
            human.SetGoalRotation(AvatarIKGoal.LeftFoot, rotation * human.GetGoalRotationFromPose(AvatarIKGoal.LeftFoot));
            human.SetGoalRotation(AvatarIKGoal.RightFoot, rotation * human.GetGoalRotationFromPose(AvatarIKGoal.RightFoot));
            human.SetGoalWeightRotation(AvatarIKGoal.LeftFoot, 1);
            human.SetGoalWeightRotation(AvatarIKGoal.RightFoot, 1);
            human.SolveIK();
        }
        private Vector3 Target(Vector3 pose)
        {
            if (rotateTrajectory) return new Vector3(0, planeY, 0)
                + Quaternion.FromToRotation(Vector3.up, normal) * (pose - new Vector3(0, flatY, 0));
            pose.y = planeY - (normal.x * pose.x + normal.z * pose.z) / normal.y + (pose.y - flatY) / normal.y;
            return pose;
        }
    }

    public static string Probe() => RunMatrix(false);
    public static string Terrain() => RunMatrix(true);
    private static string RunMatrix(bool terrain)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate.");
        var destination = Path.Combine(root, "LocomotionImplementation20261014/forward-terrain-job-" + (terrain ? "matrix-v4" : "probe-v2") + ".json");
        if (File.Exists(destination)) throw new IOException("Preserve previous terrain measurements.");
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
        var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
        var preview = EditorSceneManager.NewPreviewScene(); GameObject actor = null;
        var bindings = Object.Instantiate(TopDown3DAnimationPlaygroundSession.Find(scene).Bindings);
        try
        {
            bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = null;
            actor = new GameObject("Offline graph terrain probe"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, preview);
            actor.AddComponent<TopDown3DPlayerMotor>(); var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
            EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
            driver.ConfigureLaboratory(bindings);
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("Awake", Private).Invoke(driver, null);
            actor.SetActive(true);
            if (!driver.HumanoidAnimator.isActiveAndEnabled) throw new InvalidDataException("Terrain qualification needs an active Animator.");
            typeof(TopDown3DPlayerAnimationDriver).GetField("laboratoryForwardProfile", Private).SetValue(driver, policy);
            var render = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), driver,
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", Private));
            var graph = (PlayableGraph)typeof(TopDown3DPlayerAnimationDriver).GetField("playableGraph", Private).GetValue(driver);
            var output = (AnimationPlayableOutput)graph.GetOutput(0);
            var source = output.GetSourcePlayable();
            var job = AnimationScriptPlayable.Create(graph, new ProbeJob(), 1);
            graph.Connect(source, 0, job, 0); job.SetInputWeight(0, 1); output.SetSourcePlayable(job);
            var rows = new List<object>();
            if (!terrain) foreach (var offset in new[] { 0f, -.08f, -.16f, -.32f })
            {
                job.SetJobData(new ProbeJob { solve = offset != 0, bodyOffset = offset });
                render(1.4f, .25, 0);
                var data = job.GetJobData<ProbeJob>();
                rows.Add(new { offset, data.scale, data.leftHeight, data.rightHeight,
                    leftPose = XYZ(data.leftPose), rightPose = XYZ(data.rightPose), body = XYZ(data.body),
                    actualLeft = XYZ(driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot).position),
                    actualRight = XYZ(driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot).position),
                    hips = XYZ(driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.Hips).position),
                    head = XYZ(driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.Head).position),
                    lift = driver.LaboratoryForwardLift });
            }
            if (terrain)
            {
                var sampler = (AnimationPlaygroundFootProjection.Sampler)typeof(TopDown3DPlayerAnimationDriver).GetField("footProjectionSampler", Private).GetValue(driver);
                var previousQuality = QualitySettings.skinWeights;
                try
                {
                    QualitySettings.skinWeights = SkinWeights.FourBones;
                    foreach (var speed in new[] { 0f, 1.4f, 2.7f, 3.8f })
                    foreach (var slope in new[] { 0f, 10f, 20f, 30f, 40f, 48f })
                    foreach (var heading in slope == 0 ? new[] { 0f } : new[] { 0f, 90f, 180f, 270f })
                    foreach (var lowering in new[] { 0f, .08f, .16f, .24f })
                    {
                        var normal = Quaternion.Euler(0, heading, 0) * (Quaternion.AngleAxis(slope, Vector3.right) * Vector3.up);
                        var planeY = -bindings.capsuleHeight * .5f - bindings.capsuleRadius * (1 / normal.y - 1);
                        var plane = new Plane(normal, new Vector3(0, planeY, 0));
                        if (slope == 0 && lowering == 0) File.WriteAllText(destination + ".progress.json",
                            Newtonsoft.Json.JsonConvert.SerializeObject(new { speed, completedRows = rows.Count }));
                        var minimumSole = float.PositiveInfinity; var maximumGoalError = 0f;
                        var maximumSupportGoalError = 0f; var supportedFeet = 0; var maximumHeadY = float.NegativeInfinity;
                        var maximumPlantGoalError = 0f; var maximumPlantWindowGoalError = 0f;
                        for (var frame = 0; frame <= 64; frame++)
                        {
                            var phase = frame / 64d;
                            job.SetJobData(new ProbeJob()); render(speed, phase, 0);
                            job.SetJobData(new ProbeJob { solve = true, terrain = true, normal = normal,
                                flatY = -bindings.capsuleHeight * .5f, planeY = planeY, bodyOffset = -lowering });
                            graph.Evaluate(0);
                            var data = job.GetJobData<ProbeJob>();
                            minimumSole = Mathf.Min(minimumSole, sampler.MinimumDistance(plane));
                            maximumGoalError = Mathf.Max(maximumGoalError,
                                Vector3.Distance(driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot).position, data.leftTarget),
                                Vector3.Distance(driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot).position, data.rightTarget));
                            if (data.leftPose.y + bindings.capsuleHeight * .5f <= .16f)
                            {
                                supportedFeet++;
                                maximumSupportGoalError = Mathf.Max(maximumSupportGoalError,
                                    Vector3.Distance(driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot).position, data.leftTarget));
                            }
                            if (data.rightPose.y + bindings.capsuleHeight * .5f <= .16f)
                            {
                                supportedFeet++;
                                maximumSupportGoalError = Mathf.Max(maximumSupportGoalError,
                                    Vector3.Distance(driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot).position, data.rightTarget));
                            }
                            maximumHeadY = Mathf.Max(maximumHeadY, driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.Head).position.y);
                            var ordinal = (long)Math.Round(phase * 4);
                            var plantedLeft = AnimationPlaygroundGaitContacts.Side(ordinal) == TopDown3DFootSide.Left;
                            var plantError = Vector3.Distance(driver.HumanoidAnimator.GetBoneTransform(plantedLeft ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot).position,
                                plantedLeft ? data.leftTarget : data.rightTarget);
                            if (Math.Abs(phase - ordinal * .25) <= .03125001) maximumPlantWindowGoalError = Mathf.Max(maximumPlantWindowGoalError, plantError);
                            if (frame % 16 == 0) maximumPlantGoalError = Mathf.Max(maximumPlantGoalError, plantError);
                        }
                        rows.Add(new { speed, slope, heading, lowering, minimumSole, maximumGoalError,
                            maximumSupportGoalError, supportedFeet, maximumHeadY, maximumPlantGoalError, maximumPlantWindowGoalError });
                    }
                }
                finally { QualitySettings.skinWeights = previousQuality; }
            }
            File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(new { rows,
                scope = "Offline graph prototype, bare rig, flat baseline plus one terrain evaluation, no installed adaptation or physics simulation",
                lowAnkleProxy = "Ankle within 160mm of the flat floor; not an accepted stance classifier",
                plantWindow = "Selected semantic plant side at centre and +/-0.03125 cycles; no tangent drift or equipment qualification"
            }, Newtonsoft.Json.Formatting.Indented));
            return destination;
        }
        finally
        {
            if (actor != null) typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDestroy", Private).Invoke(actor.GetComponent<TopDown3DPlayerAnimationDriver>(), null);
            if (actor != null) Object.DestroyImmediate(actor); Object.DestroyImmediate(bindings);
            EditorSceneManager.ClosePreviewScene(preview); EditorSceneManager.CloseScene(scene, true);
        }
    }
    private static float[] XYZ(Vector3 value) => new[] { value.x, value.y, value.z };
}
