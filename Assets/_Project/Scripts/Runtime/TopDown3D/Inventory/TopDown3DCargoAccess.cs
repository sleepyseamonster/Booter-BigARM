using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DCargoAccess
    {
        [ThreadStatic] private static RaycastHit[] lineOfSightHits;
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
            lineOfSightHits ??= new RaycastHit[32];
            var direction = delta.normalized;
            var distance = delta.magnitude;
            int count;
            // A full buffer is incomplete: grow and retry before deciding that ignored
            // actor/carrier hits are the only obstacles. The warmed query allocates nothing.
            while ((count = Physics.RaycastNonAlloc(from, direction, lineOfSightHits, distance, ~0, QueryTriggerInteraction.Ignore)) == lineOfSightHits.Length)
                Array.Resize(ref lineOfSightHits, checked(lineOfSightHits.Length * 2));
            for (var i = 0; i < count; i++)
            {
                var hit = lineOfSightHits[i];
                if (!hit.transform.IsChildOf(actor) && !hit.transform.IsChildOf(target)) return false;
            }
            return true;
        }
    }
}
