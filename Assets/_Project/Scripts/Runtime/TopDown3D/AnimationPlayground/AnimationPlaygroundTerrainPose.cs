using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    // Optional stage inside the driver's existing output. It owns no physics
    // or animation time and is inactive for every foreign presentation owner.
    public sealed class AnimationPlaygroundTerrainPose : IDisposable
    {
        public struct Vertex { public int start, count, side; }
        public struct Bone { public TransformStreamHandle handle; public Vector3 scale; }
        public struct ContactJob : IAnimationJob
        {
            [ReadOnly] public NativeArray<Bone> bones;
            [ReadOnly] public NativeArray<Vertex> vertices;
            [ReadOnly] public NativeArray<AnimationPlaygroundSkinPoint> points;
            public NativeArray<Matrix4x4> matrices;
            public TransformStreamHandle hips, leftHip, rightHip, leftKnee, rightKnee;
            public float leftLength, rightLength, leftUpperLength, rightUpperLength, leftLowerLength, rightLowerLength;
            public Plane plane;
            public float flatY, maximumLowering, flatLift;
            public Vector3 characterRight;
            public Vector3 flatOrigin;
            public float supportRadius;
            public float leftReleased, rightReleased;
            public int influenceLimit, evaluations;
            public int leftFootCacheBone, rightFootCacheBone;
            public float sourceFootError;
            public Vector3 leftSolvedKnee, rightSolvedKnee, leftNativeKnee;
            public bool enabled;
            public Vector3 leftTarget, rightTarget;
            public float leftGap, rightGap, leftWeight, rightWeight, lowering, requiredLowering, incompatibleReach;
            public float sourceMinimum;

            public void ProcessRootMotion(AnimationStream stream) { }
            public void ProcessAnimation(AnimationStream stream)
            {
                if (!enabled || !stream.isHumanStream) return;
                evaluations++;
                var human = stream.AsHuman();
                var left = human.GetGoalPositionFromPose(AvatarIKGoal.LeftFoot);
                var right = human.GetGoalPositionFromPose(AvatarIKGoal.RightFoot);
                var leftFootHandle = bones[leftFootCacheBone].handle; var rightFootHandle = bones[rightFootCacheBone].handle;
                var leftRotation = leftFootHandle.GetRotation(stream); var rightRotation = rightFootHandle.GetRotation(stream);
                var nativeLeftHip = leftHip.GetPosition(stream); var nativeRightHip = rightHip.GetPosition(stream);
                var nativeLeftKnee = leftKnee.GetPosition(stream); var nativeRightKnee = rightKnee.GetPosition(stream);
                leftNativeKnee = nativeLeftKnee;
                var leftUpperRotation = leftHip.GetRotation(stream); var rightUpperRotation = rightHip.GetRotation(stream);
                var leftLowerRotation = leftKnee.GetRotation(stream); var rightLowerRotation = rightKnee.GetRotation(stream);
                for (var i = 0; i < bones.Length; i++)
                {
                    var bone = bones[i];
                    matrices[i] = Matrix4x4.TRS(bone.handle.GetPosition(stream), bone.handle.GetRotation(stream), bone.scale);
                }
                sourceFootError = Mathf.Max(Vector3.Distance(matrices[leftFootCacheBone].GetColumn(3), left),
                    Vector3.Distance(matrices[rightFootCacheBone].GetColumn(3), right));
                var leftMinimum = float.PositiveInfinity; var rightMinimum = float.PositiveInfinity;
                for (var i = 0; i < vertices.Length; i++)
                {
                    var p = Point(i);
                    if (vertices[i].side == 0) leftMinimum = Mathf.Min(leftMinimum, p.y);
                    else rightMinimum = Mathf.Min(rightMinimum, p.y);
                }
                sourceMinimum = Mathf.Min(leftMinimum, rightMinimum);
                flatLift = plane.normal.y > .999999f ? 0 : Mathf.Max(0, flatY - sourceMinimum);
                leftMinimum += flatLift; rightMinimum += flatLift;
                left += Vector3.up * flatLift; right += Vector3.up * flatLift;
                leftGap = leftMinimum - flatY; rightGap = rightMinimum - flatY;
                leftWeight = SupportWeight(leftGap); rightWeight = SupportWeight(rightGap);
                leftTarget = left; rightTarget = right; lowering = requiredLowering = incompatibleReach = 0;
                leftReleased = rightReleased = 0;
                if (plane.normal.y > .999999f) return; // Exact native pose on a flat plane.
                var forward = Vector3.Cross(characterRight, Vector3.up).normalized;
                var surfaceForward = forward + Vector3.up * (-Vector3.Dot(plane.normal, forward) / plane.normal.y);
                var rotation = Quaternion.LookRotation(surfaceForward.normalized, plane.normal)
                    * Quaternion.Inverse(Quaternion.LookRotation(forward, Vector3.up));
                leftTarget = OnTangent(left, rotation); rightTarget = OnTangent(right, rotation);
                var leftDelta = Quaternion.Slerp(Quaternion.identity, rotation, leftWeight);
                var rightDelta = Quaternion.Slerp(Quaternion.identity, rotation, rightWeight);
                var leftPlaneMinimum = float.PositiveInfinity; var rightPlaneMinimum = float.PositiveInfinity;
                for (var i = 0; i < vertices.Length; i++)
                {
                    var p = Point(i) + Vector3.up * flatLift;
                    if (vertices[i].side == 0) leftPlaneMinimum = Mathf.Min(leftPlaneMinimum, plane.GetDistanceToPoint(leftTarget + leftDelta * (p - left)));
                    else rightPlaneMinimum = Mathf.Min(rightPlaneMinimum, plane.GetDistanceToPoint(rightTarget + rightDelta * (p - right)));
                }
                // Supported feet may descend to the surface. Airborne feet keep
                // their native pose unless they would intersect the actual plane.
                leftTarget.y += Correction(leftPlaneMinimum, leftGap, leftWeight);
                rightTarget.y += Correction(rightPlaneMinimum, rightGap, rightWeight);
                var angle = Mathf.Acos(Mathf.Clamp(plane.normal.y, -1, 1));
                var onset = Mathf.SmoothStep(0, 1, Mathf.Clamp01(angle * Mathf.Rad2Deg / 5));
                // A continuous incline envelope keeps the hips from chasing each
                // late-stance reach limit. Those feet lift off when extension ends.
                requiredLowering = supportRadius * (1 / plane.normal.y - 1) + .1f * Mathf.Tan(angle) + .006f * onset;
                lowering = Mathf.Min(requiredLowering, maximumLowering);
                // Preserve airborne motion relative to the lowered body. Holding
                // it in world space can make a recovery foot orbit its hip.
                leftTarget.y -= lowering * (1 - leftWeight);
                rightTarget.y -= lowering * (1 - rightWeight);
                var leftDistance = leftPlaneMinimum + plane.normal.y * (leftTarget.y - left.y);
                var rightDistance = rightPlaneMinimum + plane.normal.y * (rightTarget.y - right.y);
                leftTarget.y += Mathf.Max(0, .02f * (1 - leftWeight) * plane.normal.y - leftDistance) / plane.normal.y;
                rightTarget.y += Mathf.Max(0, .02f * (1 - rightWeight) * plane.normal.y - rightDistance) / plane.normal.y;
                var leftBodyHip = nativeLeftHip + Vector3.up * (flatLift - lowering);
                var rightBodyHip = nativeRightHip + Vector3.up * (flatLift - lowering);
                var leftWanted = leftTarget.y; var rightWanted = rightTarget.y;
                leftTarget = Reachable(leftBodyHip, leftTarget, leftLength, out var leftConflict);
                rightTarget = Reachable(rightBodyHip, rightTarget, rightLength, out var rightConflict);
                leftReleased = leftTarget.y - leftWanted; rightReleased = rightTarget.y - rightWanted;
                incompatibleReach = Mathf.Max(leftConflict, rightConflict);
                var leftHint = Knee(leftBodyHip, leftTarget, nativeLeftKnee + Vector3.up * (flatLift - lowering),
                    leftUpperLength, leftLowerLength, -characterRight);
                var rightHint = Knee(rightBodyHip, rightTarget, nativeRightKnee + Vector3.up * (flatLift - lowering),
                    rightUpperLength, rightLowerLength, characterRight);
                var bodyShift = Vector3.up * (flatLift - lowering);
                leftHint = Vector3.Lerp(nativeLeftKnee + bodyShift, leftHint, onset);
                rightHint = Vector3.Lerp(nativeRightKnee + bodyShift, rightHint, onset);
                hips.SetPosition(stream, hips.GetPosition(stream) + bodyShift);
                leftSolvedKnee = SolveLeg(stream, leftHip, leftKnee, leftFootHandle, leftBodyHip, nativeLeftKnee + bodyShift,
                    left - Vector3.up * lowering, leftUpperRotation, leftLowerRotation,
                    Vector3.Lerp(left - Vector3.up * lowering, leftTarget, onset),
                    Quaternion.Slerp(leftRotation, leftDelta * leftRotation, onset), leftHint);
                rightSolvedKnee = SolveLeg(stream, rightHip, rightKnee, rightFootHandle, rightBodyHip, nativeRightKnee + bodyShift,
                    right - Vector3.up * lowering, rightUpperRotation, rightLowerRotation,
                    Vector3.Lerp(right - Vector3.up * lowering, rightTarget, onset),
                    Quaternion.Slerp(rightRotation, rightDelta * rightRotation, onset), rightHint);
            }
            private static Vector3 SolveLeg(AnimationStream stream, TransformStreamHandle upper, TransformStreamHandle lower, TransformStreamHandle foot,
                Vector3 hip, Vector3 knee, Vector3 end, Quaternion upperRotation, Quaternion lowerRotation,
                Vector3 target, Quaternion targetRotation, Vector3 hint)
            {
                var upperLength = Vector3.Distance(hip, knee); var lowerLength = Vector3.Distance(knee, end);
                var delta = target - hip; var distance = delta.magnitude;
                if (distance < .00001f) return knee;
                var axis = delta / distance;
                distance = Mathf.Clamp(distance, Mathf.Abs(upperLength - lowerLength) + .00001f, upperLength + lowerLength - .00001f);
                target = hip + axis * distance;
                var along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2 * distance);
                var radius = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
                var bend = Vector3.ProjectOnPlane(hint - hip, axis).normalized;
                if (bend.sqrMagnitude < .1f) bend = Vector3.ProjectOnPlane(knee - hip, axis).normalized;
                var desiredKnee = hip + axis * along + bend * radius;
                var upperDelta = Quaternion.FromToRotation(knee - hip, desiredKnee - hip);
                var posedKnee = hip + upperDelta * (knee - hip);
                var posedFoot = hip + upperDelta * (end - hip);
                var lowerDelta = Quaternion.FromToRotation(posedFoot - posedKnee, target - posedKnee);
                upper.SetRotation(stream, upperDelta * upperRotation);
                lower.SetRotation(stream, lowerDelta * upperDelta * lowerRotation);
                foot.SetRotation(stream, targetRotation);
                return desiredKnee;
            }
            private Vector3 Point(int index)
            {
                var vertex = vertices[index]; var p = Vector3.zero; var total = 0f;
                for (var i = 0; i < vertex.count; i++)
                {
                    var influence = points[vertex.start + i];
                    var rank = 0;
                    if (vertex.count > influenceLimit) for (var other = 0; other < vertex.count; other++)
                    {
                        var compare = points[vertex.start + other];
                        if (compare.weight > influence.weight || compare.weight == influence.weight && other < i) rank++;
                    }
                    if (rank >= influenceLimit) continue;
                    p += matrices[influence.bone].MultiplyPoint3x4(influence.position) * influence.weight; total += influence.weight;
                }
                return p / total;
            }
            private Vector3 OnTangent(Vector3 position, Quaternion rotation)
            {
                var tangent = rotation * Vector3.ProjectOnPlane(position - flatOrigin, Vector3.up);
                return new Vector3(flatOrigin.x + tangent.x, position.y, flatOrigin.z + tangent.z);
            }
            private float Correction(float minimum, float nativeGap, float weight)
            {
                var desiredGap = Mathf.Max(0, nativeGap) * weight;
                var correction = (desiredGap * plane.normal.y - minimum) / plane.normal.y;
                return correction >= 0 ? correction : correction * weight;
            }
            private static float SupportWeight(float gap) => 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.012f, .05f, gap));
            private Vector3 Knee(Vector3 hip, Vector3 foot, Vector3 original, float upper, float lower, Vector3 outward)
            {
                var delta = foot - hip; var distance = delta.magnitude;
                if (distance < .001f) return original;
                var axis = delta / distance;
                var along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
                var centre = hip + axis * along; var radius = Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
                // Use the signed anatomical flexion plane, rather than a pole
                // direction that can become parallel to a trailing leg.
                var native = Vector3.Cross(axis, characterRight).normalized;
                if (native.sqrMagnitude < .1f) native = Vector3.ProjectOnPlane(outward, axis).normalized;
                native = (native + Vector3.ProjectOnPlane(outward, axis).normalized * .2f).normalized;
                if (radius < .001f) return centre + radius * native;
                var up = Vector3.ProjectOnPlane(plane.normal, axis);
                var strength = up.magnitude;
                var risk = .095f - (plane.GetDistanceToPoint(centre) - radius * strength);
                if (strength < .001f || risk <= 0) return centre + radius * native;
                up /= strength; var sideways = Vector3.Cross(axis, up).normalized;
                var predicted = centre + radius * native;
                var anticipation = (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.095f, .24f, plane.GetDistanceToPoint(predicted))))
                    * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .03f, risk));
                // Keep the anatomical bend on the same side of the elevated
                // knee circle. A world-outward pole can cross that circle's
                // lower antipode on a cross-slope and switch safe endpoints.
                var sign = Vector3.Dot(native, sideways) < 0 ? -1 : 1;
                var preferred = Vector3.Slerp(native, up, anticipation).normalized;
                var cosine = Vector3.Dot(preferred, up);
                var required = Mathf.Clamp((.095f - plane.GetDistanceToPoint(centre)) / (radius * strength), -1, 1);
                if (cosine >= required) return centre + radius * preferred;
                return centre + radius * (up * required + sideways * (sign * Mathf.Sqrt(Mathf.Max(0, 1 - required * required))));
            }
            private static Vector3 Reachable(Vector3 hip, Vector3 target, float length, out float conflict)
            {
                var delta = target - hip;
                var vertical = Mathf.Sqrt(Mathf.Max(0, length * length - delta.x * delta.x - delta.z * delta.z));
                conflict = Mathf.Max(0, target.y - hip.y - vertical);
                target.y = Mathf.Max(target.y, hip.y - vertical);
                return target;
            }
        }


        private NativeArray<Bone> bones;
        private NativeArray<Vertex> vertices;
        private NativeArray<AnimationPlaygroundSkinPoint> points;
        private NativeArray<Matrix4x4> matrices;
        private AnimationScriptPlayable node;
        private readonly PlayableGraph graph;
        private readonly AnimationPlayableOutput output;
        private readonly Playable source;
        private readonly SkinnedMeshRenderer[] skins;
        private readonly Transform characterFrame;
        private readonly float supportRadius;
        public bool IsDisposed { get; private set; }
        public ContactJob Sample => !IsDisposed && node.IsValid() ? node.GetJobData<ContactJob>() : default;

        public AnimationPlaygroundTerrainPose(Animator animator, Transform visual, AnimationPlaygroundFootProjection projection,
            PlayableGraph graph, float maximumLowering)
        {
            if (animator == null || !animator.isHuman || visual == null || projection == null
                || !graph.IsValid() || graph.GetOutputCount() != 1 || !float.IsFinite(maximumLowering)
                || maximumLowering <= 0 || maximumLowering > .45f)
                throw new ArgumentException("A paired Humanoid graph and bounded terrain policy are required.");
            this.graph = graph; output = (AnimationPlayableOutput)graph.GetOutput(0); source = output.GetSourcePlayable();
            if (output.GetTarget() != animator) throw new ArgumentException("Terrain must share the existing Animator output.");
            if (!projection.Validate(projection.model, out var reason)) throw new ArgumentException(reason);
            skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            characterFrame = visual.parent != null ? visual.parent : visual;
            var shape = characterFrame.GetComponent<CapsuleCollider>();
            if (shape == null) throw new ArgumentException("Terrain pose requires its existing upright motor capsule.");
            supportRadius = shape.radius * Mathf.Max(Mathf.Abs(characterFrame.lossyScale.x), Mathf.Abs(characterFrame.lossyScale.z));
            try
            {
                bones = new NativeArray<Bone>(projection.bonePaths.Length, Allocator.Persistent);
                matrices = new NativeArray<Matrix4x4>(bones.Length, Allocator.Persistent);
                for (var i = 0; i < bones.Length; i++)
                {
                    var bone = visual.Find(projection.bonePaths[i]);
                    if (bone == null) throw new ArgumentException("A terrain sole-cache bone is missing.");
                    bones[i] = new Bone { handle = animator.BindStreamTransform(bone), scale = bone.lossyScale };
                }
                var vertexList = new List<Vertex>(); var pointList = new List<AnimationPlaygroundSkinPoint>();
                foreach (var vertex in projection.vertices)
                {
                    var side = projection.bonePaths[vertex.points[0].bone].Contains(".L") ? 0 : 1;
                    foreach (var point in vertex.points)
                        if ((projection.bonePaths[point.bone].Contains(".L") ? 0 : 1) != side)
                            throw new ArgumentException("Terrain sole vertices must have consistent leg ownership.");
                    vertexList.Add(new Vertex { start = pointList.Count, count = vertex.points.Length, side = side });
                    pointList.AddRange(vertex.points);
                }
                vertices = new NativeArray<Vertex>(vertexList.ToArray(), Allocator.Persistent);
                points = new NativeArray<AnimationPlaygroundSkinPoint>(pointList.ToArray(), Allocator.Persistent);
                if (!vertexList.Exists(v => v.side == 0) || !vertexList.Exists(v => v.side == 1))
                    throw new ArgumentException("The paired terrain cache must identify both legs.");
                var leftHip = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                var leftKnee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var rightHip = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                var rightKnee = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                var leftUpper = Vector3.Distance(leftHip.position, leftKnee.position);
                var leftLower = Vector3.Distance(leftKnee.position, leftFoot.position);
                var rightUpper = Vector3.Distance(rightHip.position, rightKnee.position);
                var rightLower = Vector3.Distance(rightKnee.position, rightFoot.position);
                var leftFootIndex = Array.FindIndex(projection.bonePaths, p => visual.Find(p) == leftFoot);
                var rightFootIndex = Array.FindIndex(projection.bonePaths, p => visual.Find(p) == rightFoot);
                if (leftFootIndex < 0 || rightFootIndex < 0)
                    throw new ArgumentException("The terrain cache must include both Humanoid foot transforms.");
                node = AnimationScriptPlayable.Create(graph, new ContactJob { bones = bones, vertices = vertices,
                    points = points, matrices = matrices, maximumLowering = maximumLowering, supportRadius = supportRadius,
                    leftFootCacheBone = leftFootIndex, rightFootCacheBone = rightFootIndex,
                    hips = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.Hips)),
                    leftHip = animator.BindStreamTransform(leftHip), leftKnee = animator.BindStreamTransform(leftKnee),
                    rightHip = animator.BindStreamTransform(rightHip), rightKnee = animator.BindStreamTransform(rightKnee),
                    leftUpperLength = leftUpper, leftLowerLength = leftLower, leftLength = leftUpper + leftLower - .006f,
                    rightUpperLength = rightUpper, rightLowerLength = rightLower, rightLength = rightUpper + rightLower - .006f }, 1);
                graph.Connect(source, 0, node, 0); node.SetInputWeight(0, 1); output.SetSourcePlayable(node);
            }
            catch { Dispose(); throw; }
        }

        public void Configure(Plane plane, float flatY)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(AnimationPlaygroundTerrainPose));
            if (!float.IsFinite(flatY) || !float.IsFinite(plane.distance) || !float.IsFinite(plane.normal.sqrMagnitude)
                || Mathf.Abs(plane.normal.sqrMagnitude - 1) > .0001f || plane.normal.y < .6691f)
                throw new ArgumentException("Terrain pose requires a finite walkable support plane.");
            var limit = -1;
            foreach (var skin in skins)
            {
                var quality = skin.quality == SkinQuality.Auto ? (int)QualitySettings.skinWeights : (int)skin.quality;
                if (limit > 0 && quality != limit) throw new InvalidOperationException("The terrain cache requires consistent influence quality.");
                limit = quality;
            }
            if (limit < 1) throw new InvalidOperationException("A terrain cache requires an active skin.");
            var data = node.GetJobData<ContactJob>(); data.enabled = true; data.plane = plane; data.flatY = flatY;
            data.characterRight = characterFrame.right;
            data.flatOrigin = new Vector3(characterFrame.position.x, flatY, characterFrame.position.z);
            data.influenceLimit = limit; node.SetJobData(data);
        }
        public void Disable()
        {
            if (IsDisposed || !node.IsValid()) return;
            var data = node.GetJobData<ContactJob>(); data.enabled = false; data.flatLift = data.lowering = 0;
            node.SetJobData(data);
        }
        public void Dispose()
        {
            if (IsDisposed) return;
            if (graph.IsValid() && node.IsValid())
            {
                output.SetSourcePlayable(source); graph.Disconnect(node, 0); graph.DestroyPlayable(node);
            }
            if (bones.IsCreated) bones.Dispose(); if (vertices.IsCreated) vertices.Dispose();
            if (points.IsCreated) points.Dispose(); if (matrices.IsCreated) matrices.Dispose();
            IsDisposed = true;
        }
    }
}
