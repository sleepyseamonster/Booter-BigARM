using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DCargoAccess
    {
        public static bool CanAccess(TopDown3DBigArmCargo cargo, Transform actor)
        {
            return cargo != null && actor != null && cargo.isActiveAndEnabled
                && cargo.gameObject.scene == actor.gameObject.scene
                && cargo.IsInAccessRange(actor.position) && HasLineOfSight(actor, cargo.transform);
        }

        public static bool HasLineOfSight(Transform actor, Transform target)
        {
            if (actor == null || target == null) return false;
            var from = actor.position + Vector3.up * 0.5f;
            var delta = target.position + Vector3.up * 0.5f - from;
            if (delta.sqrMagnitude < 0.0001f) return true;
            foreach (var hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(actor) && !hit.transform.IsChildOf(target)) return false;
            return true;
        }
    }
}
