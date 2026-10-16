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
        public const int ImplementationRevision = 14;
        public const float MaximumKneeFlexion = 150;
        private static readonly float KneeFlexionCosine = Mathf.Cos(MaximumKneeFlexion * Mathf.Deg2Rad);
        public struct Vertex { public int start, count, side; }
        public struct Bone { public TransformStreamHandle handle; public Vector3 scale; }
        public struct ContactJob : IAnimationJob
        {
            [ReadOnly] public NativeArray<Bone> bones;
            [ReadOnly] public NativeArray<Vertex> vertices;
            [ReadOnly] public NativeArray<AnimationPlaygroundSkinPoint> points;
            public NativeArray<Matrix4x4> matrices;
            public TransformStreamHandle hips, leftHip, rightHip, leftKnee, rightKnee;
            public Plane plane;
            public Plane leftPlane, rightPlane;
            public bool individualSupport;
            public float flatY, maximumLowering, flatLift;
            public Vector3 characterRight;
            public Vector3 flatOrigin;
            public Vector3 visualHostPosition;
            public float supportRadius;
            public float leftReleased, rightReleased;
            public float leftFlightLowering, rightFlightLowering;
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
                if (individualSupport)
                {
                    ProcessIndividualSupport(stream, left, right, leftRotation, rightRotation,
                        nativeLeftHip, nativeRightHip, nativeLeftKnee, nativeRightKnee,
                        leftUpperRotation, rightUpperRotation, leftLowerRotation, rightLowerRotation,
                        leftFootHandle, rightFootHandle, leftMinimum, rightMinimum);
                    return;
                }
                flatLift = plane.normal.y > .999999f ? 0 : Mathf.Max(0, flatY - sourceMinimum);
                leftMinimum += flatLift; rightMinimum += flatLift;
                left += Vector3.up * flatLift; right += Vector3.up * flatLift;
                leftGap = leftMinimum - flatY; rightGap = rightMinimum - flatY;
                leftWeight = SupportWeight(leftGap); rightWeight = SupportWeight(rightGap);
                leftTarget = left; rightTarget = right; lowering = requiredLowering = incompatibleReach = 0;
                leftReleased = rightReleased = 0;
                leftFlightLowering = rightFlightLowering = 0;
                if (plane.normal.y > .999999f) return; // Exact native pose on a flat plane.
                var forward = Vector3.Cross(characterRight, Vector3.up).normalized;
                var surfaceForward = forward + Vector3.up * (-Vector3.Dot(plane.normal, forward) / plane.normal.y);
                var rotation = Quaternion.LookRotation(surfaceForward.normalized, plane.normal)
                    * Quaternion.Inverse(Quaternion.LookRotation(forward, Vector3.up));
                // A single orthonormal support frame preserves the entire source
                // foot arc: tangential stride and normal flight height together.
                // Mapping only x/z while retaining world-up height distorts a
                // recovery foot on steep ground.
                leftTarget = OnSurface(left, rotation); rightTarget = OnSurface(right, rotation);
                var leftDelta = rotation; var rightDelta = rotation;
                var angle = Mathf.Acos(Mathf.Clamp(plane.normal.y, -1, 1));
                var onset = Mathf.SmoothStep(0, 1, Mathf.Clamp01(angle * Mathf.Rad2Deg / 5));
                // A continuous incline envelope keeps the hips from chasing each
                // late-stance reach limit. Those feet lift off when extension ends.
                requiredLowering = supportRadius * (1 / plane.normal.y - 1) + .1f * Mathf.Tan(angle) + .006f * onset;
                lowering = Mathf.Min(requiredLowering, maximumLowering);
                var leftUpperLength = Vector3.Distance(nativeLeftHip, nativeLeftKnee);
                var leftLowerLength = Vector3.Distance(nativeLeftKnee, left - Vector3.up * flatLift);
                var rightUpperLength = Vector3.Distance(nativeRightHip, nativeRightKnee);
                var rightLowerLength = Vector3.Distance(nativeRightKnee, right - Vector3.up * flatLift);
                var leftFloor = left.y - leftMinimum + .001f;
                var rightFloor = right.y - rightMinimum + .001f;
                if (!FitsFloors(lowering, nativeLeftHip, nativeRightHip, leftUpperLength, leftLowerLength, rightUpperLength, rightLowerLength, leftFloor, rightFloor))
                {
                    // Reduce pelvis lowering when its minimum knee reach would
                    // otherwise put the boot through the support plane.
                    var low = 0f; var high = lowering;
                    if (!FitsFloors(0, nativeLeftHip, nativeRightHip, leftUpperLength, leftLowerLength, rightUpperLength, rightLowerLength, leftFloor, rightFloor)) incompatibleReach = 1;
                    for (var iteration = 0; iteration < 12; iteration++)
                    {
                        var middle = (low + high) * .5f;
                        if (FitsFloors(middle, nativeLeftHip, nativeRightHip, leftUpperLength, leftLowerLength, rightUpperLength, rightLowerLength, leftFloor, rightFloor)) low = middle; else high = middle;
                    }
                    lowering = low;
                }
                var leftBodyHip = nativeLeftHip + Vector3.up * (flatLift - lowering);
                var rightBodyHip = nativeRightHip + Vector3.up * (flatLift - lowering);
                leftTarget = Reachable(leftBodyHip, leftTarget, leftUpperLength, leftLowerLength,
                    out leftReleased, out leftFlightLowering, out var leftConflict);
                rightTarget = Reachable(rightBodyHip, rightTarget, rightUpperLength, rightLowerLength,
                    out rightReleased, out rightFlightLowering, out var rightConflict);
                incompatibleReach = Mathf.Max(incompatibleReach, Mathf.Max(leftConflict, rightConflict));
                var leftHint = Knee(leftBodyHip, leftTarget, nativeLeftKnee + Vector3.up * (flatLift - lowering),
                    left - Vector3.up * lowering, leftUpperLength, leftLowerLength, -characterRight);
                var rightHint = Knee(rightBodyHip, rightTarget, nativeRightKnee + Vector3.up * (flatLift - lowering),
                    right - Vector3.up * lowering, rightUpperLength, rightLowerLength, characterRight);
                var bodyShift = Vector3.up * (flatLift - lowering);
                leftHint = Vector3.Lerp(nativeLeftKnee + bodyShift, leftHint, onset);
                rightHint = Vector3.Lerp(nativeRightKnee + bodyShift, rightHint, onset);
                hips.SetPosition(stream, hips.GetPosition(stream) + bodyShift);
                leftSolvedKnee = SolveLeg(stream, leftHip, leftKnee, leftFootHandle, leftBodyHip, nativeLeftKnee + bodyShift,
                    left - Vector3.up * lowering, leftUpperRotation, leftLowerRotation,
                    Vector3.Lerp(left - Vector3.up * lowering, leftTarget, onset),
                    Quaternion.Slerp(leftRotation, leftDelta * leftRotation, onset), leftHint, out var leftResidual);
                rightSolvedKnee = SolveLeg(stream, rightHip, rightKnee, rightFootHandle, rightBodyHip, nativeRightKnee + bodyShift,
                    right - Vector3.up * lowering, rightUpperRotation, rightLowerRotation,
                    Vector3.Lerp(right - Vector3.up * lowering, rightTarget, onset),
                    Quaternion.Slerp(rightRotation, rightDelta * rightRotation, onset), rightHint, out var rightResidual);
                incompatibleReach = Mathf.Max(incompatibleReach, Mathf.Max(leftResidual, rightResidual));
            }
            private void ProcessIndividualSupport(AnimationStream stream, Vector3 left, Vector3 right,
                Quaternion leftRotation, Quaternion rightRotation, Vector3 nativeLeftHip, Vector3 nativeRightHip,
                Vector3 nativeLeftKnee, Vector3 nativeRightKnee, Quaternion leftUpperRotation, Quaternion rightUpperRotation,
                Quaternion leftLowerRotation, Quaternion rightLowerRotation, TransformStreamHandle leftFootHandle,
                TransformStreamHandle rightFootHandle, float leftMinimum, float rightMinimum)
            {
                // The source's calibrated host correction is already applied.
                // Mixed directional poses can put a toe below that plane: solve
                // the foot instead of lifting the entire character a second time.
                flatLift = 0;
                leftGap = Mathf.Max(0, leftMinimum + flatLift - flatY); rightGap = Mathf.Max(0, rightMinimum + flatLift - flatY);
                leftWeight = SupportWeight(leftGap); rightWeight = SupportWeight(rightGap);
                var leftDelta = Quaternion.FromToRotation(Vector3.up, leftPlane.normal);
                var rightDelta = Quaternion.FromToRotation(Vector3.up, rightPlane.normal);
                leftTarget = IndividualTarget(left, leftDelta, leftPlane, 0, leftGap);
                rightTarget = IndividualTarget(right, rightDelta, rightPlane, 1, rightGap);
                var leftUpper = Vector3.Distance(nativeLeftHip, nativeLeftKnee); var leftLower = Vector3.Distance(nativeLeftKnee, left);
                var rightUpper = Vector3.Distance(nativeRightHip, nativeRightKnee); var rightLower = Vector3.Distance(nativeRightKnee, right);
                requiredLowering = Mathf.Max(RequiredLowering(nativeLeftHip, leftTarget, leftUpper, leftLower),
                    RequiredLowering(nativeRightHip, rightTarget, rightUpper, rightLower));
                lowering = Mathf.Min(requiredLowering, maximumLowering); incompatibleReach = 0;
                var shift = Vector3.up * (flatLift - lowering);
                var leftBody = nativeLeftHip + shift; var rightBody = nativeRightHip + shift;
                var leftFloor = leftPlane.GetDistanceToPoint(leftTarget) - leftGap;
                var rightFloor = rightPlane.GetDistanceToPoint(rightTarget) - rightGap;
                var bodyPlane = plane;
                plane = leftPlane; leftTarget = Reachable(leftBody, leftTarget, leftUpper, leftLower,
                    out leftReleased, out leftFlightLowering, out var leftConflict);
                plane = rightPlane; rightTarget = Reachable(rightBody, rightTarget, rightUpper, rightLower,
                    out rightReleased, out rightFlightLowering, out var rightConflict);
                plane = bodyPlane;
                // Preserve the floor while allowing the source's flight arc to
                // compress at a bump. An unreachable planted foot may release
                // by at most 80 mm; it never stretches a limb or enters terrain.
                incompatibleReach = Mathf.Max(leftConflict, rightConflict);
                incompatibleReach = Mathf.Max(incompatibleReach, Mathf.Max(leftFloor-leftPlane.GetDistanceToPoint(leftTarget),
                    rightFloor-rightPlane.GetDistanceToPoint(rightTarget)));
                incompatibleReach = Mathf.Max(incompatibleReach, Mathf.Max(leftReleased, rightReleased)-.08f);
                if (requiredLowering > maximumLowering || incompatibleReach > .001f) return;
                hips.SetPosition(stream, hips.GetPosition(stream) + shift);
                leftSolvedKnee = SolveLeg(stream, leftHip, leftKnee, leftFootHandle, leftBody, nativeLeftKnee + shift, left + shift,
                    leftUpperRotation, leftLowerRotation, leftTarget, leftDelta * leftRotation, nativeLeftKnee + shift, out var leftResidual);
                rightSolvedKnee = SolveLeg(stream, rightHip, rightKnee, rightFootHandle, rightBody, nativeRightKnee + shift, right + shift,
                    rightUpperRotation, rightLowerRotation, rightTarget, rightDelta * rightRotation, nativeRightKnee + shift, out var rightResidual);
                incompatibleReach = Mathf.Max(incompatibleReach, Mathf.Max(leftResidual, rightResidual));
            }
            private Vector3 IndividualTarget(Vector3 foot, Quaternion rotation, Plane support, int side, float gap)
            {
                var minimum = float.PositiveInfinity;
                for (var i = 0; i < vertices.Length; i++)
                    if (vertices[i].side == side) minimum = Mathf.Min(minimum, support.GetDistanceToPoint(foot + rotation * (Point(i) - foot)));
                return foot + Vector3.up * ((gap - minimum) / support.normal.y);
            }
            private float RequiredLowering(Vector3 hip, Vector3 target, float upper, float lower)
            {
                var horizontal = Vector3.ProjectOnPlane(target - hip, Vector3.up).sqrMagnitude;
                var maximum = upper + lower - .006f;
                if (horizontal >= maximum * maximum) return maximumLowering + 1;
                return Mathf.Max(0, hip.y + flatLift - target.y - Mathf.Sqrt(maximum * maximum - horizontal));
            }
            private static Vector3 SolveLeg(AnimationStream stream, TransformStreamHandle upper, TransformStreamHandle lower, TransformStreamHandle foot,
                Vector3 hip, Vector3 knee, Vector3 end, Quaternion upperRotation, Quaternion lowerRotation,
                Vector3 target, Quaternion targetRotation, Vector3 hint, out float residual)
            {
                residual = 0;
                var requestedTarget = target;
                for (var iteration = 0; iteration < 4; iteration++)
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
                var upperDelta = RotationBetween(knee - hip, desiredKnee - hip);
                upper.SetRotation(stream, upperDelta * upperRotation);
                // Read the actual FK result so parent scaling/retargeting does
                // not silently turn a reachable goal into an endpoint miss.
                var posedKnee = lower.GetPosition(stream); var posedFoot = foot.GetPosition(stream);
                var lowerDelta = RotationBetween(posedFoot - posedKnee, target - posedKnee);
                lower.SetRotation(stream, lowerDelta * lower.GetRotation(stream));
                hip = upper.GetPosition(stream); knee = lower.GetPosition(stream); end = foot.GetPosition(stream);
                upperRotation = upper.GetRotation(stream); lowerRotation = lower.GetRotation(stream);
                if ((end - target).sqrMagnitude < .0000000001f) break;
                }
                foot.SetRotation(stream, targetRotation);
                residual = Vector3.Distance(foot.GetPosition(stream), requestedTarget);
                // Only numerical FK residuals may translate the endpoint. The
                // reach envelope rejects an actual limb-length mismatch first.
                if (residual <= .001f) foot.SetPosition(stream, requestedTarget);
                return lower.GetPosition(stream);
            }
            private static Quaternion RotationBetween(Vector3 from, Vector3 to)
            {
                var magnitude = Mathf.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
                if (magnitude < .000000000001f) return Quaternion.identity;
                var cross = Vector3.Cross(from, to); var w = magnitude + Vector3.Dot(from, to);
                if (w < magnitude * .000001f) return Quaternion.FromToRotation(from, to);
                var inverse = 1 / Mathf.Sqrt(cross.sqrMagnitude + w * w);
                return new Quaternion(cross.x * inverse, cross.y * inverse, cross.z * inverse, w * inverse);
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
            private Vector3 OnSurface(Vector3 position, Quaternion rotation)
            {
                // The upright capsule touches an incline at the projection of
                // its lower sphere centre, not directly below the actor root.
                var sphereCentre = flatOrigin + Vector3.up * supportRadius;
                var surfaceOrigin = sphereCentre - plane.normal * plane.GetDistanceToPoint(sphereCentre);
                return surfaceOrigin + rotation * (position - flatOrigin);
            }
            private static float SupportWeight(float gap) => 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.012f, .05f, gap));
            private Vector3 Knee(Vector3 hip, Vector3 foot, Vector3 original, Vector3 originalFoot, float upper, float lower, Vector3 outward)
            {
                var delta = foot - hip; var distance = delta.magnitude;
                if (distance < .001f) return original;
                var axis = delta / distance;
                var along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
                var centre = hip + axis * along; var radius = Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
                var sourceAxis = (originalFoot - hip).normalized;
                var sourceBend = Vector3.ProjectOnPlane(original - hip, sourceAxis);
                if (sourceBend.sqrMagnitude < .000001f) sourceBend = Vector3.Cross(sourceAxis, characterRight);
                var native = (RotationBetween(sourceAxis, axis) * sourceBend.normalized).normalized;
                if (native.sqrMagnitude < .1f) native = Vector3.ProjectOnPlane(original - hip, axis).normalized;
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
            private static float MinimumReach(float upper, float lower)
            {
                return Mathf.Sqrt(upper * upper + lower * lower + 2 * upper * lower * KneeFlexionCosine);
            }
            private static float NormalCeiling(float tangentSquared, float minimum)
            {
                // Conservative minimum-reach envelope, C1 at both joins. For
                // u<=1 its squared distance is minimum²*(1+u²/4); u>=1
                // already satisfies minimum reach through tangential distance.
                var u = tangentSquared / (minimum * minimum);
                if (u <= 1) return -minimum * (1 - .5f * u);
                if (u >= 2) return 0;
                var t = u - 1;
                return minimum * (-.5f + .5f * (t + t * t - t * t * t));
            }
            private bool FloorFits(Vector3 hip, Vector3 target, float upper, float lower, float footFloor)
            {
                var tangentSquared = Vector3.ProjectOnPlane(target - hip, plane.normal).sqrMagnitude;
                return plane.GetDistanceToPoint(hip) + NormalCeiling(tangentSquared, MinimumReach(upper, lower)) >= footFloor;
            }
            private bool FitsFloors(float value, Vector3 leftHip, Vector3 rightHip, float leftUpper, float leftLower,
                float rightUpper, float rightLower, float leftFloor, float rightFloor)
                => FloorFits(leftHip + Vector3.up * (flatLift - value), leftTarget, leftUpper, leftLower, leftFloor)
                    && FloorFits(rightHip + Vector3.up * (flatLift - value), rightTarget, rightUpper, rightLower, rightFloor);
            private Vector3 Reachable(Vector3 hip, Vector3 target, float upper, float lower,
                out float released, out float flightLowering, out float conflict)
            {
                var delta = target - hip; var height = Vector3.Dot(delta, plane.normal);
                var tangentSquared = Vector3.ProjectOnPlane(delta, plane.normal).sqrMagnitude;
                var maximum = upper + lower - .006f;
                var bottom = -Mathf.Sqrt(Mathf.Max(0, maximum * maximum - tangentSquared));
                var top = NormalCeiling(tangentSquared, MinimumReach(upper, lower));
                conflict = Mathf.Max(Mathf.Max(0, Mathf.Sqrt(tangentSquared) - maximum), bottom - top);
                released = Mathf.Max(0, bottom - height);
                flightLowering = Mathf.Max(0, height - top);
                return target + plane.normal * (Mathf.Clamp(height, bottom, top) - height);
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
        private readonly Transform visualHost;
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
            visualHost = visual;
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
                var leftFootIndex = Array.FindIndex(projection.bonePaths, p => visual.Find(p) == leftFoot);
                var rightFootIndex = Array.FindIndex(projection.bonePaths, p => visual.Find(p) == rightFoot);
                if (leftFootIndex < 0 || rightFootIndex < 0)
                    throw new ArgumentException("The terrain cache must include both Humanoid foot transforms.");
                node = AnimationScriptPlayable.Create(graph, new ContactJob { bones = bones, vertices = vertices,
                    points = points, matrices = matrices, maximumLowering = maximumLowering, supportRadius = supportRadius,
                    leftFootCacheBone = leftFootIndex, rightFootCacheBone = rightFootIndex,
                    hips = animator.BindStreamTransform(animator.GetBoneTransform(HumanBodyBones.Hips)),
                    leftHip = animator.BindStreamTransform(leftHip), leftKnee = animator.BindStreamTransform(leftKnee),
                    rightHip = animator.BindStreamTransform(rightHip), rightKnee = animator.BindStreamTransform(rightKnee) }, 1);
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
            var data = node.GetJobData<ContactJob>(); data.enabled = true; data.individualSupport = false; data.plane = plane; data.flatY = flatY;
            data.characterRight = characterFrame.right;
            data.flatOrigin = new Vector3(characterFrame.position.x, flatY, characterFrame.position.z);
            data.visualHostPosition = visualHost.position;
            data.influenceLimit = limit; node.SetJobData(data);
        }
        public void ConfigureFeet(Plane plane, float flatY, Plane left, Plane right)
        {
            Configure(plane, flatY);
            if (!ValidFootPlane(left) || !ValidFootPlane(right)) throw new ArgumentException("Individual foot support must be finite and within 40 degrees.");
            var data = node.GetJobData<ContactJob>(); data.individualSupport = true; data.leftPlane = left; data.rightPlane = right; node.SetJobData(data);
        }
        private static bool ValidFootPlane(Plane support) => float.IsFinite(support.distance + support.normal.sqrMagnitude)
            && Mathf.Abs(support.normal.sqrMagnitude - 1) <= .0001f && support.normal.y >= .766f;
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
