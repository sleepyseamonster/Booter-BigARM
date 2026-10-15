using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class CampForwardTerrainContactQualification
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public struct Vertex { public int start, count, side; }
    public struct Bone { public TransformStreamHandle handle; public Vector3 scale; }
    public struct ContactJob : IAnimationJob
    {
        [ReadOnly] public NativeArray<Bone> bones;
        [ReadOnly] public NativeArray<Vertex> vertices;
        [ReadOnly] public NativeArray<AnimationPlaygroundSkinPoint> points;
        public NativeArray<Matrix4x4> matrices;
        public TransformStreamHandle leftHip, rightHip, leftKnee, rightKnee;
        public float leftLength, rightLength, leftUpperLength, rightUpperLength, leftLowerLength, rightLowerLength;
        public Plane plane;
        public float flatY, maximumLowering;
        public bool enabled;
        public Vector3 leftTarget, rightTarget;
        public float leftGap, rightGap, leftWeight, rightWeight, lowering, requiredLowering, incompatibleReach;
        public float sourceMinimum;

        public void ProcessRootMotion(AnimationStream stream) { }
        public void ProcessAnimation(AnimationStream stream)
        {
            if (!enabled || !stream.isHumanStream) return;
            var human = stream.AsHuman();
            var left = human.GetGoalPositionFromPose(AvatarIKGoal.LeftFoot);
            var right = human.GetGoalPositionFromPose(AvatarIKGoal.RightFoot);
            var leftRotation = human.GetGoalRotationFromPose(AvatarIKGoal.LeftFoot);
            var rightRotation = human.GetGoalRotationFromPose(AvatarIKGoal.RightFoot);
            for (var i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                matrices[i] = Matrix4x4.TRS(bone.handle.GetPosition(stream), bone.handle.GetRotation(stream), bone.scale);
            }
            var leftMinimum = float.PositiveInfinity; var rightMinimum = float.PositiveInfinity;
            for (var i = 0; i < vertices.Length; i++)
            {
                var p = Point(i);
                if (vertices[i].side == 0) leftMinimum = Mathf.Min(leftMinimum, p.y);
                else rightMinimum = Mathf.Min(rightMinimum, p.y);
            }
            sourceMinimum = Mathf.Min(leftMinimum, rightMinimum);
            leftGap = leftMinimum - flatY; rightGap = rightMinimum - flatY;
            leftWeight = SupportWeight(leftGap); rightWeight = SupportWeight(rightGap);
            leftTarget = left; rightTarget = right; lowering = requiredLowering = incompatibleReach = 0;
            if (plane.normal.y > .999999f) return; // Exact native pose on a flat plane.
            var leftDelta = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, plane.normal), leftWeight);
            var rightDelta = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, plane.normal), rightWeight);
            var leftPlaneMinimum = float.PositiveInfinity; var rightPlaneMinimum = float.PositiveInfinity;
            for (var i = 0; i < vertices.Length; i++)
            {
                var p = Point(i);
                if (vertices[i].side == 0) leftPlaneMinimum = Mathf.Min(leftPlaneMinimum, plane.GetDistanceToPoint(left + leftDelta * (p - left)));
                else rightPlaneMinimum = Mathf.Min(rightPlaneMinimum, plane.GetDistanceToPoint(right + rightDelta * (p - right)));
            }
            // Supported feet may descend to the surface. Airborne feet keep
            // their native pose unless they would intersect the actual plane.
            leftTarget.y += Correction(leftPlaneMinimum, leftGap, leftWeight);
            rightTarget.y += Correction(rightPlaneMinimum, rightGap, rightWeight);
            Reach(leftHip.GetPosition(stream), leftTarget, leftLength, out var leftLow, out var leftHigh);
            Reach(rightHip.GetPosition(stream), rightTarget, rightLength, out var rightLow, out var rightHigh);
            requiredLowering = Mathf.Max(0, leftLow, rightLow);
            var high = Mathf.Min(leftHigh, rightHigh);
            incompatibleReach = Mathf.Max(0, requiredLowering - high);
            lowering = Mathf.Clamp(requiredLowering, 0, maximumLowering);
            human.bodyPosition -= Vector3.up * lowering;
            human.SetGoalWeightPosition(AvatarIKGoal.LeftHand, 0); human.SetGoalWeightPosition(AvatarIKGoal.RightHand, 0);
            human.SetGoalWeightRotation(AvatarIKGoal.LeftHand, 0); human.SetGoalWeightRotation(AvatarIKGoal.RightHand, 0);
            human.SetGoalPosition(AvatarIKGoal.LeftFoot, leftTarget); human.SetGoalPosition(AvatarIKGoal.RightFoot, rightTarget);
            human.SetGoalRotation(AvatarIKGoal.LeftFoot, leftDelta * leftRotation);
            human.SetGoalRotation(AvatarIKGoal.RightFoot, rightDelta * rightRotation);
            human.SetHintPosition(AvatarIKHint.LeftKnee, Knee(leftHip.GetPosition(stream) - Vector3.up * lowering,
                leftTarget, leftKnee.GetPosition(stream) - Vector3.up * lowering, leftUpperLength, leftLowerLength));
            human.SetHintPosition(AvatarIKHint.RightKnee, Knee(rightHip.GetPosition(stream) - Vector3.up * lowering,
                rightTarget, rightKnee.GetPosition(stream) - Vector3.up * lowering, rightUpperLength, rightLowerLength));
            human.SetHintWeightPosition(AvatarIKHint.LeftKnee, 1); human.SetHintWeightPosition(AvatarIKHint.RightKnee, 1);
            human.SetGoalWeightPosition(AvatarIKGoal.LeftFoot, 1); human.SetGoalWeightPosition(AvatarIKGoal.RightFoot, 1);
            human.SetGoalWeightRotation(AvatarIKGoal.LeftFoot, 1); human.SetGoalWeightRotation(AvatarIKGoal.RightFoot, 1);
            human.SolveIK();
        }
        private Vector3 Point(int index)
        {
            var vertex = vertices[index]; var p = Vector3.zero;
            for (var i = 0; i < vertex.count; i++)
            {
                var influence = points[vertex.start + i];
                p += matrices[influence.bone].MultiplyPoint3x4(influence.position) * influence.weight;
            }
            return p;
        }
        private float Correction(float minimum, float nativeGap, float weight)
        {
            var desiredGap = Mathf.Max(0, nativeGap) * weight;
            var correction = (desiredGap * plane.normal.y - minimum) / plane.normal.y;
            return correction >= 0 ? correction : correction * weight;
        }
        private static float SupportWeight(float gap) => 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.012f, .05f, gap));
        private Vector3 Knee(Vector3 hip, Vector3 foot, Vector3 original, float upper, float lower)
        {
            var delta = foot - hip; var distance = delta.magnitude;
            if (distance < .001f) return original;
            var axis = delta / distance;
            var along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
            var centre = hip + axis * along; var radius = Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
            var up = Vector3.ProjectOnPlane(plane.normal, axis);
            var strength = up.magnitude;
            if (strength < .001f || radius < .001f) return original;
            up /= strength; var sideways = Vector3.Cross(axis, up).normalized;
            var native = Vector3.ProjectOnPlane(original - centre, axis).normalized;
            var cosine = Vector3.Dot(native, up);
            var required = Mathf.Clamp((.095f - plane.GetDistanceToPoint(centre)) / (radius * strength), -1, 1);
            if (cosine >= required) return original;
            var sign = Vector3.Dot(native, sideways) < 0 ? -1 : 1;
            return centre + radius * (up * required + sideways * (sign * Mathf.Sqrt(Mathf.Max(0, 1 - required * required))));
        }
        private static void Reach(Vector3 hip, Vector3 target, float length, out float low, out float high)
        {
            var delta = target - hip;
            var vertical = Mathf.Sqrt(Mathf.Max(0, length * length - delta.x * delta.x - delta.z * delta.z));
            low = hip.y - target.y - vertical; high = hip.y - target.y + vertical;
        }
    }

    public static string Run() => RunMatrix(false);
    public static string Worst() => RunMatrix(true);
    private static string RunMatrix(bool narrow)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker))) throw new InvalidOperationException("Use the marked offline candidate.");
        var destination = Path.Combine(root, "LocomotionImplementation20261014/forward-terrain-contact-" + (narrow ? "worst-v4" : "v4") + ".json");
        if (File.Exists(destination)) throw new IOException("Preserve prior contact evidence.");
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
        var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
        var preview = EditorSceneManager.NewPreviewScene(); GameObject actor = null;
        var bindings = Object.Instantiate(TopDown3DAnimationPlaygroundSession.Find(scene).Bindings);
        var quality = QualitySettings.skinWeights;
        NativeArray<Bone> bones = default; NativeArray<Vertex> vertices = default;
        NativeArray<AnimationPlaygroundSkinPoint> points = default; NativeArray<Matrix4x4> matrices = default;
        Mesh baked = null;
        try
        {
            QualitySettings.skinWeights = SkinWeights.FourBones;
            bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = null;
            actor = new GameObject("Offline contact-aware terrain graph"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, preview);
            actor.AddComponent<TopDown3DPlayerMotor>(); var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
            EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
            driver.ConfigureLaboratory(bindings); Call(driver, "Awake"); actor.SetActive(true);
            var animator = driver.HumanoidAnimator;
            if (!animator.isActiveAndEnabled) throw new InvalidDataException("An active Humanoid is required.");
            typeof(TopDown3DPlayerAnimationDriver).GetField("laboratoryForwardProfile", Private).SetValue(driver, policy);
            var render = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), driver,
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", Private));
            render(0, 0, 0); driver.ConfigureCamp(TopDown3DCampSettings.Load());
            driver.SetCampPresentation(-1, 1, false, true); Call(driver, "LateUpdate"); Call(driver, "NormalizeCampSkinWeights");
            var visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", Private).GetValue(driver);
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.quality = SkinQuality.Auto;
            var projection = policy.footProjection;
            bones = new NativeArray<Bone>(projection.bonePaths.Length, Allocator.Persistent);
            matrices = new NativeArray<Matrix4x4>(bones.Length, Allocator.Persistent);
            for (var i = 0; i < bones.Length; i++)
            {
                var bone = visual.transform.Find(projection.bonePaths[i]);
                bones[i] = new Bone { handle = animator.BindStreamTransform(bone), scale = bone.lossyScale };
            }
            var allPoints = new List<AnimationPlaygroundSkinPoint>(); var allVertices = new List<Vertex>();
            foreach (var vertex in projection.vertices)
            {
                var side = projection.bonePaths[vertex.points[0].bone].Contains(".L") ? 0 : 1;
                foreach (var point in vertex.points)
                    if ((projection.bonePaths[point.bone].Contains(".L") ? 0 : 1) != side) throw new InvalidDataException("A sole vertex crosses legs.");
                allVertices.Add(new Vertex { start = allPoints.Count, count = vertex.points.Length, side = side }); allPoints.AddRange(vertex.points);
            }
            vertices = new NativeArray<Vertex>(allVertices.ToArray(), Allocator.Persistent);
            points = new NativeArray<AnimationPlaygroundSkinPoint>(allPoints.ToArray(), Allocator.Persistent);
            var graph = (PlayableGraph)typeof(TopDown3DPlayerAnimationDriver).GetField("playableGraph", Private).GetValue(driver);
            var output = (AnimationPlayableOutput)graph.GetOutput(0); var source = output.GetSourcePlayable();
            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot); var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            float Length(HumanBodyBones a, HumanBodyBones b, HumanBodyBones c) => Vector3.Distance(animator.GetBoneTransform(a).position, animator.GetBoneTransform(b).position)
                + Vector3.Distance(animator.GetBoneTransform(b).position, animator.GetBoneTransform(c).position);
            var data = new ContactJob { bones = bones, vertices = vertices, points = points, matrices = matrices,
                leftHip = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)),
                rightHip = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)),
                leftKnee = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)),
                rightKnee = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)),
                leftUpperLength = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position),
                leftLowerLength = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position, leftFoot.position),
                rightUpperLength = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position),
                rightLowerLength = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position, rightFoot.position),
                leftLength = Length(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot) - .006f,
                rightLength = Length(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot) - .006f,
                flatY = -bindings.capsuleHeight * .5f, maximumLowering = .45f };
            var job = AnimationScriptPlayable.Create(graph, data, 1);
            graph.Connect(source, 0, job, 0); job.SetInputWeight(0, 1); output.SetSourcePlayable(job);
            var sampler = projection.Bind(visual.transform); var skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var garments = skins.Where(TopDown3DPlayerAnimationDriver.IsSoftGarment).Select(s => new AnimationPlaygroundGarmentFloor(s)).ToArray();
            baked = new Mesh(); var meshVertices = new List<Vector3>(); var rows = new List<object>();
            foreach (var speed in narrow ? new[] { 1.4f } : new[] { 0f, 1.4f, 2.05f, 2.7f, 3.25f, 3.8f })
            foreach (var slope in narrow ? new[] { 48f } : new[] { 0f, 10f, 20f, 30f, 40f, 48f })
            foreach (var heading in narrow || slope == 0 ? new[] { 0f } : new[] { 0f, 90f, 180f, 270f })
            {
                var normal = Quaternion.Euler(0, heading, 0) * (Quaternion.AngleAxis(slope, Vector3.right) * Vector3.up);
                var planeY = data.flatY - bindings.capsuleRadius * (1 / normal.y - 1);
                data.plane = new Plane(normal, new Vector3(0, planeY, 0));
                var minSole = float.PositiveInfinity; var minBody = float.PositiveInfinity; var maxHeight = float.NegativeInfinity;
                var maxLower = 0f; var maxRequired = 0f; var maxConflict = 0f; var maxGoalError = 0f; var maxPlantError = 0f; var maxPredictionError = 0f;
                var minRigidBody = float.PositiveInfinity; var maximumGarmentCorrection = 0f; var maximumResidualLift = 0f;
                var worstBodySkin = ""; var worstBodyPhase = 0f;
                var worstBodyVertex = -1; SkinnedMeshRenderer worstSkin = null;
                for (var frame = 0; frame <= 64; frame++)
                {
                    foreach (var garment in garments) garment.Restore();
                    data.enabled = false; job.SetJobData(data); render(speed, frame / 64d, 0);
                    var baseline = sampler.MinimumWorldY();
                    data.enabled = true; job.SetJobData(data); graph.Evaluate(0); var evaluated = job.GetJobData<ContactJob>();
                    maxPredictionError = Mathf.Max(maxPredictionError, Mathf.Abs(evaluated.sourceMinimum - baseline));
                    minSole = Mathf.Min(minSole, sampler.MinimumDistance(data.plane));
                    maxLower = Mathf.Max(maxLower, evaluated.lowering); maxRequired = Mathf.Max(maxRequired, evaluated.requiredLowering);
                    maxConflict = Mathf.Max(maxConflict, evaluated.incompatibleReach);
                    maxGoalError = Mathf.Max(maxGoalError, Vector3.Distance(leftFoot.position, evaluated.leftTarget), Vector3.Distance(rightFoot.position, evaluated.rightTarget));
                    if (evaluated.leftWeight >= .99f) maxPlantError = Mathf.Max(maxPlantError, Vector3.Distance(leftFoot.position, evaluated.leftTarget));
                    if (evaluated.rightWeight >= .99f) maxPlantError = Mathf.Max(maxPlantError, Vector3.Distance(rightFoot.position, evaluated.rightTarget));
                    // Measured residual clearance through the existing bounded
                    // visual host seam; no time advance or extra graph evaluate.
                    var residual = Mathf.Max(0, -sampler.MinimumDistance(data.plane)) / normal.y;
                    visual.transform.position += Vector3.up * residual;
                    maximumResidualLift = Mathf.Max(maximumResidualLift, residual);
                    foreach (var garment in garments)
                    {
                        garment.Apply(data.plane, .001f, .45f);
                        maximumGarmentCorrection = Mathf.Max(maximumGarmentCorrection, garment.MaximumDisplacement);
                    }
                    if (frame % 4 == 0) foreach (var skin in skins)
                    {
                        skin.BakeMesh(baked, true); baked.GetVertices(meshVertices);
                        for (var vertexIndex = 0; vertexIndex < meshVertices.Count; vertexIndex++)
                        {
                            var vertex = meshVertices[vertexIndex];
                            var p = skin.transform.TransformPoint(vertex);
                            var distance = data.plane.GetDistanceToPoint(p);
                            if (distance < minBody) { minBody = distance; worstBodySkin = skin.name; worstBodyPhase = frame / 64f; worstBodyVertex = vertexIndex; worstSkin = skin; }
                            if (!TopDown3DPlayerAnimationDriver.IsSoftGarment(skin)) minRigidBody = Mathf.Min(minRigidBody, distance);
                            maxHeight = Mathf.Max(maxHeight, p.y - data.flatY);
                        }
                    }
                }
                rows.Add(new { speed, slope, heading, minSole, minBody, maxHeight, maxLower, maxRequired, maxConflict, maxGoalError, maxPlantError, maxPredictionError,
                    minRigidBody, maximumGarmentCorrection, maximumResidualLift, worstBodySkin, worstBodyPhase, worstBodyVertex,
                    worstBone = worstSkin.bones[worstSkin.sharedMesh.boneWeights[worstBodyVertex].boneIndex0].name });
                File.WriteAllText(destination + ".progress.json", Newtonsoft.Json.JsonConvert.SerializeObject(rows));
            }
            File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(new { rows, state = "contact-aware-terrain-prototype-uninstalled", phases = 65,
                bodyPhases = 17, equipped = true, maximumLowering = data.maximumLowering, legLengths = new[] { data.leftLength, data.rightLength },
                scope = "Two offline evaluations at held source time, stream-cache prediction, native-flight preservation unless terrain intersects, no simulation or qualification" }, Newtonsoft.Json.Formatting.Indented));
            return destination;
        }
        finally
        {
            QualitySettings.skinWeights = quality;
            if (actor != null) Call(actor.GetComponent<TopDown3DPlayerAnimationDriver>(), "OnDestroy");
            if (bones.IsCreated) bones.Dispose(); if (vertices.IsCreated) vertices.Dispose(); if (points.IsCreated) points.Dispose(); if (matrices.IsCreated) matrices.Dispose();
            if (baked != null) Object.DestroyImmediate(baked); if (actor != null) Object.DestroyImmediate(actor); Object.DestroyImmediate(bindings);
            EditorSceneManager.ClosePreviewScene(preview); EditorSceneManager.CloseScene(scene, true);
        }
    }
    private static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
}
