using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BooterBigArm.TopDown3D
{
    // Explicit, optional stage in an existing output. It owns neither root motion
    // nor animation time. The caller owns activation and the anchor's lifetime.
    public sealed class AnimationPlaygroundTangentFootPose : IDisposable
    {
        public const float MaximumCorrection = .02f;
        public struct Influence
        {
            public TransformStreamHandle bone;
            public Vector3 position, scale;
            public float weight;
        }
        public struct ContactJob : IAnimationJob
        {
            [ReadOnly] public NativeArray<Influence> influences;
            public TransformStreamHandle upper, lower, foot;
            public Vector3 anchor, bendFallback;
            public Vector3 kneeCorrection;
            public int influenceLimit, evaluations, unavailableReason, iterations;
            public bool enabled, available;
            public float requestedCorrection, residual, reachDeficit, boneLengthError, kneeFlexion;

            public void ProcessRootMotion(AnimationStream stream) { }
            public void ProcessAnimation(AnimationStream stream)
            {
                available = false; unavailableReason = iterations = 0;
                requestedCorrection = residual = reachDeficit = boneLengthError = kneeFlexion = 0;
                kneeCorrection = Vector3.zero;
                if (!enabled) return;
                evaluations++;
                if (!stream.isHumanStream) { unavailableReason = 1; return; }
                var point = Point(stream);
                var correction = anchor - point; correction.y = 0;
                requestedCorrection = correction.magnitude;
                if (!float.IsFinite(requestedCorrection) || requestedCorrection > MaximumCorrection)
                { unavailableReason = 2; return; }
                var hip = upper.GetPosition(stream); var knee = lower.GetPosition(stream); var end = foot.GetPosition(stream);
                var upperLength = Vector3.Distance(hip, knee); var lowerLength = Vector3.Distance(knee, end);
                var target = end + correction;
                var sourceFootHeight = end.y;
                var distance = Vector3.Distance(hip, target);
                var minimum = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength + lowerLength * lowerLength
                    + 2 * upperLength * lowerLength * Mathf.Cos(AnimationPlaygroundTerrainPose.MaximumKneeFlexion * Mathf.Deg2Rad)));
                var maximum = upperLength + lowerLength;
                reachDeficit = Mathf.Max(0, Mathf.Max(distance - maximum, minimum - distance));
                if (!float.IsFinite(upperLength + lowerLength + distance + reachDeficit)
                    || upperLength < .00001f || lowerLength < .00001f || reachDeficit > .00001f)
                { unavailableReason = 3; return; }
                if (requestedCorrection <= .000001f) { residual = requestedCorrection; available = true; return; }
                var footRotation = foot.GetRotation(stream);
                var upperRotation = upper.GetRotation(stream); var lowerRotation = lower.GetRotation(stream);
                var sourceKnee = knee;
                for (var iteration = 0; iteration < 8; iteration++)
                {
                    iterations++;
                    hip = upper.GetPosition(stream); knee = lower.GetPosition(stream); end = foot.GetPosition(stream);
                    distance = Vector3.Distance(hip, target);
                    reachDeficit = Mathf.Max(0, Mathf.Max(distance - maximum, minimum - distance));
                    if (!float.IsFinite(distance) || reachDeficit > .00001f)
                    { unavailableReason = 3; Restore(stream, upperRotation, lowerRotation, footRotation); return; }
                    var axis = (target - hip).normalized;
                    distance = Mathf.Clamp(distance, minimum + .000001f, maximum - .000001f);
                    var along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2 * distance);
                    var radius = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
                    var bend = Vector3.ProjectOnPlane(sourceKnee - hip, axis).normalized;
                    if (bend.sqrMagnitude < .1f) bend = Vector3.ProjectOnPlane(bendFallback, axis).normalized;
                    if (bend.sqrMagnitude < .1f)
                    { unavailableReason = 4; Restore(stream, upperRotation, lowerRotation, footRotation); return; }
                    var desiredKnee = hip + axis * along + bend * radius;
                    upper.SetRotation(stream, Align(knee - hip, desiredKnee - hip) * upper.GetRotation(stream));
                    var posedKnee = lower.GetPosition(stream); var posedFoot = foot.GetPosition(stream);
                    lower.SetRotation(stream, Align(posedFoot - posedKnee, target - posedKnee) * lower.GetRotation(stream));
                    foot.SetRotation(stream, footRotation);
                    // Resolve the selected skinned point after restoring foot
                    // orientation, including the rig's intermediate segments.
                    // A loose ankle endpoint leaves repeated submillimeter
                    // point errors that accumulate during a long plant.
                    var remaining = anchor - Point(stream); remaining.y = 0;
                    if (remaining.magnitude < .000002f) break;
                    target = foot.GetPosition(stream) + remaining;
                    target.y = sourceFootHeight;
                }
                foot.SetRotation(stream, footRotation);
                var finalHip = upper.GetPosition(stream); var finalKnee = lower.GetPosition(stream); var finalFoot = foot.GetPosition(stream);
                boneLengthError = Mathf.Max(Mathf.Abs(Vector3.Distance(finalHip, finalKnee) - upperLength),
                    Mathf.Abs(Vector3.Distance(finalKnee, finalFoot) - lowerLength));
                kneeFlexion = Vector3.Angle(finalKnee - finalHip, finalFoot - finalKnee);
                kneeCorrection = finalKnee - sourceKnee;
                var error = anchor - Point(stream); error.y = 0; residual = error.magnitude;
                available = residual <= .001f && boneLengthError <= .001f
                    && kneeFlexion <= AnimationPlaygroundTerrainPose.MaximumKneeFlexion + .01f;
                if (!available)
                { unavailableReason = 5; Restore(stream, upperRotation, lowerRotation, footRotation); }
            }

            private static Quaternion Align(Vector3 from, Vector3 to)
            {
                // FromToRotation drops small angles before the endpoint is
                // precise enough for a persistent plant. Cross/atan2 retains
                // those angles without relying on acos near a dot of one.
                var a = from.normalized; var b = to.normalized;
                var cross = Vector3.Cross(a, b); var sine = cross.magnitude;
                var cosine = Mathf.Clamp(Vector3.Dot(a, b), -1, 1);
                if (sine > .00000001f)
                    return Quaternion.AngleAxis(Mathf.Atan2(sine, cosine) * Mathf.Rad2Deg, cross / sine);
                if (cosine >= 0) return Quaternion.identity;
                var axis = Vector3.Cross(a, Vector3.right);
                if (axis.sqrMagnitude < .000001f) axis = Vector3.Cross(a, Vector3.up);
                return Quaternion.AngleAxis(180, axis.normalized);
            }

            private void Restore(AnimationStream stream, Quaternion upperRotation, Quaternion lowerRotation, Quaternion footRotation)
            {
                upper.SetRotation(stream, upperRotation); lower.SetRotation(stream, lowerRotation); foot.SetRotation(stream, footRotation);
            }

            private Vector3 Point(AnimationStream stream)
            {
                var result = Vector3.zero; var total = 0f;
                for (var i = 0; i < influences.Length && i < influenceLimit; i++)
                {
                    var item = influences[i];
                    result += Matrix4x4.TRS(item.bone.GetPosition(stream), item.bone.GetRotation(stream), item.scale)
                        .MultiplyPoint3x4(item.position) * item.weight;
                    total += item.weight;
                }
                return result / total;
            }
        }

        private NativeArray<Influence> influences;
        private AnimationScriptPlayable node;
        private readonly PlayableGraph graph;
        private readonly AnimationPlayableOutput output;
        private readonly Playable source;
        private readonly Transform character;
        private bool attached;
        public bool IsDisposed { get; private set; }
        public ContactJob Sample => !IsDisposed && node.IsValid() ? node.GetJobData<ContactJob>() : default;

        public AnimationPlaygroundTangentFootPose(Animator animator, Transform visual,
            AnimationPlaygroundFootProjection projection, int vertex, PlayableGraph graph)
        {
            if (animator == null || !animator.isHuman || visual == null || projection == null
                || !graph.IsValid() || graph.GetOutputCount() != 1 || vertex < 0)
                throw new ArgumentException("Require an existing Humanoid output and its selected sole-cache point.");
            if (!projection.Validate(projection.model, out var reason)) throw new ArgumentException(reason);
            if (vertex >= projection.vertices.Length) throw new ArgumentException("The selected sole-cache point is absent.");
            this.graph = graph; output = (AnimationPlayableOutput)graph.GetOutput(0); source = output.GetSourcePlayable();
            if (output.GetTarget() != animator || !source.IsValid()) throw new ArgumentException("Share the existing Animator output.");
            character = visual.parent != null ? visual.parent : visual;
            var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var points = projection.vertices[vertex].points.OrderByDescending(p => p.weight).ToArray();
            var transforms = points.Select(p => projection.bonePaths[p.bone].Length == 0 ? visual : visual.Find(projection.bonePaths[p.bone])).ToArray();
            if (transforms.Any(t => t == null)) throw new ArgumentException("A sole-cache bone is missing.");
            var selected = transforms.All(t => t == left || t.IsChildOf(left)) ? left
                : transforms.All(t => t == right || t.IsChildOf(right)) ? right : null;
            if (selected == null) throw new ArgumentException("The tangent point must belong entirely to one foot subtree.");
            var upper = animator.GetBoneTransform(selected == left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            var lower = animator.GetBoneTransform(selected == left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
            if (upper == null || lower == null) throw new ArgumentException("The paired leg is incomplete.");
            try
            {
                influences = new NativeArray<Influence>(points.Length, Allocator.Persistent);
                for (var i = 0; i < points.Length; i++)
                    influences[i] = new Influence { bone = animator.BindStreamTransform(transforms[i]), position = points[i].position,
                        scale = transforms[i].lossyScale, weight = points[i].weight };
                node = AnimationScriptPlayable.Create(graph, new ContactJob { influences = influences,
                    upper = animator.BindStreamTransform(upper), lower = animator.BindStreamTransform(lower),
                    foot = animator.BindStreamTransform(selected), influenceLimit = 4 }, 1);
                graph.Connect(source, 0, node, 0); node.SetInputWeight(0, 1); output.SetSourcePlayable(node); attached = true;
            }
            catch { Dispose(); throw; }
        }

        public void Configure(Vector3 anchor, int influenceLimit)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(AnimationPlaygroundTangentFootPose));
            if (!float.IsFinite(anchor.sqrMagnitude) || influenceLimit != 1 && influenceLimit != 2 && influenceLimit != 4)
                throw new ArgumentException("Require a finite anchor and measured skin influence limit.");
            var data = node.GetJobData<ContactJob>(); data.anchor = anchor; data.influenceLimit = influenceLimit;
            data.bendFallback = character.forward; data.enabled = true; node.SetJobData(data);
        }
        public void Disable()
        {
            if (IsDisposed || !node.IsValid()) return;
            var data = node.GetJobData<ContactJob>(); data.enabled = data.available = false; node.SetJobData(data);
        }
        public void Dispose()
        {
            if (IsDisposed) return;
            if (graph.IsValid() && node.IsValid())
            {
                if (attached && !output.GetSourcePlayable().Equals((Playable)node))
                    throw new InvalidOperationException("Dispose outer pose stages before the tangent stage.");
                if (attached) output.SetSourcePlayable(source);
                graph.Disconnect(node, 0); graph.DestroyPlayable(node);
            }
            if (influences.IsCreated) influences.Dispose();
            IsDisposed = true;
        }
    }
}
