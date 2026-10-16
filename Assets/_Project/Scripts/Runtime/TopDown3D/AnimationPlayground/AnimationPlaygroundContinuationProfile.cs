using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable] public struct AnimationPlaygroundAdaptedTurnKnot
    {
        public float seconds;
        public Vector2 position, velocity;
        public Quaternion rotation;
    }

    [Serializable] public sealed class AnimationPlaygroundAdaptedTurn
    {
        public AnimationClip clip;
        public int soleVertex;
        public Vector2 soleAnchor;
        public AnimationPlaygroundAdaptedTurnKnot[] knots = Array.Empty<AnimationPlaygroundAdaptedTurnKnot>();
        public void Sample(float seconds, out Vector3 position, out Quaternion rotation)
        {
            var low = 0; var high = knots.Length - 1;
            seconds = Mathf.Clamp(seconds, 0, clip.length);
            while (high - low > 1)
            { var middle = (low + high) / 2; if (knots[middle].seconds <= seconds) low = middle; else high = middle; }
            var a = knots[low]; var b = knots[high];
            var span = b.seconds - a.seconds; var elapsed = seconds - a.seconds;
            var acceleration = (b.velocity - a.velocity) / span;
            var point = a.position + a.velocity * elapsed + .5f * acceleration * elapsed * elapsed;
            position = new Vector3(point.x, 0, point.y);
            rotation = Quaternion.Slerp(a.rotation, b.rotation, elapsed / span);
        }
        public bool Validate(AnimationPlaygroundFootProjection projection)
        {
            if (clip == null || !clip.isHumanMotion || clip.isLooping || Mathf.Abs(clip.length - 1.5f) > .001f
                || projection == null || soleVertex < 0 || soleVertex >= projection.vertices.Length
                || !float.IsFinite(soleAnchor.sqrMagnitude) || knots == null || knots.Length < 180
                || Mathf.Abs(knots[0].seconds) > .000001f || knots[0].position.magnitude > .00001f
                || knots[0].velocity.magnitude > .00001f || knots[^1].velocity.magnitude > .00001f
                || Mathf.Abs(knots[^1].seconds - clip.length) > .00001f) return false;
            for (var i = 0; i < knots.Length; i++)
            {
                var k = knots[i]; var q = k.rotation;
                var norm = q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w;
                if (!float.IsFinite(k.seconds + k.position.sqrMagnitude + k.velocity.sqrMagnitude + norm)
                    || Mathf.Abs(norm - 1) > .001f || k.position.magnitude > .4f || k.velocity.magnitude > 1.4f) return false;
                if (i == 0) continue;
                var a = knots[i - 1]; var dt = k.seconds - a.seconds;
                if (dt <= 0 || (k.velocity - a.velocity).magnitude / dt > 8.501f
                    || (k.position - a.position - (a.velocity + k.velocity) * (.5f * dt)).magnitude > .00001f) return false;
                // A quadratic segment can have its radius extremum inside the
                // interval. This conservative speed bound covers the interior.
                for(var subdivision=0;subdivision<=16;subdivision++)
                {
                    var elapsed=dt*subdivision/16;
                    var position=a.position+a.velocity*elapsed+(k.velocity-a.velocity)/dt*(.5f*elapsed*elapsed);
                    if(position.magnitude+1.4f*dt/32>.4f)return false;
                }
            }
            return true;
        }
    }

    // Explicit Camp Loop experiment. It never promotes either retained forward
    // qualification flag or the old stationary-root turn contract.
    public sealed class AnimationPlaygroundContinuationProfile : ScriptableObject
    {
        public int schema = 1;
        public bool NativeCampGround => schema == 2;
        public GameObject targetModel;
        public AnimationPlaygroundFootProjection footProjection;
        public AnimationPlaygroundFootProjection nativeBodyProjection;
        public float maximumSlope = 10, maximumSupportResidual = .015f, maximumAirSpeed = 6;
        public AnimationPlaygroundAdaptedTurn left = new(), right = new();
        public bool Validate(GameObject model, AnimationPlaygroundFootProjection projection, out string reason)
        {
            reason = null;
            if (schema != 1 && schema != 2 || targetModel != model || targetModel == null || footProjection != projection
                || projection == null || !projection.Validate(model, out reason))
                reason ??= "Restore the paired Camp Loop continuation review.";
            else if (NativeCampGround && (nativeBodyProjection == null || !nativeBodyProjection.Validate(model,out reason)))
                reason ??= "Native camp grounding requires the paired whole-body projection.";
            else if (!float.IsFinite(maximumSlope + maximumSupportResidual + maximumAirSpeed)
                || maximumSlope < 0 || maximumSlope > (NativeCampGround ? 40 : 10) || maximumSupportResidual <= 0 || maximumSupportResidual > .015f
                || maximumAirSpeed < 6 || maximumAirSpeed > 6.15f || left == null || right == null || left.clip == right.clip
                || !left.Validate(projection) || !right.Validate(projection))
                reason = "The continuation review exceeds its measured source/support bounds.";
            return reason == null;
        }
    }
}
